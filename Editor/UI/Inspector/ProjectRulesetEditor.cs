// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static NKStudio.FolderPainter.ProjectRule.KeyType;
using static NKStudio.FolderPainter.FPLocalization;

namespace NKStudio.FolderPainter
{
    [CustomEditor(typeof (ProjectRuleset))]
    public class ProjectRulesetEditor : Editor
    {
        public static readonly HashSet<ProjectRulesetEditor> EDITORS = new HashSet<ProjectRulesetEditor>();

        private const string SEARCH_RESULTS_TITLE = "Search Results";

        private SerializedProperty _rulesProperty;

        private SearchTab _tab = SearchTab.Folder;
        private DefaultAsset _asset;
        private string _query = string.Empty;
        private Filter _filter = Filter.All;
        private bool _matchCase;
        private bool _useRegex;

        // Indices of the rules shown in the list
        private readonly List<int> _visibleIndices = new List<int>();

        private VisualElement _root;
        private ToolbarToggle _folderTabToggle;
        private ToolbarToggle _keyTabToggle;
        private VisualElement _folderPanel;
        private VisualElement _keyPanel;
        private ObjectField _folderField;
        private HelpBox _warningBox;
        private Label _resultsLabel;
        private ListView _listView;

        //---------------------------------------------------------------------
        // Messages
        //---------------------------------------------------------------------

        protected void OnEnable()
        {
            EDITORS.Add(this);
            _rulesProperty = serializedObject.FindProperty(nameof(ProjectRuleset.Rules));

            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;
            OnLanguageChanged -= Rebuild;
            OnLanguageChanged += Rebuild;
        }

        protected void OnDisable()
        {
            EDITORS.Remove(this);
            Undo.undoRedoPerformed -= OnUndoRedo;
            OnLanguageChanged -= Rebuild;
        }

