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
    }
#endif

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterOnRuntimeLoad()
    {
        Register();
    }

    private static void Register()
    {
        PieUnityCapabilityRegistry.RegisterTool(
            "unity_screenshot",
            "unity",
            "Capture the current game view as a PNG screenshot rendered from the active camera (or a named camera) and saved under the persistent root at PieScreenshots/. Pair with read_file (root=persistent) to visually inspect the capture.",
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
            },
            PieUnityScreenshotRuntime.ScreenshotJson,
            capabilityKind: "inspect");
    }
}
