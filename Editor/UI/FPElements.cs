using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static NKStudio.FolderPainter.FPLocalization;

namespace NKStudio.FolderPainter
{
    /// <summary>
    /// Shared UI Toolkit building blocks for Folder Painter windows.
    /// </summary>
    internal static class FPElements
    {
        private const string STYLE_SHEET_PATH = AssetInfo.PACKAGE_PATH + "/Editor/Styles/FolderPainter.uss";

        /// <summary>Class of custom elements that react to clicks (gives them the hand cursor).</summary>
        internal const string CLICKABLE_CLASS = "fp-clickable";

        internal static readonly Color HIGHLIGHT_CLR = new Color(0.24f, 0.49f, 0.91f);
        private static readonly Color HOVER_CLR_PRO = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color HOVER_CLR_FREE = new Color(0f, 0f, 0f, 0.08f);
        private static readonly Color PANEL_CLR_PRO = new Color(0.16f, 0.16f, 0.16f);
        private static readonly Color PANEL_CLR_FREE = new Color(0.79f, 0.79f, 0.79f);

        /// <summary>Built-in icons, in declaration order (without None and Custom).</summary>
        internal static readonly ProjectIcon[] BUILT_IN_ICONS = Enum.GetValues(typeof(ProjectIcon)).Cast<ProjectIcon>()
            .Where(icon => icon != ProjectIcon.None && icon != ProjectIcon.Custom).ToArray();

        /// <summary>Built-in backgrounds, in declaration order (without None and Custom).</summary>
        internal static readonly ProjectBackground[] BUILT_IN_BACKGROUNDS = Enum.GetValues(typeof(ProjectBackground)).Cast<ProjectBackground>()
            .Where(background => background != ProjectBackground.None && background != ProjectBackground.Custom).ToArray();

        // Category prefixes of ProjectIcon / ProjectBackground values, longest first, with the suffix shown after the name
        private static readonly (string Prefix, string Suffix)[] NAME_PREFIXES =
        {
            ("TpeBg", "BG"), ("PfmBg", "BG"), ("Tag", "Tag"), ("Tpe", null), ("Pfm", null), ("Clr", null)
        };

        private static readonly HashSet<string> KEEP_AS_IS = new HashSet<string> {"iOS", "WebGL"};

        private static StyleSheet _styleSheet;

        //---------------------------------------------------------------------
        // Theme
        //---------------------------------------------------------------------

        /// <summary>
        /// Gives the root of a Folder Painter UI its font and shared style sheet (hand cursor on clickable elements).
        /// Both are inherited by the children.
        /// </summary>
        internal static void ApplyTheme(VisualElement root)
        {
            FPFonts.Apply(root);

            if (_styleSheet == null) _styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(STYLE_SHEET_PATH);
            if (_styleSheet != null && !root.styleSheets.Contains(_styleSheet)) root.styleSheets.Add(_styleSheet);
        }

        //---------------------------------------------------------------------
        // Elements
        //---------------------------------------------------------------------

        /// <summary>Gives a popup window root the usual popup background and border.</summary>
        internal static void StylePopupRoot(VisualElement root)
        {
            root.style.backgroundColor = ColorHelper.PopupBackgroundColor;
            SetBorder(root, 1f, ColorHelper.PopupBorderColor);
        }

        internal static ScrollView CreateGridScrollView(out VisualElement grid)
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                style = {flexGrow = 1, flexShrink = 1, minHeight = 0, marginTop = 2, backgroundColor = EditorGUIUtility.isProSkin ? PANEL_CLR_PRO : PANEL_CLR_FREE}
            };
            SetBorder(scroll, 1f, ColorHelper.SeparatorColor);

