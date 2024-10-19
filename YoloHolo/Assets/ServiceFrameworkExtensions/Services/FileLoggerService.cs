using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RealityCollective.ServiceFramework.Services;
using UnityEngine;

namespace ServiceFrameworkExtensions.Services
{
    [System.Runtime.InteropServices.Guid("ead723fd-b602-4bce-8b41-f43a624a5440")]
    public class FileLoggerService : BaseServiceWithConstructor, IFileLoggerService
    {
        private readonly string nl = Environment.NewLine;
        private readonly FileLoggerServiceProfile serviceProfile;
        private StreamWriter currentLogFile = null;
        private readonly Queue<string> logMessages = new();

        public FileLoggerService(string name, uint priority, FileLoggerServiceProfile profile)
            : base(name, priority)
        {
            serviceProfile = profile;
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            DeleteOldLogs();
        }

        /// <inheritdoc />
        public override void Start()
        {
            if (serviceProfile.AutoStart)
            {
                StartLogging();
            }
        }

        public override void Destroy()
        {
            StopLogging();
            base.Destroy();
        }

        public void StartLogging()
        {
            if (currentLogFile == null)
            {
                Application.logMessageReceivedThreaded += LogListener;
            }
        }

        public void StopLogging()
        {
            Application.logMessageReceivedThreaded -= LogListener;
            if (currentLogFile != null)
            {
                currentLogFile.Close();
                currentLogFile = null;
            }
        }

        public override void Update()
        {
            base.Update();
            if (logMessages.Any())
            {
                LogFile.WriteLine(logMessages.Dequeue());
            }
        }

        private void DeleteOldLogs()
        {
            foreach (var file in GetOldLogFilesToDelete())
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Log {file} could not be deleted: {e}");
                }
            }
        }

        private IEnumerable<string> GetOldLogFilesToDelete()
        {
            return Directory.GetFiles(Application.persistentDataPath,
                $"{serviceProfile.LogFilePrefix}*.log").Where(file =>
                Math.Abs((DateTimeOffset.Now - File.GetCreationTime(file)).Days) >
                serviceProfile.RetainDays);
        }

        private StreamWriter LogFile
        {
            get
            {
                if (currentLogFile == null)
                {
                    var logFilePath = Path.Combine(Application.persistentDataPath,
                        $"{serviceProfile.LogFilePrefix}{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.log");
                    currentLogFile = new StreamWriter(
                        new FileStream(logFilePath, FileMode.Append, FileAccess.Write));
                    currentLogFile.AutoFlush = true;
                }

                return currentLogFile;
            }
        }

        private void LogListener(string logString, string stacktrace, LogType lType)
        {
            if ( ShouldLog(logString, lType))
            {
                logMessages.Enqueue(GetLogString(logString, stacktrace, lType));
            }
        }

        private bool ShouldLog(string logString,LogType lType)
        {
            var logTypeFlagInt = 1 << (int)lType;
            return ((int)serviceProfile.LogTypes & logTypeFlagInt) != 0 &&
                   !serviceProfile.FilterPhrases.Any(logString.Contains);
        }
        private string GetLogString(string message, string stacktrace, LogType lType)
        {
            var timeStamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var trace = !string.IsNullOrEmpty(stacktrace) ? 
                $"StackTrace: {stacktrace}{nl}" : string.Empty;
            return
                $"Time: {timeStamp}{nl}Log: {lType}{nl}Msg: {message}{nl}{trace}====={nl}";
        }
    }
}