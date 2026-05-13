#!/usr/bin/env python3
"""
QGIS Python Script: Validate Survey Data
Run QA checks on captured survey points
"""

from qgis.core import QgsVectorLayer, QgsProject, QgsFeatureRequest, QgsGeometry
import math

def validate_survey(layer_name="Survey Devices"):
    """
    Runs QA checks on survey data:
    1. Duplicate locations (devices too close)
    2. Out of bounds (outside expected area)
    3. Low accuracy (poor GPS quality)
    4. Missing data (null coordinates)
    """
    
    # Get the layer
    layer = QgsProject.instance().mapLayersByName(layer_name)
    if not layer:
        print(f"ERROR: Layer '{layer_name}' not found!")
        return
    
    layer = layer[0]
    issues = []
    
    print(f"\n{'='*50}")
    print(f"VALIDATING: {layer_name}")
    print(f"{'='*50}\n")
    
    # Check 1: Missing GPS coordinates
    null_count = 0
    for feature in layer.getFeatures():
        lat = feature.attribute("Latitude")
        lon = feature.attribute("Longitude")
        if lat is None or lon is None or lat == 0 or lon == 0:
            null_count += 1
            device = feature.attribute("DeviceName") or "Unknown"
            issues.append(f"MISSING GPS: {device}")
    
    if null_count > 0:
        print(f"❌ MISSING GPS: {null_count} devices")
    else:
        print(f"✓ All devices have GPS coordinates")
    
    # Check 2: Low accuracy
    poor_accuracy = 0
    for feature in layer.getFeatures():
        accuracy = feature.attribute("GpsAccuracy")
        if accuracy and float(accuracy) > 30:
            poor_accuracy += 1
            device = feature.attribute("DeviceName") or "Unknown"
            issues.append(f"POOR ACCURACY: {device} ({accuracy}m)")
    
    if poor_accuracy > 0:
        print(f"⚠️  POOR ACCURACY: {poor_accuracy} devices (>30m)")
    else:
        print(f"✓ All devices have acceptable accuracy")
    
    # Check 3: Duplicates (devices within 1m of each other)
    features = list(layer.getFeatures())
    duplicates = []
    
    for i, f1 in enumerate(features):
        for f2 in features[i+1:]:
            geom1 = f1.geometry()
            geom2 = f2.geometry()
            
            if geom1 and geom2:
                distance = geom1.distance(geom2)
                if distance < 1.0:  # Less than 1 meter
                    d1 = f1.attribute("DeviceName") or "Unknown"
                    d2 = f2.attribute("DeviceName") or "Unknown"
                    duplicates.append((d1, d2, distance))
                    issues.append(f"DUPLICATE: {d1} and {d2} ({distance:.2f}m apart)")
    
    if duplicates:
        print(f"⚠️  DUPLICATES: {len(duplicates)} pairs within 1m")
        for d1, d2, dist in duplicates[:5]:  # Show first 5
            print(f"   - {d1} & {d2}: {dist:.2f}m")
    else:
        print(f"✓ No duplicate locations detected")
    
    # Check 4: Out of bounds (if boundary layer exists)
    boundary_layers = [l for l in QgsProject.instance().mapLayers().values() 
                       if "boundary" in l.name().lower()]
    
    if boundary_layers:
        boundary = boundary_layers[0]
        outside = 0
        
        for feature in layer.getFeatures():
            point = feature.geometry()
            if not point.within(boundary.geometry()):
                outside += 1
                device = feature.attribute("DeviceName") or "Unknown"
                issues.append(f"OUT OF BOUNDS: {device}")
        
        if outside > 0:
            print(f"⚠️  OUT OF BOUNDS: {outside} devices outside survey area")
        else:
            print(f"✓ All devices within survey boundary")
    else:
        print(f"? No boundary layer found (create 'survey_boundary' polygon)")
    
    # Summary
    print(f"\n{'='*50}")
    print(f"VALIDATION SUMMARY")
    print(f"{'='*50}")
    print(f"Total devices: {layer.featureCount()}")
    print(f"Issues found: {len(issues)}")
    
    if issues:
        print(f"\nReview Required:")
        for issue in issues[:10]:  # Show first 10
            print(f"  - {issue}")
        if len(issues) > 10:
            print(f"  ... and {len(issues) - 10} more")
    else:
        print(f"\n✓ All checks passed! Data is clean.")
    
    return issues

def calculate_statistics(layer_name="Survey Devices"):
    """Calculate survey statistics"""
    
    layer = QgsProject.instance().mapLayersByName(layer_name)
    if not layer:
        return
    
    layer = layer[0]
    
    accuracies = []
    for feature in layer.getFeatures():
        acc = feature.attribute("GpsAccuracy")
        if acc and float(acc) > 0:
            accuracies.append(float(acc))
    
    if accuracies:
        avg_acc = sum(accuracies) / len(accuracies)
        min_acc = min(accuracies)
        max_acc = max(accuracies)
        
        print(f"\nGPS ACCURACY STATISTICS:")
        print(f"  Average: {avg_acc:.1f}m")
        print(f"  Best: {min_acc:.1f}m")
        print(f"  Worst: {max_acc:.1f}m")
        print(f"  Samples: {len(accuracies)}")

# USAGE:
# validate_survey("Site 2996 Devices")
# calculate_statistics("Site 2996 Devices")
