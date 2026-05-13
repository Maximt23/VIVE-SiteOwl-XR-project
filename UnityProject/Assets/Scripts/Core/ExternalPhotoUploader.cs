using UnityEngine;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SiteOwlXR.Core
{
    /// <summary>
    /// Uploads photos to external storage: SharePoint, Azure Blob, File Server, or Local
    /// </summary>
    public class ExternalPhotoUploader : MonoBehaviour
    {
        [Header("Storage Provider")]
        public StorageProviderType provider = StorageProviderType.LocalNetwork;
        
        [Header("SharePoint Settings")]
        public string sharePointSiteUrl = "https://yourcompany.sharepoint.com/sites/cctv";
        public string sharePointFolder = "Shared Documents/Survey Photos";
        public string sharePointClientId = "";
        public string sharePointClientSecret = "";
        
        [Header("Azure Blob Settings")]
        public string azureConnectionString = "DefaultEndpointsProtocol=https;...";
        public string azureContainerName = "cctv-photos";
        
        [Header("File Server Settings")]
        public string fileServerPath = "\\\\fileserver\\cctv-photos";
        public string fileServerUsername = "";
        public string fileServerPassword = "";
        
        [Header("Local Settings")]
        public string localStoragePath = "";  // If empty, uses Application.persistentDataPath
        
        [Header("Upload Settings")]
        public bool createDateFolders = true;  // Organize by date
        public string fileNameFormat = "{deviceId}_{timestamp:yyyyMMdd_HHmmss}.jpg";
        
        private HttpClient httpClient;
        
        void Start()
        {
            httpClient = new HttpClient();
            Debug.Log($"[ExternalPhotoUploader] Provider: {provider}");
        }
        
        /// <summary>
        /// Uploads photo to configured external storage
        /// </summary>
        public async void UploadPhotoAsync(string localPhotoPath, string deviceId, Action<string, string> onComplete)
        {
            try
            {
                string url = null;
                string error = null;
                
                switch (provider)
                {
                    case StorageProviderType.SharePoint:
                        (url, error) = await UploadToSharePoint(localPhotoPath, deviceId);
                        break;
                        
                    case StorageProviderType.AzureBlob:
                        (url, error) = await UploadToAzureBlob(localPhotoPath, deviceId);
                        break;
                        
                    case StorageProviderType.FileServer:
                        (url, error) = UploadToFileServer(localPhotoPath, deviceId);
                        break;
                        
                    case StorageProviderType.LocalNetwork:
                    default:
                        (url, error) = StoreLocally(localPhotoPath, deviceId);
                        break;
                }
                
                onComplete?.Invoke(url, error);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExternalPhotoUploader] Upload failed: {ex.Message}");
                onComplete?.Invoke(null, ex.Message);
            }
        }
        
        /// <summary>
        /// Upload to SharePoint Online
        /// Requires: SharePoint app registration with Client ID/Secret
        /// </summary>
        private async Task<(string url, string error)> UploadToSharePoint(string localPath, string deviceId)
        {
            #if UNITY_EDITOR
            Debug.Log($"[ExternalPhotoUploader] SharePoint upload: {deviceId}");
            #endif
            
            try
            {
                // Read file
                byte[] fileBytes = File.ReadAllBytes(localPath);
                string fileName = GetFileName(deviceId);
                
                // Step 1: Get access token
                string accessToken = await GetSharePointAccessToken();
                if (string.IsNullOrEmpty(accessToken))
                {
                    return (null, "Failed to get SharePoint access token");
                }
                
                // Step 2: Upload to SharePoint
                string uploadUrl = $"{sharePointSiteUrl}/_api/web/GetFolderByServerRelativeUrl('{sharePointFolder}')/Files/add(url='{fileName}',overwrite=true)";
                
                var content = new ByteArrayContent(fileBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                httpClient.DefaultRequestHeaders.Add("X-RequestDigest", await GetSharePointFormDigest(accessToken));
                
                var response = await httpClient.PostAsync(uploadUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    // Get the direct URL
                    string fileUrl = $"{sharePointSiteUrl}/{sharePointFolder}/{fileName}";
                    Debug.Log($"[ExternalPhotoUploader] SharePoint success: {fileUrl}");
                    return (fileUrl, null);
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    return (null, $"SharePoint error: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                return (null, $"SharePoint exception: {ex.Message}");
            }
        }
        
        private async Task<string> GetSharePointAccessToken()
        {
            // OAuth 2.0 Client Credentials Flow
            try
            {
                var tokenEndpoint = "https://accounts.accesscontrol.windows.net/{tenant-id}/tokens/OAuth/2";
                
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"grant_type", "client_credentials"},
                    {"client_id", sharePointClientId},
                    {"client_secret", sharePointClientSecret},
                    {"resource", "00000003-0000-0ff1-ce00-000000000000/yourcompany.sharepoint.com@{tenant-id}"}
                });
                
                var response = await httpClient.PostAsync(tokenEndpoint, content);
                var json = await response.Content.ReadAsStringAsync();
                
                // Parse access_token from JSON
                // (simplified - use JSON parser in production)
                if (json.Contains("access_token"))
                {
                    int start = json.IndexOf("access_token\":\"") + 15;
                    int end = json.IndexOf("\"", start);
                    return json.Substring(start, end - start);
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        private async Task<string> GetSharePointFormDigest(string accessToken)
        {
            // Get form digest value for POST requests
            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await httpClient.PostAsync($"{sharePointSiteUrl}/_api/contextinfo", null);
                var json = await response.Content.ReadAsStringAsync();
                
                // Parse FormDigestValue
                if (json.Contains("FormDigestValue"))
                {
                    int start = json.IndexOf("FormDigestValue\":\"") + 18;
                    int end = json.IndexOf("\"", start);
                    return json.Substring(start, end - start);
                }
                
                return "";
            }
            catch
            {
                return "";
            }
        }
        
        /// <summary>
        /// Upload to Azure Blob Storage
        /// </summary>
        private async Task<(string url, string error)> UploadToAzureBlob(string localPath, string deviceId)
        {
            #if UNITY_EDITOR
            Debug.Log($"[ExternalPhotoUploader] Azure upload: {deviceId}");
            #endif
            
            try
            {
                // Note: In production, use Azure.Storage.Blobs SDK
                // For now, using REST API
                
                byte[] fileBytes = File.ReadAllBytes(localPath);
                string fileName = GetFileName(deviceId);
                string blobPath = createDateFolders ? $"{DateTime.Now:yyyy/MM/dd}/{fileName}" : fileName;
                
                // Parse connection string
                var connParts = ParseConnectionString(azureConnectionString);
                string accountName = connParts["AccountName"];
                string accountKey = connParts["AccountKey"];
                
                // Build URL
                string blobUrl = $"https://{accountName}.blob.core.windows.net/{azureContainerName}/{blobPath}";
                
                // Create authorization header (Shared Key)
                string authHeader = CreateAzureSharedKeyHeader(
                    "PUT", fileBytes.Length, "image/jpeg", "", "", 
                    accountName, azureContainerName, blobPath, accountKey
                );
                
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("x-ms-blob-type", "BlockBlob");
                httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
                
                var content = new ByteArrayContent(fileBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                
                var response = await httpClient.PutAsync(blobUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    Debug.Log($"[ExternalPhotoUploader] Azure success: {blobUrl}");
                    return (blobUrl, null);
                }
                else
                {
                    return (null, $"Azure error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                return (null, $"Azure exception: {ex.Message}");
            }
        }
        
        private Dictionary<string, string> ParseConnectionString(string connString)
        {
            var result = new Dictionary<string, string>();
            var parts = connString.Split(';');
            foreach (var part in parts)
            {
                var kv = part.Split('=');
                if (kv.Length == 2)
                    result[kv[0]] = kv[1];
            }
            return result;
        }
        
        private string CreateAzureSharedKeyHeader(string method, int contentLength, string contentType, 
            string md5, string date, string account, string container, string blob, string key)
        {
            // Simplified - use Azure SDK in production
            string stringToSign = $"{method}\n{md5}\n{contentType}\n{date}\n/{account}/{container}/{blob}";
            // HMAC SHA256
            // Return "SharedKey {account}:{signature}"
            return "SharedKey " + account + ":";  // TODO: Implement proper signing
        }
        
        /// <summary>
        /// Upload to Windows file server (UNC path)
        /// </summary>
        private (string url, string error) UploadToFileServer(string localPath, string deviceId)
        {
            try
            {
                string fileName = GetFileName(deviceId);
                string folderPath = createDateFolders ? 
                    Path.Combine(fileServerPath, DateTime.Now.ToString("yyyy\\MM\\dd")) :
                    fileServerPath;
                
                string destPath = Path.Combine(folderPath, fileName);
                
                // Ensure directory exists
                Directory.CreateDirectory(folderPath);
                
                // Copy file
                File.Copy(localPath, destPath, true);
                
                // Return UNC path
                Debug.Log($"[ExternalPhotoUploader] File server success: {destPath}");
                return (destPath, null);
            }
            catch (Exception ex)
            {
                return (null, $"File server error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Store locally (fallback)
        /// </summary>
        private (string url, string error) StoreLocally(string localPath, string deviceId)
        {
            try
            {
                string basePath = string.IsNullOrEmpty(localStoragePath) ? 
                    Application.persistentDataPath : localStoragePath;
                
                string folderPath = createDateFolders ?
                    Path.Combine(basePath, "Photos", DateTime.Now.ToString("yyyy\\MM\\dd")) :
                    Path.Combine(basePath, "Photos");
                
                string fileName = GetFileName(deviceId);
                string destPath = Path.Combine(folderPath, fileName);
                
                Directory.CreateDirectory(folderPath);
                File.Copy(localPath, destPath, true);
                
                // Return as file:// URL for local access
                string url = new Uri(destPath).AbsoluteUri;
                
                Debug.Log($"[ExternalPhotoUploader] Local storage: {url}");
                return (url, null);
            }
            catch (Exception ex)
            {
                return (null, $"Local storage error: {ex.Message}");
            }
        }
        
        private string GetFileName(string deviceId)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return fileNameFormat
                .Replace("{deviceId}", deviceId)
                .Replace("{timestamp:yyyyMMdd_HHmmss}", timestamp);
        }
        
        void OnDestroy()
        {
            httpClient?.Dispose();
        }
    }
    
    public enum StorageProviderType
    {
        LocalNetwork,   // Store locally
        FileServer,     // UNC path
        SharePoint,     // SharePoint Online
        AzureBlob       // Azure Blob Storage
    }
}
