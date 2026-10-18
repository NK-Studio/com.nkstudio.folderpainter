// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored.

using System;

namespace NKStudio.FolderPainter
{
    /// <summary>File format of exported rulesets (.rfset).</summary>
    [Serializable]
    internal sealed class ProjectRulesetWrapper
    {
        #pragma warning disable 414
        public int Version = ProjectRuleset.VERSION;
        #pragma warning restore 414

        public string RulesetJson;
    }
}
