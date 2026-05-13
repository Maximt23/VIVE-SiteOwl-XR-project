using UnityEngine;
using System;
using System.Collections;

namespace SiteOwlXR.Core
{
    /// <summary>
    /// Main controller that orchestrates the capture workflow.
    /// Connects calibration, device selection, photo capture, and CSV updates.
    /// </summary>
    public class CaptureController : MonoBehaviour
    {
        [Header("Dependencies")]
        public CalibrationManager Calibration;
        public CsvManager CsvManager;
        public PhotoCapture PhotoCapture;
        public Transform CameraTransform;
        
        [Header("Raycast Settings")]
        public float MaxRaycastDistance = 50f;
        public LayerMask RaycastLayers = ~0; // All layers
        
        [Header("UI References")]
        public CaptureUI CaptureUI;
        
        [Header("Events")]
        public event Action<DeviceData> OnCaptureComplete;
        public event Action<string> OnCaptureError;
        
        private DeviceData currentDevice;
        private bool isCapturing = false;
        
        void Start()
        {
            if (CameraTransform == null)
                CameraTransform = Camera.main?.transform;
            
            if (Calibration == null)
                Debug.LogError("[CaptureController] CalibrationManager not assigned!");
            if (CsvManager == null)
                Debug.LogError("[CaptureController] CsvManager not assigned!");
            if (PhotoCapture == null)
                Debug.LogError("[CaptureController] PhotoCapture not assigned!");
            
            // Subscribe to events
            if (PhotoCapture != null)
            {
                PhotoCapture.OnPhotoSaved += OnPhotoSaved;
                PhotoCapture.OnCaptureError += OnPhotoCaptureError;
            }
        }
        
        /// <summary>
        /// Selects a device for capture.
        /// </summary>
        public void SelectDevice(DeviceData device)
        {
            currentDevice = device;
            Debug.Log($"[CaptureController] Selected: {device.DeviceName}");
            
            // Show device info in UI
            CaptureUI?.ShowDeviceInfo(device);
        }
        
        /// <summary>
        /// Performs raycast from headset to find capture point.
        /// </summary>
        public bool GetCapturePoint(out Vector3 worldPoint, out float distance)
        {
            worldPoint = Vector3.zero;
            distance = 0f;
            
            if (CameraTransform == null) return false;
            
            Ray ray = new Ray(CameraTransform.position, CameraTransform.forward);
            
            if (Physics.Raycast(ray, out RaycastHit hit, MaxRaycastDistance, RaycastLayers))
            {
                worldPoint = hit.point;
                distance = hit.distance;
                return true;
            }
            
            // No hit - use point at max distance
            worldPoint = ray.GetPoint(MaxRaycastDistance);
            distance = MaxRaycastDistance;
            return true;
        }
        
        /// <summary>
        /// Initiates the full capture workflow.
        /// </summary>
        public void CaptureCurrentDevice()
        {
            if (isCapturing)
            {
                Debug.LogWarning("[CaptureController] Already capturing!");
                return;
            }
            
            if (currentDevice == null)
            {
                OnCaptureError?.Invoke("No device selected");
                return;
            }
            
            if (!Calibration.IsCalibrated)
            {
                OnCaptureError?.Invoke("System not calibrated");
                return;
            }
            
            isCapturing = true;
            CaptureUI?.ShowCapturingState(true);
            
            // Step 1: Get capture point
            if (!GetCapturePoint(out Vector3 worldPoint, out float distance))
            {
                OnCaptureError?.Invoke("Failed to get capture point");
                isCapturing = false;
                CaptureUI?.ShowCapturingState(false);
                return;
            }
            
            Debug.Log($"[CaptureController] Capture point at {worldPoint}, distance: {distance:F2}m");
            
            // Step 2: Convert to SiteOwl coordinates
            Vector2 siteOwlXY = Calibration.WorldToSiteOwl(worldPoint);
            
            // Step 3: Get user position and facing
            Vector2 userSiteOwlXY = Calibration.GetCurrentSiteOwlPosition();
            float userFacing = Calibration.GetCurrentFacingDirection();
            
            // Step 4: Calculate confidence
            CoordinateConfidence confidence = Calibration.CalculateConfidence();
            
            // Step 5: Try to get GPS (optional, secondary)
            GetGpsCoordinates(out double? latitude, out double? longitude);
            
            // Step 6: Take photo
            StartCoroutine(CaptureWithPhoto(
                currentDevice,
                siteOwlXY,
                latitude,
                longitude,
                userSiteOwlXY,
                userFacing,
                confidence
            ));
        }
        
