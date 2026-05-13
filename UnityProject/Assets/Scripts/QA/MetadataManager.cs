using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using SiteOwlXR.Core;

namespace SiteOwlXR.QA
{
    // ── Data model ────────────────────────────────────────────────────────────

    [Serializable]
    public class DeviceCaptureMeta
    {
        public string device_id;
        public string device_name;
        public string system_type;
        public string device_type;
        public float  site_owl_x;
        public float  site_owl_y;
        public double latitude;
        public double longitude;
        public string photo_path;
        public string capture_timestamp;
        public string capture_method;
        public string confidence;
        public float  user_x;
        public float  user_y;
        public float  user_facing;
        public string review_status;
    }

    [Serializable]
    public class SurveyMetadata
    {
        public string                  store_csv;
        public string                  session_start;
        public string                  session_last_updated;
        public List<DeviceCaptureMeta> captures = new();
    }

    /// <summary>
    /// Writes a sidecar survey_metadata.json alongside the SiteOwl CSV.
    /// Preserves the audit trail (photo paths, timestamps, confidence, user position)
    /// that the SiteOwl CSV schema has no columns for.
    ///
    /// Wire: subscribe to CsvManager.OnDeviceUpdated and CsvManager.OnDataLoaded.
    /// </summary>
    public class MetadataManager : MonoBehaviour
    {
        [Header("Dependencies")]
        public CsvManager CsvManager;

        [Header("Settings")]
        [Tooltip("Filename written next to the loaded CSV.")]
        public string MetadataFileName = "survey_metadata.json";

        // ── State ─────────────────────────────────────────────────────────────
        private SurveyMetadata _meta;
        private string         _metaPath;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        void Start()
        {
            if (CsvManager == null)
            {
                Debug.LogError("[MetadataManager] CsvManager not assigned!");
                return;
            }
            CsvManager.OnDataLoaded    += OnDataLoaded;
            CsvManager.OnDeviceUpdated += OnDeviceUpdated;
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void OnDataLoaded()
        {
            string csvPath = GetCsvPath();
            if (string.IsNullOrEmpty(csvPath)) return;

            _metaPath = Path.Combine(
                Path.GetDirectoryName(csvPath) ?? Application.persistentDataPath,
                MetadataFileName);

            _meta = LoadOrCreate(csvPath);
            Debug.Log($"[MetadataManager] Metadata at: {_metaPath}  ({_meta.captures.Count} existing entries)");
        }

        private void OnDeviceUpdated(DeviceData device)
        {
            if (_meta == null || device == null) return;

            // Upsert — replace existing entry for this device or append
            int idx = _meta.captures.FindIndex(c => c.device_id == device.DeviceID);
            var entry = DeviceToMeta(device);

            if (idx >= 0) _meta.captures[idx] = entry;
            else          _meta.captures.Add(entry);

            _meta.session_last_updated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            Save();
        }

        // ── Serialization ─────────────────────────────────────────────────────

        private SurveyMetadata LoadOrCreate(string csvPath)
        {
            if (File.Exists(_metaPath))
            {
                try
                {
                    string json = File.ReadAllText(_metaPath);
                    var loaded = JsonUtility.FromJson<SurveyMetadata>(json);
                    if (loaded != null) return loaded;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MetadataManager] Could not parse existing metadata: {ex.Message}");
                }
            }

            return new SurveyMetadata
            {
                store_csv     = Path.GetFileName(csvPath),
                session_start = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                session_last_updated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };
        }

        private void Save()
        {
            if (_meta == null || string.IsNullOrEmpty(_metaPath)) return;
            try
            {
                string tmpPath = _metaPath + ".tmp";
                File.WriteAllText(tmpPath, JsonUtility.ToJson(_meta, prettyPrint: true));
                if (File.Exists(_metaPath)) File.Replace(tmpPath, _metaPath, null);
                else                        File.Move(tmpPath, _metaPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MetadataManager] Save failed: {ex.Message}");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static DeviceCaptureMeta DeviceToMeta(DeviceData d) => new()
        {
            device_id         = d.DeviceID,
            device_name       = d.DeviceName,
            system_type       = d.SystemType,
            device_type       = d.DeviceType,
            site_owl_x        = d.SiteOwlX ?? 0f,
            site_owl_y        = d.SiteOwlY ?? 0f,
            latitude          = d.Latitude  ?? 0,
            longitude         = d.Longitude ?? 0,
            photo_path        = d.PhotoPath         ?? "",
            capture_timestamp = d.CaptureTimestamp  ?? "",
            capture_method    = d.CaptureMethod     ?? "",
            confidence        = d.CoordinateConfidence.ToString(),
            user_x            = d.UserX             ?? 0f,
            user_y            = d.UserY             ?? 0f,
            user_facing       = d.UserFacingDirection ?? 0f,
            review_status     = d.ReviewStatus      ?? "",
        };

        private string GetCsvPath() => CsvManager?.LoadedPath ?? "";

        void OnDestroy()
        {
            if (CsvManager != null)
            {
                CsvManager.OnDataLoaded    -= OnDataLoaded;
                CsvManager.OnDeviceUpdated -= OnDeviceUpdated;
            }
        }
    }
}
