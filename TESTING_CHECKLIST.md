# Testing Checklist

Use this checklist to verify your XR app works correctly on VIVE XR Elite.

## Pre-Flight Checklist

### ☐ Hardware Ready
- [ ] VIVE XR Elite charged (>50% battery)
- [ ] USB-C cable available
- [ ] PC with Unity installed
- [ ] Headset powered on and tracking

### ☐ Software Ready
- [ ] Unity 2022.3 LTS installed
- [ ] Android Build Support installed
- [ ] Project cloned/opened
- [ ] OpenXR configured (see QUICK_START.md)

### ☐ Headset Configuration
- [ ] Developer Mode enabled (Settings → System → Developer)
- [ ] USB Debugging enabled
- [ ] Guardian/Play Area set up
- [ ] Controllers paired

---

## Build Test

### ☐ First Build
- [ ] Open Unity project
- [ ] Switch platform to Android (if not already)
- [ ] Build Settings → Add Open Scenes
- [ ] Click Build And Run
- [ ] Wait for build (5-10 minutes first time)
- [ ] APK installs on headset
- [ ] App launches automatically

### ☐ If Build Fails
- [ ] Check Unity Console for errors
- [ ] Verify Android SDK path
- [ ] Check API levels (min 29, target 33)
- [ ] Check ARM64 architecture selected
- [ ] Try clean build: File → Build Settings → Clean Build

---

## Runtime Tests

### ☐ XR Initialization Test
1. Put on headset
2. Launch app
3. [ ] See loading screen ("Initializing XR...")
4. [ ] Loading completes within 10 seconds
5. [ ] See "XR Ready" message
6. [ ] Tracking starts (head movement moves view)

### ☐ Visual Test
1. Look around
2. [ ] See 3 test cubes in the scene
3. [ ] Cubes are clearly visible
4. [ ] UI panel visible showing status/position/input
5. [ ] Pointer line extends from view

### ☐ Tracking Test
1. [ ] Move head left/right - view changes
2. [ ] Move head up/down - view changes
3. [ ] Walk around (if space permits) - position updates
4. [ ] Check UI: Position values change with movement

### ☐ Gaze/Selection Test
1. Look at different cubes
2. [ ] Cube turns green when looking at it
3. [ ] Status text shows cube name
4. [ ] Can select all 3 cubes by looking at them

### ☐ Input Test

#### Controller Method:
1. [ ] Point controller at cube
2. [ ] Pull trigger
3. [ ] See "TRIGGER CAPTURED" message
4. [ ] Position logged to console

#### Headset Button Method:
1. [ ] Look at cube
2. [ ] Press headset button (or Space on keyboard if testing via Unity Remote)
3. [ ] See "TRIGGER CAPTURED" message

### ☐ UI Test
1. [ ] Status text readable
2. [ ] Position text shows X/Y/Z values
3. [ ] Values update in real-time
4. [ ] Input text responds to button presses

---

## Performance Tests

### ☐ Frame Rate Test
- [ ] App runs smoothly (no stuttering)
- [ ] No frame drops when moving head
- [ ] Target: 72fps (VIVE XR Elite refresh rate)

### ☐ Latency Test
- [ ] Head movement feels responsive
- [ ] No noticeable delay between movement and display
- [ ] Pointer follows gaze without lag

### ☐ Stability Test
- [ ] App runs for 5 minutes without crash
- [ ] No overheating warnings
- [ ] Tracking remains stable over time

---

## Real-World Survey Test

### ☐ CSV Loading Test
1. [ ] Place devices.csv in StreamingAssets folder
2. [ ] App loads and displays device list
3. [ ] Can see device names/types
4. [ ] Can filter/search devices

### ☐ Calibration Test
1. [ ] Stand at known location
2. [ ] Enter known SiteOwl X/Y
3. [ ] Set calibration anchor
4. [ ] Walk to device location
5. [ ] Verify position makes sense

### ☐ Capture Test
1. [ ] Select device from list
2. [ ] Aim at physical device
3. [ ] Press trigger
4. [ ] Photo captured
5. [ ] X/Y coordinates recorded
6. [ ] CSV updated with capture data

### ☐ Export Test
1. [ ] Exit app
2. [ ] Check Android file system:
   ```bash
   adb shell
   cd /sdcard/Android/data/com.siteowl.xr.capture/files
   ls
   ```
3. [ ] Verify updated CSV exists
4. [ ] Verify photos saved in Photos folder
5. [ ] Pull files to PC:
   ```bash
   adb pull /sdcard/Android/data/com.siteowl.xr.capture/files/Output/ ./Data/Output/
   ```

---

## Git Sync Test (Optional)

### ☐ WiFi Git Push Test
1. [ ] Connect headset to WiFi
2. [ ] Enable WiFi ADB
3. [ ] Make a capture
4. [ ] App attempts Git push
5. [ ] Check GitHub - commit appears

---

## Pass Criteria

**Minimum Viable Test:**
- ☐ App builds successfully
- ☐ App installs on headset
- ☐ App launches and shows XR view
- ☐ Tracking works (head movement changes view)
- ☐ Can select objects with gaze
- ☐ Can trigger capture with controller

**Full System Test:**
- ☐ All minimum tests pass
- ☐ CSV loads correctly
- ☐ Calibration works
- ☐ Device capture works
- ☐ Photo saves correctly
- ☐ CSV updates correctly
- ☐ Export works

---

## If Tests Fail

1. Check TROUBLESHOOTING.md for specific error
2. Verify headset has latest firmware
3. Restart headset and try again
4. Check Unity Console for errors
5. Check `adb logcat -s Unity:D` for device errors
6. Simplify test (try empty scene first)

---

## Sign-Off

**Tested By:** _______________  
**Date:** _______________  
**Headset Firmware:** _______________  
**Unity Version:** _______________

**Results:**
- [ ] All tests passed
- [ ] Some tests failed (see notes)

**Notes:**

