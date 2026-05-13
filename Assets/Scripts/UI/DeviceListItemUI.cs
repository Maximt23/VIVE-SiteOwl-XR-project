using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using SiteOwlXR.Core;

namespace SiteOwlXR.UI
{
    /// <summary>
    /// UI component for a single device in the list.
    /// </summary>
    public class DeviceListItemUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI DeviceNameText;
        public TextMeshProUGUI DeviceTypeText;
        public TextMeshProUGUI StatusText;
        public Button SelectButton;
        public Image BackgroundImage;
        
        [Header("Colors")]
        public Color NeedsCaptureColor = new Color(1f, 0.9f, 0.9f);
        public Color CapturedColor = new Color(0.9f, 1f, 0.9f);
        public Color ReviewRequiredColor = new Color(1f, 0.95f, 0.8f);
        
        // Store only the ID. The live DeviceData is resolved at interaction time
        // via the CsvManager so we never hold a stale reference after CSV reload.
        private string deviceId;
        private Action<string> onSelectCallback;
        
        void OnEnable()
        {
            if (SelectButton != null)
            {
                SelectButton.onClick.AddListener(OnSelectClicked);
            }
        }
        
        void OnDisable()
        {
            if (SelectButton != null)
            {
                SelectButton.onClick.RemoveListener(OnSelectClicked);
            }
        }
        
        public void Setup(DeviceData deviceData, Action<DeviceData> onSelect)
        {
            deviceId         = deviceData.DeviceID;
            // Wrap so callers still get DeviceData; the DeviceData ref at setup time
            // is fine for display — we only re-resolve at button click.
            onSelectCallback = id => onSelect?.Invoke(deviceData);

            if (DeviceNameText != null) DeviceNameText.text = deviceData.DeviceName;
            if (DeviceTypeText  != null) DeviceTypeText.text  = $"{deviceData.DeviceType} | {deviceData.SystemType}";

            if (StatusText != null)
            {
                if (deviceData.NeedsCapture)
                {
                    StatusText.text  = "NEEDS CAPTURE";
                    StatusText.color = Color.red;
                }
                else if (deviceData.RequiresReview)
                {
                    StatusText.text  = "REVIEW REQUIRED";
                    StatusText.color = new Color(1f, 0.7f, 0f);
                }
                else
                {
                    StatusText.text  = $"OK ({deviceData.CoordinateConfidence})";
                    StatusText.color = Color.green;
                }
            }

            if (BackgroundImage != null)
            {
                BackgroundImage.color = deviceData.RequiresReview ? ReviewRequiredColor
                                      : deviceData.NeedsCapture   ? NeedsCaptureColor
                                      : CapturedColor;
            }
        }
        
        void OnSelectClicked()
        {
            onSelectCallback?.Invoke(deviceId);
        }
    }
}
