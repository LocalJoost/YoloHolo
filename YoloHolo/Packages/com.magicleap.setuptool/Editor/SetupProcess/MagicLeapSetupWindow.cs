#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using MagicLeap.SetupTool.Editor.Interfaces;
using MagicLeap.SetupTool.Editor.Setup;
using MagicLeap.SetupTool.Editor.Utilities;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

#endregion

namespace MagicLeap.SetupTool.Editor
{
    public class MagicLeapSetupWindow : EditorWindow
    {

    #region TEXT AND LABELS

        private const string WINDOW_PATH = "Magic Leap/Project Setup Tool";
        private const string WINDOW_TITLE_LABEL = "Magic Leap Project Setup";
        private const string SUBTITLE_LABEL = "PROJECT SETUP TOOL";
        private const string NO_SDK_FOUND_LABEL = "The Magic Leap Hub directory could not be found. Please make sure to install the Magic Leap Hub and download the Unity bundle from the package manager.";
        private const string DOWNLOAD_HUB_BUTTON_LABEL = "Download";
        private const string SET_PATH_BUTTON_LABEL = "Locate";
        private const string HELP_BOX_TEXT = "These steps are required to develop Unity applications for the Magic Leap 2.";
        private const string LOADING_TEXT = "  Please wait. Loading and Importing...";
        private const string LINKS_TITLE = "Helpful Links:";
        private const string APPLY_ALL_BUTTON_LABEL = "Apply All";
        private const string CLOSE_BUTTON_LABEL = "Close";
        private const string GETTING_STARTED_HELP_TEXT = "Read the getting started guide";
        private const string DOWNLOAD_HUB_HELP_TEXT = "Download The Magic Leap Hub";
        private const string SELECT_SDK_LOCATION_TITLE = "Select SDK";

    #endregion

    #region HELP URLS
        private const string GETTING_STARTED_URL = "https://developer-docs.magicleap.cloud/docs/guides/getting-started";
        private const string DOWNLOAD_HUB_URL = "https://ml2-developer.magicleap.com/downloads";
    #endregion
    
        private const string LOGO_PATH = "magic-leap-window-title";
        private const string ICON_PATH = "magic-leap-window-icon";
        private static  ISetupStep[] _stepsToComplete;
        private static bool _setSteps;
        private static MagicLeapSetupWindow _setupWindow;
        private static  ApplyAllRunner _applyAllRunner;
        private static Texture2D _logo;

        private static bool _loading
        {
            get
            {
                var loading = AssetDatabase.IsAssetImportWorkerProcess() ||
                              EditorApplication.isUpdating ||
                              EditorApplication.isCompiling ||
                              _stepsToComplete.Any(e => e.Busy);
#if ML_SETUP_DEBUG
                
              
                if (loading)
                {
                    Debug.Log($"Setup Tool Is Loading. Reason:\n" +
                              $"Asset Import Worker In Process: {AssetDatabase.IsAssetImportWorkerProcess()}\n" +
                              $"Assets Updating:{EditorApplication.isUpdating}\n" +
                              $"Compiling: {EditorApplication.isCompiling}\n" +
                              $"Steps Are Busy: {_stepsToComplete.Any(e => e.Busy)}");

                    if (_stepsToComplete.Any(e => e.Busy))
                    {
                        var busySteps =  string.Join(",",_stepsToComplete.Where(x => x.Busy));
                        Debug.Log($"Setup Seps that are busy: {busySteps}");
                    }
                
                }
                


#endif
                return loading;
            }
        }


