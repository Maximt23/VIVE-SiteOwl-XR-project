# QGIS Integration for SiteOwl XR Surveyor

Real GIS workflow for real GPS data. No more made-up coordinates!

## What's Included

| File | Purpose |
|------|---------|
| `import_siteowl_csv.py` | Load survey CSV into QGIS with styling |
| `validate_survey.py` | QA checks on captured data |
| `export_formats.py` | Export to KML, GeoJSON, Shapefile, CSV |
| `siteowl_surveyor_template.qgz` | QGIS project template (create in QGIS) |

## Quick Start

### 1. Install QGIS

Download: https://qgis.org/en/site/forusers/download.html

### 2. Install Required Plugins

QGIS → Plugins → Manage and Install Plugins:
- **QuickMapServices** (Google Satellite, OpenStreetMap)
- **Lat Lon Tools** (coordinate conversion)

### 3. Create Project Template

```python
# In QGIS:
Project → New
Project Properties → CRS → EPSG:4326 (WGS 84)
Web → QuickMapServices → Google → Google Satellite
Project → Save As → siteowl_surveyor_template.qgz
```

### 4. Import Survey Data

```python
# In QGIS Python Console (Plugins → Python Console):
exec(open('C:/path/to/import_siteowl_csv.py').read())
layer = import_siteowl_csv("C:/Survey/captured_devices.csv", "Site 2996")
```

### 5. Validate Data

```python
exec(open('C:/path/to/validate_survey.py').read())
issues = validate_survey("Site 2996")
calculate_statistics("Site 2996")
```

### 6. Export to All Formats

```python
exec(open('C:/path/to/export_formats.py').read())
export_all_formats("Site 2996", "C:/Survey/Exports/Site2996/")
```

## Data Flow

```
VIVE XR Elite (Field Capture)
    |
    v
Captured CSV (Real GPS)
    |
    v
QGIS Import → Visual Validation → QA Checks
    |
    v
Export Formats:
    ├── KML → Google Earth
    ├── GeoJSON → Web Maps
    ├── Shapefile → ArcGIS
    └── CSV → SiteOwl Import
```

## Coordinate Systems

### Input (from XR App)
- **CRS**: EPSG:4326 (WGS 84)
- **Format**: CSV with Latitude, Longitude columns
- **Accuracy**: Real GPS accuracy in meters

### QGIS Display
- **Base Map**: Google Satellite (real aerial imagery)
- **Validation**: Visual overlay on real buildings/roads
- **QA**: Distance checks, duplicates, outliers

### Output
- **KML**: Google Earth 3D visualization
- **GeoJSON**: Web map integration
- **Shapefile**: Enterprise GIS (ArcGIS, etc.)
- **CSV**: Back to SiteOwl or databases

## Validation Checks

The `validate_survey.py` script checks for:

1. **Missing GPS**: Devices without coordinates
2. **Poor Accuracy**: >30m GPS accuracy (flagged for review)
3. **Duplicates**: Devices within 1m of each other
4. **Out of Bounds**: Outside survey boundary polygon
5. **Statistics**: Average accuracy, distribution

## Styling

Points are automatically colored by GPS quality:
- 🟢 **Green**: Excellent (<3m accuracy)
- 🟡 **Yellow**: Good (3-10m)
- 🟠 **Orange**: Fair (10-30m)
- 🔴 **Red**: Poor (>30m)

## Tips

- **Load boundary first**: Create a polygon layer named "survey_boundary" for out-of-bounds checking
- **Photo locations**: Import photo CSV separately to see capture locations
- **Google Earth**: Export KML and open in Google Earth Pro for 3D building context
- **Share**: GeoJSON files can be shared via web or embedded in reports

## Troubleshooting

**"Layer not found"**: Make sure layer name matches exactly (case-sensitive)

**"Import failed"**: Check CSV has Latitude/Longitude columns with numeric values

**"Blank map"**: Ensure QuickMapServices plugin is installed and internet is available

**"Wrong location"**: Verify CRS is EPSG:4326 (not a local projected CRS)
