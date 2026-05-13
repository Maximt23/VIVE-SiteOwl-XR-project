using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using SiteOwlXR.Models;
using SiteOwlXR.Services;

namespace SiteOwlXR.Uploaders
{
    /// <summary>
    /// Local/UNC path uploader with enterprise patterns
    /// </summary>
    public class LocalNetworkUploader : IPhotoUploader, IDisposable
    {
        public StorageProvider Provider => StorageProvider.LocalNetwork;
        public bool IsConfigured => !string.IsNullOrEmpty(_basePath);
        public bool IsAvailable { get; private set; } = true;
        
        private readonly string _basePath;
        private readonly bool _createDateFolders;
        private readonly ILogger _logger;
        private readonly RetryPolicy _retryPolicy;
        
        public LocalNetworkUploader(string basePath, bool createDateFolders = true, ILogger logger = null)
        {
            _basePath = basePath;
            _createDateFolders = createDateFolders;
            _logger = logger ?? new UnityLogger();
            _retryPolicy = new RetryPolicy(maxRetries: 3);
            
            // Ensure directory exists
            try
            {
                Directory.CreateDirectory(_basePath);
                IsAvailable = true;
            }
            catch (Exception ex)
            {
                _logger.LogError("LocalNetworkUploader initialization failed", ex, this);
                IsAvailable = false;
            }
        }
        
        public async Task<UploadResult> UploadAsync(
            string localFilePath,
            string deviceId,
            string siteId,
            CancellationToken cancellationToken = default)
        {
            if (!IsAvailable)
            {
                return UploadResult.FailureResult("Local network path not available");
            }
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            string uploadId = Guid.NewGuid().ToString();
            
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    // Build destination path
                    string destFolder = _basePath;
                    
                    if (_createDateFolders)
                    {
                        destFolder = Path.Combine(_basePath, DateTime.Now.ToString("yyyy"), DateTime.Now.ToString("MM"), DateTime.Now.ToString("dd"));
                    }
                    
                    string fileName = $"{siteId}_{deviceId}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                    string destPath = Path.Combine(destFolder, fileName);
                    
                    // Ensure directory exists
                    Directory.CreateDirectory(destFolder);
                    
                    // Handle file collision
                    destPath = HandleFileCollision(destPath);
                    
                    // Copy file
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    await Task.Run(() => File.Copy(localFilePath, destPath, overwrite: false), cancellationToken);
                    
                    // Verify
                    if (!File.Exists(destPath))
                    {
                        throw new IOException("File copy verification failed");
                    }
                    
                    var fileInfo = new FileInfo(destPath);
                    
                    stopwatch.Stop();
                    
                    _logger.LogInfo($"Uploaded to local storage: {destPath}", this);
                    
                    return UploadResult.SuccessResult(
                        new Uri(destPath).AbsoluteUri,
                        uploadId,
                        stopwatch.Elapsed
                    );
                    
                }, ex => ex is IOException || ex is UnauthorizedAccessException, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (RetryExhaustedException ex)
            {
                stopwatch.Stop();
                _logger.LogError($"Upload failed after retries: {ex.Message}", ex, this);
                return UploadResult.FailureResult(ex.Message, ex.InnerException);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError($"Upload failed: {ex.Message}", ex, this);
                return UploadResult.FailureResult(ex.Message, ex);
            }
        }
        
        public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IsAvailable && Directory.Exists(_basePath));
        }
        
        public float GetProgress(string uploadId)
        {
            // Local copy is synchronous, no progress tracking needed
            return 1.0f;
        }
        
        private string HandleFileCollision(string desiredPath)
        {
            if (!File.Exists(desiredPath))
            {
                return desiredPath;
            }
            
            string directory = Path.GetDirectoryName(desiredPath);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(desiredPath);
            string extension = Path.GetExtension(desiredPath);
            
            int counter = 1;
            string newPath;
            
            do
            {
                newPath = Path.Combine(directory, $"{fileNameWithoutExt}_{counter}{extension}");
                counter++;
            } while (File.Exists(newPath));
            
            return newPath;
        }
        
        public void Dispose()
        {
            // No disposable resources
        }
    }
}
