// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace NKStudio.FolderPainter
{
    /// <summary>
    /// Lazily decodes the base64 encoded PNGs embedded in the storages and caches the resulting textures.
    /// </summary>
    public static class TexturesStorageHelper<T>
    {
        // Ids already reported as missing
        private static readonly HashSet<T> MISSING = new HashSet<T>();

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        /// <summary>
        /// Returns null for an unknown id, e.g. an icon removed from the package but still referenced by an old
        /// ruleset: the folder then keeps its default icon. The warning is logged only once per id.
        /// </summary>
        public static Tuple<Texture2D, Texture2D> GetTextures(T type, FilterMode filterMode, Dictionary<T, Tuple<Lazy<string>, Lazy<string>>> strings, Dictionary<T, Tuple<Texture2D, Texture2D>> textures)
        {
            if (textures.TryGetValue(type, out var pair) && pair.Item1 != null && pair.Item2 != null) return pair;

            if (!strings.TryGetValue(type, out var lazyStrings))
            {
                if (MISSING.Add(type)) FPLogger.LogWarning($"Cannot find texture with ID: {type}");
                return null;
            }

            pair = Tuple.Create(TextureFromString(lazyStrings.Item1.Value, filterMode), TextureFromString(lazyStrings.Item2.Value, filterMode));
            textures[type] = pair;
            return pair;
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static Texture2D TextureFromString(string value, FilterMode filterMode)
        {
            // LoadImage resizes the texture and picks the format from the PNG data
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false, false)
            {
                filterMode = filterMode,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.LoadImage(Convert.FromBase64String(value));
            return texture;
        }
    }
}
