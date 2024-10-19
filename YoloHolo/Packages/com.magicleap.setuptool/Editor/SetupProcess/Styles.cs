#region

using System;
using UnityEditor;
using UnityEngine;

#endregion

namespace MagicLeap.SetupTool.Editor
{
    /// <summary>
    /// IMGUI Styles for editor window
    /// </summary>
    public static class Styles
    {
        public static  GUIStyle TitleStyleDefaultFont = new GUIStyle(EditorStyles.largeLabel)
                                                     {
                                                         alignment = TextAnchor.MiddleLeft,
                                                         fontSize = 16,
                                                         normal = { textColor = Color.white },
                                                         font = null,
                                                         padding = new RectOffset(2,0,0,-10)
                                                     };

        public static readonly GUIStyle HelpTitleStyle = new GUIStyle(EditorStyles.largeLabel)
            {fontStyle = FontStyle.Bold};

        public static readonly GUIStyle InfoTitleStyle = new GUIStyle(EditorStyles.helpBox)
            { fontSize = 12};

        public static readonly GUIStyle UrlLabel = new GUIStyle(EditorStyles.linkLabel)
        {
            name = "url-label", richText = true, alignment = TextAnchor.MiddleLeft
        };

        public static readonly GUIStyle LinkLabel = new GUIStyle(EditorStyles.linkLabel)
        {
            fontSize = EditorStyles.miniLabel.fontSize,
            wordWrap= true
        };
        public static readonly GUIStyle TextLabel = new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap= true
        };

        public static readonly GUIStyle FixButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fixedWidth = 100
        };

        public static class Utility
        {
            private static GUIStyle style = new GUIStyle();
            private static Texture2D texture = new Texture2D(1, 1);

            public static GUIStyle GetStyleWithColor(Color color)
            {
                texture.SetPixel(0, 0, color);
                texture.Apply();
                style.normal.background = texture;
                return style;
            }

            public static void SetColorForStyle(ref GUIStyle toChange, Color color)
            {
                texture.SetPixel(0, 0, color);
                texture.Apply();
                toChange.normal.background = texture;
            }
        }
    }
}