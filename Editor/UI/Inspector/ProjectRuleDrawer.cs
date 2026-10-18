// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored.

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using KeyType = NKStudio.FolderPainter.ProjectRule.KeyType;
using static NKStudio.FolderPainter.FPElements;
using static NKStudio.FolderPainter.FPLocalization;

namespace NKStudio.FolderPainter
{
    [CustomPropertyDrawer(typeof(ProjectRule))]
    public class ProjectRuleDrawer : PropertyDrawer
    {
        private const float LABELS_WIDTH = 100f;
        private const float PREVIEW_SIZE_SMALL = 16f;
        private const float PREVIEW_SIZE_LARGE = 64f;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new RuleElement(property);
        }

        //---------------------------------------------------------------------
        // Nested
        //---------------------------------------------------------------------

        private class RuleElement : VisualElement
        {
            private readonly SerializedProperty _property;
            private readonly SerializedProperty _keyType;
            private readonly SerializedProperty _iconType;
            private readonly SerializedProperty _smallIcon;
            private readonly SerializedProperty _largeIcon;
            private readonly SerializedProperty _backgroundType;
            private readonly SerializedProperty _backgroundTexture;

            private readonly DropdownField _typeField;
            private readonly Button _iconButton;
            private readonly Button _backgroundButton;
            private readonly VisualElement _customIconFields;
            private readonly VisualElement _customBackgroundFields;
            private readonly Image _previewLarge;
            private readonly Image _previewSmall;
            private readonly Image _previewRowIcon;
            private readonly Image _previewRowBackground;

            public RuleElement(SerializedProperty property)
            {
                _property = property.Copy();
                _keyType = Find(nameof(ProjectRule.Type));
                _iconType = Find(nameof(ProjectRule.IconType));
                _smallIcon = Find(nameof(ProjectRule.SmallIcon));
                _largeIcon = Find(nameof(ProjectRule.LargeIcon));
                _backgroundType = Find(nameof(ProjectRule.BackgroundType));
                _backgroundTexture = Find(nameof(ProjectRule.BackgroundTexture));

                // Also set here: the drawer can be shown outside of the ruleset inspector
                FPElements.ApplyTheme(this);
                style.flexDirection = FlexDirection.Row;
                style.paddingTop = 6;
                style.paddingBottom = 6;

                // Fields

                var fields = new VisualElement {style = {flexGrow = 1, flexShrink = 1}};

                var keyRow = Row();
                _typeField = new DropdownField(TrEnumNames(typeof(KeyType)).ToList(), _keyType.enumValueIndex)
                {
                    tooltip = Tr("Match folders by name or by full path"),
                    style = {width = LABELS_WIDTH - 4, marginLeft = 0}
                };
                _typeField.RegisterValueChangedCallback(_ =>
                {
                    _keyType.serializedObject.Update();
                    _keyType.enumValueIndex = _typeField.index;
                    _keyType.serializedObject.ApplyModifiedProperties();
                });
                keyRow.Add(_typeField);
                keyRow.Add(new PropertyField(Find(nameof(ProjectRule.Key)), string.Empty) {style = {flexGrow = 1, flexShrink = 1}});
                fields.Add(keyRow);

                fields.Add(LabeledRow(Tr("Priority"), new PropertyField(Find(nameof(ProjectRule.Priority)), string.Empty)));

                _iconButton = new Button(() => ProjectTexturePickerWindow.ShowIconPicker(GUIUtility.GUIToScreenRect(_iconButton.worldBound), _property));
                fields.Add(LabeledRow(Tr("Icon"), _iconButton, 6));

                _customIconFields = new VisualElement();
                _customIconFields.Add(LabeledRow("x16", new PropertyField(_smallIcon, string.Empty)));
                _customIconFields.Add(LabeledRow("x64", new PropertyField(_largeIcon, string.Empty)));
                fields.Add(_customIconFields);

                fields.Add(LabeledRow(Tr("Recursive"), new PropertyField(Find(nameof(ProjectRule.IsIconRecursive)), string.Empty)));

                _backgroundButton = new Button(() => ProjectTexturePickerWindow.ShowBackgroundPicker(GUIUtility.GUIToScreenRect(_backgroundButton.worldBound), _property));
                fields.Add(LabeledRow(Tr("Background"), _backgroundButton, 6));

                _customBackgroundFields = new VisualElement();
                _customBackgroundFields.Add(LabeledRow("x16", new PropertyField(_backgroundTexture, string.Empty)));
                fields.Add(_customBackgroundFields);

                fields.Add(LabeledRow(Tr("Recursive"), new PropertyField(Find(nameof(ProjectRule.IsBackgroundRecursive)), string.Empty)));

                Add(fields);

                // Preview

                var preview = new VisualElement {style = {width = PREVIEW_SIZE_LARGE + 8, flexShrink = 0, alignItems = Align.FlexEnd, paddingTop = 48}};

                var largeContainer = new VisualElement {style = {width = PREVIEW_SIZE_LARGE, height = PREVIEW_SIZE_LARGE}};
                _previewLarge = new Image {scaleMode = ScaleMode.ScaleToFit, style = {position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0}};
                var gradient = new Image {image = ProjectEditorUtility.GetPreviewGradientDrawer(), style = {position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0}};
                _previewSmall = new Image {scaleMode = ScaleMode.ScaleToFit, style = {position = Position.Absolute, left = 0, bottom = 4, width = PREVIEW_SIZE_SMALL, height = PREVIEW_SIZE_SMALL}};
                largeContainer.Add(_previewLarge);
                largeContainer.Add(gradient);
                largeContainer.Add(_previewSmall);
                preview.Add(largeContainer);

                var previewRow = new VisualElement {style = {width = PREVIEW_SIZE_LARGE, height = PREVIEW_SIZE_SMALL, marginTop = 6, flexDirection = FlexDirection.Row, alignItems = Align.Center}};
                _previewRowBackground = new Image {scaleMode = ScaleMode.StretchToFill, style = {position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0}};
                _previewRowIcon = new Image {scaleMode = ScaleMode.ScaleToFit, style = {width = PREVIEW_SIZE_SMALL, height = PREVIEW_SIZE_SMALL}};
                previewRow.Add(_previewRowBackground);
                previewRow.Add(_previewRowIcon);
                previewRow.Add(new Label(Tr("Folder")) {style = {fontSize = 10, paddingLeft = 1, paddingTop = 0, paddingBottom = 0}});
                preview.Add(previewRow);

                Add(preview);

                // Keep preview, button captions and custom fields in sync with the data
                this.TrackPropertyValue(_property, _ => Refresh());
                Refresh();
            }

