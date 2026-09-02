#!/usr/bin/env python3
"""Build the Asterra master Blender file: high-detail roster + PBR maps.

Usage:
  /Applications/Blender.app/Contents/MacOS/Blender --background --python tools/meshgen/build_asterra_art_blend.py
"""
from __future__ import annotations

import math
import shutil
import sys
from pathlib import Path

try:
    import bpy
    from mathutils import Vector
except ImportError:
    print("Run inside Blender.", file=sys.stderr)
    sys.exit(1)

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import asterra_pbr  # noqa: E402
import asterra_roster  # noqa: E402

SKIP_PRIMITIVE_UV = False

ART = ROOT / "Assets/Asterra/Shared/Art/Blender"
BLEND = ART / "AsterraArt.blend"
RENDER = ART / "Renders"
EXPORT = ART / "Exports"
TEX = ART / "Textures"
UNITY_MESH = ROOT / "Assets/Asterra/Shared/Art/Meshes"
UNITY_TEX = ROOT / "Assets/Asterra/Shared/Art/Textures"
TEX_SIZE = 512


def out_dir(path: Path) -> None:
    path.mkdir(parents=True, exist_ok=True)


def clear_scene() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)


def coll(path: str):
    scene = bpy.context.scene.collection
    parent = scene
    node = None
    for name in path.split("/"):
        node = bpy.data.collections.get(name)
        if node is None:
            node = bpy.data.collections.new(name)
            parent.children.link(node)
        elif node.name not in [c.name for c in parent.children]:
            parent.children.link(node)
        parent = node
    return node


def move_to(ob, collection) -> None:
    for c in list(ob.users_collection):
        c.objects.unlink(ob)
    collection.objects.link(ob)


def save_image(name, pixels, size, filename):
    img = bpy.data.images.new(name, width=size, height=size, alpha=False)
    img.pixels.foreach_set(pixels)
    img.filepath_raw = str(TEX / filename)
    img.file_format = "PNG"
    img.save()
    img.pack()
    return img


def generate_textures():
    out_dir(TEX)
    images = {}
    for key, sampler in asterra_pbr.SAMPLERS.items():
        alb_path = TEX / f"{key}_albedo.png"
        if alb_path.exists():
            images[key] = {
                "albedo": bpy.data.images.load(str(alb_path)),
                "rough": bpy.data.images.load(str(TEX / f"{key}_rough.png")),
                "normal": bpy.data.images.load(str(TEX / f"{key}_normal.png")),
            }
            continue
        print("tex", key)
        albedo, rough, nrm, size = asterra_pbr.write_maps(TEX, key, sampler, TEX_SIZE)
        images[key] = {
            "albedo": save_image(f"{key}_albedo", albedo, size, f"{key}_albedo.png"),
            "rough": save_image(f"{key}_rough", rough, size, f"{key}_rough.png"),
            "normal": save_image(f"{key}_normal", nrm, size, f"{key}_normal.png"),
        }
    return images


def pbr_mat(name, maps, metallic=0.0, uv_scale=1.0, normal_str=1.0, spec=0.5, transmission=0.0, coord="Object", clearcoat=0.0):
    existing = bpy.data.materials.get(name)
    if existing:
        return existing
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    bsdf.location = (200, 0)
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = metallic
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = spec
    elif "Specular" in bsdf.inputs:
        bsdf.inputs["Specular"].default_value = spec
    if clearcoat > 0.01:
        if "Coat Weight" in bsdf.inputs:
            bsdf.inputs["Coat Weight"].default_value = clearcoat
            if "Coat Roughness" in bsdf.inputs:
                bsdf.inputs["Coat Roughness"].default_value = 0.12
        elif "Clearcoat" in bsdf.inputs:
            bsdf.inputs["Clearcoat"].default_value = clearcoat
    if transmission > 0.01:
        if "Transmission Weight" in bsdf.inputs:
            bsdf.inputs["Transmission Weight"].default_value = transmission
        elif "Transmission" in bsdf.inputs:
            bsdf.inputs["Transmission"].default_value = transmission
        if "IOR" in bsdf.inputs:
            bsdf.inputs["IOR"].default_value = 1.52
        m.use_screen_refraction = True
    uv = nt.nodes.new("ShaderNodeTexCoord")
    uv.location = (-900, 0)
    mapping = nt.nodes.new("ShaderNodeMapping")
    mapping.location = (-700, 0)
    mapping.inputs["Scale"].default_value = (uv_scale, uv_scale, uv_scale)
    if coord == "UV":
        src = uv.outputs["UV"]
    elif coord == "Generated":
        src = uv.outputs["Generated"]
    else:
        src = uv.outputs["Object"]
    nt.links.new(src, mapping.inputs["Vector"])

    def tex_node(img, loc, non_color=False):
        n = nt.nodes.new("ShaderNodeTexImage")
        n.location = loc
        n.image = img
        if non_color:
            n.image.colorspace_settings.name = "Non-Color"
        nt.links.new(mapping.outputs["Vector"], n.inputs["Vector"])
        return n

    alb = tex_node(maps["albedo"], (-420, 180))
    rg = tex_node(maps["rough"], (-420, -40), True)
    nm = tex_node(maps["normal"], (-420, -260), True)
    nmap = nt.nodes.new("ShaderNodeNormalMap")
    nmap.location = (-80, -260)
    nmap.inputs["Strength"].default_value = normal_str
    nt.links.new(alb.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(rg.outputs["Color"], bsdf.inputs["Roughness"])
    nt.links.new(nm.outputs["Color"], nmap.inputs["Color"])
    nt.links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
    return m


def assign(ob, material) -> None:
    if ob.data.materials:
        ob.data.materials[0] = material
    else:
        ob.data.materials.append(material)


def cube_uv(ob, cube_size=2.0):
    if SKIP_PRIMITIVE_UV:
        return
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    try:
        bpy.ops.uv.cube_project(cube_size=cube_size, correct_aspect=True, scale_to_bounds=False)
    except TypeError:
        bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")


def solid(name, verts, faces, material, collection, uv=1.4):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([tuple(v) for v in verts], [], [tuple(f) for f in faces])
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(ob)
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    assign(ob, material)
    cube_uv(ob, uv)
    move_to(ob, collection)
    return ob


def cube(name, loc, size, material, collection, rot=(0, 0, 0), uv=2.0):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    assign(ob, material)
    cube_uv(ob, uv)
    move_to(ob, collection)
    return ob


def cyl(name, loc, radius, depth, material, collection, verts=16, rot=(0, 0, 0), uv=2.0):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=verts, radius=radius, depth=depth, location=loc, rotation=rot
    )
    ob = bpy.context.active_object
    ob.name = name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    assign(ob, material)
    cube_uv(ob, uv)
    move_to(ob, collection)
    return ob


def taper(name, loc, r1, r2, depth, material, collection, verts=18, rot=(0, 0, 0), uv=2.0):
    bpy.ops.mesh.primitive_cone_add(
        vertices=verts, radius1=r1, radius2=r2, depth=depth, location=loc, rotation=rot
    )
    ob = bpy.context.active_object
    ob.name = name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    assign(ob, material)
    cube_uv(ob, uv)
    move_to(ob, collection)
    return ob


def cone(name, loc, radius1, depth, material, collection, verts=12, rot=(0, 0, 0), uv=2.0):
    bpy.ops.mesh.primitive_cone_add(
        vertices=verts, radius1=radius1, radius2=0.02, depth=depth, location=loc, rotation=rot
    )
    ob = bpy.context.active_object
    ob.name = name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    assign(ob, material)
    cube_uv(ob, uv)
    move_to(ob, collection)
    return ob


def ico(name, loc, radius, material, collection, subdiv=3, scale=(1, 1, 1), uv=2.0):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdiv, radius=radius, location=loc)
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign(ob, material)
    cube_uv(ob, uv)
    move_to(ob, collection)
    return ob


def uv_sphere(name, loc, radius, material, collection, segs=16, rings=10, uv=1.0):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs, ring_count=rings, radius=radius, location=loc)
    ob = bpy.context.active_object
    ob.name = name
    assign(ob, material)
    cube_uv(ob, uv)
    move_to(ob, collection)
    return ob


def join(name, objects, collection):
    bpy.ops.object.select_all(action="DESELECT")
    for ob in objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    ob = bpy.context.active_object
    ob.name = name
    move_to(ob, collection)
    return ob


def bevel(ob, width=0.05, segments=2):
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    mod = ob.modifiers.new(name="Bevel", type="BEVEL")
    mod.width = width
    mod.segments = segments
    mod.limit_method = "ANGLE"
    mod.angle_limit = 0.52
    bpy.ops.object.modifier_apply(modifier=mod.name)
    try:
        bpy.ops.object.shade_auto_smooth(angle=math.radians(35))
    except Exception:
        bpy.ops.object.shade_smooth()


def subdiv(ob, levels=1):
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    mod = ob.modifiers.new(name="Sub", type="SUBSURF")
    mod.levels = max(1, levels)
    mod.render_levels = max(1, levels)
    bpy.ops.object.modifier_apply(modifier=mod.name)
    try:
        bpy.ops.object.shade_auto_smooth(angle=math.radians(28))
    except Exception:
        bpy.ops.object.shade_smooth()


