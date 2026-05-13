using System;
using System.Collections;
using System.IO;
using UnityEngine;
using SiteOwlXR.Events;
using SiteOwlXR.Services;

namespace SiteOwlXR.Managers
{
    /// <summary>
    /// Photo Capture Manager - Enterprise Grade
    /// Handles headset camera capture with fallback strategies
    /// </summary>
    public class PhotoCapture : MonoBehaviour
    {
        [Header("Configuration")]
        public int photoWidth = 1920;
        public int photoHeight = 1080;
        public int jpegQuality = 85;
        public string photosFolder = "CapturedPhotos";
        
        [Header("State")]
        public bool IsCapturing { get; private set; } = false;
        public string LastPhotoPath { get; private set; }
        
        private ILogger _logger;
        private string _photosPath;
        
        void Awake()
        {
            _logger = ServiceLocator.Get<ILogger>() ?? new UnityLogger();
            _photosPath = Path.Combine(Application.persistentDataPath, photosFolder);
            Directory.CreateDirectory(_photosPath);
        }
        
        /// <summary>
        /// Capture photo for device
        /// </summary>
        public IEnumerator TakePhoto(string deviceId, string siteId, Action<string> onComplete, Action<string> onError)
        {
            if (IsCapturing)
            {
                onError?.Invoke("Capture already in progress");
                yield break;
            }
            
            IsCapturing = true;
            _logger.LogInfo($"Starting photo capture for {deviceId}", this);
            
            // Generate filename
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"{siteId}_{deviceId}_{timestamp}.jpg";
            string filepath = Path.Combine(_photosPath, filename);
            
            // Editor mode - create mock photo
            if (Application.isEditor)
            {
                yield return CreateMockPhoto(filepath);
            }
            else
            {
                // Try native camera capture
                yield return CaptureNativePhoto(filepath, onError);
            }
            
            IsCapturing = false;
            
            if (File.Exists(filepath))
            {
                LastPhotoPath = filepath;
                _logger.LogInfo($"Photo captured: {filepath}", this);
                onComplete?.Invoke(filepath);
            }
            else
            {
                onError?.Invoke("Photo capture failed - file not created");
            }
        }
        
        IEnumerator CreateMockPhoto(string filepath)
        {
            _logger.LogDebug("Creating mock photo in editor", this);
            
            // Create a simple colored texture
            Texture2D mockTexture = new Texture2D(photoWidth, photoHeight, TextureFormat.RGB24, false);
            Color[] pixels = new Color[photoWidth * photoHeight];
            
            // Create gradient pattern
            for (int y = 0; y < photoHeight; y++)
            {
                for (int x = 0; x < photoWidth; x++)
                {
                    float r = (float)x / photoWidth;
                    float g = (float)y / photoHeight;
                    float b = 0.5f;
                    pixels[y * photoWidth + x] = new Color(r, g, b);
                }
            }
            
            mockTexture.SetPixels(pixels);
            mockTexture.Apply();
            
            // Save
            byte[] bytes = mockTexture.EncodeToJPG(jpegQuality);
            File.WriteAllBytes(filepath, bytes);
            
            Destroy(mockTexture);
            
            yield return null;
        }
        
        IEnumerator CaptureNativePhoto(string filepath, Action<string> onError)
        {
            _logger.LogDebug("Attempting native camera capture", this);
            
            // Try to use WebCamTexture or native camera API
            // This is a simplified implementation - in production you'd use
            // platform-specific native plugins for best quality
            
            yield return CaptureScreenshotFallback(filepath);
        }
        
        IEnumerator CaptureScreenshotFallback(string filepath)
        {
            _logger.LogDebug("Using screenshot fallback", this);
            
            // Wait for end of frame
            yield return new WaitForEndOfFrame();
            
            // Capture screen
            Texture2D screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenShot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenShot.Apply();
            
            // Scale to target size
            Texture2D scaled = ScaleTexture(screenShot, photoWidth, photoHeight);
            Destroy(screenShot);
            
            // Save
            byte[] bytes = scaled.EncodeToJPG(jpegQuality);
            File.WriteAllBytes(filepath, bytes);
            
            Destroy(scaled);
            
            yield return null;
        }
        
        Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, false);
            
            float scaleX = (float)source.width / targetWidth;
            float scaleY = (float)source.height / targetHeight;
            
            Color[] pixels = new Color[targetWidth * targetHeight];
            
            for (int y = 0; y < targetHeight; y++)
            {
                for (int x = 0; x < targetWidth; x++)
                {
                    int sourceX = Mathf.FloorToInt(x * scaleX);
                    int sourceY = Mathf.FloorToInt(y * scaleY);
                    pixels[y * targetWidth + x] = source.GetPixel(sourceX, sourceY);
                }
            }
            
            result.SetPixels(pixels);
            result.Apply();
            
            return result;
        }
    }
}
