using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Pie.Editor
{
    /// <summary>
    /// IL2CPP build gate for PuerTS xIl2cpp bindings.
    ///
    /// A stale/missing binding set must not silently ship: the generated code
    /// is what makes C#-&gt;JS calls fast AND is what the generated link.xml
    /// preserves from stripping. With auto-generation enabled (the default)
    /// this gate should never trigger; it exists for opted-out hosts and CI.
    ///
    /// Behavior when stale at build time:
    ///  - PIE_PUERTS_GEN_STRICT=1 (recommended for CI): fail immediately.
    ///  - otherwise: self-heal — regenerate + fix the optimization define,
    ///    then fail THIS build with a clear "re-run the build" message,
    ///    because the new/changed scripts require a recompile that aborts the
    ///    in-flight build anyway. The next build proceeds normally.
    ///
    /// The gate only applies to IL2CPP builds; Mono editor/host workflows do
    /// not use the generated code path.
    /// </summary>
    public sealed class PiePuerTSBindingBuildGate : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var target = NamedBuildTarget.FromBuildTargetGroup(report.summary.platformGroup);
            if (PlayerSettings.GetScriptingBackend(target) != ScriptingImplementation.IL2CPP)
                return;

            if (report.summary.platformGroup != BuildTargetGroup.Standalone
                || report.summary.platform != EditorUserBuildSettings.activeBuildTarget)
                throw new BuildFailedException("[Pie] xIl2cpp builds support only the active Standalone target. Switch to the requested Standalone target, compile, explicitly generate bindings, then retry. Other platforms (including iOS) are unsupported.");
            try { PiePuerTSBindingGenerator.EnsureSupportedPolicy(); }
            catch (System.Exception ex) { throw new BuildFailedException(ex.Message); }

            // Stale check first: a missing binding set must never flip the
            // define on (generated absent => add-define) moments before the
            // self-heal regeneration flips it back off.
            var stale = PiePuerTSBindingGenerator.IsStale();
            if (stale)
            {
                if (PiePuerTSBindingGenerator.IsStrict())
                {
                    throw new BuildFailedException(
                        "[Pie] PuerTS bindings are stale or missing (PIE_PUERTS_GEN_STRICT=1). "
                        + "Regenerate with -executeMethod Pie.Editor.PiePuerTSBindingGenerator.GenerateAllBatch "
                        + "or the Tools/Pie/PuerTS Bindings menu, commit the result, then build.");
                }
                try
                {
                    PiePuerTSBindingGenerator.GenerateAll();
                    PiePuerTSBindingGenerator.ApplyDefineRule(target);
                }
                catch (System.Exception ex)
                {
                    throw new BuildFailedException(
                        "[Pie] PuerTS bindings are stale and in-build regeneration failed: " + ex.Message
                        + "\nRun Tools/Pie/PuerTS Bindings/Generate (xIl2cpp, full wrapper) and retry the build.");
                }
                throw new BuildFailedException(
                    "[Pie] PuerTS bindings were stale and have been regenerated. "
                    + "Scripts changed, so this build cannot continue. Re-run the build — it will use the fresh bindings.");
            }

            if (PiePuerTSBindingGenerator.IsStrict()
                && (";" + PlayerSettings.GetScriptingDefineSymbols(target) + ";").Contains(";" + PiePuerTSBindingGenerator.DisableOptimizationDefine + ";"))
                throw new BuildFailedException("[Pie] Strict mode forbids implicit define repair. Run GenerateAllBatch explicitly, allow recompilation, and retry until exit 0 before building.");

            if (PiePuerTSBindingGenerator.ApplyDefineRule(target))
            {
                // The define edit forces a recompile that aborts this build;
                // surface a clear instruction instead of raw CS errors.
                throw new BuildFailedException(
                    "[Pie] Fixed the PUERTS_DISABLE_IL2CPP_OPTIMIZATION define for the current target "
                    + "(generated bindings and the define were inconsistent). Re-run the build.");
            }
        }

    }
}
