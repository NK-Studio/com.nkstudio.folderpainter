// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored; editor textures are drawn in code.

using System.Collections.Generic;
using UnityEngine;

namespace NKStudio.FolderPainter
{
    public static class ProjectEditorTexturesStorage
    {
        private static readonly Dictionary<ProjectEditorTexture, Texture2D> EDITOR_TEXTURES = new Dictionary<ProjectEditorTexture, Texture2D>();

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public static Texture2D GetTexture(ProjectEditorTexture type)
        {
            // Cached textures could have been destroyed (e.g. by an editor tool unloading assets)
            if (EDITOR_TEXTURES.TryGetValue(type, out var texture) && texture != null) return texture;

            texture = ProceduralTextures.CreateEditorTexture(type);
            if (texture == null)
            {
                texture = Texture2D.grayTexture;
                FPLogger.LogWarning($"Cannot find texture with ID: {type}");
            }

            EDITOR_TEXTURES[type] = texture;
            return texture;
        }
    }
}
