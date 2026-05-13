"""
build_device_library.py
Scrapes every Store_*_CCTV.csv and builds a structured device recognition
library: Data/device_recognition_library.json
"""

import csv
import json
import os
import sys
from collections import defaultdict

CSV_DIR  = (
    r"C:\Users\vn59j7j\OneDrive - Walmart Inc"
    r"\Master Excel Pathing\CCTV STORES DATA - Survey"
)
# ── Output now lives in the shared Designs folder ─────────────────────────────
OUT_DIR  = r"C:\VIVE-SiteOwl-XR-Designs\Meta data\data"
OUT_FILE = os.path.join(OUT_DIR, "device_recognition_library.json")

# Prefix → human-readable location zone
ZONE_MAP = {
    "POS":   "Point of Sale / Checkout",
    "SAL":   "Sales Floor",
    "EXT":   "Exterior / Parking",
    "ENT":   "Entrance",
    "EXIT":  "Exit",
    "STK":   "Stockroom / Backroom",
    "RX":    "Pharmacy",
    "OFF":   "Office",
    "FRNT":  "Front End",
    "AP":    "Asset Protection",
    "TBC":   "Tire Battery Center",
    "FUEL":  "Fuel Station",
    "EMGX":  "Emergency Exit",
    "HCL":   "Health Clinic",
    "FIRE":  "Fire",
    "ELEV":  "Elevator",
    "ATM":   "ATM",
}

def zone_for(name):
    prefix = name.split("_")[0].upper() if "_" in name else name.upper()
    return ZONE_MAP.get(prefix, "Other"), prefix


def main():
    os.makedirs(OUT_DIR, exist_ok=True)

    # name → aggregated data across all stores
    names     = defaultdict(lambda: {
        "zone": "", "prefix": "", "stores": [],
        "part_numbers": set(), "manufacturers": set(),
        "connection_types": set(), "device_types": set(),
    })

    # part_number → model info
    models    = defaultdict(lambda: {
        "manufacturers": set(), "connection_types": set(),
        "seen_count": 0,
    })

    prefix_counts = defaultdict(int)
    store_count   = 0
    total_rows    = 0

    csv_files = sorted(f for f in os.listdir(CSV_DIR) if f.endswith(".csv"))

    for fname in csv_files:
        store_id = fname.replace("Store_", "").replace("_CCTV.csv", "")
        store_count += 1
        fpath = os.path.join(CSV_DIR, fname)

        with open(fpath, "r", encoding="utf-8-sig", newline="") as f:
            reader = csv.DictReader(f)
            for row in reader:
                name = (row.get("Name") or "").strip()
                if not name:
                    continue
                total_rows += 1

                pn  = (row.get("Part Number")   or "").strip()
                mf  = (row.get("Manufacturer")  or "").strip()
                ip  = (row.get("IP / Analog")   or "").strip()
                dt  = (row.get("Device/Task Type") or "").strip()

                zone, prefix = zone_for(name)
                prefix_counts[prefix] += 1

                d = names[name]
                d["zone"]   = zone
                d["prefix"] = prefix
                d["stores"].append(store_id)
                if pn: d["part_numbers"].add(pn)
                if mf: d["manufacturers"].add(mf)
                if ip: d["connection_types"].add(ip)
                if dt: d["device_types"].add(dt)

                if pn:
                    m = models[pn]
                    if mf: m["manufacturers"].add(mf)
                    if ip: m["connection_types"].add(ip)
                    m["seen_count"] += 1

    # ── Serialize (sets → sorted lists) ──────────────────────────────────────
    def clean(d):
        return {k: (sorted(v) if isinstance(v, set) else v)
                for k, v in d.items()}

    unique_names_list = sorted(names.keys())

    # Build prefix summary
    prefix_summary = {}
    for prefix, zone in ZONE_MAP.items():
        count = prefix_counts.get(prefix, 0)
        if count:
            prefix_summary[prefix] = {"zone": zone, "total_occurrences": count}

    # Add any prefixes found in data but not in ZONE_MAP
    for prefix, count in sorted(prefix_counts.items()):
        if prefix not in prefix_summary:
            prefix_summary[prefix] = {"zone": "Other", "total_occurrences": count}

    output = {
        "meta": {
            "generated_by":   "build_device_library.py",
            "stores_scanned": store_count,
            "total_rows":     total_rows,
            "unique_names":   len(names),
            "unique_prefixes":len(prefix_counts),
            "unique_models":  len(models),
        },
        "zone_prefix_map": ZONE_MAP,
        "prefix_summary":  prefix_summary,
        "device_names":    {n: clean(names[n]) for n in unique_names_list},
        "camera_models":   {p: clean(models[p]) for p in sorted(models)},
    }

    with open(OUT_FILE, "w", encoding="utf-8") as f:
        json.dump(output, f, indent=2)

    # ── Console report ────────────────────────────────────────────────────────
    sys.stdout.write("=" * 55 + "\n")
    sys.stdout.write("  DEVICE RECOGNITION LIBRARY BUILD REPORT\n")
    sys.stdout.write("=" * 55 + "\n")
    sys.stdout.write(f"  Stores scanned : {store_count}\n")
    sys.stdout.write(f"  Total devices  : {total_rows}\n")
    sys.stdout.write(f"  Unique names   : {len(names)}\n")
    sys.stdout.write(f"  Unique prefixes: {len(prefix_counts)}\n")
    sys.stdout.write(f"  Camera models  : {len(models)}\n")
    sys.stdout.write("-" * 55 + "\n")
    sys.stdout.write("  PREFIX BREAKDOWN (by occurrence):\n")
    for p, c in sorted(prefix_counts.items(), key=lambda x: -x[1]):
        zone = ZONE_MAP.get(p, "Other")
        sys.stdout.write(f"    {p:<10}  {c:>5}  {zone}\n")
    sys.stdout.write("-" * 55 + "\n")
    sys.stdout.write(f"  Output → {OUT_FILE}\n")
    sys.stdout.write("=" * 55 + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
