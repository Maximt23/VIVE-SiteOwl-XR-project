using UnityEngine;
using System;
using System.Collections;

namespace SiteOwlXR.Core
{
    /// <summary>
    /// Master orchestrator for the XR capture workflow.
    ///
    ///   Raycast → coord transform → GPS → photo → [AI] → CSV write
    ///
    /// Controller trigger fires capture when aim is valid.
    /// Overwrite confirmation shown if device already has valid coordinates.
    /// </summary>
    public class CaptureController : MonoBehaviour
    {
        [Header("Dependencies — assign in Inspector")]
        public CalibrationManager Calibration;
        public CsvManager         CsvManager;
        public PhotoCapture       PhotoCapture;
        public RealGpsManager     GpsManager;      // optional; null = no GPS
        public Transform          CameraTransform;

        [Header("Raycast")]
        public float     MaxRaycastDistance = 50f;
        public LayerMask RaycastLayers      = ~0;

        [Header("UI")]
        public CaptureUI CaptureUI;

        [Header("Timing")]
        [Tooltip("Seconds before photo callback is considered timed-out.")]
        public float PhotoTimeoutSeconds = 15f;

        [Header("XR Input")]
        [Tooltip("KeyCode that fires capture (mapped to controller trigger in XR).")]
        public KeyCode TriggerKey = KeyCode.JoystickButton14; // right trigger on most XR runtimes

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<DeviceData> OnCaptureComplete;
        public event Action<string>     OnCaptureError;

        // ── State ─────────────────────────────────────────────────────────────
        private DeviceData _currentDevice;
        private bool       _isCapturing;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        void Start()
        {
            if (CameraTransform == null)
                CameraTransform = Camera.main?.transform;

            ValidateDeps();

            if (PhotoCapture != null)
            {
                PhotoCapture.OnPhotoSaved   += OnPhotoSaved;
                PhotoCapture.OnCaptureError += OnPhotoCaptureError;
                PhotoCapture.Initialize();
            }
        }

        private void ValidateDeps()
        {
            if (Calibration  == null) Debug.LogError("[CaptureController] CalibrationManager not assigned!");
            if (CsvManager   == null) Debug.LogError("[CaptureController] CsvManager not assigned!");
            if (PhotoCapture == null) Debug.LogError("[CaptureController] PhotoCapture not assigned!");
            if (GpsManager   == null) Debug.LogWarning("[CaptureController] GpsManager not assigned — GPS coords will be null.");
        }

