# CSV Format — SiteOwl CCTV Survey Export

## Source Files

SiteOwl exports one CSV per store, e.g. `Store_8225_CCTV.csv`.
These files are **56 columns wide** and originate from SiteOwl's survey tool.

---

## Column Map (56 total)

| Index | Header                  | Notes                              |
|------:|-------------------------|------------------------------------|
|     0 | Project ID              | Read-only                          |
|     1 | Plan ID                 | Read-only                          |
|     2 | Primary Device/Task ID  | Read-only                          |
|     3 | Primary Device/Task Name| Read-only                          |
|     4 | **Device ID**           | Mapped → `DeviceData.DeviceID`     |
|     5 | **Name**                | Mapped → `DeviceData.DeviceName`   |
|     6 | Abbreviated Names       | Read-only                          |
|     7 | Device / Task           | Read-only                          |
|     8 | **System Type**         | Mapped → `DeviceData.SystemType`   |
|     9 | **Device/Task Type**    | Mapped → `DeviceData.DeviceType`   |
|    10 | **Part Number**         | Mapped → `DeviceData.Description`  |
|    11 | Manufacturer            | Read-only                          |
|  …-52 | (various)               | Read-only — never touched          |
|    **53** | **Coordinates**     | ⚠️ **ONLY COLUMN WE WRITE**       |
|    54 | Archived                | Read-only                          |
|    55 | Shareable Link          | Read-only                          |

---

## Coordinates Column (Index 53)

**Format:** `(X.XX, Y.YY)` — quoted in CSV because of the comma.

**Placeholder value** (unsurveyed device): `(10.00, 30.00)`

CsvManager treats any coordinate that exactly equals `(10.00, 30.00)` as
**not yet captured** → `DeviceData.NeedsCapture = true`.

After XR capture, the value is replaced with the real surveyed position,
e.g. `(45.32, 118.76)`.

---

## What CsvManager Does

```
Load:  Read ALL 56 columns → parse only cols 4,5,8,9,10,53
       Preserve raw string[] for every other field

Save:  Clone each raw row
       Replace raw row[53] with new coordinate string
       Write ALL 56 columns back verbatim
       → Nothing else ever changes
```

Backup is created before every save in a `Backups/` sub-folder
beside the source file, named:

```
Store_8225_CCTV_backup_20250512_143022.csv
```

---

## Guard Rails

- `CsvManager.UpdateDevice()` only writes coordinates — no other field.
- Backup is **mandatory** — save aborts if backup fails.
- Column discovery is **dynamic** — the Coordinates column is found by
  header name, not hardcoded index. If SiteOwl ever adds columns before
  index 53, it still works.
- Unknown or missing fields produce a `LogError`, not a silent failure.
