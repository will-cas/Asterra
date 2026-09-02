"""Faction keeps. Observatory-grade construction; each keep keeps its own language."""
from __future__ import annotations

import math

from asterra_detail import (
    arch,
    ashlar_face,
    banner,
    door,
    finish,
    log_wall,
    pitched,
    slit_window,
    stone_drum,
    stone_shaft,
    window,
)


def keep_arcaneum(g, m, c):
    """Uncrowned: dark ashlar shaft, iron-glass bays, connected buttresses, crystal lantern."""
    p = []
    stone, frame = m.dark_stone, m.iron

    p.append(g.cyl("plinth", (0, 0, 0.28), 4.55, 0.56, stone, c, verts=8, uv=0.22))
    p.append(g.cyl("plinth_cap", (0, 0, 0.58), 4.68, 0.1, m.steel, c, verts=8, uv=0.35))
    stone_drum(g, p, c, stone, 0.65, 9.15, 3.55, verts=8, course_h=0.18, uv=0.2)
    p.append(g.cyl("string", (0, 0, 4.85), 3.72, 0.12, m.steel, c, verts=8, uv=0.35))
    p.append(g.cyl("cornice", (0, 0, 9.22), 3.82, 0.14, m.steel, c, verts=8, uv=0.32))
    p.append(g.cyl("walk", (0, 0, 9.42), 4.05, 0.12, m.iron, c, verts=8, uv=0.3))
    for k in range(8):
        ang = k * math.pi / 4 + math.pi / 8
        bx, by = math.cos(ang) * 3.85, math.sin(ang) * 3.85
        p.append(g.cube(f"butt{k}", (bx, by, 4.15), (0.72, 0.72, 6.4), stone, c, rot=(0, 0, ang), uv=0.22))
        p.append(g.cube(f"bcap{k}", (bx * 1.02, by * 1.02, 7.35), (0.82, 0.82, 0.16), m.steel, c, rot=(0, 0, ang)))
        if math.sin(ang) > -0.35:
            for z in (2.55, 4.55, 6.55, 8.15):
                slit_window(
                    g, p, c, frame, m.glass,
                    (math.cos(ang) * 3.52, math.sin(ang) * 3.52, z),
                    (0.16, 0.58, 1.25), yaw=ang,
                )
    for k in range(8):
        ang = k * math.pi / 4
        p.append(g.cube(f"mer{k}", (math.cos(ang) * 3.85, math.sin(ang) * 3.85, 9.72), (0.32, 0.32, 0.52), m.iron, c))
    p.append(g.cube("gateh", (0, -4.85, 2.35), (4.15, 2.55, 3.55), stone, c, uv=0.22))
    pitched(g, p, c, m.slate, (0, -4.85, 4.25), (4.55, 2.95, 0.12), pitch=24, gable=stone, tiles=False)
    ashlar_face(g, p, c, stone, -6.1, 0.55, 3.85, -1.85, 1.85, depth=0.07, bw=0.34, bh=0.18)
    arch(g, p, c, m.steel, (0, -6.15, 0), radius=1.15, depth=0.55, z0=2.35, count=11, block=(0.3, 0.52, 0.24))
    door(g, p, c, m.steel, frame, (0, -6.22, 1.15), (0.78, 0.12, 1.85))
    p.append(g.cube("dglass", (0, -6.32, 1.45), (1.05, 0.04, 0.95), m.glass, c))
    banner(g, p, c, m.steel, m.cloth_purple, (1.85, -5.05, 4.55), h=2.55, fly=1.15)
    stone_drum(g, p, c, stone, 9.55, 11.35, 2.15, verts=16, course_h=0.16, uv=0.22)
    p.append(g.cyl("lantern_ring", (0, 0, 11.45), 2.28, 0.08, m.steel, c, verts=16, uv=0.35))
    for k in range(8):
        ang = k * math.pi / 4
        p.append(g.cube(f"lrib{k}", (math.cos(ang) * 1.95, math.sin(ang) * 1.95, 12.15), (0.08, 0.08, 1.45), frame, c, rot=(0, 0, ang)))
        slit_window(
            g, p, c, frame, m.glass,
            (math.cos(ang) * 2.12, math.sin(ang) * 2.12, 10.35),
            (0.12, 0.48, 1.05), yaw=ang,
        )
    p.append(g.cyl("cage", (0, 0, 12.15), 1.85, 1.45, m.glass, c, verts=16, uv=0.4))
    p.append(g.ico("jewel", (0, 0, 12.15), 0.62, m.crystal, c, subdiv=2, scale=(1.0, 1.0, 1.2)))
    p.append(g.cone("lcap", (0, 0, 13.05), 2.05, 0.65, m.slate, c, verts=12))
    p.append(g.cyl("fin", (0, 0, 13.48), 0.05, 0.28, frame, c, verts=6))
    p.append(g.uv_sphere("star", (0, 0, 13.72), 0.1, m.crystal, c, segs=12, rings=8))
    return finish(g, "building_arcaneum", p, c, 0.012, 5)


