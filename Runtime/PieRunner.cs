using System;
using UnityEngine;
using System.Collections.Generic;

namespace Pie
{
    /// <summary>
    /// Runtime-first entrypoint for Pie Agent.
    /// Attach this component in scene and drive the agent from game code/UI.
    /// </summary>
    public class PieRunner : MonoBehaviour
    {
        public static PieRunner ActiveRunner { get; private set; }
        private PieBridge _bridge;
        private string _lastAssistantText = "";
        private readonly List<DevRpcMessagePayload> _devRpcMessages = new List<DevRpcMessagePayload>();
        private string _runtimeStatusText = "Idle";
        private bool _runtimeIsStreaming;
        private string _lastStructuredErrorMessage = "";
        [SerializeField] private string _projectRootOverride = "";
        [SerializeField] private PieSettings _settingsOverride;

        public PieBridge Bridge => _bridge;
        public bool IsReady => _bridge?.IsInitialized == true;
        public string ProjectRootOverride => _projectRootOverride;
        public PieSettings SettingsOverride => _settingsOverride;
        public string ActiveSessionId { get; private set; } = "";
        public PieSessionSnapshot CurrentSession { get; private set; }
        public PieAgentStatus CurrentStatus { get; private set; } = new PieAgentStatus();

        public event Action<string> OnAssistantMessage;
        public event Action<string> OnAssistantDelta;
        public event Action<string> OnToolStart;
        public event Action<string, string> OnToolEnd;
        public event Action<string> OnTurnEndDetailed;
        public event Action OnTurnEnd;
        public event Action<string> OnError;
        public event Action<PieTimelineItem> OnTimelineItemChanged;
        public event Action<PieSessionSnapshot> OnSessionChanged;
        public event Action<PieAgentStatus> OnStatusChanged;
        public event Action<PieTurnResult> OnTurnFinished;
        public event Action<PieRuntimeError> OnRuntimeError;

        [Serializable]
        private class RunnerStatePayload
        {
            public bool isReady;
            public string projectRootOverride;
            public bool hasSettingsOverride;
            public string lastAssistantText;
            public bool isStreaming;
            public bool isBusy;
            public string statusText;
            public int messageCount;
            public string mode;
        }

        [Serializable]
        private class DevTextPayload
        {
            public string text;
            public int limit = 20;
        }

        [Serializable]
        private class DevRpcMessagesPayload
        {
            public DevRpcMessagePayload[] messages;
        }

        [Serializable]
        private class DevRpcMessagePayload
        {
            public string role;
            public string title;
            public string content;
            public string summary;
            public bool isRunning;
            public bool isError;
        }

        private void Awake()
        {
            ActiveRunner = this;
            PieDevRpcDispatcher.InitializeMainThread();
            PieDevRpcServer.Start();
            PieHostBridge.Register("pie.interaction", HandleInteractionHostCall);
            PieHostBridge.Register("pie.dev_rpc", PieDevRpc.InvokeHostCall);
            PieHostBridge.Register("pie.host_bridge", PieHostBridge.InvokeHostBridge);
            RegisterDevRpcMethods();
            PieUnityCapabilitiesBootstrap.InitializeRuntime(this);
            InitializeBridge();
        }

        private bool InitializeBridge()
        {
            _bridge = new PieBridge();
            _bridge.OnJsEvent += HandleJsEvent;

            var projectRoot = string.IsNullOrWhiteSpace(_projectRootOverride)
                ? null
                : _projectRootOverride;

            if (!_bridge.Initialize(projectRoot, _settingsOverride))
            {
                var err = _bridge.LastError ?? "Bridge init failed";
                PieDiagnostics.Error($"[PieRunner] {err}");
                OnError?.Invoke(err);
                return false;
            }

            return true;
        }

        private void Update()
        {
            PieDevRpcDispatcher.Tick();
            PieUnityCapabilitiesBootstrap.HeartbeatRuntime();
            _bridge?.Tick();
        }

