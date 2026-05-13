using System;
using UnityEngine;

namespace SiteOwlXR.Models
{
    /// <summary>
    /// A GPS reading with accuracy and timestamp
    /// </summary>
    [Serializable]
    public struct GpsReading
    {
        public double Latitude;
        public double Longitude;
        public double Altitude;
        public float Accuracy;           // Horizontal accuracy in meters
        public float VerticalAccuracy;   // Vertical accuracy in meters
        public DateTime Timestamp;
        public double UnixTimestamp;     // For JSON serialization
        
        public bool IsValid => Latitude != 0 && Longitude != 0 && Accuracy < 1000;
        public bool IsHighConfidence => Accuracy < 3f;
        public bool IsMediumConfidence => Accuracy >= 3f && Accuracy < 10f;
        public bool IsLowConfidence => Accuracy >= 10f;
        
        public CoordinateConfidence GetConfidence()
        {
            if (Accuracy < 3f) return CoordinateConfidence.HIGH;
            if (Accuracy < 10f) return CoordinateConfidence.MEDIUM;
            return CoordinateConfidence.LOW;
        }
        
        /// <summary>
        /// Returns "lat,lon" format for SiteOwl Barcode field
        /// </summary>
        public string ToBarcodeFormat()
        {
            return $"{Latitude:F6},{Longitude:F6}";
        }
        
        public override string ToString()
        {
            return $"GPS: {Latitude:F6}, {Longitude:F6} (±{Accuracy:F1}m) {GetConfidence()}";
        }
        
        /// <summary>
        /// Calculate distance to another GPS reading (Haversine formula)
        /// </summary>
        public double DistanceTo(GpsReading other)
        {
            const double R = 6371000; // Earth radius in meters
            
            double lat1Rad = Latitude * Math.PI / 180;
            double lat2Rad = other.Latitude * Math.PI / 180;
            double deltaLat = (other.Latitude - Latitude) * Math.PI / 180;
            double deltaLon = (other.Longitude - Longitude) * Math.PI / 180;
            
            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
            
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return R * c;
        }
        
        /// <summary>
        /// Create from Unity's LocationInfo
        /// </summary>
        public static GpsReading FromLocationInfo(LocationInfo location)
        {
            return new GpsReading
            {
                Latitude = location.latitude,
                Longitude = location.longitude,
                Altitude = location.altitude,
                Accuracy = location.horizontalAccuracy,
                VerticalAccuracy = location.verticalAccuracy,
                Timestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddSeconds(location.timestamp),
                UnixTimestamp = location.timestamp
            };
        }
    }
}
