// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored.

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NKStudio.FolderPainter
{
    public static class ProjectEditorUtility
    {
        private const string CLOSE_ICON_PATH = AssetInfo.PACKAGE_PATH + "/Editor/Images/Close.png";

        private static Texture2D _defaultFolderIcon;
        private static Texture2D _closeIcon;
        private static string _projectName;

        //---------------------------------------------------------------------
        // Project
        //---------------------------------------------------------------------

        /// <summary>Name of the project folder, used to keep editor preferences apart per project.</summary>
        public static string ProjectName => _projectName ??= Path.GetFileName(Path.GetDirectoryName(Application.dataPath));

        //---------------------------------------------------------------------
        // Assets
        //---------------------------------------------------------------------

        /// <summary>
        /// Creates a .asset file of the specified <see cref="UnityEngine.ScriptableObject"/> inside the given folder
        /// (missing folders are created) and returns its path.
        /// </summary>
        public static string CreateAsset<T>(string folderPath, string baseName) where T : ScriptableObject
        {
            if (baseName.Contains("/"))
                throw new ArgumentException("Base name should not contain slashes", nameof(baseName));

            if (string.IsNullOrEmpty(folderPath)) folderPath = "Assets";
            EnsureAssetFolder(folderPath);

            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{baseName}.asset");
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), assetPath);
            AssetDatabase.SaveAssets();

            return assetPath;
        }

        /// <summary>
        /// Creates every missing folder of an "Assets/..." path through the AssetDatabase.
        /// </summary>
        public static void EnsureAssetFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            var parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureAssetFolder(parent);

            AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
        }

        public static string[] FindPathsForAllRulesets()
        {
            return AssetDatabase.FindAssets($"t:{nameof(ProjectRuleset)}")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .ToArray();
        }

        //---------------------------------------------------------------------
        // Textures
        //---------------------------------------------------------------------

        public static Texture2D GetDefaultFolderIcon()
        {
            if (_defaultFolderIcon == null)
                _defaultFolderIcon = EditorGUIUtility.FindTexture("Folder Icon");

            return _defaultFolderIcon;
        }

        /// <summary>White close icon: tint it to match the editor skin.</summary>
        public static Texture2D GetCloseIcon()
        {
            if (_closeIcon == null)
                _closeIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(CLOSE_ICON_PATH);

            return _closeIcon;
        }

        public static Texture2D GetEditFolderIcon(bool isSmall)
        {
            return GetTexture(isSmall ? ProjectEditorTexture.IcnEditSmall : ProjectEditorTexture.IcnEditLarge);
        }

        public static Texture2D GetSettingsButtonIcon() => GetTexture(ProjectEditorTexture.IcnSettings);
        public static Texture2D GetFilterButtonIcon() => GetTexture(ProjectEditorTexture.IcnFilter);
        public static Texture2D GetDeleteButtonIcon() => GetTexture(ProjectEditorTexture.IcnDelete);
        public static Texture2D GetFoldoutIcon() => GetTexture(ProjectEditorTexture.IcnFoldoutMiddle);
        public static Texture2D GetFoldoutLevelsIcon() => GetTexture(ProjectEditorTexture.IcnFoldoutLevels);
        public static Texture2D GetPreviewGradientPopup() => GetTexture(ProjectEditorTexture.IcnPreviewGradientPopup);
        public static Texture2D GetPreviewGradientDrawer() => GetTexture(ProjectEditorTexture.IcnPreviewGradientDrawer);

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        // The storage caches decoded textures, no need to keep another copy here
        private static Texture2D GetTexture(ProjectEditorTexture type)
        {
            return ProjectEditorTexturesStorage.GetTexture(type);
        }
    }
}