        private void OnDestroy()
        {
            if (ActiveRunner == this)
                ActiveRunner = null;
            PieUnityCapabilitiesBootstrap.ShutdownRuntime();
            PieHostBridge.Unregister("pie.interaction");
            UnregisterDevRpcMethods();
#if UNITY_EDITOR
            PieUnityCapabilitiesBootstrap.InitializeEditor();
#else
            PieDevRpcServer.Stop();
            PieHostBridge.Unregister("pie.dev_rpc");
            PieHostBridge.Unregister("pie.host_bridge");
#endif
            if (_bridge == null) return;
            _bridge.OnJsEvent -= HandleJsEvent;
            _bridge.Dispose();
            _bridge = null;
        }

        private void RegisterDevRpcMethods()
        {
            PieDevRpc.Register("runner.get_state", _ => JsonUtility.ToJson(new RunnerStatePayload
            {
                isReady = IsReady,
                projectRootOverride = _projectRootOverride ?? "",
                hasSettingsOverride = _settingsOverride != null,
                lastAssistantText = _lastAssistantText ?? "",
                isStreaming = _runtimeIsStreaming,
                isBusy = _runtimeIsStreaming,
                statusText = _runtimeStatusText ?? "",
                messageCount = _devRpcMessages.Count,
                mode = "runtime",
            }));
            PieUnityCapabilityRegistry.RegisterRpc(
                "runner.get_state",
                "runtime",
                "Legacy runtime runner state RPC.",
                "runtime",
                true,
                true,
                null,
                new PieUnityParameterDescriptor[0],
                _ => BuildRuntimeRpcStateJson());
        }

        private void UnregisterDevRpcMethods()
        {
            PieDevRpc.Unregister("runner.get_state");
        }

        private string HandleInteractionHostCall(string argsJson)
        {
            var request = JsonUtility.FromJson<PieInteractionRequest>(argsJson ?? "{}") ?? new PieInteractionRequest();
            if (string.Equals(request.type, "notify", StringComparison.Ordinal))
            {
                var message = string.IsNullOrWhiteSpace(request.message) ? "(empty notification)" : request.message;
                switch ((request.level ?? "info").ToLowerInvariant())
                {
                    case "error":
                        Debug.LogError($"[Pie Interaction] {message}");
                        break;
                    case "warning":
                        Debug.LogWarning($"[Pie Interaction] {message}");
                        break;
                    default:
                        Debug.Log($"[Pie Interaction] {message}");
                        break;
                }

                return PieInteractionResponse.ToJson(new PieInteractionResponse
                {
                    type = "notify",
                    acknowledged = true,
                });
            }

            return PieInteractionResponse.Unavailable(
                request.type,
                request.id,
                $"Runtime host does not support interaction request: {request.type}");
        }

        public bool SetConfig(string apiKey, string provider = null, string model = null, string baseUrl = null)
        {
            if (_bridge?.IsInitialized != true) return false;

            var safeApiKey = EscapeJsonString(apiKey ?? "");
            var safeProvider = EscapeJsonString(provider ?? "");
            var safeModel = EscapeJsonString(model ?? "");
            var safeBaseUrl = EscapeJsonString(baseUrl ?? "");
            var json = $"{{\"apiKey\":\"{safeApiKey}\",\"provider\":\"{safeProvider}\",\"model\":\"{safeModel}\",\"baseUrl\":\"{safeBaseUrl}\"}}";
            return _bridge.SendToJs("set_config", json);
        }

        public bool SendChatMessage(string content)
        {
            return SendChatMessage(new PieChatRequest
            {
                content = content ?? "",
                displayContent = content ?? "",
            });
        }

        public bool SendChatMessage(PieChatRequest request)
        {
            if (_bridge?.IsInitialized != true || request == null) return false;
            request.content = request.content ?? "";
            request.displayContent = string.IsNullOrEmpty(request.displayContent)
                ? request.content
                : request.displayContent;
            request.clientTurnId = request.clientTurnId ?? "";
            AddDevRpcMessage("user", "", request.displayContent, "", false, false);
            _runtimeStatusText = "Sending message";
            _runtimeIsStreaming = true;
            return _bridge.SendToJs("send_message", JsonUtility.ToJson(request));
        }

