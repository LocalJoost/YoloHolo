#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

#endregion

namespace MagicLeap.SetupTool.Editor.Utilities
{
    /// <summary>
    /// Utility that wraps the Unity Package Manager Client to work on events
    /// </summary>
    public static class PackageUtility
    {
        private static bool _hasListRequest;
        private static ListRequest _listInstalledPackagesRequest;
        private static readonly List<string> _packageNamesToCheck = new List<string>();
        private static readonly List<Action<bool, bool>> _checkRequestFinished = new List<Action<bool, bool>>();



        
        /// <summary>
        /// Adds a package dependency to the Project. This is the equivalent of installing a package.
        /// <para>--- To install the latest compatible version of a package, specify only the package.</para>
        /// <para>--- To install a git package, specify a git url</para>
        /// <para>--- To install a local package, specify a value in the format "file:pathtopackagefolder".</para>
        /// <para>--- To install a local tarball package, specify a value in the format "file:pathto/package-file.tgz".</para>
        /// <para>ArgumentException is thrown if identifier is null or empty.</para>
        /// </summary>
        /// <param name="name">package to name.</param>
        /// <returns>true of the operation was successful.</returns>
        public static async Task<bool> AddPackageAsync(string name)
        {
            var request = Client.Add(name);
            var tcs = new TaskCompletionSource<bool>();
            Debug.Log($"AddPackageAsync: {request}");
            void AddPackageProgress()
            {
                Debug.Log($"AddPackageProgress: {request}");
                if (request.IsCompleted)
                {
                    Debug.Log($"AddPackageProgress Status: {request.Status}");
                    if (request.Status >= StatusCode.Failure)
                        Debug.LogError(request.Error.message);

                    tcs.SetResult(request.Status == StatusCode.Success);
                    EditorApplication.update -= AddPackageProgress;
                }
            }

            EditorApplication.update += AddPackageProgress;
            return await tcs.Task;
        }

