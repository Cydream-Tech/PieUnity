using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Pie
{
    /// <summary>
    /// Delivery scope of a skill across editor and player builds. Authored via
    /// SKILL.md frontmatter (`pie-delivery:`), baked into a manifest asset at
    /// player-build time, and resolved per mode by the JS skill loader.
    /// </summary>
    public enum PieSkillDelivery
    {
        All = 0,
        PlayerOnly = 1,
        EditorOnly = 2,
    }

    [Serializable]
    public sealed class PieSkillFileEntry
    {
        /// Path relative to the skill directory, '/'-separated (e.g.
        /// "references/api.md"). Served by the JS runtime as a virtual file
        /// under pie-skill/<name>/<path> so player builds can read_file
        /// companion files that no longer exist on disk.
        public string path = "";
        public string content = "";
    }

    [Serializable]
    public sealed class PieSkillManifestEntry
    {
        public string name = "";
        public string description = "";
        /// Full SKILL.md content, inlined so player builds need no filesystem.
        public string content = "";
        public PieSkillDelivery delivery = PieSkillDelivery.All;
        /// Companion files (scripts, references) baked alongside SKILL.md.
        /// Empty for loose-file skills and skills with no extra files.
        public PieSkillFileEntry[] files = Array.Empty<PieSkillFileEntry>();

        public PieSkillManifestEntry Clone()
        {
            var clone = new PieSkillManifestEntry
            {
                name = name,
                description = description,
                content = content,
                delivery = delivery,
            };
            if (files != null && files.Length > 0)
            {
                clone.files = new PieSkillFileEntry[files.Length];
                for (var i = 0; i < files.Length; i++)
                {
                    var file = files[i];
                    clone.files[i] = file == null
                        ? new PieSkillFileEntry()
                        : new PieSkillFileEntry { path = file.path, content = file.content };
                }
            }
            return clone;
        }
    }

    /// <summary>
    /// Baked skill manifest loaded from Resources in player builds
    /// (Assets/Resources/Pie/SkillManifest.asset). Player builds have no
    /// filesystem skill directories, so the manifest is the only skill source;
    /// the editor prefers live filesystem scan (hot reload) and merges this
    /// manifest only for entries the filesystem does not provide.
    /// </summary>
    public sealed class PieSkillManifest : ScriptableObject
    {
        public PieSkillManifestEntry[] entries = Array.Empty<PieSkillManifestEntry>();

        private static PieSkillManifest cached;

        public static PieSkillManifest LoadCached()
        {
            if (cached == null)
            {
                cached = Resources.Load<PieSkillManifest>("Pie/SkillManifest");
            }
            return cached;
        }

        public static void ClearCache()
        {
            cached = null;
        }

        /// <summary>
        /// Manifest entries serialized as JSON for the JS bridge, filtered by
        /// delivery scope for the current mode (player keeps All + PlayerOnly;
        /// editor keeps All + EditorOnly so editor sessions see what a build
        /// would not ship, plus anything PlayerOnly via filesystem scan).
        /// </summary>
        public static string ToJsonForMode(bool isEditor)
        {
            var manifest = LoadCached();
            if (manifest == null || manifest.entries == null || manifest.entries.Length == 0)
                return "[]";

            var builder = new StringBuilder("[");
            var first = true;
            foreach (var entry in manifest.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.name)) continue;
                var keep = isEditor
                    ? entry.delivery != PieSkillDelivery.PlayerOnly
                    : entry.delivery != PieSkillDelivery.EditorOnly;
                if (!keep) continue;
                if (!first) builder.Append(",");
                first = false;
                builder.Append("{\"name\":").Append(Escape(entry.name));
                builder.Append(",\"description\":").Append(Escape(entry.description));
                builder.Append(",\"content\":").Append(Escape(entry.content));
                builder.Append(",\"delivery\":\"").Append(entry.delivery).Append("\"");
                if (entry.files != null && entry.files.Length > 0)
                {
                    builder.Append(",\"files\":[");
                    var firstFile = true;
                    foreach (var file in entry.files)
                    {
                        if (file == null || string.IsNullOrEmpty(file.path)) continue;
                        if (!firstFile) builder.Append(",");
                        firstFile = false;
                        builder.Append("{\"path\":").Append(Escape(file.path));
                        builder.Append(",\"content\":").Append(Escape(file.content)).Append("}");
                    }
                    builder.Append("]");
                }
                builder.Append("}");
            }
            builder.Append("]");
            return builder.ToString();
        }

        /// <summary>
        /// JSON string escaping. Covers every JSON mandatory escape plus all
        /// remaining C0 control characters (U+0000–U+001F) as \u00XX — a single
        /// raw control byte (e.g. \b or \f inside a SKILL.md body) would make
        /// the whole manifest unparseable on the JS side, silently dropping
        /// every bundled skill, not just the offending one.
        /// </summary>
        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            var builder = new StringBuilder("\"");
            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (ch < 0x20) builder.Append("\\u").Append(((int)ch).ToString("x4"));
                        else builder.Append(ch);
                        break;
                }
            }
            builder.Append('"');
            return builder.ToString();
        }
    }
}
