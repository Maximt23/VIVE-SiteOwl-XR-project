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
        
        private DeviceData device;
        private Action<DeviceData> onSelectCallback;
        
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
        
        /// <summary>
        /// Sets up the list item with device data.
        /// </summary>
        public void Setup(DeviceData deviceData, Action<DeviceData> onSelect)
        {
            device = deviceData;
            onSelectCallback = onSelect;
            
            if (DeviceNameText != null)
            {
                DeviceNameText.text = device.DeviceName;
            }
            
            if (DeviceTypeText != null)
            {
                DeviceTypeText.text = $"{device.DeviceType} | {device.SystemType}";
            }
            
            if (StatusText != null)
            {
                if (device.NeedsCapture)
                {
                    StatusText.text = "<color=red>NEEDS CAPTURE</color>";
                    StatusText.color = Color.red;
                }
                else if (device.RequiresReview)
                {
                    StatusText.text = "<color=yellow>REVIEW REQUIRED</color>";
                    StatusText.color = new Color(1f, 0.7f, 0f);
                }
                else
                {
                    StatusText.text = $"<color=green>✓</color> ({device.CoordinateConfidence})";
                    StatusText.color = Color.green;
                }
            }
            
            // Set background color
            if (BackgroundImage != null)
            {
                if (device.RequiresReview)
                {
                    BackgroundImage.color = ReviewRequiredColor;
                }
                else if (device.NeedsCapture)
                {
                    BackgroundImage.color = NeedsCaptureColor;
                }
                else
                {
                    BackgroundImage.color = CapturedColor;
                }
            }
        }
        
        void OnSelectClicked()
        {
            onSelectCallback?.Invoke(device);
        }
    }
}
