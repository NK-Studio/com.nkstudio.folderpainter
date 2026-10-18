using System;
using System.Collections.Generic;
using UnityEngine;

namespace NKStudio.FolderPainter
{
    /// <summary>
    /// A language Folder Painter can be displayed in.
    /// To add a language, create a class deriving from this one in <c>Localization/Languages</c>:
    /// it is found automatically and shows up in the language dropdown of the settings page.
    /// </summary>
    /// <remarks>
    /// English text is used as the key, so a missing translation simply falls back to English.
    /// </remarks>
    public abstract class FPLanguagePack
    {
        /// <summary>Stable identifier stored in the preferences, e.g. "ko", "ja", "zh-Hans".</summary>
        public abstract string Code { get; }

        /// <summary>Name of the language written in that language, e.g. "日本語".</summary>
        public abstract string DisplayName { get; }

        /// <summary>Editor languages that select this pack by default, before the user picks one.</summary>
        public virtual IReadOnlyList<SystemLanguage> SystemLanguages => Array.Empty<SystemLanguage>();

        /// <summary>English text → translated text.</summary>
        public abstract IReadOnlyDictionary<string, string> Translations { get; }

        public bool TryTranslate(string english, out string translated)
        {
            return Translations.TryGetValue(english, out translated) && !string.IsNullOrEmpty(translated);
        }

        public override string ToString() => DisplayName;
    }
}
