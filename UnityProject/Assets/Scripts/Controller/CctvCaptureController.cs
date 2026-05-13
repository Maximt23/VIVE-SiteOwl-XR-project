using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using SiteOwlXR.Models;
using SiteOwlXR.Events;
using SiteOwlXR.Managers;
using SiteOwlXR.Uploaders;
using SiteOwlXR.AI;
using SiteOwlXR.Services;

namespace SiteOwlXR.Controller
{
    /// <summary>
    /// CCTV Capture Controller - THE ORCHESTRATOR (Enterprise Grade)
    /// Manages complete capture workflow with state machine and error recovery
    /// </summary>
    public class CctvCaptureController : MonoBehaviour
    {
        public enum CaptureState
        {
            Idle,
            WaitingForCalibration,
            DeviceSelected,
            CapturingGps,
            CapturingXy,
            CapturingPhoto,
            UploadingPhoto,
            RunningAi,
            WaitingForAiConfirm,
            SavingToCsv,
            Complete,
            Error
        }
        
        [Header("State")]
        public CaptureState CurrentState { get; private set; } = CaptureState.Idle;
        public DeviceRecord CurrentDevice { get; private set; }
        public bool IsCapturing => CurrentState != CaptureState.Idle && CurrentState != CaptureState.Complete && CurrentState != CaptureState.Error;
        
        [Header("Components (Auto-Find)")]
        public XrInitializer XrInitializer;
        public RealGpsManager GpsManager;
        public CalibrationManager CalibrationManager;
        public SiteOwlCsvManager CsvManager;
        public PhotoCapture PhotoCapture;
        public ExternalPhotoUploader PhotoUploader;
        public CctvCameraRecognizer AiRecognizer;
        public Transform XrCameraTransform;
        public XRRayInteractor RayInteractor;
        
        [Header("UI References")]
        public TextMeshProUGUI StatusText;
        public TextMeshProUGUI GpsText;
        public TextMeshProUGUI ProgressText;
        public GameObject CapturePanel;
        public Button CaptureButton;
        
        private ILogger _logger;
        private LineRenderer _pointerLine;
        private GpsReading _currentGps;
        private Vector2 _currentSiteOwlXY;
        private string _currentPhotoPath;
        private string _currentPhotoUrl;
        private List<AiSuggestion> _currentAiSuggestions;
        
        void Awake()
        {
            _logger = ServiceLocator.Get<ILogger>() ?? new UnityLogger();
            FindComponents();
            SetupPointer();
        }
        
        void Start()
        {
            if (CaptureButton != null)
            {
                CaptureButton.onClick.AddListener(OnCaptureButtonClicked);
            }
            
            UpdateStatus("Ready. Select a camera to capture.");
        }
        
        void OnDestroy()
        {
            if (CaptureButton != null)
            {
                CaptureButton.onClick.RemoveListener(OnCaptureButtonClicked);
            }
        }
        
        void FindComponents()
        {
            XrInitializer = XrInitializer ?? FindObjectOfType<XrInitializer>();
            GpsManager = GpsManager ?? FindObjectOfType<RealGpsManager>();
            CalibrationManager = CalibrationManager ?? FindObjectOfType<CalibrationManager>();
            CsvManager = CsvManager ?? FindObjectOfType<SiteOwlCsvManager>();
            PhotoCapture = PhotoCapture ?? FindObjectOfType<PhotoCapture>();
            PhotoUploader = PhotoUploader ?? FindObjectOfType<ExternalPhotoUploader>();
            AiRecognizer = AiRecognizer ?? FindObjectOfType<CctvCameraRecognizer>();
            XrCameraTransform = XrCameraTransform ?? Camera.main?.transform;
            
            _logger.LogInfo("CctvCaptureController components found", this);
        }
        
