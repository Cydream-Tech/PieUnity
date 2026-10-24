using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Pie
{
    internal static class PieVoiceHttpClient
    {
        // Uploads go through PieSafeHttpClient (UnityWebRequest on the main
        // thread): HttpClient + multipart hangs indefinitely in IL2CPP player
        // builds (dogfood 3.0: voice uploads never completed and the 120s
        // CancelAfter never fired, while the same flow works in the editor).
        // UnityWebRequest is the supported stack on every player platform.
        internal static async Task<string> TranscribeAsync(
            PieVoiceOptions options,
            byte[] audioBytes,
            string mimeType,
            string sourceName,
            CancellationToken cancellationToken)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (audioBytes == null || audioBytes.Length == 0)
                throw new ArgumentException("Audio payload is empty.", nameof(audioBytes));

            var url = BuildTranscribeUrl(options.ApiBaseUrl);
            var sections = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("file", audioBytes,
                    string.IsNullOrWhiteSpace(sourceName) ? "recording.wav" : sourceName,
                    string.IsNullOrWhiteSpace(mimeType) ? "audio/wav" : mimeType),
            };
            AddSection(sections, "mode", ToApiValue(options.Mode));
            AddSection(sections, "context_hint", ToApiValue(options.ContextHint));
            AddSection(sections, "tone", ToApiValue(options.Tone));
            AddOptionalSection(sections, "language", options.Language, false);
            AddOptionalSection(sections, "target_language", options.TargetLanguage, true);
            AddOptionalSection(sections, "preserve_terms", options.PreserveTerms, false);
            AddOptionalSection(sections, "asr_model", options.AsrModel, false);
            AddOptionalSection(sections, "llm_model", options.LlmModel, false);
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer " + NormalizeBearerToken(options.VirtualKey),
                ["Accept"] = "application/json",
            };
            var timeoutSeconds = Math.Max(1, options.TimeoutSeconds);

            try
            {
                return await PieSafeHttpClient
                    .UploadMultipartAsync(url, sections, headers, timeoutSeconds, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (PieHttpException httpError)
            {
                throw new PieVoiceHttpException(httpError.StatusCode, httpError.ResponseBody);
            }
        }

        private static void AddSection(List<IMultipartFormSection> sections, string name, string value)
        {
            sections.Add(new MultipartFormDataSection(name, value ?? ""));
        }

        private static void AddOptionalSection(List<IMultipartFormSection> sections, string name, string value, bool skipNone)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            var normalized = value.Trim();
            if (skipNone && string.Equals(normalized, "none", StringComparison.OrdinalIgnoreCase))
                return;
            sections.Add(new MultipartFormDataSection(name, normalized));
        }

        internal static string ToApiValue(PieVoiceMode mode)
        {
            switch (mode)
            {
                case PieVoiceMode.Clean:
                    return "clean";
                case PieVoiceMode.Compose:
                    return "compose";
                default:
                    return "structure";
            }
        }

        internal static string ToApiValue(PieVoiceContextHint contextHint)
        {
            switch (contextHint)
            {
                case PieVoiceContextHint.Chat:
                    return "chat";
                case PieVoiceContextHint.Email:
                    return "email";
                case PieVoiceContextHint.Support:
                    return "support";
                case PieVoiceContextHint.Note:
                    return "note";
                case PieVoiceContextHint.Code:
                    return "code";
                case PieVoiceContextHint.Social:
                    return "social";
                default:
                    return "task";
            }
        }

        internal static string ToApiValue(PieVoiceTone tone)
        {
            switch (tone)
            {
                case PieVoiceTone.Neutral:
                    return "neutral";
                case PieVoiceTone.Casual:
                    return "casual";
                case PieVoiceTone.Formal:
                    return "formal";
                case PieVoiceTone.Polite:
                    return "polite";
                default:
                    return "concise";
            }
        }

        private static string BuildTranscribeUrl(string apiBaseUrl)
        {
            var baseUrl = string.IsNullOrWhiteSpace(apiBaseUrl)
                ? "https://token.magicshell.ai"
                : apiBaseUrl.Trim();
            return baseUrl.TrimEnd('/') + "/voice/transcribe";
        }

        private static string NormalizeBearerToken(string virtualKey)
        {
            var token = (virtualKey ?? "").Trim();
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = token.Substring("Bearer ".Length).Trim();
            return token;
        }

    }

    internal sealed class PieVoiceHttpException : Exception
    {
        internal int StatusCode { get; private set; }
        internal string ResponseBody { get; private set; }

        internal PieVoiceHttpException(int statusCode, string responseBody)
            : base($"Voice transcription failed with HTTP {statusCode}: {TrimResponseBody(responseBody)}")
        {
            StatusCode = statusCode;
            ResponseBody = responseBody ?? "";
        }

        private static string TrimResponseBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return "";
            body = body.Trim();
            return body.Length <= 500 ? body : body.Substring(0, 500) + "...";
        }
    }
}
