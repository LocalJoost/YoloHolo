using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MagicLeap.SetupTool.Editor.Utilities
{
    public static class ScriptableObjectUtility
    {
        /// <summary>
        /// Finds an asset of type T 
        /// </summary>
        /// <typeparam name="T">Asset Type</typeparam>
        /// <returns></returns>
        public static T FindAsset<T>() where T : Object
        {

            T asset = default;
            if (UnityEditor.EditorPrefs.HasKey(PlayerSettings.companyName + "." + PlayerSettings.productName+"_"+typeof(T).ToString()))
            {

                var cachedAsstPath = UnityEditor.EditorPrefs.GetString(typeof(T).ToString());
                asset = UnityEditor.AssetDatabase.LoadAssetAtPath(cachedAsstPath, typeof(T)) as T;
				
            }


            if (asset == null)
            {
                var assetPath = FindAssetPath<T>();
                if (!string.IsNullOrWhiteSpace(assetPath))
                {
                    var foundAsset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(T)) as T;
                    EditorPrefs.SetString(PlayerSettings.companyName + "." + PlayerSettings.productName + "_" + typeof(T).ToString(), assetPath);
                    return foundAsset;
                }
            }

            return asset;
        }

        private static string FindAssetPath<T>() where T : Object
        {
            var assets = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(T));

            if (assets.Length > 0)
            {
                var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assets[0]);
                return assetPath;

            }

            var asset = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();
            return asset != null ? AssetDatabase.GetAssetPath(asset) : null;
        }
    }
}