// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored.

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static NKStudio.FolderPainter.FPLocalization;

namespace NKStudio.FolderPainter
{
    /// <summary>
    /// Per-project editor preferences, stored in EditorPrefs and shown under Project Settings.
    /// </summary>
    public static class ProjectPreferences
    {
        public const string SETTINGS_PATH = "NK Studio/Folder Painter";

        // The ruleset is project data, so it lives in Assets rather than inside the (read-only) package
        public const string RULESET_DEFAULT = "Assets/Settings/FolderPainter/FolderPainterRuleset.asset";

        // Keys are suffixed with the project name, so several projects on one machine keep their own values
        private const string RULESET_PKEY = "NKStudio.FolderPainter.Ruleset.";
        private const string EDIT_MODIFIER_PKEY = "NKStudio.FolderPainter.EditMod.";
        private const string PROJECT_TREE_PKEY = "NKStudio.FolderPainter.ShowProjectTree.";
        private const string ROW_SHADING_PKEY = "NKStudio.FolderPainter.RowShading.";

        private const EventModifiers EDIT_MODIFIER_DEFAULT = EventModifiers.Alt;
        private const bool PROJECT_TREE_DEFAULT = true;
        private const bool ROW_SHADING_DEFAULT = true;

        private const string RULESET_HINT = "The ruleset that is currently used. You could have multiple rulesets in your project and switch between them using this option.";
        private const string EDIT_MODIFIER_HINT = "Modifier key that is used to show configuration dialogue when clicking on a folder icon.";
        private const string PROJECT_TREE_HINT = "Change this setting to show/hide the \"branches\" in the project window.";
        private const string ROW_SHADING_HINT = "Change this settings to enable/disable row shading in the project window.";
        private const string LANGUAGE_HINT = "Language used by Folder Painter windows and dialogs.";

        private static readonly string KEY_SUFFIX = ProjectEditorUtility.ProjectName;

        private static EventModifiers _editModifier;
        private static bool _showProjectTree;
        private static bool _drawRowShading;

        //---------------------------------------------------------------------
        // Properties
        //---------------------------------------------------------------------

        public static string RulesetPath { get; private set; }

        public static EventModifiers EditModifier
        {
            get => _editModifier;
            set
            {
                _editModifier = value;
                EditorPrefs.SetInt(EDIT_MODIFIER_PKEY + KEY_SUFFIX, (int) value);
            }
        }

        public static bool ShowProjectTree
        {
            get => _showProjectTree;
            set => SetBoolAndRepaint(ref _showProjectTree, value, PROJECT_TREE_PKEY);
        }

        public static bool DrawRowShading
        {
            get => _drawRowShading;
            set => SetBoolAndRepaint(ref _drawRowShading, value, ROW_SHADING_PKEY);
        }

        //---------------------------------------------------------------------
        // Ctors
        //---------------------------------------------------------------------

