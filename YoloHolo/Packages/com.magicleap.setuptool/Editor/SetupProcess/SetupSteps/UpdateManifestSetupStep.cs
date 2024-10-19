#region

using System;
using System.Collections.Generic;
using System.IO;
using MagicLeap.SetupTool.Editor.Interfaces;
using MagicLeap.SetupTool.Editor.Utilities;
using UnityEditor;
using UnityEngine;

#endregion

namespace MagicLeap.SetupTool.Editor.Setup
{
    /// <summary>
    /// Updates the SDK manifest file based on <see cref="DefaultPackageTemplate" />
    /// </summary>
    public class UpdateManifestSetupStep : ISetupStep
    {
        //Localization
        private const string UPDATE_MANIFEST_LABEL = "Update the manifest file";
        private const string UPDATE_MANIFEST_BUTTON_LABEL = "Update";
        private const string CONDITION_MET_LABEL = "Done";

        //Paths
        public const string PROJECT_MANIFEST_PATH = "Assets/Plugins/Android/AndroidManifest.xml";
        public const string PROJECT_MANIFEST_Directory = "Assets/Plugins/Android";
            
#if UNITY_EDITOR_OSX
        public const string EDITOR_MANIFEST_PATH = "PlaybackEngines/AndroidPlayer/Apk/UnityManifest.xml";
#elif UNITY_EDITOR_WIN
       public const string EDITOR_MANIFEST_PATH = "Data/PlaybackEngines/AndroidPlayer/Apk/UnityManifest.xml";
#endif


        public readonly string[] RequiredPermissions = new string[0];

        /// <inheritdoc />
        public bool Required => !HasRequiredPermissions();
        
        private static int _busyCounter;
        private int _maxSdkApiLevel;
        private static bool _manifestIsUpdated;
        /// <inheritdoc />
        public Action OnExecuteFinished { get; set; }
        public bool Block => true;
        public bool CanExecute => EnableGUI();
        private static int BusyCounter
        {
            get => _busyCounter;
            set => _busyCounter = Mathf.Clamp(value, 0, 100);
        }


        public bool Busy => BusyCounter > 0;
        /// <inheritdoc />
        public bool IsComplete => _manifestIsUpdated;


        private void CreateProjectAndroidManifest()
        {
            
#if ML_SETUP_DEBUG
             Debug.Log($"Step: {this.GetType().Name}. Creating Android Manifest...");
#endif

            
            var editorPath = Path.GetDirectoryName(EditorApplication.applicationPath);
            var editorTemplateManifest = Path.Combine(editorPath, EDITOR_MANIFEST_PATH);
            if (File.Exists(editorTemplateManifest))
            {
                Directory.CreateDirectory(PROJECT_MANIFEST_Directory);
                File.Copy(editorTemplateManifest, PROJECT_MANIFEST_PATH, true);
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogError($"file [{editorTemplateManifest}] does not exist");
            }

        }
        /// <inheritdoc />
        public void Refresh()
        {   
          
                _maxSdkApiLevel = MagicLeapPackageUtility.GetMaxAPILevel();
               
                _manifestIsUpdated = IsValidManifest();
          
            
        }

        private bool HasRequiredPermissions()
        {
            var hasProjectManifest = File.Exists(PROJECT_MANIFEST_PATH);
            if (hasProjectManifest)
            {
                var hasPermissions = true;
                var androidManifest = new AndroidManifest(PROJECT_MANIFEST_PATH);
                for (var i = 0; i < RequiredPermissions.Length; i++)
                {
                    hasPermissions = androidManifest.HasPermission(RequiredPermissions[i]);
                   if(hasPermissions == false)
                   {
                       break;
                   }
                }
                return hasPermissions;
            }

            return false;
        }


        bool IsValidManifest()
        {
            return MagicLeapPackageUtility.MinimumAPILevel == _maxSdkApiLevel && HasRequiredPermissions();
        }

        private bool EnableGUI()
        {
            
            var correctBuildTarget = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android && MagicLeapPackageUtility.IsMagicLeapSDKInstalled;
            return correctBuildTarget;
        }
        /// <inheritdoc />
        public bool Draw()
        {
            GUI.enabled = EnableGUI();
            if (CustomGuiContent.CustomButtons.DrawConditionButton(UPDATE_MANIFEST_LABEL, _manifestIsUpdated,
                CONDITION_MET_LABEL, UPDATE_MANIFEST_BUTTON_LABEL, Styles.FixButtonStyle))
            {
                Execute();
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public void Execute()
        {
            if (IsComplete)
            {
#if ML_SETUP_DEBUG
                Debug.Log($"Not executing step: {this.GetType().Name}. IsComplete: {IsComplete}");
#endif
                return;
            }

            BusyCounter++;
            var hasProjectManifest = UnityEngine.Windows.File.Exists(PROJECT_MANIFEST_PATH);
            if (!hasProjectManifest)
            {
                CreateProjectAndroidManifest();
            }

            var androidManifest = new AndroidManifest(PROJECT_MANIFEST_PATH);
         

         
            MagicLeapPackageUtility.MinimumAPILevel = _maxSdkApiLevel;

            for (var i = 0; i < RequiredPermissions.Length; i++)
            {
                androidManifest.SetPermission(RequiredPermissions[i], true);
            }
            androidManifest.Save();
         
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _manifestIsUpdated = IsValidManifest();
            OnExecuteFinished?.Invoke();
            BusyCounter--;
#if ML_SETUP_DEBUG
            Debug.Log($"{this.GetType().Name} finished.");
#endif
        }
        /// <inheritdoc cref="ISetupStep.ToString"/>
        public override string ToString()
        {
            var info =$"Step: {this.GetType().Name}, CanExecute: {CanExecute}, Busy: {Busy}, IsComplete: {IsComplete}";
            
            if (!EnableGUI())
            {
                var correctBuildTarget = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
                var hasSdkInstalled =  MagicLeapPackageUtility.IsMagicLeapSDKInstalled;
                info += "\nDisabling GUI: ";
                if (!correctBuildTarget)
                {
                    info += "[not the correct build target], ";
                }
                if (!hasSdkInstalled)
                {
                    info += "[Package is not installed]";
                }
            }
            info += $"\nMore Info: MinimumAPILevel: {MagicLeapPackageUtility.MinimumAPILevel}, MaxAPILevel: {MagicLeapPackageUtility.GetMaxAPILevel()}," +
                    $" HasProjectManifest: {File.Exists(PROJECT_MANIFEST_PATH)}";

            return info;
        }

    }
}