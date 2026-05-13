using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SiteOwlXR.AI
{
    /// <summary>
    /// Connects to external database/machine that has existing device information.
    /// Enables matching captured devices against known inventory.
    /// </summary>
    public class ExternalDataConnector : MonoBehaviour
    {
        [Header("Connection Settings")]
        [Tooltip("Must be HTTPS in production — never plain HTTP (store layout is sensitive data).")]
        public string baseUrl = "https://192.168.1.100:8080";  // Other machine IP
        public string apiKey = "";  // If authentication required
        public float timeout = 10f;
        
        [Header("Endpoints")]
        public string deviceListEndpoint = "/api/devices";
        public string deviceSearchEndpoint = "/api/devices/search";
        public string imageMatchEndpoint = "/api/match-image";
        
        [Header("Caching")]
        public bool cacheResults = true;
        public int cacheDurationMinutes = 30;
        
        private HttpClient httpClient;
        // ConcurrentDictionary is thread-safe for async Task access from multiple threads.
        // Plain Dictionary<,> is NOT safe for concurrent reads+writes and will corrupt.
        private readonly ConcurrentDictionary<string, CachedResult> cache =
            new ConcurrentDictionary<string, CachedResult>(StringComparer.Ordinal);
        
        void Start()
        {
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(timeout);
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            }
            
            Debug.Log($"[ExternalDataConnector] Initialized with base URL: {baseUrl}");
        }
        
        /// <summary>
        /// Fetches all devices from external database.
        /// </summary>
        public async Task<List<ExternalDevice>> GetAllDevices()
        {
            string cacheKey = "all_devices";
            
            if (cacheResults && IsCacheValid(cacheKey))
            {
                return cache[cacheKey].Devices;
            }
            
            try
            {
                string url = $"{baseUrl}{deviceListEndpoint}";
                Debug.Log($"[ExternalDataConnector] Fetching: {url}");
                
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                string json = await response.Content.ReadAsStringAsync();
                var devices = JsonConvert.DeserializeObject<List<ExternalDevice>>(json);
                
                if (cacheResults)
                {
                    cache[cacheKey] = new CachedResult
                    {
                        Devices = devices,
                        Timestamp = DateTime.Now
                    };
                }
                
                Debug.Log($"[ExternalDataConnector] Fetched {devices.Count} devices");
                return devices;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExternalDataConnector] Failed to fetch devices: {ex.Message}");
                return new List<ExternalDevice>();
            }
        }
        
        /// <summary>
        /// Searches external database for devices matching criteria.
        /// </summary>
        public async Task<List<ExternalDevice>> SearchDevices(string query, string type = "", string site = "")
        {
            try
            {
                string url = $"{baseUrl}{deviceSearchEndpoint}?q={Uri.EscapeDataString(query)}";
                
                if (!string.IsNullOrEmpty(type))
                    url += $"&type={Uri.EscapeDataString(type)}";
                
                if (!string.IsNullOrEmpty(site))
                    url += $"&site={Uri.EscapeDataString(site)}";
                
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                string json = await response.Content.ReadAsStringAsync();
                var devices = JsonConvert.DeserializeObject<List<ExternalDevice>>(json);
                
                Debug.Log($"[ExternalDataConnector] Search '{query}' returned {devices.Count} results");
                return devices;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExternalDataConnector] Search failed: {ex.Message}");
                return new List<ExternalDevice>();
            }
        }
        
        /// <summary>
        /// Finds similar devices based on image matching.
        /// Uploads photo to external service for AI matching.
        /// </summary>
        public async Task<List<SimilarDeviceMatch>> FindSimilarDevices(string photoPath, string excludeDeviceId = "")
        {
            try
            {
                // Read photo bytes
                byte[] photoBytes = System.IO.File.ReadAllBytes(photoPath);
                
                // Create multipart form
                var content = new MultipartFormDataContent();
                content.Add(new ByteArrayContent(photoBytes), "image", "photo.jpg");
                
                if (!string.IsNullOrEmpty(excludeDeviceId))
                {
                    content.Add(new StringContent(excludeDeviceId), "exclude_id");
                }
                
                string url = $"{baseUrl}{imageMatchEndpoint}";
                Debug.Log($"[ExternalDataConnector] Uploading image for matching...");
                
                var response = await httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                
                string json = await response.Content.ReadAsStringAsync();
                var matches = JsonConvert.DeserializeObject<List<SimilarDeviceMatch>>(json);
                
                Debug.Log($"[ExternalDataConnector] Found {matches.Count} similar devices");
                return matches;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ExternalDataConnector] Image matching failed: {ex.Message}");
                // Return empty list - caller will fall back to other methods
                return new List<SimilarDeviceMatch>();
            }
        }
        
        /// <summary>
        /// Fetches details for a specific device by ID.
        /// </summary>
        public async Task<ExternalDevice> GetDeviceById(string deviceId)
        {
            try
            {
                string url = $"{baseUrl}{deviceListEndpoint}/{deviceId}";
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                string json = await response.Content.ReadAsStringAsync();
                var device = JsonConvert.DeserializeObject<ExternalDevice>(json);
                
                return device;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExternalDataConnector] Failed to get device {deviceId}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Offline fallback: Load from local JSON file if external connection fails.
        /// </summary>
        public List<ExternalDevice> LoadLocalBackup(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    Debug.LogWarning($"[ExternalDataConnector] Local backup not found: {filePath}");
                    return new List<ExternalDevice>();
                }
                
                string json = System.IO.File.ReadAllText(filePath);
                var devices = JsonConvert.DeserializeObject<List<ExternalDevice>>(json);
                
                Debug.Log($"[ExternalDataConnector] Loaded {devices.Count} devices from local backup");
                return devices;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExternalDataConnector] Failed to load local backup: {ex.Message}");
                return new List<ExternalDevice>();
            }
        }
        
        /// <summary>
        /// Saves external data to local backup for offline use.
        /// </summary>
        public void SaveLocalBackup(List<ExternalDevice> devices, string filePath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(devices, Formatting.Indented);
                System.IO.File.WriteAllText(filePath, json);
                
                Debug.Log($"[ExternalDataConnector] Saved {devices.Count} devices to backup: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExternalDataConnector] Failed to save backup: {ex.Message}");
            }
        }
        
        private bool IsCacheValid(string key)
        {
            if (!cache.ContainsKey(key)) return false;
            
            var age = DateTime.Now - cache[key].Timestamp;
            return age.TotalMinutes < cacheDurationMinutes;
        }
        
        /// <summary>
        /// Clears all cached results.
        /// </summary>
        public void ClearCache()
        {
            cache.Clear();
            Debug.Log("[ExternalDataConnector] Cache cleared");
        }
        
        void OnDestroy()
        {
            httpClient?.Dispose();
        }
    }
    
    [Serializable]
    public class ExternalDevice
    {
        public string DeviceId;
        public string DeviceName;
        public string DeviceType;
        public string SystemType;
        public string SiteId;
        public string Description;
        public double? Latitude;
        public double? Longitude;
        public float? SiteOwlX;
        public float? SiteOwlY;
        public string PhotoUrl;  // URL to existing photo
        public DateTime LastUpdated;
        public Dictionary<string, string> Metadata;  // Extra fields
    }
    
    [Serializable]
    public class SimilarDeviceMatch
    {
        public string DeviceId;
        public string DeviceName;
        public string DeviceType;
        public string SystemType;
        public float SimilarityScore;  // 0.0 to 1.0
        public string MatchingFeatures;  // What matched (color, shape, etc.)
        public string ThumbnailUrl;
    }
    
    private class CachedResult
    {
        public List<ExternalDevice> Devices;
        public DateTime Timestamp;
    }
}
