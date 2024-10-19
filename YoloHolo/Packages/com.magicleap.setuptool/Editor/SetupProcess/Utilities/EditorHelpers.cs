using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace MagicLeap.SetupTool.Editor.Utilities
{
    public static class EditorHelpers
    {
        public static void CallWhenNotBusy(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();

            EditorApplication.delayCall += () =>
            {
                EditorApplication.update += UpdateEditor;

                void UpdateEditor()
                {
                    if (AssetDatabase.IsAssetImportWorkerProcess() ||
                        EditorApplication.isUpdating ||
                        EditorApplication.isCompiling)
                    {
                        return;
                    }

                    action?.Invoke();
                    EditorApplication.update -= UpdateEditor;
                    tcs.SetResult(true);
                }
            };
        }
  
        public static async Task WaitUntilNotBusy()
        {
            var tcs = new TaskCompletionSource<bool>();
            try
            {
                EditorApplication.delayCall += () =>
                {
                    EditorApplication.update += UpdateEditor;

                    void UpdateEditor()
                    {
                        if (AssetDatabase.IsAssetImportWorkerProcess() ||
                            EditorApplication.isUpdating ||
                            EditorApplication.isCompiling)
                        {
                            return;
                        }

                 
                        EditorApplication.update -= UpdateEditor;
                        tcs.SetResult(true);
                    }
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"Error Waiting For Not Busy Editor: {e}");
            }
      
            await tcs.Task;
        }
    }
}