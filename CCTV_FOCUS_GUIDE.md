# CCTV-Only Focus Guide

**Scope: CCTV cameras only.** No fire alarms, no access control, no HVAC - just cameras.

## Why CCTV-Only?

- ✅ **Simpler AI training** - Only need to recognize camera types
- ✅ **Faster development** - Narrow scope = faster delivery
- ✅ **Better accuracy** - AI specializes on 5 camera types vs 14 device types
- ✅ **Cleaner UI** - No clutter from irrelevant device categories
- ✅ **Focused workflow** - Surveyors know exactly what they're doing

## CCTV Equipment Categories

### Primary Camera Types

| Type | Visual Cues | Typical Location |
|------|-------------|----------------|
| **Dome Camera** | Round dome, ceiling mount, enclosed | Indoor ceilings, retail |
| **Bullet Camera** | Cylindrical, wall mount, exposed lens | Outdoor walls, parking |
| **PTZ Camera** | Dome shape, larger, motorized base | Large areas, need zoom |
| **Turret Camera** | Ball-in-socket, wall/ceiling, no glass dome | Indoor/outdoor versatile |
| **Fisheye Camera** | Small dome, 360° lens, very wide view | Corners, small rooms |
| **Panoramic Camera** | Multi-sensor, wide housing | Large open areas |
| **Thermal Camera** | Usually white housing, no visible lens | Perimeter, night vision |
| **Pinhole Camera** | Very small, hidden, tiny lens | Discrete locations |

### Mounting Types

| Mount | Description | Camera Types |
|-------|-------------|--------------|
| **Ceiling Mount** | Flush or pendant from ceiling | Dome, Turret, Fisheye |
| **Wall Mount** | Bracket from vertical surface | Bullet, Turret, Dome |
| **Pole Mount** | Strapped to vertical pole | Bullet, PTZ, Thermal |
| **Corner Mount** | Specialized corner bracket | Dome, Fisheye |
| **Parapet Mount** | Rooftop edge, vertical/horizontal | PTZ, Bullet, Panoramic |
| **Pendant Mount** | Hanging from ceiling/arm | PTZ, Dome |
| **In-Ceiling** | Recessed into drop ceiling | Dome, Turret |

### Housing Types

| Housing | Description | Use Case |
|---------|-------------|----------|
| **Indoor Standard** | Plastic/metal, no weather sealing | Interior spaces |
| **Outdoor Weatherproof** | IP66/67 rated, sealed | Exterior exposed |
| **Explosion-Proof** | Heavy metal, hazardous locations | Industrial, gas stations |
| **Vandal-Resistant** | IK10 rated, impact resistant | Public spaces, prisons |
| **Thermal Housing** | Insulated, heater/blower | Extreme temperatures |

## Simplified CSV Format

```csv
Site,CameraID,CameraType,MountType,HousingType,IP_Address,VMS_Zone,X,Y,Latitude,Longitude,Photo_Path,GPS_Accuracy,Review_Status
2996,CAM-001,Dome,Ceiling,Indoor,192.168.1.101,Front_Entrance,12.5,88.2,32.81234,-96.81234,/path/to/photo.jpg,2.1,OK
2996,CAM-002,Bullet,Wall,Outdoor,192.168.1.102,Parking_North,45.0,120.5,32.81240,-96.81230,/path/to/photo.jpg,1.8,OK
2996,CAM-003,PTZ,Pole,Outdoor,192.168.1.103,Loading_Dock,78.25,45.0,32.81228,-96.81245,/path/to/photo.jpg,3.2,OK
2996,CAM-004,Turret,Wall,Outdoor,192.168.1.104,Warehouse_Side,22.3,95.7,32.81235,-96.81233,/path/to/photo.jpg,2.5,OK
```

## AI Recognition - CCTV Only

### Training Categories (8 total)