        public bool ResumeSession(string sessionId, bool recoverInterrupted = false)
        {
            if (_bridge?.IsInitialized != true || string.IsNullOrWhiteSpace(sessionId)) return false;
            var payload = new ResumeSessionPayload
            {
                sessionId = sessionId.Trim(),
                recoverInterrupted = recoverInterrupted,
            };
            _runtimeStatusText = "Loading session";
            return _bridge.SendToJs("resume_session", JsonUtility.ToJson(payload));
        }

        public bool NewSession()
        {
            if (_bridge?.IsInitialized != true) return false;
            AddDevRpcMessage("system", "", "/new", "Create new session", false, false);
            return _bridge.SendToJs("new_session", "{}");
        }

        public bool ReloadExtensions()
        {
            if (_bridge?.IsInitialized != true) return false;
            return _bridge.SendToJs("reload_extensions", "{}");
        }

        public bool Abort()
        {
            if (_bridge?.IsInitialized != true) return false;
            return _bridge.SendToJs("abort", "{}");
        }

        public bool Reinitialize(string projectRoot = null)
        {
            _projectRootOverride = projectRoot ?? "";

            if (_bridge != null)
            {
                _bridge.OnJsEvent -= HandleJsEvent;
                _bridge.Dispose();
                _bridge = null;
            }

            _lastAssistantText = "";
            return InitializeBridge();
        }

