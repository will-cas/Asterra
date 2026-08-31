"""One function per leftover kit human. Unique silhouette, not helm swaps."""
from __future__ import annotations

import math

from asterra_detail import finish
from asterra_figure import arms, head, legs


def outcast_snarer(g, m, c):
    p = []
    p.append(g.cyl("hips", (0, 0.08, 0.82), 0.2, 0.22, m.leather, c, verts=12))
    p.append(g.cyl("pelt", (0, 0.12, 1.12), 0.28, 0.62, m.bark, c, verts=12))
    p.append(g.cube("ice_cape", (0, 0.32, 1.08), (0.55, 0.12, 0.9), m.ice, c))
    legs(g, p, c, m.leather, s=0.92, boot=m.ice)
    arms(g, p, c, m.bark, m.skin, m.skin, s=0.92)
    head(g, p, c, m, s=0.92, hair="wild", hair_mat=m.ice)
    p.append(g.ico("leaf_cap", (0, 0.06, 1.78), 0.22, m.ice, c, subdiv=1, scale=(1.25, 1.1, 0.5)))
    p.append(g.cube("frame", (0.58, -0.18, 1.05), (0.85, 0.08, 0.85), m.wood, c, rot=(0, 0, math.radians(18))))
    p.append(g.cube("net", (0.58, -0.22, 1.05), (0.72, 0.04, 0.72), m.ice, c, rot=(0, 0, math.radians(18))))
    p.append(g.cyl("pole", (0.22, -0.12, 0.95), 0.03, 1.15, m.wood, c, verts=8, rot=(0, math.radians(55), 0)))
    return finish(g, "unit_outcast_snarer", p, c, 0.01)


def outcast_wind_rider(g, m, c):
    p = []
    p.append(g.cyl("hips", (0, 0.06, 0.95), 0.18, 0.2, m.leather, c, verts=12))
    p.append(g.cyl("torso", (0, 0.08, 1.22), 0.22, 0.48, m.ice, c, verts=12))
    legs(g, p, c, m.leather, boot=m.ice)
    arms(g, p, c, m.ice, m.skin, m.skin)
    head(g, p, c, m, hair="long", hair_mat=m.ice)
    p.append(g.ico("hood", (0, 0.08, 1.88), 0.22, m.ice, c, subdiv=1, scale=(1.2, 1.15, 0.55)))
    for i, spread in enumerate((0.55, 0.85, 1.12)):
        p.append(g.cube(f"wl{i}", (-spread, 0.12, 1.42 - i * 0.06), (0.55, 0.05, 0.28), m.ice, c, rot=(0, 0, math.radians(28 + i * 8))))
        p.append(g.cube(f"wr{i}", (spread, 0.12, 1.42 - i * 0.06), (0.55, 0.05, 0.28), m.ice, c, rot=(0, 0, math.radians(-28 - i * 8))))
    p.append(g.cyl("bow_u", (0.08, -0.52, 1.55), 0.028, 1.05, m.wood, c, verts=8))
    p.append(g.cyl("bow_d", (0.08, -0.52, 0.55), 0.028, 0.95, m.wood, c, verts=8))
    p.append(g.cube("quiver", (-0.28, 0.28, 1.18), (0.12, 0.12, 0.5), m.leather, c))
    return finish(g, "unit_outcast_wind_rider", p, c, 0.01)


def outcast_exiled_heir(g, m, c):
    p = []
    p.append(g.cyl("hips", (0, 0.06, 0.92), 0.2, 0.24, m.leather, c, verts=12))
    p.append(g.cyl("torso", (0, 0.08, 1.24), 0.26, 0.58, m.bark, c, verts=14))
    p.append(g.cube("cape", (0, 0.32, 1.2), (0.62, 0.12, 1.05), m.ice, c))
    legs(g, p, c, m.leather, boot=m.ice)
    arms(g, p, c, m.bark, m.skin, m.skin)
    head(g, p, c, m, hair="beard", hair_mat=m.ice)
    p.append(g.cyl("crown", (0, 0, 1.9), 0.16, 0.1, m.gold, c, verts=12))
    for i in range(5):
        ang = i * (2 * math.pi / 5)
        p.append(g.cone(f"ant{i}", (math.sin(ang) * 0.14, -math.cos(ang) * 0.14, 2.08), 0.035, 0.28, m.ice, c, verts=5))
    p.append(g.cyl("staff", (0.58, -0.06, 1.15), 0.04, 2.05, m.bark, c, verts=10))
    p.append(g.ico("orb", (0.58, -0.06, 2.22), 0.14, m.ice, c, subdiv=1))
    return finish(g, "unit_outcast_exiled_heir", p, c, 0.01)


def outcast_village_elder(g, m, c):
    p = []
    p.append(g.cyl("hips", (0, 0.1, 0.78), 0.22, 0.22, m.leather, c, verts=12))
    p.append(g.cyl("stoop", (0, 0.16, 1.08), 0.26, 0.7, m.bark, c, verts=12, rot=(math.radians(12), 0, 0)))
    legs(g, p, c, m.leather, s=0.9, boot=m.bark)
    arms(g, p, c, m.bark, m.skin, m.skin, s=0.9, drop=0.08)
    head(g, p, c, m, s=0.9, z=1.58, hair="beard", hair_mat=m.ice)
    p.append(g.ico("cap", (0, 0.12, 1.68), 0.2, m.ice, c, subdiv=1, scale=(1.2, 1.1, 0.5)))
    p.append(g.cyl("stick", (0.48, -0.15, 0.85), 0.035, 1.55, m.wood, c, verts=8, rot=(0, math.radians(8), 0)))
    p.append(g.ico("knot", (0.48, -0.15, 1.65), 0.08, m.bark, c, subdiv=1))
    return finish(g, "unit_outcast_village_elder", p, c, 0.01)


