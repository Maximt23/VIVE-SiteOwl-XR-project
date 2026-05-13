using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Initializes XR on startup and handles device availability.
/// Ensures OpenXR is properly loaded before starting the app.
/// </summary>
public class XrInitializer : MonoBehaviour
{
    [Header("Settings")]
    public float initializationTimeout = 10f;
    public bool autoStartXr = true;
    
    [Header("UI")]
    public GameObject loadingPanel;
    public TMPro.TextMeshProUGUI loadingText;
    
    private bool isInitialized = false;
    
    void Start()
    {
        StartCoroutine(InitializeXR());
    }
    
    IEnumerator InitializeXR()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
        
        UpdateLoadingText("Initializing XR...");
        
        float timer = 0f;
        
        // Wait for XR to become active
        while (!XRSettings.isDeviceActive && timer < initializationTimeout)
        {
            timer += Time.deltaTime;
            UpdateLoadingText($"Waiting for headset... {timer:F1}s");
            yield return null;
        }
        
        if (XRSettings.isDeviceActive)
        {
            isInitialized = true;
            UpdateLoadingText($"XR Ready!\nDevice: {XRSettings.loadedDeviceName}");
            
            yield return new WaitForSeconds(1f);
            
            // Hide loading panel
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
            
            Debug.Log($"[XrInitializer] XR initialized: {XRSettings.loadedDeviceName}");
        }
        else
        {
            UpdateLoadingText("XR Timeout - Running in mock mode");
            Debug.LogWarning("[XrInitializer] XR not detected, running in fallback mode");
            
            yield return new WaitForSeconds(2f);
            
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
        }
    }
    
    void UpdateLoadingText(string message)
    {
        if (loadingText != null)
        {
            loadingText.text = message;
        }
        Debug.Log($"[XR Init] {message}");
    }
    
    /// <summary>
    /// Returns true if XR is properly initialized.
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized;
    }
    
    /// <summary>
    /// Gets the active XR device name.
    /// </summary>
    public string GetXrDeviceName()
    {
        return XRSettings.loadedDeviceName;
    }
}
