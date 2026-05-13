# Working Scene Setup - Wire It All Together

This guide shows you how to connect all the pieces into a working Unity scene.

## Step 1: Create the Scene

### 1.1 New Scene
```
File → New Scene
Save as: Assets/Scenes/CctvSurvey.unity
```

### 1.2 Delete Default Camera
```
Select: Main Camera
Delete it (we'll use XR camera)
```

## Step 2: Add XR Rig (The Foundation)

```
GameObject → XR → XR Origin (Action-based)

This creates:
- XR Origin (parent)
  - Camera Offset
    - Main Camera
  - LeftHand Controller
  - RightHand Controller
```

**Rename to:** `XR Cctv Rig`

## Step 3: Add Core Components

Attach these scripts to **XR Cctv Rig** GameObject:

### Component Stack (in order):

```csharp
// 1. XR INITIALIZATION
Component: XrInitializer.cs
Purpose: Waits for XR to be ready
Settings:
  - timeout: 10
  - autoStartXr: true
  - loadingPanel: [assign in Step 5]

// 2. GPS MANAGER  
Component: RealGpsManager.cs
Purpose: Captures real GPS coordinates
Settings:
  - desiredAccuracy: 5
  - updateDistance: 1
  - timeout: 20

// 3. CALIBRATION MANAGER
Component: CalibrationManager.cs
Purpose: SiteOwl X/Y coordinate transformation

// 4. CSV MANAGER (SiteOwl)
Component: SiteOwlCsvManager.cs
Purpose: Reads/writes CSV, preserves collaborator data
Settings:
  - CsvFileName: "cameras.csv"
  - BackupFolder: "Backups"
  - GpsBarcodeColumn: "Barcode"

// 5. PHOTO CAPTURE
Component: PhotoCapture.cs
Purpose: Takes photos with headset camera
Settings:
  - PhotosFolder: "CapturedPhotos"
  - PhotoWidth: 1920
  - PhotoHeight: 1080

// 6. EXTERNAL UPLOADER
Component: ExternalPhotoUploader.cs
Purpose: Uploads to SharePoint/Azure/FileServer
Settings:
  - provider: LocalNetwork (or your choice)
  - [Configure provider settings]

// 7. AI RECOGNIZER
Component: CctvCameraRecognizer.cs
Purpose: Identifies camera types from photos
Settings:
  - confidenceThreshold: 0.6
  - maxSuggestions: 3
  - useCctvDatabase: false (or true if connected)

// 8. THE ORCHESTRATOR
Component: CctvCaptureController.cs
Purpose: Wires everything together!
Settings:
  - All components: [auto-finds if on same object]
  - cameraTransform: Main Camera

// 9. EXTERNAL DATA CONNECTOR (optional)
Component: ExternalDataConnector.cs
Purpose: Connects to other machine with device data
Settings:
  - baseUrl: "http://your-machine:8080"
  - useExternalDatabase: true/false
```

## Step 4: Add UI Canvas

```
Right-click → UI → Canvas

Canvas Settings:
- Render Mode: World Space
- Position: (0, 1.5, 2)
- Rotation: (0, 0, 0)
- Scale: (0.005, 0.005, 0.005)

This puts UI 2m in front of user, world-sized
```

### UI Structure:

```
Canvas (World Space)
├── LoadingPanel
│   └── LoadingText (TextMeshPro)
├── MainPanel
│   ├── TitleText: "CCTV Survey"
│   ├── StatusText: "Ready"
│   ├── GpsText: "GPS: Searching..."
│   ├── CameraListPanel
│   │   └── [Scroll View with camera items]
│   ├── CapturePanel
│   │   ├── SelectedCameraText
│   │   ├── CaptureButton
│   │   └── ProgressText
│   └── AiSuggestionsPanel (AiCopilotUI)
│       ├── AnalyzingText
│       ├── SuggestionContainer
│       └── [Suggestion items]
└── DebugPanel (optional)
    ├── PositionText
    └── PointerLine (LineRenderer)
```