         static MagicLeapSetupWindow()
        {

         _stepsToComplete = new ISetupStep[] {
                                                 new SetSdkFolderSetupStep(),
                                                 new BuildTargetSetupStep(),
                                                 new ImportMagicLeapSdkStep(),
                                                 #if USE_MLSDK
                                                 new EnablePluginStep(),
                                                 #else
                                                 new EnableOpenXRPluginStep(),
                                                 #endif
                                                 new SetDefaultTextureCompressionStep(),
                                                 #if USE_MLSDK
                                                 new FixValidationSetup(),
                                                 #else
                                                 new FixOpenXRValidationSetup(),
                                                 #endif
                                                 new ColorSpaceSetupStep(),
                                                 new SetTargetArchitectureStep(),
                                                 new SetScriptingBackendStep(),
                                                 new SetMinimumAndroidApiLevelStep(),
                                                 new UpdateManifestSetupStep(),
                                                 new SwitchActiveInputHandlerStep(),
                                                 new UpdateGraphicsApiSetupStep(),

                                                };

         foreach (var setupStep in _stepsToComplete)
         {
             setupStep.OnExecuteFinished+= RefreshSteps;
         }
         
         _applyAllRunner = new ApplyAllRunner(_stepsToComplete);



        }

      

         private void OnEnable()
        {
            _applyAllRunner.Tick();
            _logo = (Texture2D)Resources.Load(LOGO_PATH, typeof(Texture2D));
            Refresh();
            Application.quitting+= ApplicationOnQuitting;
        
            
        }

        private void ApplicationOnQuitting()
        {   
            EditorPrefs.SetInt(BuildTargetSetupStep.SWITCHING_PLATFORM_PREF, 1); //reset build target step in case something goes wrong
            if (_stepsToComplete != null)
            {
                var requiredStepsNotCompleted = _stepsToComplete.Any(step => step.Required && !step.IsComplete);
                EditorPrefs.SetBool(EditorKeyUtility.AutoShowEditorPrefKey, requiredStepsNotCompleted);
#if ML_SETUP_DEBUG
                Debug.Log($"Setup Window: required Steps Not Completed:{requiredStepsNotCompleted}");
                if (requiredStepsNotCompleted)
                {
                    var requiredSteps = string.Join("\n",
                        _stepsToComplete.Where(step => step.Required && !step.IsComplete));
                    Debug.Log($"Setup Window: required Steps Not Completed:{requiredSteps}");
                }
#endif
            }
            Application.quitting -= ApplicationOnQuitting;

        }


        private void OnDisable()
        {
            if (_stepsToComplete != null)
            {
                var requiredStepsNotCompleted = _stepsToComplete.Any(step => step.Required && !step.IsComplete);
                EditorPrefs.SetBool(EditorKeyUtility.AutoShowEditorPrefKey, requiredStepsNotCompleted);
#if ML_SETUP_DEBUG
                Debug.Log($"Setup Window: required Steps Not Completed: {requiredStepsNotCompleted}");
                if (requiredStepsNotCompleted)
                {
                    var requiredSteps = string.Join("\n",
                        _stepsToComplete.Where(step => step.Required && !step.IsComplete));
                    Debug.Log($"Setup Window: required Steps Not Completed: {requiredSteps}");
                }
#endif
            }
      
        }

        private void OnDestroy()
        {
            EditorPrefs.SetBool(EditorKeyUtility.WindowClosedEditorPrefKey, true);
            
            if (_stepsToComplete != null)
            {
                var requiredStepsNotCompleted = _stepsToComplete.Any(step => step.Required && !step.IsComplete);
                EditorPrefs.SetBool(EditorKeyUtility.AutoShowEditorPrefKey, requiredStepsNotCompleted);
#if ML_SETUP_DEBUG
                Debug.Log($"Setup Window: required Steps Not Completed:{requiredStepsNotCompleted}");
                if (requiredStepsNotCompleted)
                {
                    var requiredSteps = string.Join("\n",
                        _stepsToComplete.Where(step => step.Required && !step.IsComplete));
                    Debug.Log($"Setup Window: required Steps Not Completed:{requiredSteps}");
                }
#endif
                
            }

            EditorApplication.projectChanged -=  Refresh;

        }

        
        public bool IsDrawingMissingSdkInfo()
        {

            if (string.IsNullOrWhiteSpace(MagicLeapPackageUtility.GetLatestSDKPath()))
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox(NO_SDK_FOUND_LABEL, MessageType.Warning, true);
                EditorGUILayout.Space(5);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(DOWNLOAD_HUB_BUTTON_LABEL, GUILayout.MinWidth(100), GUILayout.MinHeight(22)))
                    {
                        Process.Start(DOWNLOAD_HUB_URL);
                    }

