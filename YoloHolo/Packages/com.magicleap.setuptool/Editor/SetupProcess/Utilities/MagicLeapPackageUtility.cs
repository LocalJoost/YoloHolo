
#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ThirdParty.SimpleJson;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
#if MAGICLEAP
using UnityEditor.XR.Management;

#if UNITY_MAGICLEAP
using UnityEngine.XR.MagicLeap;
#endif
using UnityEngine.XR.Management;


#endif

#endregion

namespace MagicLeap.SetupTool.Editor.Utilities
{
	/// <summary>
	/// Script responsible for giving access to the sdk calls using reflections.
	/// </summary>
	public static class MagicLeapPackageUtility
	{

		private const string SDK_LOCATION_PREF_KEY = "MAGICLEAP_SDK_LOCATION";
		private const string MAGIC_LEAP_DEFINES_SYMBOL = "MAGICLEAP";
		private const string MAGIC_LEAP_PACKAGE_ID = "com.magicleap.unitysdk";                                                   // Used to check if the build platform is installed
		private const string MAGIC_LEAP_LOADER_ID = "MagicLeapLoader";                                                           // Used to test if the loader is installed and active
		private const string MINIMUM_API_LEVEL_EDITOR_PREF_KEY = "MagicLeap.Permissions.MinimumAPILevelDropdownValue_{0}";       //used to set and check the api level [key is an internal variable set by Unity]
		private const string SDK_PATH_EDITOR_PREF_KEY = "MagicLeapSDKRoot";                                                      //used to set and check the sdk path [key is an internal variable set by Unity]
		private const string SDK_PACKAGE_MANAGER_PATH_RELATIVE_TO_SDK_ROOT = "../../tools/unity/{0}/com.magicleap.unitysdk.tgz"; //The path to the Package Manager folder relative to the SDK Root | {0} is the sdk version
		private const string PERMISSIONS_API_LEVEL_KEY = "min_api_level";

		public static Action<bool> EnableMagicLeapXRFinished;
		public static Action<bool> EnableAppSimulatorFinished;
		public static string SdkRoot => EditorPrefs.GetString(SDK_PATH_EDITOR_PREF_KEY, null);
		
		public static bool HasRootSDKPath {

			get
			{
				var sdkRoot = SdkRoot;
				return !string.IsNullOrEmpty(sdkRoot) && Directory.Exists(sdkRoot) && File.Exists(Path.Combine(sdkRoot, ".metadata", "sdk.manifest"));
			}
		}

		private static string _mlPermissionsPath => Path.Combine(SdkRoot, "data", "ml_permissions.json");
		private static string _minimumApiLevelEditorPrefKey => string.Format(MINIMUM_API_LEVEL_EDITOR_PREF_KEY, PlayerSettings.applicationIdentifier);
		
		public static bool IsMagicLeapSDKInstalled
		{
			get
			{
#if (MAGICLEAP || OpenXR||USE_INPUT_SYSTEM_POSE_CONTROL)
				return true;
#else
                return  false;
#endif
			}
		}

		public static string DefaultUnityPackagePath => Path.GetFullPath(Path.Combine(EditorPrefs.GetString(SDK_PATH_EDITOR_PREF_KEY), string.Format(SDK_PACKAGE_MANAGER_PATH_RELATIVE_TO_SDK_ROOT, GetSdkFolderName())));

		private static string _userSelectedUnityPackagePath;




		public static int MinimumAPILevel
		{
			get
			{
				if (HasRootSDKPath)
				{
					var apiLevelLabel = EditorPrefs.GetString(_minimumApiLevelEditorPrefKey, "API Level 20");
					var labelToNumber = apiLevelLabel.Substring(10, apiLevelLabel.Length - 10);
					if (int.TryParse(labelToNumber, out var apiLevel))
					{
						return apiLevel;
					}

					Debug.LogWarning($"Could not parse [{labelToNumber}] as an API level");
					return -1;
				}

				return -1;
			}
			set
			{
				if (HasRootSDKPath)
				{
					var max = GetMaxAPILevel();
					if (value > max)
					{
						value = max;
					}

					EditorPrefs.SetString(_minimumApiLevelEditorPrefKey, $"API Level {value}");
				}
			}
		}

