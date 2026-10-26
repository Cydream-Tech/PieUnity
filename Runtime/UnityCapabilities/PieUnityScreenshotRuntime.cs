using System;
using System.IO;
using UnityEngine;

namespace Pie.Unity.Capabilities
{
    /// <summary>
    /// Captures the game view (active camera render) as a PNG screenshot saved
    /// under Application.persistentDataPath/PieScreenshots. Pair with the
    /// runtime read_file tool (root=persistent) to visually inspect captures.
    /// </summary>
    public static partial class PieUnityScreenshotRuntime
    {
        private const int MinDimension = 64;
        private const int MaxDimension = 3840;

        [Serializable]
        private sealed class ScreenshotPayload
        {
            public string requestId;
            public int width;
            public int height;
            public string cameraName;
            public string path;
            // JsonUtility keeps this field initializer when the key is absent,
            // so omitting includeUI preserves the historical game-view capture.
            public bool includeUI = true;
        }

        [Serializable]
        private sealed class ScreenshotResult
        {
            public string summary = "";
            public string root = "persistent";
            public string path = "";
            public string absolutePath = "";
            public int width;
            public int height;
            public int bytes;
            public string camera = "";
            public string mode = "";
            public bool found = true;
        }

        public static string ScreenshotJson(string argsJson)
        {
            using (var capture = PieScreenshotTimingProbe.BeginCapture())
            {
                var result = ScreenshotJsonCore(argsJson);
                capture.Succeed();
                return result;
            }
        }

        private static string ScreenshotJsonCore(string argsJson)
        {
            // FromJsonOverwrite into a pre-constructed instance: the field
            // initializer for includeUI runs in the constructor, so an absent
            // key keeps the historical default (true) regardless of how a
            // given Unity version treats initializers in FromJson (UUM-35858).
            var payload = new ScreenshotPayload();
            if (!string.IsNullOrWhiteSpace(argsJson))
                JsonUtility.FromJsonOverwrite(argsJson, payload);
            var width = Mathf.Clamp(payload.width > 0 ? payload.width : 1280, MinDimension, MaxDimension);
            var height = Mathf.Clamp(payload.height > 0 ? payload.height : 720, MinDimension, MaxDimension);
            PieScreenshotTimingProbe.Dimensions(width, height);

            var bytes = Capture(payload, width, height, out var cameraUsed, out var captureMode);
            var persistentRoot = Application.persistentDataPath;
            var directory = Path.Combine(persistentRoot, "PieScreenshots");
            var absolutePath = PieScreenshotFiles.Destination(directory, SanitizeRelativePath(payload.path));
            using (PieScreenshotTimingProbe.Measure(PieScreenshotTimingProbe.Stage.Write))
                absolutePath = PieScreenshotFiles.WriteUnique(absolutePath, bytes, persistentRoot);
            return JsonUtility.ToJson(BuildResult(directory, absolutePath, width, height, bytes.Length, cameraUsed, captureMode));
        }

        private static byte[] Capture(ScreenshotPayload payload, int width, int height, out string cameraUsed, out string captureMode, Func<Texture2D, byte[]> snapshot = null)
        {
            byte[] bytes;
            // An explicit cameraName is user intent: honor it even in play
            // mode via a targeted camera render instead of silently falling
            // back to the game view.
            var explicitCameraRequested = !string.IsNullOrWhiteSpace(payload.cameraName);
            // includeUI=false routes through a camera render even in play mode:
            // UI canvases are temporarily disabled for that render (chat
            // panels, HUDs) which the game-view capture always includes.
            if (Application.isPlaying && !explicitCameraRequested && payload.includeUI)
            {
                bytes = CaptureGameView(width, height, payload.cameraName, out cameraUsed, out captureMode, snapshot);
            }
            else
            {
                bytes = CaptureViaCamera(payload.cameraName, width, height, payload.includeUI, out cameraUsed, snapshot);
                captureMode = "camera-render";
            }

            return bytes;
        }

        private static ScreenshotResult BuildResult(string directory, string absolutePath, int width, int height, int byteCount, string cameraUsed, string captureMode)
        {
            // The reported relative path must track the uniquified file: derive
            // it from the final absolute path so path/absolutePath never diverge.
            var finalRelative = Path.GetRelativePath(directory, absolutePath).Replace('\\', '/');
            var result = new ScreenshotResult
            {
                summary = $"Captured {width}x{height} screenshot ({captureMode}, {byteCount} bytes).",
                path = $"PieScreenshots/{finalRelative}",
                absolutePath = absolutePath.Replace('\\', '/'),
                width = width,
                height = height,
                bytes = byteCount,
                camera = cameraUsed,
                mode = captureMode,
            };
            return result;
        }

        private static readonly PieScreenshotJobs Jobs = new PieScreenshotJobs();
        internal static void ResetJobs() => Jobs.Reset();