        private IEnumerator CaptureWithPhoto(
            DeviceData device,
            Vector2 siteOwlXY,
            double? latitude,
            double? longitude,
            Vector2 userXY,
            float userFacing,
            CoordinateConfidence confidence)
        {
            string photoPath = null;
            bool photoComplete = false;
            
            // Take photo
            PhotoCapture.TakePhoto(device.DeviceID, (path) =>
            {
                photoPath = path;
                photoComplete = true;
            });
            
            // Wait for photo
            yield return new WaitUntil(() => photoComplete);
            
            if (string.IsNullOrEmpty(photoPath))
            {
                OnCaptureError?.Invoke("Photo capture failed");
                isCapturing = false;
                CaptureUI?.ShowCapturingState(false);
                yield break;
            }
            
            // Record the capture
            device.RecordCapture(
                siteOwlXY.x,
                siteOwlXY.y,
                latitude,
                longitude,
                photoPath,
                confidence,
                userXY.x,
                userXY.y,
                userFacing
            );
            
            // Save to CSV
            if (CsvManager.UpdateDevice(device))
            {
                Debug.Log($"[CaptureController] Captured {device.DeviceName} at ({siteOwlXY.x:F2}, {siteOwlXY.y:F2}) " +
                    $"with confidence {confidence}, photo: {photoPath}");
                
                OnCaptureComplete?.Invoke(device);
                CaptureUI?.ShowCaptureSuccess(device);
            }
            else
            {
                OnCaptureError?.Invoke("Failed to save to CSV");
            }
            
            isCapturing = false;
            CaptureUI?.ShowCapturingState(false);
        }
        
        private void OnPhotoSaved(string path)
        {
            // Handled in coroutine
        }
        
        private void OnPhotoCaptureError(string error)
        {
            Debug.LogError($"[CaptureController] Photo error: {error}");
            // Handled in coroutine
        }
        
        /// <summary>
        /// Attempts to get GPS coordinates from Android location services.
        /// This is secondary/estimated data.
        /// </summary>
        private void GetGpsCoordinates(out double? latitude, out double? longitude)
        {
            latitude = null;
            longitude = null;
            
            // Only works on Android
            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }
            
            try
            {
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                    UnityEngine.Android.Permission.FineLocation))
                {
                    Debug.Log("[CaptureController] GPS permission not granted");
                    return;
                }
                
                if (!Input.location.isEnabledByUser)
                {
                    Debug.Log("[CaptureController] GPS not enabled");
                    return;
                }
                
                // Start if not already running
                if (Input.location.status == LocationServiceStatus.Stopped)
                {
                    Input.location.Start();
                }
                
                if (Input.location.status == LocationServiceStatus.Running)
                {
                    latitude = Input.location.lastData.latitude;
                    longitude = Input.location.lastData.longitude;
                    
                    Debug.Log($"[CaptureController] GPS acquired: {latitude}, {longitude} " +
                        $"(accuracy: {Input.location.lastData.horizontalAccuracy}m)");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CaptureController] GPS error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Shows a debug raycast line (for development).
        /// </summary>
        void Update()
        {
            if (CameraTransform != null && Calibration != null && Calibration.IsCalibrated)
            {
                // Visualize raycast in editor
                #if UNITY_EDITOR
                Debug.DrawRay(CameraTransform.position, CameraTransform.forward * MaxRaycastDistance, Color.cyan);
                #endif
            }
        }
        
        void OnDestroy()
        {
            if (PhotoCapture != null)
            {
                PhotoCapture.OnPhotoSaved -= OnPhotoSaved;
                PhotoCapture.OnCaptureError -= OnPhotoCaptureError;
            }
        }
    }
}
