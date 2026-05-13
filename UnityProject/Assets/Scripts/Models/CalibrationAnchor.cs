using System;
using UnityEngine;

namespace SiteOwlXR.Models
{
    /// <summary>
    /// A calibration anchor that transforms Unity world space to SiteOwl X/Y
    /// </summary>
    [Serializable]
    public struct CalibrationAnchor
    {
        // Known real-world position
        public float SiteOwlX;
        public float SiteOwlY;
        public double Latitude;
        public double Longitude;
        
        // Unity world space position at time of calibration
        public Vector3 WorldPosition;
        public Quaternion WorldRotation;
        public float WorldRotationY;  // Simplified: just Y rotation offset
        
        // Scale factor (meters per SiteOwl unit)
        public float MetersPerUnit;
        
        // When this was set
        public DateTime Timestamp;
        public string SetByUser;
        
        public bool IsValid => MetersPerUnit > 0;
        
        /// <summary>
        /// Convert Unity world position to SiteOwl X/Y
        /// </summary>
        public Vector2 WorldToSiteOwl(Vector3 worldPos)
        {
            if (!IsValid)
                return Vector2.zero;
            
            // Offset from anchor
            Vector3 offset = worldPos - WorldPosition;
            offset.y = 0; // Ignore height
            
            // Apply rotation to align with SiteOwl coordinate system
            Quaternion rotation = Quaternion.Euler(0, -WorldRotationY, 0);
            Vector3 rotatedOffset = rotation * offset;
            
            // Convert to SiteOwl units
            float siteOwlX = SiteOwlX + (rotatedOffset.x / MetersPerUnit);
            float siteOwlY = SiteOwlY + (rotatedOffset.z / MetersPerUnit);
            
            return new Vector2(siteOwlX, siteOwlY);
        }
        
        /// <summary>
        /// Convert SiteOwl X/Y to approximate GPS (very rough estimate)
        /// </summary>
        public (double lat, double lon) SiteOwlToGps(float siteOwlX, float siteOwlY)
        {
            // Calculate offset from anchor in SiteOwl units
            float deltaX = siteOwlX - SiteOwlX;
            float deltaY = siteOwlY - SiteOwlY;
            
            // Convert to meters
            float metersX = deltaX * MetersPerUnit;
            float metersY = deltaY * MetersPerUnit;
            
            // Rough conversion: 1 degree latitude = ~111km
            // Longitude varies by latitude
            double latOffset = metersY / 111000.0;
            double lonOffset = metersX / (111000.0 * Math.Cos(Latitude * Math.PI / 180));
            
            return (Latitude + latOffset, Longitude + lonOffset);
        }
        
        /// <summary>
        /// Get distance in meters from the anchor
        /// </summary>
        public float GetDistanceFromAnchor(Vector3 worldPos)
        {
            return Vector3.Distance(worldPos, WorldPosition);
        }
        
        /// <summary>
        /// Get confidence level based on distance from anchor
        /// </summary>
        public CoordinateConfidence GetConfidence(Vector3 worldPos)
        {
            float distance = GetDistanceFromAnchor(worldPos);
            
            if (distance < 10f) return CoordinateConfidence.HIGH;
            if (distance < 30f) return CoordinateConfidence.MEDIUM;
            return CoordinateConfidence.LOW;
        }
        
        public override string ToString()
        {
            return $"Anchor: SiteOwl({SiteOwlX:F2}, {SiteOwlY:F2}) @ World{WorldPosition} Scale:{MetersPerUnit:F2}m/unit";
        }
    }
}