def keep_great_camp(g, m, c):
    """Outcast longhouse: stacked logs, ice ridge, palisade, leather door. No masonry."""
    p = []
    p.append(g.cube("plinth", (0, 0.3, 0.28), (7.85, 14.6, 0.4), m.bark, c, uv=0.35))
    log_wall(g, p, c, m.wood, -3.55, 0.3, 0.55, 14.2, count=16, r=0.14, axis="y")
    log_wall(g, p, c, m.wood, 3.55, 0.3, 0.55, 14.2, count=16, r=0.14, axis="y")
    for y in (-6.15, -3.05, 0.3, 3.65, 6.75):
        p.append(g.cyl(f"postw{y}", (-3.72, y, 2.45), 0.2, 3.95, m.bark, c, verts=8))
        p.append(g.cyl(f"poste{y}", (3.72, y, 2.45), 0.2, 3.95, m.bark, c, verts=8))
        p.append(g.cube(f"knee_w{y}", (-3.72, y, 4.35), (0.38, 0.38, 0.2), m.bark, c))
        p.append(g.cube(f"knee_e{y}", (3.72, y, 4.35), (0.38, 0.38, 0.2), m.bark, c))
    p.append(g.cube("front", (0, -6.75, 2.35), (7.05, 0.34, 3.45), m.wood, c, uv=0.4))
    p.append(g.cube("back", (0, 7.35, 2.35), (7.05, 0.34, 3.45), m.wood, c, uv=0.4))
    pitched(g, p, c, m.bark, (0, 0.3, 4.08), (7.25, 14.5, 0.16), pitch=40, gable=m.wood, axis="y")
    p.append(g.cube("ice_ridge", (0, 0.3, 7.05), (0.35, 14.2, 0.12), m.ice, c))
    p.append(g.cube("lintel", (0, -6.95, 2.75), (2.55, 0.28, 0.26), m.bark, c))
    door(g, p, c, m.leather, m.iron, (0, -6.98, 1.4), (0.82, 0.12, 2.15))
    p.append(g.cyl("post_l", (-1.22, -6.92, 1.65), 0.16, 2.55, m.bark, c, verts=8))
    p.append(g.cyl("post_r", (1.22, -6.92, 1.65), 0.16, 2.55, m.bark, c, verts=8))
    for i in range(4):
        p.append(g.cube(f"icicle{i}", (-1.75 + i * 1.15, -6.9, 4.45), (0.07, 0.07, 0.38 + (i % 2) * 0.14), m.ice, c))
    p.append(g.cyl("smoke", (0.55, 3.55, 6.95), 0.24, 1.25, m.bark, c, verts=10))
    p.append(g.cube("smoke_cap", (0.55, 3.55, 7.62), (0.5, 0.5, 0.1), m.ice, c))
    for i in range(16):
        ang = i * math.pi / 8 + 0.2
        if math.sin(ang) < -0.62:
            continue
        r = 8.55
        p.append(g.cyl(f"pal{i}", (math.cos(ang) * r, math.sin(ang) * r, 1.05), 0.15, 1.95, m.wood, c, verts=6))
        p.append(g.cone(f"pt{i}", (math.cos(ang) * r, math.sin(ang) * r, 2.12), 0.13, 0.4, m.ice, c, verts=5))
    banner(g, p, c, m.wood, m.cloth_green, (-2.35, -6.9, 4.85), h=2.45, fly=1.15)
    return finish(g, "building_outcast_great_camp", p, c, 0.016, 4)


