#if PIE_UNITY_SPLIT_SOURCES
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Pie
{
    public static class PieDevRpcServer
    {
        private static HttpListener _listener;
        private static CancellationTokenSource _cts;
        private static Task _loopTask;
        private static readonly object SyncRoot = new object();
        private static int _port = PieUnityCapabilitiesConstants.DefaultPort;
        private static string _authToken = "";
        private static volatile bool _domainReloadPending;
        private const int ScriptRunPollIntervalMs = 25;
        private const int ScriptRunMainThreadTimeoutMs = 5000;
        private const int HealthMainThreadTimeoutMs = 750;
        private const int ShutdownWaitMs = 750;

        [Serializable]
        private class ScriptRunStatusPayload
        {
            public string taskId;
            public string status;
            public bool done;
            public int totalTimeoutMs;
            public string errorCode;
            public string errorMessage;
        }

        public static int Port => _port;
        public static string AuthToken
        {
            get
            {
                lock (SyncRoot)
                {
                    if (string.IsNullOrWhiteSpace(_authToken))
                        _authToken = GenerateAuthToken();
                    return _authToken;
                }
            }
        }

        public static bool IsRunning
        {
            get
            {
                lock (SyncRoot)
                {
                    return _listener != null && _listener.IsListening;
                }
            }
        }

        public static void Start()
        {
            lock (SyncRoot)
            {
                if (_listener != null && _listener.IsListening)
                    return;

                try
                {
                    _cts?.Cancel();
                    _listener?.Close();
                    if (string.IsNullOrWhiteSpace(_authToken))
                        _authToken = GenerateAuthToken();

                    _cts = new CancellationTokenSource();
                    _listener = StartListener();
                    _loopTask = Task.Run(() => AcceptLoop(_cts.Token));
                    _domainReloadPending = false;
                    PieDiagnostics.Info($"[PieDevRpcServer] Listening on http://localhost:{Port}/");
                }
                catch (Exception ex)
                {
                    PieDiagnostics.Warning($"[PieDevRpcServer] Failed to start: {ex.Message}");
                }
            }
        }

        public static void Stop()
        {
            StopInternal("stop", false, ShutdownWaitMs);
        }

        public static void BeginDomainReloadShutdown()
        {
            _domainReloadPending = true;
            // Domain reload must stop all old-AppDomain workers before Unity tries to unload it.
            // Do not attempt transparent streaming resume here; PieChat records an interrupted
            // snapshot and reloads session state after the new AppDomain is ready.
            PieDiagnostics.Info("[PieDevRpcServer] before reload: stopping runtime services");
            StopInternal("domain_reload", true, ShutdownWaitMs);
        }

        private static void StopInternal(string reason, bool preserveReloadPending, int waitMs)
        {
            Task loopTask = null;
            lock (SyncRoot)
            {
                PieUnityCapabilitiesBootstrap.ShutdownAll();
                PieHttpBridge.CancelAllRequests();
                PieFileBridge.CancelAllRequests();
                try { _cts?.Cancel(); } catch { }
                try { _listener?.Close(); } catch { }

                _listener = null;
                _cts = null;
                loopTask = _loopTask;
                _loopTask = null;

                if (!preserveReloadPending)
                    _domainReloadPending = false;
            }

            if (loopTask != null)
            {
                try
                {
                    if (!loopTask.Wait(waitMs))
                        PieDiagnostics.Warning($"[PieDevRpcServer] Timed out waiting for accept loop to stop ({reason}).");
                }
                catch
                {
                    // Ignore shutdown races during domain reload.
                }
            }
        }

        private static async Task AcceptLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext context = null;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                        PieDiagnostics.Warning($"[PieDevRpcServer] Accept failed: {ex.Message}");
                    break;
                }

                _ = Task.Run(() => HandleContext(context), token);
            }
        }

        private static void HandleContext(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                if (request.Url == null)
                {
                    WriteJson(context.Response, 400, "{\"ok\":false,\"error\":\"Missing URL\"}");
                    return;
                }

                var path = request.Url.AbsolutePath ?? "/";
                if (string.Equals(path, "/health", StringComparison.Ordinal))
                {
                    string healthJson;
                    try
                    {
                        // Keep health as a bounded readiness probe. Do not call into PuerTS/JS
                        // or agent/session code here; doing so during reload can enter V8 while
                        // Unity is unloading the AppDomain and recreate the resume hang/crash.
                        healthJson = PieDevRpcDispatcher.InvokeSync(() => BuildHealthJson(true), HealthMainThreadTimeoutMs);
                    }
                    catch (Exception ex)
                    {
                        healthJson = BuildHealthJson(false, ex.Message);
                    }
                    WriteJson(context.Response, 200, healthJson);
                    return;
                }

                if (!IsAuthorized(request))
                {
                    WriteJson(context.Response, 401, BuildEnvelope("rpc", path, false, "null", "RPC_UNAUTHORIZED: missing or invalid X-Pie-Token.", "RPC_UNAUTHORIZED"));
                    return;
                }

                if (_domainReloadPending)
                {
                    WriteJson(context.Response, 503, BuildEnvelope("rpc", path, false, "null", "Unity is reloading scripts. Retry shortly."));
                    return;
                }

                if (string.Equals(path, "/instances", StringComparison.Ordinal))
                {
                    WriteJson(context.Response, 200, JsonUtility.ToJson(new PieUnityInstancesPayload
                    {
                        instances = PieUnityInstanceRegistry.ReadAll(),
                    }, true));
                    return;
                }

                if (string.Equals(path, "/manifest", StringComparison.Ordinal))
                {
                    var query = ParseQueryString(request.Url.Query);
                    query.TryGetValue("namespace", out var filterNamespace);
                    query.TryGetValue("name", out var filterName);
                    WriteJson(context.Response, 200, PieUnityCapabilityRegistry.BuildManifestJson(filterNamespace, filterName));
                    return;
                }

                if (path.StartsWith("/tool/", StringComparison.Ordinal))
                {
                    var toolName = Uri.UnescapeDataString(path.Substring("/tool/".Length));
                    var toolBody = ReadRequestBody(request);
                    var toolResponse = string.Equals(toolName, "unity_script_run", StringComparison.Ordinal)
                        ? HandleUnityScriptRunTool(toolBody, _cts != null ? _cts.Token : CancellationToken.None)
                        : PieDevRpcDispatcher.InvokeSync(() => BuildCapabilityEnvelope("tool", toolName, toolBody));
                    WriteJson(context.Response, 200, toolResponse);
                    return;
                }

                if (!path.StartsWith("/rpc/", StringComparison.Ordinal))
                {
                    WriteJson(context.Response, 404, "{\"ok\":false,\"error\":\"Not found\"}");
                    return;
                }

                var method = Uri.UnescapeDataString(path.Substring("/rpc/".Length));
                var body = ReadRequestBody(request);
                var responseJson = PieDevRpcDispatcher.InvokeSync(() => BuildCapabilityEnvelope("rpc", method, body));
                WriteJson(context.Response, 200, responseJson);
            }
            catch (ThreadAbortException)
            {
                if (_domainReloadPending)
                    PieDiagnostics.Info("[PieDevRpcServer] Request aborted during domain reload.");
                else
                    PieDiagnostics.Warning("[PieDevRpcServer] Request thread was aborted.");
                throw;
            }
            catch (Exception ex)
            {
                PieDiagnostics.Error($"[PieDevRpcServer] Request handling failed: {ex.Message}");
                WriteJson(context.Response, 500, $"{{\"ok\":false,\"error\":\"{EscapeJson(ex.Message)}\"}}");
            }
        }

        private static string BuildHealthJson(bool mainThreadResponsive, string mainThreadError = "")
        {
            var availability = mainThreadResponsive ? PieUnityAvailability.GetAvailabilityNoticeJson() : "";
            var bridgeReady = PieBridge.Instance != null && PieBridge.Instance.IsInitialized;
            var scriptHostReady = PieBridge.Instance != null && PieBridge.Instance.IsUnityScriptHostReady;
            var hostBridgeReady = PieBridge.Instance != null && PieBridge.Instance.IsRuntimeHostBridgeReady;
            var runtimeBridgeReady = PieBridge.Instance != null && PieBridge.Instance.IsRuntimeBridgeReady;
            var bridgeLastError = PieBridge.Instance != null ? (PieBridge.Instance.LastError ?? "") : (PieBridge.LastInitializationError ?? "");
            var bridgeDiagnostic = BuildBridgeDiagnostic(bridgeReady, bridgeLastError);
            var scriptHostDiagnostic = BuildScriptHostDiagnostic(bridgeReady, scriptHostReady, bridgeLastError);
            var activeFileRequests = CountActive(PieFileBridge.GetActiveRequestIds());
            var ready = !_domainReloadPending && mainThreadResponsive && bridgeReady && scriptHostReady && hostBridgeReady && runtimeBridgeReady;
            var registeredHostNamespaces = PieUnityCapabilityRegistry.GetRegisteredRuntimeHostNamespaces();
            var hostDiagnostics = PieUnityCapabilityRegistry.GetRuntimeHostDiagnostics();
            var snapshot = PieUnityInstanceRegistry.GetCurrentProcessSnapshot(PieUnityCapabilityRegistry.ProjectPath);
            var owner = snapshot != null ? snapshot.discoverableOwner : null;
            var playModeActive = mainThreadResponsive && IsPlayModeActive();
            var isPlayingOrWillChangePlaymode = mainThreadResponsive && IsPlayingOrWillChangePlaymode();
            var behaviorTreeActiveCount = mainThreadResponsive ? PieBehaviorTreeRuntime.GetActiveTreeCount() : 0;
            return JsonUtility.ToJson(new PieUnityHealthPayload
            {
                instanceId = owner != null ? (owner.instanceId ?? "") : PieUnityCapabilityRegistry.InstanceId,
                projectPath = owner != null ? (owner.projectPath ?? "") : PieUnityCapabilityRegistry.ProjectPath,
                projectName = PieUnityCapabilitiesBootstrap.ProductName,
                productName = PieUnityCapabilitiesBootstrap.UnityProductName ?? "",
                applicationIdentifier = PieUnityCapabilitiesBootstrap.ApplicationIdentifier ?? "",
                mode = owner != null ? (owner.mode ?? "") : PieUnityCapabilityRegistry.Mode,
                port = Port,
                running = IsRunning,
                ready = ready,
                domainReloadPending = _domainReloadPending,
                bridgeReady = bridgeReady,
                scriptHostReady = scriptHostReady,
                hostBridgeReady = hostBridgeReady,
                runtimeBridgeReady = runtimeBridgeReady,
                bridgeLastError = bridgeLastError,
                bridgeDiagnostic = bridgeDiagnostic,
                scriptHostDiagnostic = scriptHostDiagnostic,
                discoverableOwnerMode = owner != null ? (owner.mode ?? "") : "",
                runtimeActive = snapshot != null && snapshot.runtimeActive,
                editorActive = snapshot != null && snapshot.editorActive,
                editorSuppressedByRuntime = snapshot != null && snapshot.editorSuppressedByRuntime,
                playModeActive = playModeActive,
                isPlayingOrWillChangePlaymode = isPlayingOrWillChangePlaymode,
                behaviorTreeActiveCount = behaviorTreeActiveCount,
                registeredHostCount = registeredHostNamespaces.Length,
                registeredHostNamespaces = registeredHostNamespaces,
                hostCapabilityCount = PieUnityCapabilityRegistry.GetRuntimeHostCapabilityCount(),
                hostDiagnostics = hostDiagnostics,
                mainThreadResponsive = mainThreadResponsive,
                activeHttpRequests = PieHttpBridge.ActiveRequestCount,
                activeFileRequests = activeFileRequests,
                activeScriptRuns = 0,
                availability = mainThreadResponsive
                    ? availability
                    : $"{{\"status\":\"temporarily_unavailable\",\"reason\":\"main_thread_timeout\",\"message\":\"Timed out waiting for Unity main thread: {EscapeJson(mainThreadError)}\"}}",
            });
        }

        private static bool IsPlayModeActive()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorApplication.isPlaying;
