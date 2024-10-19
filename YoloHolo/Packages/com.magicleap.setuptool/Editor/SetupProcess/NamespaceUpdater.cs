using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
namespace MagicLeap.SetupTool.Editor {
    public class NamespaceUpdater : AssetModificationProcessor
    {
        private static Assembly magicLeapAssembly;
        private const string OldNamespace = "UnityEngine.XR.OpenXR.Features.MagicLeapSupport";

        private static readonly string
            TrackingFilePath = Path.Combine(Application.dataPath, "NamespaceRefactorer.json");

        private static readonly Dictionary<string, bool> NamespaceExistenceCache = new Dictionary<string, bool>();

        private static readonly Dictionary<string, TypeMapping> TypeMappingsByClass =
            new Dictionary<string, TypeMapping>();

        private static readonly Dictionary<string, string> NamespaceReplacements = new Dictionary<string, string>
        {
            { "MagicLeapPixelSensorFeature", "MagicLeap.OpenXR.Features.PixelSensors" },
            { "MagicLeapLightEstimationFeature", "MagicLeap.OpenXR.Features.LightEstimation" },
            { "MagicLeapMarkerUnderstandingFeature", "MagicLeap.OpenXR.Features.MarkerUnderstanding" },
            { "MLXrPlaneSubsystem", "MagicLeap.OpenXR.Subsystems" },
            { "MagicLeapSystemNotificationsFeature", "MagicLeap.OpenXR.Features.SystemNotifications" },
            { "MagicLeapFacialExpressionFeature", "MagicLeap.OpenXR.Features.FacialExpressions" },
            { "MagicLeapSpatialAnchorsStorageFeature", "MagicLeap.OpenXR.Features.SpatialAnchors" },
            { "MagicLeapFeature", "MagicLeap.OpenXR.Features" },
            { "MagicLeapMeshingFeature", "MagicLeap.OpenXR.Features.Meshing" },
            { "MagicLeapReferenceSpacesFeature", "MagicLeap.OpenXR.Features" },
            { "MagicLeapRenderingExtensionsFeature", "MagicLeap.OpenXR.Features" },
            { "MLXrAnchorSubsystem", "MagicLeap.OpenXR.Subsystems" },
            { "MagicLeapUserCalibrationFeature", "MagicLeap.OpenXR.Features.UserCalibration" },
            { "MagicLeapLocalizationMapFeature", "MagicLeap.OpenXR.Features.LocalizationMaps" }
        };

        [MenuItem("Magic Leap/API Updater/Migrate Namespaces", priority = 500)]
        public static void PromptAndUpdateNamespaces()
        {
            int option = EditorUtility.DisplayDialogComplex(
                "Magic Leap API Updater",
                "It is recommended to back up your project before updating the scripts.\n\nWould you like to update all scripts in your Assets folder or choose a specific directory?",
                "Update Assets Folder", "Cancel", "Choose Directory");

            switch (option)
            {
                case 0:
                    UpdateNamespaces(Application.dataPath);
                    break;
                case 1:
                    Debug.Log("Namespace migration canceled.");
                    break;
                case 2:
                    string folderPath =
                        EditorUtility.OpenFolderPanel("Select Folder to Search", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(folderPath))
                    {
                        UpdateNamespaces(folderPath);
                    }
                    else
                    {
                        Debug.Log("No folder selected. Operation canceled.");
                    }

                    break;
            }
        }

        private static bool IsAssemblyAvailable()
        {
            if (magicLeapAssembly == null)
            {
                try
                {
                    magicLeapAssembly = Assembly.Load("MagicLeap.SDK");
                }
                catch
                {
                    Debug.LogWarning("Could not load MagicLeap.SDK Assembly.");
                }
            }

            return magicLeapAssembly != null;
        }

        private static bool IsNewAPIAvailable()
        {
            if (!IsAssemblyAvailable()) return false;

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(magicLeapAssembly);
            var numericVersionString = ExtractNumericVersion(packageInfo.version);
            return numericVersionString != null && new Version(numericVersionString) >= new Version("2.3.0");
        }

        private static string ExtractNumericVersion(string version)
        {
            var match = Regex.Match(version, @"^\d+\.\d+\.\d+");
            return match.Success ? match.Value : null;
        }

