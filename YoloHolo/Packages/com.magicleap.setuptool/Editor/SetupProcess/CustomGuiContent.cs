#region

using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

#endregion

namespace MagicLeap.SetupTool.Editor
{
    /// <summary>
    /// IMGUI custom buttons and text to show in the setup window
    /// </summary>
    public static class CustomGuiContent
    {
        public static Texture GetIcon(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Info:
                    return EditorGUIUtility.IconContent("console.infoicon").image;
                case MessageType.Warning:
                    return EditorGUIUtility.IconContent("console.warnicon").image;
                case MessageType.Error:
                    return EditorGUIUtility.IconContent("console.erroricon").image;
                case MessageType.None:
                default:
                    return null;
            }
        }
        public static void DrawUILine(Color color, float thickness = 2, int padding = 10)
        {
            var r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2.0f;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }

        public static void DisplayLink(string text, string link, int leftMargin)
        {
            var textContent = new GUIContent(text);
            var websiteUri = new Uri(link);
            DisplayLink(textContent, websiteUri, leftMargin);
        }

        public static void DisplayLink(GUIContent text, Uri link, float leftMargin)
        {
            var labelStyle = Styles.UrlLabel;
            var size = labelStyle.CalcSize(text);

            var uriRect = GUILayoutUtility.GetRect(text, labelStyle);
            uriRect.x += leftMargin;
            uriRect.width = size.x;
            if (GUI.Button(uriRect, text, labelStyle)) Process.Start(link.AbsoluteUri);

            EditorGUIUtility.AddCursorRect(uriRect, MouseCursor.Link);
            EditorGUI.DrawRect(new Rect(uriRect.x, uriRect.y + uriRect.height - 1, uriRect.width, 1),
                labelStyle.normal.textColor);
        }

        public static void DisplayLink(GUIContent text, Uri link, int leftMargin, GUIStyle buttonStyle)
        {
            var size = buttonStyle.CalcSize(text);

            var uriRect = GUILayoutUtility.GetRect(text, buttonStyle);
            uriRect.x += leftMargin;
            uriRect.width = size.x;
            if (GUI.Button(uriRect, text, buttonStyle)) Process.Start(link.AbsoluteUri);

            EditorGUIUtility.AddCursorRect(uriRect, MouseCursor.Link);
        }

        public static void DisplayLabel(GUIContent text,  int leftMargin, GUIStyle buttonStyle)
        {
            var size = buttonStyle.CalcSize(text);

            var labelRect = GUILayoutUtility.GetRect(text, buttonStyle);
            labelRect.x += leftMargin;
            labelRect.width = size.x;
            GUI.Label(labelRect, text, buttonStyle);
        }
 