MAT_TINTS = {
    "M_Gold": (0.86, 0.68, 0.24, 1.0),
    "M_Marble": (0.90, 0.86, 0.78, 1.0),
    "M_Crystal": (0.62, 0.42, 0.92, 1.0),
    "M_Glass": (0.42, 0.68, 0.82, 1.0),
    "M_Steel": (0.58, 0.62, 0.68, 1.0),
    "M_Iron": (0.38, 0.36, 0.34, 1.0),
    "M_DarkStone": (0.28, 0.27, 0.26, 1.0),
    "M_Brick": (0.52, 0.46, 0.38, 1.0),
    "M_RedBrick": (0.58, 0.32, 0.24, 1.0),
    "M_Slate": (0.32, 0.34, 0.36, 1.0),
    "M_Wood": (0.42, 0.28, 0.14, 1.0),
    "M_PaleWood": (0.72, 0.58, 0.38, 1.0),
    "M_Bark": (0.32, 0.22, 0.12, 1.0),
    "M_Leather": (0.36, 0.22, 0.12, 1.0),
    "M_Cloth": (0.72, 0.68, 0.62, 1.0),
    "M_ClothDeep": (0.28, 0.32, 0.48, 1.0),
    "M_ClothPurple": (0.42, 0.22, 0.55, 1.0),
    "M_ClothGreen": (0.32, 0.48, 0.28, 1.0),
    "M_ClothBlue": (0.22, 0.38, 0.58, 1.0),
    "M_ClothSun": (0.86, 0.62, 0.22, 1.0),
    "M_Skin": (0.82, 0.62, 0.48, 1.0),
    "M_Ice": (0.72, 0.86, 0.92, 1.0),
    "M_Plaster": (0.82, 0.78, 0.70, 1.0),
    "M_Leaf": (0.28, 0.48, 0.22, 1.0),
    "M_LeafDark": (0.18, 0.32, 0.14, 1.0),
}


def bake_vertex_colors(ob) -> None:
    """Stamp material tints onto corners so Unity OBJ retains gold/cloth/stone."""
    mesh = ob.data
    if mesh is None or not mesh.polygons:
        return
    if mesh.color_attributes.get("Col") is None:
        mesh.color_attributes.new(name="Col", type="FLOAT_COLOR", domain="CORNER")
    attr = mesh.color_attributes["Col"]
    tints = []
    for mat in mesh.materials:
        name = mat.name if mat is not None else ""
        tints.append(MAT_TINTS.get(name, (0.72, 0.68, 0.62, 1.0)))
    if not tints:
        tints = [(0.72, 0.68, 0.62, 1.0)]
    for poly in mesh.polygons:
        col = tints[poly.material_index % len(tints)]
        for li in poly.loop_indices:
            attr.data[li].color = col


class Mats:
    pass


def make_materials(images):
    m = Mats()
    m.marble = pbr_mat("M_Marble", images["marble"], uv_scale=1.35, normal_str=0.85, spec=0.55)
    m.gold = pbr_mat("M_Gold", images["gold"], metallic=0.92, uv_scale=0.85, normal_str=0.35, spec=0.95, coord="Generated", clearcoat=0.55)
    m.brick = pbr_mat("M_Brick", images["stone_brick"], uv_scale=3.2, normal_str=1.85)
    m.wood = pbr_mat("M_Wood", images["wood"], uv_scale=2.5, normal_str=0.85)
    m.leather = pbr_mat("M_Leather", images["leather"], uv_scale=2.8, normal_str=0.7)
    m.cloth = pbr_mat("M_Cloth", images["cloth"], uv_scale=0.85, normal_str=0.35)
    m.cloth_deep = pbr_mat("M_ClothDeep", images["cloth_deep"], uv_scale=0.85, normal_str=0.35)
    m.cloth_purple = pbr_mat("M_ClothPurple", images["cloth_purple"], uv_scale=0.85, normal_str=0.35)
    m.cloth_green = pbr_mat("M_ClothGreen", images["cloth_green"], uv_scale=0.85, normal_str=0.35)
    m.cloth_blue = pbr_mat("M_ClothBlue", images["cloth_blue"], uv_scale=0.85, normal_str=0.35)
    m.cloth_sun = pbr_mat("M_ClothSun", images["cloth_sun"], uv_scale=0.85, normal_str=0.35)
    m.iron = pbr_mat("M_Iron", images["iron"], metallic=0.72, uv_scale=2.4, normal_str=0.8)
    m.skin = pbr_mat("M_Skin", images["skin"], uv_scale=2.0, normal_str=0.25)
    m.bark = pbr_mat("M_Bark", images["bark"], uv_scale=1.8, normal_str=1.35)
    m.leaf = pbr_mat("M_Leaf", images["leaf"], uv_scale=2.2, normal_str=0.7)
    m.leaf_d = pbr_mat("M_LeafDark", images["leaf_dark"], uv_scale=2.2, normal_str=0.7)
    m.grass = pbr_mat("M_Grass", images["grass"], uv_scale=8.0, normal_str=0.9)
    m.crystal = pbr_mat("M_Crystal", images["crystal"], metallic=0.04, uv_scale=1.1, normal_str=0.35, spec=0.95, transmission=0.82, coord="Generated")
    m.glass = pbr_mat("M_Glass", images["glass"], metallic=0.0, uv_scale=0.6, normal_str=0.08, spec=1.0, transmission=0.94, coord="Generated")
    m.ice = pbr_mat("M_Ice", images["ice"], metallic=0.08, uv_scale=1.4, normal_str=0.55, spec=0.85)
    m.steel = pbr_mat("M_Steel", images["steel"], metallic=0.82, uv_scale=2.2, normal_str=0.75)
    m.dark_stone = pbr_mat("M_DarkStone", images["dark_stone"], uv_scale=1.2, normal_str=1.25)
    m.red_brick = pbr_mat("M_RedBrick", images["red_brick"], uv_scale=1.35, normal_str=1.45)
    m.plaster = pbr_mat("M_Plaster", images["plaster"], uv_scale=1.1, normal_str=0.85)
    m.slate = pbr_mat("M_Slate", images["slate"], uv_scale=1.6, normal_str=1.2)
    m.pale_wood = pbr_mat("M_PaleWood", images["pale_wood"], uv_scale=2.2, normal_str=0.8)
    return m


def setup_world():
    world = bpy.data.worlds.new("AsterraWorld")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (0.42, 0.52, 0.62, 1.0)
    bg.inputs[1].default_value = 1.15
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.image_settings.file_format = "PNG"
    if hasattr(scene, "eevee"):
        scene.eevee.taa_render_samples = 96
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Medium High Contrast"


def setup_lights(m):
    c = coll("06_CamerasLights")
    bpy.ops.object.light_add(type="SUN", location=(16, -22, 32))
    sun = bpy.context.active_object
    sun.name = "KeySun"
    sun.data.energy = 5.0
    sun.data.color = (1.0, 0.95, 0.84)
    sun.data.angle = math.radians(12)
    sun.rotation_euler = (math.radians(50), math.radians(8), math.radians(-32))
    move_to(sun, c)
    bpy.ops.object.light_add(type="AREA", location=(-16, 12, 11))
    fill = bpy.context.active_object
    fill.name = "FillSky"
    fill.data.energy = 320
    fill.data.size = 16
    fill.data.color = (0.58, 0.70, 0.88)
    move_to(fill, c)
    bpy.ops.object.light_add(type="AREA", location=(6, 18, 7))
    rim = bpy.context.active_object
    rim.name = "Rim"
    rim.data.energy = 220
    rim.data.size = 8
    rim.data.color = (1.0, 0.86, 0.55)
    move_to(rim, c)
    ground = cube("ground", (0, 0, -0.12), (80, 80, 0.24), m.grass, c, uv=18.0)
    return ground


def look_at(cam, target):
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat("-Z", "Y").to_euler()


def add_camera(name, loc, target, ortho=False, ortho_scale=28, lens=50):
    c = coll("06_CamerasLights")
    bpy.ops.object.camera_add(location=loc)
    cam = bpy.context.active_object
    cam.name = name
    look_at(cam, target)
    cam.data.lens = lens
    if ortho:
        cam.data.type = "ORTHO"
        cam.data.ortho_scale = ortho_scale
    move_to(cam, c)
    return cam