#else
            return Application.isPlaying;
#endif
        }

        private static bool IsPlayingOrWillChangePlaymode()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
#else
            return Application.isPlaying;
#endif
        }

        private static string BuildBridgeDiagnostic(bool bridgeReady, string bridgeLastError)
        {
            if (bridgeReady)
                return "Pie JS bridge is initialized.";

            if (!string.IsNullOrWhiteSpace(bridgeLastError))
                return "Pie JS bridge failed to initialize: " + bridgeLastError;

            if (PieBridge.Instance == null)
                return "PieBridge instance is not available. Open Tools > Pie > Pie Chat or add a PieRunner, then retry.";

            return "PieBridge exists but is not initialized. Check PuerTS 2.2.2 V8 runtime, the packaged Resources/pie/core.bytes TextAsset, and the Unity log.";
        }

        private static string BuildScriptHostDiagnostic(bool bridgeReady, bool scriptHostReady, string bridgeLastError)
        {
            if (!bridgeReady)
            {
                if (!string.IsNullOrWhiteSpace(bridgeLastError))
                    return "Unity script host is unavailable because the Pie JS bridge failed to initialize: " + bridgeLastError;
                return "Unity script host is unavailable because the Pie JS bridge is not initialized.";
            }

            if (scriptHostReady)
                return "Unity script host is installed and ready.";

            return "Unity script host is unavailable even though the Pie JS bridge initialized. Check the packaged runtime bundle and Unity log.";
        }

        private static int CountActive(System.Collections.Generic.IEnumerable<int> ids)
        {
            var count = 0;
            if (ids == null)
                return 0;
            foreach (var _ in ids)
                count++;
            return count;
        }

        private static HttpListener StartListener()
        {
            for (var port = PieUnityCapabilitiesConstants.DefaultPort; port <= PieUnityCapabilitiesConstants.MaxPort; port++)
            {
                try
                {
                    var listener = new HttpListener();
                    listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                    listener.Prefixes.Add($"http://localhost:{port}/");
                    listener.Start();
                    _port = port;
                    return listener;
                }
                catch
                {
                    // Try next port.
                }
            }

            throw new InvalidOperationException($"No available port in {PieUnityCapabilitiesConstants.DefaultPort}-{PieUnityCapabilitiesConstants.MaxPort}");
        }

        private static string ReadRequestBody(HttpListenerRequest request)
        {
            if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                return "{}";

            using var buffer = new MemoryStream();
            request.InputStream.CopyTo(buffer);
            var body = Encoding.UTF8.GetString(buffer.ToArray());
            return string.IsNullOrWhiteSpace(body) ? "{}" : body;
        }

        private static string BuildCapabilityEnvelope(string kind, string name, string argsJson)
        {
            string resultJson;
            string error;
            var ok = string.Equals(kind, "tool", StringComparison.OrdinalIgnoreCase)
                ? PieUnityCapabilityRegistry.TryInvokeTool(name, argsJson, out resultJson, out error)
                : PieUnityCapabilityRegistry.TryInvokeRpc(name, argsJson, out resultJson, out error);

            if (!ok && string.Equals(kind, "rpc", StringComparison.OrdinalIgnoreCase))
            {
                var legacy = PieDevRpc.InvokeMethod(name, argsJson);
                var legacyResponse = JsonUtility.FromJson<PieDevRpcResponse>(legacy);
                if (legacyResponse != null && legacyResponse.ok)
                {
                    ok = true;
                    resultJson = string.IsNullOrEmpty(legacyResponse.resultJson) ? "null" : legacyResponse.resultJson;
                    error = "";
                }
                else
                {
                    resultJson = "null";
                    error = legacyResponse?.error ?? error;
                }
            }

            return BuildEnvelope(kind, name, ok, resultJson, ok ? "" : (error ?? "Unknown error"));
        }

        private static string HandleUnityScriptRunTool(string argsJson, CancellationToken token)
        {
            var taskId = "";
            try
            {
                var statusJson = PieDevRpcDispatcher.InvokeSync(
                    () => PieUnityCapabilitiesBootstrap.StartUnityScriptRun(argsJson),
                    ScriptRunMainThreadTimeoutMs);

                while (true)
                {
                    var status = JsonUtility.FromJson<ScriptRunStatusPayload>(statusJson ?? "{}") ?? new ScriptRunStatusPayload();
                    if (!string.IsNullOrWhiteSpace(status.taskId))
                        taskId = status.taskId;

                    if (status.done || IsTerminalScriptRunStatus(status.status))
                    {
                        var ok = string.Equals(status.status, "completed", StringComparison.Ordinal);
                        var error = ok ? "" : (status.errorMessage ?? status.status ?? "Unity script run failed.");
                        return BuildEnvelopeOnMainThread("tool", "unity_script_run", ok, statusJson, error);
                    }

                    if (token.IsCancellationRequested)
                    {
                        TryCancelUnityScriptRun(taskId, "Dev RPC server is stopping.");
                        return BuildEnvelopeOnMainThread("tool", "unity_script_run", false, statusJson, "Dev RPC server is stopping.");
                    }

                    Thread.Sleep(ScriptRunPollIntervalMs);
                    statusJson = PieDevRpcDispatcher.InvokeSync(
                        () => PieUnityCapabilitiesBootstrap.GetUnityScriptRunStatus(taskId),
                        ScriptRunMainThreadTimeoutMs);
                }
            }
            catch (Exception ex)
            {
                TryCancelUnityScriptRun(taskId, ex.Message);
                return BuildEnvelopeOnMainThread("tool", "unity_script_run", false, "null", ex.Message);
            }
        }

        private static string BuildEnvelopeOnMainThread(string kind, string name, bool ok, string resultJson, string error)
        {
            return PieDevRpcDispatcher.InvokeSync(
                () => BuildEnvelope(kind, name, ok, resultJson, error),
                ScriptRunMainThreadTimeoutMs);
        }

        private static bool IsTerminalScriptRunStatus(string status)
        {
            return string.Equals(status, "completed", StringComparison.Ordinal)
                || string.Equals(status, "failed", StringComparison.Ordinal)
                || string.Equals(status, "cancelled", StringComparison.Ordinal);
        }

        private static void TryCancelUnityScriptRun(string taskId, string reason)
        {
            if (string.IsNullOrWhiteSpace(taskId))
                return;

            try
            {
                PieDevRpcDispatcher.InvokeSync(
                    () => PieUnityCapabilitiesBootstrap.CancelUnityScriptRun(taskId, reason ?? "Unity script run cancelled."),
                    ScriptRunMainThreadTimeoutMs);
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }

        private static string BuildEnvelope(string kind, string name, bool ok, string resultJson, string error, string errorCode = "CAPABILITY_ERROR")
        {
            var envelope = new PieUnityEnvelope
            {
                ok = ok,
                instanceId = PieUnityCapabilityRegistry.InstanceId,
                projectPath = PieUnityCapabilityRegistry.ProjectPath,
                mode = PieUnityCapabilityRegistry.Mode,
                kind = kind,
                name = name ?? "",
                result = resultJson ?? "null",
                error = ok ? "" : (error ?? "Unknown error"),
                errorCode = ok ? "" : (errorCode ?? "CAPABILITY_ERROR"),
                serverAvailability = GetSafeAvailabilityNoticeJson(),
            };

            if (!envelope.ok)
            {
                PieDiagnostics.Warning($"[PieDevRpcServer] {kind} {name} failed: {envelope.error}");
            }
            return JsonUtility.ToJson(envelope);
        }

        private static string GetSafeAvailabilityNoticeJson()
        {
            try
            {
                return PieUnityAvailability.GetAvailabilityNoticeJson();
            }
            catch
            {
                return "";
            }
        }

        private static System.Collections.Generic.Dictionary<string, string> ParseQueryString(string query)
        {
            var result = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(query))
                return result;

            var trimmed = query.TrimStart('?');
            var parts = trimmed.Split('&');
            for (var i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(parts[i]))
                    continue;
                var pair = parts[i].Split(new[] { '=' }, 2);
                var key = Uri.UnescapeDataString(pair[0] ?? "");
                var value = pair.Length > 1 ? Uri.UnescapeDataString(pair[1] ?? "") : "";
                if (!string.IsNullOrWhiteSpace(key))
                    result[key] = value;
            }

            return result;
        }

        private static void WriteJson(HttpListenerResponse response, int statusCode, string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json ?? "{}");
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = bytes.LongLength;
            using var output = response.OutputStream;
            output.Write(bytes, 0, bytes.Length);
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

        private static bool IsAuthorized(HttpListenerRequest request)
        {
            var expected = AuthToken;
            var actual = request.Headers["X-Pie-Token"] ?? "";
            return !string.IsNullOrWhiteSpace(expected)
                && string.Equals(actual, expected, StringComparison.Ordinal);
        }

        private static string GenerateAuthToken()
        {
            return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        }
    }
}

#endif
