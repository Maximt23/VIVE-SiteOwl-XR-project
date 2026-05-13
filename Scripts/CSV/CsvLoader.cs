using System.Collections.Generic;
using UnityEngine;

namespace SiteOwlXR.CSV
{
    /// <summary>
    /// Loads and parses SiteOwl device CSV files.
    /// TODO: Implement CSV parsing logic
    /// </summary>
    public class CsvLoader : MonoBehaviour
    {
        [Header("Settings")]
        public string csvFileName = "devices.csv";
        
        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnCsvLoaded;
        public UnityEngine.Events.UnityEvent OnCsvError;
        
        private string csvPath;
        
        void Start()
        {
            csvPath = System.IO.Path.Combine(Application.persistentDataPath, "Data", "Input", csvFileName);
        }
        
        /// <summary>
        /// Loads the CSV file and parses device records.
        /// </summary>
        public void LoadCsv()
        {
            // TODO: Implement CSV loading
            Debug.Log("[CsvLoader] Loading CSV from: " + csvPath);
            
            // Check if file exists
            if (!System.IO.File.Exists(csvPath))
            {
                Debug.LogError("[CsvLoader] CSV file not found: " + csvPath);
                OnCsvError?.Invoke();
                return;
            }
            
            // TODO: Parse CSV rows into DeviceData objects
            // TODO: Fire OnCsvLoaded event with parsed data
        }
        
        /// <summary>
        /// Gets list of devices that need coordinate capture.
        /// </summary>
        public List<object> GetMissingDevices()
        {
            // TODO: Return devices with empty X/Y coordinates
            return new List<object>();
        }
    }
}
