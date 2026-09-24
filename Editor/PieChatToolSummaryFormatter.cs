using System;
using System.Collections.Generic;
using System.Text;

namespace Pie.Editor
{
    internal static class PieChatToolSummaryFormatter
    {
        public static string SummarizeToolResult(string toolName, string resultText, string detailsJson)
        {
            if (toolName == "inspect_unity_context")
            {
                var source = !string.IsNullOrEmpty(detailsJson) ? detailsJson : resultText;
                var pipeline = ExtractJsonString(source, "renderPipeline") ?? "unknown pipeline";
                var scene = ExtractJsonString(source, "sceneName") ?? "unknown scene";
                return $"{pipeline}, scene={scene}";
            }

            if (string.IsNullOrEmpty(resultText)) return "Done.";
            if (toolName == "unity_script_run")
            {
                var summary = ExtractJsonString(detailsJson ?? "", "summary")
                    ?? ExtractJsonString(resultText, "summary");
                if (string.IsNullOrWhiteSpace(summary))
                {
                    // Agent-facing results may be TOON rather than JSON.
                    foreach (var line in resultText.Split('\n'))
                    {
                        if (!line.StartsWith("summary:", StringComparison.Ordinal)) continue;
                        summary = line.Substring("summary:".Length).Trim().Trim('"');
                        break;
                    }
                }
                return string.IsNullOrWhiteSpace(summary) ? "Script completed." : summary;
            }
            if (toolName == "manage_todo_list")
            {
                if (resultText.Contains("Todo action failed", StringComparison.Ordinal)) return resultText.Trim();
                if (resultText.Contains("No active todo list", StringComparison.Ordinal) || resultText.Contains("No todos", StringComparison.Ordinal)) return "No active todo list";
                if (resultText.StartsWith("Created ", StringComparison.Ordinal)) return resultText.Trim();
                if (resultText.StartsWith("Completed item ", StringComparison.Ordinal)) return resultText.Trim();
                if (resultText.StartsWith("Cleared todo list", StringComparison.Ordinal)) return resultText.Trim();
                return resultText.Trim();
            }

            if (toolName == "web_search")
            {
                var sourceCount = ExtractJsonInt(detailsJson, "sourceCount");
                var toolModel = ExtractJsonString(detailsJson, "toolModel");
                var providerMode = ExtractJsonString(detailsJson, "providerMode");
                var parts = new List<string> { sourceCount >= 0 ? $"{sourceCount} sources" : "search completed" };
                if (!string.IsNullOrEmpty(toolModel)) parts.Add(toolModel);
                if (!string.IsNullOrEmpty(providerMode)) parts.Add(providerMode);
                return string.Join(" · ", parts);
            }

            if (toolName == "web_research")
            {
                var qualityScore = ExtractJsonInt(detailsJson, "qualityScore");
                var fetchedCount = ExtractJsonArrayCount(detailsJson, "fetchedSources");
                var browserRequiredCount = ExtractJsonArrayCount(detailsJson, "browserRequiredSources");
                var parts = new List<string> { fetchedCount >= 0 ? $"{fetchedCount} fetched sources" : "research completed" };
                if (qualityScore >= 0) parts.Add($"quality {qualityScore}");
                if (browserRequiredCount > 0) parts.Add($"{browserRequiredCount} browser-required");
                return string.Join(" · ", parts);
            }

            if (toolName == "read_file" && !string.IsNullOrEmpty(detailsJson) && detailsJson.Contains("\"understanding\"", StringComparison.Ordinal))
            {
                var purpose = ExtractJsonString(detailsJson, "purpose");
                var toolModel = ExtractJsonString(detailsJson, "toolModel");
                var routeSource = ExtractJsonString(detailsJson, "routeSource");
                var brief = resultText.Replace("\r", "").Trim();
                if (brief.Length > 80) brief = brief.Substring(0, 80) + "...";
                var parts = new List<string> { string.IsNullOrEmpty(purpose) ? "file understanding" : purpose };
                if (!string.IsNullOrEmpty(toolModel)) parts.Add(toolModel);
                if (!string.IsNullOrEmpty(routeSource)) parts.Add(routeSource);
                if (!string.IsNullOrEmpty(brief)) parts.Add(brief);
                return string.Join(" · ", parts);
            }

            return resultText.Replace("\r", "").Trim();
        }

        public static string SummarizeToolFailure(string toolName, string errorText)
        {
            if (toolName == "manage_todo_list" && errorText.Contains("Todo action failed", StringComparison.Ordinal))
                return errorText.Replace("\r", "").Trim();
            return errorText.Replace("\r", "").Trim();
        }

        public static string BuildArgsSummary(string argsJson)
        {
            if (string.IsNullOrEmpty(argsJson)) return null;
            var formatted = FormatStructuredText(argsJson);
            if (string.IsNullOrEmpty(formatted)) return null;
            var singleLine = formatted.Replace("\r", "").Replace("\n", " ").Trim();
            if (singleLine.Length > 160) singleLine = singleLine.Substring(0, 160) + "...";
            return $"args: {singleLine}";
        }

