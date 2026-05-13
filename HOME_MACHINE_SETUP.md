# 🏠 Home Machine Setup — Build the APK Tonight

## What you need on your personal laptop
- Windows 10/11 (64-bit)
- 16 GB RAM recommended (8 GB minimum)
- 20 GB free disk space
- Internet connection

---

## Step 1 — Install Unity Hub (3 min)
Download from: https://unity.com/download  
Install it. Sign in with your Unity account (free personal license is fine).

---

## Step 2 — Install Unity 2022.3.20f1 LTS (15 min download)

In Unity Hub → **Installs** → **Install Editor**  
Search for `2022.3.20` → Install with these modules checked:
- ✅ **Android Build Support**
- ✅ **Android SDK & NDK Tools** (sub-checkbox under Android)
- ✅ **OpenJDK** (sub-checkbox under Android)

> If you don't see 2022.3.20f1 specifically, any **2022.3 LTS** works.

---

## Step 3 — Get the project (2 min)

**Option A — Git clone (recommended):**
```bash
git clone https://github.com/Maximt23/VIVE-SiteOwl-XR-project.git
cd VIVE-SiteOwl-XR-project
```

**Option B — Copy from USB:**  
Copy the entire `VIVE-SiteOwl-XR-project` folder to your laptop.

---

## Step 4 — Open in Unity Hub (30 sec + wait)

Unity Hub → **Projects** → **Add** → browse to:
```
VIVE-SiteOwl-XR-project\UnityProject
```
Click **Add Project**. Select **Unity 2022.3 LTS** when asked.  
Click **Open**.

⏳ Unity will compile ~30 scripts. **Wait for it to finish** (~2-3 min).  

When compilation finishes, check the **Console** (Window → General → Console).  
You should see:
```
[AutoSceneSetup] ✓ MainScene.unity created and saved!
[SceneBootstrapper] Scene wired. All components connected.
```

---

## Step 5 — Configure XR for Android (2 min)

In Unity:  
1. **Edit → Project Settings → XR Plug-in Management**  
2. Click the **Android** tab (robot icon)  
3. Check ✅ **OpenXR**  
4. Click the **⚠ warning icon** that appears → Fix all issues  
5. Under **OpenXR → Features** → enable **VIVE XR** if listed, otherwise enable **Hand Interaction Profile** + **Controller Profile**

---

## Step 6 — Build the APK (10 min)

**Option A — Menu:**
```
Build → Build Android APK
```
(This menu was added by BuildScript.cs)

**Option B — Command line:**
```bat
cd VIVE-SiteOwl-XR-project
BUILD.bat
```

APK saved to: `VIVE-SiteOwl-XR-project\Builds\SiteOwl_XR_Capture.apk`

---

## Step 7 — Transfer APK + CSV to headset

Connect VIVE XR Elite via USB cable.

```bat
# Install the app
adb install -r Builds\SiteOwl_XR_Capture.apk

# Push your survey CSV
TRANSFER_DATA.bat push "C:\path\to\Store_XXXX_CCTV.csv"
```

If adb isn't in PATH, it's at:
```
%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe
```

---

## Step 8 — Pull data after survey

```bat
TRANSFER_DATA.bat pull
```
Data saved to `CapturedData_YYYYMMDD\` in the repo folder.

---

## If anything fails

| Problem | Fix |
|---------|-----|
| Console errors on compile | Paste the error here and I'll fix it |
| OpenXR not detecting VIVE | Enable "Mock HMD Loader" for first test |
| `adb` not found | Install Android Studio or SDK Platform Tools |
| Build fails: "No Android module" | Unity Hub → Installs → ⚙ → Add Modules → Android |
| Scene is empty after open | Check Console for `[AutoSceneSetup]` errors |

---

## Bring the APK back to work

Copy `Builds\SiteOwl_XR_Capture.apk` to a USB drive or OneDrive.  
At work, install via:
```bat
adb install -r SiteOwl_XR_Capture.apk
```
