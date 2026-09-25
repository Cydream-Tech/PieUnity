using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Pie.Editor
{
    // A deliberately small Markdown presentation layer. Original text stays on the
    // message for copying/persistence; incomplete streaming blocks are valid input.
    internal sealed class PieChatMarkdown
    {
        internal sealed class Block
        {
            public string Kind;
            public string Text;
            public string Marker;
            public int Level;
            public List<string[]> Rows;
        }

        private static readonly Regex Heading = new Regex(@"^(#{1,6})[ \t]+(.+?)(?:[ \t]+#+)?[ \t]*$");
        private static readonly Regex ListItem = new Regex(@"^(\s*)([-+*]|\d+[.)])\s+(.*)$");
        private static readonly Regex Divider = new Regex(@"^\s{0,3}([-*_])(?:\s*\1){2,}\s*$");
        private static readonly Regex TableDivider = new Regex(@"^\s*\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)+\|?\s*$");
        private static readonly Regex Inline = new Regex(@"`([^`\n]+)`|\*\*(.+?)\*\*|__(.+?)__|(?<!\w)\*([^*\n]+)\*(?!\w)|(?<!\w)_([^_\n]+)_(?!\w)|\[([^\]\n]+)\]\(([^\s)]+)\)");
        private string _source;
        private List<Block> _blocks;
        private static Font _codeFont;

        internal static List<Block> Parse(string source)
        {
            var result = new List<Block>();
            var lines = (source ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.TrimStart();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (TryFence(trimmed, out var fence, out var count, out var language))
                {
                    var code = new List<string>();
                    for (i++; i < lines.Length; i++)
                    {
                        var closing = lines[i].Trim();
                        if (closing.Length >= count && closing.Trim(fence).Length == 0) break;
                        code.Add(lines[i]);
                    }
                    result.Add(new Block { Kind = "code", Text = string.Join("\n", code), Marker = language });
                    continue;
                }
                var heading = Heading.Match(line);
                if (heading.Success)
                    result.Add(new Block { Kind = "heading", Text = heading.Groups[2].Value, Level = heading.Groups[1].Length });
                else if (Divider.IsMatch(line))
                    result.Add(new Block { Kind = "divider", Text = "" });
                else if (i + 1 < lines.Length && line.Contains("|") && TableDivider.IsMatch(lines[i + 1]))
                {
                    var rows = new List<string[]> { SplitRow(line) };
                    i++;
                    while (i + 1 < lines.Length && lines[i + 1].Contains("|") && !string.IsNullOrWhiteSpace(lines[i + 1]))
                        rows.Add(SplitRow(lines[++i]));
                    result.Add(new Block { Kind = "table", Rows = rows });
                }
                else if (trimmed.StartsWith(">", StringComparison.Ordinal))
                {
                    var quote = trimmed.Substring(1).TrimStart();
                    while (i + 1 < lines.Length && lines[i + 1].TrimStart().StartsWith(">", StringComparison.Ordinal))
                        quote += "\n" + lines[++i].TrimStart().Substring(1).TrimStart();
                    result.Add(new Block { Kind = "quote", Text = quote });
                }
                else if (ListItem.IsMatch(line))
                {
                    var match = ListItem.Match(line);
                    var body = match.Groups[3].Value;
                    var marker = match.Groups[2].Value.Length == 1 ? "•" : match.Groups[2].Value;
                    if (body.StartsWith("[ ] ", StringComparison.Ordinal)) { marker = "☐"; body = body.Substring(4); }
                    else if (body.StartsWith("[x] ", StringComparison.OrdinalIgnoreCase)) { marker = "☑"; body = body.Substring(4); }
                    result.Add(new Block { Kind = "list", Text = body, Marker = marker, Level = Math.Min(6, match.Groups[1].Value.Replace("\t", "    ").Length / 2) });
                }
                else
                {
                    var paragraph = line;
                    while (i + 1 < lines.Length && !StartsBlock(lines, i + 1))
                        paragraph += "\n" + lines[++i];
                    result.Add(new Block { Kind = "paragraph", Text = paragraph });
                }
            }
            return result;
        }

        private static bool StartsBlock(string[] lines, int i)
        {
            var text = lines[i];
            return string.IsNullOrWhiteSpace(text) || Heading.IsMatch(text) || ListItem.IsMatch(text)
                || Divider.IsMatch(text) || text.TrimStart().StartsWith(">", StringComparison.Ordinal)
                || TryFence(text.TrimStart(), out _, out _, out _)
                || (i + 1 < lines.Length && text.Contains("|") && TableDivider.IsMatch(lines[i + 1]));
        }

        private static bool TryFence(string text, out char fence, out int count, out string language)
        {
            fence = text.Length > 0 ? text[0] : ' ';
            count = 0;
            language = "";
            if (fence != '`' && fence != '~') return false;
            while (count < text.Length && text[count] == fence) count++;
            if (count < 3) return false;
            language = text.Substring(count).Trim();
            return true;
        }

        private static string[] SplitRow(string line)
        {
            // Keep escaped pipes and pipes inside inline code in their cell.
            var cells = new List<string>();
            var cell = new StringBuilder();
            var code = false;
            var text = line.Trim();
            var start = text.StartsWith("|", StringComparison.Ordinal) ? 1 : 0;
            for (var i = start; i < text.Length; i++)
            {
                if (text[i] == '\\' && i + 1 < text.Length && text[i + 1] == '\\') { cell.Append("\\\\"); i++; }
                else if (text[i] == '\\' && i + 1 < text.Length && text[i + 1] == '|') { cell.Append('|'); i++; }
                else if (text[i] == '`') { code = !code; cell.Append(text[i]); }
                else if (text[i] == '|' && !code)
                {
                    cells.Add(cell.ToString().Trim());
                    cell.Clear();
                    if (i == text.Length - 1) return cells.ToArray();
                }
                else cell.Append(text[i]);
            }
            cells.Add(cell.ToString().Trim());
            return cells.ToArray();
        }

        internal static string FormatInline(string text, bool dark)
        {
            var output = new StringBuilder();
            var offset = 0;
            foreach (Match match in Inline.Matches(text ?? ""))
            {
                output.Append(Literal(text.Substring(offset, match.Index - offset)));
                if (match.Groups[1].Success)
                    output.Append("<color=" + (dark ? "#DBBC8C" : "#79551A") + ">" + Literal(match.Groups[1].Value) + "</color>");
                else if (match.Groups[2].Success || match.Groups[3].Success)
                    output.Append("<b>" + Literal(match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value) + "</b>");
                else if (match.Groups[4].Success || match.Groups[5].Success)
                    output.Append("<i>" + Literal(match.Groups[4].Success ? match.Groups[4].Value : match.Groups[5].Value) + "</i>");
                else
                    output.Append("<color=" + (dark ? "#8AB4F8" : "#245A9D") + ">" + Literal(match.Groups[6].Value) + "</color> (" + Literal(match.Groups[7].Value) + ")");
                offset = match.Index + match.Length;
            }
            output.Append(Literal((text ?? "").Substring(offset)));
            return output.ToString();
        }

        // Unity rich text has no HTML escaping. Break tag recognition without
        // changing its visible spelling. Code blocks use richText=false instead.
        private static string Literal(string text) => text.Replace("<", "<\u200B");

        internal void Draw(string source, float width)
        {
            if (_blocks == null || _source != source) { _source = source; _blocks = Parse(source); }
            var dark = EditorGUIUtility.isProSkin;
            var body = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 13, richText = true, wordWrap = true,
                padding = new RectOffset(0, 0, 2, 2), margin = new RectOffset(0, 0, 0, 0)
            };
            body.normal.textColor = dark ? new Color(.84f, .85f, .87f) : new Color(.16f, .17f, .19f);
            for (var i = 0; i < _blocks.Count; i++)
            {
                var block = _blocks[i];
                if (i > 0) GUILayout.Space(block.Kind == "list" && _blocks[i - 1].Kind == "list" ? 2 : 9);
                if (block.Kind == "code") { DrawCode(block.Text, block.Marker, width); continue; }
                if (block.Kind == "table") { DrawTable(block.Rows, body, width, dark); continue; }
                if (block.Kind == "divider")
                {
                    var rule = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
                    EditorGUI.DrawRect(rule, dark ? new Color(1, 1, 1, .15f) : new Color(0, 0, 0, .15f));
                    continue;
                }
                var style = new GUIStyle(body);
                if (block.Kind == "heading")
                {
                    style.fontSize = block.Level == 1 ? 21 : block.Level == 2 ? 17 : 14;
                    style.fontStyle = FontStyle.Bold;
                }
                var text = FormatInline(block.Text, dark);
                if (block.Kind == "list")
                {
                    var indent = block.Level * 14f;
                    var height = style.CalcHeight(new GUIContent(text), Mathf.Max(40, width - indent - 30));
                    var rect = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
                    GUI.Label(new Rect(rect.x + indent, rect.y, 26, rect.height), block.Marker, body);
                    rect.xMin += indent + 26;
                    EditorGUI.SelectableLabel(rect, text, style);
                }
                else if (block.Kind == "quote")
                {
                    style.normal.textColor = dark ? new Color(.65f, .69f, .75f) : new Color(.36f, .4f, .45f);
                    var rect = GUILayoutUtility.GetRect(0, style.CalcHeight(new GUIContent(text), Math.Max(40, width - 16)), GUILayout.ExpandWidth(true));
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3, rect.height), new Color(.45f, .55f, .7f, .7f));
                    rect.xMin += 14;
                    EditorGUI.SelectableLabel(rect, text, style);
                }
                else DrawText(text, style, width);
            }
        }

        internal static void DrawText(string text, GUIStyle style, float width)
        {
            var height = style.CalcHeight(new GUIContent(text ?? ""), Mathf.Max(40, width));
            EditorGUILayout.SelectableLabel(text ?? "", style, GUILayout.Height(Mathf.Max(style.lineHeight, height)), GUILayout.ExpandWidth(true));
        }

        internal static void DrawCode(string text, string language, float width)
        {
            var panel = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(10, 10, 8, 8) };
            EditorGUILayout.BeginVertical(panel);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(string.IsNullOrEmpty(language) ? "Code" : language, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Copy", EditorStyles.miniButton, GUILayout.Width(44))) EditorGUIUtility.systemCopyBuffer = text;
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);
            if (_codeFont == null)
            {
                // CreateDynamicFontFromOSFont returns null when none of the OS
                // fonts are available instead of throwing; probe candidates
                // one by one and fall back to the built-in font so code blocks
                // never take the chat window down with a NullReferenceException.
                foreach (var fontName in new[] { "Menlo", "Consolas", "Liberation Mono", "Courier New", "Monospace" })
                {
                    var candidate = Font.CreateDynamicFontFromOSFont(fontName, 12);
                    if (candidate == null) continue;
                    _codeFont = candidate;
                    _codeFont.hideFlags = HideFlags.HideAndDontSave;
                    break;
                }
                if (_codeFont == null)
                    _codeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            var codeStyle = new GUIStyle(EditorStyles.wordWrappedLabel) { font = _codeFont, fontSize = 12, richText = false, wordWrap = true };
            DrawText(text, codeStyle, width - 24);
            EditorGUILayout.EndVertical();
        }

        private static void DrawTable(List<string[]> rows, GUIStyle body, float width, bool dark)
        {
            var columns = rows[0].Length;
            foreach (var row in rows) columns = Math.Max(columns, row.Length);
            var cellWidth = Mathf.Max(30, width / columns);
            for (var r = 0; r < rows.Count; r++)
            {
                var style = new GUIStyle(body) { padding = new RectOffset(7, 7, 6, 6), fontStyle = r == 0 ? FontStyle.Bold : FontStyle.Normal };
                var height = 24f;
                for (var c = 0; c < columns; c++)
                    height = Mathf.Max(height, style.CalcHeight(new GUIContent(c < rows[r].Length ? FormatInline(rows[r][c], dark) : ""), cellWidth));
                var rect = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
                if (r == 0 || r % 2 == 0) EditorGUI.DrawRect(rect, new Color(.5f, .5f, .5f, r == 0 ? .18f : .07f));
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), new Color(.5f, .5f, .5f, .18f));
                for (var c = 0; c < columns; c++)
                    EditorGUI.SelectableLabel(new Rect(rect.x + rect.width * c / columns, rect.y, rect.width / columns, rect.height), c < rows[r].Length ? FormatInline(rows[r][c], dark) : "", style);
            }
        }
    }
}
