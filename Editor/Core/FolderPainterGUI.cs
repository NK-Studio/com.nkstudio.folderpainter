// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if !UNITY_6000_0_OR_NEWER
#error Folder Painter requires Unity 6.0 or newer.
#endif
#if UNITY_6000_3_OR_NEWER
using AssetId = UnityEngine.EntityId;
#else
using AssetId = System.Int32;
#endif
using static NKStudio.FolderPainter.ProjectWindowAdapter.ViewMode;

namespace NKStudio.FolderPainter
{
    /// <summary>
    /// Hooks into every Project window and draws the icons and backgrounds defined by the active ruleset.
    /// </summary>
    [InitializeOnLoad]
    internal static class FolderPainterGUI
    {
        private const double WINDOWS_UPDATE_DELAY = 0.2;
        private const float ROW_HEIGHT = 16f;
        private const float FOLDOUT_LEVELS_TEXTURE_WIDTH = 128f;
        private const float ROOT_ITEM_MAX_X = 20f;
        private const float LIST_MODE_BACKGROUND_WIDTH = 200f;

        private static readonly Color ROW_SHADING_COLOR = new Color(0f, 0f, 0f, 0.03f);

        // Callbacks registered in the project windows, kept to unsubscribe them when the windows change
        private static readonly Dictionary<object, Action<AssetId, Rect>> ON_GUI_ROW_CALLBACKS = new Dictionary<object, Action<AssetId, Rect>>();
        private static readonly Dictionary<object, Action> REPAINT_CALLBACKS = new Dictionary<object, Action>();
        private static readonly HashSet<int> WINDOW_HASH_SET = new HashSet<int>();

        // x: windows in one column layout, y: windows in two columns layout
        private static Vector2Int _projectWindowsCount = Vector2Int.zero;

        private static bool _multiSelection;
        private static double _nextWindowsUpdate;

        //---------------------------------------------------------------------
        // Ctors
        //---------------------------------------------------------------------

        static FolderPainterGUI()
        {
            EditorApplication.projectWindowItemOnGUI += DrawRowShading;
            EditorApplication.projectWindowItemOnGUI += DrawEditIcon;
            EditorApplication.update += Update;

            ProjectWindowAdapter.AddPostAssetIconDrawCallback(typeof(FolderPainterGUI), nameof(DrawIconInObjectList));
        }

        //---------------------------------------------------------------------
        // Delegates
        //---------------------------------------------------------------------

        private static void DrawRowShading(string guid, Rect rect)
        {
            if (!ProjectPreferences.DrawRowShading || !IsIconSmall(rect)) return;

            var isOdd = Mathf.FloorToInt(((rect.y - 4) / ROW_HEIGHT) % 2) != 0;
            if (isOdd) return;

            var drawArea = new Rect(rect);
            drawArea.width += rect.x + ROW_HEIGHT;
            drawArea.height += 1f;
            drawArea.x = 0f;

            // Background
            EditorGUI.DrawRect(drawArea, ROW_SHADING_COLOR);
            // Top line
            drawArea.height = 1f;
            EditorGUI.DrawRect(drawArea, ROW_SHADING_COLOR);
            // Bottom line
            drawArea.y += ROW_HEIGHT;
            EditorGUI.DrawRect(drawArea, ROW_SHADING_COLOR);
        }

        private static void DrawEditIcon(string guid, Rect rect)
        {
            if (!ProjectPreferences.IsEditModifierPressed(Event.current))
            {
                _multiSelection = false;
                return;
            }

            var isSmall = IsIconSmall(rect);
            var iconRect = GetIconRect(rect, isSmall);
            var isMouseOver = rect.Contains(Event.current.mousePosition);
            var isSelected = Selection.assetGUIDs.Contains(guid);

            // Hovering any selected folder shows the edit icon on the whole selection
            _multiSelection = isSelected ? isMouseOver || _multiSelection : !isMouseOver && _multiSelection;

            if (!isMouseOver && (!isSelected || !_multiSelection)) return;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!AssetDatabase.IsValidFolder(path)) return;

            GUI.DrawTexture(iconRect, ProjectEditorUtility.GetEditFolderIcon(isSmall));

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                ShowPopupWindow(iconRect, path);
            }

