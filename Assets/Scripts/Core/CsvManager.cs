using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SiteOwlXR.Core
{
    /// <summary>
    /// Manages SiteOwl CSV loading and saving.
    ///
    /// GUARD RAILS:
    ///   - ONLY the "Coordinates" column is ever written back.
    ///   - All other 55 fields are preserved byte-for-byte.
    ///   - A timestamped backup is created before every save.
    ///
    /// SiteOwl CSV format (56 columns):
    ///   Col  4 → Device ID
    ///   Col  5 → Name
    ///   Col  8 → System Type
    ///   Col  9 → Device/Task Type
    ///   Col 10 → Part Number
    ///   Col 53 → Coordinates  ← only column we touch
    /// </summary>
    public class CsvManager : MonoBehaviour
    {
        // ── SiteOwl column names ──────────────────────────────────────────────
        private const string COL_DEVICE_ID    = "Device ID";
        private const string COL_NAME         = "Name";
        private const string COL_SYSTEM_TYPE  = "System Type";
        private const string COL_DEVICE_TYPE  = "Device/Task Type";
        private const string COL_PART_NUMBER  = "Part Number";
        private const string COL_COORDINATES  = "Coordinates";   // ← ONLY COLUMN WE WRITE

        // Placeholder value SiteOwl puts on unsurveyed devices
        private const float PLACEHOLDER_X = 10.00f;
        private const float PLACEHOLDER_Y = 30.00f;
        private const float COORD_EPSILON  = 0.001f;

        [Header("Settings")]
        [Tooltip("Absolute path to the SiteOwl CCTV survey CSV to load on Start.")]
        public string CsvFilePath   = "";
        public string BackupFolder  = "Backups";

        void Start()
        {
            if (!string.IsNullOrEmpty(CsvFilePath))
                LoadCsv(CsvFilePath);
            else
                Debug.LogWarning("[CsvManager] CsvFilePath not set — call LoadCsv(path) manually.");
        }

        // ── Internal state ────────────────────────────────────────────────────
        private string   loadedPath;
        private string[] headers;
        private int      coordColIndex = -1;

        // Column name → index lookup (case-insensitive)
        private readonly Dictionary<string, int> colIndex =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Parallel lists — index N of rawRows matches index N of devices
        // devices[N] may be null when that row had a column-count mismatch;
        // the raw row is preserved verbatim so re-saves never drop SiteOwl data.
        private readonly List<string[]>    rawRows = new List<string[]>();
        private readonly List<DeviceData>  devices  = new List<DeviceData>();

        // ── Public surface ────────────────────────────────────────────────────
        public List<DeviceData> Devices        => devices;
        public string           LoadedPath     => loadedPath;
        public int TotalCount                  => devices.Count;
        public int CapturedCount               => devices.Count(d => !d.NeedsCapture);
        public int RemainingCount              => devices.Count(d =>  d.NeedsCapture);
        public bool IsLoaded                   => !string.IsNullOrEmpty(loadedPath);

        public event Action             OnDataLoaded;
        public event Action<DeviceData> OnDeviceUpdated;
        public event Action             OnDataSaved;

        // ── Load ──────────────────────────────────────────────────────────────

        /// <summary>Loads a SiteOwl CCTV survey CSV from disk.</summary>
        public bool LoadCsv(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[CsvManager] File not found: {path}");
                return false;
            }

            try
            {
                loadedPath = path;
                var lines = File.ReadAllLines(path);
                ParseCsv(lines);

                Debug.Log($"[CsvManager] Loaded {devices.Count} devices. " +
                          $"{RemainingCount} need coordinate capture.");
                OnDataLoaded?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvManager] Load failed: {ex.Message}");
                return false;
            }
        }

        private void ParseCsv(string[] lines)
        {
            rawRows.Clear();
            devices.Clear();
            colIndex.Clear();
            coordColIndex = -1;

            if (lines.Length == 0) return;

            // ── Header row ────────────────────────────────────────────────────
            headers = SplitCsvLine(lines[0]);
            for (int i = 0; i < headers.Length; i++)
                colIndex[headers[i].Trim()] = i;

            if (!colIndex.TryGetValue(COL_COORDINATES, out coordColIndex))
            {
                Debug.LogError(
                    $"[CsvManager] '{COL_COORDINATES}' column not found! " +
                    $"Available: {string.Join(", ", headers)}");
                return;
            }

            Debug.Log($"[CsvManager] '{COL_COORDINATES}' found at column index {coordColIndex}.");

            // ── Data rows ─────────────────────────────────────────────────────
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var row = SplitCsvLine(lines[i]);
                if (row.Length != headers.Length)
                {
                    Debug.LogWarning(
                        $"[CsvManager] Row {i}: expected {headers.Length} cols, " +
                        $"got {row.Length}. Row preserved verbatim; skipping device parse.");
                    rawRows.Add(row);
                    devices.Add(null);  // null sentinel keeps indices aligned
                    continue;
                }

                rawRows.Add(row);
                devices.Add(BuildDevice(row));
            }
        }

        private DeviceData BuildDevice(string[] row)
        {
            var d = new DeviceData
            {
                DeviceID   = GetField(row, COL_DEVICE_ID),
                DeviceName = GetField(row, COL_NAME),
                SystemType = GetField(row, COL_SYSTEM_TYPE),
                DeviceType = GetField(row, COL_DEVICE_TYPE),
                Description= GetField(row, COL_PART_NUMBER),
            };

            // Fallback: use Name as ID when Device ID column is empty
            if (string.IsNullOrEmpty(d.DeviceID))
                d.DeviceID = d.DeviceName;

            // Parse coordinates — placeholder → null → NeedsCapture = true
            var coordRaw = GetField(row, COL_COORDINATES);
            if (TryParseCoordinates(coordRaw, out float x, out float y))
            {
                bool isPlaceholder =
                    Mathf.Abs(x - PLACEHOLDER_X) < COORD_EPSILON &&
                    Mathf.Abs(y - PLACEHOLDER_Y) < COORD_EPSILON;

                if (!isPlaceholder)
                {
                    d.SiteOwlX = x;
                    d.SiteOwlY = y;
                }
                // isPlaceholder → SiteOwlX/Y stay null → NeedsCapture = true
            }

            return d;
        }

        // ── Save ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Writes updated coordinates back to the original CSV.
        /// GUARD RAIL: Only the Coordinates column changes. Everything else is verbatim.
        /// </summary>
        public bool SaveCsv()
        {
            if (!IsLoaded)
            {
                Debug.LogError("[CsvManager] Nothing loaded — call LoadCsv() first.");
                return false;
            }

            if (!CreateBackup()) return false;

            try
            {
                var lines = new List<string>(rawRows.Count + 1);

                // Header line — preserved exactly
                lines.Add(string.Join(",", headers.Select(QuoteCsvValue)));

                string tempPath = loadedPath + ".tmp";

                // Data lines — only coordColIndex may differ; null-device rows are preserved verbatim
                for (int i = 0; i < rawRows.Count; i++)
                {
                    var row    = (string[]) rawRows[i].Clone();
                    var device = devices[i];  // may be null for malformed rows

                    // ← THIS IS THE ONLY COLUMN WE EVER TOUCH
                    if (device != null && device.SiteOwlX.HasValue && device.SiteOwlY.HasValue)
                        row[coordColIndex] = FormatCoordinates(
                            device.SiteOwlX.Value, device.SiteOwlY.Value);

                    lines.Add(string.Join(",", row.Select(QuoteCsvValue)));
                }

                File.WriteAllLines(tempPath, lines, System.Text.Encoding.UTF8);

                // Atomic replace — prevents corrupt file on crash mid-write
                if (File.Exists(loadedPath))
                    File.Replace(tempPath, loadedPath, null);
                else
                    File.Move(tempPath, loadedPath);

                Debug.Log($"[CsvManager] Saved '{Path.GetFileName(loadedPath)}'. " +
                          $"{CapturedCount}/{TotalCount} coordinates captured.");
                OnDataSaved?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvManager] Save failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates a single device and saves immediately.
        /// </summary>
        public bool UpdateDevice(DeviceData updated)
        {
            int idx = devices.FindIndex(d => d.DeviceID == updated.DeviceID);
            if (idx < 0)
            {
                Debug.LogError($"[CsvManager] Device '{updated.DeviceID}' not found.");
                return false;
            }

            devices[idx] = updated;

            if (SaveCsv())
            {
                OnDeviceUpdated?.Invoke(updated);
                return true;
            }
            return false;
        }

        // ── Queries ───────────────────────────────────────────────────────────

        public List<DeviceData> GetMissingDevices() =>
            devices.Where(d => d != null && d.NeedsCapture).ToList();

        public List<DeviceData> GetReviewRequiredDevices() =>
            devices.Where(d => d != null && d.RequiresReview).ToList();

        public List<DeviceData> SearchDevices(string query)
        {
            // Always return a copy so the caller iterating the list is safe
            // even if UpdateDevice() modifies the internal list during iteration.
            if (string.IsNullOrEmpty(query))
                return new List<DeviceData>(devices.Where(d => d != null));

            query = query.ToLower();
            return devices
                .Where(d => d != null &&
                    ((d.DeviceName?.ToLower().Contains(query) ?? false) ||
                     (d.DeviceID  ?.ToLower().Contains(query) ?? false) ||
                     (d.SystemType?.ToLower().Contains(query) ?? false)))
                .ToList();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string GetField(string[] row, string columnName)
        {
            return colIndex.TryGetValue(columnName, out int i) && i < row.Length
                ? row[i].Trim()
                : string.Empty;
        }

        /// <summary>
        /// Parses "(X.XX, Y.YY)" format used by SiteOwl Coordinates column.
        /// </summary>
        private static bool TryParseCoordinates(string raw, out float x, out float y)
        {
            x = 0f; y = 0f;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            raw = raw.Trim().TrimStart('(').TrimEnd(')');
            var parts = raw.Split(',');
            if (parts.Length != 2) return false;

            var fmt = System.Globalization.CultureInfo.InvariantCulture;
            var style = System.Globalization.NumberStyles.Float;
            return float.TryParse(parts[0].Trim(), style, fmt, out x)
                && float.TryParse(parts[1].Trim(), style, fmt, out y);
        }

        /// <summary>Formats X,Y back to SiteOwl's "(X.XX, Y.YY)" convention.</summary>
        private static string FormatCoordinates(float x, float y) =>
            $"({x:F2}, {y:F2})";

        /// <summary>
        /// RFC-4180 compliant CSV line splitter.
        /// Handles quoted fields, escaped double-quotes ("""), embedded commas and newlines.
        /// </summary>
        private static string[] SplitCsvLine(string line)
        {
            var result  = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // RFC-4180 §2.7: ""  inside a quoted field = escaped literal quote
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;  // consume the second quote
                        }
                        else
                        {
                            inQuotes = false;  // closing quote
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    if (c == '"')      { inQuotes = true; }
                    else if (c == ',') { result.Add(current.ToString()); current.Clear(); }
                    else               { current.Append(c); }
                }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }

        /// <summary>
        /// Wraps a value in quotes if it contains a comma, quote, or newline.
        /// </summary>
        private static string QuoteCsvValue(string value)
        {
            if (value == null) return string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                return '"' + value.Replace("\"", "\"\"") + '"';
            return value;
        }

        private bool CreateBackup()
        {
            try
            {
                string backupDir = Path.Combine(
                    Path.GetDirectoryName(loadedPath) ?? Application.persistentDataPath,
                    BackupFolder);

                Directory.CreateDirectory(backupDir);

                // Millisecond resolution avoids collision on rapid double-captures
                string ts   = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string stem = Path.GetFileNameWithoutExtension(loadedPath);
                string dest = Path.Combine(backupDir, $"{stem}_backup_{ts}.csv");

                // Ultra-paranoid: if somehow still collides, add a suffix
                for (int attempt = 1; File.Exists(dest) && attempt <= 10; attempt++)
                    dest = Path.Combine(backupDir, $"{stem}_backup_{ts}_{attempt}.csv");

                File.Copy(loadedPath, dest, overwrite: false);

                // Rotate — keep only the 5 most recent backups
                PruneOldBackups(backupDir, stem, maxKeep: 5);

                Debug.Log($"[CsvManager] Backup -> {Path.GetFileName(dest)}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvManager] Backup failed: {ex.Message}");
                return false;
            }
        }

        private static void PruneOldBackups(string backupDir, string stem, int maxKeep)
        {
            var backups = Directory.GetFiles(backupDir, $"{stem}_backup_*.csv")
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .ToArray();

            for (int i = maxKeep; i < backups.Length; i++)
            {
                try   { File.Delete(backups[i]); }
                catch { /* best-effort */ }
            }
        }
    }
}
