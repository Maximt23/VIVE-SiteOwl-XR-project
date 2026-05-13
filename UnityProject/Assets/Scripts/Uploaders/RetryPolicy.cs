using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SiteOwlXR.Uploaders
{
    /// <summary>
    /// Enterprise retry policy with exponential backoff
    /// </summary>
    public class RetryPolicy
    {
        private readonly int _maxRetries;
        private readonly TimeSpan _initialDelay;
        private readonly TimeSpan _maxDelay;
        private readonly float _backoffMultiplier;
        
        public RetryPolicy(
            int maxRetries = 3,
            float initialDelaySeconds = 1f,
            float maxDelaySeconds = 30f,
            float backoffMultiplier = 2f)
        {
            _maxRetries = maxRetries;
            _initialDelay = TimeSpan.FromSeconds(initialDelaySeconds);
            _maxDelay = TimeSpan.FromSeconds(maxDelaySeconds);
            _backoffMultiplier = backoffMultiplier;
        }
        
        /// <summary>
        /// Execute action with retry logic
        /// </summary>
        public async Task<TResult> ExecuteAsync<TResult>(
            Func<Task<TResult>> action,
            Func<Exception, bool> shouldRetry = null,
            CancellationToken cancellationToken = default)
        {
            int attempt = 0;
            TimeSpan delay = _initialDelay;
            Exception lastException = null;
            
            while (attempt <= _maxRetries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    return await action();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (attempt >= _maxRetries)
                    {
                        throw new RetryExhaustedException(
                            $"Operation failed after {_maxRetries} retries", ex);
                    }
                    
                    if (shouldRetry != null && !shouldRetry(ex))
                    {
                        throw;
                    }
                    
                    attempt++;
                    Debug.LogWarning($"[RetryPolicy] Attempt {attempt} failed: {ex.Message}. Retrying in {delay.TotalSeconds:F1}s...");
                    
                    await Task.Delay(delay, cancellationToken);
                    
                    // Exponential backoff
                    delay = TimeSpan.FromSeconds(
                        Math.Min(delay.TotalSeconds * _backoffMultiplier, _maxDelay.TotalSeconds));
                }
            }
            
            throw new RetryExhaustedException("Unexpected retry loop exit", lastException);
        }
        
        /// <summary>
        /// Execute action with retry and result wrapper
        /// </summary>
        public async Task<(bool success, TResult result, int retries)> ExecuteWithResultAsync<TResult>(
            Func<Task<TResult>> action,
            Func<Exception, bool> shouldRetry = null,
            CancellationToken cancellationToken = default)
        {
            int attempt = 0;
            
            try
            {
                var result = await ExecuteAsync(action, shouldRetry, cancellationToken);
                return (true, result, attempt);
            }
            catch (RetryExhaustedException)
            {
                return (false, default(TResult), _maxRetries);
            }
        }
    }
    
    public class RetryExhaustedException : Exception
    {
        public RetryExhaustedException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
