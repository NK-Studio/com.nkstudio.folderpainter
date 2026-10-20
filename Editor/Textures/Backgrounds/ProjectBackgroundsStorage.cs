// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored; backgrounds are drawn in code.

using System.Collections.Generic;
using UnityEngine;

namespace NKStudio.FolderPainter
{
    public static class ProjectBackgroundsStorage
    {
        private static readonly Dictionary<ProjectBackground, Texture2D> BACKGROUND_TEXTURES = new Dictionary<ProjectBackground, Texture2D>();

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public static Texture2D GetBackground(ProjectBackground type)
        {
            // Cached textures could have been destroyed (e.g. by an editor tool unloading assets)
            if (BACKGROUND_TEXTURES.TryGetValue(type, out var texture) && texture != null) return texture;

            texture = ProceduralTextures.CreateBackground(type);
            if (texture == null)
            {
                texture = Texture2D.grayTexture;
                FPLogger.LogWarning($"Cannot find texture with ID: {type}");
            }

            BACKGROUND_TEXTURES[type] = texture;
            return texture;
        }
    }
}
