using UnityEngine;
using System;
using System.Collections;
using System.IO;

namespace SiteOwlXR.Core
{
    /// <summary>
    /// Captures photos using WebCamTexture — works on Android (VIVE XR Elite)
    /// and in the Unity editor. Windows.WebCam was HoloLens-only and is removed.
    ///
    /// Output path (in priority order):
    ///   1. PhotosOutputPath Inspector field (if non-empty)
    ///   2. Application.persistentDataPath/CapturedPhotos  (Android sandbox)
    /// </summary>
    public class PhotoCapture : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Absolute output path for photos. Leave blank to use app data folder.")]
        public string PhotosOutputPath = "";
        public int    PhotoWidth   = 1920;
        public int    PhotoHeight  = 1080;
        public string FileFormat   = "jpg";     // "jpg" or "png"

        [Header("Camera")]
        [Tooltip("Index into WebCamTexture.devices[]. 0 = first/back camera.")]
        public int    CameraDeviceIndex = 0;
        [Tooltip("Seconds to wait for WebCamTexture to warm up before grabbing a frame.")]
        public float  CameraWarmupSeconds = 1.5f;

        // ── Public state ─────────────────────────────────────────────────────
        public bool IsReady     => _ready;
        public bool IsCapturing => _capturing;

        public event Action           OnCaptureStarted;
        public event Action<string>   OnPhotoSaved;
        public event Action<string>   OnCaptureError;

        // ── Private ──────────────────────────────────────────────────────────
        private string        _photosPath;
        private bool          _ready     = false;
        private bool          _capturing = false;
        private WebCamTexture _cam;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        void Start()
        {
            _photosPath = string.IsNullOrWhiteSpace(PhotosOutputPath)
                ? Path.Combine(Application.persistentDataPath, "CapturedPhotos")
                : PhotosOutputPath;

            EnsureDir(_photosPath);
        }

        /// <summary>
        /// Opens the device camera. Called by CaptureController.Start().
        /// Safe to call multiple times — no-ops if already initialised.
        /// </summary>
        public void Initialize()
        {
            if (_ready) return;

            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogWarning("[PhotoCapture] No camera devices found. Screenshot fallback active.");
                _ready = true;   // let capture proceed — will use screenshot
                return;
            }

            int idx   = Mathf.Clamp(CameraDeviceIndex, 0, WebCamTexture.devices.Length - 1);
            string dev = WebCamTexture.devices[idx].name;

            _cam = new WebCamTexture(dev, PhotoWidth, PhotoHeight, 30);
            _cam.Play();

            _ready = true;
            Debug.Log($"[PhotoCapture] Camera '{dev}' opened ({PhotoWidth}x{PhotoHeight}).");
        }

        // ── Capture ───────────────────────────────────────────────────────────

        /// <summary>
        /// Captures a photo and invokes <paramref name="onComplete"/> with the saved path.
        /// </summary>
        public void TakePhoto(string deviceId, Action<string> onComplete)
        {
            if (_capturing)
            {
                Debug.LogWarning("[PhotoCapture] Already capturing — ignoring duplicate call.");
                return;
            }
            StartCoroutine(DoCaptureCoroutine(deviceId, onComplete));
        }

        private IEnumerator DoCaptureCoroutine(string deviceId, Action<string> onComplete)
        {
            _capturing = true;
            OnCaptureStarted?.Invoke();

            // --- Camera path ---
            if (_cam != null && _cam.isPlaying)
            {
                // Give the sensor a moment to auto-expose after the shutter opens
                yield return new WaitForSeconds(CameraWarmupSeconds);
                yield return new WaitForEndOfFrame();

                var tex = new Texture2D(_cam.width, _cam.height, TextureFormat.RGB24, false);
                tex.SetPixels32(_cam.GetPixels32());
                tex.Apply();

                string path = SaveTexture(tex, deviceId);
                Destroy(tex);

                _capturing = false;
                if (path != null)
                {
                    OnPhotoSaved?.Invoke(path);
                    onComplete?.Invoke(path);
                }
                else
                {
                    OnCaptureError?.Invoke("Failed to save photo.");
                    onComplete?.Invoke(null);
                }
            }
            else
            {
                // --- Screenshot fallback (editor / no camera) ---
                Debug.LogWarning("[PhotoCapture] Camera not available — using screenshot fallback.");
                yield return new WaitForEndOfFrame();

                var screen = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                screen.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                screen.Apply();

                var scaled = ScaleTexture(screen, PhotoWidth, PhotoHeight);
                Destroy(screen);

                string path = SaveTexture(scaled, deviceId);
                Destroy(scaled);

                _capturing = false;
                if (path != null)
                {
                    OnPhotoSaved?.Invoke(path);
                    onComplete?.Invoke(path);
                }
                else
                {
                    OnCaptureError?.Invoke("Screenshot save failed.");
                    onComplete?.Invoke(null);
                }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string SaveTexture(Texture2D texture, string deviceId)
        {
            EnsureDir(_photosPath);

            string safe   = SanitizeFileName(deviceId);
            string folder = Path.GetFullPath(Path.Combine(_photosPath, safe));

            // Path traversal guard
            if (!folder.StartsWith(Path.GetFullPath(_photosPath), StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"[PhotoCapture] Path traversal blocked for deviceId: {deviceId}");
                OnCaptureError?.Invoke("Invalid device ID.");
                return null;
            }

            EnsureDir(folder);

            string ts       = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"{safe}_{ts}.{FileFormat}";
            string fullPath = Path.Combine(folder, filename);

            byte[] bytes = FileFormat.Equals("png", StringComparison.OrdinalIgnoreCase)
                ? texture.EncodeToPNG()
                : texture.EncodeToJPG(90);

            try
            {
                File.WriteAllBytes(fullPath, bytes);
                Debug.Log($"[PhotoCapture] Saved: {fullPath}");
                return fullPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PhotoCapture] Write failed: {ex.Message}");
                return null;
            }
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unknown";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            name = name.Replace(".", "_").Replace(" ", "_").ToLower();
            return string.IsNullOrWhiteSpace(name.Trim('_')) ? "unknown" : name;
        }

        private static Texture2D ScaleTexture(Texture2D src, int w, int h)
        {
            var dst = new Texture2D(w, h, src.format, false);
            float sx = (float)src.width  / w;
            float sy = (float)src.height / h;
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = src.GetPixel(Mathf.FloorToInt(x * sx), Mathf.FloorToInt(y * sy));
            dst.SetPixels(pixels);
            dst.Apply();
            return dst;
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        void OnDestroy()
        {
            if (_cam != null && _cam.isPlaying)
                _cam.Stop();
        }
    }
}
