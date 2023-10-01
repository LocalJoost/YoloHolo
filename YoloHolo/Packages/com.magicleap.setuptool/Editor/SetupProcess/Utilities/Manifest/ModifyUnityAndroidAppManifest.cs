using System.IO;
using System.Text;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace MagicLeap.SetupTool.Editor.Utilities
{
    // reference: https://stackoverflow.com/questions/43293173/use-custom-manifest-file-and-permission-in-unity
    public class ModifyUnityAndroidAppManifest 
    {

        public void OnPostGenerateGradleAndroidProject(string manifestPath)
        {
            // If needed, add condition checks on whether you need to run the modification routine.
            // For example, specific configuration/app options enabled
         
            var androidManifest = new AndroidManifest(manifestPath);

      
            MergeResources(manifestPath);

            // Add your XML manipulation routines

            androidManifest.Save();
        }

        public int callbackOrder
        {
            get
            {
                return 1;
            }
        }

        private string _manifestFilePath;

        private string GetManifestPath(string basePath)
        {
            if (string.IsNullOrEmpty(_manifestFilePath))
            {
                var pathBuilder = new StringBuilder(basePath);
                pathBuilder.Append(Path.DirectorySeparatorChar).Append("src");
                pathBuilder.Append(Path.DirectorySeparatorChar).Append("main");
                pathBuilder.Append(Path.DirectorySeparatorChar).Append("AndroidManifest.xml");
                _manifestFilePath = pathBuilder.ToString();
            }

            return _manifestFilePath;
        }

        private void MergeResources(string basePath)
        {
            var pathToRes = new StringBuilder(Application.dataPath); //+ "/Plugins/Android/res";
            pathToRes.Append(Path.DirectorySeparatorChar).Append("Plugins");
            pathToRes.Append(Path.DirectorySeparatorChar).Append("Android");
            pathToRes.Append(Path.DirectorySeparatorChar).Append("res");

            var pathBuilder = new StringBuilder(basePath);
            pathBuilder.Append(Path.DirectorySeparatorChar).Append("src");
            pathBuilder.Append(Path.DirectorySeparatorChar).Append("main");
            pathBuilder.Append(Path.DirectorySeparatorChar).Append("res");

            Copy(pathToRes.ToString(), pathBuilder.ToString());
        }

        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            var diSource = new DirectoryInfo(sourceDirectory);
            var diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                if (fi.Name.EndsWith("meta"))
                    continue;
                Debug.Log($"Copying " + target.FullName + " to " + fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
    }

}



