# Third Party Notices

Folder Painter contains third-party material, provided under the licenses below.

## Rainbow Folders 2

- Author: Borodar — https://github.com/Borod4r/Rainbow-Folders-2
- Copyright © Borodar

### Code — Apache License 2.0

The C# code of this package is a modified version of the Rainbow Folders 2 code, licensed under the
Apache License 2.0 (see `LICENSE.md`, or https://www.apache.org/licenses/LICENSE-2.0).
Files based on it carry a notice at their top. Main changes made by NK Studio:

- ported to Unity 6 (EntityId tree views) and refactored;
- UI Toolkit configuration popup, texture picker, ruleset inspector and settings page;
- English / Korean localization with pluggable language packs;
- rule lookup fixes and performance improvements (see `CHANGELOG.md`).

### Artwork

Folder Painter contains no Rainbow Folders 2 artwork (which is licensed under CC BY-NC 4.0):

- the color (`Clr*`), tag (`Tag*`) and transparent (`Transparent*`) folder icons, the color row backgrounds and
  the editor textures (folder edit icon, preview gradients, project tree lines, filter / delete / settings buttons)
  are drawn in code by NK Studio (`Editor/Textures/ProceduralTextures.cs`) with its own palette;
- the Type (`Tpe*`, `TpeBg*`) and Platform (`Pfm*`, `PfmBg*`) icons were drawn by NK Studio;
- the Rainbow Folders logo icon (`TpeRainbow`) was removed.

This artwork is part of the package and covered by the Apache License 2.0, like the code.

## IBM Plex Sans KR

- Copyright © 2017 IBM Corp. with Reserved Font Name "Plex"
- Licensed under the SIL Open Font License 1.1 (see `Editor/Fonts/OFL.txt`, or https://openfontlicense.org)

`Editor/Fonts/FolderPainterSansKR-*.ttf` is a subset of IBM Plex Sans KR (Regular, Bold), renamed
"Folder Painter Sans KR" as required for Modified Versions under the Reserved Font Name.
