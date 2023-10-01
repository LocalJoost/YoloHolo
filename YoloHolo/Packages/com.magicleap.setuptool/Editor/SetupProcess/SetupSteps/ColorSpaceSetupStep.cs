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
    /// Switches the Color Space to Linear
    /// </summary>
    public class ColorSpaceSetupStep : ISetupStep
    {
        //Localization
        private const string COLOR_SPACE_LABEL = "Set Color Space To Linear";
        private const string CONDITION_MET_LABEL = "Done";
        private const string FIX_SETTING_BUTTON_LABEL = "Fix Setting";
        
        private static bool _correctColorSpace;

        /// <inheritdoc />
        public Action OnExecuteFinished { get; set; }
        public bool Block => false;
        
        /// <inheritdoc />
        public bool Busy { get; private set; }
        /// <inheritdoc />
        public bool IsComplete => _correctColorSpace;
        public bool CanExecute => EnableGUI();
        /// <inheritdoc />
        public void Refresh()
        {
            _correctColorSpace = PlayerSettings.colorSpace == ColorSpace.Linear;
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
            _correctColorSpace = PlayerSettings.colorSpace == ColorSpace.Linear;

            if (CustomGuiContent.CustomButtons.DrawConditionButton(COLOR_SPACE_LABEL, _correctColorSpace,
                CONDITION_MET_LABEL, FIX_SETTING_BUTTON_LABEL, Styles.FixButtonStyle))
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
                return;
            }

            Busy = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
      
     
            EditorApplication.delayCall += () =>
                                           {
                                               _correctColorSpace = PlayerSettings.colorSpace == ColorSpace.Linear;
                                               Busy = false;
                                               OnExecuteFinished?.Invoke();
                                           };
          
        }
    }
}