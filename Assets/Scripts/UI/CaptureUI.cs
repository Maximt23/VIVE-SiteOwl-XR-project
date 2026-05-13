using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SiteOwlXR.Core;

namespace SiteOwlXR.UI
{
    /// <summary>
    /// UI controller for the capture workflow.
    /// Handles device list, capture feedback, and status displays.
    /// </summary>
    public class CaptureUI : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject CalibrationPanel;
        public GameObject DeviceListPanel;
        public GameObject CapturePanel;
        public GameObject StatusPanel;
        
        [Header("Calibration UI")]
        public TMP_InputField CalibrationXInput;
        public TMP_InputField CalibrationYInput;
        public Button CalibrateButton;
        public TextMeshProUGUI CalibrationStatusText;
        
        [Header("Device List")]
        public Transform DeviceListContainer;
        public GameObject DeviceListItemPrefab;
        public TMP_InputField SearchInput;
        public Button SearchButton;
        public Button ShowMissingButton;
        public TextMeshProUGUI DeviceCountText;
        
        [Header("Capture UI")]
        public TextMeshProUGUI SelectedDeviceName;
        public TextMeshProUGUI SelectedDeviceInfo;
        public Button CaptureButton;
        public Button CancelButton;
        public GameObject CapturingIndicator;
        
        [Header("Status")]
        public TextMeshProUGUI StatusText;
        public TextMeshProUGUI PositionText;
        public TextMeshProUGUI ConfidenceText;
        
        [Header("Colors")]
        public Color HighConfidenceColor = Color.green;
        public Color MediumConfidenceColor = Color.yellow;
        public Color LowConfidenceColor = Color.red;
        public Color NoConfidenceColor = Color.gray;
        
        private CalibrationManager calibration;
        private CsvManager csvManager;
        private CaptureController captureController;
        
        void Start()
        {
            calibration = FindObjectOfType<CalibrationManager>();
            csvManager = FindObjectOfType<CsvManager>();
            captureController = FindObjectOfType<CaptureController>();
            
            SetupEventListeners();
            ShowCalibrationPanel();
        }
        
        void SetupEventListeners()
        {
            if (CalibrateButton != null)
            {
                CalibrateButton.onClick.AddListener(OnCalibrateClicked);
            }
            
            if (SearchButton != null)
            {
                SearchButton.onClick.AddListener(OnSearchClicked);
            }
            
            if (ShowMissingButton != null)
            {
                ShowMissingButton.onClick.AddListener(OnShowMissingClicked);
            }
            
            if (CaptureButton != null)
            {
                CaptureButton.onClick.AddListener(OnCaptureClicked);
            }
            
            if (CancelButton != null)
            {
                CancelButton.onClick.AddListener(OnCancelClicked);
            }
            
            if (calibration != null)
            {
                calibration.OnCalibrationComplete += OnCalibrationComplete;
            }
            
            if (csvManager != null)
            {
                csvManager.OnDataLoaded += OnDataLoaded;
            }
            
            if (captureController != null)
            {
                captureController.OnCaptureComplete += OnDeviceCaptured;
                captureController.OnCaptureError += OnCaptureError;
            }
        }
        
        void Update()
        {
            // Update position display
            if (calibration != null && calibration.IsCalibrated)
            {
                Vector2 currentPos = calibration.GetCurrentSiteOwlPosition();
                PositionText.text = $"Position: ({currentPos.x:F2}, {currentPos.y:F2})";
                
                var confidence = calibration.CalculateConfidence();
                ConfidenceText.text = $"Confidence: {confidence}";
                ConfidenceText.color = GetConfidenceColor(confidence);
            }
        }
        
        #region Panel Management
        
        public void ShowCalibrationPanel()
        {
            HideAllPanels();
            CalibrationPanel?.SetActive(true);
            
            if (calibration != null && calibration.IsCalibrated)
            {
                CalibrationStatusText.text = "<color=green>✓ Calibrated</color>";
                CalibrateButton.interactable = false;
            }
            else
            {
                CalibrationStatusText.text = "<color=red>✗ Not Calibrated</color>";
                CalibrateButton.interactable = true;
            }
        }
        
        public void ShowDeviceListPanel()
        {
            HideAllPanels();
            DeviceListPanel?.SetActive(true);
            RefreshDeviceList();
        }
        
        public void ShowCapturePanel()
        {
            HideAllPanels();
            CapturePanel?.SetActive(true);
        }
        
        void HideAllPanels()
        {
            CalibrationPanel?.SetActive(false);
            DeviceListPanel?.SetActive(false);
            CapturePanel?.SetActive(false);
        }
        
        #endregion
        
        #region Calibration
        
        void OnCalibrateClicked()
        {
            if (float.TryParse(CalibrationXInput.text, out float x) &&
                float.TryParse(CalibrationYInput.text, out float y))
            {
                calibration?.SetCalibrationAnchor(x, y);
            }
            else
            {
                ShowStatus("Invalid coordinates!", true);
            }
        }
        
