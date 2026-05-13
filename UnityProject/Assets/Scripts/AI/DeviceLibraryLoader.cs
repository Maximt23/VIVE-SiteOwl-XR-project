using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SiteOwlXR.AI
{
    // ── Data model matching camera_model_library.json ─────────────────────────

    [Serializable]
    public class CameraLibraryMeta
    {
        public string version;
        public string description;
        public int    total_models;
    }

    [Serializable]
    public class CameraModelVisual
    {
        public string[]  color;
        public string    shape;
        public string    housing;
        public string    size_category;
        public bool      ir_leds;
        public string    dome_tint;
    }

    [Serializable]
    public class CameraModel
    {
        public string          id;
        public string          manufacturer;
        public string          model_name;
        public string          series;
        public string          form_factor;   // dome | bullet | ptz | fisheye | multisensor | turret
        public string[]        part_numbers;
        public CameraModelVisual visual;
        public string[]        keywords;
        public string[]        typical_zones;
        public string          recognition_hint;
        public string[]        search_queries;
        public string[]        local_images;
    }

    [Serializable]
    private class CameraLibraryRoot
    {
        public CameraLibraryMeta meta;
        public CameraModel[]     models;
    }

    /// <summary>
    /// Loads camera_model_library.json from StreamingAssets at startup.
    /// Uses UnityWebRequest so it works on Android (StreamingAssets are inside the APK).
    ///
    /// Wire: assign this component on the same GameObject as DeviceRecognizer.
    /// DeviceRecognizer calls GetModels() after OnLibraryLoaded fires.
    /// </summary>
    public class DeviceLibraryLoader : MonoBehaviour
    {
        [Header("Library File")]
        [Tooltip("Filename inside Assets/StreamingAssets/Data/")]
        public string LibraryFileName = "camera_model_library.json";

        public bool              IsLoaded { get; private set; } = false;
        public IReadOnlyList<CameraModel> Models => _models;
        public CameraLibraryMeta Meta   => _meta;

        public event Action    OnLibraryLoaded;
        public event Action<string> OnLibraryError;

        private List<CameraModel> _models = new();
        private CameraLibraryMeta _meta;

        void Start() => StartCoroutine(LoadLibrary());

        private IEnumerator LoadLibrary()
        {
            string path = System.IO.Path.Combine(
                Application.streamingAssetsPath, "Data", LibraryFileName);

            // On Android, streamingAssetsPath is a jar:// URI — must use UnityWebRequest
            using var req = UnityWebRequest.Get(path);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                string err = $"[DeviceLibraryLoader] Failed to load '{LibraryFileName}': {req.error}";
                Debug.LogError(err);
                OnLibraryError?.Invoke(err);
                yield break;
            }

            try
            {
                var root = JsonUtility.FromJson<CameraLibraryRoot>(req.downloadHandler.text);
                if (root?.models == null || root.models.Length == 0)
                    throw new Exception("Library parsed but contained no models.");

                _models = new List<CameraModel>(root.models);
                _meta   = root.meta;
                IsLoaded = true;

                Debug.Log($"[DeviceLibraryLoader] Loaded {_models.Count} camera models " +
                          $"(library v{_meta?.version}).");
                OnLibraryLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                string err = $"[DeviceLibraryLoader] JSON parse error: {ex.Message}";
                Debug.LogError(err);
                OnLibraryError?.Invoke(err);
            }
        }
    }
}