            EditorApplication.RepaintProjectWindow();
        }

        private static void Update()
        {
            if (_nextWindowsUpdate > EditorApplication.timeSinceStartup) return;
            _nextWindowsUpdate = EditorApplication.timeSinceStartup + WINDOWS_UPDATE_DELAY;

            var projectWindows = ProjectWindowAdapter.GetAllProjectWindows();
            if (!ProjectWindowsChanged(projectWindows)) return;

            RemoveOldCallbacks();
            AddNewCallbacks(projectWindows);
        }

        /// <summary>Called by the second column (object list) after each asset icon is drawn.</summary>
        private static void DrawIconInObjectList(Rect iconRect, string guid, bool isListMode)
        {
            var rule = GetRuleForFolder(AssetDatabase.GUIDToAssetPath(guid));
            if (rule == null) return;

            var backgroundTex = rule.GetBackgroundTexture();
            if (backgroundTex == null) return;

            var backgroundRect = isListMode
                ? new Rect(iconRect) {x = iconRect.x + iconRect.width + 2f, width = LIST_MODE_BACKGROUND_WIDTH}
                : new Rect(iconRect) {y = iconRect.y + iconRect.height, height = ROW_HEIGHT};

            GUI.DrawTexture(backgroundRect, backgroundTex);
        }

        //---------------------------------------------------------------------
        // GUI
        //---------------------------------------------------------------------

        private static void DrawFoldouts(Rect rect, AssetId id, EditorWindow window)
        {
            if (!ProjectPreferences.ShowProjectTree) return;

            var fx = Mathf.Max(0, rect.x - FOLDOUT_LEVELS_TEXTURE_WIDTH - ROW_HEIGHT);
            var fw = Mathf.Min(FOLDOUT_LEVELS_TEXTURE_WIDTH, rect.x - ROW_HEIGHT);
            var foldoutRect = new Rect(rect) {width = fw, x = fx};

            var tw = foldoutRect.width / FOLDOUT_LEVELS_TEXTURE_WIDTH;
            var texCoords = new Rect(1 - tw, 0, tw, 1f);

            GUI.DrawTextureWithTexCoords(foldoutRect, ProjectEditorUtility.GetFoldoutLevelsIcon(), texCoords);

            // Items with children already have a foldout arrow, the others get a branch instead
            if (IsRootItem(rect) || ProjectWindowAdapter.HasChildren(window, id)) return;

            foldoutRect.width = ROW_HEIGHT;
            foldoutRect.x = rect.x - ROW_HEIGHT;
            GUI.DrawTexture(foldoutRect, ProjectEditorUtility.GetFoldoutIcon());
        }

        private static void DrawIconInFirstColumn(object controller, object state, Rect rect, AssetId assetId)
        {
            var rule = GetRuleForFolder(AssetDatabase.GetAssetPath(assetId));
            if (rule == null) return;

            var selected = ProjectWindowAdapter.IsItemSelected(controller, state, assetId);

            // Background is hidden on selected rows, so the selection highlight stays visible
            if (!selected)
            {
                var backgroundTex = rule.GetBackgroundTexture();
                if (backgroundTex != null)
                {
                    var backgroundRect = new Rect(rect);
                    backgroundRect.x += ROW_HEIGHT + 1f;
                    backgroundRect.width -= ROW_HEIGHT + 1f;
                    GUI.DrawTexture(backgroundRect, backgroundTex);
                }
            }

            DrawCustomIcon(controller, rule, selected, rect);
        }

        private static void DrawCustomIcon(object controller, ProjectRule rule, bool selected, Rect rect)
        {
            var iconTex = rule.GetIconTexture(true);
            if (iconTex == null) return;

            rect.width = rect.height;

            // Cover the default folder icon first
            var backgroundColor = selected
                ? ColorHelper.GetSelectionColor(ProjectWindowAdapter.HasFocus(controller))
                : ColorHelper.GetBackgroundColor();

            EditorGUI.DrawRect(rect, backgroundColor);
            GUI.DrawTexture(rect, iconTex);
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static ProjectRule GetRuleForFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path)) return null;

            var ruleset = ProjectRuleset.Instance;
            return ruleset != null ? ruleset.GetRuleByPath(path, true) : null;
        }

        private static bool ProjectWindowsChanged(IReadOnlyList<EditorWindow> projectWindows)
        {
            var actualWindowsCount = Vector2Int.zero;

            foreach (var window in projectWindows)
            {
                if (!ProjectWindowAdapter.ProjectWindowInitialized(window)) continue;
                if (!WINDOW_HASH_SET.Contains(window.GetHashCode())) return true;

                if (ProjectWindowAdapter.GetProjectViewMode(window) == OneColumn)
                    actualWindowsCount.x++;
                else
                    actualWindowsCount.y++;
            }

            return _projectWindowsCount != actualWindowsCount;
        }

        private static void RemoveOldCallbacks()
        {
            foreach (var callback in ON_GUI_ROW_CALLBACKS)
            {
                ProjectWindowAdapter.RemoveOnGUIRowCallback(callback.Key, callback.Value);
            }

            foreach (var callback in REPAINT_CALLBACKS)
            {
                ProjectWindowAdapter.RemoveRepaintCallback(callback.Key, callback.Value);
            }

            ON_GUI_ROW_CALLBACKS.Clear();
            REPAINT_CALLBACKS.Clear();
            WINDOW_HASH_SET.Clear();
            _projectWindowsCount = Vector2Int.zero;
        }

        private static void AddNewCallbacks(IReadOnlyList<EditorWindow> projectWindows)
        {
            foreach (var window in projectWindows)
            {
                if (!ProjectWindowAdapter.ProjectWindowInitialized(window)) continue;

                WINDOW_HASH_SET.Add(window.GetHashCode());

                object controller;
                if (ProjectWindowAdapter.GetProjectViewMode(window) == OneColumn)
                {
                    controller = ProjectWindowAdapter.GetAssetTreeController(window);
                    _projectWindowsCount.x++;
                }
                else
                {
                    controller = ProjectWindowAdapter.GetFolderTreeController(window);
                    _projectWindowsCount.y++;
                }

                // First Column

                if (controller != null)
                {
                    var state = ProjectWindowAdapter.GetTreeViewState(controller);

                    void OnGUIRowCallback(AssetId id, Rect rect)
                    {
                        DrawFoldouts(rect, id, window);
                        DrawIconInFirstColumn(controller, state, rect, id);
                    }

                    ProjectWindowAdapter.AddOnGUIRowCallback(controller, OnGUIRowCallback);
                    ON_GUI_ROW_CALLBACKS[controller] = OnGUIRowCallback;
                }

                // Second Column

                var objectListArea = ProjectWindowAdapter.GetObjectListArea(window);
                if (objectListArea == null) continue;

                void ListAreaRepaintCallback()
                {
                    ProjectWindowAdapter.ReplaceIconsInListArea(objectListArea, ProjectRuleset.Instance);
                }

                // Call repaint callback manually first time to make sure
                // icons in second column are applied after recompilation
                ListAreaRepaintCallback();

                ProjectWindowAdapter.AddRepaintCallback(objectListArea, ListAreaRepaintCallback);
                REPAINT_CALLBACKS[objectListArea] = ListAreaRepaintCallback;
            }
        }

        private static void ShowPopupWindow(Rect rect, string path)
        {
            var position = GUIUtility.GUIToScreenPoint(rect.position + new Vector2(0, rect.height + 2));

            var paths = _multiSelection
                ? Selection.assetGUIDs.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).Where(AssetDatabase.IsValidFolder).ToList()
                : new List<string>();

            // The clicked folder is always part of the edited folders
            var index = paths.IndexOf(path);
            if (index < 0)
            {
                paths.Add(path);
                index = paths.Count - 1;
            }

            ProjectPopupWindow.ShowWithParams(position, paths, index);
        }

        private static bool IsIconSmall(Rect rect)
        {
            return rect.width > rect.height;
        }

        private static Rect GetIconRect(Rect rect, bool isSmall)
        {
            if (isSmall)
                rect.width = rect.height;
            else
                rect.height = rect.width;

            return rect;
        }

        private static bool IsRootItem(Rect rect)
        {
            return rect.x <= ROOT_ITEM_MAX_X;
        }
    }
}
