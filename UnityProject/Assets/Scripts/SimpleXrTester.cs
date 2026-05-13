using UnityEngine;
using UnityEngine.XR;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Simple XR test script for VIVE XR Elite.
/// Displays tracking info and tests basic functionality.
/// NO STEAM REQUIRED - Uses Unity XR directly.
/// </summary>
public class SimpleXrTester : MonoBehaviour
{
    [Header("UI Displays")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI positionText;
    public TextMeshProUGUI inputText;
    
    [Header("Visual Feedback")]
    public LineRenderer pointerLine;
    public Transform reticle;
    public float pointerLength = 10f;
    
    [Header("Test Objects")]
    public Transform[] testTargets;
    public Material highlightMaterial;
    
    private Transform cameraTransform;
    private InputData headsetInput;
    private bool isXrReady = false;
    private int currentTargetIndex = 0;
    
    void Start()
    {
        cameraTransform = Camera.main?.transform;
        
        // Initialize line renderer
        if (pointerLine != null)
        {
            pointerLine.positionCount = 2;
            pointerLine.startWidth = 0.01f;
            pointerLine.endWidth = 0.005f;
        }
        
        // Check XR status
        CheckXrStatus();
        
        UpdateStatus("XR Tester Ready\nWaiting for tracking...");
    }
    
    void Update()
    {
        if (cameraTransform == null) return;
        
        // Check if XR is running
        isXrReady = XRSettings.isDeviceActive;
        
        // Update displays
        UpdatePositionDisplay();
        UpdatePointer();
        CheckInput();
        
        // Test target selection
        if (isXrReady)
        {
            CheckTargetSelection();
        }
    }
    
    void CheckXrStatus()
    {
        var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances(xrDisplaySubsystems);
        
        bool xrRunning = false;
        foreach (XRDisplaySubsystem xrDisplay in xrDisplaySubsystems)
        {
            if (xrDisplay.running)
            {
                xrRunning = true;
                break;
            }
        }
        
        string status = $"XR Status: {(xrRunning ? "ACTIVE ✓" : "WAITING...")}\n";
        status += $"Device: {XRSettings.loadedDeviceName}\n";
        status += $"Active: {XRSettings.isDeviceActive}\n";
        
        UpdateStatus(status);
    }
    
    void UpdatePositionDisplay()
    {
        if (cameraTransform == null || positionText == null) return;
        
        Vector3 pos = cameraTransform.position;
        Vector3 rot = cameraTransform.eulerAngles;
        
        string display = $"Position:\n";
        display += $"  X: {pos.x:F2}\n";
        display += $"  Y: {pos.y:F2}\n";
        display += $"  Z: {pos.z:F2}\n\n";
        display += $"Rotation:\n";
        display += $"  Yaw: {rot.y:F1f}°\n";
        
        positionText.text = display;
    }
    
    void UpdatePointer()
    {
        if (pointerLine == null || cameraTransform == null) return;
        
        Vector3 start = cameraTransform.position;
        Vector3 end = start + cameraTransform.forward * pointerLength;
        
        pointerLine.SetPosition(0, start);
        pointerLine.SetPosition(1, end);
        
        // Update reticle position
        if (reticle != null)
        {
            reticle.position = end;
            reticle.rotation = Quaternion.LookRotation(cameraTransform.forward);
        }
    }
    
    void CheckInput()
    {
        string input = "Input:\n";
        
        // Check trigger
        if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space))
        {
            input += "  ✓ TRIGGER PRESSED\n";
            OnTriggerPressed();
        }
        
        // Check grip
        if (Input.GetButtonDown("Fire2"))
        {
            input += "  ✓ GRIP PRESSED\n";
        }
        
        // Check menu
        if (Input.GetButtonDown("Menu"))
        {
            input += "  ✓ MENU PRESSED\n";
        }
        
        // Controller position
        if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightPos))
        {
            input += $"  Right Hand: {rightPos}\n";
        }
        
        if (inputText != null)
        {
            inputText.text = input;
        }
    }
    
    void CheckTargetSelection()
    {
        if (testTargets == null || testTargets.Length == 0) return;
        
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        
        for (int i = 0; i < testTargets.Length; i++)
        {
            // Simple distance check to "target"
            float distance = Vector3.Distance(ray.GetPoint(5f), testTargets[i].position);
            
            if (distance < 1f && i != currentTargetIndex)
            {
                currentTargetIndex = i;
                HighlightTarget(i);
                UpdateStatus($"Looking at: {testTargets[i].name}");
            }
        }
    }
    
    void HighlightTarget(int index)
    {
        // Reset all
        foreach (var target in testTargets)
        {
            Renderer rend = target.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.white;
            }
        }
        
        // Highlight current
        Renderer currentRend = testTargets[index].GetComponent<Renderer>();
        if (currentRend != null)
        {
            currentRend.material.color = Color.green;
        }
    }
    
    void OnTriggerPressed()
    {
        UpdateStatus("TRIGGER CAPTURED!\nPhoto would be taken here.");
        
        // Flash effect
        if (Camera.main != null)
        {
            // You could add a flash effect here
        }
        
        // Log position
        if (cameraTransform != null)
        {
            Vector3 pos = cameraTransform.position;
            Vector3 forward = cameraTransform.forward;
            
            Debug.Log($"[XR Capture] Position: {pos}, Facing: {forward}");
            
            // In real app: Convert to SiteOwl X/Y and save
        }
    }
    
    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[XR Tester] {message}");
    }
}
