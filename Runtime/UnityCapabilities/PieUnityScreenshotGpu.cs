using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using global::UnityEngine.Experimental.Rendering;
using global::UnityEngine.Rendering;

namespace Pie.Unity.Capabilities
{
    public static partial class PieUnityScreenshotRuntime
    {
        // Deliberately exclude Editor (including play mode/domain reload) and
        // batch contexts. Unsupported is decided BEFORE any capture/submission.
        private static bool CanUseGpuReadback()
        {
#if UNITY_EDITOR || UNITY_WEBGL
            return false;
#else
            var format = IsLinearColorSpace() ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
            return Application.isPlaying && !Application.isBatchMode && SystemInfo.supportsAsyncGPUReadback
                && SystemInfo.IsFormatSupported(format, FormatUsage.Render)
                && SystemInfo.IsFormatSupported(format, FormatUsage.ReadPixels)
                && SystemInfo.IsFormatSupported(GraphicsFormat.R8G8B8A8_UNorm, FormatUsage.Render);
#endif
        }

        private static void StartGpuCapture(PieScreenshotJobs.CaptureOwner owner, ScreenshotPayload payload,
            int width, int height, string destination, string root)
        {
            var capture = new GpuCapture(owner, payload, width, height, destination, root);
            if (string.IsNullOrWhiteSpace(payload.cameraName) && payload.includeUI)
            {
                // A screenshot-specific owner, not the bridge, drives EOF and
                // queued cancellation without requiring any poll RPCs.
                var go = new GameObject("Pie screenshot readback") { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    global::UnityEngine.Object.DontDestroyOnLoad(go);
                    go.AddComponent<ScreenshotPump>().Queue(capture);
                }
                catch { global::UnityEngine.Object.Destroy(go); capture.AbortQueued(); throw; }
            }
            else capture.Submit(); // Explicit cameras retain fresh Render semantics.
        }

        private sealed class ScreenshotPump : MonoBehaviour
        {
            private GpuCapture capture;
            internal void Queue(GpuCapture value) { capture = value; StartCoroutine(EndOfFrame()); }
            private IEnumerator EndOfFrame()
            {
                yield return new WaitForEndOfFrame();
                var value = capture;
                capture = null; // Callback state, not this component, owns submitted work.
                try { value?.Submit(); }
                finally { global::UnityEngine.Object.Destroy(gameObject); }
            }
            private void Update()
            {
                if (capture != null && !capture.Owner.Pending)
                {
                    capture.AbortQueued(); capture = null;
                    StopAllCoroutines();
                    global::UnityEngine.Object.Destroy(gameObject);
                }
            }
            private void OnDestroy() { capture?.AbortQueued(); capture = null; }
        }

        private sealed class GpuCapture
        {
            internal readonly PieScreenshotJobs.CaptureOwner Owner;
            private readonly ScreenshotPayload payload;
            private readonly int width, height;
            private readonly string destination, root;
            private RenderTexture source, bounce;
            private bool submitted, completed, flipRows;
            internal GpuCapture(PieScreenshotJobs.CaptureOwner owner, ScreenshotPayload payload,
                int width, int height, string destination, string root)
            { Owner = owner; this.payload = payload; this.width = width; this.height = height; this.destination = destination; this.root = root; }

