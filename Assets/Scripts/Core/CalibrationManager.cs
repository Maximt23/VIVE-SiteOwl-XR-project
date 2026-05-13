using UnityEngine;
using System;

namespace SiteOwlXR.Core
{
    /// <summary>
    /// Transforms VIVE headset world-space into SiteOwl X/Y floor-plan coordinates.
    ///
    /// Calibration flow:
    ///   1. Single-anchor (fast)  — SetCalibrationAnchor(x, y)
    ///   2. Two-point scale (accurate) — SetSecondScalePoint(x2, y2) after anchor
    ///
    /// Session persistence: calibration survives app restarts via PlayerPrefs.
    /// Confidence decays over time and distance from the anchor point.
    /// </summary>
    public class CalibrationManager : MonoBehaviour
    {
        // ── PlayerPrefs keys ─────────────────────────────────────────────────
        private const string PP_CALIBRATED   = "cal_calibrated";
        private const string PP_SITE_X       = "cal_site_x";
        private const string PP_SITE_Y       = "cal_site_y";
        private const string PP_ROT_OFFSET   = "cal_rot_offset";
        private const string PP_SCALE        = "cal_scale";
        private const string PP_TIMESTAMP    = "cal_timestamp";

        // ── Confidence decay ─────────────────────────────────────────────────
        [Header("Confidence Thresholds")]
        [Tooltip("Distance (m) below which confidence is HIGH.")]
        public float HighDistanceThreshold   = 10f;
        [Tooltip("Distance (m) below which confidence is MEDIUM.")]
        public float MediumDistanceThreshold = 30f;
        [Tooltip("Minutes after which confidence degrades one level.")]
        public float ConfidenceDecayMinutes  = 10f;

        // ── Public state ─────────────────────────────────────────────────────
        public bool      IsCalibrated              { get; private set; }
        public Vector3   CalibratedWorldPosition   { get; private set; }
        public Vector2   CalibratedSiteOwlXY       { get; private set; }
        public Quaternion CalibratedRotation       { get; private set; }
        public float     CalibrationScale          { get; private set; } = 1f;
        public DateTime  CalibrationTime           { get; private set; }

        public event Action OnCalibrationComplete;
        public event Action OnCalibrationReset;
        public event Action OnScaleCalibrated;

        // ── Private ───────────────────────────────────────────────────────────
        private Transform _cam;
        private float     _rotationOffset;

        // Two-point scale calibration state
        private bool    _awaitingSecondPoint;
        private Vector3 _firstWorldPoint;
        private Vector2 _firstSiteOwlPoint;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        void Start()
        {
            _cam = Camera.main?.transform;
            if (_cam == null)
                Debug.LogError("[CalibrationManager] Main camera not found!");

            TryLoadSession();
        }

        // ── Single-anchor calibration ─────────────────────────────────────────

        /// <summary>
        /// Anchors the current headset position to a known SiteOwl coordinate.
        /// Call this when the surveyor is standing at a marked floor point.
        /// </summary>
        public void SetCalibrationAnchor(float siteOwlX, float siteOwlY)
        {
            if (_cam == null) return;

            CalibratedWorldPosition = _cam.position;
            CalibratedSiteOwlXY     = new Vector2(siteOwlX, siteOwlY);
            CalibratedRotation      = _cam.rotation;
            CalibrationTime         = DateTime.UtcNow;

            Vector3 fwd = _cam.forward; fwd.y = 0f;
            _rotationOffset = Vector3.SignedAngle(Vector3.forward, fwd, Vector3.up);

            // Preserve scale from a previous two-point calibration if it exists
            // (CalibrationScale already set — don't overwrite with 1f)
            IsCalibrated = true;

            SaveSession();
            Debug.Log($"[CalibrationManager] Anchor set — SiteOwl ({siteOwlX:F2}, {siteOwlY:F2})" +
                      $"  rot={_rotationOffset:F1}°  scale={CalibrationScale:F4}");
            OnCalibrationComplete?.Invoke();
        }

        // ── Two-point scale calibration ───────────────────────────────────────

        /// <summary>
        /// Begins a two-point scale calibration.
        /// Call SetCalibrationAnchor first, then walk to a second known point
        /// and call SetSecondScalePoint with its SiteOwl coordinates.
        /// </summary>
        public void BeginScaleCalibration()
        {
            if (!IsCalibrated)
            {
                Debug.LogWarning("[CalibrationManager] Set anchor first before scale calibration.");
                return;
            }
            _firstWorldPoint   = _cam.position;
            _firstSiteOwlPoint = CalibratedSiteOwlXY;
            _awaitingSecondPoint = true;
            Debug.Log("[CalibrationManager] Walk to second known point and call SetSecondScalePoint().");
        }

        /// <summary>
        /// Completes two-point scale calibration. Call after BeginScaleCalibration()
        /// when the surveyor is standing at the second known floor marker.
        /// </summary>
        public void SetSecondScalePoint(float siteOwlX2, float siteOwlY2)
        {
            if (!_awaitingSecondPoint)
            {
                Debug.LogWarning("[CalibrationManager] Call BeginScaleCalibration() first.");
                return;
            }

            _awaitingSecondPoint = false;

            Vector3 worldDelta  = _cam.position - _firstWorldPoint;
            worldDelta.y = 0f;
            float worldDist  = worldDelta.magnitude;

            Vector2 siteDelta = new Vector2(siteOwlX2, siteOwlY2) - _firstSiteOwlPoint;
            float   siteDist  = siteDelta.magnitude;

            if (worldDist < 0.1f || siteDist < 0.1f)
            {
                Debug.LogWarning("[CalibrationManager] Points too close — scale unchanged.");
                return;
            }

            CalibrationScale = siteDist / worldDist;
            SaveSession();

            Debug.Log($"[CalibrationManager] Scale calibrated: {CalibrationScale:F4} " +
                      $"(worldDist={worldDist:F2}m  siteDist={siteDist:F2}u)");
            OnScaleCalibrated?.Invoke();
        }

