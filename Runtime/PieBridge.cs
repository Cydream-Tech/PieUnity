// Runtime/PieBridge.cs
// Manages the PuerTS V8 JsEnv lifecycle and the C#<->JS message channel.
//
// Lifecycle:
//   1. Initialize() — load core.bytes, create JsEnv (BackendType.V8), inject pieBridge, execute JS
//   2. EditorApplication.update drives JsEnv.Tick() for Promise resolution
//   3. SendToJs(action, json) — call JS pie.handleCSharpMessage(action, data)
//   4. ReceiveFromJs(event, json) — called by JS via pieBridge.sendToUnity(...)
//   5. Dispose() — cleanup JsEnv
//
// Hot channels are cached JS delegates, not string Eval: every interpolated
// Eval payload is unique source that V8 must parse again (SendToJs, every SSE
// line, every file completion), and string Eval forced a hand-rolled escaping
// helper. Delegates parse once at bind time and marshal arguments natively.
// Delegate bodies re-resolve their target through globalThis on each call so a
// JS-side reinstall of a host object cannot leave a stale closure behind.

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
        // Snapshot of the inject-time project root and settings so the JS
        // bridge getters can recompute search paths / file-tool roots live
        // (settings edits and play-mode transitions must not require a bridge
        // re-initialization to take effect). Static: pieBridge getters run
        // during Initialize, before the Instance singleton is assigned.
        private static string _injectedProjectRoot = null;
        private static PieSettings _injectedSettings = null;
        private bool _isUnityScriptHostReady = false;
        private bool _isRuntimeHostBridgeReady = false;
        private bool _isRuntimeBridgeReady = false;
        private bool _isTypeScriptCompilerReady = false;

        // Cached JS delegates (bound once per env lifetime; see file header).
        private Action<string, string> _jsHandleCSharpMessage;
        private Func<string, string, string> _jsScriptHostInvoke;
        private Action _jsScriptHostTick;
        private Func<string, string, string> _jsRuntimeHostInvoke;
        private Func<string, string> _jsTypeScriptCompile;
        private Action<int, string> _jsSSEPush;
        private Action<int, int, string> _jsSSEStatus;
        private Action<int, string> _jsSSETerminal;
        private Action<int, string, string> _jsFileComplete;

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
                if (_jsHandleCSharpMessage == null)
                {
                    PieDiagnostics.Error("[PieBridge.SendToJs] handleCSharpMessage delegate is not bound");
                    return false;
                }

                _jsHandleCSharpMessage(action ?? "", normalizedJson);
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
            if (!_isUnityScriptHostReady || _jsScriptHostInvoke == null)
                throw new InvalidOperationException("Unity script host is not ready.");
            return _jsScriptHostInvoke(method ?? "", argsJson ?? "{}");
        }

        public string InvokeRuntimeHostCapability(string capabilityName, string argsJson)
        {
            if (_jsEnv == null || _isDisposed)
                throw new InvalidOperationException("PieBridge is not initialized.");

            RefreshRuntimeHostBridgeReady();
            if (!_isRuntimeHostBridgeReady || _jsRuntimeHostInvoke == null)
                throw new InvalidOperationException("Runtime host bridge is not ready.");
            return _jsRuntimeHostInvoke(capabilityName ?? "", argsJson ?? "{}");
        }

        public string InvokeTypeScriptCompiler(string requestJson)
        {
            if (_jsEnv == null || _isDisposed)
                throw new InvalidOperationException("PieBridge is not initialized.");

            EnsureTypeScriptCompilerLoaded();
            if (_jsTypeScriptCompile == null)
                throw new Exception("Runtime TypeScript compiler delegate is not bound.");
            return _jsTypeScriptCompile(requestJson ?? "{}");
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
            PieWebSocketBridge.CancelAll();
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
                _jsScriptHostTick?.Invoke();
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
            RegisterDelegateSignatures();

            PieDiagnostics.Verbose("Injecting pieBridge...");
            InjectPieBridge(projectRoot, settings);

            PieDiagnostics.Verbose("Executing bundled Pie runtime...");
            _jsEnv.Eval(jsContent);

            if (!VerifyPieObject())
                throw new Exception("pie.handleCSharpMessage not found after bundled Pie runtime execution");

            BindCoreDelegates();

            RefreshUnityScriptHostReady();
            RefreshRuntimeHostBridgeReady();
            RefreshRuntimeBridgeReady();
            if (!_isUnityScriptHostReady)
                throw new Exception("Unity script host was not installed by the bundled Pie runtime.");
        }

        // Single ABI registration list for every generic Action/Func Eval in this
        // class. Required by the PuerTS 2.2.2 ungenerated Editor fallback, even
        // for reference-only core delegates; harmless on the xIl2cpp path.
        // Non-generic Action (script-host tick) is built into GenericDelegate:
        // PuerTS 2.2.2 has no parameterless UsingAction() API.
        // check-unity-binding-surface.mjs gates coverage and boot ordering.
        private void RegisterDelegateSignatures()
        {
            _jsEnv.UsingAction<string, string>();
            _jsEnv.UsingFunc<string, string, string>();
            _jsEnv.UsingFunc<string, string>();
            _jsEnv.UsingAction<int, string>();
            _jsEnv.UsingAction<int, int, string>();
            _jsEnv.UsingAction<int, string, string>();
        }

        /// <summary>
        /// Cache JS entry points as delegates so hot paths neither re-parse an
        /// Eval string per call nor need string escaping for arguments. The
        /// delegate bodies re-resolve through globalThis on every invocation,
        /// which keeps them correct if JS reinstalls one of the host objects.
        /// The script host is part of the boot contract (CreateJsEnvironment
        /// throws when it is missing), so failing to bind it is fatal here.
        /// </summary>
        private void BindCoreDelegates()
        {
            // The historical channel delivered action+payload as an embedded JS
            // literal (parsed object). Keep that contract: the shim parses the
            // JSON string before invoking the handler, so the TS side is
            // untouched. Malformed JSON now surfaces as a JS exception from the
            // shim instead of breaking generated Eval source.
            _jsHandleCSharpMessage = _jsEnv.Eval<Action<string, string>>(
                "(function(){var p=globalThis.pie;if(!p||typeof p.handleCSharpMessage!=='function')return null;" +
                "return function(action,json){globalThis.pie.handleCSharpMessage(action,JSON.parse(json));};})()");
            if (_jsHandleCSharpMessage == null)
                throw new Exception("Failed to bind pie.handleCSharpMessage delegate.");

            _jsScriptHostInvoke = _jsEnv.Eval<Func<string, string, string>>(
                "(function(){var h=globalThis.__pieUnityScriptHost;return h&&typeof h.invoke==='function'" +
                "?function(m,a){return globalThis.__pieUnityScriptHost.invoke(m,a);}:null;})()");
            _jsScriptHostTick = _jsEnv.Eval<Action>(
                "(function(){var h=globalThis.__pieUnityScriptHost;return h&&typeof h.tick==='function'" +
                "?function(){globalThis.__pieUnityScriptHost.tick();}:null;})()");
            _isUnityScriptHostReady = _jsScriptHostInvoke != null && _jsScriptHostTick != null;
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
            _jsHandleCSharpMessage = null;
            _jsScriptHostInvoke = null;
            _jsScriptHostTick = null;
            _jsRuntimeHostInvoke = null;
            _jsTypeScriptCompile = null;
            _jsSSEPush = null;
            _jsSSEStatus = null;
            _jsSSETerminal = null;
            _jsFileComplete = null;
            _isUnityScriptHostReady = false;
            _isRuntimeHostBridgeReady = false;
            _isRuntimeBridgeReady = false;
            _isTypeScriptCompilerReady = false;
            PieUnityCapabilityRegistry.ResetRuntimeHosts();
        }

        private string LoadCoreJs()
        {
#if UNITY_EDITOR
            // In the editor, prefer reading the package source file directly:
            // content edits to file: package assets (which live outside the
            // project folder) do not reliably trigger re-import, so the
            // imported TextAsset can lag behind the built bundle.
            var editorPackageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(PieBridge).Assembly);
            var editorPackagePath = editorPackageInfo != null ? editorPackageInfo.resolvedPath : "";
            if (!string.IsNullOrEmpty(editorPackagePath))
            {
                var sourcePath = Path.Combine(editorPackagePath, "Resources", "pie", "core.bytes");
                if (File.Exists(sourcePath))
                {
                    try
                    {
                        var sourceContent = File.ReadAllText(sourcePath);
                        PieDiagnostics.Verbose($"Loaded bundled Pie runtime from package source: {sourcePath}");
                        return sourceContent;
                    }
                    catch (Exception ex)
                    {
                        PieDiagnostics.Warning($"Failed to read package source {sourcePath}: {ex.Message}");
                    }
                }
            }
