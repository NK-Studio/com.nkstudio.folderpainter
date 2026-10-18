// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored.

using UnityEditor;
using UnityEngine;

namespace NKStudio.FolderPainter
{
    public static class ColorHelper
    {
        private static readonly Color POPUP_BORDER_CLR_FREE = new Color(0.51f, 0.51f, 0.51f);
        private static readonly Color POPUP_BORDER_CLR_PRO = new Color(0.13f, 0.13f, 0.13f);

        private static readonly Color POPUP_BACKGROUND_CLR_FREE = new Color(0.83f, 0.83f, 0.83f);
        private static readonly Color POPUP_BACKGROUND_CLR_PRO = new Color(0.18f, 0.18f, 0.18f);

        private static readonly Color SEPARATOR_CLR_FREE = new Color(0.65f, 0.65f, 0.65f, 1f);
        private static readonly Color SEPARATOR_CLR_PRO = new Color(0.13f, 0.13f, 0.13f, 1f);

        private static readonly Color BG_COLOR_FREE = new Color(0.7607f, 0.7607f, 0.7607f);
        private static readonly Color BG_COLOR_PRO = new Color(0.2196f, 0.2196f, 0.2196f);

        private static readonly Color SELECTION_COLOR_FREE = new Color(0.2275f, 0.4471f, 0.6902f);
        private static readonly Color SELECTION_COLOR_PRO = new Color(0.1725f, 0.3647f, 0.5294f);

        private static readonly Color SELECTION_GRAY_COLOR_FREE = new Color(0.6824f, 0.6824f, 0.6824f);
        private static readonly Color SELECTION_GRAY_COLOR_PRO = new Color(0.302f, 0.302f, 0.302f);

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public static Color PopupBorderColor => Skin(POPUP_BORDER_CLR_PRO, POPUP_BORDER_CLR_FREE);
        public static Color PopupBackgroundColor => Skin(POPUP_BACKGROUND_CLR_PRO, POPUP_BACKGROUND_CLR_FREE);
        public static Color SeparatorColor => Skin(SEPARATOR_CLR_PRO, SEPARATOR_CLR_FREE);

        /// <summary>Background of a project window row, used to hide the default folder icon.</summary>
        public static Color GetBackgroundColor()
        {
            return Skin(BG_COLOR_PRO, BG_COLOR_FREE);
        }

        public static Color GetSelectionColor(bool focused)
        {
            return focused
                ? Skin(SELECTION_COLOR_PRO, SELECTION_COLOR_FREE)
                : Skin(SELECTION_GRAY_COLOR_PRO, SELECTION_GRAY_COLOR_FREE);
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static Color Skin(Color pro, Color free)
        {
            return EditorGUIUtility.isProSkin ? pro : free;
        }
    }
}
