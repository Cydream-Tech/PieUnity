using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEngine;

namespace Pie.Editor
{
    /// <summary>
    /// Generates PuerTS xIl2cpp static bindings for the merged [Configure]
    /// surface (pie-unity preset + any host [Configure] classes), tracks
    /// freshness with a stamp file next to the generated code, and owns the
    /// PUERTS_DISABLE_IL2CPP_OPTIMIZATION define rule:
    ///
    ///   generated bindings present  => REMOVE the disable define (optimize)
    ///   generated bindings absent   => ADD the disable define (explicit
    ///                                  reflection fallback, the historically
    ///                                  predictable mode)
    ///
    /// The rule exists because RegisterInfo_Gen.cs references legacy Wrap
    /// classes when optimization is disabled; a stale disable define plus
    /// generated code breaks project compilation (dogfood spike finding).
    ///
    /// Entry points:
    ///   menu   Tools/Pie/PuerTS Bindings/Generate (xIl2cpp, full wrapper)
    ///   menu   Tools/Pie/PuerTS Bindings/Show Status
    ///   batch  -executeMethod Pie.Editor.PiePuerTSBindingGenerator.GenerateAllBatch
    ///   code   PiePuerTSBindingGenerator.GenerateAll()
    /// </summary>
    public static class PiePuerTSBindingGenerator
    {
        public const string DisableOptimizationDefine = "PUERTS_DISABLE_IL2CPP_OPTIMIZATION";
        public const string StampFileName = "pie-puerts-stamp.json";
        public const string RegisterInfoFileName = "RegisterInfo_Gen.cs";

        private const string PipelineSchema = "pie-xil2cpp-fullwrapper-v2";
        private static bool _requiresCompilation;

        [Serializable]
        private class OutputHash
        {
            public string path;
            public string hash;
        }

        [Serializable]
        private class Stamp
        {
            public string fingerprint;
            public string unityVersion;
            public string puertsVersion;
            public string mode;
            public string generatedUtc;
            public string schema;
            public OutputHash[] outputs;
        }

        // ── generation ──────────────────────────────────────────────────

        /// <summary>
        /// Run the full-wrapper xIl2cpp generation (wrappers + extension infos
        /// + link.xml + c plugin + register info), write the freshness stamp,
        /// and apply the optimization define rule. Throws on failure.
        /// </summary>
        public static void GenerateAll()
        {
            EnsureSupportedPolicy();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var fingerprint = ComputeFingerprint();
            PuertsIl2cpp.Editor.Generator.UnityMenu.GenV2();
            WriteStamp(fingerprint, "fullwrapper");
            _requiresCompilation |= ApplyDefineRule(NamedBuildTarget.Standalone);
            AssetDatabase.Refresh();
            Debug.Log($"[Pie] PuerTS bindings generated ({sw.ElapsedMilliseconds} ms, fingerprint {fingerprint.Substring(0, 8)}).");
        }

