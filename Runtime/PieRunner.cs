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

        /// <summary>Raw JSON of the most recent skills_list event from the JS
        /// runtime (empty until the first scan completes). The player smoke
        /// uses it to prove the JS runtime scanned the skill filesystem
        /// layers through puerts-fs in THIS player.</summary>
        internal string LastSkillsListJson { get; private set; } = "";
        private PieBridge _bridge;
        // RPC retains the latest visible answer; delta comparison is per response.
        private string _lastAssistantText = "";
        private string _assistantDeltaText = "";
        private long _runtimeRpcEventVersion;
        private readonly List<DevRpcMessagePayload> _devRpcMessages = new List<DevRpcMessagePayload>();
        private string _runtimeStatusText = "Idle";
        private bool _runtimeIsStreaming;
        private string _lastStructuredErrorMessage = "";
        [SerializeField] private string _projectRootOverride = "";
        [SerializeField] private PieSettings _settingsOverride;

        public enum RuntimeConfirmationPolicy { Yolo, Confirm, Deny }
        [SerializeField] private RuntimeConfirmationPolicy _confirmationPolicy = RuntimeConfirmationPolicy.Yolo;
        [SerializeField, Min(1000)] private int _confirmationTimeoutMs = 60000;
        public event Action<PieInteractionRequest> OnConfirmationRequested;
        private readonly object _confirmationLock = new object();
        private readonly Dictionary<string, PendingConfirmation> _confirmations = new Dictionary<string, PendingConfirmation>();

        private sealed class PendingConfirmation
        {
            public PieInteractionResponse response;
            public System.Threading.Timer timer;
        }

        public string GetRuntimePolicyJson()
        {
            if (_confirmationPolicy == RuntimeConfirmationPolicy.Yolo)
                return "{\"mode\":\"normal\",\"confirmationPolicy\":\"allow\",\"yoloMode\":true}";
            return _confirmationPolicy == RuntimeConfirmationPolicy.Deny
                ? "{\"mode\":\"normal\",\"confirmationPolicy\":\"deny\",\"yoloMode\":false}"
                : "{\"mode\":\"normal\",\"confirmationPolicy\":\"confirm\",\"yoloMode\":false}";
        }

        public bool RespondToConfirmation(string id, bool confirmed)
        {
            lock (_confirmationLock)
            {
                if (id == null || !_confirmations.TryGetValue(id, out var pending) || pending.response != null)
                    return false;
                pending.response = new PieInteractionResponse { type = "confirm", id = id, confirmed = confirmed };
                pending.timer?.Dispose();
                return true;
            }
        }

        private void CancelConfirmations()
        {
            lock (_confirmationLock)
            {
                foreach (var entry in _confirmations)
                {
                    entry.Value.timer?.Dispose();
                    // Abort also revokes an approval that JS has not consumed yet.
                    entry.Value.response = new PieInteractionResponse { type = "confirm", id = entry.Key, confirmed = false, skipped = true };
                }
            }
        }

        private void OnDisable()
        {
            CancelConfirmations();
            if (_bridge?.IsInitialized == true) _bridge.SendToJs("abort", "{}");
        }

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
        public event Action<PieSessionBindingResult> OnSessionBindingResult;

        [Serializable]
        public sealed class PieSessionBindingResult
        {
            public string requestId;
            public string operation;
            public string requestedSessionId;
            public string sessionId;
            public bool ok;
            public string code;
        }
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
        private class InstallSkillPayload
        {
            public string name;
            public string content;
        }

        [Serializable]
        private class SkillsListPayload
        {
            public SkillsListEntry[] skills;
        }

        [Serializable]
        private class SkillsListEntry
        {
            public string name;
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
            RecoverInterruptedSkillInstalls();
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

        private void OnApplicationPause(bool paused)
        {
            if (paused) PieWebSocketBridge.CancelAll();
        }

        private void OnDestroy()
        {
            CancelConfirmations();
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
            // Raw /rpc endpoints (legacy dispatch fallback). Capability-manifest
            // registrations for the runner live in PieSettings.RegisterRuntimeCapabilities
            // so editor and player tooling discover them through one source.
            PieDevRpc.Register("runner.get_state", _ => BuildRuntimeRpcStateJson());
            PieDevRpc.Register("runner.send_message", argsJson => DevRpcChatSend(argsJson));
            PieDevRpc.Register("runner.abort", _ => { Abort(); return BuildRuntimeRpcStateJson(); });
            PieDevRpc.Register("runner.reload_skills", _ => "{\"dispatched\":" + (ReloadSkills() ? "true" : "false") + "}");
            PieDevRpc.Register("runner.install_skill", argsJson =>
            {
                var payload = JsonUtility.FromJson<InstallSkillPayload>(argsJson ?? "{}") ?? new InstallSkillPayload();
                return InstallSkill(payload.name, payload.content);
            });
        }

        private void UnregisterDevRpcMethods()
        {
            PieDevRpc.Unregister("runner.get_state");
            PieDevRpc.Unregister("runner.send_message");
            PieDevRpc.Unregister("runner.abort");
            PieDevRpc.Unregister("runner.reload_skills");
            PieDevRpc.Unregister("runner.install_skill");
        }

        private string HandleInteractionHostCall(string argsJson)
        {
            var request = JsonUtility.FromJson<PieInteractionRequest>(argsJson ?? "{}") ?? new PieInteractionRequest();
            if (request.type == "confirm_state")
            {
                lock (_confirmationLock)
                {
                    if (request.id == null || !_confirmations.TryGetValue(request.id, out var pending))
                        return PieInteractionResponse.Unavailable("confirm", request.id, "Confirmation is no longer available.");
                    if (pending.response == null) return "{\"pending\":true}";
                    _confirmations.Remove(request.id);
                    pending.timer?.Dispose();
                    return PieInteractionResponse.ToJson(pending.response);
                }
            }
            if (request.type == "confirm")
            {
                if (!isActiveAndEnabled || _confirmationPolicy == RuntimeConfirmationPolicy.Deny || OnConfirmationRequested == null)
                    return PieInteractionResponse.ToJson(new PieInteractionResponse { type = "confirm", id = request.id, confirmed = false, skipped = true });
                if (string.IsNullOrWhiteSpace(request.id))
                    return PieInteractionResponse.Unavailable("confirm", request.id, "Confirmation id is required.");
                lock (_confirmationLock)
                {
                    if (_confirmations.ContainsKey(request.id) || _confirmations.Count >= 64)
                        return PieInteractionResponse.Unavailable("confirm", request.id, "Confirmation capacity or duplicate id.");
                    var pending = new PendingConfirmation();
                    _confirmations.Add(request.id, pending);
                    var timeout = Math.Min(Math.Max(1000, _confirmationTimeoutMs), 300000);
                    if (request.timeoutMs > 0) timeout = Math.Min(timeout, request.timeoutMs);
                    pending.timer = new System.Threading.Timer(_ =>
                    {
                        // No Unity/PuerTS calls: expiry works even while the runner has no ticks.
                        lock (_confirmationLock)
                        {
                            if (pending.response == null)
                                pending.response = new PieInteractionResponse { type = "confirm", id = request.id, confirmed = false, timedOut = true };
                        }
                    }, null, timeout, System.Threading.Timeout.Infinite);
                }
                try { OnConfirmationRequested(request); }
                catch { RespondToConfirmation(request.id, false); }
                return "{\"pending\":true}";
            }
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

        // Scoped to synchronous SendToJs callbacks; nested sends restore their
        // own optimistic snapshot before notifying OnRuntimeError listeners.
        private Action<PieRuntimeError> _restoreRejectedRuntimeSend;

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
            var json = JsonUtility.ToJson(request);
            var bridge = _bridge;
            var eventVersion = _runtimeRpcEventVersion;
            var previousRows = _devRpcMessages.ToArray(); // bounded to 200; also undo trimming on rejection
            var previousText = _lastAssistantText;
            var previousDelta = _assistantDeltaText;
            var previousStatus = _runtimeStatusText;
            var wasStreaming = _runtimeIsStreaming;
            // A queued/steering submission does not start a new assistant response.
            if (!_runtimeIsStreaming)
            {
                _lastAssistantText = "";
                _assistantDeltaText = "";
            }
            AddDevRpcMessage("user", "", request.displayContent, "", false, false);
            _runtimeStatusText = "Sending message";
            _runtimeIsStreaming = true;
            Action restore = () =>
            {
                _devRpcMessages.Clear();
                _devRpcMessages.AddRange(previousRows);
                _lastAssistantText = previousText;
                _assistantDeltaText = previousDelta;
                _runtimeStatusText = previousStatus;
                _runtimeIsStreaming = wasStreaming;
            };
            var previousRestore = _restoreRejectedRuntimeSend;
            _restoreRejectedRuntimeSend = error =>
            {
                if (ReferenceEquals(_bridge, bridge)
                    && string.Equals(error.clientTurnId ?? "", request.clientTurnId.Trim(), StringComparison.Ordinal))
                    restore();
            };
            try
            {
                var dispatched = bridge.SendToJs("send_message", json);
                // A transport rejection with no callback restores the same snapshot.
                if (!dispatched && _runtimeRpcEventVersion == eventVersion && ReferenceEquals(_bridge, bridge))
                    restore();
                return dispatched;
            }
            finally { _restoreRejectedRuntimeSend = previousRestore; }
        }

        public bool ResumeSession(string sessionId, bool recoverInterrupted = false)
            => ResumeSession(sessionId, recoverInterrupted, null);

        /// <summary>True means dispatch only. Await OnSessionBindingResult for requestId.</summary>
        public bool ResumeSession(string sessionId, bool recoverInterrupted, string requestId)
        {
            if (_bridge?.IsInitialized != true || string.IsNullOrWhiteSpace(sessionId)) return false;
            var payload = new ResumeSessionPayload
            {
                sessionId = sessionId.Trim(),
                recoverInterrupted = recoverInterrupted,
                requestId = requestId,
            };
            _runtimeStatusText = "Loading session";
            return _bridge.SendToJs("resume_session", JsonUtility.ToJson(payload));
        }

        public bool NewSession() => NewSession(null);

        /// <summary>True means dispatch only. Await OnSessionBindingResult for requestId.</summary>
        public bool NewSession(string requestId)
        {
            if (_bridge?.IsInitialized != true) return false;
            AddDevRpcMessage("system", "", "/new", "Create new session", false, false);
            return _bridge.SendToJs("new_session", JsonUtility.ToJson(new ResumeSessionPayload { requestId = requestId }));
        }

        public bool ReloadExtensions()
        {
            if (_bridge?.IsInitialized != true) return false;
            return _bridge.SendToJs("reload_extensions", "{}");
        }

        /// <summary>Ask the JS runtime to rescan skill sources (filesystem
        /// layers + baked manifest) and re-emit skills_list.</summary>
        public bool ReloadSkills()
        {
            if (_bridge?.IsInitialized != true) return false;
            return _bridge.SendToJs("reload_skills", "{}");
        }

        /// <summary>
        /// Install a skill into the player-writable filesystem layer
        /// (&lt;persistentDataPath&gt;/.agents/skills/&lt;name&gt;/SKILL.md) and reload
        /// skill sources. Player builds have no Assets tree; this layer is the
        /// runtime-installable counterpart to the baked manifest.
        /// </summary>
        public string InstallSkill(string name, string content)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("skill name is required");
            if (content == null) throw new InvalidOperationException("skill content is required");
            var invalid = name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars());
            // GetInvalidFileNameChars does not include '.', so traversal via
            // ".." must be rejected explicitly.
            if (invalid >= 0 || name.Contains("/") || name.Contains("\\") || name != name.Trim()
                || name == "." || name == ".." || name.Contains(".."))
                throw new InvalidOperationException($"invalid skill name: {name}");
            // The filesystem loader registers skills by the FRONTMATTER name,
            // not the install argument: a mismatch would write <name>/SKILL.md
            // yet register it under the frontmatter identity, stranding the
            // directory (a later install under the other identity could never
            // replace it). Content with NO parsable frontmatter name is
            // rejected too: the loader does not reject it — it falls back to
            // the FILE identity "SKILL", which strands the directory the same
            // way (review 2026-10-12, round 2).
            var frontmatterName = ExtractFrontmatterName(content);
            if (frontmatterName == null)
                throw new InvalidOperationException(
                    "skill content must carry --- frontmatter with a name: field (without one the loader registers it under the fallback identity 'SKILL')");
            if (!string.Equals(frontmatterName, name, System.StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"skill name mismatch: install argument '{name}' but frontmatter name '{frontmatterName}' — install under the frontmatter name so the directory matches the registered identity");
            var dir = System.IO.Path.Combine(
                System.IO.Path.Combine(Application.persistentDataPath, ".agents"), "skills");
            var skillDir = System.IO.Path.Combine(dir, name);
            var path = System.IO.Path.Combine(skillDir, "SKILL.md");
            AssertPathStaysUnderRoot(Application.persistentDataPath, dir, path);
            System.IO.Directory.CreateDirectory(skillDir);
            AssertPathStaysUnderRoot(Application.persistentDataPath, dir, path + ".install-lock");
            WriteAllTextAtomic(path, content);
            // SendToJs only proves DISPATCH: the JS reload handler is async and
            // reports rebuild failures through error events, never through this
            // synchronous return. The response field says exactly that — the
            // old `reloaded` name claimed a reload that was never confirmed.
            var dispatched = ReloadSkills();
            if (!dispatched) PieDiagnostics.Warning("[PieRunner] skill file written but reload_skills dispatch failed; the skill loads on next bridge init");
            return "{\"path\":\"" + EscapeJsonString(path) + "\",\"dispatched\":" + (dispatched ? "true" : "false") + "}";
        }

        /// <summary>Frontmatter name extraction mirroring the ACTUAL loader
        /// (agent-framework skills/parser.ts: parseSkillFile + parseFrontmatter)
        /// rather than the linter's stricter YAML subset: the file must START
        /// with a --- fence line whose closing --- line is followed by a
        /// newline (a fence at EOF has no frontmatter); lines are TRIMMED, so
        /// indented fields count; # lines are skipped; values keep trailing
        /// comments (only whole-value quotes strip); and the LAST duplicate
        /// name wins. Null when no parsable frontmatter name exists.
        /// </summary>
        private static string ExtractFrontmatterName(string content)
        {
            if (string.IsNullOrEmpty(content)) return null;
            // Only CRLF is normalized: the JS regex's \s*\n absorbs \r\n, but
            // lone-\r content has no \n for it to match — the loader sees NO
            // frontmatter there, and normalizing lone \r away would diverge
            // (review 2026-10-12, round 3).
            var normalized = content.Replace("\r\n", "\n");
            var lines = normalized.Split('\n');
            // Opening fence: the file must START with a --- line (leading
            // whitespace or a BOM means no frontmatter, like the JS ^--- anchor).
            if (!normalized.StartsWith("---") || lines[0].TrimEnd() != "---" || lines.Length < 3) return null;
            var end = -1;
            for (var index = 1; index < lines.Length; index++)
            {
                if (lines[index].TrimEnd() != "---") continue;
                // The JS regex requires a newline AFTER the closing fence: a
                // file ending exactly at the fence has no frontmatter at all.
                if (index == lines.Length - 1) return null;
                end = index;
                break;
            }
            if (end == -1) return null;
            var name = "";
            for (var index = 1; index < end; index++)
            {
                var line = lines[index].Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var colon = line.IndexOf(':');
                if (colon == -1) continue;
                if (line.Substring(0, colon).Trim() != "name") continue;
                var value = line.Substring(colon + 1).Trim();
                // JS parseFrontmatter uses `value = value.slice(1, -1)` with
                // NO length guard: a lone single OR double quote becomes "".
                // Substring needs a nonnegative count to mirror that slicing.
                if ((value.StartsWith("\"") && value.EndsWith("\"")) || (value.StartsWith("'") && value.EndsWith("'")))
                    value = value.Substring(1, Math.Max(0, value.Length - 2));
                // parseFrontmatter converts bracket lists to arrays and
                // String(array) joins with commas: `name: [t]` registers as
                // "t", not "[t]" — mirror that or the identity check diverges
                // (review 2026-10-12, round 3).
                if (value.StartsWith("[") && value.EndsWith("]"))
                {
                    var parts = value.Substring(1, value.Length - 2).Split(',');
                    var items = new System.Collections.Generic.List<string>();
                    foreach (var part in parts)
                    {
                        var item = part.Trim();
                        // Mirror parseFrontmatter's ORDER exactly: empties drop
                        // BEFORE unquoting, and at most ONE leading and ONE
                        // trailing quote are removed independently (its regex
                        // is /^["]|["]$/g — NOT Trim, which strips every
                        // boundary quote). `name: ["", t]` must yield the same
                        // identity (",t") on both hosts; the previous
                        // Trim('"', '\'') accepted it as "t" here while the
                        // loader registered ",t" — recreating the identity
                        // mismatch this check exists to catch (review
                        // 2026-10-12, round 4).
                        if (item.Length == 0) continue;
                        if (item[0] == '"' || item[0] == '\'') item = item.Substring(1);
                        if (item.Length > 0 && (item[item.Length - 1] == '"' || item[item.Length - 1] == '\'')) item = item.Substring(0, item.Length - 1);
                        items.Add(item);
                    }
                    value = string.Join(",", items);
                }
                name = value; // last duplicate wins, exactly like the JS loop
            }
            return name.Length > 0 ? name : null;
        }

        /// <summary>Startup recovery for interrupted skill installs: a crash
        /// or domain reload between ReplaceByBackupMove's two moves leaves the
        /// previous SKILL.md parked as a sibling `.old-*` backup with no live
        /// file. Runs once per runner startup; the per-skill OS-held lock also
        /// excludes installs in other runners/processes sharing this root.</summary>
        private static void RecoverInterruptedSkillInstalls()
        {
            var dir = System.IO.Path.Combine(
                System.IO.Path.Combine(Application.persistentDataPath, ".agents"), "skills");
            try
            {
                // Anchor the sweep's containment walk at persistentDataPath so
                // reparse points ABOVE the skills root (.agents itself) are
                // guarded exactly like InstallSkill's AssertPathStaysUnderRoot.
                RecoverOrphanedSkillBackups(dir, anchor: Application.persistentDataPath);
            }
            catch (Exception ex)
            {
                // Recovery must never block startup; a failed sweep retries on
                // the next domain reload with the backups still in place.
                PieDiagnostics.Warning($"[PieRunner] orphaned skill backup sweep failed: {ex.Message}");
            }
        }

        /// <summary>Scan a skills root for orphaned install backups. When a
        /// skill directory has `.old-*` backups but NO live SKILL.md, the
        /// newest backup is restored into place (the previous installation
        /// must survive every crash short of losing the disk). When the live
        /// file exists, the backups are residue of failed post-commit deletes:
        /// each one older than <paramref name="staleAgeMinutes"/> is removed,
        /// while FRESH parked backups are kept conservatively. Both branches
        /// hold the same per-skill exclusive lock as installation: a concurrent
        /// editor/player install's missing-live-file window must NOT trigger
        /// recovery, regardless of backup age. A busy skill is skipped until
        /// the next startup. Every mutated path is first proven to stay under
        /// <paramref name="anchor"/> (default: the skills root itself) with no
        /// reparse point on the way — InstallSkill rejects such paths, and the
        /// recovery walk must not restore/delete through a symlinked directory
        /// that redirects outside the installation root; failing paths are
        /// skipped and left for manual inspection. Returns the number of
        /// restored installations.</summary>
        public static int RecoverOrphanedSkillBackups(string skillsRoot, double staleAgeMinutes = 10, string anchor = null)
        {
            var recovered = 0;
            if (string.IsNullOrEmpty(skillsRoot) || !System.IO.Directory.Exists(skillsRoot)) return 0;
            // Anchor the containment walk where the caller did: the product
            // path anchors at persistentDataPath so components ABOVE the skills
            // root (.agents itself) are guarded exactly like InstallSkill.
            var containmentAnchor = string.IsNullOrEmpty(anchor) ? skillsRoot : anchor;
            foreach (var skillDir in System.IO.Directory.GetDirectories(skillsRoot))
            {
                if (!PathStaysUnderRoot(containmentAnchor, skillsRoot, skillDir))
                {
                    PieDiagnostics.Warning($"[PieRunner] skipped skill directory during backup recovery; path is not safely under the skills root (possible reparse point), inspect manually: {skillDir}");
                    continue;
                }
                var live = System.IO.Path.Combine(skillDir, "SKILL.md");
                var lockPath = live + ".install-lock";
                if (!PathStaysUnderRoot(containmentAnchor, skillsRoot, lockPath)) continue;
                System.IO.FileStream installLock;
                try { installLock = AcquireSkillInstallLock(live); }
                catch (System.IO.IOException) { continue; } // active install/recovery: retry next startup
                catch (UnauthorizedAccessException) { continue; } // unreadable lock: leave backups alone
                using (installLock)
                {
                    // Hold the SAME lock as the writer before inspecting live or
                    // backups, through every restore/delete. Otherwise a normal
                    // backup-move window looks exactly like an interrupted install.
                    string[] backups;
                    try { backups = System.IO.Directory.GetFiles(skillDir, "SKILL.md.old-*"); }
                    catch { continue; } // unreadable directory: not our call to delete anything
                    if (backups.Length == 0) continue;
                    if (!System.IO.File.Exists(live))
                    {
                        // Crash between the two backup moves: restore the NEWEST
                        // parked copy (several orphans accumulate only across
                        // repeated crashes).
                        string newestBackup = null;
                        var newestWrite = DateTime.MinValue;
                        foreach (var backup in backups)
                        {
                            DateTime write;
                            try { write = System.IO.File.GetLastWriteTimeUtc(backup); }
                            catch { continue; } // unreadable backup: leave it for manual inspection
                            if (newestBackup == null || write >= newestWrite)
                            {
                                newestBackup = backup;
                                newestWrite = write;
                            }
                        }
                        if (newestBackup == null) continue;
                        if (!PathStaysUnderRoot(containmentAnchor, skillsRoot, newestBackup)
                            || !PathStaysUnderRoot(containmentAnchor, skillsRoot, live))
                        {
                            PieDiagnostics.Warning($"[PieRunner] skipped orphaned backup restore; path is not safely under the skills root (possible reparse point), inspect manually: {newestBackup}");
                            continue;
                        }
                        try
                        {
                            System.IO.File.Move(newestBackup, live);
                            recovered++;
                            PieDiagnostics.Warning(
                                $"[PieRunner] restored {live} from orphaned install backup {System.IO.Path.GetFileName(newestBackup)} (interrupted before commit)");
                        }
                        catch (Exception ex)
                        {
                            // Keep the backup on disk and surface the path: manual
                            // recovery must stay possible.
                            PieDiagnostics.Error(
                                $"[PieRunner] could not restore {live} from backup {newestBackup}; the previous content survives there: {ex.Message}");
                        }
                        continue;
                    }
                    foreach (var backup in backups)
                    {
                        DateTime write;
                        try { write = System.IO.File.GetLastWriteTimeUtc(backup); }
                        catch { continue; }
                        if ((DateTime.UtcNow - write).TotalMinutes > staleAgeMinutes)
                        {
                            // Live file committed long after the backup was parked:
                            // the parked copy is garbage. Fresh ones stay as a
                            // conservative grace period after transaction completion.
                            if (!PathStaysUnderRoot(containmentAnchor, skillsRoot, backup))
                            {
                                PieDiagnostics.Warning($"[PieRunner] skipped stale backup delete; path is not safely under the skills root (possible reparse point), inspect manually: {backup}");
                                continue;
                            }
                            try { System.IO.File.Delete(backup); }
                            catch { /* best effort; retried by the next sweep */ }
                        }
                    }
                }
            }
            return recovered;
        }

        /// <summary>Cross-process per-skill transaction ownership. Both writer
        /// and recovery open the SAME persistent sibling with FileShare.None.
        /// Contention fails closed (install reports busy; recovery skips it).
        /// The OS releases the exclusive handle on dispose/domain reload/process
        /// exit, so a crash leaves no stale ownership to expire by age. The
        /// empty lock file may remain indefinitely: NEVER delete it, since
        /// unlinking it can let another process lock a different inode while
        /// the original transaction is still active. Callers containment-check
        /// the lock path before opening it, just like the skill/backup paths.
        /// </summary>
        private static System.IO.FileStream AcquireSkillInstallLock(string path)
        {
            return new System.IO.FileStream(path + ".install-lock", System.IO.FileMode.OpenOrCreate,
                System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);
        }

        /// <summary>Atomic replace via a sibling temp file: the raw
        /// WriteAllText truncated an existing installed SKILL.md IN PLACE, so
        /// a mid-write failure (disk full, domain reload) destroyed the only
        /// copy. File.Replace keeps the rename-commit contract where the
        /// profile has it; the backup-restore fallback preserves the previous
        /// file elsewhere — a plain Delete+Move could destroy it when the move
        /// fails or the process dies between the two steps.</summary>
        private static void WriteAllTextAtomic(string path, string content)
        {
            using (AcquireSkillInstallLock(path))
            {
                var temp = path + ".tmp-" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
                try
                {
                    System.IO.File.WriteAllText(temp, content);
                    if (!System.IO.File.Exists(path))
                    {
                        System.IO.File.Move(temp, path);
                        return;
                    }
                    try
                    {
                        System.IO.File.Replace(temp, path, null);
                    }
                    catch (System.NotImplementedException) { ReplaceByBackupMove(temp, path); }
                    catch (System.PlatformNotSupportedException) { ReplaceByBackupMove(temp, path); }
                }
                finally
                {
                    try { if (System.IO.File.Exists(temp)) System.IO.File.Delete(temp); } catch { /* best effort */ }
                }
            }
        }

        /// <summary>Replace without File.Replace: park the destination as a
        /// sibling backup, move the new content into place, and RESTORE the
        /// backup when the move fails or dies mid-way — the previous
        /// installation must survive every failure short of losing the disk.
        /// </summary>
        private static void ReplaceByBackupMove(string temp, string path)
        {
            var backup = path + ".old-" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            System.IO.File.Move(path, backup);
            // File.Move PRESERVES the source's LastWriteTime, but the startup
            // sweep ages backups by exactly that timestamp: a long-installed
            // skill parked milliseconds ago already looks stale, and a
            // concurrent sweep (editor + player share persistentDataPath)
            // could delete this in-flight rollback copy. The PARK time is the
            // age that matters, so stamp it onto the parked file (review
            // 2026-10-12).
            try { System.IO.File.SetLastWriteTimeUtc(backup, System.DateTime.UtcNow); }
            catch (Exception ex)
            {
                PieDiagnostics.Warning($"[PieRunner] could not stamp the parking time on {backup}; later backup sweeps may treat it as stale: {ex.Message}");
            }
            try
            {
                System.IO.File.Move(temp, path);
            }
            catch
            {
                try { if (!System.IO.File.Exists(path)) System.IO.File.Move(backup, path); }
                catch (Exception restoreError)
                {
                    // Never swallow a failed restore silently: the caller (and
                    // the log) must learn where the previous installation
                    // survived, or the failure reads as data loss (review
                    // 2026-10-12, round 5). The startup sweep picks the orphan
                    // up on the next domain reload.
                    throw new System.IO.IOException(
                        $"skill replace failed and the previous file could not be restored; the previous content survives at {backup}: {restoreError.Message}",
                        restoreError);
                }
                throw;
            }
            try { System.IO.File.Delete(backup); } catch { /* best effort; startup sweep removes stale residue */ }
        }

        /// <summary>
        /// Reject paths that escape <paramref name="root"/> lexically, or that
        /// pass through reparse points (symlinks/junctions) planted on any
        /// existing component strictly below <paramref name="anchor"/> —
        /// including components ABOVE the skills root (e.g. .agents itself).
        /// Path.GetFullPath alone does not resolve symlinks, so a pre-planted
        /// directory link would silently redirect CreateDirectory/WriteAllText
        /// outside the skills root. Missing components are fine: they will be
        /// created fresh by the caller.
        /// </summary>
        private static void AssertPathStaysUnderRoot(string anchor, string root, string target)
        {
            var fullAnchor = System.IO.Path.GetFullPath(anchor);
            var fullRoot = System.IO.Path.GetFullPath(root);
            var fullTarget = System.IO.Path.GetFullPath(target);
            var anchorPrefix = WithTrailingSeparator(fullAnchor);
            var rootPrefix = WithTrailingSeparator(fullRoot);
            // The root may sit AT the anchor (the recovery sweep anchors its own
            // walk at the skills root): only a root strictly BELOW the anchor
            // needs the prefix check. Callers that pass an unrelated root still
            // fail closed here.
            if (!(string.Equals(fullRoot, fullAnchor, StringComparison.Ordinal) || fullRoot.StartsWith(anchorPrefix, StringComparison.Ordinal))
                || !fullTarget.StartsWith(rootPrefix, StringComparison.Ordinal))
                throw new InvalidOperationException("skill path escapes the skills root");

            var current = fullAnchor;
            var relative = fullTarget.Substring(anchorPrefix.Length);
            foreach (var segment in relative.Split(System.IO.Path.DirectorySeparatorChar))
            {
                if (string.IsNullOrEmpty(segment)) continue;
                current = System.IO.Path.Combine(current, segment);
                System.IO.FileAttributes attributes;
                try
                {
                    attributes = System.IO.File.GetAttributes(current);
                }
                catch (System.IO.FileNotFoundException) { continue; }
                catch (System.IO.DirectoryNotFoundException) { continue; }
                // Other failures (access denied, IO errors) leave the component
                // existing but uninspectable: propagate so InstallSkill fails
                // closed instead of writing through an unknown path.
                if ((attributes & System.IO.FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException($"skill path component is a reparse point: {segment}");
            }
        }

        /// <summary>Non-throwing twin of <see cref="AssertPathStaysUnderRoot"/>
        /// for the RECOVERY walk: the startup sweep must skip (and leave for
        /// manual inspection) any path whose containment cannot be proven —
        /// a reparse point or an escaping path — instead of failing the whole
        /// sweep or mutating through it (InstallSkill fails closed by throwing;
        /// recovery must never block startup).</summary>
        private static bool PathStaysUnderRoot(string anchor, string root, string target)
        {
            try
            {
                AssertPathStaysUnderRoot(anchor, root, target);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string WithTrailingSeparator(string path)
        {
            return path.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + System.IO.Path.DirectorySeparatorChar;
        }

        public bool Abort()
        {
            CancelConfirmations();
            if (_bridge?.IsInitialized != true) return false;
            return _bridge.SendToJs("abort", "{}");
        }

        public bool Reinitialize(string projectRoot = null)
        {
            CancelConfirmations();
            lock (_confirmationLock) _confirmations.Clear();
            _projectRootOverride = projectRoot ?? "";

            if (_bridge != null)
            {
                _bridge.OnJsEvent -= HandleJsEvent;
                _bridge.Dispose();
                _bridge = null;
            }

            ResetRuntimeRpcHistory();
            ActiveSessionId = "";
            CurrentSession = null;
            CurrentStatus = new PieAgentStatus();
            return InitializeBridge();
        }

        private void HandleJsEvent(string eventName, string json)
        {
            _runtimeRpcEventVersion++;
            switch (eventName)
            {
                case "message_update":
                {
                    _runtimeIsStreaming = true;
                    _runtimeStatusText = "Streaming";
                    var text = ExtractJsonString(json, "text");
                    if (text != null)
                    {
                        var previousText = _assistantDeltaText;
                        _lastAssistantText = text;
                        _assistantDeltaText = text;
                        UpsertLatestAssistantMessage(text);
                        OnAssistantMessage?.Invoke(text);
                        if (text.StartsWith(previousText, StringComparison.Ordinal))
                        {
                            var delta = text.Substring(previousText.Length);
                            if (!string.IsNullOrEmpty(delta))
                                OnAssistantDelta?.Invoke(delta);
                        }
                        else
                        {
                            OnAssistantDelta?.Invoke(text);
                        }
                    }
                    break;
                }
                case "tool_start":
                {
                    var name = ExtractJsonString(json, "name") ?? "tool";
                    CompleteRunningRpcMessages(false, false);
                    _assistantDeltaText = "";
                    _runtimeIsStreaming = true;
                    _runtimeStatusText = "Using " + name;
                    AddDevRpcMessage("tool", name, "", "", true, false);
                    OnToolStart?.Invoke(name);
                    break;
                }
                case "tool_end":
                {
                    var name = ExtractJsonString(json, "name") ?? "tool";
                    var error = ExtractJsonString(json, "error");
                    var result = ExtractJsonString(json, "result");
                    CompleteLatestToolMessage(name, error ?? result ?? "", !string.IsNullOrEmpty(error));
                    OnToolEnd?.Invoke(name, error ?? result ?? "");
                    break;
                }
                case "turn_end":
                {
                    var stopReason = ExtractJsonString(json, "stopReason") ?? "";
                    CompleteRunningRpcMessages(stopReason == "error");
                    _assistantDeltaText = "";
                    _runtimeIsStreaming = stopReason == "toolUse";
                    _runtimeStatusText = _runtimeIsStreaming ? "Using tool" : stopReason == "error" ? "Error" : "Idle";
                    OnTurnEndDetailed?.Invoke(stopReason);
                    if (stopReason != "toolUse") OnTurnEnd?.Invoke();
                    break;
                }
                case "state_event":
                {
                    var name = ExtractJsonString(json, "name") ?? "";
                    var detail = ExtractJsonString(json, "detail") ?? name;
                    if (name == "message_start" && detail == "role=assistant")
                        _assistantDeltaText = "";
                    if (name == "agent_start" || name == "turn_start") _runtimeIsStreaming = true;
                    if (!string.IsNullOrWhiteSpace(detail))
                        _runtimeStatusText = detail;
                    break;
                }
                case "skills_list":
                {
                    // Startup/reload skill inventory. Previously this event
                    // had no consumer in player builds; log it once per list
                    // so what the agent actually sees is observable in the
                    // player log (bake drift, hot installs, reloads).
                    var payload = JsonUtility.FromJson<SkillsListPayload>(json ?? "{}") ?? new SkillsListPayload();
                    var names = new List<string>();
                    if (payload.skills != null)
                        foreach (var entry in payload.skills)
                            names.Add(entry == null ? "" : entry.name);
                    PieDiagnostics.Info($"[PieRunner] skills available ({names.Count}): {string.Join(", ", names)}");
                    // Cache the raw list: the JS runtime produced it by
                    // scanning the skill layers through puerts-fs, which makes
                    // it the observable end of the player smoke's JS file
                    // bridge proof (see PiePlayerSmoke).
                    LastSkillsListJson = json ?? "";
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
                case "session_binding_result":
                    OnSessionBindingResult?.Invoke(JsonUtility.FromJson<PieSessionBindingResult>(json ?? "{}"));
                    break;
                case "session_sync":
                {
                    var snapshot = JsonUtility.FromJson<PieSessionSnapshot>(json ?? "{}");
                    if (snapshot != null)
                    {
                        if (snapshot.messages == null)
                            snapshot.messages = Array.Empty<PieSessionMessage>();
                        if (snapshot.timelineItems == null)
                            snapshot.timelineItems = Array.Empty<PieTimelineItem>();
                        if (!string.IsNullOrEmpty(ActiveSessionId)
                            && !string.Equals(ActiveSessionId, snapshot.id ?? "", StringComparison.Ordinal))
                            ResetRuntimeRpcHistory();
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
                        if (!_runtimeIsStreaming)
                        {
                            CompleteRunningRpcMessages(status.turnState == "failed" && status.lastStopReason != "aborted");
                            _assistantDeltaText = "";
                        }
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
                        if (!string.IsNullOrEmpty(error.sessionId) && !string.IsNullOrEmpty(ActiveSessionId)
                            && error.sessionId != ActiveSessionId) break;
                        if (error.nonTerminal)
                        {
                            // Keep A busy on overlap; restore idle when a terminal
                            // callback attempted B during session-switch retirement.
                            _restoreRejectedRuntimeSend?.Invoke(error);
                            OnRuntimeError?.Invoke(error);
                            break;
                        }
                        _lastStructuredErrorMessage = error.message ?? "";
                        CompleteRunningRpcMessages(true);
                        _assistantDeltaText = "";
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
                        if (!string.IsNullOrEmpty(result.sessionId) && !string.IsNullOrEmpty(ActiveSessionId)
                            && result.sessionId != ActiveSessionId) break;
                        CompleteRunningRpcMessages(result.outcome == "failed");
                        _assistantDeltaText = "";
                        _runtimeIsStreaming = false;
                        _runtimeStatusText = string.Equals(result.outcome, "completed", StringComparison.Ordinal)
                            ? "Idle"
                            : result.outcome ?? "Idle";
                        OnTurnFinished?.Invoke(result);
                    }
                    break;
                }
                case "error":
                {
                    var message = ExtractJsonString(json, "message") ?? json;
                    CompleteRunningRpcMessages(true);
                    _assistantDeltaText = "";
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

        private void CompleteRunningRpcMessages(bool isError, bool includeTools = true)
        {
            foreach (var item in _devRpcMessages)
            {
                if (!item.isRunning || (item.role != "assistant" && (!includeTools || item.role != "tool"))) continue;
                item.isRunning = false;
                item.isError |= isError;
            }
        }

        private void ResetRuntimeRpcHistory()
        {
            _devRpcMessages.Clear();
            _lastAssistantText = "";
            _assistantDeltaText = "";
            _lastStructuredErrorMessage = "";
            _runtimeIsStreaming = false;
            _runtimeStatusText = "Idle";
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
                if (last != null && last.isRunning && string.Equals(last.role, "assistant", StringComparison.Ordinal))
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
            public string requestId;
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
