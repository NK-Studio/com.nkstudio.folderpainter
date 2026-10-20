using System;
using UnityEditor;
using UnityEngine;

namespace NKStudio.FolderPainter
{
    /// <summary>
    /// Draws the color, tag and transparent folder icons, the color row backgrounds and the editor textures
    /// (edit icon, preview gradients, project tree lines, buttons) in code, so the package ships no bitmap for them.
    /// </summary>
    /// <remarks>
    /// The folder outline follows the plain folder shape of the Unity editor. Icon shapes are described on a
    /// 256 x 256 grid and the 64 x 64 editor textures on their own pixel grid, both with the origin at the top left,
    /// and rendered with 4 x 4 supersampling.
    /// </remarks>
    internal static class ProceduralTextures
    {
        private const int LARGE_SIZE = 256;
        private const int SMALL_SIZE = 64;
        private const int BACKGROUND_SIZE = 16;
        private const int SUPERSAMPLING = 4;
        private const float GRID = 256f;

        // Folder outline on the 256 grid
        private const float BODY_LEFT = 32f, BODY_RIGHT = 224f, BODY_TOP = 80f, BODY_BOTTOM = 208f, BODY_RADIUS = 13f;
        private const float TAB_TOP = 48f, TAB_RIGHT = 112f, TAB_SLANT_RIGHT = 132f, TAB_RADIUS = 13f;

        // Tag: rounded square in the lower right corner, cut out of the folder with a gap around it
        private const float TAG_LEFT = 164f, TAG_TOP = 148f, TAG_SIZE = 40f, TAG_RADIUS = 5f, TAG_GAP = 4f;

        // Row backgrounds fade from this opacity on the left to transparent on the right
        private const float BACKGROUND_ALPHA = 0.26f;

        // Light editor skin: colors are darkened to keep the same contrast against the brighter UI
        private const float LIGHT_SKIN_SHADE = 0.82f;

        private static readonly Color32 FOLDER_GRAY_DARK_SKIN = new Color32(196, 196, 196, 255);
        private static readonly Color32 FOLDER_GRAY_LIGHT_SKIN = new Color32(85, 85, 85, 255);

        // Edit icon: drawn over the folder icon while the edit modifier is held, so it is opaque
        private const float EDIT_OUTLINE = 8f;
        private static readonly Color32 EDIT_BACKGROUND_DARK_SKIN = new Color32(51, 51, 51, 255);
        private static readonly Color32 EDIT_BACKGROUND_LIGHT_SKIN = new Color32(190, 190, 190, 255);
        private static readonly Color32 EDIT_OUTLINE_DARK_SKIN = new Color32(166, 166, 166, 255);
        private static readonly Color32 EDIT_OUTLINE_LIGHT_SKIN = new Color32(107, 107, 107, 255);
        private static readonly Color32 EDIT_CHECKER_DARK_SKIN = new Color32(108, 108, 108, 255);
        private static readonly Color32 EDIT_CHECKER_LIGHT_SKIN = new Color32(134, 134, 134, 255);

        // Preview gradients: shade the lower left corner of a large preview, under the small icon
        private static readonly Color32 POPUP_SHADE_DARK_SKIN = new Color32(46, 46, 46, 255);
        private static readonly Color32 POPUP_SHADE_LIGHT_SKIN = new Color32(212, 212, 212, 255);
        private static readonly Color32 DRAWER_SHADE_DARK_SKIN = new Color32(65, 65, 65, 255);
        private static readonly Color32 DRAWER_SHADE_LIGHT_SKIN = new Color32(200, 200, 200, 255);

        // Project tree lines, drawn at 4 pixels per point: one level is 14 points wide (Unity's tree indent)
        private const int TREE_PIXELS_PER_POINT = 4;
        private const int TREE_LEVELS = 9;
        private static readonly Color32 TREE_LINE_COLOR = new Color32(99, 99, 99, 255);

        // Buttons
        private static readonly Color32 SETTINGS_DARK_SKIN = new Color32(232, 232, 232, 255);
        private static readonly Color32 SETTINGS_LIGHT_SKIN = new Color32(85, 85, 85, 255);