		public static string GetSdkVersion()
		{
			
			return Regex.Replace(GetSdkFolderName(), "v", "", RegexOptions.IgnoreCase);
		}

		public static string GetUnityPackageDirectory()
		{
			var root = Environment.GetEnvironmentVariable("USERPROFILE") ?? Environment.GetEnvironmentVariable("HOME");
			if (!string.IsNullOrEmpty(root))
			{
				var sdkRoot = Path.Combine(root, "MagicLeap/tools/unity");
				if (string.IsNullOrEmpty(sdkRoot) || !Directory.Exists(sdkRoot))
				{
					var prefsRoot = EditorPrefs.GetString(SDK_LOCATION_PREF_KEY, null);
					if (!string.IsNullOrEmpty(prefsRoot))
					{
						sdkRoot = Path.Combine(prefsRoot, "tools/unity");
						if (Directory.Exists(sdkRoot))
						{
							return sdkRoot.Replace("\\", "/");
						}
					}
				}
				else
				{
					return sdkRoot.Replace("\\", "/");
				}
			}
			else
			{
				var prefsRoot = EditorPrefs.GetString(SDK_LOCATION_PREF_KEY, null);
				if (!string.IsNullOrEmpty(prefsRoot))
				{
					var sdkRoot = Path.Combine(prefsRoot, "tools/unity");
					if (Directory.Exists(sdkRoot))
					{
						return sdkRoot.Replace("\\", "/");
					}
				}
			}

			return null;
		}

		public static string GetSDKDirectory()
		{
			var root = Environment.GetEnvironmentVariable("USERPROFILE") ?? Environment.GetEnvironmentVariable("HOME");
			if (!string.IsNullOrEmpty(root))
			{
				var sdkRoot = Path.Combine(root, "MagicLeap/mlsdk");
				if (string.IsNullOrEmpty(sdkRoot) || !Directory.Exists(sdkRoot))
				{
					var prefsRoot = EditorPrefs.GetString(SDK_LOCATION_PREF_KEY, null);
					if (!string.IsNullOrEmpty(prefsRoot))
					{
						sdkRoot = Path.Combine(prefsRoot, "mlsdk");
						if (Directory.Exists(sdkRoot))
						{
							return sdkRoot.Replace("\\", "/");
						}
					}
				}
				else
				{
					return sdkRoot.Replace("\\", "/");
				}
			}
			else
			{
				var prefsRoot = EditorPrefs.GetString(SDK_LOCATION_PREF_KEY, null);
				if (!string.IsNullOrEmpty(prefsRoot))
				{
					var sdkRoot = Path.Combine(prefsRoot, "mlsdk");
					if (Directory.Exists(sdkRoot))
					{
						return sdkRoot.Replace("\\", "/");
					}
				}
			}

			return null;
		}
		public static string GetLatestUnityPackagePath()
		{


			var root = Environment.GetEnvironmentVariable("USERPROFILE") ?? Environment.GetEnvironmentVariable("HOME");


			if (!string.IsNullOrEmpty(root))
			{
				var sdkRoot = Path.Combine(root, "MagicLeap/tools/unity");
				if (string.IsNullOrEmpty(sdkRoot) || !Directory.Exists(sdkRoot))
				{
					var prefsRoot = EditorPrefs.GetString(SDK_LOCATION_PREF_KEY, null);
					if (!string.IsNullOrEmpty(prefsRoot))
					{
						sdkRoot = Path.Combine(prefsRoot, "tools/unity");
					}
				}
				
				if (!string.IsNullOrEmpty(sdkRoot)  && Directory.Exists(sdkRoot))
				{
					var getVersionDirectories = Directory.EnumerateDirectories(sdkRoot, "v*").ToList();

					getVersionDirectories.RemoveAll((e) => !File.Exists(Path.Combine(e, "com.magicleap.unitysdk.tgz")));

					getVersionDirectories.Sort(new VersionComparer());
					
					if (getVersionDirectories.Count == 0)
						return null;

					return Path.Combine(getVersionDirectories[getVersionDirectories.Count-1], "com.magicleap.unitysdk.tgz");
				}
			}


			return null;
		}

