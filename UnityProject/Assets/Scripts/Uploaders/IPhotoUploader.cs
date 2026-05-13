using System;
using System.Threading;
using System.Threading.Tasks;
using SiteOwlXR.Models;

namespace SiteOwlXR.Uploaders
{
    /// <summary>
    /// Interface for photo upload implementations
    /// Enterprise pattern: Async with cancellation support
    /// </summary>
    public interface IPhotoUploader
    {
        StorageProvider Provider { get; }
        bool IsConfigured { get; }
        bool IsAvailable { get; }  // Circuit breaker pattern
        
        /// <summary>
        /// Upload photo with retry logic and cancellation support
        /// </summary>
        Task<UploadResult> UploadAsync(
            string localFilePath,
            string deviceId,
            string siteId,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Health check for the service
        /// </summary>
        Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get upload progress (0.0 to 1.0)
        /// </summary>
        float GetProgress(string uploadId);
    }
    
    public class UploadResult
    {
        public bool Success { get; set; }
        public string RemoteUrl { get; set; }
        public string LocalPath { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
        public TimeSpan Duration { get; set; }
        public int RetryCount { get; set; }
        public string UploadId { get; set; }
        public long BytesUploaded { get; set; }
        
        public static UploadResult SuccessResult(string url, string uploadId, TimeSpan duration)
        {
            return new UploadResult
            {
                Success = true,
                RemoteUrl = url,
                UploadId = uploadId,
                Duration = duration
            };
        }
        
        public static UploadResult FailureResult(string error, Exception ex = null, int retries = 0)
        {
            return new UploadResult
            {
                Success = false,
                ErrorMessage = error,
                Exception = ex,
                RetryCount = retries
            };
        }
    }
}
