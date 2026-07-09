using System;

namespace Pie
{
    public enum PieTypeScriptModule
    {
        ESNext,
        ES2022,
        ES2020,
        CommonJS,
    }

    public enum PieTypeScriptTarget
    {
        ES2022,
        ES2021,
        ES2020,
        ES2019,
        ESNext,
    }

    [Serializable]
    public sealed class PieTypeScriptCompileRequest
    {
        public string Source = "";
        public string SourcePath = "";
        public string OutputPath = "";
        public PieTypeScriptModule Module = PieTypeScriptModule.ESNext;
        public PieTypeScriptTarget Target = PieTypeScriptTarget.ES2022;
        public bool SourceMap = false;
        public bool InlineSourceMap = false;
        public bool RemoveComments = false;
    }

    [Serializable]
    public sealed class PieTypeScriptDiagnostic
    {
        public string Category = "";
        public int Code;
        public string Message = "";
        public string FileName = "";
        public int Line;
        public int Character;
    }

    [Serializable]
    public sealed class PieTypeScriptCompileResult
    {
        public bool Ok;
        public string ErrorMessage = "";
        public string OutputText = "";
        public string SourceMapText = "";
        public string OutputPath = "";
        public string SourceMapPath = "";
        public string TypeScriptVersion = "";
        public PieTypeScriptDiagnostic[] Diagnostics = new PieTypeScriptDiagnostic[0];

        public static PieTypeScriptCompileResult Failure(string message)
        {
            return new PieTypeScriptCompileResult
            {
                Ok = false,
                ErrorMessage = message ?? "",
                Diagnostics = new[]
                {
                    new PieTypeScriptDiagnostic
                    {
                        Category = "Error",
                        Code = 90000,
                        Message = message ?? "",
                    },
                },
            };
        }
    }
}