def build_citadel(m):
    c = coll("02_Buildings/MundorCrown")
    p = []
    p.append(cube("earth", (0, 0, 0.28), (19.2, 19.2, 0.56), m.brick, c, uv=5))
    p.append(cube("plinth", (0, 0, 0.85), (17.6, 17.6, 0.7), m.brick, c, uv=4.5))
    p.append(cube("plinth_cap", (0, 0, 1.22), (17.9, 17.9, 0.16), m.slate, c, uv=4))
    p.append(cube("bailey", (0, 0, 2.7), (14.8, 14.8, 3.4), m.plaster, c, uv=3.2))
    for i, (x, y, yaw) in enumerate((
        (5.4, 7.55, 0), (-5.4, 7.55, 0),
        (7.55, 5.4, math.radians(90)), (7.55, -5.4, math.radians(90)),
        (-7.55, 5.4, math.radians(-90)), (-7.55, -5.4, math.radians(-90)),
        (5.4, -7.55, math.radians(180)), (-5.4, -7.55, math.radians(180)),
    )):
        p.append(taper(f"butt_{i}", (x, y, 2.15), 0.95, 0.45, 3.2, m.brick, c, verts=8, rot=(math.radians(8), 0, yaw), uv=1.8))
    for x, y in ((7.05, 7.05), (7.05, -7.05), (-7.05, 7.05), (-7.05, -7.05)):
        p.append(cube(f"quoin_{x}_{y}", (x, y, 2.7), (1.25, 1.25, 3.5), m.brick, c, uv=2))
    p.append(cube("keep", (0, -0.35, 6.85), (7.4, 7.2, 6.5), m.plaster, c, uv=2.8))
    p.append(cube("keep_string", (0, -0.35, 8.55), (7.65, 7.45, 0.18), m.brick, c, uv=2.5))
    p.append(cube("keep_cornice", (0, -0.35, 10.05), (7.85, 7.65, 0.22), m.slate, c, uv=2.5))
    p.append(cube("keep_bronze", (0, -0.35, 10.22), (7.55, 7.35, 0.08), m.gold, c, uv=2))
    p.append(cube("roof_a", (0, 1.2, 11.35), (6.9, 4.5, 0.16), m.slate, c, rot=(math.radians(31), 0, 0), uv=2.4))
    p.append(cube("roof_b", (0, -1.9, 11.35), (6.9, 4.5, 0.16), m.slate, c, rot=(math.radians(-31), 0, 0), uv=2.4))
    for row in range(5):
        for col in range(7):
            xx = (col - 3) * 0.85
            yy = -0.35 + (row - 2) * 0.5
            zz = 10.6 + abs(row - 2) * 0.18
            p.append(cube(
                f"tile_{row}_{col}", (xx, yy, zz), (0.88, 0.52, 0.055), m.slate, c,
                rot=(math.radians(16 if row > 2 else -16), 0, 0), uv=0.7,
            ))
    p.append(cube("ridge", (0, -0.35, 12.35), (6.5, 0.16, 0.16), m.slate, c, uv=1))
    p.append(cyl("chim_l", (-1.8, -1.4, 12.15), 0.22, 1.1, m.brick, c, verts=8, uv=1))
    p.append(cyl("chim_r", (1.8, 0.6, 12.15), 0.2, 0.95, m.brick, c, verts=8, uv=1))
    p.append(cube("chim_cap_l", (-1.8, -1.4, 12.75), (0.55, 0.55, 0.1), m.slate, c, uv=0.6))
    p.append(cube("walk", (0, 0, 4.42), (15.5, 15.5, 0.42), m.slate, c, uv=4))
    p.append(cube("walk_wood", (0, 0, 4.22), (13.2, 13.2, 0.14), m.wood, c, uv=4))
    half = 7.48
    for i in range(-4, 5):
        x = i * 1.32
        if abs(x) > 4.6:
            continue
        h = 0.95 + (0.12 if i % 2 == 0 else 0.0)
        for ny, tag in ((half, "n"), (-half, "s")):
            p.append(cube(f"mer_{tag}_{i}", (x, ny, 4.95), (0.58, 0.52, h), m.slate, c, uv=0.9))
        for nx, tag in ((half, "e"), (-half, "w")):
            p.append(cube(f"mer_{tag}_{i}", (nx, x, 4.95), (0.52, 0.58, h), m.slate, c, uv=0.9))
    for i, (loc, rot) in enumerate((
        ((0, 3.48, 7.5), (0, 0, 0)),
        ((0, -4.05, 7.5), (0, 0, 0)),
        ((3.78, -0.35, 7.5), (0, 0, math.radians(90))),
        ((-3.78, -0.35, 7.5), (0, 0, math.radians(90))),
        ((0, 3.48, 5.65), (0, 0, 0)),
        ((3.78, -0.35, 5.65), (0, 0, math.radians(90))),
        ((-3.78, -0.35, 5.65), (0, 0, math.radians(90))),
    )):
        p.append(cube(f"wframe_{i}", loc, (1.35, 0.22, 1.75), m.brick, c, rot=rot, uv=1))
        inset = list(loc)
        if abs(rot[2]) < 0.1:
            inset[1] += 0.1 if loc[1] > 0 else -0.1
        else:
            inset[0] += 0.1 if loc[0] > 0 else -0.1
        p.append(cube(f"wvoid_{i}", tuple(inset), (0.95, 0.16, 1.35), m.slate, c, rot=rot, uv=0.8))
        p.append(cube(f"wmull_{i}", tuple(inset), (0.07, 0.18, 1.35), m.wood, c, rot=rot, uv=0.5))
    for i, (x, y) in enumerate(((6.5, 6.5), (6.5, -6.5), (-6.5, 6.5), (-6.5, -6.5))):
        p.append(taper(f"tower_{i}", (x, y, 5.6), 1.85, 1.35, 8.6, m.plaster, c, verts=18, uv=2.4))
        p.append(cube(f"tring_{i}", (x, y, 9.85), (3.15, 3.15, 0.28), m.brick, c, uv=1.4))
        p.append(cube(f"tcap_{i}", (x, y, 10.15), (3.35, 3.35, 0.22), m.slate, c, uv=1.4))
        for k in range(6):
            ang = k * math.pi / 3.0
            p.append(cube(
                f"tmer_{i}_{k}",
                (x + math.cos(ang) * 1.55, y + math.sin(ang) * 1.55, 10.55),
                (0.42, 0.42, 0.7), m.slate, c, uv=0.5,
            ))
        p.append(cone(f"troof_{i}", (x, y, 11.15), 1.55, 1.35, m.slate, c, verts=16, uv=1.6))
        p.append(cyl(f"fin_{i}", (x, y, 11.9), 0.06, 0.45, m.gold, c, verts=6, uv=0.4))
        p.append(cube(f"tslit_{i}", (x + math.copysign(1.45, x), y, 6.5), (0.22, 0.32, 1.25), m.slate, c, uv=0.5))
        p.append(cube(f"tslit2_{i}", (x, y + math.copysign(1.45, y), 7.7), (0.32, 0.22, 1.15), m.slate, c, uv=0.5))
    p.append(cube("gatehouse", (0, 7.65, 3.25), (5.8, 3.8, 5.1), m.plaster, c, uv=2.4))
    p.append(cube("gate_roof_a", (0, 8.4, 6.05), (6.2, 2.4, 0.14), m.slate, c, rot=(math.radians(18), 0, 0), uv=2))
    p.append(cube("gate_roof_b", (0, 6.9, 6.05), (6.2, 2.4, 0.14), m.slate, c, rot=(math.radians(-18), 0, 0), uv=2))
    p.append(cube("pier_l", (-1.55, 9.35, 1.55), (0.7, 1.1, 3.0), m.brick, c, uv=1.5))
    p.append(cube("pier_r", (1.55, 9.35, 1.55), (0.7, 1.1, 3.0), m.brick, c, uv=1.5))
    for k in range(9):
        t = math.pi * k / 8.0
        ax = math.cos(t) * 1.45
        az = 2.55 + math.sin(t) * 1.35
        p.append(cube(f"vous_{k}", (ax, 9.4, az), (0.42, 0.95, 0.38), m.brick, c, rot=(0, math.pi / 2 - t, 0), uv=0.7))
    p.append(cube("door_l", (-0.52, 9.55, 1.35), (1.0, 0.14, 2.5), m.wood, c, uv=1.4))
    p.append(cube("door_r", (0.52, 9.55, 1.35), (1.0, 0.14, 2.5), m.wood, c, uv=1.4))
    for k in range(5):
        p.append(cyl(f"hinge_l{k}", (-1.02, 9.64, 0.55 + k * 0.45), 0.04, 0.05, m.iron, c, verts=8, rot=(math.radians(90), 0, 0), uv=0.3))
        p.append(cyl(f"hinge_r{k}", (1.02, 9.64, 0.55 + k * 0.45), 0.04, 0.05, m.iron, c, verts=8, rot=(math.radians(90), 0, 0), uv=0.3))
    for k in range(6):
        p.append(cyl(f"port_{k}", (-0.75 + k * 0.3, 9.22, 2.35), 0.035, 2.4, m.iron, c, verts=8, uv=0.35))
    p.append(cube("draw", (0, 10.55, 0.18), (2.5, 2.4, 0.12), m.wood, c, uv=2))
    p.append(cube("draw_rib", (0, 10.55, 0.26), (0.12, 2.4, 0.08), m.iron, c, uv=0.5))
    for s in range(8):
        p.append(cube(f"step_{s}", (0, 5.35 - s * 0.26, 1.2 + s * 0.2), (2.3, 0.38, 0.2), m.brick, c, uv=1))
    p.append(cube("tomb", (0, -7.75, 2.4), (4.6, 2.7, 3.3), m.brick, c, uv=2))
    p.append(taper("tcol_l", (-1.45, -8.95, 1.75), 0.22, 0.18, 2.5, m.brick, c, verts=8, uv=1))
    p.append(taper("tcol_r", (1.45, -8.95, 1.75), 0.22, 0.18, 2.5, m.brick, c, verts=8, uv=1))
    p.append(cube("tomb_door", (0, -9.1, 1.6), (1.35, 0.16, 2.05), m.wood, c, uv=1))
    p.append(cube("tomb_lintel", (0, -9.0, 2.85), (2.5, 0.35, 0.28), m.gold, c, uv=0.8))
    p.append(cube("pediment", (0, -8.95, 3.45), (2.8, 0.32, 0.55), m.brick, c, uv=1))
    p.append(cyl("pole", (2.15, 8.4, 8.35), 0.07, 3.8, m.wood, c, verts=8, uv=1))
    p.append(cube("banner1", (2.95, 8.4, 9.15), (1.55, 0.05, 0.85), m.cloth_deep, c, uv=1.2))
    p.append(cube("banner2", (2.85, 8.42, 8.45), (1.4, 0.04, 0.55), m.cloth, c, uv=1.1))
    p.append(cyl("pole2", (-2.15, 8.4, 8.0), 0.07, 3.2, m.wood, c, verts=8, uv=1))
    p.append(cube("banner_b", (-2.9, 8.4, 8.55), (1.4, 0.05, 1.05), m.cloth_deep, c, uv=1.2))
    keep = join("building_royal_citadel", p, c)
    bevel(keep, width=0.055, segments=3)
    keep["definition_id"] = "building_royal_citadel"
    return keep


