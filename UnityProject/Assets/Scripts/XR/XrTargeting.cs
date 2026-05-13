using UnityEngine;
using SiteOwlXR.Core;

namespace SiteOwlXR.XR
{
    /// <summary>
    /// Drives the XR aiming reticle and pointer line.
    /// Attach to the Main Camera (or XR Rig camera).
    ///
    /// Inspector setup:
    ///   ReticleSphere  — small sphere prefab placed at raycast hit point
    ///   PointerLine    — LineRenderer for the beam from camera to target
    ///   Colors         — green = valid hit, yellow = max-distance fallback
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class XrTargeting : MonoBehaviour
    {
        [Header("Dependencies")]
        public CaptureController CaptureController;
        public CalibrationManager Calibration;

        [Header("Reticle")]
        public GameObject ReticlePrefab;
        [Tooltip("Scale of the reticle sphere in metres.")]
        public float ReticleSize = 0.05f;

        [Header("Pointer Beam")]
        public float  BeamWidth   = 0.005f;
        public Color  HitColor    = new Color(0.2f, 1f, 0.2f, 0.8f);   // green
        public Color  NoHitColor  = new Color(1f,   0.9f, 0.2f, 0.5f); // yellow
        public Color  NoCalColor  = new Color(0.5f, 0.5f, 0.5f, 0.3f); // grey

        // ── Private ───────────────────────────────────────────────────────────
        private LineRenderer _line;
        private GameObject   _reticle;
        private Transform    _cam;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.useWorldSpace = true;
            _line.startWidth    = BeamWidth;
            _line.endWidth      = BeamWidth * 0.5f;

            if (ReticlePrefab != null)
            {
                _reticle = Instantiate(ReticlePrefab);
                _reticle.transform.localScale = Vector3.one * ReticleSize;
            }
        }

        void Start()
        {
            _cam = Camera.main?.transform;
            if (_cam == null) Debug.LogError("[XrTargeting] Main camera not found!");
        }

        // ── Update ────────────────────────────────────────────────────────────
        void Update()
        {
            if (_cam == null) return;

            bool calibrated = Calibration != null && Calibration.IsCalibrated;

            if (!calibrated)
            {
                SetBeamColor(NoCalColor);
                HideReticle();
                DrawBeam(_cam.position, _cam.position + _cam.forward * 2f);
                return;
            }

            bool gotHit = CaptureController != null
                && CaptureController.GetCapturePoint(out Vector3 hitPoint, out float dist);

            Vector3 origin = _cam.position;
            Vector3 target = gotHit ? hitPoint : origin + _cam.forward * 50f;

            // Use CaptureController's actual MaxRaycastDistance if available
            float maxDist = CaptureController != null
                ? CaptureController.MaxRaycastDistance
                : 50f;

            bool isRealHit = gotHit && dist < maxDist - 0.01f;

            SetBeamColor(isRealHit ? HitColor : NoHitColor);
            DrawBeam(origin, target);
            PlaceReticle(target, isRealHit);
        }

        // ── Trigger capture from XR targeting ─────────────────────────────────
        /// <summary>Called by XR controller button binding if using Unity Input System.</summary>
        public void TriggerCapture()
        {
            CaptureController?.CaptureCurrentDevice();
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void DrawBeam(Vector3 from, Vector3 to)
        {
            _line.enabled = true;
            _line.SetPosition(0, from);
            _line.SetPosition(1, to);
        }

        private void SetBeamColor(Color c)
        {
            _line.startColor = c;
            _line.endColor   = new Color(c.r, c.g, c.b, c.a * 0.2f);
        }

        private void PlaceReticle(Vector3 pos, bool visible)
        {
            if (_reticle == null) return;
            _reticle.SetActive(visible);
            if (visible) _reticle.transform.position = pos;
        }

        private void HideReticle()
        {
            if (_reticle != null) _reticle.SetActive(false);
        }

        void OnDestroy()
        {
            if (_reticle != null) Destroy(_reticle);
        }
    }
}
