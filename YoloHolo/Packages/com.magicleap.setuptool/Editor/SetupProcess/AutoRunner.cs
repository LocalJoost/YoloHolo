#region

using MagicLeap.SetupTool.Editor.Setup;
using MagicLeap.SetupTool.Editor.Utilities;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

#endregion

namespace MagicLeap.SetupTool.Editor
{
    /// <summary>
    /// This class controls when to show the Magic Leap setup window.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoRunner
    {
        /// <summary>
        /// Is true when the Magic Leap XR package is installed
        /// </summary>
        private static bool _hasMagicLeapInstalled
        {
            get
            {
#if MAGICLEAP
                return true;
#else
                return  false;
#endif
            }
        }


        static AutoRunner()
        {
            //Prevent the window from opening when launching Unity in command line mode
            if (Application.isBatchMode)
                return;

            EditorApplication.update += OnEditorApplicationUpdate;
            EditorApplication.quitting += OnQuit;
            Events.registeringPackages += RegisteringPackagesEventHandler;
        }

        private static void OnQuit()
        {    
            Events.registeringPackages -= RegisteringPackagesEventHandler;
            EditorApplication.quitting -= OnQuit;
            EditorPrefs.SetBool(EditorKeyUtility.WindowClosedEditorPrefKey, false);
        }
        // The method is expected to receive a PackageRegistrationEventArgs event argument to check if the Magic Leap SDK is being installed or uninstalled.
        private static void RegisteringPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs)
        {
            foreach (var addedPackage in packageRegistrationEventArgs.added)
            {

                if (addedPackage.name == "com.magicleap.unitysdk")
                {
                    if (!DefineSymbolUtility.ContainsDefineSymbolInAllBuildTargets("MAGICLEAP"))
                        DefineSymbolUtility.AddDefineSymbol("MAGICLEAP");
                }

            }

            foreach (var removedPackage in packageRegistrationEventArgs.removed)
            {

                if (removedPackage.name == "com.magicleap.unitysdk")
                {
                    if (DefineSymbolUtility.ContainsDefineSymbolInAnyBuildTarget("MAGICLEAP"))
                        DefineSymbolUtility.RemoveDefineSymbol("MAGICLEAP");
                    
                    if (DefineSymbolUtility.ContainsDefineSymbolInAnyBuildTarget("OpenXR"))
                        DefineSymbolUtility.RemoveDefineSymbol("OpenXR");

                    EditorPrefs.SetBool(EditorKeyUtility.WindowClosedEditorPrefKey, false);
                }

            }

        }


        private static void OnEditorApplicationUpdate()
        {
          
            if(EditorApplication.timeSinceStartup<15)
            {
                return;
            }

            //Do not reload information when the editor is not idle.
            if (AssetDatabase.IsAssetImportWorkerProcess()
                || EditorApplication.isCompiling
                || EditorApplication.isUpdating)
            {
                return;
            }

            var autoShow = true;
            if (!DefineSymbolUtility.ContainsDefineSymbolInAnyBuildTarget("MAGICLEAP"))
            {
                EditorPrefs.SetBool(EditorKeyUtility.AutoShowEditorPrefKey, true);
            }
            else
            {
                autoShow = EditorPrefs.GetBool(EditorKeyUtility.AutoShowEditorPrefKey, true);
            }
      
            if (!MagicLeapPackageUtility.HasRootSDKPath
                || !_hasMagicLeapInstalled
                || EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                autoShow = true;
                EditorPrefs.SetBool(EditorKeyUtility.AutoShowEditorPrefKey, true);
            }

            EditorApplication.update -= OnEditorApplicationUpdate;
            if (!autoShow && !ApplyAllRunner.WasRunning())
            {
                return;
            }

            MagicLeapSetupWindow.ForceOpen();
        }
    }
}