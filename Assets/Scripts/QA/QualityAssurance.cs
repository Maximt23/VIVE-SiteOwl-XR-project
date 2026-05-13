using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using SiteOwlXR.Core;

namespace SiteOwlXR.QA
{
    // ── Result types ─────────────────────────────────────────────────────────

    public enum QAIssueType
    {
        MissingCoordinates,
        MissingPhoto,
        LowConfidence,
        DuplicateCoordinates,
        RequiresReview,
    }

    [Serializable]
    public class QAIssue
    {
        public string      DeviceID;
        public string      DeviceName;
        public QAIssueType IssueType;
        public string      Detail;

        public override string ToString() =>
            $"[{IssueType}] {DeviceName} ({DeviceID}): {Detail}";
    }

    [Serializable]
    public class QAReport
    {
        public DateTime        GeneratedAt;
        public int             TotalDevices;
        public int             CapturedDevices;
        public int             IssueCount;
        public List<QAIssue>   Issues = new();

        public bool IsClean => IssueCount == 0;

        public string Summary() =>
            $"QA Report — {GeneratedAt:yyyy-MM-dd HH:mm} UTC\n" +
            $"  Devices : {CapturedDevices}/{TotalDevices} captured\n" +
            $"  Issues  : {IssueCount}\n" +
            (IsClean ? "  Status  : PASS" : $"  Status  : FAIL ({IssueCount} issue(s))");
    }

    /// <summary>
    /// Quality Assurance checks run after every capture and on-demand.
    /// Wire to CaptureController.OnCaptureComplete for live checks.
    /// Call RunFullReport() before ending a survey session.
    /// </summary>
    public class QualityAssurance : MonoBehaviour
    {
        [Header("Thresholds")]
        [Tooltip("SiteOwl units. Two devices within this distance are flagged as duplicate coordinates.")]
        public float DuplicateCoordThreshold = 0.5f;

        [Header("Dependencies")]
        public CsvManager CsvManager;

        public event Action<QAIssue>   OnIssueFlagged;
        public event Action<QAReport>  OnReportReady;

        // ── Live capture check ────────────────────────────────────────────────

        /// <summary>Validates a single device immediately after capture.</summary>
        public void ValidateCapture(DeviceData device)
        {
            if (device == null) return;

            var issues = new List<QAIssue>();
            CheckMissingCoords(device, issues);
            CheckMissingPhoto(device, issues);
            CheckLowConfidence(device, issues);

            foreach (var issue in issues)
            {
                Debug.LogWarning($"[QA] {issue}");
                OnIssueFlagged?.Invoke(issue);
            }
        }

        // ── Full report ───────────────────────────────────────────────────────

        public QAReport RunFullReport()
        {
            var devices = CsvManager?.Devices;
            if (devices == null)
            {
                Debug.LogError("[QA] CsvManager not assigned or no data loaded.");
                return new QAReport { GeneratedAt = DateTime.UtcNow };
            }

            var report = new QAReport
            {
                GeneratedAt     = DateTime.UtcNow,
                TotalDevices    = devices.Count,
                CapturedDevices = devices.Count(d => d != null && !d.NeedsCapture),
            };

            // Per-device checks
            var captured = devices.Where(d => d != null && !d.NeedsCapture).ToList();
            foreach (var d in devices.Where(d => d != null))
            {
                CheckMissingCoords(d, report.Issues);
                CheckMissingPhoto(d, report.Issues);
                CheckLowConfidence(d, report.Issues);
                CheckRequiresReview(d, report.Issues);
            }

            // Cross-device duplicate coordinate check
            FindDuplicateCoordinates(captured, report.Issues);

            report.IssueCount = report.Issues.Count;

            Debug.Log($"[QA] {report.Summary()}");
            if (!report.IsClean)
                foreach (var i in report.Issues)
                    Debug.LogWarning($"[QA]   {i}");

            OnReportReady?.Invoke(report);
            return report;
        }

        // ── Individual checks ─────────────────────────────────────────────────

        private static void CheckMissingCoords(DeviceData d, List<QAIssue> issues)
        {
            if (!d.NeedsCapture) return;
            issues.Add(new QAIssue
            {
                DeviceID   = d.DeviceID,
                DeviceName = d.DeviceName,
                IssueType  = QAIssueType.MissingCoordinates,
                Detail     = "No coordinates captured yet.",
            });
        }

        private static void CheckMissingPhoto(DeviceData d, List<QAIssue> issues)
        {
            if (d.HasPhoto) return;
            issues.Add(new QAIssue
            {
                DeviceID   = d.DeviceID,
                DeviceName = d.DeviceName,
                IssueType  = QAIssueType.MissingPhoto,
                Detail     = "No photo recorded for this device.",
            });
        }

        private static void CheckLowConfidence(DeviceData d, List<QAIssue> issues)
        {
            if (d.NeedsCapture) return;
            if (d.CoordinateConfidence == CoordinateConfidence.HIGH ||
                d.CoordinateConfidence == CoordinateConfidence.MEDIUM) return;

            issues.Add(new QAIssue
            {
                DeviceID   = d.DeviceID,
                DeviceName = d.DeviceName,
                IssueType  = QAIssueType.LowConfidence,
                Detail     = $"Confidence={d.CoordinateConfidence}. Recommend re-capture closer to calibration anchor.",
            });
        }

        private static void CheckRequiresReview(DeviceData d, List<QAIssue> issues)
        {
            if (!d.RequiresReview) return;
            issues.Add(new QAIssue
            {
                DeviceID   = d.DeviceID,
                DeviceName = d.DeviceName,
                IssueType  = QAIssueType.RequiresReview,
                Detail     = "Flagged REVIEW_REQUIRED — low confidence or manual override needed.",
            });
        }

        private void FindDuplicateCoordinates(List<DeviceData> captured, List<QAIssue> issues)
        {
            for (int i = 0; i < captured.Count; i++)
            for (int j = i + 1; j < captured.Count; j++)
            {
                var a = captured[i]; var b = captured[j];
                if (!a.SiteOwlX.HasValue || !b.SiteOwlX.HasValue) continue;

                float dx   = a.SiteOwlX.Value - b.SiteOwlX.Value;
                float dy   = a.SiteOwlY.Value - b.SiteOwlY.Value;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist < DuplicateCoordThreshold)
                {
                    issues.Add(new QAIssue
                    {
                        DeviceID   = a.DeviceID,
                        DeviceName = a.DeviceName,
                        IssueType  = QAIssueType.DuplicateCoordinates,
                        Detail     = $"Within {dist:F2}u of '{b.DeviceName}' ({b.DeviceID}). Possible mis-capture.",
                    });
                }
            }
        }
    }
}
