# Architecture Overview

## System Design

```
┌─────────────────────────────────────────────────────────────┐
│                    VIVE XR Elite Headset                    │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │  Camera     │  │  Headset    │  │  Controllers│         │
│  │  Passthrough│  │  Tracking   │  │  / Hands    │         │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘         │
│         │                  │                 │              │
│         └──────────────────┼─────────────────┘              │
│                            ▼                                │
│              ┌─────────────────────────┐                     │
│              │     OpenXR Runtime    │                     │
│              │  (VIVE / Wave SDK)    │                     │
│              │   [No Steam VR!]        │                     │
│              └───────────┬─────────────┘                     │
│                          ▼                                  │
│              ┌─────────────────────────┐                     │
│              │    Unity XR App         │                     │
│              │                         │                     │
│  ┌───────────┤  ┌─────────────────┐   │                     │
│  │ CSV       │  │  Calibration    │   │                     │
│  │ Loader    ├──┤  Anchor System  │   │                     │
│  └───────────┤  └─────────────────┘   │                     │
│              │                         │                     │
│  ┌───────────┤  ┌─────────────────┐   │                     │
│  │ Device    │  │  Coordinate     │   │                     │
│  │ Manager   ├──┤  Transformer    │   │                     │
│  └───────────┤  └─────────────────┘   │                     │
│              │                         │                     │
│  ┌───────────┤  ┌─────────────────┐   │                     │
│  │ Photo     │  │  GPS Estimator  │   │                     │
│  │ Capture   ├──┤  (Secondary)    │   │                     │
│  └───────────┤  └─────────────────┘   │                     │
│              │                         │                     │
│  ┌───────────┤  ┌─────────────────┐   │                     │
│  │ CSV       │  │  QA Dashboard   │   │                     │
│  │ Writer    ├──┤  & Export       │   │                     │
│  └───────────┤  └─────────────────┘   │                     │
│              │                         │                     │
│              │  ┌─────────────────┐   │                     │
│              └──┤  Git Auto-Push  │───┘                     │
│                 │  (WiFi sync)    │                         │
│                 └─────────────────┘                         │
└─────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. Calibration Anchor System
- Captures known SiteOwl X/Y point
- Establishes world-space to SiteOwl-space transform
- Persists across sessions

### 2. Coordinate Transformer
- Converts headset position → SiteOwl X/Y
- Handles rotation/scale from calibration
- Computes confidence based on drift

### 3. GPS Estimator
- Optional: reads Android location services
- Marks as estimated/secondary
- Falls back to null if unavailable

### 4. CSV Manager
- Loads device database
- Tracks which devices need capture
- Writes updates safely (backup first)
- Never overwrites non-coordinate fields

### 5. Photo Capture
- Uses VIVE passthrough camera
- Saves to device-named folder
- Links to CSV row

## Data Flow

```
┌────────────┐     ┌────────────┐     ┌────────────┐
│   User     │────▶│  Raycast   │────▶│  Device    │
│  Aims at   │     │   Select   │     │  Selected  │
│  Device    │     │            │     │            │
└────────────┘     └────────────┘     └─────┬──────┘
                                              │
                                              ▼
┌────────────┐     ┌────────────┐     ┌────────────┐
│   CSV      │◀────│  Writer    │◀────│  Capture   │
│  Updated   │     │  (Safe)    │     │  Position  │
└────────────┘     └────────────┘     └─────┬──────┘
                                              │
                                              │
┌────────────┐     ┌────────────┐     ┌─────┴──────┐
│  Photo     │◀────│  Camera    │◀────│   Take     │
│  Saved     │     │  Capture   │     │   Photo    │
└────────────┘     └────────────┘     └────────────┘
```

## Guardrails

- Backup CSV before any write
- Read-only for non-coordinate fields
- Confirmation for coordinate overwrites
- Git commit on every successful capture
- Confidence always written
- Never fake GPS coordinates
