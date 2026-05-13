# 🗒️ PROJECT FILE ORGANIZATION GUIDE

## Where Your Files Are (Default Locations)

### Windows Default Unity Projects
```
C:\Users\[YOUR_NAME]\Documents\Unity Projects\
    or
C:\Users\[YOUR_NAME]\Documents\VIVE-SiteOwl-XR-Capture\
```

### Quick Way to Find It
1. Press **Windows Key + E** (opens File Explorer)
2. Click "Documents" on left
3. Look for:
   - "Unity Projects" folder
   - "VIVE-SiteOwl-XR" folder
   - Or search: press Ctrl+F, type "VIVE"

---

## 📁 RECOMMENDED FOLDER STRUCTURE

Here is the CLEAN, ORGANIZED structure for your project:

```
VIVE-SiteOwl-XR-Capture/                    ← MAIN PROJECT FOLDER
├── 📀 README.md                          ← Start here! Project overview
├── 📖 SETUP_FOR_BEGINNERS.md           ← Step-by-step setup guide
├── 🗒️ PROJECT_ORGANIZATION.md          ← This file!
├── 🎯 QUICK_START.md                   ← 30-minute quick start
└── 🖼️ UnityProject/                    ← UNITY PROJECT FOLDER
    ├── 📁 Assets/                        ← All Unity assets
    │   ├── 📁 Scripts/                   ← ALL C# CODE
    │   │   ├── 00_Models/               ← Data models (structs, classes)
    │   │   │   ├── DeviceRecord.cs
    │   │   │   ├── GpsReading.cs
    │   │   │   ├── CalibrationAnchor.cs
    │   │   │   ├── CaptureSession.cs
    │   │   │   ├── AiSuggestion.cs
    │   │   │   └── StorageProvider.cs
    │   │   ├── 01_Events/               ← Event system
    │   │   │   └── CctvSurveyEvents.cs
    │   │   ├── 02_Services/             ← Enterprise services
    │   │   │   ├── ServiceLocator.cs
    │   │   │   ├── ILogger.cs
    │   │   │   └── UnityLogger.cs
    │   │   ├── 03_Configuration/        ← Settings
    │   │   │   └── SurveyConfiguration.cs
    │   │   ├── 10_Managers/             ← Core managers (order by priority)
    │   │   │   ├── XrInitializer.cs       ← #1: XR startup
    │   │   │   ├── RealGpsManager.cs      ← #2: GPS
    │   │   │   ├── CalibrationManager.cs  ← #3: SiteOwl transform
    │   │   │   ├── SiteOwlCsvManager.cs   ← #4: CSV I/O
    │   │   │   └── PhotoCapture.cs        ← #5: Photos
    │   │   ├── 11_Uploaders/            ← Photo upload
    │   │   │   ├── IPhotoUploader.cs
    │   │   │   ├── RetryPolicy.cs
    │   │   │   ├── ExternalPhotoUploader.cs
    │   │   │   └── LocalNetworkUploader.cs
    │   │   ├── 20_AI/                   ← AI recognition
    │   │   │   ├── CctvCameraRecognizer.cs
    │   │   │   ├── AiCopilotUI.cs
    │   │   │   └── AiSuggestionItemUI.cs
    │   │   ├── 21_Controller/           ← Main orchestrator
    │   │   │   └── CctvCaptureController.cs
    │   │   ├── 30_UI/                   ← User interface
    │   │   │   ├── CameraListUI.cs
    │   │   │   ├── CameraListItem.cs
    │   │   │   └── CaptureProgressUI.cs
    │   │   ├── 40_Memory/              ← Memory system (advanced)
    │   │   │   └── SurveyMemoryManager.cs
    │   │   └── 99_Editor/               ← Editor tools
    │   │       ├── BuildWorkingScene.cs
    │   │       ├── CreateCctvSurveyScene.cs
    │   │       └── SceneSetupHelper.cs
    │   ├── 📁 Scenes/                    ← Unity scenes
    │   │   ├── CctvSurvey_Working.unity   ← YOUR MAIN SCENE
    │   │   └── CctvSurvey_Working.unity.meta
    │   ├── 📁 StreamingAssets/           ← Data files
    │   │   └── cameras.csv              ← Camera list from work
    │   ├── 📁 Resources/                 ← Runtime resources
    │   │   └── SurveyConfiguration.asset
    │   ├── 📁 Plugins/                   ← Android plugins
    │   │   └── Android/
    │   │       └── AndroidManifest.xml
    │   └── 📁 Prefabs/                   ← Reusable objects
    │       ├── XR_Cctv_Rig.prefab
    │       └── Camera_List_Item.prefab
    ├── 📁 Packages/                    ← Unity packages
    ├── 📁 ProjectSettings/             ← Unity settings
    └── 📁 Builds/                      ← APK output
        └── CCTV_Survey_XR.apk

📁 Data/                          ← Project data (outside Unity)
├── Input/                         ← Input files
│   └── cameras.csv
├── Output/                        ← Output files
│   └── cameras_captured.csv
├── Backups/                       ← Automatic backups
├── Tasks/                         ← Calibration tasks
└── Photos/                        ← Captured photos

📁 FlatBuffersSchema/             ← Binary data schemas
└── survey_data.fbs

📁 QGIS/                          ← GIS integration
├── import_siteowl_csv.py
├── validate_survey.py
└── export_formats.py

📁 Documentation/                 ← All docs
├── CCTV_FOCUS_GUIDE.md
├── SITEOWL_INTEGRATION_GUIDE.md
├── AI_RECOGNITION_GUIDE.md
├── REAL_GPS_SETUP.md
├── QUICK_START.md
└── TROUBLESHOOTING.md

📁 Scripts/                       ← Helper scripts
├── AutoSetup.bat                  ← Automated setup
├── AutoSetup.sh                   ← Mac/Linux version
├── Build_And_Deploy.bat           ← One-click build
└── Pull_Latest_Data.bat           ← Get work's CSV

📁 .git/                          ← Git repository
├── .gitignore
└── [git internals]
```