		public static void SetSDKEditorPrefLocation(string location)
		{
			location = location.Replace("\\", "/");
			var validSDK = File.Exists(Path.Combine(location, ".metadata", "sdk.manifest"));
			if (validSDK)
			{
				var sdkParent = Directory.GetParent(location);
				if (sdkParent == null)
				{
					Debug.LogError($"Failed to find SDK parent directory from path: {location}");
				}
				else
				{
					var sdkroot = sdkParent.Parent;
					if (sdkroot == null)
					{
						Debug.LogError($"Failed to find SDK parent directory from path: {location}. Found {sdkParent.FullName}");
					}
					else
					{
						EditorPrefs.SetString(SDK_LOCATION_PREF_KEY, sdkroot.FullName);
					}
			
				}
			}
			else
			{
				bool foundSdk = false;

				if (Directory.Exists(location))
				{

					foreach (string dir in Directory.EnumerateDirectories(location))
					{
						string manifestPath = Path.Combine(dir, ".metadata", "sdk.manifest");
						if (File.Exists(manifestPath))
						{
							foundSdk = true;
							break;
						}
					}

					if (foundSdk)
					{
						DirectoryInfo info = new DirectoryInfo(location).Parent;
						if (info== null)
						{
							Debug.LogError("Selected folder is not a parent of the Magic Leap directory.");
						}
						else
						{
							EditorPrefs.SetString(SDK_LOCATION_PREF_KEY, info.FullName);
						}
				
					}
					else
					{
						foreach (string dir in Directory.EnumerateDirectories(location))
						{
							foreach (string subDir in Directory.EnumerateDirectories(dir))
							{
								string manifestPath = Path.Combine(subDir, ".metadata", "sdk.manifest");
								if (File.Exists(manifestPath))
								{
									foundSdk =  true;
									break;
								}
							}
						}

						if (foundSdk)
						{
							DirectoryInfo info = new DirectoryInfo(location);
							EditorPrefs.SetString(SDK_LOCATION_PREF_KEY, info.FullName);
						}
						else
						{
							Debug.LogError("Selected folder is the Magic Leap directory, valid sdk path, or path to the mlsdk folder.");
						}
					}
				}
				else
				{
					Debug.LogError($"{location} does not exist as a valid path.");
				}
				
			}

		}
		/// <summary>
		/// Returns custom location if default install directory was not used
		/// </summary>
		/// <returns></returns>
		public static string GetSDKLocationFromPrefs()
		{
			return EditorPrefs.GetString(SDK_LOCATION_PREF_KEY, null);
		}

