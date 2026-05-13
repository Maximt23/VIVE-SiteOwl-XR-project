# SiteOwl Integration Guide

**Working WITH existing SiteOwl workflows, not replacing them.**

## Key Integration Points

### 1. GPS Data → "Barcode" Field

SiteOwl uses the **Barcode** field to store GPS coordinates (lat,long format):

```csv
Site,Device Name,Device Type,System Type,Barcode,X,Y,...
2996,CAM-001,Dome Camera,CCTV,"32.812345,-96.812345",,,...
2996,CAM-002,Bullet Camera,CCTV,"32.812367,-96.812298",,,...
```

**Why Barcode?**
- SiteOwl repurposes this field for GPS in their mobile app
- Standard SiteOwl integration pattern
- Keeps GPS with device record

### 2. Photos Stored EXTERNALLY

**DO NOT embed images in CSV or repo.**

Instead:
```csv
Site,Device Name,...,Photo URL,Photo Path,...
2996,CAM-001,...,https://your-storage.com/photos/cam001.jpg,/storage/photos/cam001.jpg,...
```

**Storage Options:**
- SharePoint / OneDrive
- AWS S3 / Azure Blob
- Company file server
- Local NAS with web access

### 3. We ONLY Write To Specific Columns

**We WRITE (our data):**
- `Barcode` - GPS coordinates (lat,long)
- `X` - SiteOwl X (if available)
- `Y` - SiteOwl Y (if available)
- `Photo URL` - External photo link
- `Photo Path` - Local/network path
- `Review Status` - OK or REVIEW_REQUIRED
- `Capture Timestamp` - ISO 8601
- `Capture Method` - XR_CAPTURE

**We PRESERVE (collaborator data):**
- `Site` - Existing site ID
- `Device Name` - Existing device name
- `Device Type` - Existing type (we may suggest via AI)
- `System Type` - Existing system
- `Description` - Existing description
- `IP Address` - Existing network info
- `VMS Zone` - Existing zone
- `Manufacturer` - Existing make
- `Model` - Existing model
- All other columns - Untouched

## Updated CSV Format

### Input (From SiteOwl/Collaborators)
```csv
Site,Device Name,Device Type,System Type,Description,Barcode,X,Y,IP Address,VMS Zone,Manufacturer,Model,Review Status
2996,CAM-001,Dome Camera,CCTV,Main entrance,,,,192.168.1.101,Front_Entrance,Axis,P3245-LV,PENDING
2996,CAM-002,Bullet Camera,CCTV,North parking,,,,192.168.1.102,Parking_North,Hikvision,DS-2CD2T85G1-I5,PENDING
2996,CAM-003,PTZ Camera,CCTV,Loading dock,,,,192.168.1.103,Loading_Dock,Axis,Q6155-E,PENDING
```

### Output (After XR Capture)
```csv
Site,Device Name,Device Type,System Type,Description,Barcode,X,Y,IP Address,VMS Zone,Manufacturer,Model,Photo URL,Photo Path,Capture Timestamp,Capture Method,Review Status
2996,CAM-001,Dome Camera,CCTV,Main entrance,"32.812345,-96.812345",12.5,88.2,192.168.1.101,Front_Entrance,Axis,P3245-LV,https://storage.com/cam001_20250115.jpg,/storage/cam001_20250115.jpg,2025-01-15T14:30:22Z,XR_CAPTURE,OK
2996,CAM-002,Bullet Camera,CCTV,North parking,"32.812367,-96.812298",45.0,120.5,192.168.1.102,Parking_North,Hikvision,DS-2CD2T85G1-I5,https://storage.com/cam002_20250115.jpg,/storage/cam002_20250115.jpg,2025-01-15T14:31:45Z,XR_CAPTURE,OK
2996,CAM-003,PTZ Camera,CCTV,Loading dock,"32.812298,-96.812401",78.25,45.0,192.168.1.103,Loading_Dock,Axis,Q6155-E,https://storage.com/cam003_20250115.jpg,/storage/cam003_20250115.jpg,2025-01-15T14:33:12Z,XR_CAPTURE,OK
```

## Unity Code Updates

### 1. GPS → Barcode Field

```csharp
// In CsvManager.cs - Update format methods

// When writing row, format GPS for Barcode field:
string gpsBarcode = $"\"{device.Latitude},{device.Longitude}\"";

// Map to Barcode column (find Barcode header index)
if (header == "BARCODE" || header == "BAR CODE")
    value = gpsBarcode;
```

### 2. External Photo Storage

