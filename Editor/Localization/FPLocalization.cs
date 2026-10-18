using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NKStudio.FolderPainter
{
    /// <summary>
    /// Minimal editor localization. Texts are written in English in the code and translated through
    /// the <see cref="FPLanguagePack"/> of the current language; missing translations fall back to English.
    /// </summary>
    public static class FPLocalization
    {
        private const string LANGUAGE_PKEY = "NKStudio.FolderPainter.LanguageCode";
        // Before language packs, the language was stored as an enum index (0 = English, 1 = Korean)
        private const string LEGACY_LANGUAGE_PKEY = "NKStudio.FolderPainter.Language";
        private static readonly string[] LEGACY_LANGUAGE_CODES = {EnglishLanguagePack.CODE, "ko"};

        private static FPLanguagePack[] _languages;
        private static FPLanguagePack _language;

        public static event Action OnLanguageChanged;

        //---------------------------------------------------------------------
        // Properties
        //---------------------------------------------------------------------

        /// <summary>Every available language, English first.</summary>
        public static IReadOnlyList<FPLanguagePack> Languages => _languages ??= LoadLanguages();

        public static FPLanguagePack Language
        {
            get => _language ??= LoadSelectedLanguage();
            set
            {
                if (value == null || value == Language) return;

                _language = value;
                EditorPrefs.SetString(LANGUAGE_PKEY, value.Code);

                OnLanguageChanged?.Invoke();
                EditorApplication.RepaintProjectWindow();
                InternalEditorUtility.RepaintAllViews();
            }
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        /// <summary>Returns the translation of the given English text for the current language.</summary>
        public static string Tr(string english)
        {
            if (string.IsNullOrEmpty(english)) return english;
            return Language.TryTranslate(english, out var translated) ? translated : english;
        }

        public static string Tr(string englishFormat, params object[] args)
        {
            return string.Format(Tr(englishFormat), args);
        }

        /// <summary>Localized labels for a set of enum values (falls back to the enum name).</summary>
        public static string[] TrEnumNames(Type enumType)
        {
            var names = Enum.GetNames(enumType);
            for (var i = 0; i < names.Length; i++) names[i] = Tr(ObjectNames.NicifyVariableName(names[i]));
            return names;
        }

        public static FPLanguagePack FindLanguage(string code)
        {
            return Languages.FirstOrDefault(pack => string.Equals(pack.Code, code, StringComparison.OrdinalIgnoreCase));
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static FPLanguagePack[] LoadLanguages()
        {
            return TypeCache.GetTypesDerivedFrom<FPLanguagePack>()
                .Where(type => !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null)
                .Select(type => (FPLanguagePack) Activator.CreateInstance(type))
                .OrderBy(pack => pack.Code == EnglishLanguagePack.CODE ? 0 : 1)
                .ThenBy(pack => pack.Code, StringComparer.Ordinal)
                .ToArray();
        }

        private static FPLanguagePack LoadSelectedLanguage()
        {
            var code = EditorPrefs.GetString(LANGUAGE_PKEY, string.Empty);

            if (string.IsNullOrEmpty(code) && EditorPrefs.HasKey(LEGACY_LANGUAGE_PKEY))
            {
                var index = EditorPrefs.GetInt(LEGACY_LANGUAGE_PKEY, 0);
                if (index >= 0 && index < LEGACY_LANGUAGE_CODES.Length) code = LEGACY_LANGUAGE_CODES[index];
            }

            return FindLanguage(code) ?? GetSystemDefaultLanguage();
        }

        private static FPLanguagePack GetSystemDefaultLanguage()
        {
            var systemLanguage = Application.systemLanguage;
            return Languages.FirstOrDefault(pack => pack.SystemLanguages.Contains(systemLanguage))
                ?? FindLanguage(EnglishLanguagePack.CODE)
                ?? Languages[0];
        }
    }
}
