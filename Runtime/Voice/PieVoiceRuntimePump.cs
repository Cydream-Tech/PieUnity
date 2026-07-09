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
            if (_instance != null || _quitting)
                return;

            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
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
                _instance = null;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnsubscribeEditorUpdate();
#endif
        }

        private void OnApplicationQuit()
        {
            _quitting = true;
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