        /// <summary>
        /// Adds a package dependency to the Project. This is the equivalent of installing a package.
        /// <para>--- To install the latest compatible version of a package, specify only the package.</para>
        /// <para>--- To install a git package, specify a git url</para>
        /// <para> --- To install a local package, specify a value in the format "file:pathtopackagefolder".</para>
        /// <para>--- To install a local tarball package, specify a value in the format "file:pathto/package-file.tgz".</para>
        /// <para>ArgumentException is thrown if identifier is null or empty.</para>
        /// </summary>
        /// <param name="name">package to name.</param>
        /// <returns>true of the operation was successful.</returns>
        public static async Task<bool> AddPackageAndEmbedAsync(string name)
        {
          
            var tcs = new TaskCompletionSource<bool>();
            AddRequest request = null;
            EmbedRequest embedRequest = null;
            ListRequest listRequest = null;
            void RunProcess()
            {
                if (request == null)
                {
                    request = Client.Add(name);
                }

                if (request.IsCompleted)
                {
                    if (request.Status >= StatusCode.Failure)
                    {
                        Debug.LogError(request.Error.message);
                        tcs.SetResult(request.Status == StatusCode.Success);
                    }
                    else
                    {
                        var packageName = request.Result.name;
            
                  
                        if (listRequest == null)
                        {
                            listRequest = Client.List(true);
                        }

                        var packageFound = false;
                            if (listRequest.IsCompleted)
                            {
                                if (listRequest.Status == StatusCode.Success)
                                {
                                    foreach (var package in listRequest.Result)
                                        // Only retrieve packages that are currently installed in the
                                        // project (and are neither Built-In nor already Embedded)
                                        if (package.isDirectDependency
                                            && package.source
                                            != PackageSource.BuiltIn
                                            && package.source
                                            != PackageSource.Embedded)
                                            if (package.name.Equals(packageName))
                                            {
                                                packageFound = true;
                                                break;
                                            }

                                    if (packageFound)
                                    {
                                        if(embedRequest== null)
                                        embedRequest = Client.Embed(packageName);
                                     


                                        if (embedRequest.IsCompleted)
                                        {
                                            if (embedRequest.Status == StatusCode.Success)
                                                Debug.Log("Embedded: " + embedRequest.Result.packageId);
                                            else if (embedRequest.Status >= StatusCode.Failure)
                                                Debug.LogError(embedRequest.Error.message);

                                            tcs.SetResult(request.Status == StatusCode.Success);
                                           
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogError($"Could not find package: [{packageName}]");
                                        tcs.SetResult(false);
                                    }
                                }
                                else
                                {
                                    Debug.LogError(listRequest.Error.message);
                                    tcs.SetResult(false);
                                }

                 
                            }
                    }


                    EditorApplication.update -= RunProcess;
                }
                
            }
            
            EditorApplication.update += RunProcess;
            return await tcs.Task;
        }
        /// <summary>
        /// Adds a package dependency to the Project. This is the equivalent of installing a package.
        /// <para>--- To install the latest compatible version of a package, specify only the package.</para>
        /// <para>--- To install a git package, specify a git url</para>
        /// <para> --- To install a local package, specify a value in the format "file:pathtopackagefolder".</para>
        /// <para>--- To install a local tarball package, specify a value in the format "file:pathto/package-file.tgz".</para>
        /// <para>ArgumentException is thrown if identifier is null or empty.</para>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="success"> returns true or false based on if the package installation was successful</param>
        public static void AddPackage(string name, Action<bool> success)
        {
            var request = Client.Add(name);
            EditorApplication.update += AddPackageProgress;

            void AddPackageProgress()
            {
                if (request.IsCompleted)
                {
                    if (request.Status >= StatusCode.Failure) Debug.LogError(request.Error.message);

                    success.Invoke(request.Status == StatusCode.Success);
                    EditorApplication.update -= AddPackageProgress;
                }
            }
        }
        
           /// <summary>
        /// Adds a package dependency to the Project. This is the equivalent of installing a package.
        /// <para>--- To install the latest compatible version of a package, specify only the package.</para>
        /// <para>--- To install a git package, specify a git url</para>
        /// <para> --- To install a local package, specify a value in the format "file:pathtopackagefolder".</para>
        /// <para>--- To install a local tarball package, specify a value in the format "file:pathto/package-file.tgz".</para>
        /// <para>ArgumentException is thrown if identifier is null or empty.</para>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="success"> returns true or false based on if the package installation was successful</param>
        public static void AddPackageAndEmbed(string name, Action<bool> success)
        {
            var request = Client.Add(name);
            EditorApplication.update += AddPackageProgress;


            void AddPackageProgress()
            {
                if (request.IsCompleted)
                {
                    if (request.Status >= StatusCode.Failure)
                    {
                        Debug.LogError(request.Error.message);
                        success.Invoke(false);
                    }
                    else
                    {
                        var packageName = request.Result.name;
                        var listRequest = Client.List(true);
                        EditorApplication.update += CheckForAddedPackageProgress;


                        void CheckForAddedPackageProgress()
                        {
                            var packageFound = false;
                            if (listRequest.IsCompleted)
                            {
                                if (listRequest.Status == StatusCode.Success)
                                {
                                    foreach (var package in listRequest.Result)
                                        // Only retrieve packages that are currently installed in the
                                        // project (and are neither Built-In nor already Embedded)
                                        if (package.isDirectDependency
                                            && package.source
                                            != PackageSource.BuiltIn
                                            && package.source
                                            != PackageSource.Embedded)
                                            if (package.name.Equals(packageName))
                                            {
                                                packageFound = true;
                                                break;
                                            }

                                    if (packageFound)
                                    {
                                        var embedRequest = Client.Embed(packageName);
                                        EditorApplication.update += EmbedRequestProgress;


                                        void EmbedRequestProgress()
                                        {
                                            if (embedRequest.IsCompleted)
                                            {
                                                if (embedRequest.Status == StatusCode.Success)
                                                    Debug.Log("Embedded: " + embedRequest.Result.packageId);
                                                else if (embedRequest.Status >= StatusCode.Failure)
                                                    Debug.LogError(embedRequest.Error.message);

                                                success.Invoke(request.Status == StatusCode.Success);
                                                EditorApplication.update -= EmbedRequestProgress;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogError($"Could not find package: [{packageName}]");
                                        success.Invoke(false);
                                    }
                                }
                                else
                                {
                                    Debug.LogError(listRequest.Error.message);
                                    success.Invoke(false);
                                }

                                EditorApplication.update -= CheckForAddedPackageProgress;

                                // Embed(targetPackage);
                            }
                        }
                    }


                    EditorApplication.update -= AddPackageProgress;
                }
            }
        }
           
                   /// <summary>
        /// Checks if packages exists in the current project
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="success"></param>
        public static void GetPackageInfo(string packageName, Action<UnityEditor.PackageManager.PackageInfo> info)
        {
            if (_listRequest == null || _listRequest.IsCompleted)
            {
                _listRequest = Client.List(true);
                EditorApplication.update += CheckForAddedPackageProgress;
            }



            void CheckForAddedPackageProgress()
            {
                UnityEditor.PackageManager.PackageInfo packageInfo = null;
                if (_listRequest.IsCompleted)
                {
                    if (_listRequest.Status == StatusCode.Success)
                    {
                        foreach (var package in _listRequest.Result)
                            // Only retrieve packages that are currently installed in the
                            // project (and are neither Built-In nor already Embedded)
                            if (package.isDirectDependency
                            && package.source
                            != PackageSource.BuiltIn)
                                if (package.name.Equals(packageName))
                                {
                                    packageInfo = package;
                                    break;
                                }

                        info.Invoke(packageInfo);
                    }
                    else
                    {
                        Debug.LogError(_listRequest.Error.message);
                        info.Invoke(packageInfo);
                    }

                    EditorApplication.update -= CheckForAddedPackageProgress;
                }
            }
        }
        /// <summary>
        /// Moves the desired package to the Packages folder. (This prevents the package from being read only). 
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="skipCheck">Skip checking if the package exists</param>
        /// <param name="success"></param>
        public static void EmbedPackage(string packageName, bool skipCheck, Action<bool> success)
        {

            if (skipCheck)
            {
                EmbedPackage(packageName, success);
                return;
            }
            var listRequest = Client.List(true);
            EditorApplication.update += CheckForAddedPackageProgress;


            void CheckForAddedPackageProgress()
            {
                var packageFound = false;
                if (listRequest.IsCompleted)
                {
                    if (listRequest.Status == StatusCode.Success)
                    {
                        foreach (var package in listRequest.Result)
                            // Only retrieve packages that are currently installed in the
                            // project (and are neither Built-In nor already Embedded)
                            if (package.isDirectDependency
                                && package.source
                                != PackageSource.BuiltIn
                                && package.source
                                != PackageSource.Embedded)
                                if (package.name.Equals(packageName))
                                {
                                    packageFound = true;
                                    break;
                                }

                        if (packageFound)
                        {
                            var embedRequest = Client.Embed(packageName);
                            EditorApplication.update += EmbedRequestProgress;


                            void EmbedRequestProgress()
                            {
                                if (embedRequest.IsCompleted)
                                {
                                    if (embedRequest.Status == StatusCode.Success)
                                        Debug.Log("Embedded: " + embedRequest.Result.packageId);
                                    else if (embedRequest.Status >= StatusCode.Failure)
                                        Debug.LogError(embedRequest.Error.message);

                                    success.Invoke(embedRequest.Status == StatusCode.Success);
                                    EditorApplication.update -= EmbedRequestProgress;
                                }
                            }
                        }
                        else
                        {
                            Debug.LogError($"Could not find package: [{packageName}]");
                            success.Invoke(false);
                        }
                    }
                    else
                    {
                        Debug.LogError(listRequest.Error.message);
                        success.Invoke(false);
                    }

                    EditorApplication.update -= CheckForAddedPackageProgress;
                }
            }
        }
        
        
        /// <summary>
        /// Moves the desired package to the Packages folder. (This prevents the package from being read only)
        /// </summary>
        public static void EmbedPackage(string packageName, Action<bool> success)
        {
            var embedRequest = Client.Embed(packageName);
            EditorApplication.update += EmbedRequestProgress;

            void EmbedRequestProgress()
            {
                if (embedRequest.IsCompleted)
                {
                    if (embedRequest.Status == StatusCode.Success)
                        Debug.Log("Embedded: " + embedRequest.Result.packageId);
                    else if (embedRequest.Status >= StatusCode.Failure)
                        Debug.LogError(embedRequest.Error.message);

                    success.Invoke(embedRequest.Status == StatusCode.Success);
                    EditorApplication.update -= EmbedRequestProgress;
                }
            }

          
        }

        /// <summary>
        /// Removes a given package from the project
        /// </summary>
        /// <param name="name"></param>
        /// <param name="success"></param>
        public static void RemovePackage(string name, Action<bool> success)
        {
            var request = Client.Remove(name);
            EditorApplication.update += RemovePackageProgress;


            void RemovePackageProgress()
            {
                if (request.IsCompleted)
                {
                    if (request.Status >= StatusCode.Failure) Debug.LogError(request.Error.message);

                    success.Invoke(request.Status == StatusCode.Success);
                    EditorApplication.update -= RemovePackageProgress;
                }
            }
        }
        


        /// <summary>
        /// Checks if packages exists in the current project
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="success"></param>
        public static void HasPackage(string packageName, Action<bool> success)
        {
            var listRequest = Client.List(true);
            EditorApplication.update += CheckForAddedPackageProgress;


            void CheckForAddedPackageProgress()
            {
                var packageFound = false;
                if (listRequest.IsCompleted)
                {
                    if (listRequest.Status == StatusCode.Success)
                    {
                        foreach (var package in listRequest.Result)
                            // Only retrieve packages that are currently installed in the
                            // project (and are neither Built-In nor already Embedded)
                            if (package.isDirectDependency
                                && package.source
                                != PackageSource.BuiltIn)
                                if (package.name.Equals(packageName))
                                {
                                    packageFound = true;
                                    break;
                                }

                        if (packageFound)
                            success.Invoke(true);
                        else
                            success.Invoke(false);
                    }
                    else
                    {
                        Debug.LogError(listRequest.Error.message);
                        success.Invoke(false);
                    }

                    EditorApplication.update -= CheckForAddedPackageProgress;
                }
            }
        }
        /// <summary>
        /// Checks if packages exists in the current project
        /// </summary>
        /// <param name="packageName">package to name.</param>
        /// <returns>true of the operation was successful.</returns>
        public static async Task<bool> HasPackageAsync(string packageName)
        {
            var listRequest = Client.List(true);
            var tcs = new TaskCompletionSource<bool>();


            void CheckForAddedPackageProgress()
            {
             
                var packageFound = false;
                if (listRequest.IsCompleted)
                {
                    if (listRequest.Status == StatusCode.Success)
                    {
                        foreach (var package in listRequest.Result)
                            // Only retrieve packages that are currently installed in the
                            // project (and are neither Built-In nor already Embedded)
                            if (package.isDirectDependency
                                && package.source
                                != PackageSource.BuiltIn)
                                if (package.name.Equals(packageName))
                                {
                                    packageFound = true;
                                    break;
                                }
                        tcs.SetResult(packageFound);
                    }
                    else
                    {
                        Debug.LogError(listRequest.Error.message);
                        tcs.SetResult(false);
                    }

                    EditorApplication.update -= CheckForAddedPackageProgress;
                }
            }
            
            EditorApplication.update += CheckForAddedPackageProgress;
            return await tcs.Task;
        }

        private static ListRequest _listRequest;
        /// <summary>
        /// Checks if packages exists in the current project
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns>PackageInfo if successful.</returns>
        public static async Task<UnityEditor.PackageManager.PackageInfo> GetPackageInfoAsync(string packageName)
        {
            var tcs = new TaskCompletionSource<UnityEditor.PackageManager.PackageInfo>();
            if (_listRequest == null || _listRequest.IsCompleted)
            {
   
                _listRequest = Client.List(true);
                EditorApplication.update += CheckForAddedPackageProgress;
            }
            else
            {
                tcs.SetCanceled();
            }



            void CheckForAddedPackageProgress()
            {
                UnityEditor.PackageManager.PackageInfo packageInfo = null;
                if (_listRequest.IsCompleted)
                {
                    if (_listRequest.Status == StatusCode.Success)
                    {
                        foreach (var package in _listRequest.Result)
                            // Only retrieve packages that are currently installed in the
                            // project (and are neither Built-In nor already Embedded)
                            if (package.isDirectDependency
                            && package.source
                            != PackageSource.BuiltIn)
                                if (package.name.Equals(packageName))
                                {
                                    packageInfo = package;
                                    break;
                                }
                        tcs.SetResult(packageInfo);
               
                    }
                    else
                    {
                        Debug.LogError(_listRequest.Error.message);
                        tcs.SetResult(null);
                    }

                    EditorApplication.update -= CheckForAddedPackageProgress;
                }
            }
            
            await tcs.Task;
            return tcs.Task.Result;
        }
        /// <summary>
        /// Moves the desired package to the Packages folder. (This prevents the package from being read only). 
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="skipCheck">Skip checking if the package exists</param>
        /// <param name="success"></param>
        public static async Task<bool> EmbedPackageAsync(string packageName, bool skipCheck)
        {
     
            if (skipCheck)
            {
                var embedResult = await EmbedPackageAsync(packageName);
                return embedResult;
            }

            var hasPackageResult = await HasPackageAsync(packageName);
            if (!hasPackageResult)
            {
                return false;
            }
            else
            {
                var embedResult = await EmbedPackageAsync(packageName);
                return embedResult;
            }
        }


        /// <summary>
        /// Moves the desired package to the Packages folder. (This prevents the package from being read only)
        /// </summary>
        public static async Task<bool> EmbedPackageAsync(string packageName)
        {
            var tcs = new TaskCompletionSource<bool>();
            var embedRequest = Client.Embed(packageName);
            EditorApplication.update += EmbedRequestProgress;

            void EmbedRequestProgress()
            {
                if (embedRequest.IsCompleted)
                {
                    if (embedRequest.Status == StatusCode.Success)
                        Debug.Log("Embedded: " + embedRequest.Result.packageId);
                    else if (embedRequest.Status >= StatusCode.Failure)
                        Debug.LogError(embedRequest.Error.message);

                    tcs.SetResult(embedRequest.Status == StatusCode.Success);
                    EditorApplication.update -= EmbedRequestProgress;
                }
            }

            return await tcs.Task;


        }
        /// <summary>
        /// Removes a given package from the project.
        /// </summary>
        /// <param name="name">package to name to remove.</param>
        /// <returns>true of the operation was successful.</returns>
        public static async Task<bool> RemovePackageAsync(string name)
        {
            var request = Client.Remove(name);
            var tcs = new TaskCompletionSource<bool>();

            void RemovePackageProgress()
            {
                if (request.IsCompleted)
                {
                    if (request.Status >= StatusCode.Failure)
                        Debug.LogError(request.Error.message);

                    tcs.SetResult(request.Status == StatusCode.Success);
                    EditorApplication.update -= RemovePackageProgress;
                }
            }

            EditorApplication.update += RemovePackageProgress;
            return await tcs.Task;
        }

   
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private static bool _isOperationRunning = false;
        private static List<TaskCompletionSource<(bool, bool)>> _tcsList = new List<TaskCompletionSource<(bool, bool)>>();

 
        // Asynchronous method signature
        public static Task<(bool success, bool hasPackage)> HasPackageInstalledAsync(string name, bool offline = false, bool includeIndirectDependencies = false)
        {
            TaskCompletionSource<(bool, bool)> tcs = new TaskCompletionSource<(bool, bool)>();
            _packageNamesToCheck.Add(name);
            _tcsList.Add(tcs);
        
            QueueClientListOperationIfNeeded(offline, includeIndirectDependencies);
            return tcs.Task;
        }

        private static void QueueClientListOperationIfNeeded(bool offline, bool includeIndirectDependencies)
        {
            if (!_isOperationRunning)
            {
                _isOperationRunning = true;
                _semaphore.Wait(); // Synchronously wait to acquire the semaphore
                EditorApplication.update += CheckClientListOperation;
                _listInstalledPackagesRequest = Client.List(offline, includeIndirectDependencies); // Assuming this is how you initiate the list operation
            }
        }

        private static void CheckClientListOperation()
        {
            if (!_listInstalledPackagesRequest.IsCompleted) return;

            // Handle the completion
            var result = _listInstalledPackagesRequest.Result; // Assuming Result gives you the list of packages
            for (int i = 0; i < _packageNamesToCheck.Count; i++)
            {
                var packageName = _packageNamesToCheck[i];
                var hasPackage = result.Any(e => e.name.Contains(packageName));
                _tcsList[i].SetResult((_listInstalledPackagesRequest.Status == StatusCode.Success, hasPackage));
            }

            // Cleanup
            EditorApplication.update -= CheckClientListOperation;
            _packageNamesToCheck.Clear();
            _tcsList.Clear();
            _isOperationRunning = false;
            _semaphore.Release();
        }
        /// <param name="name">Name of package</param>
        /// <param name="successAndHasPackage">
        /// first bool returns true if the operation was successful. The second bool returns
        /// true if the package exists
        /// </param>
        public static void HasPackageInstalled(string name, Action<bool, bool> successAndHasPackage,
            bool offline = false, bool includeIndirectDependencies = false)
        {
            //Only run if Client.List is not currently running (only one Client operation can run at a time)
            if (!_hasListRequest)
            {
                _hasListRequest = true;
                try
                {
                    _listInstalledPackagesRequest = Client.List(offline, includeIndirectDependencies);
                    EditorApplication.update += ClientListProgress;
                }
                catch (Exception)
                {
                    successAndHasPackage?.Invoke(true,false);
                    return;
                }
              
            }
            //If Client operation is running, cache the package name and callback so that it can be checked in the currently running operation
            _packageNamesToCheck.Add(name);
            _checkRequestFinished.Add(successAndHasPackage);
        }


        private static void ClientListProgress()
        {
      
            if (!_listInstalledPackagesRequest.IsCompleted) return;
         
            // _listInstalledPackagesRequest.Result has a list of the current packages in the project
            //Name: com.magicleap.unitysdk | PackageID: PATH/TO/PACKAGE/com.magicleap.unitysdk or com.magicleap.unitysdk@{VERSION.VERSION}

            for (var i = 0; i < _packageNamesToCheck.Count; i++)
            {
         
                _checkRequestFinished[i].Invoke(_listInstalledPackagesRequest.Status == StatusCode.Success,
                                                _listInstalledPackagesRequest.Status == StatusCode.Success &&
                                                _listInstalledPackagesRequest.Result.Any(e => e.name.Contains(_packageNamesToCheck[i])));
            }


            _checkRequestFinished.Clear();
            _packageNamesToCheck.Clear();
            _hasListRequest = false;

            EditorApplication.update -= ClientListProgress;
        }
    }
}