        static ProjectPreferences()
        {
            RulesetPath = EditorPrefs.GetString(RULESET_PKEY + KEY_SUFFIX, RULESET_DEFAULT);

            var modifier = EditorPrefs.GetInt(EDIT_MODIFIER_PKEY + KEY_SUFFIX, (int) EDIT_MODIFIER_DEFAULT);
            _editModifier = Enum.IsDefined(typeof(EventModifiers), modifier) ? (EventModifiers) modifier : EDIT_MODIFIER_DEFAULT;

            _showProjectTree = EditorPrefs.GetBool(PROJECT_TREE_PKEY + KEY_SUFFIX, PROJECT_TREE_DEFAULT);
            _drawRowShading = EditorPrefs.GetBool(ROW_SHADING_PKEY + KEY_SUFFIX, ROW_SHADING_DEFAULT);
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        [SettingsProvider]
        public static SettingsProvider CreateSettingProvider()
        {
            return new SettingsProvider(SETTINGS_PATH, SettingsScope.Project)
            {
                label = AssetInfo.NAME,
                activateHandler = (_, root) => BuildSettings(root),
                keywords = new[] {"Folder", "Icon", "Ruleset", "Modifier", "Project Tree", "Row Shading", "Language"}
            };
        }

        public static void UpdateRulesetPath(string path, bool updatePref, bool reloadInstance)
        {
            if (RulesetPath == path) return;
            if (updatePref) EditorPrefs.SetString(RULESET_PKEY + KEY_SUFFIX, path);

            RulesetPath = path;

            if (!reloadInstance) return;

            ProjectRuleset.UpdateInstance();
            EditorApplication.RepaintProjectWindow();
        }

        public static bool IsEditModifierPressed(Event e)
        {
            return (e.modifiers & _editModifier) == _editModifier;
        }

        //---------------------------------------------------------------------
        // Settings UI
        //---------------------------------------------------------------------

        private static void BuildSettings(VisualElement root)
        {
            root.Clear();
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            FPElements.ApplyTheme(root);

            root.Add(new Label(AssetInfo.NAME) {style = {fontSize = 19, unityFontStyleAndWeight = FontStyle.Bold, marginTop = 2, marginBottom = 10}});

            // General

            root.Add(Header("General"));

            var languages = Languages.ToList();
            var languageField = new PopupField<FPLanguagePack>(Tr("Language"), languages, Language, FormatLanguage, FormatLanguage)
            {
                tooltip = Tr(LANGUAGE_HINT)
            };
            languageField.RegisterValueChangedCallback(evt =>
            {
                Language = evt.newValue;
                // Labels are translated when they are created
                root.schedule.Execute(() => BuildSettings(root));
            });
            root.Add(Aligned(languageField));

            root.Add(CreateRulesetField());

            var modifierField = new EnumField(Tr("Edit Modifier"), EditModifier) {tooltip = Tr(EDIT_MODIFIER_HINT)};
            modifierField.RegisterValueChangedCallback(evt => EditModifier = (EventModifiers) evt.newValue);
            root.Add(Aligned(modifierField));

            // Enhancements

            root.Add(Header("Enhancements"));

            var projectTreeToggle = new Toggle(Tr("Project Tree")) {value = ShowProjectTree, tooltip = Tr(PROJECT_TREE_HINT)};
            projectTreeToggle.RegisterValueChangedCallback(evt => ShowProjectTree = evt.newValue);
            root.Add(Aligned(projectTreeToggle));

            var rowShadingToggle = new Toggle(Tr("Row Shading")) {value = DrawRowShading, tooltip = Tr(ROW_SHADING_HINT)};
            rowShadingToggle.RegisterValueChangedCallback(evt => DrawRowShading = evt.newValue);
            root.Add(Aligned(rowShadingToggle));

            // Import / Export

            root.Add(Header("Import / Export Ruleset"));

            var buttons = new VisualElement {style = {flexDirection = FlexDirection.Row}};
            buttons.Add(new Button(ProjectRulesetExporter.Import) {text = Tr("Import"), style = {flexGrow = 1, marginLeft = 0}});
            buttons.Add(new Button(ProjectRulesetExporter.Export) {text = Tr("Export"), style = {flexGrow = 1, marginRight = 0}});
            root.Add(buttons);

            // Version

            root.Add(new VisualElement {style = {flexGrow = 1}});
            root.Add(new Label($"{Tr("Version")} {AssetInfo.Version}")
            {
                style = {unityTextAlign = TextAnchor.MiddleCenter, opacity = 0.6f, fontSize = 10, marginTop = 10, marginBottom = 6}
            });
        }

        private static VisualElement CreateRulesetField()
        {
            var paths = ProjectEditorUtility.FindPathsForAllRulesets().ToList();
            if (paths.Count == 0)
                return new HelpBox(Tr("No ruleset found in the project."), HelpBoxMessageType.Warning);

            var index = paths.IndexOf(RulesetPath);
            if (index < 0)
            {
                // The stored ruleset is gone: fall back to the first one found
                index = 0;
                UpdateRulesetPath(paths[0], true, true);
            }

            // Slashes would be shown as submenus in the dropdown
            var field = new PopupField<string>(Tr("Current Ruleset"), paths, index, FormatRulesetPath, FormatRulesetPath)
            {
                tooltip = Tr(RULESET_HINT)
            };
            field.RegisterValueChangedCallback(evt => UpdateRulesetPath(evt.newValue, true, true));
            return Aligned(field);
        }

        private static string FormatLanguage(FPLanguagePack language)
        {
            return language.DisplayName;
        }

        private static string FormatRulesetPath(string path)
        {
            return path.Replace('/', '\\');
        }

        private static Label Header(string text)
        {
            return new Label(Tr(text)) {style = {unityFontStyleAndWeight = FontStyle.Bold, marginTop = 8, marginBottom = 4}};
        }

        /// <summary>Aligns field labels with each other, the same way the Inspector does.</summary>
        private static VisualElement Aligned(VisualElement field)
        {
            field.AddToClassList(BaseField<bool>.alignedFieldUssClassName);
            return field;
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static void SetBoolAndRepaint(ref bool field, bool value, string key)
        {
            if (field == value) return;

            field = value;
            EditorPrefs.SetBool(key + KEY_SUFFIX, value);
            EditorApplication.RepaintProjectWindow();
        }
    }
}
