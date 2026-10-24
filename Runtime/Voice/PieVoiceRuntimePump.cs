using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pie
{
    internal sealed class PieVoiceRuntimePump : MonoBehaviour
    {
        private static PieVoiceRuntimePump _instance;
        private static readonly ConcurrentQueue<Action> MainThreadQueue = new ConcurrentQueue<Action>();
        private static readonly List<PieVoiceRecording> ActiveRecordings = new List<PieVoiceRecording>();
        private static int _mainThreadId = -1;
        private static bool _quitting;
        private static readonly object DispatchGate = new object();
        private static bool _acceptingRequests;

        // The pump must exist before ANY worker thread can ask for it:
        // RunOnMainThread previously created the GameObject lazily inside
        // Ensure(), which throws when the first touch happens on a worker
        // thread (dogfood: PieSafeHttpClient-driven asset tasks called from
        // Task.Run hit "Internal_CreateGameObject can only be called from
        // the main thread"). Both hooks below run on the main thread in
        // every mode, capture the main thread id, and create the pump eagerly.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeEagerCreate()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            // Statics may survive play sessions when domain reload is
            // disabled; a fresh run means the pump is wanted again.
            _quitting = false;
            Ensure();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorEagerCreate()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _quitting = false;
            Ensure();
            // Play-mode transitions destroy the pump object and set _quitting;
            // with domain reload disabled none of the static state resets on
            // its own. Restore edit-mode pumping explicitly on the way back.
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(UnityEditor.PlayModeStateChange change)
        {
            if (change != UnityEditor.PlayModeStateChange.EnteredEditMode)
                return;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _quitting = false;
            Ensure(); // main thread: recreates the pump + editor update hook
        }
#endif

#if UNITY_EDITOR
        private static bool _editorUpdateSubscribed;
#endif

        internal static bool IsMainThread
        {
            get { return _mainThreadId >= 0 && Thread.CurrentThread.ManagedThreadId == _mainThreadId; }
        }

        internal static PieVoiceRuntimePump Instance
        {
            get
            {
                Ensure();
                return _instance;
            }
        }

        internal static void Ensure()
        {
            // Unity fake-null: a destroyed pump (play-mode exit destroys
            // DontDestroyOnLoad objects) still compares != null as a managed
            // reference, so use the lifetime-aware implicit bool.
            if (_quitting || _instance)
                return;
            // Never create the GameObject off the main thread. The eager hooks
            // set _mainThreadId on the main thread in every mode; an off-main
            // caller simply enqueues and the pump drains once created.
            if (!IsMainThread)
                return;

            var go = new GameObject("PieVoiceRuntimePump");
            go.hideFlags = HideFlags.HideAndDontSave;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(go);
            }
#if UNITY_EDITOR
            else
            {
                SubscribeEditorUpdate();
            }
#endif
            _instance = go.AddComponent<PieVoiceRuntimePump>();
        }

        internal static void RunOnMainThread(Action action)
        {
            if (action == null)
                return;

            Ensure();
            if (IsMainThread)
            {
                action();
                return;
            }

            MainThreadQueue.Enqueue(action);
        }

        // Used by resource-owning callers that must not enqueue after teardown.
        // Enqueue and closing admission are atomic; accepted HTTP work is also
        // tracked by PieSafeHttpClient before entering this gate.
        internal static bool TryRunOnMainThread(Action action)
        {
            Ensure();
            lock (DispatchGate)
            {
                if (!_acceptingRequests) return false;
                if (!IsMainThread)
                {
                    MainThreadQueue.Enqueue(action);
                    return true;
                }
            }
            action();
            return true;
        }

        internal static void PostToMainThread(Action action)
        {
            if (action == null)
                return;

            Ensure();
            MainThreadQueue.Enqueue(action);
        }

        internal static void Track(PieVoiceRecording recording)
        {
            if (recording == null)
                return;

            RunOnMainThread(() =>
            {
                if (!ActiveRecordings.Contains(recording))
                    ActiveRecordings.Add(recording);
            });
        }

        internal static void Untrack(PieVoiceRecording recording)
        {
            if (recording == null)
                return;

            RunOnMainThread(() => ActiveRecordings.Remove(recording));
        }

        internal static void RequestMicrophonePermission(Action<bool> callback)
        {
            Ensure();
            Instance.StartCoroutine(RequestMicrophonePermissionRoutine(callback));
        }

        private static IEnumerator RequestMicrophonePermissionRoutine(Action<bool> callback)
        {
            if (Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                callback?.Invoke(true);
                yield break;
            }

            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
            callback?.Invoke(Application.HasUserAuthorization(UserAuthorization.Microphone));
        }

        private void Awake()
        {
            _instance = this;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            lock (DispatchGate) { _acceptingRequests = true; }
        }

        private void Update()
        {
            PumpOnce();
        }

        private static void PumpOnce()
        {
            while (MainThreadQueue.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    PieDiagnostics.Warning($"[PieVoice] Main thread callback failed: {ex.Message}");
                }
            }

            if (ActiveRecordings.Count == 0)
                return;

            var snapshot = ActiveRecordings.ToArray();
            foreach (var recording in snapshot)
            {
                try
                {
                    recording.Tick();
                }
                catch (Exception ex)
                {
                    PieDiagnostics.Warning($"[PieVoice] Recording tick failed: {ex.Message}");
                }
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                lock (DispatchGate) { _acceptingRequests = false; }
                _instance = null;
            }
            // Abort + settle in-flight requests so no caller stays pending.
            try { PieSafeHttpClient.OnPumpTeardown(); } catch { }
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnsubscribeEditorUpdate();
#endif
        }

        private void OnApplicationQuit()
        {
            _quitting = true;
            lock (DispatchGate) { _acceptingRequests = false; }
            PieSafeHttpClient.OnPumpTeardown();
        }

#if UNITY_EDITOR
        private static void SubscribeEditorUpdate()
        {
            if (_editorUpdateSubscribed)
                return;

            EditorApplication.update += EditorUpdate;
            _editorUpdateSubscribed = true;
        }

        private static void UnsubscribeEditorUpdate()
        {
            if (!_editorUpdateSubscribed)
                return;

            EditorApplication.update -= EditorUpdate;
            _editorUpdateSubscribed = false;
        }

        private static void EditorUpdate()
        {
            if (Application.isPlaying)
            {
                UnsubscribeEditorUpdate();
                return;
            }

            PumpOnce();
        }
#endif
    }
}
