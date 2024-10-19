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
        private const string SET_CORRECT_GRAPHICS_BUTTON_LABEL = "Update";
        private const string CONDITION_MET_LABEL = "Done";
        
        private static int _busyCounter;
        private static bool _hasCorrectGraphicConfiguration;
        /// <inheritdoc />
        public Action OnExecuteFinished { get; set; }
        public bool Block => true;
        
        /// <inheritdoc />
        public bool Required => true;
        
        public bool CanExecute => EnableGUI();

     
        
        private static int BusyCounter
        {
            get => _busyCounter;
            set => _busyCounter = Mathf.Clamp(value, 0, 100);
        }

        /// <inheritdoc />
        public bool Busy => BusyCounter > 0;
        /// <inheritdoc />
        public bool IsComplete => _hasCorrectGraphicConfiguration;
    
        private static bool _correctGraphicsForMagicLeap;

    
        /// <inheritdoc />
        public void Refresh()
        {
           
          
            CheckGraphicsForMagicLeap();
            _hasCorrectGraphicConfiguration = _correctGraphicsForMagicLeap ;
            

        }

        private void CheckGraphicsForMagicLeap()
        {
            _correctGraphicsForMagicLeap = UnityProjectSettingsUtility.OnlyHasGraphicsDeviceType(BuildTarget.Android, GraphicsDeviceType.Vulkan) && !UnityProjectSettingsUtility.GetAutoGraphicsApi(BuildTarget.Android);
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
            if (CustomGuiContent.CustomButtons.DrawConditionButton(SET_CORRECT_GRAPHICS_API_LABEL,
                    _hasCorrectGraphicConfiguration, CONDITION_MET_LABEL, SET_CORRECT_GRAPHICS_BUTTON_LABEL,
                    Styles.FixButtonStyle))
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

            UpdateGraphicsSettings();
        }

        /// <summary>
        /// Changes the graphics settings for all Magic Leap 2 platforms
        /// </summary>
        public  void UpdateGraphicsSettings()
        {
            BusyCounter++;
  
            var androidResetRequired = UnityProjectSettingsUtility.UseOnlyThisGraphicsApi(BuildTarget.Android, GraphicsDeviceType.Vulkan);
 
            UnityProjectSettingsUtility.SetAutoGraphicsApi(BuildTarget.Android, false);

#if ML_SETUP_DEBUG
            Debug.Log($"{this.GetType().Name}: Updated graphics api. Require restart: {androidResetRequired}");
#endif

            ApplyAllRunner.Stop();
  
            
            if (androidResetRequired)
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

            return info;
        }
    }
    
}