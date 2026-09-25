#if UNITY_EDITOR
using System.IO;
using UnityEditor;

/// <summary>
/// Unity's incremental import does not watch content changes of file: package
/// assets that live outside the project folder, so a rebuilt runtime bundle
/// (Resources/pie/core.bytes) can keep serving the previously imported
/// version after editor restarts. Force a re-import of the bundle on every
/// domain reload so the editor and the package source stay in sync.
/// </summary>
[InitializeOnLoad]
internal static class PieUnityPackageAssetRefresh
{
    private const string BundleAssetPath = "Packages/com.pie.agent/Resources/pie/core.bytes";

    static PieUnityPackageAssetRefresh()
    {
        try
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(Pie.PieBridge).Assembly);
            var sourcePath = info != null ? Path.Combine(info.resolvedPath, "Resources", "pie", "core.bytes") : null;
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                return;
            AssetDatabase.ImportAsset(BundleAssetPath, ImportAssetOptions.ForceUpdate);
        }
        catch
        {
            // Import hygiene must never break editor startup.
        }
    }
}
#endif