            private SerializedProperty Find(string relativePath)
            {
                return _property.FindPropertyRelative(relativePath);
            }

            private void Refresh()
            {
                // The element may outlive its array slot (e.g. after a rule was removed)
                try
                {
                    _ = _iconType.intValue;
                }
                catch (Exception)
                {
                    return;
                }

                var iconType = (ProjectIcon) _iconType.intValue;
                var backgroundType = (ProjectBackground) _backgroundType.intValue;
                var smallIcon = (Texture2D) _smallIcon.objectReferenceValue;
                var largeIcon = (Texture2D) _largeIcon.objectReferenceValue;

                _typeField.SetValueWithoutNotify(_typeField.choices[Mathf.Clamp(_keyType.enumValueIndex, 0, _typeField.choices.Count - 1)]);
                _iconButton.text = GetDisplayName(iconType);
                _backgroundButton.text = GetDisplayName(backgroundType);
                SetVisible(_customIconFields, iconType == ProjectIcon.Custom);
                SetVisible(_customBackgroundFields, backgroundType == ProjectBackground.Custom);

                // Preview

                var defaultIcon = ProjectEditorUtility.GetDefaultFolderIcon();
                var large = ProjectRule.ResolveIcon(iconType, smallIcon, largeIcon, false);
                var small = ProjectRule.ResolveIcon(iconType, smallIcon, largeIcon, true);

                _previewLarge.image = large != null ? large : defaultIcon;
                _previewSmall.image = small != null ? small : defaultIcon;
                _previewRowIcon.image = _previewSmall.image;
                _previewRowBackground.image = ProjectRule.ResolveBackground(backgroundType, (Texture2D) _backgroundTexture.objectReferenceValue);
            }

            private static VisualElement Row()
            {
                return new VisualElement {style = {flexDirection = FlexDirection.Row, alignItems = Align.Center, minHeight = 20}};
            }

            private static VisualElement LabeledRow(string label, VisualElement field, float marginTop = 0f)
            {
                var row = Row();
                row.style.marginTop = marginTop;
                row.Add(new Label(label) {style = {width = LABELS_WIDTH, flexShrink = 0, paddingLeft = 1}});
                field.style.flexGrow = 1;
                field.style.flexShrink = 1;
                field.style.marginLeft = 0;
                row.Add(field);
                return row;
            }
        }
    }
}
