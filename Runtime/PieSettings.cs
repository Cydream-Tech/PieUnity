using System.Collections.Generic;
using UnityEngine;

namespace Pie
{
    [System.Serializable]
    public class PieFileToolRootDefinition
    {
        [SerializeField] private string name = "";
        [SerializeField] private string description = "";
        [SerializeField] private string relativePathFromProjectRoot = "";
        [SerializeField] private string absolutePath = "";
        [SerializeField] private bool availableInEditor = true;
        [SerializeField] private bool availableInRuntime = false;
        [SerializeField] private List<string> defaultPrefixes = new List<string>();

        public string Name => name;
        public string Description => description;
        public string RelativePathFromProjectRoot => relativePathFromProjectRoot;
        public string AbsolutePath => absolutePath;
        public bool AvailableInEditor => availableInEditor;
        public bool AvailableInRuntime => availableInRuntime;
        public IReadOnlyList<string> DefaultPrefixes => defaultPrefixes;
    }

    [CreateAssetMenu(fileName = "PieSettings", menuName = "Tools/Pie/Pie Settings")]
    public class PieSettings : ScriptableObject
    {
        [SerializeField] private List<string> extensionSearchPaths = new List<string>
        {
            "Assets/Pie/Extensions",
        };

        [SerializeField] private List<string> skillSearchPaths = new List<string>
        {
            "Assets/Pie/Skills",
        };

        [SerializeField] private List<PieFileToolRootDefinition> fileToolRoots = new List<PieFileToolRootDefinition>();
        [SerializeField] private string defaultFileToolRootEditor = "persistent";
        [SerializeField] private string defaultFileToolRootRuntime = "persistent";

        public IReadOnlyList<string> ExtensionSearchPaths => extensionSearchPaths;
        public IReadOnlyList<string> SkillSearchPaths => skillSearchPaths;
        public IReadOnlyList<PieFileToolRootDefinition> FileToolRoots => fileToolRoots;
        public string DefaultFileToolRootEditor => defaultFileToolRootEditor;
        public string DefaultFileToolRootRuntime => defaultFileToolRootRuntime;
    }
}

// <pie-unity-merged-runtime>
// Unity 6000 can keep stale import state for local package scripts after malformed meta files.
// Keep these runtime definitions in an already-imported script so the package compiles deterministically.

namespace Pie
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    // Merged from Runtime/PieDevRpc.cs
    [Serializable]
        public class PieDevRpcRequest
        {
            public string method;
            public string argsJson;
        }

        [Serializable]
        public class PieDevRpcResponse
        {
            public bool ok;
            public string method;
            public string resultJson;
            public string error;

            public static string Success(string method, string resultJson)
            {
                return JsonUtility.ToJson(new PieDevRpcResponse
                {
                    ok = true,
                    method = method ?? "",
                    resultJson = string.IsNullOrEmpty(resultJson) ? "null" : resultJson,
                    error = "",
                });
            }

            public static string Failure(string method, string error)
            {
                return JsonUtility.ToJson(new PieDevRpcResponse
                {
                    ok = false,
                    method = method ?? "",
                    resultJson = "null",
                    error = string.IsNullOrEmpty(error) ? "Unknown error" : error,
                });
            }
        }

        public static class PieDevRpc
        {
            private static readonly Dictionary<string, Func<string, string>> Methods = new Dictionary<string, Func<string, string>>(StringComparer.Ordinal);

            [Serializable]
            private class MethodListPayload
            {
                public string[] methods;
            }

            public static void Register(string method, Func<string, string> handler)
            {
                if (string.IsNullOrWhiteSpace(method))
                    throw new ArgumentException("RPC method is required.", nameof(method));
                if (handler == null)
                    throw new ArgumentNullException(nameof(handler));

                Methods[method] = handler;
            }

            public static bool Unregister(string method)
            {
                if (string.IsNullOrWhiteSpace(method))
                    return false;
                return Methods.Remove(method);
            }

            public static void Clear()
            {
                Methods.Clear();
            }

            public static string InvokeHostCall(string argsJson)
            {
                var request = JsonUtility.FromJson<PieDevRpcRequest>(argsJson ?? "{}") ?? new PieDevRpcRequest();
                return InvokeMethod(request.method, request.argsJson ?? "{}");
            }

            public static string InvokeMethod(string method, string argsJson = "{}")
            {
                method = method ?? "";

                if (string.IsNullOrWhiteSpace(method))
                    return PieDevRpcResponse.Failure(method, "RPC method is required.");

                var toolFound = PieUnityCapabilityRegistry.HasTool(method);
                if (toolFound)
                {
                    if (PieUnityCapabilityRegistry.TryInvokeTool(method, argsJson, out var capabilityResultJson, out var capabilityToolError))
                        return PieDevRpcResponse.Success(method, capabilityResultJson);
                    return PieDevRpcResponse.Failure(method, capabilityToolError);
                }

                var rpcFound = PieUnityCapabilityRegistry.HasRpc(method);
                if (rpcFound)
                {
                    if (PieUnityCapabilityRegistry.TryInvokeRpc(method, argsJson, out var capabilityRpcResultJson, out var capabilityRpcError))
                        return PieDevRpcResponse.Success(method, capabilityRpcResultJson);
                    return PieDevRpcResponse.Failure(method, capabilityRpcError);
                }

                if (string.Equals(method, "rpc.list", StringComparison.Ordinal))
                {
                    var methods = new List<string>(Methods.Keys);
                    methods.Sort(StringComparer.Ordinal);
                    return PieDevRpcResponse.Success(method, JsonUtility.ToJson(new MethodListPayload
                    {
                        methods = methods.ToArray(),
                    }));
                }

                if (!Methods.TryGetValue(method, out var handler))
                {
                    PieDiagnostics.Warning($"[PieDevRpc] RPC method not found: {method}");
                    return PieDevRpcResponse.Failure(method, $"RPC method not found: {method}");
                }

                try
                {
                    var resultJson = handler(argsJson ?? "{}");
                    return PieDevRpcResponse.Success(method, resultJson);
                }
                catch (Exception ex)
                {
                    PieDiagnostics.Warning($"[PieDevRpc] {method} failed: {ex.Message}");
                    return PieDevRpcResponse.Failure(method, ex.Message);
                }
            }
        }
}

namespace Pie
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    // Merged from Runtime/PieDevRpcDispatcher.cs
    public static class PieDevRpcDispatcher
        {
            private sealed class WorkItem
            {
                public Func<string> action;
                public ManualResetEventSlim done;
                public string result;
                public Exception error;
            }

            private static readonly ConcurrentQueue<WorkItem> Queue = new ConcurrentQueue<WorkItem>();
            private static int _mainThreadId = -1;

            public static int MainThreadId => _mainThreadId;
            public static int CurrentThreadId => Thread.CurrentThread.ManagedThreadId;

            public static void InitializeMainThread()
            {
                _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            }

            public static string InvokeSync(Func<string> action, int timeoutMs = 5000)
            {
                if (action == null)
                    throw new ArgumentNullException(nameof(action));

                if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
                    return action();

                var item = new WorkItem
                {
                    action = action,
                    done = new ManualResetEventSlim(false),
                };

                Queue.Enqueue(item);
                if (!item.done.Wait(timeoutMs))
                    throw new TimeoutException("Timed out waiting for Unity main thread.");

                if (item.error != null)
                    throw item.error;

                return item.result;
            }

            public static void Tick()
            {
                while (Queue.TryDequeue(out var item))
                {
                    try
                    {
                        item.result = item.action();
                    }
                    catch (Exception ex)
                    {
                        item.error = ex;
                    }
                    finally
                    {
                        item.done.Set();
                    }
                }
            }
        }
}

