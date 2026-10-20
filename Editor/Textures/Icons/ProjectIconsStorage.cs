// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using StorageHelper = NKStudio.FolderPainter.TexturesStorageHelper<NKStudio.FolderPainter.ProjectIcon>;

namespace NKStudio.FolderPainter
{
    public static class ProjectIconsStorage
    {
        private static readonly Dictionary<ProjectIcon, Tuple<Texture2D, Texture2D>> ICON_TEXTURES;
        private static readonly Dictionary<ProjectIcon, Tuple<Lazy<string>, Lazy<string>>> ICON_STRINGS;

        //---------------------------------------------------------------------
        // Ctors
        //---------------------------------------------------------------------

        static ProjectIconsStorage()
        {
            ICON_TEXTURES = new Dictionary<ProjectIcon, Tuple<Texture2D, Texture2D>>();
            ICON_STRINGS = (EditorGUIUtility.isProSkin) ? ProjectIconsArchivePro.GetDict()
                : ProjectIconsArchiveFree.GetDict();
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        /// <summary>
        /// Returns the large (x64) and small (x16) textures of a built-in icon, in that order,
        /// or null when the icon does not exist.
        /// </summary>
        public static Tuple<Texture2D, Texture2D> GetIcons(ProjectIcon type)
        {
            if (!ProceduralTextures.IsProcedural(type))
                return StorageHelper.GetTextures(type, FilterMode.Bilinear, ICON_STRINGS, ICON_TEXTURES);

            // Cached textures could have been destroyed (e.g. by an editor tool unloading assets)
            if (ICON_TEXTURES.TryGetValue(type, out var pair) && pair.Item1 != null && pair.Item2 != null) return pair;

            pair = ProceduralTextures.CreateIcons(type);
            ICON_TEXTURES[type] = pair;
            return pair;
        }

        public static Texture2D GetIcon(ProjectIcon type, bool small)
        {
            var icons = GetIcons(type);
            if (icons == null) return null;
            return small ? icons.Item2 : icons.Item1;
        }
    }
}
