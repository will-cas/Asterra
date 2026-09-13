"""Finish M1 Mundor roster: longbow missing angles, guard, onager.

Writes stills to /tmp first, then copies into the project after Blender quits
so Unity doesn't import half-written PNGs.
"""
from __future__ import annotations

import shutil
import sys
from pathlib import Path

import bpy

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import art_review_layout as layout  # noqa: E402
import asterra_roster  # noqa: E402
import asterra_units as units  # noqa: E402
import build_asterra_art_blend as g  # noqa: E402
import export_art_review as review  # noqa: E402

TMP = Path("/tmp/asterra_roster_stills")
ROSTER = [
    "unit_royal_longbow",
    "unit_royal_guard",
    "unit_royal_onager",
]


def wipe_meshes(keep):
    for ob in list(bpy.data.objects):
        if ob.type != "MESH" or ob.name in keep:
            continue
        mesh = ob.data
        bpy.data.objects.remove(ob, do_unlink=True)
        if mesh is not None and mesh.users == 0:
            bpy.data.meshes.remove(mesh)


def render_to_tmp(scene, cam, ob, def_id):
    out = TMP / def_id
    out.mkdir(parents=True, exist_ok=True)
    bpy.context.view_layer.update()
    cx, cy, z0, z1, sx, sy, sz = review.bounds(ob)
    for name, loc, target, lens in review.camera_shots(cx, cy, z0, z1, sx, sy, sz):
        dest = out / f"{name}.png"
        review.render_shot(scene, cam, dest, loc, target, lens)
        print("camera", def_id, name, flush=True)


def main():
    TMP.mkdir(parents=True, exist_ok=True)
    g.clear_scene()
    g.setup_collections()
    g.setup_world()
    images = g.generate_textures()
    m = g.make_materials(images)
    ground = g.setup_lights(m)
    review.setup_preview_lighting(ground)
    scene = bpy.context.scene
    cam = review.ensure_camera(scene)
    review.configure_render(scene, samples=32, size=(1280, 720))

    for def_id in ROSTER:
        wipe_meshes({ground.name})
        print("start", def_id, flush=True)
        fn = units.UNITS[def_id]
        ob = fn(g, m, asterra_roster._coll_for(g, def_id))
        ob.location = (0.0, 0.0, 0.0)
        review.hide_except({ob.name, ground.name})
        g.export_game_mesh(ob, def_id)
        render_to_tmp(scene, cam, ob, def_id)
        print("done", def_id, flush=True)

    print("COPY_STILLS", flush=True)
    for def_id in ROSTER:
        src = TMP / def_id
        dst = layout.MODELS_DIR / def_id
        dst.mkdir(parents=True, exist_ok=True)
        for png in src.glob("*.png"):
            target = dst / png.name
            # drop stale meta so Unity reimports clean
            meta = Path(str(target) + ".meta")
            if meta.exists():
                meta.unlink()
            shutil.copy2(png, target)
            layout.publish_angle_link(target, def_id, png.stem)
            ang_meta = layout.ANGLES_DIR / png.stem / f"{def_id}.png.meta"
            if ang_meta.exists():
                ang_meta.unlink()
            print("copied", def_id, png.name, flush=True)

    print("mundor roster finish complete", flush=True)
    bpy.ops.wm.quit_blender()


if __name__ == "__main__":
    main()
