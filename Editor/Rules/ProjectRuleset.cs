// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using static NKStudio.FolderPainter.FPLogger;
using KeyType = NKStudio.FolderPainter.ProjectRule.KeyType;

namespace NKStudio.FolderPainter
{
    [HelpURL(AssetInfo.HELP_URL)]
    public class ProjectRuleset : ScriptableObject
    {
        public const int VERSION = 1;

        private const double INSTANCE_CHECK_INTERVAL = 1d;
        private const string UNDO_GROUP_NAME = "Modify Folder Painter Ruleset";
        private const string DEFAULT_ASSET_NAME = "FolderPainterRuleset";

        [FormerlySerializedAs("Folders")]
        public List<ProjectRule> Rules = new List<ProjectRule>();

        // Lookup caches, rebuilt every time the ruleset changes
        private readonly Dictionary<string, ProjectRule> _rulesByName = new Dictionary<string, ProjectRule>();
        private readonly Dictionary<string, ProjectRule> _rulesByPath = new Dictionary<string, ProjectRule>();
        private readonly List<RecursiveRule> _recursiveRulesByName = new List<RecursiveRule>();
        private readonly List<RecursiveRule> _recursiveRulesByPath = new List<RecursiveRule>();

        // Result of a recursive match, reused to avoid allocations while the project window is drawn
        private readonly ProjectRule _recursiveResult = new ProjectRule(KeyType.Name, string.Empty);

        private static ProjectRuleset _instance;
        private static double _nextInstanceCheck;

        //---------------------------------------------------------------------
        // Events
        //---------------------------------------------------------------------

        public static event Action OnRulesetChange;

        //---------------------------------------------------------------------
        // Instance
        //---------------------------------------------------------------------

        public static ProjectRuleset Instance
        {
            get
            {
                if (_instance == null && IsReadyForInstanceCheck()) UpdateInstance();
                return _instance;
            }
        }

        //---------------------------------------------------------------------
        // Messages
        //---------------------------------------------------------------------

        [InitializeOnLoadMethod]
        private static void SubscribeToUndo()
        {
            // Covers undo of changes made both from the popup and from the inspector
            Undo.undoRedoPerformed += NotifyRulesetChanged;
        }

