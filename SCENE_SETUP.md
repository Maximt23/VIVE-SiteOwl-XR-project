# 🎯 Scene Setup — 10-Minute Guide
> Do this ONCE in Unity Editor. SceneBootstrapper auto-wires everything else.

## Pre-check
- [ ] Unity 2022.3 LTS open on `UnityProject/`
- [ ] Platform set to Android (File → Build Settings → Android → Switch)
- [ ] OpenXR + VIVE plugin installed (Window → Package Manager)

---

## Step 1 — Create the Scene (2 min)
1. **File → New Scene** → choose "Basic (URP)" or empty
2. **File → Save As** → `Assets/Scenes/MainScene.unity`

---

## Step 2 — Create GameObjects (5 min)

Right-click Hierarchy → **Create Empty** for each row below.
Then select it → **Add Component** → type the script name.

| GameObject Name | Scripts to Add Component |
|---|---|
| **`XR Rig`** | `SceneBootstrapper`, `CaptureController`, `CalibrationManager`, `CsvManager`, `PhotoCapture`, `GitAutoPush`, `RealGpsManager`, `PermissionsManager` |
| **`AI`** | `DeviceRecognizer`, `DeviceLibraryLoader`, `ExternalDataConnector`, `AiCopilotUI` |
| **`QA`** | `QualityAssurance`, `MetadataManager` |
| **`XR Targeting`** | `XrTargeting`, **`Line Renderer`** (built-in) |

> **XR Origin**: Use your VIVE OpenXR prefab or add  
> `GameObject → XR → XR Origin (Mobile AR)` and rename to "XR Rig".  
> `Camera.main` is auto-detected by SceneBootstrapper.

---

## Step 3 — Build the UI Canvas (2 min)

1. **GameObject → UI → Canvas** — set Render Mode to **World Space**
2. Add Component: **`CaptureUI`**
3. In the Canvas, add these **child UI objects** (right-click → UI):

| Child Object | Type |
|---|---|
| `CalibrationPanel` | Panel |
| `DeviceListPanel` | Panel |
| `CapturePanel` | Panel |
| `StatusPanel` | Panel |

> Wire the panel GameObjects to CaptureUI's `CalibrationPanel`, `DeviceListPanel`, etc. Inspector slots.  
> Inside each panel, add TextMeshPro + Button children as needed — see field names in `CaptureUI.cs` header.

---

## Step 4 — Prefabs (1 min)

1. Create a simple **DeviceListItem prefab**:
   - GameObject → UI → Button → add child TextMeshPro for name, type, status
   - Add Component: `DeviceListItemUI`
   - Drag to `Assets/Prefabs/` → delete from scene
   - Assign to `CaptureUI.DeviceListItemPrefab` in Inspector

2. Create a **Reticle prefab**:
   - GameObject → 3D → Sphere → scale to `0.05`
   - Remove collider, assign a green material
   - Drag to `Assets/Prefabs/` → delete from scene
   - Assign to `XrTargeting.ReticlePrefab` in Inspector

---

## Step 5 — Build & Deploy

### Quick build (editor):
- **Build → Build and Run Android** (menu added by BuildScript)

### Command-line:
```bat
cd C:\VIVE-SiteOwl-XR-project
BUILD.bat
```

### Deploy to headset:
```bat
adb install -r Builds\SiteOwl_XR_Capture.apk
```

---

## Step 6 — Transfer survey CSV

```bat
TRANSFER_DATA.bat push "C:\path\to\Store_1234_CCTV.csv"
```

App auto-loads the most recently modified CSV on startup.

---

## Step 7 — Pull data after survey

```bat
TRANSFER_DATA.bat pull
```

Saves to `CapturedData_YYYYMMDD\` in this folder.

---

## ✅ Done. What SceneBootstrapper handles automatically:
- All cross-component wiring (no Inspector dragging beyond the 4 panel refs)
- CSV auto-load from persistentDataPath
- QA `ValidateCapture` wired to `OnCaptureComplete`
- Photo output path (external designs folder if present, else app sandbox)
- ADB path guidance logged to console if no CSV found
