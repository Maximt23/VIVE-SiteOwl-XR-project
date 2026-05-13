# Calibration Strategy

## Overview

Calibration establishes the transform between headset world space and SiteOwl X/Y coordinates.

## Method: Single Anchor Point

### Why This Approach?

- **Simple** - One known point is all we need
- **Robust** - Works without complex multi-point math
- **Fast** - Surveyor can calibrate in seconds
- **Accurate enough** - For 3m accuracy target

### How It Works

1. Surveyor stands at a known location (e.g., "Front Entrance")
2. Surveyor enters known SiteOwl X/Y coordinates
3. App captures current headset position as the anchor
4. All subsequent positions are calculated relative to this anchor

### Coordinate Transform

```
Headset Position (Unity World Space)
        |
        v
[Subtract Anchor Position]
        |
        v
[Apply Rotation Offset]
        |
        v
[Scale by Meters]
        |
        v
SiteOwl X/Y Coordinates
```

### Rotation Handling

The user enters facing direction as part of calibration:
- User stands at anchor point
- User faces known direction (usually North, toward positive Y)
- App records the offset between headset forward and SiteOwl Y+
- All future captures use this rotation offset

## Accuracy Considerations

### Sources of Error

1. **Initial anchor precision** - How well surveyor positions at known point
2. **Headset drift** - SLAM tracking error over time/distance
3. **User positioning** - How well user aims at actual device

### Error Budget (3m target)

| Source | Budget |
|--------|--------|
| Anchor placement | ±1m |
| Headset drift | ±1m |
| User aiming | ±1m |
| **Total** | **±3m** |

### Confidence Scoring

Based on distance from calibration anchor:

| Distance | Confidence | Action |
|----------|------------|--------|
| < 10m | HIGH | No review needed |
| 10-30m | MEDIUM | Flag for review if other issues |
| > 30m | LOW | REQUIRE REVIEW |

## Alternative: Multi-Anchor Calibration

For future enhancement:
- Capture 3+ known points
- Compute best-fit transform
- Redundancy reduces error
- More complex UI

## Guardrails

- Always warn user if accuracy degrades
- Suggest re-calibration after significant drift
- Record calibration timestamp and location
- Allow quick re-calibration at any time
