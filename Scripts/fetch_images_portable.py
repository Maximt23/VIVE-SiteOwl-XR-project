"""
fetch_images_portable.py
========================
Run this on any personal machine (no VPN, no proxy needed).

SETUP (one time):
  pip install duckduckgo-search requests

USAGE:
  1. Copy  camera_model_library.json  into the same folder as this script.
  2. Run:  python fetch_images_portable.py
  3. When done, copy the  camera_images/  folder back to:
       C:\\VIVE-SiteOwl-XR-Designs\\Meta data\\data\\camera_images\\
     And copy the updated  camera_model_library.json  back to:
       C:\\VIVE-SiteOwl-XR-Designs\\Meta data\\data\\

Images are saved here (relative to this script):
  camera_images/<Manufacturer>/<model_id>/image_0.jpg ...

The library JSON is updated live after each model (safe to Ctrl+C and resume).
"""

import json
import sys
import time
import urllib.request
import urllib.error
from pathlib import Path

# ── Config ────────────────────────────────────────────────────────────────────
HERE             = Path(__file__).parent
LIBRARY_FILE     = HERE / "camera_model_library.json"
IMAGE_ROOT       = HERE / "camera_images"
IMAGES_PER_MODEL = 4      # reference images per model
SLEEP_BETWEEN    = 2.0    # seconds between DuckDuckGo requests (be polite)
REQUEST_TIMEOUT  = 12     # seconds per image download
FORCE_REFETCH    = False  # set True to re-download already-fetched models


# ── DuckDuckGo image search ───────────────────────────────────────────────────

def _ddg_image_urls(query: str, want: int) -> list[str]:
    """Return up to want*3 candidate image URLs from DuckDuckGo."""
    try:
        from duckduckgo_search import DDGS  # noqa: PLC0415
    except ImportError:
        print("  [!] duckduckgo_search not installed. Run: pip install duckduckgo-search")
        return []

    urls: list[str] = []
    try:
        with DDGS() as ddgs:
            results = ddgs.images(
                query,
                region="us-en",
                safesearch="moderate",
                max_results=want * 4,
            )
            for r in (results or []):
                url = r.get("image", "")
                if url.startswith("http"):
                    urls.append(url)
                if len(urls) >= want * 3:
                    break
    except Exception as exc:
        print(f"  [!] DDG search error: {exc}")
    return urls


# ── Image download ────────────────────────────────────────────────────────────

def _download(url: str, dest: Path) -> bool:
    """Download url → dest. Returns True on success."""
    try:
        req = urllib.request.Request(
            url,
            headers={"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"},
        )
        with urllib.request.urlopen(req, timeout=REQUEST_TIMEOUT) as resp:
            data = resp.read()

        if len(data) < 4096:
            return False

        magic = data[:4]
        ok = (
            magic[:2] == b"\xff\xd8"            # JPEG
            or magic == b"\x89PNG"              # PNG
            or (magic == b"RIFF" and data[8:12] == b"WEBP")  # WebP
        )
        if not ok:
            return False

        dest.write_bytes(data)
        return True

    except Exception:
        return False


def _ext(url: str) -> str:
    low = url.lower().split("?")[0]
    if ".png"  in low: return ".png"
    if ".webp" in low: return ".webp"
    return ".jpg"


# ── Per-model fetch ───────────────────────────────────────────────────────────

def fetch_model(model: dict) -> list[str]:
    mid     = model["id"]
    mfr     = model["manufacturer"]
    queries = model.get("search_queries") or [model["model_name"] + " security camera product photo"]

    out_dir = IMAGE_ROOT / mfr / mid
    out_dir.mkdir(parents=True, exist_ok=True)

    candidate_urls: list[str] = []
    for q in queries:
        candidate_urls.extend(_ddg_image_urls(q, IMAGES_PER_MODEL))
        time.sleep(SLEEP_BETWEEN)

    # Deduplicate, preserve order
    seen: set[str] = set()
    unique = [u for u in candidate_urls if not (u in seen or seen.add(u))]  # type: ignore[func-returns-value]

    saved: list[str] = []
    for idx, url in enumerate(unique):
        if len(saved) >= IMAGES_PER_MODEL:
            break
        dest = out_dir / f"image_{len(saved)}{_ext(url)}"
        if _download(url, dest):
            saved.append(str(dest.relative_to(IMAGE_ROOT)))
            print(f"    [{len(saved)}/{IMAGES_PER_MODEL}] saved  {dest.name}")
        else:
            print(f"    [skip] {idx + 1}")

    return saved


# ── Library I/O ───────────────────────────────────────────────────────────────

def load_lib() -> dict:
    if not LIBRARY_FILE.exists():
        print(f"\n[ERROR] Library not found: {LIBRARY_FILE}")
        print("Copy camera_model_library.json into the same folder as this script.\n")
        sys.exit(1)
    with open(LIBRARY_FILE, encoding="utf-8") as f:
        return json.load(f)


def save_lib(lib: dict) -> None:
    with open(LIBRARY_FILE, "w", encoding="utf-8") as f:
        json.dump(lib, f, indent=2)


# ── Main ──────────────────────────────────────────────────────────────────────

def main() -> None:
    IMAGE_ROOT.mkdir(parents=True, exist_ok=True)

    lib    = load_lib()
    models = lib.get("models", [])
    todo   = [m for m in models if FORCE_REFETCH or not m.get("image_fetch_done")]

    print("=" * 58)
    print("  CAMERA IMAGE LIBRARY FETCHER  (portable / no proxy)")
    print("=" * 58)
    print(f"  Library : {LIBRARY_FILE.name}")
    print(f"  Output  : {IMAGE_ROOT}")
    print(f"  Models  : {len(models)} total  |  {len(todo)} to fetch")
    print("=" * 58)

    for i, model in enumerate(todo, 1):
        label = f"{model['manufacturer']} {model['model_name']}"
        print(f"\n[{i}/{len(todo)}] {label}")

        saved = fetch_model(model)
        model["local_images"]     = saved
        model["image_fetch_done"] = True
        print(f"    -> {len(saved)} image(s) saved")

        save_lib(lib)           # checkpoint — safe to Ctrl+C
        time.sleep(SLEEP_BETWEEN)

    done_n = sum(1 for m in models if m.get("image_fetch_done"))
    imgs_n = sum(len(m.get("local_images", [])) for m in models)

    print("\n" + "=" * 58)
    print(f"  DONE   {done_n}/{len(models)} models   {imgs_n} images total")
    print(f"  Copy camera_images/ and camera_model_library.json")
    print(f"  back to the Walmart machine.")
    print("=" * 58)


if __name__ == "__main__":
    main()