        void OnCalibrationComplete()
        {
            CalibrationStatusText.text = "<color=green>✓ Calibrated</color>";
            CalibrateButton.interactable = false;
            ShowStatus("Calibration complete!", false);
            
            // Auto-advance to device list
            ShowDeviceListPanel();
        }
        
        #endregion
        
        #region Device List
        
        void OnDataLoaded()
        {
            RefreshDeviceList();
        }
        
        void RefreshDeviceList()
        {
            // Clear existing
            foreach (Transform child in DeviceListContainer)
            {
                Destroy(child.gameObject);
            }
            
            if (csvManager == null) return;
            
            // Update count
            DeviceCountText.text = $"{csvManager.CapturedCount}/{csvManager.TotalCount} captured, " +
                $"{csvManager.RemainingCount} remaining";
            
            // Get devices to show
            var devices = string.IsNullOrEmpty(SearchInput?.text) 
                ? csvManager.Devices 
                : csvManager.SearchDevices(SearchInput.text);
            
            // Create list items
            foreach (var device in devices)
            {
                if (DeviceListItemPrefab != null)
                {
                    var item = Instantiate(DeviceListItemPrefab, DeviceListContainer);
                    var itemUI = item.GetComponent<DeviceListItemUI>();
                    if (itemUI != null)
                    {
                        itemUI.Setup(device, OnDeviceSelected);
                    }
                }
            }
        }
        
        void OnSearchClicked()
        {
            RefreshDeviceList();
        }
        
        void OnShowMissingClicked()
        {
            // Clear search and show only missing
            if (SearchInput != null)
            {
                SearchInput.text = "";
            }
            
            foreach (Transform child in DeviceListContainer)
            {
                Destroy(child.gameObject);
            }
            
            var missing = csvManager.GetMissingDevices();
            foreach (var device in missing)
            {
                if (DeviceListItemPrefab != null)
                {
                    var item = Instantiate(DeviceListItemPrefab, DeviceListContainer);
                    var itemUI = item.GetComponent<DeviceListItemUI>();
                    if (itemUI != null)
                    {
                        itemUI.Setup(device, OnDeviceSelected);
                    }
                }
            }
            
            DeviceCountText.text = $"Showing {missing.Count} missing devices";
        }
        
        void OnDeviceSelected(DeviceData device)
        {
            captureController?.SelectDevice(device);
            ShowCapturePanel();
        }
        
        #endregion
        
        #region Capture
        
        public void ShowDeviceInfo(DeviceData device)
        {
            SelectedDeviceName.text = device.DeviceName;
            SelectedDeviceInfo.text = $"Type: {device.DeviceType}\n" +
                $"System: {device.SystemType}\n" +
                $"ID: {device.DeviceID}\n" +
                $"{(device.NeedsCapture ? "<color=red>NEEDS CAPTURE</color>" : "<color=green>CAPTURED</color>")}";
        }
        
        public void ShowCapturingState(bool capturing)
        {
            CaptureButton.interactable = !capturing;
            CapturingIndicator?.SetActive(capturing);
        }
        
        public void ShowCaptureSuccess(DeviceData device)
        {
            ShowStatus($"Captured {device.DeviceName}!", false);
            
            if (device.RequiresReview)
            {
                ShowStatus($"<color=yellow>{device.DeviceName} marked for review</color>", false);
            }
        }
        
        void OnCaptureClicked()
        {
            captureController?.CaptureCurrentDevice();
        }
        
        void OnCancelClicked()
        {
            ShowDeviceListPanel();
        }
        
        void OnDeviceCaptured(DeviceData device)
        {
            // Device was successfully captured
            ShowCaptureSuccess(device);
        }
        
        void OnCaptureError(string error)
        {
            ShowStatus($"<color=red>Error: {error}</color>", true);
        }
        
        #endregion
        
        #region Status
        
        public void ShowStatus(string message, bool isError)
        {
            if (StatusText != null)
            {
                StatusText.text = message;
                
                // Flash effect
                if (isError)
                {
                    StatusText.color = Color.red;
                }
            }
            
            Debug.Log($"[CaptureUI] Status: {message}");
        }
        
        Color GetConfidenceColor(CoordinateConfidence confidence)
        {
            return confidence switch
            {
                CoordinateConfidence.HIGH => HighConfidenceColor,
                CoordinateConfidence.MEDIUM => MediumConfidenceColor,
                CoordinateConfidence.LOW => LowConfidenceColor,
                _ => NoConfidenceColor,
            };
        }
        
        #endregion
        
        void OnDestroy()
        {
            if (calibration != null)
            {
                calibration.OnCalibrationComplete -= OnCalibrationComplete;
            }
            
            if (csvManager != null)
            {
                csvManager.OnDataLoaded -= OnDataLoaded;
            }
            
            if (captureController != null)
            {
                captureController.OnCaptureComplete -= OnDeviceCaptured;
                captureController.OnCaptureError -= OnCaptureError;
            }
        }
    }
}