```csharp
// In PhotoCapture.cs - Upload to external storage

public async Task<string> CaptureAndUpload(string deviceId, StorageProvider provider)
{
    // 1. Capture photo locally
    string localPath = CapturePhoto(deviceId);
    
    // 2. Upload to external storage
    string externalUrl = await UploadToStorage(localPath, provider);
    // Options: SharePoint, OneDrive, S3, Azure, FTP, etc.
    
    // 3. Return both paths
    return externalUrl; // Store in CSV
}

// Storage providers:
public enum StorageProvider
{
    SharePoint,
    OneDrive,
    AwsS3,
    AzureBlob,
    FtpServer,
    LocalNetwork
}
```

### 3. Read-Only for Collaborator Columns

```csharp
// In CsvManager.cs - NEVER overwrite these:

string[] PROTECTED_COLUMNS = {
    "Site", "Site ID",
    "Device Name", "Name",
    "Device Type", "Type",
    "System Type", "System",
    "Description", "Desc",
    "IP Address", "IP",
    "VMS Zone", "Zone",
    "Manufacturer", "Make",
    "Model"
};

// When writing:
if (PROTECTED_COLUMNS.Contains(header))
    value = existingValue; // Preserve original
```

## External Storage Setup

### Option 1: SharePoint / OneDrive (Enterprise)

```csharp
// Power Automate flow:
// 1. XR app uploads to SharePoint
// 2. Flow updates SiteOwl with URL
// 3. Photos accessible company-wide

// In Unity:
SharePointClient.UploadFile(photoBytes, $"{siteId}/{deviceId}_{timestamp}.jpg");
```

### Option 2: Azure Blob Storage

```csharp
// Azure Blob client:
var blobClient = new BlobClient(connectionString, containerName, blobName);
await blobClient.UploadAsync(photoStream);
return blobClient.Uri.ToString(); // HTTPS URL
```

### Option 3: Local File Server (Air-gapped)

```csharp
// Save to network share:
string networkPath = $"\\\\fileserver\\cctv-photos\\{siteId}\\{deviceId}_{timestamp}.jpg";
File.WriteAllBytes(networkPath, photoBytes);
return networkPath; // UNC path for CSV
```

## Workflow Integration

### With Collaborators (Designers/Engineers)

```
Collaborators:                    XR Surveyor:
Design system                     Survey in field
↓ Create CSV with devices        ↓ Capture GPS + Photos
↓ Add network info (IP, etc.)    ↓ Upload photos externally
↓ Add descriptions               ↓ Write GPS to Barcode field
↓ Send CSV to field              ↓ Return updated CSV
                                  ↓ Designers import to SiteOwl
```

### Power Automate Integration

```
Trigger: New photo uploaded to SharePoint
↓
Action 1: Copy to SiteOwl storage
↓
Action 2: Update device record with photo URL
↓
Action 3: Link GPS from Barcode field to map
```

## Validation

### Check Before Writing

```csharp
// NEVER overwrite non-empty GPS:
if (!string.IsNullOrEmpty(existingBarcode) && existingBarcode != newGps)
{
    ShowWarning("GPS already exists! Overwrite?");
    // Require explicit confirmation
}

// NEVER overwrite external photo URL:
if (!string.IsNullOrEmpty(existingPhotoUrl) && existingPhotoUrl != newPhotoUrl)
{
    ShowWarning("Photo already linked! Overwrite?");
    // Require explicit confirmation
}
```

## Testing Integration

### Test 1: GPS in Barcode

```csharp
// 1. Capture device with GPS
// 2. Check CSV output
// 3. Verify: Barcode field = "32.812345,-96.812345"
```

### Test 2: External Photo

```csharp
// 1. Capture photo
// 2. Verify uploaded to SharePoint/S3
// 3. Check CSV has HTTPS URL
// 4. Open URL - photo displays
```

### Test 3: Collaborator Data Preserved

```csharp
// 1. Load CSV with existing data:
//    - IP Address = 192.168.1.101
//    - Manufacturer = Axis
// 2. Run XR capture
// 3. Verify IP and Manufacturer unchanged
```

## Summary

**We Supplement, Not Replace:**
- ✅ GPS → Barcode field
- ✅ Photos → External storage
- ✅ X/Y coordinates (if SiteOwl local)
- ✅ Capture timestamp/method
- ✅ Review status

**Collaborators Keep Ownership:**
- ✅ Device names
- ✅ Device types (AI suggests, doesn't overwrite)
- ✅ Network info (IP, VMS zone)
- ✅ Manufacturer/model
- ✅ Descriptions
- ✅ All other metadata

**Integration Pattern:**
```
Collaborators design → XR surveyor captures → 
External storage holds photos → SiteOwl combines all data
```
