# Fetch Camera Reference Images — Personal Machine Instructions

Run this on any personal computer (no VPN or Walmart proxy needed).

---

## What You Need to Copy Over

From the Walmart machine, grab these two files and put them in the **same folder** on your personal machine:

```
C:\VIVE-SiteOwl-XR-Designs\Meta data\data\camera_model_library.json
C:\VIVE-SiteOwl-XR-project\Scripts\fetch_images_portable.py
```

Example destination on personal machine: `Desktop\cam-fetch\`

---

## Run It

Open a terminal in that folder, then:

**Windows (Python already installed):**
```
pip install duckduckgo-search
python fetch_images_portable.py
```

**Mac / Linux:**
```
pip3 install duckduckgo-search
python3 fetch_images_portable.py
```

**No Python? Use uv:**
```
pip install uv
uv run --with duckduckgo-search python fetch_images_portable.py
```

Safe to Ctrl+C at any time — it saves progress after every model and skips already-fetched ones on re-run.

---

## Copy Results Back to Walmart Machine

| From personal machine | To Walmart machine |
|---|---|
| `camera_images/` | `C:\VIVE-SiteOwl-XR-Designs\Meta data\data\camera_images\` |
| `camera_model_library.json` | `C:\VIVE-SiteOwl-XR-Designs\Meta data\data\camera_model_library.json` |

---

*42 models | 9 manufacturers | ~168 images total*
