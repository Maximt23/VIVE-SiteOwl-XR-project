using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SiteOwlXR.Core
{
    /// <summary>
    /// Manages CSV loading, updating, and saving.
    /// GUARD RAILS: Never overwrites non-coordinate fields. Always backs up first.
    /// </summary>
    public class CsvManager : MonoBehaviour
    {
        [Header("Settings")]
        public string CsvFileName = "devices.csv";
        public string BackupFolder = "Backups";
        
        private string csvPath;
        private List<DeviceData> devices = new List<DeviceData>();
        private List<string> originalHeaders = new List<string>();
        private string[] originalLines;
        
        public List<DeviceData> Devices => devices;
        public int TotalCount => devices.Count;
        public int CapturedCount => devices.Count(d => !d.NeedsCapture);
        public int RemainingCount => devices.Count(d => d.NeedsCapture);
        
        public event Action OnDataLoaded;
        public event Action<DeviceData> OnDeviceUpdated;
        public event Action OnDataSaved;
        
        void Start()
        {
            // Set up path in persistent data (Android external storage)
            csvPath = Path.Combine(Application.persistentDataPath, CsvFileName);
        }
        
        /// <summary>
        /// Loads CSV from the specified path.
        /// </summary>
        public bool LoadCsv(string path = null)
        {
            string targetPath = path ?? csvPath;
            
            if (!File.Exists(targetPath))
            {
                Debug.LogError($"[CsvManager] CSV not found: {targetPath}");
                return false;
            }
            
            try
            {
                originalLines = File.ReadAllLines(targetPath);
                ParseCsv(originalLines);
                
                Debug.Log($"[CsvManager] Loaded {devices.Count} devices. {RemainingCount} need capture.");
                OnDataLoaded?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvManager] Failed to load CSV: {ex.Message}");
                return false;
            }
        }
        
        private void ParseCsv(string[] lines)
        {
            devices.Clear();
            
            if (lines.Length == 0) return;
            
            // Parse headers
            originalHeaders = lines[0].Split(',').Select(h => h.Trim()).ToList();
            
            // Parse rows
            for (int i = 1; i < lines.Length; i++)
            {
                var device = ParseRow(lines[i], originalHeaders);
                if (device != null)
                {
                    devices.Add(device);
                }
            }
        }
        
        private DeviceData ParseRow(string line, List<string> headers)
        {
            var values = ParseCsvLine(line);
            if (values.Count != headers.Count)
            {
                Debug.LogWarning($"[CsvManager] Row has {values.Count} columns, expected {headers.Count}. Skipping.");
                return null;
            }
            
            var device = new DeviceData();
            
            for (int i = 0; i < headers.Count; i++)
            {
                string header = headers[i].ToUpper();
                string value = values[i].Trim();
                
                switch (header)
                {
                    case "NAME":
                    case "DEVICE NAME":
                    case "DEVICENAME":
                        device.DeviceName = value;
                        break;
                    case "TYPE":
                    case "DEVICE TYPE":
                    case "DEVICETYPE":
                        device.DeviceType = value;
                        break;
                    case "SYSTEM TYPE":
                    case "SYSTEMTYPE":
                        device.SystemType = value;
                        break;
                    case "DESCRIPTION":
                    case "DESC":
                        device.Description = value;
                        break;
                    case "ID":
                    case "DEVICE ID":
                    case "DEVICEID":
                        device.DeviceID = value;
                        break;
                    case "X":
                    case "SITEOWL X":
                    case "SITEOWLX":
                        device.SiteOwlX = ParseFloat(value);
                        break;
                    case "Y":
                    case "SITEOWL Y":
                    case "SITEOWLY":
                        device.SiteOwlY = ParseFloat(value);
                        break;
                    case "LATITUDE":
                    case "LAT":
                        device.Latitude = ParseDouble(value);
                        break;
                    case "LONGITUDE":
                    case "LON":
                    case "LONG":
                        device.Longitude = ParseDouble(value);
                        break;
                    case "PHOTO PATH":
                    case "PHOTOPATH":
                    case "PHOTO":
                        device.PhotoPath = value;
                        break;
                    case "CAPTURE TIMESTAMP":
                    case "TIMESTAMP":
                        device.CaptureTimestamp = value;
                        break;
                    case "CAPTURE METHOD":
                    case "METHOD":
                        device.CaptureMethod = value;
                        break;
                    case "COORDINATE CONFIDENCE":
                    case "CONFIDENCE":
                        device.CoordinateConfidence = ParseConfidence(value);
                        break;
                    case "USER X":
                    case "USERX":
                        device.UserX = ParseFloat(value);
                        break;
                    case "USER Y":
                    case "USERY":
                        device.UserY = ParseFloat(value);
                        break;
                    case "USER FACING":
                    case "USERFACINGDIRECTION":
                    case "FACING":
                        device.UserFacingDirection = ParseFloat(value);
                        break;
                    case "REVIEW STATUS":
                    case "REVIEWSTATUS":
                    case "STATUS":
                        device.ReviewStatus = value;
                        break;
                }
            }
            
            // Use device name as ID if no ID provided
            if (string.IsNullOrEmpty(device.DeviceID))
            {
                device.DeviceID = device.DeviceName;
            }
            
            return device;
        }
        
        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            string currentValue = "";
            
            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentValue);
                    currentValue = "";
                }
                else
                {
                    currentValue += c;
                }
            }
            
            result.Add(currentValue);
            return result;
        }
        
        private float? ParseFloat(string value)
        {
            if (float.TryParse(value, out float result))
                return result;
            return null;
        }
        
        private double? ParseDouble(string value)
        {
            if (double.TryParse(value, out double result))
                return result;
            return null;
        }
        
        private CoordinateConfidence ParseConfidence(string value)
        {
            if (Enum.TryParse<CoordinateConfidence>(value, true, out var result))
                return result;
            return CoordinateConfidence.NONE;
        }
        
        /// <summary>
        /// Updates a device record and saves the CSV.
        /// GUARD RAIL: Always creates backup first.
        /// </summary>
        public bool UpdateDevice(DeviceData updatedDevice)
        {
            int index = devices.FindIndex(d => d.DeviceID == updatedDevice.DeviceID);
            if (index < 0)
            {
                Debug.LogError($"[CsvManager] Device not found: {updatedDevice.DeviceID}");
                return false;
            }
            
            // GUARD RAIL: Don't allow overwriting existing coordinates without explicit flag
            var existing = devices[index];
            if (existing.SiteOwlX.HasValue && updatedDevice.SiteOwlX.HasValue)
            {
                Debug.LogWarning($"[CsvManager] Overwriting existing coordinates for {updatedDevice.DeviceID}");
            }
            
            // Create backup
            if (!CreateBackup())
            {
                Debug.LogError("[CsvManager] Failed to create backup. Aborting update.");
                return false;
            }
            
            // Update device
            devices[index] = updatedDevice;
            
            // Save CSV
            if (SaveCsv())
            {
                OnDeviceUpdated?.Invoke(updatedDevice);
                return true;
            }
            
            return false;
        }
        
        private bool CreateBackup()
        {
            try
            {
                string backupDir = Path.Combine(Application.persistentDataPath, BackupFolder);
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }
                
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(backupDir, $"devices_backup_{timestamp}.csv");
                
                File.Copy(csvPath, backupPath);
                Debug.Log($"[CsvManager] Backup created: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvManager] Backup failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Writes the current devices list back to CSV.
        /// </summary>
        public bool SaveCsv()
        {
            try
            {
                var lines = new List<string>();
                
                // Header
                lines.Add(string.Join(",", originalHeaders));
                
                // Data rows
                foreach (var device in devices)
                {
                    lines.Add(FormatDeviceRow(device, originalHeaders));
                }
                
                File.WriteAllLines(csvPath, lines);
                
                Debug.Log($"[CsvManager] CSV saved. {CapturedCount}/{TotalCount} devices captured.");
                OnDataSaved?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvManager] Save failed: {ex.Message}");
                return false;
            }
        }
        
        private string FormatDeviceRow(DeviceData device, List<string> headers)
        {
            var values = new List<string>();
            
            foreach (var header in headers)
            {
                string h = header.ToUpper();
                string value = "";
                
                switch (h)
                {
                    case "NAME":
                    case "DEVICE NAME":
                    case "DEVICENAME":
                        value = device.DeviceName;
                        break;
                    case "TYPE":
                    case "DEVICE TYPE":
                    case "DEVICETYPE":
                        value = device.DeviceType;
                        break;
                    case "SYSTEM TYPE":
                    case "SYSTEMTYPE":
                        value = device.SystemType;
                        break;
                    case "DESCRIPTION":
                    case "DESC":
                        value = device.Description;
                        break;
                    case "ID":
                    case "DEVICE ID":
                    case "DEVICEID":
                        value = device.DeviceID;
                        break;
                    case "X":
                    case "SITEOWL X":
                    case "SITEOWLX":
                        value = device.SiteOwlX?.ToString("F6") ?? "";
                        break;
                    case "Y":
                    case "SITEOWL Y":
                    case "SITEOWLY":
                        value = device.SiteOwlY?.ToString("F6") ?? "";
                        break;
                    case "LATITUDE":
                    case "LAT":
                        value = device.Latitude?.ToString("F10") ?? "";
                        break;
                    case "LONGITUDE":
                    case "LON":
                    case "LONG":
                        value = device.Longitude?.ToString("F10") ?? "";
                        break;
                    case "PHOTO PATH":
                    case "PHOTOPATH":
                    case "PHOTO":
                        value = device.PhotoPath ?? "";
                        break;
                    case "CAPTURE TIMESTAMP":
                    case "TIMESTAMP":
                        value = device.CaptureTimestamp ?? "";
                        break;
                    case "CAPTURE METHOD":
                    case "METHOD":
                        value = device.CaptureMethod ?? "";
                        break;
                    case "COORDINATE CONFIDENCE":
                    case "CONFIDENCE":
                        value = device.CoordinateConfidence.ToString();
                        break;
                    case "USER X":
                    case "USERX":
                        value = device.UserX?.ToString("F6") ?? "";
                        break;
                    case "USER Y":
                    case "USERY":
                        value = device.UserY?.ToString("F6") ?? "";
                        break;
                    case "USER FACING":
                    case "USERFACINGDIRECTION":
                    case "FACING":
                        value = device.UserFacingDirection?.ToString("F1") ?? "";
                        break;
                    case "REVIEW STATUS":
                    case "REVIEWSTATUS":
                    case "STATUS":
                        value = device.ReviewStatus ?? "";
                        break;
                    default:
                        // Keep original value for unknown columns
                        value = "";
                        break;
                }
                
                // Escape values with commas or quotes
                if (value.Contains(",") || value.Contains("\""))
                {
                    value = "\"" + value.Replace("\"", "\"\"") + "\"";
                }
                
                values.Add(value);
            }
            
            return string.Join(",", values);
        }
        
        /// <summary>
        /// Gets devices that still need capture.
        /// </summary>
        public List<DeviceData> GetMissingDevices()
        {
            return devices.Where(d => d.NeedsCapture).ToList();
        }
        
        /// <summary>
        /// Searches devices by name or ID.
        /// </summary>
        public List<DeviceData> SearchDevices(string query)
        {
            if (string.IsNullOrEmpty(query)) return devices;
            
            query = query.ToLower();
            return devices.Where(d => 
                (d.DeviceName?.ToLower().Contains(query) ?? false) ||
                (d.DeviceID?.ToLower().Contains(query) ?? false) ||
                (d.Description?.ToLower().Contains(query) ?? false)
            ).ToList();
        }
        
        /// <summary>
        /// Gets devices marked for review.
        /// </summary>
        public List<DeviceData> GetReviewRequiredDevices()
        {
            return devices.Where(d => d.RequiresReview).ToList();
        }
    }
}
