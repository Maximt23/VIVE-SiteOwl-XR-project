using System;
using System.Collections.Generic;

namespace SiteOwlXR.Models
{
    /// <summary>
    /// Represents one row from the SiteOwl CSV.
    /// All fields map directly to CSV column names.
    /// PROTECTED fields are read-only after load (collaborator-owned).
    /// </summary>
    [Serializable]
    public class DeviceRecord
    {
        // --- PROTECTED (collaborator-owned, never overwrite) ---
        public string Site;
        public string DeviceName;
        public string DeviceType;
        public string SystemType;
        public string Description;
        public string IpAddress;
        public string VmsZone;
        public string Manufacturer;
        public string Model;

        // --- XR-WRITABLE (we fill these in the field) ---
        public string Barcode;          // GPS "lat,lon"
        public float? SiteOwlX;
        public float? SiteOwlY;
        public string PhotoUrl;
        public string PhotoPath;
        public DateTime? CaptureTimestamp;
        public string CaptureMethod;    // "XR_CAPTURE"
        public CoordinateConfidence Confidence;
        public float? UserX;
        public float? UserY;
        public float? UserFacingDegrees;
        public ReviewStatus ReviewStatus;

        // --- RUNTIME STATE (not in CSV) ---
        public bool IsCaptured => !string.IsNullOrEmpty(Barcode) || SiteOwlX.HasValue;
        public bool HasPhoto => !string.IsNullOrEmpty(PhotoUrl);
        public bool NeedsReview => ReviewStatus == ReviewStatus.REVIEW_REQUIRED;
        public int RowIndex;            // Original CSV row index for safe write-back
        public Dictionary<string, string> AllColumns; // All raw CSV columns for safe round-trip
        
        public override string ToString()
        {
            return $"[{Site}] {DeviceName} ({DeviceType}) - Captured: {IsCaptured}, Review: {ReviewStatus}";
        }
    }
    
    public enum CoordinateConfidence
    {
        NONE,      // No data
        LOW,       // > 3m accuracy
        MEDIUM,    // 1-3m accuracy
        HIGH       // < 1m accuracy
    }
    
    public enum ReviewStatus
    {
        PENDING,           // Not yet captured
        OK,                // Good capture
        REVIEW_REQUIRED    // Low confidence or error
    }
}