        [Serializable] private sealed class JobReport
        {
            public string requestId, status, error;
            public ScreenshotResult result;
        }
        private static ScreenshotPayload Parse(string json)
        {
            var payload = new ScreenshotPayload();
            if (!string.IsNullOrWhiteSpace(json)) JsonUtility.FromJsonOverwrite(json, payload);
            return payload;
        }
        internal static string BeginJson(string json)
        {
            var payload = Parse(json);
            var job = Jobs.BeginDeferred(payload.requestId, owner =>
            {
                var owned = owner.Job;
                var gpuReadback = CanUseGpuReadback();
                using (var scope = PieScreenshotTimingProbe.BeginCapture(captureOnly: true, phase: gpuReadback ? "gpu-admission" : null))
                {
                    var width = Mathf.Clamp(payload.width > 0 ? payload.width : 1280, MinDimension, MaxDimension);
                    var height = Mathf.Clamp(payload.height > 0 ? payload.height : 720, MinDimension, MaxDimension);
                    owned.Width = width; owned.Height = height;
                    var persistentRoot = Application.persistentDataPath;
                    owned.Directory = Path.Combine(persistentRoot, "PieScreenshots");
                    var destination = PieScreenshotFiles.Destination(owned.Directory, SanitizeRelativePath(payload.path));
                    PieScreenshotTimingProbe.Dimensions(width, height);
                    if (gpuReadback)
                    {
                        StartGpuCapture(owner, payload, width, height, destination, persistentRoot);
                        scope.Succeed();
                        return;
                    }
                    var format = default(global::UnityEngine.Experimental.Rendering.GraphicsFormat);
                    var pixels = Capture(payload, width, height, out owned.Camera, out owned.Mode, texture =>
                    {
                        format = texture.graphicsFormat;
                        // Copy NativeArray into owned managed storage before texture destruction.
                        return texture.GetRawTextureData<byte>().ToArray();
                    });
                    scope.Succeed();
                    owner.Submit(MakeWorker(pixels, format, width, height, destination, persistentRoot));
                }
            });
            return Report(payload.requestId, job);
        }
        // A separate closure factory prevents retaining payload, textures, probe
        // scopes or the main-thread capture callback in the worker's environment.
        private static Func<PieScreenshotJobs.Output> MakeWorker(byte[] pixels, UnityEngine.Experimental.Rendering.GraphicsFormat format, int width, int height, string destination, string persistentRoot, bool rgba = false, bool flipRows = false)
        {
            return () =>
            {
                // EncodeArrayToPNG is the thread-safe array encoder; use no other
                // Unity API, static probe, JsonUtility or Application on this thread.
                if (rgba) pixels = PackRgb(pixels, width, height, flipRows);
                var png = ImageConversion.EncodeArrayToPNG(pixels, format, (uint)width, (uint)height);
                return new PieScreenshotJobs.Output { Path = PieScreenshotFiles.WriteUnique(destination, png, persistentRoot), Bytes = png.Length };
            };
        }
        internal static string PollJson(string json)
        {
            var id = Parse(json).requestId;
            return Report(id, Jobs.Poll(id));
        }
        internal static string CancelJson(string json)
        {
            var id = Parse(json).requestId;
            return Report(id, Jobs.Cancel(id));
        }
        private static string Report(string id, PieScreenshotJobs.Job job)
        {
            var state = job?.Read();
            return JsonUtility.ToJson(new JobReport {
                requestId = id, status = state?.Status ?? "unknown", error = state?.Error,
                result = state?.Status == "succeeded" ? BuildResult(job.Directory, state.Result.Path, job.Width, job.Height, state.Result.Bytes, job.Camera, job.Mode) : null,
            });
        }