def _limb(c, name, loc, r, d, mat, rot=(0, 0, 0)):
    return cyl(name, loc, r, d, mat, c, verts=10, rot=rot, uv=1.0)


def build_humanoid(name, c, m, kit="levy"):
    p = []
    cloth = m.cloth
    accent = m.cloth_deep
    p.append(cyl("hips", (0, 0, 0.94), 0.16, 0.24, m.leather, c, verts=12, uv=1.2))
    p.append(cyl("torso", (0, 0.02, 1.26), 0.20, 0.46, cloth, c, verts=12, uv=2.5))
    p.append(cube("tabard", (0, -0.12, 1.14), (0.18, 0.04, 0.72), accent, c, uv=3.0))
    p.append(cube("belt", (0, 0.02, 1.02), (0.42, 0.26, 0.07), m.leather, c, uv=1))
    p.append(cube("buckle", (0, -0.14, 1.02), (0.08, 0.03, 0.08), m.gold, c, uv=0.4))
    p.append(cyl("neck", (0, 0, 1.50), 0.07, 0.14, m.skin, c, verts=12, uv=0.6))
    p.append(uv_sphere("head", (0, 0.02, 1.66), 0.135, m.skin, c, segs=18, rings=14, uv=1))
    p.append(cube("nose", (0, -0.12, 1.64), (0.045, 0.06, 0.055), m.skin, c, uv=0.4))
    p.append(cube("brow", (0, -0.1, 1.72), (0.16, 0.035, 0.03), m.skin, c, uv=0.4))
    p.append(cube("jaw", (0, -0.04, 1.57), (0.12, 0.1, 0.07), m.skin, c, uv=0.4))
    p.append(uv_sphere("eye_l", (-0.045, -0.105, 1.68), 0.018, m.slate, c, segs=8, rings=6, uv=0.3))
    p.append(uv_sphere("eye_r", (0.045, -0.105, 1.68), 0.018, m.slate, c, segs=8, rings=6, uv=0.3))
    p.append(uv_sphere("ear_l", (-0.13, 0.02, 1.66), 0.035, m.skin, c, segs=8, rings=6, uv=0.3))
    p.append(uv_sphere("ear_r", (0.13, 0.02, 1.66), 0.035, m.skin, c, segs=8, rings=6, uv=0.3))
    p.append(uv_sphere("sh_l", (-0.26, 0, 1.42), 0.11, cloth, c, segs=12, rings=8, uv=0.6))
    p.append(uv_sphere("sh_r", (0.26, 0, 1.42), 0.11, cloth, c, segs=12, rings=8, uv=0.6))
    p.append(uv_sphere("knee_l", (-0.12, 0.02, 0.44), 0.075, m.leather, c, segs=10, rings=8, uv=0.4))
    p.append(uv_sphere("knee_r", (0.12, 0.02, 0.44), 0.075, m.leather, c, segs=10, rings=8, uv=0.4))
    p.append(cyl("helm", (0, 0.0, 1.78), 0.145, 0.14, m.iron, c, verts=12, uv=1))
    p.append(cyl("brim", (0, -0.02, 1.71), 0.20, 0.035, m.iron, c, verts=14, uv=1))
    p.append(cube("nasal", (0, -0.14, 1.66), (0.035, 0.03, 0.12), m.iron, c, uv=0.4))
    p.append(cube("cloak", (0, 0.18, 1.16), (0.38, 0.05, 0.78), accent, c, uv=3.0))
    p.append(_limb(c, "thigh_l", (-0.12, 0.0, 0.64), 0.085, 0.44, m.leather))
    p.append(_limb(c, "thigh_r", (0.12, 0.0, 0.64), 0.085, 0.44, m.leather))
    p.append(_limb(c, "calf_l", (-0.12, 0.03, 0.28), 0.072, 0.36, m.leather))
    p.append(_limb(c, "calf_r", (0.12, 0.03, 0.28), 0.072, 0.36, m.leather))
    p.append(cube("boot_l", (-0.12, 0.08, 0.07), (0.17, 0.30, 0.11), m.leather, c, uv=0.8))
    p.append(cube("boot_r", (0.12, 0.08, 0.07), (0.17, 0.30, 0.11), m.leather, c, uv=0.8))
    p.append(_limb(c, "uarm_l", (-0.34, 0.0, 1.30), 0.07, 0.36, cloth, rot=(0, math.radians(16), 0)))
    p.append(_limb(c, "uarm_r", (0.34, 0.0, 1.30), 0.07, 0.36, cloth, rot=(0, math.radians(-16), 0)))
    p.append(_limb(c, "larm_l", (-0.46, 0.05, 1.02), 0.06, 0.32, m.skin, rot=(0, math.radians(10), 0)))
    p.append(_limb(c, "larm_r", (0.46, 0.05, 1.02), 0.06, 0.32, m.skin, rot=(0, math.radians(-10), 0)))
    p.append(cube("hand_l", (-0.52, 0.07, 0.84), (0.10, 0.10, 0.12), m.skin, c, uv=0.5))
    p.append(cube("hand_r", (0.52, 0.07, 0.84), (0.10, 0.10, 0.12), m.skin, c, uv=0.5))
    for i, off in enumerate((-0.035, -0.012, 0.012, 0.035)):
        p.append(cube(f"fl{i}", (-0.52 + off, 0.14, 0.84), (0.028, 0.09, 0.028), m.skin, c, uv=0.2))
        p.append(cube(f"fr{i}", (0.52 + off, 0.14, 0.84), (0.028, 0.09, 0.028), m.skin, c, uv=0.2))
    if kit == "levy":
        p.append(cyl("shaft", (0.62, -0.04, 1.2), 0.028, 2.05, m.wood, c, verts=10, uv=1))
        p.append(cone("blade", (0.62, -0.04, 2.26), 0.055, 0.26, m.iron, c, verts=8, uv=0.5))
        p.append(cube("blade_w", (0.62, -0.04, 2.14), (0.12, 0.02, 0.08), m.iron, c, uv=0.4))
        p.append(cyl("collar", (0.62, -0.04, 2.08), 0.04, 0.05, m.gold, c, verts=8, uv=0.4))
        p.append(cyl("shield", (-0.68, 0.18, 1.08), 0.40, 0.09, m.wood, c, verts=14, rot=(math.radians(82), 0, math.radians(16)), uv=1.4))
        p.append(cyl("boss", (-0.64, 0.24, 1.08), 0.10, 0.12, m.gold, c, verts=10, rot=(math.radians(82), 0, math.radians(16)), uv=0.6))
        p.append(cube("cross_h", (-0.64, 0.22, 1.08), (0.55, 0.05, 0.08), m.gold, c, uv=0.6))
        p.append(cube("cross_v", (-0.64, 0.22, 1.08), (0.08, 0.05, 0.55), m.gold, c, uv=0.6))
        p.append(cyl("rim", (-0.68, 0.18, 1.08), 0.41, 0.04, m.iron, c, verts=14, rot=(math.radians(82), 0, math.radians(16)), uv=1))
    elif kit == "builder":
        p.append(cyl("haft", (0.72, 0.0, 0.95), 0.035, 0.9, m.wood, c, verts=8, rot=(0, math.radians(90), 0), uv=1))
        p.append(cube("head_h", (1.12, 0.0, 0.95), (0.18, 0.12, 0.38), m.iron, c, uv=0.7))
    elif kit == "archer":
        p.append(cyl("bow_u", (0.05, -0.42, 1.45), 0.03, 0.95, m.wood, c, verts=8, uv=0.8))
        p.append(cyl("bow_d", (0.05, -0.42, 0.55), 0.03, 0.85, m.wood, c, verts=8, uv=0.8))
        p.append(cyl("string", (0.05, -0.18, 1.0), 0.012, 1.7, m.leather, c, verts=6, uv=0.5))
        p.append(cube("quiver", (-0.28, 0.22, 1.2), (0.12, 0.12, 0.55), m.leather, c, uv=0.8))
    elif kit == "mage":
        p.append(cyl("staff", (0.58, -0.02, 1.15), 0.035, 2.0, m.wood, c, verts=10, uv=1))
        p.append(uv_sphere("orb", (0.58, -0.02, 2.2), 0.12, m.crystal, c, segs=12, rings=8, uv=0.8))
        p.append(cone("hat", (0, 0.0, 1.98), 0.22, 0.55, cloth, c, verts=10, uv=1))
        p.append(cyl("hat_brim", (0, 0.0, 1.74), 0.28, 0.04, cloth, c, verts=12, uv=1))
    elif kit == "leader":
        p.append(cyl("crown", (0, 0.0, 1.88), 0.16, 0.1, m.gold, c, verts=12, uv=0.8))
        for i in range(5):
            ang = i * (2 * math.pi / 5)
            p.append(cube(f"spike_{i}", (math.sin(ang) * 0.14, -math.cos(ang) * 0.14, 1.98), (0.05, 0.05, 0.16), m.gold, c, uv=0.3))
        p.append(cyl("shaft", (0.62, -0.04, 1.15), 0.03, 1.7, m.wood, c, verts=8, uv=1))
        p.append(cube("axe", (0.62, -0.04, 1.95), (0.08, 0.04, 0.35), m.iron, c, uv=0.5))
    elif kit == "ember":
        p.append(cube("pauldron_l", (-0.38, 0.0, 1.42), (0.22, 0.22, 0.16), m.iron, c, uv=0.7))
        p.append(cube("pauldron_r", (0.38, 0.0, 1.42), (0.22, 0.22, 0.16), m.iron, c, uv=0.7))
        p.append(cone("crest", (0, 0.0, 1.96), 0.1, 0.28, m.crystal, c, verts=8, uv=0.5))
    elif kit == "dryad":
        p.append(ico("canopy", (0, 0.02, 1.92), 0.32, m.leaf, c, subdiv=2, scale=(1.2, 1.1, 0.7), uv=1))
        p.append(cyl("spear", (0.55, -0.02, 1.2), 0.025, 1.9, m.wood, c, verts=8, uv=1))
    mesh = join(name, p, c)
    bevel(mesh, width=0.014, segments=2)
    unwrap_smart(mesh)
    return mesh


