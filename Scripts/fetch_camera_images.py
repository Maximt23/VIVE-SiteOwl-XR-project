"""
fetch_camera_images.py
======================
Downloads reference images for every camera model in camera_model_library.json.

Uses DuckDuckGo image search via requests + NTLM proxy auth (Walmart network).
Run this from a normal Windows session (double-click run_fetch_images.bat)
so Windows handles the NTLM proxy authentication automatically.

Output:
  C:\\VIVE-SiteOwl-XR-Designs\\Meta data\\data\\camera_images\\
      <Manufacturer>\\
          <model_id>\\
              image_0.jpg  ...

Updates camera_model_library.json with local_images paths after each model.
"""

import json
import os
import sys
import time
from pathlib import Path

# ── Paths ─────────────────────────────────────────────────────────────────────
DESIGNS_ROOT = Path(r"C:\VIVE-SiteOwl-XR-Designs\Meta data")
LIBRARY_FILE = DESIGNS_ROOT / "data" / "camera_model_library.json"
IMAGE_ROOT   = DESIGNS_ROOT / "data" / "camera_images"

# ── Config ────────────────────────────────────────────────────────────────────
IMAGES_PER_MODEL = 4        # reference images to download per model
SLEEP_BETWEEN    = 2.0      # polite pause between searches (seconds)
REQUEST_TIMEOUT  = 15       # image download timeout (seconds)
FORCE_REFETCH    = False    # True → re-download even if already fetched

# ── SSL ─────────────────────────────────────────────────────────────────────
# On Walmart network the proxy does SSL inspection and presents its own cert.
# Set WALMART_CA_BUNDLE (or REQUESTS_CA_BUNDLE) to the Walmart CA PEM path.
# Only if neither is set AND the env var SITEOWL_SSL_INSECURE=1 do we skip verify.
import os as _os
_WALMART_CA     = _os.environ.get("WALMART_CA_BUNDLE") or _os.environ.get("REQUESTS_CA_BUNDLE")
_FORCE_INSECURE = _os.environ.get("SITEOWL_SSL_INSECURE", "0") == "1"
SSL_VERIFY      = _WALMART_CA if _WALMART_CA else (False if _FORCE_INSECURE else True)
_VERIFY_INSECURE = SSL_VERIFY is False

# ── Walmart proxy (NTLM auth handled by OS when run from regular session) ────
PROXY_URL = "http://sysproxy.wal-mart.com:8080"
PROXIES   = {"http": PROXY_URL, "https": PROXY_URL}

# ── Lazy imports (checked at startup) ────────────────────────────────────────
_requests = None


def _get_requests():
    """Import requests once and cache it."""
    global _requests
    if _requests is None:
        import requests  # noqa: PLC0415
        _requests = requests
    return _requests


def _image_urls_from_ddg(query: str, max_results: int) -> list[str]:
    """
    Search DuckDuckGo images for query and return candidate image URLs.
    Uses the DDG images JSON API (no JS rendering needed).
    """
    requests = _get_requests()
    headers = {
        "User-Agent": (
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
            "AppleWebKit/537.36 (KHTML, like Gecko) "
            "Chrome/124.0.0.0 Safari/537.36"
        ),
        "Accept-Language": "en-US,en;q=0.9",
    }

    # Step 1: get vqd token
    try:
        r = requests.get(
            "https://duckduckgo.com/",
            params={"q": query},
            headers=headers,
            proxies=PROXIES,
            timeout=REQUEST_TIMEOUT,
            verify=SSL_VERIFY,
        )
        import re  # noqa: PLC0415
        vqd_match = re.search(r'vqd=(["\'])([^"\']+)\1', r.text)
        vqd = vqd_match.group(2) if vqd_match else ""
    except Exception as exc:
        print(f"    ⚠  DDG token fetch failed: {exc}")
        return []

    if not vqd:
        # Fallback: try the older vqd format
        try:
            import re  # noqa: PLC0415
            vqd_match = re.search(r'vqd=([\d-]+)', r.text)
            vqd = vqd_match.group(1) if vqd_match else ""
        except Exception:
            pass

    # Step 2: query images endpoint
    try:
        img_r = requests.get(
            "https://duckduckgo.com/i.js",
            params={
                "l": "us-en",
                "o": "json",
                "q": query,
                "vqd": vqd,
                "f": ",,,,,",
                "p": "1",
            },
            headers=headers,
            proxies=PROXIES,
            timeout=REQUEST_TIMEOUT,
            verify=SSL_VERIFY,
        )
        data = img_r.json()
        results = data.get("results", [])
        urls = [r.get("image", "") for r in results if r.get("image", "").startswith("http")]
        return urls[:max_results * 3]
    except Exception as exc:
        print(f"    ⚠  DDG images endpoint failed: {exc}")
        return []


def _download_image(url: str, dest: Path) -> bool:
    """Download url to dest; returns True on success."""
    requests = _get_requests()
    try:
        headers = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"}
        r = requests.get(
            url,
            headers=headers,
            proxies=PROXIES,
            timeout=REQUEST_TIMEOUT,
            stream=True,
            verify=SSL_VERIFY,
        )
        if r.status_code != 200:
            return False

        data = b"".join(r.iter_content(chunk_size=8192))
        if len(data) < 4096:
            return False

        magic = data[:4]
        is_jpg  = magic[:2] == b"\xff\xd8"
        is_png  = magic == b"\x89PNG"
        is_webp = data[:4] == b"RIFF" and data[8:12] == b"WEBP"
        if not (is_jpg or is_png or is_webp):
            return False

        dest.write_bytes(data)
        return True

    except Exception:
        return False


