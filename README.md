# VIVE SiteOwl XR Capture

XR-assisted survey tool for capturing SiteOwl X/Y coordinates, GPS estimates, and device photos using a VIVE XR headset.

## Purpose

This tool helps surveyors capture missing coordinate/photo data for existing device records. The CSV already contains device details such as device name, type, system type, and description. The XR app only updates missing location and photo fields.

## Core Workflow

1. Load SiteOwl device CSV
2. Load task with known SiteOwl X/Y calibration coordinate
3. User stands at known task location
4. User sets calibration anchor
5. Headset tracks user position against the SiteOwl floorplan
6. User selects or searches a device
7. User aims at the physical device
8. App captures:
   - SiteOwl X
   - SiteOwl Y
   - Latitude
   - Longitude
   - Photo
   - Timestamp
   - Confidence
9. CSV updates the matching device row
10. Export file is ready for SiteOwl import / Power Automate flow

## Coordinate Systems

**Primary:** SiteOwl X/Y (source of truth)  
**Secondary:** GPS lat/long (estimated, especially indoors)

## Output Columns

| Column | Description |
|--------|-------------|
| X | SiteOwl X coordinate |
| Y | SiteOwl Y coordinate |
| Latitude | GPS latitude (estimated) |
| Longitude | GPS longitude (estimated) |
| Photo Path | Path to captured photo |
| Capture Timestamp | ISO 8601 timestamp |
| Capture Method | XR_CAPTURE |
| Coordinate Confidence | HIGH/MEDIUM/LOW |
| User X | User position in SiteOwl space |
| User Y | User position in SiteOwl space |
| User Facing Direction | Degrees from North |
| Review Status | OK or REVIEW_REQUIRED |

## Accuracy Goal

Target: within 3 meters  
Accuracy > 3m → mark as `REVIEW_REQUIRED`

## Development

### Requirements
- Unity 2022.3 LTS or newer
- VIVE XR Elite headset
- OpenXR Plugin
- VIVE OpenXR / Wave SDK (no Steam VR required!)

### Setup
1. Clone this repo
2. Open in Unity
3. Import required SDK packages
4. Build and deploy to headset via USB or ADB WiFi

## License

MIT - See LICENSE file