---

## 💾 HOW TO FIND YOUR FILES NOW

### If You Cloned with Git:
```
1. Open File Explorer (Windows Key + E)
2. Click "Documents" on left
3. Look for: "VIVE-SiteOwl-XR-Capture"
4. Double-click it!
```

### If You Downloaded ZIP:
```
1. Open Downloads folder
2. Look for: "VIVE-SiteOwl-XR-Capture.zip"
3. Right-click → Extract All
4. Choose: Documents\
5. Click Extract
```

### If You Can't Find It:
```
1. Press Windows Key
2. Type: "File Explorer"
3. Press Enter
4. Click search box (top right)
5. Type: "CctvSurveyEvents.cs"
6. Wait for search results
7. Right-click the file → Open file location
```

---

## 🎯 FILE NAMING CONVENTIONS (Used)

| Prefix | Meaning | Example |
|--------|---------|---------|
| `00_` | Data models | 00_Models/ |
| `01_` | Events | 01_Events/ |
| `02_` | Services | 02_Services/ |
| `03_` | Config | 03_Configuration/ |
| `10_` | Core managers | 10_Managers/ |
| `11_` | Uploaders | 11_Uploaders/ |
| `20_` | AI | 20_AI/ |
| `21_` | Controller | 21_Controller/ |
| `30_` | UI | 30_UI/ |
| `40_` | Memory | 40_Memory/ |
| `99_` | Editor tools | 99_Editor/ |

**Why numbers?** Sorts correctly in file explorers!

---

## 🐳 GIT REPOSITORY STRUCTURE

```
Repository: https://github.com/Maximt23/VIVE-SiteOwl-XR-project

Branches:
├── main (stable, working code)    ← USE THIS
├── develop (in-progress)         ← For collaborators
└── feature/xyz (new features)      ← Temporary

Your collaborator pushes CSV to:
└── Data/Input/cameras.csv

You pull and get:
└── Data/Output/cameras_captured.csv
```

---

## 📋 QUICK FILE REFERENCE

### Most Important Files:

| File | Purpose | Where |
|------|---------|-------|
| `README.md` | Project overview | Root folder |
| `SETUP_FOR_BEGINNERS.md` | Your setup guide | Root folder |
| `CctvCaptureController.cs` | THE BRAIN | Scripts/21_Controller/ |
| `CctvSurveyEvents.cs` | Event system | Scripts/01_Events/ |
| `cameras.csv` | Your camera list | Data/Input/ or StreamingAssets/ |
| `CctvSurvey_Working.unity` | Main scene | Assets/Scenes/ |

### To Build the App:
```
1. Open: BuildWorkingScene.cs
2. In Unity: Tools → CCTV Survey → Build Working Scene
3. Output: Builds/CCTV_Survey_XR.apk
```

### To Update from Work:
```
1. Work pushes: Data/Input/cameras.csv
2. You run: Pull_Latest_Data.bat
3. File appears in your Unity project!
```

---

## 🌐 SYNCING WITH WORK (GitHub)

### Your Workflow:
```
[Work PC]                    [GitHub]                    [Your Home PC]
    |                            |                            |
    |-- push cameras.csv -------->|                            |
    |                            |                            |
    |                            |<-- pull latest ------------|
    |                            |                            |
    |                            |                            |-- Build APK
    |                            |                            |-- Deploy to headset
    |                            |                            |
    |                            |<-- push captured.csv -------|
    |                            |                            |
    |-- pull captured.csv --------|                            |
    |                            |                            |
```

### Commands You'll Use:
```bash
# Get latest from work
git pull origin main

# Push your captured data back
git add Data/Output/cameras_captured.csv
git commit -m "Site 2996 survey complete"
git push origin main
```

---

## 🚨 IF FILES ARE MISSING

### Regenerate Scene:
```
In Unity:
Tools → CCTV Survey → Build Working Scene
(This recreates any missing files)
```

### Restore from Git:
```
git checkout -- .
(Restores all files to last commit)
```

### Clean Rebuild:
```
1. Delete UnityProject folder (keep Data/)
2. git checkout UnityProject/
3. Reopen in Unity
4. Rebuild scene
```

---

## 📋 CHECKLIST: Is Everything There?

- [ ] UnityProject/ folder exists
- [ ] Assets/Scripts/ folder exists
- [ ] At least 20 .cs files in Scripts/
- [ ] CctvSurveyEvents.cs exists
- [ ] CctvCaptureController.cs exists
- [ ] README.md in root
- [ ] Data/Input/ folder (for CSV)
- [ ] GitHub repo synced

**If all checked: You're ready to build!**

---

## 🚀 NEXT STEPS

1. **Find your folder** using steps above
2. **Verify files exist** (check checklist)
3. **Open Unity** and load project
4. **Run**: Tools → Build Working Scene
5. **Deploy** to VIVE XR Elite!

**Questions? Tell me which step you're on!** 🚀
