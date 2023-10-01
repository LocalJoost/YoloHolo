using System;
using MagicLeap.SetupTool.Editor.Interfaces;
using MagicLeap.SetupTool.Editor.Utilities;
using UnityEditor;
using UnityEngine;

namespace MagicLeap.SetupTool.Editor.Setup
{
	public class SwitchActiveInputHandlerStep: ISetupStep
	{

		private const string FIX_SETTING_BUTTON_LABEL = "Fix Setting";
		private const string CONDITION_MET_LABEL = "Done";
		private const string ENABLE_NEW_INPUT_HANDLING_LABEL = "Enable New Input System";

		private const string USE_BOTH_BUTTON_LABEL = "Use New Input System";
		private const string USE_NEW_BUTTON_LABEL = "Enable Both";
		private const string UPDATE_INPUT_TITLE = "Change Active Input";
		private const string UPDATE_INPUT_MESSAGE = "The Magic Leap SDK requires enabling the Input System Package.";

		private const string RESTART_LATER_BUTTON_LABEL = "Restart Later";
		private const string RESTART_NOW_BUTTON_LABEL = "Restart Now";
		private const string RESTART_TITLE = "Restart Editor";
		private const string RESTART_EDITOR_MESSAGE = "A restart is required for the change to take effect. ";
		
		private bool _correctInputHandling;
		/// <inheritdoc />
		public Action OnExecuteFinished { get; set; }
		public bool Block => false;
		/// <inheritdoc />
		public bool Busy { get; private set; }
		/// <inheritdoc />
		public bool IsComplete => _correctInputHandling;
		public bool CanExecute => EnableGUI();
		
		/// <inheritdoc />
		public void Refresh()
		{
			_correctInputHandling = UnityProjectSettingsUtility.GetActiveInputHandler() > 0;
		
		}

		private bool EnableGUI()
		{
			var correctBuildTarget = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
			var hasSdkInstalled = MagicLeapPackageUtility.IsMagicLeapSDKInstalled;
			return correctBuildTarget && hasSdkInstalled && MagicLeapPackageUtility.IsMagicLeapXREnabled();

		}
		/// <inheritdoc />
		public bool Draw()
		{
			GUI.enabled = EnableGUI();
			if (CustomGuiContent.CustomButtons.DrawConditionButton(ENABLE_NEW_INPUT_HANDLING_LABEL, _correctInputHandling, CONDITION_MET_LABEL, FIX_SETTING_BUTTON_LABEL, Styles.FixButtonStyle))
			{
				Busy = true;
				Execute();
				return true;
			}
			return false;
		}


		/// <inheritdoc />
		public void Execute()
		{
			if (AssetDatabase.IsAssetImportWorkerProcess() || EditorApplication.isUpdating || EditorApplication.isCompiling)
			{
				return;
			}

			bool switchedInput;
			if (EditorUtility.DisplayDialog(UPDATE_INPUT_TITLE, UPDATE_INPUT_MESSAGE, USE_BOTH_BUTTON_LABEL, USE_NEW_BUTTON_LABEL))
			{
				EditorApplication.ExecuteMenuItem("File/Save Project");
				switchedInput= UnityProjectSettingsUtility.SetActiveInputHandler(2);

				if (EditorUtility.DisplayDialog(RESTART_TITLE,
												RESTART_EDITOR_MESSAGE, RESTART_NOW_BUTTON_LABEL, RESTART_LATER_BUTTON_LABEL))
				{
					EditorApplication.ExecuteMenuItem("File/Save Project");
					UnityProjectSettingsUtility.RequestCloseAndRelaunchWithCurrentArguments();
				}
			
			}
			else
			{
				EditorApplication.ExecuteMenuItem("File/Save Project");
				switchedInput = UnityProjectSettingsUtility.SetActiveInputHandler(1);
			}

			if (switchedInput)
			{
				if (EditorUtility.DisplayDialog(RESTART_TITLE,RESTART_EDITOR_MESSAGE, RESTART_NOW_BUTTON_LABEL, RESTART_LATER_BUTTON_LABEL))
				{
					UnityProjectSettingsUtility.RequestCloseAndRelaunchWithCurrentArguments();
				}
				else
				{
					UnityProjectSettingsUtility.SetActiveInputHandler(0);
				}
			}

			OnExecuteFinished?.Invoke();

			Busy = false;
			Refresh();
		}

	
	}
}
