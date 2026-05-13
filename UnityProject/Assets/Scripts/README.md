# Unity Scripts Documentation

Complete script reference for VIVE SiteOwl XR Capture system.

## Folder Structure

```
Scripts/
├── Core/           # Essential system components
│   ├── CalibrationManager.cs
│   ├── CaptureController.cs
│   ├── CsvManager.cs
│   ├── DeviceData.cs
│   ├── GeoData.cs
│   ├── GitAutoPush.cs
│   ├── PhotoCapture.cs
│   └── RealGpsManager.cs
├── UI/             # User interface components
│   ├── CaptureUI.cs
│   └── DeviceListItemUI.cs
├── AI/             # Artificial intelligence features
│   ├── AiCopilotUI.cs
│   ├── AiSuggestionItemUI.cs
│   ├── DeviceRecognizer.cs
│   └── ExternalDataConnector.cs
├── Editor/         # Unity Editor tools
│   └── BuildScript.cs
├── SimpleXrTester.cs      # Quick XR test
└── XrInitializer.cs     # XR startup handler
```

## Core Scripts

### CalibrationManager.cs
**Purpose:** Transforms headset space to SiteOwl coordinates

**Usage:**
```csharp
// Set anchor at known SiteOwl point
calibration.SetCalibrationAnchor(siteOwlX: 12.5f, siteOwlY: 88.2f);

// Convert any position to SiteOwl coordinates
Vector2 siteOwlPos = calibration.WorldToSiteOwl(worldPosition);
```

### CaptureController.cs
**Purpose:** Main workflow orchestrator

**Key Methods:**
- `SelectDevice(device)` - Choose device to capture
- `CaptureCurrentDevice()` - Trigger full capture workflow
- `GetCapturePoint()` - Raycast to find target point

### CsvManager.cs
**Purpose:** CSV loading, updating, saving

**Guard Rails:**
- Always creates backup before writing
- Never overwrites non-coordinate fields
- Validates data integrity

### RealGpsManager.cs
**Purpose:** Captures REAL satellite GPS from VIVE XR Elite

**Key Features:**
- Real Android Location Services (not estimates)
- WGS 84 coordinate output (EPSG:4326)
- Accuracy reporting (±meters)
- Works outdoors with satellite visibility

### GeoData.cs
**Purpose:** Stores geographic coordinates

**Export Formats:**
- CSV (for SiteOwl)
- KML (for Google Earth)
- GeoJSON (for web maps)

## AI Scripts

### DeviceRecognizer.cs
**Purpose:** AI device identification from photos

**Analysis Methods:**
1. External ML model (TensorFlow, cloud API)
2. Local image analysis (color, shape, keywords)
3. External database matching (other machine)

**GUARD RAIL:** Never auto-writes - user must confirm

### ExternalDataConnector.cs
**Purpose:** Connects to other machine with device data

**Endpoints:**
- `/api/devices` - List all devices
- `/api/devices/search` - Search by name/type
- `/api/match-image` - Visual similarity matching

### AiCopilotUI.cs
**Purpose:** Displays AI suggestions with manual confirmation

**UI Flow:**
1. Show "Analyzing..." panel
2. Display suggestions with confidence scores
3. User clicks ACCEPT or REJECT
4. Only then is data applied

## UI Scripts

### CaptureUI.cs
**Purpose:** Main user interface controller

**Panels:**
- Calibration panel
- Device list panel
- Capture panel with status
- Real-time position display

## Editor Scripts

### BuildScript.cs
**Purpose:** Automated building from Unity menu

**Menu Items:**
- `Build → Build Android APK`
- `Build → Build and Run Android`

## Setup Instructions

### Minimum Required Components

```csharp
// Add to XR Origin GameObject:
1. XrInitializer.cs      // XR startup
2. CalibrationManager.cs  // Coordinate transformation
3. CsvManager.cs          // Data management
4. CaptureController.cs   // Main workflow

// For real GPS:
5. RealGpsManager.cs      // GPS capture

// For AI features:
6. DeviceRecognizer.cs    // AI analysis
7. AiCopilotUI.cs         // AI suggestions UI
```

### Component Dependencies