        // Update namespaces in the specified folder
        public static void UpdateNamespaces(string folder)
        {
            // Check if the new API is available based on the SDK version
            if (!IsNewAPIAvailable())
            {
                EditorUtility.DisplayDialog("Incompatible SDK Version",
                    "Namespace migration is only required for Magic Leap SDK v2.3.0 or higher.", "OK");
                return;
            }

            // Get all .cs files in the specified folder, excluding hidden files
            var allFiles = Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories)
                .Where(file => !Path.GetFileName(file).StartsWith('.')).ToArray();
            var savedFiles = LoadSavedFiles();

            // Process each file to update namespaces
            bool anyScriptsUpdated = ProcessFiles(allFiles, savedFiles);

            // Save the updated files to the tracking JSON file if any scripts were updated
            if (anyScriptsUpdated)
            {
                SaveUpdatedFiles(savedFiles);
            }
            else
            {
                Debug.Log("No scripts needed updating.");
            }
        }

        private static SavedFiles LoadSavedFiles()
        {
            return File.Exists(TrackingFilePath)
                ? JsonUtility.FromJson<SavedFiles>(File.ReadAllText(TrackingFilePath))
                : new SavedFiles();
        }

        /// <summary>
        /// Processes multiple C# files to update namespace references.
        /// For each file, checks for the old namespace, backs up the file, and updates type references based on defined namespace replacements.
        /// Updates the file if changes were made, and tracks progress using a progress bar.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="savedFiles">The class that can be serialized and includes a map of original files and back up files</param>
        /// <returns>Returns true if any files were updated.</returns>
        private static bool ProcessFiles(string[] files, SavedFiles savedFiles)
        {
            bool anyScriptsUpdated = false;

            // Iterate through each file to process it
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var lines = File.ReadAllLines(file);

                // Check if the file contains the old namespace
                if (lines.Any(line => IncludesNamespace(line, OldNamespace)))
                {
                    // Create a backup of the file if it doesn't already exist
                    var backupFilePath = Path.Combine(Path.GetDirectoryName(file), "." + Path.GetFileName(file));
                    if (!File.Exists(backupFilePath))
                    {
                        File.Copy(file, backupFilePath, true);
                        savedFiles.FilePairs.Add(new FilePair(GetRelativePath(file), GetRelativePath(backupFilePath)));
                    }

                    var updatedLines = lines.ToList();
                    bool scriptUpdated = false;

                    // Iterate through each namespace replacement and the associated parent class
                    foreach (var pair in NamespaceReplacements)
                    {
                        // Get the parent class that the types were associated with, for example MarkerTrackerFeature.DetectorSettings
                        var className = pair.Key;
                        // Get the new namespace associated with the feature, for example MagicLeap.OpenXR.Features.MarkerUnderstanding
                        var newNamespace = pair.Value;

                        // Create TypeMapping if not already created.
                        // A TypeMapping is a class that maps the old types with the new types using reflection.
                        // We cache the NewTypeByOldType to prevent creating a map multiple times for the same class.
                        if (!TypeMappingsByClass.ContainsKey(pair.Key))
                        {
                            TypeMappingsByClass.Add(pair.Key, new TypeMapping(className, newNamespace, OldNamespace));
                        }

                        // Process the file for the current class and namespace
                        if (ProcessFile(ref updatedLines, TypeMappingsByClass[pair.Key].NewTypeByOldType, className,
                                newNamespace))
                        {
                            scriptUpdated = true;
                        }
                    }

                    // Write the updated lines back to the file if it was modified
                    if (scriptUpdated)
                    {
                        // Remove the old namespace using statement. Returns all the lines of the file that do not include the namespace.
                        File.WriteAllLines(file, RemoveOldNamespaceUsings(updatedLines).ToArray());
                        Debug.Log($"Magic Leap API Namespace updated in file: {GetRelativePath(file)}");
                        anyScriptsUpdated = true;
                    }
                }

                // Update progress bar
                var progress = (float)i / files.Length;
                EditorUtility.DisplayProgressBar("Reflecting and Replacing Namespaces",
                    $"Processing {GetRelativePath(file)}", progress);
            }

