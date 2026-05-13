using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using SiteOwlXR.Core;

namespace SiteOwlXR.UI
{
    /// <summary>
    /// UI component for a single row in the device list.
    /// </summary>
    public class DeviceListItemUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI DeviceNameText;
        public TextMeshProUGUI DeviceTypeText;
        public TextMeshProUGUI StatusText;
        public Button          SelectButton;
        public Image           BackgroundImage;

        [Header("Colors")]
        public Color NeedsCaptureColor  = new Color(1f, 0.9f, 0.9f);
        public Color CapturedColor      = new Color(0.9f, 1f, 0.9f);
        public Color ReviewRequiredColor = new Color(1f, 0.95f, 0.8f);

        // Stored at Setup time — valid until the list refreshes (which it does after every capture)
        private DeviceData          _device;
        private Action<DeviceData>  _onSelect;

        void OnEnable()  => SelectButton?.onClick.AddListener(OnSelectClicked);
        void OnDisable() => SelectButton?.onClick.RemoveListener(OnSelectClicked);

        public void Setup(DeviceData device, Action<DeviceData> onSelect)
        {
            _device   = device;
            _onSelect = onSelect;

            if (DeviceNameText != null) DeviceNameText.text = device.DeviceName;
            if (DeviceTypeText  != null) DeviceTypeText.text  = $"{device.DeviceType} | {device.SystemType}";

            if (StatusText != null)
            {
                if (device.NeedsCapture)
                {
                    StatusText.text  = "NEEDS CAPTURE";
                    StatusText.color = Color.red;
                }
                else if (device.RequiresReview)
                {
                    StatusText.text  = "REVIEW REQUIRED";
                    StatusText.color = new Color(1f, 0.7f, 0f);
                }
                else
                {
                    StatusText.text  = $"OK ({device.CoordinateConfidence})";
                    StatusText.color = Color.green;
                }
            }

            if (BackgroundImage != null)
                BackgroundImage.color = device.RequiresReview ? ReviewRequiredColor
                                      : device.NeedsCapture   ? NeedsCaptureColor
                                      : CapturedColor;
        }

        private void OnSelectClicked() => _onSelect?.Invoke(_device);
    }
}
