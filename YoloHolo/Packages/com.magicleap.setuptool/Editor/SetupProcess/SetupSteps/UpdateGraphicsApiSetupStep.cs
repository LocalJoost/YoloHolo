#region

using System;
using MagicLeap.SetupTool.Editor.Interfaces;
using MagicLeap.SetupTool.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

#endregion

namespace MagicLeap.SetupTool.Editor.Setup
{
    /// <summary>
    /// Changes the graphics APIs to work with Magic Leap and App Simulator
    /// </summary>
    public class UpdateGraphicsApiSetupStep : ISetupStep
    {
        //Localization
        private const string SET_CORRECT_GRAPHICS_API_LABEL = "Use Vulkan Graphics API";
        private const string SET_CORRECT_GRAPHICS_API_FOR_APPSIM_LABEL = "Use OpenGL Graphics API for App Sim";
        private const string SET_CORRECT_GRAPHICS_BUTTON_LABEL = "Update";
        private const string CONDITION_MET_LABEL = "Done";
        
        private static int _busyCounter;
        private static bool _hasCorrectGraphicConfiguration;
        /// <inheritdoc />
        public Action OnExecuteFinished { get; set; }
        public bool Block => false;
        public bool CanExecute => EnableGUI();
        private bool _isAppSimulatorEnabled;
     
        
        private static int BusyCounter
        {
            get => _busyCounter;
            set => _busyCounter = Mathf.Clamp(value, 0, 100);
        }

        /// <inheritdoc />
        public bool Busy => BusyCounter > 0;
        /// <inheritdoc />
        public bool IsComplete => _hasCorrectGraphicConfiguration;
        private bool _isAppSimulatorXREnabled;
        private static bool _correctGraphicsForMagicLeap;
        private static bool _correctGraphicsForAppSim;
    
        /// <inheritdoc />
        public void Refresh()
        {
           
            _isAppSimulatorXREnabled = MagicLeapPackageUtility.IsAppSimulatorXREnabled();
            CheckGraphicsForMagicLeap();
            CheckGraphicsForAppSim();
            _hasCorrectGraphicConfiguration = _isAppSimulatorXREnabled ? _correctGraphicsForAppSim && _correctGraphicsForMagicLeap : _correctGraphicsForMagicLeap;
            

        }

        private void CheckGraphicsForMagicLeap()
        {
            _correctGraphicsForMagicLeap = UnityProjectSettingsUtility.OnlyHasGraphicsDeviceType(BuildTarget.Android, GraphicsDeviceType.Vulkan) && !UnityProjectSettingsUtility.GetAutoGraphicsApi(BuildTarget.Android);
        }

        private void CheckGraphicsForAppSim()
        {
            _correctGraphicsForAppSim = 
            UnityProjectSettingsUtility.HasGraphicsDeviceTypeAtIndex(BuildTarget.StandaloneWindows, GraphicsDeviceType.OpenGLCore,0) && !UnityProjectSettingsUtility.GetAutoGraphicsApi(BuildTarget.StandaloneWindows) &&
            UnityProjectSettingsUtility.HasGraphicsDeviceTypeAtIndex(BuildTarget.StandaloneWindows64, GraphicsDeviceType.OpenGLCore,0) && !UnityProjectSettingsUtility.GetAutoGraphicsApi(BuildTarget.StandaloneWindows64) &&
            UnityProjectSettingsUtility.HasGraphicsDeviceTypeAtIndex(BuildTarget.StandaloneOSX, GraphicsDeviceType.Metal,0) && !UnityProjectSettingsUtility.GetAutoGraphicsApi(BuildTarget.StandaloneOSX) && 
            UnityProjectSettingsUtility.HasGraphicsDeviceTypeAtIndex(BuildTarget.StandaloneLinux64, GraphicsDeviceType.OpenGLCore,0) && !UnityProjectSettingsUtility.GetAutoGraphicsApi(BuildTarget.StandaloneLinux64);
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
            if (_isAppSimulatorXREnabled && !_correctGraphicsForAppSim)
            {
                if (CustomGuiContent.CustomButtons.DrawConditionButton(SET_CORRECT_GRAPHICS_API_FOR_APPSIM_LABEL,
                                                                       _hasCorrectGraphicConfiguration, CONDITION_MET_LABEL, SET_CORRECT_GRAPHICS_BUTTON_LABEL,
                                                                       Styles.FixButtonStyle))
                {
                    Execute();
                    return true;
                }
            }
            else
            {
                if (CustomGuiContent.CustomButtons.DrawConditionButton(SET_CORRECT_GRAPHICS_API_LABEL,
                                                                       _hasCorrectGraphicConfiguration, CONDITION_MET_LABEL, SET_CORRECT_GRAPHICS_BUTTON_LABEL,
                                                                       Styles.FixButtonStyle))
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
            if (IsComplete || Busy) return;

            UpdateGraphicsSettings();
        }

        /// <summary>
        /// Changes the graphics settings for all Magic Leap 2 platforms
        /// </summary>
        public  void UpdateGraphicsSettings()
        {
            BusyCounter++;
            var hasAppSimEnabled = MagicLeapPackageUtility.IsAppSimulatorXREnabled();
            var androidResetRequired = UnityProjectSettingsUtility.UseOnlyThisGraphicsApi(BuildTarget.Android, GraphicsDeviceType.Vulkan);
            var standaloneWindowsResetRequired = hasAppSimEnabled && UnityProjectSettingsUtility.SetGraphicsApi(BuildTarget.StandaloneWindows, GraphicsDeviceType.OpenGLCore,0);
            var standaloneWindows64ResetRequired = hasAppSimEnabled && UnityProjectSettingsUtility.SetGraphicsApi(BuildTarget.StandaloneWindows64,GraphicsDeviceType.OpenGLCore, 0);
            var standaloneOSXResetRequired =hasAppSimEnabled && UnityProjectSettingsUtility.SetGraphicsApi(BuildTarget.StandaloneOSX, GraphicsDeviceType.Metal, 0);
            var standaloneLinuxResetRequired = hasAppSimEnabled && UnityProjectSettingsUtility.SetGraphicsApi(BuildTarget.StandaloneLinux64, GraphicsDeviceType.OpenGLCore,0);

            UnityProjectSettingsUtility.SetAutoGraphicsApi(BuildTarget.Android, false);
            if (hasAppSimEnabled)
            {
                UnityProjectSettingsUtility.SetAutoGraphicsApi(BuildTarget.StandaloneWindows, false);
                UnityProjectSettingsUtility.SetAutoGraphicsApi(BuildTarget.StandaloneWindows64, false);
                UnityProjectSettingsUtility.SetAutoGraphicsApi(BuildTarget.StandaloneOSX, false);
                UnityProjectSettingsUtility.SetAutoGraphicsApi(BuildTarget.StandaloneLinux64, false);
            }


            ApplyAllRunner.Stop();
  
            
            if (androidResetRequired
            || standaloneWindowsResetRequired
            || standaloneWindows64ResetRequired
            || standaloneOSXResetRequired
            || standaloneLinuxResetRequired)
            {
                UnityProjectSettingsUtility.UpdateGraphicsApi(true);
            }
            else
            {
                UnityProjectSettingsUtility.UpdateGraphicsApi(false);
            }
            Refresh();
            BusyCounter--;
            OnExecuteFinished?.Invoke();
        }
    }
}