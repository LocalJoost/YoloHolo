
#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ThirdParty.SimpleJson;
using UnityEditor;

using UnityEditorInternal;
using UnityEngine;
#if (OpenXR)
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEditor.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;

using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.Management;

#endif

#endregion

namespace MagicLeap.SetupTool.Editor.Utilities
{
	/// <summary>
	/// Script responsible for giving access to the sdk calls using reflections.
	/// </summary>
	public static class XRPackageUtility
	{

		#region LOG MESSAGES

		private const string ERROR_FAILED_TO_CREATE_WINDOW = "Failed to create a view for XR Manager Settings Instance";
		private const string XR_CANNOT_BE_FOUND = "Current XR Settings Cannot be found";
		private const string LOADER_PROP_CANNOT_BE_FOUND = "Loader Prop [m_LoaderManagerInstance] Cannot be found";
		private const string SETTINGS_NOT_FOUND = "Settings not Found";
		private const string FEATURE_NOT_INSTALLED_ERROR = "Feature [{0}] cannot be enabled because it is not installed";
		private const string FEATURE_NOT_FOUND_ERROR = "Feature [{0}] not found";
		private const string FAILED_TO_ENABLE_XR_PLUGIN_ERROR = "Could not enable XR Loader of type <{0}>. Please enable it manually.";

	#endregion
	
	private const string LOADER_ID = "OpenXRLoader"; // Used to test if the loader is installed and active.
	private const string FEATURE_SET_ID = "com.magicleap.openxr.featuregroup";

	#if (OpenXR)
		private static readonly Type _cachedXRSettingsManagerType =
			Type.GetType("UnityEditor.XR.Management.XRSettingsManager,Unity.XR.Management.Editor");

