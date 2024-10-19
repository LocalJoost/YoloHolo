#if !USE_MLSDK

#region

using System;
using System.Linq;
using MagicLeap.SetupTool.Editor.Interfaces;
using MagicLeap.SetupTool.Editor.Utilities;
using UnityEditor;
using UnityEngine;
#if (OpenXR) 
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
#endif
#endregion

namespace MagicLeap.SetupTool.Editor.Setup
{
    /// <summary>
    /// Enables the Magic Leap 2 XR plugin
    /// </summary>
    public class EnableOpenXRPluginStep : ISetupStep
    {
    
        //XR Plugin constants
        private const string LOADER_ID = "OpenXRLoader"; // Used to test if the loader is installed and active.
        private const string FEATURE_SET_ID = "com.magicleap.openxr.featuregroup";

        //Localization
        private const string ENABLE_PLUGIN_LABEL = "Enable Plugin";
        private const string ENABLE_PLUGIN_SETTINGS_LABEL = "Enable OpenXR Settings";
        private const string ENABLE_FEATURE_SET_LABEL = "Enable Feature";
        private const string ENABLE_FEATURE_SET_SETTINGS_LABEL = "Enable Magic Leap Feature";
        private const string CONDITION_MET_LABEL = "Done";
        private const string ENABLE_FINISHED_UNSUCCESSFULLY_WARNING = "Unsuccessful call:[Enable OpenXR]. action finished, but OpenXR Settings are still not enabled.";
        private const string FAILED_TO_EXECUTE_ERROR = "Failed to execute [Enable OpenXR]";
        
        

        private  static bool _runningUnityValidationAutoFix;
        private static int _busyCounter;
        private static bool _correctBuildTarget;
        private static bool _xrPluginSettingsEnabled;
        private static bool _xrFeatureSetEnabled;
        
        /// <inheritdoc />
        public Action OnExecuteFinished { get; set; }
        public bool Block => true;
        /// <inheritdoc />
        public bool Required => !_xrPluginSettingsEnabled;
        

        private static int BusyCounter
        {
            get => _busyCounter;
            set => _busyCounter = Mathf.Clamp(value, 0, 100);
        }


        private bool _hasRootSDKPath;
        /// <inheritdoc />
        public bool Busy => BusyCounter > 0;
        /// <inheritdoc />
        public bool IsComplete => XRPackageUtility.XRPluginEnabled(LOADER_ID, BuildTargetGroup.Android) && XRPackageUtility.XRFeatureSetEnabled(FEATURE_SET_ID, BuildTargetGroup.Android);


        public bool CanExecute => EnableGUI();
        /// <inheritdoc />
        public void Refresh()
        {
           
            _hasRootSDKPath = MagicLeapPackageUtility.HasRootSDKPath;
            _correctBuildTarget = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
            _xrPluginSettingsEnabled = XRPackageUtility.XRPluginEnabled(LOADER_ID, BuildTargetGroup.Android);
            _xrFeatureSetEnabled = XRPackageUtility.XRFeatureSetEnabled(FEATURE_SET_ID, BuildTargetGroup.Android);
          
        }
        
   
        private bool EnableGUI()
        {
            return _hasRootSDKPath && _correctBuildTarget && XRPackageUtility.HasSDKInstalled;
        }

       
        /// <inheritdoc />
        public bool Draw()
        {
            GUI.enabled = EnableGUI();
       
            if (!_xrPluginSettingsEnabled)
            {
                if (CustomGuiContent.CustomButtons.DrawConditionButton(ENABLE_PLUGIN_SETTINGS_LABEL, _xrPluginSettingsEnabled,
                        CONDITION_MET_LABEL, ENABLE_PLUGIN_LABEL, Styles.FixButtonStyle))
                {
                    Execute();
                    return true;
                }
            }
            else
            {
                if (CustomGuiContent.CustomButtons.DrawConditionButton(ENABLE_FEATURE_SET_SETTINGS_LABEL, _xrFeatureSetEnabled,
                        CONDITION_MET_LABEL, ENABLE_FEATURE_SET_LABEL, Styles.FixButtonStyle))
                {
                    Execute();
                    return true;
                }
            }
            

            return false;
        }

    
        /// <inheritdoc />
        public void Execute()
        {
            if (IsComplete || Busy)
            {
#if ML_SETUP_DEBUG
                Debug.Log($"Not executing step: {this.GetType().Name}. IsComplete: {IsComplete} || Busy: {Busy}");
#endif
                return;
            }

            if (!XRPackageUtility.HasSDKInstalled)
            {
                return;
            }

      
         
            if (!_xrPluginSettingsEnabled)
            {
                BusyCounter++;
                XRPackageUtility.EnableXRPluginFinished += OnEnablXRPluginFinished;
#if (OpenXR) 
                XRPackageUtility.EnableXRPlugin<OpenXRLoader>(BuildTargetGroup.Android);
#endif
            }
            else
            {
                if (!_xrFeatureSetEnabled)
                {
                    XRPackageUtility.EnableXRFeatureSet(FEATURE_SET_ID, BuildTargetGroup.Android);
                }
                
            }

          



        

            void OnEnablXRPluginFinished(bool success)
            {
                if (success)
                {
                    _xrPluginSettingsEnabled = XRPackageUtility.XRPluginEnabled(LOADER_ID, BuildTargetGroup.Android);
                    if (!_xrPluginSettingsEnabled)
                    {
                        Debug.LogWarning(ENABLE_FINISHED_UNSUCCESSFULLY_WARNING);
                    }
                    else
                    {
                        XRPackageUtility.EnableXRFeatureSet(FEATURE_SET_ID, BuildTargetGroup.Android);
                        EditorApplication.delayCall += () =>
                        {
#if ML_SETUP_DEBUG
                            Debug.Log($"Enabling XRInteraction Feature set: MagicLeapControllerProfile Android");
#endif
                            XRPackageUtility.EnableXRInteractionFeature(BuildTargetGroup.Android,
                                "MagicLeapControllerProfile Android");
                        };

                    }
                }
                else
                {
                    Debug.LogError(FAILED_TO_EXECUTE_ERROR);
                }
#if ML_SETUP_DEBUG
                Debug.Log($"Enable plugin success: {success}. {this.GetType().Name} finished.");
#endif
                BusyCounter--;
                OnExecuteFinished?.Invoke();
                XRPackageUtility.EnableXRPluginFinished -= OnEnablXRPluginFinished;
            }

            UnityProjectSettingsUtility.OpenXRManagementWindow();
      
        }
        /// <inheritdoc cref="ISetupStep.ToString"/>
        public override string ToString()
        {
            var info =$"Step: {this.GetType().Name}, CanExecute: {CanExecute}, Busy: {Busy}, IsComplete: {IsComplete}";
            
            if (!EnableGUI())
            {
           
                info += "\nDisabling GUI: ";
                if (!_hasRootSDKPath)
                {
                    info += "[no root sdk path], ";
                }
                if (!_correctBuildTarget)
                {
                    info += "[not the correct build target], ";
                }
                if (!XRPackageUtility.HasSDKInstalled)
                {
                    info += "[Package is not installed]";
                }
            }
            info += $"\nMore Info: HasRootSDKPath: {_hasRootSDKPath}, CorrectBuildTarget: {_correctBuildTarget}," +
                    $" XRPluginSettingsEnabled: {_xrPluginSettingsEnabled}, XRFeatureSetEnabled: {_xrFeatureSetEnabled}," +
                    $"HasSDKInstalled: {XRPackageUtility.HasSDKInstalled}";

            return info;
        }
    }
}

#endif