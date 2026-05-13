using UnityEngine;
using System;

namespace SiteOwlXR.Core
{
    /// <summary>
    /// Manages the calibration anchor that transforms headset space to SiteOwl space.
    /// User stands at a known SiteOwl X/Y location and sets this as the reference point.
    /// </summary>
    public class CalibrationManager : MonoBehaviour
    {
        [Header("Calibration State")]
        public bool IsCalibrated { get; private set; } = false;
        public Vector3 CalibratedWorldPosition { get; private set; }
        public Vector2 CalibratedSiteOwlXY { get; private set; }
        public Quaternion CalibratedRotation { get; private set; }
        
        [Header("Events")]
        public event Action OnCalibrationComplete;
        public event Action OnCalibrationReset;
        
        private Transform cameraTransform;
        private float calibrationScale = 1f;
        private float calibrationRotationOffset = 0f;
        
        void Start()
        {
            cameraTransform = Camera.main?.transform;
            if (cameraTransform == null)
            {
                Debug.LogError("[CalibrationManager] Main camera not found!");
            }
        }
        
        /// <summary>
        /// Sets calibration using current headset position as a known SiteOwl coordinate.
        /// </summary>
        /// <param name="siteOwlX">Known SiteOwl X coordinate</param>
        /// <param name="siteOwlY">Known SiteOwl Y coordinate</param>
        public void SetCalibrationAnchor(float siteOwlX, float siteOwlY)
        {
            if (cameraTransform == null) return;
            
            CalibratedWorldPosition = cameraTransform.position;
            CalibratedSiteOwlXY = new Vector2(siteOwlX, siteOwlY);
            CalibratedRotation = cameraTransform.rotation;
            
            // Calculate rotation offset (assuming user is facing North/SiteOwl Y+)
            Vector3 forward = cameraTransform.forward;
            forward.y = 0;
            calibrationRotationOffset = Vector3.SignedAngle(Vector3.forward, forward, Vector3.up);
            
            IsCalibrated = true;
            
            Debug.Log($"[CalibrationManager] Calibrated at World: {CalibratedWorldPosition}, SiteOwl: ({siteOwlX}, {siteOwlY}), Rotation: {calibrationRotationOffset:F1}°");
            
            OnCalibrationComplete?.Invoke();
        }
        
        /// <summary>
        /// Transforms current headset position to SiteOwl X/Y coordinates.
        /// </summary>
        public Vector2 WorldToSiteOwl(Vector3 worldPosition)
        {
            if (!IsCalibrated)
            {
                Debug.LogWarning("[CalibrationManager] Not calibrated! Returning zero.");
                return Vector2.zero;
            }
            
            // Offset from calibration point
            Vector3 offset = worldPosition - CalibratedWorldPosition;
            offset.y = 0; // Ignore height
            
            // Apply rotation to align with SiteOwl coordinate system
            Quaternion rotation = Quaternion.Euler(0, -calibrationRotationOffset, 0);
            Vector3 rotatedOffset = rotation * offset;
            
            // Apply scale and add to calibration anchor
            float siteOwlX = CalibratedSiteOwlXY.x + (rotatedOffset.x * calibrationScale);
            float siteOwlY = CalibratedSiteOwlXY.y + (rotatedOffset.z * calibrationScale);
            
            return new Vector2(siteOwlX, siteOwlY);
        }
        
        /// <summary>
        /// Gets user's current position in SiteOwl space.
        /// </summary>
        public Vector2 GetCurrentSiteOwlPosition()
        {
            if (cameraTransform == null) return Vector2.zero;
            return WorldToSiteOwl(cameraTransform.position);
        }
        
        /// <summary>
        /// Gets user's current facing direction in degrees (0 = North/SiteOwl Y+).
        /// </summary>
        public float GetCurrentFacingDirection()
        {
            if (cameraTransform == null) return 0f;
            
            Vector3 forward = cameraTransform.forward;
            forward.y = 0;
            float currentRotation = Vector3.SignedAngle(Vector3.forward, forward, Vector3.up);
            float normalizedRotation = currentRotation - calibrationRotationOffset;
            
            // Normalize to 0-360
            while (normalizedRotation < 0) normalizedRotation += 360;
            while (normalizedRotation >= 360) normalizedRotation -= 360;
            
            return normalizedRotation;
        }
        
        /// <summary>
        /// Calculates estimated accuracy/confidence based on drift from calibration.
        /// </summary>
        public CoordinateConfidence CalculateConfidence()
        {
            if (!IsCalibrated) return CoordinateConfidence.NONE;
            
            // TODO: Implement drift detection based on:
            // - Time since calibration
            // - Distance from calibration point
            // - Tracking loss events
            
            float distanceFromCal = Vector3.Distance(cameraTransform.position, CalibratedWorldPosition);
            
            if (distanceFromCal < 10f) return CoordinateConfidence.HIGH;
            if (distanceFromCal < 30f) return CoordinateConfidence.MEDIUM;
            return CoordinateConfidence.LOW;
        }
        
        public void ResetCalibration()
        {
            IsCalibrated = false;
            CalibratedWorldPosition = Vector3.zero;
            CalibratedSiteOwlXY = Vector2.zero;
            
            Debug.Log("[CalibrationManager] Calibration reset.");
            OnCalibrationReset?.Invoke();
        }
    }
    
    public enum CoordinateConfidence
    {
        NONE,
        LOW,      // > 3m expected accuracy
        MEDIUM,   // 1-3m expected accuracy
        HIGH      // < 1m expected accuracy
    }
}
