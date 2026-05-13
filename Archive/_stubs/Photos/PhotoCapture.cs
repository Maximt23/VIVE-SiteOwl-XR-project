using UnityEngine;
using System;
using System.IO;

namespace SiteOwlXR.Photos
{
    /// <summary>
    /// Captures photos using VIVE headset camera and saves them to the
    /// shared output folder: C:\VIVE-SiteOwl-XR-Designs\Meta data\Photos
    /// </summary>
    public class PhotoCapture : MonoBehaviour
    {
        [Header("Settings")]
        // Override in Inspector if the path ever changes — do NOT hardcode elsewhere.
        public string photosOutputPath = @"C:\VIVE-SiteOwl-XR-Designs\Meta data\Photos";
        public int photoWidth  = 1920;
        public int photoHeight = 1080;
        
        [Header("Events")]
        public UnityEngine.Events.UnityEvent<string> OnPhotoCaptured;
        public UnityEngine.Events.UnityEvent<string> OnPhotoError;
        
        private string photosPath = string.Empty;
        private bool isCapturing  = false;
        
        void Start()
        {
            // Use the configured output path; fall back to persistentDataPath if blank.
            photosPath = string.IsNullOrWhiteSpace(photosOutputPath)
                ? Path.Combine(Application.persistentDataPath, "Photos")
                : photosOutputPath;
            EnsureDirectoryExists();
        }
        
        void EnsureDirectoryExists()
        {
            if (!Directory.Exists(photosPath))
            {
                Directory.CreateDirectory(photosPath);
            }
        }
        
        /// <summary>
        /// Captures a photo for the specified device.
        /// </summary>
        /// <param name="deviceId">Device identifier</param>
        public void TakePhoto(string deviceId)
        {
            if (isCapturing)
            {
                Debug.LogWarning("[PhotoCapture] Already capturing!");
                return;
            }
            
            isCapturing = true;
            
            // TODO: Initialize VIVE camera or use Unity PhotoCapture
            // TODO: Capture image
            // TODO: Save to device-specific folder
            // TODO: Return file path
            
            // Placeholder: Use screenshot for now
            StartCoroutine(CaptureScreenshot(deviceId));
        }
        
        System.Collections.IEnumerator CaptureScreenshot(string deviceId)
        {
            yield return new WaitForEndOfFrame();
            
            // Create texture from screen
            Texture2D texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            
            // Save to file
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"{deviceId}_{timestamp}.jpg";
            string fullPath = Path.Combine(photosPath, filename);
            
            byte[] bytes = texture.EncodeToJPG(90);
            File.WriteAllBytes(fullPath, bytes);
            
            Destroy(texture);
            
            isCapturing = false;
            
            Debug.Log("[PhotoCapture] Saved: " + fullPath);
            OnPhotoCaptured?.Invoke(fullPath);
        }
        
        /// <summary>
        /// Returns true if a capture is in progress.
        /// </summary>
        public bool IsCapturing()
        {
            return isCapturing;
        }
    }
}
