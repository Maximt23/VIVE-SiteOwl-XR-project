# Memory System Integration Guide

**Combining FlatBuffers + MemoryBear for intelligent survey data management.**

## Architecture Overview

```
├───────────────────────────────────────────────────────┐
│                  MEMORY SYSTEM ARCHITECTURE                    │
└───────────────────────────────────────────────────────┘

    ┌──────────────────────────────────────────┐
    │         SURVEY CAPTURE (Unity XR)               │
    │                                                   │
    │  ┌───────────┐  ┌───────────┐  ┌───────────┐  │
    │  │   GPS    │  │  Photo  │  │  AI    │  │
    │  │ Capture  │  │ Capture │  │Recognize│  │
    │  └───┬─────┘  └───┬─────┘  └───┬─────┘  │
    │       │            │            │            │
    │       └──────────┼──────────┘            │
    │                     ▼                           │
    │       ┌────────────────────────────────────┐         │
    │       │  FLATBUFFERS SERIALIZATION           │         │
    │       │  Fast, compact, zero-copy           │         │
    │       │                                     │         │
    │       │  ┌──────────────────────────┐     │         │
    │       │  │ Camera Survey Schema (.fbs) │     │         │
    │       │  │   - camera_id               │     │         │
    │       │  │   - gps_coordinates         │     │         │
    │       │  │   - photo_data (bytes)      │     │         │
    │       │  │   - timestamp               │     │         │
    │       │  │   - confidence_score        │     │         │
    │       │  └──────────────────────────┘     │         │
    │       └────────────────────────────────────┘         │
    │                     ▼                           │
    │    ┌──────────────────────────────────────────┐            │
    │    │           MEMORY BEAR                     │            │
    │    │  (Contextual AI Memory System)            │            │
    │    │                                         │            │
    │    │  ┌───────────────────────────┐          │            │
    │    │  │  Survey Context Memory            │          │            │
    │    │  │                                     │          │            │
    │    │  │  ├─ Site Layout Memory              │          │            │
    │    │  │  ├─ Camera Relationships            │          │            │
    │    │  │  ├─ Surveyor Observations           │          │            │
    │    │  │  ├─ AI Pattern Recognition          │          │            │
    │    │  │  └─ Spatial Context                 │          │            │
    │    │  └───────────────────────────┘          │            │
    │    └──────────────────────────────────────────┘            │
    │                                                  │
    └────────────────────────────────────────────────────────┘

                         ▼
                         
    ┌──────────────────────────────────────────┐
    │         OUTPUT LAYERS                          │
    │                                                 │
    │  ├── CSV Export (SiteOwl import)                 │
    │  ├── FlatBuffer Binary (fast mobile sync)        │
    │  ├── AI Insights (from MemoryBear)               │
    │  └── Real-time Collaboration (sync)              │
    └──────────────────────────────────────────┘
```

## Why FlatBuffers?

**Traditional JSON/XML:**
```
Parse entire file → Load into memory → Access data
→ SLOW, memory-heavy
```

**FlatBuffers:**
```
Memory-map file → Access directly → Zero parsing
→ FAST, memory-efficient, works great on mobile/XR
```

**Use Cases:**
- 🚀 Fast camera list loading (1000+ cameras)
- 🚀 Zero-copy photo metadata access
- 🚀 Binary sync between headset and server
- 🚀 Embedded AI model data

## Why MemoryBear?

**Traditional Survey:**
```
Capture camera → Save data → No context carried forward
```

**MemoryBear-Enhanced:**
```
Capture camera → Save data → Remember patterns
                              ↓
         "Similar to CAM-003 you captured earlier"
         "This is near the loading dock zone"
         "Previous dome cameras here were Axis brand"
```

**Memory Types:**
1. **Spatial Memory** - Where cameras are relative to each other
2. **Temporal Memory** - Survey progression, what was captured when
3. **Pattern Memory** - AI learns typical camera placements
4. **Context Memory** - Site-specific knowledge ("all loading dock cameras are PTZ")

## Integration Architecture

