using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SiteOwlXR.Models;

namespace SiteOwlXR.UI
{
    /// <summary>
    /// Camera List Item UI - Individual row in camera list
    /// </summary>
    public class CameraListItem : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI TypeText;
        public TextMeshProUGUI StatusText;
        public Image StatusIcon;
        public Button SelectButton;
        
        [Header("Status Colors")]
        public Color CapturedColor = Color.green;
        public Color PendingColor = Color.yellow;
        public Color ReviewColor = Color.red;
        
        private DeviceRecord _device;
        private System.Action<DeviceRecord> _onSelect;
        
        void OnEnable()
        {
            if (SelectButton != null)
            {
                SelectButton.onClick.AddListener(OnClick);
            }
        }
        
        void OnDisable()
        {
            if (SelectButton != null)
            {
                SelectButton.onClick.RemoveListener(OnClick);
            }
        }
        
        public void Setup(DeviceRecord device, System.Action<DeviceRecord> onSelect)
        {
            _device = device;
            _onSelect = onSelect;
            
            // Set text
            if (NameText != null)
            {
                NameText.text = device.DeviceName;
            }
            
            if (TypeText != null)
            {
                TypeText.text = $"{device.DeviceType} | {device.IpAddress}";
            }
            
            // Set status
            UpdateStatus();
        }
        
        void UpdateStatus()
        {
            if (_device == null) return;
            
            if (_device.IsCaptured)
            {
                if (_device.ReviewStatus == ReviewStatus.REVIEW_REQUIRED)
                {
                    StatusText.text = "REVIEW";
                    StatusIcon.color = ReviewColor;
                }
                else
                {
                    StatusText.text = "CAPTURED";
                    StatusIcon.color = CapturedColor;
                }
            }
            else
            {
                StatusText.text = "PENDING";
                StatusIcon.color = PendingColor;
            }
        }
        
        void OnClick()
        {
            _onSelect?.Invoke(_device);
        }
    }
}
