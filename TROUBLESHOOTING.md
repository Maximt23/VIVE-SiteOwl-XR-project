# Troubleshooting Guide

## Common Issues & Solutions

### ❌ "SteamVR Required" Errors

**Problem:** Unity or device asks for SteamVR  
**Solution:** You don't need Steam! Check these:

1. **XR Plug-in Management:**
   - Make sure ONLY OpenXR is checked
   - Uncheck any SteamVR or OpenVR plugins

2. **OpenXR Features:**
   - Use HTC Vive Controller Profile (not SteamVR)
   - Use VIVE OpenXR Plugin (install from VIVE website)

### ❌ "XR Not Working" / Black Screen

**Problem:** App runs but no VR display  
**Solution:**

1. Check headset is powered on and tracking
2. Verify in Project Settings → XR Plug-in Management:
   - OpenXR is enabled
   - Android tab has OpenXR checked
3. Make sure you're using an **XR Origin** in the scene (not Main Camera)
4. Check that **Initialize XR on Startup** is checked

### ❌ "Build Failed: Android SDK"

**Problem:** Build fails with SDK errors  
**Solution:**

```bash
# Check Android SDK path in Unity:
Edit → Preferences → External Tools → Android SDK

# Should point to valid SDK. If not:
# 1. Install Android Studio
# 2. Or use Unity Hub to install Android Build Support
```

### ❌ "App Won't Install on Headset"

**Problem:** APK builds but won't install  
**Solutions:**

1. **Check Developer Mode:**
   - Headset: Settings → System → Developer → USB Debugging = ON

2. **Force install:**
   ```bash
   adb install -r SiteOwl_XR_Capture.apk
   ```

3. **Uninstall previous version:**
   ```bash
   adb uninstall com.siteowl.xr.capture
   adb install SiteOwl_XR_Capture.apk
   ```

4. **Check signature:**
   - Make sure you're not mixing debug/release builds
   - Uninstall old version first

### ❌ "No Tracking / Drifting"

**Problem:** Headset not tracking or position drifts  
**Solutions:**

1. **Lighting:**
   - Ensure room has good lighting
   - Avoid direct sunlight or mirrors

2. **Guardian/Play Area:**
   - Set up play area on headset
   - Make sure floor is detected

3. **Reset tracking:**
   - Headset: Settings → Guardian → Reset

4. **Restart headset:**
   - Full power cycle often fixes tracking issues

### ❌ "Controllers Not Working"

**Problem:** Can't click or interact  
**Solutions:**

1. **Check Interaction Profile:**
   - Project Settings → XR Plug-in Management → OpenXR
   - Must have "HTC Vive Controller Profile" enabled

2. **Test with keyboard:**
   - Space bar = Trigger
   - This helps isolate if it's a controller or app issue

3. **Pair controllers:**
   - Make sure controllers are paired to headset
   - Try re-pairing if needed

### ❌ "Camera Photo Not Working"

**Problem:** Photo capture fails or returns black  
**Solutions:**

1. **Permissions:**
   - Make sure AndroidManifest.xml has CAMERA permission
   - First run will request permission - accept it

2. **Use Screenshot Fallback:**
   - The PhotoCapture script has screenshot fallback
   - Check logs to see which method is being used

3. **VIVE Specific:**
   - Some VIVE builds need special camera setup
   - Check VIVE SDK documentation for passthrough camera

### ❌ "WiFi ADB Won't Connect"

**Problem:** Can't connect wirelessly  
**Solutions:**

1. **Same network:**
   - PC and headset must be on same WiFi network

2. **Enable first via USB:**
   ```bash
   # Must do this via USB first time:
   adb tcpip 5555
   adb connect <HEADSET_IP>:5555
   ```

3. **Find correct IP:**
   - Headset: Settings → WiFi → [Your Network] → IP Address

4. **Firewall:**
   - Check PC firewall isn't blocking ADB port 5555

### ❌ "Build Takes Forever"

**Problem:** Build process very slow  
**Solutions:**

1. **IL2CPP first build:**
   - First IL2CPP build is always slow (5-10 min)
   - Subsequent builds are faster

2. **Build only when needed:**
   - Use Unity Remote for quick testing
   - Only build APK for final testing

3. **SSD:**
   - Put project on SSD if possible

### ❌ "App Runs on PC but Not on Headset"

**Problem:** Works in editor, fails on device  
**Solutions:**

1. **Check Android API level:**
   - Minimum 29 (Android 10)
   - Target 33+

2. **Check ARM64:**
   - Must be ARM64 (not ARMv7)
   - Player Settings → Target Architectures

3. **Check shaders:**
   - Some shaders don't work on mobile
   - Use URP/Lit or Mobile shaders

4. **Check textures:**
   - Make sure textures are compressed (ASTC)

## Debugging Tips

### View Logs from Headset

```bash
# Connect headset via USB or WiFi ADB
adb logcat -s Unity:D

# Filter for your app only:
adb logcat -s "SiteOwlXR:*"
```

### Test Without Headset

```bash
# Use Unity Remote app on Android phone
# Quick iteration without full builds
```

### Check XR Status at Runtime

Add this to any script:
```csharp
void Update() {
    Debug.Log($"XR Active: {XRSettings.isDeviceActive}");
    Debug.Log($"Device: {XRSettings.loadedDeviceName}");
}
```

## Still Stuck?

1. **Check Unity Console** for red errors
2. **Check adb logcat** for device errors
3. **Simplify:** Start with empty scene, add one thing at a time
4. **Verify Setup:** Follow QUICK_START.md step by step
5. **Restart Everything:** Unity, headset, PC

## Known VIVE XR Elite Quirks

- **Passthrough camera** has some latency - normal
- **Hand tracking** less reliable than controllers - expected
- **Battery drains fast** with tracking active - normal
- **Gets warm** during extended use - normal, take breaks

## Contact

If nothing works:
- Check Unity OpenXR forums
- Check VIVE developer documentation
- Review this repo's GitHub issues