            internal void AbortQueued()
            {
                if (submitted || completed) return;
                completed = true;
                Owner.Fail(new InvalidOperationException("Screenshot end-of-frame owner stopped."));
            }
            private static RenderTexture Allocate(int w, int h, int depth, bool srgb)
            {
                var descriptor = new RenderTextureDescriptor(w, h,
                    srgb ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm, depth);
                return RenderTexture.GetTemporary(descriptor);
            }
            internal void Submit()
            {
                if (completed || submitted) return;
                if (!Owner.Pending) { AbortQueued(); return; }
                using (var scope = PieScreenshotTimingProbe.BeginCapture(captureOnly: true, phase: "gpu-setup"))
                {
                    PieScreenshotTimingProbe.Dimensions(width, height);
                    var previousActive = RenderTexture.active;
                    var previousWrite = GL.sRGBWrite;
                    Camera camera = null;
                    RenderTexture previousTarget = null;
                    List<Canvas> disabled = null;
                    Exception failure = null;
                    bool targetBorrowed = false, cameraStateRestored = false;
                    void Restore(Action setter)
                    {
                        try { setter(); }
                        catch (Exception ex) { failure = failure ?? ex; }
                    }
                    void RestoreCameraState()
                    {
                        if (cameraStateRestored) return;
                        cameraStateRestored = true;
                        if (targetBorrowed) Restore(() => camera.targetTexture = previousTarget);
                        if (disabled != null)
                            foreach (var canvas in disabled)
                                if (canvas != null) Restore(() => canvas.enabled = true);
                    }
                    try
                    {
                        var linear = IsLinearColorSpace();
                        var gameView = string.IsNullOrWhiteSpace(payload.cameraName) && payload.includeUI;
                        if (gameView)
                        {
                            // IntoRT uses the backend's screen origin. Metal native
                            // parity proves a row reversal here, unlike camera.Render.
                            flipRows = SystemInfo.graphicsUVStartsAtTop;
                            source = Allocate(Screen.width, Screen.height, 0, linear);
                            using (PieScreenshotTimingProbe.Measure(PieScreenshotTimingProbe.Stage.Capture))
                                ScreenCapture.CaptureScreenshotIntoRenderTexture(source);
                            Owner.Job.Camera = Owner.Job.Mode = "game-view";
                        }
                        else
                        {
                            camera = ResolveCamera(payload.cameraName);
                            if (camera == null) throw new InvalidOperationException("No enabled camera found for screenshot.");
                            previousTarget = camera.targetTexture;
                            targetBorrowed = true;
                            if (!payload.includeUI)
                            {
                                disabled = new List<Canvas>();
                                foreach (var canvas in global::UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                                    if (canvas != null && canvas.enabled) { disabled.Add(canvas); canvas.enabled = false; }
                            }
                            source = Allocate(width, height, 24, false);
                            camera.targetTexture = source;
                            try
                            {
                                using (PieScreenshotTimingProbe.Measure(PieScreenshotTimingProbe.Stage.Render)) camera.Render();
                            }
                            catch (Exception ex) { failure = ex; }
                            finally { RestoreCameraState(); } // UI is restored immediately after Render, even on failure.
                            if (failure != null) throw failure;
                            Owner.Job.Camera = camera.name; Owner.Job.Mode = "camera-render";
                        }
                        // Camera linear target -> sRGB bounce is exactly one transfer.
                        // Row orientation is separately snapshotted for game-view only;
                        // explicit camera pixels retain their original row order.
                        bounce = Allocate(width, height, 0, linear);
                        GL.sRGBWrite = linear;
                        using (PieScreenshotTimingProbe.Measure(PieScreenshotTimingProbe.Stage.Blit)) Graphics.Blit(source, bounce);
                    }
                    catch (Exception ex) { failure = failure ?? ex; }
                    finally
                    {
                        // Isolate every borrowed setter: one broken object must not
                        // skip other canvases or global state, or mask the setup error.
                        RestoreCameraState();
                        Restore(() => RenderTexture.active = previousActive);
                        Restore(() => GL.sRGBWrite = previousWrite);
                    }
                    if (failure != null)
                    {
                        Cleanup(); completed = true; Owner.Fail(failure);
                        return; // Restoration failure must never submit native work.
                    }
                    try
                    {
                        submitted = true;
                        AsyncGPUReadback.Request(bounce, 0, Complete);
                    }
                    catch (Exception ex)
                    {
                        // Request setup exceptions are failures, never a second capture.
                        Cleanup(); completed = true; Owner.Fail(ex);
                        return;
                    }
                    // No throwable borrowed-state restoration after ownership transfer.
                    scope.Succeed();
                }
            }
            private void Complete(AsyncGPUReadbackRequest request)
            {
                if (completed) return;
                completed = true;
                byte[] pixels = null;
                Exception failure = null;
                using (var scope = PieScreenshotTimingProbe.BeginCapture(captureOnly: true, phase: "gpu-callback-copy"))
                {
                    PieScreenshotTimingProbe.Dimensions(width, height);
                    try
                    {
                        if (request.hasError) throw new InvalidOperationException("Screenshot GPU readback failed.");
                        if (Owner.Pending)
                        {
                            if (request.width != width || request.height != height || request.layerCount != 1)
                                throw new InvalidOperationException("Unexpected screenshot GPU dimensions.");
                            using (PieScreenshotTimingProbe.Measure(PieScreenshotTimingProbe.Stage.PixelCopy))
                            {
                                var data = request.GetData<byte>();
                                if (data.Length != checked(width * height * 4)) throw new InvalidOperationException("Unexpected screenshot GPU byte count.");
                                pixels = data.ToArray(); // valid ONLY within completion lifetime
                            }
                        }
                    }
                    catch (Exception ex) { failure = ex; }
                    finally { var cleanupFailure = Cleanup(); failure = failure ?? cleanupFailure; } // release precedes worker admission
                    if (failure == null) scope.Succeed();
                }
                if (failure != null) Owner.Fail(failure);
                else if (pixels == null) Owner.Release();
                else Owner.Submit(MakeWorker(pixels, GraphicsFormat.R8G8B8_UNorm, width, height, destination, root, rgba: true, flipRows: flipRows));
            }
            private Exception Cleanup()
            {
                // Each owned RT is attempted once even if another release throws.
                // Clear references before calling Unity; a throw may follow a release.
                Exception failure = null;
                var temporary = bounce; bounce = null;
                try { if (temporary != null) RenderTexture.ReleaseTemporary(temporary); }
                catch (Exception ex) { failure = ex; }
                temporary = source; source = null;
                try { if (temporary != null) RenderTexture.ReleaseTemporary(temporary); }
                catch (Exception ex) { failure = failure ?? ex; }
                return failure;
            }
        }

        // Managed-only packing; RGBA8 is explicitly selected, never inferred from
        // platform ARGB32/BGRA defaults. Drop alpha and apply only the snapshotted
        // game-view origin correction; never infer a blanket camera flip.
        private static byte[] PackRgb(byte[] rgba, int width, int height, bool flipRows)
        {
            var count = checked(width * height);
            if (rgba.Length != checked(count * 4)) throw new InvalidOperationException("Invalid RGBA screenshot data.");
            var rgb = new byte[checked(count * 3)];
            for (int y = 0; y < height; y++)
            {
                int i = (flipRows ? height - 1 - y : y) * width * 4;
                int j = y * width * 3;
                for (int x = 0; x < width; x++, i += 4, j += 3)
                { rgb[j] = rgba[i]; rgb[j + 1] = rgba[i + 1]; rgb[j + 2] = rgba[i + 2]; }
            }
            return rgb;
        }
    }
}