def build_peasant_rigged(m):
    c = coll("01_Units/MundorCrown")
    mesh = build_humanoid("unit_royal_peasant", c, m, "levy")
    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    arm_ob = bpy.context.active_object
    arm_ob.name = "rig_royal_peasant"
    arm = arm_ob.data
    bpy.ops.armature.select_all(action="SELECT")
    bpy.ops.armature.delete()

    def bone(name, head, tail, parent=None):
        b = arm.edit_bones.new(name)
        b.head = Vector(head)
        b.tail = Vector(tail)
        if parent:
            b.parent = arm.edit_bones[parent]
        return b

    bone("Hips", (0, 0, 0.94), (0, 0, 1.1))
    bone("Spine", (0, 0, 1.1), (0, 0.02, 1.26), "Hips")
    bone("Chest", (0, 0.02, 1.26), (0, 0.02, 1.46), "Spine")
    bone("Neck", (0, 0.02, 1.46), (0, 0.02, 1.56), "Chest")
    bone("Head", (0, 0.02, 1.56), (0, 0.04, 1.88), "Neck")
    bone("LeftShoulder", (0, 0.02, 1.42), (-0.2, 0.02, 1.4), "Chest")
    bone("LeftUpperArm", (-0.2, 0.02, 1.4), (-0.4, 0.04, 1.16), "LeftShoulder")
    bone("LeftLowerArm", (-0.4, 0.04, 1.16), (-0.52, 0.07, 0.88), "LeftUpperArm")
    bone("LeftHand", (-0.52, 0.07, 0.88), (-0.56, 0.1, 0.78), "LeftLowerArm")
    bone("RightShoulder", (0, 0.02, 1.42), (0.2, 0.02, 1.4), "Chest")
    bone("RightUpperArm", (0.2, 0.02, 1.4), (0.4, 0.04, 1.16), "RightShoulder")
    bone("RightLowerArm", (0.4, 0.04, 1.16), (0.52, 0.07, 0.88), "RightUpperArm")
    bone("RightHand", (0.52, 0.07, 0.88), (0.56, 0.1, 0.78), "RightLowerArm")
    bone("LeftUpperLeg", (-0.12, 0, 0.94), (-0.12, 0.0, 0.5), "Hips")
    bone("LeftLowerLeg", (-0.12, 0.0, 0.5), (-0.12, 0.05, 0.12), "LeftUpperLeg")
    bone("LeftFoot", (-0.12, 0.05, 0.12), (-0.12, 0.2, 0.02), "LeftLowerLeg")
    bone("RightUpperLeg", (0.12, 0, 0.94), (0.12, 0.0, 0.5), "Hips")
    bone("RightLowerLeg", (0.12, 0.0, 0.5), (0.12, 0.05, 0.12), "RightUpperLeg")
    bone("RightFoot", (0.12, 0.05, 0.12), (0.12, 0.2, 0.02), "RightLowerLeg")
    bpy.ops.object.mode_set(mode="OBJECT")
    move_to(arm_ob, c)
    arm_ob.show_in_front = True
    bpy.ops.object.select_all(action="DESELECT")
    mesh.select_set(True)
    arm_ob.select_set(True)
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    mesh["definition_id"] = "unit_royal_peasant"
    return arm_ob, mesh


def build_tree(m):
    c = coll("03_World/Trees")
    p = []
    p.append(cone("trunk", (0, 0, 2.4), 0.72, 4.8, m.bark, c, verts=12, uv=2))
    p.append(cyl("trunk2", (0.2, -0.15, 5.1), 0.38, 2.6, m.bark, c, verts=10, rot=(math.radians(14), 0, math.radians(20)), uv=1.5))
    p.append(cyl("trunk3", (-0.15, 0.1, 6.6), 0.22, 1.8, m.bark, c, verts=8, rot=(math.radians(-18), 0, math.radians(-10)), uv=1.2))
    for i, (loc, rot, r, d) in enumerate((
        ((-1.15, 0.25, 4.6), (math.radians(72), 0, math.radians(40)), 0.16, 2.4),
        ((1.2, -0.2, 4.9), (math.radians(68), 0, math.radians(-48)), 0.15, 2.2),
        ((0.15, 1.1, 5.4), (math.radians(78), math.radians(10), 0), 0.12, 1.8),
        ((-0.4, -1.0, 5.8), (math.radians(60), 0, math.radians(160)), 0.11, 1.7),
        ((0.9, 0.8, 6.5), (math.radians(50), 0, math.radians(-20)), 0.1, 1.5),
    )):
        p.append(cyl(f"br_{i}", loc, r, d, m.bark, c, verts=8, rot=rot, uv=1))
    for i, (loc, rot, r, d) in enumerate((
        ((0.7, 0.15, 0.28), (math.radians(78), 0, math.radians(25)), 0.18, 1.3),
        ((-0.65, -0.2, 0.26), (math.radians(80), 0, math.radians(200)), 0.16, 1.2),
        ((0.05, 0.75, 0.22), (math.radians(82), 0, math.radians(95)), 0.14, 1.05),
        ((0.15, -0.7, 0.2), (math.radians(82), 0, math.radians(-90)), 0.13, 0.95),
    )):
        p.append(cyl(f"root_{i}", loc, r, d, m.bark, c, verts=8, rot=rot, uv=1))
    canopies = [
        ((0.15, -0.1, 7.15), 2.05, m.leaf, (1.25, 1.15, 0.78)),
        ((-1.85, 0.55, 5.85), 1.35, m.leaf_d, (1.3, 1.1, 0.7)),
        ((1.95, -0.55, 6.05), 1.28, m.leaf, (1.2, 1.15, 0.68)),
        ((0.2, 0.2, 8.55), 1.15, m.leaf_d, (1.15, 1.1, 0.62)),
        ((0.35, -1.65, 6.35), 1.18, m.leaf, (1.05, 1.2, 0.68)),
        ((-0.9, -0.9, 6.9), 0.95, m.leaf_d, (1.1, 1.0, 0.65)),
        ((1.1, 1.15, 7.2), 0.9, m.leaf, (1.05, 1.05, 0.6)),
    ]
    for i, (loc, r, mat, sc) in enumerate(canopies):
        p.append(ico(f"can_{i}", loc, r, mat, c, subdiv=3, scale=sc, uv=2))
    tree = join("prop_tree", p, c)
    bevel(tree, width=0.035, segments=2)
    return tree


