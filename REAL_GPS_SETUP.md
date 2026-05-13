# Real GPS Setup Guide

Complete guide to capturing and validating REAL GPS coordinates with VIVE XR Elite and QGIS.

## Overview

This system captures **actual satellite GPS** (not estimates) using the VIVE XR Elite's built-in GPS chip, then validates it against real satellite imagery in QGIS.

## Hardware Requirements

- VIVE XR Elite (has real GPS chip)
- Outdoor/line-of-sight to sky (GPS needs satellites)
- PC with QGIS installed
- USB cable (for first install) or WiFi

## Software Stack

### Unity (XR App)
- **RealGpsManager.cs**: Captures real GPS from Android Location Services
- **GeoData.cs**: Stores coordinates in WGS 84 (EPSG:4326)
- **Accuracy tracking**: Real GPS accuracy in meters

### QGIS (Validation)
- **QuickMapServices**: Google Satellite, real aerial imagery
- **Import script**: Load CSV as point layer
- **Validation script**: QA checks against real-world data
- **Export scripts**: KML, GeoJSON, Shapefile

## Step-by-Step Workflow

### Phase 1: Capture in the Field (VIVE XR Elite)

#### 1.1 Enable GPS on Headset
```
Settings → Location → ON
(Grant location permission to app on first run)
```

#### 1.2 Launch SiteOwl XR App
- App starts GPS service automatically
- Wait for "GPS LOCKED" message (30-60 seconds)
- Shows: "GPS: 32.812345, -96.812345 (±2.3m)"

#### 1.3 Survey Devices
```
For each device:
1. Select device from list
2. Aim at physical device
3. Press trigger
4. App captures:
   - SiteOwl X/Y (local reference)
   - REAL Latitude/Longitude (GPS satellites)
   - Accuracy (e.g., ±2.3m)
   - Timestamp
   - Photo
5. Save to CSV on headset
```

#### 1.4 Check GPS Quality
- **Green**: <3m accuracy (excellent)
- **Yellow**: 3-10m (good)
- **Orange**: 10-30m (fair)
- **Red**: >30m (poor, review required)

### Phase 2: Transfer Data to PC

#### 2.1 Connect Headset
```bash
# USB method:
adb devices

# Or WiFi (after setup):
adb connect <headset-ip>:5555
```

#### 2.2 Pull CSV File
```bash
adb pull /sdcard/Android/data/com.siteowl.xr.capture/files/Data/Output/
```

#### 2.3 Pull Photos (optional)
```bash
adb pull /sdcard/Android/data/com.siteowl.xr.capture/files/Photos/
```

### Phase 3: Validate in QGIS

#### 3.1 Open QGIS Project
```
QGIS → Project → Open → siteowl_surveyor_template.qgz
```

#### 3.2 Import Survey Data
```python
# In QGIS Python Console:
exec(open('QGIS/import_siteowl_csv.py').read())
import_siteowl_csv("Data/Output/captured_site_2996.csv", "Site 2996 Survey")
```

#### 3.3 See Your Data on Real Map
- Points appear on **Google Satellite imagery**
- Verify they align with **real buildings/equipment**
- Check for **obvious errors** (point in wrong location)

#### 3.4 Run Validation
```python
exec(open('QGIS/validate_survey.py').read())
validate_survey("Site 2996 Survey")
calculate_statistics("Site 2996 Survey")
```

#### 3.5 Review Issues
```
Example Output:
==================================================
VALIDATING: Site 2996 Survey
==================================================

✓ All devices have GPS coordinates
⚠️  POOR ACCURACY: 2 devices (>30m)
  - FA-003 (45.2m) - indoors?
  - CAM-107 (38.5m) - under roof?
✓ No duplicate locations detected
✓ All devices within survey boundary

GPS ACCURACY STATISTICS:
  Average: 4.2m
  Best: 1.1m
  Worst: 45.2m
  Samples: 23
```

### Phase 4: Export for SiteOwl

#### 4.1 Export to All Formats
```python
exec(open('QGIS/export_formats.py').read())
export_all_formats("Site 2996 Survey", "Data/Export/Site2996/")
```