		private static readonly PropertyInfo _cachedXRSettingsProperty =
			_cachedXRSettingsManagerType?.GetProperty("currentSettings",
													BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

		private static readonly MethodInfo _cachedCreateXRSettingsMethod =
			_cachedXRSettingsManagerType?.GetMethod("Create",
													BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

		private static readonly MethodInfo _cachedCreateAllChildSettingsProvidersMethod =
			_cachedXRSettingsManagerType?.GetMethod("CreateAllChildSettingsProviders",
													BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

	
	
		private static XRGeneralSettingsPerBuildTarget _currentSettings
		{
			get
			{
				var settings = (XRGeneralSettingsPerBuildTarget)_cachedXRSettingsProperty?.GetValue(null);

				return settings;
			}
		}
	#endif

		public static Action<bool> EnableXRPluginFinished;

		public static bool IsMagicLeapOpenXREnabled()
		{
	#if OpenXR
			var xrPluginSettingsEnabled = XRPackageUtility.XRPluginEnabled(LOADER_ID, BuildTargetGroup.Android);
			var xrFeatureSetEnabled = XRPackageUtility.XRFeatureSetEnabled(FEATURE_SET_ID, BuildTargetGroup.Android);
			return xrPluginSettingsEnabled && xrFeatureSetEnabled;
	#else
			return false;
	#endif
		}
		
		public static bool HasSDKInstalled
		{
			get
			{
#if (OpenXR)
				return true;
#else
                return  false;
#endif
			}
		}


		/// <summary>
		/// Refreshes the BuildTargetGroup XR Loader
		/// </summary>
		/// <param name="buildTargetGroup"> </param>
		private static void UpdateLoader(BuildTargetGroup buildTargetGroup)
		{
#if (OpenXR)

		
				if (_currentSettings == null)
				{
					Debug.LogError(XR_CANNOT_BE_FOUND);
					return;
				}
				var settings = _currentSettings.SettingsForBuildTarget(buildTargetGroup);

				if (settings == null)
				{
					settings = ScriptableObject.CreateInstance<XRGeneralSettings>();
					_currentSettings.SetSettingsForBuildTarget(buildTargetGroup, settings);
					settings.name = $"{buildTargetGroup.ToString()} Settings";
					AssetDatabase.AddObjectToAsset(settings, AssetDatabase.GetAssetOrScenePath(_currentSettings));
				}

				var serializedSettingsObject = new SerializedObject(settings);
				serializedSettingsObject.Update();
				AssetDatabase.Refresh();

				var loaderProp = serializedSettingsObject.FindProperty("m_LoaderManagerInstance");
				if (loaderProp == null)
				{
					Debug.LogError(LOADER_PROP_CANNOT_BE_FOUND);
					return;
				}
				if (loaderProp.objectReferenceValue == null)
				{
					var xrManagerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
					xrManagerSettings.name = $"{buildTargetGroup.ToString()} Providers";
					AssetDatabase.AddObjectToAsset(xrManagerSettings,AssetDatabase.GetAssetOrScenePath(_currentSettings));
					loaderProp.objectReferenceValue = xrManagerSettings;
					serializedSettingsObject.ApplyModifiedProperties();
					var serializedManagerSettingsObject = new SerializedObject(xrManagerSettings);
					xrManagerSettings.InitializeLoaderSync();
					serializedManagerSettingsObject.ApplyModifiedProperties();
					serializedManagerSettingsObject.Update();
					AssetDatabase.Refresh();
				}



				serializedSettingsObject.ApplyModifiedProperties();
				serializedSettingsObject.Update();
				UnityProjectSettingsUtility.OpenXRManagementWindow();
				EditorApplication.delayCall += () =>
												{
													var obj = loaderProp.objectReferenceValue;

													if (obj != null)
													{
														loaderProp.objectReferenceValue = obj;

														var e = UnityEditor.Editor.CreateEditor(obj);


														if (e == null)
														{
															Debug.LogError(ERROR_FAILED_TO_CREATE_WINDOW);
														}
														else
														{
															InternalEditorUtility.RepaintAllViews();
															AssetDatabase.Refresh();
															e.serializedObject.Update();
															try {
																var updateBuild = e.GetType().GetProperty("BuildTarget", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
																updateBuild.SetValue(e, (object)buildTargetGroup, null);
															}
															catch (Exception exception)
															{
																Debug.LogException(exception);
															}
											

														}
													}
													else if (obj == null)
													{
														settings.AssignedSettings = null;
														loaderProp.objectReferenceValue = null;
													}
												};


#endif
		}



	
		

		public static void EnableXRFeatureSet(string featureSetId, BuildTargetGroup buildTargetGroup)
		{
#if (OpenXR)
			var featureSet = OpenXRFeatureSetManager.FeatureSetsForBuildTarget(buildTargetGroup).FirstOrDefault((set => set.featureSetId== featureSetId));
			if (featureSet == null)
			{
				Debug.LogError(string.Format(FEATURE_NOT_FOUND_ERROR, featureSetId));
				return;
			}
			
			if (!featureSet.isInstalled)
			{
				Debug.LogError(string.Format(FEATURE_NOT_INSTALLED_ERROR, featureSetId));
				return;
			}

			featureSet.isEnabled = true;
			
			OpenXRFeatureSetManager.SetFeaturesFromEnabledFeatureSets(buildTargetGroup);
			AssetDatabase.SaveAssets();
			
#endif
		}

		public static bool XRFeatureSetEnabled(string featureSetId, BuildTargetGroup buildTargetGroup)
		{
#if (OpenXR)
			var featureSet = OpenXRFeatureSetManager.FeatureSetsForBuildTarget(buildTargetGroup).FirstOrDefault((set => set.featureSetId == featureSetId));
			return featureSet!=null && featureSet.isEnabled;
#else
			return false;
#endif
		}

		
		public static void EnableXRInteractionFeature(BuildTargetGroup buildTargetGroup,
			string interactionFeatureName)
		{
#if OpenXR
			var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(buildTargetGroup);
			if (settings == null)
			{
				Debug.LogWarning("Cannot setup interaction profile. XR Settings do not exist.");
				return;
			}
            
			var interactionProfiles = settings.GetFeatures<OpenXRInteractionFeature>().FirstOrDefault((set => set.name.Contains(interactionFeatureName)));
			if (interactionProfiles == null)
			{
				Debug.LogError("Cannot enable interaction profile. Profile does not exist.");
				return;
			}

			interactionProfiles.enabled = true;
			UnityEditor.EditorUtility.SetDirty(settings);
			AssetDatabase.SaveAssets();
			
#endif
		}
		
		public static bool XRInteractionFeatureEnabled(BuildTargetGroup buildTargetGroup,
			string interactionFeatureName)
		{
#if OpenXR
			var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(buildTargetGroup);
			if (settings == null)
			{
				Debug.LogWarning("Cannot check interaction profile. OpenXR Settings do not exist.");
				return false;
			}
            
			var interactionProfiles = settings.GetFeatures<OpenXRInteractionFeature>().FirstOrDefault((set => set.name == interactionFeatureName));

			return interactionProfiles!=null && interactionProfiles.enabled;
		
#else
			return false;
#endif
		}

		public static bool XRFeatureEnabled(string featureSetId, string feature, BuildTargetGroup buildTargetGroup)
		{
#if (OpenXR)
			foreach (var set in OpenXRFeatureSetManager.FeatureSetsForBuildTarget(buildTargetGroup))
			{
				foreach (var featureId in set.featureIds)
				{
					Debug.Log($"[{set.name}] {set.featureSetId} - {featureId}");
				}
			}
			var featureSet = OpenXRFeatureSetManager.FeatureSetsForBuildTarget(buildTargetGroup).FirstOrDefault((set => set.featureSetId == featureSetId));
			return featureSet!=null && featureSet.isEnabled;
#else
			return false;
#endif
		}
#if (OpenXR)
		/// <summary>
		/// Enables the XR plugin on the available Build Target Group
		/// </summary>
		public static void EnableXRPlugin<T>(BuildTargetGroup buildTargetGroup) where T: OpenXRLoaderBase
		{


			_cachedCreateXRSettingsMethod.Invoke(_cachedXRSettingsManagerType, null);
			_cachedCreateAllChildSettingsProvidersMethod.Invoke(_cachedXRSettingsManagerType, null);


			UpdateLoader(buildTargetGroup);
		

			EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,
													out XRGeneralSettingsPerBuildTarget xrGeneralSettingsPerBuildTarget);

			if (xrGeneralSettingsPerBuildTarget)
			{
				var androidSettings = xrGeneralSettingsPerBuildTarget.SettingsForBuildTarget(buildTargetGroup);

				
				var loader = ScriptableObjectUtility.FindAsset<T>();
				var worked =  androidSettings.Manager.TryAddLoader(loader);
		
			if (!worked){
			
				Debug.LogError(string.Format(FAILED_TO_ENABLE_XR_PLUGIN_ERROR, typeof(T).Name));
			}
				EnableXRPluginFinished.Invoke(true);
			}
			else
			{
				EnableXRPluginFinished.Invoke(false);
				Debug.LogWarning(SETTINGS_NOT_FOUND);
			}


		}
#endif

		/// <summary>
		/// Checks if the XR platform is enabled
		/// </summary>
		/// <returns> </returns>
		public static bool XRPluginEnabled(string loaderId, BuildTargetGroup buildTargetGroup)
		{
#if (OpenXR) 
			EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,out XRGeneralSettingsPerBuildTarget xrGeneralSettingsPerBuildTarget);
			var hasXRLoader = false;
			if (xrGeneralSettingsPerBuildTarget == null)
			{
				return false;
			}


			if (xrGeneralSettingsPerBuildTarget != null)
			{
				var settingsForBuildTarget = xrGeneralSettingsPerBuildTarget.SettingsForBuildTarget(buildTargetGroup);
				if (settingsForBuildTarget != null && settingsForBuildTarget.Manager != null)
				{
					hasXRLoader = settingsForBuildTarget.Manager.activeLoaders.Any(e =>
																			{
																				var fullName = e.GetType().FullName;
																				return fullName != null && fullName.Contains(loaderId);
																			});
				}
			
			}
			return hasXRLoader;
#else
			return false;
#endif

		}

	}
}