        void SetupPointer()
        {
            _pointerLine = gameObject.AddComponent<LineRenderer>();
            _pointerLine.positionCount = 2;
            _pointerLine.startWidth = 0.01f;
            _pointerLine.endWidth = 0.005f;
            _pointerLine.material = new Material(Shader.Find("Sprites/Default"));
            _pointerLine.startColor = Color.cyan;
            _pointerLine.endColor = Color.cyan;
        }
        
        void Update()
        {
            UpdatePointer();
        }
        
        void UpdatePointer()
        {
            if (XrCameraTransform == null || _pointerLine == null) return;
            
            Vector3 start = XrCameraTransform.position;
            Vector3 end = start + XrCameraTransform.forward * 10f;
            
            _pointerLine.SetPosition(0, start);
            _pointerLine.SetPosition(1, end);
        }
        
        public void SelectDevice(DeviceRecord device)
        {
            CurrentDevice = device;
            CurrentState = CaptureState.DeviceSelected;
            
            UpdateStatus($"Selected: {device.DeviceName}");
            UpdateGpsText();
            
            if (CapturePanel != null)
            {
                CapturePanel.SetActive(true);
            }
            
            CctvSurveyEvents.RaiseDeviceSelected(device);
        }
        
        void OnCaptureButtonClicked()
        {
            if (CurrentDevice == null)
            {
                UpdateStatus("Error: No device selected");
                return;
            }
            
            if (IsCapturing)
            {
                UpdateStatus("Already capturing...");
                return;
            }
            
            StartCoroutine(CaptureWorkflow());
        }
        
        IEnumerator CaptureWorkflow()
        {
            CurrentState = CaptureState.CapturingGps;
            CctvSurveyEvents.RaiseCaptureStarted(CurrentDevice);
            
            // Step 1: GPS
            yield return CaptureGpsStep();
            if (CurrentState == CaptureState.Error) yield break;
            
            // Step 2: SiteOwl X/Y from raycast
            yield return CaptureXyStep();
            if (CurrentState == CaptureState.Error) yield break;
            
            // Step 3: Photo
            yield return CapturePhotoStep();
            if (CurrentState == CaptureState.Error) yield break;
            
            // Step 4: Upload
            yield return UploadPhotoStep();
            if (CurrentState == CaptureState.Error) yield break;
            
            // Step 5: AI (optional)
            if (AiRecognizer != null)
            {
                yield return RunAiStep();
            }
            
            // Step 6: Save
            yield return SaveToCsvStep();
            if (CurrentState == CaptureState.Error) yield break;
            
            // Complete
            CurrentState = CaptureState.Complete;
            UpdateStatus($"Complete: {CurrentDevice.DeviceName} captured!");
            CctvSurveyEvents.RaiseCaptureComplete(CurrentDevice);
            
            yield return new WaitForSeconds(2f);
            CurrentState = CaptureState.Idle;
        }
        
        IEnumerator CaptureGpsStep()
        {
            UpdateProgress("Capturing GPS...");
            
            if (!GpsManager.IsReady)
            {
                yield return new WaitForSeconds(0.5f);
                
                if (!GpsManager.IsReady)
                {
                    HandleError("GPS not available");
                    yield break;
                }
            }
            
            _currentGps = GpsManager.LastReading;
            
            UpdateGpsText();
            CctvSurveyEvents.RaiseGpsCaptured(CurrentDevice, _currentGps);
            
            yield return new WaitForSeconds(0.1f);
        }
        