            grid = new VisualElement {style = {flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, paddingLeft = 2, paddingTop = 2, paddingBottom = 2}};
            scroll.Add(grid);
            return scroll;
        }

        internal static VisualElement CreateGridItem(Texture texture, string displayName, float size, Action onClick, Action onDoubleClick)
        {
            var hoverColor = EditorGUIUtility.isProSkin ? HOVER_CLR_PRO : HOVER_CLR_FREE;
            var item = new VisualElement
            {
                tooltip = displayName,
                style = {width = size, height = size, flexShrink = 0, marginRight = 2, marginBottom = 2, paddingLeft = 2, paddingRight = 2, paddingTop = 2, paddingBottom = 2}
            };
            item.AddToClassList(CLICKABLE_CLASS);
            SetBorder(item, 2f, Color.clear);
            SetRadius(item, 3f);

            item.Add(new Image {image = texture, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore, style = {flexGrow = 1}});

            item.RegisterCallback<PointerEnterEvent>(_ => item.style.backgroundColor = hoverColor);
            item.RegisterCallback<PointerLeaveEvent>(_ => item.style.backgroundColor = Color.clear);
            item.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                if (evt.clickCount >= 2) onDoubleClick(); else onClick();
                evt.StopPropagation();
            });

            return item;
        }

        internal static ToolbarSearchField CreateSearchField(Action<string> onChange)
        {
            var search = new ToolbarSearchField {style = {width = 150, flexShrink = 1}, tooltip = Tr("Filter by name")};
            search.RegisterValueChangedCallback(evt => onChange(evt.newValue ?? string.Empty));
            return search;
        }

        /// <summary>Row with a "None" and a "Custom" button, shown below the texture grids.</summary>
        internal static VisualElement CreateNoneCustomButtons(Action onNone, Action onCustom, float marginTop)
        {
            var buttons = new VisualElement {style = {flexDirection = FlexDirection.Row, flexShrink = 0, marginTop = marginTop}};
            buttons.Add(new Button(onNone) {text = Tr("None"), style = {flexGrow = 1, marginLeft = 0}});
            buttons.Add(new Button(onCustom) {text = Tr("Custom"), style = {flexGrow = 1, marginRight = 0}});
            return buttons;
        }

        //---------------------------------------------------------------------
        // Texts
        //---------------------------------------------------------------------

        /// <summary>
        /// Readable name of an icon or a background: the category prefix of the enum value (Clr, Tag, Tpe, Pfm...)
        /// is removed, and a suffix tells apart icons that would otherwise share a name ("Red (Tag)").
        /// </summary>
        internal static string GetDisplayName(Enum value)
        {
            var name = value.ToString();
            if (name == "None" || name == "Custom") return Tr(name);

            foreach (var (prefix, suffix) in NAME_PREFIXES)
            {
                if (!name.StartsWith(prefix, StringComparison.Ordinal) || name.Length == prefix.Length) continue;

                var displayName = Nicify(name.Substring(prefix.Length));
                return suffix == null ? displayName : $"{displayName} ({Tr(suffix)})";
            }

            // e.g. Transparent15 -> "Transparent 15%"
            var percent = Regex.Match(name, @"^(\D+)(\d+)$");
            return percent.Success ? $"{Nicify(percent.Groups[1].Value)} {percent.Groups[2].Value}%" : Nicify(name);
        }

        private static string Nicify(string name)
        {
            // NicifyVariableName would turn these into "I OS" and "Web GL"
            return KEEP_AS_IS.Contains(name) ? name : ObjectNames.NicifyVariableName(name);
        }

        internal static string GetCategoryName(ProjectIconCategory category)
        {
            return Tr(ObjectNames.NicifyVariableName(category.ToString()));
        }

        /// <summary>Whether a grid item should stay visible for the text typed in a search field.</summary>
        internal static bool MatchesQuery(Enum value, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;

            query = query.Trim();
            return GetDisplayName(value).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                || value.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        //---------------------------------------------------------------------
        // Styles
        //---------------------------------------------------------------------

        internal static void FixLabelWidth<T>(BaseField<T> field, float width = 100f)
        {
            field.labelElement.style.minWidth = width;
            field.labelElement.style.width = width;
            field.style.marginLeft = 0;
            field.style.marginRight = 0;
            field.style.flexShrink = 0;
        }

        internal static void SetBorder(VisualElement element, float width, Color color)
        {
            element.style.borderLeftWidth = element.style.borderRightWidth =
                element.style.borderTopWidth = element.style.borderBottomWidth = width;
            SetBorderColor(element, color);
        }

        internal static void SetBorderColor(VisualElement element, Color color)
        {
            element.style.borderLeftColor = element.style.borderRightColor =
                element.style.borderTopColor = element.style.borderBottomColor = color;
        }

        internal static void SetRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = element.style.borderTopRightRadius =
                element.style.borderBottomLeftRadius = element.style.borderBottomRightRadius = radius;
        }

        internal static void SetVisible(VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        //---------------------------------------------------------------------
        // Icon categories
        //---------------------------------------------------------------------

        /// <summary>Categories map to the numeric ranges used by <see cref="ProjectIcon"/>.</summary>
        internal static bool IsInCategory(ProjectIcon icon, ProjectIconCategory category)
        {
            var id = (int) icon;
            switch (category)
            {
                case ProjectIconCategory.All: return icon != ProjectIcon.None && icon != ProjectIcon.Custom;
                case ProjectIconCategory.Colors: return id >= 10 && id < 500;
                case ProjectIconCategory.Transparent: return id >= 500 && id < 1000;
                case ProjectIconCategory.Tags: return id >= 1000 && id < 2000;
                case ProjectIconCategory.Types: return id >= 2000 && id < 2500;
                case ProjectIconCategory.TypesBg: return id >= 2500 && id < 3000;
                case ProjectIconCategory.Platforms: return id >= 3000 && id < 3500;
                case ProjectIconCategory.PlatformsBg: return id >= 3500 && id < 4000;
                default: throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }
    }
}