def keep_tavern(g, m, c):
    """Freetown pub: brick quay, jettied pale timber, slate, dock, hanging sign."""
    p = []
    p.append(g.cube("quay", (0, 0.15, 0.22), (10.4, 12.4, 0.36), m.brick, c, uv=0.28))
    p.append(g.cube("stone", (0, 0.7, 1.55), (7.05, 10.05, 2.05), m.brick, c, uv=0.24))
    ashlar_face(g, p, c, m.brick, -4.35, 0.55, 2.45, -3.15, 3.15, depth=0.07, bw=0.38, bh=0.2)
    p.append(g.cube("quoin_l", (-3.42, -4.15, 1.55), (0.68, 0.68, 2.1), m.brick, c, uv=0.28))
    p.append(g.cube("quoin_r", (3.42, -4.15, 1.55), (0.68, 0.68, 2.1), m.brick, c, uv=0.28))
    p.append(g.cube("timber", (0, 0.7, 3.65), (7.55, 10.55, 2.1), m.pale_wood, c, uv=0.35))
    p.append(g.cube("jetty", (0, -4.55, 2.62), (7.55, 0.85, 0.16), m.wood, c))
    for x in (-2.75, -0.9, 0.9, 2.75):
        p.append(g.cube(f"brkt{x}", (x, -4.7, 2.35), (0.16, 0.55, 0.38), m.wood, c, rot=(math.radians(18), 0, 0)))
        p.append(g.cube(f"stud{x}", (x, 0.7, 3.65), (0.12, 10.4, 2.05), m.wood, c))
    pitched(g, p, c, m.slate, (0, 0.7, 4.68), (7.7, 10.7, 0.16), pitch=38, gable=m.pale_wood, axis="y")
    p.append(g.cyl("chim", (1.25, 2.55, 6.85), 0.32, 1.55, m.brick, c, verts=12, uv=0.3))
    p.append(g.cube("chim_cap", (1.25, 2.55, 7.68), (0.58, 0.58, 0.1), m.slate, c))
    p.append(g.cube("pot", (1.25, 2.55, 7.88), (0.2, 0.2, 0.26), m.brick, c))
    for x in (-1.65, 1.65):
        window(g, p, c, m.wood, m.glass, (x, -4.62, 3.75), (1.15, 0.18, 1.2))
        window(g, p, c, m.wood, m.glass, (x, -4.42, 1.55), (0.95, 0.18, 0.95))
    p.append(g.cube("lintel", (0, -5.0, 2.35), (2.05, 0.26, 0.2), m.wood, c))
    door(g, p, c, m.wood, m.iron, (0, -5.08, 1.12), (0.72, 0.12, 1.55))
    p.append(g.cube("signp", (1.65, -4.95, 3.25), (0.08, 0.08, 1.0), m.wood, c))
    p.append(g.cube("sign", (1.65, -5.2, 2.85), (1.22, 0.07, 0.55), m.cloth_blue, c))
    p.append(g.cube("dock", (0, -7.05, 0.18), (9.6, 3.65, 0.16), m.wood, c, uv=0.4))
    for i in range(-4, 5):
        p.append(g.cube(f"plank{i}", (i * 1.0, -7.05, 0.28), (0.9, 3.55, 0.05), m.pale_wood, c))
        p.append(g.cyl(f"pile{i}", (i * 1.0, -8.75, 0.15), 0.15, 1.2, m.wood, c, verts=8))
        p.append(g.cyl(f"band{i}", (i * 1.0, -8.75, 0.52), 0.17, 0.05, m.iron, c, verts=8))
    for i in range(4):
        p.append(g.cyl(f"barrel{i}", (-2.15 + i * 0.82, -5.4, 0.58), 0.25, 0.5, m.wood, c, verts=12))
        p.append(g.cyl(f"hoop{i}", (-2.15 + i * 0.82, -5.4, 0.74), 0.26, 0.04, m.iron, c, verts=12))
    p.append(g.cube("net", (2.75, -7.7, 1.05), (1.65, 0.05, 1.15), m.cloth_blue, c, rot=(math.radians(16), 0, 0)))
    return finish(g, "building_freetown_tavern", p, c, 0.014, 4)