		/// <summary>
		/// Used if default install directory is not used
		/// </summary>
		/// <returns></returns>
		public static string SearchEditorPrefLocation()
		{
			
			var customDirectory = EditorPrefs.GetString(SDK_LOCATION_PREF_KEY, null);
			if (!string.IsNullOrEmpty(customDirectory) && Directory.Exists(customDirectory.Replace("\\", "/")))
			{
				var getVersionDirectories = Directory.EnumerateDirectories(customDirectory, "v*").ToList();

				getVersionDirectories.RemoveAll((e) => !File.Exists(Path.Combine(e, ".metadata", "sdk.manifest")));

				getVersionDirectories.Sort(new VersionComparer());
			
				return getVersionDirectories.Count == 0 ? customDirectory : getVersionDirectories[getVersionDirectories.Count - 1];
			}
			else
			{
				return Environment.GetEnvironmentVariable("USERPROFILE") ?? Environment.GetEnvironmentVariable("HOME");
			}
		}
	
		
		public static string GetLatestSDKPath()
		{


			var root = Environment.GetEnvironmentVariable("USERPROFILE") ?? Environment.GetEnvironmentVariable("HOME");


			if (!string.IsNullOrEmpty(root))
			{
				var sdkRoot = Path.Combine(root, "MagicLeap/mlsdk/");

				if (string.IsNullOrEmpty(sdkRoot) || !Directory.Exists(sdkRoot))
				{
					var prefsRoot = EditorPrefs.GetString(SDK_LOCATION_PREF_KEY, null);
					if (!string.IsNullOrEmpty(prefsRoot))
					{
						sdkRoot = Path.Combine(prefsRoot, "mlsdk");
					}
				}
			
				
				if (!string.IsNullOrEmpty(sdkRoot) && Directory.Exists(sdkRoot.Replace("\\","/")))
				{
					var getVersionDirectories = Directory.EnumerateDirectories(sdkRoot, "v*").ToList();
 
					getVersionDirectories.RemoveAll((e) => !File.Exists(Path.Combine(e, ".metadata", "sdk.manifest")));
				
					getVersionDirectories.Sort(new VersionComparer());

					return getVersionDirectories.Count == 0 ? sdkRoot : getVersionDirectories[getVersionDirectories.Count - 1];
				}
			}


			return null;
		}

	
	
	

		/// <summary>
		/// Refreshes the BuildTargetGroup XR Loader
		/// </summary>
		/// <param name="buildTargetGroup"> </param>
		private static void UpdateLoader(BuildTargetGroup buildTargetGroup)
		{
#if MAGICLEAP && UNITY_ANDROID

		
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

		/// <summary>
		/// Enables Magic Leap 2 XR on the available Build Target Group
		/// </summary>
		public static void EnableMagicLeapXRPlugin()
		{
#if MAGICLEAP && UNITY_ANDROID && UNITY_MAGICLEAP

			_cachedCreateXRSettingsMethod.Invoke(_cachedXRSettingsManagerType, null);
			_cachedCreateAllChildSettingsProvidersMethod.Invoke(_cachedXRSettingsManagerType, null);


			UpdateLoader(BuildTargetGroup.Android);
		

			EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,
													out XRGeneralSettingsPerBuildTarget magicLeapSettings);

			if (magicLeapSettings)
			{
				var androidSettings = magicLeapSettings.SettingsForBuildTarget(BuildTargetGroup.Android);


				androidSettings.Manager.TryAddLoader(MagicLeapLoader.assetInstance);

				EnableMagicLeapXRFinished.Invoke(true);
			}
			else
			{
				EnableMagicLeapXRFinished.Invoke(false);
				Debug.LogWarning(SETTINGS_NOT_FOUND);
			}
#else
			EnableMagicLeapXRFinished.Invoke(false);
#endif
		}


	
		/// <summary>
		/// Checks if Magic Leap XR is enabled
		/// </summary>
		/// <returns> </returns>
		public static bool IsMagicLeapXREnabled()
		{
#if MAGICLEAP && UNITY_ANDROID
			EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,out XRGeneralSettingsPerBuildTarget androidBuildSetting);
			var hasMagicLeapLoader = false;
			if (androidBuildSetting == null)
			{
				return false;
			}


			if (androidBuildSetting != null)
			{
				var androidSettings = androidBuildSetting.SettingsForBuildTarget(BuildTargetGroup.Android);
				if (androidSettings != null && androidSettings.Manager != null)
				{
					hasMagicLeapLoader = androidSettings.Manager.activeLoaders.Any(e =>
																			{
																				var fullName = e.GetType().FullName;
																				return !string.IsNullOrEmpty(fullName) && fullName.Contains(MAGIC_LEAP_LOADER_ID);
																			});
				}
			}
			return hasMagicLeapLoader;
#else
			return false;
#endif

		}

