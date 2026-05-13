using UnityEngine;
using SiteOwlXR.Models;
using SiteOwlXR.Events;
using SiteOwlXR.Services;

namespace SiteOwlXR.Managers
{
    /// <summary>
    /// Calibration Manager - Enterprise Grade
    /// Transforms Unity world space to SiteOwl X/Y coordinates
    /// </summary>
    public class CalibrationManager : MonoBehaviour
    {
        [Header("Configuration")]
        public float pixelsPerMeter = 10f;
        public float highConfidenceDistance = 10f;
        public float mediumConfidenceDistance = 30f;
        
        [Header("State")]
        public CalibrationAnchor CurrentAnchor { get; private set; }
        public bool IsCalibrated => CurrentAnchor.IsValid;
        
        private ILogger _logger;
        private Transform _cameraTransform;
        
        void Awake()
        {
            _logger = ServiceLocator.Get<ILogger>() ?? new UnityLogger();
            _cameraTransform = Camera.main?.transform;
        }
        
        /// <summary>
        /// Set calibration anchor at current position
        /// </summary>
        public void SetAnchor(float siteOwlX, float siteOwlY, double lat, double lon)
        {
            if (_cameraTransform == null)
            {
                _logger.LogError("Cannot calibrate - no camera reference", this);
                return;
            }
            
            Vector3 worldPos = _cameraTransform.position;
            float rotationY = _cameraTransform.eulerAngles.y;
            
            CurrentAnchor = new CalibrationAnchor
            {
                SiteOwlX = siteOwlX,
                SiteOwlY = siteOwlY,
                Latitude = lat,
                Longitude = lon,
                WorldPosition = worldPos,
                WorldRotationY = rotationY,
                MetersPerUnit = 1f / pixelsPerMeter,
                Timestamp = System.DateTime.UtcNow
            };
            
            _logger.LogInfo($"Calibration set: {CurrentAnchor}", this);
            CctvSurveyEvents.RaiseCalibrationSet(CurrentAnchor);
        }
        
        /// <summary>
        /// Convert Unity world position to SiteOwl X/Y
        /// </summary>
        public Vector2 WorldToSiteOwl(Vector3 worldPosition)
        {
            if (!IsCalibrated)
            {
                _logger.LogWarning("Attempting coordinate transform without calibration", this);
                return Vector2.zero;
            }
            
            return CurrentAnchor.WorldToSiteOwl(worldPosition);
        }
        
        /// <summary>
        /// Get current camera position in SiteOwl space
        /// </summary>
        public Vector2 GetCurrentSiteOwlPosition()
        {
            if (_cameraTransform == null || !IsCalibrated)
                return Vector2.zero;
                
            return WorldToSiteOwl(_cameraTransform.position);
        }
        
        /// <summary>
        /// Get confidence level for a position
        /// </summary>
        public CoordinateConfidence GetConfidence(Vector3 worldPosition)
        {
            if (!IsCalibrated)
                return CoordinateConfidence.NONE;
                
            return CurrentAnchor.GetConfidence(worldPosition);
        }
        
        /// <summary>
        /// Reset calibration
        /// </summary>
        public void ResetCalibration()
        {
            CurrentAnchor = default;
            _logger.LogInfo("Calibration reset", this);
            CctvSurveyEvents.RaiseCalibrationReset();
        }
    }
}
