using UnityEngine;
using SiteOwlXR.Core;
using SiteOwlXR.AI;
using SiteOwlXR.UI;
using SiteOwlXR.XR;
using SiteOwlXR.QA;

/// <summary>
/// AUTO-WIRES the entire scene at runtime.
///
/// HOW TO USE IN UNITY EDITOR (10 minutes total):
///   1. File > New Scene → save as Assets/Scenes/MainScene.unity
///   2. Create these GameObjects (right-click Hierarchy > Create Empty):
///        "XR Rig"          — attach: SceneBootstrapper, CaptureController,
///                                    CalibrationManager, CsvManager, PhotoCapture,
///                                    GitAutoPush, RealGpsManager, PermissionsManager
///        "AI"              — attach: DeviceRecognizer, DeviceLibraryLoader,
///                                    ExternalDataConnector, AiCopilotUI
///        "UI"              — attach: CaptureUI
///        "XR Targeting"    — attach: XrTargeting, LineRenderer (Add Component)
///        "QA"              — attach: QualityAssurance, MetadataManager
///        "EventSystem"     — GameObject > UI > Event System
///   3. On "XR Rig": Add Component > XR > XR Origin (or use VIVE's prefab)
///   4. File > Build Settings > Add Open Scenes → Build
///
/// Everything else is wired HERE — no manual Inspector dragging needed.
/// </summary>
public class SceneBootstrapper : MonoBehaviour
{
    void Awake()
    {
        // Find every singleton in the scene
        var permissions  = FindObjectOfType<PermissionsManager>();
        var calibration  = FindObjectOfType<CalibrationManager>();
        var csvManager   = FindObjectOfType<CsvManager>();
        var photoCapture = FindObjectOfType<PhotoCapture>();
        var captureCtrl  = FindObjectOfType<CaptureController>();
        var gitPush      = FindObjectOfType<GitAutoPush>();
        var gpsManager   = FindObjectOfType<RealGpsManager>();
        var recognizer   = FindObjectOfType<DeviceRecognizer>();
        var libLoader    = FindObjectOfType<DeviceLibraryLoader>();
        var aiUI         = FindObjectOfType<AiCopilotUI>();
        var captureUI    = FindObjectOfType<CaptureUI>();
        var xrTarget     = FindObjectOfType<XrTargeting>();
        var qa           = FindObjectOfType<QualityAssurance>();
        var metadata     = FindObjectOfType<MetadataManager>();

        // ── CaptureController ─────────────────────────────────────────────────
        if (captureCtrl != null)
        {
            captureCtrl.Calibration  = calibration;
            captureCtrl.CsvManager   = csvManager;
            captureCtrl.PhotoCapture = photoCapture;
            captureCtrl.GpsManager   = gpsManager;
            captureCtrl.CaptureUI    = captureUI;
            captureCtrl.CameraTransform = Camera.main?.transform;
        }

        // ── CaptureUI ─────────────────────────────────────────────────────────
        if (captureUI != null)
        {
            captureUI.Calibration       = calibration;
            captureUI.CsvManager        = csvManager;
            captureUI.CaptureController = captureCtrl;
        }

        // ── GitAutoPush ───────────────────────────────────────────────────────
        if (gitPush != null)
            gitPush.CsvManager = csvManager;

        // ── AI ────────────────────────────────────────────────────────────────
        if (aiUI != null)
        {
            aiUI.recognizer       = recognizer;
            aiUI.captureController = captureCtrl;
        }

        // ── XR Targeting ──────────────────────────────────────────────────────
        if (xrTarget != null)
        {
            xrTarget.CaptureController = captureCtrl;
            xrTarget.Calibration       = calibration;
        }

        // ── QA ────────────────────────────────────────────────────────────────
        if (qa != null)
            qa.CsvManager = csvManager;

        if (metadata != null)
            metadata.CsvManager = csvManager;

        // ── Wire QA into capture flow ─────────────────────────────────────────
        if (captureCtrl != null && qa != null)
            captureCtrl.OnCaptureComplete += qa.ValidateCapture;

        // ── PhotoCapture output paths ─────────────────────────────────────────
        // Point to the external designs folder if it exists, else use app data
        if (photoCapture != null)
        {
            string externalPath = @"C:\VIVE-SiteOwl-XR-Designs\Meta data\Photos";
            photoCapture.PhotosOutputPath = System.IO.Directory.Exists(externalPath)
                ? externalPath
                : "";    // blank = Application.persistentDataPath/CapturedPhotos
        }

        // ── CSV auto-load ─────────────────────────────────────────────────────
        // Finds the first CSV in persistentDataPath (transferred via ADB or WiFi)
        if (csvManager != null && !csvManager.IsLoaded)
            TryAutoLoadCsv(csvManager);

        Debug.Log("[SceneBootstrapper] Scene wired. All components connected.");
    }

    private static void TryAutoLoadCsv(CsvManager csvManager)
    {
        string dataPath = Application.persistentDataPath;
        string[] csvFiles = System.IO.Directory.GetFiles(dataPath, "*.csv",
            System.IO.SearchOption.TopDirectoryOnly);

        if (csvFiles.Length == 0)
        {
            Debug.LogWarning(
                "[SceneBootstrapper] No CSV found in: " + dataPath + "\n" +
                "Transfer your survey CSV via ADB:\n" +
                $"  adb push Store_XXXX_CCTV.csv \"/sdcard/Android/data/com.siteowl.xr.capture/files/\"");
            return;
        }

        // Prefer most recently modified file
        System.Array.Sort(csvFiles,
            (a, b) => System.IO.File.GetLastWriteTimeUtc(b)
                          .CompareTo(System.IO.File.GetLastWriteTimeUtc(a)));

        Debug.Log($"[SceneBootstrapper] Auto-loading CSV: {System.IO.Path.GetFileName(csvFiles[0])}");
        csvManager.LoadCsv(csvFiles[0]);
    }
}
