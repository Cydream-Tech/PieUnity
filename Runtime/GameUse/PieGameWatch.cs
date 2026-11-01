using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pie.GameUse
{
    /// <summary>
    /// Side-channel observability for game-use episodes: what the game said
    /// (log errors/warnings via Application.logMessageReceived) and how the
    /// frame budget held (per-frame ms) while the loop played. Detectors in
    /// the engine read this each tick and put findings in the episode report;
    /// the QA story ("found 2 errors while playing") is the point.
    ///
    /// Player-first: logMessageReceived and Time.unscaledDeltaTime are the
    /// Player ∩ IL2CPP ∩ no-editor surface; the driver boots in Awake and
    /// survives scene loads (DontDestroyOnLoad), rebuilt idempotently like
    /// the input-hands driver.
    /// </summary>
    public static class PieGameWatch
    {
        private const int MaxEntries = 256;
        private static readonly Queue<Entry> LogEntries = new Queue<Entry>();
        private static int _logCursor;
        private static readonly float[] FrameMs = new float[600];
        private static int _frameCursor;
        private static long _seenErrorCount, _seenWarningCount;
        private static PieGameWatchDriver _driver;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureDriver()
        {
            // Unity destroyed-object null: a driver killed by scene teardown
            // or a script-task cleanup must be rebuilt on next use.
            if (_driver != null) return;
            var go = new GameObject("PieGameWatch");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _driver = go.AddComponent<PieGameWatchDriver>();
        }

        [UnityEngine.Scripting.Preserve]
        public static string Collect(int sinceLogCursor)
        {
            EnsureDriver();
            lock (LogEntries)
            {
                var payload = new Payload
                {
                    logCursor = _logCursor,
                    errorCount = _seenErrorCount,
                    warningCount = _seenWarningCount,
                    newEntries = new List<string>(),
                };
                // Entries carry a monotonically increasing cursor: the engine
                // sends its last-seen cursor and receives only newer entries.
                foreach (var entry in LogEntries)
                {
                    if (entry.Cursor > sinceLogCursor) payload.newEntries.Add(entry.Text);
                }
                // Frame stats over the whole buffer (the last ~600 frames).
                var worst = 0f; var over33 = 0; var over50 = 0; var sum = 0f; var n = 0;
                for (var i = 0; i < FrameMs.Length; i++)
                {
                    var ms = FrameMs[i];
                    if (ms <= 0f) continue;
                    n++;
                    sum += ms;
                    if (ms > worst) worst = ms;
                    if (ms > 33.4f) over33++;
                    if (ms > 50f) over50++;
                }
                payload.frameCount = n;
                payload.avgFrameMs = n > 0 ? sum / n : 0f;
                payload.worstFrameMs = worst;
                payload.over33 = over33;
                payload.over50 = over50;
                return JsonUtility.ToJson(payload);
            }
        }

        [UnityEngine.Scripting.Preserve]
        public static string Reset()
        {
            EnsureDriver();
            lock (LogEntries)
            {
                LogEntries.Clear();
                _logCursor = 0;
                _seenErrorCount = 0;
                _seenWarningCount = 0;
                for (var i = 0; i < FrameMs.Length; i++) FrameMs[i] = 0f;
            }
            return "{\"ok\":true}";
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            lock (LogEntries)
            {
                // The loop's own chatter (script-task logs route through Pie
                // diagnostics) is not a game finding — excluded BEFORE the
                // counters, so internal Pie warnings never inflate totals.
                if (condition != null && condition.StartsWith("[Pie ", StringComparison.Ordinal)) return;
                var isError = type == LogType.Error || type == LogType.Exception || type == LogType.Assert;
                var isWarning = type == LogType.Warning;
                if (isError) _seenErrorCount++;
                else if (isWarning) _seenWarningCount++;
                _logCursor++;
                LogEntries.Enqueue(new Entry { Cursor = _logCursor, Text = type + ": " + condition });
                while (LogEntries.Count > MaxEntries) LogEntries.Dequeue();
            }
        }

        private sealed class Entry
        {
            public int Cursor;
            public string Text;
        }

        [Serializable]
        private sealed class Payload
        {
            public int logCursor;
            public long errorCount;
            public long warningCount;
            public List<string> newEntries;
            public int frameCount;
            public float avgFrameMs;
            public float worstFrameMs;
            public int over33;
            public int over50;
        }

        [DefaultExecutionOrder(-999)]
        private sealed class PieGameWatchDriver : MonoBehaviour
        {
            private void Awake()
            {
                Application.logMessageReceived += OnLog;
            }

            private void OnDestroy()
            {
                Application.logMessageReceived -= OnLog;
            }

            private void Update()
            {
                FrameMs[_frameCursor] = Mathf.Max(0f, Time.unscaledDeltaTime * 1000f);
                _frameCursor = (_frameCursor + 1) % FrameMs.Length;
            }
        }
    }
}
