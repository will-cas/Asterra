#!/usr/bin/env python3
"""Blender batch art pass: import OBJs, bevel + shade, export polished meshes.

Usage:
  /Applications/Blender.app/Contents/MacOS/Blender --background --python tools/meshgen/blender_art_pass.py
"""
import sys
from pathlib import Path

try:
    import bpy
    import bmesh
    from mathutils import Vector
except ImportError:
    print('Run inside Blender:', file=sys.stderr)
    sys.exit(1)

ROOT = Path(__file__).resolve().parents[2]
MESH_DIR = ROOT / 'Assets/Asterra/Shared/Art/Meshes'


def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in bpy.data.meshes:
        bpy.data.meshes.remove(block)


def import_obj(path: Path):
    # Blender 4+/5: wm.obj_import
    if hasattr(bpy.ops.wm, 'obj_import'):
        bpy.ops.wm.obj_import(filepath=str(path))
    else:
        bpy.ops.import_scene.obj(filepath=str(path))
    return bpy.context.selected_objects[0] if bpy.context.selected_objects else None


def export_obj(path: Path):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.context.scene.objects:
        if obj.type == 'MESH':
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
    if hasattr(bpy.ops.wm, 'obj_export'):
        bpy.ops.wm.obj_export(
            filepath=str(path),
            export_selected_objects=True,
            export_materials=False,
            export_triangulated_mesh=True,
        )
    else:
        bpy.ops.export_scene.obj(
            filepath=str(path),
            use_selection=True,
            use_materials=False,
            use_triangles=True,
        )


def bevel_object(obj, width=0.04, segments=1):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    mod = obj.modifiers.new(name='Bevel', type='BEVEL')
    mod.width = width
    mod.segments = segments
    mod.limit_method = 'ANGLE'
    mod.angle_limit = 0.7
    bpy.ops.object.modifier_apply(modifier=mod.name)


def add_keep_banner(obj):
    """Extra silhouette detail on keeps."""
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.0, 0.0, 11.6))
    pole = bpy.context.active_object
    pole.scale = (0.18, 0.18, 1.2)
    bpy.ops.object.transform_apply(scale=True)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.7, 0.0, 12.4))
    flag = bpy.context.active_object
    flag.scale = (1.1, 0.08, 0.55)
    bpy.ops.object.transform_apply(scale=True)
    # Join into keep
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    pole.select_set(True)
    flag.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.join()
    return obj


def add_unit_cape_hint(obj):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(-0.05, -0.42, 0.85))
    cape = bpy.context.active_object
    cape.scale = (0.7, 0.08, 0.9)
    bpy.ops.object.transform_apply(scale=True)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    cape.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.join()
    return obj


def process(path: Path):
    clear_scene()
    obj = import_obj(path)
    if obj is None:
        print('skip (no object)', path.name)
        return
    obj.name = path.stem

    # Normalize: Blender may import with Z-up already from our OBJs (Y-up in file).
    bevel_w = 0.06 if path.stem.startswith('building') else 0.03
    try:
        bevel_object(obj, width=bevel_w, segments=1)
    except Exception as e:
        print('bevel failed', path.name, e)

    if path.stem == 'building_keep':
        try:
            obj = add_keep_banner(obj)
        except Exception as e:
            print('banner failed', e)
    if path.stem in ('unit_leader', 'unit_militia', 'unit_ember_raider'):
        try:
            obj = add_unit_cape_hint(obj)
        except Exception as e:
            print('cape failed', e)

    # Shade smooth for softer RTS read (still low poly).
    bpy.ops.object.shade_smooth()

    out = path  # overwrite in place
    export_obj(out)
    print('art-passed', path.name)


def main():
    files = sorted(MESH_DIR.glob('*.obj'))
    if not files:
        print('no objs in', MESH_DIR)
        return
    for path in files:
        process(path)
    print('blender art pass complete:', len(files), 'meshes')


if __name__ == '__main__':
    main()
