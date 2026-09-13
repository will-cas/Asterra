"""Render hub diorama + Blackridge map still; export faction crest meshes."""
from __future__ import annotations

import math
import shutil
import sys
from pathlib import Path

import bpy
from mathutils import Vector

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE))

import build_asterra_art_blend as g  # noqa: E402

TMP = Path("/tmp/asterra_menu_art")
OUT_UI = ROOT / "Assets/Asterra/Shared/Art/UI/Menu"
OUT_CRESTS = ROOT / "Assets/Asterra/Shared/Art/Meshes/crests"


def look_at(cam, target):
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat("-Z", "Y").to_euler()


def add_cam(name, loc, target, lens=35):
    bpy.ops.object.camera_add(location=loc)
    cam = bpy.context.active_object
    cam.name = name
    cam.data.lens = lens
    look_at(cam, target)
    return cam


def render(cam, path: Path, w=1920, h=1080, samples=64):
    scene = bpy.context.scene
    scene.camera = cam
    scene.render.resolution_x = w
    scene.render.resolution_y = h
    scene.render.filepath = str(path)
    scene.render.image_settings.file_format = "PNG"
    scene.cycles.samples = samples
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.render.render(write_still=True)
    print("wrote", path, flush=True)


def build_pass_ground(m):
    parts = []
    c = g.coll("03_World/Rocks")
    # Long pass ridge
    parts.append(g.cube("ridge", (0, 0, -0.4), (48, 14, 1.2), m.grass, c))
    parts.append(g.cube("road", (0, 0, 0.15), (40, 3.2, 0.25), m.pale_wood, c))
    parts.append(g.cube("cliff_n", (0, 9, 2.5), (50, 4, 6), m.dark_stone, c))
    parts.append(g.cube("cliff_s", (0, -9, 1.8), (50, 3.5, 4.5), m.dark_stone, c))
    for i, x in enumerate((-18, -8, 6, 16, 22)):
        parts.append(g.cube(f"rock{i}", (x, 4.5 + (i % 2), 1.2), (2.2, 1.8, 2.4), m.dark_stone, c, rot=(0, 0, math.radians(i * 18))))
        parts.append(g.ico(f"bush{i}", (x * 0.7, -3.5, 0.6), 1.1, m.leaf, c, subdiv=1, scale=(1.4, 1.1, 0.7)))
    return g.join("pass_ground", parts, c)


def build_crest(name, m, color_mat, motif="crown"):
    c = g.coll("00_StyleLock")
    p = []
    # Shield plate
    p.append(g.cyl("plate", (0, 0, 0.9), 0.55, 0.12, color_mat, c, verts=16, rot=(math.radians(90), 0, 0)))
    p.append(g.cyl("rim", (0, 0, 0.9), 0.58, 0.06, m.iron, c, verts=16, rot=(math.radians(90), 0, 0)))
    if motif == "crown":
        p.append(g.cyl("band", (0, -0.02, 1.15), 0.28, 0.08, m.gold, c, verts=12))
        for i in range(5):
            ang = -0.6 + i * 0.3
            p.append(g.cube(f"spike{i}", (math.sin(ang) * 0.22, -0.02, 1.32), (0.06, 0.06, 0.18), m.gold, c))
    elif motif == "veil":
        p.append(g.cone("hood", (0, -0.05, 1.2), 0.32, 0.45, m.cloth_purple, c, verts=8))
        p.append(g.uv_sphere("eye", (0, -0.2, 1.05), 0.08, m.crystal, c, segs=8, rings=6))
    elif motif == "leaf":
        p.append(g.ico("leaf", (0, -0.05, 1.1), 0.35, m.leaf, c, subdiv=1, scale=(0.7, 0.4, 1.1)))
    elif motif == "sun":
        p.append(g.uv_sphere("disk", (0, -0.05, 1.1), 0.22, m.gold, c, segs=12, rings=8))
        for i in range(8):
            ang = i * (math.pi / 4)
            p.append(g.cube(f"ray{i}", (math.cos(ang) * 0.35, -0.05, 1.1 + math.sin(ang) * 0.35), (0.08, 0.04, 0.22), m.gold, c, rot=(0, 0, ang)))
    elif motif == "cog":
        p.append(g.cyl("hub", (0, -0.05, 1.1), 0.18, 0.1, m.iron, c, verts=12, rot=(math.radians(90), 0, 0)))
        for i in range(8):
            ang = i * (math.pi / 4)
            p.append(g.cube(f"tooth{i}", (math.cos(ang) * 0.32, -0.05, 1.1 + math.sin(ang) * 0.0), (0.12, 0.08, 0.16), m.iron, c, rot=(0, 0, ang)))
    else:  # pirate / freetown
        p.append(g.cube("cross_h", (0, -0.05, 1.1), (0.55, 0.06, 0.1), m.leather, c))
        p.append(g.cube("cross_v", (0, -0.05, 1.1), (0.1, 0.06, 0.55), m.leather, c))
    return g.join(name, p, c)