def keep_college(g, m, c):
    """University hall: coursed brick, marble portico, clock turret — not an observatory."""
    p = []
    brick, trim, frame = m.red_brick, m.marble, m.iron

    p.append(g.cube("plinth", (0, 0.45, 0.18), (15.6, 8.85, 0.22), trim, c, uv=0.28))
    p.append(g.cube("hall", (0, 0.5, 2.65), (14.35, 7.55, 4.35), brick, c, uv=0.22))
    ashlar_face(g, p, c, brick, -3.28, 0.55, 4.65, -6.85, 6.85, depth=0.07, bw=0.4, bh=0.2)
    p.append(g.cube("string", (0, 0.5, 4.58), (14.85, 8.05, 0.12), trim, c, uv=0.35))
    p.append(g.cube("cornice", (0, 0.5, 4.72), (15.05, 8.25, 0.1), m.slate, c))
    for i in range(-3, 4):
        x = i * 2.0
        p.append(g.cube(f"butt{i}", (x, -4.05, 2.15), (0.55, 0.72, 3.55), brick, c, uv=0.22))
        p.append(g.cube(f"bcap{i}", (x, -4.12, 3.95), (0.65, 0.82, 0.14), trim, c))
    pitched(g, p, c, m.slate, (-1.35, 0.5, 4.88), (11.4, 7.85, 0.16), pitch=34, gable=brick, tiles=False)
    for i in range(6):
        x = -5.35 + i * 2.12
        window(g, p, c, frame, m.glass, (x, -3.28, 3.45), (1.15, 0.18, 1.85))
        slit_window(g, p, c, frame, m.glass, (x, -3.28, 1.45), (0.85, 0.14, 0.95))
    p.append(g.cube("portico", (0, -4.15, 0.42), (6.55, 1.75, 0.16), trim, c, uv=0.3))
    for x in (-2.35, -0.8, 0.8, 2.35):
        p.append(g.taper(f"col{x}", (x, -4.35, 2.05), 0.22, 0.17, 2.35, trim, c, verts=10))
    p.append(g.cube("entab", (0, -4.35, 3.28), (6.35, 1.2, 0.18), trim, c))
    door(g, p, c, m.wood, frame, (0, -3.45, 1.15), (0.78, 0.12, 1.75))
    tx, ty = 6.45, -1.45
    p.append(g.cube("tower_base", (tx, ty, 1.15), (3.65, 3.65, 1.85), brick, c, uv=0.22))
    stone_shaft(g, p, c, brick, 2.05, 8.35, 1.95, 1.72, verts=8, course_h=0.18, uv=0.2, cx=tx, cy=ty)
    p.append(g.cyl("tstring", (tx, ty, 8.48), 1.95, 0.12, trim, c, verts=8, uv=0.35))
    p.append(g.cyl("twalk", (tx, ty, 8.68), 2.12, 0.12, m.slate, c, verts=8, uv=0.3))
    for k in range(8):
        ang = k * math.pi / 4
        p.append(g.cube(f"tmer{k}", (tx + math.cos(ang) * 1.95, ty + math.sin(ang) * 1.95, 9.02), (0.32, 0.32, 0.55), m.slate, c))
    p.append(g.cone("troof", (tx, ty, 9.35), 1.85, 0.85, m.slate, c, verts=8))
    p.append(g.cyl("fin", (tx, ty, 9.88), 0.05, 0.28, trim, c, verts=6))
    p.append(g.cyl("dial", (tx, ty - 1.82, 6.55), 0.78, 0.1, m.gold, c, verts=20, rot=(math.radians(90), 0, 0)))
    p.append(g.cyl("dial_rim", (tx, ty - 1.88, 6.55), 0.84, 0.05, frame, c, verts=20, rot=(math.radians(90), 0, 0)))
    for k in range(12):
        ang = k * math.pi / 6
        p.append(g.cube(
            f"tick{k}",
            (tx + math.cos(ang) * 0.62, ty - 1.92, 6.55 + math.sin(ang) * 0.62),
            (0.04, 0.04, 0.1 if k % 3 else 0.16),
            frame,
            c,
        ))
    p.append(g.cube("hand_h", (tx, ty - 1.94, 6.55), (0.38, 0.04, 0.05), frame, c))
    p.append(g.cube("hand_m", (tx, ty - 1.94, 6.78), (0.05, 0.04, 0.32), frame, c))
    for i, x in enumerate((-4.35, -1.45, 1.35, 4.05)):
        p.append(g.cyl(f"chim{i}", (x, 2.45, 6.85), 0.24, 1.25, brick, c, verts=10, uv=0.3))
        p.append(g.cube(f"cc{i}", (x, 2.45, 7.52), (0.44, 0.44, 0.1), m.slate, c))
    banner(g, p, c, m.wood, m.cloth_deep, (4.25, -3.55, 5.85), h=2.45, fly=1.1)
    return finish(g, "building_university_grand_college", p, c, 0.012, 5)


