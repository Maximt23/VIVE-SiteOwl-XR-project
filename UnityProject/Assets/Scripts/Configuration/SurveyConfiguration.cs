using UnityEngine;

namespace SiteOwlXR.Configuration
{
    /// <summary>
    /// ScriptableObject for centralized configuration
    /// Enterprise-grade configuration management
    /// </summary>
    [CreateAssetMenu(fileName = "SurveyConfiguration", menuName = "CCTV Survey/Configuration")]
    public class SurveyConfiguration : ScriptableObject
    {
        [Header("GPS Settings")]
        public float desiredAccuracy = 5f;
        public float updateDistance = 1f;
        public float gpsTimeout = 20f;
        public bool useMockGpsInEditor = true;
        public float mockLatitude = 32.812345f;
        public float mockLongitude = -96.812345f;
        
        [Header("Calibration")]
        public float pixelsPerMeter = 10f;
        public float highConfidenceDistance = 10f;
        public float mediumConfidenceDistance = 30f;
        
        [Header("Photo Capture")]
        public int photoWidth = 1920;
        public int photoHeight = 1080;
        public int jpegQuality = 85;
        public string photosFolder = "CapturedPhotos";
        
        [Header("AI Recognition")]
        public float aiConfidenceThreshold = 0.6f;
        public int maxAiSuggestions = 3;
        public bool useExternalAiApi = false;
        public string externalAiUrl = "";
        
        [Header("Storage")]
        public Models.StorageProvider primaryProvider = Models.StorageProvider.LocalNetwork;
        public bool fallbackToLocal = true;
        public int uploadTimeoutSeconds = 30;
        public int uploadRetryCount = 3;
        public float uploadRetryDelay = 2f;
        
        [Header("CSV")]
        public string csvFileName = "cameras.csv";
        public string backupFolder = "Backups";
        public bool createTimestampedBackups = true;
        
        [Header("Performance")]
        public int maxCameraCacheSize = 100;
        public bool useCompression = true;
        public int targetFrameRate = 72;
        
        [Header("Enterprise Features")]
        public bool enableTelemetry = true;
        public bool enableDetailedLogging = false;
        public LogLevel minimumLogLevel = LogLevel.Info;
        public bool enableMetrics = true;
        
        private static SurveyConfiguration _instance;
        public static SurveyConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<SurveyConfiguration>("SurveyConfiguration");
                    if (_instance == null)
                    {
                        Debug.LogWarning("No SurveyConfiguration found in Resources. Creating default.");
                        _instance = CreateInstance<SurveyConfiguration>();
                    }
                }
                return _instance;
            }
        }
    }
    
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }
}
