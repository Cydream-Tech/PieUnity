using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pie
{
    public static class PieProjectPaths
    {
        [Serializable]
        public sealed class FileToolRootJson
        {
            public string name;
            public string description;
            public string path;
            public string[] defaultPrefixes;
        }

        [Serializable]
        public sealed class FileToolRootsPayload
        {
            public string defaultRoot;
            public FileToolRootJson[] roots;
        }

        private sealed class FileToolRootCandidate
        {
            public string Name;
            public string Description;
            public string Path;
            public bool AvailableInEditor;
            public bool AvailableInRuntime;
            public List<string> DefaultPrefixes = new List<string>();
        }

        private const string DefaultSettingsAssetPath = "Assets/Pie/PieSettings.asset";
        private const string DefaultExtensionRelativePath = "Assets/Pie/Extensions";
        private const string DefaultSkillRelativePath = "Assets/Pie/Skills";
        private const string DefaultAgentsFileRelativePath = "AGENTS.md";
        private const string DefaultAgentsSkillRelativePath = ".agents/skills";
        private const string RuntimeStateFolderName = "Pie";

        public static string GetExtensionSearchPathsJson(string projectRoot)
        {
            return BuildJsonArray(GetExtensionSearchPaths(projectRoot));
        }

        public static string GetExtensionSearchPathsJson(string projectRoot, PieSettings settingsOverride)
        {
            return BuildJsonArray(GetExtensionSearchPaths(projectRoot, settingsOverride));
        }

        public static string GetSkillSearchPathsJson(string projectRoot)
        {
            return BuildJsonArray(GetSkillSearchPaths(projectRoot));
        }

        public static string GetSkillSearchPathsJson(string projectRoot, PieSettings settingsOverride)
        {
            return BuildJsonArray(GetSkillSearchPaths(projectRoot, settingsOverride));
        }

        public static string GetFileToolRootsJson(string projectRoot, PieSettings settingsOverride, bool isEditor)
        {
            var payload = BuildFileToolRootsPayload(projectRoot, settingsOverride, isEditor);
            return JsonUtility.ToJson(payload);
        }

        public static string GetPrimaryExtensionDirectory(string projectRoot)
        {
            return GetPrimaryPath(projectRoot, GetExtensionSearchPaths(projectRoot), DefaultExtensionRelativePath);
        }

        public static string GetPrimarySkillDirectory(string projectRoot)
        {
            // Creation target for new skills: configured paths or the Pie default
            // only — the cross-tool .agents/skills layer is a read/discovery path
            // and must not capture newly authored skills.
            return GetPrimaryPath(projectRoot, ResolvePaths(projectRoot, LoadConfiguredPaths(settings => settings.SkillSearchPaths, null), DefaultSkillRelativePath), DefaultSkillRelativePath);
        }

        public static string GetProjectAgentsPath(string projectRoot)
        {
            return NormalizePath(Path.Combine(projectRoot ?? "", DefaultAgentsFileRelativePath));
        }

        public static string GetRuntimeStateRoot()
        {
            return NormalizePath(Path.Combine(Application.persistentDataPath, RuntimeStateFolderName));
        }

        public static string GetSessionsDirectory()
        {
            return NormalizePath(Path.Combine(GetRuntimeStateRoot(), "sessions"));
        }

        public static string GetDefaultSettingsAssetPath()
        {
            return DefaultSettingsAssetPath;
        }

        public static IReadOnlyList<string> GetExtensionSearchPaths(string projectRoot)
        {
            return GetExtensionSearchPaths(projectRoot, null);
        }

        public static IReadOnlyList<string> GetExtensionSearchPaths(string projectRoot, PieSettings settingsOverride)
        {
            return ResolvePaths(projectRoot, LoadConfiguredPaths(settings => settings.ExtensionSearchPaths, settingsOverride), DefaultExtensionRelativePath);
        }

        public static IReadOnlyList<string> GetSkillSearchPaths(string projectRoot)
        {
            return GetSkillSearchPaths(projectRoot, null);
        }

        public static IReadOnlyList<string> GetSkillSearchPaths(string projectRoot, PieSettings settingsOverride)
        {
            var resolved = ResolvePaths(projectRoot, LoadConfiguredPaths(settings => settings.SkillSearchPaths, settingsOverride), DefaultSkillRelativePath);
            // Cross-tool standard layer (".agents/skills"), lower priority than
            // the Pie defaults: the JS loader registers paths in order and later
            // registrations win on name collisions, matching the CLI ordering
            // (builtin < ~/.agents < ~/.pie < ancestor .agents < ./.pie).
            var agentsSkills = ResolveProjectPath(projectRoot, DefaultAgentsSkillRelativePath);
            if (!string.IsNullOrEmpty(agentsSkills) && resolved.FindIndex(p => string.Equals(p, agentsSkills, StringComparison.OrdinalIgnoreCase)) < 0)
                resolved.Insert(0, agentsSkills);
            return resolved;
        }

        private static string GetPrimaryPath(string projectRoot, IReadOnlyList<string> resolvedPaths, string fallbackRelativePath)
        {
            if (resolvedPaths != null && resolvedPaths.Count > 0)
                return resolvedPaths[0];

            return NormalizePath(Path.Combine(projectRoot ?? "", fallbackRelativePath));
        }

        private static List<string> ResolvePaths(string projectRoot, IReadOnlyList<string> configuredPaths, string fallbackRelativePath)
        {
            var resolved = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (configuredPaths != null)
            {
                for (var i = 0; i < configuredPaths.Count; i++)
                {
                    var path = ResolveProjectPath(projectRoot, configuredPaths[i]);
                    if (string.IsNullOrEmpty(path))
                        continue;

                    if (seen.Add(path))
                        resolved.Add(path);
                }
            }

            if (resolved.Count == 0)
            {
                var fallback = ResolveProjectPath(projectRoot, fallbackRelativePath);
                if (!string.IsNullOrEmpty(fallback))
                    resolved.Add(fallback);
            }

            return resolved;
        }

        private static IReadOnlyList<string> LoadConfiguredPaths(Func<PieSettings, IReadOnlyList<string>> selector, PieSettings settingsOverride)
        {
            if (settingsOverride != null)
                return selector(settingsOverride);

#if UNITY_EDITOR
            var settings = LoadSettingsAsset();
            if (settings == null)
                return null;

            return selector(settings);
#else
            return null;
#endif
        }

        private static IReadOnlyList<PieFileToolRootDefinition> LoadConfiguredFileToolRoots(PieSettings settingsOverride)
        {
            if (settingsOverride != null)
                return settingsOverride.FileToolRoots;

#if UNITY_EDITOR
            var settings = LoadSettingsAsset();
            if (settings == null)
                return null;

            return settings.FileToolRoots;
#else
            return null;
#endif
        }

        private static string LoadConfiguredDefaultFileToolRoot(PieSettings settingsOverride, bool isEditor)
        {
            if (settingsOverride != null)
                return isEditor ? settingsOverride.DefaultFileToolRootEditor : settingsOverride.DefaultFileToolRootRuntime;

#if UNITY_EDITOR
            var settings = LoadSettingsAsset();
            if (settings == null)
                return null;

            return isEditor ? settings.DefaultFileToolRootEditor : settings.DefaultFileToolRootRuntime;
#else
            return null;
#endif
        }

        private static FileToolRootsPayload BuildFileToolRootsPayload(string projectRoot, PieSettings settingsOverride, bool isEditor)
        {
            var roots = GetActiveFileToolRoots(projectRoot, settingsOverride, isEditor);
            var payload = new FileToolRootsPayload
            {
                defaultRoot = ResolveDefaultFileToolRootName(roots, settingsOverride, isEditor),
                roots = new FileToolRootJson[roots.Count],
            };

            for (var i = 0; i < roots.Count; i++)
            {
                payload.roots[i] = new FileToolRootJson
                {
                    name = roots[i].Name,
                    description = roots[i].Description,
                    path = roots[i].Path,
                    defaultPrefixes = roots[i].DefaultPrefixes.ToArray(),
                };
            }

            return payload;
        }

        private static List<FileToolRootCandidate> GetActiveFileToolRoots(string projectRoot, PieSettings settingsOverride, bool isEditor)
        {
            var merged = GetBuiltInFileToolRoots(projectRoot);
            var configuredRoots = LoadConfiguredFileToolRoots(settingsOverride);

            if (configuredRoots != null)
            {
                for (var i = 0; i < configuredRoots.Count; i++)
                {
                    var configured = configuredRoots[i];
                    if (configured == null || string.IsNullOrWhiteSpace(configured.Name))
                        continue;

                    var name = configured.Name.Trim();
                    var existing = merged.FindIndex(candidate => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
                    var resolvedPath = ResolveConfiguredFileToolRootPath(projectRoot, configured);

                    if (existing >= 0)
                    {
                        if (!string.IsNullOrWhiteSpace(configured.Description))
                            merged[existing].Description = configured.Description.Trim();

                        if (!string.IsNullOrWhiteSpace(resolvedPath))
                            merged[existing].Path = resolvedPath;

                        merged[existing].AvailableInEditor = configured.AvailableInEditor;
                        merged[existing].AvailableInRuntime = configured.AvailableInRuntime;

                        if (configured.DefaultPrefixes != null && configured.DefaultPrefixes.Count > 0)
                            merged[existing].DefaultPrefixes = NormalizePrefixes(configured.DefaultPrefixes);

                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(resolvedPath))
                        continue;

                    merged.Add(new FileToolRootCandidate
                    {
                        Name = name,
                        Description = configured.Description?.Trim() ?? "",
                        Path = resolvedPath,
                        AvailableInEditor = configured.AvailableInEditor,
                        AvailableInRuntime = configured.AvailableInRuntime,
                        DefaultPrefixes = NormalizePrefixes(configured.DefaultPrefixes),
                    });
                }
            }

            var active = new List<FileToolRootCandidate>();
            for (var i = 0; i < merged.Count; i++)
            {
                var candidate = merged[i];
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.Name) || string.IsNullOrWhiteSpace(candidate.Path))
                    continue;

                var allowed = isEditor ? candidate.AvailableInEditor : candidate.AvailableInRuntime;
                if (!allowed)
                    continue;

                if (!isEditor && string.Equals(candidate.Name, "project", StringComparison.OrdinalIgnoreCase))
                    continue;

                active.Add(candidate);
            }

            return active;
        }

        private static List<FileToolRootCandidate> GetBuiltInFileToolRoots(string projectRoot)
        {
            return new List<FileToolRootCandidate>
            {
                new FileToolRootCandidate
                {
                    Name = "persistent",
                    Description = "Runtime-owned files under Application.persistentDataPath.",
                    Path = NormalizePath(Application.persistentDataPath),
                    AvailableInEditor = true,
                    AvailableInRuntime = true,
                    DefaultPrefixes = new List<string>(),
                },
                new FileToolRootCandidate
                {
                    Name = "project",
                    Description = "Unity project files under the project root, including Assets and Packages.",
                    Path = NormalizePath(projectRoot),
                    AvailableInEditor = true,
                    AvailableInRuntime = false,
                    DefaultPrefixes = new List<string> { "Assets", "Packages", "ProjectSettings" },
                },
            };
        }

        private static string ResolveDefaultFileToolRootName(
            List<FileToolRootCandidate> roots,
            PieSettings settingsOverride,
            bool isEditor)
        {
            var configured = LoadConfiguredDefaultFileToolRoot(settingsOverride, isEditor);
            if (!string.IsNullOrWhiteSpace(configured) &&
                roots.Exists(root => string.Equals(root.Name, configured, StringComparison.OrdinalIgnoreCase)))
            {
                return configured.Trim();
            }

            if (roots.Exists(root => string.Equals(root.Name, "persistent", StringComparison.OrdinalIgnoreCase)))
                return "persistent";

            return roots.Count > 0 ? roots[0].Name : null;
        }

        private static string ResolveConfiguredFileToolRootPath(string projectRoot, PieFileToolRootDefinition configured)
        {
            if (configured == null)
                return null;

            if (!string.IsNullOrWhiteSpace(configured.AbsolutePath))
                return NormalizePath(configured.AbsolutePath.Trim());

            if (!string.IsNullOrWhiteSpace(configured.RelativePathFromProjectRoot))
                return NormalizePath(Path.Combine(projectRoot ?? "", configured.RelativePathFromProjectRoot.Trim()));

            return null;
        }

        private static List<string> NormalizePrefixes(IReadOnlyList<string> prefixes)
        {
            var results = new List<string>();
            if (prefixes == null)
                return results;

            for (var i = 0; i < prefixes.Count; i++)
            {
                var value = prefixes[i];
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                var normalized = value.Trim().Replace("\\", "/").Trim('/');
                if (string.IsNullOrWhiteSpace(normalized))
                    continue;

                if (!results.Contains(normalized))
                    results.Add(normalized);
            }

            return results;
        }

#if UNITY_EDITOR
        private static PieSettings LoadSettingsAsset()
        {
            var direct = AssetDatabase.LoadAssetAtPath<PieSettings>(DefaultSettingsAssetPath);
            if (direct != null)
                return direct;

            var guids = AssetDatabase.FindAssets("t:PieSettings");
            for (var i = 0; i < guids.Length; i++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<PieSettings>(assetPath);
                if (asset != null)
                    return asset;
            }

            return null;
        }
#endif

        private static string ResolveProjectPath(string projectRoot, string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
                return null;

            var normalized = configuredPath.Replace("\\", "/").Trim();
            if (Path.IsPathRooted(normalized))
                return NormalizePath(normalized);

            return NormalizePath(Path.Combine(projectRoot ?? "", normalized));
        }

        private static string NormalizePath(string value)
        {
            return string.IsNullOrEmpty(value)
                ? value
                : Path.GetFullPath(value).Replace("\\", "/");
        }

        private static string BuildJsonArray(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
                return "[]";

            var escaped = new string[values.Count];
            for (var i = 0; i < values.Count; i++)
                escaped[i] = "\"" + EscapeJson(values[i] ?? "") + "\"";

            return "[" + string.Join(",", escaped) + "]";
        }

        private static string EscapeJson(string value)
        {
            return (value ?? "")
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}
