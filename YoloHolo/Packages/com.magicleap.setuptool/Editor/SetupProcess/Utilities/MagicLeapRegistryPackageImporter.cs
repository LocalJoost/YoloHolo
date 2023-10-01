using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace MagicLeap.SetupTool.Editor.Utilities
{
	public static class MagicLeapRegistryPackageImporter
	{
	  private static readonly string ManifestPath =
            Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");

        private static readonly string ML_UNITY_SDK = "com.magicleap.unitysdk";

        private static readonly Manifest PackageManifest = new Manifest(ManifestPath);

        private static readonly Manifest.ScopedRegistry ML_REGISTRY = new Manifest.ScopedRegistry()
        {
            name = "Magic Leap",
            url = "https://registry.npmjs.org",
            scopes = new[] {"com.magicleap"}
        };

        public static void AddRegistryAndImport(Action<bool> success)
        {
            if (!ContainsMLRegistry(PackageManifest))
            {
                AddMLRegistryToManifest(PackageManifest);
            }

            EditorApplication.delayCall += () => { InstallMlsdkPackage(success); };
        }

        private static void InstallMlsdkPackage(Action<bool> success)
        {
            AddRequest addRequest = Client.Add(ML_UNITY_SDK);

            EditorApplication.update += AddPackageProgress;

            void AddPackageProgress()
            {
                if (addRequest.IsCompleted)
                {
                    if (addRequest.Status >= StatusCode.Failure)
                    {
                        Debug.LogError(addRequest.Error.message);
                    }

                    success.Invoke(addRequest.Status == StatusCode.Success);
                    EditorApplication.update -= AddPackageProgress;
                }
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the given Manifest contains the ML registry
        /// </summary>
        static bool ContainsMLRegistry(Manifest manifest) =>
            manifest.scopedRegistries.ToList().Any(d => d.name == ML_REGISTRY.name);

        /// <summary>
        /// Updates the given Manifest to include the ML registry
        /// </summary>
        static void AddMLRegistryToManifest(Manifest manifest)
        {
            // first remove old registry if it exists
            var registries = manifest.scopedRegistries.ToList();

            registries.Add(ML_REGISTRY);
            manifest.scopedRegistries = registries.ToArray();
            manifest.Serialize();

            AssetDatabase.Refresh();
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
