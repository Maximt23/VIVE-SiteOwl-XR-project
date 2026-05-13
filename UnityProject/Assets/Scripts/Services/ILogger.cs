using System;
using UnityEngine;

namespace SiteOwlXR.Services
{
    /// <summary>
    /// Enterprise logging interface
    /// Supports structured logging, log levels, and multiple sinks
    /// </summary>
    public interface ILogger
    {
        void LogDebug(string message, object context = null);
        void LogInfo(string message, object context = null);
        void LogWarning(string message, object context = null);
        void LogError(string message, Exception exception = null, object context = null);
        void LogCritical(string message, Exception exception = null, object context = null);
        
        void LogMetric(string metricName, double value, System.Collections.Generic.Dictionary<string, string> tags = null);
        void LogEvent(string eventName, System.Collections.Generic.Dictionary<string, string> properties = null);
    }
    
    /// <summary>
    /// Unity console implementation with log levels
    /// </summary>
    public class UnityLogger : ILogger
    {
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Debug;
        
        public void LogDebug(string message, object context = null)
        {
            if (MinimumLogLevel <= LogLevel.Debug)
                Debug.Log(FormatMessage("DEBUG", message, context));
        }
        
        public void LogInfo(string message, object context = null)
        {
            if (MinimumLogLevel <= LogLevel.Info)
                Debug.Log(FormatMessage("INFO", message, context));
        }
        
        public void LogWarning(string message, object context = null)
        {
            if (MinimumLogLevel <= LogLevel.Warning)
                Debug.LogWarning(FormatMessage("WARN", message, context));
        }
        
        public void LogError(string message, Exception exception = null, object context = null)
        {
            if (MinimumLogLevel <= LogLevel.Error)
            {
                string fullMessage = FormatMessage("ERROR", message, context);
                if (exception != null)
                    fullMessage += $"\nException: {exception}";
                Debug.LogError(fullMessage);
            }
        }
        
        public void LogCritical(string message, Exception exception = null, object context = null)
        {
            string fullMessage = FormatMessage("CRITICAL", message, context);
            if (exception != null)
                fullMessage += $"\nException: {exception}";
            Debug.LogError(fullMessage);
        }
        
        public void LogMetric(string metricName, double value, System.Collections.Generic.Dictionary<string, string> tags = null)
        {
            string tagStr = tags != null ? string.Join(",", tags) : "";
            Debug.Log($"[METRIC] {metricName}={value} {tagStr}");
        }
        
        public void LogEvent(string eventName, System.Collections.Generic.Dictionary<string, string> properties = null)
        {
            string propStr = properties != null ? string.Join(",", properties) : "";
            Debug.Log($"[EVENT] {eventName} {propStr}");
        }
        
        private string FormatMessage(string level, string message, object context)
        {
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string contextStr = context != null ? $" [{context.GetType().Name}]" : "";
            return $"[{timestamp}] [{level}]{contextStr} {message}";
        }
    }
    
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }
}
