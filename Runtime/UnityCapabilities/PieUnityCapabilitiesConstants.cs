#if PIE_UNITY_SPLIT_SOURCES
using System;
using System.IO;

namespace Pie
{
    public static class PieUnityCapabilitiesConstants
    {
        public const string Version = "0.1.25";
        public const int DefaultPort = 8091;
        public const int MaxPort = 8100;
        public const int RegistryTtlSeconds = 120;
        public const string ServiceName = "pie-unity";
        public const string ManifestSchemaVersion = "2";
        public const string SkillProtocolVersion = "pie-unity-rpc/2";

        public static string RegistryDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pie-unity");

        public static string InstancesDirectory =>
            Path.Combine(RegistryDirectory, "instances");

        public static string SharedLogsDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pie", "logs");

        public static string RuntimeLogFilePath =>
            Path.Combine(SharedLogsDirectory, "pie-unity.log");
    }
}

#endif
