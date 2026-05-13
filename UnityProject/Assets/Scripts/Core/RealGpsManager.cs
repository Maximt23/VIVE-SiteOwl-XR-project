using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SiteOwlXR.Core
{
    /// <summary>
    /// REAL GPS Manager for VIVE XR Elite.
    /// Captures actual satellite GPS coordinates, not estimates.
    /// Uses Android Location Services - no external hardware needed.
    /// </summary>
    public class RealGpsManager : MonoBehaviour
    {
        [Header("GPS Settings")]
        public float desiredAccuracy = 5f;      // Target accuracy in meters
        public float updateDistance = 1f;     // Min distance between updates
        public float timeout = 20f;           // Timeout for GPS lock
        
        [Header("Status")]
        public bool IsGpsEnabled { get; private set; } = false;
        public bool IsGpsReady { get; private set; } = false;
        public LocationServiceStatus GpsStatus { get; private set; }
        
        [Header("Current GPS Data")]
        public double Latitude { get; private set; } = 0;
        public double Longitude { get; private set; } = 0;
        public double Altitude { get; private set; } = 0;
        public float Accuracy { get; private set; } = 999f;
        public DateTime Timestamp { get; private set; }
        
        [Header("Events")]
        public event Action OnGpsInitialized;
        public event Action OnGpsDataUpdated;
        public event Action<string> OnGpsError;
        
        private Coroutine gpsCoroutine;
        
        void Start()
        {
            // Only works on Android (VIVE XR Elite)
            if (Application.platform != RuntimePlatform.Android)
            {
                Debug.Log("[RealGpsManager] Running on non-Android platform. Using mock GPS.");
                OnGpsError?.Invoke("Non-Android platform - GPS unavailable");
                return;
            }
            
            StartGps();
        }
        
        /// <summary>
        /// Starts the REAL GPS service.
        /// This accesses the actual GPS chip in the VIVE XR Elite.
        /// </summary>
        public void StartGps()
        {
            if (!IsGpsEnabled)
            {
                gpsCoroutine = StartCoroutine(InitializeGps());
            }
        }
        
        /// <summary>
        /// Stops GPS to save battery.
        /// </summary>
        public void StopGps()
        {
            if (IsGpsEnabled)
            {
                Input.location.Stop();
                IsGpsEnabled = false;
                IsGpsReady = false;
                Debug.Log("[RealGpsManager] GPS stopped.");
            }
            
            if (gpsCoroutine != null)
            {
                StopCoroutine(gpsCoroutine);
            }
        }
        
        IEnumerator InitializeGps()
        {
            Debug.Log("[RealGpsManager] Initializing REAL GPS...");
            
            // Check if user has location service enabled
            if (!Input.location.isEnabledByUser)
            {
                Debug.LogWarning("[RealGpsManager] Location services not enabled by user!");
                OnGpsError?.Invoke("GPS not enabled. Enable in Android Settings > Location.");
                yield break;
            }
            
            // Start location service with real satellite GPS
            Input.location.Start(desiredAccuracy, updateDistance);
            IsGpsEnabled = true;
            
            // Wait for initialization
            float timer = 0f;
            while (Input.location.status == LocationServiceStatus.Initializing && timer < timeout)
            {
                GpsStatus = Input.location.status;
                timer += Time.deltaTime;
                Debug.Log($"[RealGpsManager] Waiting for GPS lock... {timer:F1}s");
                yield return new WaitForSeconds(1f);
            }
            
            // Check if timed out
            if (timer >= timeout)
            {
                Debug.LogError("[RealGpsManager] GPS initialization timed out!");
                OnGpsError?.Invoke("GPS timeout. Check sky visibility.");
                Input.location.Stop();
                IsGpsEnabled = false;
                yield break;
            }
            
            // Check if failed
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.LogError("[RealGpsManager] GPS initialization failed!");
                OnGpsError?.Invoke("GPS failed. Hardware issue?");
                Input.location.Stop();
                IsGpsEnabled = false;
                yield break;
            }
            
            // GPS is ready!
            if (Input.location.status == LocationServiceStatus.Running)
            {
                IsGpsReady = true;
                GpsStatus = Input.location.status;
                
                // Get first reading
                UpdateGpsData();
                
                Debug.Log($"[RealGpsManager] GPS LOCKED! Location: {Latitude}, {Longitude} (±{Accuracy}m)");
                OnGpsInitialized?.Invoke();

                // Poll at 1 Hz — GPS hardware only updates ~1/sec anyway
                InvokeRepeating(nameof(PollGps), 1f, 1f);
            }
        }
        
        void Update() { }  // GPS polling moved to InvokeRepeating — see InitializeGps()

        private void PollGps()
        {
            if (IsGpsReady && Input.location.status == LocationServiceStatus.Running)
                UpdateGpsData();
        }
        
        void UpdateGpsData()
        {
            var location = Input.location.lastData;
            
            bool changed = false;
            
            if (Math.Abs(Latitude - location.latitude) > 0.0000001)
            {
                Latitude = location.latitude;
                changed = true;
            }
            
            if (Math.Abs(Longitude - location.longitude) > 0.0000001)
            {
                Longitude = location.longitude;
                changed = true;
            }
            
            if (Math.Abs(Altitude - location.altitude) > 0.1)
            {
                Altitude = location.altitude;
                changed = true;
            }
            
            if (Math.Abs(Accuracy - location.horizontalAccuracy) > 0.1)
            {
                Accuracy = location.horizontalAccuracy;
                changed = true;
            }
            
            Timestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(location.timestamp);
            
            if (changed)
            {
                OnGpsDataUpdated?.Invoke();
            }
        }
        
        /// <summary>
        /// Gets the current GPS position as a formatted string.
        /// </summary>
        public string GetPositionString()
        {
            if (!IsGpsReady)
                return "GPS: Searching...";
            
            return $"GPS: {Latitude:F6}, {Longitude:F6} (±{Accuracy:F1}m)";
        }
        
        /// <summary>
        /// Gets GPS quality based on accuracy.
        /// </summary>
        public GpsQuality GetGpsQuality()
        {
            if (!IsGpsReady)
                return GpsQuality.Unavailable;
            
            if (Accuracy <= 3f)
                return GpsQuality.Excellent;
            if (Accuracy <= 10f)
                return GpsQuality.Good;
            if (Accuracy <= 30f)
                return GpsQuality.Fair;
            
            return GpsQuality.Poor;
        }
        
        /// <summary>
        /// Calculates distance in meters between two GPS points.
        /// Uses Haversine formula for accuracy.
        /// </summary>
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000; // Earth radius in meters
            
            double lat1Rad = lat1 * Math.PI / 180;
            double lat2Rad = lat2 * Math.PI / 180;
            double deltaLat = (lat2 - lat1) * Math.PI / 180;
            double deltaLon = (lon2 - lon1) * Math.PI / 180;
            
            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
            
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return R * c;
        }
        
        /// <summary>
        /// Gets the GPS position at the time of capture.
        /// </summary>
        public (double lat, double lon, float accuracy, string timestamp) CaptureGps()
        {
            if (!IsGpsReady)
            {
                Debug.LogWarning("[RealGpsManager] GPS not ready, returning zeros.");
                return (0, 0, 999, DateTime.UtcNow.ToString("O"));
            }
            
            return (
                Latitude,
                Longitude,
                Accuracy,
                Timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ")
            );
        }
        
        void OnDestroy()
        {
            StopGps();
        }
    }
    
    public enum GpsQuality
    {
        Unavailable,
        Poor,       // > 30m accuracy
        Fair,       // 10-30m
        Good,       // 3-10m
        Excellent   // < 3m
    }
}