```csharp
// CCTV-only categories for AI
new CameraCategory { 
    type = "Dome Camera", 
    keywords = new[] { "dome", "round", "ceiling", "enclosed", "bubble" },
    visualFeatures = new[] { "circular shape", "white or black dome", "ceiling mounted" }
},
new CameraCategory { 
    type = "Bullet Camera", 
    keywords = new[] { "bullet", "cylinder", "wall", "rain guard", "visor" },
    visualFeatures = new[] { "cylindrical body", "pointing outward", "mounting arm" }
},
new CameraCategory { 
    type = "PTZ Camera", 
    keywords = new[] { "ptz", "dome", "moving", "motorized", "base" },
    visualFeatures = new[] { "larger dome", "motor base", "positioner" }
},
new CameraCategory { 
    type = "Turret Camera", 
    keywords = new[] { "turret", "eyeball", "ball", "socket" },
    visualFeatures = new[] { "ball-in-socket", "adjustable angle", "no dome" }
},
new CameraCategory { 
    type = "Fisheye Camera", 
    keywords = new[] { "fisheye", "360", "panoramic", "small dome" },
    visualFeatures = new[] { "small dome", "ceiling corner", "360 marking" }
},
new CameraCategory { 
    type = "Panoramic Camera", 
    keywords = new[] { "panoramic", "multi-sensor", "wide", "array" },
    visualFeatures = new[] { "wide housing", "multiple lenses", "180+ degrees" }
},
new CameraCategory { 
    type = "Thermal Camera", 
    keywords = new[] { "thermal", "infrared", "heat", "white housing" },
    visualFeatures = new[] { "white housing", "no visible glass", "perimeter" }
},
new CameraCategory { 
    type = "Pinhole Camera", 
    keywords = new[] { "pinhole", "hidden", "mini", "covert" },
    visualFeatures = new[] { "very small", "tiny lens", "discrete location" }
}
```

### AI Recognition Logic (CCTV Focused)

```csharp
if (detectedFeatures.Contains("round_dome") && mountedOn == "ceiling")
    suggest = "Dome Camera";

if (detectedFeatures.Contains("cylindrical") && mountedOn == "wall")
    suggest = "Bullet Camera";

if (detectedFeatures.Contains("dome") && hasMotorizedBase)
    suggest = "PTZ Camera";

if (detectedFeatures.Contains("ball_socket") && !hasDome)
    suggest = "Turret Camera";

if (detectedFeatures.Contains("360") || detectedFeatures.Contains("ultra_wide"))
    suggest = "Fisheye Camera";
```

## Simplified UI

### Camera Selector
```
+----------------------------------+
| SELECT CAMERA TYPE               |
+----------------------------------+
| ◯ Dome Camera         [SELECT]  |
| ▶ Bullet Camera        [SELECT]  |
| ◉ PTZ Camera           [SELECT]  |
| ● Turret Camera        [SELECT]  |
| ◯ Fisheye (360°)      [SELECT]  |
| ▽ Panoramic            [SELECT]  |
| ◯ Thermal              [SELECT]  |
| ● Pinhole/Mini         [SELECT]  |
+----------------------------------+
| [AI SUGGEST FROM PHOTO]          |
| [SCAN BARCODE/QR]                |
+----------------------------------+
```

### Capture Screen
```
+----------------------------------+
| CAPTURING: CAM-003               |
| Type: PTZ Camera                 |
| Mount: Pole Mount                |
|                                  |
| Position: Locked                 |
| GPS: 32.81234, -96.81245 ±2.3m |
| Photo: Captured ✓               |
|                                  |
| [RETAKE PHOTO]  [CONFIRM]        |
+----------------------------------+
```

## CCTV-Specific Workflow

### Phase 1: Pre-Survey
```
1. Receive camera list from client
2. Note any missing coordinates
3. Load into XR app
4. Surveyor reviews list before field work
```

### Phase 2: Field Survey (XR)
```
1. Walk to camera location
2. Select camera from list (or AI suggest)
3. Aim at camera, capture position
4. Photo auto-captured
5. AI suggests camera type (confirm/edit)
6. Note mount type and housing
7. Save and move to next camera
```

### Phase 3: Office (QGIS)
```
1. Import all captured cameras
2. Visual validation on satellite map
3. Check for gaps in coverage
4. Verify positions make sense
5. Export clean CSV
6. Send to VMS/integrator
```

## CCTV-Specific QA Checks

### Automatic Validations
```python
# In QGIS validate_cameras.py:

1. Check camera spacing
   - Minimum: 2m apart (not too close)
   - Maximum: 100m apart in same zone (coverage gaps)

2. Mount type vs location
   - Outdoor camera with indoor mount? Flag it.
   - Ceiling mount on outdoor wall? Flag it.

3. Viewing angle validation
   - Cameras pointing at walls? Flag it.
   - Overlapping coverage? Note it.

4. IP address conflicts
   - Duplicate IPs in list? Flag it.
   - Invalid subnet? Flag it.
```

