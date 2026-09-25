using System;
using UnityEditor;
using UnityEngine;

namespace Pie.Editor
{
    internal static class PieChatSessionRecovery
    {
        private const string Prefix = "Pie_EditorRecovery_";
        public const string ActiveSessionKey = Prefix + "ActiveSessionId";
        public const string WasStreamingKey = Prefix + "WasStreaming";
        public const string LastUserKey = Prefix + "LastUserMessage";
        public const string DraftKey = Prefix + "DraftInput";
        public const string ProviderKey = Prefix + "Provider";
        public const string ModelKey = Prefix + "Model";
        public const string BaseUrlKey = Prefix + "BaseUrl";
        public const string SavedAtKey = Prefix + "SavedAtTicksUtc";
        public const string LastKnownActiveSessionKey = Prefix + "LastKnownActiveSession";
        public const string AutoResendSessionKey = Prefix + "AutoResendSessionId";
        public const double RestoreDelaySeconds = 0.5d;

        [Serializable]
        internal sealed class Snapshot
        {
            public string activeSessionId;
            public bool wasStreaming;
            public string lastUserMessageText;
            public string draftInputText;
            public string provider;
            public string model;
            public string baseUrl;
            public long savedAtTicksUtc;
        }

        [Serializable]
        private sealed class DevRecoveryStatePayload
        {
            public bool pendingAfterReload;
            public string activeSessionId;
            public string lastKnownActiveSessionId;
            public bool wasStreaming;
            public string lastUserMessageText;
            public string draftInputText;
            public string savedAt;
            public bool hasSnapshot;
            public bool restoreScheduled;
            public double restoreNotBeforeAt;
        }

        [Serializable]
        private sealed class ResumeSessionPayload
        {
            public string sessionId;
            public bool recoverInterrupted;
            public string lastUserText;
        }

        public static bool HasSnapshot()
        {
            return !string.IsNullOrEmpty(SessionState.GetString(SavedAtKey, ""));
        }

        public static void Save(
            string activeSessionId,
            bool wasStreaming,
            string lastUserMessageText,
            string draftInputText,
            string provider,
            string model,
            string baseUrl,
            long savedAtTicksUtc)
        {
            SessionState.SetString(ActiveSessionKey, activeSessionId ?? "");
            SessionState.SetBool(WasStreamingKey, wasStreaming);
            SessionState.SetString(LastUserKey, lastUserMessageText ?? "");
            SessionState.SetString(DraftKey, draftInputText ?? "");
            SessionState.SetString(ProviderKey, provider ?? "");
            SessionState.SetString(ModelKey, model ?? "");
            SessionState.SetString(BaseUrlKey, baseUrl ?? "");
            SessionState.SetString(SavedAtKey, savedAtTicksUtc.ToString());
        }

        public static Snapshot Read()
        {
            var savedAtRaw = SessionState.GetString(SavedAtKey, "");
            long savedAtTicksUtc = 0;
            if (!string.IsNullOrEmpty(savedAtRaw))
                long.TryParse(savedAtRaw, out savedAtTicksUtc);

            return new Snapshot
            {
                activeSessionId = SessionState.GetString(ActiveSessionKey, ""),
                wasStreaming = SessionState.GetBool(WasStreamingKey, false),
                lastUserMessageText = SessionState.GetString(LastUserKey, ""),
                draftInputText = SessionState.GetString(DraftKey, ""),
                provider = SessionState.GetString(ProviderKey, ""),
                model = SessionState.GetString(ModelKey, ""),
                baseUrl = SessionState.GetString(BaseUrlKey, ""),
                savedAtTicksUtc = savedAtTicksUtc,
            };
        }

        public static string GetAutoResendSessionId()
        {
            return SessionState.GetString(AutoResendSessionKey, "");
        }

        public static void SetAutoResendSessionId(string sessionId)
        {
            SessionState.SetString(AutoResendSessionKey, sessionId ?? "");
        }

        public static void Clear()
        {
            SessionState.EraseString(ActiveSessionKey);
            SessionState.EraseBool(WasStreamingKey);
            SessionState.EraseString(LastUserKey);
            SessionState.EraseString(DraftKey);
            SessionState.EraseString(ProviderKey);
            SessionState.EraseString(ModelKey);
            SessionState.EraseString(BaseUrlKey);
            SessionState.EraseString(SavedAtKey);
        }

        public static void SetLastKnownActiveSession(string activeSessionId)
        {
            SessionState.SetString(LastKnownActiveSessionKey, activeSessionId ?? "");
        }

        public static string GetLastKnownActiveSession()
        {
            return SessionState.GetString(LastKnownActiveSessionKey, "");
        }

        public static string BuildRecoveryStateJson(bool restoreScheduled, double restoreNotBeforeAt)
        {
            var hasSnapshot = HasSnapshot();
            return JsonUtility.ToJson(new DevRecoveryStatePayload
            {
                pendingAfterReload = hasSnapshot,
                activeSessionId = SessionState.GetString(ActiveSessionKey, ""),
                lastKnownActiveSessionId = SessionState.GetString(LastKnownActiveSessionKey, ""),
                wasStreaming = SessionState.GetBool(WasStreamingKey, false),
                lastUserMessageText = SessionState.GetString(LastUserKey, ""),
                draftInputText = SessionState.GetString(DraftKey, ""),
                savedAt = SessionState.GetString(SavedAtKey, ""),
                hasSnapshot = hasSnapshot,
                restoreScheduled = restoreScheduled,
                restoreNotBeforeAt = restoreNotBeforeAt,
            });
        }

        public static string BuildResumeArgsJson(Snapshot snapshot)
        {
            return JsonUtility.ToJson(new ResumeSessionPayload
            {
                sessionId = snapshot?.activeSessionId ?? "",
                recoverInterrupted = snapshot?.wasStreaming == true,
                lastUserText = snapshot?.lastUserMessageText ?? "",
            });
        }
    }
}