		/// <summary>
		/// Returns the SDK folder name
		/// </summary>
		/// <returns> </returns>
		public static string GetSdkFolderName()
		{
			var sdkRoot = EditorPrefs.GetString(SDK_PATH_EDITOR_PREF_KEY, null);


			if (!string.IsNullOrEmpty(sdkRoot) && Directory.Exists(sdkRoot))
			{
				return new DirectoryInfo(sdkRoot).Name;
			}

			return "0.0.0";
		}




		public static int GetMaxAPILevel()
		{
			if (HasRootSDKPath)
			{
				if (!File.Exists(_mlPermissionsPath))
				{
					Debug.LogWarningFormat(PROBLEM_FINDING_ML_PERMISSIONS, _mlPermissionsPath);
					return -1;
				}

				var json = File.ReadAllText(_mlPermissionsPath);
				var permissionNodes = JSONNode.Parse(json).Children;
				var maxApiLevel = -1;
				foreach (var permissionNode in permissionNodes)
				{
					var permissionObject = permissionNode.AsObject;
					if (permissionObject.HasKey(PERMISSIONS_API_LEVEL_KEY))
					{
						var apiLevelForPermission = permissionNode[PERMISSIONS_API_LEVEL_KEY].AsInt;
						if (maxApiLevel < apiLevelForPermission)
						{
							maxApiLevel = apiLevelForPermission;
						}
					}
				}

				return maxApiLevel;
			}

			return -1;
		}

	



	#region LOG MESSAGES

		private const string ERROR_FAILED_TO_CREATE_WINDOW = "Failed to create a view for XR Manager Settings Instance";
		private const string PROBLEM_FINDING_ML_PERMISSIONS = "Problem finding Magic Leap Permissions at [{0}]";
		private const string XR_CANNOT_BE_FOUND = "Current XR Settings Cannot be found";
		private const string LOADER_PROP_CANNOT_BE_FOUND = "Loader Prop [m_LoaderManagerInstance] Cannot be found";
		private const string SETTINGS_NOT_FOUND = "Settings not Found";

	#endregion

#if MAGICLEAP && UNITY_ANDROID


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

		public class VersionComparer : IComparer<string>
		{
			private string GetVersionString(string inputString)
			{
				string returnVal = inputString;
				if (string.IsNullOrEmpty(inputString))
				{
					return "0.0.0_dev0-ec0";
				}


				if (Directory.Exists(inputString))
				{
					returnVal = new DirectoryInfo(inputString).Name;
				}

				if (File.Exists(inputString))
				{
					returnVal = new FileInfo(inputString).Name;
				}

				returnVal= returnVal.Replace("\\", "/");
				var lastIndexOfSlash = returnVal.IndexOf('/');
				if (lastIndexOfSlash > -1)
				{
					returnVal = returnVal.Substring(lastIndexOfSlash + 1, returnVal.Length - (lastIndexOfSlash + 1));
				}

				var indexOfV = returnVal.IndexOf('v');
				if (indexOfV > -1)
				{
					returnVal = returnVal.Substring(indexOfV + 1, returnVal.Length - (indexOfV + 1));
				}



				return returnVal;
			}

		
			public int Compare(string x, string y)
			{
				if (string.IsNullOrWhiteSpace(x) && string.IsNullOrWhiteSpace(y))
				{
					return 0;
				}

				x = GetVersionString(x);
				y = GetVersionString(y);
				var xParts = x.Split('-', '_', '.');
				var yParts = y.Split('-', '_', '.');

				var length = Math.Max(xParts.Length, yParts.Length);

				for (int i = 0; i < length; i++)
				{
					if (i >= xParts.Length)
						return 1;
					if (i >= yParts.Length)
						return -1;

					if (int.TryParse(xParts[i], out int xNum) && int.TryParse(yParts[i], out int yNum))
					{
						if (xNum != yNum)
							return xNum.CompareTo(yNum);
					}
					else
					{
						int compareResult = string.Compare(xParts[i], yParts[i], StringComparison.Ordinal);
						if (compareResult != 0)
							return compareResult;
					}
				}

				return 0;
			}
		}
	
	
	      private class Manifest
        {
            /// <summary>
            /// File format for manifests
            /// </summary>
            private class ManifestFile
            {
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles",
                    Justification = "manifest.json syntax")]
                public ScopedRegistry[] scopedRegistries;

