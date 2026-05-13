#!/usr/bin/env python3
"""
QGIS Python Script: Export Survey Data to Multiple Formats
Export to KML (Google Earth), GeoJSON (Web), Shapefile (ArcGIS), etc.
"""

import os
from qgis.core import QgsVectorFileWriter, QgsCoordinateTransformContext, QgsProject

def export_to_kml(layer_name, output_path):
    """Export layer to KML for Google Earth"""
    layer = QgsProject.instance().mapLayersByName(layer_name)
    if not layer:
        print(f"Layer {layer_name} not found")
        return
    
    layer = layer[0]
    
    options = QgsVectorFileWriter.SaveVectorOptions()
    options.driverName = "KML"
    options.fileEncoding = "UTF-8"
    
    error = QgsVectorFileWriter.writeAsVectorFormatV2(
        layer,
        output_path,
        QgsCoordinateTransformContext(),
        options
    )
    
    if error[0] == QgsVectorFileWriter.NoError:
        print(f"✓ Exported to KML: {output_path}")
    else:
        print(f"✗ KML export failed: {error}")

def export_to_geojson(layer_name, output_path):
    """Export layer to GeoJSON for web maps"""
    layer = QgsProject.instance().mapLayersByName(layer_name)
    if not layer:
        print(f"Layer {layer_name} not found")
        return
    
    layer = layer[0]
    
    options = QgsVectorFileWriter.SaveVectorOptions()
    options.driverName = "GeoJSON"
    options.fileEncoding = "UTF-8"
    
    error = QgsVectorFileWriter.writeAsVectorFormatV2(
        layer,
        output_path,
        QgsCoordinateTransformContext(),
        options
    )
    
    if error[0] == QgsVectorFileWriter.NoError:
        print(f"✓ Exported to GeoJSON: {output_path}")
    else:
        print(f"✗ GeoJSON export failed: {error}")

def export_to_shapefile(layer_name, output_path):
    """Export layer to Shapefile for ArcGIS"""
    layer = QgsProject.instance().mapLayersByName(layer_name)
    if not layer:
        print(f"Layer {layer_name} not found")
        return
    
    layer = layer[0]
    
    options = QgsVectorFileWriter.SaveVectorOptions()
    options.driverName = "ESRI Shapefile"
    options.fileEncoding = "UTF-8"
    
    error = QgsVectorFileWriter.writeAsVectorFormatV2(
        layer,
        output_path,
        QgsCoordinateTransformContext(),
        options
    )
    
    if error[0] == QgsVectorFileWriter.NoError:
        print(f"✓ Exported to Shapefile: {output_path}")
    else:
        print(f"✗ Shapefile export failed: {error}")

def export_clean_csv(layer_name, output_path, min_quality="Fair"):
    """
    Export cleaned CSV for SiteOwl import
    Filters out poor quality and invalid data
    """
    layer = QgsProject.instance().mapLayersByName(layer_name)
    if not layer:
        print(f"Layer {layer_name} not found")
        return
    
    layer = layer[0]
    
    quality_order = ["Excellent", "Good", "Fair", "Poor"]
    min_index = quality_order.index(min_quality) if min_quality in quality_order else 2
    
    valid_count = 0
    filtered_count = 0
    
    with open(output_path, 'w') as f:
        # Write header
        f.write("Site,DeviceName,Latitude,Longitude,Altitude,GpsAccuracy,GpsQuality,Timestamp,SiteOwlX,SiteOwlY,ReviewStatus\n")
        
        for feature in layer.getFeatures():
            quality = feature.attribute("GpsQuality") or "Unknown"
            
            if quality in quality_order and quality_order.index(quality) <= min_index:
                valid_count += 1
                
                site = feature.attribute("SiteId") or ""
                device = feature.attribute("DeviceName") or ""
                lat = feature.attribute("Latitude") or 0
                lon = feature.attribute("Longitude") or 0
                alt = feature.attribute("Altitude") or 0
                acc = feature.attribute("GpsAccuracy") or 0
                time = feature.attribute("GpsTimestamp") or ""
                sx = feature.attribute("SiteOwlX") or 0
                sy = feature.attribute("SiteOwlY") or 0
                
                f.write(f"{site},{device},{lat},{lon},{alt},{acc},{quality},{time},{sx},{sy},OK\n")
            else:
                filtered_count += 1
    
    print(f"✓ Exported clean CSV: {output_path}")
    print(f"  Valid records: {valid_count}")
    print(f"  Filtered (low quality): {filtered_count}")

def export_all_formats(layer_name, output_dir):
    """Export layer to all common formats"""
    
    os.makedirs(output_dir, exist_ok=True)
    
    print(f"\nExporting {layer_name} to all formats...")
    print("="*50)
    
    # KML for Google Earth
    export_to_kml(layer_name, os.path.join(output_dir, f"{layer_name}.kml"))
    
    # GeoJSON for web
    export_to_geojson(layer_name, os.path.join(output_dir, f"{layer_name}.geojson"))
    
    # Shapefile for ArcGIS
    export_to_shapefile(layer_name, os.path.join(output_dir, f"{layer_name}.shp"))
    
    # Clean CSV for SiteOwl
    export_clean_csv(layer_name, os.path.join(output_dir, f"{layer_name}_clean.csv"), "Fair")
    
    print("="*50)
    print(f"All exports saved to: {output_dir}")

# USAGE:
# export_all_formats("Site 2996 Devices", "C:/Survey/Exports/Site2996/")
# export_to_kml("Site 2996 Devices", "C:/Survey/Exports/site2996.kml")
