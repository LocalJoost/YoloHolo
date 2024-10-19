#if USE_MLSDK
#region

using System;
using MagicLeap.SetupTool.Editor.Interfaces;
using MagicLeap.SetupTool.Editor.Utilities;
using UnityEditor;
using UnityEngine;

#endregion

namespace MagicLeap.SetupTool.Editor.Setup
{
    /// <summary>
    /// Enables the Magic Leap 2 XR plugin
    /// </summary>
    public class EnablePluginStep : ISetupStep
    {
    
        //Localization
        private const string ENABLE_PLUGIN_LABEL = "Enable Plugin";
        private const string ENABLE_PLUGIN_SETTINGS_LABEL = "Enable Magic Leap XR Settings";
        private const string CONDITION_MET_LABEL = "Done";
        private const string ENABLE_MAGICLEAP_FINISHED_UNSUCCESSFULLY_WARNING = "Unsuccessful call:[Enable Magic Leap XR]. action finished, but Magic Leap XR Settings are still not enabled.";
        private const string FAILED_TO_EXECUTE_ERROR = "Failed to execute [Enable Magic Leap XR]";



        private static int _busyCounter;
        private static bool _correctBuildTarget;
        
        /// <inheritdoc />
        public Action OnExecuteFinished { get; set; }
        public bool Block => true;
        /// <inheritdoc />
        public bool Required => !_magicLeapXRSettingsEnabled;
        

        private static int BusyCounter
        {
            get => _busyCounter;
            set => _busyCounter = Mathf.Clamp(value, 0, 100);
        }


        private bool _hasRootSDKPath;
        private  bool _magicLeapXRSettingsEnabled;
        /// <inheritdoc />
        public bool Busy => BusyCounter > 0;
        /// <inheritdoc />
        public bool IsComplete => _magicLeapXRSettingsEnabled;

        public bool CanExecute => EnableGUI();
        /// <inheritdoc />
        public void Refresh()
        {
      
                _hasRootSDKPath = MagicLeapPackageUtility.HasRootSDKPath;
                _correctBuildTarget = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
                _magicLeapXRSettingsEnabled = MagicLeapPackageUtility.IsMagicLeapXREnabled();
            
        }
        private bool EnableGUI()
        {
            return _hasRootSDKPath && _correctBuildTarget && MagicLeapPackageUtility.IsMagicLeapSDKInstalled;
        }
        /// <inheritdoc />
        public bool Draw()
        {
            GUI.enabled = EnableGUI();
            if (CustomGuiContent.CustomButtons.DrawConditionButton(ENABLE_PLUGIN_SETTINGS_LABEL, _magicLeapXRSettingsEnabled, CONDITION_MET_LABEL, ENABLE_PLUGIN_LABEL, Styles.FixButtonStyle))
            {
                Execute();
                return true;
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

            if (!MagicLeapPackageUtility.IsMagicLeapSDKInstalled)
            {
                return;
            }

         
            if (!_magicLeapXRSettingsEnabled)
            {
                BusyCounter++;
                MagicLeapPackageUtility.EnableMagicLeapXRFinished += OnEnableMagicLeapPluginFinished;
                MagicLeapPackageUtility.EnableMagicLeapXRPlugin();
            }




            void OnEnableMagicLeapPluginFinished(bool success)
            {
                if (success)
                {
                    _magicLeapXRSettingsEnabled = MagicLeapPackageUtility.IsMagicLeapXREnabled();
                    if (!_magicLeapXRSettingsEnabled)
                        Debug.LogWarning(ENABLE_MAGICLEAP_FINISHED_UNSUCCESSFULLY_WARNING);
                }
                else
                {
                    Debug.LogError(FAILED_TO_EXECUTE_ERROR);
                }
                #if ML_SETUP_DEBUG
                Debug.Log($"Enable plugin success: {success}. {this.GetType().Name} finished");
                #endif
                OnExecuteFinished?.Invoke();
                BusyCounter--;
                MagicLeapPackageUtility.EnableMagicLeapXRFinished -= OnEnableMagicLeapPluginFinished;
                Refresh();
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
                    $" XRPluginSettingsEnabled: {_magicLeapXRSettingsEnabled}," +
                    $"HasSDKInstalled: {XRPackageUtility.HasSDKInstalled}";

            return info;
        }

    }
}
#endif