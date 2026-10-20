using System.Collections.Generic;
using UnityEngine;

namespace NKStudio.FolderPainter
{
    internal sealed class KoreanLanguagePack : FPLanguagePack
    {
        private static readonly SystemLanguage[] SYSTEM_LANGUAGES = {SystemLanguage.Korean};

        private static readonly Dictionary<string, string> TRANSLATIONS = new Dictionary<string, string>
        {
            // Preferences
            {"Language", "언어"},
            {"Language used by Folder Painter windows and dialogs.", "Folder Painter 창과 대화상자에 사용할 언어입니다."},
            {"General", "일반"},
            {"Current Ruleset", "현재 룰셋"},
            {"The ruleset that is currently used. You could have multiple rulesets in your project and switch between them using this option.", "현재 사용 중인 룰셋입니다. 프로젝트에 룰셋을 여러 개 두고 이 옵션으로 전환할 수 있습니다."},
            {"Edit Modifier", "편집 보조키"},
            {"Modifier key that is used to show configuration dialogue when clicking on a folder icon.", "폴더 아이콘을 클릭할 때 설정 창을 띄우는 보조키입니다."},
            {"Enhancements", "부가 기능"},
            {"Project Tree", "프로젝트 트리"},
            {"Change this setting to show/hide the \"branches\" in the project window.", "프로젝트 창의 트리 \"가지\" 선을 표시하거나 숨깁니다."},
            {"Row Shading", "행 음영"},
            {"Change this settings to enable/disable row shading in the project window.", "프로젝트 창의 줄무늬 음영을 켜거나 끕니다."},
            {"Import / Export Ruleset", "룰셋 가져오기 / 내보내기"},
            {"Import", "가져오기"},
            {"Export", "내보내기"},
            {"Version", "버전"},
            {"Documentation", "문서"},
            {"No ruleset found in the project.", "프로젝트에서 룰셋을 찾을 수 없습니다."},

            // Popups
            {"Priority", "우선순위"},
            {"Rules with higher priority override lower ones", "우선순위가 높은 규칙이 낮은 규칙을 덮어씁니다"},
            {"Icon", "아이콘"},
            {"Background", "배경"},
            {"Recursive", "하위 포함"},
            {"Icon recursive", "아이콘 하위 포함"},
            {"BG recursive", "배경 하위 포함"},
            {"Apply the icon to all subfolders as well", "모든 하위 폴더에도 아이콘을 적용합니다"},
            {"Apply the background to all subfolders as well", "모든 하위 폴더에도 배경을 적용합니다"},
            {"Match folders by name or by full path", "폴더를 이름 또는 전체 경로로 매칭합니다"},
            {"Folder", "폴더"},
            {"{0} folders", "폴더 {0}개"},
            {"Close (Esc)", "닫기 (Esc)"},
            {"Filter by name", "이름으로 검색"},
            {"Icon category", "아이콘 카테고리"},
            {"Nothing found", "검색 결과 없음"},
            {"Recent", "최근"},
            {"None", "없음"},
            {"Custom", "사용자 지정"},
            {"All Rules", "모든 규칙"},
            {"Folder Rules", "이 폴더 규칙"},
            {"Revert to Default", "기본값으로 되돌리기"},
            {"Copy", "복사"},
            {"Paste", "붙여넣기"},
            {"Copy this style (icon, background, priority)", "이 스타일 복사 (아이콘, 배경, 우선순위)"},
            {"Paste a previously copied style", "복사한 스타일 붙여넣기"},
            {"Cancel", "취소"},
            {"Apply", "적용"},

            // Icon categories
            {"All", "전체"},
            {"Colors", "색상"},
            {"Transparent", "투명"},
            {"Tags", "태그"},
            {"Types", "유형"},
            {"Types Bg", "유형 (배경)"},
            {"Platforms", "플랫폼"},
            {"Platforms Bg", "플랫폼 (배경)"},

            // Icon name suffixes, e.g. "Red (Tag)"
            {"Tag", "태그"},
            {"BG", "배경"},

            // Key types
            {"Name", "이름"},
            {"Path", "경로"},

            // Ruleset inspector
            {"Search Results", "검색 결과"},
            {"Filter by folder", "폴더로 필터"},
            {"Filter by key", "키로 필터"},
            {"Match case", "대소문자 구분"},
            {"Regex", "정규식"},

            // Import / export
            {"Import Ruleset", "룰셋 가져오기"},
            {"Export Ruleset", "룰셋 내보내기"},
            {"Ruleset successfully imported.", "룰셋을 가져왔습니다."},
            {"There is no package with custom icons. Skipping package import.", "사용자 지정 아이콘 패키지가 없어 패키지 가져오기를 건너뜁니다."},
            {"Ruleset successfully exported.", "룰셋을 내보냈습니다."},
            {"There is no custom icons in ruleset. Skipping package export.", "룰셋에 사용자 지정 아이콘이 없어 패키지 내보내기를 건너뜁니다."},
            {"Package with custom icons successfully exported.", "사용자 지정 아이콘 패키지를 내보냈습니다."},
        };

        public override string Code => "ko";
        public override string DisplayName => "한국어";
        public override IReadOnlyList<SystemLanguage> SystemLanguages => SYSTEM_LANGUAGES;
        public override IReadOnlyDictionary<string, string> Translations => TRANSLATIONS;
    }
}
