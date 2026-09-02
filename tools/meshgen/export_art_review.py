#!/usr/bin/env python3
"""Export a roster mesh (OBJ/FBX) and eight review stills.

Canonical stills: Assets/Asterra/Shared/Art/Blender/Renders/models/<id>/<camera>.png
Comparison copies: .../angles/<camera>/<id>.png

Usage:
  /Applications/Blender.app/Contents/MacOS/Blender --background --python tools/meshgen/export_art_review.py -- --only <definition_id>
  /Applications/Blender.app/Contents/MacOS/Blender --background --python tools/meshgen/export_art_review.py -- --kind keeps|buildings|units|props|all [--force]
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

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import asterra_roster  # noqa: E402
import art_review_layout as layout  # noqa: E402
import build_asterra_art_blend as g  # noqa: E402


def look_at(cam, target):
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat("-Z", "Y").to_euler()


def log(*parts):
    print(*parts, flush=True)


def kind_of(def_id):
    if def_id.startswith("building_"):
        return "buildings"
    if def_id.startswith("unit_"):
        return "units"
    return "props"


def cli_flags():
    only = None
    kind = None
    force = False
    extra = []
    if "--" in sys.argv:
        extra = sys.argv[sys.argv.index("--") + 1 :]
    i = 0
    while i < len(extra):
        tok = extra[i]
        if tok == "--only" and i + 1 < len(extra):
            only = extra[i + 1]
            i += 2
            continue
        if tok == "--kind" and i + 1 < len(extra):
            kind = extra[i + 1]
            i += 2
            continue
        if tok == "--force":
            force = True
            i += 1
            continue
        if not tok.startswith("-"):
            only = tok
        i += 1
    return only, kind, force


def render_shot(scene, cam, path, loc, target, lens=40):
    cam.location = loc
    look_at(cam, Vector(target))
    cam.data.lens = lens
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    log("wrote", path)


def setup_preview_lighting(ground=None):
    sun = bpy.data.objects.get("KeySun")
    if sun and sun.data:
        sun.data.energy = 4.2
        sun.data.angle = math.radians(1.8)
        sun.rotation_euler = (math.radians(48), math.radians(8), math.radians(-32))
    fill = bpy.data.objects.get("FillSky")
    if fill and fill.data:
        fill.data.energy = 18
    rim = bpy.data.objects.get("Rim")
    if rim and rim.data:
        rim.data.energy = 45
    if ground is not None:
        ground.scale = (0.22, 0.22, 1.0)
        mat = ground.active_material
        if mat and mat.use_nodes:
            bsdf = mat.node_tree.nodes.get("Principled BSDF")
            if bsdf and "Base Color" in bsdf.inputs:
                bsdf.inputs["Base Color"].default_value = (0.22, 0.23, 0.24, 1.0)
    world = bpy.context.scene.world
    if world and world.use_nodes:
        nt = world.node_tree
        bg = nt.nodes.get("Background")
        sky = nt.nodes.new("ShaderNodeTexSky")
        sky.location = (-280, 0)
        try:
            sky.sky_type = "NISHITA"
        except Exception:
            pass
        if "sun_elevation" in sky.inputs:
            sky.inputs["sun_elevation"].default_value = math.radians(42)
        if "sun_rotation" in sky.inputs:
            sky.inputs["sun_rotation"].default_value = math.radians(40)
        if "air_density" in sky.inputs:
            sky.inputs["air_density"].default_value = 1.05
        if bg:
            nt.links.new(sky.outputs["Color"], bg.inputs["Color"])
            bg.inputs[1].default_value = 0.28


def configure_render(scene, *, samples=48, size=(1600, 900)):
    try:
        scene.render.engine = "CYCLES"
        scene.cycles.samples = samples
        scene.cycles.use_denoising = True
        scene.cycles.device = "CPU"
    except Exception:
        try:
            scene.render.engine = "BLENDER_EEVEE_NEXT"
        except Exception:
            scene.render.engine = "BLENDER_EEVEE"
        if hasattr(scene, "eevee"):
            scene.eevee.taa_render_samples = samples
    scene.render.resolution_x = size[0]
    scene.render.resolution_y = size[1]
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Medium High Contrast"


def bounds(ob):
    corners = [ob.matrix_world @ Vector(c) for c in ob.bound_box]
    xs = [v.x for v in corners]
    ys = [v.y for v in corners]
    zs = [v.z for v in corners]
    cx = (min(xs) + max(xs)) * 0.5
    cy = (min(ys) + max(ys)) * 0.5
    z0, z1 = min(zs), max(zs)
    sx = max(max(xs) - min(xs), 0.8)
    sy = max(max(ys) - min(ys), 0.8)
    sz = max(z1 - z0, 1.2)
    return cx, cy, z0, z1, sx, sy, sz


def camera_shots(cx, cy, z0, z1, sx, sy, sz):
    span = max(sx, sy, sz)
    dist = span * 1.55
    mid = z0 + sz * 0.42
    upper = z0 + sz * 0.78
    return [
        ("front", (cx, cy - max(sy * 1.7, dist * 0.85), mid * 0.55 + 1.1), (cx, cy - sy * 0.15, mid), 35),
        ("three-quarter", (cx + dist * 0.72, cy - dist * 0.95, z0 + sz * 0.28 + 1.4), (cx, cy + sy * 0.05, mid), 40),
        ("side", (cx + dist * 1.15, cy, mid), (cx, cy, mid), 42),
        ("rear", (cx - dist * 0.55, cy + dist * 0.95, mid), (cx, cy, mid), 40),
        ("low", (cx + dist * 0.28, cy - dist * 0.75, z0 + 0.85), (cx, cy, z0 + sz * 0.62), 28),
        ("detail", (cx + sz * 0.55, cy - sz * 0.85, upper), (cx, cy, upper), 50),
        ("high", (cx + dist * 0.42, cy - dist * 0.55, z0 + sz * 1.05), (cx, cy, mid), 35),
        ("top", (cx + span * 0.12, cy - dist * 0.4, z0 + sz * 1.25), (cx, cy, mid), 32),
    ]


def render_cameras(scene, cam, ob, def_id):
    bpy.context.view_layer.update()
    cx, cy, z0, z1, sx, sy, sz = bounds(ob)
    model_dir = layout.MODELS_DIR / def_id
    model_dir.mkdir(parents=True, exist_ok=True)
    for name, loc, target, lens in camera_shots(cx, cy, z0, z1, sx, sy, sz):
        dest = model_dir / f"{name}.png"
        render_shot(scene, cam, dest, loc, target, lens)
        layout.publish_angle_link(dest, def_id, name)
        log("camera", def_id, name)


def delete_object(ob):
    mesh = ob.data if ob.type == "MESH" else None
    bpy.data.objects.remove(ob, do_unlink=True)
    if mesh is not None and mesh.users == 0:
        bpy.data.meshes.remove(mesh)


def roster_items():
    items = (
        list(asterra_roster.KEEPS.items())
        + list(asterra_roster.BUILDINGS.items())
        + list(asterra_roster.UNITS.items())
        + list(asterra_roster.PROPS.items())
    )
    return [(k, fn) for k, fn in items if k not in asterra_roster.SKIP_IDS]


def filtered_items(kind):
    items = roster_items()
    if kind == "all":
        return items
    if kind == "keeps":
        return [(k, fn) for k, fn in asterra_roster.KEEPS.items() if k not in asterra_roster.SKIP_IDS]
    return [(k, fn) for k, fn in items if kind_of(k) == kind]


def ensure_camera(scene):
    cam = bpy.data.objects.get("preview_cam")
    if cam is None:
        bpy.ops.object.camera_add()
        cam = bpy.context.active_object
        cam.name = "preview_cam"
    scene.camera = cam
    return cam


def hide_except(keep_names):
    for ob in bpy.data.objects:
        if ob.type != "MESH":
            continue
        hide = ob.name not in keep_names
        ob.hide_render = hide
        ob.hide_viewport = hide


def main():
    only, kind, force = cli_flags()
    if only is None and kind is None:
        raise SystemExit(
            "usage: Blender --background --python tools/meshgen/export_art_review.py -- "
            "--only <definition_id>  |  --kind keeps|buildings|units|props|all  [--force]"
        )
    if kind is not None and kind not in ("keeps", "buildings", "units", "props", "all"):
        raise SystemExit(f"unknown --kind {kind}")

    g.clear_scene()
    g.setup_collections()
    g.setup_world()
    images = g.generate_textures()
    m = g.make_materials(images)
    ground = g.setup_lights(m)
    setup_preview_lighting(ground)
    scene = bpy.context.scene
    cam = ensure_camera(scene)
    configure_render(scene, samples=48, size=(1600, 900))
    layout.MODELS_DIR.mkdir(parents=True, exist_ok=True)
    layout.ANGLES_DIR.mkdir(parents=True, exist_ok=True)
    catalog = dict(roster_items())
    if only:
        if only not in catalog:
            raise SystemExit(f"unknown mesh {only}")
        items = [(only, catalog[only])]
    else:
        items = filtered_items(kind)
    for i, (def_id, fn) in enumerate(items):
        if layout.captured(def_id) and not only and not force:
            log(f"skip {i + 1}/{len(items)} {def_id}")
            continue
        log(f"start {i + 1}/{len(items)} {def_id}")
        ob = fn(g, m, asterra_roster._coll_for(g, def_id))
        ob.location = (0.0, 0.0, 0.0)
        hide_except({ob.name, ground.name})
        if not force and not def_id.startswith("scenery_"):
            log("export", def_id)
            g.export_game_mesh(ob, def_id)
        render_cameras(scene, cam, ob, def_id)
        delete_object(ob)
        log("done", def_id)
    log("canonical stills", layout.MODELS_DIR)
    bpy.ops.wm.quit_blender()


if __name__ == "__main__":
    main()
