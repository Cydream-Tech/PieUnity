using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Pie.Editor
{
    /// <summary>
    /// Minimal smoke player for validating the Pie runtime in a
    /// REAL player build (the only place IL2CPP stripping, TCC, and platform
    /// HTTP behavior are honest):
    ///
    ///   Unity -batchmode -projectPath &lt;yourProject&gt; -buildTarget StandaloneOSX \
    ///     -executeMethod Pie.Editor.PiePlayerSmokeBuild.PrepareSmokeBindingsBatch -quit
    ///   Unity -batchmode -projectPath &lt;yourProject&gt; -buildTarget StandaloneOSX \
    ///     -executeMethod Pie.Editor.PiePlayerSmokeBuild.BuildSmokePlayer \
    ///     -pieSmokeOutput /abs/path/PieSmoke.app -quit
    ///
    /// The scene is intentionally tiny (camera + PieRunner + PiePlayerSmoke)
    /// so the build is fast and any failure is attributable to the Pie
    /// runtime, not to game content. The build uses the project's CURRENT
    /// scripting backend — switch to IL2CPP first if that is what you ship.
    ///
    /// After launching the app, verify it with:
    ///   node products/pie-unity/scripts/unity-player-smoke.mjs --instance &lt;id&gt;
    /// </summary>
    public static class PiePlayerSmokeBuild
    {
        private const string SmokeDir = "Assets/PiePlayerSmoke";
        private const string ScenePath = SmokeDir + "/PiePlayerSmoke.unity";
        private const string SettingsPath = SmokeDir + "/PieSettings.asset";

        /// <summary>
        /// Explicit generation under the SAME identity used by BuildSmokePlayer.
        /// Do not call GenerateAllBatch here: its Exit would bypass restoration.
        /// Exit 2 requires a fresh Unity process to compile and repeat preparation.
        /// After smoke verification, GenerateAllBatch restores host-fresh bindings.
        /// </summary>
        public static void PrepareSmokeBindingsBatch()
        {
            var previousProductName = PlayerSettings.productName;
            var exitCode = 1;
            try
            {
                PlayerSettings.productName = "PieSmoke";
                AssetDatabase.SaveAssets();
                PiePuerTSBindingGenerator.GenerateAll();
                if (EditorApplication.isCompiling || PiePuerTSBindingGenerator.IsStale())
                {
                    exitCode = 2;
                    Debug.LogWarning("PIE-SMOKE-GEN-NOT-READY: reopen Unity with the same -buildTarget, compile, and repeat PrepareSmokeBindingsBatch; do not build yet.");
                }
                else
                {
                    exitCode = 0;
                    Debug.Log("PIE-SMOKE-GEN-OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("PIE-SMOKE-GEN-FAIL " + ex);
            }
            finally
            {
                PlayerSettings.productName = previousProductName;
                AssetDatabase.SaveAssets();
            }
            EditorApplication.Exit(exitCode);
        }

        public static void BuildSmokePlayer()
        {
            string output = null;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-pieSmokeOutput") output = args[i + 1];
            if (string.IsNullOrEmpty(output))
                output = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "build", "PieSmoke.app"));

            if (Directory.Exists(output) || File.Exists(output))
            {
                Debug.LogError("[PiePlayerSmoke] build output already exists: " + output);
                EditorApplication.Exit(2);
                return;
            }

            // Give the smoke player its own product identity: sharing the
            // host project's productName would collide RPC instance ids and
            // persistentDataPath with the real game player. Restored in
            // finally: a throwing build must not rename the host project.
            var previousProductName = PlayerSettings.productName;
            var buildFailed = false;
            try
            {
                PlayerSettings.productName = "PieSmoke";
                AssetDatabase.SaveAssets();
                var target = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
                if (PlayerSettings.GetScriptingBackend(target) == ScriptingImplementation.IL2CPP
                    && PiePuerTSBindingGenerator.IsStrict() && PiePuerTSBindingGenerator.IsStale())
                    throw new InvalidOperationException("Smoke bindings are stale or missing. Run Pie.Editor.PiePlayerSmokeBuild.PrepareSmokeBindingsBatch with the same -buildTarget until exit 0, then build in a separate Unity process. Host-identity bindings are not smoke-identity bindings.");
                var settings = EnsureSettingsAsset();
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
                camGo.AddComponent<AudioListener>();

                var runnerGo = new GameObject("PieRunner");
                var runner = runnerGo.AddComponent<PieRunner>();
                var so = new SerializedObject(runner);
                so.FindProperty("_settingsOverride").objectReferenceValue = settings;
                so.ApplyModifiedPropertiesWithoutUndo();
                runnerGo.AddComponent<PiePlayerSmoke>();

                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new System.Exception("SaveScene returned false for " + ScenePath);
                AssetDatabase.SaveAssets();

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = output,
                    target = EditorUserBuildSettings.activeBuildTarget,
                    options = BuildOptions.Development,
                });

                if (report.summary.result.ToString() == "Succeeded" && report.summary.totalErrors == 0)
                {
                    Debug.Log("[PiePlayerSmoke] BUILD OK: " + output);
                }
                else
                {
                    Debug.LogError("[PiePlayerSmoke] BUILD FAILED: " + report.summary.result + " errors=" + report.summary.totalErrors);
                    buildFailed = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[PiePlayerSmoke] BUILD FAILED: " + ex);
                buildFailed = true;
            }
            finally
            {
                PlayerSettings.productName = previousProductName;
                AssetDatabase.SaveAssets();
            }
            // Exit terminates the Editor immediately; doing this inside the try
            // bypasses finally and leaves the host project renamed after failure.
            if (buildFailed) EditorApplication.Exit(1);
        }

        private static PieSettings EnsureSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PieSettings>(SettingsPath);
            if (settings == null)
            {
                Directory.CreateDirectory(SmokeDir);
                settings = ScriptableObject.CreateInstance<PieSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            return settings;
        }
    }
}
