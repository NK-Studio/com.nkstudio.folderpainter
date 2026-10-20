# Changelog

## [1.0.0]

### Changed
- Reorganized the `Editor` folder by responsibility (Core, Rules, Preferences, Localization, UI, Textures, Utility).
- Renamed the editor assembly to `NKStudio.FolderPainter.Editor` and `ProjectPopupWindowUITK` to `ProjectPopupWindow`.
- Project Settings page is now built with UI Toolkit; the IMGUI `EditorPrefsItem` classes were removed. Stored preferences are kept.
- Rule lookups no longer allocate while the Project window is drawn, and use ordinal string comparison.
- Icon / background texture resolution is shared by every view instead of being duplicated.
- The settings page shows the version from `package.json`.
- Auto-creating the default ruleset no longer changes the selection or focuses the Project window.
- Localization is split into one `FPLanguagePack` per language, discovered automatically, so new languages
  (e.g. Japanese, Chinese) only need a new file. The language is stored as a code; the previous setting is migrated.
- Every Folder Painter window, the ruleset inspector and the settings page use the bundled "Folder Painter Sans KR"
  font (IBM Plex Sans KR subset, Regular + Bold). Characters outside the subset fall back to the editor fonts.
- The popup close button uses `Editor/Images/Close.png`, tinted to the editor skin.
- The color, tag and transparent folder icons and the color row backgrounds are now drawn in code
  (`ProceduralTextures`) with a new NK Studio palette (OKLCH, even lightness and chroma, yellows kept bright)
  instead of embedded Rainbow Folders 2 PNGs. Tags are rounded squares with a gap around them.
- The editor textures (folder edit icon, preview gradients, project tree lines, filter / delete / settings buttons)
  are drawn in code as well. The package no longer contains Rainbow Folders 2 artwork (CC BY-NC 4.0), so it is
  covered by the Apache License 2.0 (fonts: SIL Open Font License 1.1). The unused `IcnFoldoutFirst` and
  `IcnFoldoutLast` textures were removed.
- The Type and Platform icons were redrawn.
- Removed the small folder row under the popup preview; clickable elements show a hand cursor.
- Icon tooltips no longer show the enum prefixes ("Tpe Editor" → "Editor", "Red (Tag)", "Scripts (BG)").
- Removed the Rainbow Folders logo icon (`ProjectIcon.TpeRainbow`) and the default rule using it for `Assets/Plugins/Borodar`.
  Rules that still reference a removed icon now show the default folder icon instead of a gray square.

### Fixed
- A folder lost its own non-recursive icon or background when an ancestor had a recursive rule that did not win.
- Undo / redo of popup changes did not refresh the Project window unless the ruleset inspector was open.
- Possible `NullReferenceException` when the ruleset changed before anything subscribed to `OnRulesetChange`.
- Alt+clicking a folder outside the current multi-selection opened the popup with an invalid index.
- The settings page threw when no ruleset existed in the project.
- Importing a ruleset can now be undone, and invalid `.rfset` files are reported instead of throwing.
