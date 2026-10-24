using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Pie.Editor
{
    /// <summary>
    /// Keeps PuerTS xIl2cpp generated bindings fresh for the host project:
    /// after every domain reload (which includes first install and package
    /// updates), check the freshness stamp once and regenerate when stale.
    ///
    /// Semantics (agreed productization rules):
    ///  - default ON; hosts opt out via PieSettings.autoGeneratePuerTSBindings.
    ///  - failures are warnings, never editor-blocking errors;
    ///  - runs at most once per domain reload, never during play mode,
    ///    never in import worker processes;
    ///  - also heals the optimization define rule for the active target when
    ///    generation itself is already fresh (one-time legacy fixups).
    /// </summary>
    [InitializeOnLoad]
    internal static class PiePuerTSBindingAutoGenerate
    {
        private static bool _ran;

        static PiePuerTSBindingAutoGenerate()
        {
            if (AssetDatabase.IsAssetImportWorkerProcess())
                return;
            EditorApplication.delayCall += RunOnce;
        }

        private static void RunOnce()
        {
            if (_ran)
                return;
            _ran = true;

            if (EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isCompiling
                || EditorApplication.isUpdating)
            {
                // Defer to the next domain reload; delayCall chains would risk
                // generating mid-import.
                _ran = false;
                EditorApplication.delayCall += RunOnce;
                return;
            }

            // Strict CI must not mutate outputs or defines before the build gate.
            // Explicit GenerateAllBatch remains available in strict mode.
            if (PiePuerTSBindingGenerator.IsStrict() || !PiePuerTSBindingGenerator.IsAutoGenerateEnabled())
                return;

            try
            {
                PiePuerTSBindingGenerator.EnsureSupportedPolicy();
                if (PiePuerTSBindingGenerator.IsStale())
                {
                    Debug.Log("[Pie] PuerTS bindings are stale or missing; auto-generating (disable via PieSettings.autoGeneratePuerTSBindings).");
                    PiePuerTSBindingGenerator.GenerateAll();
                }
                else
                {
                    // One-time define heal for hosts that carry a stale
                    // PUERTS_DISABLE_IL2CPP_OPTIMIZATION from before generated
                    // bindings existed (dogfood saga-era finding).
                    PiePuerTSBindingGenerator.ApplyDefineRule(
                        NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Pie] PuerTS binding auto-generation failed (non-fatal; generate manually via Tools/Pie/PuerTS Bindings): " + ex.Message);
            }
        }
    }
}
