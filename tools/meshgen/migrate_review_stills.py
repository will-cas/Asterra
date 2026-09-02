#!/usr/bin/env python3
"""One-shot: rename old camera files, rebuild angle links, drop leftover capture trees."""
from __future__ import annotations

import shutil
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import art_review_layout as layout  # noqa: E402


def rename_old_cameras():
    if not layout.MODELS_DIR.exists():
        return
    for folder in layout.MODELS_DIR.iterdir():
        if not folder.is_dir():
            continue
        for old, new in layout.OLD_CAMERA_NAMES.items():
            src = folder / f"{old}.png"
            dest = folder / f"{new}.png"
            if src.exists() and not dest.exists():
                src.rename(dest)
            elif src.exists() and dest.exists():
                src.unlink()
            meta = folder / f"{old}.png.meta"
            if meta.exists():
                meta.unlink()


def drop_legacy_trees():
    for name in ("review", "roster"):
        path = layout.RENDER_DIR / name
        if path.exists():
            shutil.rmtree(path)
            print("removed", path)
    for name in ("hero", "gate", "up", "crown"):
        path = layout.ANGLES_DIR / name
        if path.exists():
            shutil.rmtree(path)
            print("removed", path)


def main():
    rename_old_cameras()
    drop_legacy_trees()
    n = layout.rebuild_angle_links()
    print("angle links", n)


if __name__ == "__main__":
    main()