        /// <summary>
        /// NK Studio palette, in <see cref="ProjectIcon"/> / <see cref="ProjectBackground"/> color order (Red → Crimson).
        /// OKLCH: chroma 0.165, lightness 0.66 raised up to 0.80 around yellow, hues placed by name; clipped to sRGB.
        /// </summary>
        private static readonly Color32[] PALETTE =
        {
            Hex(0xE6625D), // Red          25°
            Hex(0xE76B3E), // Vermilion    40°
            Hex(0xE97E17), // Orange       56°
            Hex(0xEA9E00), // Amber        76°
            Hex(0xD6BE07), // Yellow      100°
            Hex(0xB7C733), // Lime        116°
            Hex(0x95C747), // Chartreuse  128°
            Hex(0x6CC259), // Harlequin   140°
            Hex(0x41BD67), // Green       150°
            Hex(0x00B779), // Emerald     162°
            Hex(0x00B489), // SpringGreen 172°
            Hex(0x00B1A0), // Aquamarine  185°
            Hex(0x00ADBD), // Cyan        202°
            Hex(0x00A6DA), // SkyBlue     222°
            Hex(0x009CEC), // Azure       240°
            Hex(0x3F93F4), // Cerulean    254°
            Hex(0x678AF6), // Blue        268°
            Hex(0x8482F3), // Indigo      282°
            Hex(0x9D79EA), // Violet      296°
            Hex(0xB171DD), // Purple      310°
            Hex(0xC669C5), // Magenta     328°
            Hex(0xD663A9), // Fuchsia     345°
            Hex(0xDF608E), // Rose          0°
            Hex(0xE46077), // Crimson      12°
        };

        private const int AMBER = 3;
        private const int VERMILION = 1;

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        /// <summary>Whether the icon is drawn here rather than decoded from an embedded PNG.</summary>
        public static bool IsProcedural(ProjectIcon icon)
        {
            return GetColorIndex(icon) >= 0 || GetTagIndex(icon) >= 0 || GetTransparency(icon) > 0f;
        }

        /// <summary>Large (x256) and small (x64) textures of a procedural icon, or null for any other icon.</summary>
        public static Tuple<Texture2D, Texture2D> CreateIcons(ProjectIcon icon)
        {
            if (!IsProcedural(icon)) return null;
            return Tuple.Create(CreateIcon(icon, LARGE_SIZE), CreateIcon(icon, SMALL_SIZE));
        }

        /// <summary>Row background for a color, or null for any other background.</summary>
        public static Texture2D CreateBackground(ProjectBackground background)
        {
            var index = GetColorIndex((int) background);
            if (index < 0) return null;

            var color = (Color) PALETTE[index];
            var pixels = new Color[BACKGROUND_SIZE * BACKGROUND_SIZE];
            for (var x = 0; x < BACKGROUND_SIZE; x++)
            {
                color.a = BACKGROUND_ALPHA * (1f - (x + 0.5f) / BACKGROUND_SIZE);
                for (var y = 0; y < BACKGROUND_SIZE; y++) pixels[y * BACKGROUND_SIZE + x] = color;
            }

            return CreateTexture(BACKGROUND_SIZE, BACKGROUND_SIZE, pixels);
        }

        public static Texture2D CreateEditorTexture(ProjectEditorTexture type)
        {
            switch (type)
            {
                case ProjectEditorTexture.IcnEditSmall: return CreateEditIcon(SMALL_SIZE, 4);
                case ProjectEditorTexture.IcnEditLarge: return CreateEditIcon(LARGE_SIZE, 8);
                case ProjectEditorTexture.IcnPreviewGradientPopup: return CreatePreviewGradient(Skin(POPUP_SHADE_DARK_SKIN, POPUP_SHADE_LIGHT_SKIN));
                case ProjectEditorTexture.IcnPreviewGradientDrawer: return CreatePreviewGradient(Skin(DRAWER_SHADE_DARK_SKIN, DRAWER_SHADE_LIGHT_SKIN));
                case ProjectEditorTexture.IcnFoldoutMiddle: return CreateTreeBranch();
                case ProjectEditorTexture.IcnFoldoutLevels: return CreateTreeLevels();
                case ProjectEditorTexture.IcnFilter: return Render(SMALL_SIZE, SMALL_SIZE, (x, y) => IsInFunnel(x, y) ? Shade(PALETTE[AMBER]) : Color.clear);
                case ProjectEditorTexture.IcnDelete: return Render(SMALL_SIZE, SMALL_SIZE, (x, y) => IsInCross(x, y) ? Shade(PALETTE[VERMILION]) : Color.clear);
                case ProjectEditorTexture.IcnSettings: return Render(SMALL_SIZE, SMALL_SIZE, (x, y) => IsInGear(x, y) ? Skin(SETTINGS_DARK_SKIN, SETTINGS_LIGHT_SKIN) : Color.clear);
                default: return null;
            }
        }

