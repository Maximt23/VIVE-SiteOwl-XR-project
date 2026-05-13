using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace SiteOwlXR.AI
{
    /// <summary>
    /// UI component for a single AI suggestion item.
    /// </summary>
    public class AiSuggestionItemUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI rankText;
        public TextMeshProUGUI deviceNameText;
        public TextMeshProUGUI deviceTypeText;
        public TextMeshProUGUI confidenceText;
        public TextMeshProUGUI reasoningText;
        public TextMeshProUGUI methodText;
        public Image confidenceBar;
        public Image backgroundImage;
        
        [Header("Buttons")]
        public Button acceptButton;
        public Button rejectButton;
        public Button detailsButton;
        
        [Header("Colors")]
        public Color highConfidenceColor = new Color(0.2f, 0.8f, 0.2f);
        public Color mediumConfidenceColor = new Color(1f, 0.8f, 0.2f);
        public Color lowConfidenceColor = new Color(1f, 0.3f, 0.2f);
        public Color rank1Color = new Color(1f, 0.8f, 0.2f);  // Gold
        public Color rank2Color = new Color(0.8f, 0.8f, 0.8f); // Silver
        public Color rank3Color = new Color(0.8f, 0.5f, 0.3f); // Bronze
        
        private Action onAccept;
        private Action onReject;
        
        void OnEnable()
        {
            if (acceptButton != null)
                acceptButton.onClick.AddListener(OnAcceptClicked);
            
            if (rejectButton != null)
                rejectButton.onClick.AddListener(OnRejectClicked);
            
            if (detailsButton != null)
                detailsButton.onClick.AddListener(OnDetailsClicked);
        }
        
        void OnDisable()
        {
            if (acceptButton != null)
                acceptButton.onClick.RemoveListener(OnAcceptClicked);
            
            if (rejectButton != null)
                rejectButton.onClick.RemoveListener(OnRejectClicked);
            
            if (detailsButton != null)
                detailsButton.onClick.RemoveListener(OnDetailsClicked);
        }
        
        public void Setup(
            int rank,
            string deviceName,
            string deviceType,
            string systemType,
            float confidence,
            string reasoning,
            string method,
            Action onAcceptCallback,
            Action onRejectCallback)
        {
            // Set callbacks
            onAccept = onAcceptCallback;
            onReject = onRejectCallback;
            
            // Set text
            rankText.text = $"#{rank}";
            deviceNameText.text = deviceName;
            deviceTypeText.text = $"{deviceType} | {systemType}";
            confidenceText.text = $"{confidence:P0} MATCH";
            reasoningText.text = reasoning;
            methodText.text = $"via {method}";
            
            // Set colors based on confidence
            Color confidenceColor = GetConfidenceColor(confidence);
            confidenceBar.color = confidenceColor;
            confidenceText.color = confidenceColor;
            
            // Set rank badge color
            Color rankColor = GetRankColor(rank);
            rankText.color = rankColor;
            
            // Highlight #1 suggestion
            if (rank == 1 && backgroundImage != null)
            {
                backgroundImage.color = new Color(1f, 1f, 0.9f);  // Light yellow highlight
            }
        }
        
        void OnAcceptClicked()
        {
            Debug.Log("[AiSuggestionItemUI] User accepted suggestion");
            onAccept?.Invoke();
        }
        
        void OnRejectClicked()
        {
            Debug.Log("[AiSuggestionItemUI] User rejected suggestion");
            onReject?.Invoke();
        }
        
        void OnDetailsClicked()
        {
            // Show more details about this suggestion
            // Could show comparison photo, feature breakdown, etc.
            Debug.Log("[AiSuggestionItemUI] Showing details");
        }
        
        Color GetConfidenceColor(float confidence)
        {
            if (confidence >= 0.8f) return highConfidenceColor;
            if (confidence >= 0.5f) return mediumConfidenceColor;
            return lowConfidenceColor;
        }
        
        Color GetRankColor(int rank)
        {
            switch (rank)
            {
                case 1: return rank1Color;
                case 2: return rank2Color;
                case 3: return rank3Color;
                default: return Color.white;
            }
        }
    }
}
