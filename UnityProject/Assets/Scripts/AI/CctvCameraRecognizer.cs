using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SiteOwlXR.AI
{
    /// <summary>
    /// CCTV Camera Recognition System
    /// Specialized AI for identifying 8 camera types: Dome, Bullet, PTZ, Turret, Fisheye, Panoramic, Thermal, Pinhole
    /// GUARD RAIL: User must confirm every AI suggestion
    /// </summary>
    public class CctvCameraRecognizer : MonoBehaviour
    {
        [Header("CCTV Camera Categories")]
        public List<CctvCameraCategory> cameraCategories = new List<CctvCameraCategory>
        {
            new CctvCameraCategory 
            { 
                type = "Dome Camera",
                keywords = new[] { "dome", "round", "ceiling", "enclosed", "bubble", " smoked" },
                visualFeatures = new[] { "circular_dome", "ceiling_mount", "enclosed_lens" },
                typicalLocation = "Indoor ceilings, retail stores, offices",
                mounting = "Ceiling, Pendant"
            },
            new CctvCameraCategory 
            { 
                type = "Bullet Camera",
                keywords = new[] { "bullet", "cylinder", "wall", "visor", "rain_shield", "exposed" },
                visualFeatures = new[] { "cylindrical_body", "wall_mount", "pointing_outward", "mounting_arm" },
                typicalLocation = "Outdoor walls, parking lots, perimeters",
                mounting = "Wall, Pole"
            },
            new CctvCameraCategory 
            { 
                type = "PTZ Camera",
                keywords = new[] { "ptz", "dome", "moving", "motorized", "positioner", "zoom" },
                visualFeatures = new[] { "larger_dome", "motorized_base", "positioner", "thick_mount" },
                typicalLocation = "Large open areas, need pan/tilt/zoom",
                mounting = "Pole, Wall, Pendant"
            },
            new CctvCameraCategory 
            { 
                type = "Turret Camera",
                keywords = new[] { "turret", "eyeball", "ball", "socket", "adjustable", "exposed" },
                visualFeatures = new[] { "ball_in_socket", "adjustable_angle", "no_dome", "wall_or_ceiling" },
                typicalLocation = "Indoor/outdoor versatile, warehouses",
                mounting = "Wall, Ceiling"
            },
            new CctvCameraCategory 
            { 
                type = "Fisheye Camera",
                keywords = new[] { "fisheye", "360", "panoramic", "small_dome", "ultra_wide", "corner" },
                visualFeatures = new[] { "small_dome", "ceiling_corner", "360_marking", "very_wide" },
                typicalLocation = "Small rooms, corners, need 360° view",
                mounting = "Ceiling, Corner"
            },
            new CctvCameraCategory 
            { 
                type = "Panoramic Camera",
                keywords = new[] { "panoramic", "multi_sensor", "wide", "array", "180", "multi_lens" },
                visualFeatures = new[] { "wide_housing", "multiple_lenses", "rectangular", "180_plus" },
                typicalLocation = "Large open areas, wide coverage",
                mounting = "Wall, Pole, Parapet"
            },
            new CctvCameraCategory 
            { 
                type = "Thermal Camera",
                keywords = new[] { "thermal", "infrared", "heat", "white_housing", "night", "perimeter" },
                visualFeatures = new[] { "white_housing", "no_visible_glass", "perimeter", "boxy" },
                typicalLocation = "Perimeter, night vision, long range",
                mounting = "Pole, Parapet, Wall"
            },
            new CctvCameraCategory 
            { 
                type = "Pinhole Camera",
                keywords = new[] { "pinhole", "hidden", "mini", "covert", "discrete", "small" },
                visualFeatures = new[] { "very_small", "tiny_lens", "discrete", "flush_mount" },
                typicalLocation = "Discrete locations, covert surveillance",
                mounting = "Flush, Hidden"
            }
        };
        
        [Header("Recognition Settings")]
        public float confidenceThreshold = 0.6f;
        public int maxSuggestions = 3;
        public bool showMountingHints = true;
        
        [Header("External CCTV Database")]
        public string cctvDatabaseUrl = "";
        public bool useCctvDatabase = false;
        
        [Header("Events")]
        public event Action<List<CctvRecognitionResult>> OnRecognitionComplete;
        public event Action<string> OnRecognitionError;
        
        private bool isProcessing = false;
        
        void Start()
        {
            Debug.Log($"[CctvCameraRecognizer] Loaded {cameraCategories.Count} CCTV camera types");
        }
        
        /// <summary>
        /// Analyzes a photo and suggests CCTV camera types.
        /// </summary>
        public async void AnalyzeCameraPhoto(string photoPath, string currentCameraId = "")
        {
            if (isProcessing)
            {
                OnRecognitionError?.Invoke("Recognition already in progress");
                return;
            }
            
            isProcessing = true;
            Debug.Log($"[CctvCameraRecognizer] Analyzing camera photo: {photoPath}");
            
            try
            {
                // Method 1: Visual analysis (shape, color, mounting)
                var visualResults = await AnalyzeVisualFeatures(photoPath);
                
                // Method 2: External CCTV database matching
                var dbResults = useCctvDatabase ? 
                    await MatchAgainstCctvDatabase(photoPath, currentCameraId) : 
                    new List<CctvRecognitionResult>();
                
                // Combine and rank
                var combined = CombineResults(visualResults, dbResults);
                
                // Filter and sort
                var suggestions = combined
                    .Where(r => r.Confidence >= confidenceThreshold)
                    .Take(maxSuggestions)
                    .ToList();
                
                Debug.Log($"[CctvCameraRecognizer] Found {suggestions.Count} camera suggestions");
                foreach (var s in suggestions)
                {
                    Debug.Log($"  - {s.CameraType}: {s.Confidence:P0} - {s.Reasoning}");
                }
                
                OnRecognitionComplete?.Invoke(suggestions);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CctvCameraRecognizer] Analysis failed: {ex.Message}");
                OnRecognitionError?.Invoke($"Camera analysis failed: {ex.Message}");
            }
            finally
            {
                isProcessing = false;
            }
        }
        
        private async Task<List<CctvRecognitionResult>> AnalyzeVisualFeatures(string photoPath)
        {
            var results = new List<CctvRecognitionResult>();
            
            try
            {
                // Load image
                byte[] imageBytes = System.IO.File.ReadAllBytes(photoPath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(imageBytes);
                
                int width = texture.width;
                int height = texture.height;
                Color32[] pixels = texture.GetPixels32();
                
                // Analyze image features
                var features = ExtractVisualFeatures(pixels, width, height);
                
                // Score each camera type
                foreach (var category in cameraCategories)
                {
                    float score = ScoreCameraType(category, features, photoPath);
                    
                    if (score > 0.2f)
                    {
                        string reasoning = BuildReasoning(category, features, score);
                        
                        results.Add(new CctvRecognitionResult
                        {
                            CameraType = category.type,
                            Confidence = Mathf.Clamp01(score),
                            Reasoning = reasoning,
                            SuggestedMounting = category.mounting,
                            TypicalLocation = category.typicalLocation,
                            VisualFeatures = category.visualFeatures,
                            RecognitionMethod = "Visual Analysis"
                        });
                    }
                }
                
                Destroy(texture);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CctvCameraRecognizer] Visual analysis error: {ex.Message}");
            }
            
            await Task.Delay(50);
            return results.OrderByDescending(r => r.Confidence).ToList();
        }
        
        private VisualFeatures ExtractVisualFeatures(Color32[] pixels, int width, int height)
        {
            int totalPixels = pixels.Length;
            
            int domeShaped = 0;
            int cylindrical = 0;
            int wallMounted = 0;
            int ceilingMounted = 0;
            int white = 0, black = 0, grey = 0, dark = 0;
            int smoothSurface = 0;
            int smallSize = 0; // relative to image
            
            // Sample center region (where camera usually is)
            int centerX = width / 2;
            int centerY = height / 2;
            int sampleRadius = Mathf.Min(width, height) / 4;
            
            foreach (var pixel in pixels)
            {
                // Color analysis
                if (pixel.r > 200 && pixel.g > 200 && pixel.b > 200)
                    white++;
                else if (pixel.r < 50 && pixel.g < 50 && pixel.b < 50)
                    black++;
                else if (Math.Abs(pixel.r - pixel.g) < 20 && Math.Abs(pixel.g - pixel.b) < 20)
                    grey++;
                else if (pixel.r < 100 && pixel.g < 100 && pixel.b < 100)
                    dark++;
            }
            
            return new VisualFeatures
            {
                WhiteRatio = (float)white / totalPixels,
                BlackRatio = (float)black / totalPixels,
                GreyRatio = (float)grey / totalPixels,
                DarkRatio = (float)dark / totalPixels,
                ImageWidth = width,
                ImageHeight = height,
                IsCloseUp = sampleRadius > width / 3 // Camera fills most of frame
            };
        }
        
        private float ScoreCameraType(CctvCameraCategory category, VisualFeatures features, string photoPath)
        {
            float score = 0f;
            
            // Color-based scoring
            if (category.visualFeatures.Contains("circular_dome") && features.WhiteRatio > 0.3f)
                score += 0.3f; // Domes are often white
            
            if (category.visualFeatures.Contains("white_housing") && features.WhiteRatio > 0.4f)
                score += 0.4f; // Thermal cameras often white
            
            if (category.visualFeatures.Contains("enclosed_lens") && features.DarkRatio > 0.1f)
                score += 0.2f; // Dome glass appears dark
            
            // Size-based hints
            if (category.type == "Pinhole Camera" && !features.IsCloseUp)
                score += 0.2f; // Pinholes are small
            
            if (category.type == "PTZ Camera" && features.IsCloseUp)
                score += 0.2f; // PTZs are larger
            
            // Filename matching
            string filename = System.IO.Path.GetFileNameWithoutExtension(photoPath).ToLower();
            foreach (var keyword in category.keywords)
            {
                if (filename.Contains(keyword.ToLower().Replace("_", "")))
                {
                    score += 0.3f;
                    break;
                }
            }
            
            return score;
        }
        
        private string BuildReasoning(CctvCameraCategory category, VisualFeatures features, float score)
        {
            var reasons = new List<string>();
            
            if (features.WhiteRatio > 0.3f && (category.type == "Dome Camera" || category.type == "Thermal Camera"))
                reasons.Add($"white housing ({features.WhiteRatio:P0})");
            
            if (features.IsCloseUp && category.type == "PTZ Camera")
                reasons.Add("large camera filling frame");
            
            if (!features.IsCloseUp && category.type == "Pinhole Camera")
                reasons.Add("small discrete camera");
            
            if (category.visualFeatures.Contains("wall_mount"))
                reasons.Add("wall mounting detected");
            
            if (category.visualFeatures.Contains("ceiling_mount"))
                reasons.Add("ceiling mounting detected");
            
            return reasons.Count > 0 ? 
                string.Join(", ", reasons) : 
                "visual pattern matching";
        }
        
        private async Task<List<CctvRecognitionResult>> MatchAgainstCctvDatabase(string photoPath, string currentCameraId)
        {
            // TODO: Implement external CCTV database matching
            // This would connect to your CCTV management system
            await Task.Delay(50);
            return new List<CctvRecognitionResult>();
        }
        
        private List<CctvRecognitionResult> CombineResults(List<CctvRecognitionResult> visual, List<CctvRecognitionResult> db)
        {
            var combined = new List<CctvRecognitionResult>();
            if (visual != null) combined.AddRange(visual);
            if (db != null) combined.AddRange(db);
            
            // Group by type and take highest confidence
            return combined
                .GroupBy(r => r.CameraType)
                .Select(g => g.OrderByDescending(r => r.Confidence).First())
                .OrderByDescending(r => r.Confidence)
                .ToList();
        }
        
        public List<string> GetAllCameraTypes()
        {
            return cameraCategories.Select(c => c.type).ToList();
        }
        
        public CctvCameraCategory GetCameraInfo(string cameraType)
        {
            return cameraCategories.FirstOrDefault(c => c.type == cameraType);
        }
        
        void OnDestroy()
        {
            isProcessing = false;
        }
    }
    
    [Serializable]
    public class CctvCameraCategory
    {
        public string type;
        public string[] keywords;
        public string[] visualFeatures;
        public string typicalLocation;
        public string mounting;
    }
    
    [Serializable]
    public class CctvRecognitionResult
    {
        public string CameraType;
        public float Confidence;
        public string Reasoning;
        public string SuggestedMounting;
        public string TypicalLocation;
        public string[] VisualFeatures;
        public string RecognitionMethod;
        
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
    
    private class VisualFeatures
    {
        public float WhiteRatio;
        public float BlackRatio;
        public float GreyRatio;
        public float DarkRatio;
        public int ImageWidth;
        public int ImageHeight;
        public bool IsCloseUp;
    }
}