def keep_temple(g, m, c):
    """Rising Sun: marble portico, nave, coursed drum, ribbed gold dome, sun disc."""
    p = []
    stone, sacred, frame = m.marble, m.gold, m.iron

    p.append(g.cube("stylobate", (0, 0.55, 0.28), (10.4, 14.2, 0.42), stone, c, uv=0.24))
    p.append(g.cube("nave", (0, 1.55, 2.95), (8.55, 12.15, 4.85), stone, c, uv=0.2))
    ashlar_face(g, p, c, stone, -4.5, 0.55, 5.05, -4.05, 4.05, depth=0.07, bw=0.38, bh=0.2)
    p.append(g.cube("string", (0, 1.55, 5.18), (8.95, 12.55, 0.1), stone, c, uv=0.32))
    p.append(g.cube("cornice", (0, 1.55, 5.3), (9.15, 12.75, 0.1), stone, c))
    pitched(g, p, c, m.slate, (0, -1.15, 5.38), (8.7, 6.55, 0.16), pitch=22, gable=stone)
    for i in range(4):
        y = -1.85 + i * 2.25
        window(g, p, c, frame, m.glass, (4.32, y, 3.25), (0.16, 1.05, 1.95), yaw=math.radians(90))
        window(g, p, c, frame, m.glass, (-4.32, y, 3.25), (0.16, 1.05, 1.95), yaw=math.radians(90))
    p.append(g.cube("porch_deck", (0, -5.85, 0.62), (9.85, 3.35, 0.38), stone, c, uv=0.26))
    for i, x in enumerate((-3.55, -2.13, -0.71, 0.71, 2.13, 3.55)):
        p.append(g.cube(f"base{i}", (x, -6.05, 0.95), (0.48, 0.48, 0.28), stone, c))
        p.append(g.taper(f"col{i}", (x, -6.05, 2.55), 0.26, 0.2, 2.95, stone, c, verts=12))
        p.append(g.cube(f"cap{i}", (x, -6.05, 4.12), (0.48, 0.44, 0.16), sacred, c))
    p.append(g.cube("architrave", (0, -6.05, 4.35), (9.25, 1.15, 0.22), stone, c))
    p.append(g.cube("frieze", (0, -6.05, 4.55), (9.15, 1.1, 0.16), stone, c))
    p.append(g.cyl("sun", (0, -6.55, 4.55), 0.42, 0.08, sacred, c, verts=16, rot=(math.radians(90), 0, 0)))
    p.append(g.cube("pcornice", (0, -6.05, 4.72), (9.45, 1.28, 0.14), stone, c))
    pitched(g, p, c, stone, (0, -6.05, 4.88), (9.15, 1.45, 0.12), pitch=24, gable=stone)
    arch(g, p, c, stone, (0, -4.55, 0), radius=1.05, depth=0.5, z0=2.35, count=11, block=(0.28, 0.48, 0.22))
    door(g, p, c, m.wood, frame, (0, -4.62, 1.15), (0.82, 0.12, 1.85))
    cy = 3.35
    stone_drum(g, p, c, stone, 6.15, 8.05, 2.15, verts=18, course_h=0.16, uv=0.22, cx=0.0, cy=cy)
    p.append(g.cyl("drum_ring", (0, cy, 8.15), 2.28, 0.08, sacred, c, verts=18, uv=0.4))
    for k in range(8):
        ang = k * math.pi / 4
        slit_window(
            g, p, c, frame, m.glass,
            (math.cos(ang) * 2.12, cy + math.sin(ang) * 2.12, 7.05),
            (0.14, 0.48, 1.05), yaw=ang,
        )
    p.append(g.ico("dome", (0, cy, 9.05), 2.22, sacred, c, subdiv=4, scale=(1.0, 1.0, 0.52)))
    for k in range(8):
        ang = k * math.pi / 8
        p.append(g.cube(f"drib{k}", (0, cy, 9.35), (0.05, 1.55, 0.72), m.iron, c, rot=(math.radians(10), 0, ang)))
    p.append(g.cyl("lantern", (0, cy, 10.55), 0.32, 0.65, stone, c, verts=10, uv=0.3))
    p.append(g.cone("lantern_c", (0, cy, 11.02), 0.38, 0.42, sacred, c, verts=8))
    p.append(g.cyl("fin", (0, cy, 11.32), 0.04, 0.22, sacred, c, verts=6))
    banner(g, p, c, m.wood, m.cloth_sun, (3.25, -5.85, 6.55), h=2.45, fly=1.15)
    return finish(g, "building_church_grand_temple", p, c, 0.012, 5)


KEEPS = {
    "building_arcaneum": keep_arcaneum,
    "building_outcast_great_camp": keep_great_camp,
    "building_freetown_tavern": keep_tavern,
    "building_university_grand_college": keep_college,
    "building_church_grand_temple": keep_temple,
}