        IEnumerator CaptureXyStep()
        {
            UpdateProgress("Capturing position...");
            
            if (!CalibrationManager.IsCalibrated)
            {
                HandleError("System not calibrated");
                yield break;
            }
            
            // Raycast from camera
            Ray ray = new Ray(XrCameraTransform.position, XrCameraTransform.forward);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                _currentSiteOwlXY = CalibrationManager.WorldToSiteOwl(hit.point);
                float confidence = (float)CalibrationManager.GetConfidence(hit.point);
                
                CctvSurveyEvents.RaiseXyCaptured(CurrentDevice, _currentSiteOwlXY, confidence);
            }
            else
            {
                // No hit - use max distance
                Vector3 farPoint = ray.GetPoint(50f);
                _currentSiteOwlXY = CalibrationManager.WorldToSiteOwl(farPoint);
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        IEnumerator CapturePhotoStep()
        {
            UpdateProgress("Capturing photo...");
            
            string photoPath = null;
            bool captureComplete = false;
            string errorMsg = null;
            
            StartCoroutine(PhotoCapture.TakePhoto(
                CurrentDevice.DeviceName,
                CurrentDevice.Site,
                path => { photoPath = path; captureComplete = true; },
                error => { errorMsg = error; captureComplete = true; }
            ));
            
            yield return new WaitUntil(() => captureComplete);
            
            if (!string.IsNullOrEmpty(errorMsg))
            {
                HandleError($"Photo capture failed: {errorMsg}");
                yield break;
            }
            
            _currentPhotoPath = photoPath;
            CctvSurveyEvents.RaisePhotoCaptured(CurrentDevice, photoPath);
            
            yield return new WaitForSeconds(0.1f);
        }
        
        IEnumerator UploadPhotoStep()
        {
            UpdateProgress("Uploading photo...");
            
            var uploadTask = PhotoUploader.UploadAsync(_currentPhotoPath, CurrentDevice.DeviceName, CurrentDevice.Site);
            
            while (!uploadTask.IsCompleted)
            {
                yield return null;
            }
            
            var result = uploadTask.Result;
            
            if (!result.Success)
            {
                HandleError($"Upload failed: {result.ErrorMessage}");
                yield break;
            }
            
            _currentPhotoUrl = result.RemoteUrl;
            CctvSurveyEvents.RaisePhotoUploaded(CurrentDevice, result.RemoteUrl);
            
            yield return new WaitForSeconds(0.1f);
        }
        
        IEnumerator RunAiStep()
        {
            UpdateProgress("Analyzing photo...");
            
            // TODO: Implement AI analysis
            // For now, skip
            
            yield return new WaitForSeconds(0.5f);
        }
        
        IEnumerator SaveToCsvStep()
        {
            UpdateProgress("Saving data...");
            
            // Update device record
            CurrentDevice.Barcode = _currentGps.ToBarcodeFormat();
            CurrentDevice.SiteOwlX = _currentSiteOwlXY.x;
            CurrentDevice.SiteOwlY = _currentSiteOwlXY.y;
            CurrentDevice.PhotoPath = _currentPhotoPath;
            CurrentDevice.PhotoUrl = _currentPhotoUrl;
            CurrentDevice.CaptureTimestamp = DateTime.UtcNow;
            CurrentDevice.CaptureMethod = "XR_CAPTURE";
            CurrentDevice.Confidence = _currentGps.GetConfidence();
            CurrentDevice.ReviewStatus = _currentGps.GetConfidence() == CoordinateConfidence.LOW ? 
                ReviewStatus.REVIEW_REQUIRED : ReviewStatus.OK;
            
            CsvManager.UpdateDevice(CurrentDevice);
            
            yield return new WaitForSeconds(0.5f);
        }
        
        void HandleError(string message)
        {
            CurrentState = CaptureState.Error;
            _logger.LogError(message, null, this);
            UpdateStatus($"Error: {message}");
            CctvSurveyEvents.RaiseCaptureError(CurrentDevice, message);
        }
        
        void UpdateStatus(string message)
        {
            if (StatusText != null)
            {
                StatusText.text = message;
            }
            _logger.LogInfo(message, this);
        }
        
        void UpdateGpsText()
        {
            if (GpsText != null && GpsManager.IsReady)
            {
                GpsText.text = GpsManager.LastReading.ToString();
            }
        }
        
        void UpdateProgress(string step)
        {
            if (ProgressText != null)
            {
                ProgressText.text = step;
            }
            CctvSurveyEvents.RaiseCaptureProgress(CurrentDevice, step, 0f);
        }
    }
}