        public static void DisplayHoverButtonLink(Uri link)
        {
            var content = new GUIContent(EditorGUIUtility.IconContent("_Help"));

            var con = new GUIStyle(EditorStyles.miniButtonRight);
            var text = content.image as Texture2D;
            con.normal.background = text;

            con.normal.background = text;

            con.active.background = text;


            if (GUILayout.Button(content, con, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false)))
                Process.Start(link.AbsoluteUri);
        }

        public static void HelpBox(string message, string link, MessageType messageType, Action linkClicked)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(GetIcon(messageType), GUILayout.ExpandWidth(false));
            GUILayout.BeginVertical();
            GUILayout.Label(message, Styles.TextLabel);
            if (GUILayout.Button(link, Styles.LinkLabel))
                linkClicked?.Invoke();
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
        public static class CustomButtons
        {
            public static bool DrawConditionButton(string label, bool condition, string conditionMetText,
                string conditionMissingText, GUIStyle buttonStyle, string helpText, string url,
                bool disableOnConditionMet = true)
            {
                return DrawConditionButton(label, condition, new GUIContent(conditionMetText), conditionMissingText,
                    buttonStyle, helpText, url, disableOnConditionMet);
            }

            public static bool DrawConditionButton(string label, bool condition, GUIContent conditionMetText,
                string conditionMissingText, GUIStyle buttonStyle, string helpText, string url,
                bool disableOnConditionMet = true)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = Color.clear;

                var linkContent = new GUIContent(EditorGUIUtility.IconContent("_Help").image, helpText);
                GUI.backgroundColor = Color.white;
                GUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
                if (!string.IsNullOrEmpty(url))
                {

                    DisplayLink(linkContent, new Uri(url), 0, EditorStyles.boldLabel);
                }
                else
                {
                    DisplayLabel(linkContent, 0, EditorStyles.boldLabel);
                }
                var lastEnabledGUIState = GUI.enabled;
                var returnValue = false;
                GUILayout.FlexibleSpace();
                if (condition)
                {
                    GUI.backgroundColor = Color.green;

                    if (disableOnConditionMet) GUI.enabled = false;
                    if (GUILayout.Button(conditionMetText, buttonStyle)) returnValue = true;

                    if (disableOnConditionMet) GUI.enabled = lastEnabledGUIState;
                }
                else
                {
                    GUI.backgroundColor = Color.yellow;
                    if (GUILayout.Button(conditionMissingText, buttonStyle)) return true;
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.Space(5);

                GUI.backgroundColor = Color.white;
                return returnValue;
            }

            public static bool DrawConditionButton(string label, bool condition, string conditionMetText,
                string conditionMissingText, GUIStyle buttonStyle, bool disableOnConditionMet = true,
                string conditionMetTooltip = "", GUIStyle groupStyle = null, Color? conditionMetColor = null,
                Color? conditionMissingColor = null)
            {
                return DrawConditionButton(new GUIContent(label), condition,
                                           new GUIContent(conditionMetText, conditionMetTooltip), new GUIContent(conditionMissingText),
                                           buttonStyle, disableOnConditionMet, groupStyle, conditionMetColor, conditionMissingColor);
            }

            public static bool DrawConditionButton(string label, bool condition, string conditionMetText,
                                                   string conditionMissingText, GUIStyle buttonStyle, bool disableOnConditionMet,
                                                   string conditionMetTooltip, string conditionMissingTooltip, GUIStyle groupStyle = null, Color? conditionMetColor = null,
                                                   Color? conditionMissingColor = null)
            {
                return DrawConditionButton(new GUIContent(label), condition,
                                           new GUIContent(conditionMetText, conditionMetTooltip), new GUIContent(conditionMissingText, conditionMissingTooltip),
                                           buttonStyle, disableOnConditionMet, groupStyle, conditionMetColor, conditionMissingColor);
            }
            
            public static bool DrawButton(GUIContent label,  GUIContent button, GUIStyle buttonStyle, GUIStyle groupStyle = null, Color? color = null)
            {
                if (groupStyle == null) groupStyle = EditorStyles.helpBox;
                var returnValue = false;
                GUILayout.BeginHorizontal(groupStyle);
                {
                    EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
                    GUILayout.FlexibleSpace();
                   
                        GUI.backgroundColor = color ?? Color.green;
                        if (GUILayout.Button(button, buttonStyle)) returnValue = true;

                
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                GUI.backgroundColor = Color.white;
                return returnValue;
            }

            public static bool DrawConditionButton(GUIContent label, bool condition, GUIContent conditionMetText,
                GUIContent conditionMissingText, GUIStyle buttonStyle, bool disableOnConditionMet = true,
                GUIStyle groupStyle = null, Color? conditionMetColor = null, Color? conditionMissingColor = null)
            {
                if (groupStyle == null) groupStyle = EditorStyles.helpBox;

                var lastEnabledGUIState = GUI.enabled;
                var returnValue = false;
                GUILayout.BeginHorizontal(groupStyle);
                {
                    EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
                    GUILayout.FlexibleSpace();
                    if (condition)
                    {
                        GUI.backgroundColor = conditionMetColor ?? Color.green;
                        if (disableOnConditionMet) GUI.enabled = false;

                        if (GUILayout.Button(conditionMetText, buttonStyle)) returnValue = true;

                        if (disableOnConditionMet) GUI.enabled = lastEnabledGUIState;
                    }
                    else
                    {
                        GUI.backgroundColor = conditionMissingColor ?? Color.yellow;
                        if (GUILayout.Button(conditionMissingText, buttonStyle)) returnValue = true;
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                GUI.backgroundColor = Color.white;
                return returnValue;
            }
        }
    }
}