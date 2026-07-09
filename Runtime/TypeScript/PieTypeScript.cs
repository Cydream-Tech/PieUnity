using System;
using System.IO;
using UnityEngine;

namespace Pie
{
    public static class PieTypeScript
    {
        public static PieTypeScriptCompileResult CompileSource(PieTypeScriptCompileRequest request)
        {
            var normalized = request ?? new PieTypeScriptCompileRequest();
            if (string.IsNullOrEmpty(normalized.Source))
                return PieTypeScriptCompileResult.Failure("TypeScript source is required.");

            return InvokeCompiler(normalized);
        }

        public static void CompileSource(PieTypeScriptCompileRequest request, Action<PieTypeScriptCompileResult> callback)
        {
            callback?.Invoke(CompileSource(request));
        }

        public static PieTypeScriptCompileResult CompileFile(PieTypeScriptCompileRequest request)
        {
            var normalized = request ?? new PieTypeScriptCompileRequest();
            if (string.IsNullOrWhiteSpace(normalized.SourcePath))
                return PieTypeScriptCompileResult.Failure("SourcePath is required.");
            if (!File.Exists(normalized.SourcePath))
                return PieTypeScriptCompileResult.Failure($"TypeScript source file was not found: {normalized.SourcePath}");

            normalized.Source = File.ReadAllText(normalized.SourcePath);
            var result = InvokeCompiler(normalized);
            if (!result.Ok)
                return result;

            var outputPath = string.IsNullOrWhiteSpace(normalized.OutputPath)
                ? Path.ChangeExtension(normalized.SourcePath, ".mjs")
                : normalized.OutputPath;

            try
            {
                var outputDirectory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                File.WriteAllText(outputPath, result.OutputText ?? "");
                result.OutputPath = outputPath;

                if (normalized.SourceMap && !normalized.InlineSourceMap && !string.IsNullOrEmpty(result.SourceMapText))
                {
                    var sourceMapPath = outputPath + ".map";
                    File.WriteAllText(sourceMapPath, result.SourceMapText);
                    result.SourceMapPath = sourceMapPath;
                }
            }
            catch (Exception ex)
            {
                return PieTypeScriptCompileResult.Failure($"Failed to write TypeScript output: {ex.Message}");
            }

            return result;
        }

        public static void CompileFile(PieTypeScriptCompileRequest request, Action<PieTypeScriptCompileResult> callback)
        {
            callback?.Invoke(CompileFile(request));
        }

        private static PieTypeScriptCompileResult InvokeCompiler(PieTypeScriptCompileRequest request)
        {
            var bridge = PieBridge.Instance;
            if (bridge == null || !bridge.IsInitialized)
                return PieTypeScriptCompileResult.Failure("PieBridge is not initialized.");

            try
            {
                var payload = new CompilePayload
                {
                    source = request.Source ?? "",
                    sourcePath = request.SourcePath ?? "",
                    module = ToModuleString(request.Module),
                    target = ToTargetString(request.Target),
                    sourceMap = request.SourceMap,
                    inlineSourceMap = request.InlineSourceMap,
                    removeComments = request.RemoveComments,
                };
                var json = JsonUtility.ToJson(payload);
                var resultJson = bridge.InvokeTypeScriptCompiler(json);
                return FromPayload(JsonUtility.FromJson<CompileResultPayload>(resultJson ?? "{}"));
            }
            catch (Exception ex)
            {
                return PieTypeScriptCompileResult.Failure(ex.Message);
            }
        }

        private static PieTypeScriptCompileResult FromPayload(CompileResultPayload payload)
        {
            if (payload == null)
                return PieTypeScriptCompileResult.Failure("TypeScript compiler returned an empty response.");

            var diagnostics = payload.diagnostics ?? new CompileDiagnosticPayload[0];
            var mappedDiagnostics = new PieTypeScriptDiagnostic[diagnostics.Length];
            for (var i = 0; i < diagnostics.Length; i++)
            {
                mappedDiagnostics[i] = new PieTypeScriptDiagnostic
                {
                    Category = diagnostics[i].category ?? "",
                    Code = diagnostics[i].code,
                    Message = diagnostics[i].message ?? "",
                    FileName = diagnostics[i].fileName ?? "",
                    Line = diagnostics[i].line,
                    Character = diagnostics[i].character,
                };
            }

            return new PieTypeScriptCompileResult
            {
                Ok = payload.ok,
                ErrorMessage = payload.errorMessage ?? "",
                OutputText = payload.outputText ?? "",
                SourceMapText = payload.sourceMapText ?? "",
                TypeScriptVersion = payload.typeScriptVersion ?? "",
                Diagnostics = mappedDiagnostics,
            };
        }

        private static string ToModuleString(PieTypeScriptModule module)
        {
            switch (module)
            {
                case PieTypeScriptModule.CommonJS:
                    return "CommonJS";
                case PieTypeScriptModule.ES2020:
                    return "ES2020";
                case PieTypeScriptModule.ES2022:
                    return "ES2022";
                default:
                    return "ESNext";
            }
        }

        private static string ToTargetString(PieTypeScriptTarget target)
        {
            switch (target)
            {
                case PieTypeScriptTarget.ES2019:
                    return "ES2019";
                case PieTypeScriptTarget.ES2020:
                    return "ES2020";
                case PieTypeScriptTarget.ES2021:
                    return "ES2021";
                case PieTypeScriptTarget.ESNext:
                    return "ESNext";
                default:
                    return "ES2022";
            }
        }

        [Serializable]
        private sealed class CompilePayload
        {
            public string source;
            public string sourcePath;
            public string module;
            public string target;
            public bool sourceMap;
            public bool inlineSourceMap;
            public bool removeComments;
        }

        [Serializable]
        private sealed class CompileResultPayload
        {
            public bool ok;
            public string outputText;
            public string sourceMapText;
            public string errorMessage;
            public CompileDiagnosticPayload[] diagnostics;
            public string typeScriptVersion;
        }

        [Serializable]
        private sealed class CompileDiagnosticPayload
        {
            public string category;
            public int code;
            public string message;
            public string fileName;
            public int line;
            public int character;
        }
    }
}