def build_producer(m):
    c = coll("02_Buildings/MundorCrown")
    p = []
    p.append(cube("hall", (0, 0, 1.4), (6.4, 5.0, 2.8), m.plaster, c, uv=3))
    p.append(cube("roof_a", (0, 1.1, 3.35), (6.8, 3.2, 0.22), m.slate, c, rot=(math.radians(28), 0, 0), uv=2))
    p.append(cube("roof_b", (0, -1.1, 3.35), (6.8, 3.2, 0.22), m.slate, c, rot=(math.radians(-28), 0, 0), uv=2))
    p.append(cyl("chim_l", (-2.5, -1.6, 2.6), 0.35, 3.4, m.brick, c, verts=10, uv=1.5))
    p.append(cyl("chim_r", (2.5, -1.6, 2.6), 0.35, 3.4, m.brick, c, verts=10, uv=1.5))
    p.append(cube("door", (0, 2.55, 1.15), (1.6, 0.2, 2.1), m.wood, c, uv=1))
    p.append(cube("frame", (0, 2.55, 1.2), (1.9, 0.16, 2.4), m.wood, c, uv=1))
    for x in (-1.8, 1.8):
        p.append(cube(f"win_{x}", (x, 2.52, 1.7), (0.9, 0.16, 1.1), m.slate, c, uv=0.8))
    ob = join("building_producer", p, c)
    bevel(ob, 0.04, 2)
    return ob


def build_tower(m):
    c = coll("02_Buildings/MundorCrown")
    p = []
    p.append(cube("base", (0, 0, 0.9), (3.0, 3.0, 1.8), m.brick, c, uv=2))
    p.append(cyl("shaft", (0, 0, 6.2), 0.95, 8.8, m.plaster, c, verts=12, uv=2.5))
    p.append(cube("top", (0, 0, 10.85), (2.9, 2.9, 1.1), m.slate, c, uv=2))
    p.append(cone("roof", (0, 0, 12.3), 1.5, 2.0, m.slate, c, verts=12, uv=1.5))
    for i in range(4):
        ang = i * math.pi * 0.5
        p.append(cube(f"slit_{i}", (math.sin(ang) * 0.95, math.cos(ang) * 0.95, 7.4), (0.22, 0.22, 1.15), m.slate, c, uv=0.5))
    for i in range(-1, 2):
        p.append(cube(f"mer_{i}", (i * 0.85, 1.4, 11.5), (0.5, 0.4, 0.7), m.slate, c, uv=0.5))
    ob = join("building_tower", p, c)
    bevel(ob, 0.04, 2)
    return ob


def build_turret(m):
    c = coll("02_Buildings/MundorCrown")
    p = [
        cube("base", (0, 0, 0.6), (2.3, 2.3, 1.2), m.brick, c, uv=1.5),
        cyl("shaft", (0, 0, 2.9), 0.72, 3.6, m.plaster, c, verts=12, uv=1.5),
        cube("cap", (0, 0, 4.85), (2.05, 2.05, 0.55), m.slate, c, uv=1.5),
        cyl("gun", (0.85, 0, 5.05), 0.12, 1.5, m.iron, c, verts=8, rot=(0, math.radians(90), 0), uv=0.8),
        cone("roof", (0, 0, 5.55), 1.05, 1.1, m.slate, c, verts=10, uv=1),
    ]
    ob = join("building_turret", p, c)
    bevel(ob, 0.03, 2)
    return ob


def build_wall(m):
    c = coll("02_Buildings/MundorCrown")
    p = [cube("body", (0, 0, 1.8), (11.0, 1.5, 3.6), m.brick, c, uv=4)]
    for i, x in enumerate((-4.6, -2.3, 0, 2.3, 4.6)):
        p.append(cube(f"mer_{i}", (x, 0, 3.85), (0.85, 1.55, 1.15), m.slate, c, uv=1))
    p.append(cube("gate", (0, 0.85, 1.3), (2.1, 0.2, 2.2), m.wood, c, uv=1.2))
    ob = join("building_wall", p, c)
    bevel(ob, 0.04, 2)
    return ob


def build_outpost(m):
    c = coll("02_Buildings/MundorCrown")
    p = [
        cube("hut", (0, 0, 1.0), (4.1, 4.1, 2.0), m.plaster, c, uv=2.5),
        cube("roof_a", (0, 0.9, 2.45), (4.4, 2.6, 0.2), m.wood, c, rot=(math.radians(26), 0, 0), uv=2),
        cube("roof_b", (0, -0.9, 2.45), (4.4, 2.6, 0.2), m.wood, c, rot=(math.radians(-26), 0, 0), uv=2),
        cyl("pole", (0, 0, 4.4), 0.12, 3.2, m.wood, c, verts=8, uv=1),
        cube("flag", (0.7, 0, 5.7), (1.4, 0.05, 0.7), m.cloth, c, uv=1),
        cube("door", (0, 2.1, 0.9), (1.1, 0.16, 1.6), m.wood, c, uv=1),
        cube("crate", (-1.5, 1.6, 0.35), (0.7, 0.7, 0.7), m.wood, c, uv=1),
    ]
    ob = join("building_outpost", p, c)
    bevel(ob, 0.035, 2)
    return ob


def build_keep_generic(m):
    """Shared keep silhouette (non-Mundor slot)."""
    c = coll("02_Buildings/MundorCrown")
    p = [
        cube("base", (0, 0, 1.3), (8.2, 8.2, 2.6), m.brick, c, uv=3),
        cube("mid", (0, 0, 5.6), (5.4, 5.4, 6.0), m.plaster, c, uv=2.5),
        cube("top", (0, 0, 9.6), (2.8, 2.8, 2.2), m.slate, c, uv=1.5),
        cone("roof", (0, 0, 11.5), 1.7, 2.2, m.slate, c, verts=12, uv=1.5),
        cube("gate", (0, 4.2, 1.5), (2.6, 1.2, 2.8), m.wood, c, uv=1.5),
    ]
    for i, (x, y) in enumerate(((3.4, 3.4), (3.4, -3.4), (-3.4, 3.4), (-3.4, -3.4))):
        p.append(cube(f"tur_{i}", (x, y, 9.2), (1.5, 1.5, 2.0), m.brick, c, uv=1))
        p.append(cone(f"tr_{i}", (x, y, 10.55), 0.95, 1.4, m.slate, c, verts=8, uv=1))
    ob = join("building_keep", p, c)
    bevel(ob, 0.05, 2)
    return ob


def build_cavalry(m):
    c = coll("01_Units/MundorCrown")
    p = [
        cube("body", (0, 0.05, 0.72), (1.55, 0.5, 0.55), m.leather, c, uv=1.4),
        uv_sphere("chest", (0.35, 0.05, 0.78), 0.28, m.leather, c, segs=12, rings=8, uv=1),
        uv_sphere("rump", (-0.5, 0.05, 0.78), 0.26, m.leather, c, segs=12, rings=8, uv=1),
        cyl("neck", (0.62, 0.05, 1.05), 0.12, 0.45, m.leather, c, verts=10, rot=(0, math.radians(-55), 0), uv=0.8),
        uv_sphere("head", (0.85, 0.05, 1.22), 0.16, m.leather, c, segs=12, rings=8, uv=0.8),
        cube("rider", (-0.05, 0.05, 1.25), (0.4, 0.32, 0.55), m.cloth, c, uv=1),
        uv_sphere("rhead", (-0.05, 0.05, 1.62), 0.12, m.skin, c, segs=10, rings=8, uv=0.6),
        cyl("helm", (-0.05, 0.05, 1.72), 0.13, 0.1, m.iron, c, verts=10, uv=0.6),
    ]
    for i, x in enumerate((-0.45, 0.4)):
        for j, y in enumerate((-0.18, 0.18)):
            p.append(cyl(f"leg_{i}{j}", (x, y, 0.32), 0.08, 0.5, m.leather, c, verts=8, uv=0.7))
            p.append(cube(f"hoof_{i}{j}", (x, y, 0.06), (0.12, 0.1, 0.08), m.iron, c, uv=0.4))
    ob = join("unit_cavalry", p, c)
    bevel(ob, 0.016, 2)
    return ob


def build_siege(m):
    c = coll("01_Units/MundorCrown")
    p = [
        cube("bed", (0, 0, 0.55), (1.7, 1.05, 0.28), m.wood, c, uv=1.5),
        cube("side_l", (0, 0.48, 0.85), (1.5, 0.1, 0.55), m.wood, c, uv=1.2),
        cube("side_r", (0, -0.48, 0.85), (1.5, 0.1, 0.55), m.wood, c, uv=1.2),
        cube("arm", (0.15, 0, 1.25), (0.14, 0.14, 1.15), m.wood, c, uv=1),
        cube("spoon", (0.15, 0, 1.85), (0.45, 0.28, 0.12), m.iron, c, uv=0.7),
        cube("frame", (-0.35, 0, 1.15), (0.16, 0.85, 0.85), m.wood, c, uv=1),
    ]
    for i, (x, y) in enumerate(((-0.55, 0.5), (-0.55, -0.5), (0.55, 0.5), (0.55, -0.5))):
        p.append(cyl(f"wheel_{i}", (x, y, 0.28), 0.28, 0.14, m.wood, c, verts=12, rot=(math.radians(90), 0, 0), uv=1))
        p.append(cyl(f"hub_{i}", (x, y, 0.28), 0.08, 0.18, m.iron, c, verts=8, rot=(math.radians(90), 0, 0), uv=0.5))
    ob = join("unit_siege", p, c)
    bevel(ob, 0.018, 2)
    return ob


