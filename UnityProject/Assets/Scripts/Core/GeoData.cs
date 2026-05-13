using System;
using UnityEngine;

namespace SiteOwlXR.Core
{
    /// <summary>
    /// Geographic data structure for real-world coordinates.
    /// Uses standard WGS 84 (EPSG:4326) coordinate system.
    /// </summary>
    [Serializable]
    public class GeoData
    {
        // REAL GPS coordinates (WGS 84)
        public double Latitude;      // -90 to 90
        public double Longitude;     // -180 to 180
        public double Altitude;      // meters above sea level
        
        // Local SiteOwl coordinates (for reference)
        public float SiteOwlX;
        public float SiteOwlY;
        
        // GPS quality metrics
        public float GpsAccuracy;           // Horizontal accuracy in meters
        public float GpsVerticalAccuracy;   // Vertical accuracy in meters
        public string GpsTimestamp;         // ISO 8601 format
        public GpsQuality GpsQuality;
        
        // CRS information (Coordinate Reference System)
        public string CrsName = "WGS 84";
        public string CrsEpsg = "EPSG:4326";
        
        // Capture context
        public string CaptureMethod = "GPS_SATELLITE";
        public string DeviceName;
        public string SiteId;
        
        /// <summary>
        /// Creates GeoData from a GPS capture.
        /// </summary>
        public static GeoData FromGpsCapture(
            double lat, 
            double lon, 
            float accuracy,
            string deviceName,
            string siteId,
            float siteOwlX = 0,
            float siteOwlY = 0)
        {
            return new GeoData
            {
                Latitude = lat,
                Longitude = lon,
                GpsAccuracy = accuracy,
                GpsQuality = accuracy <= 3 ? GpsQuality.Excellent :
                            accuracy <= 10 ? GpsQuality.Good :
                            accuracy <= 30 ? GpsQuality.Fair : GpsQuality.Poor,
                GpsTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                DeviceName = deviceName,
                SiteId = siteId,
                SiteOwlX = siteOwlX,
                SiteOwlY = siteOwlY
            };
        }
        
        /// <summary>
        /// Returns true if GPS coordinates are valid.
        /// </summary>
        public bool IsValid()
        {
            return Latitude >= -90 && Latitude <= 90 &&
                   Longitude >= -180 && Longitude <= 180 &&
                   GpsAccuracy < 1000; // Sanity check
        }
        
        /// <summary>
        /// Gets formatted coordinate string for display.
        /// </summary>
        public string GetCoordinateString()
        {
            return $"{Latitude:F6}°N, {Longitude:F6}°W (±{GpsAccuracy:F1}m)";
        }
        
        /// <summary>
        /// Exports to CSV row format for QGIS/SiteOwl.
        /// </summary>
        public string ToCsvRow()
        {
            return $"{SiteId},{DeviceName},{Latitude:F10},{Longitude:F10},{Altitude:F2},{GpsAccuracy:F2},{GpsQuality},{GpsTimestamp},{SiteOwlX:F2},{SiteOwlY:F2},{CrsEpsg}";
        }
        
        /// <summary>
        /// Creates a KML placemark string for Google Earth.
        /// </summary>
        public string ToKmlPlacemark()
        {
            return $@"<Placemark>
  <name>{DeviceName}</name>
  <description>
    Site: {SiteId}
    Accuracy: {GpsAccuracy:F1}m
    Quality: {GpsQuality}
    SiteOwl: ({SiteOwlX:F2}, {SiteOwlY:F2})
  </description>
  <Point>
    <coordinates>{Longitude},{Latitude},{Altitude}</coordinates>
  </Point>
</Placemark>";
        }
        
        /// <summary>
        /// Creates GeoJSON feature for web maps.
        /// </summary>
        public string ToGeoJson()
        {
            return $@"{{
  ""type"": ""Feature"",
  ""geometry"": {{
    ""type"": ""Point"",
    ""coordinates"": [{Longitude}, {Latitude}, {Altitude}]
  }},
  ""properties"": {{
    ""name"": ""{DeviceName}"",
    ""site"": ""{SiteId}"",
    ""accuracy"": {GpsAccuracy},
    ""quality"": ""{GpsQuality}"",
    ""siteowl_x"": {SiteOwlX},
    ""siteowl_y"": {SiteOwlY},
    ""timestamp"": ""{GpsTimestamp}""
  }}
}}";
        }
    }
    
    public enum GpsQuality
    {
        Unavailable,
        Poor,       // > 30m
        Fair,       // 10-30m
        Good,       // 3-10m
        Excellent   // < 3m
    }
}
