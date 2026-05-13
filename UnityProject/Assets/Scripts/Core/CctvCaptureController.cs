using UnityEngine;
using System;
using System.Collections;
using System.Threading.Tasks;
using SiteOwlXR.AI;

namespace SiteOwlXR.Core
{
    /// <summary>
    /// CCTV Capture Controller - THE ORCHESTRATOR
    /// Connects all systems: GPS, Photos, AI, CSV, External Storage
    /// Manages the complete capture workflow from start to finish
    /// </summary>
    public class CctvCaptureController : MonoBehaviour
    {
        [Header("System Components (Auto-Find if not assigned)")]
        public RealGpsManager gpsManager;
        public SiteOwlCsvManager csvManager;
        public PhotoCapture photoCapture;
        public CctvCameraRecognizer cameraRecognizer;
        public AiCopilotUI aiCopilotUI;
        public ExternalPhotoUploader photoUploader;
        public ExternalDataConnector dataConnector;
        
        [Header("XR References")]
        public Transform cameraTransform;
        public LineRenderer pointerLine;
        
        [Header("Current State")]
        public CameraData currentCamera;
        public bool isCapturing = false;
        public CaptureState currentState = CaptureState.Idle;
        
        [Header("Events")]
        public event Action<CameraData> OnCaptureStarted;
        public event Action<CameraData> OnGpsCaptured;
        public event Action<CameraData, string> OnPhotoCaptured;  // camera, photoUrl
        public event Action<CameraData> OnAiAnalysisComplete;
        public event Action<CameraData> OnUserConfirmed;
        public event Action<CameraData> OnCaptureComplete;
        public event Action<string> OnCaptureError;
        
        public enum CaptureState
        {
            Idle,
            GpsCapturing,
            PhotoCapturing,
            PhotoUploading,
            AiAnalyzing,
            WaitingForUser,
            SavingData,
            Complete,
            Error
        }
        
        void Start()
        {
            // Auto-find components if not assigned
            FindComponents();
            
            // Subscribe to system events
            SubscribeToEvents();
            
            if (cameraTransform == null)
                cameraTransform = Camera.main?.transform;
            
            Debug.Log("[CctvCaptureController] Initialized and ready");
        }
        
        void FindComponents()
        {
            gpsManager = gpsManager ?? FindObjectOfType<RealGpsManager>();
            csvManager = csvManager ?? FindObjectOfType<SiteOwlCsvManager>();
            photoCapture = photoCapture ?? FindObjectOfType<PhotoCapture>();
            cameraRecognizer = cameraRecognizer ?? FindObjectOfType<CctvCameraRecognizer>();
            aiCopilotUI = aiCopilotUI ?? FindObjectOfType<AiCopilotUI>();
            photoUploader = photoUploader ?? FindObjectOfType<ExternalPhotoUploader>();
            dataConnector = dataConnector ?? FindObjectOfType<ExternalDataConnector>();
            
            // Log what's found
            Debug.Log($"[CctvCaptureController] Components found:");
            Debug.Log($"  GPS: {(gpsManager != null ? "✓" : "✗")}");
            Debug.Log($"  CSV: {(csvManager != null ? "✓" : "✗")}");
            Debug.Log($"  Photo: {(photoCapture != null ? "✓" : "✗")}");
            Debug.Log($"  AI: {(cameraRecognizer != null ? "✓" : "✗")}");
            Debug.Log($"  Uploader: {(photoUploader != null ? "✓" : "✗")}");
        }
        
        void SubscribeToEvents()
        {
            // GPS events
            if (gpsManager != null)
            {
                gpsManager.OnGpsInitialized += () => {
                    Debug.Log("[CctvCaptureController] GPS ready");
                };
            }
            
            // AI events
            if (cameraRecognizer != null)
            {
                cameraRecognizer.OnRecognitionComplete += (results) => {
                    currentState = CaptureState.WaitingForUser;
                    OnAiAnalysisComplete?.Invoke(currentCamera);
                };
            }
        }
        
        /// <summary>
        /// STARTS the complete capture workflow
        /// Called when user selects a camera and presses capture
        /// </summary>
        public void StartCapture(CameraData camera)
        {
            if (isCapturing)
            {
                OnCaptureError?.Invoke("Capture already in progress");
                return;
            }
            
            currentCamera = camera;
            isCapturing = true;
            currentState = CaptureState.GpsCapturing;
            
            Debug.Log($"[CctvCaptureController] Starting capture for {camera.DeviceName}");
            OnCaptureStarted?.Invoke(camera);
            
            // Start the sequence
            StartCoroutine(CaptureSequence());
        }
        