        /// <summary>Batch entry: generation failures exit non-zero.</summary>
        public static void GenerateAllBatch()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                GenerateAll();
                if (_requiresCompilation || EditorApplication.isCompiling || IsStale())
                {
                    Debug.LogWarning("PIE-PUERTS-GEN-NOT-READY: scripts/defines changed. Reopen Unity, allow compilation, and retry GenerateAllBatch with the same -buildTarget; do not build yet.");
                    EditorApplication.Exit(2);
                    return;
                }
                Debug.Log($"PIE-PUERTS-GEN-OK ms={sw.ElapsedMilliseconds}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"PIE-PUERTS-GEN-FAIL ms={sw.ElapsedMilliseconds} {ex}");
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Tools/Pie/PuerTS Bindings/Generate (xIl2cpp, full wrapper)")]
        private static void GenerateAllMenu()
        {
            try
            {
                GenerateAll();
            }
            catch (Exception ex)
            {
                Debug.LogError("[Pie] PuerTS binding generation failed: " + ex);
            }
        }

        [MenuItem("Tools/Pie/PuerTS Bindings/Show Status")]
        private static void ShowStatusMenu()
        {
            var outputDir = GetOutputDirectory();
            Debug.Log("[Pie] PuerTS bindings status:\n"
                + $"  output dir: {outputDir}\n"
                + $"  register info present: {HasGeneratedBindings()}\n"
                + $"  stamp present: {File.Exists(GetStampPath())}\n"
                + $"  stale: {IsStale()}\n"
                + $"  auto-generate: {IsAutoGenerateEnabled()}\n"
                + $"  fingerprint: {ComputeFingerprint().Substring(0, 8)}");
        }

        // ── freshness ───────────────────────────────────────────────────

        /// <summary>
        /// True when the generated bindings are missing, the stamp is missing,
        /// or the merged [Configure] surface / PuerTS version / Unity version
        /// changed since the last generation.
        /// </summary>
        public static bool IsStale()
        {
            if (_requiresCompilation || !HasGeneratedBindings())
                return true;
            var stampPath = GetStampPath();
            if (!File.Exists(stampPath))
                return true;
            try
            {
                var stamp = JsonUtility.FromJson<Stamp>(File.ReadAllText(stampPath));
                return stamp == null || stamp.schema != PipelineSchema
                    || !ValidateOutputs(stamp.outputs)
                    || !string.Equals(stamp.fingerprint, ComputeFingerprint(), StringComparison.Ordinal);
            }
            catch
            {
                return true;
            }
        }

        public static bool HasGeneratedBindings()
        {
            var outputDirectory = GetOutputDirectory();
            foreach (var path in RequiredOutputs)
            {
                var file = Path.Combine(outputDirectory, path);
                if (!File.Exists(file) || new FileInfo(file).Length == 0)
                    return false;
            }
            return true;
        }

        public static string GetOutputDirectory()
        {
            return Puerts.Configure.GetCodeOutputDirectory();
        }

        public static string GetStampPath()
        {
            return Path.Combine(GetOutputDirectory(), StampFileName);
        }

        /// <summary>
        /// Conservative fingerprint of compiled target/defines, assembly-qualified
        /// bindings, compilation source files, external DLL references, configuration
        /// and generator resources. Generated outputs, metas and native output are
        /// excluded to avoid feedback loops. A define transition requires a new
        /// compiled editor session before output can be considered ready.
        /// </summary>
        public static string ComputeFingerprint()
        {
            var tags = Puerts.Configure.GetConfigureByTags(new List<string> { "Puerts.BindingAttribute" });
            var names = new List<string>();
            List<KeyValuePair<object, int>> bindings;
            if (tags.TryGetValue("Puerts.BindingAttribute", out bindings) && bindings != null)
            {
                foreach (var entry in bindings)
                {
                    var type = entry.Key as Type;
                    if (type != null)
                        names.Add(type.AssemblyQualifiedName ?? type.FullName ?? type.Name);
                }
            }
            names.Sort(StringComparer.Ordinal);

            var puertsVersion = "unknown";
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(Puerts.JsEnv).Assembly);
            if (packageInfo != null && !string.IsNullOrEmpty(packageInfo.version))
                puertsVersion = packageInfo.version;

            var activeTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
            var disableDefinePresent = ContainsDefine(
                PlayerSettings.GetScriptingDefineSymbols(activeTarget) ?? "", DisableOptimizationDefine);
            var inputs = CollectInputHashes();
            var raw = string.Join("\n", names)
                + "\nschema:" + PipelineSchema
                + "\ntarget:" + EditorUserBuildSettings.activeBuildTarget
                + "\nselectedGroup:" + EditorUserBuildSettings.selectedBuildTargetGroup
                + "\ndefines:" + PlayerSettings.GetScriptingDefineSymbols(activeTarget)
                + "\ninputs:" + string.Join("\n", inputs)
                + "\npuerts:" + puertsVersion
                + "\nunity:" + Application.unityVersion
                + "\nmode:fullwrapper"
                + "\ndisableOptimizationDefine:" + (disableDefinePresent ? "true" : "false");
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static void WriteStamp(string fingerprint, string mode)
        {
            var stamp = new Stamp
            {
                fingerprint = fingerprint,
                unityVersion = Application.unityVersion,
                puertsVersion = "",
                mode = mode,
                generatedUtc = DateTime.UtcNow.ToString("o"),
                schema = PipelineSchema,
                outputs = CaptureOutputs(),
            };
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(Puerts.JsEnv).Assembly);
            if (packageInfo != null)
                stamp.puertsVersion = packageInfo.version ?? "";

            Directory.CreateDirectory(GetOutputDirectory());
            File.WriteAllText(GetStampPath(), JsonUtility.ToJson(stamp, true));
        }