#### 4.2 Output Files
```
Data/Export/Site2996/
  Site 2996 Survey.kml          → Google Earth
  Site 2996 Survey.geojson      → Web maps
  Site 2996 Survey.shp           → ArcGIS
  Site 2996 Survey_clean.csv     → SiteOwl import
```

#### 4.3 Review in Google Earth
1. Open KML file in Google Earth Pro (free)
2. See points on 3D terrain and buildings
3. Validate visual alignment
4. Screenshot for client report

## GPS Best Practices

### Getting Best Accuracy

| Factor | Impact | Solution |
|--------|--------|----------|
| **Sky visibility** | Critical | Avoid indoors, dense tree cover |
| **Building shadow** | Moderate | Stay away from tall buildings |
| **Time to lock** | 30-60s | Stand still at start for best lock |
| **Movement speed** | Minor | Walk slowly, don't run |
| **Multipath** | Location error | Avoid near reflective surfaces |

### When GPS Won't Work Well

❌ **Don't expect good GPS here:**
- Deep indoors (basements, interior rooms)
- Underground parking
- Dense urban canyons (downtown high-rises)
- Heavy tree canopy (dense forest)

✅ **GPS works great here:**
- Outdoors with sky view
- Roof access
- Parking lots
- Near windows (partial sky view)

### Handling Poor GPS

If indoors or poor signal:
1. **Mark location manually** in QGIS later
2. **Use SiteOwl X/Y** for relative positioning
3. **Flag for review** in validation
4. **Add notes**: "Indoors - GPS poor, positioned visually"

## Coordinate System Reference

### What We Use

| System | EPSG Code | Use Case |
|--------|-----------|----------|
| **WGS 84** | EPSG:4326 | GPS coordinates, global standard |
| **Web Mercator** | EPSG:3857 | Web maps (Google, OSM) |
| **SiteOwl Local** | Custom | Building-relative X/Y |

### Why WGS 84?

- GPS satellites broadcast in WGS 84
- Google Maps, Google Earth use it
- Universal standard (works anywhere)
- No projection distortion at local scale

### Converting Later

If SiteOwl needs different CRS:
```python
# In QGIS:
Layer → Save As → Select target CRS
# QGIS handles all math automatically
```

## Troubleshooting

### "GPS not locking"
- Move to area with better sky visibility
- Wait 60+ seconds
- Restart headset and try again
- Check if other apps can get GPS

### "Wrong location on map"
- Verify CSV has correct columns (Latitude, Longitude)
- Check CRS is EPSG:4326 (not swapped X/Y)
- Validate with phone GPS at same spot

### "Points don't match building"
- GPS accuracy might be lower than expected
- Check if building is new (not in satellite imagery)
- Consider ±5m typical accuracy

## Example Data

### Captured CSV Format
```csv
SiteId,DeviceName,Latitude,Longitude,Altitude,GpsAccuracy,GpsQuality,GpsTimestamp,SiteOwlX,SiteOwlY
2996,CAM-101,32.812345,-96.812345,156.2,2.3,Excellent,2025-01-15T14:30:22Z,12.50,88.20
2996,FA-001,32.812367,-96.812298,155.8,1.8,Excellent,2025-01-15T14:31:45Z,15.20,92.10
2996,CAM-102,32.812298,-96.812401,156.5,4.1,Good,2025-01-15T14:33:12Z,8.30,85.40
```

### Validation Report
```
Site 2996 Survey Validation
================================
Total devices: 23
Valid GPS: 23 (100%)
Excellent (<3m): 15 (65%)
Good (3-10m): 6 (26%)
Fair (10-30m): 2 (9%)
Poor (>30m): 0 (0%)

Issues:
- None! Data is clean.

Recommended: Ready for SiteOwl import
```

## Next Steps

1. ✅ Test GPS capture with SimpleXrTester
2. ✅ Install QGIS and import test data
3. ✅ Validate workflow with sample CSV
4. ✅ Export to Google Earth and verify
5. 🚀 Ready for real surveys!
