using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SiteOwlXR.AI
{
    /// <summary>
    /// AI Device Recognition System
    /// Analyzes photos to identify device types and suggest matches from existing database.
    /// GUARD RAIL: Never auto-writes AI guesses without user confirmation.
    /// </summary>
    public class DeviceRecognizer : MonoBehaviour
    {
        [Header("Recognition Settings")]
        public float confidenceThreshold = 0.7f;  // Minimum confidence to suggest
        public int maxSuggestions = 3;            // Number of suggestions to show
        
        [Header("External Data Source")]
        public string externalDataEndpoint = "";  // URL to other machine/database
        public bool useExternalDatabase = false;
        
        [Header("Device Categories")]
        public List<DeviceCategory> knownCategories = new List<DeviceCategory>
        {
            new DeviceCategory { type = "Dome Camera", keywords = new[] { "dome", "ceiling", "camera", "round", "enclosed" }, systemType = "CCTV" },
            new DeviceCategory { type = "Bullet Camera", keywords = new[] { "bullet", "camera", "cylinder", "outdoor", "wall" }, systemType = "CCTV" },
            new DeviceCategory { type = "PTZ Camera", keywords = new[] { "ptz", "camera", "dome", "moving", "motorized" }, systemType = "CCTV" },
            new DeviceCategory { type = "Card Reader", keywords = new[] { "card", "reader", "rfid", "prox", "keypad", "access" }, systemType = "Access Control" },
            new DeviceCategory { type = "Door Contact", keywords = new[] { "door", "contact", "sensor", "magnetic", "reed" }, systemType = "Access Control" },
            new DeviceCategory { type = "Smoke Detector", keywords = new[] { "smoke", "detector", "fire", "round", "ceiling" }, systemType = "Fire Alarm" },
            new DeviceCategory { type = "Horn Strobe", keywords = new[] { "horn", "strobe", "red", "loud", "fire", "alarm" }, systemType = "Fire Alarm" },
            new DeviceCategory { type = "Pull Station", keywords = new[] { "pull", "station", "red", "handle", "fire", "manual" }, systemType = "Fire Alarm" },
            new DeviceCategory { type = "Motion Sensor", keywords = new[] { "motion", "sensor", "pir", "infrared", "white", "round" }, systemType = "Intrusion" },
            new DeviceCategory { type = "Glass Break", keywords = new[] { "glass", "break", "sensor", "small", "white" }, systemType = "Intrusion" },
            new DeviceCategory { type = "Thermostat", keywords = new[] { "thermostat", "hvac", "wall", "digital", "display" }, systemType = "HVAC" },
            new DeviceCategory { type = "Damper", keywords = new[] { "damper", "hvac", "duct", "fire", "smoke" }, systemType = "HVAC" },
            new DeviceCategory { type = "Emergency Exit Sign", keywords = new[] { "exit", "sign", "green", "red", "emergency", "light" }, systemType = "Emergency" },
            new DeviceCategory { type = "Intercom", keywords = new[] { "intercom", "speaker", "microphone", "button", "wall" }, systemType = "Communications" },
        };
        
        [Header("Events")]
        public event Action<List<RecognitionResult>> OnRecognitionComplete;
        public event Action<string> OnRecognitionError;
        
        private ExternalDataConnector externalData;
        private bool isProcessing = false;
        
        void Start()
        {
            externalData = GetComponent<ExternalDataConnector>();
            
            // Log available categories
            Debug.Log($"[DeviceRecognizer] Loaded {knownCategories.Count} device categories");
        }
        
        /// <summary>
        /// Analyzes a photo and returns device recognition suggestions.
        /// </summary>
        public async void AnalyzePhoto(string photoPath, string currentDeviceId = "")
        {
            if (isProcessing)
            {
                OnRecognitionError?.Invoke("Recognition already in progress");
                return;
            }
            
            isProcessing = true;
            
            Debug.Log($"[DeviceRecognizer] Analyzing photo: {photoPath}");
            
            try
            {
                // Method 1: Try external AI service (TensorFlow, cloud API, etc.)
                var externalResults = await TryExternalAnalysis(photoPath);
                
                // Method 2: Fall back to local keyword-based analysis
                if (externalResults == null || externalResults.Count == 0)
                {
                    externalResults = await LocalImageAnalysis(photoPath);
                }
                
                // Method 3: Match against existing database
                var dbMatches = await MatchAgainstDatabase(photoPath, currentDeviceId);
                
                // Combine and rank results
                var combinedResults = CombineResults(externalResults, dbMatches);
                
                // Filter by confidence threshold
                var suggestions = combinedResults
                    .Where(r => r.Confidence >= confidenceThreshold)
                    .Take(maxSuggestions)
                    .ToList();
                
                Debug.Log($"[DeviceRecognizer] Found {suggestions.Count} suggestions");
                foreach (var s in suggestions)
                {
                    Debug.Log($"  - {s.SuggestedName}: {s.Confidence:P0} confidence");
                }
                
                OnRecognitionComplete?.Invoke(suggestions);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeviceRecognizer] Analysis failed: {ex.Message}");
                OnRecognitionError?.Invoke($"Analysis failed: {ex.Message}");
            }
            finally
            {
                isProcessing = false;
            }
        }
        
        /// <summary>
        /// Tries to use external AI service (TensorFlow Lite, cloud API, etc.)
        /// </summary>
        private async Task<List<RecognitionResult>> TryExternalAnalysis(string photoPath)
        {
            // TODO: Implement actual ML model inference
            // Options:
            // 1. TensorFlow Lite model running locally on headset
            // 2. REST API call to external service
            // 3. Send to PC/server for processing
            
            // Placeholder: Return null to trigger fallback
            await Task.Delay(100); // Simulate async work
            return null;
        }
        
        /// <summary>
        /// Local keyword-based analysis (fallback when no ML available)
        /// Uses EXIF data, filename, and basic image properties
        /// </summary>
        private async Task<List<RecognitionResult>> LocalImageAnalysis(string photoPath)
        {
            var results = new List<RecognitionResult>();
            
            try
            {
                // Load image
                byte[] imageBytes = System.IO.File.ReadAllBytes(photoPath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(imageBytes);
                
                // Basic image analysis
                Color32[] pixels = texture.GetPixels32();
                int totalPixels = pixels.Length;
                
                // Calculate dominant colors
                int redPixels = 0, whitePixels = 0, blackPixels = 0, greyPixels = 0;
                
                foreach (var pixel in pixels)
                {
                    if (pixel.r > 200 && pixel.g < 100 && pixel.b < 100)
                        redPixels++;
                    else if (pixel.r > 200 && pixel.g > 200 && pixel.b > 200)
                        whitePixels++;
                    else if (pixel.r < 50 && pixel.g < 50 && pixel.b < 50)
                        blackPixels++;
                    else if (Math.Abs(pixel.r - pixel.g) < 20 && Math.Abs(pixel.g - pixel.b) < 20)
                        greyPixels++;
                }
                
                // Score categories based on colors
                float redRatio = (float)redPixels / totalPixels;
                float whiteRatio = (float)whitePixels / totalPixels;
                float blackRatio = (float)blackPixels / totalPixels;
                float greyRatio = (float)greyPixels / totalPixels;
                
                // Match against categories
                foreach (var category in knownCategories)
                {
                    float score = 0f;
                    
                    // Color-based scoring
                    if (category.keywords.Contains("red") && redRatio > 0.1f)
                        score += 0.3f;
                    if (category.keywords.Contains("white") && whiteRatio > 0.3f)
                        score += 0.2f;
                    if (category.keywords.Contains("black") && blackRatio > 0.2f)
                        score += 0.2f;
                    
                    // Filename matching
                    string filename = System.IO.Path.GetFileNameWithoutExtension(photoPath).ToLower();
                    foreach (var keyword in category.keywords)
                    {
                        if (filename.Contains(keyword.ToLower()))
                        {
                            score += 0.4f;
                            break;
                        }
                    }
                    
                    if (score > 0.2f)
                    {
                        results.Add(new RecognitionResult
                        {
                            SuggestedName = category.type,
                            SuggestedSystemType = category.systemType,
                            Confidence = Mathf.Clamp01(score),
                            RecognitionMethod = "Color+Filename Analysis",
                            Reasoning = $"Red:{redRatio:P0}, White:{whiteRatio:P0}, Filename match"
                        });
                    }
                }
                
                Destroy(texture);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DeviceRecognizer] Local analysis failed: {ex.Message}");
            }
            
            await Task.Delay(50);
            return results.OrderByDescending(r => r.Confidence).ToList();
        }
        
        /// <summary>
        /// Matches photo against existing device database from "other machine"
        /// </summary>
        private async Task<List<RecognitionResult>> MatchAgainstDatabase(string photoPath, string currentDeviceId)
        {
            var results = new List<RecognitionResult>();
            
            if (externalData != null && useExternalDatabase)
            {
                try
                {
                    // Query external database for similar devices
                    var similarDevices = await externalData.FindSimilarDevices(photoPath, currentDeviceId);
                    
                    foreach (var device in similarDevices)
                    {
                        results.Add(new RecognitionResult
                        {
                            SuggestedName = device.DeviceName,
                            SuggestedType = device.DeviceType,
                            SuggestedSystemType = device.SystemType,
                            Confidence = device.SimilarityScore,
                            RecognitionMethod = "Database Match",
                            Reasoning = $"Matched with {device.SimilarityScore:P0} similarity",
                            MatchedDeviceId = device.DeviceId
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DeviceRecognizer] Database query failed: {ex.Message}");
                }
            }
            
            await Task.Delay(50);
            return results;
        }
        
        /// <summary>
        /// Combines and deduplicates results from multiple sources
        /// </summary>
        private List<RecognitionResult> CombineResults(List<RecognitionResult> source1, List<RecognitionResult> source2)
        {
            var combined = new List<RecognitionResult>();
            
            if (source1 != null) combined.AddRange(source1);
            if (source2 != null) combined.AddRange(source2);
            
            // Group by suggested name and take highest confidence
            var grouped = combined
                .GroupBy(r => r.SuggestedName)
                .Select(g => g.OrderByDescending(r => r.Confidence).First())
                .OrderByDescending(r => r.Confidence)
                .ToList();
            
            return grouped;
        }
        
        /// <summary>
        /// Gets recognition statistics for a category.
        /// </summary>
        public Dictionary<string, int> GetCategoryStats()
        {
            var stats = new Dictionary<string, int>();
            foreach (var cat in knownCategories)
            {
                var key = $"{cat.systemType}: {cat.type}";
                stats[key] = cat.keywords.Length;
            }
            return stats;
        }
        
        void OnDestroy()
        {
            isProcessing = false;
        }
    }
    
    [Serializable]
    public class DeviceCategory
    {
        public string type;
        public string systemType;
        public string[] keywords;
    }
    
    [Serializable]
    public class RecognitionResult
    {
        public string SuggestedName;
        public string SuggestedType;
        public string SuggestedSystemType;
        public float Confidence;  // 0.0 to 1.0
        public string RecognitionMethod;  // How AI decided this
        public string Reasoning;  // Human-readable explanation
        public string MatchedDeviceId;  // If matched to existing device
        
        public bool IsHighConfidence => Confidence >= 0.8f;
        public bool IsMediumConfidence => Confidence >= 0.5f && Confidence < 0.8f;
        public bool IsLowConfidence => Confidence < 0.5f;
        
        public string GetConfidenceLabel()
        {
            if (IsHighConfidence) return "HIGH";
            if (IsMediumConfidence) return "MEDIUM";
            return "LOW";
        }
    }
}
