#region

using System.Linq;
using MagicLeap.SetupTool.Editor.Interfaces;
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
        private const string RUNNER_STARTED_PREF = "START-AUTO-SETUP";
        #endregion


        #region TEXT AND LABELS

            private const string APPLY_ALL_PROMPT_TITLE = "Configure all settings";
            private const string APPLY_ALL_PROMPT_MESSAGE = "This will update the project to the recommended settings for Magic Leap. Would you like to continue?";
            private const string APPLY_ALL_PROMPT_OK = "Continue";
            private const string APPLY_ALL_PROMPT_CANCEL = "Cancel";
            private const string APPLY_ALL_PROMPT_NOTHING_TO_DO_MESSAGE = "All settings are configured.";
            private const string APPLY_ALL_PROMPT_NOTHING_TO_DO_OK = "Close";

        #endregion

        private static int _currentStep = -1;
        internal bool AllAutoStepsComplete => _stepsToComplete!= null && _stepsToComplete.All(step => step.IsComplete);

        private readonly ISetupStep[] _stepsToComplete;
        public static bool Running => (CurrentStep != -1);


        //Current step. -1 = done
        private static int CurrentStep
        {
            get => _currentStep;
            set
            {
                EditorPrefs.SetInt(MAGICLEAP_AUTO_SETUP_PREF, value);
                _currentStep = value;
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
            if (CurrentStep != -1)
            {
#if ML_SETUP_DEBUG
                Debug.Log($"Stop");
#endif
                EditorPrefs.SetInt("TEMP_" + MAGICLEAP_AUTO_SETUP_PREF, CurrentStep);
                CurrentStep = -1;
            }
        }

        internal void Tick()
        {
        
            var loading = AssetDatabase.IsAssetImportWorkerProcess() || EditorApplication.isCompiling || EditorApplication.isUpdating;

            if (CurrentStep != -1 && !loading)
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

          
            
            if (CurrentStep<0 || CurrentStep >= _stepsToComplete.Length )
            {
#if ML_SETUP_DEBUG
                Debug.Log($"Finished: Setting Prefs");
#endif
                Stop();
                EditorPrefs.SetInt("TEMP_" + MAGICLEAP_AUTO_SETUP_PREF, -1);
                EditorPrefs.SetInt(RUNNER_STARTED_PREF, 0);
                return;
            }

            if (_stepsToComplete[_currentStep].Block)
            {

                if (_stepsToComplete[_currentStep].Busy)
                {
#if ML_SETUP_DEBUG
                    Debug.Log($"{_stepsToComplete[_currentStep]}: Is Busy");
#endif
                    return;
                }
            }
            if (_stepsToComplete[CurrentStep].IsComplete)
            {
#if ML_SETUP_DEBUG
                Debug.Log($"Move to next step");
#endif
                CurrentStep += 1;
                return;
            }
            
            if (!_stepsToComplete[CurrentStep].CanExecute)
            {
#if ML_SETUP_DEBUG
                Debug.LogError($"{_stepsToComplete[_currentStep]}: Cannot execute!");
#endif
                EditorPrefs.SetInt(RUNNER_STARTED_PREF, 0);
                Stop();
                return;
            }
        
            if (!_stepsToComplete[CurrentStep].IsComplete)
            {
#if ML_SETUP_DEBUG
                Debug.Log($"{_stepsToComplete[_currentStep]}: Execute");
#endif
                _stepsToComplete[_currentStep].Execute();
            }


            CurrentStep = _currentStep + 1;
#if ML_SETUP_DEBUG
            Debug.Log($"Set [{MAGICLEAP_AUTO_SETUP_PREF}] to {CurrentStep}");
            Debug.Log($"Set [TEMP_{MAGICLEAP_AUTO_SETUP_PREF}] to {CurrentStep}");
#endif
            EditorPrefs.SetInt("TEMP_" + MAGICLEAP_AUTO_SETUP_PREF, _currentStep + 1);
        }
        public static bool WasRunning()
        {
            var runnerStarted = EditorPrefs.GetInt(RUNNER_STARTED_PREF, 0);
            return (runnerStarted == 1 &&
                    EditorPrefs.GetInt(string.Format(CURRENT_INPUT_SYSTEM_PREF, EditorKeyUtility.GetProjectKey()), 0) !=
                    (int)UnityProjectSettingsUtility.InputSystemType);

        }
        public static void CheckLastAutoSetupState()
        {
             var runnerStarted = EditorPrefs.GetInt(RUNNER_STARTED_PREF, 0);
             if (runnerStarted == 1 && MagicLeapPackageUtility.IsMagicLeapSDKInstalled && EditorPrefs.GetInt(string.Format(CURRENT_INPUT_SYSTEM_PREF, EditorKeyUtility.GetProjectKey()), 0) != (int)UnityProjectSettingsUtility.InputSystemType)
             {
#if ML_SETUP_DEBUG
                 Debug.Log($"resume");
#endif
                 //resume
                 CurrentStep = 0;
             }
             else
             {
                 CurrentStep = EditorPrefs.GetInt(MAGICLEAP_AUTO_SETUP_PREF, -1);
                 EditorPrefs.SetInt("TEMP_" + MAGICLEAP_AUTO_SETUP_PREF, -1);
#if ML_SETUP_DEBUG
                 Debug.Log($"Stop");
#endif
             }
        }
    
        internal void RunApplyAll()
        {
            if (!AllAutoStepsComplete)
            {
                EditorPrefs.SetInt(RUNNER_STARTED_PREF, 1);
                var dialogComplex = EditorUtility.DisplayDialog(APPLY_ALL_PROMPT_TITLE, APPLY_ALL_PROMPT_MESSAGE,APPLY_ALL_PROMPT_OK, APPLY_ALL_PROMPT_CANCEL);
                if (dialogComplex)
                {
                    EditorPrefs.SetInt(string.Format(CURRENT_INPUT_SYSTEM_PREF, EditorKeyUtility.GetProjectKey()), (int)UnityProjectSettingsUtility.InputSystemType);
                    CurrentStep = 0;
                }
                else
                {
                    CurrentStep = -1;
                }
            }
            else
            {
                EditorUtility.DisplayDialog(APPLY_ALL_PROMPT_TITLE, APPLY_ALL_PROMPT_NOTHING_TO_DO_MESSAGE,APPLY_ALL_PROMPT_NOTHING_TO_DO_OK);
                EditorPrefs.SetInt(RUNNER_STARTED_PREF, 0);
            }
        }


 

    }
}