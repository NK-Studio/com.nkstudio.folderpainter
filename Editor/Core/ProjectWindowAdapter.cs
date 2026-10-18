// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
#if UNITY_6000_3_OR_NEWER
using AssetId = UnityEngine.EntityId;
using AssetTreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<UnityEngine.EntityId>;
#else
using AssetId = System.Int32;
using AssetTreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem;
#endif

namespace NKStudio.FolderPainter
{
    /// <summary>
    /// Reflection based access to the internals of the Project Browser.
    /// </summary>
    public static class ProjectWindowAdapter
    {
        private const string EDITOR_WINDOW_TYPE = "UnityEditor.ProjectBrowser";

        private const BindingFlags STATIC_PRIVATE = BindingFlags.Static | BindingFlags.NonPublic;
        private const BindingFlags STATIC_PUBLIC = BindingFlags.Static | BindingFlags.Public;
        private const BindingFlags INSTANCE_PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags INSTANCE_PUBLIC = BindingFlags.Instance | BindingFlags.Public;

        // Project Browser
        private static readonly MethodInfo ALL_PROJECT_BROWSERS_METHOD;
        private static readonly MethodInfo PROJECT_BROWSER_INITIALIZED_METHOD;
        // First Column
        private static readonly FieldInfo PROJECT_VIEW_MODE_FIELD;
        private static readonly FieldInfo PROJECT_ASSET_TREE_FIELD;
        private static readonly FieldInfo PROJECT_FOLDER_TREE_FIELD;
        private static readonly FieldInfo CONTROLLER_DRAG_SELECTION_FIELD;
        private static readonly FieldInfo INTEGER_CACHE_LIST_FIELD;
        private static readonly PropertyInfo CONTROLLER_DATA_PROPERTY;
        private static readonly PropertyInfo CONTROLLER_STATE_PROPERTY;
        private static readonly PropertyInfo CONTROLLER_GUI_CALLBACK_PROPERTY;
        private static readonly MethodInfo CONTROLLER_HAS_FOCUS_METHOD;
        private static readonly PropertyInfo STATE_SELECTED_IDS_PROPERTY;
        private static readonly MethodInfo ASSET_TREE_ROWS_METHOD;
        private static readonly MethodInfo FOLDER_TREE_ROWS_METHOD;
        // Second Column
        private static readonly FieldInfo PROJECT_OBJECT_LIST_FIELD;
        private static readonly FieldInfo PROJECT_LOCAL_ASSETS_FIELD;
        private static readonly PropertyInfo OBJECT_LIST_REPAINT_CALLBACK;
        private static readonly FieldInfo OBJECT_LIST_ICON_EVENT;
        private static readonly PropertyInfo ASSETS_LIST_MODE_PROPERTY;
        private static readonly FieldInfo LIST_FILTERED_HIERARCHY_FIELD;
        private static readonly PropertyInfo FILTERED_HIERARCHY_RESULTS_PROPERTY;
        // Filter Result
        private static readonly FieldInfo FILTER_RESULT_ID_FIELD;
        private static readonly FieldInfo FILTER_RESULT_IS_FOLDER_FIELD;
        private static readonly PropertyInfo FILTER_RESULT_ICON_PROPERTY;

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        static ProjectWindowAdapter()
        {
            var assembly = Assembly.GetAssembly(typeof(EditorWindow));

            // Project Browser

            var projectWindowType = assembly.GetType(EDITOR_WINDOW_TYPE);
            ALL_PROJECT_BROWSERS_METHOD = projectWindowType.GetMethod("GetAllProjectBrowsers", STATIC_PUBLIC);
            PROJECT_BROWSER_INITIALIZED_METHOD = projectWindowType.GetMethod("Initialized", INSTANCE_PUBLIC);

            // First Column

            PROJECT_VIEW_MODE_FIELD = projectWindowType.GetField("m_ViewMode", INSTANCE_PRIVATE);
            PROJECT_ASSET_TREE_FIELD = projectWindowType.GetField("m_AssetTree", INSTANCE_PRIVATE);
            PROJECT_FOLDER_TREE_FIELD = projectWindowType.GetField("m_FolderTree", INSTANCE_PRIVATE);

#if UNITY_6000_3_OR_NEWER
            var treeViewControllerType = assembly.GetType("UnityEditor.IMGUI.Controls.TreeViewController`1").MakeGenericType(typeof(AssetId));
            var integerCacheType = treeViewControllerType.GetNestedType("IntegerCache", INSTANCE_PRIVATE).MakeGenericType(typeof(AssetId));
            var treeViewStateType = typeof(TreeViewState<AssetId>);
#else
            var treeViewControllerType = assembly.GetType("UnityEditor.IMGUI.Controls.TreeViewController");
            var integerCacheType = treeViewControllerType.GetNestedType("IntegerCache", INSTANCE_PRIVATE);
            var treeViewStateType = assembly.GetType("UnityEditor.IMGUI.Controls.TreeViewState");
#endif
            CONTROLLER_DRAG_SELECTION_FIELD = treeViewControllerType.GetField("m_DragSelection", INSTANCE_PRIVATE);
            INTEGER_CACHE_LIST_FIELD = integerCacheType.GetField("m_List", INSTANCE_PRIVATE);
            CONTROLLER_DATA_PROPERTY = treeViewControllerType.GetProperty("data", INSTANCE_PUBLIC);
            CONTROLLER_STATE_PROPERTY = treeViewControllerType.GetProperty("state", INSTANCE_PUBLIC);
            CONTROLLER_GUI_CALLBACK_PROPERTY = treeViewControllerType.GetProperty("onGUIRowCallback", INSTANCE_PUBLIC);
            CONTROLLER_HAS_FOCUS_METHOD = treeViewControllerType.GetMethod("HasFocus", INSTANCE_PUBLIC);
            STATE_SELECTED_IDS_PROPERTY = treeViewStateType.GetProperty("selectedIDs", INSTANCE_PUBLIC);

            // One column layout shows the whole asset tree, two column layout shows only the folder tree on the left
            ASSET_TREE_ROWS_METHOD = assembly.GetType("UnityEditor.AssetsTreeViewDataSource").GetMethod("GetRows", INSTANCE_PUBLIC);
            FOLDER_TREE_ROWS_METHOD = assembly.GetType("UnityEditor.ProjectBrowserColumnOneTreeViewDataSource").GetMethod("GetRows", INSTANCE_PUBLIC);

            // Second Column

            PROJECT_OBJECT_LIST_FIELD = projectWindowType.GetField("m_ListArea", INSTANCE_PRIVATE);

            var objectListType = assembly.GetType("UnityEditor.ObjectListArea");
            PROJECT_LOCAL_ASSETS_FIELD = objectListType.GetField("m_LocalAssets", INSTANCE_PRIVATE);
            OBJECT_LIST_REPAINT_CALLBACK = objectListType.GetProperty("repaintCallback", INSTANCE_PUBLIC);
            OBJECT_LIST_ICON_EVENT = objectListType.GetField("postAssetIconDrawCallback", STATIC_PRIVATE);

            var localGroupType = objectListType.GetNestedType("LocalGroup", INSTANCE_PRIVATE);
            ASSETS_LIST_MODE_PROPERTY = localGroupType.GetProperty("ListMode", INSTANCE_PUBLIC);
            LIST_FILTERED_HIERARCHY_FIELD = localGroupType.GetField("m_FilteredHierarchy", INSTANCE_PRIVATE);

            var filteredHierarchyType = assembly.GetType("UnityEditor.FilteredHierarchy");
            FILTERED_HIERARCHY_RESULTS_PROPERTY = filteredHierarchyType.GetProperty("results", INSTANCE_PUBLIC);

            // Filter Result

            var filterResultType = filteredHierarchyType.GetNestedType("FilterResult");
#if UNITY_6000_3_OR_NEWER
            FILTER_RESULT_ID_FIELD = filterResultType.GetField("entityId", INSTANCE_PUBLIC);
#else
            FILTER_RESULT_ID_FIELD = filterResultType.GetField("instanceID", INSTANCE_PUBLIC);
#endif
            FILTER_RESULT_IS_FOLDER_FIELD = filterResultType.GetField("isFolder", INSTANCE_PUBLIC);
            FILTER_RESULT_ICON_PROPERTY = filterResultType.GetProperty("icon", INSTANCE_PUBLIC);

            // Callbacks

            ProjectRuleset.OnRulesetChange += ApplyDefaultIconsToSecondColumn;
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        [SuppressMessage("ReSharper", "ReturnTypeCanBeEnumerable.Global")]
        public static IReadOnlyList<EditorWindow> GetAllProjectWindows()
        {
            return (IReadOnlyList<EditorWindow>) ALL_PROJECT_BROWSERS_METHOD.Invoke(null, null);
        }

        public static object GetAssetTreeController(EditorWindow window)
        {
            return PROJECT_ASSET_TREE_FIELD.GetValue(window);
        }

        public static object GetFolderTreeController(EditorWindow window)
        {
            return PROJECT_FOLDER_TREE_FIELD.GetValue(window);
        }

        public static object GetTreeViewState(object treeViewController)
        {
            return CONTROLLER_STATE_PROPERTY.GetValue(treeViewController);
        }

        public static bool HasChildren(EditorWindow window, AssetId assetId)
        {
            var treeViewItems = GetFirstColumnItems(window);
            if (treeViewItems == null) return false;

            foreach (var item in treeViewItems)
            {
                if (item.id.Equals(assetId)) return item.hasChildren;
            }

            return false;
        }

        public static bool IsItemSelected(object treeViewController, object state, AssetId assetId)
        {
            // While the user drags a selection rectangle, the pending selection is shown instead of the real one
            var dragSelectionCache = CONTROLLER_DRAG_SELECTION_FIELD.GetValue(treeViewController);
            var dragSelection = dragSelectionCache != null ? (List<AssetId>) INTEGER_CACHE_LIST_FIELD.GetValue(dragSelectionCache) : null;
            if (dragSelection != null && dragSelection.Count > 0) return dragSelection.Contains(assetId);

            var selectedIds = (List<AssetId>) STATE_SELECTED_IDS_PROPERTY.GetValue(state);
            return selectedIds != null && selectedIds.Contains(assetId);
        }

        public static bool HasFocus(object treeViewController)
        {
            return (bool) CONTROLLER_HAS_FOCUS_METHOD.Invoke(treeViewController, null);
        }

        public static ViewMode GetProjectViewMode(EditorWindow window)
        {
            return (ViewMode) PROJECT_VIEW_MODE_FIELD.GetValue(window);
        }

        public static bool ProjectWindowInitialized(EditorWindow window)
        {
            return (bool) PROJECT_BROWSER_INITIALIZED_METHOD.Invoke(window, null);
        }

        public static object GetObjectListArea(EditorWindow window)
        {
            return PROJECT_OBJECT_LIST_FIELD.GetValue(window);
        }

        /// <summary>
        /// Swaps the icons of the folders shown in the second column with the ones defined by the ruleset.
        /// </summary>
        public static void ReplaceIconsInListArea(object objectListArea, ProjectRuleset ruleset)
        {
            if (ruleset == null) return;

            var items = GetListAreaItems(objectListArea, out var inListMode);
            if (items == null) return;

            foreach (var item in items)
            {
                if (!ListItemIsFolder(item)) continue;

                var path = AssetDatabase.GetAssetPath(GetInstanceIdFromListItem(item));
                var rule = ruleset.GetRuleByPath(path, true);
                if (rule == null) continue;

                var iconTex = rule.GetIconTexture(inListMode);
                if (iconTex != null) SetIconForListItem(item, iconTex);
            }
        }

        //---------------------------------------------------------------------
        // Callbacks
        //---------------------------------------------------------------------

        public static void AddOnGUIRowCallback(object treeViewController, Action<AssetId, Rect> action)
        {
            var value = (Action<AssetId, Rect>) CONTROLLER_GUI_CALLBACK_PROPERTY.GetValue(treeViewController);
            CONTROLLER_GUI_CALLBACK_PROPERTY.SetValue(treeViewController, action + value);
        }

        [SuppressMessage("ReSharper", "DelegateSubtraction")]
        public static void RemoveOnGUIRowCallback(object treeViewController, Action<AssetId, Rect> action)
        {
            var value = (Action<AssetId, Rect>) CONTROLLER_GUI_CALLBACK_PROPERTY.GetValue(treeViewController);
            CONTROLLER_GUI_CALLBACK_PROPERTY.SetValue(treeViewController, value - action);
        }

        public static void AddRepaintCallback(object objectListArea, Action repaintCallback)
        {
            var value = (Action) OBJECT_LIST_REPAINT_CALLBACK.GetValue(objectListArea);
            OBJECT_LIST_REPAINT_CALLBACK.SetValue(objectListArea, value + repaintCallback);
        }

        [SuppressMessage("ReSharper", "DelegateSubtraction")]
        public static void RemoveRepaintCallback(object objectListArea, Action repaintCallback)
        {
            var value = (Action) OBJECT_LIST_REPAINT_CALLBACK.GetValue(objectListArea);
            OBJECT_LIST_REPAINT_CALLBACK.SetValue(objectListArea, value - repaintCallback);
        }

        /// <summary>
        /// Subscribes a static method to <c>ObjectListArea.postAssetIconDrawCallback</c>.
        /// The delegate type is internal, so the method is bound by name.
        /// </summary>
        public static void AddPostAssetIconDrawCallback(Type target, string method)
        {
            var callback = Delegate.CreateDelegate(OBJECT_LIST_ICON_EVENT.FieldType, target, method);
            var value = (Delegate) OBJECT_LIST_ICON_EVENT.GetValue(null);
            OBJECT_LIST_ICON_EVENT.SetValue(null, Delegate.Combine(callback, value));
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static IEnumerable<AssetTreeViewItem> GetFirstColumnItems(EditorWindow window)
        {
            var assetTree = PROJECT_ASSET_TREE_FIELD.GetValue(window);
            if (assetTree != null) return GetTreeRows(assetTree, ASSET_TREE_ROWS_METHOD);

            var folderTree = PROJECT_FOLDER_TREE_FIELD.GetValue(window);
            if (folderTree != null) return GetTreeRows(folderTree, FOLDER_TREE_ROWS_METHOD);

            return null;
        }

        private static IEnumerable<AssetTreeViewItem> GetTreeRows(object treeViewController, MethodInfo getRowsMethod)
        {
            var treeViewData = CONTROLLER_DATA_PROPERTY.GetValue(treeViewController, null);
            return treeViewData != null ? (IEnumerable<AssetTreeViewItem>) getRowsMethod.Invoke(treeViewData, null) : null;
        }

        private static IEnumerable<object> GetListAreaItems(object objectListArea, out bool inListMode)
        {
            inListMode = false;
            if (objectListArea == null) return null;

            var localAssets = PROJECT_LOCAL_ASSETS_FIELD.GetValue(objectListArea);
            if (localAssets == null) return null;

            inListMode = (bool) ASSETS_LIST_MODE_PROPERTY.GetValue(localAssets, null);
            var filteredHierarchy = LIST_FILTERED_HIERARCHY_FIELD.GetValue(localAssets);
            if (filteredHierarchy == null) return null;

            return (IEnumerable<object>) FILTERED_HIERARCHY_RESULTS_PROPERTY.GetValue(filteredHierarchy, null);
        }

        /// <summary>
        /// Restores default icons in the second column, so icons of removed rules disappear.
        /// Custom icons are applied again by the repaint callback.
        /// </summary>
        private static void ApplyDefaultIconsToSecondColumn()
        {
            foreach (var window in GetAllProjectWindows())
            {
                var listItems = GetListAreaItems(GetObjectListArea(window), out _);
                if (listItems == null) continue;

                foreach (var item in listItems) SetIconForListItem(item, null);

                window.Repaint();
            }
        }

        private static AssetId GetInstanceIdFromListItem(object listItem)
        {
            return (AssetId) FILTER_RESULT_ID_FIELD.GetValue(listItem);
        }

        private static void SetIconForListItem(object listItem, Texture2D icon)
        {
            FILTER_RESULT_ICON_PROPERTY.SetValue(listItem, icon, null);
        }

        private static bool ListItemIsFolder(object listItem)
        {
            return (bool) FILTER_RESULT_IS_FOLDER_FIELD.GetValue(listItem);
        }

        //---------------------------------------------------------------------
        // Nested
        //---------------------------------------------------------------------

        public enum ViewMode
        {
            OneColumn,
            TwoColumns,
        }
    }
}