        IEnumerator CaptureSequence()
        {
            // STEP 1: Capture GPS
            yield return CaptureGpsStep();
            
            // STEP 2: Capture Photo
            yield return CapturePhotoStep();
            
            // STEP 3: Upload Photo to External Storage
            yield return UploadPhotoStep();
            
            // STEP 4: AI Analysis (optional)
            if (cameraRecognizer != null)
            {
                yield return AiAnalysisStep();
            }
            else
            {
                // Skip AI, go straight to saving
                currentState = CaptureState.SavingData;
            }
            
            // STEP 5: Save to CSV (happens after user confirms in AI step, or immediately)
            if (currentState == CaptureState.SavingData)
            {
                yield return SaveDataStep();
            }
            
            // Complete
            currentState = CaptureState.Complete;
            isCapturing = false;
            OnCaptureComplete?.Invoke(currentCamera);
            
            Debug.Log($"[CctvCaptureController] Capture complete for {currentCamera.DeviceName}");
        }
        
        IEnumerator CaptureGpsStep()
        {
            currentState = CaptureState.GpsCapturing;
            
            if (gpsManager == null || !gpsManager.IsGpsReady)
            {
                Debug.LogWarning("[CctvCaptureController] GPS not ready, using zeros");
                currentCamera.Latitude = 0;
                currentCamera.Longitude = 0;
                currentCamera.GpsAccuracy = 999;
            }
            else
            {
                // Get current GPS position
                currentCamera.Latitude = gpsManager.Latitude;
                currentCamera.Longitude = gpsManager.Longitude;
                currentCamera.GpsAccuracy = gpsManager.Accuracy;
                
                // Format for SiteOwl Barcode field
                currentCamera.GpsBarcode = $"{currentCamera.Latitude:F6},{currentCamera.Longitude:F6}";
                
                Debug.Log($"[CctvCaptureController] GPS captured: {currentCamera.GpsBarcode} (±{currentCamera.GpsAccuracy:F1}m)");
            }
            
            // Also capture SiteOwl X/Y if calibrated
            var calibration = FindObjectOfType<CalibrationManager>();
            if (calibration != null && calibration.IsCalibrated)
            {
                Vector2 siteOwlPos = calibration.GetCurrentSiteOwlPosition();
                currentCamera.SiteOwlX = siteOwlPos.x;
                currentCamera.SiteOwlY = siteOwlPos.y;
            }
            
            OnGpsCaptured?.Invoke(currentCamera);
            yield return new WaitForSeconds(0.1f);  // Let UI update
        }
        
        IEnumerator CapturePhotoStep()
        {
            currentState = CaptureState.PhotoCapturing;
            
            if (photoCapture == null)
            {
                Debug.LogError("[CctvCaptureController] Photo capture not available");
                OnCaptureError?.Invoke("Photo capture not configured");
                yield break;
            }
            
            // Capture photo
            string localPath = null;
            photoCapture.TakePhoto(currentCamera.DeviceName, (path) => {
                localPath = path;
            });
            
            // Wait for photo
            yield return new WaitUntil(() => localPath != null || !photoCapture.IsCapturing);
            
            if (string.IsNullOrEmpty(localPath))
            {
                OnCaptureError?.Invoke("Photo capture failed");
                yield break;
            }
            
            currentCamera.PhotoPath = localPath;
            currentCamera.CaptureTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            currentCamera.CaptureMethod = "XR_CAPTURE";
            
            Debug.Log($"[CctvCaptureController] Photo captured: {localPath}");
            
            // Don't trigger event yet - wait for upload
            yield return new WaitForSeconds(0.1f);
        }
        
        IEnumerator UploadPhotoStep()
        {
            currentState = CaptureState.PhotoUploading;
            
            if (photoUploader == null)
            {
                Debug.LogWarning("[CctvCaptureController] No photo uploader configured, using local path only");
                OnPhotoCaptured?.Invoke(currentCamera, currentCamera.PhotoPath);
                yield break;
            }
            
            Debug.Log("[CctvCaptureController] Uploading photo to external storage...");
            
            string externalUrl = null;
            bool uploadComplete = false;
            
            // Start upload
            _ = photoUploader.UploadPhotoAsync(currentCamera.PhotoPath, currentCamera.DeviceName, (url, error) => {
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"[CctvCaptureController] Upload failed: {error}");
                }
                externalUrl = url;
                uploadComplete = true;
            });
            