        // Please note that this could be called multiple times without corresponding OnDisable call
        // For example, when renaming scriptable object in Project Browser
        private void OnEnable()
        {
            NotifyRulesetChanged();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public static void UpdateInstance()
        {
            _instance = LoadRulesetFromAsset();
            if (_instance != null) _instance.RebuildCache();
        }

        /// <summary>
        /// Must be called after the active ruleset was modified, so lookups and the project window are refreshed.
        /// </summary>
        public static void NotifyRulesetChanged()
        {
            if (_instance != null) _instance.RebuildCache();
            OnRulesetChange?.Invoke();
        }

        public static void ShowInspector(DefaultAsset asset = null)
        {
            Selection.activeObject = Instance;
            // Workaround with double delay to make sure all Inspectors already enabled
            EditorApplication.delayCall += () => EditorApplication.delayCall += () =>
            {
                foreach (var editor in ProjectRulesetEditor.EDITORS)
                {
                    editor.ShowRulesForFolder(asset);
                }
            };
        }

        /// <summary>
        /// Searches for a rule that has the same type and key values.
        /// Returns the first occurrence within the settings, if found; null otherwise.
        /// </summary>
        public ProjectRule GetRule(ProjectRule match)
        {
            if (match == null || IsEmpty()) return null;
            return Rules.Find(x => IsSameKey(x, match));
        }

        /// <summary>
        /// Searches for a rule that should be applied for the specified path (regardless of the key type).
        /// When several rules match, the one with the highest priority wins; on a tie the last one wins.
        /// The returned rule must not be stored: recursive matches reuse the same instance.
        /// </summary>
        public ProjectRule GetRuleByPath(string folderPath, bool allowRecursive = false)
        {
            if (IsEmpty() || string.IsNullOrEmpty(folderPath)) return null;

            ProjectRule result = null;

            if (allowRecursive)
            {
                foreach (var recursive in _recursiveRulesByName)
                {
                    if (folderPath.IndexOf(recursive.Pattern, StringComparison.Ordinal) >= 0)
                        result = PickRecursive(result, recursive.Rule);
                }
            }

            _rulesByName.TryGetValue(ProjectRule.GetFolderName(folderPath), out var ruleByName);
            result = Pick(result, ruleByName);

            if (allowRecursive)
            {
                foreach (var recursive in _recursiveRulesByPath)
                {
                    if (folderPath.StartsWith(recursive.Pattern, StringComparison.Ordinal))
                        result = PickRecursive(result, recursive.Rule);
                }
            }

            _rulesByPath.TryGetValue(folderPath, out var ruleByPath);
            return Pick(result, ruleByPath);
        }

        /// <summary>
        /// Searches for a rule that has the same type and key, and updates its other fields with
        /// provided value, if found; creates a new rule otherwise. Rules without any texture are removed.
        /// </summary>
        public void UpdateRule(ProjectRule match, ProjectRule value)
        {
            Undo.RecordObject(this, UNDO_GROUP_NAME);

            var existingRule = GetRule(match);
            if (!value.HasAtLeastOneTexture())
            {
                if (existingRule != null) RemoveAll(match);
                return;
            }

            if (existingRule != null)
            {
                existingRule.CopyFrom(value);
                SaveSetting();
            }
            else
            {
                AddRule(value);
            }
        }

        public void AddRule(ProjectRule value)
        {
            Rules.Add(new ProjectRule(value));
            SaveSetting();
        }

        public void RemoveAll(ProjectRule match)
        {
            if (match == null) return;

            Undo.RecordObject(this, UNDO_GROUP_NAME);
            Rules.RemoveAll(x => IsSameKey(x, match));
            SaveSetting();
        }

        public void SaveSetting()
        {
            EditorUtility.SetDirty(this);
            NotifyRulesetChanged();
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static bool IsReadyForInstanceCheck()
        {
            if (EditorApplication.timeSinceStartup <= _nextInstanceCheck) return false;

            _nextInstanceCheck = EditorApplication.timeSinceStartup + INSTANCE_CHECK_INTERVAL;
            return true;
        }

        private static ProjectRuleset LoadRulesetFromAsset()
        {
            var assetPath = ProjectPreferences.RulesetPath;
            var ruleset = AssetDatabase.LoadAssetAtPath<ProjectRuleset>(assetPath);
            if (ruleset) return ruleset;

            LogWarning($"There is no ruleset at path: {assetPath}\n" +
                       "Trying to find another ruleset in project... ");

            assetPath = ProjectEditorUtility.FindPathsForAllRulesets().FirstOrDefault();
            ruleset = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<ProjectRuleset>(assetPath);

            if (ruleset)
            {
                Log($"Found another ruleset at path: {assetPath}\n" +
                    $"You could select a different one in \"Project Settings → {ProjectPreferences.SETTINGS_PATH} → Current Ruleset\"");

                ProjectPreferences.UpdateRulesetPath(assetPath, true, false);
                return ruleset;
            }

            var defaultFolder = Path.GetDirectoryName(ProjectPreferences.RULESET_DEFAULT)?.Replace('\\', '/');
            assetPath = ProjectEditorUtility.CreateAsset<ProjectRuleset>(defaultFolder, DEFAULT_ASSET_NAME);
            ruleset = AssetDatabase.LoadAssetAtPath<ProjectRuleset>(assetPath);

            if (ruleset)
            {
                Log($"No rulesets found. Created new ruleset at path: {assetPath}");

                ProjectPreferences.UpdateRulesetPath(assetPath, true, false);
                return ruleset;
            }

            LogError("Could not find any ruleset in the project.\n" +
                     "Please make sure that your project contains at least one ruleset and it has been imported correctly.");

            return null;
        }

        private void RebuildCache()
        {
            _rulesByName.Clear();
            _rulesByPath.Clear();
            _recursiveRulesByName.Clear();
            _recursiveRulesByPath.Clear();

            if (Rules == null) return;

            for (var i = 0; i < Rules.Count; i++)
            {
                var rule = Rules[i];
                if (rule == null) continue;

                rule.Ordinal = i;
                if (string.IsNullOrEmpty(rule.Key)) continue;

                switch (rule.Type)
                {
                    case KeyType.Name:
                        AddToLookup(_rulesByName, rule);
                        if (rule.IsRecursive())
                            _recursiveRulesByName.Add(new RecursiveRule(rule, ProjectRule.GetRecursiveNamePattern(rule.Key)));
                        break;

                    case KeyType.Path:
                        AddToLookup(_rulesByPath, rule);
                        if (rule.IsRecursive())
                            _recursiveRulesByPath.Add(new RecursiveRule(rule, ProjectRule.GetRecursivePathPattern(rule.Key)));
                        break;
                }
            }
        }

        private static void AddToLookup(Dictionary<string, ProjectRule> lookup, ProjectRule rule)
        {
            lookup.TryGetValue(rule.Key, out var current);
            lookup[rule.Key] = Pick(current, rule);
        }

        private static ProjectRule Pick(ProjectRule current, ProjectRule candidate)
        {
            return Outranks(candidate, current) ? candidate : current;
        }

        /// <summary>
        /// Same as <see cref="Pick"/>, but an inherited rule only passes on the parts marked as recursive.
        /// </summary>
        private ProjectRule PickRecursive(ProjectRule current, ProjectRule candidate)
        {
            if (!Outranks(candidate, current)) return current;

            _recursiveResult.CopyFrom(candidate);
            if (!candidate.IsIconRecursive) _recursiveResult.IconType = ProjectIcon.None;
            if (!candidate.IsBackgroundRecursive) _recursiveResult.BackgroundType = ProjectBackground.None;

            return _recursiveResult;
        }

        private static bool Outranks(ProjectRule candidate, ProjectRule current)
        {
            if (candidate == null) return false;
            if (current == null) return true;
            if (candidate.Priority != current.Priority) return candidate.Priority > current.Priority;
            return candidate.Ordinal > current.Ordinal;
        }

        private static bool IsSameKey(ProjectRule a, ProjectRule b)
        {
            return a.Type == b.Type && a.Key == b.Key;
        }

        private bool IsEmpty()
        {
            return Rules == null || Rules.Count == 0;
        }

        //---------------------------------------------------------------------
        // Nested
        //---------------------------------------------------------------------

        private readonly struct RecursiveRule
        {
            public readonly ProjectRule Rule;
            public readonly string Pattern;

            public RecursiveRule(ProjectRule rule, string pattern)
            {
                Rule = rule;
                Pattern = pattern;
            }
        }
    }
}