        public override VisualElement CreateInspectorGUI()
        {
            _root = new VisualElement();
            Build();
            return _root;
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        /// <summary>
        /// Shows only the rules that affect the given folder (or all rules when the folder is null).
        /// </summary>
        public void ShowRulesForFolder(DefaultAsset asset)
        {
            _tab = SearchTab.Folder;
            _asset = asset;

            if (_root == null) return;
            SelectTab(SearchTab.Folder);
            _folderField.SetValueWithoutNotify(asset);
            RefreshList();
        }

        //---------------------------------------------------------------------
        // Layout
        //---------------------------------------------------------------------

        private void Build()
        {
            _root.Clear();
            _root.style.marginTop = 6;
            FPElements.ApplyTheme(_root);

            // Search tabs

            var toolbar = new Toolbar {style = {marginLeft = 0, marginRight = 0}};
            _folderTabToggle = new ToolbarToggle {text = Tr("Filter by folder"), style = {flexGrow = 1}};
            _folderTabToggle.RegisterValueChangedCallback(_ => SelectTab(SearchTab.Folder));
            _keyTabToggle = new ToolbarToggle {text = Tr("Filter by key"), style = {flexGrow = 1}};
            _keyTabToggle.RegisterValueChangedCallback(_ => SelectTab(SearchTab.Key));
            toolbar.Add(_folderTabToggle);
            toolbar.Add(_keyTabToggle);
            _root.Add(toolbar);

            var searchBox = new VisualElement {style = {paddingLeft = 6, paddingRight = 6, paddingTop = 6, paddingBottom = 6, marginBottom = 4}};
            searchBox.style.backgroundColor = EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, 0.15f) : new Color(0f, 0f, 0f, 0.06f);

            // By folder
            _folderPanel = new VisualElement();
            _folderField = new ObjectField {objectType = typeof(DefaultAsset), allowSceneObjects = false, value = _asset};
            _folderField.RegisterValueChangedCallback(evt =>
            {
                _asset = evt.newValue as DefaultAsset;
                RefreshList();
            });
            _folderPanel.Add(_folderField);
            searchBox.Add(_folderPanel);

            // By key
            _keyPanel = new VisualElement();

            var queryRow = new VisualElement {style = {flexDirection = FlexDirection.Row, alignItems = Align.Center}};
            var filterField = new DropdownField(TrEnumNames(typeof(Filter)).ToList(), (int) _filter) {style = {width = 70, marginLeft = 0}};
            filterField.RegisterValueChangedCallback(_ =>
            {
                _filter = (Filter) filterField.index;
                RefreshList();
            });
            var queryField = new ToolbarSearchField {value = _query, style = {flexGrow = 1, flexShrink = 1, width = StyleKeyword.Auto, marginRight = 0}};
            queryField.RegisterValueChangedCallback(evt =>
            {
                _query = evt.newValue ?? string.Empty;
                RefreshList();
            });
            queryRow.Add(filterField);
            queryRow.Add(queryField);
            _keyPanel.Add(queryRow);

            var optionsRow = new VisualElement {style = {flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd, marginTop = 2}};
            var matchCaseToggle = new Toggle(Tr("Match case")) {value = _matchCase};
            matchCaseToggle.RegisterValueChangedCallback(evt =>
            {
                _matchCase = evt.newValue;
                RefreshList();
            });
            var regexToggle = new Toggle(Tr("Regex")) {value = _useRegex, style = {marginLeft = 12}};
            regexToggle.RegisterValueChangedCallback(evt =>
            {
                _useRegex = evt.newValue;
                RefreshList();
            });
            optionsRow.Add(CompactToggle(matchCaseToggle));
            optionsRow.Add(CompactToggle(regexToggle));
            _keyPanel.Add(optionsRow);
            searchBox.Add(_keyPanel);

            _warningBox = new HelpBox(string.Empty, HelpBoxMessageType.Warning) {style = {marginTop = 4, marginLeft = 0, marginRight = 0}};
            searchBox.Add(_warningBox);

            _root.Add(searchBox);

            // Rules

            _resultsLabel = new Label(Tr(SEARCH_RESULTS_TITLE)) {style = {unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2, marginLeft = 2}};
            _root.Add(_resultsLabel);

            _listView = new ListView
            {
                itemsSource = _visibleIndices,
                makeItem = () => new PropertyField(),
                bindItem = BindRule,
                unbindItem = (element, _) => ((PropertyField) element).Unbind(),
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                showBorder = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                showAddRemoveFooter = true,
                selectionType = SelectionType.Multiple,
                style = {flexGrow = 1}
            };
            _listView.itemIndexChanged += OnRuleMoved;
            _listView.overridingAddButtonBehavior = (_, _) => AddRule();
            _listView.onRemove = _ => RemoveSelectedRules();
            _root.Add(_listView);

            // Edits made through the bound fields must reach the project window
            _root.TrackSerializedObjectValue(serializedObject, _ => ProjectRuleset.NotifyRulesetChanged());

            SelectTab(_tab);
        }

        private static VisualElement CompactToggle(Toggle toggle)
        {
            toggle.labelElement.style.minWidth = StyleKeyword.Auto;
            toggle.labelElement.style.paddingRight = 4;
            toggle.style.flexDirection = FlexDirection.RowReverse;
            return toggle;
        }

        //---------------------------------------------------------------------
        // List
        //---------------------------------------------------------------------

        private void BindRule(VisualElement element, int index)
        {
            var field = (PropertyField) element;
            var ruleIndex = _visibleIndices[index];
            if (ruleIndex >= _rulesProperty.arraySize) return;

            field.BindProperty(_rulesProperty.GetArrayElementAtIndex(ruleIndex));
        }

        private void AddRule()
        {
            serializedObject.Update();
            _rulesProperty.arraySize++;
            // Unity copies the last element into the new slot: start from a clean rule instead
            var rule = _rulesProperty.GetArrayElementAtIndex(_rulesProperty.arraySize - 1);
            rule.boxedValue = new ProjectRule(Name, string.Empty);
            serializedObject.ApplyModifiedProperties();

            ProjectRuleset.NotifyRulesetChanged();
            RefreshList();
            _listView.ScrollToItem(-1);
        }

