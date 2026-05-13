using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SiteOwlXR.Core
{
    /// <summary>
    /// SiteOwl CSV Manager - SUPPLEMENTS collaborator data, never overwrites.
    /// 
    /// What we WRITE:
    /// - Barcode: GPS coordinates (lat,long)
    /// - X, Y: SiteOwl coordinates (if available)
    /// - Photo URL: External storage link
    /// - Photo Path: Local/network path
    /// - Capture metadata: Timestamp, Method, Confidence
    /// - Review Status: OK or REVIEW_REQUIRED
    /// 
    /// What we PRESERVE (collaborator data):
    /// - Site, Device Name, Device Type, System Type
    /// - Description, IP Address, VMS Zone
    /// - Manufacturer, Model, Serial Number
    /// - All other columns
    /// 
    /// What we SUGGEST (via AI, user confirms):
    /// - Device Type (if empty)
    /// - Device categorization
    /// </summary>
    public class SiteOwlCsvManager : MonoBehaviour
    {
        [Header("Settings")]
        public string CsvFileName = "cameras.csv";
        public string BackupFolder = "Backups";
        
        [Header("Column Mapping")]
        public string GpsBarcodeColumn = "Barcode";  // SiteOwl GPS field
        public string PhotoUrlColumn = "Photo URL";    // External photo link
        public string PhotoPathColumn = "Photo Path";  // Local path
        public string CaptureTimestampColumn = "Capture Timestamp";
        public string CaptureMethodColumn = "Capture Method";
        public string ReviewStatusColumn = "Review Status";
        
        [Header("Protected Columns (Never Overwrite)")]
        public string[] ProtectedColumns = new[] {
            "Site", "Site ID", "SITE", "SITE ID",
            "Device Name", "Name", "DEVICE NAME", "NAME",
            "Device Type", "Type", "DEVICE TYPE", "TYPE", "Camera Type",
            "System Type", "System", "SYSTEM TYPE", "SYSTEM",
            "Description", "Desc", "DESCRIPTION", "DESC",
            "IP Address", "IP", "IP_ADDRESS", "IPADDRESS",
            "VMS Zone", "Zone", "VMS_ZONE", "ZONE",
            "Manufacturer", "Make", "MANUFACTURER", "MAKE",
            "Model", "MODEL",
            "Serial Number", "Serial", "SERIAL", "S/N"
        };
        
        [Header("Data")]
        private string csvPath;
        private List<CameraData> cameras = new List<CameraData>();
        private List<string> originalHeaders = new List<string>();
        private string[] originalLines;
        
        public List<CameraData> Cameras => cameras;
        public int TotalCount => cameras.Count;
        public int CapturedCount => cameras.Count(c => !string.IsNullOrEmpty(c.GpsBarcode));
        public int MissingCount => cameras.Count(c => string.IsNullOrEmpty(c.GpsBarcode));
        
        public event Action OnDataLoaded;
        public event Action<CameraData> OnCameraUpdated;
        public event Action OnDataSaved;
        
        void Start()
        {
            csvPath = Path.Combine(Application.persistentDataPath, "Data", "Input", CsvFileName);
        }
        
        /// <summary>
        /// Loads CSV and parses camera records.
        /// PRESERVES all existing collaborator data.
        /// </summary>
        public bool LoadCsv(string path = null)
        {
            string targetPath = path ?? csvPath;
            
            if (!File.Exists(targetPath))
            {
                Debug.LogError($"[SiteOwlCsvManager] CSV not found: {targetPath}");
                return false;
            }
            
            try
            {
                originalLines = File.ReadAllLines(targetPath);
                ParseCsv(originalLines);
                
                Debug.Log($"[SiteOwlCsvManager] Loaded {cameras.Count} cameras. " +
                    $"Captured: {CapturedCount}, Missing: {MissingCount}");
                
                OnDataLoaded?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SiteOwlCsvManager] Load failed: {ex.Message}");
                return false;
            }
        }
        
        private void ParseCsv(string[] lines)
        {
            cameras.Clear();
            
            if (lines.Length == 0) return;
            
            // Parse headers
            originalHeaders = lines[0].Split(',').Select(h => h.Trim().ToUpper()).ToList();
            
            // Parse rows
            for (int i = 1; i < lines.Length; i++)
            {
                var camera = ParseCameraRow(lines[i], originalHeaders);
                if (camera != null)
                {
                    cameras.Add(camera);
                }
            }
        }
        
        private CameraData ParseCameraRow(string line, List<string> headers)
        {
            var values = ParseCsvLine(line);
            if (values.Count != headers.Count)
            {
                Debug.LogWarning($"[SiteOwlCsvManager] Row has {values.Count} columns, expected {headers.Count}");
                return null;
            }
            
            var camera = new CameraData();
            
            for (int i = 0; i < headers.Count; i++)
            {
                string header = headers[i];
                string value = values[i].Trim();
                
                // Site
                if (header == "SITE" || header == "SITE ID")
                    camera.SiteId = value;
                
                // Device identification (COLLABORATOR OWNED - preserve)
                else if (header == "DEVICE NAME" || header == "NAME")
                    camera.DeviceName = value;
                else if (header == "DEVICE TYPE" || header == "TYPE" || header == "CAMERA TYPE")
                    camera.DeviceType = value;
                else if (header == "SYSTEM TYPE" || header == "SYSTEM")
                    camera.SystemType = value;
                else if (header == "DESCRIPTION" || header == "DESC")
                    camera.Description = value;
                
                // GPS Barcode (our data)
                else if (header == "BARCODE" || header == "BAR CODE")
                    camera.GpsBarcode = value;
                
                // SiteOwl coordinates
                else if (header == "X" || header == "SITEOWL X")
                    camera.SiteOwlX = ParseFloat(value);
                else if (header == "Y" || header == "SITEOWL Y")
                    camera.SiteOwlY = ParseFloat(value);
                
                // Network info (COLLABORATOR OWNED - preserve)
                else if (header == "IP ADDRESS" || header == "IP")
                    camera.IpAddress = value;
                else if (header == "VMS ZONE" || header == "ZONE")
                    camera.VmsZone = value;
                
                // Hardware info (COLLABORATOR OWNED - preserve)
                else if (header == "MANUFACTURER" || header == "MAKE")
                    camera.Manufacturer = value;
                else if (header == "MODEL")
                    camera.Model = value;
                else if (header == "SERIAL" || header == "SERIAL NUMBER" || header == "S/N")
                    camera.SerialNumber = value;
                
                // Photos (our data)
                else if (header.Contains("PHOTO URL") || header == "PHOTOURL")
                    camera.PhotoUrl = value;
                else if (header.Contains("PHOTO PATH") || header == "PHOTOPATH")
                    camera.PhotoPath = value;
                
                // Capture metadata (our data)
                else if (header.Contains("TIMESTAMP") || header == "CAPTURE TIMESTAMP")
                    camera.CaptureTimestamp = value;
                else if (header.Contains("METHOD") || header == "CAPTURE METHOD")
                    camera.CaptureMethod = value;
                else if (header.Contains("CONFIDENCE") || header == "GPS ACCURACY")
                    camera.GpsAccuracy = ParseFloat(value);
                
                // Review status (our data)
                else if (header.Contains("REVIEW") || header == "REVIEW STATUS" || header == "STATUS")
                    camera.ReviewStatus = value;
            }
            
            // Use device name as ID if no explicit ID
            if (string.IsNullOrEmpty(camera.DeviceId))
                camera.DeviceId = camera.DeviceName;
            
            return camera;
        }
        
        /// <summary>
        /// Records GPS capture for a camera.
        /// GPS stored in Barcode field as "lat,long".
        /// </summary>
        public void RecordGpsCapture(string deviceId, double latitude, double longitude, float accuracy)
        {
            var camera = cameras.FirstOrDefault(c => c.DeviceId == deviceId || c.DeviceName == deviceId);
            if (camera == null)
            {
                Debug.LogError($"[SiteOwlCsvManager] Camera not found: {deviceId}");
                return;
            }
            
            // Format GPS for Barcode field (SiteOwl standard)
            camera.GpsBarcode = $"{latitude:F6},{longitude:F6}";
            camera.Latitude = latitude;
            camera.Longitude = longitude;
            camera.GpsAccuracy = accuracy;
            
            Debug.Log($"[SiteOwlCsvManager] GPS recorded for {deviceId}: {camera.GpsBarcode}");
        }
        
        /// <summary>
        /// Records photo information.
        /// Photo stored externally, URL/path saved in CSV.
        /// </summary>
        public void RecordPhoto(string deviceId, string photoUrl, string photoPath)
        {
            var camera = cameras.FirstOrDefault(c => c.DeviceId == deviceId || c.DeviceName == deviceId);
            if (camera == null) return;
            
            camera.PhotoUrl = photoUrl;
            camera.PhotoPath = photoPath;
            camera.CaptureTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            camera.CaptureMethod = "XR_CAPTURE";
            camera.ReviewStatus = "OK";
            
            Debug.Log($"[SiteOwlCsvManager] Photo recorded for {deviceId}: {photoUrl}");
        }
        
        /// <summary>
        /// Saves updated CSV.
        /// PRESERVES all collaborator data.
        /// Only updates our specific columns.
        /// </summary>
        public bool SaveCsv()
        {
            // Create backup
            if (!CreateBackup())
            {
                Debug.LogError("[SiteOwlCsvManager] Backup failed, aborting save");
                return false;
            }
            
            try
            {
                var lines = new List<string>();
                
                // Header row
                lines.Add(string.Join(",", originalHeaders));
                
                // Data rows
                for (int i = 1; i < originalLines.Length; i++)
                {
                    string updatedLine = UpdateCameraRow(originalLines[i], originalHeaders, i - 1);
                    lines.Add(updatedLine);
                }
                
                File.WriteAllLines(csvPath, lines);
                
                Debug.Log($"[SiteOwlCsvManager] Saved. {CapturedCount}/{TotalCount} cameras have GPS");
                OnDataSaved?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SiteOwlCsvManager] Save failed: {ex.Message}");
                return false;
            }
        }
        
        private string UpdateCameraRow(string originalLine, List<string> headers, int cameraIndex)
        {
            if (cameraIndex >= cameras.Count)
                return originalLine; // Return unchanged if no matching camera
            
            var camera = cameras[cameraIndex];
            var values = ParseCsvLine(originalLine);
            
            for (int i = 0; i < headers.Count && i < values.Count; i++)
            {
                string header = headers[i];
                
                // Only update OUR columns
                // GPS Barcode
                if ((header == "BARCODE" || header == "BAR CODE") && !string.IsNullOrEmpty(camera.GpsBarcode))
                {
                    values[i] = camera.GpsBarcode;
                }
                // SiteOwl X
                else if ((header == "X" || header == "SITEOWL X") && camera.SiteOwlX.HasValue)
                {
                    values[i] = camera.SiteOwlX.Value.ToString("F2");
                }
                // SiteOwl Y
                else if ((header == "Y" || header == "SITEOWL Y") && camera.SiteOwlY.HasValue)
                {
                    values[i] = camera.SiteOwlY.Value.ToString("F2");
                }
                // Photo URL
                else if ((header.Contains("PHOTO URL") || header == "PHOTOURL") && !string.IsNullOrEmpty(camera.PhotoUrl))
                {
                    values[i] = camera.PhotoUrl;
                }
                // Photo Path
                else if ((header.Contains("PHOTO PATH") || header == "PHOTOPATH") && !string.IsNullOrEmpty(camera.PhotoPath))
                {
                    values[i] = camera.PhotoPath;
                }
                // Timestamp
                else if ((header.Contains("TIMESTAMP") || header == "CAPTURE TIMESTAMP") && !string.IsNullOrEmpty(camera.CaptureTimestamp))
                {
                    values[i] = camera.CaptureTimestamp;
                }
                // Method
                else if ((header.Contains("METHOD") || header == "CAPTURE METHOD") && !string.IsNullOrEmpty(camera.CaptureMethod))
                {
                    values[i] = camera.CaptureMethod;
                }
                // GPS Accuracy
                else if ((header.Contains("ACCURACY") || header == "GPS ACCURACY") && camera.GpsAccuracy.HasValue)
                {
                    values[i] = camera.GpsAccuracy.Value.ToString("F1");
                }
                // Review Status
                else if ((header.Contains("REVIEW") || header == "REVIEW STATUS" || header == "STATUS") && !string.IsNullOrEmpty(camera.ReviewStatus))
                {
                    values[i] = camera.ReviewStatus;
                }
                // COLLABORATOR COLUMNS - PRESERVE EXISTING
                else if (IsCollaboratorColumn(header))
                {
                    // Keep original value - don't overwrite
                    continue;
                }
            }
            
            return string.Join(",", values);
        }
        
        private bool IsCollaboratorColumn(string header)
        {
            return ProtectedColumns.Any(protectedHeader => 
                header.Contains(protectedHeader) || 
                protectedHeader.Contains(header));
        }
        
        private bool CreateBackup()
        {
            try
            {
                string backupDir = Path.Combine(Application.persistentDataPath, BackupFolder);
                Directory.CreateDirectory(backupDir);
                
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(backupDir, $"{Path.GetFileNameWithoutExtension(CsvFileName)}_backup_{timestamp}.csv");
                
                File.Copy(csvPath, backupPath);
                Debug.Log($"[SiteOwlCsvManager] Backup: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SiteOwlCsvManager] Backup failed: {ex.Message}");
                return false;
            }
        }
        
        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            string current = "";
            
            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            
            result.Add(current);
            return result;
        }
        
        private float? ParseFloat(string value)
        {
            if (float.TryParse(value, out float result))
                return result;
            return null;
        }
        
        public List<CameraData> GetMissingCameras()
        {
            return cameras.Where(c => string.IsNullOrEmpty(c.GpsBarcode)).ToList();
        }
        
        public List<CameraData> SearchCameras(string query)
        {
            if (string.IsNullOrEmpty(query)) return cameras;
            
            query = query.ToLower();
            return cameras.Where(c => 
                (c.DeviceName?.ToLower().Contains(query) ?? false) ||
                (c.DeviceId?.ToLower().Contains(query) ?? false) ||
                (c.IpAddress?.ToLower().Contains(query) ?? false)
            ).ToList();
        }
    }
    
    [Serializable]
    public class CameraData
    {
        // Identification
        public string DeviceId;
        public string DeviceName;
        public string SiteId;
        
        // Collaborator-owned (PRESERVE)
        public string DeviceType;      // CCTV camera type
        public string SystemType;    // Usually "CCTV"
        public string Description;
        public string IpAddress;
        public string VmsZone;
        public string Manufacturer;
        public string Model;
        public string SerialNumber;
        
        // Our data (GPS in Barcode field)
        public string GpsBarcode;      // Format: "lat,long"
        public double? Latitude;
        public double? Longitude;
        public float? SiteOwlX;
        public float? SiteOwlY;
        public float? GpsAccuracy;
        
        // Our data (Photos external)
        public string PhotoUrl;        // HTTPS link
        public string PhotoPath;     // Local/network path
        
        // Our data (Capture metadata)
        public string CaptureTimestamp;
        public string CaptureMethod;   // XR_CAPTURE
        public string ReviewStatus;    // OK or REVIEW_REQUIRED
        
        // UI helpers
        public bool HasGps => !string.IsNullOrEmpty(GpsBarcode);
        public bool HasPhoto => !string.IsNullOrEmpty(PhotoUrl) || !string.IsNullOrEmpty(PhotoPath);
        public bool NeedsReview => ReviewStatus == "REVIEW_REQUIRED";
    }
}
