// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored.

using UnityEditor.PackageManager;

namespace NKStudio.FolderPainter
{
    internal static class AssetInfo
    {
        public const string NAME = "Folder Painter";

        /// <summary>Asset path of the package root (Unity uses the package name, not the folder name on disk).</summary>
        public const string PACKAGE_PATH = "Packages/com.nkstudio.folderpainter";

        public const string DOCS_URL = "https://nk-studio.github.io/Packages/com.nkstudio.folderpainter@1.0/manual/index.html";
        public const string HELP_URL = "https://nk-studio.github.io/Packages/com.nkstudio.folderpainter@1.0/manual/rulesets.html";

        // Shown when the package info cannot be read (e.g. the code was copied into Assets)
        private const string FALLBACK_VERSION = "1.0.0";

        private static string _version;

        /// <summary>Version declared in package.json.</summary>
        public static string Version =>
            _version ??= PackageInfo.FindForAssembly(typeof(AssetInfo).Assembly)?.version ?? FALLBACK_VERSION;
    }
}