        /// <summary>
        /// Full-fidelity capture of the last rendered game frame, including
        /// Screen Space Overlay UI, which a camera render into a target
        /// texture would skip. Falls back to a camera render when no game
        /// frame is available.
        /// </summary>
        private static byte[] CaptureGameView(int width, int height, string cameraName, out string cameraUsed, out string captureMode, Func<Texture2D, byte[]> snapshot = null)
        {
            Texture2D captured = null;
            try
            {
                using (PieScreenshotTimingProbe.Measure(PieScreenshotTimingProbe.Stage.Capture))
                    captured = ScreenCapture.CaptureScreenshotAsTexture();
            }
            catch (Exception)
            {
                captured = null;
            }
            if (captured == null)
            {
                PieScreenshotTimingProbe.Fallback();
                captureMode = "camera-render";
                return CaptureViaCamera(cameraName, width, height, true, out cameraUsed, snapshot);
            }
            try
            {
                var readable = ScaleToSize(captured, width, height);
                try
                {
                    captureMode = "game-view";
                    cameraUsed = "game-view";
                    if (snapshot != null) return snapshot(readable);
                    using (PieScreenshotTimingProbe.Measure(PieScreenshotTimingProbe.Stage.Encode))
                        return readable.EncodeToPNG();
                }
                finally
                {
                    UnityEngine.Object.Destroy(readable);
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(captured);
            }
        }

        private static byte[] CaptureViaCamera(string cameraName, int width, int height, bool includeUI, out string cameraUsed, Func<Texture2D, byte[]> snapshot = null)
        {
            var camera = ResolveCamera(cameraName);
            if (camera == null)
            {
                var message = string.IsNullOrWhiteSpace(cameraName)
                    ? "No enabled camera found in the loaded scene to capture."
                    : $"No enabled camera named '{cameraName}' found in the loaded scene.";
                throw new InvalidOperationException(message);
            }
            cameraUsed = camera.name;
            return RenderCameraToPng(camera, width, height, includeUI, snapshot);
        }

        private static Texture2D ScaleToSize(Texture2D source, int width, int height)
        {
            // CaptureScreenshotAsTexture can return display bytes with UNorm
            // metadata. In Linear projects sampling those as linear would encode
            // them a second time. Retag only this legacy game-view input; keep
            // its bytes intact and leave camera/GPU capture paths unchanged.
            if (!IsLinearColorSpace() || UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat))
                return ScaleToSizeCore(source, width, height);

            var tagged = new Texture2D(source.width, source.height, source.format, false, false);
            try
            {
                using (PieScreenshotTimingProbe.Measure(PieScreenshotTimingProbe.Stage.PixelCopy))
                {
                    tagged.LoadRawTextureData(source.GetRawTextureData<byte>());
                    tagged.Apply(false);
                }
                return ScaleToSizeCore(tagged, width, height);
            }
            finally
            {
                UnityEngine.Object.Destroy(tagged);
            }
        }

        private static Texture2D ScaleToSizeCore(Texture2D source, int width, int height)
        {
            // A PNG must carry display-referred sRGB bytes regardless of the
            // project's color space: source pixel metadata, the blit write
            // state, and the readback must agree or dark scenes come out
            // wrong (observed live from an IL2CPP player, where the mismatch
            // plus framebuffer alpha compositing masqueraded as a ~2.2 gamma
            // wash). Blitting into an sRGB-read-write render texture with
            // sRGB writing explicitly enabled makes the GPU perform exactly
            // one linear->sRGB conversion on write. Gamma projects keep the
            // previous RGB behavior (alpha is now intentionally dropped).
            var linear = IsLinearColorSpace();
            if (!linear && source.width == width && source.height == height)
            {
                // RGB24 readback: a screenshot is opaque by nature. Keeping the
                // framebuffer's alpha (0 where nothing wrote it) produces PNGs
                // that image viewers composite onto white, which masquerades
                // as a wash on dark scenes (observed live).
                var copy = new Texture2D(width, height, TextureFormat.RGB24, false);
                try
                {
                    using (PieScreenshotTimingProbe.Measure(PieScreenshotTimingProbe.Stage.PixelCopy))
                    {
                        copy.SetPixels(source.GetPixels());
                        copy.Apply(false);
                    }
                    return copy;
                }
                catch
                {
                    // Ownership transfers to the caller only after successful
                    // initialization; destroy on failure so the native texture
                    // is not leaked (review finding).
                    UnityEngine.Object.Destroy(copy);
                    throw;
                }
            }
            var scaled = RenderTexture.GetTemporary(
                width,
                height,
                0,
                RenderTextureFormat.ARGB32,
                linear ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Default);
            var previousActive = RenderTexture.active;
            var previousSrgbWrite = GL.sRGBWrite;
            try
            {
                // Graphics.Blit honors the destination's sRGB encoding only
                // when sRGB writing is enabled; the state is global and may
                // have been left disabled by earlier rendering (e.g. image
                // effects), so scope it explicitly around the conversion blit.
                if (linear)
                    GL.sRGBWrite = true;
                using (PieScreenshotTimingProbe.Measure(PieScreenshotTimingProbe.Stage.Blit))
                    Graphics.Blit(source, scaled);
                RenderTexture.active = scaled;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                try
                {
                    using (PieScreenshotTimingProbe.Measure(PieScreenshotTimingProbe.Stage.ReadbackApply))
                    {
                        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                        texture.Apply(false);
                    }
                    return texture;
                }
                catch
                {
                    UnityEngine.Object.Destroy(texture);
                    throw;
                }
            }
            finally
            {
                RenderTexture.active = previousActive;
                GL.sRGBWrite = previousSrgbWrite;
                RenderTexture.ReleaseTemporary(scaled);
            }
        }

