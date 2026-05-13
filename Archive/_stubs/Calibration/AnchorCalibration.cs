using UnityEngine;

namespace SiteOwlXR.Calibration
{
    /// <summary>
    /// Manages calibration anchor for SiteOwl coordinate transformation.
    /// TODO: Implement calibration workflow
    /// </summary>
    public class AnchorCalibration : MonoBehaviour
    {
        [Header("Calibration State")]
        public bool IsCalibrated = false;
        public Vector3 AnchorWorldPosition;
        public Vector2 AnchorSiteOwlXY;
        public float RotationOffset; // Degrees
        
        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnCalibrationComplete;
        public UnityEngine.Events.UnityEvent OnCalibrationReset;
        
        private Transform cameraTransform;
        
        void Start()
        {
            cameraTransform = Camera.main?.transform;
        }
        
        /// <summary>
        /// Sets the calibration anchor at current headset position.
        /// </summary>
        /// <param name="siteOwlX">Known SiteOwl X coordinate</param>
        /// <param name="siteOwlY">Known SiteOwl Y coordinate</param>
        public void SetAnchor(float siteOwlX, float siteOwlY)
        {
            if (cameraTransform == null) return;
            
            AnchorWorldPosition = cameraTransform.position;
            AnchorSiteOwlXY = new Vector2(siteOwlX, siteOwlY);
            
            // Calculate rotation offset (assuming user faces North / SiteOwl Y+)
            Vector3 forward = cameraTransform.forward;
            forward.y = 0;
            RotationOffset = Vector3.SignedAngle(Vector3.forward, forward, Vector3.up);
            
            IsCalibrated = true;
            
            Debug.Log($"[AnchorCalibration] Calibrated: World {AnchorWorldPosition}, SiteOwl ({siteOwlX}, {siteOwlY}), Rotation {RotationOffset:F1}°");
            
            OnCalibrationComplete?.Invoke();
        }
        
        /// <summary>
        /// Transforms world position to SiteOwl X/Y coordinates.
        /// </summary>
        public Vector2 WorldToSiteOwl(Vector3 worldPosition)
        {
            if (!IsCalibrated)
            {
                Debug.LogWarning("[AnchorCalibration] Not calibrated!");
                return Vector2.zero;
            }
            
            // Offset from anchor
            Vector3 offset = worldPosition - AnchorWorldPosition;
            offset.y = 0;
            
            // Apply rotation
            Quaternion rotation = Quaternion.Euler(0, -RotationOffset, 0);
            Vector3 rotatedOffset = rotation * offset;
            
            // Convert to SiteOwl coordinates
            float siteOwlX = AnchorSiteOwlXY.x + rotatedOffset.x;
            float siteOwlY = AnchorSiteOwlXY.y + rotatedOffset.z;
            
            return new Vector2(siteOwlX, siteOwlY);
        }
        
        /// <summary>
        /// Gets current headset position in SiteOwl coordinates.
        /// </summary>
        public Vector2 GetCurrentSiteOwlPosition()
        {
            if (cameraTransform == null) return Vector2.zero;
            return WorldToSiteOwl(cameraTransform.position);
        }
        
        /// <summary>
        /// Resets calibration state.
        /// </summary>
        public void ResetCalibration()
        {
            IsCalibrated = false;
            AnchorWorldPosition = Vector3.zero;
            AnchorSiteOwlXY = Vector2.zero;
            RotationOffset = 0;
            
            Debug.Log("[AnchorCalibration] Calibration reset.");
            OnCalibrationReset?.Invoke();
        }
    }
}
