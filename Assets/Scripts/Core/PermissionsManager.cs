using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SiteOwlXR.Core
{
    /// <summary>
    /// Requests Android runtime permissions in order before the app proceeds.
    /// Blocks the capture workflow until Camera and Location are granted.
    /// Storage permissions handled separately for Android 10/11+ compatibility.
    ///
    /// Wire this as the very first component to run — set Script Execution Order
    /// to execute before CalibrationManager and CaptureController.
    /// </summary>
    public class PermissionsManager : MonoBehaviour
    {
        [Header("UI (optional — shows status while requesting)")]
        public GameObject PermissionsBlockerPanel;
        public TextMeshProUGUI StatusText;
        public Button RetryButton;

        [Header("Required Permissions")]
        public bool RequireCamera   = true;
        public bool RequireLocation = true;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Fired once all required permissions are granted.</summary>
        public event Action OnPermissionsGranted;
        /// <summary>Fired if the user permanently denies a required permission.</summary>
        public event Action<string> OnPermissionDenied;

        public bool AllGranted { get; private set; } = false;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            StartCoroutine(RequestPermissionsFlow());
#else
            // In editor or non-Android builds, skip — assume granted
            AllGranted = true;
            OnPermissionsGranted?.Invoke();
            if (PermissionsBlockerPanel != null)
                PermissionsBlockerPanel.SetActive(false);
#endif
        }

        // ── Permission flow ───────────────────────────────────────────────────

        private IEnumerator RequestPermissionsFlow()
        {
            ShowBlocker("Checking permissions...");

            if (RetryButton != null)
                RetryButton.onClick.AddListener(() => StartCoroutine(RequestPermissionsFlow()));

            var needed = new List<string>();

            if (RequireCamera &&
                !UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                    UnityEngine.Android.Permission.Camera))
                needed.Add(UnityEngine.Android.Permission.Camera);

            if (RequireLocation &&
                !UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                    UnityEngine.Android.Permission.FineLocation))
                needed.Add(UnityEngine.Android.Permission.FineLocation);

            if (needed.Count == 0)
            {
                GrantAll();
                yield break;
            }

            foreach (var permission in needed)
            {
                string label = FriendlyName(permission);
                ShowBlocker($"Requesting {label} permission...");

                UnityEngine.Android.Permission.RequestUserPermission(permission);

                // Wait for dialog to close — Unity doesn't expose a callback,
                // so we poll for up to 10 seconds
                float waited = 0f;
                while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission)
                       && waited < 10f)
                {
                    yield return new WaitForSeconds(0.25f);
                    waited += 0.25f;
                }

                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
                {
                    ShowBlocker(
                        $"{label} permission denied.\n" +
                        "Please allow it in Android Settings and tap Retry.",
                        showRetry: true);
                    OnPermissionDenied?.Invoke(permission);
                    yield break;
                }
            }

            GrantAll();
        }

        private void GrantAll()
        {
            AllGranted = true;
            if (PermissionsBlockerPanel != null)
                PermissionsBlockerPanel.SetActive(false);
            Debug.Log("[PermissionsManager] All required permissions granted.");
            OnPermissionsGranted?.Invoke();
        }

        private void ShowBlocker(string message, bool showRetry = false)
        {
            if (PermissionsBlockerPanel != null)
                PermissionsBlockerPanel.SetActive(true);
            if (StatusText != null)
                StatusText.text = message;
            if (RetryButton != null)
                RetryButton.gameObject.SetActive(showRetry);
        }

        private static string FriendlyName(string permission) => permission switch
        {
            UnityEngine.Android.Permission.Camera       => "Camera",
            UnityEngine.Android.Permission.FineLocation => "Location",
            _ => permission.Split('.')[^1]
        };
    }
}