def main():
    TMP.mkdir(parents=True, exist_ok=True)
    OUT_UI.mkdir(parents=True, exist_ok=True)
    OUT_CRESTS.mkdir(parents=True, exist_ok=True)

    g.clear_scene()
    g.setup_collections()
    g.setup_world()
    images = g.generate_textures()
    m = g.make_materials(images)
    ground_light = g.setup_lights(m)

    # Diorama
    pass_mesh = build_pass_ground(m)
    citadel = g.build_citadel(m)
    citadel.location = (8, 0, 0)
    citadel.scale = (0.55, 0.55, 0.55)

    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"

    hub_cam = add_cam("cam_hub", (-18, -22, 9), (4, 0, 3), lens=32)
    map_cam = add_cam("cam_map", (0, 0, 55), (0, 0, 0), lens=40)
    map_cam.rotation_euler = (0, 0, 0)
    # top-down
    map_cam.location = (0, 0, 42)
    look_at(map_cam, (0, 0, 0))

    render(hub_cam, TMP / "hub_diorama_blackridge.png", 1920, 1080, samples=48)
    render(map_cam, TMP / "map_preview_blackridge.png", 1024, 1024, samples=32)

    # Crests — clear diorama meshes first
    for ob in list(bpy.data.objects):
        if ob.type == "MESH" and ob.name != ground_light.name:
            mesh = ob.data
            bpy.data.objects.remove(ob, do_unlink=True)
            if mesh and mesh.users == 0:
                bpy.data.meshes.remove(mesh)

    crests = [
        ("crest_mundor_crown", m.cloth, "crown"),
        ("crest_uncrowned", m.cloth_purple, "veil"),
        ("crest_outcast_host", m.leaf, "leaf"),
        ("crest_rising_sun", m.gold, "sun"),
        ("crest_university_guild", m.iron, "cog"),
        ("crest_freetown", m.leather, "cross"),
    ]
    for name, mat, motif in crests:
        ob = build_crest(name, m, mat, motif)
        ob.location = (0, 0, 0)
        dest_obj = OUT_CRESTS / f"{name}.obj"
        dest_fbx = OUT_CRESTS / f"{name}.fbx"
        g.export_obj(ob, dest_obj)
        g.export_fbx(ob, dest_fbx)
        g.export_game_mesh(ob, name)
        print("crest", name, flush=True)
        mesh = ob.data
        bpy.data.objects.remove(ob, do_unlink=True)
        if mesh and mesh.users == 0:
            bpy.data.meshes.remove(mesh)

    for src in TMP.glob("*.png"):
        dst = OUT_UI / src.name
        if dst.exists():
            dst.unlink()
        meta = Path(str(dst) + ".meta")
        if meta.exists():
            meta.unlink()
        shutil.copy2(src, dst)
        print("copied", dst, flush=True)

    print("menu art export complete", flush=True)
    bpy.ops.wm.quit_blender()


if __name__ == "__main__":
    main()