                [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles",
                    Justification = "manifest.json syntax")]
                public Dependencies dependencies;
            }

            /// <summary>
            /// File format for manifests without any registries
            /// </summary>
            private class ManifestFileWithoutRegistries
            {
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles",
                    Justification = "manifest.json syntax")]
                public Dependencies dependencies;
            }

            /// <summary>
            /// Dummy struct for encapsulation -- dependencies are manually handled via direct string manipulation
            /// </summary>
            [Serializable]
            public struct Dependencies
            {
            }

            [Serializable]
            public struct ScopedRegistry
            {
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles",
                    Justification = "manifest.json syntax")]
                public string name;

                [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles",
                    Justification = "manifest.json syntax")]
                public string url;

                [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles",
                    Justification = "manifest.json syntax")]
                public string[] scopes;
            }


            private const int INDEX_NOT_FOUND_ERROR = -1;
            private const string DEPENDENCIES_KEY = "\"dependencies\"";

            public string Path { get; private set; }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles",
                Justification = "manifest.json syntax")]
            public string dependencies;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles",
                Justification = "manifest.json syntax")]
            public ScopedRegistry[] scopedRegistries;

            public Manifest(string path)
            {
                Path = path;
                string fullJsonString = File.ReadAllText(path);
                var manifestFile = JsonUtility.FromJson<ManifestFile>(fullJsonString);

                scopedRegistries = manifestFile.scopedRegistries ?? new ScopedRegistry[0];
                var startIndex = GetDependenciesStart(fullJsonString);
                var endIndex = GetDependenciesEnd(fullJsonString, startIndex);

                dependencies = (startIndex == INDEX_NOT_FOUND_ERROR || endIndex == INDEX_NOT_FOUND_ERROR)
                    ? null
                    : fullJsonString.Substring(startIndex, endIndex - startIndex);
            }

            public void Serialize()
            {
                string jsonString = (scopedRegistries.Length > 0)
                    ? JsonUtility.ToJson(
                        new ManifestFile {scopedRegistries = scopedRegistries, dependencies = new Dependencies()}, true)
                    : JsonUtility.ToJson(new ManifestFileWithoutRegistries() {dependencies = new Dependencies()}, true);

                int startIndex = GetDependenciesStart(jsonString);
                int endIndex = GetDependenciesEnd(jsonString, startIndex);

                var stringBuilder = new StringBuilder();
                stringBuilder.Append(jsonString.Substring(0, startIndex));
                stringBuilder.Append(dependencies);
                stringBuilder.Append(jsonString.Substring(endIndex, jsonString.Length - endIndex));

                File.WriteAllText(Path, stringBuilder.ToString());
            }

            static int GetDependenciesStart(string json)
            {
                int dependenciesIndex = json.IndexOf(DEPENDENCIES_KEY, StringComparison.InvariantCulture);
                if (dependenciesIndex == INDEX_NOT_FOUND_ERROR)
                    return INDEX_NOT_FOUND_ERROR;

                int dependenciesStartIndex = json.IndexOf('{', dependenciesIndex + DEPENDENCIES_KEY.Length);
                if (dependenciesStartIndex == INDEX_NOT_FOUND_ERROR)
                    return INDEX_NOT_FOUND_ERROR;

                dependenciesStartIndex++;
                return dependenciesStartIndex;
            }

            static int GetDependenciesEnd(string jsonString, int dependenciesStartIndex) =>
                jsonString.IndexOf('}', dependenciesStartIndex);
        }
	}
}
