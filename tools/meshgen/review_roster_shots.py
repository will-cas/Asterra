#!/usr/bin/env python3
"""Open AsterraArt.blend and render a 4-angle contact sheet per roster mesh.

Usage:
  /Applications/Blender.app/Contents/MacOS/Blender --background --python tools/meshgen/review_roster_shots.py
"""
from __future__ import annotations

import math
import sys
from pathlib import Path

try:
    import bpy
    from mathutils import Vector
except ImportError:
    print("Run inside Blender.", file=sys.stderr)
    sys.exit(1)

ROOT = Path(__file__).resolve().parents[2]
BLEND = ROOT / "Assets/Asterra/Shared/Art/Blender/AsterraArt.blend"
OUT = ROOT / "Assets/Asterra/Shared/Art/Blender/Renders/review"
TMP = OUT / "_tmp"


def look_at(cam, target):
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat("-Z", "Y").to_euler()


def aabb(ob):
    mat = ob.matrix_world
    xs, ys, zs = [], [], []
    for v in ob.data.vertices:
        w = mat @ v.co
        if w.z < 0.48:
            continue
        xs.append(w.x)
        ys.append(w.y)
        zs.append(w.z)
    if len(xs) < 12:
        corners = [mat @ Vector(c) for c in ob.bound_box]
        xs = [v.x for v in corners]
        ys = [v.y for v in corners]
        zs = [v.z for v in corners]
    mn = Vector((min(xs), min(ys), min(zs)))
    mx = Vector((max(xs), max(ys), max(zs)))
    return mn, mx


def collect_roster():
    found = {}
    for ob in bpy.data.objects:
        if ob.type != "MESH":
            continue
        kid = ob.get("definition_id")
        if not kid:
            continue
        if not (kid.startswith("unit_") or kid.startswith("building_")):
            continue
        if kid in found:
            continue
        found[kid] = ob
    return dict(sorted(found.items()))


def hide_all_meshes(hide):
    for ob in bpy.data.objects:
        if ob.type == "MESH":
            ob.hide_render = hide
            ob.hide_viewport = hide


def stitch_2x2(paths, dest: Path, size: int):
    import numpy as np

    tiles = []
    for p in paths:
        img = bpy.data.images.load(str(p), check_existing=False)
        w, h = img.size
        px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, img.channels)
        if img.channels == 3:
            a = np.ones((h, w, 1), dtype=np.float32)
            px = np.concatenate([px, a], axis=2)
        tiles.append(px)
        bpy.data.images.remove(img)
    top = np.concatenate([tiles[0], tiles[1]], axis=1)
    bot = np.concatenate([tiles[2], tiles[3]], axis=1)
    sheet = np.concatenate([bot, top], axis=0)
    sh, sw, sc = sheet.shape
    name = dest.stem
    out = bpy.data.images.new(name, width=sw, height=sh, alpha=True)
    out.pixels.foreach_set(sheet.reshape(-1))
    out.filepath_raw = str(dest)
    out.file_format = "PNG"
    out.save()
    bpy.data.images.remove(out)


def main():
    if not BLEND.exists():
        print("missing blend", BLEND)
        sys.exit(1)
    bpy.ops.wm.open_mainfile(filepath=str(BLEND))
    OUT.mkdir(parents=True, exist_ok=True)
    TMP.mkdir(parents=True, exist_ok=True)

    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    except Exception:
        scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    if hasattr(scene, "eevee"):
        scene.eevee.taa_render_samples = 24
    if scene.world and scene.world.use_nodes:
        bg = scene.world.node_tree.nodes.get("Background")
        if bg:
            bg.inputs[0].default_value = (0.42, 0.46, 0.5, 1.0)
            bg.inputs[1].default_value = 0.55

    roster = collect_roster()
    print("review count", len(roster))
    keys = list(roster.keys())

    cam = bpy.data.objects.get("review_cam")
    if cam is None:
        bpy.ops.object.camera_add()
        cam = bpy.context.active_object
        cam.name = "review_cam"
    scene.camera = cam

    saved_locs = {ob: ob.location.copy() for ob in roster.values()}

    for i, key in enumerate(keys):
        ob = roster[key]
        hide_all_meshes(True)
        ob.hide_render = False
        ob.hide_viewport = False
        ob.location = (0.0, 0.0, 0.0)
        bpy.context.view_layer.update()
        mn, mx = aabb(ob)
        span = max(mx.x - mn.x, mx.y - mn.y, mx.z - mn.z, 0.8)
        cx = (mn.x + mx.x) * 0.5
        cy = (mn.y + mx.y) * 0.5
        cz = (mn.z + mx.z) * 0.45
        dist = span * (2.35 if key.startswith("building_") else 2.7)
        height = max(span * 0.42, cz + span * 0.12)
        target = Vector((cx, cy, cz))
        views = [
            ("front", Vector((cx, cy - dist, height))),
            ("three_quarter", Vector((cx + dist * 0.72, cy - dist * 0.72, height * 1.05))),
            ("side", Vector((cx + dist, cy, height))),
            ("high", Vector((cx + dist * 0.35, cy - dist * 0.55, span * 1.15))),
        ]
        paths = []
        for name, loc in views:
            cam.location = loc
            look_at(cam, target)
            path = TMP / f"{key}_{name}.png"
            scene.render.filepath = str(path)
            bpy.ops.render.render(write_still=True)
            paths.append(path)
        dest = OUT / f"{key}.png"
        stitch_2x2(paths, dest, 512)
        ob.location = saved_locs[ob]
        print(f"{i + 1}/{len(keys)}", key, "span", round(span, 2))

    print("wrote", OUT)
    bpy.ops.wm.quit_blender()


if __name__ == "__main__":
    main()
