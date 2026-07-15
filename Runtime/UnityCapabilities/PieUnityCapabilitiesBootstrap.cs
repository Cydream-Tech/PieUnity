#if PIE_UNITY_SPLIT_SOURCES
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Pie
{
    public static class PieUnityCapabilitiesBootstrap
    {
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);
        private static Func<string, string> _editorResumeSessionHandler;
        private static string _productName = "";
        private static string _unityProductName = "";
        private static string _applicationIdentifier = "";
        private static DateTime _nextEditorHeartbeatUtc = DateTime.MinValue;
        private static DateTime _nextRuntimeHeartbeatUtc = DateTime.MinValue;
        private static string _editorInstanceId = "";
        private static string _editorProjectPath = "";
        private static string _runtimeInstanceId = "";
        private static string _runtimeProjectPath = "";
        public static string ProductName => _productName;
        public static string UnityProductName => _unityProductName;
        public static string ApplicationIdentifier => _applicationIdentifier;

        [Serializable]
        private sealed class TextPayload
        {
            public string text;
            public int limit = 20;
            public string sessionId;
            public string name;
        }

        [Serializable]
        private sealed class LogReadPayload
        {
            public string source = "active";
            public int tailLines = 200;
            public int maxBytes = 65536;
            public string contains = "";
        }

        public static void InitializeEditor()
        {
            var projectPath = GetProjectPath();
            _productName = DeriveProjectName(projectPath);
            CaptureUnityApplicationMetadata();
            _nextEditorHeartbeatUtc = DateTime.UtcNow.Add(HeartbeatInterval);
            var instanceId = BuildInstanceId(projectPath, "editor", _productName);
            _editorInstanceId = instanceId;
            _editorProjectPath = projectPath;
            PieUnityCapabilityRegistry.ConfigureContext(instanceId, projectPath, "editor");
            PieUnityInstanceRegistry.Register(instanceId, projectPath, _productName, "editor", PieDevRpcServer.Port, PieDevRpcServer.AuthToken, GetUnityProductName(), GetApplicationIdentifier());
            RegisterSharedCapabilities(isEditor: true);
            PieUnityEditorAuthoring.RegisterEditorTools();
        }

        public static void InitializeRuntime(PieRunner runner)
        {
            var projectPath = GetProjectPath(runner != null ? runner.ProjectRootOverride : null);
            _productName = DeriveProjectName(projectPath);
            CaptureUnityApplicationMetadata();
            _nextRuntimeHeartbeatUtc = DateTime.UtcNow.Add(HeartbeatInterval);
            var instanceId = BuildInstanceId(projectPath, "runtime", _productName);
            _runtimeInstanceId = instanceId;
            _runtimeProjectPath = projectPath;
            PieUnityCapabilityRegistry.ConfigureContext(instanceId, projectPath, "runtime");
            PieUnityInstanceRegistry.Register(instanceId, projectPath, _productName, "runtime", PieDevRpcServer.Port, PieDevRpcServer.AuthToken, GetUnityProductName(), GetApplicationIdentifier());
            RegisterSharedCapabilities(isEditor: false);
            RegisterRuntimeCapabilities(runner, projectPath);
        }

        public static void Heartbeat()
        {
            if (!string.IsNullOrWhiteSpace(_runtimeInstanceId))
            {
                HeartbeatRuntime();
                return;
            }

            HeartbeatEditor();
        }

        public static void HeartbeatEditor()
        {
            var now = DateTime.UtcNow;
            if (now < _nextEditorHeartbeatUtc)
                return;

            _nextEditorHeartbeatUtc = now.Add(HeartbeatInterval);
            CaptureUnityApplicationMetadata();
            if (string.IsNullOrWhiteSpace(_editorInstanceId))
                return;
            PieUnityInstanceRegistry.Register(
                _editorInstanceId,
                _editorProjectPath,
                _productName,
                "editor",
                PieDevRpcServer.Port,
                PieDevRpcServer.AuthToken,
                GetUnityProductName(),
                GetApplicationIdentifier());
        }

        public static void HeartbeatRuntime()
        {
            var now = DateTime.UtcNow;
            if (now < _nextRuntimeHeartbeatUtc)
                return;

            _nextRuntimeHeartbeatUtc = now.Add(HeartbeatInterval);
            CaptureUnityApplicationMetadata();
            if (string.IsNullOrWhiteSpace(_runtimeInstanceId))
                return;
            PieUnityInstanceRegistry.Register(
                _runtimeInstanceId,
                _runtimeProjectPath,
                _productName,
                "runtime",
                PieDevRpcServer.Port,
                PieDevRpcServer.AuthToken,
                GetUnityProductName(),
                GetApplicationIdentifier());
        }

        public static void Shutdown()
        {
            ShutdownAll();
        }

        public static void ShutdownEditor()
        {
            if (string.IsNullOrWhiteSpace(_editorInstanceId))
                return;

            PieUnityInstanceRegistry.Unregister(_editorInstanceId);
            _editorInstanceId = "";
            _editorProjectPath = "";
            _nextEditorHeartbeatUtc = DateTime.MinValue;
        }

        public static void ShutdownRuntime()
        {
            PieBehaviorTreeRuntime.StopAll();
            if (string.IsNullOrWhiteSpace(_runtimeInstanceId))
                return;

            PieUnityInstanceRegistry.Unregister(_runtimeInstanceId);
            _runtimeInstanceId = "";
            _runtimeProjectPath = "";
            _nextRuntimeHeartbeatUtc = DateTime.MinValue;
        }

        public static void ShutdownAll()
        {
            ShutdownRuntime();
            ShutdownEditor();
        }

        private static void RegisterSharedCapabilities(bool isEditor)
        {
            PieUnityCapabilityRegistry.RegisterRpc(
                "rpc.list",
                "rpc",
                "List registered low-level RPC methods.",
                isEditor ? "editor" : "runtime",
                true,
                false,
                null,
                new PieUnityParameterDescriptor[0],
                _ => BuildRpcListJson());

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_project_inspect",
                "unity",
                "Inspect the current Unity project/runtime context, including render pipeline, active scene, and AGENTS.md status.",
                "editor+runtime",
                true,
                false,
                null,
                new PieUnityParameterDescriptor[0],
                _ => PieUnityContextSnapshot.BuildJson(isEditor, PieUnityCapabilityRegistry.ProjectPath),
                capabilityKind: "inspect");

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_scene_query",
                "unity",
                "Query Unity objects and return structured refs.",
                "editor+runtime",
                true,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "scope", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "name", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "path", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "type", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "limit", type = "integer", required = false },
                },
                PieUnityHostRuntime.QueryJson,
                capabilityKind: "query");

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_scene_object_inspect",
                "unity",
                "Inspect one explicit Unity scene object ref returned by unity_scene_query or unity_scene_object_edit.",
                "editor+runtime",
                true,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "target", type = "object", required = true },
                },
                PieUnityHostRuntime.InspectJson,
                capabilityKind: "inspect");

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_scene_object_edit",
                "unity",
                "Edit one Unity scene object with an explicit mutation patch. Supported actions: create_scene_object, destroy_scene_object, set_transform, set_active, set_parent, set_name, set_tag_layer, add_component, remove_component, set_component_enabled, set_component_property. Existing-object actions require an explicit target ref. Use unity_scene_query/unity_scene_object_inspect for reads.",
                "editor+runtime",
                false,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "action", type = "string", required = true },
                    new PieUnityParameterDescriptor { name = "target", type = "object", required = false },
                    new PieUnityParameterDescriptor { name = "name", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "primitiveType", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "parentRef", type = "object", required = false },
                    new PieUnityParameterDescriptor { name = "parentName", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "active", type = "boolean", required = false },
                    new PieUnityParameterDescriptor { name = "tag", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "layer", type = "integer", required = false },
                    new PieUnityParameterDescriptor { name = "componentType", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "enabled", type = "boolean", required = false },
                    new PieUnityParameterDescriptor { name = "propertyName", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "value", type = "any", required = false },
                    new PieUnityParameterDescriptor { name = "applyPosition", type = "boolean", required = false },
                    new PieUnityParameterDescriptor { name = "x", type = "number", required = false },
                    new PieUnityParameterDescriptor { name = "y", type = "number", required = false },
                    new PieUnityParameterDescriptor { name = "z", type = "number", required = false },
                    new PieUnityParameterDescriptor { name = "applyRotation", type = "boolean", required = false },
                    new PieUnityParameterDescriptor { name = "rotX", type = "number", required = false },
                    new PieUnityParameterDescriptor { name = "rotY", type = "number", required = false },
                    new PieUnityParameterDescriptor { name = "rotZ", type = "number", required = false },
                    new PieUnityParameterDescriptor { name = "applyScale", type = "boolean", required = false },
                    new PieUnityParameterDescriptor { name = "scaleX", type = "number", required = false },
                    new PieUnityParameterDescriptor { name = "scaleY", type = "number", required = false },
                    new PieUnityParameterDescriptor { name = "scaleZ", type = "number", required = false },
                },
                PieUnityHostRuntime.ApplyJson,
                capabilityKind: "mutate");

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_log_read",
                "unity.log",
                "Read Unity/Pie logs for diagnostics. source may be active, editor, player, or pie. Use after compile/runtime errors instead of guessing.",
                "editor+runtime",
                true,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "source", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "tailLines", type = "integer", required = false },
                    new PieUnityParameterDescriptor { name = "maxBytes", type = "integer", required = false },
                    new PieUnityParameterDescriptor { name = "contains", type = "string", required = false },
                },
                ReadUnityLogJson,
                capabilityKind: "inspect");

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_refresh",
                "unity",
                "Refresh Unity host state. In the editor this refreshes assets; in runtime it is a no-op.",
                "editor+runtime",
                false,
                false,
                null,
                new PieUnityParameterDescriptor[0],
                _ => PieUnityHostRuntime.RefreshJson(),
                capabilityKind: "host");

            RegisterScriptCapabilities();
            PieBehaviorTreeBootstrap.RegisterCapabilities(isEditor);
        }

        private static void RegisterRuntimeCapabilities(PieRunner runner, string projectPath)
        {
            if (runner == null)
                return;

            PieUnityCapabilityRegistry.RegisterRpc(
                "runner.get_state",
                "runtime",
                "Get current runtime runner state.",
                "runtime",
                true,
                true,
                null,
                new PieUnityParameterDescriptor[0],
                _ => runner.BuildRuntimeRpcStateJson());

            PieUnityCapabilityRegistry.RegisterTool(
                "chat_send",
                "chat",
                "Send a chat message through the active runtime runner.",
                "runtime",
                false,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "text", type = "string", required = true },
                },
                runner.DevRpcChatSend);

            PieUnityCapabilityRegistry.RegisterTool(
                "chat_get_state",
                "chat",
                "Get runtime chat state.",
                "runtime",
                true,
                false,
                null,
                new PieUnityParameterDescriptor[0],
                _ => runner.BuildRuntimeRpcStateJson());

            PieUnityCapabilityRegistry.RegisterTool(
                "chat_get_messages",
                "chat",
                "Get recent runtime chat messages.",
                "runtime",
                true,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "limit", type = "integer", required = false },
                },
                runner.DevRpcChatGetMessages);
        }

        public static void RegisterEditorWindowCapabilities(
            Func<string> getState,
            Func<string, string> send,
            Func<string, string> getMessages,
            Func<string, string> resumeSession)
        {
#if UNITY_EDITOR
            _editorResumeSessionHandler = resumeSession;
            PieUnityCapabilityRegistry.RegisterTool(
                "chat_send",
                "chat",
                "Send a chat message to Pie Chat.",
                "editor",
                false,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "text", type = "string", required = true },
                },
                send);

            PieUnityCapabilityRegistry.RegisterTool(
                "chat_get_state",
                "chat",
                "Get Pie Chat state.",
                "editor",
                true,
                false,
                null,
                new PieUnityParameterDescriptor[0],
                _ => getState());

            PieUnityCapabilityRegistry.RegisterTool(
                "chat_get_messages",
                "chat",
                "Get recent Pie Chat messages.",
                "editor",
                true,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "limit", type = "integer", required = false },
                },
                getMessages);

            PieUnityCapabilityRegistry.RegisterTool(
                "session_new",
                "session",
                "Create a new session in Pie Chat.",
                "editor",
                false,
                false,
                null,
                new PieUnityParameterDescriptor[0],
                _ => send("{\"text\":\"/new\"}"));

            PieUnityCapabilityRegistry.RegisterTool(
                "session_resume",
                "session",
                "Resume a session by id in Pie Chat.",
                "editor",
                false,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "sessionId", type = "string", required = true },
                },
                ResumeSessionViaChat);

            PieUnityCapabilityRegistry.RegisterTool(
                "session_tree",
                "session",
                "Show the current session tree in Pie Chat.",
                "editor",
                false,
                false,
                null,
                new PieUnityParameterDescriptor[0],
                _ => send("{\"text\":\"/tree\"}"));

