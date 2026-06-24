#if PIE_UNITY_SPLIT_SOURCES
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Pie
{
    public static class PieUnityInstanceRegistry
    {
        private static readonly object FileSyncRoot = new object();
        private const int IoRetryCount = 5;
        private const int IoRetryDelayMs = 25;

        public static void Register(string instanceId, string projectPath, string projectName, string mode, int port, string token, string productName = "", string applicationIdentifier = "")
        {
            try
            {
                lock (FileSyncRoot)
                {
                    var pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    WriteInstanceFile(new PieUnityInstance
                    {
                        instanceId = instanceId,
                        projectPath = projectPath,
                        projectName = projectName,
                        displayName = string.IsNullOrWhiteSpace(productName) ? projectName : productName,
                        productName = productName,
                        applicationIdentifier = applicationIdentifier,
                        mode = mode,
                        port = port,
                        token = token,
                        pid = pid,
                        lastSeenUnix = now,
                        version = PieUnityCapabilitiesConstants.Version,
                        packageVersion = PieUnityCapabilitiesConstants.Version,
                    });
                }
            }
            catch (Exception ex)
            {
                PieDiagnostics.Warning($"[PieUnityInstanceRegistry] Register failed: {ex.Message}");
            }
        }

        public static void Unregister(string instanceId)
        {
            try
            {
                lock (FileSyncRoot)
                {
                    TryDeleteInstanceFile(instanceId);
                }
            }
            catch (Exception ex)
            {
                PieDiagnostics.Verbose($"[PieUnityInstanceRegistry] Unregister cleanup skipped: {ex.Message}");
            }
        }

        public static PieUnityInstance[] ReadAll()
        {
            try
            {
                lock (FileSyncRoot)
                {
                    return SelectDiscoverableOwners(ReadAllInternal()).ToArray();
                }
            }
            catch
            {
                return new PieUnityInstance[0];
            }
        }

        public static PieUnityInstance[] ReadAllActive()
        {
            try
            {
                lock (FileSyncRoot)
                {
                    return ReadAllInternal().ToArray();
                }
            }
            catch
            {
                return new PieUnityInstance[0];
            }
        }

        public static PieUnityDiscoverableSnapshot GetCurrentProcessSnapshot(string projectPath)
        {
            try
            {
                lock (FileSyncRoot)
                {
                    var pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                    var normalizedProjectPath = NormalizeProjectPath(projectPath);
                    var active = ReadAllInternal();
                    var local = active.FindAll(item =>
                        item != null
                        && item.pid == pid
                        && (string.IsNullOrWhiteSpace(normalizedProjectPath) || string.Equals(NormalizeProjectPath(item.projectPath), normalizedProjectPath, StringComparison.OrdinalIgnoreCase)));

                    var owner = SelectOwner(local);
                    var runtimeActive = local.Exists(item => string.Equals(item.mode, "runtime", StringComparison.OrdinalIgnoreCase));
                    var editorActive = local.Exists(item => string.Equals(item.mode, "editor", StringComparison.OrdinalIgnoreCase));
                    return new PieUnityDiscoverableSnapshot
                    {
                        discoverableOwner = owner,
                        runtimeActive = runtimeActive,
                        editorActive = editorActive,
                        editorSuppressedByRuntime = runtimeActive && editorActive && owner != null && string.Equals(owner.mode, "runtime", StringComparison.OrdinalIgnoreCase),
                    };
                }
            }
            catch
            {
                return new PieUnityDiscoverableSnapshot();
            }
        }

        private static List<PieUnityInstance> ReadAllInternal()
        {
            var directoryPath = PieUnityCapabilitiesConstants.InstancesDirectory;
            if (!Directory.Exists(directoryPath))
                return new List<PieUnityInstance>();

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var next = new List<PieUnityInstance>();
            var filePaths = Directory.GetFiles(directoryPath, "*.json");
            for (var i = 0; i < filePaths.Length; i++)
            {
                var item = TryReadInstanceFile(filePaths[i]);
                if (!IsValidIdentity(item))
                {
                    TryDeleteStaleFile(filePaths[i]);
                    continue;
                }

                if (IsExpired(item, now))
                {
                    TryDeleteStaleFile(filePaths[i]);
                    continue;
                }
                next.Add(item);
            }

            return next;
        }

        private static List<PieUnityInstance> SelectDiscoverableOwners(List<PieUnityInstance> active)
        {
            var next = new List<PieUnityInstance>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < active.Count; i++)
            {
                var item = active[i];
                var key = NormalizeProjectPath(item != null ? item.projectPath : "");
                if (string.IsNullOrWhiteSpace(key))
                    key = item != null ? (item.instanceId ?? "") : "";
                if (!seen.Add(key))
                    continue;

                var matches = active.FindAll(candidate =>
                    string.Equals(NormalizeProjectPath(candidate != null ? candidate.projectPath : ""), key, StringComparison.OrdinalIgnoreCase));
                var owner = SelectOwner(matches);
                if (owner != null)
                    next.Add(owner);
            }

            return next;
        }

        private static PieUnityInstance SelectOwner(List<PieUnityInstance> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return null;

            PieUnityInstance best = null;
            for (var i = 0; i < candidates.Count; i++)
            {
                var item = candidates[i];
                if (item == null)
                    continue;
                if (best == null)
                {
                    best = item;
                    continue;
                }

                var itemIsRuntime = string.Equals(item.mode, "runtime", StringComparison.OrdinalIgnoreCase);
                var bestIsRuntime = string.Equals(best.mode, "runtime", StringComparison.OrdinalIgnoreCase);
                if (itemIsRuntime != bestIsRuntime)
                {
                    if (itemIsRuntime)
                        best = item;
                    continue;
                }

                if (item.lastSeenUnix > best.lastSeenUnix)
                {
                    best = item;
                    continue;
                }

                if (item.lastSeenUnix == best.lastSeenUnix
                    && string.Compare(item.instanceId ?? "", best.instanceId ?? "", StringComparison.OrdinalIgnoreCase) < 0)
                    best = item;
            }

            return best;
        }

        private static string NormalizeProjectPath(string projectPath)
        {
            return (projectPath ?? "").Replace("\\", "/").Trim();
        }

        private static void WriteInstanceFile(PieUnityInstance instance)
        {
            if (!IsValidIdentity(instance))
                throw new InvalidOperationException("Cannot register a Unity instance without a valid identity.");

            Directory.CreateDirectory(PieUnityCapabilitiesConstants.InstancesDirectory);
            var targetPath = BuildInstanceFilePath(instance);
            var json = JsonUtility.ToJson(instance, true);
            var tempPath = targetPath + "." + System.Diagnostics.Process.GetCurrentProcess().Id + "." + Guid.NewGuid().ToString("N") + ".tmp";

            Exception lastError = null;
            for (var attempt = 0; attempt < IoRetryCount; attempt++)
            {
                try
                {
                    using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(true)))
                    {
                        writer.Write(json);
                        writer.Flush();
                        stream.Flush(true);
                    }

                    if (File.Exists(targetPath))
                        File.Replace(tempPath, targetPath, null, true);
                    else
                        File.Move(tempPath, targetPath);

                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    try
                    {
                        if (File.Exists(tempPath))
                            File.Delete(tempPath);
                    }
                    catch
                    {
                        // Ignore temp cleanup failures.
                    }

                    System.Threading.Thread.Sleep(IoRetryDelayMs * (attempt + 1));
                }
            }

            throw lastError ?? new IOException("Failed to write Unity instance registry.");
        }

        private static PieUnityInstance TryReadInstanceFile(string filePath)
        {
            for (var attempt = 0; attempt < IoRetryCount; attempt++)
            {
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                    {
                        var json = reader.ReadToEnd();
                        if (string.IsNullOrWhiteSpace(json))
                            return null;
                        return JsonUtility.FromJson<PieUnityInstance>(json);
                    }
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(IoRetryDelayMs * (attempt + 1));
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static string BuildInstanceFilePath(PieUnityInstance instance)
        {
            var instanceId = instance != null ? (instance.instanceId ?? "") : "";
            var pid = instance != null ? instance.pid : 0;
            var port = instance != null ? instance.port : 0;
            return Path.Combine(PieUnityCapabilitiesConstants.InstancesDirectory, instanceId + "_" + pid + "_" + port + ".json");
        }

        private static bool IsExpired(PieUnityInstance item, long now)
        {
            if (item == null)
                return true;

            if (now - item.lastSeenUnix > PieUnityCapabilitiesConstants.RegistryTtlSeconds)
                return true;

            if (!IsProcessAlive(item.pid))
                return true;

            return false;
        }

        private static bool IsValidIdentity(PieUnityInstance item)
        {
            if (item == null)
                return false;
            if (string.IsNullOrWhiteSpace(item.instanceId))
                return false;
            if (string.IsNullOrWhiteSpace(item.projectPath))
                return false;
            if (string.IsNullOrWhiteSpace(item.mode))
                return false;
            if (item.port <= 0)
                return false;
            if (item.pid <= 0)
                return false;
            if (item.lastSeenUnix <= 0)
                return false;
            return true;
        }

        private static void TryDeleteInstanceFile(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                return;
            if (!Directory.Exists(PieUnityCapabilitiesConstants.InstancesDirectory))
                return;
            var pattern = instanceId + "*.json";
            var filePaths = Directory.GetFiles(PieUnityCapabilitiesConstants.InstancesDirectory, pattern);
            for (var i = 0; i < filePaths.Length; i++)
                TryDeleteStaleFile(filePaths[i]);
        }

        private static void TryDeleteStaleFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            for (var attempt = 0; attempt < IoRetryCount; attempt++)
            {
                try
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                    return;
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(IoRetryDelayMs * (attempt + 1));
                }
                catch (UnauthorizedAccessException)
                {
                    System.Threading.Thread.Sleep(IoRetryDelayMs * (attempt + 1));
                }
                catch
                {
                    return;
                }
            }
        }

        private static bool IsProcessAlive(int pid)
        {
            try
            {
                var process = System.Diagnostics.Process.GetProcessById(pid);
                return process != null && !process.HasExited;
            }
            catch
            {
                return false;
            }
        }
    }
}

#endif
