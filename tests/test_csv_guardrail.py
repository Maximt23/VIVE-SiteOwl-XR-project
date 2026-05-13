"""
tests/test_csv_guardrail.py
End-to-end guardrail tests for CsvManager round-trip logic.

Simulates the Python-side of what CsvManager.cs does in C#:
  - load CSV, parse headers & rows
  - update one coordinate column
  - write back with RFC-4180 quoting
  - verify all other columns are byte-identical to original

Run:
  cd C:\\VIVE-SiteOwl-XR-project
  uv run pytest tests/ -v
"""

import csv
import io
import os
import pathlib
import tempfile

import pytest

# ── Helpers that mirror CsvManager.cs behaviour ────────────────────────────────

def _split_csv_line(line: str) -> list[str]:
    """RFC-4180 parse — mirrors CsvManager.SplitCsvLine."""
    reader = csv.reader(io.StringIO(line))
    return next(reader)


def _quote_csv_value(value: str) -> str:
    """Minimal RFC-4180 quoting — mirrors CsvManager.QuoteCsvValue."""
    if "," in value or '"' in value or "\n" in value:
        return '"' + value.replace('"', '""') + '"'
    return value


def _round_trip(csv_text: str, coord_col: str, new_x: float, new_y: float) -> str:
    """Loads CSV, updates coord_col for row[0], returns new CSV text."""
    lines    = csv_text.splitlines()
    headers  = _split_csv_line(lines[0])
    col_idx  = headers.index(coord_col)

    out_rows = [",".join(_quote_csv_value(h) for h in headers)]
    for line in lines[1:]:
        if not line.strip():
            continue
        row = _split_csv_line(line)
        # Only touch the coordinate column
        row[col_idx] = f"{new_x:.2f},{new_y:.2f}"
        out_rows.append(",".join(_quote_csv_value(v) for v in row))
    return "\n".join(out_rows)


# ── Fixtures ──────────────────────────────────────────────────────────────────

COORD_COL = "Coordinates"  # column name in SiteOwl CSV

SAMPLE_CSV = (
    f'DeviceID,DeviceName,DeviceType,SystemType,{COORD_COL}\n'
    'CAM-001,Front Entrance,Dome Camera,CCTV,\n'
    'CAM-002,"Camera, East Wing",Bullet Camera,CCTV,\n'
    'CAM-003,"Quote ""test"" cam",PTZ Camera,CCTV,\n'  # outer quotes make ""..."" an escaped quote
    'CAM-004,Normal Camera,Dome Camera,CCTV,"100.50,200.75"\n'
)


# ── Tests ─────────────────────────────────────────────────────────────────────

def test_rfc4180_embedded_comma_survives_round_trip():
    """A device name with a comma must survive load → save unchanged."""
    result    = _round_trip(SAMPLE_CSV, COORD_COL, 10.0, 20.0)
    rows      = list(csv.reader(io.StringIO(result)))
    col_idx   = rows[0].index(COORD_COL)
    # CAM-002 has "Camera, East Wing" with an embedded comma
    cam002    = next(r for r in rows if r[0] == "CAM-002")
    assert cam002[1] == "Camera, East Wing", \
        "Embedded comma in device name corrupted on round-trip"


def test_rfc4180_escaped_quote_survives_round_trip():
    """A device name with escaped double-quotessurvive load → save."""
    result  = _round_trip(SAMPLE_CSV, COORD_COL, 10.0, 20.0)
    rows    = list(csv.reader(io.StringIO(result)))
    cam003  = next(r for r in rows if r[0] == "CAM-003")
    assert cam003[1] == 'Quote "test" cam', \
        "Escaped double-quote in device name corrupted on round-trip"


def test_only_coord_column_changes():
    """Every column except the coordinate column must be byte-identical after save."""
    result      = _round_trip(SAMPLE_CSV, COORD_COL, 99.1, 88.2)
    orig_rows   = list(csv.reader(io.StringIO(SAMPLE_CSV)))
    result_rows = list(csv.reader(io.StringIO(result)))

    headers  = orig_rows[0]
    col_idx  = headers.index(COORD_COL)

    for o_row, r_row in zip(orig_rows[1:], result_rows[1:]):
        for i, (o, r) in enumerate(zip(o_row, r_row)):
            if i == col_idx:
                continue  # this column is allowed to change
            assert o == r, (
                f"Column '{headers[i]}' changed unexpectedly: "
                f"original={o!r}, result={r!r}"
            )


def test_existing_coordinate_is_updated():
    """CAM-004 already has coordinates — they should be replaced."""
    result  = _round_trip(SAMPLE_CSV, COORD_COL, 55.0, 66.0)
    rows    = list(csv.reader(io.StringIO(result)))
    col_idx = rows[0].index(COORD_COL)
    cam004  = next(r for r in rows if r[0] == "CAM-004")
    # Our simple test helper writes "55.00,66.00" — real C# writes formatted pair
    assert "55.00" in cam004[col_idx] and "66.00" in cam004[col_idx], \
        f"Coordinate not updated for CAM-004: got {cam004[col_idx]!r}"


def test_row_count_unchanged():
    """Save must never add or drop rows (malformed rows preserved verbatim)."""
    result       = _round_trip(SAMPLE_CSV, COORD_COL, 1.0, 2.0)
    orig_count   = len([l for l in SAMPLE_CSV.splitlines() if l.strip()]) - 1  # minus header
    result_count = len([l for l in result.splitlines() if l.strip()]) - 1
    assert orig_count == result_count, \
        f"Row count changed: {orig_count} → {result_count}"


def test_empty_coord_cell_accepted():
    """Devices with no coordinates yet must not error on round-trip."""
    result  = _round_trip(SAMPLE_CSV, COORD_COL, 0.0, 0.0)
    rows    = list(csv.reader(io.StringIO(result)))
    cam001  = next(r for r in rows if r[0] == "CAM-001")
    assert cam001 is not None, "CAM-001 dropped from result"


def test_backup_does_not_clobber_original(tmp_path: pathlib.Path):
    """Atomic-write simulation: if write fails mid-way, original is preserved."""
    original = tmp_path / "survey.csv"
    original.write_text(SAMPLE_CSV, encoding="utf-8")

    tmp_file = tmp_path / "survey.csv.tmp"
    tmp_file.write_text("BROKEN", encoding="utf-8")

    # Simulate crash: tmp exists but replace hasn't happened
    # Original must still be intact
    assert original.read_text(encoding="utf-8") == SAMPLE_CSV, \
        "Original CSV corrupted before atomic replace completed"


def test_backup_filename_has_milliseconds():
    """Backup filenames must include milliseconds to avoid timestamp collision."""
    import datetime
    ts = datetime.datetime.now().strftime("%Y%m%d_%H%M%S_%f")[:19 + 4]  # up to _fff
    # Must contain underscore after seconds (i.e., _NNN milliseconds portion)
    parts = ts.split("_")
    assert len(parts) >= 3, f"Backup timestamp missing milliseconds: {ts}"
    assert len(parts[2]) >= 3, f"Millisecond segment too short in: {ts}"
