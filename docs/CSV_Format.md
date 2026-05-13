# CSV Format Specification

## Input CSV (From SiteOwl)

The input CSV contains existing device records with missing coordinate data.

### Required Columns

| Column | Type | Description |
|--------|------|-------------|
| Site | String | Site identifier (e.g., "2996") |
| Device Name | String | Human-readable name (e.g., "CAM-101") |
| Device Type | String | Equipment type (e.g., "Dome Camera") |
| System Type | String | System category (e.g., "CCTV", "Fire Alarm") |

### Capture Columns (Populated by XR App)

| Column | Type | Description |
|--------|------|-------------|
| X | Float | SiteOwl X coordinate (meters) |
| Y | Float | SiteOwl Y coordinate (meters) |
| Latitude | Double | GPS latitude (estimated, optional) |
| Longitude | Double | GPS longitude (estimated, optional) |
| Photo Path | String | Path to captured image file |
| Review Status | String | "OK", "REVIEW_REQUIRED", or "PENDING" |

### Optional Capture Metadata Columns

| Column | Type | Description |
|--------|------|-------------|
| Capture Timestamp | ISO 8601 | When capture occurred |
| Capture Method | String | "XR_CAPTURE" |
| Coordinate Confidence | String | "HIGH", "MEDIUM", "LOW" |
| User X | Float | User position at capture (SiteOwl space) |
| User Y | Float | User position at capture (SiteOwl space) |
| User Facing | Float | User facing direction (degrees, 0=North) |

## Output CSV (Updated for SiteOwl Import)

The output CSV follows the same format as input, with captured fields populated.

### Review Status Values

- **PENDING** - Device has not been captured yet
- **OK** - Captured with acceptable confidence
- **REVIEW_REQUIRED** - Captured but requires human review (low confidence, out of bounds, duplicate, etc.)

### Example Input Row

```csv
2996,CAM-101,Dome Camera,CCTV,,,,,,PENDING
```

### Example Output Row

```csv
2996,CAM-101,Dome Camera,CCTV,12.50,88.20,32.81234,-96.81234,/Photos/CAM-101_20250115_143022.jpg,OK
```

## Import Back to SiteOwl

The output CSV is designed for:
1. Direct import into SiteOwl (if supported)
2. Power Automate flow processing
3. Manual data entry with photo evidence
