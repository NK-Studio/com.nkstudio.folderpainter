using System.Collections.Generic;

namespace NKStudio.FolderPainter
{
    /// <summary>Source language: texts are written in English in the code, so nothing has to be translated.</summary>
    internal sealed class EnglishLanguagePack : FPLanguagePack
    {
        public const string CODE = "en";

        private static readonly Dictionary<string, string> TRANSLATIONS = new Dictionary<string, string>();

        public override string Code => CODE;
        public override string DisplayName => "English";
        public override IReadOnlyDictionary<string, string> Translations => TRANSLATIONS;
    }
}
