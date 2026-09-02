"""Shared review-still layout. Canonical files live in models/<id>/<camera>.png."""
from __future__ import annotations

import os
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
RENDER_DIR = ROOT / "Assets/Asterra/Shared/Art/Blender/Renders"
MODELS_DIR = RENDER_DIR / "models"
ANGLES_DIR = RENDER_DIR / "angles"
ARCHIVE_DIR = RENDER_DIR / "archive"

CAMERAS = (
    "front",
    "three-quarter",
    "side",
    "rear",
    "low",
    "detail",
    "high",
    "top",
)

OLD_CAMERA_NAMES = {
    "hero": "three-quarter",
    "gate": "front",
    "up": "low",
    "crown": "detail",
}

NOTES_PLACEHOLDER = "_(Add defects and decisions here after each review pass.)_"
STATUSES = ("missing-stills", "captured", "iterate", "done")


def captured(def_id: str) -> bool:
    folder = MODELS_DIR / def_id
    return all((folder / f"{name}.png").exists() for name in CAMERAS)


def missing_cameras(def_id: str) -> list[str]:
    folder = MODELS_DIR / def_id
    return [name for name in CAMERAS if not (folder / f"{name}.png").exists()]


def publish_angle_link(model_png: Path, def_id: str, camera: str) -> None:
    angle_png = ANGLES_DIR / camera / f"{def_id}.png"
    angle_png.parent.mkdir(parents=True, exist_ok=True)
    if angle_png.exists() or angle_png.is_symlink():
        angle_png.unlink()
    try:
        os.link(model_png, angle_png)
    except OSError:
        shutil.copy2(model_png, angle_png)


def rebuild_angle_links() -> int:
    if ANGLES_DIR.exists():
        shutil.rmtree(ANGLES_DIR)
    count = 0
    if not MODELS_DIR.exists():
        return 0
    for folder in sorted(MODELS_DIR.iterdir()):
        if not folder.is_dir():
            continue
        for camera in CAMERAS:
            src = folder / f"{camera}.png"
            if src.exists():
                publish_angle_link(src, folder.name, camera)
                count += 1
    return count
