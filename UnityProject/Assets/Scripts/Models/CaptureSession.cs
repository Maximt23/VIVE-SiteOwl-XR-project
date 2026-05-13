using System;
using System.Collections.Generic;

namespace SiteOwlXR.Models
{
    /// <summary>
    /// Represents a complete survey session
    /// </summary>
    [Serializable]
    public class CaptureSession
    {
        public string SessionId;
        public string SiteId;
        public string SiteName;
        public string SurveyorId;
        public string SurveyorName;
        
        public DateTime StartTime;
        public DateTime? EndTime;
        
        public List<DeviceRecord> Devices;
        public List<string> CapturedDeviceIds;
        public List<string> RemainingDeviceIds;
        
        public int ExpectedDeviceCount;
        public int CapturedCount => CapturedDeviceIds?.Count ?? 0;
        public bool IsComplete => CapturedCount >= ExpectedDeviceCount;
        
        public CalibrationAnchor Anchor;
        public GpsReading LastGpsReading;
        
        public string SyncStatus;  // "local_only", "synced", "conflict"
        public DateTime? LastSyncTime;
        
        public List<string> Errors;
        public List<string> Warnings;
        
        public CaptureSession()
        {
            SessionId = Guid.NewGuid().ToString();
            StartTime = DateTime.UtcNow;
            Devices = new List<DeviceRecord>();
            CapturedDeviceIds = new List<string>();
            RemainingDeviceIds = new List<string>();
            Errors = new List<string>();
            Warnings = new List<string>();
            SyncStatus = "local_only";
        }
        
        public void MarkDeviceCaptured(string deviceId)
        {
            if (!CapturedDeviceIds.Contains(deviceId))
            {
                CapturedDeviceIds.Add(deviceId);
                RemainingDeviceIds.Remove(deviceId);
            }
        }
        
        public void EndSession()
        {
            EndTime = DateTime.UtcNow;
        }
        
        public TimeSpan GetDuration()
        {
            if (EndTime.HasValue)
                return EndTime.Value - StartTime;
            return DateTime.UtcNow - StartTime;
        }
        
        public override string ToString()
        {
            return $"Session {SessionId.Substring(0, 8)}: {CapturedCount}/{ExpectedDeviceCount} captured, {GetDuration().TotalMinutes:F1} min";
        }
    }
}