def build_gold(m):
    c = coll("03_World/Resources")
    p = []
    for i, (loc, sc) in enumerate((
        ((0, 0, 0.45), (1.2, 1.0, 0.9)),
        ((0.45, 0.2, 1.05), (0.7, 0.6, 1.1)),
        ((-0.4, -0.15, 0.9), (0.55, 0.5, 0.9)),
        ((0.1, 0.05, 1.7), (0.4, 0.35, 0.7)),
        ((-0.1, 0.15, 2.15), (0.25, 0.22, 0.45)),
    )):
        p.append(ico(f"g_{i}", loc, 0.45, m.crystal, c, subdiv=2, scale=sc, uv=1))
    ob = join("resource_gold", p, c)
    bevel(ob, 0.02, 1)
    return ob


def build_timber(m):
    c = coll("03_World/Resources")
    p = [
        cyl("log_a", (0, 0, 0.4), 0.32, 2.5, m.bark, c, verts=12, rot=(0, math.radians(90), 0), uv=1.5),
        cyl("log_b", (0.1, 0.25, 0.85), 0.26, 2.1, m.bark, c, verts=12, rot=(0, math.radians(90), math.radians(8)), uv=1.4),
        cyl("log_c", (-0.15, -0.2, 0.95), 0.22, 1.7, m.bark, c, verts=10, rot=(0, math.radians(90), math.radians(-6)), uv=1.2),
        cyl("end_a", (-1.25, 0, 0.4), 0.32, 0.08, m.wood, c, verts=12, rot=(0, math.radians(90), 0), uv=0.8),
        cyl("end_b", (1.25, 0, 0.4), 0.32, 0.08, m.wood, c, verts=12, rot=(0, math.radians(90), 0), uv=0.8),
    ]
    ob = join("resource_timber", p, c)
    bevel(ob, 0.02, 2)
    return ob


def build_rock(m):
    c = coll("03_World/Rocks")
    p = [
        ico("a", (0, 0, 0.45), 0.7, m.slate, c, subdiv=2, scale=(1.3, 1.0, 0.7), uv=1.5),
        ico("b", (0.45, 0.2, 0.35), 0.45, m.brick, c, subdiv=2, scale=(1.1, 0.9, 0.65), uv=1.2),
        ico("c", (-0.4, -0.15, 0.28), 0.38, m.slate, c, subdiv=2, scale=(1.0, 1.1, 0.55), uv=1),
    ]
    ob = join("prop_rock", p, c)
    bevel(ob, 0.03, 2)
    return ob


def build_bridge(m):
    c = coll("03_World/Bridges")
    p = [cube("deck", (0, 0, 0.55), (6.5, 1.8, 0.22), m.wood, c, uv=3)]
    for i in range(-2, 3):
        p.append(cube(f"plank_{i}", (i * 1.15, 0, 0.62), (1.05, 1.85, 0.08), m.wood, c, uv=1))
    for y in (-0.85, 0.85):
        p.append(cube(f"rail_{y}", (0, y, 1.05), (6.5, 0.12, 0.7), m.wood, c, uv=2))
    for x in (-2.8, 2.8):
        p.append(cyl(f"post_{x}", (x, 0.85, 0.7), 0.1, 1.2, m.wood, c, verts=8, uv=0.8))
        p.append(cyl(f"post2_{x}", (x, -0.85, 0.7), 0.1, 1.2, m.wood, c, verts=8, uv=0.8))
    ob = join("prop_bridge", p, c)
    bevel(ob, 0.025, 2)
    return ob


def unwrap_smart(ob):
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.04)
    bpy.ops.object.mode_set(mode="OBJECT")


def hide_set(objects, hide):
    for ob in objects:
        if ob is None:
            continue
        ob.hide_render = hide
        ob.hide_viewport = hide
        for ch in getattr(ob, "children_recursive", []):
            ch.hide_render = hide
            ch.hide_viewport = hide


def render_cam(cam, filename):
    scene = bpy.context.scene
    scene.camera = cam
    scene.render.filepath = str(RENDER / filename)
    bpy.ops.render.render(write_still=True)
    print("rendered", filename)


def export_obj(ob, dest: Path):
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    dest.parent.mkdir(parents=True, exist_ok=True)
    kwargs = dict(
        filepath=str(dest),
        export_selected_objects=True,
        export_materials=False,
        export_triangulated_mesh=True,
        forward_axis="NEGATIVE_Z",
        up_axis="Y",
    )
    try:
        bpy.ops.wm.obj_export(export_colors="SRGB", **kwargs)
    except TypeError:
        try:
            bpy.ops.wm.obj_export(export_vertex_colors=True, **kwargs)
        except TypeError:
            bpy.ops.wm.obj_export(**kwargs)


def export_fbx(ob, dest: Path, armature=None):
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    if armature is not None:
        armature.select_set(True)
        bpy.context.view_layer.objects.active = armature
    else:
        bpy.context.view_layer.objects.active = ob
    dest.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(dest),
        use_selection=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_space_transform=True,
    )


def _identity_export(root, write_fn):
    loc = root.location.copy()
    rot = root.rotation_euler.copy()
    sc = root.scale.copy()
    root.location = (0.0, 0.0, 0.0)
    root.rotation_euler = (0.0, 0.0, 0.0)
    root.scale = (1.0, 1.0, 1.0)
    bpy.context.view_layer.update()
    try:
        write_fn()
    finally:
        root.location = loc
        root.rotation_euler = rot
        root.scale = sc


def copy_textures_to_unity():
    out_dir(UNITY_TEX)
    for src in TEX.glob("*.png"):
        shutil.copy2(src, UNITY_TEX / src.name)
        print("tex copy", src.name)


def export_game_mesh(ob, game_key, armature=None):
    print("export", game_key)
    root = armature if armature is not None else ob

    def write():
        export_obj(ob, UNITY_MESH / f"{game_key}.obj")
        export_obj(ob, EXPORT / f"{game_key}.obj")
        export_fbx(ob, UNITY_MESH / f"{game_key}.fbx", armature)
        export_fbx(ob, EXPORT / f"{game_key}.fbx", armature)

    _identity_export(root, write)


def setup_collections():
    for f in ("Uncrowned", "MundorCrown", "OutcastHost", "Freetown", "UniversityGuild", "RisingSun"):
        coll(f"01_Units/{f}")
        coll(f"02_Buildings/{f}")
    coll("03_World/Trees")
    coll("03_World/Rocks")
    coll("03_World/Resources")
    coll("03_World/Bridges")
    coll("00_StyleLock")
    coll("06_CamerasLights")


