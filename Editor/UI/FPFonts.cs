using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace NKStudio.FolderPainter
{
    /// <summary>
    /// Font of the Folder Painter UI (a subset of IBM Plex Sans KR).
    /// Characters missing from the subset, such as user folder names, fall back to the editor fonts.
    /// </summary>
    internal static class FPFonts
    {
        private const string FONTS_PATH = AssetInfo.PACKAGE_PATH + "/Editor/Fonts/";
        private const string REGULAR_PATH = FONTS_PATH + "FolderPainterSansKR-Regular.ttf";
        private const string BOLD_PATH = FONTS_PATH + "FolderPainterSansKR-Bold.ttf";

        // FontAsset.fontWeightTable is indexed by weight / 100
        private const int BOLD_WEIGHT_INDEX = 7;

        private static FontAsset _regular;
        private static FontAsset _bold;

        //---------------------------------------------------------------------
        // Ctors
        //---------------------------------------------------------------------

        static FPFonts()
        {
            // Font assets are created at runtime: release them before their owner domain goes away
            AssemblyReloadEvents.beforeAssemblyReload += Release;
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        /// <summary>
        /// Uses the Folder Painter font for the element and its children (the font is inherited).
        /// <see cref="FontStyle.Bold"/> picks the bold typeface. Keeps the editor font when the files are missing.
        /// </summary>
        public static void Apply(VisualElement element)
        {
            var font = GetFontAsset();
            if (font != null) element.style.unityFontDefinition = FontDefinition.FromSDFFont(font);
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static FontAsset GetFontAsset()
        {
            if (_regular != null) return _regular;

            _regular = CreateFontAsset(REGULAR_PATH);
            if (_regular == null) return null;

            _bold = CreateFontAsset(BOLD_PATH);
            var weights = _regular.fontWeightTable;
            if (_bold != null && weights != null && weights.Length > BOLD_WEIGHT_INDEX)
            {
                weights[BOLD_WEIGHT_INDEX].regularTypeface = _bold;
            }

            return _regular;
        }

        private static FontAsset CreateFontAsset(string path)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (font == null)
            {
                FPLogger.LogWarning($"Missing font: {path}. The editor font is used instead.");
                return null;
            }

            // Dynamic asset: glyphs are added to the atlas when they are first displayed
            var fontAsset = FontAsset.CreateFontAsset(font);
            if (fontAsset == null) return null;

            fontAsset.name = font.name;
            fontAsset.hideFlags = HideFlags.HideAndDontSave;
            if (fontAsset.material != null) fontAsset.material.hideFlags = HideFlags.HideAndDontSave;
            if (fontAsset.atlasTextures != null)
            {
                foreach (var texture in fontAsset.atlasTextures)
                {
                    if (texture != null) texture.hideFlags = HideFlags.HideAndDontSave;
                }
            }

            return fontAsset;
        }

        private static void Release()
        {
            Destroy(ref _regular);
            Destroy(ref _bold);
        }

        private static void Destroy(ref FontAsset fontAsset)
        {
            if (fontAsset == null) return;

            if (fontAsset.atlasTextures != null)
            {
                foreach (var texture in fontAsset.atlasTextures)
                {
                    if (texture != null) Object.DestroyImmediate(texture);
                }
            }

            if (fontAsset.material != null) Object.DestroyImmediate(fontAsset.material);
            Object.DestroyImmediate(fontAsset);
            fontAsset = null;
        }
    }
}