## Step 5: Wire the UI to Scripts

### Loading Panel
```csharp
XrInitializer.loadingPanel → assign LoadingPanel GameObject
XrInitializer.loadingText → assign LoadingText
```

### Main Status
```csharp
// Create or assign these TextMeshProUGUI objects:

StatusText → shows capture progress
GpsText → shows GPS coordinates
SelectedCameraText → shows current camera
ProgressText → shows "Capturing GPS..." etc.
```

### Camera List
```csharp
// Create CameraListItem.prefab:
Prefab structure:
- Background (Image)
  - DeviceNameText (TextMeshProUGUI)
  - DeviceTypeText (TextMeshProUGUI)
  - IpAddressText (TextMeshProUGUI)
  - StatusIcon (Image: green/yellow/red)
  - SelectButton (Button)

// In scene:
CameraListPanel → Vertical Layout Group
  → Content (Scroll View content)
    → [Instantiate CameraListItem prefabs here]
```

### AI Suggestions
```csharp
AiCopilotUI.suggestionsPanel → assign AiSuggestionsPanel
AiCopilotUI.analyzingPanel → assign AnalyzingPanel  
AiCopilotUI.suggestionContainer → assign SuggestionContainer
AiCopilotUI.statusText → assign StatusText
AiCopilotUI.confidenceBar → assign Slider/Image
AiCopilotUI.recognizer → assign CctvCameraRecognizer
AiCopilotUI.captureController → assign CctvCaptureController
```

### Suggestion Item Prefab
```csharp
// Create AiSuggestionItem.prefab:
Prefab structure:
- Background (Image)
  - RankText: "#1" (TextMeshProUGUI)
  - CameraTypeText: "DOME CAMERA" (TextMeshProUGUI)
  - ConfidenceText: "85% MATCH" (TextMeshProUGUI)
  - ReasoningText: "Round dome, ceiling" (TextMeshProUGUI)
  - AcceptButton (Button)
  - RejectButton (Button)
```

## Step 6: Add Event Listeners

### In CctvCaptureController:
```csharp
void Start()
{
    // Subscribe to events
    OnGpsCaptured += (camera) => {
        GpsText.text = $"GPS: {camera.GpsBarcode}";
    };
    
    OnPhotoCaptured += (camera, url) => {
        StatusText.text = "Photo uploaded!";
    };
    
    OnCaptureComplete += (camera) => {
        StatusText.text = $"{camera.DeviceName} captured!";
        // Refresh camera list
        RefreshCameraList();
    };
    
    OnCaptureError += (error) => {
        StatusText.text = $"Error: {error}";
    };
}
```

### Button Wiring:
```csharp
// Capture Button
CaptureButton.onClick.AddListener(() => {
    var controller = GetComponent<CctvCaptureController>();
    controller.StartCapture(currentCamera);
});

// AI Accept Button (in AiSuggestionItem)
AcceptButton.onClick.AddListener(() => {
    controller.UserConfirmedCameraType(cameraType);
});

// AI Reject Button
RejectButton.onClick.AddListener(() => {
    // Show next suggestion or manual entry
});
```

## Step 7: Create Test Data

### Create CSV File
```
Assets/StreamingAssets/cameras.csv
```

Content:
```csv
Site,Device Name,Device Type,System Type,Description,Barcode,X,Y,IP Address,VMS Zone,Manufacturer,Model,Photo URL,Photo Path,Capture Timestamp,Capture Method,GPS Accuracy,Review Status
2996,CAM-001,Dome Camera,CCTV,Main entrance dome,,,,192.168.1.101,Front_Entrance,Axis,P3245-LV,,,,,,,PENDING
2996,CAM-002,Bullet Camera,CCTV,North parking,,,,192.168.1.102,Parking_North,Hikvision,DS-2CD2T85G1-I5,,,,,,,PENDING
2996,CAM-003,PTZ Camera,CCTV,Loading dock,,,,192.168.1.103,Loading_Dock,Axis,Q6155-E,,,,,,,PENDING
```

## Step 8: Build Settings

