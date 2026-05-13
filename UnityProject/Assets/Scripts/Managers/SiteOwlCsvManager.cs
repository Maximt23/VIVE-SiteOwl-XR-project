using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SiteOwlXR.Models;
using SiteOwlXR.Events;
using SiteOwlXR.Services;

namespace SiteOwlXR.Managers
{
    /// <summary>
    /// SiteOwl CSV Manager - Enterprise Grade
    /// Handles CSV read/write with collaborator data protection and backup
    /// </summary>
    public class SiteOwlCsvManager : MonoBehaviour
    {
        [Header("Configuration")]
        public string csvFileName = "cameras.csv";
        public string backupFolder = "Backups";
        
        [Header("Protected Columns (Never Overwrite)")]
        public string[] ProtectedColumns = new[] {
            "Site", "Site ID", "SITE", "SITE ID",
            "Device Name", "Name", "DEVICE NAME", "NAME",
            "Device Type", "Type", "DEVICE TYPE", "TYPE",
            "System Type", "System", "SYSTEM TYPE", "SYSTEM",
            "Description", "Desc", "DESCRIPTION", "DESC",
            "IP Address", "IP", "IP_ADDRESS", "IPADDRESS",
            "VMS Zone", "Zone", "VMS_ZONE", "ZONE",
            "Manufacturer", "Make", "MANUFACTURER", "MAKE",
            "Model", "MODEL"
        };
        
        [Header("State")]
        public List<DeviceRecord> Devices { get; private set; } = new List<DeviceRecord>();
        public int TotalCount => Devices.Count;
        public int CapturedCount => Devices.Count(d => d.IsCaptured);
        public int MissingCount => Devices.Count(d => !d.IsCaptured);
        
        private ILogger _logger;
        private string _csvPath;
        private string[] _originalLines;
        private List<string> _headers;
        
        void Awake()
        {
            _logger = ServiceLocator.Get<ILogger>() ?? new UnityLogger();
        }
        
        void Start()
        {
            _csvPath = Path.Combine(Application.streamingAssetsPath, csvFileName);
            LoadCsv();
        }
        
        /// <summary>
        /// Load CSV from StreamingAssets
        /// </summary>
        public bool LoadCsv()
        {
            if (!File.Exists(_csvPath))
            {
                _logger.LogError($"CSV not found: {_csvPath}", null, this);
                return false;
            }
            
            try
            {
                _originalLines = File.ReadAllLines(_csvPath);
                ParseCsv();
                
                _logger.LogInfo($"Loaded {Devices.Count} devices ({CapturedCount} captured, {MissingCount} missing)", this);
                CctvSurveyEvents.RaiseDevicesLoaded(Devices.Count);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load CSV: {ex.Message}", ex, this);
                CctvSurveyEvents.RaiseCsvError(ex.Message);
                return false;
            }
        }
        
        void ParseCsv()
        {
            Devices.Clear();
            
            if (_originalLines.Length == 0) return;
            
            // Parse headers
            _headers = ParseLine(_originalLines[0]);
            
            // Parse rows
            for (int i = 1; i < _originalLines.Length; i++)
            {
                var record = ParseRecord(_originalLines[i], i);
                if (record != null)
                {
                    Devices.Add(record);
                }
            }
        }
        
        DeviceRecord ParseRecord(string line, int rowIndex)
        {
            var values = ParseLine(line);
            if (values.Count != _headers.Count)
            {
                _logger.LogWarning($"Row {rowIndex} has {values.Count} columns, expected {_headers.Count}", this);
                return null;
            }
            
            var record = new DeviceRecord
            {
                RowIndex = rowIndex,
                AllColumns = new Dictionary<string, string>()
            };
            
            // Store all raw columns
            for (int i = 0; i < _headers.Count; i++)
            {
                record.AllColumns[_headers[i]] = values[i];
            }
            
            // Map known columns
            for (int i = 0; i < _headers.Count; i++)
            {
                string header = _headers[i].ToUpper();
                string value = values[i];
                
                // Protected columns (read-only)
                if (header == "SITE" || header == "SITE ID") record.Site = value;
                else if (header == "DEVICE NAME" || header == "NAME") record.DeviceName = value;
                else if (header == "DEVICE TYPE" || header == "TYPE" || header == "CAMERA TYPE") record.DeviceType = value;
                else if (header == "SYSTEM TYPE" || header == "SYSTEM") record.SystemType = value;
                else if (header == "DESCRIPTION" || header == "DESC") record.Description = value;
                else if (header == "IP ADDRESS" || header == "IP") record.IpAddress = value;
                else if (header == "VMS ZONE" || header == "ZONE") record.VmsZone = value;
                else if (header == "MANUFACTURER" || header == "MAKE") record.Manufacturer = value;
                else if (header == "MODEL") record.Model = value;
                
                // Our columns (may be empty initially)
                else if (header == "BARCODE" || header == "BAR CODE") record.Barcode = value;
                else if (header == "X" || header == "SITEOWL X") ParseFloat(value, v => record.SiteOwlX = v);
                else if (header == "Y" || header == "SITEOWL Y") ParseFloat(value, v => record.SiteOwlY = v);
                else if (header.Contains("PHOTO URL") || header == "PHOTOURL") record.PhotoUrl = value;
                else if (header.Contains("PHOTO PATH") || header == "PHOTOPATH") record.PhotoPath = value;
            }
            
            return record;
        }
        
