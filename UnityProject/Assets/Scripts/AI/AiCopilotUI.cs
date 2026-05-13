using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SiteOwlXR.Core;

namespace SiteOwlXR.AI
{
    /// <summary>
    /// AI Survey Copilot UI
    /// Displays AI suggestions for device recognition with manual confirmation.
    /// GUARD RAIL: User must explicitly approve every AI suggestion.
    /// </summary>
    public class AiCopilotUI : MonoBehaviour
    {
        [Header("UI Panels")]
        public GameObject suggestionsPanel;
        public GameObject analyzingPanel;
        public GameObject noMatchesPanel;
        
        [Header("Suggestion Items")]
        public Transform suggestionContainer;
        public GameObject suggestionItemPrefab;
        
        [Header("Status Display")]
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI confidenceText;
        public Image confidenceBar;
        
        [Header("Colors")]
        public Color highConfidenceColor = new Color(0.2f, 0.8f, 0.2f);
        public Color mediumConfidenceColor = new Color(1f, 0.8f, 0.2f);
        public Color lowConfidenceColor = new Color(1f, 0.3f, 0.2f);
        
        [Header("Dependencies")]
        public DeviceRecognizer recognizer;
        public CaptureController captureController;
        
        private DeviceData currentDevice;
        private List<RecognitionResult> currentSuggestions;
        private string currentPhotoPath;
        
        void Start()
        {
            if (recognizer != null)
            {
                recognizer.OnRecognitionComplete += OnRecognitionComplete;
                recognizer.OnRecognitionError += OnRecognitionError;
            }
            
            HideAllPanels();
        }
        
        void HideAllPanels()
        {
            suggestionsPanel?.SetActive(false);
            analyzingPanel?.SetActive(false);
            noMatchesPanel?.SetActive(false);
        }
        
        /// <summary>
        /// Starts AI analysis on a captured photo.
        /// </summary>
        public void AnalyzeCapturedPhoto(string photoPath, DeviceData device)
        {
            currentDevice = device;
            currentPhotoPath = photoPath;
            
            HideAllPanels();
            analyzingPanel?.SetActive(true);
            
            UpdateStatus("AI analyzing photo...", "Scanning image for device identification");
            
            // Trigger recognition
            recognizer?.AnalyzePhotoAsync(photoPath, device.DeviceID);
        }
        
        void OnRecognitionComplete(List<RecognitionResult> suggestions)
        {
            currentSuggestions = suggestions;
            
            HideAllPanels();
            
            if (suggestions == null || suggestions.Count == 0)
            {
                ShowNoMatches();
            }
            else
            {
                ShowSuggestions(suggestions);
            }
        }
        
        void OnRecognitionError(string error)
        {
            HideAllPanels();
            UpdateStatus("AI Analysis Failed", error);
            noMatchesPanel?.SetActive(true);
        }
        
        void ShowSuggestions(List<RecognitionResult> suggestions)
        {
            suggestionsPanel?.SetActive(true);
            
            // Clear existing items
            foreach (Transform child in suggestionContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create suggestion items
            for (int i = 0; i < suggestions.Count; i++)
            {
                var suggestion = suggestions[i];
                var item = Instantiate(suggestionItemPrefab, suggestionContainer);
                
                var itemUI = item.GetComponent<AiSuggestionItemUI>();
                if (itemUI != null)
                {
                    itemUI.Setup(
                        rank: i + 1,
                        deviceName: suggestion.SuggestedName,
                        deviceType: suggestion.SuggestedType,
                        systemType: suggestion.SuggestedSystemType,
                        confidence: suggestion.Confidence,
                        reasoning: suggestion.Reasoning,
                        method: suggestion.RecognitionMethod,
                        onAccept: () => AcceptSuggestion(suggestion),
                        onReject: () => RejectSuggestion(suggestion)
                    );
                }
            }
            
            // Update status
            var best = suggestions[0];
            UpdateStatus(
                $"AI Suggestion: {best.SuggestedName}",
                $"Confidence: {best.Confidence:P0} - {best.RecognitionMethod}"
            );
            
            // Update confidence bar
            confidenceBar.fillAmount = best.Confidence;
            confidenceBar.color = GetConfidenceColor(best.Confidence);
            
            confidenceText.text = $"{best.Confidence:P0} CONFIDENCE";
            confidenceText.color = GetConfidenceColor(best.Confidence);
        }
        
        void ShowNoMatches()
        {
            noMatchesPanel?.SetActive(true);
            UpdateStatus("No AI Matches", "AI couldn't identify this device. Manual entry required.");
            
            // Show manual entry UI
            // (implementation depends on your manual entry system)
        }
        
        /// <summary>
        /// User accepted an AI suggestion.
        /// </summary>
        void AcceptSuggestion(RecognitionResult suggestion)
        {
            Debug.Log($"[AiCopilotUI] User accepted: {suggestion.SuggestedName} ({suggestion.Confidence:P0})");
            
            // Update device with AI suggestion
            // BUT ONLY after user confirmation!
            if (currentDevice != null)
            {
                // Store AI suggestion in metadata (not overwriting actual data)
                currentDevice.CaptureMethod = $"XR_CAPTURE + AI_SUGGESTION_{suggestion.GetConfidenceLabel()}";
                
                // Show confirmation
                UpdateStatus(
                    $"Accepted: {suggestion.SuggestedName}",
                    "AI suggestion applied. Continue with capture."
                );
            }
            
            // Hide suggestions and continue
            HideAllPanels();
            
            // Notify capture controller to proceed
            captureController?.ProceedWithCapture();
        }
        
        /// <summary>
        /// User rejected an AI suggestion.
        /// </summary>
        void RejectSuggestion(RecognitionResult suggestion)
        {
            Debug.Log($"[AiCopilotUI] User rejected: {suggestion.SuggestedName}");
            
            // Remove from current suggestions
            currentSuggestions?.Remove(suggestion);
            
            // Show remaining suggestions or manual entry
            if (currentSuggestions != null && currentSuggestions.Count > 0)
            {
                ShowSuggestions(currentSuggestions);
            }
            else
            {
                ShowNoMatches();
            }
        }
        
        /// <summary>
        /// User wants to manually enter device info.
        /// </summary>
        public void OnManualEntryClicked()
        {
            HideAllPanels();
            
            // Trigger manual entry UI
            // (implementation depends on your system)
            
            UpdateStatus("Manual Entry", "Please identify the device manually.");
        }
        
        /// <summary>
        /// User wants to skip AI and continue with current device.
        /// </summary>
        public void OnSkipAiClicked()
        {
            HideAllPanels();
            UpdateStatus("AI Skipped", "Continuing with selected device.");
            captureController?.ProceedWithCapture();
        }
        
        void UpdateStatus(string title, string detail)
        {
            if (statusText != null)
            {
                statusText.text = $"<b>{title}</b>\n{detail}";
            }
        }
        
        Color GetConfidenceColor(float confidence)
        {
            if (confidence >= 0.8f) return highConfidenceColor;
            if (confidence >= 0.5f) return mediumConfidenceColor;
            return lowConfidenceColor;
        }
        
        void OnDestroy()
        {
            if (recognizer != null)
            {
                recognizer.OnRecognitionComplete -= OnRecognitionComplete;
                recognizer.OnRecognitionError -= OnRecognitionError;
            }
        }
    }
}
