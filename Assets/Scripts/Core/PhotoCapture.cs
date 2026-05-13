using UnityEngine;
using System;
using System.IO;
using System.Collections;

namespace SiteOwlXR.Core
{
    /// <summary>
    /// Handles photo capture using the VIVE headset passthrough camera.
    /// Saves photos to device-specific folders.
    /// </summary>
    public class PhotoCapture : MonoBehaviour
    {
        [Header("Settings")]
        public int PhotoWidth = 1920;
        public int PhotoHeight = 1080;
        public string PhotosFolder = "CapturedPhotos";
        public string FileFormat = "jpg";
        
        [Header("Debug")]
        public bool ShowDebugUI = false;
        
        private string photosPath;
        private UnityEngine.Windows.WebCam.PhotoCapture photoCaptureObject;
        private bool isCapturing = false;
        private Action<string> onPhotoComplete;
        private string pendingDeviceId;
        
        public bool IsReady => photoCaptureObject != null;
        public bool IsCapturing => isCapturing;
        
        public event Action OnCaptureStarted;
        public event Action<string> OnPhotoSaved;
        public event Action<string> OnCaptureError;
        
        void Start()
        {
            photosPath = Path.Combine(Application.persistentDataPath, PhotosFolder);
            EnsureDirectoryExists();
            
            // Check if PhotoCapture is available on this platform
            if (!UnityEngine.Windows.WebCam.PhotoCapture.IsSupported)
            {
                Debug.LogWarning("[PhotoCapture] PhotoCapture not supported on this platform. " +
                    "Will use screenshot fallback.");
            }
        }
        
        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(photosPath))
            {
                Directory.CreateDirectory(photosPath);
            }
        }
        
        /// <summary>
        /// Starts the photo capture system. Call this before taking photos.
        /// </summary>
        public void Initialize()
        {
            if (!UnityEngine.Windows.WebCam.PhotoCapture.IsSupported)
            {
                Debug.Log("[PhotoCapture] Using screenshot fallback.");
                return;
            }
            
            var resolution = UnityEngine.Windows.WebCam.PhotoCapture.SupportedResolutions.GetEnumerator();
            resolution.MoveNext();
            
            var cameraParams = new UnityEngine.Windows.WebCam.CameraParameters(
                resolution.Current.width,
                resolution.Current.height,
                UnityEngine.Windows.WebCam.PhotoCapture.SupportedFormats.PNG
            );
            
            photoCaptureObject = UnityEngine.Windows.WebCam.PhotoCapture.Create();
            photoCaptureObject.StartPhotoModeAsync(cameraParams, OnPhotoModeStarted);
        }
        
        private void OnPhotoModeStarted(UnityEngine.Windows.WebCam.PhotoCapture.PhotoCaptureResult result)
        {
            if (result.success)
            {
                Debug.Log("[PhotoCapture] Photo mode started successfully.");
            }
            else
            {
                Debug.LogError("[PhotoCapture] Failed to start photo mode!");
                OnCaptureError?.Invoke("Failed to initialize camera");
            }
        }
        
        /// <summary>
        /// Captures a photo for the specified device.
        /// </summary>
        /// <param name="deviceId">Device identifier for folder naming</param>
        /// <param name="onComplete">Callback with full path to saved photo</param>
        public void TakePhoto(string deviceId, Action<string> onComplete)
        {
            if (isCapturing)
            {
                Debug.LogWarning("[PhotoCapture] Already capturing!");
                return;
            }
            
            pendingDeviceId = deviceId;
            onPhotoComplete = onComplete;
            isCapturing = true;
            
            OnCaptureStarted?.Invoke();
            
            if (photoCaptureObject != null)
            {
                photoCaptureObject.TakePhotoAsync(OnPhotoCaptured);
            }
            else
            {
                // Fallback: Use screenshot
                StartCoroutine(TakeScreenshotFallback());
            }
        }
        
        private void OnPhotoCaptured(UnityEngine.Windows.WebCam.PhotoCapture.PhotoCaptureResult result, 
            UnityEngine.Windows.WebCam.PhotoCaptureFrame photoCaptureFrame)
        {
            if (!result.success)
            {
                Debug.LogError("[PhotoCapture] Capture failed!");
                isCapturing = false;
                OnCaptureError?.Invoke("Photo capture failed");
                onPhotoComplete?.Invoke(null);
                return;
            }
            
            // Create texture and apply frame
            var texture = new Texture2D(PhotoWidth, PhotoHeight, TextureFormat.RGB24, false);
            photoCaptureFrame.UploadImageDataToTexture(texture);
            
            // Save the photo
            string path = SaveTexture(texture, pendingDeviceId);
            
            // Cleanup
            Destroy(texture);
            
            isCapturing = false;
            OnPhotoSaved?.Invoke(path);
            onPhotoComplete?.Invoke(path);
        }
        
        private IEnumerator TakeScreenshotFallback()
        {
            // Wait for end of frame
            yield return new WaitForEndOfFrame();
            
            // Capture screen
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            
            // Scale to target size
            var scaled = ScaleTexture(texture, PhotoWidth, PhotoHeight);
            Destroy(texture);
            
            // Save
            string path = SaveTexture(scaled, pendingDeviceId);
            Destroy(scaled);
            
            isCapturing = false;
            OnPhotoSaved?.Invoke(path);
            onPhotoComplete?.Invoke(path);
        }
        
        private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            var result = new Texture2D(targetWidth, targetHeight, source.format, false);
            var pixels = new Color[targetWidth * targetHeight];
            
            float xScale = (float)source.width / targetWidth;
            float yScale = (float)source.height / targetHeight;
            
            for (int y = 0; y < targetHeight; y++)
            {
                for (int x = 0; x < targetWidth; x++)
                {
                    int sourceX = Mathf.FloorToInt(x * xScale);
                    int sourceY = Mathf.FloorToInt(y * yScale);
                    pixels[y * targetWidth + x] = source.GetPixel(sourceX, sourceY);
                }
            }
            
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }
        
        private string SaveTexture(Texture2D texture, string deviceId)
        {
            EnsureDirectoryExists();
            
            // Create device folder
            string safeDeviceId = SanitizeFileName(deviceId);
            string deviceFolder = Path.Combine(photosPath, safeDeviceId);
            if (!Directory.Exists(deviceFolder))
            {
                Directory.CreateDirectory(deviceFolder);
            }
            
            // Generate filename with timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"{safeDeviceId}_{timestamp}.{FileFormat}";
            string fullPath = Path.Combine(deviceFolder, filename);
            
            // Encode and save
            byte[] bytes;
            if (FileFormat.ToLower() == "png")
            {
                bytes = texture.EncodeToPNG();
            }
            else
            {
                bytes = texture.EncodeToJPG(90); // 90% quality
            }
            
            File.WriteAllBytes(fullPath, bytes);
            
            Debug.Log($"[PhotoCapture] Saved: {fullPath}");
            return fullPath;
        }
        
        private string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unknown";
            
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            
            return name.Replace(" ", "_").ToLower();
        }
        
        /// <summary>
        /// Stops the photo capture system and releases resources.
        /// </summary>
        public void StopPhotoMode()
        {
            if (photoCaptureObject != null)
            {
                photoCaptureObject.StopPhotoModeAsync(OnPhotoModeStopped);
            }
        }
        
        private void OnPhotoModeStopped(UnityEngine.Windows.WebCam.PhotoCapture.PhotoCaptureResult result)
        {
            photoCaptureObject.Dispose();
            photoCaptureObject = null;
            Debug.Log("[PhotoCapture] Photo mode stopped.");
        }
        
        void OnDestroy()
        {
            StopPhotoMode();
        }
    }
}