def main():
    out_dir(ART)
    out_dir(RENDER)
    out_dir(EXPORT)
    out_dir(TEX)
    clear_scene()
    setup_collections()
    setup_world()
    print("generating PBR maps…")
    images = generate_textures()
    m = make_materials(images)
    ground = setup_lights(m)

    citadel = build_citadel(m)
    arm, peasant = build_peasant_rigged(m)
    tree = build_tree(m)
    producer = build_producer(m)
    tower = build_tower(m)
    turret = build_turret(m)
    wall = build_wall(m)
    outpost = build_outpost(m)
    keep = build_keep_generic(m)
    militia = build_humanoid("unit_militia", coll("01_Units/MundorCrown"), m, "levy")
    builder = build_humanoid("unit_builder", coll("01_Units/MundorCrown"), m, "builder")
    archer = build_humanoid("unit_archer", coll("01_Units/MundorCrown"), m, "archer")
    mage = build_humanoid("unit_mage", coll("01_Units/MundorCrown"), m, "mage")
    leader = build_humanoid("unit_leader", coll("01_Units/MundorCrown"), m, "leader")
    ember = build_humanoid("unit_ember_raider", coll("01_Units/MundorCrown"), m, "ember")
    dryad = build_humanoid("unit_dryad", coll("01_Units/MundorCrown"), m, "dryad")
    cavalry = build_cavalry(m)
    siege = build_siege(m)
    gold = build_gold(m)
    timber = build_timber(m)
    rock = build_rock(m)
    bridge = build_bridge(m)

    # Layout: citadel at origin, peasant foreground, tree west,
    # buildings east, units south, props north.
    arm.location = (8.6, -13.5, 0)
    arm.rotation_euler = (0, 0, math.radians(-38))
    tree.location = (-13.5, 7.0, 0)
    producer.location = (28, 0, 0)
    tower.location = (40, 0, 0)
    turret.location = (48, 0, 0)
    wall.location = (58, 0, 0)
    outpost.location = (70, 0, 0)
    keep.location = (82, 0, 0)
    units = [militia, builder, archer, mage, leader, ember, dryad, cavalry, siege]
    for i, u in enumerate(units):
        u.location = (i * 3.6 - 8, -30, 0)
        u.rotation_euler = (0, 0, 0)
    gold.location = (-6, 22, 0)
    timber.location = (2, 22, 0)
    rock.location = (9, 22, 0)
    bridge.location = (18, 22, 0)

    global SKIP_PRIMITIVE_UV
    SKIP_PRIMITIVE_UV = True
    print("generating unique faction roster…")
    roster = asterra_roster.build_all(sys.modules[__name__], m)
    SKIP_PRIMITIVE_UV = False
    for i, k in enumerate(roster["keeps"]):
        k.location = (i * 26 - 52, 62, 0)
    for i, b in enumerate(roster["buildings"]):
        b.location = ((i % 12) * 14 - 70, 88 + (i // 12) * 16, 0)
    for i, u in enumerate(roster["units"]):
        u.location = ((i % 16) * 3.4 - 24, -52 - (i // 16) * 4.2, 0)

    all_assets = [
        citadel, arm, peasant, tree, producer, tower, turret, wall, outpost, keep,
        militia, builder, archer, mage, leader, ember, dryad, cavalry, siege,
        gold, timber, rock, bridge, ground,
        *roster["keeps"], *roster["buildings"], *roster["units"],
    ]

    cam_lineup = add_camera("cam_lineup", (20, -24, 13), (1, -3, 5), lens=35)
    cam_rts = add_camera("cam_rts", (0, 0, 44), (0, 0, 0), ortho=True, ortho_scale=38)
    cam_keep = add_camera("cam_keep", (17, -19, 8.5), (0, 2, 5.5), lens=40)
    cam_gate = add_camera("cam_gate", (0.2, 16.5, 3.4), (0, 9.1, 2.4), lens=32)
    cam_peasant = add_camera("cam_peasant", (10.4, -16.4, 1.5), (8.6, -13.5, 1.12), lens=50)
    cam_portrait = add_camera("cam_portrait", (8.6, -15.6, 1.72), (8.6, -13.5, 1.52), lens=55)
    cam_tree = add_camera("cam_tree", (-5.5, -2.2, 5.2), (-13.5, 7.0, 4.2), lens=40)
    cam_units = add_camera("cam_units", (4, -70, 9), (4, -56, 1.2), lens=28)
    cam_blds = add_camera("cam_buildings", (14, 48, 36), (14, 104, 5), lens=28)
    cam_props = add_camera("cam_props", (8, 12, 8), (8, 22, 1), lens=35)

    render_cam(cam_lineup, "01_style_lock_lineup.png")
    render_cam(cam_rts, "02_style_lock_rts_top.png")
    hide_set([arm, peasant, tree] + units, True)
    render_cam(cam_keep, "03_citadel.png")
    render_cam(cam_gate, "03b_citadel_gate.png")
    hide_set([citadel, tree] + units, True)
    hide_set([arm, peasant, ground], False)
    render_cam(cam_peasant, "04_peasant.png")
    render_cam(cam_portrait, "05_peasant_portrait.png")
    hide_set([arm, peasant, citadel], True)
    hide_set([tree, ground], False)
    render_cam(cam_tree, "06_tree.png")
    hide_set(all_assets, False)
    hide_set([citadel, arm, peasant, tree, producer, tower, turret, wall, outpost, keep, gold, timber, rock, bridge], True)
    hide_set(units, True)
    hide_set(roster["units"] + [ground], False)
    render_cam(cam_units, "07_units.png")
    hide_set(roster["units"], True)
    hide_set(roster["buildings"] + [ground], False)
    render_cam(cam_blds, "08_buildings.png")
    hide_set(roster["buildings"], True)
    hide_set([gold, timber, rock, bridge, ground], False)
    render_cam(cam_props, "09_props.png")

    def by_id(objs, key):
        for o in objs:
            if o.get("definition_id") == key:
                return o
        return None

    hide_set(all_assets, True)
    keep_row = [citadel] + roster["keeps"]
    saved_keep = [k.location.copy() for k in keep_row]
    for i, k in enumerate(keep_row):
        k.location = ((i - 2.5) * 30.0, 0.0, 0.0)
        k.hide_render = False
        k.hide_viewport = False
    ground.hide_render = False
    ground.hide_viewport = False
    cam_factions = add_camera("cam_faction_keeps", (0, -52, 26), (0, 0, 5), ortho=True, ortho_scale=175)
    render_cam(cam_factions, "10_faction_keeps.png")
    for k, loc in zip(keep_row, saved_keep):
        k.location = loc

    close = [
        ("12_arcaneum.png", "building_arcaneum", (16, -18, 9), (0, 0, 6)),
        ("13_great_camp.png", "building_outcast_great_camp", (16, -18, 8), (0, 0, 4)),
        ("14_tavern.png", "building_freetown_tavern", (16, -18, 8), (0, 0, 4)),
        ("15_college.png", "building_university_grand_college", (18, -20, 10), (2, 0, 6)),
        ("16_temple.png", "building_church_grand_temple", (16, -22, 9), (0, 0, 6)),
        ("17_spider.png", "unit_university_mechanical_spider", (3.6, -7.2, 2.1), (0.0, 0.05, 0.75)),
        ("18_giant.png", "unit_outcast_frost_giant", (4.5, -5.2, 2.4), (0, 0, 1.8)),
        ("19_crab.png", "unit_freetown_warrior_crab", (2.6, -2.8, 1.3), (0, 0, 0.5)),
        ("20_golem.png", "unit_veiled_golem", (3.0, -3.4, 1.8), (0, 0, 1.3)),
        ("21_airship.png", "unit_university_airship", (4.0, -4.5, 2.4), (0, 0, 1.6)),
        ("22_elemental.png", "unit_veiled_elemental", (2.6, -2.8, 1.6), (0, 0, 1.1)),
        ("23_solar_engine.png", "unit_church_solar_engine", (3.2, -3.6, 1.8), (0, 0, 1.1)),
        ("24_bridge.png", "building_bridge", (10, -8, 5), (0, 0, 1.4)),
        ("25_conservatory.png", "building_blackroot_conservatory", (14, -16, 8), (0, 0, 4)),
        ("26_aerie.png", "building_outcast_aerie", (14, -16, 10), (0, 0, 5)),
        ("27_observatory.png", "building_university_grand_observatory", (16, -18, 10), (0, 0, 5)),
        ("28_barracks.png", "building_royal_barracks", (14, -16, 8), (0, 0, 4)),
        ("29_legion.png", "unit_royal_legion", (3.2, -5.2, 1.8), (0, 0, 1.1)),
        ("30_privateer.png", "unit_freetown_privateer", (3.2, -5.2, 1.8), (0, 0, 1.1)),
    ]
    pool = roster["keeps"] + roster["buildings"] + roster["units"]
    for filename, key, loc, target in close:
        ob = by_id(pool, key)
        if ob is None:
            continue
        hide_set(all_assets, True)
        hide_set([ob, ground], False)
        saved = ob.location.copy()
        ob.location = (0, 0, 0)
        cam = add_camera(f"cam_{key}", loc, target, lens=35)
        render_cam(cam, filename)
        ob.location = saved
    hide_set(all_assets, False)

    copy_textures_to_unity()
    export_game_mesh(citadel, "building_keep")
    export_game_mesh(citadel, "building_royal_citadel")
    export_game_mesh(peasant, "unit_militia", arm)
    export_game_mesh(peasant, "unit_royal_peasant", arm)
    export_game_mesh(tree, "prop_tree")
    export_game_mesh(producer, "building_producer")
    export_game_mesh(tower, "building_tower")
    export_game_mesh(turret, "building_turret")
    export_game_mesh(wall, "building_wall")
    export_game_mesh(outpost, "building_outpost")
    export_game_mesh(builder, "unit_builder")
    export_game_mesh(archer, "unit_archer")
    export_game_mesh(mage, "unit_mage")
    export_game_mesh(leader, "unit_leader")
    export_game_mesh(ember, "unit_ember_raider")
    export_game_mesh(dryad, "unit_dryad")
    export_game_mesh(cavalry, "unit_cavalry")
    export_game_mesh(siege, "unit_siege")
    export_game_mesh(gold, "resource_gold")
    export_game_mesh(timber, "resource_timber")
    export_game_mesh(rock, "prop_rock")
    export_game_mesh(bridge, "prop_bridge")
    for ob in roster["keeps"] + roster["buildings"] + roster["units"]:
        key = ob.get("definition_id") or ob.name.split(".")[0]
        export_game_mesh(ob, key)

    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
    for ob in (citadel, peasant, tree, militia, producer, tower):
        print(ob.name, "faces", len(ob.data.polygons))
    for ob in roster["keeps"]:
        print(ob.name, "faces", len(ob.data.polygons))
    for ob in roster["units"]:
        if ob.get("definition_id") == "unit_university_mechanical_spider":
            print(ob.name, "faces", len(ob.data.polygons))


if __name__ == "__main__":
    main()
