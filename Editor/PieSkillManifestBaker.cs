#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Pie;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;

namespace Pie.Editor
{
    /// <summary>
    /// Bakes filesystem skills into a Resources-loaded ScriptableObject
    /// manifest (Assets/Resources/Pie/SkillManifest.asset) so player builds
    /// keep their skills: player builds have no Assets/.agents directory
    /// structure, and the runtime skill scan is filesystem-only.
    ///
    /// Delivery is authored per skill via SKILL.md frontmatter:
    ///   pie-delivery: all | player-only | editor-only   (default: all)
    ///
    /// Editor-only skills are excluded from the bake entirely; the manifest
    /// ships All + PlayerOnly entries, and PieSkillManifest.ToJsonForMode
    /// filters per mode at load time.
    /// </summary>
    public static class PieSkillManifestBaker
    {
        private const string ManifestAssetPath = "Assets/Resources/Pie/SkillManifest.asset";
        private const string DeliveryKey = "pie-delivery";

        public const string MenuPath = "Tools/Pie/Bake Skill Manifest (Player)";

        [MenuItem(MenuPath, priority = 260)]
        public static void BakeViaMenu()
        {
            var (entries, summary) = Bake();
            Debug.Log($"[Pie] Skill manifest baked: {summary}");
            if (entries > 0) PieSkillManifest.ClearCache();
        }

        public static int PreprocessBuild()
        {
            // A bake failure must never abort a player build: the manifest is
            // an additive payload (missing manifest = no bundled skills), so
            // log loudly and let the build continue.
            try
            {
                var (entries, summary) = Bake();
                Debug.Log($"[Pie] Skill manifest baked for player build: {summary}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Pie] Skill manifest bake failed (build continues without bundled skills): {ex.Message}\n{ex.StackTrace}");
            }
            PieSkillManifest.ClearCache();
            return 0;
        }

        private static (int count, string summary) Bake()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var paths = PieProjectPaths.GetSkillSearchPaths(projectRoot);
            var entries = new List<PieSkillManifestEntry>();
            var byName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // paths are ordered lowest-priority first (the JS filesystem loader
            // registers them in this order and later registrations win on name
            // collisions). The bake must resolve identical semantics: a later
            // layer with the same skill name REPLACES the earlier entry, so the
            // player manifest matches what the editor's filesystem scan would
            // have chosen (project layer beats repo layer).
            foreach (var skillsDir in paths)
            {
                if (string.IsNullOrWhiteSpace(skillsDir) || !Directory.Exists(skillsDir)) continue;
                CollectFromDirectory(skillsDir, entries, byName);
            }

            WriteManifestAsset(entries);
            return (entries.Count, $"{entries.Count} skill(s) -> {ManifestAssetPath}");
        }

        private static void CollectFromDirectory(string skillsDir, List<PieSkillManifestEntry> entries, Dictionary<string, int> byName)
        {
            foreach (var dir in Directory.GetDirectories(skillsDir))
            {
                var skillMd = Path.Combine(dir, "SKILL.md");
                if (!File.Exists(skillMd)) continue;
                AddEntry(Path.GetFileName(dir), skillMd, dir, entries, byName);
            }
            foreach (var file in Directory.GetFiles(skillsDir, "*.md"))
            {
                AddEntry(Path.GetFileNameWithoutExtension(file), file, skillsDir, entries, byName);
            }
        }

        private static void AddEntry(string name, string skillMdPath, string baseDir, List<PieSkillManifestEntry> entries, Dictionary<string, int> byName)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            string content;
            try { content = File.ReadAllText(skillMdPath) ?? ""; }
            catch { return; }

            var delivery = ParseDelivery(content);
            // EditorOnly entries are kept (not skipped here) so a later
            // EditorOnly skill can override an earlier layer's All entry —
            // exactly like the filesystem loader's name resolution.
            // ToJsonForMode filters them out for player consumption.
            var entry = new PieSkillManifestEntry
            {
                name = name,
                description = ParseDescription(content, name),
                content = content,
                delivery = delivery,
            };

            if (byName.TryGetValue(name, out var index))
            {
                // Later layer wins: replace the earlier entry in place.
                entries[index] = entry;
            }
            else
            {
                byName[name] = entries.Count;
                entries.Add(entry);
            }
        }

        internal static PieSkillDelivery ParseDelivery(string content)
        {
            var match = Regex.Match(
                content ?? "",
                @"^---\s*\n(?<front>[\s\S]*?)\n---",
                RegexOptions.None);
            if (!match.Success) return PieSkillDelivery.All;
            var line = Regex.Match(
                match.Groups["front"].Value,
                @"^" + DeliveryKey + @":\s*(?<value>\S+)\s*$",
                RegexOptions.Multiline);
            if (!line.Success) return PieSkillDelivery.All;
            switch (line.Groups["value"].Value.Trim().ToLowerInvariant())
            {
                case "player-only":
                case "playeronly":
                case "player":
                    return PieSkillDelivery.PlayerOnly;
                case "editor-only":
                case "editoronly":
                case "editor":
                    return PieSkillDelivery.EditorOnly;
                default:
                    return PieSkillDelivery.All;
            }
        }

        internal static string ParseDescription(string content, string fallbackName)
        {
            var frontmatter = Regex.Match(content ?? "", @"^---\s*\n(?<front>[\s\S]*?)\n---");
            if (frontmatter.Success)
            {
                var description = Regex.Match(
                    frontmatter.Groups["front"].Value,
                    @"^description:\s*[""']?(?<value>.+?)[""']?\s*$",
                    RegexOptions.Multiline);
                if (description.Success) return description.Groups["value"].Value.Trim();
            }
            var heading = Regex.Match(content ?? "", @"^#\s+(?<value>.+)$", RegexOptions.Multiline);
            if (heading.Success) return heading.Groups["value"].Value.Trim();
            return fallbackName;
        }

        private static void WriteManifestAsset(List<PieSkillManifestEntry> entries)
        {
            var directory = Path.GetDirectoryName(ManifestAssetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Update in place when the asset already exists: DeleteAsset +
            // CreateAsset would mint a fresh GUID on every bake, churning
            // references and version control for a data-only change.
            var manifest = AssetDatabase.LoadAssetAtPath<PieSkillManifest>(ManifestAssetPath);
            if (manifest == null)
            {
                manifest = ScriptableObject.CreateInstance<PieSkillManifest>();
                AssetDatabase.CreateAsset(manifest, ManifestAssetPath);
            }

            manifest.entries = entries.ToArray();
            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private sealed class PlayerBuildProcessor : IPreprocessBuildWithReport
        {
            public int callbackOrder => -100;

            public void OnPreprocessBuild(BuildReport report)
            {
                PieSkillManifestBaker.PreprocessBuild();
            }
        }
    }
}
#endif
