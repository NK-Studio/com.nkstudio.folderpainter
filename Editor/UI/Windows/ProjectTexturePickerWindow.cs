using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static NKStudio.FolderPainter.FPElements;
using static NKStudio.FolderPainter.FPLocalization;

namespace NKStudio.FolderPainter
{
    /// <summary>
    /// Dropdown picker used by the ruleset inspector to choose an icon or a background for a rule.
    /// Writes the result directly into the serialized rule.
    /// </summary>
    public class ProjectTexturePickerWindow : EditorWindow
    {
        private const float WINDOW_WIDTH = 372f;
        private const float WINDOW_HEIGHT = 320f;
        private const float GRID_ITEM_SIZE = 54f;

        private Mode _mode;
        private SerializedProperty _rule;
        private ProjectIconCategory _category = ProjectIconCategory.All;
        private string _query = string.Empty;

        // Grid items by the numeric value of their ProjectIcon / ProjectBackground
        private readonly Dictionary<int, VisualElement> _items = new Dictionary<int, VisualElement>();

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public static void ShowIconPicker(Rect activatorScreenRect, SerializedProperty rule)
        {
            Open(activatorScreenRect, rule, Mode.Icon);
        }

        public static void ShowBackgroundPicker(Rect activatorScreenRect, SerializedProperty rule)
        {
            Open(activatorScreenRect, rule, Mode.Background);
        }

        //---------------------------------------------------------------------
        // Messages
        //---------------------------------------------------------------------

        private void CreateGUI()
        {
            // Window survived domain reload but the serialized property is gone
            if (_rule == null)
            {
                EditorApplication.delayCall += Close;
                return;
            }

            var root = rootVisualElement;
            StylePopupRoot(root);
            FPElements.ApplyTheme(root);
            root.style.paddingLeft = root.style.paddingRight = root.style.paddingBottom = 6;
            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Escape) return;
                evt.StopPropagation();
                Close();
            }, TrickleDown.TrickleDown);

            // Toolbar

            var toolbar = new Toolbar {style = {flexShrink = 0, marginLeft = -6, marginRight = -6, marginBottom = 4}};
            toolbar.Add(_mode == Mode.Icon
                ? CreateCategoryMenu()
                : new Label(Tr("Background")) {style = {unityTextAlign = TextAnchor.MiddleLeft, paddingLeft = 6}});
            toolbar.Add(new ToolbarSpacer {flex = true});

            var search = CreateSearchField(query =>
            {
                _query = query;
                ApplyFilters();
            });
            toolbar.Add(search);
            root.Add(toolbar);

            // Grid

            var scroll = CreateGridScrollView(out var grid);
            var current = GetTypeProperty().intValue;

            if (_mode == Mode.Icon)
            {
                foreach (var icon in BUILT_IN_ICONS)
                    AddItem(grid, (int) icon, ProjectIconsStorage.GetIcon(icon, false), GetDisplayName(icon), current);
            }
            else
            {
                foreach (var background in BUILT_IN_BACKGROUNDS)
                    AddItem(grid, (int) background, ProjectBackgroundsStorage.GetBackground(background), GetDisplayName(background), current);
            }
            root.Add(scroll);

            // Buttons (None and Custom share the same values in both enums)

            root.Add(CreateNoneCustomButtons(() => Pick((int) ProjectIcon.None), () => Pick((int) ProjectIcon.Custom), 4));

            ApplyFilters();
            root.schedule.Execute(() => search.Q<TextField>()?.Focus());
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static void Open(Rect activatorScreenRect, SerializedProperty rule, Mode mode)
        {
            var window = CreateInstance<ProjectTexturePickerWindow>();
            window._mode = mode;
            window._rule = rule.Copy();
            window.ShowAsDropDown(activatorScreenRect, new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT));
        }

        private ToolbarMenu CreateCategoryMenu()
        {
            var menu = new ToolbarMenu {text = GetCategoryName(_category)};
            foreach (ProjectIconCategory category in Enum.GetValues(typeof(ProjectIconCategory)))
            {
                menu.menu.AppendAction(GetCategoryName(category), _ =>
                {
                    _category = category;
                    menu.text = GetCategoryName(category);
                    ApplyFilters();
                }, _ => _category == category ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
            return menu;
        }

        private void AddItem(VisualElement grid, int id, Texture texture, string displayName, int current)
        {
            var item = CreateGridItem(texture, displayName, GRID_ITEM_SIZE, () => Pick(id), () => Pick(id));
            if (id == current) SetBorderColor(item, HIGHLIGHT_CLR);
            _items[id] = item;
            grid.Add(item);
        }

        private void ApplyFilters()
        {
            foreach (var pair in _items)
            {
                bool visible;
                if (_mode == Mode.Icon)
                {
                    var icon = (ProjectIcon) pair.Key;
                    visible = IsInCategory(icon, _category) && MatchesQuery(icon, _query);
                }
                else
                {
                    visible = MatchesQuery((ProjectBackground) pair.Key, _query);
                }

                SetVisible(pair.Value, visible);
            }
        }

        private SerializedProperty GetTypeProperty()
        {
            return _rule.FindPropertyRelative(_mode == Mode.Icon ? nameof(ProjectRule.IconType) : nameof(ProjectRule.BackgroundType));
        }

        /// <summary>0 = None, 1 = Custom, anything else is a built-in texture id.</summary>
        private void Pick(int id)
        {
            GetTypeProperty().intValue = id;

            if (_mode == Mode.Icon)
            {
                _rule.FindPropertyRelative(nameof(ProjectRule.SmallIcon)).objectReferenceValue = null;
                _rule.FindPropertyRelative(nameof(ProjectRule.LargeIcon)).objectReferenceValue = null;
                if (id == (int) ProjectIcon.None) _rule.FindPropertyRelative(nameof(ProjectRule.IsIconRecursive)).boolValue = false;
            }
            else
            {
                _rule.FindPropertyRelative(nameof(ProjectRule.BackgroundTexture)).objectReferenceValue = null;
                if (id == (int) ProjectBackground.None) _rule.FindPropertyRelative(nameof(ProjectRule.IsBackgroundRecursive)).boolValue = false;
            }

            _rule.serializedObject.ApplyModifiedProperties();
            ProjectRuleset.NotifyRulesetChanged();
            Close();
        }

        //---------------------------------------------------------------------
        // Nested
        //---------------------------------------------------------------------

        private enum Mode
        {
            Icon,
            Background
        }
    }
}