        void Update()
        {
            // XR controller trigger fires capture
            if (Input.GetKeyDown(TriggerKey) && !_isCapturing)
                CaptureCurrentDevice();

#if UNITY_EDITOR
            if (Calibration != null && Calibration.IsCalibrated && CameraTransform != null)
                Debug.DrawRay(CameraTransform.position, CameraTransform.forward * MaxRaycastDistance, Color.cyan);
#endif
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SelectDevice(DeviceData device)
        {
            _currentDevice = device;
            CaptureUI?.ShowDeviceInfo(device);
            Debug.Log($"[CaptureController] Selected: {device.DeviceName}");
        }

        /// <summary>
        /// Called by AiCopilotUI after user accepts/skips an AI suggestion.
        /// </summary>
        public void ProceedWithCapture()
        {
            if (_currentDevice == null) { OnCaptureError?.Invoke("No device selected"); return; }
            CaptureCurrentDevice();
        }

        public void CaptureCurrentDevice()
        {
            if (_isCapturing)
            {
                Debug.LogWarning("[CaptureController] Already capturing.");
                return;
            }
            if (_currentDevice == null)  { OnCaptureError?.Invoke("No device selected");   return; }
            if (!Calibration.IsCalibrated){ OnCaptureError?.Invoke("System not calibrated"); return; }

            // Overwrite guard — ask user before clobbering valid coordinates
            if (!_currentDevice.NeedsCapture)
            {
                CaptureUI?.ShowOverwriteConfirmation(_currentDevice, ConfirmOverwrite);
                return;
            }

            StartCapture();
        }

        private void ConfirmOverwrite() => StartCapture();

        // ── Capture pipeline ──────────────────────────────────────────────────

        private void StartCapture()
        {
            _isCapturing = true;
            CaptureUI?.ShowCapturingState(true);

            if (!GetCapturePoint(out Vector3 worldPoint, out _))
            {
                Fail("Failed to get capture point"); return;
            }

            Vector2 siteOwlXY   = Calibration.WorldToSiteOwl(worldPoint);
            Vector2 userXY      = Calibration.GetCurrentSiteOwlPosition();
            float   userFacing  = Calibration.GetCurrentFacingDirection();
            var     confidence  = Calibration.CalculateConfidence();

            GetGps(out double? lat, out double? lon);

            StartCoroutine(CaptureWithPhoto(
                _currentDevice, siteOwlXY, lat, lon, userXY, userFacing, confidence));
        }

        private IEnumerator CaptureWithPhoto(
            DeviceData device, Vector2 siteOwlXY,
            double? lat, double? lon,
            Vector2 userXY, float userFacing,
            CoordinateConfidence confidence)
        {
            string photoPath    = null;
            bool   photoReady   = false;

            PhotoCapture.TakePhoto(device.DeviceID, path =>
            {
                photoPath  = path;
                photoReady = true;
            });

            float deadline = Time.time + PhotoTimeoutSeconds;
            yield return new WaitUntil(() => photoReady || Time.time > deadline);

            if (!photoReady || string.IsNullOrEmpty(photoPath))
            {
                Fail(photoReady ? "Photo save failed" : "Photo capture timed out");
                yield break;
            }

            device.RecordCapture(
                siteOwlXY.x, siteOwlXY.y,
                lat, lon,
                photoPath, confidence,
                userXY.x, userXY.y, userFacing);

            if (CsvManager.UpdateDevice(device))
            {
                Debug.Log($"[CaptureController] Captured {device.DeviceName} " +
                          $"({siteOwlXY.x:F2},{siteOwlXY.y:F2}) conf={confidence}");
                OnCaptureComplete?.Invoke(device);
                CaptureUI?.ShowCaptureSuccess(device);
            }
            else
            {
                Fail("Failed to save to CSV");
                yield break;
            }

            _isCapturing = false;
            CaptureUI?.ShowCapturingState(false);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        public bool GetCapturePoint(out Vector3 worldPoint, out float distance)
        {
            worldPoint = Vector3.zero; distance = 0f;
            if (CameraTransform == null) return false;

            var ray = new Ray(CameraTransform.position, CameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, MaxRaycastDistance, RaycastLayers))
            {
                worldPoint = hit.point;
                distance   = hit.distance;
            }
            else
            {
                worldPoint = ray.GetPoint(MaxRaycastDistance);
                distance   = MaxRaycastDistance;
            }
            return true;
        }

        private void GetGps(out double? lat, out double? lon)
        {
            lat = null; lon = null;
            if (GpsManager == null || !GpsManager.IsGpsReady) return;
            var (la, lo, _, _) = GpsManager.CaptureGps();
            lat = la; lon = lo;
        }

        private void Fail(string msg)
        {
            Debug.LogError($"[CaptureController] {msg}");
            OnCaptureError?.Invoke(msg);
            _isCapturing = false;
            CaptureUI?.ShowCapturingState(false);
        }

        private void OnPhotoSaved(string _)   { /* handled in coroutine */ }
        private void OnPhotoCaptureError(string _) { /* handled in coroutine */ }

        void OnDestroy()
        {
            if (PhotoCapture != null)
            {
                PhotoCapture.OnPhotoSaved   -= OnPhotoSaved;
                PhotoCapture.OnCaptureError -= OnPhotoCaptureError;
            }
        }
    }
}
