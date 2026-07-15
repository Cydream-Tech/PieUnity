// Runtime/PieHttpBridge.cs
// C# managed HTTP client with true SSE streaming for PuerTS V8 (no Node.js backend needed).
// JS side polls PollLine() to consume SSE lines one at a time as they arrive.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Pie
{
    /// <summary>
    /// Bridges HTTP SSE streaming from C# to PuerTS V8 JavaScript.
    ///
    /// Flow:
    ///   JS: requestId = PieHttpBridge.StartStreamRequest(url, body, bearerToken, headersJson)
    ///   JS: while (true) { line = PieHttpBridge.PollLine(requestId); if (line === null) break; yield line; }
    ///   JS: PieHttpBridge.CancelRequest(requestId)  // optional early cancel
    /// </summary>
    public static class PieHttpBridge
    {
        private static readonly HttpClient _httpClient = CreateHttpClient();

        private static readonly ConcurrentDictionary<int, RequestState> _requests =
            new ConcurrentDictionary<int, RequestState>();

        private static int _nextRequestId = 1;

        // ─────────────────────────────────────────────────────────────────────
        // Public API (called from JS via PuerTS)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Start a provider SSE request. JS receives lines through
        /// _pieSSEPush/_pieSSEStatus and receives the final stream outcome through
        /// _pieSSETerminal; ordinary text fetches must use FetchTextAsync.
        /// headersJson is optional JSON object of additional headers, e.g. {"Content-Type":"application/json"}.
        /// </summary>
        public static int StartStreamRequest(string method, string url, string body, string bearerToken, string headersJson = null)
        {
            int id = Interlocked.Increment(ref _nextRequestId);
            var state = new RequestState(id);
            _requests[id] = state;
            PieDiagnostics.Info($"[PieHttpBridge] Start managed request [{id}] {(method ?? "POST")} {url}");

            state.WorkerTask = Task.Run(() => RunRequestAsync(state, method, url, body, bearerToken, headersJson));

            return id;
        }

        /// <summary>
        /// Create an UploadHandlerRaw from a UTF-8 string payload.
        /// Kept as a narrow bridge so PuerTS does not need to resolve
        /// UnityWebRequest constructor overloads for byte[] on its own.
        /// </summary>
        public static UploadHandlerRaw CreateUtf8UploadHandler(string body)
        {
            var payload = Encoding.UTF8.GetBytes(body ?? string.Empty);
            return new UploadHandlerRaw(payload);
        }

        /// <summary>
        /// Upload a file with multipart/form-data. This is the only upload path
        /// used by JS file adapters; it is intentionally separate from text fetch
        /// and provider SSE streaming.
        /// </summary>
        public static Task<string> UploadFileMultipartAsync(
            string url,
            string filePath,
            string bearerToken,
            string purpose,
            string mimeType,
            string sourceName,
            string headersJson = null)
        {
            return Task.Run(async () =>
            {
                if (string.IsNullOrWhiteSpace(url))
                    throw new ArgumentException("Upload URL is required.", nameof(url));
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                    throw new FileNotFoundException("Upload file not found.", filePath);

                var parsedHeaders = ParseHeaders(headersJson);
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                if (!string.IsNullOrEmpty(bearerToken))
                {
                    var token = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? bearerToken.Substring(7)
                        : bearerToken;
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                foreach (var kv in parsedHeaders)
                {
                    if (string.Equals(kv.Key, "Authorization", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(kv.Key, "Content-Type", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(kv.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                        continue;
                    request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                }

                using var form = new MultipartFormDataContent();
                var bytes = File.ReadAllBytes(filePath);
                var fileContent = new ByteArrayContent(bytes);
                if (!string.IsNullOrWhiteSpace(mimeType))
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                form.Add(fileContent, "file", string.IsNullOrWhiteSpace(sourceName) ? Path.GetFileName(filePath) : sourceName);
                form.Add(new StringContent(string.IsNullOrWhiteSpace(purpose) ? "file-extract" : purpose), "purpose");
                request.Content = form;

                using var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"HTTP {(int)response.StatusCode}: {body}");
                return body;
            });
        }

        /// <summary>
        /// Perform a plain buffered HTTP fetch for shared tools such as web_fetch.
        /// Returns a JSON payload built by BuildFetchTextResponseJson so JS can
        /// consume it through puer.$promise without polling UnityWebRequest.
        /// </summary>
        public static Task<string> FetchTextAsync(
            string method,
            string url,
            string body,
            string bearerToken,
            string headersJson = null)
        {
            return Task.Run(async () =>
            {
                if (string.IsNullOrWhiteSpace(url))
                    throw new ArgumentException("Fetch URL is required.", nameof(url));

                var normalizedMethod = string.IsNullOrWhiteSpace(method) ? "GET" : method.ToUpperInvariant();
                var parsedHeaders = ParseHeaders(headersJson);
                using var request = new HttpRequestMessage(new HttpMethod(normalizedMethod), url);

                if (!string.IsNullOrEmpty(bearerToken))
                {
                    var token = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? bearerToken.Substring(7)
                        : bearerToken;
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var hasAccept = false;
                foreach (var kv in parsedHeaders)
                {
                    if (string.Equals(kv.Key, "Authorization", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(kv.Key, "Content-Type", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(kv.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (string.Equals(kv.Key, "Accept", StringComparison.OrdinalIgnoreCase))
                        hasAccept = true;

                    request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                }

                if (!hasAccept)
                    request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,text/plain;q=0.8,*/*;q=0.5");

                if (!string.IsNullOrEmpty(body)
                    && !string.Equals(normalizedMethod, "GET", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(normalizedMethod, "HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    var contentType = parsedHeaders.TryGetValue("Content-Type", out var explicitContentType)
                        ? explicitContentType
                        : "application/json";
                    request.Content = new StringContent(body, Encoding.UTF8, contentType);
                }

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead);
                var responseText = response.Content != null
                    ? await response.Content.ReadAsStringAsync()
                    : string.Empty;
                var contentTypeHeader = response.Content?.Headers?.ContentType?.ToString() ?? string.Empty;
                var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;

                return BuildFetchTextResponseJson(
                    (int)response.StatusCode,
                    response.ReasonPhrase ?? string.Empty,
                    response.IsSuccessStatusCode,
                    finalUrl,
                    contentTypeHeader,
                    responseText);
            });
        }

        /// <summary>
        /// Poll for the next SSE line.
        /// Returns: the next line string, "" if no data yet, or null if the stream has ended.
        /// After null is returned the request ID is cleaned up automatically.
        /// </summary>
        public static string PollLine(int requestId)
        {
            if (!_requests.TryGetValue(requestId, out var state))
                return null; // unknown id — treat as ended

            // Try to dequeue a buffered line
            if (state.Lines.TryDequeue(out var line))
            {
                return line; // may be null (the end sentinel) or a real line
            }

            if (state.IsComplete)
            {
                // Stream finished, no more lines
                _requests.TryRemove(requestId, out _);
                return null;
            }

            // No data available yet
            return "";
        }

        /// <summary>Cancel an in-flight request early.</summary>
        public static void CancelRequest(int requestId)
        {
            if (_requests.TryRemove(requestId, out var state))
            {
                CancelState(state);
            }
        }

        public static void CancelAllRequests()
        {
            // Reload safety: managed SSE reads can block on ReadLineAsync unless the stream
            // itself is closed. Cancel tokens first, then close/abort/dispose every native and
            // managed handle, and only wait for workers for a bounded time. Never let an old
            // AppDomain streaming worker keep Unity assembly reload waiting indefinitely.
            var workerTasks = new List<Task>();
            foreach (var requestId in _requests.Keys)
            {
                if (_requests.TryRemove(requestId, out var state))
                {
                    if (state.WorkerTask != null)
                        workerTasks.Add(state.WorkerTask);
                    CancelState(state);
                }
            }

            try { _httpClient.CancelPendingRequests(); } catch { }

            if (workerTasks.Count == 0)
                return;

            try
            {
                if (!Task.WaitAll(workerTasks.ToArray(), TimeSpan.FromMilliseconds(750)))
                    PieDiagnostics.Warning($"[PieHttpBridge] Timed out waiting for {workerTasks.Count} request worker(s) to stop.");
            }
            catch
            {
                // Ignore cancellation/fault races during domain reload / disposal.
            }
        }

        private static void CancelState(RequestState state)
        {
            try { state.Cts.Cancel(); } catch { }
            try { state.NativeRequest?.Abort(); } catch { }
            try { state.NativeStream?.Dispose(); } catch { }
            try { state.NativeTcpClient?.Close(); } catch { }
            try { state.ManagedStream?.Dispose(); } catch { }
            try { state.ManagedResponse?.Dispose(); } catch { }
        }

        /// <summary>
        /// Get the HTTP status code for a request.
        /// Returns -1 if still waiting for response headers, 0 if unknown/finished.
        /// </summary>
        public static int GetStatus(int requestId)
        {
            if (_requests.TryGetValue(requestId, out var state))
                return state.StatusCode;
            return 0;
        }

        /// <summary>Check if a request encountered an error.</summary>
        public static string GetError(int requestId)
        {
            if (_requests.TryGetValue(requestId, out var state))
                return state.Error;
            return null;
        }

        /// <summary>
        /// Dequeue one line without the empty-string ambiguity of PollLine.
        /// Returns true if a line was dequeued (line may be "" for empty SSE lines),
        /// or when the request is complete and all lines have been drained (line is null).
        /// Returns false if the queue is empty (no data available yet).
        /// Completed requests remain registered until MarkTerminalPushed so their
        /// terminal error cannot be lost after an earlier successful HTTP status.
        /// </summary>
        public static bool TryPollLine(int requestId, out string line)
        {
            if (!_requests.TryGetValue(requestId, out var state))
            {
                line = null;
                return true; // unknown id → treat as stream ended
            }

            if (state.Lines.TryDequeue(out line))
                return true; // real line (may be "" for SSE separator or null for end sentinel)

            if (state.IsComplete)
            {
                line = null;
                return true; // stream ended
            }

            line = null;
            return false; // queue empty, still streaming
        }

        /// <summary>Returns IDs of all active (not yet cleaned up) requests.</summary>
        public static IEnumerable<int> GetActiveRequestIds() => _requests.Keys;

        public static int ActiveRequestCount => _requests.Count;

        /// <summary>
        /// Pump UnityWebRequest-backed requests from the host update loop.
        /// This is a fallback for Editor cases where DownloadHandlerScript
        /// does not continuously invoke ReceiveData for SSE payloads.
        /// </summary>
        public static void PumpActiveRequests()
        {
            foreach (var pair in _requests)
            {
                var state = pair.Value;
                if (state == null || state.IsComplete)
                    continue;

                try
                {
                    if (state.NativeRequest == null)
                        continue;

                    DrainUnityWebRequest(state);
                }
                catch (Exception ex)
                {
                    state.Error = ex.Message;
                    state.IsComplete = true;
                    PieDiagnostics.Warning($"[PieHttpBridge] PumpActiveRequests failed [{state.Id}]: {ex.Message}");
                }
            }
        }

        /// <summary>Whether the HTTP status for this request has already been pushed to JS.</summary>
        public static bool IsStatusPushed(int requestId)
        {
            return _requests.TryGetValue(requestId, out var state) && state.StatusPushed;
        }

        /// <summary>Mark that the HTTP status has been pushed to JS.</summary>
        public static void MarkStatusPushed(int requestId)
        {
            if (_requests.TryGetValue(requestId, out var state))
                state.StatusPushed = true;
        }

        /// <summary>
        /// Return the terminal stream outcome once the worker has completed and
        /// every queued line has been delivered. This is deliberately independent
        /// from StatusPushed: a 200 response can still fail while reading its body.
        /// </summary>
        public static bool TryGetTerminal(int requestId, out string error)
        {
            error = null;
            if (!_requests.TryGetValue(requestId, out var state)
                || !state.IsComplete
                || !state.Lines.IsEmpty
                || state.TerminalPushed)
            {
                return false;
            }

            error = state.Error;
            return true;
        }

        /// <summary>
        /// Mark the terminal stream outcome as delivered exactly once, then release
        /// the completed request. Cancellation may have already removed the state.
        /// </summary>
        public static void MarkTerminalPushed(int requestId)
        {
            if (_requests.TryGetValue(requestId, out var state))
            {
                state.TerminalPushed = true;
                _requests.TryRemove(requestId, out _);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Internal
        // ─────────────────────────────────────────────────────────────────────

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip |
                                          System.Net.DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(180)
            };
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        private static async Task RunNativeSocketRequestAsync(
            RequestState state,
            string method,
            string url,
            string body,
            string bearerToken,
            string headersJson)
        {
            var normalizedMethod = string.IsNullOrWhiteSpace(method) ? "POST" : method.ToUpperInvariant();
            var parsedHeaders = ParseHeaders(headersJson);

            try
            {
                var uri = new Uri(url);
                var port = uri.Port > 0 ? uri.Port : (string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase) ? 443 : 80);
                var client = new TcpClient();
                state.NativeTcpClient = client;
                client.NoDelay = true;
                await client.ConnectAsync(uri.Host, port);

                Stream stream = client.GetStream();
                if (string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                {
                    var sslStream = new SslStream(stream, false, (_, _, _, _) => true);
                    sslStream.AuthenticateAsClient(uri.Host);
                    stream = sslStream;
                }

                state.NativeStream = stream;
                await WriteHttpRequestAsync(stream, uri, normalizedMethod, body, bearerToken, parsedHeaders, state.Cts.Token);
                await ReadHttpResponseAsync(stream, state, state.Cts.Token);
            }
            catch (OperationCanceledException)
            {
                if (state.StatusCode < 0)
                    state.StatusCode = 499;
                state.Error = "Request cancelled";
                PieDiagnostics.Verbose("[PieHttpBridge] Native socket request cancelled");
            }
            catch (ObjectDisposedException)
            {
                if (state.StatusCode < 0)
                    state.StatusCode = 0;
                state.Error = "Request disposed";
                PieDiagnostics.Verbose("[PieHttpBridge] Native socket request disposed");
            }
            catch (Exception ex)
            {
                if (state.StatusCode < 0)
                    state.StatusCode = 0;
                state.Error = ex.Message;
                PieDiagnostics.Error($"[PieHttpBridge] Native socket request error: {normalizedMethod} {url} failed: {ex.Message}");
            }
            finally
            {
                FlushByteLineBuffer(state);
                state.IsComplete = true;
                try { state.NativeStream?.Dispose(); } catch { }
                try { state.NativeTcpClient?.Close(); } catch { }
                state.NativeStream = null;
                state.NativeTcpClient = null;
            }
        }

        private static async Task RunRequestAsync(
            RequestState state,
            string method,
            string url,
            string body,
            string bearerToken,
            string headersJson)
        {
            var normalizedMethod = string.IsNullOrWhiteSpace(method) ? "POST" : method.ToUpperInvariant();
            var parsedHeaders = ParseHeaders(headersJson);

            try
            {
                PieDiagnostics.Info($"[PieHttpBridge] Managed HttpClient request [{state.Id}] {normalizedMethod} {url}");
                await RunHttpClientRequestAsync(
                    state,
                    normalizedMethod,
                    url,
                    body,
                    bearerToken,
                    parsedHeaders);
            }
            catch (OperationCanceledException)
            {
                state.Error = "Request cancelled";
                PieDiagnostics.Verbose("[PieHttpBridge] Request cancelled");
            }
            catch (WebException ex)
            {
                var errorBody = TryReadWebExceptionBody(ex);
                state.Error = string.IsNullOrWhiteSpace(errorBody) ? ex.Message : $"HTTP error: {errorBody}";
                PieDiagnostics.Error(
                    $"[PieHttpBridge] Request error: {normalizedMethod} {url} failed: {ex.Message} body={ClipForLog(errorBody)}");
            }
            catch (Exception ex)
            {
                state.Error = ex.Message;
                PieDiagnostics.Error($"[PieHttpBridge] Request error: {normalizedMethod} {url} failed: {ex.Message}");
            }
            finally
            {
                state.IsComplete = true;
            }
        }

        private static async Task WriteHttpRequestAsync(
            Stream stream,
            Uri uri,
            string method,
            string body,
            string bearerToken,
            Dictionary<string, string> parsedHeaders,
            CancellationToken cancellationToken)
        {
            var path = string.IsNullOrEmpty(uri.PathAndQuery) ? "/" : uri.PathAndQuery;
            var payload = !string.IsNullOrEmpty(body) && !string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
                ? Encoding.UTF8.GetBytes(body)
                : null;

            var request = new StringBuilder();
            request.Append(method).Append(' ').Append(path).Append(" HTTP/1.1\r\n");
            request.Append("Host: ").Append(uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}").Append("\r\n");
            request.Append("Accept: text/event-stream\r\n");
            request.Append("Accept-Encoding: identity\r\n");
            request.Append("Connection: close\r\n");
            request.Append("User-Agent: pie-unity-native-stream/").Append(PieUnityCapabilitiesConstants.Version).Append("\r\n");

            if (!string.IsNullOrEmpty(bearerToken))
            {
                var token = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? bearerToken.Substring(7)
                    : bearerToken;
                request.Append("Authorization: Bearer ").Append(token).Append("\r\n");
            }

            foreach (var kv in parsedHeaders)
            {
                if (string.Equals(kv.Key, "Authorization", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(kv.Key, "Host", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(kv.Key, "Connection", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(kv.Key, "Accept-Encoding", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(kv.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                    continue;

                request.Append(kv.Key).Append(": ").Append(kv.Value).Append("\r\n");
            }

            if (payload != null)
            {
                if (!parsedHeaders.ContainsKey("Content-Type"))
                    request.Append("Content-Type: application/json\r\n");
                request.Append("Content-Length: ").Append(payload.Length).Append("\r\n");
            }

            request.Append("\r\n");

            var headerBytes = Encoding.ASCII.GetBytes(request.ToString());
            await stream.WriteAsync(headerBytes, 0, headerBytes.Length, cancellationToken);
            if (payload != null)
                await stream.WriteAsync(payload, 0, payload.Length, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        private static async Task ReadHttpResponseAsync(Stream stream, RequestState state, CancellationToken cancellationToken)
        {
            var statusLine = await ReadAsciiLineAsync(stream, cancellationToken);
            if (string.IsNullOrEmpty(statusLine))
                throw new IOException("HTTP response was empty");

            var statusParts = statusLine.Split(' ');
            if (statusParts.Length < 2 || !int.TryParse(statusParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var statusCode))
                throw new IOException("HTTP status line was invalid: " + statusLine);

            state.StatusCode = statusCode;
            PieDiagnostics.Info($"[PieHttpBridge] Response headers [{state.Id}] status={state.StatusCode}");

            var headers = await ReadHeadersAsync(stream, cancellationToken);
            var isChunked = headers.TryGetValue("transfer-encoding", out var transferEncoding)
                && transferEncoding.IndexOf("chunked", StringComparison.OrdinalIgnoreCase) >= 0;
            long contentLength = 0;
            var hasContentLength = headers.TryGetValue("content-length", out var contentLengthText)
                && long.TryParse(contentLengthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out contentLength);

            if (statusCode >= 400)
            {
                var errorBody = isChunked
                    ? await ReadChunkedTextBodyAsync(stream, cancellationToken)
                    : hasContentLength
                        ? await ReadFixedTextBodyAsync(stream, contentLength, cancellationToken)
                        : await ReadBodyUntilCloseTextAsync(stream, cancellationToken);
                state.Error = $"HTTP {statusCode}: {errorBody}";
                return;
            }

            if (isChunked)
            {
                await ReadChunkedSseBodyAsync(stream, state, cancellationToken);
                return;
            }

            if (hasContentLength)
            {
                await ReadFixedLengthSseBodyAsync(stream, state, contentLength, cancellationToken);
                return;
            }

            await ReadUntilCloseSseBodyAsync(stream, state, cancellationToken);
        }

        private static async Task<Dictionary<string, string>> ReadHeadersAsync(Stream stream, CancellationToken cancellationToken)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (true)
            {
                var line = await ReadAsciiLineAsync(stream, cancellationToken);
                if (line == null || line.Length == 0)
                    break;

                var separator = line.IndexOf(':');
                if (separator <= 0)
                    continue;

                var key = line.Substring(0, separator).Trim();
                var value = line.Substring(separator + 1).Trim();
                headers[key] = value;
            }
            return headers;
        }

        private static async Task<string> ReadAsciiLineAsync(Stream stream, CancellationToken cancellationToken)
        {
            var bytes = new List<byte>(128);
            var buffer = new byte[1];

            while (true)
            {
                var read = await stream.ReadAsync(buffer, 0, 1, cancellationToken);
                if (read <= 0)
                {
                    if (bytes.Count == 0)
                        return null;
                    break;
                }

                if (buffer[0] == (byte)'\n')
                    break;

                bytes.Add(buffer[0]);
            }

            if (bytes.Count > 0 && bytes[bytes.Count - 1] == (byte)'\r')
                bytes.RemoveAt(bytes.Count - 1);

            return Encoding.ASCII.GetString(bytes.ToArray());
        }

        private static async Task ReadChunkedSseBodyAsync(Stream stream, RequestState state, CancellationToken cancellationToken)
        {
            var buffer = new byte[16 * 1024];
            while (true)
            {
                var sizeLine = await ReadAsciiLineAsync(stream, cancellationToken);
                if (string.IsNullOrWhiteSpace(sizeLine))
                    continue;

                var sizeToken = sizeLine.Split(';')[0].Trim();
                var chunkSize = int.Parse(sizeToken, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (chunkSize == 0)
                {
                    while (true)
                    {
                        var trailer = await ReadAsciiLineAsync(stream, cancellationToken);
                        if (string.IsNullOrEmpty(trailer))
                            break;
                    }
                    break;
                }

                var remaining = chunkSize;
                while (remaining > 0)
                {
                    var read = await stream.ReadAsync(buffer, 0, Math.Min(buffer.Length, remaining), cancellationToken);
                    if (read <= 0)
                        throw new IOException("Chunked response ended unexpectedly");
                    AppendBodyBytes(state, buffer, read);
                    remaining -= read;
                }

                await ConsumeExpectedCrlfAsync(stream, cancellationToken);
            }
        }

        private static async Task ReadFixedLengthSseBodyAsync(Stream stream, RequestState state, long contentLength, CancellationToken cancellationToken)
        {
            var remaining = contentLength;
            var buffer = new byte[16 * 1024];
            while (remaining > 0)
            {
                var read = await stream.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, remaining), cancellationToken);
                if (read <= 0)
                    break;
                AppendBodyBytes(state, buffer, read);
                remaining -= read;
            }
        }

        private static async Task ReadUntilCloseSseBodyAsync(Stream stream, RequestState state, CancellationToken cancellationToken)
        {
            var buffer = new byte[16 * 1024];
            while (true)
            {
                var read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (read <= 0)
                    break;
                AppendBodyBytes(state, buffer, read);
            }
        }

        private static async Task<string> ReadChunkedTextBodyAsync(Stream stream, CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            var readBuffer = new byte[8 * 1024];
            while (true)
            {
                var sizeLine = await ReadAsciiLineAsync(stream, cancellationToken);
                if (string.IsNullOrWhiteSpace(sizeLine))
                    continue;

                var sizeToken = sizeLine.Split(';')[0].Trim();
                var chunkSize = int.Parse(sizeToken, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (chunkSize == 0)
                {
                    while (true)
                    {
                        var trailer = await ReadAsciiLineAsync(stream, cancellationToken);
                        if (string.IsNullOrEmpty(trailer))
                            break;
                    }
                    break;
                }

                var remaining = chunkSize;
                while (remaining > 0)
                {
                    var read = await stream.ReadAsync(readBuffer, 0, Math.Min(readBuffer.Length, remaining), cancellationToken);
                    if (read <= 0)
                        throw new IOException("Chunked response ended unexpectedly");
                    buffer.Write(readBuffer, 0, read);
                    remaining -= read;
                }

                await ConsumeExpectedCrlfAsync(stream, cancellationToken);
            }

            return Encoding.UTF8.GetString(buffer.ToArray());
        }

        private static async Task<string> ReadFixedTextBodyAsync(Stream stream, long contentLength, CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            var readBuffer = new byte[8 * 1024];
            var remaining = contentLength;
            while (remaining > 0)
            {
                var read = await stream.ReadAsync(readBuffer, 0, (int)Math.Min(readBuffer.Length, remaining), cancellationToken);
                if (read <= 0)
                    break;
                buffer.Write(readBuffer, 0, read);
                remaining -= read;
            }
            return Encoding.UTF8.GetString(buffer.ToArray());
        }

        private static async Task<string> ReadBodyUntilCloseTextAsync(Stream stream, CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            var readBuffer = new byte[8 * 1024];
            while (true)
            {
                var read = await stream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken);
                if (read <= 0)
                    break;
                buffer.Write(readBuffer, 0, read);
            }
            return Encoding.UTF8.GetString(buffer.ToArray());
        }

        private static async Task ConsumeExpectedCrlfAsync(Stream stream, CancellationToken cancellationToken)
        {
            var crlf = new byte[2];
            var offset = 0;
            while (offset < 2)
            {
                var read = await stream.ReadAsync(crlf, offset, 2 - offset, cancellationToken);
                if (read <= 0)
                    throw new IOException("HTTP stream ended before chunk terminator");
                offset += read;
            }
        }

        private static void AppendBodyBytes(RequestState state, byte[] buffer, int count)
        {
            state.ReceiveDataCallCount++;
            if (state.ReceiveDataCallCount <= 8)
                PieDiagnostics.Info($"[PieHttpBridge] Native stream [{state.Id}] chunk={state.ReceiveDataCallCount} bytes={count}");

            for (var i = 0; i < count; i++)
            {
                var value = buffer[i];
                if (value == (byte)'\n')
                {
                    FlushByteLineBuffer(state);
                    continue;
                }

                state.ByteLineBuffer.Add(value);
            }
        }

        private static void FlushByteLineBuffer(RequestState state)
        {
            if (state.ByteLineBuffer.Count == 0)
                return;

            var count = state.ByteLineBuffer.Count;
            if (state.ByteLineBuffer[count - 1] == (byte)'\r')
                count--;

            var bytes = state.ByteLineBuffer.ToArray();
            var line = count > 0 ? Encoding.UTF8.GetString(bytes, 0, count) : string.Empty;
            state.ByteLineBuffer.Clear();
            EmitBufferedLine(state, line, clearBuffer: false);
        }

        private static void StartUnityWebRequest(
            RequestState state,
            string method,
            string url,
            string body,
            string bearerToken,
            string headersJson)
        {
            try
            {
                var normalizedMethod = string.IsNullOrWhiteSpace(method) ? "POST" : method.ToUpperInvariant();
                var parsedHeaders = ParseHeaders(headersJson);
                var request = new UnityWebRequest(url, normalizedMethod);
                var handler = new DownloadHandlerBuffer();
                request.downloadHandler = handler;
                request.disposeDownloadHandlerOnDispose = true;
                request.disposeUploadHandlerOnDispose = true;
                request.useHttpContinue = false;
                request.timeout = 180;

                foreach (var kv in parsedHeaders)
                {
                    if (string.Equals(kv.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                        continue;
                    request.SetRequestHeader(kv.Key, kv.Value);
                }

                if (!parsedHeaders.ContainsKey("Accept-Encoding"))
                    request.SetRequestHeader("Accept-Encoding", "identity");

                if (!string.IsNullOrEmpty(bearerToken))
                {
                    var token = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? bearerToken.Substring(7)
                        : bearerToken;
                    request.SetRequestHeader("Authorization", "Bearer " + token);
                }

                if (!string.IsNullOrEmpty(body) && !string.Equals(normalizedMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    var payload = Encoding.UTF8.GetBytes(body);
                    request.uploadHandler = new UploadHandlerRaw(payload);
                    request.SetRequestHeader("Content-Type", "application/json");
                }

                if (!parsedHeaders.ContainsKey("Accept"))
                    request.SetRequestHeader("Accept", "text/event-stream");

                state.NativeRequest = request;
                state.Cts.Token.Register(() =>
                {
                    try
                    {
                        request.Abort();
                    }
                    catch
                    {
                        // Ignore cancellation races during reload/disposal.
                    }
                });

                var operation = request.SendWebRequest();
                operation.completed += _ =>
                {
                    CompleteUnityWebRequest(state);
                };
                PieDiagnostics.Info($"[PieHttpBridge] UnityWebRequest started [{state.Id}] {normalizedMethod} {url}");
            }
            catch (Exception ex)
            {
                state.Error = ex.Message;
                state.StatusCode = 0;
                state.IsComplete = true;
                PieDiagnostics.Error($"[PieHttpBridge] Failed to start UnityWebRequest: {method} {url} failed: {ex.Message}");
            }
        }

        private static void CompleteUnityWebRequest(RequestState state)
        {
            try
            {
                DrainUnityWebRequest(state, flushTail: true);

                var request = state.NativeRequest;
                if (request != null)
                {
                    if (state.StatusCode < 0)
                    {
                        var responseCode = request.responseCode;
                        state.StatusCode = responseCode > 0 ? (int)responseCode : 0;
                    }
                    PieDiagnostics.Info($"[PieHttpBridge] UnityWebRequest completed [{state.Id}] status={state.StatusCode} result={request.result}");

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        var responseText = request.downloadHandler != null ? request.downloadHandler.text ?? "" : "";
                        state.Error = !string.IsNullOrWhiteSpace(responseText)
                            ? $"HTTP {state.StatusCode}: {responseText}"
                            : request.error ?? "UnityWebRequest failed";
                    }
                }
            }
            catch (Exception ex)
            {
                state.Error = ex.Message;
                if (state.StatusCode < 0)
                    state.StatusCode = 0;
                PieDiagnostics.Warning($"[PieHttpBridge] CompleteUnityWebRequest failed: {ex.Message}");
            }
            finally
            {
                state.IsComplete = true;
                try
                {
                    state.NativeRequest?.Dispose();
                }
                catch
                {
                    // Ignore dispose races.
                }
                state.NativeRequest = null;
            }
        }

        private static void DrainUnityWebRequest(RequestState state, bool flushTail = false)
        {
            var request = state.NativeRequest;
            if (request == null)
                return;

            if (state.StatusCode < 0 && request.responseCode > 0)
            {
                state.StatusCode = (int)request.responseCode;
                PieDiagnostics.Info($"[PieHttpBridge] Response headers [{state.Id}] status={state.StatusCode}");
            }

            if (!(request.downloadHandler is DownloadHandlerBuffer buffer))
                return;

            var text = buffer.text ?? string.Empty;
            if (text.Length < state.ProcessedTextLength)
            {
                state.ProcessedTextLength = 0;
                state.TextLineBuffer.Length = 0;
            }

            if (text.Length > state.ProcessedTextLength)
            {
                var delta = text.Substring(state.ProcessedTextLength);
                state.ProcessedTextLength = text.Length;

                state.ReceiveDataCallCount++;
                if (state.ReceiveDataCallCount <= 6)
                    PieDiagnostics.Info($"[PieHttpBridge] Drain [{state.Id}] call={state.ReceiveDataCallCount} chars={delta.Length} downloadedBytes={request.downloadedBytes}");

                AppendTextChunk(state, delta, flushTail: false);
            }

            if (flushTail && state.TextLineBuffer.Length > 0)
                EmitBufferedLine(state, state.TextLineBuffer.ToString(), clearBuffer: true);
        }

        private static void AppendTextChunk(RequestState state, string chunk, bool flushTail)
        {
            for (var i = 0; i < chunk.Length; i++)
            {
                var ch = chunk[i];
                if (ch == '\n')
                {
                    EmitBufferedLine(state, state.TextLineBuffer.ToString(), clearBuffer: true);
                    continue;
                }

                state.TextLineBuffer.Append(ch);
            }

            if (flushTail && state.TextLineBuffer.Length > 0)
                EmitBufferedLine(state, state.TextLineBuffer.ToString(), clearBuffer: true);
        }

        private static void EmitBufferedLine(RequestState state, string rawLine, bool clearBuffer)
        {
            var line = rawLine;
            if (line.EndsWith("\r", StringComparison.Ordinal))
                line = line.Substring(0, line.Length - 1);

            if (clearBuffer)
                state.TextLineBuffer.Length = 0;

            if (!string.IsNullOrEmpty(line) && state.LoggedLineCount < 8)
            {
                state.FirstLineLogged = true;
                state.LoggedLineCount++;
                PieDiagnostics.Info($"[PieHttpBridge] Line [{state.Id}] #{state.LoggedLineCount} {ClipForLog(line)}");
            }

            state.Lines.Enqueue(line);
        }

        private static async Task RunHttpClientRequestAsync(
            RequestState state,
            string method,
            string url,
            string body,
            string bearerToken,
            Dictionary<string, string> parsedHeaders)
        {
            try
            {
                using var request = new HttpRequestMessage(new HttpMethod(method), url);

                // Auth header
                if (!string.IsNullOrEmpty(bearerToken))
                {
                    var token = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? bearerToken.Substring(7)
                        : bearerToken;
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                foreach (var kv in parsedHeaders)
                {
                    if (string.Equals(kv.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (string.Equals(kv.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                        continue;

                    request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                }

                if (!string.IsNullOrEmpty(body))
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    state.Cts.Token);
                state.ManagedResponse = response;

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    var contentType = response.Content?.Headers?.ContentType?.ToString() ?? "(none)";
                    state.StatusCode = (int)response.StatusCode;
                    state.Error = $"HTTP {(int)response.StatusCode}: {errorBody}";
                    PieDiagnostics.Warning(
                        $"[PieHttpBridge] Request failed: {method} {url} status={(int)response.StatusCode} contentType={contentType} body={ClipForLog(errorBody)}");
                    return;
                }

                state.StatusCode = (int)response.StatusCode;

                using var stream = await response.Content.ReadAsStreamAsync();
                state.ManagedStream = stream;
                using var reader = new StreamReader(stream, Encoding.UTF8);

                while (!reader.EndOfStream && !state.Cts.Token.IsCancellationRequested)
                {
                    var line = await ReadLineWithCancellationAsync(reader, state);
                    if (line != null)
                    {
                        state.Lines.Enqueue(line);
                    }
                }
            }
            finally
            {
                try { state.ManagedStream?.Dispose(); } catch { }
                try { state.ManagedResponse?.Dispose(); } catch { }
                state.ManagedStream = null;
                state.ManagedResponse = null;
            }
        }

        private static async Task<string> ReadLineWithCancellationAsync(StreamReader reader, RequestState state)
        {
            var readTask = reader.ReadLineAsync();
            if (readTask.IsCompleted)
                return await readTask;

            var cancelSignal = new TaskCompletionSource<bool>();
            using var cancelRegistration = state.Cts.Token.Register(() =>
            {
                cancelSignal.TrySetResult(true);
                try { state.ManagedStream?.Dispose(); } catch { }
                try { state.ManagedResponse?.Dispose(); } catch { }
            });
            var cancelTask = cancelSignal.Task;
            var completed = await Task.WhenAny(readTask, cancelTask);
            if (completed == readTask)
                return await readTask;

            throw new OperationCanceledException(state.Cts.Token);
        }

        private static async Task RunHttpWebRequestAsync(
            RequestState state,
            string method,
            string url,
            string body,
            string bearerToken,
            Dictionary<string, string> parsedHeaders)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.ProtocolVersion = HttpVersion.Version10;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Timeout = (int)TimeSpan.FromSeconds(180).TotalMilliseconds;
            request.ReadWriteTimeout = (int)TimeSpan.FromSeconds(180).TotalMilliseconds;
            request.KeepAlive = false;
            request.Proxy = null;
            request.ServicePoint.Expect100Continue = false;

            if (!string.IsNullOrEmpty(bearerToken))
            {
                var token = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? bearerToken.Substring(7)
                    : bearerToken;
                request.Headers[HttpRequestHeader.Authorization] = $"Bearer {token}";
            }

            foreach (var kv in parsedHeaders)
            {
                if (string.Equals(kv.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.Equals(kv.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    request.ContentType = kv.Value;
                    continue;
                }

                if (string.Equals(kv.Key, "Accept", StringComparison.OrdinalIgnoreCase))
                {
                    request.Accept = kv.Value;
                    continue;
                }

                request.Headers[kv.Key] = kv.Value;
            }

            if (string.IsNullOrEmpty(request.Accept))
                request.Accept = "application/json";

            if (!string.IsNullOrEmpty(body) && !string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(request.ContentType))
                    request.ContentType = "application/json";

                var payload = Encoding.UTF8.GetBytes(body);
                using var requestStream = await request.GetRequestStreamAsync();
                await requestStream.WriteAsync(payload, 0, payload.Length, state.Cts.Token);
            }

            using var response = (HttpWebResponse)await request.GetResponseAsync();
            state.StatusCode = (int)response.StatusCode;

            using var stream = response.GetResponseStream();
            if (stream == null)
            {
                state.Error = "Response stream was null";
                return;
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            while (!reader.EndOfStream && !state.Cts.Token.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line != null)
                {
                    state.Lines.Enqueue(line);
                }
            }
        }

        private static bool ShouldFallbackToHttpClient(Exception ex)
        {
            var message = ex.Message ?? string.Empty;
            return message.IndexOf("invalid or unrecognized response", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("SSL connection could not be established", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("authentication failed", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string TryReadWebExceptionBody(WebException ex)
        {
            try
            {
                if (!(ex.Response is HttpWebResponse response))
                    return string.Empty;

                using var stream = response.GetResponseStream();
                if (stream == null)
                    return string.Empty;
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ClipForLog(string text, int maxLength = 240)
        {
            if (string.IsNullOrEmpty(text))
                return "(empty)";

            text = text.Replace("\r", "\\r").Replace("\n", "\\n");
            return text.Length <= maxLength
                ? text
                : text.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// Minimal JSON object parser for header dictionaries.
        /// Handles flat JSON objects with string values and proper escaping.
        /// </summary>
        private static Dictionary<string, string> ParseHeaders(string headersJson)
        {
            if (string.IsNullOrEmpty(headersJson))
                return new Dictionary<string, string>();

            try
            {
                return SimpleJsonParse(headersJson);
            }
            catch (Exception ex)
            {
                PieDiagnostics.Warning($"[PieHttpBridge] Failed to parse headersJson: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var builder = new StringBuilder(value.Length + 16);
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
                        if (ch < 0x20)
                            builder.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            builder.Append(ch);
                        break;
                }
            }
            return builder.ToString();
        }

        private static string JsonString(string value)
        {
            return "\"" + EscapeJson(value ?? string.Empty) + "\"";
        }

        private static string BuildFetchTextResponseJson(
            int status,
            string statusText,
            bool ok,
            string url,
            string contentType,
            string body)
        {
            var builder = new StringBuilder(256 + (body?.Length ?? 0));
            builder.Append("{");
            builder.Append("\"status\":").Append(status.ToString(CultureInfo.InvariantCulture)).Append(",");
            builder.Append("\"statusText\":").Append(JsonString(statusText)).Append(",");
            builder.Append("\"ok\":").Append(ok ? "true" : "false").Append(",");
            builder.Append("\"url\":").Append(JsonString(url)).Append(",");
            builder.Append("\"headers\":{\"content-type\":").Append(JsonString(contentType)).Append("},");
            builder.Append("\"body\":").Append(JsonString(body));
            builder.Append("}");
            return builder.ToString();
        }

        private static Dictionary<string, string> SimpleJsonParse(string json)
        {
            var result = new Dictionary<string, string>();
            json = json.Trim();
            if (!json.StartsWith("{") || !json.EndsWith("}")) return result;

            int index = 1;

            void SkipWhitespace()
            {
                while (index < json.Length && char.IsWhiteSpace(json[index]))
                    index++;
            }

            string ParseString()
            {
                if (index >= json.Length || json[index] != '"')
                    throw new FormatException("Expected string");

                index++; // Skip opening quote
                var sb = new StringBuilder();

                while (index < json.Length)
                {
                    char ch = json[index++];
                    if (ch == '"')
                        return sb.ToString();

                    if (ch != '\\')
                    {
                        sb.Append(ch);
                        continue;
                    }

                    if (index >= json.Length)
                        throw new FormatException("Invalid escape sequence");

                    char escaped = json[index++];
                    switch (escaped)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (index + 4 > json.Length)
                                throw new FormatException("Invalid unicode escape");
                            sb.Append((char)Convert.ToInt32(json.Substring(index, 4), 16));
                            index += 4;
                            break;
                        default:
                            throw new FormatException($"Unsupported escape sequence: \\{escaped}");
                    }
                }

                throw new FormatException("Unterminated string");
            }

            string ParseLiteral()
            {
                int start = index;
                while (index < json.Length && json[index] != ',' && json[index] != '}')
                    index++;
                return json.Substring(start, index - start).Trim();
            }

            while (index < json.Length)
            {
                SkipWhitespace();
                if (index < json.Length && json[index] == '}')
                    break;

                string key = ParseString();

                SkipWhitespace();
                if (index >= json.Length || json[index] != ':')
                    throw new FormatException("Expected ':' after key");
                index++;

                SkipWhitespace();
                string value = index < json.Length && json[index] == '"'
                    ? ParseString()
                    : ParseLiteral();

                result[key] = value;

                SkipWhitespace();
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                    continue;
                }

                if (index < json.Length && json[index] == '}')
                    break;
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // State
        // ─────────────────────────────────────────────────────────────────────

        private class RequestState
        {
            public readonly int Id;
            public readonly ConcurrentQueue<string> Lines = new ConcurrentQueue<string>();
            public readonly CancellationTokenSource Cts = new CancellationTokenSource();
            public Task WorkerTask = null;
            public volatile bool IsComplete = false;
            public volatile int StatusCode = -1;
            public volatile bool StatusPushed = false;
            public volatile bool TerminalPushed = false;
            public string Error = null;
            public UnityWebRequest NativeRequest = null;
            public TcpClient NativeTcpClient = null;
            public Stream NativeStream = null;
            public HttpResponseMessage ManagedResponse = null;
            public Stream ManagedStream = null;
            public bool FirstLineLogged = false;
            public int ReceiveDataCallCount = 0;
            public int LoggedLineCount = 0;
            public int ProcessedTextLength = 0;
            public readonly StringBuilder TextLineBuffer = new StringBuilder(4096);
            public readonly List<byte> ByteLineBuffer = new List<byte>(4096);

            public RequestState(int id) { Id = id; }
        }
    }
}
