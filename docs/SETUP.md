# Setup Guide

## Prerequisites

1. **Unity 2022.3 LTS** or newer
2. **VIVE XR Elite** headset with:
   - Fully charged battery
   - WiFi access (for Git push)
   - USB-C cable for debugging
3. **Android SDK** (for ADB deployment)
4. **Git** installed on the headset or on your PC

## Unity Project Setup

### 1. Create New Unity Project

```bash
# Or use Unity Hub
mkdir -p VIVE-SiteOwl-XR/UnityProject
cd VIVE-SiteOwl-XR/UnityProject
```

### 2. Switch Platform to Android

1. File → Build Settings
2. Select "Android"
3. Click "Switch Platform"

### 3. Install Required Packages

Open Package Manager (Window → Package Manager) and install:

**Required:**
- XR Plugin Management
- OpenXR Plugin
- OpenXR Meta (for VIVE Wave support)

**Optional:**
- XR Hands (for hand tracking)
- XR Interaction Toolkit (for UI interactions)

### 4. Configure XR Plugin Management

1. Edit → Project Settings → XR Plugin Management
2. Check "OpenXR" for Android tab
3. Click OpenXR → Add Feature
4. Select:
   - VIVE Controller Profile
   - Hand Interaction Profile (optional)
   - Passthrough (for camera capture)

### 5. Player Settings

Edit → Project Settings → Player (Android):

**Other Settings:**
- Minimum API Level: 29 (Android 10.0)
- Target API Level: 33+
- Scripting Backend: IL2CPP
- Target Architectures: ARM64

**Publishing Settings:**
- Custom Main Gradle Template: ✓ (for permissions)

### 6. Android Manifest Permissions

Add to `Assets/Plugins/Android/AndroidManifest.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    
    <!-- Camera for photo capture -->
    <uses-permission android:name="android.permission.CAMERA" />
    
    <!-- Location for GPS (optional, estimated) -->
    <uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
    <uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
    
    <!-- Storage for CSV and photos -->
    <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
    <uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
    
    <!-- Internet for Git push -->
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
    
    <application android:label="@string/app_name">
        <activity android:name="com.unity3d.player.UnityPlayerActivity"
                  android:exported="true">
            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.LAUNCHER" />
            </intent-filter>
        </activity>
    </application>
</manifest>
```

## VIVE XR Elite Setup

### Developer Mode

1. On headset: Settings → System → Developer → Enable USB Debugging
2. Connect USB-C to PC
3. Allow debugging prompt on headset

### WiFi ADB (Recommended)

```bash
# Connect via USB first
adb devices

# Enable ADB over WiFi
adb tcpip 5555

# Find headset IP (on headset: Settings → WiFi → [Network] → IP)
adb connect <HEADSET_IP>:5555

# Now you can unplug USB and develop wirelessly!
```

## Deployment Options

### Option 1: Direct USB Build & Run

1. Connect headset via USB
2. Unity: File → Build And Run
3. App installs and launches automatically

### Option 2: ADB WiFi Build & Run

```bash
# After WiFi ADB setup above
adb connect 192.168.1.XXX:5555

# In Unity, select device and Build And Run
```

### Option 3: APK Install

1. Unity: File → Build Settings → Build
2. Generate APK file
3. ADB install:
```bash
adb install -r VIVE-SiteOwl-XR.apk
```

## Data File Setup

### CSV Location

Place your SiteOwl CSV in:

**Android:** `/sdcard/Android/data/com.yourcompany.siteowlxr/files/devices.csv`

Or use the app's file picker to load from:
- Internal storage
- USB drive (OTG)
- Cloud storage (if installed)

### CSV Format

See `templates/devices_template.csv` for the expected format.

Required columns:
- `ID` or `DeviceID` - unique identifier
- `Name` or `DeviceName` - display name
- `Type` or `DeviceType` - equipment type
- `X` and `Y` - leave empty for capture

## Testing Without Steam VR

**Good news:** This app uses **OpenXR**, not Steam VR!

Just:
1. Put on VIVE XR Elite
2. Launch the app
3. Done! 

No Steam needed on the headset or PC.

## Troubleshooting

### "Camera not available"
- Ensure `uses-permission android.permission.CAMERA` is in manifest
- Check that Passthrough feature is enabled in OpenXR settings

### "Cannot read CSV"
- Verify file is at correct path
- Check CSV has proper headers
- Ensure storage permissions granted

### "Git push fails"
- Check WiFi connection
- Verify Git credentials on headset
- Check that remote origin is configured

### Tracking issues
- Ensure good lighting
- Clear guardian/play area
- Restart headset if drift is excessive
