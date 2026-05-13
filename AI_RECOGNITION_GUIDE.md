# AI Device Recognition Guide

Computer vision and AI assistance for identifying devices during XR surveys.

## Overview

The AI Survey Copilot helps identify devices from photos, suggests matches from existing databases, and assists with data entry - **but never auto-writes without your confirmation**.

## How It Works

### 1. Photo Capture
```
1. Surveyor aims at device
2. Triggers photo capture
3. Image saved locally
```

### 2. AI Analysis (Multiple Methods)
```
Method 1: External ML Model (TensorFlow, etc.)
   - Runs on headset or cloud
   - Identifies device type from image
   - Returns confidence score

Method 2: Local Analysis (Fallback)
   - Analyzes image colors/patterns
   - Matches against known categories
   - Uses filename/keywords

Method 3: Database Matching
   - Connects to "other machine" with device data
   - Compares photo against existing device photos
   - Finds similar devices by visual similarity
```

### 3. Suggestion Display
```
AI shows top 3 suggestions:

#1 DOME CAMERA (85% confidence)
   CCTV System | Color analysis: Round white object
   [ACCEPT] [REJECT] [DETAILS]

#2 BULLET CAMERA (62% confidence)
   CCTV System | Shape: Cylindrical
   [ACCEPT] [REJECT] [DETAILS]

#3 PTZ CAMERA (45% confidence)
   CCTV System | Keywords in filename
   [ACCEPT] [REJECT] [DETAILS]

[SKIP AI] [MANUAL ENTRY]
```

### 4. User Decision (GUARD RAIL)
```
❌ AI NEVER auto-writes
✅ User must click ACCEPT
✅ User can reject and see other options
✅ User can skip AI entirely
✅ User can enter manually
```

## Device Categories

The AI knows these device types:

| Category | System | Key Visual Features |
|----------|--------|---------------------|
| **Dome Camera** | CCTV | Round, enclosed, ceiling mounted |
| **Bullet Camera** | CCTV | Cylindrical, outdoor, wall mount |
| **PTZ Camera** | CCTV | Dome shape, motorized |
| **Card Reader** | Access Control | Keypad, card slot, wall mount |
| **Door Contact** | Access Control | Small, magnetic, door frame |
| **Smoke Detector** | Fire Alarm | Round, white, ceiling mounted |
| **Horn Strobe** | Fire Alarm | Red, loud, visible strobe |
| **Pull Station** | Fire Alarm | Red, handle, "PULL" text |
| **Motion Sensor** | Intrusion | White dome, PIR lens |
| **Glass Break** | Intrusion | Small, white, acoustic |
| **Thermostat** | HVAC | Wall mount, digital display |
| **Damper** | HVAC | Duct mounted, fire rated |
| **Exit Sign** | Emergency | Green/red, illuminated |
| **Intercom** | Communications | Speaker, microphone, buttons |

## Integration with External Database

### The "Other Machine"

Your existing device database can be queried for:
- **Device names** at this site
- **Device types** typically found
- **Historical photos** for visual matching
- **Spatial relationships** ("camera A is next to door B")

### Setup Connection
```csharp
// ExternalDataConnector.cs
baseUrl = "http://192.168.1.100:8080";  // Your machine's IP
useExternalDatabase = true;

// Endpoints:
GET /api/devices              - List all devices
GET /api/devices/search?q=... - Search by name/type
POST /api/match-image         - Upload photo for matching
```

### Data Flow
```
XR App captures photo
        |
        v
Upload to external service (optional)
        |
        v
Query: "Find devices matching this image"
        |
        v
Return: Similar devices with similarity scores
        |
        v
Show to user as suggestions
```

## AI Confidence Levels

| Confidence | Color | Action |
|------------|-------|--------|
| **80-100%** | 🟢 Green | High confidence - likely correct |
| **50-79%** | 🟡 Yellow | Medium confidence - verify with user |
| **<50%** | 🔴 Red | Low confidence - manual entry likely |

## User Interface

### During Capture
```
[●] Aiming at device...
[●] Photo captured
[●] AI analyzing...
[●] Suggestions ready
```

### Suggestion Panel
```
+----------------------------------+
| AI SUGGESTION                    |
|                                  |
| #1 DOME CAMERA        85% ◔     |
|    CCTV System                   |
|    Round white object detected   |
|                                  |
|    [ACCEPT]  [REJECT]  [INFO]   |
|                                  |
| #2 BULLET CAMERA      62% ◔     |
|    CCTV System                   |
|    [ACCEPT]  [REJECT]            |
|                                  |
| [SKIP AI]  [MANUAL ENTRY]       |
+----------------------------------+
```