            // Wait with timeout
            float timeout = 30f;
            float timer = 0f;
            while (!uploadComplete && timer < timeout)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            
            if (!string.IsNullOrEmpty(externalUrl))
            {
                currentCamera.PhotoUrl = externalUrl;
                Debug.Log($"[CctvCaptureController] Photo uploaded: {externalUrl}");
                OnPhotoCaptured?.Invoke(currentCamera, externalUrl);
            }
            else
            {
                Debug.LogWarning("[CctvCaptureController] Using local path (upload failed or disabled)");
                OnPhotoCaptured?.Invoke(currentCamera, currentCamera.PhotoPath);
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        IEnumerator AiAnalysisStep()
        {
            currentState = CaptureState.AiAnalyzing;
            
            if (string.IsNullOrEmpty(currentCamera.PhotoPath))
            {
                Debug.LogWarning("[CctvCaptureController] No photo for AI analysis");
                currentState = CaptureState.SavingData;
                yield break;
            }
            
            Debug.Log("[CctvCaptureController] Starting AI analysis...");
            
            // Trigger AI analysis
            cameraRecognizer?.AnalyzeCameraPhoto(currentCamera.PhotoPath, currentCamera.DeviceId);
            
            // Wait for user to confirm (this will be called from AiCopilotUI)
            yield return new WaitUntil(() => currentState == CaptureState.SavingData);
        }
        
        /// <summary>
        /// Called from AI UI when user accepts a suggestion
        /// </summary>
        public void UserConfirmedCameraType(string cameraType)
        {
            if (currentCamera != null)
            {
                // Only update if user confirms - this is the guard rail!
                if (string.IsNullOrEmpty(currentCamera.DeviceType) || 
                    currentCamera.DeviceType == "Unknown" ||
                    currentCamera.DeviceType != cameraType)
                {
                    Debug.Log($"[CctvCaptureController] User confirmed camera type: {cameraType}");
                    currentCamera.DeviceType = cameraType;
                    currentCamera.CaptureMethod += "+AI_CONFIRMED";
                }
            }
            
            // Move to saving state
            currentState = CaptureState.SavingData;
            OnUserConfirmed?.Invoke(currentCamera);
        }
        
        /// <summary>
        /// Called from AI UI when user skips AI or rejects all suggestions
        /// </summary>
        public void UserSkippedAi()
        {
            Debug.Log("[CctvCaptureController] User skipped AI, proceeding with manual entry");
            currentState = CaptureState.SavingData;
            OnUserConfirmed?.Invoke(currentCamera);
        }
        
        IEnumerator SaveDataStep()
        {
            currentState = CaptureState.SavingData;
            
            if (csvManager == null)
            {
                Debug.LogError("[CctvCaptureController] CSV manager not available");
                OnCaptureError?.Invoke("Cannot save - CSV manager missing");
                yield break;
            }
            
            // Record GPS
            if (currentCamera.Latitude.HasValue && currentCamera.Longitude.HasValue)
            {
                csvManager.RecordGpsCapture(
                    currentCamera.DeviceId,
                    currentCamera.Latitude.Value,
                    currentCamera.Longitude.Value,
                    currentCamera.GpsAccuracy ?? 999f
                );
            }
            
            // Record Photo
            csvManager.RecordPhoto(
                currentCamera.DeviceId,
                currentCamera.PhotoUrl,
                currentCamera.PhotoPath
            );
            
            // Save CSV
            bool saved = csvManager.SaveCsv();
            
            if (saved)
            {
                Debug.Log($"[CctvCaptureController] Data saved for {currentCamera.DeviceName}");
            }
            else
            {
                OnCaptureError?.Invoke("Failed to save CSV");
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        
        /// <summary>
        /// Gets current capture progress for UI display
        /// </summary>
        public string GetProgressText()
        {
            switch (currentState)
            {
                case CaptureState.Idle:
                    return "Ready to capture";
                case CaptureState.GpsCapturing:
                    return "Capturing GPS...";
                case CaptureState.PhotoCapturing:
                    return "Taking photo...";
                case CaptureState.PhotoUploading:
                    return "Uploading photo...";
                case CaptureState.AiAnalyzing:
                    return "AI analyzing...";
                case CaptureState.WaitingForUser:
                    return "Confirm camera type";
                case CaptureState.SavingData:
                    return "Saving data...";
                case CaptureState.Complete:
                    return "Capture complete!";
                case CaptureState.Error:
                    return "Error occurred";
                default:
                    return "Working...";
            }
        }
        
        /// <summary>
        /// Emergency cancel
        /// </summary>
        public void CancelCapture()
        {
            if (isCapturing)
            {
                StopAllCoroutines();
                isCapturing = false;
                currentState = CaptureState.Idle;
                Debug.Log("[CctvCaptureController] Capture cancelled");
            }
        }
        
        void OnDestroy()
        {
            CancelCapture();
        }
    }
}