        // Known outputs of the pinned GenV2 full-wrapper pipeline. Also hash every
        // non-meta output, including additional wrapper shards and native files.
        private static readonly string[] RequiredOutputs =
        {
            RegisterInfoFileName, "ExtensionMethodInfos_Gen.cs", "link.xml",
            "Plugins/puerts_il2cpp/PuertsIl2cppWrapper.cpp",
            "Plugins/puerts_il2cpp/PuertsIl2cppWrapperDef0.cpp",
            "Plugins/puerts_il2cpp/PuertsIl2cppFieldWrapper.cpp",
            "Plugins/puerts_il2cpp/PuertsIl2cppBridge.cpp",
            "Plugins/puerts_il2cpp/PuertsValueType.h",
            "Plugins/puerts_il2cpp/unityenv_for_puerts.h",
            "Plugins/puerts_il2cpp/Puerts_il2cpp.cpp",
            "Plugins/puerts_il2cpp/TDataTrans.h",
            "Plugins/puerts_il2cpp/pesapi_adpt.c",
            "Plugins/puerts_il2cpp/pesapi.h",
            "Plugins/puerts_il2cpp/pesapi_webgl.h",
            "Plugins/puerts_il2cpp/pesapi_webgl.cpp"
        };

        private static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private static bool IsUnder(string path, string directory)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar).StartsWith(Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static OutputHash[] CaptureOutputs()
        {
            if (!HasGeneratedBindings())
                throw new InvalidOperationException("GenV2 did not produce the required managed/link/native outputs. Check the PuerTS version and output configuration.");
            var root = Path.GetFullPath(GetOutputDirectory()).TrimEnd(Path.DirectorySeparatorChar);
            var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.Ordinal);
            var outputs = new List<OutputHash>();
            var stampPath = Path.Combine(root, StampFileName);
            foreach (var file in files)
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) || file == stampPath)
                    continue;
                outputs.Add(new OutputHash { path = file.Substring(root.Length + 1).Replace('\\', '/'), hash = HashFile(file) });
            }
            return outputs.ToArray();
        }

        private static bool ValidateOutputs(OutputHash[] recorded)
        {
            if (recorded == null) return false;
            var actual = CaptureOutputs();
            if (recorded.Length != actual.Length) return false;
            for (var i = 0; i < actual.Length; i++)
                if (recorded[i] == null || recorded[i].path != actual[i].path || recorded[i].hash != actual[i].hash)
                    return false;
            return true;
        }

        private static string ResolveInputPath(string path)
        {
            var normalized = path.Replace('\\', '/');
            if (normalized.StartsWith("Packages/", StringComparison.Ordinal))
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(normalized);
                if (package != null)
                    return Path.GetFullPath(Path.Combine(package.resolvedPath, normalized.Substring(("Packages/" + package.name + "/").Length)));
            }
            return Path.GetFullPath(path);
        }

        private static List<string> CollectInputHashes()
        {
            var inputs = new List<string>();
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // PuerTS resolves this by scanning all loaded types; resolve once,
            // not once per source file (thousands of scans can take minutes).
            var outputDirectory = GetOutputDirectory();
            var assemblies = new List<UnityEditor.Compilation.Assembly>(CompilationPipeline.GetAssemblies(AssembliesType.Editor));
            assemblies.AddRange(CompilationPipeline.GetAssemblies(AssembliesType.Player));
            var scriptOutputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var assembly in assemblies)
                scriptOutputs.Add(Path.GetFileName(assembly.outputPath));
            foreach (var assembly in assemblies)
            {
                var defines = (string[])assembly.defines.Clone();
                Array.Sort(defines, StringComparer.Ordinal);
                inputs.Add("assembly:" + assembly.name + ":" + string.Join(";", defines));
                foreach (var file in assembly.sourceFiles) files.Add(ResolveInputPath(file));
                foreach (var reference in assembly.compiledAssemblyReferences)
                    if (!scriptOutputs.Contains(Path.GetFileName(reference))) files.Add(Path.GetFullPath(reference));
                var asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assembly.name);
                if (!string.IsNullOrEmpty(asmdef)) files.Add(ResolveInputPath(asmdef));
            }
            // Bound inputs only: never recursively scan all Assets/package trees
            // (local packages can contain node_modules and rebaked scene assets).
            // Arbitrary external data read by custom Configure code requires an
            // explicit regeneration; it cannot be inferred from the compiler graph.
            foreach (var path in new[] { "Packages/manifest.json", "Packages/packages-lock.json", "ProjectSettings/ProjectSettings.asset" })
                if (File.Exists(path)) files.Add(Path.GetFullPath(path));
            foreach (var package in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                var manifest = Path.Combine(package.resolvedPath, "package.json");
                if (File.Exists(manifest)) files.Add(Path.GetFullPath(manifest));
                if (package.name != "com.tencent.puerts.core") continue;
                var resources = Path.Combine(package.resolvedPath, "Editor", "Resources");
                if (Directory.Exists(resources))
                    foreach (var template in Directory.GetFiles(resources, "*", SearchOption.AllDirectories))
                        if (!template.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                            files.Add(Path.GetFullPath(template));
            }
            foreach (var file in files)
            {
                if (IsUnder(file, outputDirectory) || file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)
                    || file.Replace('\\', '/').IndexOf("/Plugins/puerts_il2cpp/", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                // Content identity avoids machine-specific absolute paths in committed stamps.
                inputs.Add("file:" + Path.GetFileName(file) + ":" + HashFile(file));
            }
            inputs.Sort(StringComparer.Ordinal);
            return inputs;
        }

        public static bool IsStrict()
        {
            var value = Environment.GetEnvironmentVariable("PIE_PUERTS_GEN_STRICT");
            return string.Equals(value, "1", StringComparison.Ordinal)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        public static void EnsureSupportedPolicy()
        {
            if (BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget) != BuildTargetGroup.Standalone
                || EditorUserBuildSettings.selectedBuildTargetGroup != BuildTargetGroup.Standalone)
                throw new InvalidOperationException("[Pie] xIl2cpp generation supports Standalone only. Switch to a Standalone -buildTarget and let Unity compile before generating. iOS/WebGL/consoles are unsupported; no reflection fallback is guaranteed.");
            foreach (var assembly in CompilationPipeline.GetAssemblies(AssembliesType.Editor))
                foreach (var define in assembly.defines)
                    if (define == "CPP_OUTPUT_TO_NATIVE_SRC" || define == "PUERTS_CPP_OUTPUT_TO_UPM")
                        throw new InvalidOperationException("[Pie] External native output is unsupported. Remove CPP_OUTPUT_TO_NATIVE_SRC / PUERTS_CPP_OUTPUT_TO_UPM and recompile before generation.");
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone) ?? "";
            if (ContainsDefine(defines, "CPP_OUTPUT_TO_NATIVE_SRC") || ContainsDefine(defines, "PUERTS_CPP_OUTPUT_TO_UPM"))
                throw new InvalidOperationException("[Pie] Remove external native-output defines and recompile before generation.");
            if (!IsUnder(GetOutputDirectory(), Application.dataPath))
                throw new InvalidOperationException("[Pie] CodeOutputDirectory must be a dedicated directory inside Assets (normally Assets/Gen), not an external directory or Assets itself.");
        }

        // ── optimization define rule ────────────────────────────────────

        /// <summary>
        /// Apply the define rule for one build target. Returns true when the
        /// defines were modified (which implies Unity will recompile).
        /// </summary>
        public static bool ApplyDefineRule(NamedBuildTarget target)
        {
            EnsureSupportedPolicy();
            if (target != NamedBuildTarget.Standalone)
                throw new InvalidOperationException("[Pie] Define management supports Standalone only.");
            var generatedPresent = HasGeneratedBindings();
            var defines = PlayerSettings.GetScriptingDefineSymbols(target) ?? "";
            var hasDefine = ContainsDefine(defines, DisableOptimizationDefine);

            if (generatedPresent && hasDefine)
            {
                PlayerSettings.SetScriptingDefineSymbols(target,
                    RemoveDefine(defines, DisableOptimizationDefine));
                Debug.Log($"[Pie] Removed {DisableOptimizationDefine} for {target}: generated bindings exist, xIl2cpp optimization stays enabled.");
                return true;
            }
            if (!generatedPresent && !hasDefine)
            {
                PlayerSettings.SetScriptingDefineSymbols(target,
                    string.IsNullOrEmpty(defines) ? DisableOptimizationDefine : defines + ";" + DisableOptimizationDefine);
                Debug.Log($"[Pie] Added {DisableOptimizationDefine} for {target}: no generated bindings, using explicit reflection fallback.");
                return true;
            }
            return false;
        }

        private static bool ContainsDefine(string defines, string define)
        {
            return (";" + defines + ";").Contains(";" + define + ";");
        }

        private static string RemoveDefine(string defines, string define)
        {
            return (";" + defines + ";").Replace(";" + define + ";", ";").Trim(';');
        }

        // ── auto-generate setting (PieSettings) ─────────────────────────

        /// <summary>
        /// PieSettings.autoGeneratePuerTSBindings (default true). Missing
        /// settings asset means a fresh install, where auto-generation is the
        /// product intent, so the default is true.
        /// </summary>
        public static bool IsAutoGenerateEnabled()
        {
            var settings = LoadPieSettingsAsset();
            return settings == null || settings.AutoGeneratePuerTSBindings;
        }

        private static PieSettings LoadPieSettingsAsset()
        {
            var direct = AssetDatabase.LoadAssetAtPath<PieSettings>(PieProjectPaths.GetDefaultSettingsAssetPath());
            if (direct != null)
                return direct;
            var guids = AssetDatabase.FindAssets("t:PieSettings");
            foreach (var guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<PieSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                    return asset;
            }
            return null;
        }
    }
}
