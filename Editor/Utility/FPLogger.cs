// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored.

using UnityEngine;

namespace NKStudio.FolderPainter
{
    internal static class FPLogger
    {
        private const string TAG = "<b>[" + AssetInfo.NAME + "]</b>";

        public static void Log(string message)
        {
            Debug.Log($"{TAG} {message}");
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning($"{TAG} {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"{TAG} {message}");
        }
    }
}
