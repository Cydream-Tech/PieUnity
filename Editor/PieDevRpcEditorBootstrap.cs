#if PIE_UNITY_SPLIT_SOURCES
using UnityEditor;
using UnityEngine;

namespace Pie.Editor
{
    [InitializeOnLoad]
    internal static class PieDevRpcEditorBootstrap
    {
        private static bool _registeredUpdate;
        private static bool _wasPlaying;
        [System.Serializable]
        private class ThreadInfoPayload
        {
            public int mainThreadId;
            public int currentThreadId;
        }

        static PieDevRpcEditorBootstrap()
        {
            if (!_registeredUpdate)
            {
                EditorApplication.update += Tick;
                _registeredUpdate = true;
            }
            EditorApplication.delayCall += PieDevRpcDispatcher.InitializeMainThread;
            AssemblyReloadEvents.beforeAssemblyReload -= HandleBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += HandleBeforeAssemblyReload;
            PieDevRpcServer.Start();
            PieHostBridge.Register("pie.dev_rpc", PieDevRpc.InvokeHostCall);
            PieHostBridge.Register("pie.host_bridge", PieHostBridge.InvokeHostBridge);
            PieUnityCapabilitiesBootstrap.InitializeEditor();
            PieDevRpc.Register("rpc.thread_info", _ => JsonUtility.ToJson(new ThreadInfoPayload
            {
                mainThreadId = PieDevRpcDispatcher.MainThreadId,
                currentThreadId = PieDevRpcDispatcher.CurrentThreadId,
            }));

            PieChatWindow.EnsureDevRpcReady();
        }

        private static void HandleBeforeAssemblyReload()
        {
            PieDevRpcServer.BeginDomainReloadShutdown();
        }

        private static void Tick()
        {
            PieDevRpcDispatcher.Tick();
            var playing = EditorApplication.isPlayingOrWillChangePlaymode;
            if (_wasPlaying && !playing)
            {
                PieUnityCapabilitiesBootstrap.InitializeEditor();
                PieChatWindow.EnsureDevRpcReady();
                PieChatWindow.DevRpcReconnect();
            }
            _wasPlaying = playing;
            PieUnityCapabilitiesBootstrap.HeartbeatEditor();
        }
    }
}

#endif