        public static string BuildEffectiveArgsFromDetails(string toolName, string detailsJson)
        {
            if (string.IsNullOrEmpty(detailsJson)) return null;
            if (toolName == "find_files")
            {
                var parts = new List<string>();
                var rawPattern = ExtractJsonString(detailsJson, "rawPattern");
                var effectivePattern = ExtractJsonString(detailsJson, "effectivePattern");
                var searchPath = ExtractJsonString(detailsJson, "searchPath");
                if (!string.IsNullOrEmpty(rawPattern)) parts.Add($"rawPattern={rawPattern}");
                if (!string.IsNullOrEmpty(effectivePattern)) parts.Add($"pattern={effectivePattern}");
                if (!string.IsNullOrEmpty(searchPath)) parts.Add($"path={searchPath}");
                return parts.Count > 0 ? $"args: {{ {string.Join(", ", parts)} }}" : null;
            }
            if (toolName == "grep_text")
            {
                var parts = new List<string>();
                var effectivePattern = ExtractJsonString(detailsJson, "effectivePattern");
                var searchPath = ExtractJsonString(detailsJson, "searchPath");
                var glob = ExtractJsonString(detailsJson, "glob");
                if (!string.IsNullOrEmpty(effectivePattern)) parts.Add($"pattern={effectivePattern}");
                if (!string.IsNullOrEmpty(glob)) parts.Add($"glob={glob}");
                if (!string.IsNullOrEmpty(searchPath)) parts.Add($"path={searchPath}");
                return parts.Count > 0 ? $"args: {{ {string.Join(", ", parts)} }}" : null;
            }
            return null;
        }

        public static string FormatStructuredText(string text)
        {
            var safeText = (text ?? "").Trim();
            if (string.IsNullOrEmpty(safeText)) return "(no result)";
            var prettyJson = TryPrettyPrintJson(safeText);
            return !string.IsNullOrEmpty(prettyJson) ? prettyJson : safeText.Replace("\r", "");
        }

        private static string TryPrettyPrintJson(string text)
        {
            var trimmed = (text ?? "").Trim();
            if (trimmed.Length < 2) return null;
            var first = trimmed[0];
            var last = trimmed[trimmed.Length - 1];
            if (!((first == '{' && last == '}') || (first == '[' && last == ']'))) return null;

            var sb = new StringBuilder();
            var indent = 0;
            var inString = false;
            for (var i = 0; i < trimmed.Length; i++)
            {
                var c = trimmed[i];
                var escaped = i > 0 && trimmed[i - 1] == '\\';
                if (c == '"' && !escaped)
                {
                    inString = !inString;
                    sb.Append(c);
                    continue;
                }
                if (inString)
                {
                    sb.Append(c);
                    continue;
                }
                switch (c)
                {
                    case '{':
                    case '[':
                        sb.Append(c).Append('\n');
                        AppendIndent(sb, ++indent);
                        break;
                    case '}':
                    case ']':
                        sb.Append('\n');
                        indent = Math.Max(0, indent - 1);
                        AppendIndent(sb, indent);
                        sb.Append(c);
                        break;
                    case ',':
                        sb.Append(c).Append('\n');
                        AppendIndent(sb, indent);
                        break;
                    case ':':
                        sb.Append(": ");
                        break;
                    case ' ':
                    case '\n':
                    case '\t':
                    case '\r':
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        private static void AppendIndent(StringBuilder sb, int indent)
        {
            for (var i = 0; i < indent; i++) sb.Append("  ");
        }

        private static int ExtractJsonInt(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return -1;
            var pattern = $"\"{key}\":";
            var index = json.IndexOf(pattern, StringComparison.Ordinal);
            if (index < 0) return -1;
            index += pattern.Length;
            while (index < json.Length && char.IsWhiteSpace(json[index])) index++;
            var start = index;
            while (index < json.Length && char.IsDigit(json[index])) index++;
            return index > start && int.TryParse(json.Substring(start, index - start), out var value) ? value : -1;
        }

        private static int ExtractJsonArrayCount(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return -1;
            var pattern = $"\"{key}\":";
            var index = json.IndexOf(pattern, StringComparison.Ordinal);
            if (index < 0) return -1;
            index += pattern.Length;
            while (index < json.Length && char.IsWhiteSpace(json[index])) index++;
            if (index >= json.Length || json[index] != '[') return -1;

            index++;
            var depth = 0;
            var inString = false;
            var escape = false;
            var count = 0;
            var hasValue = false;
            for (; index < json.Length; index++)
            {
                var ch = json[index];
                if (inString)
                {
                    if (escape) escape = false;
                    else if (ch == '\\') escape = true;
                    else if (ch == '"') inString = false;
                    continue;
                }
                if (ch == '"') { inString = true; hasValue = true; continue; }
                if (ch == '[' || ch == '{') { depth++; hasValue = true; continue; }
                if (ch == ']' && depth == 0) return hasValue ? count + 1 : 0;
                if (ch == ']' || ch == '}') { depth--; continue; }
                if (ch == ',' && depth == 0) { count++; hasValue = false; continue; }
                if (!char.IsWhiteSpace(ch)) hasValue = true;
            }
            return -1;
        }

        private static string ExtractJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return null;
            var pattern = $"\"{key}\":\"";
            var start = json.IndexOf(pattern, StringComparison.Ordinal);
            if (start < 0) return null;
            start += pattern.Length;

            var sb = new StringBuilder();
            var i = start;
            while (i < json.Length)
            {
                var c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    switch (json[i + 1])
                    {
                        case 'n':  sb.Append('\n'); i += 2; continue;
                        case 'r':  sb.Append('\r'); i += 2; continue;
                        case 't':  sb.Append('\t'); i += 2; continue;
                        case '"':  sb.Append('"');  i += 2; continue;
                        case '\\': sb.Append('\\'); i += 2; continue;
                        default:   sb.Append(json[i + 1]); i += 2; continue;
                    }
                }
                if (c == '"') break;
                sb.Append(c);
                i++;
            }
            return sb.ToString();
        }
    }
}
