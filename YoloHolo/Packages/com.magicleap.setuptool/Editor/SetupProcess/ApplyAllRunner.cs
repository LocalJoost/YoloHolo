#region

using System;
using System.Collections;
using System.IO;
using System.Linq;
using MagicLeap.SetupTool.Editor.Interfaces;
using MagicLeap.SetupTool.Editor.Setup;
using MagicLeap.SetupTool.Editor.Utilities;
using UnityEditor;
using UnityEngine;

#endregion

namespace MagicLeap.SetupTool.Editor
{
    /// <summary>
    /// Manages the apply all action and runs through all of the configuration steps.
    /// </summary>
    public class ApplyAllRunner
    {
        
        #region EDITOR PREFS
        private const string CURRENT_INPUT_SYSTEM_PREF = "CURRENT_INPUT_SYSTEM_{0}";
        private const string MAGICLEAP_AUTO_SETUP_PREF = "MAGICLEAP-AUTO-SETUP";
        #endregion


        #region TEXT AND LABELS

            private const string APPLY_ALL_PROMPT_TITLE = "Configure all settings";
            private const string APPLY_ALL_PROMPT_MESSAGE = "This will update the project to the recommended settings for Magic Leap. Would you like to continue?";
            private const string APPLY_ALL_PROMPT_OK = "Continue";
            private const string APPLY_ALL_PROMPT_CANCEL = "Cancel";
            private const string APPLY_ALL_PROMPT_NOTHING_TO_DO_MESSAGE = "All settings are configured.";
            private const string APPLY_ALL_PROMPT_NOTHING_TO_DO_OK = "Close";

        #endregion

        private static int _currentApplyAllState = -1;
        private static int _current = -1;
        internal bool AllAutoStepsComplete => _stepsToComplete!= null && _stepsToComplete.All(step => step.IsComplete);

        private readonly ISetupStep[] _stepsToComplete;
        public static bool Running => (CurrentApplyAllState != -1);


        //Current step. -1 = done
        private static int CurrentApplyAllState
        {
            get => _currentApplyAllState;
            set
            {
                EditorPrefs.SetInt(MAGICLEAP_AUTO_SETUP_PREF, value);
                _currentApplyAllState = value;
            }
        }

        public ApplyAllRunner(params ISetupStep[] steps)
        {
            _stepsToComplete = steps;
            EditorApplication.quitting+= OnQuittingEditor;
        }

        private void OnQuittingEditor()
        {
            Stop();
        }


        internal static void Stop()
        {
            if (CurrentApplyAllState != -1)
            {
              
                EditorPrefs.SetInt("TEMP_" + MAGICLEAP_AUTO_SETUP_PREF, CurrentApplyAllState);
                CurrentApplyAllState = -1;
            }
        }

        internal void Tick()
        {
        
            var loading = AssetDatabase.IsAssetImportWorkerProcess() || EditorApplication.isCompiling || EditorApplication.isUpdating;

            if (CurrentApplyAllState != -1 && !loading)
            {
                ApplyAll();
            }
        }
        private void ApplyAll()
        {
        
            if (_stepsToComplete == null)
            {
                return;
            }
            
            if ( CurrentApplyAllState > _stepsToComplete.Length-1)
            {
                Stop();
                return;
            }

            if (!_stepsToComplete[_currentApplyAllState].CanExecute)
            {
                Stop();
                return;
            }





        
            if ((_currentApplyAllState >= 0 && _currentApplyAllState < _stepsToComplete.Length) && !_stepsToComplete[_currentApplyAllState].IsComplete)
            {

                _current = _currentApplyAllState;
                _stepsToComplete[_currentApplyAllState].Execute();
            }

            if ((_currentApplyAllState>=0 && _currentApplyAllState<_stepsToComplete.Length) && _stepsToComplete[_currentApplyAllState].Block)
            {
                if (_stepsToComplete[_currentApplyAllState].Busy)
                {
                    return;
                }
            }
            CurrentApplyAllState = _currentApplyAllState + 1;
            EditorPrefs.SetInt("TEMP_" + MAGICLEAP_AUTO_SETUP_PREF, CurrentApplyAllState);
           
        }
        
        public static void CheckLastAutoSetupState()
        {
          
            if (MagicLeapPackageUtility.IsMagicLeapSDKInstalled && EditorPrefs.GetInt(string.Format(CURRENT_INPUT_SYSTEM_PREF, EditorKeyUtility.GetProjectKey()), 0) != (int)UnityProjectSettingsUtility.InputSystemType)
            {
           
                var lastStep = EditorPrefs.GetInt("TEMP_" + MAGICLEAP_AUTO_SETUP_PREF, 0);
                CurrentApplyAllState = lastStep;
            }
            else
            {
             
                CurrentApplyAllState = EditorPrefs.GetInt(MAGICLEAP_AUTO_SETUP_PREF, -1);
                EditorPrefs.SetInt("TEMP_" + MAGICLEAP_AUTO_SETUP_PREF, -1);
            }
        }
    
        internal void RunApplyAll()
        {
            if (!AllAutoStepsComplete)
            {
                var dialogComplex = EditorUtility.DisplayDialog(APPLY_ALL_PROMPT_TITLE, APPLY_ALL_PROMPT_MESSAGE,APPLY_ALL_PROMPT_OK, APPLY_ALL_PROMPT_CANCEL);
                if (dialogComplex)
                {
                    EditorPrefs.SetInt(string.Format(CURRENT_INPUT_SYSTEM_PREF, EditorKeyUtility.GetProjectKey()), (int)UnityProjectSettingsUtility.InputSystemType);
                    CurrentApplyAllState = 0;
                }
                else
                {
                    CurrentApplyAllState = -1;
                }
            }
            else
            {
                EditorUtility.DisplayDialog(APPLY_ALL_PROMPT_TITLE, APPLY_ALL_PROMPT_NOTHING_TO_DO_MESSAGE,
                    APPLY_ALL_PROMPT_NOTHING_TO_DO_OK);
            }
        }


 

    }
}