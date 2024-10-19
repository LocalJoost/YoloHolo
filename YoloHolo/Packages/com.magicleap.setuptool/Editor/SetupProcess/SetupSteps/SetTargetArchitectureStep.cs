using System;
using MagicLeap.SetupTool.Editor.Interfaces;
using MagicLeap.SetupTool.Editor.Utilities;
using UnityEditor;
using UnityEngine;

namespace MagicLeap.SetupTool.Editor.Setup
{
	public class SetTargetArchitectureStep: ISetupStep
	{
		//Localization
		private const string FIX_SETTING_BUTTON_LABEL = "Fix Setting";
		private const string CONDITION_MET_LABEL = "Done";
		private const string SET_TARGET_ARCHEITECTURE_LABEL = "Change to X86_64 architecture";
		
		private bool _architectureSet;
		public bool CanExecute => EnableGUI();


		/// <inheritdoc />
		public Action OnExecuteFinished { get; set; }
		public bool Block => false;
		
		/// <inheritdoc />
		public bool Required => true;
		
		/// <inheritdoc />
		public bool Busy { get; private set; }
		/// <inheritdoc />
		public bool IsComplete => _architectureSet;

		private bool EnableGUI()
		{
		
			var correctBuildTarget = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android && MagicLeapPackageUtility.IsMagicLeapSDKInstalled;
			return correctBuildTarget;
		}

		/// <inheritdoc />
		public void Refresh()
		{
			_architectureSet = (((int)PlayerSettings.Android.targetArchitectures & (~(int)AndroidArchitecture.X86_64)) == 0);
		}
		/// <inheritdoc />
		public bool Draw()
		{
			GUI.enabled = EnableGUI();
			if (CustomGuiContent.CustomButtons.DrawConditionButton(SET_TARGET_ARCHEITECTURE_LABEL, _architectureSet, CONDITION_MET_LABEL, 
			FIX_SETTING_BUTTON_LABEL, Styles.FixButtonStyle))
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
			if (IsComplete)
			{
#if ML_SETUP_DEBUG
				Debug.Log($"Not executing step: {this.GetType().Name}. Busy: {Busy}");
#endif
				Busy = false;
				return;
			}
			PlayerSettings.Android.targetArchitectures = AndroidArchitecture.X86_64;
			_architectureSet = (((int)PlayerSettings.Android.targetArchitectures & (~(int)AndroidArchitecture.X86_64)) == 0);
			OnExecuteFinished?.Invoke();
			Busy = false;
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
			info += $"\nMore Info: Active Architecture: {_architectureSet}";

			return info;
		}

	}
}