                    if (GUILayout.Button(SET_PATH_BUTTON_LABEL, GUILayout.MinWidth(100), GUILayout.MinHeight(22)))
                    {
                        var root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        if (string.IsNullOrEmpty(root))
                        {
                            root = Environment.GetEnvironmentVariable("HOME");
                        }

                        var directoryToUse = root;
                        if (!string.IsNullOrEmpty(root))
                        {
                            var sdkRoot = Path.Combine(root, "MagicLeap/mlsdk/").Replace("\\", "/");
                            if (Directory.Exists(sdkRoot))
                            {
                                directoryToUse = sdkRoot;
                            }
                        }
                       
                       

                        var path = EditorUtility.OpenFolderPanel(SELECT_SDK_LOCATION_TITLE, directoryToUse,null);
                        MagicLeapPackageUtility.SetSDKEditorPrefLocation(path);
                    }
                }
                GUILayout.EndHorizontal();
                return true;
            }

            return false;
        }
        public void OnGUI()
        {
            DrawHeader();
        
            if (IsDrawingMissingSdkInfo())
            {
                return;
            }
       
            if (_loading || ApplyAllRunner.Running)
            {
                DrawWaitingInfo();
                return;
            }

            DrawInfoBox();
        
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Space(5);

                foreach (var setupStep in _stepsToComplete)
                {
                    if (setupStep.Draw()) Repaint();
                }

                GUI.backgroundColor = Color.clear;
            }

            GUILayout.EndVertical();
            GUILayout.Space(10);
            DrawHelpLinks();
            DrawFooter();

        }

        private void OnFocus()
        {
            RefreshSteps();
        }


        private void OnInspectorUpdate()
        {
            Repaint();
            if (_loading)
            {
                return;
            }

            _applyAllRunner.Tick();
        }
        private static void RefreshSteps()
        {
            foreach (var setupStep in _stepsToComplete)
            {
                setupStep.Refresh();
            }
#if ML_SETUP_DEBUG
            Debug.Log($"Setup Step Info:\n----\n{string.Join("\n----\n",_stepsToComplete.ToList())}\n----");
#endif
       
        }

        private static void Open()
        {
            _setupWindow = GetWindow<MagicLeapSetupWindow>(false, WINDOW_TITLE_LABEL);
          
            
            _setupWindow.minSize = new Vector2(350, 700);
            _setupWindow.maxSize = new Vector2(400, 750);
            _setupWindow.titleContent = new GUIContent(WINDOW_TITLE_LABEL, (Texture2D)Resources.Load(ICON_PATH, typeof(Texture2D)));


           
            _setupWindow.Show();
            EditorApplication.delayCall += ()=>
                                           {
                                               EditorApplication.update += UpdateEditor;
                                               void UpdateEditor()
                                               {
                                                    if(_loading)
                                                    {
                                                        return;
                                                    }
                                                   ApplyAllRunner.CheckLastAutoSetupState();
                                                   EditorApplication.update -= UpdateEditor;
                                               }
                                             
                                           };
          
            EditorApplication.projectChanged += Refresh;
            EditorSceneManager.sceneOpened += (s, l) => { _applyAllRunner.Tick(); };
            EditorApplication.quitting += () =>
            {
                EditorPrefs.SetBool(EditorKeyUtility.WindowClosedEditorPrefKey, false);
            };
    

        }
        public static void ForceClose()
        {
            //call with delay to avoid errors when restarting editor
            EditorApplication.delayCall +=()=>
            {
                var canForceClose =EditorPrefs.GetBool(EditorKeyUtility.AutoShowEditorPrefKey,true);
                if(!canForceClose)
                    return;
                _setupWindow = GetWindow<MagicLeapSetupWindow>(false, WINDOW_TITLE_LABEL);
                _setupWindow.Close();
            };

        }
        public static void ForceOpen()
        {
    
                    //call with delay to avoid errors when restarting editor
                    EditorApplication.delayCall +=()=>
                                          {
#if ML_SETUP_DEBUG
                                              Debug.Log($"Setup Window: ForceOpen:{EditorPrefs.GetBool(EditorKeyUtility.WindowClosedEditorPrefKey, false)}");
#endif
                                              if (!ApplyAllRunner.WasRunning() && EditorPrefs.GetBool(EditorKeyUtility.WindowClosedEditorPrefKey, false)) 
                                                  return;
                                              Open();
                                          };

        }

        [MenuItem(WINDOW_PATH, priority = 100)]
        public static void MenuOpen()
        {
            Open();
        }

        private static void Refresh()
        {
            //Refresh steps after UI updates.
            EditorApplication.delayCall += RefreshSteps;
        }
        
        #region Draw Window Controls

        private void DrawHeader()
        {
            //Draw Magic Leap brand image
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(_logo);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            CustomGuiContent.DrawUILine(Color.grey, 1, 5);
            GUILayout.BeginVertical();
            {
                EditorGUILayout.LabelField(SUBTITLE_LABEL, Styles.TitleStyleDefaultFont);
                GUILayout.EndVertical();
            }
            GUILayout.Space(5);
            GUI.backgroundColor = Color.white;
            GUILayout.Space(2);
        }

        private void DrawInfoBox()
        {
            GUILayout.Space(5);
          
            var content = new GUIContent(HELP_BOX_TEXT);
            EditorGUILayout.LabelField(content, Styles.InfoTitleStyle);

            GUILayout.Space(5);
            GUI.backgroundColor = Color.white;
        }

        private void DrawHelpLinks()
        {
            var currentGUIEnabledStatus = GUI.enabled;
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Space(2);
                EditorGUILayout.LabelField(LINKS_TITLE, Styles.HelpTitleStyle);
                CustomGuiContent.DisplayLink(GETTING_STARTED_HELP_TEXT, GETTING_STARTED_URL, 3);
                CustomGuiContent.DisplayLink(DOWNLOAD_HUB_HELP_TEXT, DOWNLOAD_HUB_URL, 3);
                GUILayout.Space(2);
                GUILayout.Space(2);
            }
            GUILayout.EndVertical();
            GUI.enabled = currentGUIEnabledStatus;
        }

        private void DrawWaitingInfo()
        {

            GUILayout.Space(5);
            var content = new GUIContent(LOADING_TEXT);
            EditorGUILayout.LabelField(content, Styles.InfoTitleStyle);
            GUI.enabled = false;

            GUILayout.Space(5);
            GUI.backgroundColor = Color.white;
        }

        private void DrawFooter()
        {
            GUILayout.FlexibleSpace();
            var currentGUIEnabledStatus = GUI.enabled;
            GUI.enabled = !_loading;

            if (_applyAllRunner.AllAutoStepsComplete)
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button(CLOSE_BUTTON_LABEL, GUILayout.MinWidth(20), GUILayout.MinHeight(30))) Close();
            }
            else
            {
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button(APPLY_ALL_BUTTON_LABEL, GUILayout.MinWidth(20), GUILayout.MinHeight(30))) _applyAllRunner.RunApplyAll();
            }

      
            GUI.enabled = currentGUIEnabledStatus;
            GUI.backgroundColor = Color.clear;
        }

        #endregion
    }
}