"""Export royal builder body (no welded mallet) + separate mallet prop + unit_builder alias."""
from __future__ import annotations

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


def wipe_meshes(keep):
    for ob in list(bpy.data.objects):
        if ob.type != "MESH" or ob.name in keep:
            continue
        mesh = ob.data
        bpy.data.objects.remove(ob, do_unlink=True)
        if mesh is not None and mesh.users == 0:
            bpy.data.meshes.remove(mesh)


def main():
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
    layout.MODELS_DIR.mkdir(parents=True, exist_ok=True)
    layout.ANGLES_DIR.mkdir(parents=True, exist_ok=True)

    jobs = [
        ("unit_royal_builder", units.royal_builder, ["unit_royal_builder", "unit_builder"]),
        ("unit_royal_builder_mallet", units.royal_builder_mallet, ["unit_royal_builder_mallet", "unit_builder_mallet"]),
    ]
    for def_id, fn, aliases in jobs:
        wipe_meshes({ground.name})
        print("start", def_id, flush=True)
        ob = fn(g, m, asterra_roster._coll_for(g, "unit_royal_builder"))
        ob.location = (0, 0, 0)
        review.hide_except({ob.name, ground.name})
        for alias in aliases:
            print("export", alias, flush=True)
            g.export_game_mesh(ob, alias)
        if "mallet" not in def_id:
            review.render_cameras(scene, cam, ob, def_id)
        print("done", def_id, flush=True)

    print("builder mallet export complete", flush=True)
    bpy.ops.wm.quit_blender()


if __name__ == "__main__":
    main()
