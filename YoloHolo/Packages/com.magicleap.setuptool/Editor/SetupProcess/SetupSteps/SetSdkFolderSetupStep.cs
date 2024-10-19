#region

using System;
using System.IO;
using System.Linq;
using MagicLeap.SetupTool.Editor.Interfaces;
using MagicLeap.SetupTool.Editor.Utilities;
using UnityEditor;
using UnityEngine;

#endregion

namespace MagicLeap.SetupTool.Editor.Setup
{
    /// <summary>
    /// Sets the Magic Leap 2 SDK folder in the Preferences window
    /// </summary>
    public class SetSdkFolderSetupStep : ISetupStep
    {

        private const string SDK_PATH_EDITOR_PREF_KEY = "MagicLeapSDKRoot"; 

        //Localization
        private const string LOCATE_SDK_FOLDER_LABEL = "Set Magic Leap SDK folder";
        private const string CONDITION_MET_CHANGE_LABEL = "Change";
        private const string UPDATE_SDK_FOLDER_BUTTON_LABEL = "Update SDK";
        private const string LOCATE_SDK_FOLDER_BUTTON_LABEL = "Locate SDK";
        private const string SDK_FILE_BROWSER_TITLE = "Set external Magic Leap SDK Folder";        //Title text of SDK path browser
        private const string SET_MAGIC_LEAP_DIR_MESSAGE = "Updated Magic Leap SDK path to [{0}]."; //[0] folder path
        private const string INVALID_PATH_TITLE = "Invalid Path";
        private const string INVALID_PATH_MESSAGE = "This path is invalid. Make sure the SDK path includes the .manifest folder.";
        private const string INVALID_PATH_CANCEL_BUTTON_LABEL = "Cancel";  
        private static bool _hasLatestSdkSelected;
        private static string _sdkRoot;
        private static string _latestSDK;
        private static bool _hasRootSDKPath;
        public bool CanExecute => true;
        /// <inheritdoc />
        public bool Busy { get; private set; }
        /// <inheritdoc />
        public Action OnExecuteFinished { get; set; }
        public bool Block => true;
        /// <inheritdoc />
        public bool IsComplete => _hasRootSDKPath;
        
        public bool Required => !_hasRootSDKPath;
        
        
        /// <inheritdoc />
        public void Refresh()
        {
          
            _hasRootSDKPath = MagicLeapPackageUtility.HasRootSDKPath;
            _sdkRoot = MagicLeapPackageUtility.SdkRoot;
            _latestSDK = MagicLeapPackageUtility.GetLatestSDKPath();
            if (!string.IsNullOrWhiteSpace(_sdkRoot) && Directory.Exists(_sdkRoot))
            {
                var versionComparer = new MagicLeapPackageUtility.VersionComparer();
                _hasLatestSdkSelected = versionComparer.Compare(MagicLeapPackageUtility.GetLatestSDKPath(), _sdkRoot) <= 0;
    
            }

        }


        /// <inheritdoc />
        public bool Draw()
        {

            if (!_hasRootSDKPath)
            {
                if (CustomGuiContent.CustomButtons.DrawConditionButton(new GUIContent(LOCATE_SDK_FOLDER_LABEL),
                                                                       _hasRootSDKPath,new GUIContent(CONDITION_MET_CHANGE_LABEL, _sdkRoot),
                                                                       new GUIContent(LOCATE_SDK_FOLDER_BUTTON_LABEL, _sdkRoot), Styles.FixButtonStyle, false))
                {
                   
                    Execute();
                    return true;
                }
            }
            else
            {
                if (CustomGuiContent.CustomButtons.DrawConditionButton(new GUIContent(LOCATE_SDK_FOLDER_LABEL),
                                                                       _hasLatestSdkSelected, new GUIContent(CONDITION_MET_CHANGE_LABEL, _sdkRoot),
                                                                       new GUIContent(UPDATE_SDK_FOLDER_BUTTON_LABEL, string.Format(SET_MAGIC_LEAP_DIR_MESSAGE, _latestSDK)), Styles.FixButtonStyle, false))
                {
                
                    ChangeSDK();
                    return true;
                }
            }
 

            return false;
        }

        /// <inheritdoc />
        public void Execute()
        {
            Busy = true;
            BrowseForSDK();
      
        }

        /// <summary>
        /// Gets the current SDK location. If none is found. returns the mlsdk folder
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentSDKLocation()
        {
            var currentPath = _sdkRoot;
            if (string.IsNullOrEmpty(currentPath) || !Directory.Exists(currentPath)) currentPath = DefaultSDKPath();

            //select folder just outside of the version folder i.e: PATH/v[x].[x].[x]
            if (string.IsNullOrEmpty(currentPath))
                return "";
            if (currentPath.Contains("v")) return Path.GetFullPath(Path.Combine(currentPath, "../"));

            return currentPath;
        }

