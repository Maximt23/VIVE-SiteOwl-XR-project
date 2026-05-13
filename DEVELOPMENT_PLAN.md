# Development Plan

## Project Name
VIVE SiteOwl XR Capture

## Objective
Build a VIVE XR application that allows a surveyor to capture missing SiteOwl X/Y, GPS estimate, and photo evidence for existing device records.

## Tech Stack
- Unity 2022 LTS or newer
- VIVE OpenXR
- XR Interaction Toolkit
- C#
- CSV read/write
- GitHub
- Power Automate optional
- SharePoint/OneDrive optional

## Important: No SteamVR Dependency

**This project must not use SteamVR.**

Reason:
- SteamVR access is not available in the work environment.
- SteamVR creates an unnecessary IT/access dependency.
- The target build should use VIVE OpenXR directly.

**Approved XR stack:**
- Unity
- OpenXR
- VIVE OpenXR Plugin
- XR Interaction Toolkit

**Blocked dependencies:**
- SteamVR Unity Plugin
- OpenVR-only packages
- Any workflow requiring Steam login/access

## System Modules

### 1. CSV Data Module
Responsibilities:
- Load SiteOwl device CSV
- Parse device records
- Track missing fields
- Write updated rows
- Create backup before saving

### 2. Task Calibration Module
Responsibilities:
- Load task file
- Read known SiteOwl X/Y
- Set calibration anchor
- Track headset position relative to anchor

### 3. XR Targeting Module
Responsibilities:
- Show pointer/raycast
- Capture target hit point
- Convert world point into SiteOwl X/Y
- Estimate confidence

### 4. GPS Estimate Module
Responsibilities:
- Use known anchor lat/long if available
- Convert local X/Y offset into approximate lat/long
- Mark GPS confidence

### 5. Photo Capture Module
Responsibilities:
- Capture device photo
- Save image locally
- Name image by site/device/timestamp
- Write photo path to CSV

### 6. AI Survey Copilot Module
Responsibilities:
- Show missing devices
- Show captured devices
- Show low-confidence captures
- Suggest possible device type from photo
- **Never auto-write AI guesses without confirmation**

### 7. QA Module
Responsibilities:
- Detect missing coordinates
- Detect missing photos
- Detect duplicate coordinates
- Detect low confidence
- Detect out-of-bound captures
- Mark review status

## MVP Acceptance Criteria

The MVP is complete when:
- App loads a device CSV
- App loads a calibration task
- User can set known X/Y anchor
- User can select a device
- User can aim and capture X/Y
- App can estimate lat/long
- App can take/save photo
- App updates only the matching CSV row
- App exports backup and updated CSV
- App commits changes to GitHub

## First Real Build Goal

**Do NOT start with AI.** Start with this exact order:

1. CSV Loader → 
2. Device Selector → 
3. XR Raycast → 
4. X/Y Capture → 
5. Photo Capture → 
6. CSV Update → 
7. QA Validation → 
8. AI Assist

If you ignore this order, the project turns into spaghetti garbage.

## MVP Demo Target

If you can demonstrate THIS:
1. Load CSV
2. Select device
3. Aim at object in XR
4. Capture X/Y
5. Take picture
6. Save updated CSV

**You already win.**

Do not try to build:
- Computer vision
- Automatic device recognition
- Perfect GPS
- Autonomous AI mapping

That is how overnight projects die.

## Real Git Workflow

Every major feature:
```bash
git checkout -b feature/xr-raycast
git add .
git commit -m "Add XR raycast targeting"
git push origin feature/xr-raycast
```
Then merge into main.

## What This Project ACTUALLY Is

Not: "VR camera mapper"

It is: "XR-assisted spatial survey acquisition integrated into the SiteOwl import workflow with QA validation and photo evidence capture."

That framing matters massively in enterprise environments.
