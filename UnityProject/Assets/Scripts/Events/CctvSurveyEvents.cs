using System;
using UnityEngine;
using SiteOwlXR.Models;

namespace SiteOwlXR.Events
{
    /// <summary>
    /// Static event bus for all survey-related events.
    /// No MonoBehaviour. No singleton. Subscribe and unsubscribe to prevent leaks.
    /// </summary>
    public static class CctvSurveyEvents
    {
        // === XR & SYSTEM EVENTS ===
        
        /// <summary>
        /// XR subsystem is ready and tracking
        /// </summary>
        public static event Action OnXrReady;
        public static void RaiseXrReady() => OnXrReady?.Invoke();
        
        /// <summary>
        /// XR initialization failed or timed out
        /// </summary>
        public static event Action<string> OnXrError;
        public static void RaiseXrError(string error) => OnXrError?.Invoke(error);
        
        // === GPS EVENTS ===
        
        /// <summary>
        /// GPS signal acquired with good accuracy
        /// </summary>
        public static event Action<GpsReading> OnGpsAcquired;
        public static void RaiseGpsAcquired(GpsReading reading) => OnGpsAcquired?.Invoke(reading);
        
        /// <summary>
        /// GPS accuracy improved (better signal)
        /// </summary>
        public static event Action<GpsReading> OnGpsImproved;
        public static void RaiseGpsImproved(GpsReading reading) => OnGpsImproved?.Invoke(reading);
        
        /// <summary>
        /// GPS signal lost
        /// </summary>
        public static event Action OnGpsLost;
        public static void RaiseGpsLost() => OnGpsLost?.Invoke();
        
        // === CALIBRATION EVENTS ===
        
        /// <summary>
        /// Calibration anchor has been set
        /// </summary>
        public static event Action<CalibrationAnchor> OnCalibrationSet;
        public static void RaiseCalibrationSet(CalibrationAnchor anchor) => OnCalibrationSet?.Invoke(anchor);
        
        /// <summary>
        /// Calibration was reset
        /// </summary>
        public static event Action OnCalibrationReset;
        public static void RaiseCalibrationReset() => OnCalibrationReset?.Invoke();
        
        // === DEVICE & SELECTION EVENTS ===
        
        /// <summary>
        /// CSV data loaded successfully
        /// </summary>
        public static event Action<int> OnDevicesLoaded;  // count
        public static void RaiseDevicesLoaded(int count) => OnDevicesLoaded?.Invoke(count);
        
        /// <summary>
        /// User selected a device from the list
        /// </summary>
        public static event Action<DeviceRecord> OnDeviceSelected;
        public static void RaiseDeviceSelected(DeviceRecord device) => OnDeviceSelected?.Invoke(device);
        
        /// <summary>
        /// Device was updated (captured)
        /// </summary>
        public static event Action<DeviceRecord> OnDeviceUpdated;
        public static void RaiseDeviceUpdated(DeviceRecord device) => OnDeviceUpdated?.Invoke(device);
        
        // === CAPTURE WORKFLOW EVENTS ===
        
        /// <summary>
        /// Capture started for a device
        /// </summary>
        public static event Action<DeviceRecord> OnCaptureStarted;
        public static void RaiseCaptureStarted(DeviceRecord device) => OnCaptureStarted?.Invoke(device);
        
        /// <summary>
        /// GPS captured for device
        /// </summary>
        public static event Action<DeviceRecord, GpsReading> OnGpsCaptured;
        public static void RaiseGpsCaptured(DeviceRecord device, GpsReading gps) => OnGpsCaptured?.Invoke(device, gps);
        
        /// <summary>
        /// SiteOwl X/Y captured from raycast
        /// </summary>
        public static event Action<DeviceRecord, Vector2, float> OnXyCaptured;  // device, siteOwlXY, confidence
        public static void RaiseXyCaptured(DeviceRecord device, Vector2 xy, float confidence) => OnXyCaptured?.Invoke(device, xy, confidence);
        
        /// <summary>
        /// Photo captured locally
        /// </summary>
        public static event Action<DeviceRecord, string> OnPhotoCaptured;  // device, localPath
        public static void RaisePhotoCaptured(DeviceRecord device, string localPath) => OnPhotoCaptured?.Invoke(device, localPath);
        
        /// <summary>
        /// Photo uploaded to external storage
        /// </summary>
        public static event Action<DeviceRecord, string> OnPhotoUploaded;  // device, remoteUrl
        public static void RaisePhotoUploaded(DeviceRecord device, string remoteUrl) => OnPhotoUploaded?.Invoke(device, remoteUrl);
        
