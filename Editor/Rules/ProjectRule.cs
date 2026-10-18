// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored.

using System;
using JetBrains.Annotations;
using UnityEngine;

namespace NKStudio.FolderPainter
{
    [Serializable]
    public class ProjectRule
    {
        // Field names are part of the serialized asset format: do not rename them
        public KeyType Type;
        public string Key;

        public int Ordinal;
        public int Priority;

        public ProjectIcon IconType;
        public Texture2D SmallIcon;
        public Texture2D LargeIcon;
        public bool IsIconRecursive;

        public ProjectBackground BackgroundType;
        public Texture2D BackgroundTexture;
        public bool IsBackgroundRecursive;

        [UsedImplicitly]
        public bool IsHidden;

        //---------------------------------------------------------------------
        // Ctors
        //---------------------------------------------------------------------

        public ProjectRule(ProjectRule value)
        {
            CopyFrom(value);
        }

        public ProjectRule(KeyType type, string key)
        {
            Type = type;
            Key = key;
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public void CopyFrom(ProjectRule target)
        {
            Type = target.Type;
            Key = target.Key;
            Ordinal = target.Ordinal;
            CopyStyleFrom(target);
        }

        /// <summary>
        /// Copies everything that defines how a folder looks (icon, background, priority),
        /// but keeps the key this rule matches.
        /// </summary>
        public void CopyStyleFrom(ProjectRule target)
        {
            Priority = target.Priority;

            IconType = target.IconType;
            SmallIcon = target.SmallIcon;
            LargeIcon = target.LargeIcon;
            IsIconRecursive = target.IsIconRecursive;

            BackgroundType = target.BackgroundType;
            BackgroundTexture = target.BackgroundTexture;
            IsBackgroundRecursive = target.IsBackgroundRecursive;
        }

        public void ResetStyle()
        {
            CopyStyleFrom(new ProjectRule(Type, Key));
        }

        public bool HasIcon()
        {
            return IconType != ProjectIcon.None && (!HasCustomIcon() || SmallIcon != null || LargeIcon != null);
        }

        public bool HasCustomIcon()
        {
            return IconType == ProjectIcon.Custom;
        }

        public bool HasBackground()
        {
            return BackgroundType != ProjectBackground.None && (!HasCustomBackground() || BackgroundTexture != null);
        }

        public bool HasCustomBackground()
        {
            return BackgroundType == ProjectBackground.Custom;
        }

        public bool HasAtLeastOneTexture()
        {
            return HasIcon() || HasBackground();
        }

        public bool IsRecursive()
        {
            return IsIconRecursive || IsBackgroundRecursive;
        }

        /// <summary>Icon to draw for this rule, or null when the rule has no (valid) icon.</summary>
        public Texture2D GetIconTexture(bool small)
        {
            return ResolveIcon(IconType, SmallIcon, LargeIcon, small);
        }

        /// <summary>Background to draw for this rule, or null when the rule has no (valid) background.</summary>
        public Texture2D GetBackgroundTexture()
        {
            return ResolveBackground(BackgroundType, BackgroundTexture);
        }

        //---------------------------------------------------------------------
        // Static
        //---------------------------------------------------------------------

        public static Texture2D ResolveIcon(ProjectIcon type, Texture2D customSmall, Texture2D customLarge, bool small)
        {
            switch (type)
            {
                case ProjectIcon.None: return null;
                case ProjectIcon.Custom: return small ? customSmall : customLarge;
                default: return ProjectIconsStorage.GetIcon(type, small);
            }
        }

        public static Texture2D ResolveBackground(ProjectBackground type, Texture2D custom)
        {
            switch (type)
            {
                case ProjectBackground.None: return null;
                case ProjectBackground.Custom: return custom;
                default: return ProjectBackgroundsStorage.GetBackground(type);
            }
        }

        /// <summary>
        /// Whether a rule with the given key would affect the folder at <paramref name="folderPath"/>.
        /// Recursive rules also affect every subfolder of a matching folder.
        /// </summary>
        public static bool Matches(KeyType type, string key, bool isRecursive, string folderPath)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(folderPath)) return false;

            switch (type)
            {
                case KeyType.Name:
                    return key == GetFolderName(folderPath)
                        || isRecursive && folderPath.IndexOf(GetRecursiveNamePattern(key), StringComparison.Ordinal) >= 0;
                case KeyType.Path:
                    return key == folderPath
                        || isRecursive && folderPath.StartsWith(GetRecursivePathPattern(key), StringComparison.Ordinal);
                default:
                    return false;
            }
        }

        /// <summary>A recursive name rule applies to a folder when one of its ancestors has this name.</summary>
        internal static string GetRecursiveNamePattern(string key) => "/" + key + "/";

        /// <summary>A recursive path rule applies to a folder when it lives below this path.</summary>
        internal static string GetRecursivePathPattern(string key) => key + "/";

        internal static string GetFolderName(string path)
        {
            return path.Substring(path.LastIndexOf('/') + 1);
        }

        //---------------------------------------------------------------------
        // Nested
        //---------------------------------------------------------------------

        public enum KeyType
        {
            Name,
            Path
        }
    }
}
