using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using KeyType = NKStudio.FolderPainter.ProjectRule.KeyType;
using static NKStudio.FolderPainter.FPLocalization;
using static NKStudio.FolderPainter.FPElements;

namespace NKStudio.FolderPainter
{
    /// <summary>
    /// Folder configuration popup, opened by clicking a folder icon while holding the edit modifier.
    /// Icons and backgrounds are picked inside the same window instead of separate sub-popups.
    /// </summary>
    public class ProjectPopupWindow : EditorWindow
    {
        private const float WINDOW_WIDTH = 372f;
        private const float WINDOW_HEIGHT = 480f;
        private const float CLOSE_BUTTON_WIDTH = 22f;
        private const float GRID_ITEM_SIZE = 54f;
        private const float RECENT_ITEM_SIZE = 24f;
        private const float PREVIEW_SIZE_LARGE = 64f;
        private const float PREVIEW_SIZE_SMALL = 16f;
        private const int RECENT_MAX = 10;

        private const string RECENT_ICONS_PKEY = "NKStudio.FolderPainter.RecentIcons";
        private const string RECENT_BACKGROUNDS_PKEY = "NKStudio.FolderPainter.RecentBackgrounds";
        private const string LAST_TAB_PKEY = "NKStudio.FolderPainter.PopupTab";
        private const string LAST_CATEGORY_PKEY = "NKStudio.FolderPainter.PopupCategory";
        private const string CLIPBOARD_SKEY = "NKStudio.FolderPainter.Clipboard";

        private static readonly Color HEADER_CLR_PRO = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color HEADER_CLR_FREE = new Color(0.76f, 0.76f, 0.76f);
        // The close icon is white: it is tinted to the editor text color
        private static readonly Color CLOSE_ICON_CLR_PRO = new Color(0.77f, 0.77f, 0.77f);
        private static readonly Color CLOSE_ICON_CLR_FREE = new Color(0.25f, 0.25f, 0.25f);

        private List<string> _paths;
        private ProjectRuleset _ruleset;
        private ProjectRule[] _existingRules;
        private ProjectRule _currentRule;

        private Tab _tab;
        private ProjectIconCategory _category;
        private string _query = string.Empty;

        // Preview
        private Image _previewLarge;
        private Image _previewSmall;
        // Settings
        private TextField _keyField;
        private IntegerField _priorityField;
        private Toggle _iconRecursiveToggle;
        private Toggle _backgroundRecursiveToggle;
        // Tabs
        private ToolbarToggle _iconTabToggle;
        private ToolbarToggle _backgroundTabToggle;
        private VisualElement _iconPanel;
        private VisualElement _backgroundPanel;
        private Label _iconNameLabel;
        private Label _backgroundNameLabel;
        private VisualElement _recentIconsRow;
        private VisualElement _recentBackgroundsRow;
        private VisualElement _customIconFields;
        private VisualElement _customBackgroundFields;
        private ObjectField _smallIconField;
        private ObjectField _largeIconField;
        private ObjectField _backgroundField;
        private Label _emptyIconsLabel;
        private Button _pasteButton;

        private readonly Dictionary<ProjectIcon, VisualElement> _iconItems = new Dictionary<ProjectIcon, VisualElement>();
        private readonly Dictionary<ProjectBackground, VisualElement> _backgroundItems = new Dictionary<ProjectBackground, VisualElement>();

        private Vector2 _dragOffset;
        private IMGUIContainer _headerDragArea;

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public static void ShowWithParams(Vector2 inPosition, List<string> paths, int pathIndex)
        {
            var ruleset = ProjectRuleset.Instance;
            if (ruleset == null || paths == null || paths.Count == 0) return;

            foreach (var opened in Resources.FindObjectsOfTypeAll<ProjectPopupWindow>()) opened.Close();

            var window = CreateInstance<ProjectPopupWindow>();
            window.Init(ruleset, paths, Mathf.Clamp(pathIndex, 0, paths.Count - 1));

            var rect = new Rect(inPosition, new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT));
            window.minSize = window.maxSize = rect.size;
            window.position = rect;
            window.ShowPopup();
            window.Focus();
        }

        //---------------------------------------------------------------------
        // Messages
        //---------------------------------------------------------------------

