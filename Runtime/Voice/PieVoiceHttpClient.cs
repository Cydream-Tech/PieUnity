using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Pie
{
    internal static class PieVoiceHttpClient
    {
        private static readonly HttpClient HttpClient = CreateHttpClient();

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
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            using (var form = new MultipartFormDataContent())
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds)));

                var token = NormalizeBearerToken(options.VirtualKey);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.TryAddWithoutValidation("Accept", "application/json");

                var fileContent = new ByteArrayContent(audioBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(mimeType) ? "audio/wav" : mimeType);
                form.Add(fileContent, "file", string.IsNullOrWhiteSpace(sourceName) ? "recording.wav" : sourceName);

                AddField(form, "mode", ToApiValue(options.Mode));
                AddField(form, "context_hint", ToApiValue(options.ContextHint));
                AddField(form, "tone", ToApiValue(options.Tone));
                AddOptionalField(form, "language", options.Language, false);
                AddOptionalField(form, "target_language", options.TargetLanguage, true);
                AddOptionalField(form, "preserve_terms", options.PreserveTerms, false);
                AddOptionalField(form, "asr_model", options.AsrModel, false);
                AddOptionalField(form, "llm_model", options.LlmModel, false);

                request.Content = form;

                using (var response = await HttpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    timeout.Token))
                {
                    var body = response.Content != null
                        ? await response.Content.ReadAsStringAsync()
                        : "";
                    if (!response.IsSuccessStatusCode)
                        throw new PieVoiceHttpException((int)response.StatusCode, body);
                    return body;
                }
            }
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

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(180),
            };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
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

        private static void AddField(MultipartFormDataContent form, string name, string value)
        {
            form.Add(new StringContent(value ?? ""), name);
        }

        private static void AddOptionalField(MultipartFormDataContent form, string name, string value, bool skipNone)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            var normalized = value.Trim();
            if (skipNone && string.Equals(normalized, "none", StringComparison.OrdinalIgnoreCase))
                return;

            form.Add(new StringContent(normalized), name);
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
