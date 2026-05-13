using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SiteOwlXR.Core
{
    /// <summary>
    /// Automatically pushes CSV changes to GitHub when connected to WiFi.
    /// Ensures data backup and audit trail.
    /// </summary>
    public class GitAutoPush : MonoBehaviour
    {
        [Header("Settings")]
        public bool AutoPushOnCapture = true;
        public string GitPath = "git";
        public int PushTimeoutSeconds = 30;
        
        [Header("Logging")]
        public bool LogToConsole = true;
        
        private CsvManager csvManager;
        private bool isPushing = false;
        
        // Path to the repo (on Android, this might be different)
        private string repoPath;
        
        void Start()
        {
            csvManager = FindObjectOfType<CsvManager>();
            repoPath = Application.persistentDataPath;
            
            if (csvManager != null && AutoPushOnCapture)
            {
                csvManager.OnDataSaved += OnDataSaved;
            }
            
            Log("GitAutoPush initialized. Repository path: " + repoPath);
        }
        
        void OnDataSaved()
        {
            if (!AutoPushOnCapture || isPushing) return;
            
            // Check for WiFi
            if (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
            {
                Log("WiFi detected. Queuing git push...");
                _ = PushAsync();
            }
            else
            {
                Log("No WiFi. Skipping auto-push (will sync when connected).");
            }
        }
        
        /// <summary>
        /// Manually triggers a git push.
        /// </summary>
        public async Task<bool> PushAsync()
        {
            if (isPushing) return false;
            
            isPushing = true;
            bool success = false;
            
            try
            {
                // Stage CSV files
                var stageResult = await RunGitCommand("add *.csv");
                if (!stageResult.success)
                {
                    Log("Failed to stage files: " + stageResult.error);
                }
                
                // Commit
                string commitMessage = $"Data capture: {System.DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
                var commitResult = await RunGitCommand($"commit -m \"{commitMessage}\" --allow-empty");
                
                if (!commitResult.success && !commitResult.output.Contains("nothing to commit"))
                {
                    Log("Commit failed: " + commitResult.error);
                }
                else
                {
                    // Push
                    var pushResult = await RunGitCommand("push origin HEAD");
                    if (pushResult.success)
                    {
                        Log("Successfully pushed to GitHub!");
                        success = true;
                    }
                    else
                    {
                        Log("Push failed: " + pushResult.error);
                    }
                }
            }
            finally
            {
                isPushing = false;
            }
            
            return success;
        }
        
        private async Task<(bool success, string output, string error)> RunGitCommand(string arguments)
        {
            var tcs = new TaskCompletionSource<(bool success, string output, string error)>();
            
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = GitPath,
                        Arguments = arguments,
                        WorkingDirectory = repoPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using (var process = Process.Start(psi))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        
                        bool exited = process.WaitForExit(PushTimeoutSeconds * 1000);
                        
                        if (!exited)
                        {
                            process.Kill();
                            tcs.TrySetResult((false, output, "Timeout"));
                            return;
                        }
                        
                        tcs.TrySetResult((process.ExitCode == 0, output, error));
                    }
                }
                catch (System.Exception ex)
                {
                    tcs.TrySetResult((false, "", ex.Message));
                }
            });
            
            return await tcs.Task;
        }
        
        private void Log(string message)
        {
            if (LogToConsole)
            {
                UnityEngine.Debug.Log($"[GitAutoPush] {message}");
            }
        }
        
        /// <summary>
        /// Call this from a UI button to manually trigger push.
        /// </summary>
        public void ManualPush()
        {
            _ = PushAsync();
        }
        
        void OnDestroy()
        {
            if (csvManager != null)
            {
                csvManager.OnDataSaved -= OnDataSaved;
            }
        }
    }
}
