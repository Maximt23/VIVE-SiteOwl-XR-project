using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FlatBuffers;
using CCTVSurvey;  // Generated from .fbs schema

namespace SiteOwlXR.Memory
{
    /// <summary>
    /// Survey Memory Manager - Bridges FlatBuffers + MemoryBear
    /// 
    /// Handles:
    /// - Fast binary serialization (FlatBuffers)
    /// - AI contextual memory (MemoryBear integration)
    /// - Spatial relationships between cameras
    /// - Site pattern learning
    /// </summary>
    public class SurveyMemoryManager : MonoBehaviour
    {
        [Header("FlatBuffers Storage")]
        public string surveyDataPath = "SurveyData";
        public bool useCompression = true;  // GZip compression for storage
        
        [Header("MemoryBear Integration")]
        public bool useMemoryBear = false;
        public string memoryBearEndpoint = "http://localhost:8080";  // MemoryBear server
        public string apiKey = "";
        
        [Header("Current Session")]
        public SurveySession currentSession;
        public string currentSessionId;
        
        [Header("Performance")]
        public int maxCacheSize = 100;  // Keep last N cameras in memory
        private Queue<string> cameraCache = new Queue<string>();
        private Dictionary<string, CameraCapture> cameraLookup = new Dictionary<string, CameraCapture>();
        
        private HttpClient httpClient;
        
        void Start()
        {
            httpClient = new HttpClient();
            
            // Ensure directory exists
            string fullPath = Path.Combine(Application.persistentDataPath, surveyDataPath);
            Directory.CreateDirectory(fullPath);
            
            Debug.Log("[SurveyMemoryManager] Initialized");
            Debug.Log($"  Storage: {fullPath}");
            Debug.Log($"  MemoryBear: {(useMemoryBear ? "Enabled" : "Disabled")}");
        }
        
        #region Session Management
        
        /// <summary>
        /// Starts a new survey session
        /// </summary>
        public async Task StartSession(string siteId, string siteName, string surveyorId, int expectedCameras)
        {
            currentSessionId = Guid.NewGuid().ToString();
            
            // Build FlatBuffer
            var builder = new FlatBufferBuilder(1024);
            
            // Create session
            var sessionIdOffset = builder.CreateString(currentSessionId);
            var siteIdOffset = builder.CreateString(siteId);
            var surveyorOffset = builder.CreateString(surveyorId);
            var startTimeOffset = builder.CreateString(DateTime.UtcNow.ToString("O"));
            
            // Load or create site context
            var siteContext = await LoadSiteContext(siteId);
            var contextOffset = siteContext != null ? 
                SerializeSiteContext(builder, siteContext) : 
                CreateEmptySiteContext(builder, siteId, siteName);
            
            // Create session object
            var session = SurveySession.CreateSurveySession(builder,
                sessionIdOffset,
                siteIdOffset,
                contextOffset,
                surveyorOffset,
                builder.CreateString(""), // surveyor_name - add param if needed
                startTimeOffset,
                builder.CreateString(""), // end_time
                expectedCameras,
                SurveySession.CreateCamerasCapturedVector(builder, new Offset<CameraCapture>[0]),
                SurveySession.CreateCamerasRemainingVector(builder, new StringOffset[0]),
                builder.CreateString(DateTime.UtcNow.ToString("O")),
                builder.CreateString("local_only"),
                builder.CreateString(""),
                SurveySession.CreatePatternDiscoveriesVector(builder, new StringOffset[0])
            );
            
            builder.Finish(session.Value);
            
            // Store in memory
            byte[] buffer = builder.SizedByteArray();
            currentSession = SurveySession.GetRootAsSurveySession(new ByteBuffer(buffer));
            
            // Save to disk
            await SaveSessionAsync();
            
            Debug.Log($"[SurveyMemoryManager] Session started: {currentSessionId}");
            Debug.Log($"  Site: {siteId} ({expectedCameras} cameras expected)");
        }
        