```
CaptureController depends on:
  ├── CalibrationManager
  ├── CsvManager
  ├── PhotoCapture
  └── RealGpsManager (optional)

AiCopilotUI depends on:
  ├── DeviceRecognizer
  └── CaptureController

DeviceRecognizer optionally uses:
  └── ExternalDataConnector
```

## Configuration

### Core System Settings
```csharp
// CalibrationManager
public float calibrationScale = 1f;      // Scale factor
public float calibrationRotationOffset;    // Rotation offset

// RealGpsManager
public float desiredAccuracy = 5f;         // Target GPS accuracy
public float updateDistance = 1f;          // Min update distance

// CsvManager
public string CsvFileName = "devices.csv"; // Input filename
public string BackupFolder = "Backups";    // Backup location
```

### AI System Settings
```csharp
// DeviceRecognizer
public float confidenceThreshold = 0.7f;   // Min confidence to suggest
public int maxSuggestions = 3;            // Number of suggestions

// ExternalDataConnector
public string baseUrl = "http://192.168.1.100:8080"; // Your machine
public bool useExternalDatabase = true;
```

## Events & Callbacks

### CalibrationManager
```csharp
calibration.OnCalibrationComplete += () => {
    Debug.Log("Calibration done!");
};
```

### RealGpsManager
```csharp
gpsManager.OnGpsInitialized += () => {
    Debug.Log($"GPS: {gpsManager.Latitude}, {gpsManager.Longitude}");
};

gpsManager.OnGpsDataUpdated += () => {
    // GPS position updated
};
```

### DeviceRecognizer
```csharp
recognizer.OnRecognitionComplete += (results) => {
    foreach (var result in results) {
        Debug.Log($"AI suggests: {result.SuggestedName} ({result.Confidence:P0})");
    }
};
```

## Testing

### Quick XR Test
```csharp
// Add SimpleXrTester.cs to any GameObject
// Run in editor or on headset
// Shows position, rotation, pointer
```

### GPS Test
```csharp
// Add RealGpsManager.cs
// Build to headset
// Go outdoors with sky visibility
// Wait for "GPS LOCKED" message
```

### AI Test
```csharp
// Add DeviceRecognizer + AiCopilotUI
// Take photo of known device
// Verify AI suggests correct type
// Test accept/reject workflow
```

## Performance Notes

### Lightweight Scripts (Run Every Frame)
- `XrInitializer` - Minimal overhead after init
- `RealGpsManager` - Updates only when GPS updates
- `SimpleXrTester` - Debug only, remove for production

### Heavy Operations (Async/On Demand)
- `DeviceRecognizer.AnalyzePhoto()` - Async analysis
- `CsvManager.SaveCsv()` - File I/O
- `ExternalDataConnector` - Network calls

### VR Performance Tips
- Keep UI Canvas at world space (not screen space)
- Use single-pass instanced rendering
- Limit real-time shadows
- Use mobile-optimized shaders

## Troubleshooting

### Script not working
- Check if attached to correct GameObject
- Verify dependencies are assigned
- Check Unity Console for errors

### Missing references
```csharp
// Auto-find in Start():
csvManager = FindObjectOfType<CsvManager>();
calibration = FindObjectOfType<CalibrationManager>();
```

### Event not firing
- Ensure += subscription happens before event
- Check for null references
- Verify script execution order

## Extending

### Adding New Device Category
```csharp
// In DeviceRecognizer.cs
knownCategories.Add(new DeviceCategory {
    type = "New Device",
    systemType = "System",
    keywords = new[] { "visual", "keywords", "here" }
});
```

### Custom Export Format
```csharp
// In GeoData.cs
public string ToCustomFormat() {
    return $"{DeviceName},{Latitude},{Longitude},{CustomField}";
}
```

### New Validation Rule
```csharp
// In CsvManager or QA script
if (device.GpsAccuracy > 10) {
    device.ReviewStatus = "HIGH_GPS_ERROR";
}
```

## See Also

- `QUICK_START.md` - Setup instructions
- `REAL_GPS_SETUP.md` - GPS configuration
- `AI_RECOGNITION_GUIDE.md` - AI features
- `TROUBLESHOOTING.md` - Common issues
