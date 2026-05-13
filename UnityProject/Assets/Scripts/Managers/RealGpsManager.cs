using System;
using System.Collections;
using UnityEngine;
using SiteOwlXR.Events;
using SiteOwlXR.Models;
using SiteOwlXR.Services;
using SiteOwlXR.Configuration;

namespace SiteOwlXR.Managers
{
    /// <summary>
    /// GPS Manager - Enterprise Grade
    /// Handles Android location services with permission management, accuracy tracking, and error recovery
    /// </summary>
    public class RealGpsManager : MonoBehaviour
    {
        [Header("Configuration")]
        public float desiredAccuracy = 5f;
        public float updateDistance = 1f;
        public float timeoutSeconds = 20f;
        public bool useMockGpsInEditor = true;
        public float mockLatitude = 32.812345f;
        public float mockLongitude = -96.812345f;
        
        [Header("Status")]
        public GpsReading LastReading { get; private set; }
        public bool IsReady { get; private set; } = false;
        public bool IsInitializing { get; private set; } = false;
        public string StatusMessage { get; private set; } = "Not started";
        
        [Header("Statistics")]
        public int ReadingsCount { get; private set; } = 0;
        public float AverageAccuracy { get; private set; } = 999f;
        public DateTime? FirstReadingTime { get; private set; }
        
        private ILogger _logger;
        private SurveyConfiguration _config;
        private Coroutine _gpsCoroutine;
        private float _accuracySum = 0f;
        
        void Awake()
        {
            _logger = ServiceLocator.Get<ILogger>() ?? new UnityLogger();
            _config = SurveyConfiguration.Instance;
            
            // Override with config if available
            if (_config != null)
            {
                desiredAccuracy = _config.desiredAccuracy;
                updateDistance = _config.updateDistance;
                timeoutSeconds = _config.gpsTimeout;
                useMockGpsInEditor = _config.useMockGpsInEditor;
                mockLatitude = _config.mockLatitude;
                mockLongitude = _config.mockLongitude;
            }
        }
        
        void OnEnable()
        {
            CctvSurveyEvents.OnXrReady += OnXrReady;
        }
        
        void OnDisable()
        {
            CctvSurveyEvents.OnXrReady -= OnXrReady;
            StopGps();
        }
        
        void OnDestroy()
        {
            StopGps();
        }
        
        void OnXrReady()
        {
            // Start GPS after XR is ready
            StartCoroutine(InitializeGpsCoroutine());
        }
        
        IEnumerator InitializeGpsCoroutine()
        {
            IsInitializing = true;
            StatusMessage = "Initializing GPS...";
            _logger.LogInfo("Starting GPS initialization", this);
            
            // Editor mock mode
            if (Application.isEditor && useMockGpsInEditor)
            {
                _logger.LogInfo("Editor mode - using mock GPS", this);
                yield return new WaitForSeconds(0.5f);
                
                var mockReading = new GpsReading
                {
                    Latitude = mockLatitude,
                    Longitude = mockLongitude,
                    Altitude = 150,
                    Accuracy = 2.5f,
                    Timestamp = DateTime.UtcNow
                };
                
                ProcessReading(mockReading);
                IsInitializing = false;
                yield break;
            }
            
            // Check if location is enabled by user
            #if UNITY_ANDROID
            if (!Input.location.isEnabledByUser)
            {
                StatusMessage = "Location not enabled. Enable in Android Settings.";
                _logger.LogWarning("Location services not enabled by user", this);
                IsInitializing = false;
                yield break;
            }
            #endif
            
            // Start location service
            Input.location.Start(desiredAccuracy, updateDistance);
            
            // Wait for initialization with timeout
            float timer = 0f;
            StatusMessage = "Waiting for GPS signal...";
            
            while (timer < timeoutSeconds)
            {
                if (Input.location.status == LocationServiceStatus.Running)
                {
                    // GPS is running, get first reading
                    var reading = GpsReading.FromLocationInfo(Input.location.lastData);
                    
                    if (reading.IsValid)
                    {
                        ProcessReading(reading);
                        
                        // Continue monitoring for better accuracy
                        _gpsCoroutine = StartCoroutine(MonitorGpsCoroutine());
                        
                        IsInitializing = false;
                        yield break;
                    }
                }
                else if (Input.location.status == LocationServiceStatus.Failed)
                {
                    StatusMessage = "GPS initialization failed";
                    _logger.LogError("GPS service failed to start", null, this);
                    IsInitializing = false;
                    yield break;
                }
                
                StatusMessage = $"Waiting for GPS... {timer:F0}s";
                yield return new WaitForSeconds(0.5f);
                timer += 0.5f;
            }
            
            // Timeout
            StatusMessage = $"GPS timeout after {timeoutSeconds}s";
            _logger.LogWarning($"GPS initialization timed out", this);
            IsInitializing = false;
        }
        
        IEnumerator MonitorGpsCoroutine()
        {
            _logger.LogInfo("GPS monitoring started", this);
            StatusMessage = "GPS Active";
            
            while (true)
            {
                if (Input.location.status != LocationServiceStatus.Running)
                {
                    _logger.LogWarning("GPS signal lost", this);
                    CctvSurveyEvents.RaiseGpsLost();
                    StatusMessage = "GPS signal lost";
                    IsReady = false;
                    yield break;
                }
                
                var reading = GpsReading.FromLocationInfo(Input.location.lastData);
                
                if (reading.IsValid)
                {
                    // Check if accuracy improved
                    if (reading.Accuracy < LastReading.Accuracy)
                    {
                        _logger.LogDebug($"GPS accuracy improved: {reading.Accuracy:F1}m", this);
                        CctvSurveyEvents.RaiseGpsImproved(reading);
                    }
                    
                    ProcessReading(reading);
                }
                
                yield return new WaitForSeconds(1f);
            }
        }
        
        void ProcessReading(GpsReading reading)
        {
            LastReading = reading;
            IsReady = true;
            ReadingsCount++;
            
            if (FirstReadingTime == null)
            {
                FirstReadingTime = DateTime.UtcNow;
                _logger.LogInfo($"First GPS reading: {reading}", this);
                CctvSurveyEvents.RaiseGpsAcquired(reading);
            }
            
            // Update average accuracy
            _accuracySum += reading.Accuracy;
            AverageAccuracy = _accuracySum / ReadingsCount;
        }
        
        /// <summary>
        /// Get current reading formatted for SiteOwl Barcode field
        /// </summary>
        public string GetBarcodeFormat()
        {
            if (!IsReady || !LastReading.IsValid)
                return "";
                
            return LastReading.ToBarcodeFormat();
        }
        
        /// <summary>
        /// Get confidence level based on accuracy
        /// </summary>
        public CoordinateConfidence GetConfidence()
        {
            if (!IsReady)
                return CoordinateConfidence.NONE;
                
            return LastReading.GetConfidence();
        }
        
        /// <summary>
        /// Stop GPS service
        /// </summary>
        public void StopGps()
        {
            if (_gpsCoroutine != null)
            {
                StopCoroutine(_gpsCoroutine);
                _gpsCoroutine = null;
            }
            
            if (Input.location.isEnabledByUser)
            {
                Input.location.Stop();
                _logger.LogInfo("GPS stopped", this);
            }
            
            IsReady = false;
            StatusMessage = "GPS Stopped";
        }
        
        /// <summary>
        /// Retry GPS initialization
        /// </summary>
        public void Retry()
        {
            StopGps();
            IsReady = false;
            ReadingsCount = 0;
            _accuracySum = 0f;
            FirstReadingTime = null;
            
            StartCoroutine(InitializeGpsCoroutine());
        }
    }
}