        private void HandleJsEvent(string eventName, string json)
        {
            switch (eventName)
            {
                case "message_update":
                {
                    _runtimeIsStreaming = true;
                    _runtimeStatusText = "Streaming";
                    var text = ExtractJsonString(json, "text");
                    if (text != null)
                    {
                        OnAssistantMessage?.Invoke(text);
                        if (text.StartsWith(_lastAssistantText, StringComparison.Ordinal))
                        {
                            var delta = text.Substring(_lastAssistantText.Length);
                            if (!string.IsNullOrEmpty(delta))
                                OnAssistantDelta?.Invoke(delta);
                        }
                        else
                        {
                            OnAssistantDelta?.Invoke(text);
                        }
                        _lastAssistantText = text;
                        UpsertLatestAssistantMessage(text);
                    }
                    break;
                }
                case "tool_start":
                {
                    var name = ExtractJsonString(json, "name") ?? "tool";
                    _runtimeStatusText = "Using " + name;
                    AddDevRpcMessage("tool", name, "", "", true, false);
                    OnToolStart?.Invoke(name);
                    break;
                }
                case "tool_end":
                {
                    var name = ExtractJsonString(json, "name") ?? "tool";
                    var error = ExtractJsonString(json, "error");
                    // The runtime event bridge emits resultText/resultJson. Keep
                    // the legacy result fallback for older core.bytes bundles so
                    // existing embedded callers remain compatible during upgrades.
                    var result = ExtractJsonString(json, "resultText");
                    if (string.IsNullOrEmpty(result))
                        result = ExtractJsonString(json, "result");
                    if (string.IsNullOrEmpty(result))
                        result = ExtractJsonString(json, "resultJson");
                    CompleteLatestToolMessage(name, error ?? result ?? "", !string.IsNullOrEmpty(error));
                    OnToolEnd?.Invoke(name, error ?? result ?? "");
                    break;
                }
                case "turn_end":
                {
                    var stopReason = ExtractJsonString(json, "stopReason") ?? "";
                    OnTurnEndDetailed?.Invoke(stopReason);
                    _runtimeIsStreaming = false;
                    _runtimeStatusText = stopReason == "toolUse" ? "Using tool" : "Idle";
                    if (stopReason == "toolUse")
                        break;

                    _lastAssistantText = "";
                    OnTurnEnd?.Invoke();
                    break;
                }
                case "state_event":
                {
                    var detail = ExtractJsonString(json, "detail") ?? ExtractJsonString(json, "name") ?? "";
                    if (!string.IsNullOrWhiteSpace(detail))
                        _runtimeStatusText = detail;
                    break;
                }
                case "timeline_item":
                {
                    var item = JsonUtility.FromJson<PieTimelineItem>(json ?? "{}");
                    if (item != null)
                    {
                        UpsertCurrentTimelineItem(item);
                        OnTimelineItemChanged?.Invoke(item);
                    }
                    break;
                }
                case "session_sync":
                {
                    var snapshot = JsonUtility.FromJson<PieSessionSnapshot>(json ?? "{}");
                    if (snapshot != null)
                    {
                        if (snapshot.messages == null)
                            snapshot.messages = Array.Empty<PieSessionMessage>();
                        if (snapshot.timelineItems == null)
                            snapshot.timelineItems = Array.Empty<PieTimelineItem>();
                        CurrentSession = snapshot;
                        ActiveSessionId = snapshot.id ?? "";
                        OnSessionChanged?.Invoke(snapshot);
                    }
                    break;
                }
                case "status_snapshot":
                {
                    var status = JsonUtility.FromJson<PieAgentStatus>(json ?? "{}");
                    if (status != null)
                    {
                        CurrentStatus = status;
                        _runtimeIsStreaming = string.Equals(status.phase, "responding", StringComparison.Ordinal)
                            || string.Equals(status.phase, "running_tool", StringComparison.Ordinal)
                            || string.Equals(status.turnState, "continuing", StringComparison.Ordinal);
                        _runtimeStatusText = !string.IsNullOrEmpty(status.activeToolName)
                            ? "Using " + status.activeToolName
                            : status.phase ?? "";
                        OnStatusChanged?.Invoke(status);
                    }
                    break;
                }
                case "runtime_error":
                {
                    var error = JsonUtility.FromJson<PieRuntimeError>(json ?? "{}");
                    if (error != null)
                    {
                        _lastStructuredErrorMessage = error.message ?? "";
                        _runtimeIsStreaming = false;
                        _runtimeStatusText = "Error";
                        OnRuntimeError?.Invoke(error);
                    }
                    break;
                }
                case "turn_finished":
                {
                    var result = JsonUtility.FromJson<PieTurnResult>(json ?? "{}");
                    if (result != null)
                    {
                        _runtimeIsStreaming = false;
                        _runtimeStatusText = string.Equals(result.outcome, "completed", StringComparison.Ordinal)
                            ? "Idle"
                            : result.outcome ?? "Idle";
                        _lastAssistantText = "";
                        OnTurnFinished?.Invoke(result);
                    }
                    break;
                }
                case "error":
                {
                    var message = ExtractJsonString(json, "message") ?? json;
                    _runtimeIsStreaming = false;
                    _runtimeStatusText = "Error";
                    AddDevRpcMessage("system", "error", message ?? "", "", false, true);
                    OnError?.Invoke(message);
                    if (!string.Equals(message, _lastStructuredErrorMessage, StringComparison.Ordinal))
                    {
                        OnRuntimeError?.Invoke(new PieRuntimeError
                        {
                            code = "LEGACY_ERROR",
                            category = "agent",
                            message = message ?? "",
                            retryable = false,
                            sessionId = ActiveSessionId,
                        });
                    }
                    _lastStructuredErrorMessage = "";
                    break;
                }
            }
        }

        public string BuildRuntimeRpcStateJson()
        {
            return JsonUtility.ToJson(new RunnerStatePayload
            {
                isReady = IsReady,
                projectRootOverride = _projectRootOverride ?? "",
                hasSettingsOverride = _settingsOverride != null,
                lastAssistantText = _lastAssistantText ?? "",
                isStreaming = _runtimeIsStreaming,
                isBusy = _runtimeIsStreaming,
                statusText = _runtimeStatusText ?? "",
                messageCount = _devRpcMessages.Count,
                mode = "runtime",
            });
        }

        public string DevRpcChatSend(string argsJson)
        {
            var payload = JsonUtility.FromJson<DevTextPayload>(argsJson ?? "{}") ?? new DevTextPayload();
            if (string.IsNullOrWhiteSpace(payload.text))
                throw new InvalidOperationException("text is required.");
            SendChatMessage(payload.text);
            return BuildRuntimeRpcStateJson();
        }