        private static Camera ResolveCamera(string cameraName)
        {
            if (!string.IsNullOrWhiteSpace(cameraName))
            {
                foreach (var camera in Camera.allCameras)
                {
                    if (camera != null && camera.enabled && camera.gameObject.activeInHierarchy
                        && string.Equals(camera.name, cameraName, StringComparison.Ordinal))
                        return camera;
                }
                return null;
            }

            var main = Camera.main;
            if (main != null && main.enabled && main.gameObject.activeInHierarchy)
                return main;

            foreach (var camera in Camera.allCameras)
            {
                if (camera != null && camera.enabled && camera.gameObject.activeInHierarchy)
                    return camera;
            }
            return null;
        }

        private static byte[] RenderCameraToPng(Camera camera, int width, int height, bool includeUI, Func<Texture2D, byte[]> snapshot = null)
        {
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var previousSrgbWrite = GL.sRGBWrite;
            var linear = IsLinearColorSpace();
            // includeUI=false: rendering into a target texture only skips
            // Screen Space - Overlay canvases on its own; Screen Space -
            // Camera and World Space canvases still render (observed live in
            // PieArcade, whose chat/HUD canvases are camera-space). Disable
            // every currently-enabled canvas for the duration of this
            // explicit render and restore immediately after — the render is
            // synchronous, so no presented frame ever shows the UI missing.
            // Note: an explicit camera.Render() is a fresh render whose result
            // can differ from the final presented frame; on-camera
            // post-processing behavior depends on the pipeline/effect.
            System.Collections.Generic.List<Canvas> disabledCanvases = null;
            RenderTexture renderTexture = null;
            RenderTexture bounce = null;
            byte[] png = null;
            try
            {
                if (!includeUI)
                {
                    var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                    foreach (var canvas in canvases)
                    {
                        if (canvas == null || !canvas.enabled)
                            continue;
                        // Record BEFORE disabling so a failure mid-disable can
                        // never lose the restoration entry (review finding).
                        (disabledCanvases ??= new System.Collections.Generic.List<Canvas>()).Add(canvas);
                        canvas.enabled = false;
                    }
                }
                // In a linear project the camera renders into an explicitly
                // linear target first (a fresh render into an sRGB destination
                // could interact unpredictably with on-camera effects), and
                // the readback bounces through an sRGB-read-write target for
                // exactly one linear->sRGB encode — see ScaleToSize.
                renderTexture = RenderTexture.GetTemporary(
                    width,
                    height,
                    24,
                    RenderTextureFormat.ARGB32,
                    linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default);
                camera.targetTexture = renderTexture;
                using (PieScreenshotTimingProbe.Measure(PieScreenshotTimingProbe.Stage.Render))
                    camera.Render();
                var readSource = renderTexture;
                if (linear)
                {
                    bounce = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    // Scope the global sRGB-write state around the conversion
                    // blit (see ScaleToSize).
                    GL.sRGBWrite = true;
                    using (PieScreenshotTimingProbe.Measure(PieScreenshotTimingProbe.Stage.Blit))
                        Graphics.Blit(renderTexture, bounce);
                    readSource = bounce;
                }
                RenderTexture.active = readSource;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                try
                {
                    using (PieScreenshotTimingProbe.Measure(PieScreenshotTimingProbe.Stage.ReadbackApply))
                    {
                        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                        texture.Apply(false);
                    }
                    if (snapshot != null) png = snapshot(texture);
                    else
                        using (PieScreenshotTimingProbe.Measure(PieScreenshotTimingProbe.Stage.Encode))
                            png = texture.EncodeToPNG();
                }
                finally
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }
            finally
            {
                // Isolate cleanup stages: restoring the camera must not block
                // canvas restoration, and neither may block the RT releases
                // (review finding: setup failures previously leaked the
                // disabled-canvases state and temporary RTs).
                try { camera.targetTexture = previousTarget; } catch (Exception) { /* camera destroyed mid-capture */ }
                RenderTexture.active = previousActive;
                GL.sRGBWrite = previousSrgbWrite;
                if (disabledCanvases != null)
                {
                    foreach (var canvas in disabledCanvases)
                    {
                        if (canvas != null)
                            canvas.enabled = true;
                    }
                }
                if (bounce != null)
                    RenderTexture.ReleaseTemporary(bounce);
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);
            }
            return png ?? Array.Empty<byte>();
        }

        private static bool IsLinearColorSpace()
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear;
        }

        private static string SanitizeRelativePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            var normalized = path.Trim().Replace('\\', '/');
            if (normalized.StartsWith("/", StringComparison.Ordinal))
                return null;
            var parts = normalized.Split('/');
            var depth = 0;
            foreach (var part in parts)
            {
                if (part == "..") depth -= 1;
                else if (part.Length > 0 && part != ".") depth += 1;
                if (depth < 0) return null;
            }
            if (!normalized.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                normalized += ".png";
            return normalized;
        }
    }
}
