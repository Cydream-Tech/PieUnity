using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEditor.UnityLinker;

namespace Pie.Editor
{
    /// <summary>
    /// Supplies Pie's linker descriptor explicitly because Unity does not
    /// discover link.xml files inside UPM packages automatically.
    /// </summary>
    public sealed class PieUnityLinkerProcessor : IUnityLinkerProcessor
    {
        public int callbackOrder => 0;

        public string GenerateAdditionalLinkXmlFile(
            BuildReport report,
            UnityLinkerBuildPipelineData data)
        {
            var packageInfo = PackageInfo.FindForAssembly(typeof(PieUnityLinkerProcessor).Assembly);
            if (packageInfo == null || string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                throw new BuildFailedException(
                    "PieUnity could not resolve its installed package path for IL2CPP preservation.");
            }

            var linkXmlPath = Path.Combine(packageInfo.resolvedPath, "Runtime", "link.xml");
            if (!File.Exists(linkXmlPath))
            {
                throw new BuildFailedException(
                    $"PieUnity IL2CPP linker descriptor is missing: {linkXmlPath}");
            }

            return linkXmlPath;
        }
    }
}
