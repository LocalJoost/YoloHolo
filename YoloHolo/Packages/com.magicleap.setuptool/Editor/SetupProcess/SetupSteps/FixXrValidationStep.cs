using System;
using System.Collections.Generic;
using System.Linq;
using MagicLeap.SetupTool.Editor.Interfaces;
using MagicLeap.SetupTool.Editor.Utilities;
using UnityEditor;
#if MAGICLEAP
using UnityEngine;
using UnityEngine.XR.MagicLeap;
#endif

namespace MagicLeap.SetupTool.Editor.Setup
{
	public class FixValidationSetup: ISetupStep
	{

#if MAGICLEAP
		private static readonly List<MagicLeapProjectValidation.ValidationRule> _unityValidationFailures = new List<MagicLeapProjectValidation.ValidationRule>();
		private static List<MagicLeapProjectValidation.ValidationRule> _fixAllStack = new List<MagicLeapProjectValidation.ValidationRule>();
#endif

		private const string STEP_LABEL = "Apply Magic Leap XR Validation";
		private const string CONDITION_MISSING_TEXT = "Fix";
		private const string CONDITION_MET_LABEL = "Done";
		
		private bool _validationComplete;
		private bool _runningUnityValidationAutoFix;
		/// <inheritdoc />
		public Action OnExecuteFinished { get; set; }
		/// <inheritdoc />
		public bool Block => true;
		/// <inheritdoc />
		public bool Busy => _runningUnityValidationAutoFix;
		/// <inheritdoc />
		public bool IsComplete => _validationComplete;

		/// <inheritdoc />
		public bool CanExecute => MagicLeapPackageUtility.IsMagicLeapSDKInstalled && MagicLeapPackageUtility.IsMagicLeapXREnabled();

		/// <inheritdoc />
		public void Refresh()
		{
#if MAGICLEAP
			MagicLeapProjectValidation.GetCurrentValidationIssues(_unityValidationFailures);
			_validationComplete = !_unityValidationFailures.Any(i => i.fixIt != null && i.fixItAutomatic);
	
#endif

		}

		/// <inheritdoc />
		public bool Draw()
		{

			if (CustomGuiContent.CustomButtons.DrawConditionButton(STEP_LABEL, _validationComplete,
																	CONDITION_MET_LABEL, CONDITION_MISSING_TEXT, Styles.FixButtonStyle))
			{

				Execute();
				return true;
			}

			return false;
		}

		/// <inheritdoc />
		public void Execute()
		{
			if (_runningUnityValidationAutoFix)
			{
				return;
			}

			RunAutoFix();
		}

		public void RunAutoFix()
		{
		
#if MAGICLEAP
			_runningUnityValidationAutoFix = true;
			MagicLeapProjectValidation.GetCurrentValidationIssues(_unityValidationFailures);
			_fixAllStack = _unityValidationFailures.Where(i => i.fixIt != null && i.fixItAutomatic).ToList();
			EditorApplication.update += OnEditorUpdate;
#endif

		}

		private void OnEditorUpdate()
		{
#if MAGICLEAP
	
			if (!_runningUnityValidationAutoFix)
			{
				EditorApplication.update -= OnEditorUpdate;
				OnExecuteFinished?.Invoke();
			}

			if (_fixAllStack.Count > 0)
			{
				_fixAllStack[0].fixIt?.Invoke();
				_fixAllStack.RemoveAt(0);
			}

			if (_fixAllStack.Count == 0)
			{
				_runningUnityValidationAutoFix = false;
			}

#endif
		}


		public bool NoValidationSteps()
		{
#if MAGICLEAP
	return !_unityValidationFailures.Any(i => i.fixIt != null && i.fixItAutomatic);
#else
			return false;
#endif

		}




	}
}
