# Quick Start - Build & Test on VIVE XR Elite

## 🎯 Goal: Get a working XR app on your headset in 30 minutes

**NO STEAM REQUIRED!** This uses Unity OpenXR directly.

## Prerequisites

- [ ] Unity Hub installed
- [ ] Unity 2022.3 LTS (or newer) installed with Android Build Support
- [ ] VIVE XR Elite with Developer Mode enabled
- [ ] USB-C cable
- [ ] Android SDK (comes with Unity or install separately)

## Step 1: Create Unity Project (5 min)

1. Open **Unity Hub**
2. Click **New Project**
3. Select **3D (URP)** template
4. Set location: `VIVE-SiteOwl-XR-Capture/UnityProject/`
5. Click **Create Project**

## Step 2: Switch to Android (2 min)

1. **File → Build Settings**
2. Select **Android**
3. Click **Switch Platform** (wait for reimport)

## Step 3: Install Packages (5 min)

**Window → Package Manager → Unity Registry:**

Install these packages:
- ☑ **XR Plugin Management**
- ☑ **OpenXR Plugin**
- ☑ **XR Hands** (optional but recommended)
- ☑ **TextMeshPro** (should already be there)

## Step 4: Configure OpenXR (5 min)

**Edit → Project Settings → XR Plug-in Management**

### Android Tab:
1. Check **OpenXR**
2. Click **OpenXR** (shows features list)
3. Add these Interaction Profiles:
   - ☑ HTC Vive Controller Profile
   - ☑ Hand Interaction Profile (optional)
4. Add these Features:
   - ☑ Hand Tracking (if using hands)
   - ☑ Passthrough (for camera)

## Step 5: Player Settings (3 min)

**Edit → Project Settings → Player (Android)**

**Other Settings:**
- Minimum API Level: **29** (Android 10.0)
- Target API Level: **33** (or higher)
- Scripting Backend: **IL2CPP**
- Target Architectures: **ARM64** only

**Publishing Settings:**
- Check **Custom Main Gradle Template**

## Step 6: Copy Project Files (2 min)

From this repo, copy to your Unity project:

```
Assets/Scripts/SimpleXrTester.cs → UnityProject/Assets/Scripts/
Assets/Scripts/XrInitializer.cs → UnityProject/Assets/Scripts/
Assets/Plugins/Android/AndroidManifest.xml → UnityProject/Assets/Plugins/Android/
```

## Step 7: Create Test Scene (5 min)

1. **File → New Scene**
2. Save as: `Assets/Scenes/XR_Test.unity`
3. Delete the **Main Camera** (we'll use XR camera)
4. **GameObject → XR → XR Origin (Action-based)**
5. Add **XrInitializer** script to XR Origin
6. Add **SimpleXrTester** script to XR Origin
7. Create UI Canvas:
   - Right-click → UI → Canvas
   - Set Render Mode: **World Space**
   - Position: (0, 1.5, 2) - 2 meters in front
   - Scale: (0.005, 0.005, 0.005)
8. Add UI elements:
   - Create 3 TextMeshPro texts:
     - "Status" (top)
     - "Position" (middle)
     - "Input" (bottom)
   - Wire them up to the SimpleXrTester script
9. Add visual pointer:
   - Create **LineRenderer** on XR Origin
   - Set positions: (0,0,0) and (0,0,10)
   - Wire to SimpleXrTester.pointerLine

## Step 8: Add Test Targets

1. Create 3 **Cubes** in the scene
2. Position them at different locations:
   - Cube 1: (3, 1, 5)
   - Cube 2: (-2, 1, 8)
   - Cube 3: (0, 2, 12)
3. Name them: "Target_A", "Target_B", "Target_C"
4. Wire them up to SimpleXrTester.testTargets

## Step 9: Connect Headset (3 min)

### Enable Developer Mode on VIVE XR Elite:
1. Put on headset
2. **Settings → System → Developer**
3. Enable **USB Debugging**
4. Connect USB-C to PC
5. Accept the "Allow debugging?" prompt on headset

### Verify connection:
```bash
# In command prompt:
adb devices

# Should show:
# <serial>    device
```

## Step 10: Build & Run (5 min)

1. **File → Build Settings**
2. Click **Add Open Scenes** (should show XR_Test)
3. Click **Build And Run**
4. Choose filename: `SiteOwl_XR_Test.apk`
5. Wait for build...

**The app will:**
- Build the APK
- Install on headset
- Launch automatically!

## Step 11: Test in VR

Put on the headset and you should see:

1. ✅ **Loading screen** ("Initializing XR...")
2. ✅ **3 cubes** in front of you
3. ✅ **UI panel** showing:
   - Status: "XR Ready"
   - Position: Your X/Y/Z coordinates
   - Input: Controller/button states
4. ✅ **Pointer line** extending from your view
5. ✅ When you look at a cube → it turns **green**
6. ✅ Press **Trigger** → Captures position (logs to console)

## 🎉 Success!

You now have a working XR app on VIVE XR Elite **without Steam!**

## Troubleshooting

### "XR not detected"
- Make sure headset is powered on and tracking
- Restart Unity after installing OpenXR
- Check XR Plug-in Management is set to OpenXR

### "Build fails"
- Make sure Android SDK is installed
- Check minimum API level matches headset
- Ensure ARM64 is selected (not ARMv7)

### "App won't install"
- Uninstall any previous versions
- Check USB debugging is enabled
- Try: `adb install -r <apk_path>`

### "No tracking"
- Check guardian/floor is set up on headset
- Ensure room lighting is sufficient
- Try resetting play area

## Next Steps

Now that XR works, start building the **real app**:

1. **CSV Loader** - Load your device list
2. **Calibration** - Set SiteOwl anchor point
3. **Device Selector** - Choose device to capture
4. **Photo Capture** - Take device photos
5. **Export** - Save updated CSV

Use the scripts in `Assets/Scripts/Core/` as your starting point!

## WiFi ADB (Optional)

After USB setup, you can develop wirelessly:

```bash
# Enable WiFi ADB
adb tcpip 5555

# Get headset IP (Settings → WiFi → Your Network → IP)
adb connect <HEADSET_IP>:5555

# Disconnect USB - now you're wireless!
adb devices  # Should show WiFi device
```

Build & Run will now work over WiFi!