#endif
        }

        private static void RegisterScriptCapabilities()
        {
            PieUnityCapabilityRegistry.RegisterTool(
                "unity_script_run",
                "unity.script",
                "Run a JavaScript or single-file TypeScript generator task inside the Unity script host. language defaults to javascript; TypeScript imports are not supported. The script must define export function* run(ctx, args) and yield for multi-frame work. Do not pass C#, shader source, or raw file contents to this tool. It returns only after completion, failure, cancellation, or timeout.",
                "editor+runtime",
                false,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "script", type = "string", required = true },
                    new PieUnityParameterDescriptor { name = "language", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "name", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "entry", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "args", type = "object", required = false },
                    new PieUnityParameterDescriptor { name = "totalTimeoutMs", type = "number", required = false },
                    new PieUnityParameterDescriptor { name = "perStepTimeoutMs", type = "number", required = false },
                    new PieUnityParameterDescriptor { name = "maxFrames", type = "number", required = false },
                },
                InvokeScriptHostRunUnavailable,
                capabilityKind: "script",
                errorCodes: new[]
                {
                    "TYPESCRIPT_COMPILE_ERROR",
                    "SCRIPT_ERROR",
                    "STEP_TIMEOUT",
                    "TOTAL_TIMEOUT",
                    "MAX_FRAMES",
                    "CANCELLED",
                    "UNSUPPORTED_LANGUAGE",
                });
        }

        private static string ResumeSessionViaChat(string argsJson)
        {
            if (_editorResumeSessionHandler == null)
                throw new InvalidOperationException("session_resume is only available in the Unity Editor.");

            return _editorResumeSessionHandler(argsJson);
        }

        public static string BuildResumeSessionCommandJson(string argsJson)
        {
            var payload = JsonUtility.FromJson<TextPayload>(argsJson ?? "{}") ?? new TextPayload();
            var sessionId = payload.sessionId ?? payload.text ?? "";
            sessionId = sessionId.Trim();
            return JsonUtility.ToJson(new TextPayload
            {
                text = string.IsNullOrEmpty(sessionId) ? "/resume" : "/resume " + sessionId,
            });
        }

        private static string InvokeScriptHostRunUnavailable(string argsJson)
        {
            throw new InvalidOperationException("unity_script_run is asynchronous and must be executed through the dev RPC server or the embedded JS tool surface.");
        }

        public static string StartUnityScriptRun(string argsJson)
        {
            return InvokeScriptHost("run_start", argsJson);
        }

        public static string GetUnityScriptRunStatus(string taskId)
        {
            return InvokeScriptHost("run_status", "{\"taskId\":\"" + EscapeJson(taskId ?? "") + "\"}");
        }

        public static string CancelUnityScriptRun(string taskId, string reason)
        {
            return InvokeScriptHost("run_cancel", "{\"taskId\":\"" + EscapeJson(taskId ?? "") + "\",\"reason\":\"" + EscapeJson(reason ?? "") + "\"}");
        }

        private static string EscapeJson(string value)
        {
            return (value ?? "")
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        private static string InvokeScriptHost(string method, string argsJson)
        {
            var bridge = PieBridge.Instance;
            if (bridge == null || !bridge.IsInitialized)
                throw new InvalidOperationException("Unity script host is not initialized.");

            return bridge.InvokeUnityScriptHost(method, argsJson ?? "{}");
        }

        private static string ReadUnityLogJson(string argsJson)
        {
            var payload = JsonUtility.FromJson<LogReadPayload>(argsJson ?? "{}") ?? new LogReadPayload();
            var requestedSource = string.IsNullOrWhiteSpace(payload.source) ? "active" : payload.source.Trim().ToLowerInvariant();
            var resolvedSource = ResolveLogSource(requestedSource);
            var candidates = GetLogCandidates(resolvedSource);
            var selectedPath = "";
            for (var i = 0; i < candidates.Count; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    selectedPath = candidates[i];
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return "{"
                    + "\"source\":\"" + Escape(requestedSource) + "\","
                    + "\"resolvedSource\":\"" + Escape(resolvedSource) + "\","
                    + "\"found\":false,"
                    + "\"path\":\"\","
                    + "\"pieLogPath\":\"" + Escape(NormalizePath(PieDiagnostics.GetLogFilePath())) + "\","
                    + "\"candidates\":[" + JoinQuotedNormalized(candidates) + "],"
                    + "\"content\":\"\""
                    + "}";
            }

            var maxBytes = Mathf.Clamp(payload.maxBytes <= 0 ? 65536 : payload.maxBytes, 1024, 262144);
            var tailLines = Mathf.Clamp(payload.tailLines <= 0 ? 200 : payload.tailLines, 1, 2000);
            var content = ReadTailText(selectedPath, maxBytes);
            if (!string.IsNullOrWhiteSpace(payload.contains))
                content = FilterLinesContaining(content, payload.contains);
            content = TailLines(content, tailLines);
            var fileInfo = new FileInfo(selectedPath);
            var truncated = fileInfo.Exists && fileInfo.Length > maxBytes;

            return "{"
                + "\"source\":\"" + Escape(requestedSource) + "\","
                + "\"resolvedSource\":\"" + Escape(resolvedSource) + "\","
                + "\"found\":true,"
                + "\"path\":\"" + Escape(NormalizePath(selectedPath)) + "\","
                + "\"pieLogPath\":\"" + Escape(NormalizePath(PieDiagnostics.GetLogFilePath())) + "\","
                + "\"bytes\":" + (fileInfo.Exists ? fileInfo.Length : 0) + ","
                + "\"truncated\":" + (truncated ? "true" : "false") + ","
                + "\"content\":\"" + Escape(content) + "\""
                + "}";
        }

        private static string ResolveLogSource(string source)
        {
            if (string.Equals(source, "active", StringComparison.OrdinalIgnoreCase))
            {
#if UNITY_EDITOR
                return "editor";
#else
                return "player";
#endif
            }
            if (string.Equals(source, "editor", StringComparison.OrdinalIgnoreCase)
                || string.Equals(source, "player", StringComparison.OrdinalIgnoreCase)
                || string.Equals(source, "pie", StringComparison.OrdinalIgnoreCase))
                return source.ToLowerInvariant();
            throw new InvalidOperationException("Unsupported log source: " + source);
        }

        private static List<string> GetLogCandidates(string source)
        {
            var candidates = new List<string>();
            if (string.Equals(source, "pie", StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add(PieDiagnostics.GetLogFilePath());
                return candidates;
            }

            if (string.Equals(source, "player", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(Application.consoleLogPath))
                    candidates.Add(Application.consoleLogPath);
                candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "Player.log"));
                candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs", ProductName, "Player.log"));
                candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "unity3d", ProductName, "Player.log"));
                return candidates;
            }

