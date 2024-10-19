#region

using System.IO;
using UnityEditor;
using UnityEngine;

#endregion

namespace MagicLeap.SetupTool.Editor.Utilities
{
    public static class EditorKeyUtility
    {
        private const string MAGIC_LEAP_SETUP_POSTFIX_KEY = "MAGIC_LEAP_SETUP_KEY";

        private const string MAGIC_LEAP_SETUP_CLOSED_WINDOW_KEY = "MAGIC_LEAP_SETUP_CLOSED_WINDOW_KEY";
        private const string MAGIC_LEAP_SETUP_USE_OPEN_XR_KEY = "MAGIC_LEAP_SETUP_USE_OPEN_XR_KEY";

        public static string ProjectKeyAndPath
        {
            get
            {
                var projectKey = GetProjectKey();
                var path = Path.GetFullPath(Application.dataPath);
                return $"[{projectKey}]-[{path}]";
            }
        }

        public static string UseOpenXREditorPrefKey => $"{MAGIC_LEAP_SETUP_USE_OPEN_XR_KEY}_{ProjectKeyAndPath}";
        public static string WindowClosedEditorPrefKey => $"{MAGIC_LEAP_SETUP_CLOSED_WINDOW_KEY}_{ProjectKeyAndPath}";
        public static string AutoShowEditorPrefKey => $"{MAGIC_LEAP_SETUP_POSTFIX_KEY}_{ProjectKeyAndPath}";

        public static string GetProjectKey()
        {
            return PlayerSettings.companyName + "." + PlayerSettings.productName;
        }
    }
}