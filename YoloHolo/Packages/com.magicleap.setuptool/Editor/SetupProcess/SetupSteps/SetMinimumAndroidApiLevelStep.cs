using System;
using MagicLeap.SetupTool.Editor.Interfaces;
using UnityEditor;
using UnityEngine;

namespace MagicLeap.SetupTool.Editor.Setup
{
	public class SetMinimumAndroidApiLevelStep : ISetupStep
	{
		//Localization
		private const string MINIMUM_API_LEVEL_LABEL = "Set Minimum Android API Level";
		private const string CONDITION_MET_LABEL = "Done";
		private const string FIX_SETTING_BUTTON_LABEL = "Fix Setting";

		private static bool _correctMinimumApiLevel;
		
		/// <inheritdoc />
		public Action OnExecuteFinished { get; set; }
		/// <inheritdoc />
		public bool Block => false;
		/// <inheritdoc />
		public bool Busy { get; private set; }
		/// <inheritdoc />
		public bool IsComplete => _correctMinimumApiLevel;
		/// <inheritdoc />
		public bool CanExecute => EnableGUI();
		
		/// <inheritdoc />
		public bool Required => true;

		private bool EnableGUI()
		{
			var correctBuildTarget = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
			return correctBuildTarget;
		}



		/// <inheritdoc />
		public void Refresh()
		{
			_correctMinimumApiLevel = PlayerSettings.Android.minSdkVersion == AndroidSdkVersions.AndroidApiLevel29;
			
		}

		/// <inheritdoc />
		public bool Draw()
		{
			GUI.enabled = EnableGUI();

			if (CustomGuiContent.CustomButtons.DrawConditionButton(MINIMUM_API_LEVEL_LABEL, _correctMinimumApiLevel,CONDITION_MET_LABEL, FIX_SETTING_BUTTON_LABEL, Styles.FixButtonStyle))
			{

				Execute();
				return true;
			}

			return false;
		}

		/// <inheritdoc />
		public void Execute()
		{
			Busy = true;
			PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;

			EditorApplication.delayCall += () =>
											{
												_correctMinimumApiLevel = PlayerSettings.Android.minSdkVersion == AndroidSdkVersions.AndroidApiLevel29;
												Busy = false;
												OnExecuteFinished?.Invoke();
#if ML_SETUP_DEBUG
												Debug.Log($"{this.GetType().Name} finished.");
#endif
											};
		}
		/// <inheritdoc cref="ISetupStep.ToString"/>
		public override string ToString()
		{
			var info =$"Step: {this.GetType().Name}, CanExecute: {CanExecute}, Busy: {Busy}, IsComplete: {IsComplete}";
            
			if (!EnableGUI())
			{
				var correctBuildTarget = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
				info += "\nDisabling GUI: ";
				if (!correctBuildTarget)
				{
					info += "[not the correct build target], ";
				}
			}
			info += $"\nMore Info: MinSdkVersion: {PlayerSettings.Android.minSdkVersion }";

			return info;
		}


	}
}
