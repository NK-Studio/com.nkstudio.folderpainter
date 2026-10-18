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

        // Rainbow Folders release this package was ported from: its documentation still applies
        private const string BASE_VERSION = "2.4.2";
        public const string HELP_URL = "http://www.borodar.com/stuff/rainbowfolders/docs/quickstart_v" + BASE_VERSION + ".pdf";

        private static string _version;

        /// <summary>Version declared in package.json.</summary>
        public static string Version =>
            _version ??= PackageInfo.FindForAssembly(typeof(AssetInfo).Assembly)?.version ?? BASE_VERSION;
    }
}