        private void RemoveSelectedRules()
        {
            var selected = _listView.selectedIndices.Select(i => _visibleIndices[i]).OrderByDescending(i => i).ToList();
            if (selected.Count == 0 && _visibleIndices.Count > 0) selected.Add(_visibleIndices[_visibleIndices.Count - 1]);
            if (selected.Count == 0) return;

            serializedObject.Update();
            foreach (var ruleIndex in selected) _rulesProperty.DeleteArrayElementAtIndex(ruleIndex);
            serializedObject.ApplyModifiedProperties();

            _listView.ClearSelection();
            ProjectRuleset.NotifyRulesetChanged();
            RefreshList();
        }

        private void OnRuleMoved(int from, int to)
        {
            // Reordering is only enabled while the full list is shown, so list index == rule index
            serializedObject.Update();
            _rulesProperty.MoveArrayElement(from, to);
            serializedObject.ApplyModifiedProperties();

            ProjectRuleset.NotifyRulesetChanged();
            RefreshList();
        }

        private void RefreshList()
        {
            if (_listView == null) return;

            serializedObject.Update();
            FPElements.SetVisible(_warningBox, false);

            var isFiltering = IsFiltering();
            var regex = isFiltering && _tab == SearchTab.Key && _useRegex ? MakeRegexFromQuery() : null;

            _visibleIndices.Clear();
            for (var i = 0; i < _rulesProperty.arraySize; i++)
            {
                if (!isFiltering || Matches(_rulesProperty.GetArrayElementAtIndex(i), regex)) _visibleIndices.Add(i);
            }

            // Moving or adding rules inside a filtered view would be ambiguous
            _listView.reorderable = !isFiltering;
            _listView.reorderMode = ListViewReorderMode.Animated;
            _listView.showAddRemoveFooter = !isFiltering;
            _resultsLabel.style.display = isFiltering ? DisplayStyle.Flex : DisplayStyle.None;

            _listView.RefreshItems();
        }

        private void SelectTab(SearchTab tab)
        {
            _tab = tab;
            _folderTabToggle.SetValueWithoutNotify(tab == SearchTab.Folder);
            _keyTabToggle.SetValueWithoutNotify(tab == SearchTab.Key);
            _folderPanel.style.display = tab == SearchTab.Folder ? DisplayStyle.Flex : DisplayStyle.None;
            _keyPanel.style.display = tab == SearchTab.Key ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshList();
        }

        private void Rebuild()
        {
            if (_root == null) return;
            Build();
            _root.Bind(serializedObject);
        }

        // The ruleset itself listens to undo/redo, only the list has to follow here
        private void OnUndoRedo()
        {
            RefreshList();
        }

        //---------------------------------------------------------------------
        // Filtering
        //---------------------------------------------------------------------

        private bool IsFiltering()
        {
            return _tab == SearchTab.Folder
                ? _asset != null
                : !string.IsNullOrEmpty(_query) || _filter != Filter.All;
        }

        private bool Matches(SerializedProperty rule, Regex regex)
        {
            var key = rule.FindPropertyRelative(nameof(ProjectRule.Key)).stringValue ?? string.Empty;
            var type = (ProjectRule.KeyType) rule.FindPropertyRelative(nameof(ProjectRule.Type)).enumValueIndex;

            if (_tab == SearchTab.Folder)
            {
                var isRecursive = rule.FindPropertyRelative(nameof(ProjectRule.IsIconRecursive)).boolValue
                    || rule.FindPropertyRelative(nameof(ProjectRule.IsBackgroundRecursive)).boolValue;

                return ProjectRule.Matches(type, key, isRecursive, AssetDatabase.GetAssetPath(_asset));
            }

            if (_filter == Filter.Name && type != Name) return false;
            if (_filter == Filter.Path && type != Path) return false;

            if (_useRegex) return regex.IsMatch(key);

            var comparison = _matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            return key.IndexOf(_query, comparison) >= 0;
        }

        private Regex MakeRegexFromQuery()
        {
            var options = _matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;

            try
            {
                return new Regex(_query, options);
            }
            catch (ArgumentException ex)
            {
                _warningBox.text = ex.Message;
                _warningBox.style.display = DisplayStyle.Flex;
                return new Regex("(?!.*)"); // matches nothing
            }
        }

        //---------------------------------------------------------------------
        // Nested
        //---------------------------------------------------------------------

        private enum SearchTab
        {
            Folder, Key
        }

        private enum Filter
        {
            All, Name, Path
        }
    }
}
