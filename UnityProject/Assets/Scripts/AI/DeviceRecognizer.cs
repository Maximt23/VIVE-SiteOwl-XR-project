using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        
        [Header("Device Categories — fallback if library not loaded")]
        public List<DeviceCategory> knownCategories = new List<DeviceCategory>
        {
            new DeviceCategory { type = "Dome Camera",        keywords = new[] { "dome", "ceiling", "camera", "round", "enclosed" },        systemType = "CCTV" },
            new DeviceCategory { type = "Bullet Camera",      keywords = new[] { "bullet", "camera", "cylinder", "outdoor", "wall" },       systemType = "CCTV" },
            new DeviceCategory { type = "PTZ Camera",         keywords = new[] { "ptz", "camera", "dome", "moving", "motorized" },          systemType = "CCTV" },
            new DeviceCategory { type = "Card Reader",        keywords = new[] { "card", "reader", "rfid", "prox", "keypad", "access" },    systemType = "Access Control" },
            new DeviceCategory { type = "Door Contact",       keywords = new[] { "door", "contact", "sensor", "magnetic", "reed" },         systemType = "Access Control" },
            new DeviceCategory { type = "Smoke Detector",     keywords = new[] { "smoke", "detector", "fire", "round", "ceiling" },         systemType = "Fire Alarm" },
            new DeviceCategory { type = "Horn Strobe",        keywords = new[] { "horn", "strobe", "red", "loud", "fire", "alarm" },        systemType = "Fire Alarm" },
            new DeviceCategory { type = "Pull Station",       keywords = new[] { "pull", "station", "red", "handle", "fire", "manual" },    systemType = "Fire Alarm" },
            new DeviceCategory { type = "Motion Sensor",      keywords = new[] { "motion", "sensor", "pir", "infrared", "white", "round" }, systemType = "Intrusion" },
            new DeviceCategory { type = "Glass Break",        keywords = new[] { "glass", "break", "sensor", "small", "white" },           systemType = "Intrusion" },
            new DeviceCategory { type = "Thermostat",         keywords = new[] { "thermostat", "hvac", "wall", "digital", "display" },      systemType = "HVAC" },
            new DeviceCategory { type = "Damper",             keywords = new[] { "damper", "hvac", "duct", "fire", "smoke" },              systemType = "HVAC" },
            new DeviceCategory { type = "Emergency Exit Sign",keywords = new[] { "exit", "sign", "green", "red", "emergency", "light" },   systemType = "Emergency" },
            new DeviceCategory { type = "Intercom",           keywords = new[] { "intercom", "speaker", "microphone", "button", "wall" },   systemType = "Communications" },
        };

        [Header("Events")]
        public event Action<List<RecognitionResult>> OnRecognitionComplete;
        public event Action<string> OnRecognitionError;

        private ExternalDataConnector externalData;
        private DeviceLibraryLoader   libraryLoader;
        private bool isProcessing = false;

        void Start()
        {
            externalData  = GetComponent<ExternalDataConnector>();
            libraryLoader = GetComponent<DeviceLibraryLoader>();

            if (libraryLoader != null)
            {
                if (libraryLoader.IsLoaded)
                    MergeLibraryIntoCategories();
                else
                    libraryLoader.OnLibraryLoaded += MergeLibraryIntoCategories;
            }

            Debug.Log($"[DeviceRecognizer] {knownCategories.Count} fallback categories ready.");
        }

        /// <summary>
        /// Enriches knownCategories with all models from camera_model_library.json.
        /// Called once the library finishes loading from StreamingAssets.
        /// </summary>
        private void MergeLibraryIntoCategories()
        {
            if (libraryLoader?.Models == null) return;
            int added = 0;
            foreach (var model in libraryLoader.Models)
            {
                // Skip if an identical type already exists (avoid duplicating generic entries)
                string label = $"{model.manufacturer} {model.model_name}";
                if (knownCategories.Exists(c => c.type.Equals(label, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var cat = new DeviceCategory
                {
                    type       = label,
                    systemType = "CCTV",
                    keywords   = BuildKeywords(model),
                };
                knownCategories.Add(cat);
                added++;
            }
            Debug.Log($"[DeviceRecognizer] Merged {added} models from library. " +
                      $"Total categories: {knownCategories.Count}");
        }

        private static string[] BuildKeywords(CameraModel model)
        {
            var kw = new System.Collections.Generic.List<string>();
            if (model.keywords != null)     kw.AddRange(model.keywords);
            if (model.part_numbers != null) kw.AddRange(model.part_numbers);
            kw.Add(model.form_factor ?? "");
            kw.Add(model.manufacturer?.ToLower() ?? "");
            kw.Add(model.model_name?.ToLower()   ?? "");
            if (model.visual?.color != null) kw.AddRange(model.visual.color);
            return kw.Where(k => !string.IsNullOrWhiteSpace(k)).ToArray();
        }
        
        /// <summary>
        /// Analyzes a photo and raises OnRecognitionComplete with device suggestions.
        /// Returns a Task so callers can observe exceptions properly.
        /// Fire-and-forget: _ = AnalyzePhotoAsync(path);
        /// </summary>
        public async Task AnalyzePhotoAsync(string photoPath, string currentDeviceId = "")
        {
            if (isProcessing)
            {
                OnRecognitionError?.Invoke("Recognition already in progress");
                return;
            }

            isProcessing = true;
            Debug.Log($"[DeviceRecognizer] Analyzing: {photoPath}");

            try
            {
                var externalResults = await TryExternalAnalysis(photoPath);

                if (externalResults == null || externalResults.Count == 0)
                    externalResults = await LocalImageAnalysis(photoPath);

                var dbMatches  = await MatchAgainstDatabase(photoPath, currentDeviceId);
                var combined   = CombineResults(externalResults, dbMatches);
                var suggestions = combined
                    .Where(r => r.Confidence >= confidenceThreshold)
                    .Take(maxSuggestions)
                    .ToList();

                Debug.Log($"[DeviceRecognizer] {suggestions.Count} suggestion(s)");
                OnRecognitionComplete?.Invoke(suggestions);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeviceRecognizer] Analysis failed: {ex}");
                OnRecognitionError?.Invoke($"Analysis failed: {ex.Message}");
            }
            finally
            {
                isProcessing = false;  // always released, even on exception
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
        /// Local color-based analysis.
        /// Pixel sampling is done on the main-thread synchronization context to avoid
        /// UnityException (Unity APIs are main-thread only).
        /// </summary>
        private async Task<List<RecognitionResult>> LocalImageAnalysis(string photoPath)
        {
            var results = new List<RecognitionResult>();

            // Capture main-thread context before any await
            var mainThread = SynchronizationContext.Current;

            try
            {
                // Read raw bytes on any thread — no Unity API involved
                byte[] imageBytes = await Task.Run(() => System.IO.File.ReadAllBytes(photoPath));

                // Color analysis MUST happen on the main thread (Texture2D is UI-thread-only)
                float redRatio = 0f, whiteRatio = 0f, blackRatio = 0f;

                var tcs = new TaskCompletionSource<bool>();
                mainThread.Post(_ =>
                {
                    try
                    {
                        var texture = new Texture2D(2, 2);
                        texture.LoadImage(imageBytes);

                        Color32[] pixels     = texture.GetPixels32();
                        int       total      = pixels.Length;
                        int red = 0, white = 0, black = 0;

                        foreach (var p in pixels)
                        {
                            if (p.r > 200 && p.g < 100 && p.b < 100) red++;
                            else if (p.r > 200 && p.g > 200 && p.b > 200) white++;
                            else if (p.r < 50  && p.g < 50  && p.b < 50)  black++;
                        }

                        redRatio   = (float)red   / total;
                        whiteRatio = (float)white / total;
                        blackRatio = (float)black / total;

                        UnityEngine.Object.Destroy(texture);
                        tcs.TrySetResult(true);
                    }
                    catch (Exception ex) { tcs.TrySetException(ex); }
                }, null);

                await tcs.Task;

                // Score categories based on dominant colors
                foreach (var category in knownCategories)
                {
                    float score = 0f;
                    if (category.keywords.Contains("red")   && redRatio   > 0.1f) score += 0.3f;
                    if (category.keywords.Contains("white")  && whiteRatio > 0.3f) score += 0.2f;
                    if (category.keywords.Contains("black")  && blackRatio > 0.2f) score += 0.2f;

                    if (score > 0.2f)
                    {
                        results.Add(new RecognitionResult
                        {
                            SuggestedName       = category.type,
                            SuggestedSystemType = category.systemType,
                            Confidence          = Mathf.Clamp01(score),
                            RecognitionMethod   = "Color Analysis",
                            Reasoning           = $"Red:{redRatio:P0} White:{whiteRatio:P0} Black:{blackRatio:P0}"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DeviceRecognizer] Local analysis failed: {ex.Message}");
            }

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