        /// <summary>
        /// Adds a camera capture to memory (FlatBuffers + MemoryBear)
        /// </summary>
        public async Task AddCameraCapture(Core.CameraData camera, AIRecognition aiResult = null)
        {
            if (currentSession == null)
            {
                Debug.LogError("[SurveyMemoryManager] No active session");
                return;
            }
            
            // Build FlatBuffer
            var builder = new FlatBufferBuilder(1024);
            var capture = BuildCameraCapture(builder, camera, aiResult);
            builder.Finish(capture.Value);
            
            // Deserialize and add to session
            byte[] buffer = builder.SizedByteArray();
            var newCapture = CameraCapture.GetRootAsCameraCapture(new ByteBuffer(buffer));
            
            // Add to lookup cache
            string camId = newCapture.CameraId;
            cameraLookup[camId] = newCapture;
            cameraCache.Enqueue(camId);
            
            // Trim cache if needed
            while (cameraCache.Count > maxCacheSize)
            {
                string oldId = cameraCache.Dequeue();
                cameraLookup.Remove(oldId);
            }
            
            // Analyze spatial relationships (MemoryBear)
            var relationships = await AnalyzeSpatialRelationships(newCapture);
            
            // Update session
            await UpdateSessionWithCapture(newCapture, relationships);
            
            // Save
            await SaveSessionAsync();
            
            // Sync to MemoryBear if enabled
            if (useMemoryBear)
            {
                await SyncToMemoryBear(newCapture);
            }
            
            Debug.Log($"[SurveyMemoryManager] Added camera: {camId}");
        }
        
        /// <summary>
        /// Builds a CameraCapture FlatBuffer object
        /// </summary>
        private Offset<CameraCapture> BuildCameraCapture(FlatBufferBuilder builder, Core.CameraData camera, AIRecognition aiResult)
        {
            // String offsets
            var idOffset = builder.CreateString(camera.DeviceId);
            var nameOffset = builder.CreateString(camera.DeviceName ?? "");
            var typeOffset = builder.CreateString(camera.DeviceType ?? "");
            var systemOffset = builder.CreateString(camera.SystemType ?? "CCTV");
            var descOffset = builder.CreateString(camera.Description ?? "");
            var ipOffset = builder.CreateString(camera.IpAddress ?? "");
            var zoneOffset = builder.CreateString(camera.VmsZone ?? "");
            var mfgOffset = builder.CreateString(camera.Manufacturer ?? "");
            var modelOffset = builder.CreateString(camera.Model ?? "");
            var serialOffset = builder.CreateString(camera.SerialNumber ?? "");
            var methodOffset = builder.CreateString(camera.CaptureMethod ?? "XR_CAPTURE");
            var timestampOffset = builder.CreateString(camera.CaptureTimestamp ?? DateTime.UtcNow.ToString("O"));
            var surveyorOffset = builder.CreateString("surveyor_001"); // Add param if needed
            var statusOffset = builder.CreateString(camera.ReviewStatus ?? "OK");
            
            // GPS
            var gps = GPSCoordinates.CreateGPSCoordinates(builder,
                camera.Latitude ?? 0,
                camera.Longitude ?? 0,
                camera.GpsAccuracy ?? 999f,
                camera.Altitude ?? 0
            );
            
            // SiteOwl coordinates
            var siteowl = SiteOwlCoordinates.CreateSiteOwlCoordinates(builder,
                camera.SiteOwlX ?? 0,
                camera.SiteOwlY ?? 0,
                0
            );
            
            // Photo metadata
            var photoPathOffset = builder.CreateString(camera.PhotoPath ?? "");
            var photoUrlOffset = builder.CreateString(camera.PhotoUrl ?? "");
            var photoTsOffset = builder.CreateString(camera.CaptureTimestamp ?? "");
            var hashVector = CameraCapture.CreatePhotoHashVector(builder, new byte[0]);
            
            PhotoMetadata.StartPhotoMetadata(builder);
            PhotoMetadata.AddLocalPath(builder, photoPathOffset);
            PhotoMetadata.AddExternalUrl(builder, photoUrlOffset);
            PhotoMetadata.AddFileHash(builder, hashVector);
            PhotoMetadata.AddFileSize(builder, 0);
            PhotoMetadata.AddWidth(builder, 1920);
            PhotoMetadata.AddHeight(builder, 1080);
            PhotoMetadata.AddTimestamp(builder, photoTsOffset);
            var photo = PhotoMetadata.EndPhotoMetadata(builder);
            
            // AI Recognition (optional)
            Offset<AIRecognition> ai = default;
            if (aiResult != null)
            {
                var suggestedOffset = builder.CreateString(aiResult.SuggestedType ?? "");
                var methodAiOffset = builder.CreateString(aiResult.RecognitionMethod ?? "");
                var reasoningOffset = builder.CreateString(aiResult.Reasoning ?? "");
                
                AIRecognition.StartAIRecognition(builder);
                AIRecognition.AddSuggestedType(builder, suggestedOffset);
                AIRecognition.AddConfidence(builder, aiResult.Confidence);
                AIRecognition.AddRecognitionMethod(builder, methodAiOffset);
                AIRecognition.AddReasoning(builder, reasoningOffset);
                ai = AIRecognition.EndAIRecognition(builder);
            }
            
            // Build CameraCapture
            CameraCapture.StartCameraCapture(builder);
            CameraCapture.AddCameraId(builder, idOffset);
            CameraCapture.AddDeviceName(builder, nameOffset);
            CameraCapture.AddDeviceType(builder, typeOffset);
            CameraCapture.AddSystemType(builder, systemOffset);
            CameraCapture.AddDescription(builder, descOffset);
            CameraCapture.AddIpAddress(builder, ipOffset);
            CameraCapture.AddVmsZone(builder, zoneOffset);
            CameraCapture.AddManufacturer(builder, mfgOffset);
            CameraCapture.AddModel(builder, modelOffset);
            CameraCapture.AddSerialNumber(builder, serialOffset);
            CameraCapture.AddGps(builder, gps);
            CameraCapture.AddSiteowlCoords(builder, siteowl);
            CameraCapture.AddPhoto(builder, photo);
            CameraCapture.AddCaptureTimestamp(builder, timestampOffset);
            CameraCapture.AddCaptureMethod(builder, methodOffset);
            CameraCapture.AddSurveyorId(builder, surveyorOffset);
            if (aiResult != null) CameraCapture.AddAiRecognition(builder, ai);
            CameraCapture.AddReviewStatus(builder, statusOffset);
            CameraCapture.AddQualityScore(builder, camera.GpsAccuracy.HasValue ? 1f - (camera.GpsAccuracy.Value / 30f) : 0.5f);
            
            return CameraCapture.EndCameraCapture(builder);
        }
        
