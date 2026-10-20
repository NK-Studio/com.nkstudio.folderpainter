# Folder Painter

Unity 프로젝트 창의 폴더에 색상과 아이콘을 입히는 에디터 확장입니다 (Unity 6.0 이상).
[Rainbow Folders 2](https://github.com/Borod4r/Rainbow-Folders-2)를 Unity 6으로 포팅하고, UI Toolkit 기반의 설정 팝업과
룰셋 인스펙터, 영어 / 한국어 현지화를 더했습니다.

📖 **문서**: [English](https://nk-studio.github.io/Packages/com.nkstudio.folderpainter@1.0/manual/index.html) · [한국어](https://nk-studio.github.io/kr/Packages/com.nkstudio.folderpainter@1.0/manual/index.html)

## 설치

Package Manager → **+** → **Install package from git URL...** 에 아래 주소를 입력합니다.

```
https://github.com/NK-Studio/com.nkstudio.folderpainter.git
```

## 사용법

- 편집 보조키(기본값 **Alt**)를 누른 채 폴더 아이콘을 클릭하면 설정 팝업이 열립니다.
- 규칙은 룰셋 에셋에 저장됩니다. 기본 위치는 `Assets/Settings/FolderPainter/FolderPainterRuleset.asset`입니다.
- 환경 설정: **Project Settings → NK Studio → Folder Painter**

## 언어 추가

UI 문구는 코드에 영어로 작성되어 있고, 이 영어 문구를 번역 키로 사용합니다. 번역이 없는 문구는 영어로 표시됩니다.

1. `Editor/Localization/Languages/KoreanLanguagePack.cs`를 복사합니다 (예: `JapaneseLanguagePack.cs`).
2. 클래스 이름을 바꾸고 `Code`(`"ja"`, `"zh-Hans"`, `"zh-Hant"` 등), `DisplayName`(`"日本語"`, `"简体中文"` 등),
   `SystemLanguages`(`SystemLanguage.Japanese`, `SystemLanguage.ChineseSimplified` 등)를 지정합니다.
3. 값을 번역합니다. 키는 영어 그대로 둡니다.

언어 팩은 자동으로 인식되므로 다른 파일은 수정할 필요가 없고, 설정 화면의 언어 목록에 바로 나타납니다.

## 폴더 구조

```
Editor/
├─ Core/          프로젝트 창 연동 (그리기 콜백, 리플렉션 어댑터)
├─ Rules/         규칙 데이터 모델과 룰셋 에셋
│  └─ ImportExport/  .rfset 가져오기 / 내보내기
├─ Preferences/   설정 화면과 에디터 환경 설정
├─ Localization/  번역 API
│  └─ Languages/  언어별 언어 팩 (영어, 한국어, ...)
├─ UI/            공용 UI Toolkit 요소, FPFonts (UI 폰트)
│  ├─ Windows/    폴더 설정 팝업, 텍스처 선택 창
│  └─ Inspector/  룰셋 인스펙터, 규칙 드로어
├─ Textures/      Type / Platform 아이콘(PNG), 코드로 그리는 색상·태그·투명 아이콘, 배경, 에디터 텍스처
├─ Images/        UI 이미지 에셋 (예: Close.png)
├─ Fonts/         UI 폰트: IBM Plex Sans KR 서브셋 (Regular, Bold) + OFL.txt
└─ Utility/       로그, 색상, 에셋 도우미, 패키지 정보
```

## 라이선스

- 코드: Apache License 2.0 (`LICENSE.md`). Borodar의 Rainbow Folders 2를 기반으로 합니다.
- 아트워크: Rainbow Folders 2의 아트워크(CC BY-NC 4.0)는 포함하지 않습니다. 색상·태그·투명 폴더 아이콘, 색상 배경,
  에디터 텍스처는 NK Studio가 코드로 그리고 (`ProceduralTextures.cs`, 자체 팔레트), Type과 Platform 아이콘은
  NK Studio가 그렸습니다. 코드와 같이 Apache License 2.0을 따르며 상업적으로 이용할 수 있습니다.
- 폰트: IBM Plex Sans KR 서브셋, SIL Open Font License 1.1 (`Editor/Fonts/OFL.txt`).

자세한 내용은 `Third Party Notices.md`를 참고하세요.
