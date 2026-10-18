// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored.

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static NKStudio.FolderPainter.FPLogger;
using static NKStudio.FolderPainter.FPLocalization;

namespace NKStudio.FolderPainter
{
    /// <summary>
    /// Exports the active ruleset to a .rfset file, plus a .unitypackage with its custom textures.
    /// </summary>
    public static class ProjectRulesetExporter
    {
        private const string EXTENSION = "rfset";
        private const string UNDO_NAME = "Import Folder Painter Ruleset";

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public static void Import()
        {
            var ruleset = GetRulesetOrLogError();
            if (ruleset == null) return;

            var filePath = EditorUtility.OpenFilePanel(Tr("Import Ruleset"), "", EXTENSION);
            if (string.IsNullOrEmpty(filePath)) return;

            ImportCustomIcons(filePath);
            ImportRuleset(filePath, ruleset);
        }

        public static void Export()
        {
            var ruleset = GetRulesetOrLogError();
            if (ruleset == null) return;

            var dirPath = EditorUtility.SaveFolderPanel(Tr("Export Ruleset"), "", "");
            if (string.IsNullOrEmpty(dirPath)) return;

            var projectName = ProjectEditorUtility.ProjectName;
            var rulesetName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(ruleset));
            var fileName = $"{projectName}.{rulesetName}";

            ExportRuleset(ruleset, dirPath, fileName);
            ExportCustomIcons(ruleset, dirPath, fileName);
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static ProjectRuleset GetRulesetOrLogError()
        {
            var ruleset = ProjectRuleset.Instance;
            if (ruleset == null) LogError("There is no active ruleset.");
            return ruleset;
        }

        private static void ImportRuleset(string path, ProjectRuleset ruleset)
        {
            var wrapper = new ProjectRulesetWrapper();
            EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(path), wrapper);

            if (string.IsNullOrEmpty(wrapper.RulesetJson))
            {
                LogError($"Invalid ruleset file: {path}");
                return;
            }

            Undo.RecordObject(ruleset, UNDO_NAME);
            EditorJsonUtility.FromJsonOverwrite(wrapper.RulesetJson, ruleset);
            ruleset.SaveSetting();

            Log(Tr("Ruleset successfully imported."));
        }

        private static void ImportCustomIcons(string path)
        {
            var importDir = Path.GetDirectoryName(path);
            var importFileName = Path.GetFileNameWithoutExtension(path);
            var packagePath = $"{importDir}/{importFileName}.unitypackage";

            if (!File.Exists(packagePath))
            {
                Log(Tr("There is no package with custom icons. Skipping package import."));
                return;
            }

            AssetDatabase.ImportPackage(packagePath, true);
        }

        private static void ExportRuleset(ProjectRuleset ruleset, string dirPath, string fileName)
        {
            var wrapper = new ProjectRulesetWrapper {RulesetJson = EditorJsonUtility.ToJson(ruleset)};
            var filePath = $"{dirPath}/{fileName}.{EXTENSION}";
            File.WriteAllText(filePath, EditorJsonUtility.ToJson(wrapper, true));

            Log(Tr("Ruleset successfully exported."));
        }

        private static void ExportCustomIcons(ProjectRuleset ruleset, string dirPath, string fileName)
        {
            var rulesetPath = AssetDatabase.GetAssetPath(ruleset);
            var textureDependencies = AssetDatabase.GetDependencies(rulesetPath)
                .Where(dependency => AssetDatabase.GetMainAssetTypeAtPath(dependency) == typeof(Texture2D))
                .ToArray();

            if (textureDependencies.Length == 0)
            {
                Log(Tr("There is no custom icons in ruleset. Skipping package export."));
                return;
            }

            var packagePath = $"{dirPath}/{fileName}.unitypackage";
            AssetDatabase.ExportPackage(textureDependencies, packagePath);

            Log(Tr("Package with custom icons successfully exported."));
        }
    }
}
