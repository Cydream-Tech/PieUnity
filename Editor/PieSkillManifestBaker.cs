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
    /// EditorOnly entries are KEPT in the baked asset so a later
    /// EditorOnly layer can override an earlier layer's All entry (same name
    /// resolution as the filesystem loader); PieSkillManifest.ToJsonForMode
    /// filters them out for player consumption and PlayerOnly for editor
    /// consumption at load time. Identical name collisions across layers
    /// resolve later-layer-wins, matching the editor's filesystem scan.
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
            // A bake failure must never abort a player build. If a manifest
            // from an earlier successful bake exists it is kept and will ship
            // (possibly stale); with none, the build simply carries no bundled
            // skills. Either way, warn loudly so the drift is visible.
            try
            {
                var (entries, summary) = Bake();
                Debug.Log($"[Pie] Skill manifest baked for player build: {summary}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Pie] Skill manifest bake failed; any previously baked manifest will ship as-is (possibly stale). " +
                    $"Re-run Tools/Pie/Bake Skill Manifest (Player) after fixing. Cause: {ex.Message}\n{ex.StackTrace}");
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
            var failedLayers = 0;

            // paths are ordered lowest-priority first (the JS filesystem loader
            // registers them in this order and later registrations win on name
            // collisions). The bake must resolve identical semantics: a later
            // layer with the same skill name REPLACES the earlier entry, so the
            // player manifest matches what the editor's filesystem scan would
            // have chosen (project layer beats repo layer).
            foreach (var skillsDir in paths)
            {
                if (string.IsNullOrWhiteSpace(skillsDir) || !Directory.Exists(skillsDir)) continue;
                try
                {
                    CollectFromDirectory(skillsDir, entries, byName);
                }
                catch (Exception ex)
                {
                    // One unreadable layer must not kill the whole bake: other
                    // layers still contribute their skills.
                    failedLayers++;
                    Debug.LogWarning($"[Pie] Skill manifest: skipping unreadable skills directory '{skillsDir}': {ex.Message}");
                }
            }

            if (failedLayers > 0 && entries.Count == 0)
            {
                // Every layer failed and nothing was collected: writing now
                // would replace the previous manifest with an empty one. Fail
                // the bake instead so any existing (stale) manifest ships.
                throw new InvalidOperationException($"all {failedLayers} skill layer(s) were unreadable; refusing to overwrite the manifest with an empty one");
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
            catch (Exception ex)
            {
                Debug.LogWarning($"[Pie] Skill manifest: skipping unreadable skill '{skillMdPath}': {ex.Message}");
                return;
            }

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
            else if (EntriesEqual(manifest.entries, entries))
            {
                // Nothing changed: skip SetDirty/Save so rebakes are no-ops
                // for version control (no asset churn).
                return;
            }

            manifest.entries = entries.ToArray();
            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static bool EntriesEqual(PieSkillManifestEntry[] current, List<PieSkillManifestEntry> next)
        {
            if (current == null) return next.Count == 0;
            if (current.Length != next.Count) return false;
            for (var i = 0; i < current.Length; i++)
            {
                var a = current[i];
                var b = next[i];
                if (a == null || b == null) return a == b;
                if (!string.Equals(a.name, b.name, StringComparison.Ordinal)) return false;
                if (!string.Equals(a.description, b.description, StringComparison.Ordinal)) return false;
                if (!string.Equals(a.content, b.content, StringComparison.Ordinal)) return false;
                if (a.delivery != b.delivery) return false;
            }
            return true;
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