### Add Scene to Build:
```
File → Build Settings
Click "Add Open Scenes"
Select Android platform
Switch Platform
```

### Player Settings:
```
Edit → Project Settings → Player

Company Name: YourCompany
Product Name: CCTV Survey XR

Other Settings:
- Minimum API Level: 29 (Android 10.0)
- Target API Level: 33
- Scripting Backend: IL2CPP
- Target Architectures: ARM64

XR Plug-in Management:
- Check: OpenXR
- Add: HTC Vive Controller Profile
```

## Step 9: Test in Editor (Before Building)

### Mock GPS for Testing:
```csharp
// In RealGpsManager, add mock mode:
public bool useMockGps = true;  // Check this in Inspector

// When enabled, returns fake GPS:
Latitude = 32.812345;
Longitude = -96.812345;
```

### Test Sequence:
```
1. Hit Play in Unity Editor
2. Check LoadingPanel shows "Initializing XR..."
3. Check StatusText shows "Ready"
4. Check CameraListPanel shows CAM-001, CAM-002, CAM-003
5. Click on CAM-001
6. Click Capture Button
7. Check ProgressText shows each step
8. Verify CSV gets updated with GPS in Barcode field
```

## Step 10: Build and Deploy

### Build APK:
```
File → Build Settings → Build And Run

Output: CCTV_Survey_XR.apk
```

### Deploy to VIVE XR Elite:
```bash
# Enable Developer Mode on headset first!
# Connect USB

adb devices  # Should show headset

# Unity's "Build And Run" will install automatically
# Or manually:
adb install -r CCTV_Survey_XR.apk
```

## Debugging Connection Issues

### If components don't auto-find:
```csharp
// In CctvCaptureController, manually assign:
public override void FindComponents()
{
    gpsManager = GameObject.Find("XR Cctv Rig").GetComponent<RealGpsManager>();
    csvManager = GameObject.Find("XR Cctv Rig").GetComponent<SiteOwlCsvManager>();
    // etc...
}
```

### If events don't fire:
```csharp
// Add debug logging:
void OnCaptureStarted(CameraData cam)
{
    Debug.Log($"[TEST] Capture started: {cam.DeviceName}");
}
```

### Check component order:
```csharp
// In Script Execution Order:
// 1. XrInitializer (first)
// 2. RealGpsManager (after XR)
// 3. CctvCaptureController (last)
```

## Working Data Flow (Verified)

```
1. User clicks CAM-001 in list
   ↓ CurrentCamera = CAM-001

2. User clicks Capture Button
   ↓ CctvCaptureController.StartCapture(CAM-001)

3. GPS Captured
   ↓ RealGpsManager captures lat/lon
   ↓ CAM-001.GpsBarcode = "32.812345,-96.812345"
   ↓ OnGpsCaptured event fires
   ↓ UI updates: "GPS: 32.812345,-96.812345"

4. Photo Captured
   ↓ PhotoCapture.TakePhoto() saves locally
   ↓ CAM-001.PhotoPath = "/path/to/photo.jpg"

5. Photo Uploaded
   ↓ ExternalPhotoUploader uploads
   ↓ CAM-001.PhotoUrl = "https://sharepoint.com/photo.jpg"
   ↓ OnPhotoCaptured event fires

6. AI Analysis (optional)
   ↓ CctvCameraRecognizer analyzes photo
   ↓ Shows suggestions in AiCopilotUI
   ↓ User clicks Accept or Skip

7. Data Saved
   ↓ SiteOwlCsvManager writes to CSV
   ↓ Barcode field = "32.812345,-96.812345"
   ↓ Photo URL added
   ↓ OnCaptureComplete fires
   ↓ Camera list shows CAM-001 as captured
```

## Next Steps

Now you have:
- ✅ All components connected
- ✅ Events wired together  
- ✅ Working data flow
- ✅ Real GPS → Barcode field
- ✅ Photos → External storage
- ✅ AI suggestions → User confirmation

**Ready for real testing on VIVE XR Elite!**