### FlatBuffers Schema
```fbs
// survey_data.fbs
namespace CCTVSurvey;

struct GPSCoordinates {
  latitude:double;
  longitude:double;
  accuracy:float;
}

table CameraCapture {
  camera_id:string;
  device_name:string;
  camera_type:string;
  mount_type:string;
  gps:GPSCoordinates;
  siteowl_x:float;
  siteowl_y:float;
  photo_url:string;
  photo_hash:[ubyte];  // For integrity checking
  timestamp:string;
  capture_method:string;
  ai_confidence:float;
  ai_suggested_type:string;
  surveyor_notes:string;
  related_cameras:[string];  // MemoryBear: spatial relationships
  capture_context:string;    // MemoryBear: environmental context
}

table SurveySession {
  session_id:string;
  site_id:string;
  surveyor_id:string;
  start_time:string;
  end_time:string;
  cameras:[CameraCapture];
  site_context:SiteContext;  // MemoryBear site memory
}

table SiteContext {
  site_name:string;
  typical_camera_types:[string];
  known_patterns:string;    // MemoryBear learned patterns
  spatial_relationships:string;  // JSON of camera positions
  surveyor_observations:[string];
}

root_type SurveySession;
```

### C# FlatBuffers Code Generation

```bash
# Generate C# classes from schema
flatc --csharp survey_data.fbs

# Output: SurveyData.cs
# Use for serialization/deserialization
```

## MemoryBear Integration

### MemoryBear for Survey Context

```csharp
// Initialize MemoryBear with survey context
var memoryBear = new MemoryBearClient();

// Load site memory
await memoryBear.LoadContext("site_2996");

// During capture, query memories:
var similarCameras = await memoryBear.Query(
    "cameras near loading dock with dome type"
);

// AI uses context:
"CAM-003 is similar to CAM-001 you captured 5 minutes ago. 
 Both are Axis dome cameras in the reception area."
```

### Types of Survey Memory

```csharp
public enum SurveyMemoryType
{
    // SPATIAL: Where things are
    CameraPosition,      // GPS + SiteOwl X/Y
    CameraRelationships, // "CAM-001 is 10m from CAM-002"
    ZoneMapping,         // Reception, Loading Dock, etc.
    
    // TEMPORAL: When things happened
    CaptureSequence,     // Order of captures
    SurveyProgress,      // 15/23 cameras complete
    TimePatterns,        // "Morning survey more accurate"
    
    // SEMANTIC: What things are
    CameraTypes,         // Dome, Bullet, PTZ distribution
    BrandPatterns,       // "Site uses mostly Axis"
    MountingPatterns,    // "All loading dock = pole mount"
    
    // CONTEXT: Environmental
    LightingConditions,  // Indoor vs outdoor patterns
    WeatherConditions,   // If GPS accuracy affected
    SurveyorNotes        // "Hard to access, need ladder"
}
```

## Implementation Plan

### Phase 1: FlatBuffers Serialization
```csharp
// Fast binary data for mobile
1. Define schemas for Camera, Survey, Site
2. Generate C# classes
3. Replace JSON where performance matters
4. Sync binary between headset and server
```

### Phase 2: MemoryBear Context
```csharp
// AI memory for survey assistance
1. Integrate MemoryBear SDK
2. Store spatial relationships
3. Query similar cameras
4. Suggest based on patterns
```

### Phase 3: Smart Survey Assistance
```csharp
// AI uses memory to help surveyor
1. "This area typically has dome cameras"
2. "You're near CAM-005 you captured earlier"
3. "Based on pattern, expect PTZ at loading dock"
4. "GPS accuracy typically 2-3m in this zone"
```

## Benefits

### FlatBuffers Benefits:
- 🚀 **100x faster** than JSON parsing on mobile
- 🚀 **Zero memory allocation** during access
- 🚀 **Cross-platform** (Unity, server, web)
- 🚀 **Schema evolution** (add fields without breaking)
- 🚀 **Small binary size** for sync over slow connections

### MemoryBear Benefits:
- 🧠 **Contextual AI** that learns the site
- 🧠 **Spatial awareness** (where cameras are)
- 🧠 **Pattern recognition** (typical placements)
- 🧠 **Surveyor assistance** ("you missed one near here")
- 🧠 **Knowledge persistence** (site memory across surveys)

## Next Steps

1. **Add FlatBuffers schema** (survey_data.fbs)
2. **Generate C# classes** (flatc compiler)
3. **Integrate MemoryBear SDK** (NuGet/package)
4. **Build SurveyMemoryManager** (bridges both systems)
5. **Test binary serialization** (speed vs JSON)

Ready to build the memory system? 🧠