namespace Pie
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;

    // Merged from Runtime/PieDevRpcServer.cs
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
                        PieUnityCapabilitiesBootstrap.Heartbeat();
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
                        PieUnityCapabilitiesBootstrap.Heartbeat();
                        return;
                    }

                    if (string.Equals(path, "/manifest", StringComparison.Ordinal))
                    {
                        var query = ParseQueryString(request.Url.Query);
                        query.TryGetValue("namespace", out var filterNamespace);
                        query.TryGetValue("name", out var filterName);
                        WriteJson(context.Response, 200, PieUnityCapabilityRegistry.BuildManifestJson(filterNamespace, filterName));
                        PieUnityCapabilitiesBootstrap.Heartbeat();
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
                        PieUnityCapabilitiesBootstrap.Heartbeat();
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
                    PieUnityCapabilitiesBootstrap.Heartbeat();
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

namespace Pie
{
    using System;
    using UnityEngine;

    // Merged from Runtime/PieInteractionModels.cs
    [Serializable]
        public class PieInteractionRequest
        {
            public string type;
            public string id;
            public string message;
            public string level;
            public string prompt;
            public string detail;
            public string placeholder;
            public string prefill;
            public string[] options;
            public int timeoutMs;
        }

        [Serializable]
        public class PieInteractionResponse
        {
            public string type;
            public bool acknowledged;
            public string id;
            public string selection;
            public bool confirmed;
            public string value;
            public bool unavailable;
            public string message;
            public bool timedOut;
            public bool skipped;

            public static string ToJson(PieInteractionResponse response)
            {
                return JsonUtility.ToJson(response ?? new PieInteractionResponse());
            }

            public static string Unavailable(string type, string id, string message)
            {
                return ToJson(new PieInteractionResponse
                {
                    type = type,
                    id = id,
                    unavailable = true,
                    message = message,
                });
            }
        }
}

namespace Pie
{
    // Merged from Runtime/UnityCapabilities/PieUnityAvailability.cs
    public static class PieUnityAvailability
        {
            public static string GetAvailabilityNoticeJson()
            {
    #if UNITY_EDITOR
                if (PieDevRpcDispatcher.MainThreadId >= 0 && PieDevRpcDispatcher.CurrentThreadId != PieDevRpcDispatcher.MainThreadId)
                {
                    return "";
                }

                if (UnityEditor.EditorApplication.isCompiling)
                {
                    return "{\"status\":\"temporarily_unavailable\",\"reason\":\"compiling\",\"message\":\"Unity is compiling scripts. Wait for compilation to finish and retry.\"}";
                }

                if (UnityEditor.EditorApplication.isUpdating)
                {
                    return "{\"status\":\"temporarily_unavailable\",\"reason\":\"updating\",\"message\":\"Unity is refreshing assets or updating the editor state. Retry shortly.\"}";
                }
    #endif
                return "";
            }
        }
}

namespace Pie
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using UnityEngine;

    // Merged from Runtime/UnityCapabilities/PieUnityCapabilitiesBootstrap.cs
    public static class PieUnityCapabilitiesBootstrap
        {
            private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);
            private static Func<string, string> _editorResumeSessionHandler;
            private static string _productName = "";
            private static string _unityProductName = "";
            private static string _applicationIdentifier = "";
            private static DateTime _nextEditorHeartbeatUtc = DateTime.MinValue;
            private static DateTime _nextRuntimeHeartbeatUtc = DateTime.MinValue;
            private static string _editorInstanceId = "";
            private static string _editorProjectPath = "";
            private static string _runtimeInstanceId = "";
            private static string _runtimeProjectPath = "";
            public static string ProductName => _productName;
            public static string UnityProductName => _unityProductName;
            public static string ApplicationIdentifier => _applicationIdentifier;

            [Serializable]
            private sealed class TextPayload
            {
                public string text;
                public int limit = 20;
                public string sessionId;
                public string name;
            }

            [Serializable]
            private sealed class LogReadPayload
            {
                public string source = "active";
                public int tailLines = 200;
                public int maxBytes = 65536;
                public string contains = "";
            }

            public static void InitializeEditor()
            {
                var projectPath = GetProjectPath();
                _productName = DeriveProjectName(projectPath);
                CaptureUnityApplicationMetadata();
                _nextEditorHeartbeatUtc = DateTime.UtcNow.Add(HeartbeatInterval);
                var instanceId = BuildInstanceId(projectPath, "editor", _productName);
                _editorInstanceId = instanceId;
                _editorProjectPath = projectPath;
                PieUnityCapabilityRegistry.ConfigureContext(instanceId, projectPath, "editor");
                PieUnityInstanceRegistry.Register(instanceId, projectPath, _productName, "editor", PieDevRpcServer.Port, PieDevRpcServer.AuthToken, GetUnityProductName(), GetApplicationIdentifier());
                RegisterSharedCapabilities(isEditor: true);
                PieUnityEditorAuthoring.RegisterEditorTools();
            }

            public static void InitializeRuntime(PieRunner runner)
            {
                var projectPath = GetProjectPath(runner != null ? runner.ProjectRootOverride : null);
                _productName = DeriveProjectName(projectPath);
                CaptureUnityApplicationMetadata();
                _nextRuntimeHeartbeatUtc = DateTime.UtcNow.Add(HeartbeatInterval);
                var instanceId = BuildInstanceId(projectPath, "runtime", _productName);
                _runtimeInstanceId = instanceId;
                _runtimeProjectPath = projectPath;
                PieUnityCapabilityRegistry.ConfigureContext(instanceId, projectPath, "runtime");
                PieUnityInstanceRegistry.Register(instanceId, projectPath, _productName, "runtime", PieDevRpcServer.Port, PieDevRpcServer.AuthToken, GetUnityProductName(), GetApplicationIdentifier());
                RegisterSharedCapabilities(isEditor: false);
                RegisterRuntimeCapabilities(runner, projectPath);
            }

            public static void Heartbeat()
            {
                if (!string.IsNullOrWhiteSpace(_runtimeInstanceId))
                {
                    HeartbeatRuntime();
                    return;
                }

                HeartbeatEditor();
            }

            public static void HeartbeatEditor()
            {
                var now = DateTime.UtcNow;
                if (now < _nextEditorHeartbeatUtc)
                    return;

                _nextEditorHeartbeatUtc = now.Add(HeartbeatInterval);
                CaptureUnityApplicationMetadata();
                if (string.IsNullOrWhiteSpace(_editorInstanceId))
                    return;
                PieUnityInstanceRegistry.Register(
                    _editorInstanceId,
                    _editorProjectPath,
                    _productName,
                    "editor",
                    PieDevRpcServer.Port,
                    PieDevRpcServer.AuthToken,
                    GetUnityProductName(),
                    GetApplicationIdentifier());
            }

            public static void HeartbeatRuntime()
            {
                var now = DateTime.UtcNow;
                if (now < _nextRuntimeHeartbeatUtc)
                    return;

                _nextRuntimeHeartbeatUtc = now.Add(HeartbeatInterval);
                CaptureUnityApplicationMetadata();
                if (string.IsNullOrWhiteSpace(_runtimeInstanceId))
                    return;
                PieUnityInstanceRegistry.Register(
                    _runtimeInstanceId,
                    _runtimeProjectPath,
                    _productName,
                    "runtime",
                    PieDevRpcServer.Port,
                    PieDevRpcServer.AuthToken,
                    GetUnityProductName(),
                    GetApplicationIdentifier());
            }

            public static void Shutdown()
            {
                ShutdownAll();
            }

            public static void ShutdownEditor()
            {
                if (string.IsNullOrWhiteSpace(_editorInstanceId))
                    return;

                PieUnityInstanceRegistry.Unregister(_editorInstanceId);
                _editorInstanceId = "";
                _editorProjectPath = "";
                _nextEditorHeartbeatUtc = DateTime.MinValue;
            }

            public static void ShutdownRuntime()
            {
                if (string.IsNullOrWhiteSpace(_runtimeInstanceId))
                    return;

                PieUnityInstanceRegistry.Unregister(_runtimeInstanceId);
                _runtimeInstanceId = "";
                _runtimeProjectPath = "";
                _nextRuntimeHeartbeatUtc = DateTime.MinValue;
            }

            public static void ShutdownAll()
            {
                ShutdownRuntime();
                ShutdownEditor();
            }

            private static void RegisterSharedCapabilities(bool isEditor)
            {
                PieUnityCapabilityRegistry.RegisterRpc(
                    "rpc.list",
                    "rpc",
                    "List registered low-level RPC methods.",
                    isEditor ? "editor" : "runtime",
                    true,
                    false,
                    null,
                    new PieUnityParameterDescriptor[0],
                    _ => BuildRpcListJson());

                PieUnityCapabilityRegistry.RegisterTool(
                    "unity_project_inspect",
                    "unity",
                    "Inspect the current Unity project/runtime context, including render pipeline, active scene, and AGENTS.md status.",
                    "editor+runtime",
                    true,
                    false,
                    null,
                    new PieUnityParameterDescriptor[0],
                    _ => PieUnityContextSnapshot.BuildJson(isEditor, PieUnityCapabilityRegistry.ProjectPath),
                    capabilityKind: "inspect");

                PieUnityCapabilityRegistry.RegisterTool(
                    "unity_scene_query",
                    "unity",
                    "Query Unity objects and return structured refs.",
                    "editor+runtime",
                    true,
                    false,
                    null,
                    new[]
                    {
                        new PieUnityParameterDescriptor { name = "scope", type = "string", required = false },
                        new PieUnityParameterDescriptor { name = "name", type = "string", required = false },
                        new PieUnityParameterDescriptor { name = "path", type = "string", required = false },
                        new PieUnityParameterDescriptor { name = "type", type = "string", required = false },
                        new PieUnityParameterDescriptor { name = "limit", type = "integer", required = false },
                    },
                    PieUnityHostRuntime.QueryJson,
                    capabilityKind: "query");

                PieUnityCapabilityRegistry.RegisterTool(
                    "unity_scene_object_inspect",
                    "unity",
                    "Inspect one explicit Unity scene object ref returned by unity_scene_query or unity_scene_object_edit.",
                    "editor+runtime",
                    true,
                    false,
                    null,
                    new[]
                    {
                        new PieUnityParameterDescriptor { name = "target", type = "object", required = true },
                    },
                    PieUnityHostRuntime.InspectJson,
                    capabilityKind: "inspect");

                PieUnityCapabilityRegistry.RegisterTool(
                    "unity_scene_object_edit",
                    "unity",
                    "Edit one Unity scene object with an explicit mutation patch. Supported actions: create_scene_object, destroy_scene_object, set_transform, set_active, set_parent, set_name, set_tag_layer, add_component, remove_component, set_component_enabled, set_component_property. Existing-object actions require an explicit target ref. Use unity_scene_query/unity_scene_object_inspect for reads.",
                    "editor+runtime",
                    false,
                    false,
                    null,
                    new[]
                    {
                        new PieUnityParameterDescriptor { name = "action", type = "string", required = true },
                        new PieUnityParameterDescriptor { name = "target", type = "object", required = false },
                        new PieUnityParameterDescriptor { name = "name", type = "string", required = false },
                        new PieUnityParameterDescriptor { name = "primitiveType", type = "string", required = false },
                        new PieUnityParameterDescriptor { name = "parentRef", type = "object", required = false },
                        new PieUnityParameterDescriptor { name = "parentName", type = "string", required = false },
                        new PieUnityParameterDescriptor { name = "active", type = "boolean", required = false },
                        new PieUnityParameterDescriptor { name = "tag", type = "string", required = false },
                        new PieUnityParameterDescriptor { name = "layer", type = "integer", required = false },
                        new PieUnityParameterDescriptor { name = "componentType", type = "string", required = false },
                        new PieUnityParameterDescriptor { name = "enabled", type = "boolean", required = false },
                        new PieUnityParameterDescriptor { name = "propertyName", type = "string", required = false },
                        new PieUnityParameterDescriptor { name = "value", type = "any", required = false },
                        new PieUnityParameterDescriptor { name = "applyPosition", type = "boolean", required = false },
                        new PieUnityParameterDescriptor { name = "x", type = "number", required = false },
                        new PieUnityParameterDescriptor { name = "y", type = "number", required = false },
                        new PieUnityParameterDescriptor { name = "z", type = "number", required = false },
                        new PieUnityParameterDescriptor { name = "applyRotation", type = "boolean", required = false },
                        new PieUnityParameterDescriptor { name = "rotX", type = "number", required = false },
                        new PieUnityParameterDescriptor { name = "rotY", type = "number", required = false },
                        new PieUnityParameterDescriptor { name = "rotZ", type = "number", required = false },
                        new PieUnityParameterDescriptor { name = "applyScale", type = "boolean", required = false },
                        new PieUnityParameterDescriptor { name = "scaleX", type = "number", required = false },
                        new PieUnityParameterDescriptor { name = "scaleY", type = "number", required = false },
                        new PieUnityParameterDescriptor { name = "scaleZ", type = "number", required = false },
                    },
                    PieUnityHostRuntime.ApplyJson,
                    capabilityKind: "mutate");

                PieUnityCapabilityRegistry.RegisterTool(
                    "unity_log_read",
                    "unity.log",
                    "Read Unity/Pie logs for diagnostics. source may be active, editor, player, or pie. Use after compile/runtime errors instead of guessing.",
                    "editor+runtime",
                    true,
                    false,
                    null,
                    new[]
                    {
                        new PieUnityParameterDescriptor { name = "source", type = "string", required = false },
                        new PieUnityParameterDescriptor { name = "tailLines", type = "integer", required = false },
                        new PieUnityParameterDescriptor { name = "maxBytes", type = "integer", required = false },
                        new PieUnityParameterDescriptor { name = "contains", type = "string", required = false },
                    },
                    ReadUnityLogJson,
                    capabilityKind: "inspect");

                PieUnityCapabilityRegistry.RegisterTool(
                    "unity_refresh",
                    "unity",
                    "Refresh Unity host state. In the editor this refreshes assets; in runtime it is a no-op.",
                    "editor+runtime",
                    false,
                    false,
                    null,
                    new PieUnityParameterDescriptor[0],
                    _ => PieUnityHostRuntime.RefreshJson(),
                    capabilityKind: "host");

                RegisterScriptCapabilities();
            }

            private static void RegisterRuntimeCapabilities(PieRunner runner, string projectPath)
            {
                if (runner == null)
                    return;

                PieUnityCapabilityRegistry.RegisterRpc(
                    "runner.get_state",
                    "runtime",
                    "Get current runtime runner state.",
                    "runtime",
                    true,
                    true,
                    null,
                    new PieUnityParameterDescriptor[0],
                    _ => runner.BuildRuntimeRpcStateJson());

                PieUnityCapabilityRegistry.RegisterTool(
                    "chat_send",
                    "chat",
                    "Send a chat message through the active runtime runner.",
                    "runtime",
                    false,
                    false,
                    null,
                    new[]
                    {
                        new PieUnityParameterDescriptor { name = "text", type = "string", required = true },
                    },
                    runner.DevRpcChatSend);

                PieUnityCapabilityRegistry.RegisterTool(
                    "chat_get_state",
                    "chat",
                    "Get runtime chat state.",
                    "runtime",
                    true,
                    false,
                    null,
                    new PieUnityParameterDescriptor[0],
                    _ => runner.BuildRuntimeRpcStateJson());

                PieUnityCapabilityRegistry.RegisterTool(
                    "chat_get_messages",
                    "chat",
                    "Get recent runtime chat messages.",
                    "runtime",
                    true,
                    false,
                    null,
                    new[]
                    {
                        new PieUnityParameterDescriptor { name = "limit", type = "integer", required = false },
                    },
                    runner.DevRpcChatGetMessages);
            }

            public static void RegisterEditorWindowCapabilities(
                Func<string> getState,
                Func<string, string> send,
                Func<string, string> getMessages,
                Func<string, string> resumeSession)
            {
    #if UNITY_EDITOR
                _editorResumeSessionHandler = resumeSession;
                PieUnityCapabilityRegistry.RegisterTool(
                    "chat_send",
                    "chat",
                    "Send a chat message to Pie Chat.",
                    "editor",
                    false,
                    false,
                    null,
                    new[]
                    {
                        new PieUnityParameterDescriptor { name = "text", type = "string", required = true },
                    },
                    send);

                PieUnityCapabilityRegistry.RegisterTool(
                    "chat_get_state",
                    "chat",
                    "Get Pie Chat state.",
                    "editor",
                    true,
                    false,
                    null,
                    new PieUnityParameterDescriptor[0],
                    _ => getState());

                PieUnityCapabilityRegistry.RegisterTool(
                    "chat_get_messages",
                    "chat",
                    "Get recent Pie Chat messages.",
                    "editor",
                    true,
                    false,
                    null,
                    new[]
                    {
                        new PieUnityParameterDescriptor { name = "limit", type = "integer", required = false },
                    },
                    getMessages);

                PieUnityCapabilityRegistry.RegisterTool(
                    "session_new",
                    "session",
                    "Create a new session in Pie Chat.",
                    "editor",
                    false,
                    false,
                    null,
                    new PieUnityParameterDescriptor[0],
                    _ => send("{\"text\":\"/new\"}"));

                PieUnityCapabilityRegistry.RegisterTool(
                    "session_resume",
                    "session",
                    "Resume a session by id in Pie Chat.",
                    "editor",
                    false,
                    false,
                    null,
                    new[]
                    {
                        new PieUnityParameterDescriptor { name = "sessionId", type = "string", required = true },
                    },
                    ResumeSessionViaChat);

                PieUnityCapabilityRegistry.RegisterTool(
                    "session_tree",
                    "session",
                    "Show the current session tree in Pie Chat.",
                    "editor",
                    false,
                    false,
                    null,
                    new PieUnityParameterDescriptor[0],
                    _ => send("{\"text\":\"/tree\"}"));

    #endif
            }

            private static void RegisterScriptCapabilities()
            {
                PieUnityCapabilityRegistry.RegisterTool(
                    "unity_script_run",
                    "unity.script",
                    "Run a JavaScript generator task inside the Unity script host. The script must define export function* run(ctx, args) and yield for multi-frame work. Do not pass C#, shader source, or raw file contents to this tool. It returns only after completion, failure, or timeout.",
                    "editor+runtime",
                    false,
                    false,
                    null,
                    new[]
                    {
                        new PieUnityParameterDescriptor { name = "script", type = "string", required = true },
                        new PieUnityParameterDescriptor { name = "name", type = "string", required = false },
                        new PieUnityParameterDescriptor { name = "entry", type = "string", required = false },
                        new PieUnityParameterDescriptor { name = "args", type = "object", required = false },
                        new PieUnityParameterDescriptor { name = "totalTimeoutMs", type = "number", required = false },
                        new PieUnityParameterDescriptor { name = "perStepTimeoutMs", type = "number", required = false },
                        new PieUnityParameterDescriptor { name = "maxFrames", type = "number", required = false },
                    },
                    InvokeScriptHostRunUnavailable,
                    capabilityKind: "script");
            }

            private static string ResumeSessionViaChat(string argsJson)
            {
                if (_editorResumeSessionHandler == null)
                    throw new InvalidOperationException("session_resume is only available in the Unity Editor.");

                return _editorResumeSessionHandler(argsJson);
            }

            public static string BuildResumeSessionCommandJson(string argsJson)
            {
                var payload = JsonUtility.FromJson<TextPayload>(argsJson ?? "{}") ?? new TextPayload();
                var sessionId = payload.sessionId ?? payload.text ?? "";
                sessionId = sessionId.Trim();
                return JsonUtility.ToJson(new TextPayload
                {
                    text = string.IsNullOrEmpty(sessionId) ? "/resume" : "/resume " + sessionId,
                });
            }

            private static string InvokeScriptHostRunUnavailable(string argsJson)
            {
                throw new InvalidOperationException("unity_script_run is asynchronous and must be executed through the dev RPC server or the embedded JS tool surface.");
            }

            public static string StartUnityScriptRun(string argsJson)
            {
                return InvokeScriptHost("run_start", argsJson);
            }

            public static string GetUnityScriptRunStatus(string taskId)
            {
                return InvokeScriptHost("run_status", "{\"taskId\":\"" + EscapeJson(taskId ?? "") + "\"}");
            }

            public static string CancelUnityScriptRun(string taskId, string reason)
            {
                return InvokeScriptHost("run_cancel", "{\"taskId\":\"" + EscapeJson(taskId ?? "") + "\",\"reason\":\"" + EscapeJson(reason ?? "") + "\"}");
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

            private static string InvokeScriptHost(string method, string argsJson)
            {
                var bridge = PieBridge.Instance;
                if (bridge == null || !bridge.IsInitialized)
                    throw new InvalidOperationException("Unity script host is not initialized.");

                return bridge.InvokeUnityScriptHost(method, argsJson ?? "{}");
            }

            public static string ReadUnityLogJson(string argsJson)
            {
                var payload = JsonUtility.FromJson<LogReadPayload>(argsJson ?? "{}") ?? new LogReadPayload();
                var requestedSource = string.IsNullOrWhiteSpace(payload.source) ? "active" : payload.source.Trim().ToLowerInvariant();
                var resolvedSource = ResolveLogSource(requestedSource);
                var candidates = GetLogCandidates(resolvedSource);
                var selectedPath = "";
                for (var i = 0; i < candidates.Count; i++)
                {
                    if (File.Exists(candidates[i]))
                    {
                        selectedPath = candidates[i];
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(selectedPath))
                {
                    return "{"
                        + "\"source\":\"" + Escape(requestedSource) + "\","
                        + "\"resolvedSource\":\"" + Escape(resolvedSource) + "\","
                        + "\"found\":false,"
                        + "\"path\":\"\","
                        + "\"pieLogPath\":\"" + Escape(NormalizePath(PieDiagnostics.GetLogFilePath())) + "\","
                        + "\"candidates\":[" + JoinQuotedNormalized(candidates) + "],"
                        + "\"content\":\"\""
                        + "}";
                }

                var maxBytes = Mathf.Clamp(payload.maxBytes <= 0 ? 65536 : payload.maxBytes, 1024, 262144);
                var tailLines = Mathf.Clamp(payload.tailLines <= 0 ? 200 : payload.tailLines, 1, 2000);
                var content = ReadTailText(selectedPath, maxBytes);
                if (!string.IsNullOrWhiteSpace(payload.contains))
                    content = FilterLinesContaining(content, payload.contains);
                content = TailLines(content, tailLines);
                var fileInfo = new FileInfo(selectedPath);
                var truncated = fileInfo.Exists && fileInfo.Length > maxBytes;

                return "{"
                    + "\"source\":\"" + Escape(requestedSource) + "\","
                    + "\"resolvedSource\":\"" + Escape(resolvedSource) + "\","
                    + "\"found\":true,"
                    + "\"path\":\"" + Escape(NormalizePath(selectedPath)) + "\","
                    + "\"pieLogPath\":\"" + Escape(NormalizePath(PieDiagnostics.GetLogFilePath())) + "\","
                    + "\"bytes\":" + (fileInfo.Exists ? fileInfo.Length : 0) + ","
                    + "\"truncated\":" + (truncated ? "true" : "false") + ","
                    + "\"content\":\"" + Escape(content) + "\""
                    + "}";
            }

            private static string ResolveLogSource(string source)
            {
                if (string.Equals(source, "active", StringComparison.OrdinalIgnoreCase))
                {
    #if UNITY_EDITOR
                    return "editor";
    #else
                    return "player";
    #endif
                }
                if (string.Equals(source, "editor", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(source, "player", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(source, "pie", StringComparison.OrdinalIgnoreCase))
                    return source.ToLowerInvariant();
                throw new InvalidOperationException("Unsupported log source: " + source);
            }

            private static List<string> GetLogCandidates(string source)
            {
                var candidates = new List<string>();
                if (string.Equals(source, "pie", StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(PieDiagnostics.GetLogFilePath());
                    return candidates;
                }

                if (string.Equals(source, "player", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(Application.consoleLogPath))
                        candidates.Add(Application.consoleLogPath);
                    candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "Player.log"));
                    candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs", ProductName, "Player.log"));
                    candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "unity3d", ProductName, "Player.log"));
                    return candidates;
                }

    #if UNITY_EDITOR_OSX
                candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs", "Unity", "Editor.log"));
    #elif UNITY_EDITOR_WIN
                candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor", "Editor.log"));
    #elif UNITY_EDITOR_LINUX
                candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "unity3d", "Editor.log"));
    #endif
                if (!string.IsNullOrWhiteSpace(Application.consoleLogPath))
                    candidates.Add(Application.consoleLogPath);
                return candidates;
            }

            private static string ReadTailText(string filePath, int maxBytes)
            {
                var info = new FileInfo(filePath);
                var length = info.Exists ? info.Length : 0;
                var bytesToRead = (int)Math.Min(Math.Max(0, length), Math.Max(1, maxBytes));
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (length > bytesToRead)
                        stream.Seek(-bytesToRead, SeekOrigin.End);
                    var buffer = new byte[bytesToRead];
                    var read = stream.Read(buffer, 0, bytesToRead);
                    return Encoding.UTF8.GetString(buffer, 0, read);
                }
            }

            private static string TailLines(string text, int maxLines)
            {
                var lines = (text ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                var start = Math.Max(0, lines.Length - Math.Max(1, maxLines));
                var result = new List<string>();
                for (var i = start; i < lines.Length; i++)
                    result.Add(lines[i]);
                return string.Join("\n", result.ToArray());
            }

            private static string FilterLinesContaining(string text, string contains)
            {
                var lines = (text ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                var result = new List<string>();
                for (var i = 0; i < lines.Length; i++)
                {
                    if (lines[i].IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
                        result.Add(lines[i]);
                }
                return string.Join("\n", result.ToArray());
            }

            private static string BuildRpcListJson()
            {
                var descriptors = PieUnityCapabilityRegistry.GetManifestEntries(null, null);
                var items = new List<string>();
                for (var i = 0; i < descriptors.Length; i++)
                {
                    var item = descriptors[i];
                    if (!string.Equals(item.kind, "rpc", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (item.deprecated)
                        continue;
                    items.Add(item.name);
                }

                return "{\"methods\":[" + JoinQuoted(items) + "]}";
            }

            private static string BuildInstanceId(string projectPath, string mode, string productName)
            {
                var safeProject = Regex.Replace(productName ?? "PieUnity", "[^a-zA-Z0-9]", "");
                var hash = ComputeStableHash(projectPath ?? "");
                return $"{safeProject}_{mode}_{hash}";
            }

            private static string DeriveProjectName(string projectPath)
            {
                var normalized = NormalizePath(projectPath).TrimEnd('/');
                var name = Path.GetFileName(normalized);
                return string.IsNullOrWhiteSpace(name) ? "PieUnity" : name;
            }

            private static string GetUnityProductName()
            {
                return string.IsNullOrWhiteSpace(_unityProductName) ? _productName : _unityProductName;
            }

            private static string GetApplicationIdentifier()
            {
                return string.IsNullOrWhiteSpace(_applicationIdentifier) ? "" : _applicationIdentifier;
            }

            private static void CaptureUnityApplicationMetadata()
            {
                try
                {
                    _unityProductName = string.IsNullOrWhiteSpace(Application.productName) ? _productName : Application.productName;
                    _applicationIdentifier = string.IsNullOrWhiteSpace(Application.identifier) ? "" : Application.identifier;
                }
                catch
                {
                    if (string.IsNullOrWhiteSpace(_unityProductName))
                        _unityProductName = _productName;
                    if (_applicationIdentifier == null)
                        _applicationIdentifier = "";
                }
            }

            private static string ComputeStableHash(string value)
            {
                using (var sha = SHA256.Create())
                {
                    var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""));
                    return BitConverter.ToString(bytes, 0, 4).Replace("-", "");
                }
            }

            private static string GetProjectPath(string projectRootOverride = null)
            {
                if (!string.IsNullOrWhiteSpace(projectRootOverride))
                    return NormalizePath(projectRootOverride);

    #if UNITY_EDITOR
                return NormalizePath(Directory.GetParent(Application.dataPath).FullName);
    #else
                return NormalizePath(Application.persistentDataPath);
    #endif
            }

            private static string NormalizePath(string path)
            {
                return (path ?? "").Replace("\\", "/");
            }

            private static string JoinQuoted(List<string> items)
            {
                var quoted = new string[items.Count];
                for (var i = 0; i < items.Count; i++)
                    quoted[i] = "\"" + Escape(items[i]) + "\"";
                return string.Join(",", quoted);
            }

            private static string JoinQuotedNormalized(List<string> items)
            {
                var normalized = new List<string>();
                for (var i = 0; i < items.Count; i++)
                    normalized.Add(NormalizePath(items[i]));
                return JoinQuoted(normalized);
            }

            private static string Escape(string text)
            {
                return (text ?? "")
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
            }
        }
}

namespace Pie
{
    using System;
    using System.IO;

    // Merged from Runtime/UnityCapabilities/PieUnityCapabilitiesConstants.cs
    public static class PieUnityCapabilitiesConstants
        {
            public const string Version = "0.1.24";
            public const string ManifestSchemaVersion = "2";
            public const string SkillProtocolVersion = "pie-unity-rpc/2";
            public const int DefaultPort = 8091;
            public const int MaxPort = 8100;
            public const int RegistryTtlSeconds = 120;
            public const string ServiceName = "pie-unity";

            public static string RegistryDirectory =>
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pie-unity");

            public static string InstancesDirectory =>
                Path.Combine(RegistryDirectory, "instances");

            public static string SharedLogsDirectory =>
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pie", "logs");

            public static string RuntimeLogFilePath =>
                Path.Combine(SharedLogsDirectory, "pie-unity.log");
        }
}

namespace Pie
{
    using System;
    using UnityEngine;

    // Merged from Runtime/UnityCapabilities/PieUnityCapabilitiesModels.cs
    [Serializable]
        public sealed class PieUnityEnvelope
        {
            public bool ok = true;
            public string service = "pie-unity";
            public string version = PieUnityCapabilitiesConstants.Version;
            public string instanceId = "";
            public string projectPath = "";
            public string mode = "";
            public string kind = "";
            public string name = "";
            public string result = "null";
            public string error = "";
            public string errorCode = "";
            public string serverAvailability = "";
        }

        [Serializable]
        public sealed class PieUnityParameterDescriptor
        {
            public string name;
            public string type;
            public bool required;
        }

        [Serializable]
        public sealed class PieUnityCapabilityDescriptor
        {
            public string schemaVersion = PieUnityCapabilitiesConstants.ManifestSchemaVersion;
            public string kind;
            public string name;
            public string ns;
            public string description;
            public string mode;
            public string availableIn;
            public string capabilityKind;
            public string source = "unity_builtin";
            public string hostNamespace = "";
            public string hostDisplayName = "";
            public string owner = "";
            public string writeScope = "";
            public string returns = "";
            public string recommendedWorkflow = "";
            public string[] examples = new string[0];
            public string[] errorCodes = new string[0];
            public bool readOnly;
            public bool deprecated;
            public bool convenience;
            public bool requiresMainThread;
            public bool destructive;
            public bool editorOnly;
            public bool runtimeOnly;
            public bool canTriggerDomainReload;
            public string[] aliases;
            public PieUnityParameterDescriptor[] parameters;
        }

        [Serializable]
        public sealed class PieUnityManifestSummary
        {
            public string service = "pie-unity";
            public string version = PieUnityCapabilitiesConstants.Version;
            public string manifestSchemaVersion = PieUnityCapabilitiesConstants.ManifestSchemaVersion;
            public string skillProtocolVersion = PieUnityCapabilitiesConstants.SkillProtocolVersion;
            public string instanceId = "";
            public string projectPath = "";
            public string projectName = "";
            public string productName = "";
            public string applicationIdentifier = "";
            public string mode = "";
            public string[] namespaces;
            public int stableTools;
            public int deprecatedAliases;
        }

        [Serializable]
        public sealed class PieUnityManifestDetail
        {
            public string service = "pie-unity";
            public string version = PieUnityCapabilitiesConstants.Version;
            public string manifestSchemaVersion = PieUnityCapabilitiesConstants.ManifestSchemaVersion;
            public string skillProtocolVersion = PieUnityCapabilitiesConstants.SkillProtocolVersion;
            public string instanceId = "";
            public string projectPath = "";
            public string projectName = "";
            public string productName = "";
            public string applicationIdentifier = "";
            public string mode = "";
            public string filterNamespace = "";
            public string filterName = "";
            public PieUnityCapabilityDescriptor[] capabilities;
        }

        [Serializable]
        public sealed class PieUnityHealthPayload
        {
            public string service = "pie-unity";
            public string version = PieUnityCapabilitiesConstants.Version;
            public string manifestSchemaVersion = PieUnityCapabilitiesConstants.ManifestSchemaVersion;
            public string skillProtocolVersion = PieUnityCapabilitiesConstants.SkillProtocolVersion;
            public string instanceId = "";
            public string projectPath = "";
            public string projectName = "";
            public string productName = "";
            public string applicationIdentifier = "";
            public string mode = "";
            public int port;
            public bool running = true;
            public bool ready = true;
            public bool domainReloadPending = false;
        public bool bridgeReady = false;
        public bool scriptHostReady = false;
        public bool hostBridgeReady = false;
        public bool runtimeBridgeReady = false;
        public string bridgeLastError = "";
        public string bridgeDiagnostic = "";
        public string scriptHostDiagnostic = "";
        public string discoverableOwnerMode = "";
        public bool runtimeActive = false;
        public bool editorActive = false;
        public bool editorSuppressedByRuntime = false;
        public int registeredHostCount = 0;
        public string[] registeredHostNamespaces = new string[0];
        public int hostCapabilityCount = 0;
            public string[] hostDiagnostics = new string[0];
            public bool mainThreadResponsive = true;
            public int activeHttpRequests = 0;
            public int activeFileRequests = 0;
            public int activeScriptRuns = 0;
            public string availability = "";
        }

        [Serializable]
        public sealed class PieUnityInstance
        {
            public string instanceId;
            public string projectPath;
            public string projectName;
            public string displayName;
            public string productName;
            public string applicationIdentifier;
            public string mode;
            public int port;
            public string token;
            public int pid;
            public long lastSeenUnix;
            public string version;
            public string packageVersion;
        }

        [Serializable]
    public sealed class PieUnityInstancesPayload
    {
        public string service = "pie-unity";
        public PieUnityInstance[] instances;
    }

    [Serializable]
    public sealed class PieUnityDiscoverableSnapshot
    {
        public PieUnityInstance discoverableOwner;
        public bool runtimeActive = false;
        public bool editorActive = false;
        public bool editorSuppressedByRuntime = false;
    }

        [Serializable]
        public sealed class PieUnityRef
        {
            public string scope = "";
            public string mode = "";
            public string id = "";
            public string path = "";
            public string name = "";
            public string type = "";
            public string scene = "";
            public string parentPath = "";
        }

        [Serializable]
        public sealed class PieUnityQueryPayload
        {
            public string scope = "scene_object";
            public string name = "";
            public string path = "";
            public string type = "";
            public int limit = 20;
        }

        [Serializable]
        public sealed class PieUnityQueryResult
        {
            public string scope = "scene_object";
            public string summary = "";
            public PieUnityRef[] results;
        }

        [Serializable]
        public sealed class PieUnityInspectPayload
        {
            public PieUnityRef target;
        }

        [Serializable]
        public sealed class PieUnityInspectResult
        {
            public string summary = "";
            public PieUnityRef target;
            public bool found;
            public bool activeSelf = true;
            public bool activeInHierarchy = true;
            public int childCount;
            public string[] componentTypes;
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale;
        }

        [Serializable]
        public sealed class PieUnityApplyPayload
        {
            public string action = "";
            public PieUnityRef target;
            public string name = "";
            public string primitiveType = "";
            public PieUnityRef parentRef;
            public string parentName = "";
            public bool active = true;
            public string tag = "";
            public int layer = -1;
            public string componentType = "";
            public bool enabled = true;
            public string propertyName = "";
            public bool applyPosition;
            public float x;
            public float y;
            public float z;
            public bool applyRotation;
            public float rotX;
            public float rotY;
            public float rotZ;
            public bool applyScale;
            public float scaleX = 1f;
            public float scaleY = 1f;
            public float scaleZ = 1f;
        }

        [Serializable]
        public sealed class PieUnityApplyResult
        {
            public string action = "";
            public string summary = "";
            public PieUnityRef target;
            public PieUnityRef[] createdRefs;
        }
}

namespace Pie
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEngine;

    // Merged from Runtime/UnityCapabilities/PieUnityCapabilityRegistry.cs
    public static class PieUnityCapabilityRegistry
        {
            private sealed class CapabilityRegistration
            {
                public PieUnityCapabilityDescriptor Descriptor;
                public Func<string, string> Handler;
            }

            private sealed class RuntimeHostRegistration
            {
                public string Namespace = "";
                public string DisplayName = "";
                public readonly HashSet<string> ToolKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                public readonly HashSet<string> RpcKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                public readonly HashSet<string> CapabilityNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            private static readonly Dictionary<string, CapabilityRegistration> RpcMethods = new Dictionary<string, CapabilityRegistration>(StringComparer.OrdinalIgnoreCase);
            private static readonly Dictionary<string, CapabilityRegistration> ToolMethods = new Dictionary<string, CapabilityRegistration>(StringComparer.OrdinalIgnoreCase);
            private static readonly Dictionary<string, RuntimeHostRegistration> RuntimeHosts = new Dictionary<string, RuntimeHostRegistration>(StringComparer.OrdinalIgnoreCase);
            private static readonly List<string> RuntimeHostDiagnostics = new List<string>();
            private static readonly object SyncRoot = new object();
            private static string _instanceId = "";
            private static string _projectPath = "";
            private static string _mode = "";

            public static void ConfigureContext(string instanceId, string projectPath, string mode)
            {
                lock (SyncRoot)
                {
                    _instanceId = instanceId ?? "";
                    _projectPath = projectPath ?? "";
                    _mode = mode ?? "";
                }
            }

            public static string InstanceId
            {
                get { lock (SyncRoot) return _instanceId; }
            }

            public static string ProjectPath
            {
                get { lock (SyncRoot) return _projectPath; }
            }

            public static string Mode
            {
                get { lock (SyncRoot) return _mode; }
            }

            public static void RegisterRpc(
                string name,
                string ns,
                string description,
                string mode,
                bool readOnly,
                bool deprecated,
                string[] aliases,
                PieUnityParameterDescriptor[] parameters,
                Func<string, string> handler,
                string capabilityKind = "host",
                bool convenience = false,
                bool requiresMainThread = true,
                string owner = "",
                string writeScope = "",
                string returns = "",
                string recommendedWorkflow = "",
                string[] examples = null,
                string[] errorCodes = null,
                bool destructive = false,
                bool canTriggerDomainReload = false)
            {
                RegisterBuiltinInternal(
                    RpcMethods,
                    "rpc",
                    name,
                    ns,
                    description,
                    mode,
                    readOnly,
                    deprecated,
                    aliases,
                    parameters,
                    handler,
                    capabilityKind,
                    convenience,
                    requiresMainThread,
                    owner,
                    writeScope,
                    returns,
                    recommendedWorkflow,
                    examples,
                    errorCodes,
                    destructive,
                    canTriggerDomainReload);
            }

            public static void RegisterTool(
                string name,
                string ns,
                string description,
                string mode,
                bool readOnly,
                bool deprecated,
                string[] aliases,
                PieUnityParameterDescriptor[] parameters,
                Func<string, string> handler,
                string capabilityKind = "host",
                bool convenience = false,
                bool requiresMainThread = true,
                string owner = "",
                string writeScope = "",
                string returns = "",
                string recommendedWorkflow = "",
                string[] examples = null,
                string[] errorCodes = null,
                bool destructive = false,
                bool canTriggerDomainReload = false)
            {
                RegisterBuiltinInternal(
                    ToolMethods,
                    "tool",
                    name,
                    ns,
                    description,
                    mode,
                    readOnly,
                    deprecated,
                    aliases,
                    parameters,
                    handler,
                    capabilityKind,
                    convenience,
                    requiresMainThread,
                    owner,
                    writeScope,
                    returns,
                    recommendedWorkflow,
                    examples,
                    errorCodes,
                    destructive,
                    canTriggerDomainReload);
            }

            public static void RegisterProjectTool(
                string name,
                string ns,
                string owner,
                string description,
                bool readOnly,
                PieUnityParameterDescriptor[] parameters,
                Func<string, string> handler,
                string writeScope = "project",
                string returns = "",
                string recommendedWorkflow = "",
                string[] examples = null,
                string[] errorCodes = null,
                bool destructive = false,
                bool canTriggerDomainReload = false)
            {
                RegisterTool(
                    name,
                    ns,
                    description,
                    "editor",
                    readOnly,
                    false,
                    null,
                    parameters,
                    handler,
                    capabilityKind: "project",
                    convenience: false,
                    requiresMainThread: true,
                    owner: owner,
                    writeScope: writeScope,
                    returns: returns,
                    recommendedWorkflow: recommendedWorkflow,
                    examples: examples,
                    errorCodes: errorCodes,
                    destructive: destructive,
                    canTriggerDomainReload: canTriggerDomainReload);
            }

            public static bool TryReplaceRuntimeHost(
                string hostNamespace,
                string hostDisplayName,
                PieUnityCapabilityDescriptor[] descriptors,
                Func<PieUnityCapabilityDescriptor, Func<string, string>> handlerFactory,
                out string error)
            {
                lock (SyncRoot)
                {
                    var normalizedNamespace = NormalizeHostNamespace(hostNamespace);
                    if (string.IsNullOrWhiteSpace(normalizedNamespace))
                    {
                        error = "Runtime host namespace is required.";
                        AppendRuntimeHostDiagnostic(error);
                        return false;
                    }

                    if (handlerFactory == null)
                    {
                        error = $"Runtime host {normalizedNamespace} is missing a handler factory.";
                        AppendRuntimeHostDiagnostic(error);
                        return false;
                    }

                    var items = descriptors ?? new PieUnityCapabilityDescriptor[0];
                    if (items.Length == 0)
                    {
                        error = $"Runtime host {normalizedNamespace} did not register any capabilities.";
                        AppendRuntimeHostDiagnostic(error);
                        return false;
                    }

                    var validationError = ValidateRuntimeHostDescriptors(normalizedNamespace, items);
                    if (!string.IsNullOrWhiteSpace(validationError))
                    {
                        error = validationError;
                        AppendRuntimeHostDiagnostic(error);
                        return false;
                    }

                    RemoveRuntimeHostLocked(normalizedNamespace);
                    var registration = new RuntimeHostRegistration
                    {
                        Namespace = normalizedNamespace,
                        DisplayName = string.IsNullOrWhiteSpace(hostDisplayName) ? normalizedNamespace : hostDisplayName.Trim(),
                    };

                    for (var i = 0; i < items.Length; i++)
                    {
                        var descriptor = CloneDescriptor(items[i]);
                        descriptor.source = "runtime_host";
                        descriptor.hostNamespace = normalizedNamespace;
                        descriptor.hostDisplayName = registration.DisplayName;
                        descriptor.ns = string.IsNullOrWhiteSpace(descriptor.ns) ? normalizedNamespace : descriptor.ns;
                        descriptor.mode = string.IsNullOrWhiteSpace(descriptor.mode) ? "runtime" : descriptor.mode;
                        descriptor.availableIn = descriptor.mode;
                        descriptor.aliases = descriptor.aliases ?? new string[0];
                        descriptor.parameters = descriptor.parameters ?? new PieUnityParameterDescriptor[0];
                        descriptor.examples = descriptor.examples ?? new string[0];
                        descriptor.errorCodes = descriptor.errorCodes ?? new string[0];

                        var handler = handlerFactory(descriptor);
                        if (handler == null)
                        {
                            error = $"Runtime host {normalizedNamespace} did not provide a handler for capability {descriptor.name}.";
                            AppendRuntimeHostDiagnostic(error);
                            RemoveRuntimeHostLocked(normalizedNamespace);
                            return false;
                        }

                        RegisterRegistrationLocked(
                            string.Equals(descriptor.kind, "rpc", StringComparison.OrdinalIgnoreCase) ? RpcMethods : ToolMethods,
                            descriptor,
                            handler,
                            descriptor.kind,
                            descriptor.aliases,
                            registration);
                    }

                    RuntimeHosts[normalizedNamespace] = registration;
                    error = "";
                    return true;
                }
            }

            public static void UnregisterRuntimeHost(string hostNamespace)
            {
                lock (SyncRoot)
                {
                    RemoveRuntimeHostLocked(NormalizeHostNamespace(hostNamespace));
                }
            }

            public static void ResetRuntimeHosts()
            {
                lock (SyncRoot)
                {
                    var namespaces = RuntimeHosts.Keys.ToArray();
                    for (var i = 0; i < namespaces.Length; i++)
                        RemoveRuntimeHostLocked(namespaces[i]);
                    RuntimeHosts.Clear();
                    RuntimeHostDiagnostics.Clear();
                }
            }

            public static string[] GetRegisteredRuntimeHostNamespaces()
            {
                lock (SyncRoot)
                {
                    return RuntimeHosts.Keys
                        .OrderBy((item) => item, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                }
            }

            public static int GetRuntimeHostCount()
            {
                lock (SyncRoot)
                    return RuntimeHosts.Count;
            }

            public static int GetRuntimeHostCapabilityCount()
            {
                lock (SyncRoot)
                    return RuntimeHosts.Values.Sum((entry) => entry.CapabilityNames.Count);
            }

            public static string[] GetRuntimeHostDiagnostics()
            {
                lock (SyncRoot)
                    return RuntimeHostDiagnostics.ToArray();
            }

            public static bool TryInvokeRpc(string name, string argsJson, out string resultJson, out string error)
            {
                return TryInvoke("rpc", RpcMethods, name, argsJson, out resultJson, out error);
            }

            public static bool TryInvokeTool(string name, string argsJson, out string resultJson, out string error)
            {
                return TryInvoke("tool", ToolMethods, name, argsJson, out resultJson, out error);
            }

            public static bool HasTool(string name)
            {
                lock (SyncRoot)
                    return ToolMethods.ContainsKey(name ?? "");
            }

            public static bool HasRpc(string name)
            {
                lock (SyncRoot)
                    return RpcMethods.ContainsKey(name ?? "");
            }

            public static PieUnityCapabilityDescriptor[] GetManifestEntries(string filterNamespace, string filterName)
            {
                lock (SyncRoot)
                {
                    var all = RpcMethods.Values
                        .Concat(ToolMethods.Values)
                        .Select((entry) => entry.Descriptor)
                        .GroupBy((entry) => $"{entry.kind}:{entry.name}", StringComparer.OrdinalIgnoreCase)
                        .Select((group) => group.First())
                        .OrderBy((entry) => entry.kind, StringComparer.Ordinal)
                        .ThenBy((entry) => entry.name, StringComparer.Ordinal)
                        .ToArray();

                    if (!string.IsNullOrWhiteSpace(filterName))
                    {
                        return all.Where((entry) => string.Equals(entry.name, filterName, StringComparison.OrdinalIgnoreCase)).ToArray();
                    }

                    if (!string.IsNullOrWhiteSpace(filterNamespace))
                    {
                        return all.Where((entry) => string.Equals(entry.ns, filterNamespace, StringComparison.OrdinalIgnoreCase)).ToArray();
                    }

                    return all;
                }
            }

            public static string BuildManifestJson(string filterNamespace, string filterName)
            {
                var instanceId = InstanceId;
                var projectPath = ProjectPath;
                var mode = Mode;
                if (!string.IsNullOrWhiteSpace(filterNamespace) || !string.IsNullOrWhiteSpace(filterName))
                {
                    var detail = new PieUnityManifestDetail
                    {
                        instanceId = instanceId,
                        projectPath = projectPath,
                        projectName = DeriveProjectName(projectPath),
                        productName = PieUnityCapabilitiesBootstrap.UnityProductName ?? "",
                        applicationIdentifier = PieUnityCapabilitiesBootstrap.ApplicationIdentifier ?? "",
                        mode = mode,
                        filterNamespace = filterNamespace ?? "",
                        filterName = filterName ?? "",
                        capabilities = GetManifestEntries(filterNamespace, filterName),
                    };
                    return JsonUtility.ToJson(detail, true);
                }

                var entries = GetManifestEntries(null, null);
                var namespaces = entries
                    .Select((entry) => entry.ns)
                    .Where((entry) => !string.IsNullOrWhiteSpace(entry))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy((entry) => entry, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var summary = new PieUnityManifestSummary
                {
                    instanceId = instanceId,
                    projectPath = projectPath,
                    projectName = DeriveProjectName(projectPath),
                    productName = PieUnityCapabilitiesBootstrap.UnityProductName ?? "",
                    applicationIdentifier = PieUnityCapabilitiesBootstrap.ApplicationIdentifier ?? "",
                    mode = mode,
                    namespaces = namespaces,
                    stableTools = entries.Count((entry) => string.Equals(entry.kind, "tool", StringComparison.OrdinalIgnoreCase) && !entry.deprecated),
                    deprecatedAliases = entries.Count((entry) => entry.deprecated),
                };
                return JsonUtility.ToJson(summary, true);
            }

            private static bool TryInvoke(
                string kind,
                Dictionary<string, CapabilityRegistration> source,
                string name,
                string argsJson,
                out string resultJson,
                out string error)
            {
                lock (SyncRoot)
                {
                    if (!source.TryGetValue(name ?? "", out var registration))
                    {
                        resultJson = "null";
                        error = $"Capability not found: {name}";
                        PieDiagnostics.Warning($"[PieUnityCapabilityRegistry] {kind} {name} failed: {error}");
                        return false;
                    }

                    try
                    {
                        resultJson = registration.Handler(argsJson ?? "{}") ?? "null";
                        error = "";
                        return true;
                    }
                    catch (Exception ex)
                    {
                        resultJson = "null";
                        error = ex.Message;
                        PieDiagnostics.Warning($"[PieUnityCapabilityRegistry] {kind} {name} failed: {ex.Message}");
                        return false;
                    }
                }
            }

            private static void RegisterBuiltinInternal(
                Dictionary<string, CapabilityRegistration> source,
                string kind,
                string name,
                string ns,
                string description,
                string capabilityMode,
                bool readOnly,
                bool deprecated,
                string[] aliases,
                PieUnityParameterDescriptor[] parameters,
                Func<string, string> handler,
                string capabilityKind,
                bool convenience,
                bool requiresMainThread,
                string capabilityOwner,
                string writeScope,
                string returnValue,
                string recommendedWorkflow,
                string[] examples,
                string[] errorCodes,
                bool destructive,
                bool canTriggerDomainReload)
            {
                lock (SyncRoot)
                {
                    var descriptor = new PieUnityCapabilityDescriptor();
                    descriptor.kind = kind;
                    descriptor.name = name;
                    descriptor.ns = ns;
                    descriptor.description = description;
                    descriptor.mode = capabilityMode;
                    descriptor.availableIn = capabilityMode;
                    descriptor.capabilityKind = capabilityKind ?? "host";
                    descriptor.source = "unity_builtin";
                    descriptor.owner = capabilityOwner ?? "";
                    descriptor.writeScope = writeScope ?? "";
                    descriptor.returns = returnValue ?? "";
                    descriptor.recommendedWorkflow = recommendedWorkflow ?? "";
                    descriptor.examples = examples ?? new string[0];
                    descriptor.errorCodes = errorCodes ?? new string[0];
                    descriptor.readOnly = readOnly;
                    descriptor.deprecated = deprecated;
                    descriptor.convenience = convenience;
                    descriptor.requiresMainThread = requiresMainThread;
                    descriptor.destructive = destructive;
                    descriptor.editorOnly = string.Equals(capabilityMode, "editor", StringComparison.OrdinalIgnoreCase);
                    descriptor.runtimeOnly = string.Equals(capabilityMode, "runtime", StringComparison.OrdinalIgnoreCase);
                    descriptor.canTriggerDomainReload = canTriggerDomainReload;
                    descriptor.aliases = aliases ?? new string[0];
                    descriptor.parameters = parameters ?? new PieUnityParameterDescriptor[0];

                    RegisterRegistrationLocked(source, descriptor, handler, kind, descriptor.aliases, null);
                }
            }

            private static void RegisterRegistrationLocked(
                Dictionary<string, CapabilityRegistration> source,
                PieUnityCapabilityDescriptor descriptor,
                Func<string, string> handler,
                string kind,
                string[] aliases,
                RuntimeHostRegistration runtimeHost)
            {
                var registration = new CapabilityRegistration
                {
                    Descriptor = descriptor,
                    Handler = handler,
                };

                source[descriptor.name] = registration;
                if (runtimeHost != null)
                {
                    runtimeHost.CapabilityNames.Add(descriptor.name);
                    TrackRuntimeHostKey(runtimeHost, descriptor.kind, descriptor.name);
                }

                if (aliases == null)
                    return;

                for (var i = 0; i < aliases.Length; i++)
                {
                    var alias = aliases[i];
                    if (string.IsNullOrWhiteSpace(alias))
                        continue;
                    source[alias] = registration;
                    if (runtimeHost != null)
                        TrackRuntimeHostKey(runtimeHost, descriptor.kind, alias);
                }
            }

            private static string ValidateRuntimeHostDescriptors(string hostNamespace, PieUnityCapabilityDescriptor[] descriptors)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < descriptors.Length; i++)
                {
                    var descriptor = descriptors[i];
                    if (descriptor == null)
                        return $"Runtime host {hostNamespace} has a null capability descriptor.";

                    string kind = descriptor.kind ?? string.Empty;
                    kind = kind.Trim().ToLowerInvariant();
                    if (kind != "tool" && kind != "rpc")
                        return $"Runtime host {hostNamespace} capability {descriptor.name ?? "(unnamed)"} must use kind tool or rpc.";

                    string name = descriptor.name ?? string.Empty;
                    name = name.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        return $"Runtime host {hostNamespace} has a capability with an empty name.";

                    string ns = descriptor.ns ?? string.Empty;
                    ns = ns.Trim();
                    if (string.IsNullOrWhiteSpace(ns))
                        return $"Runtime host {hostNamespace} capability {name} must declare a namespace.";

                    if (!string.Equals(ns, hostNamespace, StringComparison.OrdinalIgnoreCase))
                        return $"Runtime host {hostNamespace} capability {name} must stay in namespace {hostNamespace}.";

                    var scopedKey = $"{kind}:{name}";
                    if (!seen.Add(scopedKey))
                        return $"Runtime host {hostNamespace} registered duplicate capability {name}.";

                    var conflict = FindConflictingRegistration(kind == "rpc" ? RpcMethods : ToolMethods, name, hostNamespace);
                    if (!string.IsNullOrWhiteSpace(conflict))
                        return conflict;

                    var aliases = descriptor.aliases ?? new string[0];
                    for (var aliasIndex = 0; aliasIndex < aliases.Length; aliasIndex++)
                    {
                        string alias = aliases[aliasIndex] ?? string.Empty;
                        alias = alias.Trim();
                        if (string.IsNullOrWhiteSpace(alias))
                            continue;

                        var aliasKey = $"{kind}:{alias}";
                        if (!seen.Add(aliasKey))
                            return $"Runtime host {hostNamespace} registered duplicate alias {alias}.";

                        conflict = FindConflictingRegistration(kind == "rpc" ? RpcMethods : ToolMethods, alias, hostNamespace);
                        if (!string.IsNullOrWhiteSpace(conflict))
                            return conflict;
                    }
                }

                return "";
            }

            private static string FindConflictingRegistration(Dictionary<string, CapabilityRegistration> source, string key, string hostNamespace)
            {
                if (!source.TryGetValue(key ?? "", out var existing))
                    return "";

                if (existing?.Descriptor == null)
                    return "";

                if (string.Equals(existing.Descriptor.source, "runtime_host", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(existing.Descriptor.hostNamespace, hostNamespace, StringComparison.OrdinalIgnoreCase))
                    return "";

                var owner = string.Equals(existing.Descriptor.source, "runtime_host", StringComparison.OrdinalIgnoreCase)
                    ? $"runtime host {existing.Descriptor.hostNamespace}"
                    : "built-in pie-unity capability";
                return $"Runtime host {hostNamespace} cannot register {key} because it conflicts with {owner}.";
            }

            private static void RemoveRuntimeHostLocked(string hostNamespace)
            {
                if (string.IsNullOrWhiteSpace(hostNamespace))
                    return;

                if (!RuntimeHosts.TryGetValue(hostNamespace, out var registration))
                    return;

                foreach (var key in registration.ToolKeys)
                    ToolMethods.Remove(key);
                foreach (var key in registration.RpcKeys)
                    RpcMethods.Remove(key);

                RuntimeHosts.Remove(hostNamespace);
            }

            private static void TrackRuntimeHostKey(RuntimeHostRegistration registration, string kind, string key)
            {
                if (registration == null || string.IsNullOrWhiteSpace(key))
                    return;

                if (string.Equals(kind, "rpc", StringComparison.OrdinalIgnoreCase))
                    registration.RpcKeys.Add(key);
                else
                    registration.ToolKeys.Add(key);
            }

            private static PieUnityCapabilityDescriptor CloneDescriptor(PieUnityCapabilityDescriptor source)
            {
                return new PieUnityCapabilityDescriptor
                {
                    schemaVersion = string.IsNullOrWhiteSpace(source.schemaVersion) ? PieUnityCapabilitiesConstants.ManifestSchemaVersion : source.schemaVersion,
                    kind = source.kind ?? "",
                    name = source.name ?? "",
                    ns = source.ns ?? "",
                    description = source.description ?? "",
                    mode = source.mode ?? "runtime",
                    availableIn = source.availableIn ?? source.mode ?? "runtime",
                    capabilityKind = string.IsNullOrWhiteSpace(source.capabilityKind) ? "host" : source.capabilityKind,
                    owner = source.owner ?? "",
                    writeScope = source.writeScope ?? "",
                    returns = source.returns ?? "",
                    recommendedWorkflow = source.recommendedWorkflow ?? "",
                    examples = source.examples ?? new string[0],
                    errorCodes = source.errorCodes ?? new string[0],
                    readOnly = source.readOnly,
                    deprecated = source.deprecated,
                    convenience = source.convenience,
                    requiresMainThread = source.requiresMainThread,
                    destructive = source.destructive,
                    editorOnly = source.editorOnly,
                    runtimeOnly = source.runtimeOnly,
                    canTriggerDomainReload = source.canTriggerDomainReload,
                    aliases = source.aliases ?? new string[0],
                    parameters = source.parameters ?? new PieUnityParameterDescriptor[0],
                };
            }

            private static string NormalizeHostNamespace(string hostNamespace)
            {
                return (hostNamespace ?? "").Trim();
            }

            private static void AppendRuntimeHostDiagnostic(string message)
            {
                if (string.IsNullOrWhiteSpace(message))
                    return;

                RuntimeHostDiagnostics.Add(message);
                if (RuntimeHostDiagnostics.Count > 32)
                    RuntimeHostDiagnostics.RemoveRange(0, RuntimeHostDiagnostics.Count - 32);
                PieDiagnostics.Warning($"[PieUnityCapabilityRegistry] {message}");
            }

            private static string DeriveProjectName(string projectPath)
            {
                var normalized = (projectPath ?? "").Replace("\\", "/").TrimEnd('/');
                var index = normalized.LastIndexOf('/');
                if (index >= 0 && index < normalized.Length - 1)
                    return normalized.Substring(index + 1);
                return string.IsNullOrWhiteSpace(normalized) ? "" : normalized;
            }
        }
}

namespace Pie
{
    using System;
    using System.IO;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    // Merged from Runtime/UnityCapabilities/PieUnityContextSnapshot.cs
    [Serializable]
        public sealed class PieUnityContextSnapshotPayload
        {
            public string projectRoot = "";
            public string mode = "";
            public string productName = "";
            public string unityVersion = "";
            public string sceneName = "";
            public string scenePath = "";
            public string selectedObject = "";
            public int rootObjectCount;
            public string[] rootObjects;
            public bool hasAgentsFile;
            public string[] skillSearchPaths;
        }

        public static class PieUnityContextSnapshot
        {
            public static string BuildJson(bool isEditor, string projectRoot)
            {
                var activeScene = SceneManager.GetActiveScene();
                var roots = activeScene.IsValid() ? activeScene.GetRootGameObjects() : new GameObject[0];
                var rootNames = new string[Math.Min(roots.Length, 12)];
                for (var i = 0; i < rootNames.Length; i++)
                    rootNames[i] = roots[i] != null ? roots[i].name : "";

                var payload = new PieUnityContextSnapshotPayload
                {
                    projectRoot = projectRoot ?? "",
                    mode = isEditor ? "editor" : "runtime",
                    productName = PieUnityCapabilitiesBootstrap.ProductName ?? "",
                    unityVersion = Application.unityVersion,
                    sceneName = activeScene.name ?? "",
                    scenePath = activeScene.path ?? "",
                    selectedObject = GetSelectedObjectName(isEditor),
                    rootObjectCount = roots.Length,
                    rootObjects = rootNames,
                    hasAgentsFile = File.Exists(PieProjectPaths.GetProjectAgentsPath(projectRoot)),
                    skillSearchPaths = ToArray(PieProjectPaths.GetSkillSearchPaths(projectRoot)),
                };

                return JsonUtility.ToJson(payload);
            }

            private static string[] ToArray(System.Collections.Generic.IReadOnlyList<string> list)
            {
                if (list == null || list.Count == 0)
                    return new string[0];
                var items = new string[list.Count];
                for (var i = 0; i < list.Count; i++)
                    items[i] = list[i] ?? "";
                return items;
            }

            private static string GetSelectedObjectName(bool isEditor)
            {
    #if UNITY_EDITOR
                if (isEditor && Selection.activeGameObject != null)
                    return Selection.activeGameObject.name ?? "";
    #endif
                return "";
            }
        }
}

namespace Pie
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    // Merged from Runtime/UnityCapabilities/PieUnityHostRuntime.cs
    public static class PieUnityHostRuntime
        {
            public static string QueryJson(string argsJson)
            {
                var payload = JsonUtility.FromJson<PieUnityQueryPayload>(argsJson ?? "{}") ?? new PieUnityQueryPayload();
                var refs = QuerySceneObjects(payload);

                var result = new PieUnityQueryResult
                {
                    scope = string.IsNullOrWhiteSpace(payload.scope) ? "scene_object" : payload.scope,
                    summary = $"Found {refs.Length} scene object(s).",
                    results = refs,
                };
                return JsonUtility.ToJson(result);
            }

            public static string InspectJson(string argsJson)
            {
                var payload = JsonUtility.FromJson<PieUnityInspectPayload>(argsJson ?? "{}") ?? new PieUnityInspectPayload();
                var target = ResolveTarget(payload.target);
                if (target == null)
                    throw new InvalidOperationException("target is required.");

                var gameObject = ResolveSceneObject(target);
                if (gameObject == null)
                    throw new InvalidOperationException("Target scene object not found.");

                var componentTypes = GetComponentTypes(gameObject);
                var inspect = new PieUnityInspectResult
                {
                    summary = $"Inspected {gameObject.name}.",
                    target = ToRef(gameObject),
                    found = true,
                    activeSelf = gameObject.activeSelf,
                    activeInHierarchy = gameObject.activeInHierarchy,
                    childCount = gameObject.transform.childCount,
                    componentTypes = componentTypes,
                    position = gameObject.transform.position,
                    rotation = gameObject.transform.eulerAngles,
                    scale = gameObject.transform.localScale,
                };
                return JsonUtility.ToJson(inspect);
            }

            public static string ApplyJson(string argsJson)
            {
                var payload = JsonUtility.FromJson<PieUnityApplyPayload>(argsJson ?? "{}") ?? new PieUnityApplyPayload();
                var action = (payload.action ?? "").Trim();
                if (string.IsNullOrWhiteSpace(action))
                    throw new InvalidOperationException("action is required.");

                if (string.Equals(action, "create_scene_object", StringComparison.OrdinalIgnoreCase))
                    return CreateSceneObjectJson(payload);

                if (string.Equals(action, "set_transform", StringComparison.OrdinalIgnoreCase))
                    return SetTransformJson(payload);
                if (string.Equals(action, "destroy_scene_object", StringComparison.OrdinalIgnoreCase))
                    return DestroySceneObjectJson(payload);
                if (string.Equals(action, "set_active", StringComparison.OrdinalIgnoreCase))
                    return SetActiveJson(payload);
                if (string.Equals(action, "set_parent", StringComparison.OrdinalIgnoreCase))
                    return SetParentJson(payload);
                if (string.Equals(action, "set_name", StringComparison.OrdinalIgnoreCase))
                    return SetNameJson(payload);
                if (string.Equals(action, "set_tag_layer", StringComparison.OrdinalIgnoreCase))
                    return SetTagLayerJson(payload);
                if (string.Equals(action, "add_component", StringComparison.OrdinalIgnoreCase))
                    return AddComponentJson(payload);
                if (string.Equals(action, "remove_component", StringComparison.OrdinalIgnoreCase))
                    return RemoveComponentJson(payload);
                if (string.Equals(action, "set_component_enabled", StringComparison.OrdinalIgnoreCase))
                    return SetComponentEnabledJson(payload);
                if (string.Equals(action, "set_component_property", StringComparison.OrdinalIgnoreCase))
                    return SetComponentPropertyJson(payload, argsJson ?? "{}");

                throw new InvalidOperationException("Unsupported action: " + action);
            }

            public static string RefreshJson()
            {
    #if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
                return "{\"refreshed\":true,\"summary\":\"Editor assets refreshed.\"}";
    #else
                return "{\"refreshed\":false,\"summary\":\"Refresh is a no-op in runtime mode.\"}";
    #endif
            }

            private static string CreateSceneObjectJson(PieUnityApplyPayload payload)
            {
                var requestedName = (payload.name ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(requestedName))
                {
                    var existingRefs = QuerySceneObjects(new PieUnityQueryPayload
                    {
                        scope = "scene_object",
                        name = requestedName,
                        limit = 3,
                    });

                    if (existingRefs.Length > 1)
                    {
                        throw new InvalidOperationException(
                            "Multiple scene objects already exist with name " + requestedName + ". Reuse or select one existing object instead of creating another duplicate.");
                    }

                    if (existingRefs.Length == 1)
                    {
                        var existingObject = ResolveSceneObject(existingRefs[0]);
                        if (existingObject == null)
                            throw new InvalidOperationException("Existing scene object could not be resolved: " + requestedName);

                        ApplySceneObjectPlacement(existingObject, payload, createdNew: false);
                        var existingRef = ToRef(existingObject);
                        var reusedResult = new PieUnityApplyResult
                        {
                            action = "create_scene_object",
                            summary = "Reused existing scene object " + existingObject.name + ".",
                            target = existingRef,
                            createdRefs = new PieUnityRef[0],
                        };
                        return JsonUtility.ToJson(reusedResult);
                    }
                }

                var gameObject = CreateSceneObject(payload);
                if (gameObject == null)
                    throw new InvalidOperationException("Failed to create scene object.");

                ApplySceneObjectPlacement(gameObject, payload, createdNew: true);

    #if UNITY_EDITOR
                UnityEditor.Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
                UnityEditor.Selection.activeGameObject = gameObject;
                UnityEditor.EditorGUIUtility.PingObject(gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    #endif

                var targetRef = ToRef(gameObject);
                var result = new PieUnityApplyResult
                {
                    action = "create_scene_object",
                    summary = "Created scene object " + gameObject.name + ".",
                    target = targetRef,
                    createdRefs = new[] { targetRef },
                };
                return JsonUtility.ToJson(result);
            }

            private static void ApplySceneObjectPlacement(GameObject gameObject, PieUnityApplyPayload payload, bool createdNew)
            {
                if (gameObject == null)
                    throw new InvalidOperationException("Scene object is required.");

                if (HasRef(payload.parentRef) || !string.IsNullOrWhiteSpace(payload.parentName))
                {
                    var parentRef = ResolveTarget(payload.parentRef);
                    GameObject parent = parentRef != null ? ResolveSceneObject(parentRef) : null;
                    if (parent == null && !string.IsNullOrWhiteSpace(payload.parentName))
                        parent = ResolveSceneObjectByQuery(payload.parentName);
                    if (parent == null)
                    {
                        if (createdNew)
                            UnityEngine.Object.DestroyImmediate(gameObject);
                        throw new InvalidOperationException("Parent object not found.");
                    }

                    gameObject.transform.SetParent(parent.transform, false);
                }

                if (!string.IsNullOrWhiteSpace(payload.name))
                    gameObject.name = payload.name.Trim();
                gameObject.transform.position = new Vector3(payload.x, payload.y, payload.z);

    #if UNITY_EDITOR
                if (!createdNew)
                {
                    UnityEditor.Selection.activeGameObject = gameObject;
                    UnityEditor.EditorGUIUtility.PingObject(gameObject);
                    UnityEditor.EditorUtility.SetDirty(gameObject);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                }
    #endif
            }

            private static string SetTransformJson(PieUnityApplyPayload payload)
            {
                var gameObject = ResolveApplyTarget(payload);

                if (payload.applyPosition)
                    gameObject.transform.position = new Vector3(payload.x, payload.y, payload.z);
                if (payload.applyRotation)
                    gameObject.transform.eulerAngles = new Vector3(payload.rotX, payload.rotY, payload.rotZ);
                if (payload.applyScale)
                    gameObject.transform.localScale = new Vector3(payload.scaleX, payload.scaleY, payload.scaleZ);

    #if UNITY_EDITOR
                MarkSceneObjectChanged(gameObject);
    #endif

                var result = new PieUnityApplyResult
                {
                    action = "set_transform",
                    summary = "Updated transform for " + gameObject.name + ".",
                    target = ToRef(gameObject),
                    createdRefs = new PieUnityRef[0],
                };
                return JsonUtility.ToJson(result);
            }

            private static string DestroySceneObjectJson(PieUnityApplyPayload payload)
            {
                var gameObject = ResolveApplyTarget(payload, requireExplicitTarget: true);
                var targetRef = ToRef(gameObject);
    #if UNITY_EDITOR
                UnityEditor.Undo.DestroyObjectImmediate(gameObject);
    #else
                UnityEngine.Object.Destroy(gameObject);
    #endif
                var result = new PieUnityApplyResult
                {
                    action = "destroy_scene_object",
                    summary = "Destroyed scene object " + targetRef.name + ".",
                    target = targetRef,
                    createdRefs = new PieUnityRef[0],
                };
                return JsonUtility.ToJson(result);
            }

            private static string SetActiveJson(PieUnityApplyPayload payload)
            {
                var gameObject = ResolveApplyTarget(payload);
                gameObject.SetActive(payload.active);
    #if UNITY_EDITOR
                MarkSceneObjectChanged(gameObject);
    #endif
                return JsonUtility.ToJson(new PieUnityApplyResult
                {
                    action = "set_active",
                    summary = "Set active for " + gameObject.name + " to " + payload.active + ".",
                    target = ToRef(gameObject),
                    createdRefs = new PieUnityRef[0],
                });
            }

            private static string SetParentJson(PieUnityApplyPayload payload)
            {
                var gameObject = ResolveApplyTarget(payload);
                if (!HasRef(payload.parentRef) && string.IsNullOrWhiteSpace(payload.parentName))
                    throw new InvalidOperationException("parentRef or parentName is required.");

                var parentRef = ResolveTarget(payload.parentRef);
                var parent = parentRef != null ? ResolveSceneObject(parentRef) : null;
                if (parent == null && !string.IsNullOrWhiteSpace(payload.parentName))
                    parent = ResolveSceneObjectByQuery(payload.parentName);
                if (parent == null)
                    throw new InvalidOperationException("Parent object not found.");

                gameObject.transform.SetParent(parent.transform, false);
    #if UNITY_EDITOR
                MarkSceneObjectChanged(gameObject);
    #endif
                return JsonUtility.ToJson(new PieUnityApplyResult
                {
                    action = "set_parent",
                    summary = "Set parent for " + gameObject.name + " to " + parent.name + ".",
                    target = ToRef(gameObject),
                    createdRefs = new PieUnityRef[0],
                });
            }

            private static string SetNameJson(PieUnityApplyPayload payload)
            {
                var gameObject = ResolveApplyTarget(payload);
                var nextName = (payload.name ?? "").Trim();
                if (string.IsNullOrWhiteSpace(nextName))
                    throw new InvalidOperationException("name is required.");
                gameObject.name = nextName;
    #if UNITY_EDITOR
                MarkSceneObjectChanged(gameObject);
    #endif
                return JsonUtility.ToJson(new PieUnityApplyResult
                {
                    action = "set_name",
                    summary = "Renamed scene object to " + gameObject.name + ".",
                    target = ToRef(gameObject),
                    createdRefs = new PieUnityRef[0],
                });
            }

            private static string SetTagLayerJson(PieUnityApplyPayload payload)
            {
                var gameObject = ResolveApplyTarget(payload);
                var changed = false;
                if (!string.IsNullOrWhiteSpace(payload.tag))
                {
                    gameObject.tag = payload.tag.Trim();
                    changed = true;
                }
                if (payload.layer >= 0)
                {
                    gameObject.layer = payload.layer;
                    changed = true;
                }
                if (!changed)
                    throw new InvalidOperationException("tag or layer is required.");
    #if UNITY_EDITOR
                MarkSceneObjectChanged(gameObject);
    #endif
                return JsonUtility.ToJson(new PieUnityApplyResult
                {
                    action = "set_tag_layer",
                    summary = "Updated tag/layer for " + gameObject.name + ".",
                    target = ToRef(gameObject),
                    createdRefs = new PieUnityRef[0],
                });
            }

            private static string AddComponentJson(PieUnityApplyPayload payload)
            {
                var gameObject = ResolveApplyTarget(payload);
                var componentType = ResolveComponentType(payload.componentType);
                var component = gameObject.AddComponent(componentType);
    #if UNITY_EDITOR
                MarkSceneObjectChanged(gameObject);
    #endif
                return JsonUtility.ToJson(new PieUnityApplyResult
                {
                    action = "add_component",
                    summary = "Added component " + component.GetType().FullName + " to " + gameObject.name + ".",
                    target = ToRef(gameObject),
                    createdRefs = new PieUnityRef[0],
                });
            }

            private static string RemoveComponentJson(PieUnityApplyPayload payload)
            {
                var gameObject = ResolveApplyTarget(payload, requireExplicitTarget: true);
                var componentType = ResolveComponentType(payload.componentType);
                if (componentType == typeof(Transform))
                    throw new InvalidOperationException("Transform cannot be removed.");
                var component = gameObject.GetComponent(componentType);
                if (component == null)
                    throw new InvalidOperationException("Component not found on " + gameObject.name + ": " + componentType.FullName);
    #if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(component);
                MarkSceneObjectChanged(gameObject);
    #else
                UnityEngine.Object.Destroy(component);
    #endif
                return JsonUtility.ToJson(new PieUnityApplyResult
                {
                    action = "remove_component",
                    summary = "Removed component " + componentType.FullName + " from " + gameObject.name + ".",
                    target = ToRef(gameObject),
                    createdRefs = new PieUnityRef[0],
                });
            }

            private static string SetComponentEnabledJson(PieUnityApplyPayload payload)
            {
                var gameObject = ResolveApplyTarget(payload);
                var componentType = ResolveComponentType(payload.componentType);
                var component = ResolveSingleComponent(gameObject, componentType);
                var enabledProperty = component.GetType().GetProperty(
                    "enabled",
                    BindingFlags.Instance | BindingFlags.Public);
                if (enabledProperty == null || !enabledProperty.CanWrite || enabledProperty.PropertyType != typeof(bool))
                    throw new InvalidOperationException("Component does not expose a writable bool enabled property: " + componentType.FullName);

                enabledProperty.SetValue(component, payload.enabled, null);
    #if UNITY_EDITOR
                MarkSceneObjectChanged(gameObject);
    #endif
                return JsonUtility.ToJson(new PieUnityApplyResult
                {
                    action = "set_component_enabled",
                    summary = "Set enabled for " + componentType.FullName + " on " + gameObject.name + " to " + payload.enabled + ".",
                    target = ToRef(gameObject),
                    createdRefs = new PieUnityRef[0],
                });
            }

            private static string SetComponentPropertyJson(PieUnityApplyPayload payload, string argsJson)
            {
                var gameObject = ResolveApplyTarget(payload);
                var componentType = ResolveComponentType(payload.componentType);
                var component = ResolveSingleComponent(gameObject, componentType);
                var propertyName = (payload.propertyName ?? "").Trim();
                if (string.IsNullOrWhiteSpace(propertyName))
                    throw new InvalidOperationException("propertyName is required.");

                if (!TryExtractJsonProperty(argsJson, "value", out var rawValue))
                    throw new InvalidOperationException("value is required.");

                var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
                var field = component.GetType().GetField(propertyName, bindingFlags);
                if (field != null)
                {
                    if (field.IsInitOnly || field.IsLiteral)
                        throw new InvalidOperationException("Component field is not writable: " + propertyName);
                    var converted = ConvertJsonValue(rawValue, field.FieldType);
                    field.SetValue(component, converted);
    #if UNITY_EDITOR
                    MarkComponentChanged(component);
    #endif
                    return JsonUtility.ToJson(new PieUnityApplyResult
                    {
                        action = "set_component_property",
                        summary = "Set field " + propertyName + " on " + componentType.FullName + ".",
                        target = ToRef(gameObject),
                        createdRefs = new PieUnityRef[0],
                    });
                }

                var property = component.GetType().GetProperty(propertyName, bindingFlags);
                if (property == null)
                    throw new InvalidOperationException("Component property or field not found: " + propertyName);
                if (!property.CanWrite)
                    throw new InvalidOperationException("Component property is not writable: " + propertyName);
                if (property.GetIndexParameters().Length > 0)
                    throw new InvalidOperationException("Indexed component properties are not supported: " + propertyName);

                var propertyValue = ConvertJsonValue(rawValue, property.PropertyType);
                property.SetValue(component, propertyValue, null);
    #if UNITY_EDITOR
                MarkComponentChanged(component);
    #endif
                return JsonUtility.ToJson(new PieUnityApplyResult
                {
                    action = "set_component_property",
                    summary = "Set property " + propertyName + " on " + componentType.FullName + ".",
                    target = ToRef(gameObject),
                    createdRefs = new PieUnityRef[0],
                });
            }

            private static GameObject CreateSceneObject(PieUnityApplyPayload payload)
            {
                var primitiveType = (payload.primitiveType ?? "").Trim();
                if (string.IsNullOrWhiteSpace(primitiveType))
                    return new GameObject(string.IsNullOrWhiteSpace(payload.name) ? "GameObject" : payload.name.Trim());

                switch (primitiveType.ToLowerInvariant())
                {
                    case "cube": return GameObject.CreatePrimitive(PrimitiveType.Cube);
                    case "sphere": return GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    case "capsule": return GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    case "cylinder": return GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    case "plane": return GameObject.CreatePrimitive(PrimitiveType.Plane);
                    case "quad": return GameObject.CreatePrimitive(PrimitiveType.Quad);
                    default: throw new InvalidOperationException("Unsupported primitiveType: " + primitiveType);
                }
            }

            private static GameObject ResolveApplyTarget(PieUnityApplyPayload payload, bool requireExplicitTarget = false)
            {
                var hasExplicitTarget = HasRef(payload.target);
                var target = ResolveTarget(payload.target);
                if (target == null)
                {
                    if (hasExplicitTarget)
                        throw new InvalidOperationException("Target not found.");
                    throw new InvalidOperationException(requireExplicitTarget
                        ? "Explicit target is required for this destructive action."
                        : "target is required.");
                }

                var gameObject = ResolveSceneObject(target);
                if (gameObject == null)
                    throw new InvalidOperationException("Target scene object not found.");
                return gameObject;
            }

            private static Component ResolveSingleComponent(GameObject gameObject, Type componentType)
            {
                if (gameObject == null)
                    throw new InvalidOperationException("Scene object is required.");
                if (componentType == null)
                    throw new InvalidOperationException("componentType is required.");

                var components = gameObject.GetComponents(componentType);
                if (components == null || components.Length == 0)
                    throw new InvalidOperationException("Component not found on " + gameObject.name + ": " + componentType.FullName);
                if (components.Length > 1)
                    throw new InvalidOperationException("Multiple components of type " + componentType.FullName + " found on " + gameObject.name + ". Multiple same-type components are not supported by this action.");
                return components[0];
            }

            private static object ConvertJsonValue(string rawValue, Type targetType)
            {
                if (targetType == null)
                    throw new InvalidOperationException("Target member type is unavailable.");

                var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
                if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                    throw new InvalidOperationException("UnityEngine.Object reference values are not supported by set_component_property.");
                if (type.IsArray || (type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type)))
                    throw new InvalidOperationException("Array, list, dictionary, and nested collection values are not supported by set_component_property.");

                var raw = (rawValue ?? "").Trim();
                if (string.Equals(raw, "null", StringComparison.OrdinalIgnoreCase))
                {
                    if (!type.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                        return null;
                    throw new InvalidOperationException("null is not valid for " + type.FullName);
                }

                if (type == typeof(string))
                    return ReadJsonStringOrRaw(raw);
                if (type == typeof(bool))
                    return ParseJsonBool(raw);
                if (type.IsEnum)
                    return Enum.Parse(type, ReadJsonStringOrRaw(raw), true);
                if (IsNumericType(type))
                    return Convert.ChangeType(ParseJsonDouble(raw), type, CultureInfo.InvariantCulture);
                if (type == typeof(Vector2))
                    return new Vector2(ReadRequiredJsonFloat(raw, "x"), ReadRequiredJsonFloat(raw, "y"));
                if (type == typeof(Vector3))
                    return new Vector3(ReadRequiredJsonFloat(raw, "x"), ReadRequiredJsonFloat(raw, "y"), ReadRequiredJsonFloat(raw, "z"));
                if (type == typeof(Color))
                    return new Color(
                        ReadRequiredJsonFloat(raw, "r"),
                        ReadRequiredJsonFloat(raw, "g"),
                        ReadRequiredJsonFloat(raw, "b"),
                        TryReadJsonNumberMember(raw, "a", out var a) ? a : 1f);

                throw new InvalidOperationException("Unsupported component property type: " + type.FullName);
            }

            private static bool IsNumericType(Type type)
            {
                return type == typeof(byte)
                    || type == typeof(sbyte)
                    || type == typeof(short)
                    || type == typeof(ushort)
                    || type == typeof(int)
                    || type == typeof(uint)
                    || type == typeof(long)
                    || type == typeof(ulong)
                    || type == typeof(float)
                    || type == typeof(double)
                    || type == typeof(decimal);
            }

            private static bool ParseJsonBool(string raw)
            {
                if (string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase))
                    return false;
                throw new InvalidOperationException("Expected boolean JSON value.");
            }

            private static double ParseJsonDouble(string raw)
            {
                if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    throw new InvalidOperationException("Expected numeric JSON value.");
                return value;
            }

            private static float ReadRequiredJsonFloat(string raw, string memberName)
            {
                if (TryReadJsonNumberMember(raw, memberName, out var value))
                    return value;
                throw new InvalidOperationException("Missing numeric JSON member: " + memberName);
            }

            private static bool TryReadJsonNumberMember(string raw, string memberName, out float value)
            {
                value = 0f;
                if (!TryExtractJsonProperty(raw, memberName, out var memberRaw))
                    return false;
                if (!float.TryParse(memberRaw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    throw new InvalidOperationException("JSON member " + memberName + " must be numeric.");
                return true;
            }

            private static bool TryExtractJsonProperty(string json, string propertyName, out string rawValue)
            {
                rawValue = "";
                var text = json ?? "";
                var key = "\"" + propertyName + "\"";
                for (var i = 0; i < text.Length; i++)
                {
                    if (!StringStartsWith(text, i, key))
                        continue;
                    var cursor = i + key.Length;
                    cursor = SkipWhitespace(text, cursor);
                    if (cursor >= text.Length || text[cursor] != ':')
                        continue;
                    cursor = SkipWhitespace(text, cursor + 1);
                    var end = FindJsonValueEnd(text, cursor);
                    if (end <= cursor)
                        return false;
                    rawValue = text.Substring(cursor, end - cursor);
                    return true;
                }
                return false;
            }

            private static int SkipWhitespace(string text, int index)
            {
                while (index < text.Length && char.IsWhiteSpace(text[index]))
                    index++;
                return index;
            }

            private static bool StringStartsWith(string text, int index, string value)
            {
                if (index < 0 || index + value.Length > text.Length)
                    return false;
                return string.CompareOrdinal(text, index, value, 0, value.Length) == 0;
            }

            private static int FindJsonValueEnd(string text, int start)
            {
                if (start >= text.Length)
                    return start;

                var first = text[start];
                if (first == '"')
                {
                    var escaped = false;
                    for (var i = start + 1; i < text.Length; i++)
                    {
                        var ch = text[i];
                        if (escaped)
                        {
                            escaped = false;
                            continue;
                        }
                        if (ch == '\\')
                        {
                            escaped = true;
                            continue;
                        }
                        if (ch == '"')
                            return i + 1;
                    }
                    return text.Length;
                }

                if (first == '{' || first == '[')
                {
                    var depth = 0;
                    var inString = false;
                    var escaped = false;
                    for (var i = start; i < text.Length; i++)
                    {
                        var ch = text[i];
                        if (inString)
                        {
                            if (escaped)
                            {
                                escaped = false;
                                continue;
                            }
                            if (ch == '\\')
                            {
                                escaped = true;
                                continue;
                            }
                            if (ch == '"')
                                inString = false;
                            continue;
                        }
                        if (ch == '"')
                        {
                            inString = true;
                            continue;
                        }
                        if (ch == '{' || ch == '[')
                            depth++;
                        if (ch == '}' || ch == ']')
                        {
                            depth--;
                            if (depth == 0)
                                return i + 1;
                        }
                    }
                    return text.Length;
                }

                for (var i = start; i < text.Length; i++)
                {
                    var ch = text[i];
                    if (ch == ',' || ch == '}')
                        return i;
                }
                return text.Length;
            }

            private static string ReadJsonStringOrRaw(string raw)
            {
                raw = (raw ?? "").Trim();
                if (raw.Length >= 2 && raw[0] == '"' && raw[raw.Length - 1] == '"')
                    return UnescapeJsonString(raw.Substring(1, raw.Length - 2));
                return raw;
            }

            private static string UnescapeJsonString(string value)
            {
                var result = new System.Text.StringBuilder();
                for (var i = 0; i < value.Length; i++)
                {
                    var ch = value[i];
                    if (ch != '\\' || i + 1 >= value.Length)
                    {
                        result.Append(ch);
                        continue;
                    }

                    var escaped = value[++i];
                    switch (escaped)
                    {
                        case '"': result.Append('"'); break;
                        case '\\': result.Append('\\'); break;
                        case '/': result.Append('/'); break;
                        case 'b': result.Append('\b'); break;
                        case 'f': result.Append('\f'); break;
                        case 'n': result.Append('\n'); break;
                        case 'r': result.Append('\r'); break;
                        case 't': result.Append('\t'); break;
                        case 'u':
                            if (i + 4 >= value.Length)
                                throw new InvalidOperationException("Invalid JSON unicode escape.");
                            var hex = value.Substring(i + 1, 4);
                            result.Append((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            i += 4;
                            break;
                        default:
                            result.Append(escaped);
                            break;
                    }
                }
                return result.ToString();
            }

            private static Type ResolveComponentType(string componentTypeName)
            {
                var requested = (componentTypeName ?? "").Trim();
                if (string.IsNullOrWhiteSpace(requested))
                    throw new InvalidOperationException("componentType is required.");

                var matches = new List<Type>();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (var i = 0; i < assemblies.Length; i++)
                {
                    Type[] types;
                    try
                    {
                        types = assemblies[i].GetTypes();
                    }
                    catch
                    {
                        continue;
                    }

                    for (var j = 0; j < types.Length; j++)
                    {
                        var type = types[j];
                        if (type == null || !typeof(Component).IsAssignableFrom(type) || type.IsAbstract)
                            continue;
                        if (string.Equals(type.FullName, requested, StringComparison.Ordinal)
                            || string.Equals(type.Name, requested, StringComparison.Ordinal))
                        {
                            matches.Add(type);
                        }
                    }
                }

                if (matches.Count == 0)
                    throw new InvalidOperationException("Component type not found: " + requested);
                if (matches.Count > 1)
                    throw new InvalidOperationException("Component type is ambiguous: " + requested + ". Use a full type name.");
                return matches[0];
            }

    #if UNITY_EDITOR
            private static void MarkSceneObjectChanged(GameObject gameObject)
            {
                if (gameObject == null)
                    return;
                UnityEditor.EditorUtility.SetDirty(gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }

            private static void MarkComponentChanged(Component component)
            {
                if (component == null)
                    return;
                UnityEditor.EditorUtility.SetDirty(component);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            }
    #endif

            private static PieUnityRef[] QuerySceneObjects(PieUnityQueryPayload payload)
            {
                var limit = Math.Max(1, payload.limit <= 0 ? 20 : payload.limit);
                var results = new List<PieUnityRef>();
                var scene = SceneManager.GetActiveScene();
                if (!scene.IsValid())
                    return new PieUnityRef[0];

                var roots = scene.GetRootGameObjects();
                for (var i = 0; i < roots.Length; i++)
                {
                    CollectMatches(roots[i], payload, results, limit);
                    if (results.Count >= limit)
                        break;
                }

                return results.ToArray();
            }

            private static void CollectMatches(GameObject gameObject, PieUnityQueryPayload payload, List<PieUnityRef> results, int limit)
            {
                if (gameObject == null || results.Count >= limit)
                    return;

                if (Matches(gameObject, payload))
                    results.Add(ToRef(gameObject));

                var transform = gameObject.transform;
                for (var i = 0; i < transform.childCount && results.Count < limit; i++)
                    CollectMatches(transform.GetChild(i).gameObject, payload, results, limit);
            }

            private static bool Matches(GameObject gameObject, PieUnityQueryPayload payload)
            {
                if (gameObject == null)
                    return false;

                if (!string.IsNullOrWhiteSpace(payload.scope)
                    && !string.Equals(payload.scope, "scene_object", StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrWhiteSpace(payload.name)
                    && !string.Equals(gameObject.name, payload.name, StringComparison.Ordinal))
                    return false;

                if (!string.IsNullOrWhiteSpace(payload.path)
                    && !string.Equals(BuildHierarchyPath(gameObject.transform), payload.path, StringComparison.Ordinal))
                    return false;

                if (!string.IsNullOrWhiteSpace(payload.type))
                {
                    var components = gameObject.GetComponents<Component>();
                    var matched = false;
                    for (var i = 0; i < components.Length; i++)
                    {
                        var typeName = components[i] != null ? components[i].GetType().Name : "";
                        var fullName = components[i] != null ? components[i].GetType().FullName : "";
                        if (string.Equals(typeName, payload.type, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(fullName, payload.type, StringComparison.OrdinalIgnoreCase))
                        {
                            matched = true;
                            break;
                        }
                    }
                    if (!matched)
                        return false;
                }

                return true;
            }

            private static PieUnityRef ResolveTarget(PieUnityRef target)
            {
                if (HasRef(target))
                    return target;

                return null;
            }

            private static bool HasRef(PieUnityRef target)
            {
                if (target == null)
                    return false;

                return !string.IsNullOrWhiteSpace(target.id)
                    || !string.IsNullOrWhiteSpace(target.path)
                    || !string.IsNullOrWhiteSpace(target.name);
            }

            private static GameObject ResolveSceneObject(PieUnityRef target)
            {
                if (target == null)
                    return null;

                var scene = SceneManager.GetActiveScene();
                if (!scene.IsValid())
                    return null;

                var roots = scene.GetRootGameObjects();
                for (var i = 0; i < roots.Length; i++)
                {
                    var resolved = ResolveSceneObjectRecursive(roots[i], target);
                    if (resolved != null)
                        return resolved;
                }
                return null;
            }

            private static GameObject ResolveSceneObjectRecursive(GameObject gameObject, PieUnityRef target)
            {
                if (gameObject == null)
                    return null;

                if (string.Equals(target.id, gameObject.GetInstanceID().ToString(), StringComparison.Ordinal)
                    || (!string.IsNullOrWhiteSpace(target.path) && string.Equals(target.path, BuildHierarchyPath(gameObject.transform), StringComparison.Ordinal)))
                    return gameObject;

                var transform = gameObject.transform;
                for (var i = 0; i < transform.childCount; i++)
                {
                    var resolved = ResolveSceneObjectRecursive(transform.GetChild(i).gameObject, target);
                    if (resolved != null)
                        return resolved;
                }
                return null;
            }

            private static GameObject ResolveSceneObjectByQuery(string nameOrPath)
            {
                var payload = new PieUnityQueryPayload
                {
                    scope = "scene_object",
                    name = nameOrPath.IndexOf('/') >= 0 ? "" : nameOrPath,
                    path = nameOrPath.IndexOf('/') >= 0 ? nameOrPath : "",
                    limit = 2,
                };
                var refs = QuerySceneObjects(payload);
                if (refs.Length != 1)
                    return null;
                return ResolveSceneObject(refs[0]);
            }

            private static PieUnityRef ToRef(GameObject gameObject)
            {
                return new PieUnityRef
                {
                    scope = "scene_object",
                    mode = PieUnityCapabilityRegistry.Mode,
                    id = gameObject.GetInstanceID().ToString(),
                    path = BuildHierarchyPath(gameObject.transform),
                    name = gameObject.name ?? "",
                    type = gameObject.GetType().FullName ?? "UnityEngine.GameObject",
                    scene = gameObject.scene.path ?? gameObject.scene.name ?? "",
                    parentPath = gameObject.transform.parent != null ? BuildHierarchyPath(gameObject.transform.parent) : "",
                };
            }

            private static string[] GetComponentTypes(GameObject gameObject)
            {
                var components = gameObject.GetComponents<Component>();
                var items = new List<string>();
                for (var i = 0; i < components.Length; i++)
                {
                    var type = components[i] != null ? components[i].GetType().FullName : "";
                    if (!string.IsNullOrWhiteSpace(type))
                        items.Add(type);
                }
                return items.ToArray();
            }

            private static string BuildHierarchyPath(Transform transform)
            {
                if (transform == null)
                    return "";

                var names = new List<string>();
                var current = transform;
                while (current != null)
                {
                    names.Add(current.name ?? "");
                    current = current.parent;
                }
                names.Reverse();
                return string.Join("/", names.ToArray());
            }

        }
}

namespace Pie
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using UnityEngine;

    // Merged from Runtime/UnityCapabilities/PieUnityInstanceRegistry.cs
    public static class PieUnityInstanceRegistry
        {
            private static readonly object FileSyncRoot = new object();
            private const int IoRetryCount = 5;
            private const int IoRetryDelayMs = 25;

            public static void Register(string instanceId, string projectPath, string projectName, string mode, int port, string token, string productName = "", string applicationIdentifier = "")
            {
                try
                {
                    lock (FileSyncRoot)
                    {
                        var pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        WriteInstanceFile(new PieUnityInstance
                        {
                            instanceId = instanceId,
                            projectPath = projectPath,
                            projectName = projectName,
                            displayName = string.IsNullOrWhiteSpace(productName) ? projectName : productName,
                            productName = productName,
                            applicationIdentifier = applicationIdentifier,
                            mode = mode,
                            port = port,
                            token = token,
                            pid = pid,
                            lastSeenUnix = now,
                            version = PieUnityCapabilitiesConstants.Version,
                            packageVersion = PieUnityCapabilitiesConstants.Version,
                        });
                    }
                }
                catch (Exception ex)
                {
                    PieDiagnostics.Warning($"[PieUnityInstanceRegistry] Register failed: {ex.Message}");
                }
            }

            public static void Unregister(string instanceId)
            {
                try
                {
                    lock (FileSyncRoot)
                    {
                        TryDeleteInstanceFile(instanceId);
                    }
                }
                catch (Exception ex)
                {
                    PieDiagnostics.Verbose($"[PieUnityInstanceRegistry] Unregister cleanup skipped: {ex.Message}");
                }
            }

            public static PieUnityInstance[] ReadAll()
            {
                try
                {
                    lock (FileSyncRoot)
                    {
                        return SelectDiscoverableOwners(ReadAllInternal()).ToArray();
                    }
                }
                catch
                {
                    return new PieUnityInstance[0];
                }
            }

            public static PieUnityInstance[] ReadAllActive()
            {
                try
                {
                    lock (FileSyncRoot)
                    {
                        return ReadAllInternal().ToArray();
                    }
                }
                catch
                {
                    return new PieUnityInstance[0];
                }
            }

            public static PieUnityDiscoverableSnapshot GetCurrentProcessSnapshot(string projectPath)
            {
                try
                {
                    lock (FileSyncRoot)
                    {
                        var pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                        var normalizedProjectPath = NormalizeProjectPath(projectPath);
                        var active = ReadAllInternal();
                        var local = active.FindAll(item =>
                            item != null
                            && item.pid == pid
                            && (string.IsNullOrWhiteSpace(normalizedProjectPath) || string.Equals(NormalizeProjectPath(item.projectPath), normalizedProjectPath, StringComparison.OrdinalIgnoreCase)));

                        var owner = SelectOwner(local);
                        var runtimeActive = local.Exists(item => string.Equals(item.mode, "runtime", StringComparison.OrdinalIgnoreCase));
                        var editorActive = local.Exists(item => string.Equals(item.mode, "editor", StringComparison.OrdinalIgnoreCase));
                        return new PieUnityDiscoverableSnapshot
                        {
                            discoverableOwner = owner,
                            runtimeActive = runtimeActive,
                            editorActive = editorActive,
                            editorSuppressedByRuntime = runtimeActive && editorActive && owner != null && string.Equals(owner.mode, "runtime", StringComparison.OrdinalIgnoreCase),
                        };
                    }
                }
                catch
                {
                    return new PieUnityDiscoverableSnapshot();
                }
            }

            private static List<PieUnityInstance> ReadAllInternal()
            {
                var directoryPath = PieUnityCapabilitiesConstants.InstancesDirectory;
                if (!Directory.Exists(directoryPath))
                    return new List<PieUnityInstance>();

                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var next = new List<PieUnityInstance>();
                var filePaths = Directory.GetFiles(directoryPath, "*.json");
                for (var i = 0; i < filePaths.Length; i++)
                {
                    var item = TryReadInstanceFile(filePaths[i]);
                    if (!IsValidIdentity(item))
                    {
                        TryDeleteStaleFile(filePaths[i]);
                        continue;
                    }
                    if (IsExpired(item, now))
                    {
                        TryDeleteStaleFile(filePaths[i]);
                        continue;
                    }
                    next.Add(item);
                }

                return next;
            }

            private static List<PieUnityInstance> SelectDiscoverableOwners(List<PieUnityInstance> active)
            {
                var next = new List<PieUnityInstance>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < active.Count; i++)
                {
                    var item = active[i];
                    var key = NormalizeProjectPath(item != null ? item.projectPath : "");
                    if (string.IsNullOrWhiteSpace(key))
                        key = item != null ? (item.instanceId ?? "") : "";
                    if (!seen.Add(key))
                        continue;

                    var matches = active.FindAll(candidate =>
                        string.Equals(NormalizeProjectPath(candidate != null ? candidate.projectPath : ""), key, StringComparison.OrdinalIgnoreCase));
                    var owner = SelectOwner(matches);
                    if (owner != null)
                        next.Add(owner);
                }

                return next;
            }

            private static PieUnityInstance SelectOwner(List<PieUnityInstance> candidates)
            {
                if (candidates == null || candidates.Count == 0)
                    return null;

                PieUnityInstance best = null;
                for (var i = 0; i < candidates.Count; i++)
                {
                    var item = candidates[i];
                    if (item == null)
                        continue;
                    if (best == null)
                    {
                        best = item;
                        continue;
                    }

                    var itemIsRuntime = string.Equals(item.mode, "runtime", StringComparison.OrdinalIgnoreCase);
                    var bestIsRuntime = string.Equals(best.mode, "runtime", StringComparison.OrdinalIgnoreCase);
                    if (itemIsRuntime != bestIsRuntime)
                    {
                        if (itemIsRuntime)
                            best = item;
                        continue;
                    }

                    if (item.lastSeenUnix > best.lastSeenUnix)
                    {
                        best = item;
                        continue;
                    }

                    if (item.lastSeenUnix == best.lastSeenUnix
                        && string.Compare(item.instanceId ?? "", best.instanceId ?? "", StringComparison.OrdinalIgnoreCase) < 0)
                        best = item;
                }

                return best;
            }

            private static string NormalizeProjectPath(string projectPath)
            {
                return (projectPath ?? "").Replace("\\", "/").Trim();
            }

            private static void WriteInstanceFile(PieUnityInstance instance)
            {
                if (!IsValidIdentity(instance))
                    throw new InvalidOperationException("Cannot register a Unity instance without a valid identity.");

                Directory.CreateDirectory(PieUnityCapabilitiesConstants.InstancesDirectory);
                var targetPath = BuildInstanceFilePath(instance.instanceId);
                var json = JsonUtility.ToJson(instance, true);
                var tempPath = targetPath + "." + System.Diagnostics.Process.GetCurrentProcess().Id + "." + Guid.NewGuid().ToString("N") + ".tmp";

                Exception lastError = null;
                for (var attempt = 0; attempt < IoRetryCount; attempt++)
                {
                    try
                    {
                        using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                        using (var writer = new StreamWriter(stream, new UTF8Encoding(true)))
                        {
                            writer.Write(json);
                            writer.Flush();
                            stream.Flush(true);
                        }

                        if (File.Exists(targetPath))
                            File.Replace(tempPath, targetPath, null, true);
                        else
                            File.Move(tempPath, targetPath);

                        if (File.Exists(tempPath))
                            File.Delete(tempPath);
                        return;
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                        try
                        {
                            if (File.Exists(tempPath))
                                File.Delete(tempPath);
                        }
                        catch
                        {
                            // Ignore temp cleanup failures.
                        }

                        System.Threading.Thread.Sleep(IoRetryDelayMs * (attempt + 1));
                    }
                }

                throw lastError ?? new IOException("Failed to write Unity instance registry.");
            }

            private static PieUnityInstance TryReadInstanceFile(string filePath)
            {
                for (var attempt = 0; attempt < IoRetryCount; attempt++)
                {
                    try
                    {
                        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                        using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                        {
                            var json = reader.ReadToEnd();
                            if (string.IsNullOrWhiteSpace(json))
                                return null;
                            return JsonUtility.FromJson<PieUnityInstance>(json);
                        }
                    }
                    catch (IOException)
                    {
                        System.Threading.Thread.Sleep(IoRetryDelayMs * (attempt + 1));
                    }
                    catch
                    {
                        return null;
                    }
                }

                return null;
            }

            private static string BuildInstanceFilePath(string instanceId)
            {
                return Path.Combine(PieUnityCapabilitiesConstants.InstancesDirectory, instanceId + ".json");
            }

            private static bool IsExpired(PieUnityInstance item, long now)
            {
                if (item == null)
                    return true;

                if (now - item.lastSeenUnix > PieUnityCapabilitiesConstants.RegistryTtlSeconds)
                    return true;

                if (!IsProcessAlive(item.pid))
                    return true;

                return false;
            }

            private static bool IsValidIdentity(PieUnityInstance item)
            {
                if (item == null)
                    return false;
                if (string.IsNullOrWhiteSpace(item.instanceId))
                    return false;
                if (string.IsNullOrWhiteSpace(item.projectPath))
                    return false;
                if (string.IsNullOrWhiteSpace(item.mode))
                    return false;
                if (item.port <= 0)
                    return false;
                if (item.pid <= 0)
                    return false;
                if (item.lastSeenUnix <= 0)
                    return false;
                return true;
            }

            private static void TryDeleteInstanceFile(string instanceId)
            {
                if (string.IsNullOrWhiteSpace(instanceId))
                    return;
                TryDeleteStaleFile(BuildInstanceFilePath(instanceId));
            }

            private static void TryDeleteStaleFile(string filePath)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return;

                for (var attempt = 0; attempt < IoRetryCount; attempt++)
                {
                    try
                    {
                        if (File.Exists(filePath))
                            File.Delete(filePath);
                        return;
                    }
                    catch (IOException)
                    {
                        System.Threading.Thread.Sleep(IoRetryDelayMs * (attempt + 1));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        System.Threading.Thread.Sleep(IoRetryDelayMs * (attempt + 1));
                    }
                    catch
                    {
                        return;
                    }
                }
            }

            private static bool IsProcessAlive(int pid)
            {
                try
                {
                    var process = System.Diagnostics.Process.GetProcessById(pid);
                    return process != null && !process.HasExited;
                }
                catch
                {
                    return false;
                }
            }
        }
}
// </pie-unity-merged-runtime>