def outcast_hunt_caller(g, m, c):
    p = []
    p.append(g.cyl("hips", (0, 0.04, 0.92), 0.18, 0.22, m.leather, c, verts=12))
    p.append(g.cyl("torso", (0, 0.06, 1.22), 0.24, 0.52, m.leather, c, verts=12))
    p.append(g.cube("hide", (0, 0.28, 1.15), (0.5, 0.1, 0.75), m.bark, c))
    legs(g, p, c, m.leather, boot=m.ice)
    arms(g, p, c, m.leather, m.skin, m.skin)
    head(g, p, c, m, hair="wild", hair_mat=m.leather)
    p.append(g.ico("hood", (0, 0.08, 1.86), 0.2, m.ice, c, subdiv=1, scale=(1.15, 1.1, 0.5)))
    p.append(g.cyl("horn", (0.42, -0.22, 1.25), 0.07, 0.35, m.bark, c, verts=10, rot=(math.radians(70), 0, 0)))
    p.append(g.cyl("bow_u", (0.06, -0.5, 1.5), 0.03, 1.1, m.wood, c, verts=8))
    p.append(g.cyl("bow_d", (0.06, -0.5, 0.48), 0.03, 1.0, m.wood, c, verts=8))
    p.append(g.cube("quiver", (-0.3, 0.26, 1.2), (0.14, 0.14, 0.55), m.leather, c))
    return finish(g, "unit_outcast_hunt_caller", p, c, 0.01)


