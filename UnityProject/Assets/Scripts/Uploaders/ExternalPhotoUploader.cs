using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using SiteOwlXR.Models;
using SiteOwlXR.Services;
using SiteOwlXR.Events;
using SiteOwlXR.Configuration;

namespace SiteOwlXR.Uploaders
{
    /// <summary>
    /// External Photo Uploader - Enterprise Grade
    /// Orchestrates upload to configured provider with fallback
    /// </summary>
    public class ExternalPhotoUploader : MonoBehaviour
    {
        [Header("Configuration")]
        public StorageProvider primaryProvider = StorageProvider.LocalNetwork;
        public bool fallbackToLocal = true;
        
        [Header("Provider Settings")]
        public string localNetworkPath = "";
        
        private ILogger _logger;
        private SurveyConfiguration _config;
        private IPhotoUploader _primaryUploader;
        private IPhotoUploader _fallbackUploader;
        
        void Awake()
        {
            _logger = ServiceLocator.Get<ILogger>() ?? new UnityLogger();
            _config = SurveyConfiguration.Instance;
            
            InitializeUploaders();
        }
        
        void InitializeUploaders()
        {
            // Primary uploader
            switch (primaryProvider)
            {
                case StorageProvider.LocalNetwork:
                    string path = string.IsNullOrEmpty(localNetworkPath) ? 
                        Application.persistentDataPath : localNetworkPath;
                    _primaryUploader = new LocalNetworkUploader(path);
                    break;
                    
                // Add other providers here as they're implemented
                default:
                    _primaryUploader = new LocalNetworkUploader(Application.persistentDataPath);
                    break;
            }
            
            // Fallback uploader
            if (fallbackToLocal && primaryProvider != StorageProvider.LocalNetwork)
            {
                _fallbackUploader = new LocalNetworkUploader(Application.persistentDataPath);
            }
            
            _logger.LogInfo($"ExternalPhotoUploader initialized: Primary={primaryProvider}, Fallback={fallbackToLocal}", this);
        }
        
        /// <summary>
        /// Upload photo with retry and fallback
        /// </summary>
        public async Task<UploadResult> UploadAsync(string localFilePath, string deviceId, string siteId)
        {
            _logger.LogInfo($"Starting upload for {deviceId}", this);
            
            // Try primary
            if (_primaryUploader?.IsAvailable == true)
            {
                var result = await _primaryUploader.UploadAsync(localFilePath, deviceId, siteId);
                
                if (result.Success)
                {
                    _logger.LogInfo($"Upload successful: {result.RemoteUrl}", this);
                    CctvSurveyEvents.RaisePhotoUploaded(new DeviceRecord { DeviceName = deviceId }, result.RemoteUrl);
                    return result;
                }
                
                _logger.LogWarning($"Primary upload failed: {result.ErrorMessage}", this);
            }
            
            // Try fallback
            if (_fallbackUploader?.IsAvailable == true)
            {
                _logger.LogInfo("Trying fallback upload", this);
                var fallbackResult = await _fallbackUploader.UploadAsync(localFilePath, deviceId, siteId);
                
                if (fallbackResult.Success)
                {
                    _logger.LogInfo($"Fallback upload successful: {fallbackResult.RemoteUrl}", this);
                    CctvSurveyEvents.RaisePhotoUploaded(new DeviceRecord { DeviceName = deviceId }, fallbackResult.RemoteUrl);
                    return fallbackResult;
                }
                
                _logger.LogError($"Fallback upload also failed: {fallbackResult.ErrorMessage}", null, this);
            }
            
            // Both failed
            var failure = UploadResult.FailureResult("All upload methods failed");
            CctvSurveyEvents.RaiseCaptureError(new DeviceRecord { DeviceName = deviceId }, failure.ErrorMessage);
            return failure;
        }
    }
}
