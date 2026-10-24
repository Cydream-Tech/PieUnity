using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Pie
{
    /// <summary>
    /// Player-safe HTTP for IL2CPP builds.
    ///
    /// System.Net.Http.HttpClient is unreliable in IL2CPP player builds:
    /// multipart uploads hang indefinitely (dogfood 3.0: voice uploads never
    /// completed and even CancellationTokenSource.CancelAfter never fired),
    /// while the same flow works in the editor. Plain GETs have been observed
    /// to work, but the boundary is undocumented — host code that must run in
    /// a player should route ALL HTTP through this helper instead. It drives
    /// UnityWebRequest on the main thread (the supported stack on every
    /// player platform) with:
    ///   - exactly-once completion (also on play-mode exit / quit),
    ///   - cancellation that actually aborts the request,
    ///   - a wall-clock timeout backstop (UnityWebRequest.timeout semantics
    ///     vary by platform),
    ///   - a clear edit-mode fast-fail instead of a silent hang.
    /// </summary>
    public static class PieSafeHttpClient
    {
        /// <summary>Completed response. Non-2xx statuses are returned, not
        /// thrown; call EnsureSuccess() or use the Get*/Post* helpers.</summary>
        public sealed class PieSafeHttpResponse
        {
            public int StatusCode { get; internal set; }
            public byte[] Body { get; internal set; }
            public string ContentType { get; internal set; }

            public string GetText()
            {
                if (Body == null || Body.Length == 0)
                    return "";
                try { return System.Text.Encoding.UTF8.GetString(Body); }
                catch { return ""; }
            }

            public PieSafeHttpResponse EnsureSuccess()
            {
                if (StatusCode < 200 || StatusCode >= 300)
                    throw new PieHttpException(StatusCode, GetText());
                return this;
            }
        }

        /// <summary>GET that throws PieHttpException on non-2xx.</summary>
        public static async Task<byte[]> GetBytesAsync(
            string url,
            IDictionary<string, string> headers = null,
            int timeoutSeconds = 120,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = await SendAsync("GET", url, null, null, headers, timeoutSeconds, cancellationToken)
                .ConfigureAwait(false);
            return response.EnsureSuccess().Body ?? new byte[0];
        }

        /// <summary>GET decoded as UTF-8; throws PieHttpException on non-2xx.</summary>
        public static async Task<string> GetStringAsync(
            string url,
            IDictionary<string, string> headers = null,
            int timeoutSeconds = 120,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = await SendAsync("GET", url, null, null, headers, timeoutSeconds, cancellationToken)
                .ConfigureAwait(false);
            return response.EnsureSuccess().GetText();
        }

        /// <summary>POST a raw body (e.g. JSON) and return the response as
        /// UTF-8 text; throws PieHttpException on non-2xx. Use SendAsync when
        /// the caller needs the raw bytes or the response content type.</summary>
        public static async Task<string> PostStringAsync(
            string url,
            string body,
            string contentType,
            IDictionary<string, string> headers = null,
            int timeoutSeconds = 120,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var bytes = string.IsNullOrEmpty(body) ? new byte[0] : System.Text.Encoding.UTF8.GetBytes(body);
            var response = await SendAsync("POST", url, bytes, contentType, headers, timeoutSeconds, cancellationToken)
                .ConfigureAwait(false);
            return response.EnsureSuccess().GetText();
        }

        /// <summary>Multipart form upload (UnityWebRequest.Post with
        /// IMultipartFormSection); returns the response body as UTF-8 text and
        /// throws PieHttpException on non-2xx. This is the only multipart
        /// stack that works in IL2CPP player builds.</summary>
        public static async Task<string> UploadMultipartAsync(
            string url,
            IList<IMultipartFormSection> sections,
            IDictionary<string, string> headers = null,
            int timeoutSeconds = 120,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (sections == null || sections.Count == 0)
                throw new ArgumentException("At least one multipart section is required.", nameof(sections));
            var owned = new List<IMultipartFormSection>(sections);
            var response = await SendCoreAsync(
                () => UnityWebRequest.Post(url, owned),
                headers,
                timeoutSeconds,
                cancellationToken).ConfigureAwait(false);
            return response.EnsureSuccess().GetText();
        }

        /// <summary>Raw-method send. Returns the completed response without
        /// throwing on non-2xx so callers can inspect status/content type
        /// (e.g. binary downloads where JSON bodies signal errors).</summary>
        public static Task<PieSafeHttpResponse> SendAsync(
            string method,
            string url,
            byte[] body,
            string contentType,
            IDictionary<string, string> headers,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("url is required.", nameof(url));
            var verb = string.IsNullOrWhiteSpace(method) ? "GET" : method.Trim().ToUpperInvariant();
            return SendCoreAsync(
                () => CreateRequest(verb, url, body, contentType),
                headers,
                timeoutSeconds,
                cancellationToken);
        }

        private static UnityWebRequest CreateRequest(string verb, string url, byte[] body, string contentType)
        {
            UnityWebRequest request = null;
            try
            {
                if (verb == "GET")
                {
                    request = UnityWebRequest.Get(url);
                }
                else
                {
                    request = new UnityWebRequest(url, verb);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    if (body != null)
                        request.uploadHandler = new UploadHandlerRaw(body);
                }
                if (body != null && !string.IsNullOrEmpty(contentType))
                    request.SetRequestHeader("Content-Type", contentType);
                return request;
            }
            catch
            {
                // A partially constructed request must not leak when handler
                // or header assignment throws.
                if (request != null)
                {
                    try { request.Dispose(); } catch { }
                }
                throw;
            }
        }

        private static Task<PieSafeHttpResponse> SendCoreAsync(
            Func<UnityWebRequest> requestFactory,
            IDictionary<string, string> headers,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<PieSafeHttpResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            var timeout = Math.Max(1, timeoutSeconds);

            // Ownership starts NOW, before the main-thread dispatch: a request
            // that is merely queued (pump destroyed before its action runs,
            // cancellation while queued) is still settled exactly once — by
            // the cancellation registration, the queued action, the coroutine,
            // or pump teardown, whichever comes first.
            var pending = new PendingRequest { Completion = tcs };
            lock (InFlightGate) { InFlight.Add(pending); }
            if (cancellationToken.CanBeCanceled)
            {
                var registration = cancellationToken.Register(() =>
                {
                    // Settlement is thread-safe, but resource ownership stays in
                    // InFlight until the coroutine/queued action or teardown
                    // performs main-thread cleanup (even if no next frame runs).
                    pending.Settle(null, new OperationCanceledException(), cancellationToken);
                });
                pending.SetCancellation(registration);
            }

            Action start = () =>
            {
                // All Unity API checks live inside the main-thread action
                // (callers may be on worker threads; Application.isPlaying is
                // main-thread-only).
                try
                {
                    if (pending.IsSettled)
                    {
                        // Cancelled (or torn down) while queued.
                        pending.Cleanup();
                        return;
                    }
                    // Request coroutines need the play-mode scheduler; edit-mode
                    // pumping only drains the action queue and would never
                    // advance them. Fail fast instead of hanging forever.
                    if (!Application.isPlaying)
                    {
                        pending.Cleanup();
                        pending.Settle(null, new InvalidOperationException(
                            "PieSafeHttpClient requires Play Mode (UnityWebRequest needs the play-mode scheduler)."), cancellationToken);
                        return;
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        pending.Cleanup();
                        pending.Settle(null, new OperationCanceledException(), cancellationToken);
                        return;
                    }
                    var pump = PieVoiceRuntimePump.Instance;
                    if (pump == null)
                    {
                        pending.Cleanup();
                        pending.Settle(null, new InvalidOperationException("Runtime pump is unavailable (shutting down?)."), cancellationToken);
                        return;
                    }

                    UnityWebRequest request = null;
                    try
                    {
                        request = requestFactory();
                        pending.Own(request);
                        if (pending.IsSettled) { pending.Cleanup(); return; }
                        request.timeout = timeout;
                        if (headers != null)
                        {
                            foreach (var pair in headers)
                            {
                                if (string.IsNullOrEmpty(pair.Key) || pair.Value == null)
                                    continue;
                                request.SetRequestHeader(pair.Key, pair.Value);
                            }
                        }
                    }
                    catch (Exception setupError)
                    {
                        pending.Cleanup();
                        pending.Settle(null, setupError, cancellationToken);
                        return;
                    }

                    if (pending.IsSettled)
                    {
                        pending.Cleanup();
                        return;
                    }
                    pump.StartCoroutine(SendRoutine(pending, timeout, cancellationToken));
                }
                catch (Exception ex)
                {
                    pending.Cleanup();
                    pending.Settle(null, ex, cancellationToken);
                }
            };
            try
            {
                if (!PieVoiceRuntimePump.TryRunOnMainThread(start))
                {
                    // Not queued, so no Unity resource exists. Safe on a worker.
                    RemoveInFlight(pending);
                    pending.Settle(null, new InvalidOperationException("Runtime pump is unavailable (shutting down?)."), cancellationToken);
                }
            }
            catch (Exception dispatchError)
            {
                // The action catches its own errors; this is dispatch/Ensure failure.
                RemoveInFlight(pending);
                pending.Settle(null, dispatchError, cancellationToken);
            }
            return tcs.Task;
        }

        private static void RemoveInFlight(PendingRequest pending)
        {
            lock (InFlightGate) { InFlight.Remove(pending); }
        }

        /// <summary>One queued-or-running request; Settle is exactly-once
        /// across cancellation, the queued action, coroutine completion, and
        /// pump teardown.</summary>
        private sealed class PendingRequest
        {
            public UnityWebRequest Request;
            public TaskCompletionSource<PieSafeHttpResponse> Completion;
            private CancellationTokenRegistration Cancellation;
            private readonly object _gate = new object();
            private int _settled;
            private bool _cleaned;

            public bool IsSettled => Volatile.Read(ref _settled) != 0;

            public void SetCancellation(CancellationTokenRegistration registration)
            {
                lock (_gate) { Cancellation = registration; }
                if (IsSettled) registration.Dispose();
            }

            // Main thread only. A factory may re-enter teardown before returning.
            public void Own(UnityWebRequest request)
            {
                if (_cleaned)
                {
                    try { request?.Abort(); } catch { }
                    try { request?.Dispose(); } catch { }
                }
                else Request = request;
            }

            // Main thread only. Settlement and resource cleanup are independent.
            public void Cleanup()
            {
                if (_cleaned) return;
                _cleaned = true;
                var request = Request;
                Request = null;
                try { request?.Abort(); } catch { }
                try { request?.Dispose(); } catch { }
                RemoveInFlight(this);
            }

            public void Settle(PieSafeHttpResponse response, Exception error, CancellationToken cancellationToken)
            {
                if (Interlocked.Exchange(ref _settled, 1) != 0) return;
                CancellationTokenRegistration registration;
                lock (_gate) { registration = Cancellation; }
                try { registration.Dispose(); } catch { }
                if (error is OperationCanceledException && cancellationToken.IsCancellationRequested)
                    Completion.TrySetCanceled(cancellationToken);
                else if (error != null)
                    Completion.TrySetException(error);
                else
                    Completion.TrySetResult(response);
            }
        }

        private static readonly object InFlightGate = new object();
        private static readonly List<PendingRequest> InFlight = new List<PendingRequest>();

        /// <summary>Pump teardown aborts and settles every queued or running
        /// request so no caller is left pending (play-mode exit, quit).</summary>
        internal static void OnPumpTeardown()
        {
            List<PendingRequest> pending;
            lock (InFlightGate)
            {
                pending = new List<PendingRequest>(InFlight);
                InFlight.Clear();
            }
            foreach (var p in pending)
            {
                p.Cleanup();
                p.Settle(null, new InvalidOperationException("Runtime pump shut down during the request."), CancellationToken.None);
            }
        }

        private static IEnumerator SendRoutine(
            PendingRequest pending,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            // Exactly-once completion: every terminal path funnels to the end;
            // nothing in this iterator may throw past the caller (a coroutine
            // exception would otherwise strand the awaiting task forever).
            var request = pending.Request;
            PieSafeHttpResponse response = null;
            Exception error = null;

            UnityWebRequestAsyncOperation op = null;
            try { op = request.SendWebRequest(); }
            catch (Exception sendError) { error = sendError; }

            if (error == null)
            {
                var startedAt = Time.realtimeSinceStartup;
                while (true)
                {
                    bool done;
                    try
                    {
                        // Polling getters live inside the try: a throw here
                        // must reach Settle, not escape the coroutine driver.
                        done = op.isDone;
                    }
                    catch (Exception pollError)
                    {
                        error = pollError;
                        break;
                    }
                    if (done) break;
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    // Wall-clock backstop: UnityWebRequest.timeout alone has
                    // platform-dependent semantics; never wait past it by much.
                    if (Time.realtimeSinceStartup - startedAt > timeoutSeconds + 5f)
                    {
                        error = new TimeoutException("HTTP " + request.method + " " + request.url + " timed out.");
                        break;
                    }
                    yield return null;
                }
            }

            if (error == null)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        error = new OperationCanceledException();
                    }
                    else if (request.result == UnityWebRequest.Result.ConnectionError
                        || request.result == UnityWebRequest.Result.DataProcessingError)
                    {
                        error = new InvalidOperationException(
                            "HTTP " + request.method + " " + request.url + " failed: " + request.error);
                    }
                    else
                    {
                        // ProtocolError (non-2xx) is a completed response: the
                        // body usually carries the actionable error detail.
                        response = new PieSafeHttpResponse
                        {
                            StatusCode = (int)request.responseCode,
                            Body = request.downloadHandler != null ? request.downloadHandler.data : null,
                            ContentType = request.GetResponseHeader("Content-Type") ?? "",
                        };
                    }
                }
                catch (Exception readError)
                {
                    error = readError;
                }
            }
            pending.Cleanup();
            pending.Settle(response, error, cancellationToken);
        }
    }

    /// <summary>Non-2xx response surfaced through EnsureSuccess()/helpers;
    /// carries the status code and a trimmed body for diagnostics.</summary>
    public class PieHttpException : Exception
    {
        public int StatusCode { get; private set; }
        public string ResponseBody { get; private set; }

        public PieHttpException(int statusCode, string responseBody)
            : base($"HTTP {statusCode}: {TrimResponseBody(responseBody)}")
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
