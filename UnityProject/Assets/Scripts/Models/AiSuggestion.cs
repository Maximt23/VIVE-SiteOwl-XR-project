using System;

namespace SiteOwlXR.Models
{
    /// <summary>
    /// An AI-generated suggestion for device type
    /// </summary>
    [Serializable]
    public class AiSuggestion
    {
        public string SuggestedType;        // e.g., "Dome Camera"
        public string SuggestedSystemType;  // e.g., "CCTV"
        public float Confidence;            // 0.0 to 1.0
        public string RecognitionMethod;    // "Visual", "Database", "Pattern"
        public string Reasoning;              // Human-readable explanation
        public int Rank;                    // 1, 2, 3...
        
        public bool IsHighConfidence => Confidence >= 0.8f;
        public bool IsMediumConfidence => Confidence >= 0.5f && Confidence < 0.8f;
        public bool IsLowConfidence => Confidence < 0.5f;
        
        public string GetConfidenceLabel()
        {
            if (IsHighConfidence) return "HIGH";
            if (IsMediumConfidence) return "MEDIUM";
            return "LOW";
        }
        
        public override string ToString()
        {
            return $"#{Rank} {SuggestedType} ({Confidence:P0}) - {Reasoning}";
        }
    }
}