        #endregion
        
        #region MemoryBear Integration
        
        private async Task<List<SpatialRelationship>> AnalyzeSpatialRelationships(CameraCapture newCamera)
        {
            var relationships = new List<SpatialRelationship>();
            
            // Compare with cached cameras
            foreach (var kvp in cameraLookup)
            {
                if (kvp.Key == newCamera.CameraId) continue;
                
                var other = kvp.Value;
                
                // Calculate distance if both have GPS
                if (newCamera.Gps.HasValue && other.Gps.HasValue)
                {
                    double dist = CalculateDistance(
                        newCamera.Gps.Value.Latitude, newCamera.Gps.Value.Longitude,
                        other.Gps.Value.Latitude, other.Gps.Value.Longitude
                    );
                    
                    if (dist < 50)  // Within 50 meters
                    {
                        // Calculate direction
                        double bearing = CalculateBearing(
                            newCamera.Gps.Value.Latitude, newCamera.Gps.Value.Longitude,
                            other.Gps.Value.Latitude, other.Gps.Value.Longitude
                        );
                        
                        relationships.Add(new SpatialRelationship
                        {
                            RelatedCameraId = other.CameraId,
                            DistanceMeters = (float)dist,
                            DirectionDegrees = (float)bearing,
                            RelationshipType = dist < 10 ? "adjacent" : "nearby"
                        });
                    }
                }
            }
            
            return relationships;
        }
        
