// Runtime/PieBridge.cs
// Manages the PuerTS V8 JsEnv lifecycle and the C#<->JS message channel.
//
// Lifecycle:
//   1. Initialize() — load core.bytes, create JsEnv (BackendType.V8), inject pieBridge, execute JS
//   2. EditorApplication.update drives JsEnv.Tick() for Promise resolution
//   3. SendToJs(action, json) — call JS pie.handleCSharpMessage(action, data)
//   4. ReceiveFromJs(event, json) — called by JS via pieBridge.sendToUnity(...)
//   5. Dispose() — cleanup JsEnv

using System;
using System.IO;
using UnityEngine;
using Puerts;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pie
{
    public class PieBridge : IDisposable
    {
        private JsEnv _jsEnv;
        private bool _isInitialized = false;
        private bool _isDisposed = false;
        private string _lastError = null;
        private bool _isUnityScriptHostReady = false;
        private bool _isRuntimeHostBridgeReady = false;
        private bool _isRuntimeBridgeReady = false;
        private bool _isTypeScriptCompilerReady = false;

        /// <summary>Fired when JS calls pieBridge.sendToUnity(event, jsonData).</summary>
        public event Action<string, string> OnJsEvent;

        public bool IsInitialized => _isInitialized && !_isDisposed && _jsEnv != null;
        public bool IsUnityScriptHostReady => IsInitialized && _isUnityScriptHostReady;
        public bool IsRuntimeHostBridgeReady => IsInitialized && _isRuntimeHostBridgeReady;
        public bool IsRuntimeBridgeReady => IsInitialized && _isRuntimeBridgeReady;
        public bool IsTypeScriptCompilerReady => IsInitialized && _isTypeScriptCompilerReady;
        public string LastError => _lastError;
        // PuerTS 2.2.2 uses the stable JsEnv + BackendType.V8 path here.
        // pie-unity relies on cooperative script step guards instead of native
        // V8 termination, which is not a product-level safety boundary.
        public bool IsScriptStepTerminationAvailable => false;

        // Singleton so JS can call back via CS.Pie.PieBridge.Instance
        public static PieBridge Instance { get; private set; }
        public static string LastInitializationError { get; private set; }

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        public bool Initialize(string projectRoot = null, PieSettings settings = null)
        {
            if (_isInitialized)
                return true;

            try
            {
                LastInitializationError = "";
                PieDiagnostics.Verbose("Loading bundled Pie runtime...");
                var jsContent = LoadCoreJs();
                if (string.IsNullOrEmpty(jsContent))
                    throw new Exception("Failed to load bundled Pie runtime from Resources/pie/core.bytes.");

                CreateJsEnvironment(jsContent, projectRoot, settings);

#if UNITY_EDITOR
                EditorApplication.update += Tick;
#endif

                _isInitialized = true;
                Instance = this;

                PieDiagnostics.Info($"PieBridge ready (V8, core.bytes {jsContent.Length} chars)");
                return true;
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                LastInitializationError = ex.Message;
                PieDiagnostics.Error($"PieBridge init failed: {ex.Message}\n{ex.StackTrace}");

                DestroyJsEnvironment();
                return false;
            }
        }

        /// <summary>Send a message to the JS Agent. action = "send_message", jsonData = "{\"content\":\"...\"}"</summary>
        public bool SendToJs(string action, string jsonData)
        {
            if (_jsEnv == null || _isDisposed)
            {
                PieDiagnostics.Warning("[PieBridge.SendToJs] Not initialized");
                return false;
            }

            try
            {
                var normalizedJson = NormalizeMessageJson(jsonData);
                if (normalizedJson == null)
                {
                    PieDiagnostics.Error("[PieBridge.SendToJs] BRIDGE_INVALID_JSON: payload is not valid JSON");
                    return false;
                }

                _jsEnv.Eval($"pie.handleCSharpMessage({JsonString(action ?? "")}, {normalizedJson});");
                return true;
            }
            catch (Exception ex)
            {
                PieDiagnostics.Error($"[PieBridge.SendToJs] Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>Called by JS pieBridge.sendToUnity(event, json) — dispatches OnJsEvent.</summary>
        public void ReceiveFromJs(string eventName, string json)
        {
            OnJsEvent?.Invoke(eventName, json);
        }

        public string InvokeUnityScriptHost(string method, string argsJson)
        {
            if (_jsEnv == null || _isDisposed)
                throw new InvalidOperationException("PieBridge is not initialized.");

            RefreshUnityScriptHostReady();
            var script = $"(function(){{var host=globalThis.__pieUnityScriptHost;if(!host||typeof host.invoke!=='function') throw new Error('Unity script host is not ready.'); return host.invoke({JsonString(method ?? "")},{JsonString(argsJson ?? "{}")});}})()";
            return _jsEnv.Eval<string>(script);
        }

        public string InvokeRuntimeHostCapability(string capabilityName, string argsJson)
        {
            if (_jsEnv == null || _isDisposed)
                throw new InvalidOperationException("PieBridge is not initialized.");

            RefreshRuntimeHostBridgeReady();
            var script = $"(function(){{var host=globalThis.__pieRuntimeHostBridge;if(!host||typeof host.invokeCapability!=='function') throw new Error('Runtime host bridge is not ready.'); return host.invokeCapability({JsonString(capabilityName ?? "")},{JsonString(argsJson ?? "{}")});}})()";
            return _jsEnv.Eval<string>(script);
        }

        public string InvokeTypeScriptCompiler(string requestJson)
        {
            if (_jsEnv == null || _isDisposed)
                throw new InvalidOperationException("PieBridge is not initialized.");

            EnsureTypeScriptCompilerLoaded();
            var script = $"(function(){{var compiler=globalThis.__pieTypeScriptCompiler;if(!compiler||typeof compiler.compileJson!=='function') throw new Error('Runtime TypeScript compiler is not ready.'); return compiler.compileJson({JsonString(requestJson ?? "{}")});}})()";
            return _jsEnv.Eval<string>(script);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

#if UNITY_EDITOR
            EditorApplication.update -= Tick;
#endif

            // Domain reload can happen while a streaming request is still in flight.
            // Cancel all background bridge work before tearing down JsEnv so old-domain
            // tasks do not block assembly reload.
            PieHttpBridge.CancelAllRequests();
            PieFileBridge.CancelAllRequests();

            DestroyJsEnvironment();

            if (Instance == this) Instance = null;
            PieDiagnostics.Info("[PieBridge] Disposed");
        }

        /// <summary>Drive JS microtasks and SSE delivery from a host update loop.</summary>
        public void Tick()
        {
            if (_isDisposed || _jsEnv == null) return;

            try
            {
                // Pump Unity-backed async bridges before ticking JS so queued data
                // is visible to the same frame's microtasks and event handlers.
                PieHttpBridge.PumpActiveRequests();
                PushSSEData();
                PushFileData();
                _jsEnv.Eval("(function(){var host=globalThis.__pieUnityScriptHost;if(host&&typeof host.tick==='function') host.tick();})()");
                RefreshUnityScriptHostReady();
                RefreshRuntimeHostBridgeReady();
                RefreshRuntimeBridgeReady();
                _jsEnv.Tick();
            }
            catch (Exception ex)
            {
                PieDiagnostics.Error($"[PieBridge.Tick] {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Internal
        // ─────────────────────────────────────────────────────────────────────

        private void CreateJsEnvironment(string jsContent, string projectRoot, PieSettings settings)
        {
            PieDiagnostics.Verbose("Creating JsEnv (PuerTS 2.2.2 BackendType.V8)...");
            _jsEnv = new JsEnv(new DefaultLoader(), -1, BackendType.V8, IntPtr.Zero, IntPtr.Zero);

            PieDiagnostics.Verbose("Injecting pieBridge...");
            InjectPieBridge(projectRoot, settings);

            PieDiagnostics.Verbose("Executing bundled Pie runtime...");
            _jsEnv.Eval(jsContent);

            if (!VerifyPieObject())
                throw new Exception("pie.handleCSharpMessage not found after bundled Pie runtime execution");

            RefreshUnityScriptHostReady();
            RefreshRuntimeHostBridgeReady();
            RefreshRuntimeBridgeReady();
            if (!_isUnityScriptHostReady)
                throw new Exception("Unity script host was not installed by the bundled Pie runtime.");
        }

        private void DestroyJsEnvironment()
        {
            try
            {
                _jsEnv?.Dispose();
            }
            catch (Exception ex)
            {
                PieDiagnostics.Warning($"[PieBridge] JsEnv dispose failed: {ex.Message}");
            }
            _jsEnv = null;
            _isUnityScriptHostReady = false;
            _isRuntimeHostBridgeReady = false;
            _isRuntimeBridgeReady = false;
            _isTypeScriptCompilerReady = false;
            PieUnityCapabilityRegistry.ResetRuntimeHosts();
        }

        private string LoadCoreJs()
        {
            var textAsset = Resources.Load<TextAsset>("pie/core");
            if (textAsset != null)
            {
                PieDiagnostics.Verbose($"Loaded bundled Pie runtime via Resources.Load from Resources/pie/core.bytes ({textAsset.text.Length} chars)");
                return textAsset.text;
            }

#if UNITY_EDITOR
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(PieBridge).Assembly);
            var resolvedPackagePath = packageInfo != null ? packageInfo.resolvedPath : "";

            // Development fallback for local authoring/package layouts when the
            // canonical TextAsset resource has not been imported yet.
            string[] candidates = {
                string.IsNullOrEmpty(resolvedPackagePath) ? "" : Path.Combine(resolvedPackagePath, "Resources", "pie", "core.bytes"),
                Path.Combine(projectRoot, "Packages", "com.pie.agent", "Resources", "pie", "core.bytes"),
                Path.Combine(projectRoot, "Packages", "pie-unity", "Resources", "pie", "core.bytes"),
            };

            foreach (var path in candidates)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                PieDiagnostics.Verbose($"Trying: {path}");
                if (File.Exists(path))
                {
                    try
                    {
                        var content = File.ReadAllText(path);
                        PieDiagnostics.Verbose($"Loaded bundled Pie runtime from development fallback path: {path}");
                        return content;
                    }
                    catch (Exception ex)
                    {
                        PieDiagnostics.Warning($"Failed to read {path}: {ex.Message}");
                    }
                }
            }

            // Try PackageCache (installed via git/tarball)
            var cacheDir = Path.Combine(projectRoot, "Library", "PackageCache");
            if (Directory.Exists(cacheDir))
            {
                foreach (var dir in Directory.GetDirectories(cacheDir, "com.pie.agent@*"))
                {
                    var path = Path.Combine(dir, "Resources", "pie", "core.bytes");
                    if (File.Exists(path))
                    {
                        try
                        {
                            PieDiagnostics.Verbose($"Loaded bundled Pie runtime from PackageCache fallback path: {path}");
                            return File.ReadAllText(path);
                        }
                        catch { /* continue */ }
                    }
                }
            }
#endif
            PieDiagnostics.Warning("Bundled Pie runtime TextAsset was not found at Resources/pie/core.bytes.");
            return null;
        }

        public static string LoadResourceText(string resourcePath)
        {
            var normalizedResourcePath = NormalizeResourcePath(resourcePath);
            if (string.IsNullOrEmpty(normalizedResourcePath))
                return "";

            var textAsset = Resources.Load<TextAsset>(normalizedResourcePath);
            if (textAsset != null)
                return textAsset.text;

#if UNITY_EDITOR
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? "";
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(PieBridge).Assembly);
            var resolvedPackagePath = packageInfo != null ? packageInfo.resolvedPath : "";
            var relativeBytesPath = Path.Combine("Resources", normalizedResourcePath + ".bytes");

            string[] candidates = {
                string.IsNullOrEmpty(resolvedPackagePath) ? "" : Path.Combine(resolvedPackagePath, relativeBytesPath),
                Path.Combine(projectRoot, "Packages", "com.pie.agent", relativeBytesPath),
                Path.Combine(projectRoot, "Packages", "pie-unity", relativeBytesPath),
            };

            foreach (var path in candidates)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                if (File.Exists(path))
                {
                    try
                    {
                        return File.ReadAllText(path);
                    }
                    catch (Exception ex)
                    {
                        PieDiagnostics.Warning($"Failed to read {path}: {ex.Message}");
                    }
                }
            }

            var cacheDir = Path.Combine(projectRoot, "Library", "PackageCache");
            if (Directory.Exists(cacheDir))
            {
                foreach (var dir in Directory.GetDirectories(cacheDir, "com.pie.agent@*"))
                {
                    var path = Path.Combine(dir, relativeBytesPath);
                    if (!File.Exists(path))
                        continue;
                    try
                    {
                        return File.ReadAllText(path);
                    }
                    catch (Exception ex)
                    {
                        PieDiagnostics.Warning($"Failed to read PackageCache resource {path}: {ex.Message}");
                    }
                }
            }
#endif

            return "";
        }

        private static string NormalizeResourcePath(string resourcePath)
        {
            var normalized = (resourcePath ?? "").Replace("\\", "/").Trim();
            if (normalized.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(0, normalized.Length - ".bytes".Length);
            return normalized.Trim('/');
        }

        private void EnsureTypeScriptCompilerLoaded()
        {
            if (_isTypeScriptCompilerReady)
                return;

            var alreadyLoaded = _jsEnv.Eval<bool>("(function(){return !!(globalThis.__pieTypeScriptCompiler&&typeof globalThis.__pieTypeScriptCompiler.compileJson==='function');})()");
            if (alreadyLoaded)
            {
                _isTypeScriptCompilerReady = true;
                return;
            }

            var compilerJs = LoadResourceText("pie/typescript-compiler");
            if (string.IsNullOrEmpty(compilerJs))
                throw new Exception("Failed to load bundled TypeScript compiler from Resources/pie/typescript-compiler.bytes.");

            _jsEnv.Eval(compilerJs);
            alreadyLoaded = _jsEnv.Eval<bool>("(function(){return !!(globalThis.__pieTypeScriptCompiler&&typeof globalThis.__pieTypeScriptCompiler.compileJson==='function');})()");
            if (!alreadyLoaded)
                throw new Exception("Runtime TypeScript compiler did not register globalThis.__pieTypeScriptCompiler.");

            _isTypeScriptCompilerReady = true;
        }

        private void InjectPieBridge(string projectRoot, PieSettings settings)
        {
            var isEditor = false;
#if UNITY_EDITOR
            isEditor = true;
            if (string.IsNullOrEmpty(projectRoot))
                projectRoot = Directory.GetParent(Application.dataPath).FullName
                    .Replace("\\", "/");
#else
            if (string.IsNullOrEmpty(projectRoot))
                projectRoot = Application.persistentDataPath.Replace("\\", "/");
#endif
            if (!Directory.Exists(projectRoot))
                Directory.CreateDirectory(projectRoot);

            var isEditorJs = isEditor ? "true" : "false";
            var extensionSearchPathsJson = JsonString(PieProjectPaths.GetExtensionSearchPathsJson(projectRoot, settings));
            var skillSearchPathsJson = JsonString(PieProjectPaths.GetSkillSearchPathsJson(projectRoot, settings));
            var fileToolRootsJson = JsonString(PieProjectPaths.GetFileToolRootsJson(projectRoot, settings, isEditor));
            var initCode = $@"
(function() {{
    globalThis.pieBridge = {{
        projectRoot: {JsonString(projectRoot)},
        isEditor: {isEditorJs},
        extensionSearchPathsJson: {extensionSearchPathsJson},
        skillSearchPathsJson: {skillSearchPathsJson},
        fileToolRootsJson: {fileToolRootsJson},

        getProjectRoot: function() {{
            return globalThis.pieBridge.projectRoot;
        }},

        getProjectAgentsPath: function() {{
            return CS.Pie.PieProjectPaths.GetProjectAgentsPath(globalThis.pieBridge.projectRoot);
        }},

        getPersistentDataPath: function() {{
            return String(CS.UnityEngine.Application.persistentDataPath || '').replace(/\\/g, '/');
        }},

        getRuntimeStateRoot: function() {{
            return CS.Pie.PieProjectPaths.GetRuntimeStateRoot();
        }},

        getSessionsDirectory: function() {{
            return CS.Pie.PieProjectPaths.GetSessionsDirectory();
        }},

        getExtensionSearchPathsJson: function() {{
            return globalThis.pieBridge.extensionSearchPathsJson;
        }},

        getSkillSearchPathsJson: function() {{
            return globalThis.pieBridge.skillSearchPathsJson;
        }},

        getFileToolRootsJson: function() {{
            return globalThis.pieBridge.fileToolRootsJson;
        }},

        sendToUnity: function(eventName, data) {{
            try {{
                var json = typeof data === 'string' ? data : JSON.stringify(data);
                var inst = CS.Pie.PieBridge.Instance;
                if (inst) inst.ReceiveFromJs(eventName, json);
            }} catch(e) {{
                console.error('[pieBridge.sendToUnity]', e && e.message || e);
            }}
        }},

        callHost: function(name, data) {{
            try {{
                var json = typeof data === 'string' ? data : JSON.stringify(data || {{}});
                return CS.Pie.PieHostBridge.Invoke(name, json);
            }} catch(e) {{
                throw new Error('[pieBridge.callHost] ' + (e && e.message || e));
            }}
        }},

        log: function(level, msg) {{
            try {{
                switch(level) {{
                    case 'error':   CS.Pie.PieDiagnostics.Error('[JS] ' + msg); break;
                    case 'warn':    CS.Pie.PieDiagnostics.Warning('[JS] ' + msg); break;
                    default:        CS.Pie.PieDiagnostics.Info('[JS] ' + msg); break;
                }}
            }} catch(e) {{ /* ignore */ }}
        }}
    }};

    // Route console → Unity (Verbose/Warning/Error)
    console.log   = function() {{ var m = Array.prototype.join.call(arguments, ' '); CS.Pie.PieDiagnostics.Verbose(m); }};
    console.warn  = function() {{ var m = Array.prototype.join.call(arguments, ' '); CS.Pie.PieDiagnostics.Warning(m); }};
    console.error = function() {{ var m = Array.prototype.join.call(arguments, ' '); CS.Pie.PieDiagnostics.Error(m); }};

    return true;
}})()";

            var ok = _jsEnv.Eval<bool>(initCode);
            if (!ok) throw new Exception("pieBridge injection returned false");
        }

        private bool VerifyPieObject()
        {
            var pieExists = _jsEnv.Eval<bool>("typeof globalThis.pie !== 'undefined'");
            if (!pieExists)
            {
                var keys = _jsEnv.Eval<string>("Object.keys(globalThis).slice(0, 30).join(', ')");
                PieDiagnostics.Warning($"pie not found. globals: {keys}");
                return false;
            }

            var handlerExists = _jsEnv.Eval<bool>(
                "typeof globalThis.pie.handleCSharpMessage === 'function'");
            if (!handlerExists)
            {
                var pieKeys = _jsEnv.Eval<string>("Object.keys(globalThis.pie).join(', ')");
                PieDiagnostics.Warning($"handleCSharpMessage not found. pie keys: {pieKeys}");
                return false;
            }

            return true;
        }

        private void RefreshUnityScriptHostReady()
        {
            if (_jsEnv == null || _isDisposed)
            {
                _isUnityScriptHostReady = false;
                return;
            }

            try
            {
                _isUnityScriptHostReady = _jsEnv.Eval<bool>(
                    "!!(globalThis.__pieUnityScriptHost && typeof globalThis.__pieUnityScriptHost.invoke === 'function' && typeof globalThis.__pieUnityScriptHost.tick === 'function')");
            }
            catch (Exception ex)
            {
                _isUnityScriptHostReady = false;
                PieDiagnostics.Warning($"[PieBridge] Failed to probe Unity script host readiness: {ex.Message}");
            }
        }

        private void RefreshRuntimeHostBridgeReady()
        {
            if (_jsEnv == null || _isDisposed)
            {
                _isRuntimeHostBridgeReady = false;
                return;
            }

            try
            {
                _isRuntimeHostBridgeReady = _jsEnv.Eval<bool>(
                    "!!(globalThis.__pieRuntimeHostBridge && typeof globalThis.__pieRuntimeHostBridge.invokeCapability === 'function' && typeof globalThis.__pieRuntimeHostBridge.hasRegisteredHostBridge === 'function' && globalThis.__pieRuntimeHostBridge.hasRegisteredHostBridge())");
            }
            catch (Exception ex)
            {
                _isRuntimeHostBridgeReady = false;
                PieDiagnostics.Warning($"[PieBridge] Failed to probe runtime host bridge readiness: {ex.Message}");
            }
        }

        private void RefreshRuntimeBridgeReady()
        {
            if (_jsEnv == null || _isDisposed)
            {
                _isRuntimeBridgeReady = false;
                return;
            }

            try
            {
                _isRuntimeBridgeReady = _jsEnv.Eval<bool>(
                    "!!(globalThis.__pieUnityRuntimeBridge && typeof globalThis.__pieUnityRuntimeBridge.has === 'function' && typeof globalThis.__pieUnityRuntimeBridge.get === 'function' && typeof globalThis.__pieUnityRuntimeBridge.members === 'function' && typeof globalThis.__pieUnityRuntimeBridge.call === 'function')");
            }
            catch (Exception ex)
            {
                _isRuntimeBridgeReady = false;
                PieDiagnostics.Warning($"[PieBridge] Failed to probe runtime bridge readiness: {ex.Message}");
            }
        }

        private const int MAX_LINES_PER_FRAME = 3;

        private void PushSSEData()
        {
            foreach (var reqId in PieHttpBridge.GetActiveRequestIds())
            {
                var status = PieHttpBridge.GetStatus(reqId);
                if (status != -1 && !PieHttpBridge.IsStatusPushed(reqId))
                {
                    PieHttpBridge.MarkStatusPushed(reqId);
                    // A successful response can complete (and fail) before this
                    // frame pumps it. Do not smuggle that body-read failure through
                    // the response-header event; the terminal event must follow all
                    // queued SSE lines so protocol markers such as [DONE] win.
                    var error = status >= 200 && status < 300
                        ? null
                        : PieHttpBridge.GetError(reqId);
                    var errorArg = error != null ? JsonString(error) : "null";
                    _jsEnv.Eval($"globalThis._pieSSEStatus({reqId},{status},{errorArg})");
                    return;
                }

                int pushed = 0;
                while (pushed < MAX_LINES_PER_FRAME)
                {
                    if (!PieHttpBridge.TryPollLine(reqId, out var line))
                        break;

                    if (line == null)
                        break;

                    _jsEnv.Eval($"globalThis._pieSSEPush({reqId},{JsonString(line)})");
                    pushed++;
                }

                if (PieHttpBridge.TryGetTerminal(reqId, out var terminalError))
                {
                    var errorArg = terminalError != null ? JsonString(terminalError) : "null";
                    _jsEnv.Eval(
                        $"typeof globalThis._pieSSETerminal === 'function' " +
                        $"? globalThis._pieSSETerminal({reqId},{errorArg}) " +
                        $": globalThis._pieSSEPush({reqId},null)");
                    PieHttpBridge.MarkTerminalPushed(reqId);
                }
            }
        }

        private void PushFileData()
        {
            foreach (var reqId in PieFileBridge.GetActiveRequestIds())
            {
                if (!PieFileBridge.IsRequestComplete(reqId) || PieFileBridge.IsResultPushed(reqId))
                    continue;

                PieFileBridge.MarkResultPushed(reqId);
                var error = PieFileBridge.GetRequestError(reqId);
                var resultJson = PieFileBridge.GetRequestResultJson(reqId);
                var errorArg = error != null ? JsonString(error) : "null";
                var resultArg = resultJson != null ? JsonString(resultJson) : "null";
                _jsEnv.Eval($"globalThis._pieFileComplete({reqId},{resultArg},{errorArg})");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Produce a JSON string literal including surrounding quotes.</summary>
        private static string JsonString(string value)
        {
            if (value == null) return "null";
            return "\"" + value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t")
                + "\"";
        }

        private string NormalizeMessageJson(string value)
        {
            var trimmed = string.IsNullOrWhiteSpace(value) ? "{}" : value.Trim();
            if (!LooksLikeJsonValue(trimmed))
                return null;

            try
            {
                var isValid = _jsEnv.Eval<bool>(
                    $"(function(){{try{{JSON.parse({JsonString(trimmed)});return true;}}catch(e){{return false;}}}})()");
                return isValid ? trimmed : null;
            }
            catch
            {
                return null;
            }
        }

        private static bool LooksLikeJsonValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var first = value[0];
            return first == '{'
                || first == '['
                || first == '"'
                || first == '-'
                || (first >= '0' && first <= '9')
                || value == "true"
                || value == "false"
                || value == "null";
        }
    }
}