def freetown_builder(g, m, c):
    p = []
    p.append(g.cyl("hips", (0, 0.04, 0.94), 0.2, 0.24, m.leather, c, verts=12))
    p.append(g.cyl("shirt", (0, 0.04, 1.24), 0.22, 0.48, m.pale_wood, c, verts=12))
    p.append(g.cube("apron", (0, -0.18, 1.05), (0.38, 0.06, 0.85), m.leather, c))
    p.append(g.cube("pocket", (0.12, -0.22, 0.95), (0.14, 0.05, 0.16), m.wood, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.pale_wood, m.skin, m.skin)
    head(g, p, c, m, hair="crop", hair_mat=m.leather)
    p.append(g.cube("hat", (0, 0, 1.86), (0.42, 0.28, 0.1), m.leather, c))
    p.append(g.cube("brim", (0, -0.2, 1.82), (0.22, 0.18, 0.05), m.leather, c))
    p.append(g.cyl("haft", (0.72, 0, 0.92), 0.035, 0.95, m.wood, c, verts=8, rot=(0, math.radians(90), 0)))
    p.append(g.cube("mallet", (1.18, 0, 0.92), (0.22, 0.16, 0.38), m.iron, c))
    p.append(g.cube("nails", (-0.28, 0.22, 1.05), (0.12, 0.1, 0.08), m.iron, c))
    return finish(g, "unit_freetown_builder", p, c, 0.01)


def freetown_mudslinger(g, m, c):
    p = []
    p.append(g.cyl("hips", (0, 0.04, 0.94), 0.22, 0.24, m.leather, c, verts=12))
    p.append(g.cyl("coat", (0, 0.06, 1.18), 0.3, 0.95, m.cloth_blue, c, verts=14))
    p.append(g.cube("sash", (0, -0.22, 1.12), (0.28, 0.05, 0.12), m.cloth, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.cloth_blue, m.skin, m.skin)
    head(g, p, c, m, hair="crop")
    p.append(g.cube("hat", (0, 0, 1.86), (0.46, 0.3, 0.1), m.leather, c))
    p.append(g.cube("brim", (0, -0.22, 1.82), (0.24, 0.2, 0.05), m.leather, c))
    p.append(g.cyl("sling", (0.55, -0.15, 1.15), 0.025, 0.85, m.leather, c, verts=8, rot=(0, math.radians(40), 0)))
    p.append(g.uv_sphere("mud", (0.95, -0.22, 1.35), 0.1, m.bark, c, segs=10, rings=6))
    p.append(g.cyl("pot", (-0.22, 0.28, 1.05), 0.12, 0.22, m.brick, c, verts=10))
    return finish(g, "unit_freetown_mudslinger", p, c, 0.01)


def freetown_privateer(g, m, c):
    p = []
    p.append(g.cyl("hips", (0, 0.04, 0.94), 0.2, 0.24, m.leather, c, verts=12))
    p.append(g.cyl("coat", (0, 0.08, 1.2), 0.3, 1.05, m.cloth_blue, c, verts=14))
    p.append(g.cube("tails", (0, 0.28, 0.78), (0.36, 0.12, 0.75), m.cloth_blue, c))
    p.append(g.cube("sash", (0, -0.24, 1.08), (0.32, 0.05, 0.14), m.gold, c, rot=(0, 0, math.radians(12))))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.cloth_blue, m.skin, m.skin)
    head(g, p, c, m, hair="long", hair_mat=m.leather)
    p.append(g.cube("hat", (0, 0, 1.88), (0.48, 0.32, 0.12), m.leather, c))
    p.append(g.cube("cock", (0, -0.24, 1.86), (0.22, 0.2, 0.08), m.cloth_blue, c))
    p.append(g.cube("cutlass", (0.62, -0.06, 1.22), (0.05, 0.025, 0.95), m.iron, c))
    p.append(g.cube("guard", (0.62, -0.06, 0.78), (0.18, 0.08, 0.06), m.gold, c))
    p.append(g.cube("pistol", (-0.42, 0.18, 1.05), (0.08, 0.22, 0.08), m.wood, c, rot=(0, math.radians(90), 0)))
    return finish(g, "unit_freetown_privateer", p, c, 0.01)


def freetown_highwayman(g, m, c):
    p = []
    p.append(g.cyl("hips", (0, 0.04, 0.94), 0.18, 0.22, m.leather, c, verts=12))
    p.append(g.cyl("cloak", (0, 0.12, 1.18), 0.32, 1.15, m.cloth_blue, c, verts=12))
    p.append(g.cube("mask", (0, -0.14, 1.66), (0.2, 0.06, 0.08), m.leather, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.cloth_blue, m.skin, m.skin)
    head(g, p, c, m, hair="crop")
    p.append(g.cube("hat", (0, 0, 1.88), (0.5, 0.28, 0.1), m.leather, c))
    p.append(g.cube("brim", (0, -0.24, 1.84), (0.26, 0.18, 0.05), m.leather, c))
    p.append(g.cube("dg_l", (-0.48, -0.08, 0.95), (0.04, 0.025, 0.42), m.iron, c))
    p.append(g.cube("dg_r", (0.48, -0.08, 0.95), (0.04, 0.025, 0.42), m.iron, c))
    p.append(g.cube("satchel", (0.22, 0.28, 1.05), (0.28, 0.16, 0.22), m.leather, c))
    return finish(g, "unit_freetown_highwayman", p, c, 0.01)


def freetown_brute(g, m, c):
    p = []
    s = 1.32
    p.append(g.cyl("hips", (0, 0.06, 0.95 * s), 0.3, 0.34, m.leather, c, verts=14))
    p.append(g.cyl("torso", (0, 0.08, 1.22 * s), 0.36, 0.68, m.leather, c, verts=14))
    p.append(g.cube("gut", (0, -0.14, 1.05 * s), (0.42, 0.24, 0.42), m.leather, c))
    legs(g, p, c, m.leather, s=s, boot_s=(0.22, 0.38, 0.14))
    arms(g, p, c, m.leather, m.skin, m.skin, s=s)
    head(g, p, c, m, s=s, hair="crop")
    p.append(g.cube("hat", (0, 0, 1.86 * s), (0.4 * s, 0.26 * s, 0.1 * s), m.leather, c))
    p.append(g.cyl("club", (0.82 * s, -0.08, 1.05 * s), 0.08 * s, 1.35 * s, m.wood, c, verts=10))
    p.append(g.ico("knot", (0.82 * s, -0.08, 1.75 * s), 0.16 * s, m.wood, c, subdiv=1))
    return finish(g, "unit_freetown_brute", p, c, 0.014)


def freetown_jump_imp(g, m, c):
    p = []
    s = 0.58
    p.append(g.cyl("hips", (0, 0.08, 0.88 * s), 0.14, 0.18, m.cloth_blue, c, verts=10))
    p.append(g.cyl("torso", (0, 0.1, 1.12 * s), 0.16, 0.36, m.cloth_blue, c, verts=10))
    legs(g, p, c, m.leather, s=s, boot=m.cloth_blue)
    arms(g, p, c, m.cloth_blue, m.skin, m.skin, s=s)
    head(g, p, c, m, s=s, hair="wild", hair_mat=m.cloth_blue)
    p.append(g.cone("ear_l", (-0.12 * s, 0.04, 1.58 * s), 0.03, 0.16, m.skin, c, verts=5))
    p.append(g.cone("ear_r", (0.12 * s, 0.04, 1.58 * s), 0.03, 0.16, m.skin, c, verts=5))
    p.append(g.cone("hood", (0, 0.08, 1.72 * s), 0.16, 0.28, m.cloth_blue, c, verts=8))
    p.append(g.cube("dg", (0.32 * s, -0.08, 0.85 * s), (0.035, 0.02, 0.32 * s), m.iron, c))
    p.append(g.cube("tail", (0, 0.22 * s, 0.7 * s), (0.06, 0.28 * s, 0.06), m.cloth_blue, c, rot=(math.radians(25), 0, 0)))
    return finish(g, "unit_freetown_jump_imp", p, c, 0.008)


def freetown_cannon_fodder(g, m, c):
    p = []
    p.append(g.cyl("hips", (0, 0.04, 0.94), 0.18, 0.22, m.leather, c, verts=12))
    p.append(g.cyl("rags", (0, 0.04, 1.2), 0.24, 0.55, m.cloth, c, verts=10))
    p.append(g.cube("patch", (0.12, -0.16, 1.15), (0.16, 0.04, 0.22), m.cloth_blue, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.cloth, m.skin, m.skin)
    head(g, p, c, m, hair="crop")
    p.append(g.cube("hat", (0, 0, 1.84), (0.4, 0.26, 0.08), m.leather, c, rot=(0, 0, math.radians(12))))
    p.append(g.cyl("barrel", (-0.55, 0.1, 1.05), 0.28, 0.7, m.wood, c, verts=12, rot=(math.radians(80), 0, 0)))
    p.append(g.cube("sw", (0.58, -0.04, 1.15), (0.045, 0.025, 0.85), m.iron, c))
    return finish(g, "unit_freetown_cannon_fodder", p, c, 0.01)


def freetown_improvised_explosive(g, m, c):
    p = []
    p.append(g.cyl("hips", (0, 0.04, 0.94), 0.2, 0.24, m.leather, c, verts=12))
    p.append(g.cyl("coat", (0, 0.06, 1.2), 0.28, 0.95, m.leather, c, verts=12))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.leather, m.skin, m.skin)
    head(g, p, c, m, hair="crop")
    p.append(g.cube("hat", (0, 0, 1.86), (0.42, 0.28, 0.1), m.leather, c))
    p.append(g.cyl("cask", (0, 0.38, 1.18), 0.32, 0.65, m.wood, c, verts=14))
    p.append(g.cyl("hoop_a", (0, 0.38, 1.05), 0.34, 0.05, m.iron, c, verts=14))
    p.append(g.cyl("hoop_b", (0, 0.38, 1.35), 0.34, 0.05, m.iron, c, verts=14))
    p.append(g.cyl("fuse", (0, 0.38, 1.58), 0.035, 0.28, m.leather, c, verts=6))
    p.append(g.uv_sphere("spark", (0, 0.38, 1.74), 0.06, m.gold, c, segs=8, rings=6))
    return finish(g, "unit_freetown_improvised_explosive", p, c, 0.01)


def freetown_brewmaster(g, m, c):
    p = []
    p.append(g.uv_sphere("belly", (0, 0.1, 1.02), 0.34, m.cloth_blue, c, segs=14, rings=10))
    p.append(g.cyl("torso", (0, 0.06, 1.32), 0.22, 0.38, m.cloth_blue, c, verts=12))
    legs(g, p, c, m.cloth_blue, boot=m.leather)
    arms(g, p, c, m.cloth_blue, m.skin, m.skin)
    head(g, p, c, m, hair="beard", hair_mat=m.leather)
    p.append(g.cube("hat", (0, 0, 1.86), (0.46, 0.3, 0.1), m.leather, c))
    p.append(g.cyl("keg", (0, 0.42, 1.12), 0.28, 0.55, m.wood, c, verts=14, rot=(math.radians(90), 0, 0)))
    p.append(g.cyl("tankard", (0.55, -0.16, 1.05), 0.08, 0.18, m.gold, c, verts=10))
    p.append(g.cyl("mallet", (0.72, 0, 0.85), 0.03, 0.7, m.wood, c, verts=8, rot=(0, math.radians(90), 0)))
    return finish(g, "unit_freetown_brewmaster", p, c, 0.01)


def freetown_captain(g, m, c):
    p = []
    p.append(g.cyl("hips", (0, 0.04, 0.94), 0.2, 0.24, m.leather, c, verts=12))
    p.append(g.cyl("coat", (0, 0.08, 1.22), 0.32, 1.12, m.cloth_blue, c, verts=14))
    p.append(g.cube("epau_l", (-0.32, 0, 1.48), (0.2, 0.2, 0.1), m.gold, c))
    p.append(g.cube("epau_r", (0.32, 0, 1.48), (0.2, 0.2, 0.1), m.gold, c))
    p.append(g.cube("tails", (0, 0.3, 0.72), (0.38, 0.14, 0.8), m.cloth_blue, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.cloth_blue, m.skin, m.skin)
    head(g, p, c, m, hair="long", hair_mat=m.leather)
    p.append(g.cube("hat", (0, 0, 1.9), (0.52, 0.22, 0.14), m.leather, c))
    p.append(g.cube("plume", (0.08, 0.02, 2.08), (0.06, 0.06, 0.28), m.cloth, c))
    p.append(g.cube("rapier", (0.62, -0.05, 1.28), (0.04, 0.02, 1.15), m.iron, c))
    p.append(g.cyl("scope", (0.18, -0.38, 1.7), 0.04, 0.32, m.gold, c, verts=8, rot=(math.radians(90), 0, 0)))
    return finish(g, "unit_freetown_captain", p, c, 0.01)


def freetown_dockmaster(g, m, c):
    p = []
    p.append(g.cyl("hips", (0, 0.04, 0.94), 0.2, 0.24, m.leather, c, verts=12))
    p.append(g.cyl("coat", (0, 0.06, 1.2), 0.28, 1.0, m.pale_wood, c, verts=12))
    p.append(g.cube("ledger", (0.42, -0.22, 1.12), (0.28, 0.06, 0.36), m.leather, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.pale_wood, m.skin, m.skin)
    head(g, p, c, m, hair="crop")
    p.append(g.cube("hat", (0, 0, 1.86), (0.44, 0.3, 0.1), m.leather, c))
    p.append(g.cyl("coil", (0, 0.32, 1.08), 0.16, 0.22, m.cloth_blue, c, verts=10, rot=(math.radians(90), 0, 0)))
    p.append(g.cube("quill", (0.52, -0.18, 1.35), (0.03, 0.03, 0.28), m.wood, c, rot=(0, math.radians(35), 0)))
    return finish(g, "unit_freetown_dockmaster", p, c, 0.01)


def freetown_fence(g, m, c):
    p = []
    p.append(g.cyl("hips", (0, 0.04, 0.94), 0.18, 0.22, m.leather, c, verts=12))
    p.append(g.cyl("coat", (0, 0.08, 1.18), 0.26, 0.95, m.cloth_blue, c, verts=12))
    p.append(g.cube("pouch_l", (-0.22, 0.12, 0.95), (0.16, 0.12, 0.14), m.leather, c))
    p.append(g.cube("pouch_r", (0.22, 0.12, 0.95), (0.16, 0.12, 0.14), m.leather, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.cloth_blue, m.skin, m.skin)
    head(g, p, c, m, hair="crop")
    p.append(g.cube("hat", (0, 0, 1.86), (0.4, 0.26, 0.08), m.leather, c))
    p.append(g.cube("coin", (0.48, -0.18, 1.05), (0.12, 0.04, 0.12), m.gold, c))
    p.append(g.cube("dg", (0.5, -0.1, 0.92), (0.04, 0.02, 0.38), m.iron, c))
    p.append(g.cube("gem", (-0.18, -0.2, 1.15), (0.08, 0.05, 0.08), m.crystal, c))
    return finish(g, "unit_freetown_fence", p, c, 0.01)


def freetown_island_speaker(g, m, c):
    p = []
    p.append(g.cyl("hips", (0, 0.04, 0.94), 0.22, 0.24, m.leather, c, verts=12))
    p.append(g.cyl("coat", (0, 0.08, 1.22), 0.32, 1.15, m.cloth_blue, c, verts=14))
    p.append(g.cube("shell_cape", (0, 0.3, 1.15), (0.55, 0.1, 0.95), m.pale_wood, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.cloth_blue, m.skin, m.skin)
    head(g, p, c, m, hair="long", hair_mat=m.leather)
    p.append(g.cyl("crown", (0, 0, 1.9), 0.16, 0.1, m.gold, c, verts=12))
    for i in range(6):
        ang = i * math.pi / 3
        p.append(g.cone(f"shell{i}", (math.sin(ang) * 0.15, -math.cos(ang) * 0.15, 2.02), 0.04, 0.18, m.pale_wood, c, verts=5))
    p.append(g.cyl("staff", (0.58, -0.05, 1.15), 0.04, 2.05, m.wood, c, verts=10))
    p.append(g.ico("coral", (0.58, -0.05, 2.22), 0.16, m.cloth_blue, c, subdiv=1))
    return finish(g, "unit_freetown_island_speaker", p, c, 0.01)


def university_practitioner(g, m, c):
    p = []
    p.append(g.cyl("gown", (0, 0.04, 1.08), 0.3, 1.45, m.leather, c, verts=14))
    p.append(g.cube("apron", (0, -0.24, 1.1), (0.28, 0.05, 0.95), m.cloth, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.leather, m.skin, m.skin)
    head(g, p, c, m, hair="crop")
    p.append(g.cyl("board", (0, 0, 1.88), 0.17, 0.1, m.leather, c, verts=10))
    p.append(g.cube("tassel", (0.2, 0, 1.82), (0.04, 0.04, 0.22), m.gold, c))
    p.append(g.cyl("haft", (0.7, 0, 0.95), 0.032, 0.85, m.wood, c, verts=8, rot=(0, math.radians(90), 0)))
    p.append(g.cube("hammer", (1.12, 0, 0.95), (0.18, 0.12, 0.32), m.iron, c))
    p.append(g.cyl("gear", (0.22, 0.28, 1.25), 0.12, 0.06, m.gold, c, verts=14, rot=(0, math.radians(90), 0)))
    return finish(g, "unit_university_practitioner", p, c, 0.01)


def university_poison_specialist(g, m, c):
    p = []
    p.append(g.cyl("gown", (0, 0.04, 1.08), 0.32, 1.5, m.cloth_deep, c, verts=14))
    p.append(g.cube("mask", (0, -0.14, 1.66), (0.2, 0.08, 0.12), m.leather, c))
    p.append(g.cube("beak", (0, -0.24, 1.62), (0.08, 0.16, 0.08), m.leather, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.cloth_deep, m.cloth_deep, m.skin)
    head(g, p, c, m)
    p.append(g.cyl("board", (0, 0, 1.9), 0.16, 0.1, m.cloth_deep, c, verts=8))
    p.append(g.cube("rack", (0, 0.32, 1.22), (0.42, 0.16, 0.3), m.wood, c))
    for i, x in enumerate((-0.12, 0, 0.12)):
        p.append(g.cyl(f"v{i}", (x, 0.4, 1.42), 0.045, 0.22, m.glass, c, verts=8))
        p.append(g.uv_sphere(f"liq{i}", (x, 0.4, 1.38), 0.04, m.crystal, c, segs=6, rings=4))
    p.append(g.cyl("staff", (0.55, -0.06, 1.15), 0.03, 1.85, m.wood, c, verts=8))
    return finish(g, "unit_university_poison_specialist", p, c, 0.01)


def university_chancellor(g, m, c):
    p = []
    p.append(g.cyl("gown", (0, 0.06, 1.1), 0.36, 1.55, m.cloth_deep, c, verts=16))
    p.append(g.cube("facing", (0, -0.3, 1.18), (0.22, 0.05, 1.15), m.gold, c))
    p.append(g.cube("chain", (0, -0.28, 1.35), (0.2, 0.05, 0.45), m.gold, c))
    legs(g, p, c, m.cloth_deep, boot=m.leather)
    arms(g, p, c, m.cloth_deep, m.cloth_deep, m.skin)
    head(g, p, c, m, hair="beard", hair_mat=m.leather)
    p.append(g.cyl("board", (0, 0, 1.92), 0.18, 0.12, m.cloth_deep, c, verts=10))
    p.append(g.cube("tassel", (0.22, 0, 1.86), (0.05, 0.05, 0.28), m.gold, c))
    p.append(g.cube("folio", (0.42, -0.28, 1.12), (0.42, 0.1, 0.55), m.leather, c))
    p.append(g.cyl("staff", (0.62, -0.04, 1.2), 0.035, 2.05, m.wood, c, verts=10))
    p.append(g.uv_sphere("orb", (0.62, -0.04, 2.28), 0.12, m.gold, c, segs=10, rings=6))
    return finish(g, "unit_university_chancellor", p, c, 0.01)


def university_arms_dean(g, m, c):
    p = []
    p.append(g.cyl("mail", (0, 0.02, 1.26), 0.24, 0.52, m.iron, c, verts=14))
    p.append(g.cube("breast", (0, -0.16, 1.28), (0.32, 0.08, 0.45), m.iron, c))
    p.append(g.cube("tabard", (0, -0.2, 1.1), (0.22, 0.05, 0.85), m.cloth_deep, c))
    legs(g, p, c, m.iron, boot=m.leather)
    arms(g, p, c, m.iron, m.iron, m.leather)
    head(g, p, c, m, hair="crop")
    p.append(g.cyl("board", (0, 0, 1.88), 0.16, 0.1, m.cloth_deep, c, verts=8))
    p.append(g.cube("sw", (0.62, -0.04, 1.38), (0.055, 0.03, 1.05), m.iron, c))
    p.append(g.cube("guard", (0.62, -0.04, 0.88), (0.22, 0.05, 0.06), m.gold, c))
    p.append(g.cube("kite", (-0.65, 0.14, 1.12), (0.52, 0.08, 0.9), m.wood, c, rot=(math.radians(10), 0, math.radians(8))))
    return finish(g, "unit_university_arms_dean", p, c, 0.01)


def university_climate_dean(g, m, c):
    p = []
    p.append(g.cyl("gown", (0, 0.04, 1.08), 0.32, 1.48, m.cloth_deep, c, verts=14))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.cloth_deep, m.skin, m.skin)
    head(g, p, c, m, hair="long", hair_mat=m.leather)
    p.append(g.cyl("board", (0, 0, 1.9), 0.16, 0.1, m.cloth_deep, c, verts=8))
    p.append(g.cyl("rod", (0, 0.02, 2.15), 0.025, 0.45, m.iron, c, verts=8))
    p.append(g.cube("vane", (0.16, 0.02, 2.35), (0.38, 0.04, 0.16), m.gold, c))
    p.append(g.cyl("staff", (0.55, -0.05, 1.15), 0.03, 1.95, m.wood, c, verts=8))
    p.append(g.uv_sphere("glass", (0.55, -0.05, 2.18), 0.1, m.glass, c, segs=10, rings=6))
    return finish(g, "unit_university_climate_dean", p, c, 0.01)


def university_archivist(g, m, c):
    p = []
    p.append(g.cyl("gown", (0, 0.04, 1.08), 0.3, 1.48, m.cloth_deep, c, verts=14))
    p.append(g.cube("stole", (0, -0.26, 1.15), (0.16, 0.05, 1.05), m.gold, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.cloth_deep, m.skin, m.skin)
    head(g, p, c, m, hair="beard")
    p.append(g.cyl("board", (0, 0, 1.88), 0.16, 0.1, m.cloth_deep, c, verts=8))
    for i, x in enumerate((-0.16, 0, 0.16)):
        p.append(g.cyl(f"scroll{i}", (x, 0.32, 1.22), 0.05, 0.32, m.cloth, c, verts=8, rot=(math.radians(90), 0, 0)))
    p.append(g.cube("folio", (0.48, -0.22, 1.1), (0.32, 0.08, 0.42), m.leather, c))
    p.append(g.cyl("staff", (0.58, -0.04, 1.15), 0.03, 1.9, m.wood, c, verts=8))
    return finish(g, "unit_university_archivist", p, c, 0.01)


def university_provost(g, m, c):
    p = []
    p.append(g.cyl("gown", (0, 0.04, 1.08), 0.32, 1.5, m.cloth_deep, c, verts=14))
    p.append(g.cube("keys", (0.18, 0.22, 1.02), (0.08, 0.22, 0.16), m.gold, c))
    p.append(g.cube("chain", (0, -0.26, 1.28), (0.22, 0.05, 0.55), m.gold, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.cloth_deep, m.skin, m.skin)
    head(g, p, c, m, hair="crop")
    p.append(g.cyl("board", (0, 0, 1.88), 0.16, 0.1, m.cloth_deep, c, verts=8))
    p.append(g.cyl("staff", (0.55, -0.05, 1.15), 0.035, 1.95, m.wood, c, verts=8))
    p.append(g.uv_sphere("seal", (0.55, -0.05, 2.18), 0.1, m.gold, c, segs=10, rings=6))
    return finish(g, "unit_university_provost", p, c, 0.01)


def church_mason(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0.02, 1.24), 0.22, 0.5, m.marble, c, verts=12))
    p.append(g.cube("apron", (0, -0.16, 1.05), (0.34, 0.05, 0.75), m.cloth, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.marble, m.skin, m.skin)
    head(g, p, c, m, hair="crop")
    p.append(g.cyl("helm", (0, 0, 1.84), 0.15, 0.14, m.gold, c, verts=12))
    p.append(g.cyl("disc", (0, -0.16, 1.84), 0.18, 0.04, m.gold, c, verts=12, rot=(math.radians(80), 0, 0)))
    p.append(g.cube("ashlar", (0.55, -0.12, 0.52), (0.42, 0.28, 0.28), m.marble, c))
    p.append(g.cyl("haft", (0.72, 0, 0.95), 0.032, 0.85, m.wood, c, verts=8, rot=(0, math.radians(90), 0)))
    p.append(g.cube("mallet", (1.14, 0, 0.95), (0.18, 0.14, 0.32), m.iron, c))
    return finish(g, "unit_church_mason", p, c, 0.01)


def church_sun_priest(g, m, c):
    p = []
    p.append(g.cyl("alb", (0, 0.04, 1.08), 0.32, 1.52, m.cloth_sun, c, verts=14))
    p.append(g.cube("stole", (0, -0.28, 1.18), (0.2, 0.05, 1.15), m.gold, c))
    p.append(g.cyl("sun_chest", (0, -0.32, 1.38), 0.12, 0.04, m.gold, c, verts=12, rot=(math.radians(80), 0, 0)))
    legs(g, p, c, m.cloth_sun, boot=m.leather)
    arms(g, p, c, m.cloth_sun, m.cloth_sun, m.skin)
    head(g, p, c, m, hair="crop")
    p.append(g.cyl("helm", (0, 0, 1.86), 0.16, 0.16, m.gold, c, verts=12))
    p.append(g.cyl("disc", (0, -0.18, 1.86), 0.2, 0.05, m.gold, c, verts=14, rot=(math.radians(80), 0, 0)))
    p.append(g.cyl("staff", (0.58, -0.04, 1.2), 0.035, 2.05, m.gold, c, verts=10))
    p.append(g.cyl("sun", (0.58, -0.04, 2.28), 0.16, 0.05, m.gold, c, verts=14, rot=(math.radians(80), 0, 0)))
    return finish(g, "unit_church_sun_priest", p, c, 0.01)


def church_sun_stalker(g, m, c):
    p = []
    p.append(g.cyl("robe", (0, 0.08, 1.12), 0.24, 1.25, m.cloth_deep, c, verts=12))
    p.append(g.cube("panel", (0, -0.22, 1.1), (0.18, 0.05, 0.95), m.gold, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.cloth_deep, m.skin, m.skin)
    head(g, p, c, m)
    p.append(g.cone("hood", (0, 0.1, 1.95), 0.24, 0.42, m.cloth_deep, c, verts=8))
    p.append(g.cyl("scope", (0.12, -0.36, 1.68), 0.04, 0.38, m.gold, c, verts=8, rot=(math.radians(90), 0, 0)))
    p.append(g.cube("lens", (0.12, -0.55, 1.68), (0.08, 0.04, 0.08), m.glass, c))
    p.append(g.cube("dg", (0.5, -0.1, 0.92), (0.04, 0.02, 0.4), m.iron, c))
    return finish(g, "unit_church_sun_stalker", p, c, 0.01)


def church_purifier(g, m, c):
    p = []
    p.append(g.cyl("alb", (0, 0.04, 1.08), 0.3, 1.48, m.cloth_sun, c, verts=14))
    p.append(g.cube("stole", (0, -0.26, 1.15), (0.18, 0.05, 1.1), m.gold, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.cloth_sun, m.skin, m.skin)
    head(g, p, c, m, hair="crop")
    p.append(g.cyl("helm", (0, 0, 1.86), 0.16, 0.14, m.gold, c, verts=12))
    p.append(g.cyl("censer", (0.52, -0.12, 0.85), 0.1, 0.22, m.gold, c, verts=12))
    p.append(g.cyl("chain", (0.52, -0.12, 1.15), 0.02, 0.45, m.gold, c, verts=6))
    p.append(g.uv_sphere("smoke", (0.52, -0.12, 1.05), 0.08, m.cloth, c, segs=8, rings=6))
    p.append(g.cyl("torch", (0.62, -0.04, 1.2), 0.03, 1.15, m.wood, c, verts=8))
    p.append(g.uv_sphere("flame", (0.62, -0.04, 1.82), 0.1, m.gold, c, segs=8, rings=6))
    return finish(g, "unit_church_purifier", p, c, 0.01)


def church_inquisitor(g, m, c):
    p = []
    p.append(g.cyl("alb", (0, 0.04, 1.08), 0.3, 1.48, m.cloth_sun, c, verts=14))
    p.append(g.cube("stole", (0, -0.28, 1.18), (0.2, 0.05, 1.12), m.cloth_deep, c))
    p.append(g.cube("chain", (0, -0.3, 1.32), (0.18, 0.04, 0.5), m.gold, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.cloth_sun, m.skin, m.skin)
    head(g, p, c, m, hair="crop")
    p.append(g.cyl("helm", (0, 0, 1.86), 0.16, 0.16, m.gold, c, verts=12))
    p.append(g.cyl("disc", (0, -0.18, 1.84), 0.2, 0.04, m.gold, c, verts=12, rot=(math.radians(80), 0, 0)))
    p.append(g.cube("sw", (0.6, -0.04, 1.35), (0.05, 0.025, 1.05), m.iron, c))
    p.append(g.cube("guard", (0.6, -0.04, 0.85), (0.2, 0.05, 0.05), m.gold, c))
    return finish(g, "unit_church_inquisitor", p, c, 0.01)


def church_eclipse_warden(g, m, c):
    p = []
    p.append(g.cyl("plate", (0, 0.02, 1.28), 0.26, 0.55, m.marble, c, verts=14))
    p.append(g.cube("plastron", (0, -0.18, 1.3), (0.36, 0.1, 0.5), m.marble, c))
    p.append(g.cube("sun_tab", (0, -0.22, 1.12), (0.24, 0.05, 0.85), m.cloth_deep, c))
    legs(g, p, c, m.marble, boot=m.iron)
    arms(g, p, c, m.marble, m.marble, m.leather)
    head(g, p, c, m)
    p.append(g.cyl("helm", (0, 0, 1.86), 0.17, 0.18, m.gold, c, verts=14))
    p.append(g.cyl("disc", (0, -0.18, 1.86), 0.22, 0.05, m.gold, c, verts=14, rot=(math.radians(80), 0, 0)))
    p.append(g.cube("sw", (0.65, -0.04, 1.4), (0.06, 0.03, 1.15), m.iron, c))
    p.append(g.cube("heater", (-0.68, 0.14, 1.12), (0.52, 0.09, 0.85), m.marble, c, rot=(math.radians(8), 0, math.radians(8))))
    return finish(g, "unit_church_eclipse_warden", p, c, 0.01)


def church_dawn_herald(g, m, c):
    p = []
    p.append(g.cyl("alb", (0, 0.04, 1.08), 0.3, 1.48, m.cloth_sun, c, verts=14))
    p.append(g.cube("stole", (0, -0.26, 1.15), (0.18, 0.05, 1.1), m.gold, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.cloth_sun, m.skin, m.skin)
    head(g, p, c, m, hair="long", hair_mat=m.leather)
    p.append(g.cyl("helm", (0, 0, 1.86), 0.16, 0.14, m.gold, c, verts=12))
    p.append(g.cyl("pole", (-0.08, 0.28, 1.75), 0.035, 1.65, m.wood, c, verts=8))
    p.append(g.cube("flag", (0.28, 0.28, 2.4), (0.65, 0.05, 0.5), m.cloth_sun, c))
    p.append(g.cube("sunf", (0.28, 0.3, 2.45), (0.18, 0.04, 0.18), m.gold, c))
    p.append(g.cyl("staff", (0.55, -0.04, 1.15), 0.03, 1.85, m.gold, c, verts=8))
    return finish(g, "unit_church_dawn_herald", p, c, 0.01)


def church_reliquary(g, m, c):
    p = []
    p.append(g.cyl("alb", (0, 0.04, 1.08), 0.32, 1.5, m.cloth_sun, c, verts=14))
    p.append(g.cube("stole", (0, -0.28, 1.18), (0.2, 0.05, 1.12), m.gold, c))
    legs(g, p, c, m.cloth_sun, boot=m.leather)
    arms(g, p, c, m.cloth_sun, m.skin, m.skin)
    head(g, p, c, m, hair="crop")
    p.append(g.cyl("helm", (0, 0, 1.86), 0.16, 0.16, m.gold, c, verts=12))
    p.append(g.cube("chest", (0.52, -0.18, 1.02), (0.48, 0.3, 0.35), m.gold, c))
    p.append(g.cyl("sunr", (0.52, -0.34, 1.12), 0.12, 0.04, m.gold, c, verts=12, rot=(math.radians(80), 0, 0)))
    p.append(g.cube("lid", (0.52, -0.18, 1.22), (0.5, 0.32, 0.08), m.marble, c))
    p.append(g.cyl("staff", (0.62, -0.04, 1.2), 0.03, 1.9, m.gold, c, verts=8))
    return finish(g, "unit_church_reliquary", p, c, 0.01)


def unit_pathfinder(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0.02, 1.24), 0.2, 0.48, m.leather, c, verts=12))
    p.append(g.cube("cloak", (0, 0.22, 1.15), (0.42, 0.08, 0.85), m.cloth, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.leather, m.skin, m.skin)
    head(g, p, c, m, hair="crop")
    p.append(g.cyl("helm", (0, 0, 1.8), 0.15, 0.14, m.iron, c, verts=12))
    p.append(g.cyl("brim", (0, -0.02, 1.71), 0.22, 0.04, m.iron, c, verts=14))
    p.append(g.cyl("scope", (0.12, -0.36, 1.68), 0.04, 0.32, m.iron, c, verts=8, rot=(math.radians(90), 0, 0)))
    p.append(g.cyl("bow_u", (0.06, -0.5, 1.48), 0.028, 1.0, m.wood, c, verts=8))
    p.append(g.cyl("bow_d", (0.06, -0.5, 0.52), 0.028, 0.9, m.wood, c, verts=8))
    p.append(g.cube("quiver", (-0.28, 0.24, 1.18), (0.12, 0.12, 0.5), m.leather, c))
    return finish(g, "unit_pathfinder", p, c, 0.01)


def unit_sapper(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0.02, 1.24), 0.22, 0.48, m.leather, c, verts=12))
    p.append(g.cube("apron", (0, -0.16, 1.05), (0.32, 0.05, 0.7), m.cloth, c))
    legs(g, p, c, m.leather)
    arms(g, p, c, m.leather, m.skin, m.skin)
    head(g, p, c, m, hair="crop")
    p.append(g.cyl("helm", (0, 0, 1.8), 0.15, 0.14, m.iron, c, verts=12))
    p.append(g.cyl("brim", (0, -0.02, 1.71), 0.22, 0.04, m.iron, c, verts=14))
    p.append(g.cube("pack", (0, 0.32, 1.22), (0.45, 0.24, 0.38), m.wood, c))
    p.append(g.cyl("cask", (0, 0.38, 1.05), 0.22, 0.35, m.wood, c, verts=12))
    p.append(g.cyl("fuse", (0, 0.38, 1.28), 0.03, 0.18, m.leather, c, verts=6))
    p.append(g.cyl("pick", (0.7, 0, 1.05), 0.03, 0.9, m.wood, c, verts=8, rot=(0, math.radians(20), 0)))
    return finish(g, "unit_sapper", p, c, 0.01)


UNIQUE_HUMANS = {
    "unit_outcast_snarer": outcast_snarer,
    "unit_outcast_wind_rider": outcast_wind_rider,
    "unit_outcast_exiled_heir": outcast_exiled_heir,
    "unit_outcast_village_elder": outcast_village_elder,
    "unit_outcast_hunt_caller": outcast_hunt_caller,
    "unit_freetown_builder": freetown_builder,
    "unit_freetown_mudslinger": freetown_mudslinger,
    "unit_freetown_privateer": freetown_privateer,
    "unit_freetown_highwayman": freetown_highwayman,
    "unit_freetown_brute": freetown_brute,
    "unit_freetown_jump_imp": freetown_jump_imp,
    "unit_freetown_cannon_fodder": freetown_cannon_fodder,
    "unit_freetown_improvised_explosive": freetown_improvised_explosive,
    "unit_freetown_brewmaster": freetown_brewmaster,
    "unit_freetown_captain": freetown_captain,
    "unit_freetown_dockmaster": freetown_dockmaster,
    "unit_freetown_fence": freetown_fence,
    "unit_freetown_island_speaker": freetown_island_speaker,
    "unit_university_practitioner": university_practitioner,
    "unit_university_poison_specialist": university_poison_specialist,
    "unit_university_chancellor": university_chancellor,
    "unit_university_arms_dean": university_arms_dean,
    "unit_university_climate_dean": university_climate_dean,
    "unit_university_archivist": university_archivist,
    "unit_university_provost": university_provost,
    "unit_church_mason": church_mason,
    "unit_church_sun_priest": church_sun_priest,
    "unit_church_sun_stalker": church_sun_stalker,
    "unit_church_purifier": church_purifier,
    "unit_church_inquisitor": church_inquisitor,
    "unit_church_eclipse_warden": church_eclipse_warden,
    "unit_church_dawn_herald": church_dawn_herald,
    "unit_church_reliquary": church_reliquary,
    "unit_pathfinder": unit_pathfinder,
    "unit_sapper": unit_sapper,
}
