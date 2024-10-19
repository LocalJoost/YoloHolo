using System;
using System.Linq;
using MagicLeap.SetupTool.Editor.Interfaces;
using MagicLeap.SetupTool.Editor.Utilities;
using UnityEditor;
using UnityEngine;

namespace MagicLeap.SetupTool.Editor.Setup
{
	public class SetDefaultTextureCompressionStep : ISetupStep
	{
		//Localization
		private const string REQUIRED_TEXTURE_COMPRESSION_LABEL_DXTC_RGTC = "DXTC_RGTC";
		private const string REQUIRED_NORMAL_MAP_COMPRESSION_LABEL = "DXT5nm";
		private const string REQUIRED_TEXTURE_COMPRESSION_LABEL_DXTC = "DXTC";
		private const string FIX_SETTING_BUTTON_LABEL = "Fix Setting";
		private const string CONDITION_MET_LABEL = "Done";
		private const string SET_TEXTURE_COMPRESSION_LABEL = "Use DXT texture compression";
		private const string SET_NORMAL_MAP_COMPRESSION_LABEL = "Use DXT5nm normal compression";
		private static bool _isTextureCompressionSet = false;
		private static bool _isNormalMapCompressionSet = false;
		public bool CanExecute => EnableGUI();
		/// <inheritdoc />
		public Action OnExecuteFinished { get; set; }
		public bool Block => true;

		/// <inheritdoc />
		public bool Required => true;
		
		/// <inheritdoc />
		public bool Busy { get; private set; }
		/// <inheritdoc />
		public bool IsComplete => _isTextureCompressionSet && _isNormalMapCompressionSet;

		/// <inheritdoc />
		public void Refresh()
		{
			_isNormalMapCompressionSet = UnityProjectSettingsUtility.IsNormalMapCompressionSet(BuildTargetGroup.Android,REQUIRED_NORMAL_MAP_COMPRESSION_LABEL);
			_isTextureCompressionSet = UnityProjectSettingsUtility.IsTextureCompressionSet(BuildTargetGroup.Android, REQUIRED_TEXTURE_COMPRESSION_LABEL_DXTC_RGTC) || UnityProjectSettingsUtility.IsTextureCompressionSet(BuildTargetGroup.Android, REQUIRED_TEXTURE_COMPRESSION_LABEL_DXTC);
		
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
			if (!_isTextureCompressionSet || _isNormalMapCompressionSet)
			{
				if (CustomGuiContent.CustomButtons.DrawConditionButton(SET_TEXTURE_COMPRESSION_LABEL,
					    _isTextureCompressionSet, CONDITION_MET_LABEL, FIX_SETTING_BUTTON_LABEL, Styles.FixButtonStyle))
				{
					Busy = true;
					Execute();
					return true;
				}
			}
			else
			{
			
					if (CustomGuiContent.CustomButtons.DrawConditionButton(SET_NORMAL_MAP_COMPRESSION_LABEL,
						    _isNormalMapCompressionSet, CONDITION_MET_LABEL, FIX_SETTING_BUTTON_LABEL, Styles.FixButtonStyle))
					{
						Busy = true;
						Execute();
						return true;
					}
				
			}
			

			return false;
		}
		
		/// <inheritdoc />
		public void Execute()
		{
			if (IsComplete)
			{
#if ML_SETUP_DEBUG
				Debug.Log($"Not executing step: {this.GetType().Name}.IsComplete: {IsComplete} || Busy: {Busy}");
#endif
				Busy = false;
				return;
			}
			if (_isNormalMapCompressionSet && _isTextureCompressionSet)
			{
				return;
			}
		
			if (!_isNormalMapCompressionSet)
			{
				UnityProjectSettingsUtility.SetNormalMapCompression(BuildTargetGroup.Android, REQUIRED_NORMAL_MAP_COMPRESSION_LABEL);
			}
			if (!_isTextureCompressionSet)
			{
				UnityProjectSettingsUtility.SetTextureCompression(BuildTargetGroup.Android, REQUIRED_TEXTURE_COMPRESSION_LABEL_DXTC_RGTC);
			}

			Refresh();
			OnExecuteFinished?.Invoke();
			
#if ML_SETUP_DEBUG
			Debug.Log($"{this.GetType().Name} finished.");
#endif
			Busy = false;
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
			info += $"\nMore Info: NormalMapCompressionSet: {_isNormalMapCompressionSet }, TextureCompressionSet: {_isTextureCompressionSet}";

			return info;
		}

	}
}
