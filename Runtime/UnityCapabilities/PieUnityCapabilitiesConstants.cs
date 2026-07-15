#if PIE_UNITY_SPLIT_SOURCES
using System;
using System.IO;

namespace Pie
{
    public static class PieUnityCapabilitiesConstants
    {
        public const string Version = "0.1.29";
        public const int DefaultPort = 8091;
        public const int MaxPort = 8100;
        public const int RegistryTtlSeconds = 120;
        public const string ServiceName = "pie-unity";
        public const string ManifestSchemaVersion = "2";
        public const string SkillProtocolVersion = "pie-unity-rpc/2";

        private static string UserStateRoot
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                // Environment.SpecialFolder.UserProfile resolves to a read-only
                // location in Android Players. Keep runtime discovery/log state
                // inside Unity's writable app-owned storage instead.
                return UnityEngine.Application.persistentDataPath;
#else
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
#endif
            }
        }

        public static string RegistryDirectory =>
            Path.Combine(UserStateRoot, ".pie-unity");

        public static string InstancesDirectory =>
            Path.Combine(RegistryDirectory, "instances");

        public static string SharedLogsDirectory =>
            Path.Combine(UserStateRoot, ".pie", "logs");

        public static string RuntimeLogFilePath =>
            Path.Combine(SharedLogsDirectory, "pie-unity.log");
    }
}

#endif