        private void OnEnable()
        {
            OnLanguageChanged += Rebuild;
        }

        private void OnDisable()
        {
            OnLanguageChanged -= Rebuild;
        }

        private void CreateGUI()
        {
            // Window survived domain reload but its non-serialized state is gone
            if (_currentRule == null)
            {
                EditorApplication.delayCall += Close;
                return;
            }

            var root = rootVisualElement;
            StylePopupRoot(root);
            FPElements.ApplyTheme(root);
            root.focusable = true;
            root.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            root.Add(CreateHeader());
            root.Add(CreateBody());
            root.Add(CreateTabBar());

            var panels = new VisualElement {style = {flexGrow = 1, flexShrink = 1, minHeight = 0, marginLeft = 6, marginRight = 6}};
            _iconPanel = CreateIconPanel();
            _backgroundPanel = CreateBackgroundPanel();
            panels.Add(_iconPanel);
            panels.Add(_backgroundPanel);
            root.Add(panels);

            root.Add(CreateFooter());

            // UI Toolkit shrinks children by default: only the icon grid is allowed to give up space
            foreach (var child in root.Children()) if (child != panels) child.style.flexShrink = 0;

            SelectTab(_tab);
            RefreshAll();
            root.schedule.Execute(() => root.Focus());
        }

        private void Rebuild()
        {
            if (_currentRule == null) return;
            rootVisualElement.Clear();
            _iconItems.Clear();
            _backgroundItems.Clear();
            CreateGUI();
        }

        //---------------------------------------------------------------------
        // Layout
        //---------------------------------------------------------------------