        //---------------------------------------------------------------------
        // Icons
        //---------------------------------------------------------------------

        private static Texture2D CreateIcon(ProjectIcon icon, int size)
        {
            var colorIndex = GetColorIndex(icon);
            var tagIndex = GetTagIndex(icon);
            var transparency = GetTransparency(icon);

            var folderColor = colorIndex >= 0 ? Shade(PALETTE[colorIndex]) : Skin(FOLDER_GRAY_DARK_SKIN, FOLDER_GRAY_LIGHT_SKIN);
            if (transparency > 0f) folderColor.a = transparency;
            var tagColor = tagIndex >= 0 ? Shade(PALETTE[tagIndex]) : Color.clear;

            var scale = GRID / size;
            return Render(size, size, (px, py) =>
            {
                float x = px * scale, y = py * scale;
                if (tagIndex >= 0 && IsInTag(x, y, 0f)) return tagColor;
                if (IsInFolder(x, y, 0f) && (tagIndex < 0 || !IsInTag(x, y, TAG_GAP))) return folderColor;
                return Color.clear;
            });
        }

        /// <summary>Folder outline over a checkerboard, on an opaque background.</summary>
        private static Texture2D CreateEditIcon(int size, int checkerPixels)
        {
            var background = Skin(EDIT_BACKGROUND_DARK_SKIN, EDIT_BACKGROUND_LIGHT_SKIN);
            var outline = Skin(EDIT_OUTLINE_DARK_SKIN, EDIT_OUTLINE_LIGHT_SKIN);
            var checker = Skin(EDIT_CHECKER_DARK_SKIN, EDIT_CHECKER_LIGHT_SKIN);

            var scale = GRID / size;
            // The outline keeps a visible width on the small icon
            var outlineWidth = Mathf.Max(EDIT_OUTLINE, 2f * scale);

            return Render(size, size, (px, py) =>
            {
                float x = px * scale, y = py * scale;
                if (!IsInFolder(x, y, 0f)) return background;
                if (!IsInFolder(x, y, outlineWidth)) return outline;

                var odd = ((int) (px / checkerPixels) + (int) (py / checkerPixels)) % 2 == 1;
                return odd ? checker : background;
            });
        }

        /// <summary>
        /// Folder outline: a rounded body with a tab on its upper left, whose right side slants down.
        /// A positive inset shrinks the shape, which is used to draw outlines.
        /// </summary>
        private static bool IsInFolder(float x, float y, float inset)
        {
            var left = BODY_LEFT + inset;
            var bodyRadius = Mathf.Max(1f, BODY_RADIUS - inset * 0.5f);
            if (IsInRoundedRect(x, y, left, BODY_TOP + inset, BODY_RIGHT - inset, BODY_BOTTOM - inset, bodyRadius)) return true;

            var top = TAB_TOP + inset;
            if (y < top || y > BODY_TOP + inset + bodyRadius || x < left) return false;

            // The slanted side moves left by a bit more than the inset, so the outline keeps its width there
            var slant = (TAB_SLANT_RIGHT - TAB_RIGHT) / (BODY_TOP - TAB_TOP);
            var shift = inset * Mathf.Sqrt(1f + slant * slant);
            var right = TAB_RIGHT - shift + (TAB_SLANT_RIGHT - TAB_RIGHT) * Mathf.Clamp01((y - TAB_TOP) / (BODY_TOP - TAB_TOP));
            if (x > right) return false;

            // Rounded upper left corner
            var radius = Mathf.Max(1f, TAB_RADIUS - inset * 0.5f);
            if (x < left + radius && y < top + radius)
                return Sqr(x - (left + radius)) + Sqr(y - (top + radius)) <= radius * radius;

            return true;
        }