## External Database Integration (CCTV Focused)

### API Endpoints for CCTV System
```
GET /api/cameras?site=2996
→ Returns all cameras at site

GET /api/cameras/unlocated?site=2996
→ Returns cameras missing coordinates

POST /api/cameras/match-image
→ Upload photo, find matching camera model

GET /api/cameras/types
→ Returns supported camera types for dropdown

POST /api/cameras/{id}/location
→ Update camera coordinates (from XR capture)
```

### CCTV Data Model
```json
{
  "cameraId": "CAM-003",
  "siteId": "2996",
  "cameraType": "PTZ",
  "mountType": "Pole",
  "housingType": "Outdoor",
  "manufacturer": "Axis",
  "model": "Q6155-E",
  "ipAddress": "192.168.1.103",
  "macAddress": "00:40:8C:XX:XX:XX",
  "vmsZone": "Loading_Dock",
  "coordinates": {
    "siteowlX": 78.25,
    "siteowlY": 45.0,
    "latitude": 32.81228,
    "longitude": -96.81245
  },
  "photoUrl": "/photos/cam003_20250115.jpg",
  "lastSurvey": "2025-01-15T14:30:00Z"
}
```

## Benefits of CCTV-Only Scope

### For Surveyors
- ✅ Familiar equipment (all cameras)
- ✅ Predictable mounting patterns
- ✅ Clear visual identification
- ✅ Faster recognition/decisions

### For AI
- ✅ Limited categories (8 vs 14+)
- ✅ More training data per category
- ✅ Higher accuracy
- ✅ Faster inference

### For Development
- ✅ Smaller codebase
- ✅ Simpler UI
- ✅ Focused testing
- ✅ Faster delivery

### For Clients
- ✅ Purpose-built tool
- ✅ No irrelevant features
- ✅ Clean reports
- ✅ Professional focus

## Future Expansion (Phase 2+)

If needed later, can add:
- 🔗 Access Control (card readers, door contacts)
- 🔥 Fire Alarm (pull stations, smoke detectors)
- 🌡️ Environmental (temp sensors, leak detectors)
- 🛡️ Intrusion (motion sensors, glass break)

**But for now: CCTV ONLY.** Do one thing and do it well!

## Sample Camera Database

```csv
Site,CameraID,CameraType,MountType,Manufacturer,Model,IP_Address,VMS_Zone,Status
2996,CAM-001,Dome,Ceiling,Axis,P3245-LV,192.168.1.101,Front_Entrance,Active
2996,CAM-002,Bullet,Wall,Hikvision,DS-2CD2T85G1-I5,192.168.1.102,Parking_North,Active
2996,CAM-003,PTZ,Pole,Axis,Q6155-E,192.168.1.103,Loading_Dock,Active
2996,CAM-004,Turret,Wall,Dahua,IPC-HDW5841T-ZE,192.168.1.104,Warehouse_East,Active
2996,CAM-005,Fisheye,Ceiling,Axis,M3057-PLVE,192.168.1.105,Reception,Active
2996,CAM-006,Panoramic,Wall,Avigilon,H4 Multisensor,192.168.1.106,Main_Floor,Active
2996,CAM-007,Thermal,Parapet,FLIR,PT-Series,192.168.1.107,Perimeter_North,Active
```

## Quick Decision Tree

```
Is it round and ceiling mounted?
  ├─− Yes → Dome or Fisheye
  └─− No → Continue

Is it cylindrical and wall mounted?
  ├─− Yes → Bullet
  └─− No → Continue

Is it dome-shaped with motor base?
  ├─− Yes → PTZ
  └─− No → Continue

Is it ball-shaped in socket?
  ├─− Yes → Turret
  └─− No → Continue

Is it wide/multi-sensor?
  ├─− Yes → Panoramic
  └─− No → Continue

Is it white, no visible lens, perimeter?
  ├─− Yes → Thermal
  └─− No → Pinhole or Unknown
```

## Summary

**CCTV-Only Focus = Success**

- ✓ Simpler to build
- ✓ Faster to train AI
- ✓ Easier for surveyors
- ✓ More accurate recognition
- ✓ Cleaner deliverables

**Build the perfect CCTV surveying tool, then expand later if needed.**