        private VisualElement CreateHeader()
        {
            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    height = 22,
                    flexShrink = 0,
                    paddingLeft = 8,
                    backgroundColor = EditorGUIUtility.isProSkin ? HEADER_CLR_PRO : HEADER_CLR_FREE
                }
            };

            // Dragging is handled in IMGUI: GUIToScreenPoint gives real screen coordinates,
            // whereas UI Toolkit pointer positions lag behind while the window is moving
            _headerDragArea = new IMGUIContainer(HandleHeaderDrag) {style = {position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0}};
            header.Add(_headerDragArea);

            var title = _paths.Count == 1 ? Path.GetFileName(_paths[0]) : Tr("{0} folders", _paths.Count);
            header.Add(new Label(title)
            {
                pickingMode = PickingMode.Ignore,
                style = {flexGrow = 1, unityFontStyleAndWeight = FontStyle.Bold, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis}
            });

            var closeButton = new Button(Close)
            {
                tooltip = Tr("Close (Esc)"),
                style =
                {
                    width = CLOSE_BUTTON_WIDTH, height = 20, marginRight = 0, backgroundColor = Color.clear,
                    paddingLeft = 4, paddingRight = 4, paddingTop = 3, paddingBottom = 3
                }
            };
            SetBorder(closeButton, 0f, Color.clear);

            var iconColor = EditorGUIUtility.isProSkin ? CLOSE_ICON_CLR_PRO : CLOSE_ICON_CLR_FREE;
            var closeIcon = new Image
            {
                image = ProjectEditorUtility.GetCloseIcon(),
                scaleMode = ScaleMode.ScaleToFit,
                tintColor = iconColor,
                pickingMode = PickingMode.Ignore,
                style = {flexGrow = 1}
            };
            closeButton.Add(closeIcon);
            closeButton.RegisterCallback<PointerEnterEvent>(_ => closeIcon.tintColor = EditorGUIUtility.isProSkin ? Color.white : Color.black);
            closeButton.RegisterCallback<PointerLeaveEvent>(_ => closeIcon.tintColor = iconColor);
            header.Add(closeButton);

            return header;
        }

        private VisualElement CreateBody()
        {
            var body = new VisualElement {style = {flexDirection = FlexDirection.Row, paddingLeft = 8, paddingRight = 8, paddingTop = 8, paddingBottom = 6}};
            body.Add(CreatePreview());

            // Settings

            var settings = new VisualElement {style = {flexGrow = 1, marginLeft = 6}};

            var keyRow = new VisualElement {style = {flexDirection = FlexDirection.Row, flexShrink = 0}};
            var typeField = new DropdownField(TrEnumNames(typeof(KeyType)).ToList(), (int) _currentRule.Type)
            {
                tooltip = Tr("Match folders by name or by full path"),
                style = {width = 64, marginLeft = 0}
            };
            typeField.RegisterValueChangedCallback(_ =>
            {
                _currentRule.Type = (KeyType) typeField.index;
                RefreshKey();
            });
            _keyField = new TextField {isReadOnly = true, style = {flexGrow = 1, flexShrink = 1, marginRight = 0}};
            _keyField.SetEnabled(false);
            keyRow.Add(typeField);
            keyRow.Add(_keyField);
            settings.Add(keyRow);

            _priorityField = new IntegerField(Tr("Priority")) {tooltip = Tr("Rules with higher priority override lower ones")};
            _priorityField.RegisterValueChangedCallback(evt => _currentRule.Priority = evt.newValue);
            FixLabelWidth(_priorityField);
            settings.Add(_priorityField);

            _iconRecursiveToggle = CreateToggle(Tr("Icon recursive"), Tr("Apply the icon to all subfolders as well"), value => _currentRule.IsIconRecursive = value);
            settings.Add(_iconRecursiveToggle);

            _backgroundRecursiveToggle = CreateToggle(Tr("BG recursive"), Tr("Apply the background to all subfolders as well"), value => _currentRule.IsBackgroundRecursive = value);
            settings.Add(_backgroundRecursiveToggle);

            body.Add(settings);
            return body;
        }

        private VisualElement CreatePreview()
        {
            var preview = new VisualElement {style = {width = PREVIEW_SIZE_LARGE + 8, flexShrink = 0, alignItems = Align.Center}};

            var largeContainer = new VisualElement {style = {width = PREVIEW_SIZE_LARGE, height = PREVIEW_SIZE_LARGE, flexShrink = 0}};
            _previewLarge = new Image {scaleMode = ScaleMode.ScaleToFit, style = {position = Position.Absolute, left = 0, top = 0, width = PREVIEW_SIZE_LARGE, height = PREVIEW_SIZE_LARGE}};
            var gradient = new Image {image = ProjectEditorUtility.GetPreviewGradientPopup(), style = {position = Position.Absolute, left = 0, top = 0, width = PREVIEW_SIZE_LARGE, height = PREVIEW_SIZE_LARGE}};
            _previewSmall = new Image {scaleMode = ScaleMode.ScaleToFit, style = {position = Position.Absolute, left = 0, bottom = 4, width = PREVIEW_SIZE_SMALL, height = PREVIEW_SIZE_SMALL}};
            largeContainer.Add(_previewLarge);
            largeContainer.Add(gradient);
            largeContainer.Add(_previewSmall);
            preview.Add(largeContainer);

            return preview;
        }

        private VisualElement CreateTabBar()
        {
            var bar = new Toolbar {style = {flexShrink = 0, marginTop = 2}};

            _iconTabToggle = new ToolbarToggle {text = Tr("Icon")};
            _iconTabToggle.RegisterValueChangedCallback(_ => SelectTab(Tab.Icon));
            _backgroundTabToggle = new ToolbarToggle {text = Tr("Background")};
            _backgroundTabToggle.RegisterValueChangedCallback(_ => SelectTab(Tab.Background));
            bar.Add(_iconTabToggle);
            bar.Add(_backgroundTabToggle);

            bar.Add(new ToolbarSpacer {flex = true});
            bar.Add(CreateSearchField(query =>
            {
                _query = query;
                ApplyFilters();
            }));

            return bar;
        }

        private VisualElement CreateIconPanel()
        {
            var panel = CreatePanel();

            var topRow = CreateTopRow();
            var categoryField = new DropdownField(TrEnumNames(typeof(ProjectIconCategory)).ToList(), (int) _category)
            {
                tooltip = Tr("Icon category"),
                style = {width = 110, marginLeft = 0}
            };
            categoryField.RegisterValueChangedCallback(_ =>
            {
                _category = (ProjectIconCategory) categoryField.index;
                EditorPrefs.SetInt(LAST_CATEGORY_PKEY, (int) _category);
                ApplyFilters();
            });
            topRow.Add(categoryField);
            _iconNameLabel = CreateNameLabel();
            topRow.Add(_iconNameLabel);
            panel.Add(topRow);

            _recentIconsRow = CreateRecentRow();
            panel.Add(_recentIconsRow);

            var scroll = CreateGridScrollView(out var grid);
            foreach (var icon in BUILT_IN_ICONS)
            {
                var item = CreateGridItem(ProjectIconsStorage.GetIcon(icon, false), GetDisplayName(icon), GRID_ITEM_SIZE,
                    () => SelectIcon(icon), () => ApplyIcon(icon));
                _iconItems[icon] = item;
                grid.Add(item);
            }
            _emptyIconsLabel = new Label(Tr("Nothing found")) {style = {opacity = 0.6f, marginLeft = 4, marginTop = 4}};
            grid.Add(_emptyIconsLabel);
            panel.Add(scroll);

            panel.Add(CreateNoneCustomButtons(() => SelectIcon(ProjectIcon.None), () => SelectIcon(ProjectIcon.Custom), 2));

            _customIconFields = new VisualElement {style = {flexShrink = 0}};
            _smallIconField = CreateTextureField("x16", tex => _currentRule.SmallIcon = tex);
            _largeIconField = CreateTextureField("x64", tex => _currentRule.LargeIcon = tex);
            _customIconFields.Add(_smallIconField);
            _customIconFields.Add(_largeIconField);
            panel.Add(_customIconFields);

            return panel;
        }

        private VisualElement CreateBackgroundPanel()
        {
            var panel = CreatePanel();

            // Same header rows as the icon tab, so the grid keeps its size when switching tabs
            var topRow = CreateTopRow();
            _backgroundNameLabel = CreateNameLabel();
            topRow.Add(_backgroundNameLabel);
            panel.Add(topRow);

            _recentBackgroundsRow = CreateRecentRow();
            panel.Add(_recentBackgroundsRow);

            var scroll = CreateGridScrollView(out var grid);
            foreach (var background in BUILT_IN_BACKGROUNDS)
            {
                var item = CreateGridItem(ProjectBackgroundsStorage.GetBackground(background), GetDisplayName(background), GRID_ITEM_SIZE,
                    () => SelectBackground(background), () => ApplyBackground(background));
                _backgroundItems[background] = item;
                grid.Add(item);
            }
            panel.Add(scroll);

            panel.Add(CreateNoneCustomButtons(() => SelectBackground(ProjectBackground.None), () => SelectBackground(ProjectBackground.Custom), 2));

            _customBackgroundFields = new VisualElement {style = {flexShrink = 0}};
            _backgroundField = CreateTextureField("x16", tex => _currentRule.BackgroundTexture = tex);
            _customBackgroundFields.Add(_backgroundField);
            panel.Add(_customBackgroundFields);

            return panel;
        }

        private VisualElement CreateFooter()
        {
            var footer = new VisualElement {style = {flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 6, paddingRight = 6, paddingTop = 4, paddingBottom = 6}};

            footer.Add(CreateIconButton(ProjectEditorUtility.GetSettingsButtonIcon(), Tr("All Rules"), () =>
            {
                ProjectRuleset.ShowInspector();
                Close();
            }));
            footer.Add(CreateIconButton(ProjectEditorUtility.GetFilterButtonIcon(), Tr("Folder Rules"), () =>
            {
                ProjectRuleset.ShowInspector(AssetDatabase.LoadAssetAtPath<DefaultAsset>(_paths[0]));
                Close();
            }));
            footer.Add(CreateIconButton(ProjectEditorUtility.GetDeleteButtonIcon(), Tr("Revert to Default"), ResetToDefault));

            footer.Add(new Button(CopyStyle) {text = Tr("Copy"), tooltip = Tr("Copy this style (icon, background, priority)")});
            _pasteButton = new Button(PasteStyle) {text = Tr("Paste"), tooltip = Tr("Paste a previously copied style")};
            footer.Add(_pasteButton);

            footer.Add(new VisualElement {style = {flexGrow = 1}});

            footer.Add(new Button(Close) {text = Tr("Cancel"), tooltip = "Esc", style = {width = 60}});
            footer.Add(new Button(Apply) {text = Tr("Apply"), tooltip = "Enter", style = {width = 60, marginRight = 0, unityFontStyleAndWeight = FontStyle.Bold}});

            return footer;
        }

        //---------------------------------------------------------------------
        // Element factories
        //---------------------------------------------------------------------

        private static VisualElement CreatePanel()
        {
            return new VisualElement {style = {flexGrow = 1, flexShrink = 1, minHeight = 0}};
        }

        private static VisualElement CreateTopRow()
        {
            return new VisualElement {style = {flexDirection = FlexDirection.Row, alignItems = Align.Center, flexShrink = 0, marginTop = 2, height = 22}};
        }

        private static Label CreateNameLabel()
        {
            return new Label {style = {flexGrow = 1, unityTextAlign = TextAnchor.MiddleRight, opacity = 0.7f}};
        }

        private static VisualElement CreateRecentRow()
        {
            return new VisualElement {style = {flexDirection = FlexDirection.Row, alignItems = Align.Center, flexShrink = 0, marginTop = 2, height = RECENT_ITEM_SIZE + 4}};
        }

        private static Toggle CreateToggle(string label, string tooltip, Action<bool> onChange)
        {
            var toggle = new Toggle(label) {tooltip = tooltip};
            toggle.RegisterValueChangedCallback(evt => onChange(evt.newValue));
            FixLabelWidth(toggle);
            return toggle;
        }

        private static Button CreateIconButton(Texture2D icon, string tooltip, Action onClick)
        {
            var button = new Button(onClick) {tooltip = tooltip, style = {width = 20, height = 20, paddingLeft = 2, paddingRight = 2, paddingTop = 2, paddingBottom = 2}};
            button.Add(new Image {image = icon, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore, style = {flexGrow = 1}});
            return button;
        }

        private static ObjectField CreateTextureField(string label, Action<Texture2D> onChange)
        {
            var field = new ObjectField(label) {objectType = typeof(Texture2D), allowSceneObjects = false};
            field.RegisterValueChangedCallback(evt => onChange((Texture2D) evt.newValue));
            FixLabelWidth(field, 40f);
            return field;
        }

        //---------------------------------------------------------------------
        // Actions
        //---------------------------------------------------------------------

        private void SelectTab(Tab tab)
        {
            _tab = tab;
            EditorPrefs.SetInt(LAST_TAB_PKEY, (int) tab);

            SetVisible(_iconPanel, tab == Tab.Icon);
            SetVisible(_backgroundPanel, tab == Tab.Background);

            // Toggles behave like radio buttons
            _iconTabToggle.SetValueWithoutNotify(tab == Tab.Icon);
            _backgroundTabToggle.SetValueWithoutNotify(tab == Tab.Background);
        }

        private void SelectIcon(ProjectIcon icon)
        {
            _currentRule.IconType = icon;
            if (icon != ProjectIcon.Custom)
            {
                _currentRule.SmallIcon = null;
                _currentRule.LargeIcon = null;
            }
            if (icon == ProjectIcon.None) _currentRule.IsIconRecursive = false;

            RefreshAll();
        }

        private void SelectBackground(ProjectBackground background)
        {
            _currentRule.BackgroundType = background;
            if (background != ProjectBackground.Custom) _currentRule.BackgroundTexture = null;
            if (background == ProjectBackground.None) _currentRule.IsBackgroundRecursive = false;

            RefreshAll();
        }

        private void ApplyIcon(ProjectIcon icon)
        {
            SelectIcon(icon);
            Apply();
        }

        private void ApplyBackground(ProjectBackground background)
        {
            SelectBackground(background);
            Apply();
        }

        private void ResetToDefault()
        {
            _currentRule.ResetStyle();
            RefreshAll();
        }

        private void CopyStyle()
        {
            SessionState.SetString(CLIPBOARD_SKEY, EditorJsonUtility.ToJson(_currentRule));
            RefreshPasteButton();
        }

        private void PasteStyle()
        {
            var json = SessionState.GetString(CLIPBOARD_SKEY, string.Empty);
            if (string.IsNullOrEmpty(json)) return;

            var copied = new ProjectRule(KeyType.Path, string.Empty);
            EditorJsonUtility.FromJsonOverwrite(json, copied);

            // Keep the key of the current folder, take only the style
            _currentRule.CopyStyleFrom(copied);
            RefreshAll();
        }

        private void Apply()
        {
            for (var i = 0; i < _paths.Count; i++)
            {
                _currentRule.Key = GetKey(_paths[i]);
                _ruleset.UpdateRule(_existingRules[i], _currentRule);
            }

            if (_currentRule.IconType != ProjectIcon.None && !_currentRule.HasCustomIcon())
                PushRecent(RECENT_ICONS_PKEY, (int) _currentRule.IconType);
            if (_currentRule.BackgroundType != ProjectBackground.None && !_currentRule.HasCustomBackground())
                PushRecent(RECENT_BACKGROUNDS_PKEY, (int) _currentRule.BackgroundType);

            EditorApplication.RepaintProjectWindow();
            Close();
        }

        //---------------------------------------------------------------------
        // Refresh
        //---------------------------------------------------------------------

        private void RefreshAll()
        {
            RefreshKey();
            RefreshPreview();
            RefreshSelection();
            RefreshRecents();
            RefreshPasteButton();

            _priorityField.SetValueWithoutNotify(_currentRule.Priority);
            _iconRecursiveToggle.SetValueWithoutNotify(_currentRule.IsIconRecursive);
            _backgroundRecursiveToggle.SetValueWithoutNotify(_currentRule.IsBackgroundRecursive);
            _iconRecursiveToggle.SetEnabled(_currentRule.IconType != ProjectIcon.None);
            _backgroundRecursiveToggle.SetEnabled(_currentRule.BackgroundType != ProjectBackground.None);

            _smallIconField.SetValueWithoutNotify(_currentRule.SmallIcon);
            _largeIconField.SetValueWithoutNotify(_currentRule.LargeIcon);
            _backgroundField.SetValueWithoutNotify(_currentRule.BackgroundTexture);
            SetVisible(_customIconFields, _currentRule.HasCustomIcon());
            SetVisible(_customBackgroundFields, _currentRule.HasCustomBackground());

            _iconNameLabel.text = GetDisplayName(_currentRule.IconType);
            _backgroundNameLabel.text = GetDisplayName(_currentRule.BackgroundType);

            ApplyFilters();
        }

        private void RefreshKey()
        {
            _keyField.SetValueWithoutNotify(_paths.Count == 1 ? GetKey(_paths[0]) : "---");
        }

        private void RefreshPreview()
        {
            var defaultIcon = ProjectEditorUtility.GetDefaultFolderIcon();
            var large = _currentRule.GetIconTexture(false);
            var small = _currentRule.GetIconTexture(true);

            _previewLarge.image = large != null ? large : defaultIcon;
            _previewSmall.image = small != null ? small : defaultIcon;
        }

        private void RefreshSelection()
        {
            foreach (var pair in _iconItems)
                SetBorderColor(pair.Value, pair.Key == _currentRule.IconType ? HIGHLIGHT_CLR : Color.clear);

            foreach (var pair in _backgroundItems)
                SetBorderColor(pair.Value, pair.Key == _currentRule.BackgroundType ? HIGHLIGHT_CLR : Color.clear);
        }

        private void RefreshRecents()
        {
            FillRecentRow(_recentIconsRow, RECENT_ICONS_PKEY, _iconItems, _currentRule.IconType,
                icon => ProjectIconsStorage.GetIcon(icon, false), SelectIcon, ApplyIcon);

            FillRecentRow(_recentBackgroundsRow, RECENT_BACKGROUNDS_PKEY, _backgroundItems, _currentRule.BackgroundType,
                ProjectBackgroundsStorage.GetBackground, SelectBackground, ApplyBackground);
        }

        private static void FillRecentRow<T>(VisualElement row, string prefKey, Dictionary<T, VisualElement> knownItems, T current,
            Func<T, Texture2D> getTexture, Action<T> select, Action<T> apply) where T : Enum
        {
            row.Clear();
            row.Add(new Label(Tr("Recent")) {style = {opacity = 0.6f, width = 46}});

            foreach (var id in LoadRecent(prefKey))
            {
                // Ignore ids that no longer exist in the enum
                var value = (T) Enum.ToObject(typeof(T), id);
                if (!knownItems.ContainsKey(value)) continue;

                var item = CreateGridItem(getTexture(value), GetDisplayName(value), RECENT_ITEM_SIZE, () => select(value), () => apply(value));
                if (value.Equals(current)) SetBorderColor(item, HIGHLIGHT_CLR);
                row.Add(item);
            }

            if (row.childCount == 1) row.Add(new Label(Tr("None")) {style = {opacity = 0.4f}});
        }

        private void RefreshPasteButton()
        {
            _pasteButton.SetEnabled(!string.IsNullOrEmpty(SessionState.GetString(CLIPBOARD_SKEY, string.Empty)));
        }

        private void ApplyFilters()
        {
            var visibleIcons = 0;
            foreach (var pair in _iconItems)
            {
                var visible = IsInCategory(pair.Key, _category) && MatchesQuery(pair.Key, _query);
                SetVisible(pair.Value, visible);
                if (visible) visibleIcons++;
            }
            SetVisible(_emptyIconsLabel, visibleIcons == 0);

            foreach (var pair in _backgroundItems)
                SetVisible(pair.Value, MatchesQuery(pair.Key, _query));
        }

        //---------------------------------------------------------------------
        // Events
        //---------------------------------------------------------------------

        private void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    evt.StopPropagation();
                    Close();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    evt.StopPropagation();
                    // Let focused fields commit their values first
                    rootVisualElement.schedule.Execute(Apply);
                    break;
            }
        }

        private void HandleHeaderDrag()
        {
            var e = Event.current;
            var id = GUIUtility.GetControlID(FocusType.Passive);
            var area = _headerDragArea.contentRect;
            // The close button sits on top of the right edge: it gets its own cursor and is not a drag handle
            var closeRect = new Rect(area.xMax - CLOSE_BUTTON_WIDTH, area.y, CLOSE_BUTTON_WIDTH, area.height);
            var rect = new Rect(area.x, area.y, area.width - CLOSE_BUTTON_WIDTH, area.height);

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Pan);
            EditorGUIUtility.AddCursorRect(closeRect, MouseCursor.Link);

            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown when e.button == 0 && rect.Contains(e.mousePosition):
                    _dragOffset = position.position - GUIUtility.GUIToScreenPoint(e.mousePosition);
                    GUIUtility.hotControl = id;
                    e.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    position = new Rect(GUIUtility.GUIToScreenPoint(e.mousePosition) + _dragOffset, position.size);
                    e.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    e.Use();
                    break;
            }
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private void Init(ProjectRuleset ruleset, List<string> paths, int pathIndex)
        {
            _paths = paths;
            _ruleset = ruleset;

            _existingRules = paths.Select(path => ruleset.GetRuleByPath(path)).ToArray();
            _currentRule = new ProjectRule(KeyType.Path, paths[pathIndex]);
            if (_existingRules[pathIndex] != null) _currentRule.CopyFrom(_existingRules[pathIndex]);

            _tab = (Tab) EditorPrefs.GetInt(LAST_TAB_PKEY, (int) Tab.Icon);
            if (!Enum.IsDefined(typeof(Tab), _tab)) _tab = Tab.Icon;

            _category = (ProjectIconCategory) EditorPrefs.GetInt(LAST_CATEGORY_PKEY, (int) ProjectIconCategory.All);
            if (!Enum.IsDefined(typeof(ProjectIconCategory), _category)) _category = ProjectIconCategory.All;
        }

        private string GetKey(string path)
        {
            return _currentRule.Type == KeyType.Path ? path : Path.GetFileName(path);
        }

        private static List<int> LoadRecent(string key)
        {
            var result = new List<int>();
            foreach (var part in EditorPrefs.GetString(key, string.Empty).Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part, out var id)) result.Add(id);
            }
            return result;
        }

        private static void PushRecent(string key, int id)
        {
            var list = LoadRecent(key);
            list.Remove(id);
            list.Insert(0, id);
            if (list.Count > RECENT_MAX) list.RemoveRange(RECENT_MAX, list.Count - RECENT_MAX);
            EditorPrefs.SetString(key, string.Join(",", list));
        }

        //---------------------------------------------------------------------
        // Nested
        //---------------------------------------------------------------------

        private enum Tab
        {
            Icon,
            Background
        }
    }
}
