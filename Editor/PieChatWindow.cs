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
        private bool _showSkills = false;
        private bool _showSessions = false;
        private Vector2 _skillsScrollPos;
        private Vector2 _sessionsScrollPos;
        private readonly List<SkillInfo> _skills = new List<SkillInfo>();
        private readonly List<SessionInfo> _sessions = new List<SessionInfo>();
        private string _selectedSkillPath = "";
        private string _selectedSkillName = "";
        private string _skillEditorContent = "";
        private string _newSkillName = "";
        private bool _isCreatingSkill = false;

        // Settings
        private const string PREF_API_KEY      = "Pie_APIKey";
        private const string PREF_PROVIDER     = "Pie_Provider";
        private const string PREF_MODEL        = "Pie_Model";
        private const string PREF_BASE_URL     = "Pie_BaseUrl";
        private const string PREF_SHOW_LOGS    = "Pie_ShowLogs";
        private const string PREF_VERBOSE_LOGS = "Pie_VerboseLogs";

        private string _apiKey    = "";
        private string _provider  = "openai";
        private string _model     = "gpt-4.1-mini";
        private string _baseUrl   = "";
        private bool   _showApiKey = false;
        private bool   _showSettings = true;
        private bool   _showLogs = false;
        private bool   _verboseLogs = false;

        // ─── Menu ─────────────────────────────────────────────────────────────
        [MenuItem("Tools/Pie/Pie Chat #&p")]   // Alt+Shift+P
        public static void ShowWindow()
        {
            var window = GetWindow<PieChatWindow>("Pie");
            window.minSize = new Vector2(360, 500);
            window.Show();
        }

        // ─── Lifecycle ────────────────────────────────────────────────────────
        private void OnEnable()
        {
            _apiKey   = EditorPrefs.GetString(PREF_API_KEY, "");
            _provider = EditorPrefs.GetString(PREF_PROVIDER, "openai");
            _model    = EditorPrefs.GetString(PREF_MODEL, "gpt-4.1-mini");
            _baseUrl  = EditorPrefs.GetString(PREF_BASE_URL, "");
            _showLogs = EditorPrefs.GetBool(PREF_SHOW_LOGS, false);
            _verboseLogs = EditorPrefs.GetBool(PREF_VERBOSE_LOGS, false);
            PieDiagnostics.CurrentLevel = _verboseLogs ? PieLogLevel.Verbose : PieLogLevel.Info;

            ConnectBridge();
        }

        private void OnDisable()
        {
            DisposeBridge();
        }

        private void OnInspectorUpdate()
        {
            if (_isStreaming) Repaint();
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
            }

            RefreshSessions();
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
            EditorGUILayout.LabelField($"{session.modelId} | {session.updatedAt}", EditorStyles.wordWrappedMiniLabel);
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
            if (GUILayout.Button("From Chat", GUILayout.Width(75)))
            {
                _isCreatingSkill = true;
                _selectedSkillPath = "";
                _selectedSkillName = "";
                _newSkillName = SuggestSkillNameFromChat();
                _skillEditorContent = CreateSkillDraftFromChat();
            }
            if (GUILayout.Button("From Memory", GUILayout.Width(90)))
            {
                _isCreatingSkill = true;
                _selectedSkillPath = "";
                _selectedSkillName = "";
                _newSkillName = "project-memory-skill";
                _skillEditorContent = CreateSkillDraftFromMemory();
            }
            if (GUILayout.Button("Reload", GUILayout.Width(60)))
                _bridge?.SendToJs("reload_skills", "{}");
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                _bridge?.SendToJs("list_skills", "{}");
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
            if (_bridge?.IsInitialized != true)
            {
                EditorGUILayout.HelpBox(
                    $"Bridge not connected.\nError: {_bridge?.LastError ?? "unknown"}\nClick Reconnect in toolbar.",
                    MessageType.Error);
            }

            DrawInlineStatusBar();

            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName("PieInput");
            _inputText = EditorGUILayout.TextArea(_inputText, GUILayout.Height(60), GUILayout.ExpandWidth(true));

            var canSend = !_isStreaming
                && !string.IsNullOrWhiteSpace(_inputText)
                && _bridge?.IsInitialized == true;

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

            // Placeholder
            AddMessage("assistant", "…");
            _bridge.SendToJs("send_message", json);
            _inputText = "";
            ScrollToBottomSoon();
        }

        private void AbortCurrentTurn()
        {
            if (_bridge?.IsInitialized != true) return;
            _bridge.SendToJs("abort", "{}");
            _isStreaming = false;
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
                case "state_event":     HandleStateEvent(json);    break;
                case "tool_start":      HandleToolStart(json);     break;
                case "tool_end":        HandleToolEnd(json);       break;
                case "turn_end":        HandleTurnEnd(json);        break;
                case "skills_list":     HandleSkillsList(json);     break;
                case "error":           HandleError(json);          break;
                case "agent_end":
                    _isStreaming = false;
                    _statusText = "Idle";
                    break;
            }
            ScheduleRepaint();
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
                }
                else if (json.Contains("\"type\":\"text_delta\""))
                {
                    var delta = ExtractJsonString(json, "delta");
                    if (delta != null)
                    {
                        last.Content = (last.Content == "…" ? "" : last.Content) + delta;
                    }
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
                    _statusText = "Agent started";
                    break;
                case "turn_start":
                    _statusText = "Turn started";
                    break;
                case "message_start":
                    _statusText = string.IsNullOrEmpty(detail) ? "Composing message..." : detail;
                    break;
                case "message_update":
                    _statusText = string.IsNullOrEmpty(detail) ? "Streaming response..." : $"Streaming response... {detail}";
                    break;
                case "message_end":
                    _statusText = "Message completed";
                    break;
                case "turn_end":
                    _statusText = string.IsNullOrEmpty(detail) ? "Turn completed" : detail;
                    break;
                case "agent_end":
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
            PieDiagnostics.Verbose($"[PieChatWindow] tool_start raw: {json}");
            var toolName = ExtractJsonString(json, "name") ?? "tool";
            var callId = ExtractJsonString(json, "callId");
            var argsJson = ExtractJsonString(json, "argsJson");
            PieDiagnostics.Verbose($"[PieChatWindow] tool_start parsed name={toolName} callId={callId} argsJson={argsJson}");
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
            PieDiagnostics.Verbose($"[PieChatWindow] tool_end raw: {json}");
            var toolName = ExtractJsonString(json, "name") ?? "tool";
            var callId = ExtractJsonString(json, "callId");
            var isError = ExtractJsonBool(json, "isError");
            var error = ExtractJsonString(json, "error");
            var argsJson = ExtractJsonString(json, "argsJson");
            var effectiveArgsJson = ExtractJsonString(json, "effectiveArgsJson");
            var resultText = ExtractJsonString(json, "resultText");
            var resultJson = ExtractJsonString(json, "resultJson");
            var detailsJson = ExtractJsonString(json, "detailsJson");
            PieDiagnostics.Verbose($"[PieChatWindow] tool_end parsed name={toolName} callId={callId} argsJson={argsJson} effectiveArgsJson={effectiveArgsJson}");
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
            ScrollToBottomSoon();
        }

        private void HandleTurnEnd(string json)
        {
            var stopReason = ExtractJsonString(json, "stopReason");

            if (stopReason == null || stopReason == "stop" || stopReason == "error"
                || stopReason == "aborted" || stopReason == "maxTokens")
            {
                _isStreaming = false;
                _statusText = stopReason == "aborted" ? "Aborted" : "Idle";
            }
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
                _messages[_messages.Count - 1] = new ChatMessage("assistant", $"⚠ {msg}");
            else
                AddSystemMessage($"⚠ {msg}");
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

        private string SuggestSkillNameFromChat()
        {
            var latestUser = "";
            for (var i = _messages.Count - 1; i >= 0; i--)
            {
                if (_messages[i].Role == "user")
                {
                    latestUser = _messages[i].Content;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(latestUser))
                return "chat-derived-skill";

            var lower = latestUser.Trim().ToLowerInvariant();
            if (lower.Length > 24) lower = lower.Substring(0, 24);
            return SanitizeSkillName(lower.Replace(" ", "-"));
        }

        private string CreateSkillDraftFromChat()
        {
            var latestUser = "";
            var latestAssistant = "";
            for (var i = _messages.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(latestAssistant) && _messages[i].Role == "assistant")
                    latestAssistant = _messages[i].Content;
                if (string.IsNullOrEmpty(latestUser) && _messages[i].Role == "user")
                    latestUser = _messages[i].Content;
                if (!string.IsNullOrEmpty(latestUser) && !string.IsNullOrEmpty(latestAssistant))
                    break;
            }

            var skillName = SuggestSkillNameFromChat();
            return $"---\nname: {skillName}\ndescription: Derived from recent chat workflow.\n---\n\n# {skillName}\n\nUse this skill when the task resembles the recent workflow below.\n\n## User Intent\n\n{latestUser}\n\n## Observed Assistant Workflow\n\n{latestAssistant}\n\n## Notes\n\n- Refine this draft into stable instructions.\n- Replace chat-specific details with reusable guidance.\n";
        }

        private string CreateSkillDraftFromMemory()
        {
            try
            {
                var projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
                var agentsPath = Pie.PieProjectPaths.GetProjectMemoryPath(projectRoot);
                var memory = System.IO.File.Exists(agentsPath)
                    ? System.IO.File.ReadAllText(agentsPath)
                    : "No AGENTS.md found yet.";
                var excerpt = memory.Length > 2000 ? memory.Substring(0, 2000) + "\n..." : memory;

                return $"---\nname: project-memory-skill\ndescription: Draft skill based on current AGENTS.md project memory.\n---\n\n# project-memory-skill\n\nUse this skill when work should follow the project's persistent conventions.\n\n## Memory Excerpt\n\n{excerpt}\n\n## Notes\n\n- Convert this excerpt into concise reusable rules.\n- Keep the final skill narrower than AGENTS.md.\n";
            }
            catch (Exception ex)
            {
                return $"---\nname: project-memory-skill\ndescription: Draft skill based on current AGENTS.md project memory.\n---\n\n# project-memory-skill\n\nFailed to read AGENTS.md: {ex.Message}\n";
            }
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
                        var session = JsonUtility.FromJson<SessionInfo>(json);
                        if (session != null && !string.IsNullOrEmpty(session.id))
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

        // ─── Helpers ──────────────────────────────────────────────────────────
        private void AddMessage(string role, string content) =>
            _messages.Add(new ChatMessage(role, content));

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
            if (toolName == "inspect_unity_context")
            {
                var source = !string.IsNullOrEmpty(detailsJson) ? detailsJson : resultText;
                var pipeline = ExtractJsonString(source, "renderPipeline") ?? "unknown pipeline";
                var scene = ExtractJsonString(source, "sceneName") ?? "unknown scene";
                return $"{pipeline}, scene={scene}";
            }

            if (string.IsNullOrEmpty(resultText)) return "Done.";

            if (toolName == "write_project_memory")
            {
                return "AGENTS.md updated";
            }

            if (toolName == "read_project_memory")
            {
                return resultText.Contains("No project memory found", StringComparison.Ordinal)
                    ? "No AGENTS.md yet"
                    : "Loaded AGENTS.md";
            }

            if (toolName == "manage_todo_list")
            {
                if (resultText.Contains("Validation failed", StringComparison.Ordinal)) return "Todo validation failed";
                if (resultText.Contains("No todos", StringComparison.Ordinal)) return "No todos";
                return resultText.Replace("Todos updated successfully. ", "").Trim();
            }

            return resultText.Replace("\r", "").Trim();
        }

        private string SummarizeToolFailure(string toolName, string errorText)
        {
            if (toolName == "manage_todo_list" && errorText.Contains("Validation failed", StringComparison.Ordinal))
                return "Todo validation failed";
            return errorText.Replace("\r", "").Trim();
        }

        private string BuildArgsSummary(string argsJson)
        {
            if (string.IsNullOrEmpty(argsJson))
                return null;

            var formatted = FormatStructuredText(argsJson);
            if (string.IsNullOrEmpty(formatted))
                return null;

            var singleLine = formatted.Replace("\r", "").Replace("\n", " ").Trim();
            if (singleLine.Length > 160)
                singleLine = singleLine.Substring(0, 160) + "...";

            return $"args: {singleLine}";
        }

        private string BuildEffectiveArgsFromDetails(string toolName, string detailsJson)
        {
            if (string.IsNullOrEmpty(detailsJson))
                return null;

            if (toolName == "search_files")
            {
                var effectivePattern = ExtractJsonString(detailsJson, "effectivePattern");
                var searchPath = ExtractJsonString(detailsJson, "searchPath");
                var rawPattern = ExtractJsonString(detailsJson, "rawPattern");
                var parts = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrEmpty(rawPattern)) parts.Add($"rawPattern={rawPattern}");
                if (!string.IsNullOrEmpty(effectivePattern)) parts.Add($"pattern={effectivePattern}");
                if (!string.IsNullOrEmpty(searchPath)) parts.Add($"path={searchPath}");
                return parts.Count > 0 ? $"args: {{ {string.Join(", ", parts)} }}" : null;
            }

            if (toolName == "search_file_content")
            {
                var effectivePattern = ExtractJsonString(detailsJson, "effectivePattern");
                var searchPath = ExtractJsonString(detailsJson, "searchPath");
                var glob = ExtractJsonString(detailsJson, "glob");
                var parts = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrEmpty(effectivePattern)) parts.Add($"pattern={effectivePattern}");
                if (!string.IsNullOrEmpty(glob)) parts.Add($"glob={glob}");
                if (!string.IsNullOrEmpty(searchPath)) parts.Add($"path={searchPath}");
                return parts.Count > 0 ? $"args: {{ {string.Join(", ", parts)} }}" : null;
            }

            return null;
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
            if (!_followLatest) return;
            if (_scrollToLatestScheduled) return;

            _scrollToLatestScheduled = true;
            EditorApplication.delayCall += () =>
            {
                _scrollToLatestScheduled = false;
                if (!_followLatest) return;
                _chatScrollPos.y = float.MaxValue;
                Repaint();
            };
        }

        private void JumpToLatest()
        {
            _followLatest = true;
            _scrollToLatestScheduled = false;
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
    }
}