        /// Opens dialogue to select SDK folder
        /// </summary>
        public void ChangeSDK()
        {
            string directorPath = GetCurrentSDKLocation();
            string selectedFolder = GetCurrentSDKFolderName(); 
            if (_hasRootSDKPath && !_hasLatestSdkSelected)
            {

                selectedFolder = new DirectoryInfo(MagicLeapPackageUtility.GetLatestSDKPath()).Name;
            }


            var path = EditorUtility.OpenFolderPanel(SDK_FILE_BROWSER_TITLE, directorPath, selectedFolder);
            if (path.Length != 0)
            {
                SetRootSDK(path);
            }
            else
            {
                ApplyAllRunner.Stop();
                Busy = false;
            }
        }
        /// <summary>
        /// Opens dialogue to select SDK folder
        /// </summary>
        public void BrowseForSDK()
        {
       
            var director = new DirectoryInfo(MagicLeapPackageUtility.GetLatestSDKPath());
#if ML_SETUP_DEBUG
            Debug.Log($"Get SDK Path Directory: {director}");
#endif
            var directorName = director.Name;
            var directorPath = director.Parent.FullName;
           
            var path = EditorUtility.OpenFolderPanel(SDK_FILE_BROWSER_TITLE, directorPath, directorName);
            if (path.Length != 0)
            {
                SetRootSDK(path);
            }
            else
            {
                ApplyAllRunner.Stop();
                Busy = false;
                OnExecuteFinished?.Invoke();
            }
        }

        /// <summary>
        /// Gets current SDK folder name based on the SDK path
        /// </summary>
        /// <returns></returns>
        private static string GetCurrentSDKFolderName()
        {
            var currentPath = _sdkRoot;
            if (string.IsNullOrEmpty(currentPath) || !Directory.Exists(currentPath))
            {
                currentPath = FindSDKPath();
            }

            //version folder i.e: v[x].[x].[x]
            if (!string.IsNullOrEmpty(currentPath) && currentPath.Contains("v"))
            {
                var dirName = new DirectoryInfo(currentPath).Name;
                return dirName;
            }

            return "";
        }

        /// <summary>
        /// Returns the default Magic Leap install path [HOME/MagicLeap/mlsdk/]
        /// </summary>
        /// <returns></returns>
        private static string DefaultSDKPath()
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(root)) root = Environment.GetEnvironmentVariable("HOME");

            if (!string.IsNullOrEmpty(root))
            {
                var sdkRoot = Path.Combine(root, "MagicLeap/mlsdk/").Replace("\\","/");
                return Directory.Exists(sdkRoot) ? sdkRoot : root;
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        /// <summary>
        /// Sets the SDK path in the Unity Editor
        /// </summary>
        /// <param name="path"></param>
        private void SetRootSDK(string path)
        {
#if ML_SETUP_DEBUG
            Debug.Log($"Setting SDK Path To: {path}");
#endif
            if (File.Exists(Path.Combine(path, ".metadata", "sdk.manifest")))
            {
#if ML_SETUP_DEBUG
                Debug.Log($"Successfully set sdk path.");
#endif
                EditorPrefs.SetString(SDK_PATH_EDITOR_PREF_KEY, path);
                Busy = false;
                Refresh();
                OnExecuteFinished?.Invoke();
            }
            else
            {
#if ML_SETUP_DEBUG
                Debug.LogWarning($"Failed to set SDK Path because .metadata folder was not found.");
#endif
                if (EditorUtility.DisplayDialog(INVALID_PATH_TITLE, INVALID_PATH_MESSAGE, LOCATE_SDK_FOLDER_BUTTON_LABEL, INVALID_PATH_CANCEL_BUTTON_LABEL))
                {
                    BrowseForSDK();
                }
                else
                {
                    ApplyAllRunner.Stop();
                    Busy = false;
                    OnExecuteFinished?.Invoke();
                }
            }
    
        }


        /// <summary>
        /// Finds the SDK path based on the default install location and newest added folder
        /// </summary>
        /// <returns></returns>
        private static string FindSDKPath()
        {
            
            var editorSdkPath = EditorPrefs.GetString(SDK_PATH_EDITOR_PREF_KEY, null);
            if (string.IsNullOrEmpty(editorSdkPath)
                || !Directory.Exists(editorSdkPath))
            {
                if (!string.IsNullOrEmpty(MagicLeapPackageUtility.GetLatestSDKPath()))
                {
                    return editorSdkPath;
                }
            }
            else
            {
                return editorSdkPath;
            }

            return null;
        }

        /// <inheritdoc cref="ISetupStep.ToString"/>
        public override string ToString()
        {
            var info =$"Step: {this.GetType().Name}, CanExecute: {CanExecute}, Busy: {Busy}, IsComplete: {IsComplete}";
            
            
            info += $"\nMore Info: HasRootSDKPath: {_hasRootSDKPath}\nSDK Root: {_sdkRoot}\n" +
                    $"LatestSDK:{_latestSDK}\nFoundSDK:{FindSDKPath()}\nDefaultSDKPath: {DefaultSDKPath()}\n" +
                    $"CurrentSDKFolderName: {GetCurrentSDKFolderName()}\nCurrentSDKLocation: {GetCurrentSDKLocation()}\n" +
                    $"HasLatestSdkSelected: {_hasLatestSdkSelected}";

            return info;
        }
     
    }
}