        private static bool IsInTag(float x, float y, float grow)
        {
            return IsInRoundedRect(x, y, TAG_LEFT - grow, TAG_TOP - grow, TAG_LEFT + TAG_SIZE + grow, TAG_TOP + TAG_SIZE + grow, TAG_RADIUS + grow);
        }

        //---------------------------------------------------------------------
        // Editor textures (64 x 64 pixel grid unless noted)
        //---------------------------------------------------------------------

        /// <summary>Opaque in the lower left corner, fading out along the diagonal.</summary>
        private static Texture2D CreatePreviewGradient(Color shade)
        {
            return Render(SMALL_SIZE, SMALL_SIZE, (x, y) =>
            {
                // Distance from the lower left corner, measured across the diagonal
                var distance = x + (SMALL_SIZE - y);
                var color = shade;
                color.a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(16f, 52f, distance));
                return color;
            });
        }

        /// <summary>Branch for tree items without children: a vertical line with a stub to the right.</summary>
        private static Texture2D CreateTreeBranch()
        {
            return Render(SMALL_SIZE, SMALL_SIZE, (x, y) =>
            {
                var vertical = x >= 32f && x < 36f;
                var stub = y >= 32f && y < 36f && x >= 36f && x < 56f;
                return vertical || stub ? (Color) TREE_LINE_COLOR : Color.clear;
            });
        }

        /// <summary>
        /// One dashed vertical line per tree level (512 x 64), getting more visible towards the item.
        /// The texture is drawn right-aligned in front of each item.
        /// </summary>
        private static Texture2D CreateTreeLevels()
        {
            const int width = 128 * TREE_PIXELS_PER_POINT;
            const int levelWidth = 14 * TREE_PIXELS_PER_POINT;
            const int firstLine = 10 * TREE_PIXELS_PER_POINT;
            const int dash = 2 * TREE_PIXELS_PER_POINT;

            return Render(width, SMALL_SIZE, (x, y) =>
            {
                var level = Mathf.FloorToInt((x - firstLine) / levelWidth);
                if (level < 0 || level >= TREE_LEVELS) return Color.clear;

                var inLine = x - firstLine - level * levelWidth < TREE_PIXELS_PER_POINT;
                var inDash = Mathf.FloorToInt((y - TREE_PIXELS_PER_POINT) / dash) % 2 == 0 && y >= TREE_PIXELS_PER_POINT;
                if (!inLine || !inDash) return Color.clear;

                var color = (Color) TREE_LINE_COLOR;
                color.a = 0.1f * (level + 1);
                return color;
            });
        }

        /// <summary>Gear with eight teeth and a round hole.</summary>
        private static bool IsInGear(float x, float y)
        {
            const float cx = 32f, cy = 32f, outer = 27f, inner = 20f, hole = 9f;
            const int teeth = 8;

            var dx = x - cx;
            var dy = y - cy;
            var r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r < hole || r > outer) return false;
            if (r <= inner) return true;