def _ext_for(url: str) -> str:
    lower = url.lower().split("?")[0]
    if ".png" in lower:
        return ".png"
    if ".webp" in lower:
        return ".webp"
    return ".jpg"


def check_connectivity() -> bool:
    """Return True if we can reach DuckDuckGo through the proxy."""
    requests = _get_requests()
    try:
        r = requests.get(
            "https://duckduckgo.com",
            proxies=PROXIES,
            timeout=10,
            verify=SSL_VERIFY,
        )
        return r.status_code < 500
    except Exception as exc:
        print(f"  Connectivity check failed: {exc}")
        return False


def fetch_images_for_model(model: dict, image_root: Path) -> list[str]:
    """Fetch and save reference images for one model. Returns relative path list."""
    mid   = model["id"]
    mfr   = model["manufacturer"]
    queries = model.get("search_queries") or [model["model_name"] + " security camera product photo"]

    out_dir = image_root / mfr / mid
    out_dir.mkdir(parents=True, exist_ok=True)

    candidate_urls: list[str] = []
    for query in queries:
        urls = _image_urls_from_ddg(query, max_results=IMAGES_PER_MODEL)
        candidate_urls.extend(urls)
        time.sleep(SLEEP_BETWEEN)

    # Deduplicate
    seen: set[str] = set()
    unique_urls = [u for u in candidate_urls if not (u in seen or seen.add(u))]  # type: ignore[func-returns-value]

    saved: list[str] = []
    for idx, url in enumerate(unique_urls):
        if len(saved) >= IMAGES_PER_MODEL:
            break
        ext  = _ext_for(url)
        dest = out_dir / f"image_{len(saved)}{ext}"
        if _download_image(url, dest):
            saved.append(str(dest.relative_to(image_root)))
            print(f"    OK [{len(saved)}/{IMAGES_PER_MODEL}] {dest.name}")
        else:
            print(f"    -- skip ({idx + 1})")

    return saved


def load_library(path: Path) -> dict:
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def save_library(library: dict, path: Path) -> None:
    with open(path, "w", encoding="utf-8") as f:
        json.dump(library, f, indent=2)


def main() -> None:
    # ── Dependency check ──────────────────────────────────────────────────────
    try:
        import requests  # noqa: F401
    except ImportError:
        print("ERROR: 'requests' not installed.")
        print("Run: uv pip install requests requests-ntlm")
        sys.exit(1)

    # Suppress SSL warnings ONLY when verification is explicitly disabled
    if _VERIFY_INSECURE:
        import urllib3  # noqa: PLC0415
        urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
        print("WARNING: SSL verification disabled. Set WALMART_CA_BUNDLE env var to fix.")

    IMAGE_ROOT.mkdir(parents=True, exist_ok=True)
    library = load_library(LIBRARY_FILE)
    models  = library.get("models", [])

    to_process = [m for m in models if FORCE_REFETCH or not m.get("image_fetch_done")]

    print("=" * 60)
    print("  CAMERA IMAGE LIBRARY FETCHER")
    print("=" * 60)
    print(f"  Library   : {LIBRARY_FILE}")
    print(f"  Images    : {IMAGE_ROOT}")
    print(f"  Proxy     : {PROXY_URL}")
    print(f"  Total     : {len(models)} models  |  To fetch: {len(to_process)}")
    print("=" * 60)

    # ── Connectivity gate ─────────────────────────────────────────────────────
    print("\nChecking proxy connectivity...")
    if not check_connectivity():
        print(
            "\n⚠  Cannot reach DuckDuckGo through Walmart proxy.\n"
            "   Run this script from a normal Windows CMD session\n"
            "   (double-click run_fetch_images.bat) so Windows\n"
            "   can handle NTLM proxy authentication automatically.\n"
        )
        sys.exit(2)
    print("  ✓ Proxy OK — starting downloads\n")

    # ── Main fetch loop ───────────────────────────────────────────────────────
    for i, model in enumerate(to_process, 1):
        name = f"{model['manufacturer']} {model['model_name']}"
        print(f"\n[{i}/{len(to_process)}] {name}")

        saved = fetch_images_for_model(model, IMAGE_ROOT)
        model["local_images"]     = saved
        model["image_fetch_done"] = True
        print(f"    → {len(saved)} image(s) saved")

        # Checkpoint after each model
        save_library(library, LIBRARY_FILE)
        time.sleep(SLEEP_BETWEEN)

    # ── Summary ───────────────────────────────────────────────────────────────
    done_total  = sum(1 for m in models if m.get("image_fetch_done"))
    img_total   = sum(len(m.get("local_images", [])) for m in models)
    print("\n" + "=" * 60)
    print("  FETCH COMPLETE")
    print(f"  Models fetched : {done_total}/{len(models)}")
    print(f"  Images saved   : {img_total}")
    print(f"  Library        : {LIBRARY_FILE}")
    print("=" * 60)


if __name__ == "__main__":
    main()
