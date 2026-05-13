namespace SiteOwlXR.Models
{
    /// <summary>
    /// Photo storage provider options
    /// </summary>
    public enum StorageProvider
    {
        LocalNetwork,   // Local storage or UNC path
        SharePoint,     // SharePoint Online
        AzureBlob,      // Azure Blob Storage
        FileServer,     // Windows file server (UNC)
        S3,             // AWS S3 (future)
        GoogleCloud     // GCS (future)
    }
}