        public string DevRpcChatGetMessages(string argsJson)
        {
            var payload = JsonUtility.FromJson<DevTextPayload>(argsJson ?? "{}") ?? new DevTextPayload();
            var count = Math.Min(Math.Max(1, payload.limit), _devRpcMessages.Count);
            var items = new DevRpcMessagePayload[count];
            var start = Math.Max(0, _devRpcMessages.Count - count);
            for (var i = 0; i < count; i++)
                items[i] = _devRpcMessages[start + i];
            return JsonUtility.ToJson(new DevRpcMessagesPayload
            {
                messages = items,
            });
        }

        private void AddDevRpcMessage(string role, string title, string content, string summary, bool isRunning, bool isError)
        {
            _devRpcMessages.Add(new DevRpcMessagePayload
            {
                role = role ?? "",
                title = title ?? "",
                content = content ?? "",
                summary = summary ?? "",
                isRunning = isRunning,
                isError = isError,
            });
            TrimDevRpcMessages();
        }

        private void UpsertLatestAssistantMessage(string text)
        {
            if (_devRpcMessages.Count > 0)
            {
                var last = _devRpcMessages[_devRpcMessages.Count - 1];
                if (last != null && string.Equals(last.role, "assistant", StringComparison.Ordinal))
                {
                    last.content = text ?? "";
                    last.summary = text ?? "";
                    last.isRunning = _runtimeIsStreaming;
                    last.isError = false;
                    return;
                }
            }

            AddDevRpcMessage("assistant", "", text ?? "", text ?? "", _runtimeIsStreaming, false);
        }

        private void CompleteLatestToolMessage(string toolName, string content, bool isError)
        {
            for (var i = _devRpcMessages.Count - 1; i >= 0; i--)
            {
                var item = _devRpcMessages[i];
                if (item == null || !string.Equals(item.role, "tool", StringComparison.Ordinal))
                    continue;
                if (!string.Equals(item.title, toolName, StringComparison.Ordinal))
                    continue;
                item.content = content ?? "";
                item.summary = content ?? "";
                item.isRunning = false;
                item.isError = isError;
                return;
            }

            AddDevRpcMessage("tool", toolName, content ?? "", content ?? "", false, isError);
        }

        private void TrimDevRpcMessages()
        {
            const int maxMessages = 200;
            if (_devRpcMessages.Count <= maxMessages)
                return;
            _devRpcMessages.RemoveRange(0, _devRpcMessages.Count - maxMessages);
        }

        private void UpsertCurrentTimelineItem(PieTimelineItem item)
        {
            if (CurrentSession == null || item == null)
                return;

            var items = CurrentSession.timelineItems ?? Array.Empty<PieTimelineItem>();
            for (var i = 0; i < items.Length; i++)
            {
                if (items[i] != null && string.Equals(items[i].itemId, item.itemId, StringComparison.Ordinal))
                {
                    items[i] = item;
                    CurrentSession.timelineItems = items;
                    return;
                }
            }

            var expanded = new PieTimelineItem[items.Length + 1];
            Array.Copy(items, expanded, items.Length);
            expanded[items.Length] = item;
            CurrentSession.timelineItems = expanded;
        }

        [Serializable]
        private sealed class ResumeSessionPayload
        {
            public string sessionId;
            public bool recoverInterrupted;
        }

        private static string EscapeJsonString(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        private static string ExtractJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;

            var pattern = $"\"{key}\":\"";
            var start = json.IndexOf(pattern, StringComparison.Ordinal);
            if (start < 0) return null;
            start += pattern.Length;

            var sb = new System.Text.StringBuilder();
            var i = start;
            while (i < json.Length)
            {
                var c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    switch (json[i + 1])
                    {
                        case 'n':  sb.Append('\n'); i += 2; continue;
                        case 'r':  sb.Append('\r'); i += 2; continue;
                        case 't':  sb.Append('\t'); i += 2; continue;
                        case '"':  sb.Append('"');  i += 2; continue;
                        case '\\': sb.Append('\\'); i += 2; continue;
                        default:   sb.Append(json[i + 1]); i += 2; continue;
                    }
                }
                if (c == '"') break;
                sb.Append(c);
                i++;
            }
            return sb.ToString();
        }
    }
}