        void ParseFloat(string value, Action<float?> setter)
        {
            if (float.TryParse(value, out float result))
                setter(result);
        }
        
        List<string> ParseLine(string line)
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
                    result.Add(current.Trim());
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            
            result.Add(current.Trim());
            return result;
        }
        
        /// <summary>
        /// Update a device record and save CSV
        /// </summary>
        public void UpdateDevice(DeviceRecord device)
        {
            // Find existing
            var existing = Devices.FirstOrDefault(d => d.DeviceName == device.DeviceName);
            if (existing == null)
            {
                _logger.LogError($"Device not found: {device.DeviceName}", null, this);
                return;
            }
            
            // Update in list
            int index = Devices.IndexOf(existing);
            Devices[index] = device;
            
            // Save
            SaveCsv();
            
            CctvSurveyEvents.RaiseDeviceUpdated(device);
        }
        
        /// <summary>
        /// Save CSV with backup
        /// </summary>
        public bool SaveCsv()
        {
            // Create backup
            if (!CreateBackup())
            {
                _logger.LogError("Backup failed, aborting save", this);
                return false;
            }
            
            try
            {
                var lines = new List<string>();
                lines.Add(_originalLines[0]); // Header
                
                for (int i = 1; i < _originalLines.Length; i++)
                {
                    var device = Devices.FirstOrDefault(d => d.RowIndex == i);
                    if (device != null)
                    {
                        lines.Add(BuildRow(device));
                    }
                    else
                    {
                        lines.Add(_originalLines[i]); // Unchanged row
                    }
                }
                
                string outputPath = Path.Combine(Application.persistentDataPath, "Output", csvFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                File.WriteAllLines(outputPath, lines);
                
                _logger.LogInfo($"CSV saved: {outputPath}", this);
                CctvSurveyEvents.RaiseCsvSaved(outputPath);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Save failed: {ex.Message}", ex, this);
                CctvSurveyEvents.RaiseCsvError(ex.Message);
                return false;
            }
        }
        
        string BuildRow(DeviceRecord device)
        {
            var values = new List<string>();
            
            foreach (var header in _headers)
            {
                string value = "";
                string headerUpper = header.ToUpper();
                
                // Our columns - write our data
                if (headerUpper == "BARCODE" || headerUpper == "BAR CODE")
                    value = device.Barcode ?? "";
                else if (headerUpper == "X" || headerUpper == "SITEOWL X")
                    value = device.SiteOwlX?.ToString("F2") ?? "";
                else if (headerUpper == "Y" || headerUpper == "SITEOWL Y")
                    value = device.SiteOwlY?.ToString("F2") ?? "";
                else if (headerUpper.Contains("PHOTO URL") || headerUpper == "PHOTOURL")
                    value = device.PhotoUrl ?? "";
                else if (headerUpper.Contains("PHOTO PATH") || headerUpper == "PHOTOPATH")
                    value = device.PhotoPath ?? "";
                else if (headerUpper.Contains("TIMESTAMP") || headerUpper == "CAPTURE TIMESTAMP")
                    value = device.CaptureTimestamp?.ToString("O") ?? "";
                else if (headerUpper.Contains("METHOD") || headerUpper == "CAPTURE METHOD")
                    value = device.CaptureMethod ?? "";
                else if (headerUpper.Contains("CONFIDENCE") || headerUpper == "COORDINATE CONFIDENCE")
                    value = device.Confidence.ToString();
                else if (headerUpper.Contains("REVIEW") || headerUpper == "REVIEW STATUS" || headerUpper == "STATUS")
                    value = device.ReviewStatus.ToString();
                // Protected columns - preserve original
                else if (device.AllColumns.TryGetValue(header, out string originalValue))
                    value = originalValue;
                
                // Escape if needed
                if (value.Contains(",") || value.Contains("\""))
                    value = "\"" + value.Replace("\"", "\"\"") + "\"";
                
                values.Add(value);
            }
            
            return string.Join(",", values);
        }
        
        bool CreateBackup()
        {
            try
            {
                string backupDir = Path.Combine(Application.persistentDataPath, backupFolder);
                Directory.CreateDirectory(backupDir);
                
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(backupDir, $"{Path.GetFileNameWithoutExtension(csvFileName)}_backup_{timestamp}.csv");
                
                File.Copy(_csvPath, backupPath);
                
                _logger.LogInfo($"Backup created: {backupPath}", this);
                CctvSurveyEvents.RaiseBackupCreated(backupPath);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Backup failed: {ex.Message}", ex, this);
                return false;
            }
        }
        
        public List<DeviceRecord> GetMissingDevices()
        {
            return Devices.Where(d => !d.IsCaptured).ToList();
        }
    }
}
