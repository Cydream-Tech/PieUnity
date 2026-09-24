// Editor/PieChatWindow.cs
// Pie Agent chat window for the Unity Editor.
// Menu: Tools > Pie > Pie Chat  (Alt+Shift+P)

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Pie.Editor
{
    public class PieChatWindow : EditorWindow
    {
        private static PieChatWindow _currentWindow;
        private static readonly Queue<InteractionAutomationResponse> _queuedInteractionResponses = new Queue<InteractionAutomationResponse>();

        // ─── State ────────────────────────────────────────────────────────────
        private PieBridge _bridge;
        private string _inputText = "";
        private Vector2 _chatScrollPos;
        private Vector2 _logScrollPos;
        private List<ChatMessage> _messages = new List<ChatMessage>();
        private bool _isStreaming = false;
        private double _lastRepaintTime = 0;
        private string _statusText = "Idle";
        private bool _followLatest = true;
        private bool _scrollToLatestScheduled = false;
        private int _scrollScheduleVersion = 0;
        private bool _showSkills = false;
        private bool _showSessions = false;
        private Vector2 _skillsScrollPos;
        private Vector2 _sessionsScrollPos;
        private readonly List<SkillInfo> _skills = new List<SkillInfo>();
        private readonly List<SessionInfo> _sessions = new List<SessionInfo>();
        private readonly List<TodoStateItem> _todoItems = new List<TodoStateItem>();
        private string _selectedSkillPath = "";
        private string _selectedSkillName = "";
        private string _skillEditorContent = "";
        private string _newSkillName = "";
        private bool _isCreatingSkill = false;
        private string _activeSessionId = "";
        private string _lastSubmittedInputText = "";
        private string _pendingRecoveryNotice = "";
        private bool _recoveryRestoreScheduled = false;
        private double _recoveryRestoreNotBeforeAt = -1;
        private PendingInteraction _pendingInteraction;
        private int _configAppliedVersion = 0;

        // Settings
        private const string PREF_API_KEY      = "Pie_APIKey";
        private const string PREF_PROVIDER     = "Pie_Provider";
        private const string PREF_MODEL        = "Pie_Model";
        private const string PREF_BASE_URL     = "Pie_BaseUrl";
        private const string PREF_SHOW_LOGS    = "Pie_ShowLogs";
        private const string PREF_VERBOSE_LOGS = "Pie_VerboseLogs";
        private const string PREF_AUTO_RESUME  = "Pie_AutoResume";

        private string _apiKey    = "";
        private string _provider  = "openai";
        private string _model     = "gpt-4.1-mini";
        private string _baseUrl   = "";
        private bool   _showApiKey = false;
        private bool   _showSettings = true;
        private bool   _showLogs = false;
        private bool   _verboseLogs = false;
        private bool   _autoResume = true;

        [Serializable]
        private class DevTextPayload
        {
            public string text;
            public int limit = 20;
        }

        [Serializable]
        private class DevConfigPayload
        {
            public string apiKey;
            public string provider;
            public string model;
            public string baseUrl;
        }

        [Serializable]
        private class ConfigAppliedPayload
        {
            public string provider;
            public string model;
            public string baseUrl;
            public bool verboseLogs;
        }

        [Serializable]
        private class DevSessionPathPayload
        {
            public string sessionId;
        }

        [Serializable]
        private class DevSessionPathResult
        {
            public string sessionsDir;
            public string sessionPath;
            public bool exists;
        }

        [Serializable]
        private class InteractionAutomationResponse
        {
            public string type;
            public string selection;
            public int selectedIndex = -1;
            public string value;
            public bool cancel;
            public bool skip;
            public bool confirmed;
        }

        [Serializable]
        private class InteractionAutomationEnvelope
        {
            public InteractionAutomationResponse response;
        }

        [Serializable]
        private class InteractionStatePayload
        {
            public bool isOpen;
            public bool completed;
            public string id;
            public string type;
            public string prompt;
            public string detail;
            public string[] options;
            public string value;
            public string responseJson;
            public int timeoutMs;
            public int remainingMs;
        }

        // ─── Menu ─────────────────────────────────────────────────────────────
        [MenuItem("Tools/Pie/Pie Chat #&p")]   // Alt+Shift+P
        public static void ShowWindow()
        {
            var window = GetWindow<PieChatWindow>("Pie");
            window.minSize = new Vector2(360, 500);
            window.Show();
        }

        internal static string DevRpcOpen()
        {
            ShowWindow();
            return BuildDevChatStateJson(_currentWindow);
        }

        internal static string DevRpcGetState()
        {
            return BuildDevChatStateJson(_currentWindow);
        }

        internal static string DevRpcSetInput(string argsJson)
        {
            var window = EnsureWindow();
            var payload = JsonUtility.FromJson<DevTextPayload>(argsJson ?? "{}") ?? new DevTextPayload();
            window._inputText = payload.text ?? "";
            window.Repaint();
            return BuildDevChatStateJson(window);
        }

        internal static string DevRpcSend(string argsJson)
        {
            var window = EnsureWindow();
            var payload = JsonUtility.FromJson<DevTextPayload>(argsJson ?? "{}") ?? new DevTextPayload();
            if (!string.IsNullOrEmpty(payload.text))
                window._inputText = payload.text;
            if (window.IsBusy())
            {
                window._statusText = "Busy: request already in progress";
                window.Repaint();
                return BuildDevChatStateJson(window);
            }
            window.SendMessage();
            window.Repaint();
            return BuildDevChatStateJson(window);
        }

        internal static string DevRpcAbort()
        {
            var window = EnsureWindow();
            window.AbortCurrentTurn();
            window.Repaint();
            return BuildDevChatStateJson(window);
        }

        internal static string DevRpcGetMessages(string argsJson)
        {
            var window = EnsureWindow();
            var payload = JsonUtility.FromJson<DevTextPayload>(argsJson ?? "{}") ?? new DevTextPayload();
            return window.GetRecentMessagesJson(Math.Max(1, payload.limit));
        }

        internal static string DevRpcReconnect()
        {
            var window = EnsureWindow();
            window.ConnectBridge();
            window.Repaint();
            return BuildDevChatStateJson(window);
        }

        internal static string DevRpcSetConfig(string argsJson)
        {
            var window = EnsureWindow();
            var payload = JsonUtility.FromJson<DevConfigPayload>(argsJson ?? "{}") ?? new DevConfigPayload();
            var previousVersion = window._configAppliedVersion;

            if (payload.apiKey != null)
            {
                window._apiKey = payload.apiKey;
                EditorPrefs.SetString(PREF_API_KEY, window._apiKey);
            }
            if (payload.provider != null)
            {
                window._provider = payload.provider;
                EditorPrefs.SetString(PREF_PROVIDER, window._provider);
            }
            if (payload.model != null)
            {
                window._model = payload.model;
                EditorPrefs.SetString(PREF_MODEL, window._model);
            }
            if (payload.baseUrl != null)
            {
                window._baseUrl = payload.baseUrl;
                EditorPrefs.SetString(PREF_BASE_URL, window._baseUrl);
            }

            window.PushSettings();
            if (window._configAppliedVersion == previousVersion)
                window._statusText = "Applying config...";
            window.Repaint();
            return BuildDevChatStateJson(window);
        }

        internal static string DevRpcGetSessionPathInfo(string argsJson)
        {
            var payload = JsonUtility.FromJson<DevSessionPathPayload>(argsJson ?? "{}") ?? new DevSessionPathPayload();
            var sessionId = payload.sessionId ?? "";
            var sessionsDir = Pie.PieProjectPaths.GetSessionsDirectory();
            var safeId = System.Text.RegularExpressions.Regex.Replace(sessionId, "[^a-zA-Z0-9_-]", "_");
            var sessionPath = string.IsNullOrEmpty(safeId)
                ? ""
                : System.IO.Path.Combine(sessionsDir, safeId + ".json").Replace("\\", "/");
            var exists = !string.IsNullOrEmpty(sessionPath) && System.IO.File.Exists(sessionPath);
            return JsonUtility.ToJson(new DevSessionPathResult
            {
                sessionsDir = sessionsDir,
                sessionPath = sessionPath,
                exists = exists,
            });
        }

        internal static string DevRpcGetRecoveryState()
        {
            _currentWindow?.TryRunScheduledRecoveryRestore(true);
            return PieChatSessionRecovery.BuildRecoveryStateJson(
                _currentWindow?._recoveryRestoreScheduled == true,
                _currentWindow?._recoveryRestoreNotBeforeAt ?? -1);
        }

        internal static string DevRpcBeginInteraction(string argsJson)
        {
            var window = EnsureWindow();
            var request = JsonUtility.FromJson<PieInteractionRequest>(argsJson ?? "{}") ?? new PieInteractionRequest();
            window.BeginInteraction(request);
            return window.GetInteractionStateJson();
        }

        internal static string DevRpcGetInteractionState(string _argsJson)
        {
            var window = EnsureWindow();
            return window.GetInteractionStateJson();
        }

        internal static string DevRpcRespondInteraction(string argsJson)
        {
            var window = EnsureWindow();
            var response = ParseInteractionAutomationResponse(argsJson);
            window.ApplyInteractionAutomationResponse(response);
            return window.GetInteractionStateJson();
        }

        internal static string DevRpcEnqueueInteractionResponse(string argsJson)
        {
            var response = ParseInteractionAutomationResponse(argsJson);
            _queuedInteractionResponses.Enqueue(response);
            if (_currentWindow != null)
            {
                _currentWindow.TryApplyQueuedInteractionResponse();
                _currentWindow.Repaint();
            }
            return _currentWindow != null ? _currentWindow.GetInteractionStateJson() : JsonUtility.ToJson(new InteractionStatePayload());
        }

        private static InteractionAutomationResponse ParseInteractionAutomationResponse(string argsJson)
        {
            var raw = string.IsNullOrWhiteSpace(argsJson) ? "{}" : argsJson;
            var envelope = JsonUtility.FromJson<InteractionAutomationEnvelope>(raw);
            if (envelope?.response != null)
                return envelope.response;
            return JsonUtility.FromJson<InteractionAutomationResponse>(raw) ?? new InteractionAutomationResponse();
        }

        internal static string DevRpcClearInteractionResponses()
        {
            _queuedInteractionResponses.Clear();
            if (_currentWindow != null)
            {
                return _currentWindow.GetInteractionStateJson();
            }
            return JsonUtility.ToJson(new InteractionStatePayload());
        }

        internal static string DevRpcConsumeCompletedInteraction(string _argsJson)
        {
            if (_currentWindow != null && _currentWindow._pendingInteraction != null && _currentWindow._pendingInteraction.completed)
            {
                _currentWindow.ClearInteraction();
                return _currentWindow.GetInteractionStateJson();
            }
            return _currentWindow != null ? _currentWindow.GetInteractionStateJson() : JsonUtility.ToJson(new InteractionStatePayload());
        }

        private static PieChatWindow EnsureWindow()
        {
            if (_currentWindow != null)
                return _currentWindow;
            ShowWindow();
            return _currentWindow ?? GetWindow<PieChatWindow>("Pie");
        }

        internal static void EnsureDevRpcReady()
        {
            PieDevRpcServer.Start();
            PieDevRpc.Register("pie_chat.open", _ => DevRpcOpen());
            PieDevRpc.Register("pie_chat.get_state", _ => DevRpcGetState());
            PieDevRpc.Register("pie_chat.set_input", argsJson => DevRpcSetInput(argsJson));
            PieDevRpc.Register("pie_chat.send", argsJson => DevRpcSend(argsJson));
            PieDevRpc.Register("pie_chat.abort", _ => DevRpcAbort());
            PieDevRpc.Register("pie_chat.get_messages", argsJson => DevRpcGetMessages(argsJson));
            PieDevRpc.Register("pie_chat.reconnect", _ => DevRpcReconnect());
            PieDevRpc.Register("pie_chat.set_config", argsJson => DevRpcSetConfig(argsJson));
            PieDevRpc.Register("pie_chat.get_session_path_info", argsJson => DevRpcGetSessionPathInfo(argsJson));
            PieDevRpc.Register("pie_chat.get_recovery_state", _ => DevRpcGetRecoveryState());
            PieDevRpc.Register("interaction.begin", argsJson => DevRpcBeginInteraction(argsJson));
            PieDevRpc.Register("interaction.get_state", argsJson => DevRpcGetInteractionState(argsJson));
            PieDevRpc.Register("interaction.respond", argsJson => DevRpcRespondInteraction(argsJson));
            PieDevRpc.Register("interaction.enqueue_response", argsJson => DevRpcEnqueueInteractionResponse(argsJson));
            PieDevRpc.Register("interaction.clear_responses", _ => DevRpcClearInteractionResponses());
            PieDevRpc.Register("interaction.consume_completed", argsJson => DevRpcConsumeCompletedInteraction(argsJson));
            PieUnityCapabilityRegistry.RegisterRpc("pie_chat.open", "chat", "Open Pie Chat and initialize its bridge.", "editor", false, false, null, new PieUnityParameterDescriptor[0], _ => DevRpcOpen());
            PieUnityCapabilityRegistry.RegisterRpc("pie_chat.set_input", "chat", "Set the Pie Chat input text.", "editor", false, false, null, new[]
            {
                new PieUnityParameterDescriptor { name = "text", type = "string", required = true },
            }, DevRpcSetInput);
            PieUnityCapabilityRegistry.RegisterRpc("pie_chat.set_config", "chat", "Set the Pie Chat model configuration.", "editor", false, false, null, new PieUnityParameterDescriptor[0], DevRpcSetConfig);
            PieUnityCapabilityRegistry.RegisterRpc("pie_chat.reconnect", "chat", "Reconnect the Pie Chat bridge.", "editor", false, false, null, new PieUnityParameterDescriptor[0], _ => DevRpcReconnect());
            PieUnityCapabilityRegistry.RegisterRpc("pie_chat.get_session_path_info", "chat", "Inspect the active session path.", "editor", true, false, null, new PieUnityParameterDescriptor[0], DevRpcGetSessionPathInfo);
            PieUnityCapabilityRegistry.RegisterRpc("pie_chat.get_recovery_state", "chat", "Inspect the chat recovery state.", "editor", true, false, null, new PieUnityParameterDescriptor[0], _ => DevRpcGetRecoveryState());
            PieUnityCapabilityRegistry.RegisterRpc("pie_chat.send", "chat", "Legacy Pie Chat send RPC.", "editor", false, true, null, new[]
            {
                new PieUnityParameterDescriptor { name = "text", type = "string", required = true },
            }, DevRpcSend);
            PieUnityCapabilityRegistry.RegisterRpc("pie_chat.abort", "chat", "Abort the active Pie Chat turn.", "editor", false, false, null, new PieUnityParameterDescriptor[0], _ => DevRpcAbort());
            PieUnityCapabilityRegistry.RegisterRpc("pie_chat.get_state", "chat", "Legacy Pie Chat state RPC.", "editor", true, true, null, new PieUnityParameterDescriptor[0], _ => DevRpcGetState());
            PieUnityCapabilityRegistry.RegisterRpc("pie_chat.get_messages", "chat", "Legacy Pie Chat messages RPC.", "editor", true, true, null, new[]
            {
                new PieUnityParameterDescriptor { name = "limit", type = "integer", required = false },
            }, DevRpcGetMessages);
            PieUnityCapabilityRegistry.RegisterRpc("interaction.begin", "interaction", "Legacy interaction begin RPC.", "editor", false, true, null, new PieUnityParameterDescriptor[0], DevRpcBeginInteraction);
            PieUnityCapabilityRegistry.RegisterRpc("interaction.get_state", "interaction", "Legacy interaction state RPC.", "editor", true, true, null, new PieUnityParameterDescriptor[0], DevRpcGetInteractionState);
            PieUnityCapabilityRegistry.RegisterRpc("interaction.respond", "interaction", "Legacy interaction response RPC.", "editor", false, true, null, new PieUnityParameterDescriptor[0], DevRpcRespondInteraction);
            PieUnityCapabilitiesBootstrap.RegisterEditorWindowCapabilities(
                () => DevRpcGetState(),
                DevRpcSend,
                DevRpcGetMessages,
                argsJson => DevRpcSend(PieUnityCapabilitiesBootstrap.BuildResumeSessionCommandJson(argsJson)));
        }

        // ─── Lifecycle ────────────────────────────────────────────────────────
        private void OnEnable()
        {
            _currentWindow = this;
            _recoveryRestoreScheduled = false;
            _recoveryRestoreNotBeforeAt = -1;
            EnsureDevRpcReady();
            _apiKey   = EditorPrefs.GetString(PREF_API_KEY, "");
            _provider = EditorPrefs.GetString(PREF_PROVIDER, "openai");
            _model    = EditorPrefs.GetString(PREF_MODEL, "gpt-4.1-mini");
            _baseUrl  = EditorPrefs.GetString(PREF_BASE_URL, "");
            _showLogs = EditorPrefs.GetBool(PREF_SHOW_LOGS, false);
            _verboseLogs = EditorPrefs.GetBool(PREF_VERBOSE_LOGS, false);
            _autoResume = EditorPrefs.GetBool(PREF_AUTO_RESUME, true);
            PieDiagnostics.CurrentLevel = _verboseLogs ? PieLogLevel.Verbose : PieLogLevel.Info;
            AssemblyReloadEvents.beforeAssemblyReload -= HandleBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += HandleBeforeAssemblyReload;
            EditorApplication.update -= TickRecoveryRestore;
            EditorApplication.update += TickRecoveryRestore;
            PieHostBridge.Register("pie.interaction", HandleInteractionHostCall);

            ConnectBridge();
            if (_autoResume && HasRecoverySnapshot())
                ScheduleRecoveryRestore(PieChatSessionRecovery.RestoreDelaySeconds);
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= HandleBeforeAssemblyReload;
            EditorApplication.update -= TickRecoveryRestore;
            PieHostBridge.Unregister("pie.interaction");
            if (_currentWindow == this)
                _currentWindow = null;
            DisposeBridge();
        }

        private static string BuildDevChatStateJson(PieChatWindow window)
        {
            return PieChatDevRpcPayloads.BuildStateJson(
                window != null,
                window?._bridge?.IsInitialized == true,
                window?._isStreaming == true,
                window?.IsBusy() == true,
                window?._statusText ?? "",
                window?._inputText ?? "",
                window?._messages.Count ?? 0,
                window?._todoItems.Count ?? 0,
                window?._activeSessionId ?? "",
                window?._configAppliedVersion ?? 0);
        }

        private bool IsBusy()
        {
            if (_isStreaming)
                return true;
            if (_pendingInteraction != null)
                return true;
            for (var i = 0; i < _messages.Count; i++)
            {
                if (_messages[i].IsRunning)
                    return true;
            }
            return false;
        }

        private string GetRecentMessagesJson(int limit)
        {
            var count = Math.Min(limit, _messages.Count);
            var items = new PieChatDevRpcPayloads.DevChatMessagePayload[count];
            var start = Math.Max(0, _messages.Count - count);
            for (var i = 0; i < count; i++)
            {
                var msg = _messages[start + i];
                items[i] = new PieChatDevRpcPayloads.DevChatMessagePayload
                {
                    role = msg.Role ?? "",
                    title = msg.Title ?? "",
                    content = msg.Content ?? "",
                    summary = msg.Summary ?? "",
                    isRunning = msg.IsRunning,
                    isError = msg.IsError,
                };
            }

            return PieChatDevRpcPayloads.BuildMessagesJson(items);
        }

        private string HandleInteractionHostCall(string argsJson)
        {
            var request = JsonUtility.FromJson<PieInteractionRequest>(argsJson ?? "{}") ?? new PieInteractionRequest();

            switch (request.type)
            {
                case "notify":
                    AddSystemMessage(request.message ?? "");
                    Repaint();
                    return PieInteractionResponse.ToJson(new PieInteractionResponse
                    {
                        type = "notify",
                        acknowledged = true,
                    });

                case "confirm":
                {
                    var confirmed = EditorUtility.DisplayDialog(
                        "Pie",
                        string.IsNullOrWhiteSpace(request.detail) ? request.prompt : $"{request.prompt}\n\n{request.detail}",
                        "Yes",
                        "No");
                    return PieInteractionResponse.ToJson(new PieInteractionResponse
                    {
                        type = "confirm",
                        id = request.id,
                        confirmed = confirmed,
                    });
                }

                case "select_one":
                {
                    var selection = PieInteractionDialogWindow.ShowSelectOne(request.prompt, request.options ?? new string[0]);
                    return PieInteractionResponse.ToJson(new PieInteractionResponse
                    {
                        type = "select_one",
                        id = request.id,
                        selection = selection,
                    });
                }

                case "text_input":
                {
                    var value = PieInteractionDialogWindow.ShowTextInput(request.prompt, request.placeholder, false);
                    return PieInteractionResponse.ToJson(new PieInteractionResponse
                    {
                        type = "text_input",
                        id = request.id,
                        value = value,
                    });
                }

                case "multiline_input":
                {
                    var value = PieInteractionDialogWindow.ShowTextInput(request.prompt, request.prefill, true);
                    return PieInteractionResponse.ToJson(new PieInteractionResponse
                    {
                        type = "multiline_input",
                        id = request.id,
                        value = value,
                    });
                }
            }

            return PieInteractionResponse.Unavailable(
                request.type,
                request.id,
                $"Editor host does not support interaction request: {request.type}");
        }

        private void BeginInteraction(PieInteractionRequest request)
        {
            _pendingInteraction = new PendingInteraction
            {
                request = request ?? new PieInteractionRequest(),
                openedAt = EditorApplication.timeSinceStartup,
                value = request?.prefill ?? request?.placeholder ?? "",
                selectedIndex = -1,
            };
            _statusText = $"Waiting for user input: {_pendingInteraction.request.type}";
            TryApplyQueuedInteractionResponse();
            Repaint();
        }

        private string GetInteractionStateJson()
        {
            CheckInteractionTimeout();
            var payload = new InteractionStatePayload();
            if (_pendingInteraction != null)
            {
                payload.isOpen = true;
                payload.completed = _pendingInteraction.completed;
                payload.id = _pendingInteraction.request?.id ?? "";
                payload.type = _pendingInteraction.request?.type ?? "";
                payload.prompt = _pendingInteraction.request?.prompt ?? "";
                payload.detail = _pendingInteraction.request?.detail ?? "";
                payload.options = _pendingInteraction.request?.options ?? new string[0];
                payload.value = _pendingInteraction.value ?? "";
                payload.responseJson = _pendingInteraction.responseJson ?? "";
                payload.timeoutMs = Math.Max(0, _pendingInteraction.request?.timeoutMs ?? 0);
                if (payload.timeoutMs > 0)
                {
                    var elapsedMs = (int)((EditorApplication.timeSinceStartup - _pendingInteraction.openedAt) * 1000.0);
                    payload.remainingMs = Math.Max(0, payload.timeoutMs - elapsedMs);
                }
            }
            return JsonUtility.ToJson(payload);
        }

        private void ApplyInteractionAutomationResponse(InteractionAutomationResponse response)
        {
            if (_pendingInteraction == null)
            {
                _queuedInteractionResponses.Enqueue(response);
                return;
            }

            CompleteInteractionFromAutomation(response ?? new InteractionAutomationResponse());
        }

        private void CompleteInteractionFromAutomation(InteractionAutomationResponse response)
        {
            if (_pendingInteraction == null)
                return;

            if (response.cancel)
            {
                CompleteInteraction(BuildCancelledInteractionResponse());
                return;
            }

            if (response.skip)
            {
                CompleteInteraction(BuildSkippedInteractionResponse(false));
                return;
            }

            switch (_pendingInteraction.request.type)
            {
                case "select_one":
                    var selection = response.selection;
                    if ((selection == null || selection.Length == 0)
                        && response.selectedIndex >= 0
                        && _pendingInteraction.request.options != null
                        && response.selectedIndex < _pendingInteraction.request.options.Length)
                    {
                        selection = _pendingInteraction.request.options[response.selectedIndex];
                    }
                    CompleteInteraction(new PieInteractionResponse
                    {
                        type = "select_one",
                        id = _pendingInteraction.request.id,
                        selection = selection,
                    });
                    break;
                case "confirm":
                    CompleteInteraction(new PieInteractionResponse
                    {
                        type = "confirm",
                        id = _pendingInteraction.request.id,
                        confirmed = response.confirmed,
                    });
                    break;
                case "text_input":
                case "multiline_input":
                    _pendingInteraction.value = response.value ?? "";
                    CompleteInteraction(new PieInteractionResponse
                    {
                        type = _pendingInteraction.request.type,
                        id = _pendingInteraction.request.id,
                        value = _pendingInteraction.value,
                    });
                    break;
            }
        }

        private void TryApplyQueuedInteractionResponse()
        {
            if (_pendingInteraction == null || _queuedInteractionResponses.Count == 0)
                return;

            var response = _queuedInteractionResponses.Peek();
            var type = _pendingInteraction.request?.type ?? "";
            if (!string.IsNullOrEmpty(response.type) && !string.Equals(response.type, type, StringComparison.Ordinal))
                return;

            _queuedInteractionResponses.Dequeue();
            CompleteInteractionFromAutomation(response);
        }

        private void CheckInteractionTimeout()
        {
            if (_pendingInteraction == null || _pendingInteraction.completed)
                return;

            var timeoutMs = _pendingInteraction.request?.timeoutMs ?? 0;
            if (timeoutMs <= 0)
                return;

            var elapsedMs = (EditorApplication.timeSinceStartup - _pendingInteraction.openedAt) * 1000.0;
            if (elapsedMs >= timeoutMs)
            {
                CompleteInteraction(BuildSkippedInteractionResponse(true));
            }
        }

        private void ClearInteraction()
        {
            _pendingInteraction = null;
            if (!_isStreaming)
                _statusText = "Idle";
            Repaint();
        }

        private PieInteractionResponse BuildCancelledInteractionResponse()
        {
            var type = _pendingInteraction?.request?.type ?? "notify";
            var id = _pendingInteraction?.request?.id ?? "";
            if (type == "confirm")
            {
                return new PieInteractionResponse { type = "confirm", id = id, confirmed = false };
            }
            return new PieInteractionResponse { type = type, id = id };
        }

        private PieInteractionResponse BuildSkippedInteractionResponse(bool timedOut)
        {
            var type = _pendingInteraction?.request?.type ?? "notify";
            var id = _pendingInteraction?.request?.id ?? "";
            if (type == "confirm")
            {
                return new PieInteractionResponse
                {
                    type = "confirm",
                    id = id,
                    confirmed = false,
                    skipped = true,
                    timedOut = timedOut,
                };
            }
            return new PieInteractionResponse
            {
                type = type,
                id = id,
                skipped = true,
                timedOut = timedOut,
            };
        }

        private void CompleteInteraction(PieInteractionResponse response)
        {
            if (_pendingInteraction == null)
                return;

            _pendingInteraction.completed = true;
            _pendingInteraction.responseJson = PieInteractionResponse.ToJson(response);
            _statusText = response.timedOut
                ? "Input timed out; skipped"
                : response.skipped
                    ? "Input skipped"
                    : "Interaction completed";
            Repaint();
        }

        private void OnInspectorUpdate()
        {
            TickRecoveryRestore();
            CheckInteractionTimeout();
            if (_isStreaming) Repaint();
            if (_pendingInteraction != null && !_pendingInteraction.completed)
                Repaint();
        }

        private void TickRecoveryRestore()
        {
            if (_autoResume && HasRecoverySnapshot() && !_recoveryRestoreScheduled)
                ScheduleRecoveryRestore(PieChatSessionRecovery.RestoreDelaySeconds);

            if (_autoResume && HasRecoverySnapshot())
                TryRunScheduledRecoveryRestore(false);
        }

        private void ConnectBridge()
        {
            DisposeBridge();
            _isStreaming = false;  // reset any stuck streaming state

            _bridge = new PieBridge();
            _bridge.OnJsEvent += HandleJsEvent;

            var ok = _bridge.Initialize();
            if (!ok)
            {
                PieDiagnostics.Error($"Bridge init failed: {_bridge.LastError}");
                AddSystemMessage($"⚠ Bridge failed: {_bridge.LastError}");
            }
            else
            {
                // Push saved settings to JS
                PushSettings();
                _bridge.SendToJs("list_skills", "{}");
                if (_autoResume && HasRecoverySnapshot())
                    ScheduleRecoveryRestore(PieChatSessionRecovery.RestoreDelaySeconds);
            }

            RefreshSessions();
        }

        private void HandleBeforeAssemblyReload()
        {
            PieDiagnostics.Info("[PieChatWindow] beforeAssemblyReload");
            SaveRecoverySnapshotIfNeeded();
            InterruptActiveTurnForAssemblyReload();
            DisposeBridge();
        }

        private void InterruptActiveTurnForAssemblyReload()
        {
            if (_bridge?.IsInitialized == true)
            {
                try
                {
                    _bridge.SendToJs("abort", "{\"reason\":\"domain_reload\"}");
                }
                catch (Exception ex)
                {
                    PieDiagnostics.Warning($"[PieChatWindow] abort before assembly reload failed: {ex.Message}");
                }
            }

            _isStreaming = false;
            _statusText = "Interrupted by C# compilation";

            if (_messages != null)
            {
                for (var i = 0; i < _messages.Count; i++)
                {
                    if (_messages[i] != null && _messages[i].IsRunning)
                        _messages[i].IsRunning = false;
                }
            }
        }

        private void DisposeBridge()
        {
            if (_bridge == null) return;
            _bridge.OnJsEvent -= HandleJsEvent;
            _bridge.Dispose();
            _bridge = null;
        }

        // ─── GUI ──────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            DrawToolbar();

            if (_showSettings) DrawSettings();
            if (_showSkills) DrawSkillsPanel();
            if (_showSessions) DrawSessionsPanel();

            if (_showLogs)
            {
                DrawLogs();
            }
            else
            {
                DrawMessages();
                DrawTodoPanel();
                DrawTokenSummaryBar();
                DrawInput();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                _messages.Clear();
                _isStreaming = false;
                _bridge?.SendToJs("new_session", "{}");
            }

            _showSettings = GUILayout.Toggle(_showSettings, "Settings", EditorStyles.toolbarButton, GUILayout.Width(60));
            _showSkills   = GUILayout.Toggle(_showSkills,   "Skills",   EditorStyles.toolbarButton, GUILayout.Width(50));
            _showSessions = GUILayout.Toggle(_showSessions, "Sessions", EditorStyles.toolbarButton, GUILayout.Width(65));
            _showLogs     = GUILayout.Toggle(_showLogs,     "Logs",     EditorStyles.toolbarButton, GUILayout.Width(40));

            GUILayout.FlexibleSpace();

            // Connection indicator
            if (_bridge?.IsInitialized == true)
            {
                GUILayout.Label("● Ready", new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(0.2f, 0.8f, 0.2f) } });
            }
            else
            {
                GUILayout.Label("○ Disconnected", new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(0.9f, 0.3f, 0.3f) } });

                if (GUILayout.Button("Reconnect", EditorStyles.toolbarButton, GUILayout.Width(75)))
                    ConnectBridge();
            }

            if (_isStreaming)
                GUILayout.Label("⏳", EditorStyles.miniLabel, GUILayout.Width(20));

            GUILayout.Space(8);
            GUILayout.Label(_statusText, EditorStyles.miniLabel, GUILayout.Width(150));
            _followLatest = GUILayout.Toggle(_followLatest, "Follow", EditorStyles.toolbarButton, GUILayout.Width(55));
            if (!_followLatest && GUILayout.Button("Latest", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                JumpToLatest();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            // API Key
            EditorGUILayout.BeginHorizontal();
            _showApiKey = EditorGUILayout.Toggle("Show", _showApiKey, GUILayout.Width(80));
            var newKey = _showApiKey
                ? EditorGUILayout.TextField("API Key", _apiKey)
                : EditorGUILayout.PasswordField("API Key", _apiKey);
            if (newKey != _apiKey)
            {
                _apiKey = newKey;
                EditorPrefs.SetString(PREF_API_KEY, _apiKey);
                PushSettings();
            }
            EditorGUILayout.EndHorizontal();

            // Provider
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Provider", GUILayout.Width(80));
            var newProvider = EditorGUILayout.TextField(_provider);
            if (newProvider != _provider)
            {
                _provider = newProvider;
                EditorPrefs.SetString(PREF_PROVIDER, _provider);
                PushSettings();
            }
            EditorGUILayout.EndHorizontal();

            // Model
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Model", GUILayout.Width(80));
            var newModel = EditorGUILayout.TextField(_model);
            if (newModel != _model)
            {
                _model = newModel;
                EditorPrefs.SetString(PREF_MODEL, _model);
                PushSettings();
            }
            EditorGUILayout.EndHorizontal();

            // Base URL
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Base URL", GUILayout.Width(80));
            var newBaseUrl = EditorGUILayout.TextField(_baseUrl);
            if (newBaseUrl != _baseUrl)
            {
                _baseUrl = newBaseUrl;
                EditorPrefs.SetString(PREF_BASE_URL, _baseUrl);
                PushSettings();
            }
            EditorGUILayout.EndHorizontal();

            var newVerboseLogs = EditorGUILayout.ToggleLeft("Verbose Logs", _verboseLogs);
            if (newVerboseLogs != _verboseLogs)
            {
                _verboseLogs = newVerboseLogs;
                EditorPrefs.SetBool(PREF_VERBOSE_LOGS, _verboseLogs);
                PieDiagnostics.CurrentLevel = _verboseLogs ? PieLogLevel.Verbose : PieLogLevel.Info;
                PushSettings();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSessionsPanel()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sessions", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("New", GUILayout.Width(50)))
                SendCommand("/new");
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                RefreshSessions();
            EditorGUILayout.EndHorizontal();
            var newAutoResume = EditorGUILayout.ToggleLeft("Auto-resume", _autoResume);
            if (newAutoResume != _autoResume)
            {
                _autoResume = newAutoResume;
                EditorPrefs.SetBool(PREF_AUTO_RESUME, _autoResume);
            }

            _sessionsScrollPos = EditorGUILayout.BeginScrollView(_sessionsScrollPos, GUILayout.Height(140));
            if (_sessions.Count == 0)
            {
                EditorGUILayout.LabelField("No sessions found.", EditorStyles.miniLabel);
            }
            else
            {
                var children = new Dictionary<string, List<SessionInfo>>();
                var roots = new List<SessionInfo>();
                foreach (var session in _sessions)
                {
                    if (string.IsNullOrEmpty(session.parentSessionId))
                    {
                        roots.Add(session);
                    }
                    else
                    {
                        if (!children.ContainsKey(session.parentSessionId))
                            children[session.parentSessionId] = new List<SessionInfo>();
                        children[session.parentSessionId].Add(session);
                    }
                }

                roots.Sort((a, b) => string.CompareOrdinal(b.updatedAt, a.updatedAt));
                foreach (var root in roots)
                    DrawSessionNode(root, children, 0);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSessionNode(SessionInfo session, Dictionary<string, List<SessionInfo>> children, int depth)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(depth * 14f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{session.title} ({session.id})", EditorStyles.miniBoldLabel);
            var messageLabel = session.messageCount == 1 ? "1 message" : $"{session.messageCount} messages";
            EditorGUILayout.LabelField($"{session.modelId} | {messageLabel} | {FormatSessionTimestamp(session.updatedAt)}", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Resume", GUILayout.Width(60)))
                SendCommand($"/resume {session.id}");
            if (GUILayout.Button("Fork", GUILayout.Width(50)))
                SendCommand($"/fork {session.id}");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (!children.ContainsKey(session.id)) return;
            children[session.id].Sort((a, b) => string.CompareOrdinal(b.updatedAt, a.updatedAt));
            foreach (var child in children[session.id])
                DrawSessionNode(child, children, depth + 1);
        }

        private void DrawSkillsPanel()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Skills", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("New", GUILayout.Width(50)))
            {
                _isCreatingSkill = true;
                _selectedSkillPath = "";
                _selectedSkillName = "";
                _newSkillName = "";
                _skillEditorContent = CreateSkillTemplate("new-skill");
            }
            if (GUILayout.Button("Reload", GUILayout.Width(60)))
                _bridge?.SendToJs("reload_skills", "{}");
            EditorGUILayout.EndHorizontal();

            _skillsScrollPos = EditorGUILayout.BeginScrollView(_skillsScrollPos, GUILayout.Height(120));
            if (_skills.Count == 0)
            {
                EditorGUILayout.LabelField("No skills loaded.", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var skill in _skills)
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{skill.name} [{skill.source}]", EditorStyles.miniBoldLabel);
                    GUILayout.FlexibleSpace();
                    if (skill.source == "project" && GUILayout.Button("Edit", GUILayout.Width(45)))
                    {
                        OpenSkillForEdit(skill);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.LabelField(skill.description, EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndScrollView();

            if (_isCreatingSkill || !string.IsNullOrEmpty(_selectedSkillPath))
            {
                DrawSkillEditor();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSkillEditor()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(_isCreatingSkill ? "Create Project Skill" : $"Edit Skill: {_selectedSkillName}", EditorStyles.boldLabel);

            if (_isCreatingSkill)
            {
                _newSkillName = EditorGUILayout.TextField("Skill Name", _newSkillName);
            }
            else
            {
                EditorGUILayout.LabelField("Path", _selectedSkillPath, EditorStyles.miniLabel);
            }

            _skillEditorContent = EditorGUILayout.TextArea(_skillEditorContent, GUILayout.MinHeight(140));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save", GUILayout.Width(60)))
            {
                SaveSkillEditor();
            }

            GUI.enabled = !_isCreatingSkill && !string.IsNullOrEmpty(_selectedSkillPath);
            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                DeleteSelectedSkill();
            }
            GUI.enabled = true;

            if (GUILayout.Button("Cancel", GUILayout.Width(60)))
            {
                _isCreatingSkill = false;
                _selectedSkillPath = "";
                _selectedSkillName = "";
                _skillEditorContent = "";
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMessages()
        {
            if (Event.current.type == EventType.ScrollWheel)
                _followLatest = false;

            _chatScrollPos = EditorGUILayout.BeginScrollView(_chatScrollPos, GUILayout.ExpandHeight(true));

            foreach (var msg in _messages)
                DrawMessage(msg);

            EditorGUILayout.EndScrollView();
        }

        private void DrawTodoPanel()
        {
            if (_todoItems.Count == 0)
                return;

            var completedCount = CountCompletedTodos();
            var inProgressCount = CountTodosByStatus("in-progress");
            var pendingCount = _todoItems.Count - completedCount - inProgressCount;
            var currentTodo = FindFirstTodoByStatus("in-progress") ?? FindFirstTodoByStatus("not-started");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Todo List ({completedCount}/{_todoItems.Count})", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"Tracking progress. Active: {inProgressCount}  Pending: {System.Math.Max(0, pendingCount)}",
                EditorStyles.miniLabel);
            if (currentTodo != null)
            {
                var currentStatus = string.Equals(currentTodo.status, "in-progress", StringComparison.OrdinalIgnoreCase)
                    ? "Current"
                    : "Next";
                EditorGUILayout.LabelField(
                    $"{currentStatus}: {currentTodo.id}. {currentTodo.title}",
                    EditorStyles.miniBoldLabel);
                if (!string.IsNullOrWhiteSpace(currentTodo.description))
                    EditorGUILayout.LabelField(currentTodo.description, EditorStyles.wordWrappedMiniLabel);
            }
            GUILayout.Space(2f);
            foreach (var todo in _todoItems)
            {
                var status = string.IsNullOrEmpty(todo.status) ? "not-started" : todo.status;
                var marker = status == "completed" ? "✓" : status == "in-progress" ? "→" : "•";
                EditorGUILayout.LabelField($"{marker} {todo.id}. {todo.title}", EditorStyles.miniBoldLabel);
                if (!string.IsNullOrWhiteSpace(todo.description)
                    && !ReferenceEquals(todo, currentTodo)
                    && string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase))
                    EditorGUILayout.LabelField(todo.description, EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(4f);
        }

        private void DrawMessage(ChatMessage msg)
        {
            Color roleColor;
            string roleLabel;
            GUIStyle boxStyle = "box";
            GUIStyle contentStyle = new GUIStyle(msg.IsCompact ? EditorStyles.wordWrappedMiniLabel : EditorStyles.wordWrappedLabel)
            {
                wordWrap = true
            };
            GUIStyle toolTitleStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                wordWrap = true,
                clipping = TextClipping.Overflow
            };
            switch (msg.Role)
            {
                case "user":      roleColor = new Color(0.3f, 0.6f, 1f);  roleLabel = "YOU";    break;
                case "assistant": roleColor = new Color(0.2f, 0.8f, 0.3f); roleLabel = "PIE";   break;
                case "thinking":  roleColor = new Color(0.65f, 0.7f, 0.9f); roleLabel = "THINK"; break;
                case "tool":      roleColor = new Color(0.9f, 0.7f, 0.2f); roleLabel = "TOOL";  break;
                default:          roleColor = Color.gray;                   roleLabel = "SYS";   break;
            }

            var previousColor = GUI.color;
            if (msg.Role == "assistant")
                GUI.color = new Color(0.19f, 0.22f, 0.19f);
            else if (msg.Role == "user")
                GUI.color = new Color(0.18f, 0.2f, 0.24f);
            else if (msg.Role == "thinking")
                GUI.color = new Color(0.16f, 0.18f, 0.24f);
            else if (msg.Role == "tool")
                GUI.color = msg.IsError
                    ? new Color(0.28f, 0.19f, 0.19f)
                    : msg.IsRunning
                        ? new Color(0.27f, 0.24f, 0.18f)
                        : new Color(0.21f, 0.21f, 0.18f);
            else
                GUI.color = new Color(0.22f, 0.22f, 0.22f);

            EditorGUILayout.BeginVertical(boxStyle);
            GUI.color = previousColor;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(roleLabel, new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = roleColor }
            });
            GUILayout.FlexibleSpace();
            if (msg.Role == "user" && msg.InputTokens > 0)
                GUILayout.Label($"~↑{FormatTokenCount(msg.InputTokens)} tok", EditorStyles.miniLabel, GUILayout.Width(84));
            if (msg.Role == "assistant" && msg.TotalTokens > 0)
                GUILayout.Label($"{(msg.IsEstimatedUsage ? "~" : "")}{msg.TotalTokens} tok", EditorStyles.miniLabel, GUILayout.Width(72));
            if (GUILayout.Button("Copy", EditorStyles.miniButton, GUILayout.Width(42)))
                EditorGUIUtility.systemCopyBuffer = msg.Content ?? "";
            EditorGUILayout.EndHorizontal();

            if (msg.Role == "tool" && !string.IsNullOrEmpty(msg.Title))
            {
                DrawToolHeader(msg, toolTitleStyle);
                if (!string.IsNullOrEmpty(msg.ArgsSummary))
                    DrawSelectableText(msg.ArgsSummary, EditorStyles.wordWrappedMiniLabel);
                if (!string.IsNullOrEmpty(msg.Summary))
                    DrawSelectableText(msg.Summary, EditorStyles.wordWrappedMiniLabel);
                if (msg.IsExpanded)
                    DrawSelectableText(msg.Content, contentStyle);
            }
            else
            {
                DrawSelectableText(msg.Content, contentStyle);
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(msg.IsCompact ? 2 : 4);
        }

        private void DrawInput()
        {
            CheckInteractionTimeout();

            if (_bridge?.IsInitialized != true)
            {
                EditorGUILayout.HelpBox(
                    $"Bridge not connected.\nError: {_bridge?.LastError ?? "unknown"}\nClick Reconnect in toolbar.",
                    MessageType.Error);
            }

            DrawInlineStatusBar();
            DrawPendingInteractionPanel();

            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName("PieInput");
            GUI.enabled = _pendingInteraction == null || _pendingInteraction.completed;
            _inputText = EditorGUILayout.TextArea(_inputText, GUILayout.Height(60), GUILayout.ExpandWidth(true));

            var canSend = (_pendingInteraction == null || _pendingInteraction.completed)
                && !_isStreaming
                && !string.IsNullOrWhiteSpace(_inputText)
                && _bridge?.IsInitialized == true;
            var estimatedInputTokens = EstimateTextTokens(_inputText);

            EditorGUILayout.BeginVertical(GUILayout.Width(60));
            GUI.enabled = canSend;
            if (GUILayout.Button("Send", GUILayout.Width(55), GUILayout.Height(40)))
            {
                SendMessage();
                GUI.FocusControl("PieInput");
            }
            GUI.enabled = true;
            if (_isStreaming)
            {
                if (GUILayout.Button("✕", GUILayout.Width(55), GUILayout.Height(18)))
                    AbortCurrentTurn();
            }
            EditorGUILayout.EndVertical();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (estimatedInputTokens > 0)
                GUILayout.Label($"Next send ~{FormatTokenCount(estimatedInputTokens)} tok", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Ctrl+Enter to send", EditorStyles.centeredGreyMiniLabel);

            // Keyboard shortcut
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return && e.control && canSend)
            {
                SendMessage();
                e.Use();
            }
        }

        private void DrawInlineStatusBar()
        {
            EditorGUILayout.BeginHorizontal();

            var spinner = _isStreaming ? GetSpinnerFrame() : " ";
            var spinnerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = _isStreaming ? new Color(0.95f, 0.8f, 0.35f) : Color.gray }
            };
            GUILayout.Label(spinner, spinnerStyle, GUILayout.Width(16));

            var statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal = { textColor = new Color(0.75f, 0.78f, 0.82f) }
            };
            GUILayout.Label(string.IsNullOrEmpty(_statusText) ? "Idle" : _statusText, statusStyle, GUILayout.ExpandWidth(true));

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2f);
        }

        private void DrawPendingInteractionPanel()
        {
            if (_pendingInteraction == null || _pendingInteraction.completed)
                return;

            var request = _pendingInteraction.request;
            if (request == null)
                return;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Waiting for input", EditorStyles.boldLabel);
            if (!string.IsNullOrWhiteSpace(request.prompt))
                EditorGUILayout.LabelField(request.prompt, EditorStyles.wordWrappedLabel);
            if (!string.IsNullOrWhiteSpace(request.detail))
                EditorGUILayout.LabelField(request.detail, EditorStyles.wordWrappedMiniLabel);

            if (request.timeoutMs > 0)
            {
                var elapsedMs = (int)((EditorApplication.timeSinceStartup - _pendingInteraction.openedAt) * 1000.0);
                var remainingMs = Math.Max(0, request.timeoutMs - elapsedMs);
                EditorGUILayout.LabelField($"Auto-skip in {Mathf.CeilToInt(remainingMs / 1000f)}s", EditorStyles.miniLabel);
            }

            switch (request.type)
            {
                case "select_one":
                    DrawInlineSelectOne(request);
                    break;
                case "confirm":
                    DrawInlineConfirm(request);
                    break;
                case "text_input":
                    _pendingInteraction.value = EditorGUILayout.TextField(_pendingInteraction.value ?? "");
                    break;
                case "multiline_input":
                    _pendingInteraction.value = EditorGUILayout.TextArea(_pendingInteraction.value ?? "", GUILayout.MinHeight(100));
                    break;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Cancel", GUILayout.Width(70)))
                    CompleteInteraction(BuildCancelledInteractionResponse());
                if (GUILayout.Button("Skip", GUILayout.Width(70)))
                    CompleteInteraction(BuildSkippedInteractionResponse(false));

                GUILayout.FlexibleSpace();

                GUI.enabled = CanSubmitPendingInteraction();
                if (GUILayout.Button(GetPendingInteractionSubmitLabel(), GUILayout.Width(100)))
                    SubmitPendingInteraction();
                GUI.enabled = true;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(4f);
        }

        private void DrawInlineSelectOne(PieInteractionRequest request)
        {
            var options = request.options ?? Array.Empty<string>();
            for (var i = 0; i < options.Length; i++)
            {
                var isSelected = _pendingInteraction.selectedIndex == i;
                if (GUILayout.Toggle(isSelected, options[i], "Button"))
                {
                    _pendingInteraction.selectedIndex = i;
                    CompleteInteraction(new PieInteractionResponse
                    {
                        type = "select_one",
                        id = request.id,
                        selection = options[i],
                    });
                    GUIUtility.ExitGUI();
                    return;
                }
            }
        }

        private void DrawInlineConfirm(PieInteractionRequest request)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Yes"))
                    CompleteInteraction(new PieInteractionResponse
                    {
                        type = "confirm",
                        id = request.id,
                        confirmed = true,
                    });
                if (GUILayout.Button("No"))
                    CompleteInteraction(new PieInteractionResponse
                    {
                        type = "confirm",
                        id = request.id,
                        confirmed = false,
                    });
            }
        }

        private bool CanSubmitPendingInteraction()
        {
            if (_pendingInteraction?.request == null)
                return false;

            switch (_pendingInteraction.request.type)
            {
                case "select_one":
                    return false;
                case "confirm":
                    return false;
                default:
                    return true;
            }
        }

        private string GetPendingInteractionSubmitLabel()
        {
            return "Submit";
        }

        private void SubmitPendingInteraction()
        {
            if (_pendingInteraction?.request == null)
                return;

            switch (_pendingInteraction.request.type)
            {
                case "select_one":
                    if (_pendingInteraction.request.options == null
                        || _pendingInteraction.selectedIndex < 0
                        || _pendingInteraction.selectedIndex >= _pendingInteraction.request.options.Length)
                        return;
                    CompleteInteraction(new PieInteractionResponse
                    {
                        type = "select_one",
                        id = _pendingInteraction.request.id,
                        selection = _pendingInteraction.request.options[_pendingInteraction.selectedIndex],
                    });
                    break;
                case "text_input":
                case "multiline_input":
                    CompleteInteraction(new PieInteractionResponse
                    {
                        type = _pendingInteraction.request.type,
                        id = _pendingInteraction.request.id,
                        value = _pendingInteraction.value ?? "",
                    });
                    break;
            }
        }

        private void DrawTokenSummaryBar()
        {
            var totalInput = 0;
            var totalOutput = 0;
            var totalCacheRead = 0;
            var totalCacheWrite = 0;
            var hasEstimated = false;

            foreach (var message in _messages)
            {
                if (message.Role != "assistant")
                    continue;

                totalInput += Math.Max(0, message.InputTokens);
                totalOutput += Math.Max(0, message.OutputTokens);
                totalCacheRead += Math.Max(0, message.CacheReadTokens);
                totalCacheWrite += Math.Max(0, message.CacheWriteTokens);
                hasEstimated = hasEstimated || message.IsEstimatedUsage;
            }

            var totalTokens = totalInput + totalOutput + totalCacheRead + totalCacheWrite;
            if (totalTokens <= 0)
            {
                GUILayout.Space(4f);
                return;
            }

            EditorGUILayout.BeginHorizontal("box");
            var prefix = hasEstimated ? "~" : "";
            GUILayout.Label(
                $"Session tokens {prefix}↑{FormatTokenCount(totalInput)}  ↓{FormatTokenCount(totalOutput)}  Σ{FormatTokenCount(totalTokens)}",
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (totalCacheRead > 0 || totalCacheWrite > 0)
                GUILayout.Label($"cache R{FormatTokenCount(totalCacheRead)} W{FormatTokenCount(totalCacheWrite)}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLogs()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
                PieDiagnostics.Clear();
            if (GUILayout.Button("Copy", GUILayout.Width(60)))
                EditorGUIUtility.systemCopyBuffer = PieDiagnostics.ExportLogs();
            EditorGUILayout.EndHorizontal();

            _logScrollPos = EditorGUILayout.BeginScrollView(_logScrollPos, GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(PieDiagnostics.ExportLogs(), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        // ─── Messaging ────────────────────────────────────────────────────────
        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(_inputText)) return;
            if (_bridge?.IsInitialized != true) return;

            var text = _inputText.Trim();
            AddMessage("user", text);

            var escaped = text.Replace("\\", "\\\\").Replace("\"", "\\\"")
                              .Replace("\n", "\\n").Replace("\r", "\\r");
            var json = $"{{\"content\":\"{escaped}\"}}";

            _isStreaming = true;
            _statusText = "Thinking...";
            _lastSubmittedInputText = text;

            // Placeholder
            _messages.Add(new ChatMessage("assistant", "…")
            {
                IsRunning = true,
            });
            _bridge.SendToJs("send_message", json);
            _inputText = "";
            ScrollToBottomSoon();
        }

        private void AbortCurrentTurn()
        {
            if (_bridge?.IsInitialized != true) return;
            _bridge.SendToJs("abort", "{}");
            _isStreaming = false;
            ClearRunningMessages();
            _statusText = "Aborted";
        }

        private void ClearRunningMessages()
        {
            if (_messages == null) return;
            for (var i = 0; i < _messages.Count; i++)
            {
                if (_messages[i] != null && _messages[i].IsRunning)
                    _messages[i].IsRunning = false;
            }
        }

        private bool _repaintScheduled = false;

        private void ScheduleRepaint()
        {
            if (_repaintScheduled) return;
            _repaintScheduled = true;
            EditorApplication.delayCall += () =>
            {
                _repaintScheduled = false;
                Repaint();
            };
        }

        private void HandleJsEvent(string eventName, string json)
        {
            switch (eventName)
            {
                case "message_update":
                    HandleMessageUpdate(json);
                    var now = EditorApplication.timeSinceStartup;
                    if (now - _lastRepaintTime > 0.1)
                    {
                        _lastRepaintTime = now;
                        ScheduleRepaint();
                    }
                    return;
                case "thinking_event":  HandleThinkingEvent(json); break;
                case "message_metrics": HandleMessageMetrics(json); break;
                case "state_event":     HandleStateEvent(json);    break;
                case "tool_start":      HandleToolStart(json);     break;
                case "tool_end":        HandleToolEnd(json);       break;
                case "turn_end":        HandleTurnEnd(json);        break;
                case "session_sync":    HandleSessionSync(json);    break;
                case "skills_list":     HandleSkillsList(json);     break;
                case "config_applied":  HandleConfigApplied(json);  break;
                case "error":           HandleError(json);          break;
                case "agent_end":
                    _isStreaming = false;
                    _statusText = "Idle";
                    break;
            }
            ScheduleRepaint();
        }

        private void HandleConfigApplied(string json)
        {
            _configAppliedVersion += 1;
            var payload = JsonUtility.FromJson<ConfigAppliedPayload>(json ?? "{}") ?? new ConfigAppliedPayload();
            if (!string.IsNullOrEmpty(payload.provider))
                _provider = payload.provider;
            if (!string.IsNullOrEmpty(payload.model))
                _model = payload.model;
            if (payload.baseUrl != null)
                _baseUrl = payload.baseUrl;
            _verboseLogs = payload.verboseLogs;
            _statusText = "Config applied";
        }

        private static int EstimateTextTokens(string text)
        {
            var normalized = (text ?? "").Trim();
            if (string.IsNullOrEmpty(normalized))
                return 0;
            return Math.Max(1, Mathf.CeilToInt(normalized.Length / 4f));
        }

        private static string FormatTokenCount(int count)
        {
            if (count < 1000)
                return count.ToString();
            if (count < 10000)
                return (count / 1000f).ToString("0.0") + "k";
            if (count < 1000000)
                return Mathf.RoundToInt(count / 1000f) + "k";
            return (count / 1000000f).ToString("0.0") + "M";
        }

        private static string FormatSessionTimestamp(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "";

            if (!DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                return raw;

            var local = parsed.ToLocalTime();
            var now = DateTime.Now;
            if (local.Year == now.Year)
                return local.ToString("MM-dd HH:mm");
            return local.ToString("yyyy-MM-dd HH:mm");
        }

        private void HandleMessageUpdate(string json)
        {
            try
            {
                if (_messages.Count == 0) return;
                var last = _messages[_messages.Count - 1];

                // After tool use, the last message is "tool" — need a new assistant bubble
                if (last.Role != "assistant")
                {
                    AddMessage("assistant", "");
                    last = _messages[_messages.Count - 1];
                }

                if (json.Contains("\"type\":\"text_full\""))
                {
                    var text = ExtractJsonString(json, "text");
                    if (text != null) last.Content = text;
                    last.IsRunning = true;
                }
                else if (json.Contains("\"type\":\"text_delta\""))
                {
                    var delta = ExtractJsonString(json, "delta");
                    if (delta != null)
                    {
                        last.Content = (last.Content == "…" ? "" : last.Content) + delta;
                    }
                    last.IsRunning = true;
                }

                _statusText = "Streaming response...";
                ScrollToBottomSoon();
            }
            catch (Exception ex)
            {
                PieDiagnostics.Warning($"HandleMessageUpdate error: {ex.Message}");
            }
        }

        private void HandleStateEvent(string json)
        {
            var stateName = ExtractJsonString(json, "name") ?? "state";
            var detail = ExtractJsonString(json, "detail") ?? "";

            switch (stateName)
            {
                case "agent_start":
                    _isStreaming = true;
                    _statusText = "Agent started";
                    break;
                case "turn_start":
                    _isStreaming = true;
                    _statusText = "Turn started";
                    break;
                case "message_start":
                    _isStreaming = true;
                    _statusText = string.IsNullOrEmpty(detail) ? "Composing message..." : detail;
                    break;
                case "message_update":
                    _isStreaming = true;
                    _statusText = string.IsNullOrEmpty(detail) ? "Streaming response..." : $"Streaming response... {detail}";
                    break;
                case "message_end":
                    _statusText = "Message completed";
                    break;
                case "turn_end":
                    _isStreaming = false;
                    _statusText = string.IsNullOrEmpty(detail) ? "Turn completed" : detail;
                    break;
                case "agent_end":
                    _isStreaming = false;
                    _statusText = "Idle";
                    break;
                default:
                    if (!string.IsNullOrEmpty(detail))
                        _statusText = detail;
                    break;
            }

            ScheduleRepaint();
        }

        private void HandleThinkingEvent(string json)
        {
            var type = ExtractJsonString(json, "type") ?? "delta";
            var delta = ExtractJsonString(json, "delta") ?? "";
            var text = ExtractJsonString(json, "text") ?? "";

            if (type == "start")
            {
                _messages.Add(new ChatMessage("thinking", "")
                {
                    IsCompact = true,
                    IsRunning = true,
                });
                ScrollToBottomSoon();
                return;
            }

            var thinkingMessage = FindLatestThinkingMessage();
            if (thinkingMessage == null)
            {
                thinkingMessage = new ChatMessage("thinking", "")
                {
                    IsCompact = true,
                    IsRunning = true,
                };
                _messages.Add(thinkingMessage);
            }

            if (type == "delta")
            {
                thinkingMessage.Content += delta;
            }
            else if (type == "end")
            {
                thinkingMessage.Content = string.IsNullOrEmpty(text) ? thinkingMessage.Content : text;
                thinkingMessage.IsRunning = false;
            }

            ScrollToBottomSoon();
        }

        private void HandleToolStart(string json)
        {
            var toolName = ExtractJsonString(json, "name") ?? "tool";
            var callId = ExtractJsonString(json, "callId");
            var argsJson = ExtractJsonString(json, "argsJson");
            _statusText = $"Using {toolName}...";
            _messages.Add(new ChatMessage("tool", "running...")
            {
                ToolName = toolName,
                ToolCallId = callId,
                Title = toolName,
                ArgsText = argsJson,
                ArgsSummary = BuildArgsSummary(argsJson),
                Summary = "Waiting for result...",
                IsCompact = true,
                IsRunning = true,
            });
            ScrollToBottomSoon();
        }

        private void HandleToolEnd(string json)
        {
            var toolName = ExtractJsonString(json, "name") ?? "tool";
            var callId = ExtractJsonString(json, "callId");
            var isError = ExtractJsonBool(json, "isError");
            var error = ExtractJsonString(json, "error");
            var argsJson = ExtractJsonString(json, "argsJson");
            var effectiveArgsJson = ExtractJsonString(json, "effectiveArgsJson");
            var resultText = ExtractJsonString(json, "resultText");
            var resultJson = ExtractJsonString(json, "resultJson");
            var detailsJson = ExtractJsonString(json, "detailsJson");
            var toolMessage = FindLatestToolMessage(toolName, callId);
            if (toolMessage == null)
            {
                toolMessage = new ChatMessage("tool", "");
                toolMessage.ToolName = toolName;
                toolMessage.ToolCallId = callId;
                toolMessage.Title = toolName;
                toolMessage.IsCompact = true;
                toolMessage.IsExpanded = false;
                _messages.Add(toolMessage);
            }

            toolMessage.IsRunning = false;
            if (HasMeaningfulArgs(argsJson))
            {
                toolMessage.ArgsText = argsJson;
                toolMessage.ArgsSummary = BuildArgsSummary(argsJson);
            }
            if (HasMeaningfulArgs(effectiveArgsJson))
            {
                toolMessage.ArgsText = effectiveArgsJson;
                toolMessage.ArgsSummary = BuildArgsSummary(effectiveArgsJson);
            }
            if (!HasMeaningfulArgs(toolMessage.ArgsText))
            {
                var effectiveArgs = BuildEffectiveArgsFromDetails(toolName, detailsJson);
                if (!string.IsNullOrEmpty(effectiveArgs))
                {
                    toolMessage.ArgsSummary = effectiveArgs;
                    toolMessage.ArgsText = effectiveArgs.Substring("args: ".Length);
                }
            }
            if (isError || error != null)
            {
                var errorText = !string.IsNullOrEmpty(error)
                    ? error
                    : (!string.IsNullOrEmpty(resultText) ? resultText : "Tool returned an error result.");
                PieDiagnostics.Warning($"[Chat] Tool error: {errorText.Substring(0, System.Math.Min(200, errorText.Length))}");
                toolMessage.IsError = true;
                toolMessage.Summary = SummarizeToolFailure(toolName, errorText);
                toolMessage.Content = BuildToolDetail(toolName, toolMessage.ArgsText, resultText, detailsJson, errorText);
                _statusText = $"Tool failed: {toolName}";
                AddSystemMessage($"⚠ Tool error: {errorText}");
            }
            else
            {
                toolMessage.Summary = SummarizeToolResult(toolName, resultText, detailsJson);
                toolMessage.Content = BuildToolDetail(toolName, toolMessage.ArgsText, resultText, detailsJson, null);
                _statusText = $"Completed {toolName}";
            }
            if (toolName == "manage_todo_list")
                ApplyTodoStateFromToolDetails(detailsJson);
            ScrollToBottomSoon();
        }

        private void HandleTurnEnd(string json)
        {
            var stopReason = ExtractJsonString(json, "stopReason");

            if (stopReason == null || stopReason == "stop" || stopReason == "error"
                || stopReason == "aborted" || stopReason == "maxTokens")
            {
                _isStreaming = false;
                for (var i = _messages.Count - 1; i >= 0; i--)
                {
                    if (_messages[i].Role == "assistant")
                    {
                        _messages[i].IsRunning = false;
                        break;
                    }
                }
                _statusText = stopReason == "aborted" ? "Aborted" : "Idle";
            }
        }

        private void HandleSessionSync(string json)
        {
            try
            {
                var payload = JsonUtility.FromJson<SessionSyncPayload>(json);
                _activeSessionId = payload?.id ?? "";
                if (!string.IsNullOrEmpty(_activeSessionId))
                    PieChatSessionRecovery.SetLastKnownActiveSession(_activeSessionId);
                _messages.Clear();
                _todoItems.Clear();

                if (payload?.messages != null)
                {
                    foreach (var message in payload.messages)
                    {
                        var role = string.IsNullOrEmpty(message.role) ? "assistant" : message.role;
                        var content = FlattenSessionContent(message);
                        if (string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(message.errorMessage))
                            content = $"⚠ {message.errorMessage}";
                        if (string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(message.stopReason))
                            content = $"[{message.stopReason}]";
                        _messages.Add(new ChatMessage(role, content ?? "")
                        {
                            InputTokens = message.usage != null ? message.usage.input : (role == "user" ? EstimateTextTokens(content) : 0),
                            OutputTokens = message.usage != null ? message.usage.output : 0,
                            CacheReadTokens = message.usage != null ? message.usage.cacheRead : 0,
                            CacheWriteTokens = message.usage != null ? message.usage.cacheWrite : 0,
                            TotalTokens = message.usage != null ? message.usage.totalTokens : (role == "user" ? EstimateTextTokens(content) : 0),
                            IsEstimatedUsage = message.usage != null && message.usage.estimated,
                        });
                    }
                }

                if (payload?.todoState != null)
                    _todoItems.AddRange(payload.todoState);

                _isStreaming = false;
                var pendingRecoverySessionId = PieChatSessionRecovery.Read().activeSessionId;
                var expectedRecoverySessionId = !string.IsNullOrEmpty(pendingRecoverySessionId)
                    ? pendingRecoverySessionId
                    : "";
                var isRecoverySync = !string.IsNullOrEmpty(payload?.id)
                    && payload.id == _activeSessionId
                    && (
                        payload.id == expectedRecoverySessionId
                        || HasRecoverySnapshot()
                    );
                _statusText = payload == null
                    ? "Session synced"
                    : $"Session: {payload.title} ({payload.id})";
                if (isRecoverySync)
                {
                    ClearRecoverySnapshot();
                    _statusText = "Idle";
                }
                if (!string.IsNullOrEmpty(_pendingRecoveryNotice))
                {
                    AddSystemMessage(_pendingRecoveryNotice);
                    _pendingRecoveryNotice = "";
                }
                RefreshSessions();
                ScrollToBottomSoon(true);
            }
            catch (Exception ex)
            {
                PieDiagnostics.Warning($"[PieChatWindow] HandleSessionSync: {ex.Message}");
            }
        }

        private void ApplyTodoStateFromToolDetails(string detailsJson)
        {
            if (string.IsNullOrEmpty(detailsJson))
                return;

            try
            {
                var details = JsonUtility.FromJson<TodoToolDetailsPayload>(detailsJson);
                _todoItems.Clear();
                if (details?.todos != null)
                    _todoItems.AddRange(details.todos);
            }
            catch (Exception ex)
            {
                PieDiagnostics.Warning($"[PieChatWindow] ApplyTodoStateFromToolDetails: {ex.Message}");
            }
        }

        private int CountCompletedTodos()
        {
            var completed = 0;
            foreach (var todo in _todoItems)
            {
                if (string.Equals(todo.status, "completed", StringComparison.OrdinalIgnoreCase))
                    completed++;
            }
            return completed;
        }

        private int CountTodosByStatus(string status)
        {
            var count = 0;
            foreach (var todo in _todoItems)
            {
                var normalizedStatus = string.IsNullOrEmpty(todo.status) ? "not-started" : todo.status;
                if (string.Equals(normalizedStatus, status, StringComparison.OrdinalIgnoreCase))
                    count++;
            }
            return count;
        }

        private TodoStateItem FindFirstTodoByStatus(string status)
        {
            foreach (var todo in _todoItems)
            {
                var normalizedStatus = string.IsNullOrEmpty(todo.status) ? "not-started" : todo.status;
                if (string.Equals(normalizedStatus, status, StringComparison.OrdinalIgnoreCase))
                    return todo;
            }
            return null;
        }

        private void HandleError(string json)
        {
            _isStreaming = false;
            _statusText = "Error";
            var msg = ExtractJsonString(json, "message") ?? json;
            PieDiagnostics.Warning($"[PieChatWindow] HandleError: {msg.Substring(0, System.Math.Min(80, msg.Length))}");
            // Replace trailing "…" placeholder (from SendMessage) with the error
            if (_messages.Count > 0 && _messages[_messages.Count - 1].Role == "assistant"
                && _messages[_messages.Count - 1].Content == "…")
                _messages[_messages.Count - 1] = new ChatMessage("assistant", $"⚠ {msg}") { IsRunning = false };
            else
                AddSystemMessage($"⚠ {msg}");
        }

        private void HandleMessageMetrics(string json)
        {
            try
            {
                var payload = JsonUtility.FromJson<MessageMetricsPayload>(json);
                if (payload?.usage == null)
                    return;

                for (var i = _messages.Count - 1; i >= 0; i--)
                {
                    var message = _messages[i];
                    if (message.Role != "assistant")
                        continue;

                    message.InputTokens = payload.usage.input;
                    message.OutputTokens = payload.usage.output;
                    message.CacheReadTokens = payload.usage.cacheRead;
                    message.CacheWriteTokens = payload.usage.cacheWrite;
                    message.TotalTokens = payload.usage.totalTokens;
                    message.IsEstimatedUsage = payload.usage.estimated;
                    break;
                }
            }
            catch (Exception ex)
            {
                PieDiagnostics.Warning($"[PieChatWindow] HandleMessageMetrics: {ex.Message}");
            }
        }

        private void HandleSkillsList(string json)
        {
            try
            {
                var payload = JsonUtility.FromJson<SkillsPayload>(json);
                _skills.Clear();
                if (payload?.skills != null)
                    _skills.AddRange(payload.skills);
            }
            catch (Exception ex)
            {
                PieDiagnostics.Warning($"[PieChatWindow] HandleSkillsList: {ex.Message}");
            }
        }

        private void OpenSkillForEdit(SkillInfo skill)
        {
            _isCreatingSkill = false;
            _selectedSkillName = skill.name;
            _selectedSkillPath = skill.path;
            if (!string.IsNullOrEmpty(_selectedSkillPath) && System.IO.File.Exists(_selectedSkillPath))
            {
                _skillEditorContent = System.IO.File.ReadAllText(_selectedSkillPath);
            }
        }

        private void SaveSkillEditor()
        {
            try
            {
                string targetPath = _selectedSkillPath;
                if (_isCreatingSkill)
                {
                    var sanitized = SanitizeSkillName(_newSkillName);
                    if (string.IsNullOrEmpty(sanitized))
                    {
                        AddSystemMessage("⚠ Skill name is required.");
                        return;
                    }

                    var projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
                    var skillsDir = Pie.PieProjectPaths.GetPrimarySkillDirectory(projectRoot);
                    System.IO.Directory.CreateDirectory(skillsDir);
                    targetPath = System.IO.Path.Combine(skillsDir, sanitized + ".md");
                    _selectedSkillName = sanitized;
                    _selectedSkillPath = targetPath;
                }

                if (string.IsNullOrEmpty(targetPath))
                    return;

                System.IO.File.WriteAllText(targetPath, _skillEditorContent ?? "");
                _isCreatingSkill = false;
                _bridge?.SendToJs("reload_skills", "{}");
                RefreshSessions();
            }
            catch (Exception ex)
            {
                AddSystemMessage($"⚠ Failed to save skill: {ex.Message}");
            }
        }

        private void DeleteSelectedSkill()
        {
            try
            {
                if (!string.IsNullOrEmpty(_selectedSkillPath) && System.IO.File.Exists(_selectedSkillPath))
                {
                    System.IO.File.Delete(_selectedSkillPath);
                    _selectedSkillPath = "";
                    _selectedSkillName = "";
                    _skillEditorContent = "";
                    _bridge?.SendToJs("reload_skills", "{}");
                    RefreshSessions();
                }
            }
            catch (Exception ex)
            {
                AddSystemMessage($"⚠ Failed to delete skill: {ex.Message}");
            }
        }

        private string SanitizeSkillName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var lower = value.Trim().ToLowerInvariant().Replace(" ", "-");
            var sb = new System.Text.StringBuilder();
            foreach (var ch in lower)
            {
                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '_')
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        private string CreateSkillTemplate(string skillName)
        {
            return $"---\nname: {skillName}\ndescription: Describe what this skill is for.\n---\n\n# {skillName}\n\nDescribe when to use this skill.\n";
        }

        // ─── Settings push ────────────────────────────────────────────────────
        private void PushSettings()
        {
            if (_bridge?.IsInitialized != true) return;

            var escaped = _apiKey.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var escapedBaseUrl = _baseUrl.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var verboseLogs = _verboseLogs ? "true" : "false";
            var json = $"{{\"apiKey\":\"{escaped}\",\"provider\":\"{_provider}\",\"model\":\"{_model}\",\"baseUrl\":\"{escapedBaseUrl}\",\"verboseLogs\":{verboseLogs}}}";
            _bridge.SendToJs("set_config", json);
        }

        private void SendCommand(string command)
        {
            if (_bridge?.IsInitialized != true) return;
            var escaped = command.Replace("\\", "\\\\").Replace("\"", "\\\"");
            _bridge.SendToJs("send_message", $"{{\"content\":\"{escaped}\"}}");
            RefreshSessions();
        }

        private void SendRecoveryResume(PieChatSessionRecovery.Snapshot snapshot)
        {
            if (_bridge?.IsInitialized != true || snapshot == null || string.IsNullOrEmpty(snapshot.activeSessionId))
                return;

            PieDiagnostics.Verbose($"[PieChatWindow] Sending recovery resume_session for {snapshot.activeSessionId}");
            _bridge.SendToJs(
                "resume_session",
                PieChatSessionRecovery.BuildResumeArgsJson(snapshot));
        }

        private void RefreshSessions()
        {
            try
            {
                var projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
                var sessionsDir = Pie.PieProjectPaths.GetSessionsDirectory();
                _sessions.Clear();
                if (!System.IO.Directory.Exists(sessionsDir)) return;

                foreach (var file in System.IO.Directory.GetFiles(sessionsDir, "*.json"))
                {
                    try
                    {
                        var json = System.IO.File.ReadAllText(file);
                        var session = ReadSessionInfoFromJson(json);
                        if (session != null && !string.IsNullOrEmpty(session.id) && session.messageCount > 0)
                            _sessions.Add(session);
                    }
                    catch
                    {
                        // Ignore malformed session files
                    }
                }
            }
            catch (Exception ex)
            {
                PieDiagnostics.Warning($"[PieChatWindow] RefreshSessions: {ex.Message}");
            }
        }

        private static SessionInfo ReadSessionInfoFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var legacy = JsonUtility.FromJson<SessionInfo>(json);
            if (legacy != null && !string.IsNullOrEmpty(legacy.id))
                return legacy;

            var framework = JsonUtility.FromJson<FrameworkSessionFile>(json);
            var metadata = framework?.metadata;
            if (metadata == null || string.IsNullOrEmpty(metadata.id))
                return null;

            var messageCount = metadata.messageCount;
            if (messageCount <= 0 && framework.messages != null)
                messageCount = framework.messages.Length;

            return new SessionInfo
            {
                id = metadata.id,
                title = !string.IsNullOrEmpty(metadata.name) ? metadata.name : "Untitled Session",
                createdAt = UnixMillisToIsoString(metadata.createdAt),
                updatedAt = UnixMillisToIsoString(metadata.updatedAt),
                parentSessionId = metadata.parentSessionId,
                provider = !string.IsNullOrEmpty(metadata.provider) ? metadata.provider : "openai",
                modelId = !string.IsNullOrEmpty(metadata.modelId) ? metadata.modelId : "unknown",
                messageCount = messageCount,
            };
        }

        private static string UnixMillisToIsoString(double value)
        {
            if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value))
                return "";

            try
            {
                return DateTimeOffset.FromUnixTimeMilliseconds((long)value).UtcDateTime.ToString("O");
            }
            catch
            {
                return "";
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────
        private void AddMessage(string role, string content)
        {
            var message = new ChatMessage(role, content);
            if (role == "user")
            {
                var estimated = EstimateTextTokens(content);
                message.InputTokens = estimated;
                message.TotalTokens = estimated;
                message.IsEstimatedUsage = true;
            }
            _messages.Add(message);
        }

        private void AddSystemMessage(string content) =>
            _messages.Add(new ChatMessage("system", content));

        private ChatMessage FindLatestToolMessage(string toolName, string callId)
        {
            for (var i = _messages.Count - 1; i >= 0; i--)
            {
                var message = _messages[i];
                if (message.Role != "tool" || !message.IsRunning)
                    continue;

                if (!string.IsNullOrEmpty(callId) && message.ToolCallId == callId)
                    return message;

                if (message.ToolName == toolName)
                    return message;
            }
            return null;
        }

        private bool HasMeaningfulArgs(string argsJson)
        {
            if (string.IsNullOrEmpty(argsJson))
                return false;

            var trimmed = argsJson.Trim();
            return trimmed != "{}" && trimmed != "{ }" && trimmed != "null";
        }

        private ChatMessage FindLatestThinkingMessage()
        {
            for (var i = _messages.Count - 1; i >= 0; i--)
            {
                var message = _messages[i];
                if (message.Role == "thinking" && message.IsRunning)
                    return message;
            }
            return null;
        }

        private string SummarizeToolResult(string toolName, string resultText, string detailsJson)
        {
            return PieChatToolSummaryFormatter.SummarizeToolResult(toolName, resultText, detailsJson);
        }

        private string SummarizeToolFailure(string toolName, string errorText)
        {
            return PieChatToolSummaryFormatter.SummarizeToolFailure(toolName, errorText);
        }

        private string BuildArgsSummary(string argsJson)
        {
            return PieChatToolSummaryFormatter.BuildArgsSummary(argsJson);
        }

        private string BuildEffectiveArgsFromDetails(string toolName, string detailsJson)
        {
            return PieChatToolSummaryFormatter.BuildEffectiveArgsFromDetails(toolName, detailsJson);
        }

        private string BuildToolDetail(string toolName, string argsJson, string resultText, string detailsJson, string error)
        {
            var status = !string.IsNullOrEmpty(error) ? "error" : "done";
            var summary = !string.IsNullOrEmpty(error)
                ? SummarizeToolFailure(toolName, error)
                : SummarizeToolResult(toolName, resultText, detailsJson);
            var sections = new System.Collections.Generic.List<string>
            {
                $"tool: {toolName}",
                $"status: {status}",
                $"summary: {summary}",
            };

            if (!string.IsNullOrEmpty(argsJson))
                sections.Add($"args:\n{FormatStructuredText(argsJson)}");

            if (!string.IsNullOrEmpty(resultText))
                sections.Add($"result:\n{FormatStructuredText(resultText)}");

            if (!string.IsNullOrEmpty(detailsJson))
                sections.Add($"details:\n{FormatStructuredText(detailsJson)}");

            if (!string.IsNullOrEmpty(error))
                sections.Add($"error:\n{FormatStructuredText(error)}");

            return string.Join("\n\n", sections);
        }

        private string FormatStructuredText(string text)
        {
            var safeText = (text ?? "").Trim();
            if (string.IsNullOrEmpty(safeText))
                return "(no result)";

            var prettyJson = TryPrettyPrintJson(safeText);
            if (!string.IsNullOrEmpty(prettyJson))
                return prettyJson;

            return safeText.Replace("\r", "");
        }

        private string TryPrettyPrintJson(string text)
        {
            var trimmed = (text ?? "").Trim();
            if (trimmed.Length < 2) return null;

            var first = trimmed[0];
            var last = trimmed[trimmed.Length - 1];
            var looksLikeJson = (first == '{' && last == '}') || (first == '[' && last == ']');
            if (!looksLikeJson) return null;

            var sb = new System.Text.StringBuilder();
            var indent = 0;
            var inString = false;
            for (var i = 0; i < trimmed.Length; i++)
            {
                var c = trimmed[i];
                var escaped = i > 0 && trimmed[i - 1] == '\\';

                if (c == '"' && !escaped)
                {
                    inString = !inString;
                    sb.Append(c);
                    continue;
                }

                if (inString)
                {
                    sb.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '{':
                    case '[':
                        sb.Append(c);
                        sb.Append('\n');
                        indent++;
                        AppendIndent(sb, indent);
                        break;
                    case '}':
                    case ']':
                        sb.Append('\n');
                        indent = Mathf.Max(0, indent - 1);
                        AppendIndent(sb, indent);
                        sb.Append(c);
                        break;
                    case ',':
                        sb.Append(c);
                        sb.Append('\n');
                        AppendIndent(sb, indent);
                        break;
                    case ':':
                        sb.Append(": ");
                        break;
                    case ' ':
                    case '\n':
                    case '\t':
                    case '\r':
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        private bool ExtractJsonBool(string json, string key)
        {
            var truePattern = $"\"{key}\":true";
            if (json.IndexOf(truePattern, StringComparison.Ordinal) >= 0)
                return true;

            var falsePattern = $"\"{key}\":false";
            if (json.IndexOf(falsePattern, StringComparison.Ordinal) >= 0)
                return false;

            return false;
        }

        private int ExtractJsonInt(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                return -1;

            var pattern = $"\"{key}\":";
            var index = json.IndexOf(pattern, StringComparison.Ordinal);
            if (index < 0)
                return -1;

            index += pattern.Length;
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;

            var start = index;
            while (index < json.Length && char.IsDigit(json[index]))
                index++;

            if (index <= start)
                return -1;

            return int.TryParse(json.Substring(start, index - start), out var value) ? value : -1;
        }

        private int ExtractJsonArrayCount(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                return -1;

            var pattern = $"\"{key}\":";
            var index = json.IndexOf(pattern, StringComparison.Ordinal);
            if (index < 0)
                return -1;

            index += pattern.Length;
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;
            if (index >= json.Length || json[index] != '[')
                return -1;

            index++;
            var depth = 0;
            var inString = false;
            var escape = false;
            var count = 0;
            var hasValue = false;
            for (; index < json.Length; index++)
            {
                var ch = json[index];
                if (inString)
                {
                    if (escape) escape = false;
                    else if (ch == '\\') escape = true;
                    else if (ch == '"') inString = false;
                    continue;
                }
                if (ch == '"')
                {
                    inString = true;
                    hasValue = true;
                    continue;
                }
                if (ch == '[' || ch == '{')
                {
                    depth++;
                    hasValue = true;
                    continue;
                }
                if (ch == ']' && depth == 0)
                    return hasValue ? count + 1 : 0;
                if (ch == ']' || ch == '}')
                {
                    depth--;
                    continue;
                }
                if (ch == ',' && depth == 0)
                {
                    count++;
                    hasValue = false;
                    continue;
                }
                if (!char.IsWhiteSpace(ch))
                    hasValue = true;
            }
            return -1;
        }

        private void AppendIndent(System.Text.StringBuilder sb, int indent)
        {
            for (var i = 0; i < indent; i++)
                sb.Append("  ");
        }

        private void DrawSelectableText(string text, GUIStyle style)
        {
            var safeText = text ?? "";
            var width = Mathf.Max(120f, position.width - 72f);
            var height = EstimateSelectableTextHeight(safeText, style, width);
            EditorGUILayout.SelectableLabel(safeText, style, GUILayout.MinHeight(height), GUILayout.ExpandWidth(true));
        }

        private float EstimateSelectableTextHeight(string text, GUIStyle style, float width)
        {
            var safeText = text ?? "";
            var availableWidth = Mathf.Max(80f, width - 8f);
            var approxCharWidth = Mathf.Max(5.5f, (style.fontSize > 0 ? style.fontSize : 12) * 0.55f);
            var charsPerLine = Mathf.Max(1, Mathf.FloorToInt(availableWidth / approxCharWidth));
            var lineCount = 0;

            var logicalLines = safeText.Replace("\r\n", "\n").Split('\n');
            foreach (var logicalLine in logicalLines)
            {
                var length = string.IsNullOrEmpty(logicalLine) ? 1 : logicalLine.Length;
                lineCount += Mathf.Max(1, Mathf.CeilToInt((float)length / charsPerLine));
            }

            var lineHeight = Mathf.Max(16f, style.lineHeight > 0f ? style.lineHeight : EditorGUIUtility.singleLineHeight);
            return Mathf.Max(lineHeight + 6f, lineCount * lineHeight + 6f);
        }

        private void DrawToolHeader(ChatMessage msg, GUIStyle titleStyle)
        {
            var indicator = msg.IsExpanded ? "▼" : "▶";
            var stateText = msg.IsError ? "error" : (msg.IsRunning ? "running" : "done");
            var stateColor = msg.IsError
                ? new Color(0.95f, 0.45f, 0.45f)
                : msg.IsRunning
                    ? new Color(0.95f, 0.8f, 0.35f)
                    : new Color(0.6f, 0.85f, 0.55f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(indicator, EditorStyles.label, GUILayout.Width(18)))
                msg.IsExpanded = !msg.IsExpanded;

            var width = Mathf.Max(120f, position.width - 150f);
            var titleHeight = EstimateSelectableTextHeight(msg.Title ?? "", titleStyle, width);
            EditorGUILayout.SelectableLabel(msg.Title ?? "", titleStyle, GUILayout.MinHeight(titleHeight), GUILayout.ExpandWidth(true));

            var previousColor = GUI.color;
            GUI.color = stateColor;
            GUILayout.Label(stateText, EditorStyles.miniBoldLabel, GUILayout.Width(54));
            GUI.color = previousColor;
            EditorGUILayout.EndHorizontal();
        }

        private void ScrollToBottomSoon()
        {
            ScrollToBottomSoon(false);
        }

        private void ScrollToBottomSoon(bool force)
        {
            if (!force && !_followLatest) return;
            if (_scrollToLatestScheduled) return;

            _scrollToLatestScheduled = true;
            var scheduleVersion = ++_scrollScheduleVersion;
            EditorApplication.delayCall += () =>
            {
                _scrollToLatestScheduled = false;
                if (scheduleVersion != _scrollScheduleVersion) return;
                if (!force && !_followLatest) return;
                if (force)
                    _followLatest = true;
                _chatScrollPos.y = float.MaxValue;
                Repaint();
            };
        }

        private void JumpToLatest()
        {
            _followLatest = true;
            _scrollToLatestScheduled = false;
            _scrollScheduleVersion++;
            _chatScrollPos.y = float.MaxValue;
            Repaint();
        }

        private string GetSpinnerFrame()
        {
            var frames = new[] { "◐", "◓", "◑", "◒" };
            var index = (int)(EditorApplication.timeSinceStartup * 8.0) % frames.Length;
            return frames[index];
        }

        private string ExtractJsonString(string json, string key)
        {
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

        private void SaveRecoverySnapshot()
        {
            try
            {
                var wasInterrupted = HasInFlightAssistantWork();
                var activeSessionId = !string.IsNullOrEmpty(_activeSessionId)
                    ? _activeSessionId
                    : PieChatSessionRecovery.GetLastKnownActiveSession();
                var latestUserText = IsRestorableInputText(_lastSubmittedInputText)
                    ? _lastSubmittedInputText
                    : (GetLatestUserMessage() ?? "");
                PieChatSessionRecovery.Save(
                    activeSessionId,
                    wasInterrupted,
                    latestUserText,
                    _inputText,
                    _provider,
                    _model,
                    _baseUrl,
                    DateTime.UtcNow.Ticks);
            }
            catch (Exception ex)
            {
                PieDiagnostics.Warning($"[PieChatWindow] SaveRecoverySnapshot: {ex.Message}");
            }
        }

        private void SaveRecoverySnapshotIfNeeded()
        {
            if (!HasRecoverableContext())
            {
                return;
            }

            SaveRecoverySnapshot();
            var snapshot = PieChatSessionRecovery.Read();
            PieDiagnostics.Verbose($"[PieChatWindow] SaveRecoverySnapshotIfNeeded: saved session={snapshot.activeSessionId} streaming={snapshot.wasStreaming}");
        }

        private void ScheduleRecoveryRestore(double delaySeconds)
        {
            if (!HasRecoverySnapshot())
                return;

            var target = EditorApplication.timeSinceStartup + Math.Max(0d, delaySeconds);
            if (_recoveryRestoreScheduled)
                return;

            _recoveryRestoreScheduled = true;
            _recoveryRestoreNotBeforeAt = target;
        }

        private void TryRunScheduledRecoveryRestore(bool ignoreDelay)
        {
            if (!_recoveryRestoreScheduled)
                return;

            if (!HasRecoverySnapshot())
            {
                _recoveryRestoreScheduled = false;
                _recoveryRestoreNotBeforeAt = -1;
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            if (_bridge?.IsInitialized != true)
                return;

            if (!_autoResume)
                return;

            if (_isStreaming || _pendingInteraction != null)
                return;

            if (!ignoreDelay && _recoveryRestoreNotBeforeAt > 0 && EditorApplication.timeSinceStartup < _recoveryRestoreNotBeforeAt)
                return;

            _recoveryRestoreScheduled = false;
            _recoveryRestoreNotBeforeAt = -1;
            TryRestoreRecoverySnapshot();
        }

        private bool HasRecoverableContext()
        {
            if (!string.IsNullOrEmpty(_activeSessionId))
                return true;

            if (_isStreaming)
                return true;

            if (!string.IsNullOrWhiteSpace(_inputText))
                return true;

            return _messages != null && _messages.Count > 0;
        }

        private bool HasInFlightAssistantWork()
        {
            if (_isStreaming)
                return true;

            if (_messages == null)
                return false;

            for (var i = 0; i < _messages.Count; i++)
            {
                if (_messages[i] != null && _messages[i].IsRunning)
                    return true;
            }

            return false;
        }

        private void TryRestoreRecoverySnapshot()
        {
            try
            {
                if (!HasRecoverySnapshot())
                    return;

                if (_bridge?.IsInitialized != true)
                    return;

                var snapshot = ReadRecoverySnapshot();
                if (snapshot.savedAtTicksUtc <= 0)
                {
                    ClearRecoverySnapshot();
                    return;
                }

                var age = new TimeSpan(Math.Max(0, DateTime.UtcNow.Ticks - snapshot.savedAtTicksUtc));
                if (age > TimeSpan.FromMinutes(30))
                {
                    ClearRecoverySnapshot();
                    return;
                }

                if (string.IsNullOrEmpty(snapshot.activeSessionId))
                    snapshot.activeSessionId = PieChatSessionRecovery.GetLastKnownActiveSession();

                if (snapshot.wasStreaming)
                {
                    var restoredInput = IsRestorableInputText(snapshot.draftInputText)
                        ? snapshot.draftInputText
                        : (IsRestorableInputText(snapshot.lastUserMessageText) ? snapshot.lastUserMessageText : "");
                    if (!string.IsNullOrEmpty(restoredInput))
                    {
                        _inputText = restoredInput;
                        _pendingRecoveryNotice = "Previous response was interrupted by C# compilation. The last prompt has been restored to the input box.";
                    }
                    else
                    {
                        _pendingRecoveryNotice = "Previous response was interrupted by C# compilation.";
                    }
                }
                else if (IsRestorableInputText(snapshot.draftInputText))
                {
                    _inputText = snapshot.draftInputText;
                    _pendingRecoveryNotice = "Draft input restored after C# compilation.";
                }

                if (string.IsNullOrEmpty(snapshot.activeSessionId))
                {
                    if (!string.IsNullOrEmpty(_pendingRecoveryNotice))
                    {
                        AddSystemMessage(_pendingRecoveryNotice);
                        _pendingRecoveryNotice = "";
                    }
                    ClearRecoverySnapshot();
                    _statusText = "Idle";
                    return;
                }

                _statusText = "Restoring session...";
                SendRecoveryResume(snapshot);
                ClearRecoverySnapshot();
            }
            catch (Exception ex)
            {
                PieDiagnostics.Warning($"[PieChatWindow] TryRestoreRecoverySnapshot: {ex.Message}");
            }
        }

        private PieChatSessionRecovery.Snapshot ReadRecoverySnapshot() => PieChatSessionRecovery.Read();

        private void ClearRecoverySnapshot() => PieChatSessionRecovery.Clear();

        private bool HasRecoverySnapshot() => PieChatSessionRecovery.HasSnapshot();

        private bool IsRestorableInputText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var trimmed = value.Trim();
            return !trimmed.StartsWith("/", StringComparison.Ordinal);
        }

        private string GetLatestUserMessage()
        {
            for (var i = _messages.Count - 1; i >= 0; i--)
            {
                if (_messages[i].Role == "user" && !string.IsNullOrWhiteSpace(_messages[i].Content))
                    return _messages[i].Content;
            }

            return "";
        }

        private string FlattenSessionContent(SessionSyncMessage message)
        {
            if (message?.content == null || message.content.Length == 0)
                return "";

            var parts = new List<string>();
            foreach (var block in message.content)
            {
                if (block == null)
                    continue;

                if (block.type == "text" && !string.IsNullOrEmpty(block.text))
                    parts.Add(block.text);
            }

            return string.Join("", parts);
        }

        // ─── Data ─────────────────────────────────────────────────────────────
        private class ChatMessage
        {
            public string Role;
            public string Content;
            public string Title;
            public string Summary;
            public string ArgsText;
            public string ArgsSummary;
            public string ToolName;
            public string ToolCallId;
            public int InputTokens;
            public int OutputTokens;
            public int CacheReadTokens;
            public int CacheWriteTokens;
            public int TotalTokens;
            public bool IsEstimatedUsage;
            public bool IsCompact;
            public bool IsRunning;
            public bool IsError;
            public bool IsExpanded;
            public ChatMessage(string role, string content) { Role = role; Content = content; }
        }

        [Serializable]
        private class SkillInfo
        {
            public string name;
            public string description;
            public string source;
            public string path;
        }

        [Serializable]
        private class SkillsPayload
        {
            public SkillInfo[] skills;
        }

        [Serializable]
        private class SessionInfo
        {
            public string id;
            public string title;
            public string createdAt;
            public string updatedAt;
            public string parentSessionId;
            public string provider;
            public string modelId;
            public int messageCount;
        }

        [Serializable]
        private class FrameworkSessionFile
        {
            public FrameworkSessionMetadata metadata;
            public SessionSyncMessage[] messages;
        }

        [Serializable]
        private class FrameworkSessionMetadata
        {
            public string id;
            public string name;
            public double createdAt;
            public double updatedAt;
            public string parentSessionId;
            public string provider;
            public string modelId;
            public int messageCount;
        }

        [Serializable]
        private class SessionSyncPayload
        {
            public string id;
            public string title;
            public int messageCount;
            public SessionSyncMessage[] messages;
            public TodoStateItem[] todoState;
        }

        [Serializable]
        private class TodoStateItem
        {
            public int id;
            public string title;
            public string description;
            public string status;
        }

        [Serializable]
        private class TodoToolDetailsPayload
        {
            public string action;
            public TodoStateItem[] todos;
            public TodoStateItem[] completedTodos;
            public bool autoCleared;
            public string error;
        }

        [Serializable]
        private class SessionSyncMessage
        {
            public string role;
            public string errorMessage;
            public string stopReason;
            public SessionSyncUsage usage;
            public SessionSyncContent[] content;
        }

        [Serializable]
        private class SessionSyncUsage
        {
            public int input;
            public int output;
            public int cacheRead;
            public int cacheWrite;
            public int totalTokens;
            public bool estimated;
        }

        [Serializable]
        private class MessageMetricsPayload
        {
            public string role;
            public SessionSyncUsage usage;
        }

        [Serializable]
        private class SessionSyncContent
        {
            public string type;
            public string text;
        }

        private class PendingInteraction
        {
            public PieInteractionRequest request;
            public double openedAt;
            public int selectedIndex;
            public string value;
            public bool completed;
            public string responseJson;
        }
    }
}
