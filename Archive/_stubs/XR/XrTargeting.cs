using UnityEngine;

namespace SiteOwlXR.XR
{
    /// <summary>
    /// Handles XR raycast targeting for device capture.
    /// TODO: Implement raycast and hit detection
    /// </summary>
    public class XrTargeting : MonoBehaviour
    {
        [Header("Raycast Settings")]
        public float maxDistance = 50f;
        public LayerMask targetLayers = ~0; // All layers
        
        [Header("Visual Feedback")]
        public LineRenderer lineRenderer;
        public Transform reticle;
        
        private Transform cameraTransform;
        private RaycastHit currentHit;
        private bool hasValidTarget = false;
        
        void Start()
        {
            cameraTransform = Camera.main?.transform;
        }
        
        void Update()
        {
            PerformRaycast();
            UpdateVisuals();
        }
        
        /// <summary>
        /// Performs raycast from headset to find target point.
        /// </summary>
        void PerformRaycast()
        {
            if (cameraTransform == null) return;
            
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            hasValidTarget = Physics.Raycast(ray, out currentHit, maxDistance, targetLayers);
        }
        
        /// <summary>
        /// Updates visual feedback (line and reticle).
        /// </summary>
        void UpdateVisuals()
        {
            // TODO: Update line renderer
            // TODO: Move reticle to hit point
            // TODO: Change color based on validity
        }
        
        /// <summary>
        /// Gets the current target point in world space.
        /// </summary>
        public Vector3 GetTargetPoint()
        {
            if (hasValidTarget)
            {
                return currentHit.point;
            }
            
            // Return point at max distance if no hit
            return Camera.main.transform.position + Camera.main.transform.forward * maxDistance;
        }
        
        /// <summary>
        /// Returns true if currently pointing at a valid surface.
        /// </summary>
        public bool HasValidTarget()
        {
            return hasValidTarget;
        }
        
        /// <summary>
        /// Trigger capture at current target point.
        /// </summary>
        public void TriggerCapture()
        {
            if (!hasValidTarget) return;
            
            Vector3 targetPoint = GetTargetPoint();
            Debug.Log("[XrTargeting] Capture triggered at: " + targetPoint);
            
            // TODO: Notify capture controller
            // TODO: Convert to SiteOwl coordinates
            // TODO: Take photo
        }
    }
}