            // Teeth cover a bit less than half of each sector
            var angle = Mathf.Atan2(dy, dx) / (2f * Mathf.PI) * teeth;
            var fraction = angle - Mathf.Floor(angle);
            return Mathf.Abs(fraction - 0.5f) < 0.22f;
        }

        /// <summary>Funnel: a wide rounded top that narrows into a stem.</summary>
        private static bool IsInFunnel(float x, float y)
        {
            // Rim
            if (IsInRoundedRect(x, y, 6f, 8f, 58f, 17f, 3f)) return true;

            // Cone from the rim down to the stem
            if (y >= 17f && y <= 36f)
            {
                var t = (y - 17f) / 19f;
                var halfWidth = Mathf.Lerp(23f, 7f, t);
                return Mathf.Abs(x - 32f) <= halfWidth;
            }

            // Stem, cut at an angle at the bottom
            return x >= 25f && x <= 39f && y >= 36f && y <= 50f + (x - 25f) * 0.43f;
        }

        /// <summary>Two rounded bars crossing at the center.</summary>
        private static bool IsInCross(float x, float y)
        {
            const float halfLength = 25f, halfThickness = 6.5f;

            var dx = x - 32f;
            var dy = y - 32f;
            var u = (dx + dy) * 0.70710678f;
            var v = (dx - dy) * 0.70710678f;
            return IsInCapsule(u, v, halfLength, halfThickness) || IsInCapsule(v, u, halfLength, halfThickness);
        }

        private static bool IsInCapsule(float along, float across, float halfLength, float halfThickness)
        {
            var a = Mathf.Max(0f, Mathf.Abs(along) - (halfLength - halfThickness));
            return a * a + across * across <= halfThickness * halfThickness;
        }

        //---------------------------------------------------------------------
        // Ids
        //---------------------------------------------------------------------

        // Colors use ids 10, 20, ... 240 in both enums; tags reuse them offset by 1000
        private static int GetColorIndex(ProjectIcon icon) => GetColorIndex((int) icon);

        private static int GetTagIndex(ProjectIcon icon) => GetColorIndex((int) icon - 1000);

        private static int GetColorIndex(int id)
        {
            if (id < 10 || id % 10 != 0) return -1;
            var index = id / 10 - 1;
            return index < PALETTE.Length ? index : -1;
        }

        private static float GetTransparency(ProjectIcon icon)
        {
            switch (icon)
            {
                case ProjectIcon.Transparent15: return 0.15f;
                case ProjectIcon.Transparent25: return 0.25f;
                case ProjectIcon.Transparent50: return 0.50f;
                case ProjectIcon.Transparent75: return 0.75f;
                default: return 0f;
            }
        }

        //---------------------------------------------------------------------
        // Rendering
        //---------------------------------------------------------------------

        /// <summary>
        /// Renders a texture by averaging <paramref name="sample"/> over SUPERSAMPLING² points per pixel.
        /// Sample coordinates are in pixels, with the origin at the top left.
        /// </summary>
        private static Texture2D Render(int width, int height, Func<float, float, Color> sample)
        {
            var pixels = new Color[width * height];
            const float step = 1f / SUPERSAMPLING;
            const float samples = SUPERSAMPLING * SUPERSAMPLING;

            for (var py = 0; py < height; py++)
            {
                for (var px = 0; px < width; px++)
                {
                    // Premultiplied sum, so edges blend towards the color and not towards black
                    var sum = Vector4.zero;
                    for (var sy = 0; sy < SUPERSAMPLING; sy++)
                    {
                        var y = py + (sy + 0.5f) * step;
                        for (var sx = 0; sx < SUPERSAMPLING; sx++)
                        {
                            var c = sample(px + (sx + 0.5f) * step, y);
                            sum += new Vector4(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
                        }
                    }

                    var alpha = sum.w / samples;
                    var color = alpha > 0f ? new Color(sum.x / sum.w, sum.y / sum.w, sum.z / sum.w, alpha) : Color.clear;

                    // Textures start at the bottom row
                    pixels[(height - 1 - py) * width + px] = color;
                }
            }

            return CreateTexture(width, height, pixels);
        }

        private static bool IsInRoundedRect(float x, float y, float left, float top, float right, float bottom, float radius)
        {
            if (x < left || x > right || y < top || y > bottom) return false;

            var cx = Mathf.Clamp(x, left + radius, right - radius);
            var cy = Mathf.Clamp(y, top + radius, bottom - radius);
            return Sqr(x - cx) + Sqr(y - cy) <= radius * radius;
        }

        private static Color Skin(Color32 darkSkin, Color32 lightSkin)
        {
            return EditorGUIUtility.isProSkin ? darkSkin : lightSkin;
        }

        private static Color Shade(Color32 color)
        {
            var result = (Color) color;
            if (EditorGUIUtility.isProSkin) return result;

            result.r *= LIGHT_SKIN_SHADE;
            result.g *= LIGHT_SKIN_SHADE;
            result.b *= LIGHT_SKIN_SHADE;
            return result;
        }

        private static Texture2D CreateTexture(int width, int height, Color[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Color32 Hex(int rgb) => new Color32((byte) (rgb >> 16), (byte) (rgb >> 8), (byte) rgb, 255);

        private static float Sqr(float value) => value * value;
    }
}