#if UNITY_EDITOR_OSX
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs", "Unity", "Editor.log"));
#elif UNITY_EDITOR_WIN
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor", "Editor.log"));
#elif UNITY_EDITOR_LINUX
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "unity3d", "Editor.log"));
#endif
            if (!string.IsNullOrWhiteSpace(Application.consoleLogPath))
                candidates.Add(Application.consoleLogPath);
            return candidates;
        }

        private static string ReadTailText(string filePath, int maxBytes)
        {
            var info = new FileInfo(filePath);
            var length = info.Exists ? info.Length : 0;
            var bytesToRead = (int)Math.Min(Math.Max(0, length), Math.Max(1, maxBytes));
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (length > bytesToRead)
                    stream.Seek(-bytesToRead, SeekOrigin.End);
                var buffer = new byte[bytesToRead];
                var read = stream.Read(buffer, 0, bytesToRead);
                return Encoding.UTF8.GetString(buffer, 0, read);
            }
        }

        private static string TailLines(string text, int maxLines)
        {
            var lines = (text ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            var start = Math.Max(0, lines.Length - Math.Max(1, maxLines));
            var result = new List<string>();
            for (var i = start; i < lines.Length; i++)
                result.Add(lines[i]);
            return string.Join("\n", result.ToArray());
        }

        private static string FilterLinesContaining(string text, string contains)
        {
            var lines = (text ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            var result = new List<string>();
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
                    result.Add(lines[i]);
            }
            return string.Join("\n", result.ToArray());
        }

        private static string BuildRpcListJson()
        {
            var descriptors = PieUnityCapabilityRegistry.GetManifestEntries(null, null);
            var items = new List<string>();
            for (var i = 0; i < descriptors.Length; i++)
            {
                var item = descriptors[i];
                if (!string.Equals(item.kind, "rpc", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (item.deprecated)
                    continue;
                items.Add(item.name);
            }

            return "{\"methods\":[" + JoinQuoted(items) + "]}";
        }

        private static string BuildInstanceId(string projectPath, string mode, string productName)
        {
            var safeProject = Regex.Replace(productName ?? "PieUnity", "[^a-zA-Z0-9]", "");
            var hash = ComputeStableHash(projectPath ?? "");
            return $"{safeProject}_{mode}_{hash}";
        }

        private static string DeriveProjectName(string projectPath)
        {
            var normalized = NormalizePath(projectPath).TrimEnd('/');
            var name = Path.GetFileName(normalized);
            return string.IsNullOrWhiteSpace(name) ? "PieUnity" : name;
        }

        private static string GetUnityProductName()
        {
            return string.IsNullOrWhiteSpace(_unityProductName) ? _productName : _unityProductName;
        }

        private static string GetApplicationIdentifier()
        {
            return string.IsNullOrWhiteSpace(_applicationIdentifier) ? "" : _applicationIdentifier;
        }

        private static void CaptureUnityApplicationMetadata()
        {
            try
            {
                _unityProductName = string.IsNullOrWhiteSpace(Application.productName) ? _productName : Application.productName;
                _applicationIdentifier = string.IsNullOrWhiteSpace(Application.identifier) ? "" : Application.identifier;
            }
            catch
            {
                if (string.IsNullOrWhiteSpace(_unityProductName))
                    _unityProductName = _productName;
                if (_applicationIdentifier == null)
                    _applicationIdentifier = "";
            }
        }

        private static string ComputeStableHash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""));
                return BitConverter.ToString(bytes, 0, 4).Replace("-", "");
            }
        }

        private static string GetProjectPath(string projectRootOverride = null)
        {
            if (!string.IsNullOrWhiteSpace(projectRootOverride))
                return NormalizePath(projectRootOverride);

#if UNITY_EDITOR
            return NormalizePath(Directory.GetParent(Application.dataPath).FullName);
#else
            return NormalizePath(Application.persistentDataPath);
#endif
        }

        private static string NormalizePath(string path)
        {
            return (path ?? "").Replace("\\", "/");
        }

        private static string JoinQuoted(List<string> items)
        {
            var quoted = new string[items.Count];
            for (var i = 0; i < items.Count; i++)
                quoted[i] = "\"" + Escape(items[i]) + "\"";
            return string.Join(",", quoted);
        }

        private static string JoinQuotedNormalized(List<string> items)
        {
            var normalized = new List<string>();
            for (var i = 0; i < items.Count; i++)
                normalized.Add(NormalizePath(items[i]));
            return JoinQuoted(normalized);
        }

        private static string Escape(string text)
        {
            return (text ?? "")
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}

#endif