#endif
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
            if (!alreadyLoaded)
            {
                var compilerJs = LoadResourceText("pie/typescript-compiler");
                if (string.IsNullOrEmpty(compilerJs))
                    throw new Exception("Failed to load bundled TypeScript compiler from Resources/pie/typescript-compiler.bytes.");

                _jsEnv.Eval(compilerJs);
                alreadyLoaded = _jsEnv.Eval<bool>("(function(){return !!(globalThis.__pieTypeScriptCompiler&&typeof globalThis.__pieTypeScriptCompiler.compileJson==='function');})()");
                if (!alreadyLoaded)
                    throw new Exception("Runtime TypeScript compiler did not register globalThis.__pieTypeScriptCompiler.");
            }

            _jsTypeScriptCompile = _jsEnv.Eval<Func<string, string>>(
                "(function(){var c=globalThis.__pieTypeScriptCompiler;return c&&typeof c.compileJson==='function'" +
                "?function(j){return globalThis.__pieTypeScriptCompiler.compileJson(j);}:null;})()");
            if (_jsTypeScriptCompile == null)
                throw new Exception("Failed to bind TypeScript compiler delegate.");

            _isTypeScriptCompilerReady = true;
        }

        private void InjectPieBridge(string projectRoot, PieSettings settings)
        {
            var isEditor = false;
#if UNITY_EDITOR
            isEditor = !Application.isPlaying;
            if (string.IsNullOrEmpty(projectRoot))
                projectRoot = Directory.GetParent(Application.dataPath).FullName
                    .Replace("\\", "/");
#else
            if (string.IsNullOrEmpty(projectRoot))
                projectRoot = Application.persistentDataPath.Replace("\\", "/");
#endif
            // Snapshot AFTER the default-derivation above: callers may pass a
            // null projectRoot (PieRunner without an override), and the live
            // getters must see the resolved root, not the null they were handed.
            _injectedProjectRoot = projectRoot;
            _injectedSettings = settings;
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

        getRuntimePolicy: function() {{
            var runner = CS.Pie.PieRunner.ActiveRunner;
            return runner ? JSON.parse(runner.GetRuntimePolicyJson()) : {{ mode: 'normal', confirmationPolicy: 'allow', yoloMode: true }};
        }},

        getProjectRoot: function() {{
            return globalThis.pieBridge.projectRoot;
        }},

        getRepoRoot: function() {{
            return CS.Pie.PieProjectPaths.GetRepoRoot(globalThis.pieBridge.projectRoot);
        }},

        getProjectAgentsPathsJson: function() {{
            return CS.Pie.PieProjectPaths.GetProjectAgentsPathsJson(globalThis.pieBridge.projectRoot);
        }},

        getBundledSkillsJson: function() {{
            return CS.Pie.PieBridge.GetLiveBundledSkillsJson();
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
            return CS.Pie.PieBridge.GetLiveExtensionSearchPathsJson();
        }},

        getSkillSearchPathsJson: function() {{
            return CS.Pie.PieBridge.GetLiveSkillSearchPathsJson();
        }},

        getFileToolRootsJson: function() {{
            return CS.Pie.PieBridge.GetLiveFileToolRootsJson();
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

        /// <summary>
        /// Live recompute of the JS bridge payloads (skill/extension search
        /// paths, file-tool roots) so PieSettings edits and editor play-mode
        /// transitions take effect on the next read instead of requiring a
        /// bridge re-initialization. The inject-time JSON snapshot properties
        /// on pieBridge remain for debugging/compatibility.
        /// </summary>
        private static bool ComputeIsEditorForFileTools()
        {
#if UNITY_EDITOR
            return !UnityEngine.Application.isPlaying;
#else
            return false;
#endif
        }

        public static string GetLiveExtensionSearchPathsJson()
        {
            if (_injectedProjectRoot == null) return WarnUninitializedLivePayload("extension search paths");
            return PieProjectPaths.GetExtensionSearchPathsJson(_injectedProjectRoot, _injectedSettings);
        }

        public static string GetLiveSkillSearchPathsJson()
        {
            if (_injectedProjectRoot == null) return WarnUninitializedLivePayload("skill search paths");
            return PieProjectPaths.GetSkillSearchPathsJson(_injectedProjectRoot, _injectedSettings);
        }

        public static string GetLiveFileToolRootsJson()
        {
            if (_injectedProjectRoot == null) return WarnUninitializedLivePayload("file-tool roots");
            return PieProjectPaths.GetFileToolRootsJson(_injectedProjectRoot, _injectedSettings, ComputeIsEditorForFileTools());
        }

        /// <summary>
        /// Live payloads require InjectPieBridge to have run in this domain
        /// (with domain-reload disabled a fresh domain may read before any
        /// inject). Returning "[]" with a warning is safer than feeding the
        /// path resolvers an empty project root, which would resolve against
        /// the process working directory.
        /// </summary>
        private static string WarnUninitializedLivePayload(string what)
        {
            Debug.LogWarning($"[Pie] Live {what} requested before PieBridge injection; returning empty set.");
            return "[]";
        }

        public static string GetLiveBundledSkillsJson()
        {
#if UNITY_EDITOR
            var isEditor = !UnityEngine.Application.isPlaying;
#else
            var isEditor = false;
#endif
            return PieSkillManifest.ToJsonForMode(isEditor);
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
            // Bound delegates do not regress within one env lifetime: nothing in
            // the bundle uninstalls the script host, and DestroyJsEnvironment
            // resets the flag. Skip the per-frame Eval probe once ready.
            if (_isUnityScriptHostReady)
                return;

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
            if (_isRuntimeHostBridgeReady && _jsRuntimeHostInvoke != null)
                return;

            try
            {
                _isRuntimeHostBridgeReady = _jsEnv.Eval<bool>(
                    "!!(globalThis.__pieRuntimeHostBridge && typeof globalThis.__pieRuntimeHostBridge.invokeCapability === 'function' && typeof globalThis.__pieRuntimeHostBridge.hasRegisteredHostBridge === 'function' && globalThis.__pieRuntimeHostBridge.hasRegisteredHostBridge())");
                if (_isRuntimeHostBridgeReady)
                {
                    _jsRuntimeHostInvoke = _jsEnv.Eval<Func<string, string, string>>(
                        "(function(){var b=globalThis.__pieRuntimeHostBridge;return b&&typeof b.invokeCapability==='function'" +
                        "?function(n,a){return globalThis.__pieRuntimeHostBridge.invokeCapability(n,a);}:null;})()");
                    _isRuntimeHostBridgeReady = _jsRuntimeHostInvoke != null;
                }
            }
            catch (Exception ex)
            {
                _isRuntimeHostBridgeReady = false;
                _jsRuntimeHostInvoke = null;
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
            // Readiness flag only (no C# caller delegates); bound state does not
            // regress within one env lifetime, so stop probing once ready.
            if (_isRuntimeBridgeReady)
                return;

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

        /// <summary>
        /// SSE/file push handlers are installed by the JS HTTP/file client, which
        /// can happen after env boot. Bind lazily: a C#-side request can only
        /// originate from that same JS client, so by the time anything is queued
        /// the handlers exist and stay bound for the rest of the env lifetime.
        /// </summary>
        private void EnsureSseDelegatesBound()
        {
            if (_jsSSEPush != null && _jsSSEStatus != null)
                return;

            try
            {
                _jsSSEPush = _jsEnv.Eval<Action<int, string>>(
                    "(function(){return typeof globalThis._pieSSEPush==='function'" +
                    "?function(id,line){globalThis._pieSSEPush(id,line);}:null;})()");
                _jsSSEStatus = _jsEnv.Eval<Action<int, int, string>>(
                    "(function(){return typeof globalThis._pieSSEStatus==='function'" +
                    "?function(id,st,err){globalThis._pieSSEStatus(id,st,err);}:null;})()");
                _jsSSETerminal = _jsEnv.Eval<Action<int, string>>(
                    "(function(){return typeof globalThis._pieSSETerminal==='function'" +
                    "?function(id,err){globalThis._pieSSETerminal(id,err);}:null;})()");
            }
            catch (Exception ex)
            {
                PieDiagnostics.Warning($"[PieBridge] SSE delegate binding failed: {ex.Message}");
            }
        }

        private void EnsureFileDelegateBound()
        {
            if (_jsFileComplete != null)
                return;

            try
            {
                _jsFileComplete = _jsEnv.Eval<Action<int, string, string>>(
                    "(function(){return typeof globalThis._pieFileComplete==='function'" +
                    "?function(id,res,err){globalThis._pieFileComplete(id,res,err);}:null;})()");
            }
            catch (Exception ex)
            {
                PieDiagnostics.Warning($"[PieBridge] File delegate binding failed: {ex.Message}");
            }
        }

        private void PushSSEData()
        {
            foreach (var reqId in PieHttpBridge.GetActiveRequestIds())
            {
                EnsureSseDelegatesBound();
                if (_jsSSEPush == null || _jsSSEStatus == null)
                    return; // Nothing marked pushed; delivery resumes once JS installs the handlers.

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
                    _jsSSEStatus(reqId, status, error);
                    return;
                }

                int pushed = 0;
                while (pushed < MAX_LINES_PER_FRAME)
                {
                    if (!PieHttpBridge.TryPollLine(reqId, out var line))
                        break;

                    if (line == null)
                        break;

                    _jsSSEPush(reqId, line);
                    pushed++;
                }

                if (PieHttpBridge.TryGetTerminal(reqId, out var terminalError))
                {
                    if (_jsSSETerminal != null)
                        _jsSSETerminal(reqId, terminalError);
                    else
                        _jsSSEPush(reqId, null);
                    PieHttpBridge.MarkTerminalPushed(reqId);
                }
            }
        }

        private void PushFileData()
        {
            foreach (var reqId in PieFileBridge.GetActiveRequestIds())
            {
                EnsureFileDelegateBound();
                if (_jsFileComplete == null)
                    return; // Nothing marked pushed; delivery resumes once JS installs the handler.

                if (!PieFileBridge.IsRequestComplete(reqId) || PieFileBridge.IsResultPushed(reqId))
                    continue;

                PieFileBridge.MarkResultPushed(reqId);
                var error = PieFileBridge.GetRequestError(reqId);
                var resultJson = PieFileBridge.GetRequestResultJson(reqId);
                _jsFileComplete(reqId, resultJson, error);
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

        /// <summary>
        /// Cheap syntactic gate only. Deep JSON validity is enforced by the JS
        /// shim's JSON.parse: delegate calls marshal the payload as a plain
        /// string argument, so malformed JSON can no longer corrupt generated
        /// Eval source — the reason the old code paid an extra JS roundtrip per
        /// message to pre-validate.
        /// </summary>
        private string NormalizeMessageJson(string value)
        {
            var trimmed = string.IsNullOrWhiteSpace(value) ? "{}" : value.Trim();
            return LooksLikeJsonValue(trimmed) ? trimmed : null;
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
