using System;
using System.IO;
using UnityEngine;

namespace Pie
{
    /// <summary>
    /// Minimal player-smoke bootstrap. Put it next to a PieRunner in a tiny
    /// scene — Pie.Editor.PiePlayerSmokeBuild.BuildSmokePlayer does exactly
    /// that — and you get a verifiable "the Pie runtime works in THIS player
    /// build" signal over RPC:
    ///
    ///   node products/pie-unity/scripts/unity-player-smoke.mjs --instance <id>
    ///
    /// Why this exists: editor tests cannot catch IL2CPP-only failures
    /// (dogfood P1: System.IO reflection stripped → sessions/file tools dead;
    /// P2: base64 bridge corruption). The self-test proves:
    ///  - the tool/manifest pipeline is alive (this tool is only reachable
    ///    through it),
    ///  - the native file bridge reads/writes — the same System.IO surface
    ///    the JS file tools drive through PuerTS CS.* bindings,
    ///  - the synchronous base64 fallback bridge round-trips,
    ///  - session/config paths resolve in this environment.
    /// The external verifier additionally proves the JS-side filesystem
    /// bridge end to end (see ProbeSkillPrefix): a nonce probe skill is
    /// installed, skills_reload makes the JS runtime rescan the skill
    /// layers through puerts-fs, and the nonce must appear in the
    /// JS-emitted skills list this component reports.
    /// </summary>
    public sealed class PiePlayerSmoke : MonoBehaviour
    {
        public const string HeartbeatCanary = "pie-smoke-alive";

        /// <summary>Probe skill name prefix. The smoke verifier installs a
        /// nonce-suffixed probe skill (C#-side file write), triggers
        /// skills_reload, and polls the self-test for exact probeName membership
        /// in the full inventory produced by the JS runtime scanning through
        /// puerts-fs (CS.System.IO), independently of diagnostic truncation. A C#-written file
        /// appearing in the JS-produced list is honest end-to-end proof the
        /// JS filesystem read bridge works in THIS player (the IL2CPP P1
        /// surface); a C#-only round-trip would prove nothing about JS.</summary>
        public const string ProbeSkillPrefix = "pie-smoke-probe-";

        public static string SmokeDirectory
        {
            get { return Path.Combine(Application.persistentDataPath, ".pie-smoke"); }
        }

        public static string HeartbeatPath
        {
            get { return Path.Combine(SmokeDirectory, "heartbeat.txt"); }
        }

        private void Start()
        {
            WriteHeartbeat();
            PieUnityCapabilityRegistry.RegisterTool(
                "pie_smoke_selftest", "pie",
                "Run the Pie player smoke self-test: bridge/manifest pipeline, native file bridge round-trip, " +
                "synchronous base64 fallback, and session path resolution. Read-only; safe to call anytime.",
                "runtime", true, false, null, new[] {
                    new PieUnityParameterDescriptor { name = "probeName", type = "string", required = false }
                },
                args => RunSelfTest(args));
        }

        private void WriteHeartbeat()
        {
            try
            {
                Directory.CreateDirectory(SmokeDirectory);
                File.WriteAllText(HeartbeatPath,
                    HeartbeatCanary + "\n"
                    + "utc=" + DateTime.UtcNow.ToString("o") + "\n"
                    + "backend=" + Backend + "\n"
                    + "unity=" + Application.unityVersion + "\n"
                    + "product=" + Application.productName + "\n");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PiePlayerSmoke] heartbeat write failed: " + ex.Message);
            }
        }

        [Serializable] private sealed class ProbeArgs { public string probeName; }
        [Serializable] private sealed class SkillNames { public SkillName[] skills; }
        [Serializable] private sealed class SkillName { public string name; }