### After Selection
```
✓ Accepted: DOME CAMERA (85%)
Device ID: CAM-101
Type: CCTV
AI Method: Color + Shape Analysis

[CONTINUE CAPTURE]
```

## Configuration

### Adjust AI Behavior
```csharp
// DeviceRecognizer.cs settings
confidenceThreshold = 0.7f;    // Only show suggestions >70%
maxSuggestions = 3;          // Show top 3 matches
useExternalDatabase = true;    // Connect to other machine
```

### Add Custom Categories
```csharp
// Add to knownCategories list
new DeviceCategory {
    type = "Custom Device",
    systemType = "Custom System",
    keywords = new[] { "keyword1", "keyword2", "visual_feature" }
}
```

## Performance Considerations

### On-Device Processing
- **Pros**: Fast, works offline, no network needed
- **Cons**: Limited model size, less accurate
- **Best for**: Keyword matching, basic color analysis

### Cloud/External Processing
- **Pros**: Powerful ML models, high accuracy, database matching
- **Cons**: Requires network, slower, privacy concerns
- **Best for**: Visual similarity matching, complex recognition

### Hybrid Approach (Recommended)
```
1. Try external API first (best accuracy)
2. If fails/unavailable, use local analysis
3. Combine results and rank by confidence
4. Show user the best options
```

## Training Custom Models

### Option 1: TensorFlow Lite
```python
# Train on your device photos
# Convert to TFLite model
# Load in Unity with TensorFlow Lite plugin
```

### Option 2: Cloud Service
```csharp
// AWS Rekognition, Google Vision, Azure Computer Vision
// Upload image, get labels
// Filter for security/device related labels
```

### Option 3: Simple Heuristics (Current)
```csharp
// Color analysis (red = fire alarm, white = camera)
// Shape detection (dome, cylinder, box)
// Filename keywords (CAM, FA, ACS)
// No training required!
```

## Limitations & Honesty

### What AI Can Do Well
- ✅ Match colors to categories (red = fire alarm)
- ✅ Identify common shapes (dome, bullet, box)
- ✅ Suggest based on location context
- ✅ Match against existing photo database
- ✅ Speed up data entry for common devices

### What AI Struggles With
- ❌ Rare/unique devices
- ❌ Poor lighting/blurry photos
- ❌ Unusual mounting positions
- ❌ Damaged/modified equipment
- ❌ Brand/model identification

### GUARD RAIL: Always User Control
```
AI can suggest, but NEVER decides.
User must:
- See the suggestion
- Understand the reasoning
- Click ACCEPT to apply
- Or reject and choose manually
```

## Testing AI Recognition

### Test 1: Known Device
```
1. Photograph a clearly visible dome camera
2. AI should suggest "DOME CAMERA" with >70% confidence
3. Accept and verify correct type assigned
```

### Test 2: Ambiguous Device
```
1. Photograph a device that's hard to identify
2. AI should show multiple options or low confidence
3. Verify user can manually enter or skip
```

### Test 3: External Database
```
1. Upload photo to external matching service
2. Verify similar devices from database are found
3. Check that similarity scores make sense
```

## Troubleshooting

### "No suggestions shown"
- Lower confidenceThreshold (e.g., 0.5f)
- Check if categories are relevant to your site
- Verify image quality (lighting, focus)

### "Wrong suggestions"
- Add more specific keywords to categories
- Train custom model on your specific devices
- Use external database for visual matching

### "External connection fails"
- Check network connection
- Verify baseUrl and endpoints
- Check if external machine is running
- Fall back to local analysis

## Future Enhancements

### Phase 1: Basic (Current)
- ✅ Color/shape analysis
- ✅ Keyword matching
- ✅ External database query

### Phase 2: Improved
- 📝 TensorFlow Lite model on headset
- 📝 More device categories
- 📝 Better confidence scoring

### Phase 3: Advanced
- 📝 Real-time recognition (before capture)
- 📝 Spatial reasoning ("device near door")
- 📝 Brand/model identification
- 📝 Anomaly detection (unexpected device)

## Privacy & Security

### Photo Handling
- ✅ Photos stay on headset until transferred
- ✅ External analysis is optional
- ✅ Can work completely offline
- ✅ User controls what gets uploaded

### External Database
- ✅ Read-only queries (no writes)
- ✅ API key authentication
- ✅ HTTPS recommended for production
- ✅ No sensitive location data exposed
