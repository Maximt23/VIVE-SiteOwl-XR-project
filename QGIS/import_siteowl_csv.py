#!/usr/bin/env python3
"""
QGIS Python Script: Import SiteOwl Survey CSV
Run this in QGIS Python Console to load captured survey data
"""

import os
from qgis.core import QgsVectorLayer, QgsProject, QgsField, QgsFeature, QgsGeometry, QgsPointXY
from PyQt5.QtCore import QVariant

def import_siteowl_csv(csv_path, layer_name="Survey Devices"):
    """
    Imports SiteOwl survey CSV into QGIS as a point layer.
    Expects columns: DeviceName, Latitude, Longitude, GpsAccuracy, etc.
    """
    
    if not os.path.exists(csv_path):
        print(f"ERROR: CSV file not found: {csv_path}")
        return None
    
    print(f"Importing: {csv_path}")
    
    # Create a memory layer for points
    uri = f"file:///{csv_path}?delimiter=,&xField=Longitude&yField=Latitude&crs=EPSG:4326"
    layer = QgsVectorLayer(uri, layer_name, "delimitedtext")
    
    if not layer.isValid():
        print("ERROR: Failed to create layer. Check CSV format.")
        return None
    
    # Add to project
    QgsProject.instance().addMapLayer(layer)
    
    # Style the layer
    from qgis.core import QgsSimpleMarkerSymbolLayer, QgsSymbol, QgsRendererCategory, QgsCategorizedSymbolRenderer
    
    # Create categories by GPS quality
    categories = []
    
    # Excellent (<3m) - Green
    symbol_excellent = QgsSymbol.defaultSymbol(layer.geometryType())
    symbol_excellent.setColor("#00FF00")
    symbol_excellent.setSize(6)
    category_excellent = QgsRendererCategory("Excellent", symbol_excellent, "Excellent (<3m)")
    categories.append(category_excellent)
    
    # Good (3-10m) - Yellow
    symbol_good = QgsSymbol.defaultSymbol(layer.geometryType())
    symbol_good.setColor("#FFFF00")
    symbol_good.setSize(6)
    category_good = QgsRendererCategory("Good", symbol_good, "Good (3-10m)")
    categories.append(category_good)
    
    # Fair (10-30m) - Orange
    symbol_fair = QgsSymbol.defaultSymbol(layer.geometryType())
    symbol_fair.setColor("#FFA500")
    symbol_fair.setSize(6)
    category_fair = QgsRendererCategory("Fair", symbol_fair, "Fair (10-30m)")
    categories.append(category_fair)
    
    # Poor (>30m) - Red
    symbol_poor = QgsSymbol.defaultSymbol(layer.geometryType())
    symbol_poor.setColor("#FF0000")
    symbol_poor.setSize(6)
    category_poor = QgsRendererCategory("Poor", symbol_poor, "Poor (>30m)")
    categories.append(category_poor)
    
    renderer = QgsCategorizedSymbolRenderer("GpsQuality", categories)
    layer.setRenderer(renderer)
    layer.triggerRepaint()
    
    # Zoom to layer
    canvas = iface.mapCanvas()
    canvas.setExtent(layer.extent())
    canvas.refresh()
    
    print(f"SUCCESS: Loaded {layer.featureCount()} devices")
    print(f"Layer: {layer_name}")
    print(f"CRS: EPSG:4326 (WGS 84)")
    
    return layer

# USAGE:
# csv_file = "C:/path/to/your/captured_devices.csv"
# layer = import_siteowl_csv(csv_file, "Site 2996 Devices")
