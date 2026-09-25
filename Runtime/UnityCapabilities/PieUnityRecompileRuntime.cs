#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Pie;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Pie.Unity.Capabilities
{
    /// <summary>
    /// Background recompile support for hosts driving the editor over RPC.
    /// Unity 6 defers change-scan-based refresh while the editor window is
    /// unfocused, so this capability bypasses the scan entirely: it
    /// force-imports the given paths and requests script compilation without
    /// ever activating the app. Callers poll unity_compile_status for
    /// completion and read unity_log_read for compile errors.
    /// </summary>
    public static class PieUnityRecompileRuntime
    {
        [Serializable]
        private sealed class RecompilePayload
        {
            public string[] paths;
        }

        [Serializable]
        private sealed class RecompileResult
        {
            public bool requested;
            public bool coalesced;
            public int imported;
            public int skipped;
            public string importedPaths = "";
            public string skippedPaths = "";
            public string summary = "";
        }

        /// Asset types that can affect script compilation. Anything else is
        /// skipped loudly instead of force-imported as a side effect.
        private static readonly HashSet<string> CompileAssetExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".asmdef", ".asmref",
        };

        // Compilation storm guard: agent loops may call this repeatedly in
        // quick succession; one in-flight compilation plus a short cooldown
        // coalesce bursts into a single compile request.
        private const double CooldownSeconds = 2.0;
        private const double StaleInFlightSeconds = 120.0;
        private static bool compilationInFlight;
        private static double lastRequestSeconds = double.NegativeInfinity;

        static PieUnityRecompileRuntime()
        {
            CompilationPipeline.compilationStarted += _ => compilationInFlight = true;
            CompilationPipeline.compilationFinished += _ => compilationInFlight = false;
        }

        public static string RecompileJson(string argsJson)
        {
            var payload = JsonUtility.FromJson<RecompilePayload>(argsJson ?? "{}") ?? new RecompilePayload();

            var imported = new List<string>();
            var skipped = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in payload.paths ?? new string[0])
            {
                var assetPath = ToAssetPath(raw);
                if (assetPath == null)
                {
                    skipped.Add($"{raw ?? ""} (unresolvable path)");
                    continue;
                }
                if (!seen.Add(assetPath)) continue; // dedupe repeated paths
                var extension = Path.GetExtension(assetPath);
                if (!CompileAssetExtensions.Contains(extension))
                {
                    skipped.Add($"{assetPath} (unsupported for script compilation: {extension})");
                    continue;
                }
                try
                {
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                    imported.Add(assetPath);
                }
                catch (Exception ex)
                {
                    PieDiagnostics.Warning($"[unity_recompile] import failed for {assetPath}: {ex.Message}");
                    skipped.Add($"{raw ?? ""} (import failed)");
                }
            }

            // Parameterless overload recompiles dirty assemblies; ForceUpdate
            // imports above already marked the given paths dirty. An empty
            // import set never triggers a global recompile.
            var now = EditorApplication.timeSinceStartup;
            if (compilationInFlight && now - lastRequestSeconds > StaleInFlightSeconds)
            {
                compilationInFlight = false; // stale guard: no finish event ever arrived
            }
            var coalesced = compilationInFlight || (now - lastRequestSeconds) < CooldownSeconds;
            var requested = imported.Count > 0 && !coalesced;
            if (requested)
            {
                compilationInFlight = true;
                lastRequestSeconds = now;
                CompilationPipeline.RequestScriptCompilation();
            }

            string summary;
            if (imported.Count == 0)
            {
                summary = $"No script assets to import; no compilation requested (skipped {skipped.Count}).";
            }
            else if (coalesced)
            {
                summary = $"Imported {imported.Count} script asset(s); a compilation request is already in flight or within cooldown, so this call was coalesced into it.";
            }
            else
            {
                summary = $"Compilation REQUESTED (imported {imported.Count}, skipped {skipped.Count}); requested=true only means the compile was queued, not that it succeeded. Poll unity_compile_status; read unity_log_read with contains \"error CS\" for failures.";
            }
            var result = new RecompileResult
            {
                requested = requested,
                coalesced = coalesced && imported.Count > 0,
                imported = imported.Count,
                skipped = skipped.Count,
                importedPaths = string.Join("\n", imported.ToArray()),
                skippedPaths = string.Join("\n", skipped.ToArray()),
                summary = summary,
            };
            return JsonUtility.ToJson(result);
        }

        /// <summary>
        /// Accepts "Packages/…" / "Assets/…" asset paths, absolute paths inside
        /// the pie package source, or absolute paths inside this project, and
        /// returns the asset database path (or null when outside both roots).
        /// </summary>
        private static string ToAssetPath(string raw)
        {
            var path = (raw ?? "").Trim().Replace("\\", "/");
            if (path.Length == 0) return null;
            if (path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return path;

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(PieBridge).Assembly);
            var packageRoot = packageInfo != null ? (packageInfo.resolvedPath ?? "").Replace("\\", "/") : "";
            if (packageRoot.Length > 0 && path.StartsWith(packageRoot + "/", StringComparison.Ordinal))
                return $"Packages/{packageInfo.name}/{path.Substring(packageRoot.Length + 1)}";

            var projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace("\\", "/");
            if (path.StartsWith(projectRoot + "/", StringComparison.Ordinal))
                return path.Substring(projectRoot.Length + 1);

            return null;
        }
    }

    [UnityEditor.InitializeOnLoad]
    internal static class PieUnityRecompileRegistration
    {
        static PieUnityRecompileRegistration()
        {
            PieUnityCapabilityRegistry.RegisterTool(
                "unity_recompile",
                "unity.editor",
                "Force-import the given C#/assembly-definition asset paths (no change-scan) and request script compilation without focusing the editor. Only .cs/.asmdef/.asmref assets are imported; duplicates are collapsed, an empty import set never triggers compilation, and rapid repeat calls coalesce into one compile. `requested=true` means the compile was queued, not that it succeeded: poll unity_compile_status afterwards and read unity_log_read (contains \"error CS\") for failures. Paths may be Packages/… or Assets/… asset paths, or absolute paths within the pie package source or the project.",
                "editor",
                false,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "paths", type = "object", required = false },
                },
                PieUnityRecompileRuntime.RecompileJson,
                capabilityKind: "host",
                writeScope: "editor");
        }
    }
}
#endif
