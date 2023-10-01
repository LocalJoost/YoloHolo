#region

using System;
using MagicLeap.SetupTool.Editor.Interfaces;
using MagicLeap.SetupTool.Editor.Utilities;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

#endregion

namespace MagicLeap.SetupTool.Editor.Setup
{
	/// <summary>
	/// Switches the build platform to Android
	/// </summary>
	public class BuildTargetSetupStep : ISetupStep, IActiveBuildTargetChanged
	{
		//Localization
		private const string SWITCHING_PLATFORM_PREF = "SWITCHING_PLATFORMS";
		private const string FIX_SETTING_BUTTON_LABEL = "Fix Setting";
		private const string CONDITION_MET_LABEL = "Done";
		private const string BUILD_SETTING_LABEL = "Set build target to Android";


		private const string FAILD_TO_SWITCH_BUILD_TARGET_DIALOG_HEADER = "Failed to set Active Build Target";
		private const string FAILD_TO_SWITCH_BUILD_TARGET_DIALOG_BODY = "Failed to automaticly switch to the Android Build Target. Please set the Build Target manually.";
		private const string FAILD_TO_SWITCH_BUILD_TARGET_DIALOG_OK = "Open Build Settings";
		private const string FAILD_TO_SWITCH_BUILD_TARGET_DIALOG_CANCEL = "Cancel";
		/// <inheritdoc />
		public int callbackOrder => 0;
		
		/// <inheritdoc />
		public Action OnExecuteFinished { get; set; }

		/// <inheritdoc />
		public bool Block => true;
		/// <inheritdoc />
		public bool Busy { get; private set; } = true;
		/// <inheritdoc />
		public bool IsComplete => _correctBuildTarget;
		public bool CanExecute  => true;
		private static bool _correctBuildTarget;
		
		/// <inheritdoc/>
		public void Refresh()
		{
		
			_correctBuildTarget = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
			Busy = EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android && EditorPrefs.GetInt(SWITCHING_PLATFORM_PREF, 1)==0;
	
		}

		/// <inheritdoc/>
		public bool Draw()
		{
			if (CustomGuiContent.CustomButtons.DrawConditionButton(BUILD_SETTING_LABEL, _correctBuildTarget,CONDITION_MET_LABEL,
			FIX_SETTING_BUTTON_LABEL, Styles.FixButtonStyle))
			{
				Execute();
				return true;
			}
			return false;
		}

		/// <inheritdoc/>
		public void Execute()
		{
			if (IsComplete || Busy)
			{
				return;
			}

			EditorPrefs.SetInt(SWITCHING_PLATFORM_PREF, 0);
			bool success = EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android, BuildTarget.Android);
			if (!success)
			{
				
				var openBuildTargetMenu = EditorUtility.DisplayDialog(FAILD_TO_SWITCH_BUILD_TARGET_DIALOG_HEADER, FAILD_TO_SWITCH_BUILD_TARGET_DIALOG_BODY, FAILD_TO_SWITCH_BUILD_TARGET_DIALOG_OK, FAILD_TO_SWITCH_BUILD_TARGET_DIALOG_CANCEL);
				if (openBuildTargetMenu)
				{
					EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
					EditorApplication.ExecuteMenuItem("File/Build Settings...");
				}
				else
				{
					ApplyAllRunner.Stop();
					EditorPrefs.SetInt(SWITCHING_PLATFORM_PREF, 1);
				}
			}
		}

		/// <inheritdoc />
		public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
		{
			_correctBuildTarget = newTarget == BuildTarget.Android;
			EditorPrefs.SetInt(SWITCHING_PLATFORM_PREF, 1);
	
			if (newTarget != BuildTarget.Android)
			{
				ApplyAllRunner.Stop();
			}

			OnExecuteFinished?.Invoke();
		}
	}
}
