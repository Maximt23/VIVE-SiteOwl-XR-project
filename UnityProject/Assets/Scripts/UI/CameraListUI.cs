using System.Collections.Generic;
using UnityEngine;
using TMPro;
using SiteOwlXR.Models;
using SiteOwlXR.Events;
using SiteOwlXR.Managers;
using SiteOwlXR.Controller;

namespace SiteOwlXR.UI
{
    /// <summary>
    /// Camera List UI - Enterprise Grade
    /// Displays and manages camera selection list
    /// </summary>
    public class CameraListUI : MonoBehaviour
    {
        [Header("UI References")]
        public Transform ListContainer;
        public GameObject CameraListItemPrefab;
        public TextMeshProUGUI CountText;
        public TMP_InputField SearchField;
        
        [Header("Colors")]
        public Color CapturedColor = new Color(0.2f, 0.8f, 0.2f);
        public Color PendingColor = new Color(1f, 0.8f, 0.2f);
        public Color ErrorColor = new Color(1f, 0.3f, 0.2f);
        
        private SiteOwlCsvManager _csvManager;
        private CctvCaptureController _controller;
        private List<CameraListItem> _listItems = new List<CameraListItem>();
        private string _currentSearch = "";
        
        void Start()
        {
            _csvManager = FindObjectOfType<SiteOwlCsvManager>();
            _controller = FindObjectOfType<CctvCaptureController>();
            
            CctvSurveyEvents.OnDevicesLoaded += OnDevicesLoaded;
            CctvSurveyEvents.OnDeviceUpdated += OnDeviceUpdated;
            CctvSurveyEvents.OnCaptureComplete += OnCaptureComplete;
            
            if (SearchField != null)
            {
                SearchField.onValueChanged.AddListener(OnSearchChanged);
            }
        }
        
        void OnDestroy()
        {
            CctvSurveyEvents.OnDevicesLoaded -= OnDevicesLoaded;
            CctvSurveyEvents.OnDeviceUpdated -= OnDeviceUpdated;
            CctvSurveyEvents.OnCaptureComplete -= OnCaptureComplete;
            
            if (SearchField != null)
            {
                SearchField.onValueChanged.RemoveListener(OnSearchChanged);
            }
        }
        
        void OnDevicesLoaded(int count)
        {
            RefreshList();
        }
        
        void OnDeviceUpdated(DeviceRecord device)
        {
            RefreshList();
        }
        
        void OnCaptureComplete(DeviceRecord device)
        {
            RefreshList();
        }
        
        void OnSearchChanged(string search)
        {
            _currentSearch = search.ToLower();
            RefreshList();
        }
        
        public void RefreshList()
        {
            if (_csvManager == null) return;
            
            // Clear existing
            foreach (var item in _listItems)
            {
                Destroy(item.gameObject);
            }
            _listItems.Clear();
            
            // Get devices
            var devices = _csvManager.Devices;
            int shown = 0;
            int captured = 0;
            
            foreach (var device in devices)
            {
                // Filter by search
                if (!string.IsNullOrEmpty(_currentSearch))
                {
                    if (!device.DeviceName.ToLower().Contains(_currentSearch) &&
                        !device.DeviceType.ToLower().Contains(_currentSearch))
                    {
                        continue;
                    }
                }
                
                // Create item
                var itemObj = Instantiate(CameraListItemPrefab, ListContainer);
                var item = itemObj.GetComponent<CameraListItem>();
                
                if (item != null)
                {
                    item.Setup(device, OnItemSelected);
                    _listItems.Add(item);
                    shown++;
                    
                    if (device.IsCaptured) captured++;
                }
            }
            
            // Update count text
            if (CountText != null)
            {
                CountText.text = $"Showing {shown} of {devices.Count} ({captured} captured)";
            }
        }
        
        void OnItemSelected(DeviceRecord device)
        {
            _controller?.SelectDevice(device);
        }
    }
}
