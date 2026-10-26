using Pie;
using Pie.Unity.Capabilities;

#if UNITY_EDITOR
// Self-registering capability mount. Kept in its own source file so the
// registration follows this asset's import lifecycle independent of edits to
// PieUnityCapabilitiesBootstrap (whose incremental recompile behavior for
// file: package sources can lag behind direct source edits).
[UnityEditor.InitializeOnLoad]
#endif
internal static class PieUnityScreenshotRegistration
{
#if UNITY_EDITOR
    static PieUnityScreenshotRegistration()
    {
        Register();
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += PieUnityScreenshotRuntime.ResetJobs;
        UnityEditor.EditorApplication.playModeStateChanged += state => {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode || state == UnityEditor.PlayModeStateChange.ExitingEditMode)
                PieUnityScreenshotRuntime.ResetJobs();
        };
    }
#endif

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterOnRuntimeLoad()
    {
        PieUnityScreenshotRuntime.ResetJobs();
        Register();
    }

    private static void Register()
    {
        UnityEngine.Application.quitting -= PieUnityScreenshotRuntime.ResetJobs;
        UnityEngine.Application.quitting += PieUnityScreenshotRuntime.ResetJobs;
#if !UNITY_WEBGL || UNITY_EDITOR
        // WebGL without managed worker threads retains the synchronous capability.
        RegisterJobRpc("screenshot.begin", PieUnityScreenshotRuntime.BeginJson, true);
        RegisterJobRpc("screenshot.poll", PieUnityScreenshotRuntime.PollJson, false);
        RegisterJobRpc("screenshot.cancel", PieUnityScreenshotRuntime.CancelJson, false);
#endif
        PieScreenshotTimingProbe.Register();
        PieUnityCapabilityRegistry.RegisterTool(
            "unity_screenshot",
            "unity",
            "Capture the current game view as a PNG screenshot rendered from the active camera (or a named camera) and saved under the persistent root at PieScreenshots/. Captures are opaque (RGB, no alpha) and sRGB-encoded in both gamma and linear color space projects. Pass includeUI=false to capture via a camera render with UI canvases temporarily disabled (chat panels, HUD); that fresh render can differ from the final presented frame. Pair with read_file (root=persistent) to visually inspect the capture.",
            "editor+runtime",
            true,
            false,
            null,
            new[]
            {
                new PieUnityParameterDescriptor { name = "width", type = "integer", required = false },
                new PieUnityParameterDescriptor { name = "height", type = "integer", required = false },
                new PieUnityParameterDescriptor { name = "cameraName", type = "string", required = false },
                new PieUnityParameterDescriptor { name = "path", type = "string", required = false },
                new PieUnityParameterDescriptor { name = "includeUI", type = "boolean", required = false },
            },
            PieUnityScreenshotRuntime.ScreenshotJson,
            capabilityKind: "inspect");
    }

    private static void RegisterJobRpc(string name, System.Func<string, string> handler, bool begin)
    {
        System.Func<string, string> dispatch = json => PieDevRpcDispatcher.InvokeSync(() => handler(json));
        var parameters = new System.Collections.Generic.List<PieUnityParameterDescriptor> {
            new PieUnityParameterDescriptor { name = "requestId", type = "string", required = true },
        };
        if (begin) parameters.AddRange(new[] {
            new PieUnityParameterDescriptor { name = "width", type = "integer" },
            new PieUnityParameterDescriptor { name = "height", type = "integer" },
            new PieUnityParameterDescriptor { name = "cameraName", type = "string" },
            new PieUnityParameterDescriptor { name = "path", type = "string" },
            new PieUnityParameterDescriptor { name = "includeUI", type = "boolean" },
        });
        PieUnityCapabilityRegistry.RegisterRpc(name, "screenshot", "Private screenshot job lifecycle.",
            "editor+runtime", name == "screenshot.poll", false, null, parameters.ToArray(), dispatch, requiresMainThread: true);
        // rpc.list's legacy method inventory is separate from the capability registry.
        PieDevRpc.Register(name, dispatch);
    }
}