        /// <summary>
        /// AI suggestions ready
        /// </summary>
        public static event Action<DeviceRecord, System.Collections.Generic.List<AiSuggestion>> OnAiSuggestionReady;
        public static void RaiseAiSuggestionReady(DeviceRecord device, System.Collections.Generic.List<AiSuggestion> suggestions) => OnAiSuggestionReady?.Invoke(device, suggestions);
        
        /// <summary>
        /// User confirmed AI suggestion
        /// </summary>
        public static event Action<DeviceRecord, string> OnAiConfirmed;  // device, confirmedType
        public static void RaiseAiConfirmed(DeviceRecord device, string confirmedType) => OnAiConfirmed?.Invoke(device, confirmedType);
        
        /// <summary>
        /// User skipped AI (manual entry)
        /// </summary>
        public static event Action<DeviceRecord> OnAiSkipped;
        public static void RaiseAiSkipped(DeviceRecord device) => OnAiSkipped?.Invoke(device);
        
        /// <summary>
        /// Capture completed successfully
        /// </summary>
        public static event Action<DeviceRecord> OnCaptureComplete;
        public static void RaiseCaptureComplete(DeviceRecord device) => OnCaptureComplete?.Invoke(device);
        
        /// <summary>
        /// Capture failed with error
        /// </summary>
        public static event Action<DeviceRecord, string> OnCaptureError;
        public static void RaiseCaptureError(DeviceRecord device, string error) => OnCaptureError?.Invoke(device, error);
        
        /// <summary>
        /// Progress update during capture (for UI)
        /// </summary>
        public static event Action<DeviceRecord, string, float> OnCaptureProgress;  // device, step, percent
        public static void RaiseCaptureProgress(DeviceRecord device, string step, float percent) => OnCaptureProgress?.Invoke(device, step, percent);
        
        // === CSV & DATA EVENTS ===
        
        /// <summary>
        /// CSV saved successfully
        /// </summary>
        public static event Action<string> OnCsvSaved;  // filePath
        public static void RaiseCsvSaved(string filePath) => OnCsvSaved?.Invoke(filePath);
        
        /// <summary>
        /// CSV save failed
        /// </summary>
        public static event Action<string> OnCsvError;  // error message
        public static void RaiseCsvError(string error) => OnCsvError?.Invoke(error);
        
        /// <summary>
        /// Backup created before write
        /// </summary>
        public static event Action<string> OnBackupCreated;  // backupPath
        public static void RaiseBackupCreated(string backupPath) => OnBackupCreated?.Invoke(backupPath);
        
        // === UI & STATE EVENTS ===
        
        /// <summary>
        /// Survey session started
        /// </summary>
        public static event Action<CaptureSession> OnSessionStarted;
        public static void RaiseSessionStarted(CaptureSession session) => OnSessionStarted?.Invoke(session);
        
        /// <summary>
        /// Survey session ended
        /// </summary>
        public static event Action<CaptureSession> OnSessionEnded;
        public static void RaiseSessionEnded(CaptureSession session) => OnSessionEnded?.Invoke(session);
        
        /// <summary>
        /// All devices captured (survey complete)
        /// </summary>
        public static event Action OnSurveyComplete;
        public static void RaiseSurveyComplete() => OnSurveyComplete?.Invoke();
        
        // === UTILITY ===
        
        /// <summary>
        /// Unsubscribe all listeners. Call on app shutdown to prevent memory leaks.
        /// </summary>
        public static void UnsubscribeAll()
        {
            OnXrReady = null;
            OnXrError = null;
            OnGpsAcquired = null;
            OnGpsImproved = null;
            OnGpsLost = null;
            OnCalibrationSet = null;
            OnCalibrationReset = null;
            OnDevicesLoaded = null;
            OnDeviceSelected = null;
            OnDeviceUpdated = null;
            OnCaptureStarted = null;
            OnGpsCaptured = null;
            OnXyCaptured = null;
            OnPhotoCaptured = null;
            OnPhotoUploaded = null;
            OnAiSuggestionReady = null;
            OnAiConfirmed = null;
            OnAiSkipped = null;
            OnCaptureComplete = null;
            OnCaptureError = null;
            OnCaptureProgress = null;
            OnCsvSaved = null;
            OnCsvError = null;
            OnBackupCreated = null;
            OnSessionStarted = null;
            OnSessionEnded = null;
            OnSurveyComplete = null;
        }
    }
}
