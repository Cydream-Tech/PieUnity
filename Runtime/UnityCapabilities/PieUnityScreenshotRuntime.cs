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
    public static class PieUnityScreenshotRuntime
    {
        private const int MinDimension = 64;
        private const int MaxDimension = 3840;

        [Serializable]
        private sealed class ScreenshotPayload
        {
            public int width;
            public int height;
            public string cameraName;
            public string path;
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
            var payload = JsonUtility.FromJson<ScreenshotPayload>(argsJson ?? "{}") ?? new ScreenshotPayload();
            var width = Mathf.Clamp(payload.width > 0 ? payload.width : 1280, MinDimension, MaxDimension);
            var height = Mathf.Clamp(payload.height > 0 ? payload.height : 720, MinDimension, MaxDimension);

            string cameraUsed;
            string captureMode;
            byte[] bytes;
            // An explicit cameraName is user intent: honor it even in play
            // mode via a targeted camera render instead of silently falling
            // back to the game view.
            var explicitCameraRequested = !string.IsNullOrWhiteSpace(payload.cameraName);
            if (Application.isPlaying && !explicitCameraRequested)
            {
                bytes = CaptureGameView(width, height, payload.cameraName, out cameraUsed, out captureMode);
            }
            else
            {
                bytes = CaptureViaCamera(payload.cameraName, width, height, out cameraUsed);
                captureMode = "camera-render";
            }

            var directory = Path.Combine(Application.persistentDataPath, "PieScreenshots");
            Directory.CreateDirectory(directory);
            var relativePath = SanitizeRelativePath(payload.path)
                ?? $"capture_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.png";
            var absolutePath = Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? directory);
            // Never silently overwrite an existing capture: uniquify the
            // file name so repeated explicit paths and same-millisecond
            // defaults are preserved.
            if (File.Exists(absolutePath))
            {
                var stem = Path.Combine(
                    Path.GetDirectoryName(absolutePath) ?? directory,
                    Path.GetFileNameWithoutExtension(absolutePath));
                var ext = Path.GetExtension(absolutePath);
                for (var attempt = 1; ; attempt += 1)
                {
                    var candidate = $"{stem}_{attempt}{ext}";
                    if (!File.Exists(candidate))
                    {
                        absolutePath = candidate;
                        break;
                    }
                }
            }

            File.WriteAllBytes(absolutePath, bytes);

            // The reported relative path must track the uniquified file: derive
            // it from the final absolute path so path/absolutePath never diverge.
            var finalRelative = Path.GetRelativePath(directory, absolutePath).Replace('\\', '/');
            var result = new ScreenshotResult
            {
                summary = $"Captured {width}x{height} screenshot ({captureMode}, {bytes.Length} bytes).",
                path = $"PieScreenshots/{finalRelative}",
                absolutePath = absolutePath.Replace('\\', '/'),
                width = width,
                height = height,
                bytes = bytes.Length,
                camera = cameraUsed,
                mode = captureMode,
            };
            return JsonUtility.ToJson(result);
        }

        /// <summary>
        /// Full-fidelity capture of the last rendered game frame, including
        /// Screen Space Overlay UI, which a camera render into a target
        /// texture would skip. Falls back to a camera render when no game
        /// frame is available.
        /// </summary>
        private static byte[] CaptureGameView(int width, int height, string cameraName, out string cameraUsed, out string captureMode)
        {
            Texture2D captured = null;
            try
            {
                captured = ScreenCapture.CaptureScreenshotAsTexture();
            }
            catch (Exception)
            {
                captured = null;
            }
            if (captured == null)
            {
                captureMode = "camera-render";
                return CaptureViaCamera(cameraName, width, height, out cameraUsed);
            }
            try
            {
                var readable = ScaleToSize(captured, width, height);
                try
                {
                    captureMode = "game-view";
                    cameraUsed = "game-view";
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

        private static byte[] CaptureViaCamera(string cameraName, int width, int height, out string cameraUsed)
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
            return RenderCameraToPng(camera, width, height);
        }

        private static Texture2D ScaleToSize(Texture2D source, int width, int height)
        {
            if (source.width == width && source.height == height)
            {
                var copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
                copy.SetPixels(source.GetPixels());
                copy.Apply(false);
                return copy;
            }
            var scaled = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            try
            {
                Graphics.Blit(source, scaled);
                RenderTexture.active = scaled;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false);
                return texture;
            }
            finally
            {
                RenderTexture.active = previousActive;
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

        private static byte[] RenderCameraToPng(Camera camera, int width, int height)
        {
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            byte[] png;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                try
                {
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    texture.Apply(false);
                    png = texture.EncodeToPNG();
                }
                finally
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
            return png ?? Array.Empty<byte>();
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