        private async Task SyncToMemoryBear(CameraCapture capture)
        {
            if (!useMemoryBear) return;
            
            try
            {
                // Build MemoryBear memory entry
                var memory = new
                {
                    type = "camera_capture",
                    camera_id = capture.CameraId,
                    site_id = currentSession.SiteId,
                    gps = new { lat = capture.Gps.Value.Latitude, lon = capture.Gps.Value.Longitude },
                    camera_type = capture.DeviceType,
                    timestamp = capture.CaptureTimestamp,
                    context = capture.EnvironmentalContext?.Indoor == true ? "indoor" : "outdoor"
                };
                
                // Send to MemoryBear
                string json = JsonUtility.ToJson(memory);
                var content = new StringContent(json);
                
                await httpClient.PostAsync($"{memoryBearEndpoint}/api/memory", content);
                
                Debug.Log($"[SurveyMemoryManager] Synced to MemoryBear: {capture.CameraId}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SurveyMemoryManager] MemoryBear sync failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Queries MemoryBear for contextual information
        /// </summary>
        public async Task<string> QueryMemoryBear(string query)
        {
            if (!useMemoryBear) return "";
            
            try
            {
                var response = await httpClient.GetAsync(
                    $"{memoryBearEndpoint}/api/query?site={currentSession.SiteId}&q={Uri.EscapeDataString(query)}"
                );
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SurveyMemoryManager] MemoryBear query failed: {ex.Message}");
            }
            
            return "";
        }
        
        #endregion
        
        #region Persistence
        
        private async Task SaveSessionAsync()
        {
            if (currentSession == null) return;
            
            string path = GetSessionPath(currentSessionId);
            
            // Serialize current session
            var builder = new FlatBufferBuilder(1024);
            // ... rebuild full session with all cameras ...
            
            byte[] data = builder.SizedByteArray();
            
            // Optionally compress
            if (useCompression)
            {
                data = Compress(data);
            }
            
            await File.WriteAllBytesAsync(path, data);
            
            Debug.Log($"[SurveyMemoryManager] Session saved: {path} ({data.Length} bytes)");
        }
        
        private async Task<SurveySession> LoadSessionAsync(string sessionId)
        {
            string path = GetSessionPath(sessionId);
            
            if (!File.Exists(path))
                return null;
            
            byte[] data = await File.ReadAllBytesAsync(path);
            
            if (useCompression)
            {
                data = Decompress(data);
            }
            
            return SurveySession.GetRootAsSurveySession(new ByteBuffer(data));
        }
        
        private async Task<SiteContext> LoadSiteContext(string siteId)
        {
            // Try to load from MemoryBear first
            if (useMemoryBear)
            {
                try
                {
                    var response = await httpClient.GetAsync(
                        $"{memoryBearEndpoint}/api/site/{siteId}/context"
                    );
                    
                    if (response.IsSuccessStatusCode)
                    {
                        // Deserialize MemoryBear context
                        // ... implementation ...
                    }
                }
                catch { }
            }
            
            // Fall back to local storage
            string path = Path.Combine(Application.persistentDataPath, surveyDataPath, $"site_{siteId}.bin");
            if (File.Exists(path))
            {
                byte[] data = await File.ReadAllBytesAsync(path);
                if (useCompression) data = Decompress(data);
                // Deserialize
            }
            
            return null;
        }
        
        private string GetSessionPath(string sessionId)
        {
            return Path.Combine(Application.persistentDataPath, surveyDataPath, $"session_{sessionId}.bin");
        }
        
        #endregion
        
        #region Utilities
        
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula
            const double R = 6371000;
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
        
        private double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
        {
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double y = Math.Sin(dLon) * Math.Cos(lat2 * Math.PI / 180);
            double x = Math.Cos(lat1 * Math.PI / 180) * Math.Sin(lat2 * Math.PI / 180) -
                Math.Sin(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * Math.Cos(dLon);
            double bearing = Math.Atan2(y, x) * 180 / Math.PI;
            return (bearing + 360) % 360;
        }
        
        private byte[] Compress(byte[] data)
        {
            using (var ms = new MemoryStream())
            {
                using (var gzip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionLevel.Optimal))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return ms.ToArray();
            }
        }
        
        private byte[] Decompress(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var gzip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress))
            using (var result = new MemoryStream())
            {
                gzip.CopyTo(result);
                return result.ToArray();
            }
        }
        
        #endregion
        
        void OnDestroy()
        {
            // Save before exit
            _ = SaveSessionAsync();
            httpClient?.Dispose();
        }
    }
    
    // Helper class for AI recognition result
    public class AIRecognition
    {
        public string SuggestedType;
        public float Confidence;
        public string RecognitionMethod;
        public string Reasoning;
        public List<string> AlternativeSuggestions;
    }
    
    // Spatial relationship data
    public struct SpatialRelationship
    {
        public string RelatedCameraId;
        public float DistanceMeters;
        public float DirectionDegrees;
        public string RelationshipType;
    }
}