        // ── Coordinate transforms ─────────────────────────────────────────────

        public Vector2 WorldToSiteOwl(Vector3 worldPosition)
        {
            if (!IsCalibrated) return Vector2.zero;

            Vector3 offset = worldPosition - CalibratedWorldPosition;
            offset.y = 0f;

            Vector3 rotated = Quaternion.Euler(0f, -_rotationOffset, 0f) * offset;
            return new Vector2(
                CalibratedSiteOwlXY.x + rotated.x * CalibrationScale,
                CalibratedSiteOwlXY.y + rotated.z * CalibrationScale);
        }

        public Vector2 GetCurrentSiteOwlPosition() =>
            _cam != null ? WorldToSiteOwl(_cam.position) : Vector2.zero;

        public float GetCurrentFacingDirection()
        {
            if (_cam == null) return 0f;
            Vector3 fwd = _cam.forward; fwd.y = 0f;
            float angle = Vector3.SignedAngle(Vector3.forward, fwd, Vector3.up);
            return Mathf.Repeat(angle - _rotationOffset, 360f);
        }

        // ── Confidence ────────────────────────────────────────────────────────

        /// <summary>
        /// Confidence based on distance from anchor AND time since calibration.
        /// Decays one level per <see cref="ConfidenceDecayMinutes"/> elapsed.
        /// </summary>
        public CoordinateConfidence CalculateConfidence()
        {
            if (!IsCalibrated || _cam == null) return CoordinateConfidence.NONE;

            float dist          = Vector3.Distance(_cam.position, CalibratedWorldPosition);
            float elapsedMins   = (float)(DateTime.UtcNow - CalibrationTime).TotalMinutes;
            int   decayLevels   = Mathf.FloorToInt(elapsedMins / ConfidenceDecayMinutes);

            CoordinateConfidence distBased;
            if      (dist < HighDistanceThreshold)   distBased = CoordinateConfidence.HIGH;
            else if (dist < MediumDistanceThreshold)  distBased = CoordinateConfidence.MEDIUM;
            else                                       distBased = CoordinateConfidence.LOW;

            int level = (int)distBased - decayLevels;
            return (CoordinateConfidence)Mathf.Max(level, (int)CoordinateConfidence.LOW);
        }

        // ── Session persistence ───────────────────────────────────────────────

        private void SaveSession()
        {
            PlayerPrefs.SetInt(PP_CALIBRATED, 1);
            PlayerPrefs.SetFloat(PP_SITE_X,     CalibratedSiteOwlXY.x);
            PlayerPrefs.SetFloat(PP_SITE_Y,     CalibratedSiteOwlXY.y);
            PlayerPrefs.SetFloat(PP_ROT_OFFSET, _rotationOffset);
            PlayerPrefs.SetFloat(PP_SCALE,      CalibrationScale);
            PlayerPrefs.SetString(PP_TIMESTAMP, CalibrationTime.ToString("O"));
            PlayerPrefs.Save();
            Debug.Log("[CalibrationManager] Session saved to PlayerPrefs.");
        }

        private void TryLoadSession()
        {
            if (PlayerPrefs.GetInt(PP_CALIBRATED, 0) != 1) return;

            // On restart the headset is at a new world origin — we can restore
            // the SiteOwl anchor values and scale, but world position must be
            // re-anchored. Mark as NOT calibrated so the UI prompts the user,
            // but pre-populate the last known coordinate fields as defaults.
            float sx  = PlayerPrefs.GetFloat(PP_SITE_X, 0f);
            float sy  = PlayerPrefs.GetFloat(PP_SITE_Y, 0f);
            float rot = PlayerPrefs.GetFloat(PP_ROT_OFFSET, 0f);
            float sc  = PlayerPrefs.GetFloat(PP_SCALE, 1f);
            string ts = PlayerPrefs.GetString(PP_TIMESTAMP, "");

            CalibratedSiteOwlXY = new Vector2(sx, sy);
            CalibrationScale    = sc;
            _rotationOffset     = rot;

            if (DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind,
                                  out DateTime saved))
                CalibrationTime = saved;

            Debug.Log($"[CalibrationManager] Restored last session: SiteOwl({sx:F2},{sy:F2})" +
                      $" scale={sc:F4} — re-anchor required on startup.");
            // IsCalibrated stays false — surveyor must physically re-anchor.
        }

        public float GetLastKnownSiteOwlX() => CalibratedSiteOwlXY.x;
        public float GetLastKnownSiteOwlY() => CalibratedSiteOwlXY.y;

        public void ResetCalibration()
        {
            IsCalibrated            = false;
            CalibratedWorldPosition = Vector3.zero;
            CalibratedSiteOwlXY     = Vector2.zero;
            CalibrationScale        = 1f;
            _rotationOffset         = 0f;
            _awaitingSecondPoint    = false;
            PlayerPrefs.DeleteKey(PP_CALIBRATED);
            Debug.Log("[CalibrationManager] Calibration reset.");
            OnCalibrationReset?.Invoke();
        }
    }

    public enum CoordinateConfidence
    {
        NONE   = 0,
        LOW    = 1,   // > 3m expected error
        MEDIUM = 2,   // 1–3m expected error
        HIGH   = 3    // < 1m expected error
    }
}
