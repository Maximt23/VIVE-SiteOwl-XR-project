using System;
using UnityEngine;

namespace SiteOwlXR.Core
{
    /// <summary>
    /// Represents a device record from SiteOwl CSV.
    /// This is our internal data model - matches SiteOwl schema.
    /// </summary>
    [Serializable]
    public class DeviceData
    {
        // From SiteOwl (READ-ONLY in our app)
        public string DeviceName;
        public string DeviceType;
        public string SystemType;
        public string Description;
        public string DeviceID;  // Unique identifier
        
        // Coordinate fields (we update these)
        public float? SiteOwlX;  // Nullable - null means needs capture
        public float? SiteOwlY;
        public double? Latitude;  // GPS - secondary
        public double? Longitude;
        
        // Capture metadata (we add these)
        public string PhotoPath;
        public string CaptureTimestamp;
        public string CaptureMethod;
        public CoordinateConfidence CoordinateConfidence;
        public float? UserX;  // User position at time of capture
        public float? UserY;
        public float? UserFacingDirection;
        public string ReviewStatus;  // OK or REVIEW_REQUIRED
        
        // UI helpers (not persisted)
        public bool NeedsCapture => !SiteOwlX.HasValue || !SiteOwlY.HasValue;
        public bool HasPhoto => !string.IsNullOrEmpty(PhotoPath);
        public bool RequiresReview => ReviewStatus == "REVIEW_REQUIRED";
        
        /// <summary>
        /// Marks this device for review if accuracy is low.
        /// </summary>
        public void UpdateReviewStatus()
        {
            if (CoordinateConfidence == CoordinateConfidence.LOW || 
                CoordinateConfidence == CoordinateConfidence.NONE)
            {
                ReviewStatus = "REVIEW_REQUIRED";
            }
            else
            {
                ReviewStatus = "OK";
            }
        }
        
        /// <summary>
        /// Creates a capture record with all required fields.
        /// </summary>
        public void RecordCapture(
            float siteOwlX, 
            float siteOwlY,
            double? latitude,
            double? longitude,
            string photoPath,
            CoordinateConfidence confidence,
            float userX,
            float userY,
            float userFacing)
        {
            SiteOwlX = siteOwlX;
            SiteOwlY = siteOwlY;
            Latitude = latitude;
            Longitude = longitude;
            PhotoPath = photoPath;
            CaptureTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            CaptureMethod = "XR_CAPTURE";
            CoordinateConfidence = confidence;
            UserX = userX;
            UserY = userY;
            UserFacingDirection = userFacing;
            
            UpdateReviewStatus();
        }
        
        public override string ToString()
        {
            return $"[{DeviceID}] {DeviceName} ({DeviceType}) - X:{SiteOwlX?.ToString("F2") ?? "MISSING"}, Y:{SiteOwlY?.ToString("F2") ?? "MISSING"}";
        }
    }
}
