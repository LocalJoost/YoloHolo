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
    public class EnablePluginSetupStep : ISetupStep
    {
    
        //Localization
        private const string ENABLE_PLUGIN_LABEL = "Enable Plugin";
        private const string ENABLE_PLUGIN_SETTINGS_LABEL = "Enable Magic Leap XR Settings";
        private const string CONDITION_MET_LABEL = "Done";
        private const string ENABLE_MAGICLEAP_FINISHED_UNSUCCESSFULLY_WARNING = "Unsuccessful call:[Enable Magic Leap XR]. action finished, but Magic Leap XR Settings are still not enabled.";
        private const string FAILED_TO_EXECUTE_ERROR = "Failed to execute [Enable Magic Leap XR]";
        private const string ENABLE_MAGICLEAP_APPSIM_FINISHED_UNSUCCESSFULLY_WARNING = "Unsuccessful call:[Enable App Simulator]. action finished, but  App Simulator Settings are still not enabled.";
        private const string FAILED_TO_EXECUTE_APPSIM_ERROR = "Failed to execute [Enable App Simulator]";

        private const string ENABLE_APP_SIMULATOR_LABEL = "Enable App Sim";
        private const string ENABLE_APP_SIMULATOR_DIALOG_HEADER = "Enable XR Platform";
        private const string ENABLE_APP_SIMULATOR_DIALOG_BODY = "Would you like to enable App Simulator to test your project without building to device?";
        private const string ENABLE_APP_SIMULATOR_DIALOG_OK = "Enable";
        private const string ENABLE_APP_SIMULATOR_DIALOG_CANCEL = "Don't enable";
        private static int _busyCounter;
        private static bool _correctBuildTarget;
        /// <inheritdoc />
        public Action OnExecuteFinished { get; set; }
        public bool Block => false;

        private static int BusyCounter
        {
            get => _busyCounter;
            set => _busyCounter = Mathf.Clamp(value, 0, 100);
        }


        private bool _hasRootSDKPath;
        private  bool _magicLeapXRSettingsEnabled;
        private bool _isAppSimulatorEnabled;
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
                _isAppSimulatorEnabled = MagicLeapPackageUtility.IsAppSimulatorXREnabled();
            
        }
        private bool EnableGUI()
        {
            return _hasRootSDKPath && _correctBuildTarget && MagicLeapPackageUtility.IsMagicLeapSDKInstalled;
        }
        /// <inheritdoc />
        public bool Draw()
        {
            GUI.enabled = EnableGUI();
            if (_magicLeapXRSettingsEnabled)
            {
      
                if (CustomGuiContent.CustomButtons.DrawConditionButton(ENABLE_APP_SIMULATOR_LABEL, _isAppSimulatorEnabled, CONDITION_MET_LABEL, ENABLE_APP_SIMULATOR_LABEL, Styles.FixButtonStyle))
                {
                    AddAppSimulator();
                    return true;
                }
            }
            else
            {
                if (CustomGuiContent.CustomButtons.DrawConditionButton(ENABLE_PLUGIN_SETTINGS_LABEL, _magicLeapXRSettingsEnabled, CONDITION_MET_LABEL, ENABLE_PLUGIN_LABEL, Styles.FixButtonStyle))
                {
                    Execute();
                    return true;
                }
            }
            

            return false;
        }

        private void AddAppSimulator()
        {
            if (!_isAppSimulatorEnabled)
            {
                BusyCounter++;
                MagicLeapPackageUtility.EnableAppSimulatorFinished += OnEnableMagicLeapAppSimulatorPluginFinished;
                MagicLeapPackageUtility.EnableAppSimulatorXRPlugin();
                UnityProjectSettingsUtility.OpenXRManagementWindow();
            }



            void OnEnableMagicLeapAppSimulatorPluginFinished(bool success)
            {

                if (success)
                {

                    _isAppSimulatorEnabled = MagicLeapPackageUtility.IsAppSimulatorXREnabled();
                    if (!_isAppSimulatorEnabled)
                    {
                        Debug.LogWarning(ENABLE_MAGICLEAP_APPSIM_FINISHED_UNSUCCESSFULLY_WARNING);
                    }
                }
                else
                {
                    Debug.LogError(FAILED_TO_EXECUTE_APPSIM_ERROR);
                }

                OnExecuteFinished?.Invoke();
                BusyCounter--;
                MagicLeapPackageUtility.EnableAppSimulatorFinished -= OnEnableMagicLeapAppSimulatorPluginFinished;
                Refresh();
     
            }
        }

        /// <inheritdoc />
        public void Execute()
        {
            if (IsComplete || Busy) return;

            if (!MagicLeapPackageUtility.IsMagicLeapSDKInstalled)
            {
                return;
            }

            var shouldEnableAppSimulator = EditorUtility.DisplayDialog(ENABLE_APP_SIMULATOR_DIALOG_HEADER, ENABLE_APP_SIMULATOR_DIALOG_BODY, ENABLE_APP_SIMULATOR_DIALOG_OK, ENABLE_APP_SIMULATOR_DIALOG_CANCEL);
        
         
            if (!_magicLeapXRSettingsEnabled)
            {
                BusyCounter++;
                MagicLeapPackageUtility.EnableMagicLeapXRFinished += OnEnableMagicLeapPluginFinished;
                MagicLeapPackageUtility.EnableMagicLeapXRPlugin();
            }

            if (shouldEnableAppSimulator)
            {
                AddAppSimulator();
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

                OnExecuteFinished?.Invoke();
                BusyCounter--;
                MagicLeapPackageUtility.EnableMagicLeapXRFinished -= OnEnableMagicLeapPluginFinished;
                Refresh();
            }

            UnityProjectSettingsUtility.OpenXRManagementWindow();
      
        }
    }
}