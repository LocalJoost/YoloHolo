
using System.Collections.Generic;
using RealityCollective.ServiceFramework.Definitions;
using RealityCollective.ServiceFramework.Interfaces;
using UnityEngine;

namespace ServiceFrameworkExtensions.Services
{
    [CreateAssetMenu(menuName = "MRTKExtensions/FileLoggerServiceProfile", fileName = "FileLoggerServiceProfile",
        order = (int)CreateProfileMenuItemIndices.ServiceConfig)]
    public class FileLoggerServiceProfile : BaseServiceProfile<IServiceModule>
    {
        [SerializeField]
        [Tooltip("The prefix for the logfiles")]
        private string logFilePrefix = "AppLog";
        public string LogFilePrefix => logFilePrefix;

        [SerializeField]
        [Tooltip("These phrases will be filtered from the logfiles")]
        private List<string> filterPhrases = new();
        public List<string> FilterPhrases => filterPhrases;
        
        [SerializeField]
        [Tooltip("The log types to log")]
        private LogTypeFlags logTypes = LogTypeFlags.Error | LogTypeFlags.Assert |
                                        LogTypeFlags.Warning | LogTypeFlags.Log | LogTypeFlags.Exception;
        public LogTypeFlags LogTypes => logTypes;
        
        [SerializeField]
        [Tooltip("old logfiles will be deleted after this many days")]
        private int retainDays = 4;
        public int RetainDays => retainDays;
        
        [SerializeField]
        [Tooltip("Auto start the logging")]
        private bool autoStart = true;
        public bool AutoStart => autoStart;
    }
}
