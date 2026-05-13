using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR;
using TMPro;
using SiteOwlXR.Events;
using SiteOwlXR.Services;

namespace SiteOwlXR.Managers
{
    /// <summary>
    /// XR Initialization Manager - Enterprise Grade
    /// Waits for XR subsystems with timeout, error handling, and proper state reporting
    /// </summary>
    public class XrInitializer : MonoBehaviour
    {
        [Header("Configuration")]
        public float timeoutSeconds = 10f;
        public bool skipXrInEditor = true;
        
        [Header("UI References")]
        public GameObject loadingPanel;
        public TextMeshProUGUI loadingText;
        public TextMeshProUGUI statusText;
        
        [Header("Diagnostics")]
        public bool IsXrReady { get; private set; } = false;
        public string InitializationError { get; private set; }
        public float InitializationTime { get; private set; }
        
        private ILogger _logger;
        private System.Diagnostics.Stopwatch _stopwatch;
        private Coroutine _initCoroutine;
        
        void Awake()
        {
            _logger = ServiceLocator.Get<ILogger>() ?? new UnityLogger();
            _stopwatch = new System.Diagnostics.Stopwatch();
        }
        
        void Start()
        {
            ShowLoading("Initializing XR...");
            _initCoroutine = StartCoroutine(InitializeXrCoroutine());
        }
        
        void OnDestroy()
        {
            if (_initCoroutine != null)
            {
                StopCoroutine(_initCoroutine);
            }
        }
        
        IEnumerator InitializeXrCoroutine()
        {
            _stopwatch.Start();
            _logger.LogInfo("Starting XR initialization", this);
            
            // Editor mode - mock XR for testing
            if (Application.isEditor && skipXrInEditor)
            {
                _logger.LogInfo("Editor mode - skipping XR initialization", this);
                yield return new WaitForSeconds(0.5f); // Simulate init time
                
                IsXrReady = true;
                InitializationTime = _stopwatch.ElapsedMilliseconds;
                
                _logger.LogInfo($"XR initialized (editor mock) in {InitializationTime:F0}ms", this);
                CctvSurveyEvents.RaiseXrReady();
                
                HideLoading();
                yield break;
            }
            
            // Check if XR is enabled
            if (!XRSettings.enabled)
            {
                HandleError("XR is not enabled in Player Settings");
                yield break;
            }
            
            // Wait for XR Manager
            var xrManager = XRGeneralSettings.Instance?.Manager;
            if (xrManager == null)
            {
                HandleError("XR Manager not found");
                yield break;
            }
            
            // Wait for initialization with timeout
            float timer = 0f;
            bool xrLoaded = false;
            
            while (timer < timeoutSeconds)
            {
                // Check if XR is loaded
                if (xrManager.activeLoader != null)
                {
                    xrLoaded = true;
                    break;
                }
                
                // Update UI
                float progress = timer / timeoutSeconds;
                ShowLoading($"Initializing XR... {(progress * 100):F0}%");
                
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
            
            if (!xrLoaded)
            {
                HandleError($"XR initialization timed out after {timeoutSeconds}s");
                yield break;
            }
            
            // Verify tracking is working
            yield return StartCoroutine(VerifyTrackingCoroutine());
        }
        
        IEnumerator VerifyTrackingCoroutine()
        {
            _logger.LogInfo("XR loader active, verifying tracking...", this);
            ShowLoading("Verifying tracking...");
            
            float verifyTimer = 0f;
            const float verifyTimeout = 5f;
            
            while (verifyTimer < verifyTimeout)
            {
                // Check if we have position data
                if (Camera.main != null && Camera.main.transform.position != Vector3.zero)
                {
                    // Tracking is working
                    IsXrReady = true;
                    InitializationTime = _stopwatch.ElapsedMilliseconds;
                    
                    _logger.LogInfo($"XR ready with tracking in {InitializationTime:F0}ms", this);
                    
                    // Report success
                    ShowLoading("XR Ready!");
                    yield return new WaitForSeconds(0.5f);
                    
                    CctvSurveyEvents.RaiseXrReady();
                    HideLoading();
                    
                    yield break;
                }
                
                yield return new WaitForSeconds(0.1f);
                verifyTimer += 0.1f;
            }
            
            // Tracking didn't initialize but loader is active
            // This is a warning, not a failure - may work after user puts on headset
            IsXrReady = true;
            InitializationTime = _stopwatch.ElapsedMilliseconds;
            
            _logger.LogWarning($"XR loaded but tracking not verified. User may need to put on headset.", this);
            
            ShowLoading("XR Ready (put on headset)");
            yield return new WaitForSeconds(1f);
            
            CctvSurveyEvents.RaiseXrReady();
            HideLoading();
        }
        
        void HandleError(string message)
        {
            InitializationError = message;
            IsXrReady = false;
            _stopwatch.Stop();
            
            _logger.LogError($"XR initialization failed: {message}", null, this);
            
            ShowLoading($"Error: {message}");
            
            CctvSurveyEvents.RaiseXrError(message);
            
            // Keep loading panel visible with error
            if (statusText != null)
            {
                statusText.text = $"XR Error: {message}\nCheck headset connection and restart.";
                statusText.color = Color.red;
            }
        }
        
        void ShowLoading(string message)
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
            }
            
            if (loadingText != null)
            {
                loadingText.text = message;
            }
            
            _logger.LogDebug($"Loading: {message}", this);
        }
        
        void HideLoading()
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
            
            _logger.LogDebug("Loading panel hidden", this);
        }
        
        /// <summary>
        /// Public method to retry initialization
        /// </summary>
        public void RetryInitialization()
        {
            if (_initCoroutine != null)
            {
                StopCoroutine(_initCoroutine);
            }
            
            InitializationError = null;
            IsXrReady = false;
            _stopwatch.Reset();
            
            _initCoroutine = StartCoroutine(InitializeXrCoroutine());
        }
    }
}
