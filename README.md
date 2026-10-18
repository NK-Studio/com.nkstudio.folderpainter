# Folder Painter

Colors and icons for folders in the Unity Project window (Unity 6.0+).
Port of [Rainbow Folders 2](https://github.com/Borod4r/Rainbow-Folders-2) with a UI Toolkit configuration popup,
a UI Toolkit ruleset inspector and English / Korean localization.

## Installation

Package Manager → **+** → **Install package from git URL...**

```
https://github.com/NK-Studio/com.nkstudio.folderpainter.git
```

## Usage

- Hold the edit modifier (**Alt** by default) and click a folder icon to open the configuration popup.
- Rules are stored in a ruleset asset, by default `Assets/Settings/FolderPainter/FolderPainterRuleset.asset`.
- Preferences: **Project Settings → NK Studio → Folder Painter**.

## Adding a language

Texts are written in English in the code and used as translation keys. Missing translations fall back to English.

1. Copy `Editor/Localization/Languages/KoreanLanguagePack.cs` (e.g. to `JapaneseLanguagePack.cs`).
2. Rename the class and set `Code` (`"ja"`, `"zh-Hans"`, `"zh-Hant"`, ...), `DisplayName` (`"日本語"`, `"简体中文"`, ...)
   and `SystemLanguages` (`SystemLanguage.Japanese`, `SystemLanguage.ChineseSimplified`, ...).
3. Translate the values. Keys must stay in English.

Packs are discovered automatically: no other file has to be changed, and the language shows up in the settings dropdown.

## Layout

```
Editor/
├─ Core/          Project window hooks (drawing callbacks, reflection adapter)
├─ Rules/         Rule data model and ruleset asset
│  └─ ImportExport/  .rfset import / export
├─ Preferences/   Settings provider and editor preferences
├─ Localization/  Translation API
│  └─ Languages/  One language pack per file (English, Korean, ...)
├─ UI/            Shared UI Toolkit elements, FPFonts (UI font)
│  ├─ Windows/    Folder popup, texture picker
│  └─ Inspector/  Ruleset inspector, rule drawer
├─ Textures/      Built-in icons, backgrounds and editor textures (embedded PNGs, code only)
├─ Images/        Image assets of the UI (e.g. Close.png)
├─ Fonts/         UI font: subset of IBM Plex Sans KR (Regular, Bold) + OFL.txt
└─ Utility/       Logging, colors, asset helpers, package info
```

## License

- Code: Apache License 2.0 (`LICENSE.md`), based on Rainbow Folders 2 by Borodar.
- Built-in artwork from Rainbow Folders 2 (folder colors, tags, backgrounds, editor textures): CC BY-NC 4.0 —
  **not for commercial use**. The Type and Platform icons were redrawn by NK Studio.
- Font: subset of IBM Plex Sans KR, SIL Open Font License 1.1 (`Editor/Fonts/OFL.txt`).

See `Third Party Notices.md` for details.