        // Full inventory is parsed, never the diagnostic prefix. Output remains bounded.
        public static bool ContainsProbeSkill(string skillsJson, string probeName)
        {
            if (string.IsNullOrEmpty(probeName) || probeName.Length > 128
                || !probeName.StartsWith(ProbeSkillPrefix, StringComparison.Ordinal)
                || string.IsNullOrEmpty(skillsJson)) return false;
            var inventory = JsonUtility.FromJson<SkillNames>(skillsJson);
            if (inventory?.skills == null) return false;
            foreach (var skill in inventory.skills)
                if (skill != null && string.Equals(skill.name, probeName, StringComparison.Ordinal)) return true;
            return false;
        }

        private static string RunSelfTest(string argsJson)
        {
            var result = new PiePlayerSmokeResult
            {
                backend = Backend,
                unityVersion = Application.unityVersion,
                persistentDataPath = Application.persistentDataPath,
                heartbeatPath = HeartbeatPath,
            };
            try
            {
                result.runnerActive = PieRunner.ActiveRunner != null;
                result.sessionsDirectory = PieProjectPaths.GetSessionsDirectory();

                // Observable end of the JS filesystem proof: the JS runtime
                // emitted this list after scanning the skill layers through
                // puerts-fs (CS.System.IO via PuerTS). The verifier installs a
                // nonce probe skill (C# write) and watches for the nonce here;
                // if IL2CPP stripped the JS reflection surface, the rescan
                // fails JS-side and the nonce never appears. Only the diagnostic
                // copy is capped; the structured proof scans the full inventory.
                var skillsJson = PieRunner.ActiveRunner != null ? PieRunner.ActiveRunner.LastSkillsListJson : null;
                var args = JsonUtility.FromJson<ProbeArgs>(string.IsNullOrEmpty(argsJson) ? "{}" : argsJson);
                result.probeName = args?.probeName ?? "";
                if (result.probeName.Length > 128) result.probeName = "";
                result.probeFound = ContainsProbeSkill(skillsJson, result.probeName);
                result.skillsListJson = string.IsNullOrEmpty(skillsJson)
                    ? ""
                    : (skillsJson.Length <= 4000 ? skillsJson : skillsJson.Substring(0, 4000));

                // Native file bridge round-trip (System.IO — the surface the
                // JS file tools reach through CS.* bindings; IL2CPP P1 class).
                Directory.CreateDirectory(SmokeDirectory);
                var probePath = Path.Combine(SmokeDirectory, "selftest.bin");
                var payload = new byte[] { 0x50, 0x69, 0x65, 0x21 };
                File.WriteAllBytes(probePath, payload);
                var readBack = File.ReadAllBytes(probePath);
                result.fileBridgeRoundtrip = readBack.Length == payload.Length
                    && readBack[0] == payload[0] && readBack[3] == payload[3];

                // Synchronous base64 fallback bridge (added after the dogfood
                // P2: the Task-based bridge could settle with invalid values).
                var base64 = PieFileBridge.ReadAllBytesBase64(probePath);
                result.base64Roundtrip = string.Equals(base64, Convert.ToBase64String(payload), StringComparison.Ordinal);

                result.ok = result.runnerActive
                    && result.fileBridgeRoundtrip
                    && result.base64Roundtrip
                    && !string.IsNullOrEmpty(result.sessionsDirectory);
                result.summary = result.ok
                    ? "Pie player smoke self-test passed."
                    : "Pie player smoke self-test FAILED; inspect the flags.";
            }
            catch (Exception ex)
            {
                result.ok = false;
                result.error = ex.GetType().Name + ": " + ex.Message;
                result.summary = "Pie player smoke self-test threw.";
            }
            return JsonUtility.ToJson(result, true);
        }

        private static string Backend
        {
            get
            {
#if ENABLE_IL2CPP
                return "IL2CPP";
#else
                return "Mono";
#endif
            }
        }

        [Serializable]
        private sealed class PiePlayerSmokeResult
        {
            public bool ok;
            public string summary;
            public string backend;
            public string unityVersion;
            public string persistentDataPath;
            public string sessionsDirectory;
            public string heartbeatPath;
            public bool runnerActive;
            public bool fileBridgeRoundtrip;
            public bool base64Roundtrip;
            public string skillsListJson;
            public string probeName;
            public bool probeFound;
            public string error;
        }
    }
}