            EditorUtility.ClearProgressBar();
            return anyScriptsUpdated;
        }

        private static void SaveUpdatedFiles(SavedFiles savedFiles)
        {
            File.WriteAllText(TrackingFilePath, JsonUtility.ToJson(savedFiles));
            AssetDatabase.Refresh();
        }

        public static bool IncludesNamespace(string line, string nameSpace)
        {
            return Regex.IsMatch(line.Trim(), $@"^using\s+{Regex.Escape(nameSpace)};\s*(//.*)?$");
        }


        public static List<string> RemoveOldNamespaceUsings(List<string> lines)
        {
            return lines.Where(line => !IncludesNamespace(line, OldNamespace)).ToList();
        }

        [MenuItem("Magic Leap/API Updater/Restore Original Script Files")]
        public static void RestoreOriginalFiles()
        {
            if (File.Exists(TrackingFilePath))
            {
                if (EditorUtility.DisplayDialog("Confirm Restore",
                        "The current version of the scripts will be reverted.\nScripts cannot be restored unless they are tracked by source control tools.\n\nAre you sure you want to restore the original scripts?",
                        "Yes", "No"))
                {
                    RestoreFiles();
                }
            }
            else
            {
                Debug.LogWarning("No tracking file found. Make sure you have run the namespace replacement first.");
            }
        }

        [MenuItem("Magic Leap/API Updater/Restore Original Script Files", true)]
        public static bool ValidateRestoreOriginalFiles()
        {
            return File.Exists(TrackingFilePath) &&
                   JsonUtility.FromJson<SavedFiles>(File.ReadAllText(TrackingFilePath)).FilePairs.Count > 0;
        }

        [MenuItem("Magic Leap/API Updater/Delete Script Backups", true)]
        public static bool ValidateDeleteAllBackups()
        {
            return File.Exists(TrackingFilePath) &&
                   JsonUtility.FromJson<SavedFiles>(File.ReadAllText(TrackingFilePath)).FilePairs.Count > 0;
        }

        [MenuItem("Magic Leap/API Updater/Delete Script Backups")]
        public static void DeleteAllBackups()
        {
            if (EditorUtility.DisplayDialog("Confirm Delete",
                    "You will not be able to restore the original versions of the scripts unless the project is being tracked by other source control. Are you sure you want to delete all script backup files?",
                    "Yes", "No"))
            {
                DeleteBackupFiles();
            }
        }

        /// <summary>
        /// Restores original C# files from backups using a tracking JSON file.
        /// Copies each backup file back to its original location, deletes the backups, updates the progress bar,
        /// and refreshes the Unity AssetDatabase. Deletes the tracking JSON file upon completion.
        /// </summary>
        private static void RestoreFiles()
        {
            // Read the tracking JSON file to get the list of saved file pairs
            var json = File.ReadAllText(TrackingFilePath);
            var fileMap = JsonUtility.FromJson<SavedFiles>(json);

            // Iterate through each file pair in the saved files list
            for (int i = 0; i < fileMap.FilePairs.Count; i++)
            {
                var pair = fileMap.FilePairs[i];

                // Get the absolute paths for the original and backup files
                var originalPath = GetAbsolutePath(pair.OriginalPath);
                var backupPath = GetAbsolutePath(pair.BackupPath);

                // Copy the backup file to the original location, effectively restoring the original file
                File.Copy(backupPath, originalPath, true);

                // Delete the backup file after restoring the original file
                File.Delete(backupPath);

                // Update the progress bar to indicate the progress of the restoration process
                var progress = (float)i / fileMap.FilePairs.Count;
                EditorUtility.DisplayProgressBar("Restoring Original Files", $"Restoring {pair.OriginalPath}",
                    progress);
            }

            // Delete the tracking JSON file after all files have been restored
            File.Delete(TrackingFilePath);

            // Refresh the AssetDatabase to ensure that Unity recognizes the restored files
            AssetDatabase.Refresh();

            // Clear the progress bar once the restoration process is complete
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Deletes all backup files created during namespace migration.
        /// Uses a tracking JSON file to find and delete backup files, then deletes the tracking file itself.
        /// Refreshes the Unity AssetDatabase and logs a warning if no tracking file is found.
        /// </summary>
        private static void DeleteBackupFiles()
        {
            // Check if the tracking JSON file exists
            if (File.Exists(TrackingFilePath))
            {
                // Read the tracking JSON file to get the list of saved file pairs
                var json = File.ReadAllText(TrackingFilePath);
                var fileMap = JsonUtility.FromJson<SavedFiles>(json);

                // Iterate through each file pair in the saved files list
                foreach (var pair in fileMap.FilePairs)
                {
                    // Get the absolute path for the backup file
                    var backupPath = GetAbsolutePath(pair.BackupPath);

                    // Delete the backup file if it exists
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                }

                // Delete the tracking JSON file after all backup files have been deleted
                File.Delete(TrackingFilePath);

                // Refresh the AssetDatabase to ensure that Unity recognizes the changes
                AssetDatabase.Refresh();

                // Log a message indicating that all backup files and the tracking JSON file have been deleted
                Debug.Log("All backup files and the tracking JSON file have been deleted.");
            }
            else
            {
                // Log a warning message if the tracking JSON file is not found
                Debug.LogWarning("No tracking file found. Make sure you have run the namespace replacement first.");
            }
        }

        // Process a single file to replace old namespaces with new ones
        private static bool ProcessFile(ref List<string> lines, Dictionary<Type, Type> typeMap, string parentClassName,
            string newNamespace)
        {
            // Check if the class is using the parent class, and if the new namespace has already been added to the file
            bool needsNewNamespace = lines.Any(line => line.Contains($"{parentClassName}"))
                                     && !lines.Any(line => IncludesNamespace(line, newNamespace));
            // Flag to track if any updates were made to the file
            bool wasUpdated = false;
            // Index to track where to insert the new namespace if needed
            int namespaceIndex = -1;

            // Iterate through each line in the file
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];

                // If the new namespace was not added and the line is not a using directive, set the index to insert the new namespace
                if (needsNewNamespace && namespaceIndex == -1 && !line.StartsWith("using "))
                {
                    namespaceIndex = i;
                }

                // Iterate through each type mapping
                foreach (var map in typeMap)
                {
                    // Determine the old and new short names for the type
                    // Example: If the old type was nested (e.g., OldNamespace.ParentType.NestedType),
                    // we use the parent type's name and the nested type's name
                    string oldShortName = map.Key.IsNested && map.Key.DeclaringType != null
                        ? $"{map.Key.DeclaringType.Name}.{map.Key.Name}"
                        : map.Key.Name;
                    string newShortName = map.Value.IsNested && map.Value.DeclaringType != null
                        ? $"{map.Value.DeclaringType.Name}.{map.Value.Name}"
                        : map.Value.Name;

                    // Use a regular expression to find all matches of the old short name in the line
                    // The regex \b{Regex.Escape(oldShortName)}\b ensures we match whole words only
                    var matches = Regex.Matches(line, $@"\b{Regex.Escape(oldShortName)}\b");

                    // If there are any matches, process each match
                    if (matches.Count > 0)
                    {
                        var updatedLine = new System.Text.StringBuilder(line);
                        int offset = 0;

                        // Iterate through each match found by the regex
                        foreach (Match match in matches)
                        {
                            string matchedWord = match.Value;
                            // Replace the matched old short name with the new short name
                            string replacementWord = matchedWord.Replace(oldShortName, newShortName);

                            // Check if the replacement word exists in the new namespace
                            if (TypeExistsInNamespace(typeMap, newNamespace, replacementWord))
                            {
                                // Update the line with the new type name
                                // The offset is used to adjust for the difference in length between the old and new type names
                                updatedLine.Remove(match.Index + offset, matchedWord.Length);
                                updatedLine.Insert(match.Index + offset, replacementWord);
                                offset += replacementWord.Length - matchedWord.Length;
                                wasUpdated = true;
                            }
                        }

                        // Convert the StringBuilder back to a string and update the line
                        line = updatedLine.ToString();
                    }
                }

                // Update the lines list with the modified line
                lines[i] = line;
            }

            // If any updates were made and the namespace index is valid, insert the new namespace using directive
            if (namespaceIndex > -1)
            {
                lines.Insert(namespaceIndex, $"using {newNamespace};");
                wasUpdated = true;
            }

            return wasUpdated;
        }

        // Check if a type exists in the specified namespace
        private static bool TypeExistsInNamespace(Dictionary<Type, Type> newTypeByOldType, string namespaceName,
            string typeName)
        {
            // Check if the type existence is already cached
            if (NamespaceExistenceCache.TryGetValue(typeName, out bool exists))
                return exists;

            // Determine if the type exists in the specified namespace
            // Iterate through the types in the dictionary values and check the following:
            // - The type's namespace matches the specified namespace
            // - The type's name matches the specified type name OR
            // - The type is nested and the combination of its declaring type's name and its own name matches the specified type name
            bool typeExists = newTypeByOldType.Values.Any(type =>
                type.Namespace == namespaceName &&
                (type.Name == typeName ||
                 (type.IsNested && $"{type.DeclaringType.Name}.{type.Name}" == typeName)));

            // Cache the result for future lookups
            NamespaceExistenceCache[typeName] = typeExists;

            // Return the result
            return typeExists;
        }

        // Convert an absolute file path to a relative path based on the project directory
        private static string GetRelativePath(string absolutePath)
        {
            try
            {
                // Get the full path of the project directory
                string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

                // Create a Uri object for the project directory with a trailing directory separator
                Uri projectUri = new Uri(projectPath + Path.DirectorySeparatorChar);

                // Create a Uri object for the absolute file path
                Uri absoluteUri = new Uri(absolutePath);

                // If the absolute path is not already an absolute URI, create one
                if (!absoluteUri.IsAbsoluteUri)
                {
                    absoluteUri = new Uri(projectPath + Path.DirectorySeparatorChar + absolutePath);
                }

                // Convert the absolute URI to a relative URI based on the project URI
                string relativeUri = projectUri.MakeRelativeUri(absoluteUri).ToString();

                // Unescape any escaped characters and replace forward slashes with backslashes
                return Uri.UnescapeDataString(relativeUri).Replace("/", Path.DirectorySeparatorChar.ToString());
            }
            catch (Exception ex)
            {
                // Log an error if any exception occurs and return the original absolute path as a fallback
                Debug.LogError($"Error getting relative path: {ex.Message}");
                return absolutePath;
            }
        }

        // Convert a relative file path to an absolute path based on the project directory
        private static string GetAbsolutePath(string relativePath)
        {
            try
            {
                // Get the full path of the project directory
                string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

                // Combine the project path with the relative path to get the absolute path
                return Path.GetFullPath(Path.Combine(projectPath, relativePath));
            }
            catch (Exception ex)
            {
                // Log an error if any exception occurs and return the original relative path as a fallback
                Debug.LogError($"Error getting absolute path: {ex.Message}");
                return relativePath;
            }
        }

        public class TypeMapping
        {
            // Dictionary to hold the mapping between old and new types
            public Dictionary<Type, Type> NewTypeByOldType { get; }

            // Constructor to create a mapping between old and new types
            public TypeMapping(string parentClassName, string newNamespace, string oldNamespace)
            {
                // Initialize the dictionary to store the type mappings
                NewTypeByOldType = new Dictionary<Type, Type>();

                // Get the old parent type from the old namespace using reflection
                var oldParentType = magicLeapAssembly.GetTypes()
                    .FirstOrDefault(t => t.IsClass && t.Name == parentClassName && t.Namespace == oldNamespace);

                // If the old parent type is not found, log an error and exit
                if (oldParentType == null)
                {
                    Debug.LogError("Parent class not found in the old namespace.");
                    return;
                }

                // Get the new parent type from the new namespace using reflection
                var newParentType = magicLeapAssembly.GetTypes()
                    .FirstOrDefault(t => t.IsClass && t.Name == parentClassName && t.Namespace == newNamespace);

                // If the new parent type is not found, log an error and exit
                if (newParentType == null)
                {
                    Debug.LogError("Parent class not found in the new namespace.");
                    return;
                }

                // Map the nested types from the old parent type to the new parent type
                foreach (var type in oldParentType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    // Construct the full type name for the new type
                    var newFullTypeName = $"{newNamespace}.{type.Name}";

                    // Get the new type using the full type name
                    var newType = magicLeapAssembly.GetType(newFullTypeName);

                    // If the new type is found, add it to the type map
                    if (newType != null)
                    {
                        NewTypeByOldType[type] = newType;
                    }
                }
            }
        }

        [Serializable]
        public class SavedFiles
        {
            public List<FilePair> FilePairs = new List<FilePair>();
        }

        [Serializable]
        public class FilePair
        {
            public string OriginalPath;
            public string BackupPath;

            public FilePair(string original, string backup)
            {
                OriginalPath = original;
                BackupPath = backup;
            }
        }
    }
    }