﻿using System;
using MagicLeap.SetupTool.Editor.Interfaces;
using MagicLeap.SetupTool.Editor.Utilities;
using UnityEditor;
using UnityEngine;

namespace MagicLeap.SetupTool.Editor.Setup
{
	public class SetScriptingBackendStep: ISetupStep
	{
		//Localization
		private const string FIX_SETTING_BUTTON_LABEL = "Fix Setting";
		private const string CONDITION_MET_LABEL = "Done";
		private const string SET_SCRIPTING_BACKEND_LABEL = "Set IL2CPP scripting backend";
		
		private bool _correctScriptingBackend;
		public bool CanExecute => EnableGUI();
		/// <inheritdoc />
		public Action OnExecuteFinished { get; set; }
		public bool Block => false;
		/// <inheritdoc />
		public bool Busy { get; private set; }
		/// <inheritdoc />
		public bool IsComplete => _correctScriptingBackend;
		/// <inheritdoc />
		public void Refresh()
		{
			
			_correctScriptingBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP;
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
			if (CustomGuiContent.CustomButtons.DrawConditionButton(SET_SCRIPTING_BACKEND_LABEL, _correctScriptingBackend, CONDITION_MET_LABEL, FIX_SETTING_BUTTON_LABEL, Styles.FixButtonStyle))
			{
				Busy = true;
				Execute();
				return true;
			}

			return false;
		}

		/// <inheritdoc />
		public void Execute() {
			if (IsComplete)
			{
				Busy = false;
				return;
			}
			PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
			_correctScriptingBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP;
			Busy = false;
			OnExecuteFinished?.Invoke();
		}

		
	}
}
