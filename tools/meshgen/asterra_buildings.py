"""Unique faction buildings. Each id has its own massing — no shared hall kit."""
from __future__ import annotations

import math

from asterra_detail import (
    arch,
    banner,
    chimney,
    cornice,
    door,
    facade_windows,
    finish,
    ladder_to,
    log_wall,
    pitched,
    quoins,
    side_windows,
    stairs,
    string_course,
    timber_posts,
    wall_merlons,
    window,
    ashlar_face,
    slit_window,
    stone_drum,
    stone_shaft,
)


def _earth(g, p, c, mat, s=(8, 8, 0.36)):
    return


# --- Uncrowned: glass and steel ---

def bld_arcane_academy(g, m, c):
    """Two dark-stone wings around a crystal court."""
    p = []
    stone, frame = m.dark_stone, m.iron
    p.append(g.cube("court", (0, 0.15, 0.22), (5.6, 4.8, 0.16), stone, c, uv=0.28))
    p.append(g.cube("wing_l", (-4.45, 0.3, 1.95), (4.05, 7.05, 3.05), stone, c, uv=0.2))
    p.append(g.cube("wing_r", (4.45, 0.3, 1.95), (4.05, 7.05, 3.05), stone, c, uv=0.2))
    ashlar_face(g, p, c, stone, -3.22, 0.45, 3.35, -6.35, -2.55, depth=0.07, bw=0.36, bh=0.18)
    ashlar_face(g, p, c, stone, -3.22, 0.45, 3.35, 2.55, 6.35, depth=0.07, bw=0.36, bh=0.18)
    p.append(g.cube("link", (0, 2.35, 1.55), (5.05, 2.05, 2.15), stone, c, uv=0.22))
    string_course(g, p, c, m.steel, (-4.45, 0.3, 3.42), (4.2, 7.2, 0.1))
    string_course(g, p, c, m.steel, (4.45, 0.3, 3.42), (4.2, 7.2, 0.1))
    pitched(g, p, c, m.slate, (-4.45, 0.3, 3.52), (4.25, 7.25, 0.14), pitch=24, gable=stone)
    pitched(g, p, c, m.slate, (4.45, 0.3, 3.52), (4.25, 7.25, 0.14), pitch=24, gable=stone)
    for x in (-4.45, 4.45):
        facade_windows(g, p, c, frame, m.glass, -3.22, 2.05, (x - 1.05, x + 1.05), (0.85, 0.14, 1.15))
    p.append(g.ico("orb", (0, 0.1, 1.15), 0.58, m.crystal, c, subdiv=2, scale=(1.0, 1.0, 1.05)))
    p.append(g.cube("gate", (0, -3.15, 1.25), (2.15, 1.05, 1.85), stone, c, uv=0.24))
    arch(g, p, c, stone, (0, -3.65, 0), radius=0.72, depth=0.42, z0=1.55, count=9, block=(0.24, 0.42, 0.2))
    door(g, p, c, m.wood, frame, (0, -3.72, 1.05), (0.58, 0.1, 1.45))
    return finish(g, "building_arcane_academy", p, c, 0.014, 4)


def bld_conservatory(g, m, c):
    """Iron greenhouse: glass barrel, no house roof."""
    p = []
    p.append(g.cube("bed", (0, 0.4, 0.28), (8.4, 10.2, 0.28), m.dark_stone, c, uv=0.3))
    p.append(g.cube("plinth", (0, 0.4, 0.72), (8.0, 9.8, 0.42), m.steel, c, uv=0.35))
    p.append(g.cube("vault", (0, 0.4, 3.15), (7.4, 9.2, 3.55), m.glass, c, uv=0.4))
    for i in range(-3, 4):
        p.append(g.cube(f"rib{i}", (0, i * 1.35, 3.15), (7.5, 0.1, 3.65), m.steel, c))
        p.append(g.cube(f"vr{i}", (i * 1.05, 0.4, 3.15), (0.1, 9.3, 3.65), m.steel, c))
    p.append(g.cube("ridge", (0, 0.4, 5.05), (0.18, 9.4, 0.18), m.steel, c))
    p.append(g.cube("porch", (0, -5.0, 1.15), (3.4, 2.05, 1.55), m.steel, c, uv=0.32))
    door(g, p, c, m.steel, m.iron, (0, -6.02, 0.95), (0.7, 0.1, 1.45))
    for i in range(6):
        p.append(g.cyl(f"pot{i}", (-2.4 + i * 0.85, -4.35, 0.72), 0.2, 0.38, m.dark_stone, c, verts=8))
    return finish(g, "building_blackroot_conservatory", p, c, 0.016, 4)


def bld_ruins(g, m, c):
    """Broken colonnade, no roof."""
    p = []
    p.append(g.cube("stylobate", (0, 0, 0.28), (9.2, 8.4, 0.36), m.dark_stone, c, uv=0.24))
    for i, x in enumerate((-3.2, -1.05, 1.05, 3.2)):
        h = 3.4 if i != 2 else 1.55
        p.append(g.taper(f"col{i}", (x, -3.2, 0.5 + h * 0.5), 0.3, 0.22, h, m.dark_stone, c, verts=8))
        if i != 2:
            p.append(g.taper(f"colb{i}", (x, 3.0, 2.15), 0.3, 0.22, 3.05, m.dark_stone, c, verts=8))
    p.append(g.cube("entab", (0, -3.2, 3.95), (8.2, 1.05, 0.24), m.steel, c, rot=(0, 0, math.radians(6))))
    p.append(g.cube("rubble", (2.4, 0.6, 0.55), (2.6, 2.05, 0.8), m.dark_stone, c, rot=(0, 0, math.radians(18))))
    p.append(g.cube("fallen", (-1.2, 1.4, 0.72), (3.4, 0.5, 0.5), m.dark_stone, c, rot=(0, math.radians(12), math.radians(70))))
    return finish(g, "building_ancient_ruins", p, c, 0.016, 4)


def bld_conjuring_hall(g, m, c):
    """Round ritual chamber of dark stone and crystal."""
    p = []
    stone, frame = m.dark_stone, m.iron
    p.append(g.cyl("plinth", (0, 0, 0.18), 4.25, 0.36, stone, c, verts=20, uv=0.24))
    stone_drum(g, p, c, stone, 0.38, 4.05, 3.85, verts=20, course_h=0.18, uv=0.22)
    p.append(g.cyl("ring", (0, 0, 4.12), 4.05, 0.12, m.steel, c, verts=20, uv=0.35))
    p.append(g.ico("dome", (0, 0, 5.25), 2.45, m.crystal, c, subdiv=3, scale=(1.0, 1.0, 0.52)))
    for k in range(6):
        ang = k * math.pi / 6
        p.append(g.cube(f"drib{k}", (0, 0, 5.45), (0.05, 1.55, 0.55), frame, c, rot=(math.radians(8), 0, ang)))
    p.append(g.cyl("oculus", (0, 0, 6.55), 0.42, 0.22, frame, c, verts=12))
    for k in range(8):
        ang = k * math.pi / 4
        if math.sin(ang) < -0.7:
            continue
        slit_window(
            g, p, c, frame, m.glass,
            (math.cos(ang) * 3.82, math.sin(ang) * 3.82, 2.15),
            (0.14, 0.72, 1.35), yaw=ang,
        )
    p.append(g.cyl("dais", (0, 0, 0.55), 1.45, 0.28, stone, c, verts=12, uv=0.3))
    p.append(g.cube("gate", (0, -4.15, 1.35), (2.15, 1.05, 2.05), stone, c, uv=0.22))
    arch(g, p, c, stone, (0, -4.68, 0), radius=0.82, depth=0.48, z0=1.65, count=10, block=(0.26, 0.46, 0.22))
    door(g, p, c, m.wood, frame, (0, -4.72, 1.05), (0.58, 0.1, 1.55))
    return finish(g, "building_conjuring_hall", p, c, 0.012, 5)


def bld_high_temple(g, m, c):
    """Heretical nave and dark spire; crystal sits in an iron crown, not a lantern."""
    p = []
    stone, frame = m.dark_stone, m.iron

    p.append(g.cube("stylobate", (0, 0.35, 0.22), (7.4, 8.6, 0.44), stone, c, uv=0.22))
    p.append(g.cube("nave", (0, 0.55, 2.15), (5.05, 7.35, 3.55), stone, c, uv=0.2))
    ashlar_face(g, p, c, stone, -3.1, 0.5, 3.7, -2.2, 2.2, depth=0.07, bw=0.36, bh=0.18)
    p.append(g.cube("string", (0, 0.55, 3.85), (5.35, 7.65, 0.12), m.steel, c, uv=0.35))
    for x, y in ((-2.7, -2.85), (2.7, -2.85), (-2.7, 3.85), (2.7, 3.85)):
        p.append(g.cube(f"butt{x}{y}", (x, y, 1.95), (0.7, 0.7, 3.15), stone, c, uv=0.22))
        p.append(g.cube(f"bcap{x}{y}", (x, y, 3.55), (0.82, 0.82, 0.14), m.steel, c))
    pitched(g, p, c, m.slate, (0, 0.55, 3.95), (5.25, 7.45, 0.14), pitch=32, gable=stone)
    p.append(g.cube("porch", (0, -3.85, 1.45), (3.15, 1.85, 2.35), stone, c, uv=0.24))
    pitched(g, p, c, m.slate, (0, -3.85, 2.7), (3.45, 2.15, 0.12), pitch=28, gable=stone, tiles=False)
    arch(g, p, c, stone, (0, -4.75, 0), radius=0.92, depth=0.52, z0=1.9, count=11, block=(0.28, 0.5, 0.24))
    door(g, p, c, m.wood, frame, (0, -4.8, 1.05), (0.62, 0.1, 1.55))
    for x in (-1.55, 1.55):
        slit_window(g, p, c, frame, m.glass, (x, -3.72, 2.35), (0.42, 0.14, 1.35))
    for y in (-1.15, 0.85, 2.65):
        slit_window(g, p, c, frame, m.glass, (-2.55, y, 2.15), (0.14, 0.42, 1.45), yaw=math.radians(90))
        slit_window(g, p, c, frame, m.glass, (2.55, y, 2.15), (0.14, 0.42, 1.45), yaw=math.radians(-90))
    p.append(g.cube("crossing", (0, 0.55, 5.55), (2.85, 2.85, 2.05), stone, c, uv=0.22))
    p.append(g.cyl("collar", (0, 0.55, 6.65), 1.72, 0.22, m.steel, c, verts=16, uv=0.32))
    stone_shaft(g, p, c, stone, 6.75, 13.35, 1.55, 0.72, verts=16, course_h=0.16, uv=0.2, cx=0.0, cy=0.55)
    for z in (8.35, 10.55, 12.45):
        t = (z - 6.75) / 6.6
        rr = 1.55 + (0.72 - 1.55) * t
        p.append(g.cyl(f"band{z}", (0, 0.55, z), rr + 0.08, 0.08, m.steel, c, verts=18, uv=0.35))
        slit_window(g, p, c, frame, m.glass, (0, 0.55 - rr - 0.01, z), (0.32, 0.12, 0.95))
        slit_window(g, p, c, frame, m.glass, (0, 0.55 + rr + 0.01, z), (0.32, 0.12, 0.95), yaw=math.pi)
    p.append(g.cyl("neck", (0, 0.55, 13.55), 0.82, 0.22, m.steel, c, verts=12, uv=0.4))
    for k in range(6):
        ang = k * math.pi / 3
        p.append(g.cube(
            f"prong{k}",
            (math.cos(ang) * 0.55, 0.55 + math.sin(ang) * 0.55, 14.15),
            (0.08, 0.08, 1.15),
            frame,
            c,
            rot=(math.radians(18), 0, ang),
        ))
    p.append(g.ico("heart", (0, 0.55, 14.35), 0.48, m.crystal, c, subdiv=2, scale=(1.0, 1.0, 1.25)))
    p.append(g.ico("tip", (0, 0.55, 15.05), 0.22, m.crystal, c, subdiv=2, scale=(0.7, 0.7, 1.6)))
    p.append(g.cyl("fin", (0, 0.55, 15.45), 0.04, 0.28, frame, c, verts=6))
    return finish(g, "building_high_temple", p, c, 0.012, 5)


def bld_portal_gate(g, m, c):
    """Standing ring gate, no hall."""
    p = []
    p.append(g.cube("sill", (0, 0, 0.22), (6.2, 2.05, 0.32), m.steel, c, uv=0.32))
    p.append(g.cube("pier_l", (-2.35, 0, 2.25), (0.85, 1.45, 4.05), m.steel, c, uv=0.28))
    p.append(g.cube("pier_r", (2.35, 0, 2.25), (0.85, 1.45, 4.05), m.steel, c, uv=0.28))
    p.append(g.cyl("ring", (0, -0.18, 2.45), 2.12, 0.26, m.steel, c, verts=20, rot=(math.radians(90), 0, 0)))
    p.append(g.cyl("ring2", (0, -0.04, 2.45), 2.32, 0.1, m.iron, c, verts=20, rot=(math.radians(90), 0, 0)))
    p.append(g.cyl("glass", (0, -0.18, 2.45), 1.72, 0.07, m.glass, c, verts=20, rot=(math.radians(90), 0, 0)))
    p.append(g.cube("lintel", (0, 0, 4.45), (5.4, 1.55, 0.28), m.iron, c))
    for k in range(8):
        ang = k * math.pi / 4
        p.append(g.cube(f"riv{k}", (math.cos(ang) * 2.12, -0.18, 2.45 + math.sin(ang) * 2.12), (0.16, 0.2, 0.16), m.iron, c))
    p.append(g.uv_sphere("spark", (0, -0.32, 2.45), 0.28, m.crystal, c, segs=10, rings=6))
    return finish(g, "building_portal_gate", p, c, 0.014, 4)


def bld_shadowed_gate(g, m, c):
    """Dark twin of the portal: iron ring and void cloth."""
    p = []
    p.append(g.cube("sill", (0, 0, 0.2), (6.4, 2.2, 0.28), m.dark_stone, c, uv=0.24))
    p.append(g.taper("pier_l", (-2.45, 0, 2.35), 0.65, 0.4, 4.15, m.dark_stone, c, verts=6))
    p.append(g.taper("pier_r", (2.45, 0, 2.35), 0.65, 0.4, 4.15, m.dark_stone, c, verts=6))
    p.append(g.cyl("ring", (0, -0.12, 2.28), 2.0, 0.2, m.iron, c, verts=16, rot=(math.radians(90), 0, 0)))
    p.append(g.cyl("void", (0, -0.12, 2.28), 1.6, 0.05, m.cloth_purple, c, verts=16, rot=(math.radians(90), 0, 0)))
    p.append(g.cube("thorns", (0, 0, 4.65), (5.6, 1.25, 0.55), m.steel, c, rot=(0, 0, math.radians(8))))
    return finish(g, "building_shadowed_gate", p, c, 0.014, 4)


def bld_watchtower(g, m, c):
    """Coursed lookout with a glass lantern and slate cone."""
    p = []
    stone, frame = m.dark_stone, m.iron
    p.append(g.cyl("plinth", (0, 0, 0.22), 1.72, 0.4, stone, c, verts=14, uv=0.24))
    stone_shaft(g, p, c, stone, 0.45, 7.85, 1.08, 0.78, verts=14, course_h=0.18, uv=0.22)
    for z in (2.15, 4.25, 6.15):
        slit_window(g, p, c, frame, m.glass, (0, -0.95, z), (0.28, 0.12, 0.95))
    p.append(g.cyl("walk", (0, 0, 8.05), 1.45, 0.1, m.slate, c, verts=12, uv=0.3))
    p.append(g.cyl("rail", (0, 0, 8.35), 1.42, 0.05, frame, c, verts=12, uv=0.4))
    for k in range(8):
        ang = k * math.pi / 4
        p.append(g.cyl(f"post{k}", (math.cos(ang) * 1.38, math.sin(ang) * 1.38, 8.28), 0.04, 0.32, frame, c, verts=6))
    p.append(g.cyl("cab", (0, 0, 8.85), 1.15, 1.15, m.glass, c, verts=12, uv=0.4))
    for i in range(-1, 2):
        p.append(g.cube(f"lx{i}", (i * 0.72, 0, 8.85), (0.07, 2.15, 1.15), frame, c))
        p.append(g.cube(f"ly{i}", (0, i * 0.72, 8.85), (2.15, 0.07, 1.15), frame, c))
    p.append(g.cone("roof", (0, 0, 9.65), 1.35, 0.72, m.slate, c, verts=10))
    p.append(g.cyl("fin", (0, 0, 10.12), 0.05, 0.32, m.steel, c, verts=6))
    door(g, p, c, m.wood, frame, (0, -1.15, 0.95), (0.48, 0.1, 1.25))
    return finish(g, "building_watchtower", p, c, 0.012, 5)


def bld_palisade(g, m, c):
    p = []
    p.append(g.cube("base", (0, 0, 0.85), (9.0, 0.7, 1.65), m.steel, c, uv=0.3))
    p.append(g.cube("walk", (0, 0, 1.78), (9.0, 0.95, 0.14), m.iron, c))
    p.append(g.cube("rail", (0, -0.42, 2.15), (9.0, 0.06, 0.08), m.iron, c))
    for i in range(-4, 5):
        p.append(g.cube(f"bar{i}", (i * 0.95, -0.32, 1.45), (0.08, 0.08, 1.25), m.steel, c))
        p.append(g.cube(f"gl{i}", (i * 0.95, -0.38, 1.35), (0.7, 0.07, 0.95), m.glass, c))
    p.append(g.cube("post_l", (-4.35, 0, 1.45), (0.45, 0.82, 2.65), m.dark_stone, c, uv=0.24))
    p.append(g.cube("post_r", (4.35, 0, 1.45), (0.45, 0.82, 2.65), m.dark_stone, c, uv=0.24))
    return finish(g, "building_palisade", p, c, 0.014)


def bld_outpost(g, m, c):
    """Small steel blockhouse with a roof turret."""
    p = []
    p.append(g.cube("plinth", (0, 0, 0.18), (5.15, 5.15, 0.28), m.steel, c, uv=0.3))
    p.append(g.cube("box", (0, 0, 1.45), (4.65, 4.65, 2.15), m.steel, c, uv=0.28))
    p.append(g.cube("cap", (0, 0, 2.62), (5.05, 5.05, 0.16), m.iron, c))
    p.append(g.cube("turret", (0, 0, 3.45), (2.05, 2.05, 1.35), m.steel, c, uv=0.3))
    p.append(g.cone("troof", (0, 0, 4.28), 1.25, 0.62, m.steel, c, verts=8))
    facade_windows(g, p, c, m.iron, m.glass, -2.35, 1.55, (-1.15, 1.15), (0.82, 0.14, 0.95))
    side_windows(g, p, c, m.iron, m.glass, -2.35, 1.55, (-1.15, 1.15), size=(0.14, 0.82, 0.95))
    door(g, p, c, m.steel, m.iron, (0, -2.38, 0.95), (0.55, 0.1, 1.25))
    wall_merlons(g, p, c, m.iron, -2.38, 2.88, (-1.5, 0, 1.5), depth=0.35, h=0.48)
    wall_merlons(g, p, c, m.iron, 2.38, 2.88, (-1.5, 0, 1.5), depth=0.35, h=0.48)
    return finish(g, "building_outpost", p, c, 0.014, 4)


# --- Mundor: limestone / slate ---

def bld_barracks(g, m, c):
    """Long two-door barracks, chimneys, not a square house."""
    p = []
    _earth(g, p, c, m.brick, (14, 8, 0.4))
    p.append(g.cube("plinth", (0, 0.2, 0.55), (13.6, 6.4, 0.3), m.brick, c))
    p.append(g.cube("hall", (0, 0.2, 2.05), (13.0, 5.8, 2.7), m.plaster, c))
    ashlar_face(g, p, c, m.brick, -2.72, 1.15, 3.15, -6.2, 6.2, depth=0.08)
    ashlar_face(g, p, c, m.brick, 3.12, 1.15, 3.15, -6.2, 6.2, depth=0.08)
    p.append(g.cube("string", (0, 0.2, 3.45), (13.4, 6.15, 0.14), m.brick, c))
    pitched(g, p, c, m.slate, (0, 0.2, 3.6), (13.6, 6.5, 0.16), pitch=32, gable=m.plaster, tiles=True)
    facade_windows(g, p, c, m.brick, m.glass, -2.75, 2.15, (-5.2, -3.4, -1.5, 0, 1.5, 3.4, 5.2), (0.95, 0.18, 1.2))
    quoins(g, p, c, m.brick, 0, 0.2, 2.05, 6.3, 2.7, 2.7)
    chimney(g, p, c, m.brick, m.slate, (-5.2, 1.4, 5.15), h=1.35, r=0.28)
    chimney(g, p, c, m.brick, m.slate, (5.2, 1.4, 5.15), h=1.35, r=0.28)
    door(g, p, c, m.wood, m.iron, (-2.8, -2.9, 1.15), (0.65, 0.12, 1.7))
    door(g, p, c, m.wood, m.iron, (2.8, -2.9, 1.15), (0.65, 0.12, 1.7))
    stairs(g, p, c, m.brick, (-2.8, -3.7, 0.3), count=5, width=2.0)
    stairs(g, p, c, m.brick, (2.8, -3.7, 0.3), count=5, width=2.0)
    return finish(g, "building_royal_barracks", p, c, 0.04, 3)


def bld_court(g, m, c):
    """Portico hall of justice."""
    p = []
    _earth(g, p, c, m.brick, (14, 12, 0.44))
    p.append(g.cube("hall", (0, 1.0, 2.5), (8.4, 8.2, 4.0), m.plaster, c))
    ashlar_face(g, p, c, m.brick, -3.12, 1.2, 4.2, -3.8, 3.8, depth=0.08)
    p.append(g.cube("string", (0, 1.0, 4.55), (8.8, 8.6, 0.16), m.brick, c))
    pitched(g, p, c, m.slate, (0, 1.0, 4.7), (9.0, 8.8, 0.16), pitch=26, gable=m.plaster, tiles=True)
    facade_windows(g, p, c, m.brick, m.glass, -3.15, 2.85, (-2.4, 0, 2.4), (1.05, 0.18, 1.45))
    quoins(g, p, c, m.brick, 0, 1.0, 2.5, 4.0, 3.9, 4.0)
    p.append(g.cube("styl", (0, -3.7, 0.7), (8.6, 2.8, 0.4), m.brick, c))
    for x in (-3.0, -1.0, 1.0, 3.0):
        p.append(g.taper(f"col{x}", (x, -3.9, 2.15), 0.22, 0.18, 2.7, m.brick, c, verts=8))
    p.append(g.cube("entab", (0, -3.9, 3.55), (8.4, 1.2, 0.22), m.brick, c))
    p.append(g.cube("ped_a", (0, -4.15, 4.15), (8.2, 1.4, 0.14), m.plaster, c, rot=(math.radians(18), 0, 0)))
    p.append(g.cube("ped_b", (0, -3.65, 4.15), (8.2, 1.4, 0.14), m.plaster, c, rot=(math.radians(-18), 0, 0)))
    door(g, p, c, m.wood, m.iron, (0, -3.15, 1.35), (0.8, 0.14, 2.0))
    stairs(g, p, c, m.brick, (0, -5.2, 0.3), count=6, width=6.0)
    return finish(g, "building_royal_court", p, c, 0.04, 3)


def bld_farm(g, m, c):
    """Barn + silo + pen."""
    p = []
    _earth(g, p, c, m.brick, (12, 11, 0.36))
    p.append(g.cube("barn", (0, 0.6, 2.15), (8.4, 6.6, 3.4), m.plaster, c))
    pitched(g, p, c, m.slate, (0, 0.6, 4.0), (9.0, 7.2, 0.16), pitch=38, gable=m.plaster, axis="y", tiles=True)
    p.append(g.cube("door_big", (0, -2.7, 1.55), (2.6, 0.16, 2.4), m.wood, c))
    p.append(g.cyl("silo", (5.2, 1.4, 2.4), 1.15, 4.2, m.plaster, c, verts=12))
    p.append(g.cone("silo_c", (5.2, 1.4, 4.7), 1.25, 0.85, m.slate, c, verts=10))
    p.append(g.cube("pen", (0, -5.0, 0.45), (7.2, 3.2, 0.7), m.wood, c))
    p.append(g.cube("hay", (2.4, -4.8, 1.0), (1.6, 1.4, 0.9), m.cloth, c))
    facade_windows(g, p, c, m.wood, m.glass, -2.75, 2.6, (-2.6, 2.6), (0.9, 0.16, 0.9))
    chimney(g, p, c, m.brick, m.slate, (-2.8, 1.6, 5.35), h=1.1, r=0.24)
    stairs(g, p, c, m.brick, (0, -3.5, 0.25), count=4, width=2.4)
    return finish(g, "building_royal_farm", p, c, 0.04, 3)


def bld_outpost_tower(g, m, c):
    """Round stone tower with merlons and a slate cone."""
    p = []
    p.append(g.cyl("base", (0, 0, 0.45), 2.25, 0.55, m.brick, c, verts=18, uv=0.35))
    stone_shaft(g, p, c, m.brick, 0.75, 7.55, 1.78, 1.38, verts=18, course_h=0.2, uv=0.32)
    p.append(g.cyl("walk", (0, 0, 7.72), 1.72, 0.16, m.slate, c, verts=16, uv=0.4))
    for k in range(8):
        ang = k * math.pi / 4
        p.append(g.cube(f"mer{k}", (math.cos(ang) * 1.52, math.sin(ang) * 1.52, 8.12), (0.36, 0.36, 0.62), m.slate, c))
    p.append(g.cone("roof", (0, 0, 8.75), 1.48, 1.25, m.slate, c, verts=12))
    p.append(g.cyl("fin", (0, 0, 9.55), 0.06, 0.38, m.gold, c, verts=6))
    window(g, p, c, m.brick, m.glass, (0, -1.62, 3.15), (0.65, 0.16, 1.15))
    window(g, p, c, m.brick, m.glass, (0, -1.48, 5.45), (0.52, 0.14, 0.95))
    arch(g, p, c, m.brick, (0, -1.95, 0), radius=0.72, depth=0.55, z0=1.45, count=7)
    door(g, p, c, m.wood, m.iron, (0, -2.05, 1.0), (0.52, 0.12, 1.45))
    stairs(g, p, c, m.brick, (0, -2.85, 0.22), count=5, width=1.7)
    return finish(g, "building_royal_outpost_tower", p, c, 0.016, 4)


def bld_royal_wall(g, m, c):
    p = []
    p.append(g.cube("base", (0, 0, 0.95), (8.8, 1.45, 1.85), m.brick, c))
    p.append(g.cube("walk", (0, 0, 2.0), (8.8, 1.65, 0.22), m.slate, c))
    for i in range(-3, 4):
        if i == 0:
            continue
        p.append(g.cube(f"mer{i}", (i * 1.1, 0, 2.4), (0.52, 0.52, 0.72), m.slate, c))
    p.append(g.cube("post_l", (-4.2, 0, 1.55), (0.75, 1.6, 2.8), m.brick, c))
    p.append(g.cube("post_r", (4.2, 0, 1.55), (0.75, 1.6, 2.8), m.brick, c))
    arch(g, p, c, m.brick, (0, -0.8, 0), radius=0.85, depth=0.7, z0=1.35, count=7)
    return finish(g, "building_royal_wall", p, c, 0.03)


def bld_keep_turret(g, m, c):
    """Small wall turret of coursed stone."""
    p = []
    stone_drum(g, p, c, m.brick, 0.15, 3.15, 1.55, verts=14, course_h=0.22, uv=0.38)
    p.append(g.cyl("walk", (0, 0, 3.28), 1.72, 0.14, m.slate, c, verts=12, uv=0.4))
    for k in range(8):
        ang = k * math.pi / 4
        p.append(g.cube(f"m{k}", (math.cos(ang) * 1.48, math.sin(ang) * 1.48, 3.58), (0.32, 0.32, 0.5), m.slate, c))
    p.append(g.cone("roof", (0, 0, 4.05), 1.42, 0.82, m.slate, c, verts=10))
    window(g, p, c, m.brick, m.glass, (0, -1.55, 1.75), (0.65, 0.14, 0.9))
    door(g, p, c, m.wood, m.iron, (0, -1.62, 0.82), (0.48, 0.1, 1.15))
    return finish(g, "building_keep_turret", p, c, 0.016, 4)


def bld_bridge(g, m, c):
    p = []
    for x in (-4.4, 4.4):
        p.append(g.cube(f"pier{x}", (x, 0, 0.75), (1.5, 2.4, 1.5), m.brick, c))
        p.append(g.cube(f"cap{x}", (x, 0, 1.55), (1.7, 2.6, 0.16), m.slate, c))
    p.append(g.cube("deck", (0, 0, 1.65), (10.8, 2.1, 0.18), m.wood, c))
    for i in range(-4, 5):
        p.append(g.cube(f"plank{i}", (i * 1.15, 0, 1.76), (1.05, 2.0, 0.05), m.pale_wood, c))
        p.append(g.cyl(f"pn{i}", (i * 1.15, 1.0, 1.95), 0.07, 0.9, m.wood, c, verts=6))
        p.append(g.cyl(f"ps{i}", (i * 1.15, -1.0, 1.95), 0.07, 0.9, m.wood, c, verts=6))
    p.append(g.cube("rail_n", (0, 1.0, 2.2), (10.6, 0.12, 0.7), m.wood, c))
    p.append(g.cube("rail_s", (0, -1.0, 2.2), (10.6, 0.12, 0.7), m.wood, c))
    return finish(g, "building_bridge", p, c, 0.03)


def bld_stone_wall(g, m, c):
    return bld_royal_wall(g, m, c)  # same language; rename after join


# --- Outcast: ice and wood ---

def bld_burrows(g, m, c):
    """Earth mound village, not a house."""
    p = []
    p.append(g.cube("snow", (0, 0, 0.16), (8, 8, 0.32), m.ice, c))
    p.append(g.ico("mound", (0, 0.4, 1.15), 3.4, m.bark, c, subdiv=2, scale=(1.4, 1.5, 0.55)))
    p.append(g.ico("mound2", (-1.6, 1.2, 0.85), 1.6, m.bark, c, subdiv=2, scale=(1.3, 1.2, 0.5)))
    p.append(g.ico("mound3", (1.8, 0.2, 0.75), 1.3, m.bark, c, subdiv=1, scale=(1.2, 1.3, 0.45)))
    p.append(g.cyl("hole", (0, -2.4, 0.95), 0.95, 0.7, m.dark_stone, c, verts=12, rot=(math.radians(90), 0, 0)))
    p.append(g.cube("lintel", (0, -2.55, 1.45), (2.2, 0.35, 0.25), m.ice, c))
    p.append(g.cyl("smoke", (0.8, 0.8, 2.55), 0.22, 1.1, m.bark, c, verts=8))
    for i in range(8):
        ang = i * math.pi / 4 + 0.2
        p.append(g.cyl(f"pal{i}", (math.cos(ang) * 5.4, math.sin(ang) * 5.4, 0.95), 0.13, 1.5, m.wood, c, verts=6))
        p.append(g.cone(f"pt{i}", (math.cos(ang) * 5.4, math.sin(ang) * 5.4, 1.8), 0.11, 0.35, m.ice, c, verts=5))
    return finish(g, "building_outcast_burrows", p, c, 0.03)


def bld_aerie(g, m, c, name="building_outcast_aerie"):
    p = []
    p.append(g.cube("snow", (0, 0, 0.16), (8, 8, 0.32), m.ice, c))
    for i, (x, y) in enumerate(((-2.0, -1.8), (2.0, -1.8), (0, 2.1))):
        p.append(g.cyl(f"trunk{i}", (x, y, 3.4), 0.42, 6.4, m.bark, c, verts=10))
        p.append(g.ico(f"can{i}", (x, y, 6.2), 1.15, m.ice, c, subdiv=1, scale=(1.3, 1.3, 0.55)))
    p.append(g.cube("deck", (0, 0, 6.35), (5.0, 5.0, 0.2), m.wood, c))
    for s in (-1, 1):
        p.append(g.cube(f"rx{s}", (s * 2.4, 0, 6.8), (0.12, 4.9, 0.6), m.wood, c))
        p.append(g.cube(f"ry{s}", (0, s * 2.4, 6.8), (4.9, 0.12, 0.6), m.wood, c))
    p.append(g.ico("nest", (0, -0.2, 7.55), 0.85, m.ice, c, subdiv=1, scale=(1.2, 1.2, 0.5)))
    if "treetop" in name:
        p.append(g.cyl("basket", (0, 0.3, 7.35), 1.35, 1.1, m.wood, c, verts=10))
        p.append(g.cyl("look", (0, -1.4, 7.55), 0.55, 0.85, m.wood, c, verts=8))
    else:
        p.append(g.cube("hut", (0, 0.3, 7.15), (2.4, 2.2, 1.35), m.wood, c))
        pitched(g, p, c, m.bark, (0, 0.3, 7.9), (2.8, 2.6, 0.12), pitch=40, gable=m.wood, axis="y", tiles=True)
        window(g, p, c, m.wood, m.ice, (0, -0.85, 7.25), (0.7, 0.14, 0.7))
    ladder_to(g, p, c, m.wood, -3.15, 0.35, -2.35, 6.15, count=14, width=1.2, rail_x=0.55)
    return finish(g, name, p, c, 0.03)


def bld_village_hall(g, m, c):
    """Smaller longhouse than the keep."""
    p = []
    p.append(g.cube("snow", (0, 0, 0.16), (8, 10, 0.32), m.ice, c))
    p.append(g.cube("floor", (0, 0.3, 0.5), (5.6, 11.2, 0.2), m.wood, c))
    for i in range(12):
        z = 0.7 + i * 0.22
        p.append(g.cyl(f"lw{i}", (-2.7, 0.3, z), 0.13, 11.0, m.wood, c, verts=8, rot=(math.radians(90), 0, 0)))
        p.append(g.cyl(f"le{i}", (2.7, 0.3, z), 0.13, 11.0, m.wood, c, verts=8, rot=(math.radians(90), 0, 0)))
    pitched(g, p, c, m.bark, (0, 0.3, 3.55), (6.4, 11.8, 0.14), pitch=42, gable=m.wood, axis="y", tiles=True)
    p.append(g.cube("front", (0, -5.15, 2.35), (5.6, 0.32, 3.5), m.wood, c))
    p.append(g.cube("back", (0, 5.75, 2.35), (5.6, 0.32, 3.5), m.wood, c))
    door(g, p, c, m.leather, m.iron, (0, -5.35, 1.2), (0.7, 0.12, 1.7))
    window(g, p, c, m.wood, m.ice, (-1.6, -5.35, 2.55), (0.85, 0.14, 0.9))
    window(g, p, c, m.wood, m.ice, (1.6, -5.35, 2.55), (0.85, 0.14, 0.9))
    stairs(g, p, c, m.wood, (0, -6.1, 0.3), count=5, width=2.0)
    return finish(g, "building_outcast_village_hall", p, c, 0.035, 3)


def bld_mine(g, m, c):
    """Headframe over a pit."""
    p = []
    p.append(g.cube("snow", (0, 0, 0.16), (8, 8, 0.32), m.ice, c))
    p.append(g.cube("tip", (0, 0.6, 0.7), (5.2, 5.2, 0.5), m.bark, c))
    p.append(g.cyl("pit", (0, 0.6, 0.45), 1.4, 0.4, m.dark_stone, c, verts=12))
    for x, y in ((-2.0, -1.6), (2.0, -1.6), (-2.0, 2.8), (2.0, 2.8)):
        p.append(g.cyl(f"leg{x}{y}", (x, y, 2.8), 0.18, 4.4, m.wood, c, verts=8))
    p.append(g.cube("head", (0, 0.6, 5.15), (4.6, 4.8, 0.28), m.wood, c))
    p.append(g.cube("boom", (0, -2.4, 4.4), (0.22, 5.2, 0.22), m.wood, c, rot=(math.radians(28), 0, 0)))
    p.append(g.cyl("wheel", (0, -4.4, 3.55), 0.55, 0.16, m.iron, c, verts=12, rot=(math.radians(90), 0, 0)))
    p.append(g.cube("hut", (3.4, 0.6, 1.55), (2.6, 3.2, 2.2), m.wood, c))
    pitched(g, p, c, m.bark, (3.4, 0.6, 2.75), (2.9, 3.5, 0.12), pitch=32, gable=m.wood, axis="y")
    return finish(g, "building_outcast_mine", p, c, 0.03)


def bld_ground_works(g, m, c):
    p = []
    p.append(g.cube("bank", (0, 0, 0.75), (8.8, 1.8, 1.5), m.ice, c))
    for i in range(-4, 5):
        p.append(g.cyl(f"post{i}", (i * 0.95, 0, 1.5), 0.15, 2.2, m.wood, c, verts=6))
        p.append(g.cone(f"cap{i}", (i * 0.95, 0, 2.7), 0.13, 0.4, m.ice, c, verts=5))
    p.append(g.cube("rail", (0, 0, 1.85), (8.6, 0.14, 0.14), m.wood, c))
    return finish(g, "building_outcast_ground_works", p, c, 0.02)


# --- Freetown: fishing town ---

def bld_smugglers_den(g, m, c):
    """Low stone cellar opening onto a dock."""
    p = []
    _earth(g, p, c, m.brick, (12, 16, 0.4))
    p.append(g.cube("cellar", (0, 0.8, 1.15), (7.2, 6.4, 1.7), m.brick, c))
    p.append(g.cube("upper", (0, 0.8, 2.55), (6.4, 5.6, 1.2), m.pale_wood, c))
    pitched(g, p, c, m.slate, (0, 0.8, 3.25), (7.0, 6.2, 0.14), pitch=28, gable=m.pale_wood, tiles=True)
    p.append(g.cube("dock", (0, -5.4, 0.2), (10.4, 4.2, 0.18), m.wood, c))
    for i in range(-4, 5):
        p.append(g.cyl(f"pile{i}", (i * 1.1, -7.35, 0.2), 0.14, 1.2, m.wood, c, verts=8))
    door(g, p, c, m.wood, m.iron, (0, -2.45, 0.95), (0.8, 0.14, 1.5))
    window(g, p, c, m.wood, m.glass, (-2.0, -2.4, 2.45), (0.9, 0.16, 0.8))
    window(g, p, c, m.wood, m.glass, (2.0, -2.4, 2.45), (0.9, 0.16, 0.8))
    for i in range(4):
        p.append(g.cyl(f"bar{i}", (-2.0 + i * 0.9, -3.4, 0.55), 0.24, 0.5, m.wood, c, verts=10))
    stairs(g, p, c, m.brick, (0, -3.3, 0.25), count=4, width=2.2)
    return finish(g, "building_freetown_smugglers_den", p, c, 0.035, 3)


def bld_hut(g, m, c):
    """Tiny steep cottage."""
    p = []
    _earth(g, p, c, m.brick, (6, 6.5, 0.28))
    p.append(g.cube("stone", (0, 0.2, 0.95), (4.0, 4.6, 1.4), m.brick, c))
    p.append(g.cube("wood", (0, 0.2, 2.05), (4.3, 4.9, 1.1), m.pale_wood, c))
    pitched(g, p, c, m.slate, (0, 0.2, 2.7), (4.8, 5.4, 0.14), pitch=48, gable=m.pale_wood, axis="y", tiles=True)
    chimney(g, p, c, m.brick, m.slate, (1.0, 1.2, 4.15), h=1.15, r=0.22)
    door(g, p, c, m.wood, m.iron, (0, -2.15, 0.85), (0.55, 0.1, 1.25))
    window(g, p, c, m.wood, m.glass, (-1.1, -2.2, 2.05), (0.7, 0.14, 0.7))
    window(g, p, c, m.wood, m.glass, (1.1, -2.2, 2.05), (0.7, 0.14, 0.7))
    window(g, p, c, m.wood, m.glass, (0, -2.15, 1.15), (0.55, 0.12, 0.5))
    stairs(g, p, c, m.brick, (0, -2.8, 0.22), count=3, width=1.4)
    return finish(g, "building_freetown_hut", p, c, 0.03)


def bld_black_market(g, m, c):
    """Open stalls under awnings, no closed hall."""
    p = []
    _earth(g, p, c, m.brick, (12, 10, 0.36))
    p.append(g.cube("plaza", (0, 0, 0.4), (10.4, 7.4, 0.16), m.brick, c))
    for i, x in enumerate((-3.2, 0, 3.2)):
        p.append(g.cube(f"stall{i}", (x, 0.4, 0.85), (2.6, 2.4, 0.9), m.wood, c))
        p.append(g.cube(f"awn{i}", (x, -0.6, 1.85), (2.9, 3.2, 0.08), m.cloth_blue, c, rot=(math.radians(12), 0, 0)))
        p.append(g.cyl(f"pl{i}", (x - 1.2, -1.8, 1.2), 0.08, 1.6, m.wood, c, verts=6))
        p.append(g.cyl(f"pr{i}", (x + 1.2, -1.8, 1.2), 0.08, 1.6, m.wood, c, verts=6))
    p.append(g.cube("back", (0, 2.4, 1.55), (9.4, 1.6, 2.2), m.pale_wood, c))
    pitched(g, p, c, m.slate, (0, 2.4, 2.75), (9.8, 2.0, 0.12), pitch=24, gable=m.pale_wood, tiles=True)
    return finish(g, "building_freetown_black_market", p, c, 0.03)


def bld_crows_nest(g, m, c):
    """Ship mast lookout."""
    p = []
    _earth(g, p, c, m.brick, (6.5, 6.5, 0.28))
    p.append(g.cube("deck", (0, 0, 0.45), (5.4, 5.4, 0.22), m.wood, c))
    p.append(g.cyl("mast", (0, 0, 4.6), 0.22, 8.6, m.wood, c, verts=10))
    p.append(g.cyl("basket", (0, 0, 7.25), 1.15, 0.85, m.pale_wood, c, verts=12))
    p.append(g.cube("nest", (0, 0, 7.65), (2.4, 2.4, 0.12), m.pale_wood, c))
    for s in (-1, 1):
        p.append(g.cube(f"railx{s}", (s * 1.15, 0, 7.95), (0.08, 2.3, 0.5), m.wood, c))
        p.append(g.cube(f"raily{s}", (0, s * 1.15, 7.95), (2.3, 0.08, 0.5), m.wood, c))
        p.append(g.cyl(f"stay{s}", (s * 1.6, 0, 3.8), 0.04, 6.4, m.iron, c, verts=6, rot=(0, math.radians(s * 12), 0)))
    p.append(g.cube("flag", (0.7, 0, 8.55), (1.3, 0.05, 0.7), m.cloth_blue, c))
    p.append(g.cube("yard", (0, 0, 5.4), (4.6, 0.1, 0.1), m.wood, c))
    p.append(g.cube("sail", (0, -0.15, 4.4), (0.06, 2.6, 2.4), m.cloth_blue, c))
    ladder_to(g, p, c, m.wood, -2.6, 0.4, -0.4, 7.0, count=16, width=0.7, rail_x=0.32)
    return finish(g, "building_freetown_crows_nest", p, c, 0.025)


def bld_barricade(g, m, c, name="building_freetown_barricades"):
    p = []
    p.append(g.cube("sill", (0, 0, 0.22), (9.2, 2.2, 0.28), m.pale_wood, c))
    for i in range(-3, 4):
        p.append(g.cube(f"crate{i}", (i * 1.2, 0.05, 0.85), (1.05, 1.15, 1.05), m.wood, c))
        p.append(g.cube(f"lid{i}", (i * 1.2, 0.05, 1.42), (1.12, 1.22, 0.12), m.pale_wood, c))
        p.append(g.cyl(f"barrel{i}", (i * 1.2, 0.85, 0.55), 0.32, 0.7, m.wood, c, verts=10))
    p.append(g.cube("plank_a", (0, -0.55, 1.55), (8.6, 0.12, 0.18), m.pale_wood, c, rot=(0, 0, math.radians(8))))
    p.append(g.cube("plank_b", (0, 0.55, 1.75), (8.4, 0.12, 0.18), m.wood, c, rot=(0, 0, math.radians(-6))))
    p.append(g.cube("sail", (0, 0.15, 2.15), (7.4, 0.08, 1.15), m.cloth_blue, c))
    return finish(g, name, p, c, 0.02)


def bld_ferry_dock(g, m, c):
    """Pier is the building."""
    p = []
    p.append(g.cube("quay", (0, 1.4, 0.35), (8.4, 4.2, 0.4), m.brick, c))
    p.append(g.cube("dock", (0, -2.6, 0.2), (11.2, 6.4, 0.18), m.wood, c))
    for i in range(-4, 5):
        p.append(g.cube(f"pl{i}", (i * 1.15, -2.6, 0.32), (1.05, 6.2, 0.05), m.pale_wood, c))
        p.append(g.cyl(f"pi{i}", (i * 1.15, -5.6, 0.15), 0.15, 1.25, m.wood, c, verts=8))
    p.append(g.cube("shed", (0, 1.6, 1.45), (3.4, 2.6, 1.8), m.pale_wood, c))
    pitched(g, p, c, m.slate, (0, 1.6, 2.45), (3.8, 3.0, 0.12), pitch=30, gable=m.pale_wood, axis="y")
    p.append(g.cyl("bollard_l", (-3.4, -4.4, 0.7), 0.18, 0.7, m.wood, c, verts=8))
    p.append(g.cyl("bollard_r", (3.4, -4.4, 0.7), 0.18, 0.7, m.wood, c, verts=8))
    return finish(g, "building_ferry_dock", p, c, 0.03)


# --- University: brick ---

def bld_workshop(g, m, c):
    """Sawtooth factory roof."""
    p = []
    _earth(g, p, c, m.brick, (14, 12, 0.4))
    p.append(g.cube("shop", (0, 0.3, 1.85), (11.2, 7.4, 2.8), m.red_brick, c))
    ashlar_face(g, p, c, m.marble, -3.42, 0.9, 2.9, -5.2, 5.2, depth=0.08)
    for i, y in enumerate((-2.0, 0.3, 2.6)):
        p.append(g.cube(f"tooth_n{i}", (0, y - 0.7, 3.55), (11.4, 1.6, 0.14), m.slate, c, rot=(math.radians(-28), 0, 0)))
        p.append(g.cube(f"tooth_s{i}", (0, y + 0.55, 3.35), (11.4, 0.9, 0.12), m.glass, c, rot=(math.radians(55), 0, 0)))
    p.append(g.cyl("stack", (4.2, 1.4, 4.55), 0.4, 2.4, m.red_brick, c, verts=12))
    p.append(g.cube("door_big", (0, -3.45, 1.45), (2.8, 0.16, 2.2), m.wood, c))
    facade_windows(g, p, c, m.marble, m.glass, -3.45, 2.0, (-4.2, -2.6, 2.6, 4.2), (1.15, 0.18, 1.25))
    quoins(g, p, c, m.marble, 0, 0.3, 1.85, 5.4, 3.5, 2.8)
    p.append(g.cyl("vat", (4.4, -2.6, 1.15), 0.7, 1.3, m.iron, c, verts=12))
    stairs(g, p, c, m.marble, (0, -4.3, 0.3), count=5, width=3.0)
    return finish(g, "building_university_workshop", p, c, 0.04, 3)


def bld_library(g, m, c):
    """Long stacks under a reading dome."""
    p = []
    _earth(g, p, c, m.brick, (14, 12, 0.4))
    p.append(g.cube("nave", (0, 0.6, 2.4), (10.4, 7.2, 3.8), m.red_brick, c))
    pitched(g, p, c, m.slate, (0, 0.6, 4.4), (10.8, 7.8, 0.16), pitch=24, gable=m.red_brick, tiles=True)
    p.append(g.cyl("drum", (0, 0.6, 5.45), 1.95, 1.15, m.marble, c, verts=16, uv=0.4))
    p.append(g.ico("dome", (0, 0.6, 6.45), 2.0, m.gold, c, subdiv=2, scale=(1.0, 1.0, 0.52)))
    for x in (-3.6, -1.2, 1.2, 3.6):
        window(g, p, c, m.marble, m.glass, (x, -3.05, 2.55), (1.2, 0.18, 2.0))
    door(g, p, c, m.wood, m.iron, (0, -3.15, 1.25), (0.75, 0.14, 1.9))
    p.append(g.cube("stacks", (0, 1.6, 1.4), (8.4, 1.2, 1.6), m.wood, c))
    stairs(g, p, c, m.marble, (0, -4.1, 0.3), count=6, width=3.4)
    return finish(g, "building_university_forbidden_library", p, c, 0.04, 3)


def bld_alchemist(g, m, c):
    """Chimney cluster and glass vats."""
    p = []
    _earth(g, p, c, m.brick, (11, 10, 0.4))
    p.append(g.cube("lab", (0, 0.4, 1.85), (6.4, 6.0, 2.8), m.red_brick, c))
    pitched(g, p, c, m.slate, (0, 0.4, 3.35), (6.8, 6.5, 0.14), pitch=30, gable=m.red_brick, tiles=True)
    for i, x in enumerate((-1.6, 0, 1.6)):
        p.append(g.cyl(f"chim{i}", (x, 1.6, 4.55), 0.32 + i * 0.04, 1.8 + i * 0.25, m.red_brick, c, verts=10))
        p.append(g.cube(f"cap{i}", (x, 1.6, 5.5 + i * 0.25), (0.5, 0.5, 0.1), m.slate, c))
    p.append(g.cyl("vat_l", (-2.8, -2.6, 1.2), 0.65, 1.4, m.glass, c, verts=12))
    p.append(g.cyl("vat_r", (2.8, -2.6, 1.05), 0.5, 1.1, m.glass, c, verts=12))
    p.append(g.cyl("coil", (2.8, -2.6, 1.85), 0.12, 0.9, m.iron, c, verts=8))
    door(g, p, c, m.wood, m.iron, (0, -2.65, 1.15), (0.65, 0.12, 1.7))
    stairs(g, p, c, m.marble, (0, -3.5, 0.28), count=5, width=2.0)
    return finish(g, "building_university_alchemist", p, c, 0.035, 3)


def bld_clockwork_tower(g, m, c):
    """Wizard clock-spire: octagon keep, coursed shaft, four clock faces, glass lantern."""
    p = []
    stone, trim, frame = m.brick, m.marble, m.iron

    def r_at(z, z0=4.85, z1=11.55, r0=1.72, r1=1.28):
        t = (z - z0) / max(z1 - z0, 0.01)
        return r0 + (r1 - r0) * t

    p.append(g.cyl("courtyard", (0, 0, 0.03), 6.4, 0.08, m.dark_stone, c, verts=16, uv=0.45))
    p.append(g.cyl("plinth", (0, 0, 0.22), 2.55, 0.4, stone, c, verts=8, uv=0.22))
    p.append(g.cyl("plinth_cap", (0, 0, 0.46), 2.65, 0.1, trim, c, verts=8, uv=0.35))
    stone_drum(g, p, c, stone, 0.52, 4.15, 2.28, verts=8, course_h=0.18, uv=0.2)
    p.append(g.cyl("keep_string", (0, 0, 4.22), 2.42, 0.1, trim, c, verts=8, uv=0.35))
    p.append(g.cyl("keep_cornice", (0, 0, 4.4), 2.52, 0.12, stone, c, verts=8, uv=0.24))
    for k in range(8):
        ang = k * math.pi / 4 + math.pi / 8
        bx, by = math.cos(ang) * 2.48, math.sin(ang) * 2.48
        p.append(g.cube(f"butt{k}", (bx, by, 2.2), (0.5, 0.5, 3.05), stone, c, rot=(0, 0, ang), uv=0.22))
        p.append(g.cube(f"bcap{k}", (bx * 1.02, by * 1.02, 3.82), (0.58, 0.58, 0.14), trim, c, rot=(0, 0, ang)))
        if math.sin(ang) > -0.55:
            slit_window(
                g, p, c, frame, m.glass,
                (math.cos(ang) * 2.26, math.sin(ang) * 2.26, 2.25),
                (0.14, 0.5, 1.35), yaw=ang,
            )
    p.append(g.cube("porch", (0, -2.75, 1.35), (2.35, 1.4, 2.05), stone, c, uv=0.24))
    pitched(g, p, c, m.slate, (0, -2.75, 2.45), (2.65, 1.65, 0.12), pitch=28, gable=stone, tiles=False)
    for x in (-0.9, 0.9):
        p.append(g.taper(f"col{x}", (x, -3.2, 1.55), 0.18, 0.14, 1.75, trim, c, verts=8))
    p.append(g.cube("entab", (0, -3.2, 2.5), (2.2, 0.62, 0.12), trim, c))
    p.append(g.cyl("gear", (0, -3.42, 2.15), 0.32, 0.06, m.gold, c, verts=16, rot=(math.radians(90), 0, 0)))
    p.append(g.cyl("hub", (0, -3.45, 2.15), 0.12, 0.05, m.iron, c, verts=10, rot=(math.radians(90), 0, 0)))
    ashlar_face(g, p, c, stone, -3.42, 0.4, 2.05, -0.95, 0.95, depth=0.07, bw=0.32, bh=0.18)
    arch(g, p, c, stone, (0, -3.48, 0), radius=0.72, depth=0.42, z0=1.7, count=9, block=(0.24, 0.42, 0.2))
    door(g, p, c, m.wood, frame, (0, -3.52, 1.05), (0.52, 0.1, 1.55))
    stairs(g, p, c, stone, (0, -4.15, 0.09), count=7, width=2.05, step=(0.22, 0.12))
    banner(g, p, c, frame, m.cloth_deep, (1.45, -2.85, 3.25), h=2.25, fly=1.05)
    p.append(g.cyl("collar", (0, 0, 4.62), 1.95, 0.24, trim, c, verts=16, uv=0.32))
    stone_shaft(g, p, c, stone, 4.75, 11.55, 1.72, 1.28, verts=18, course_h=0.16, uv=0.2)
    for z in (6.85, 9.15):
        p.append(g.cyl(f"band{z}", (0, 0, z), r_at(z) + 0.1, 0.09, trim, c, verts=20, uv=0.35))
    for z in (5.85, 8.05, 10.35):
        rr = r_at(z)
        slit_window(g, p, c, frame, m.glass, (0, -rr - 0.01, z), (0.34, 0.12, 1.05))
        slit_window(g, p, c, frame, m.glass, (0, rr + 0.01, z), (0.34, 0.12, 1.05), yaw=math.pi)
        slit_window(g, p, c, frame, m.glass, (rr + 0.01, 0, z), (0.12, 0.34, 1.05), yaw=math.radians(90))
        slit_window(g, p, c, frame, m.glass, (-rr - 0.01, 0, z), (0.12, 0.34, 1.05), yaw=math.radians(-90))
    p.append(g.cyl("midwalk", (0, 0, 8.55), 1.88, 0.11, m.slate, c, verts=16, uv=0.3))
    p.append(g.cyl("midrail", (0, 0, 8.92), 1.84, 0.055, trim, c, verts=16, uv=0.4))
    for k in range(10):
        ang = k * math.pi / 5
        p.append(g.taper(f"corb{k}", (math.cos(ang) * 1.58, math.sin(ang) * 1.58, 8.22), 0.16, 0.08, 0.48, stone, c, verts=6, rot=(math.radians(16), 0, ang)))
    for k in range(12):
        ang = k * math.pi / 6
        p.append(g.taper(f"mach{k}", (math.cos(ang) * 1.42, math.sin(ang) * 1.42, 11.35), 0.16, 0.09, 0.58, stone, c, verts=6, rot=(math.radians(18), 0, ang)))
    p.append(g.cyl("clockwalk", (0, 0, 11.75), 1.95, 0.14, m.slate, c, verts=16, uv=0.28))
    p.append(g.cyl("clockrail", (0, 0, 12.12), 1.9, 0.06, trim, c, verts=16, uv=0.4))
    stone_drum(g, p, c, stone, 11.9, 13.85, 1.72, verts=16, course_h=0.16, uv=0.22)
    p.append(g.cyl("clock_gold", (0, 0, 13.95), 1.88, 0.08, m.gold, c, verts=20, uv=0.45))
    for yaw, pos in (
        (0.0, (0, -1.78, 12.85)),
        (math.pi, (0, 1.78, 12.85)),
        (math.radians(90), (1.78, 0, 12.85)),
        (math.radians(-90), (-1.78, 0, 12.85)),
    ):
        nx, ny = math.sin(yaw), -math.cos(yaw)
        ux, uy = math.cos(yaw), math.sin(yaw)
        face_rot = (math.radians(90), 0, yaw)
        p.append(g.cyl("dial", pos, 0.58, 0.07, trim, c, verts=24, rot=face_rot))
        p.append(g.cyl("bezel", (pos[0] + nx * 0.02, pos[1] + ny * 0.02, pos[2]), 0.66, 0.05, m.gold, c, verts=24, rot=face_rot))
        p.append(g.cyl("inner_ring", (pos[0] + nx * 0.03, pos[1] + ny * 0.03, pos[2]), 0.5, 0.02, m.iron, c, verts=24, rot=face_rot))
        for i in range(12):
            a = i * math.pi / 6
            rr = 0.44
            tx = pos[0] + ux * math.sin(a) * rr + nx * 0.05
            ty = pos[1] + uy * math.sin(a) * rr + ny * 0.05
            tz = pos[2] + math.cos(a) * rr
            mark = 0.1 if i % 3 == 0 else 0.055
            p.append(g.cube(
                "tick", (tx, ty, tz),
                (abs(ux) * 0.04 + abs(nx) * 0.04 + 0.03, abs(uy) * 0.04 + abs(ny) * 0.04 + 0.03, mark),
                m.iron, c,
            ))
        def hand(ang, length, thick):
            mid = length * 0.42
            hx = pos[0] + ux * math.sin(ang) * mid + nx * 0.06
            hy = pos[1] + uy * math.sin(ang) * mid + ny * 0.06
            hz = pos[2] + math.cos(ang) * mid
            if abs(ny) > 0.5:
                rot = (0, -ang, 0)
                size = (thick, 0.05, length)
            else:
                rot = (ang, 0, 0)
                size = (0.05, thick, length)
            p.append(g.cube("hand", (hx, hy, hz), size, m.iron, c, rot=rot))
        hand(-math.pi / 3, 0.32, 0.06)
        hand(math.pi / 3, 0.44, 0.045)
        p.append(g.cyl("boss", (pos[0] + nx * 0.07, pos[1] + ny * 0.07, pos[2]), 0.055, 0.05, m.gold, c, verts=12, rot=face_rot))
    p.append(g.cyl("lantern", (0, 0, 14.65), 1.15, 1.15, m.glass, c, verts=16, uv=0.4))
    for k in range(8):
        ang = k * math.pi / 4
        p.append(g.cube(
            f"mull{k}", (math.cos(ang) * 1.14, math.sin(ang) * 1.14, 14.65),
            (0.1, 0.1, 1.18), m.gold, c, rot=(0, 0, ang),
        ))
    p.append(g.cyl("lantern_sill", (0, 0, 14.08), 1.22, 0.08, m.gold, c, verts=16, uv=0.4))
    p.append(g.cyl("lantern_cap", (0, 0, 15.22), 1.22, 0.08, m.gold, c, verts=16, uv=0.4))
    p.append(g.ico("bell", (0, 0, 14.55), 0.32, m.gold, c, subdiv=2, scale=(1.0, 1.0, 1.15)))
    p.append(g.cone("lantern_roof", (0, 0, 15.55), 1.35, 0.85, m.slate, c, verts=12))
    p.append(g.cyl("fin", (0, 0, 16.05), 0.06, 0.35, m.gold, c, verts=8))
    p.append(g.uv_sphere("star", (0, 0, 16.32), 0.1, m.crystal, c, segs=14, rings=8))
    return finish(g, "building_university_clockwork_tower", p, c, 0.01, 5)


def bld_moat(g, m, c):
    p = []
    p.append(g.cube("water", (0, 0, 0.35), (9.0, 2.6, 0.7), m.glass, c))
    p.append(g.cube("inner", (0, -1.1, 0.95), (9.0, 0.75, 1.7), m.red_brick, c))
    p.append(g.cube("outer", (0, 1.1, 0.95), (9.0, 0.75, 1.7), m.red_brick, c))
    for i in range(-3, 4):
        p.append(g.cube(f"mer{i}", (i * 1.2, -1.1, 2.0), (0.52, 0.52, 0.58), m.red_brick, c))
    return finish(g, "building_university_moat", p, c, 0.025)


def bld_observatory(g, m, c):
    """Tall wizard tower: octagonal keep, coursed shaft, ribbed dome observatory."""
    p = []
    stone, trim, frame = m.brick, m.marble, m.iron

    def r_at(z, z0=5.15, z1=14.15, r0=1.92, r1=1.42):
        t = (z - z0) / max(z1 - z0, 0.01)
        return r0 + (r1 - r0) * t

    p.append(g.cyl("plinth", (0, 0, 0.22), 2.95, 0.44, stone, c, verts=8, uv=0.22))
    p.append(g.cyl("plinth_cap", (0, 0, 0.48), 3.05, 0.1, trim, c, verts=8, uv=0.35))
    stone_drum(g, p, c, stone, 0.55, 4.45, 2.58, verts=8, course_h=0.18, uv=0.2)
    p.append(g.cyl("keep_string", (0, 0, 4.52), 2.75, 0.12, trim, c, verts=8, uv=0.35))
    p.append(g.cyl("keep_cornice", (0, 0, 4.72), 2.88, 0.14, stone, c, verts=8, uv=0.24))
    for k in range(8):
        ang = k * math.pi / 4 + math.pi / 8
        bx, by = math.cos(ang) * 2.78, math.sin(ang) * 2.78
        p.append(g.cube(f"butt{k}", (bx, by, 2.35), (0.58, 0.58, 3.35), stone, c, rot=(0, 0, ang), uv=0.22))
        p.append(g.cube(f"bcap{k}", (bx * 1.02, by * 1.02, 4.12), (0.66, 0.66, 0.16), trim, c, rot=(0, 0, ang)))
        if math.sin(ang) > -0.55:
            slit_window(
                g, p, c, frame, m.glass,
                (math.cos(ang) * 2.55, math.sin(ang) * 2.55, 2.45),
                (0.14, 0.55, 1.45), yaw=ang,
            )
    p.append(g.cube("porch", (0, -3.15, 1.45), (2.65, 1.55, 2.25), stone, c, uv=0.24))
    pitched(g, p, c, m.slate, (0, -3.15, 2.65), (2.95, 1.85, 0.12), pitch=26, gable=stone, tiles=False)
    for x in (-1.0, 1.0):
        p.append(g.taper(f"col{x}", (x, -3.62, 1.65), 0.2, 0.15, 1.95, trim, c, verts=8))
    p.append(g.cube("entab", (0, -3.62, 2.72), (2.45, 0.7, 0.14), trim, c))
    ashlar_face(g, p, c, stone, -3.9, 0.4, 2.2, -1.05, 1.05, depth=0.07, bw=0.34, bh=0.18)
    arch(g, p, c, stone, (0, -3.95, 0), radius=0.82, depth=0.48, z0=1.85, count=11, block=(0.28, 0.48, 0.24))
    door(g, p, c, m.wood, frame, (0, -4.0, 1.12), (0.58, 0.1, 1.7))
    stairs(g, p, c, stone, (0, -4.65, 0.09), count=8, width=2.35, step=(0.22, 0.12))
    banner(g, p, c, frame, m.cloth_deep, (1.65, -3.25, 3.55), h=2.45, fly=1.15)
    p.append(g.cyl("collar", (0, 0, 5.0), 2.15, 0.28, trim, c, verts=16, uv=0.32))
    stone_shaft(g, p, c, stone, 5.15, 14.15, 1.92, 1.42, verts=20, course_h=0.16, uv=0.2)
    for z in (7.35, 10.55, 13.55):
        p.append(g.cyl(f"band{z}", (0, 0, z), r_at(z) + 0.1, 0.09, trim, c, verts=22, uv=0.35))
    for z in (6.25, 8.55, 11.25, 13.15):
        rr = r_at(z)
        slit_window(g, p, c, frame, m.glass, (0, -rr - 0.01, z), (0.38, 0.12, 1.15))
        slit_window(g, p, c, frame, m.glass, (0, rr + 0.01, z), (0.38, 0.12, 1.15), yaw=math.pi)
        slit_window(g, p, c, frame, m.glass, (rr + 0.01, 0, z), (0.12, 0.38, 1.15), yaw=math.radians(90))
        slit_window(g, p, c, frame, m.glass, (-rr - 0.01, 0, z), (0.12, 0.38, 1.15), yaw=math.radians(-90))
    p.append(g.cyl("midwalk", (0, 0, 10.72), 2.12, 0.12, m.slate, c, verts=16, uv=0.3))
    p.append(g.cyl("midrail", (0, 0, 11.12), 2.08, 0.06, trim, c, verts=16, uv=0.4))
    for k in range(12):
        ang = k * math.pi / 6
        p.append(g.taper(f"corb{k}", (math.cos(ang) * 1.78, math.sin(ang) * 1.78, 10.38), 0.18, 0.09, 0.52, stone, c, verts=6, rot=(math.radians(16), 0, ang)))
        p.append(g.cyl(f"post{k}", (math.cos(ang) * 2.05, math.sin(ang) * 2.05, 11.0), 0.05, 0.38, trim, c, verts=6))
    for k in range(16):
        ang = k * math.pi / 8
        p.append(g.taper(f"mach{k}", (math.cos(ang) * 1.62, math.sin(ang) * 1.62, 14.05), 0.2, 0.1, 0.72, stone, c, verts=6, rot=(math.radians(20), 0, ang)))
    p.append(g.cyl("walk", (0, 0, 14.52), 2.42, 0.14, m.slate, c, verts=16, uv=0.28))
    p.append(g.cyl("walkrail", (0, 0, 14.92), 2.38, 0.07, trim, c, verts=16, uv=0.4))
    for k in range(12):
        ang = k * math.pi / 6
        p.append(g.cube(f"mer{k}", (math.cos(ang) * 2.28, math.sin(ang) * 2.28, 15.05), (0.26, 0.26, 0.48), m.slate, c))
        p.append(g.taper(f"garg{k}", (math.cos(ang) * 2.52, math.sin(ang) * 2.52, 14.55), 0.12, 0.05, 0.45, m.dark_stone, c, verts=5, rot=(math.radians(70), 0, ang)))
    stone_drum(g, p, c, stone, 14.65, 16.85, 2.22, verts=16, course_h=0.15, uv=0.22)
    p.append(g.cyl("obs_string", (0, 0, 16.95), 2.38, 0.09, trim, c, verts=16, uv=0.35))
    p.append(g.cyl("obs_gold", (0, 0, 17.08), 2.45, 0.06, m.gold, c, verts=24, uv=0.45))
    for k in range(12):
        ang = k * math.pi / 6
        slit_window(
            g, p, c, frame, m.glass,
            (math.cos(ang) * 2.22, math.sin(ang) * 2.22, 15.75),
            (0.12, 0.48, 1.05), yaw=ang,
        )
        p.append(g.ico(f"shard{k}", (math.cos(ang) * 2.55, math.sin(ang) * 2.55, 17.35), 0.16, m.crystal, c, subdiv=2, scale=(0.55, 0.55, 1.35)))
    p.append(g.ico("dome", (0, 0, 17.92), 2.32, m.gold, c, subdiv=5, scale=(1.0, 1.0, 0.52)))
    for k in range(6):
        ang = k * math.pi / 6
        p.append(g.cube(f"rib{k}", (0, 0, 18.35), (0.04, 1.55, 0.72), m.iron, c, rot=(math.radians(8), 0, ang)))
    p.append(g.cyl("dome_ring", (0, 0, 17.22), 2.2, 0.05, m.iron, c, verts=32))
    p.append(g.cyl("yoke_l", (-0.38, -0.15, 18.45), 0.08, 0.95, m.gold, c, verts=10, rot=(math.radians(90), 0, 0)))
    p.append(g.cyl("yoke_r", (0.38, -0.15, 18.45), 0.08, 0.95, m.gold, c, verts=10, rot=(math.radians(90), 0, 0)))
    p.append(g.cyl("pivot", (0, -0.55, 18.45), 0.12, 0.85, m.iron, c, verts=12, rot=(0, math.radians(90), 0)))
    p.append(g.cyl("tube_o", (0, -2.05, 18.95), 0.17, 2.35, m.gold, c, verts=20, rot=(math.radians(48), 0, 0)))
    p.append(g.cyl("tube_i", (0, -2.05, 18.95), 0.11, 2.42, m.iron, c, verts=16, rot=(math.radians(48), 0, 0)))
    for t in (0.22, 0.48, 0.72):
        p.append(g.cyl(f"ring{t}", (0, -0.9 - t * 2.0, 18.45 + t * 1.15), 0.2, 0.06, m.iron, c, verts=16, rot=(math.radians(48), 0, 0)))
    p.append(g.cyl("dew", (0, -3.05, 19.52), 0.2, 0.28, m.gold, c, verts=16, rot=(math.radians(48), 0, 0)))
    p.append(g.cyl("lens", (0, -3.22, 19.62), 0.13, 0.05, m.glass, c, verts=16, rot=(math.radians(48), 0, 0)))
    p.append(g.uv_sphere("cw", (0, 1.15, 17.75), 0.18, m.iron, c, segs=14, rings=10))
    p.append(g.cone("finial", (0, 0, 19.28), 0.16, 0.42, m.gold, c, verts=10))
    p.append(g.uv_sphere("star", (0, 0, 19.58), 0.09, m.crystal, c, segs=16, rings=10))
    p.append(g.cyl("courtyard", (0, 0, 0.03), 7.2, 0.08, m.dark_stone, c, verts=16, uv=0.45))
    return finish(g, "building_university_grand_observatory", p, c, 0.01, 5)


def bld_weather_rods(g, m, c):
    """Field of rods, tiny instrument hut."""
    p = []
    _earth(g, p, c, m.brick, (12, 10, 0.36))
    p.append(g.cube("pad", (0, 0, 0.4), (10.4, 7.4, 0.16), m.marble, c))
    p.append(g.cube("hut", (0, 2.2, 1.25), (3.0, 2.4, 1.7), m.red_brick, c))
    pitched(g, p, c, m.slate, (0, 2.2, 2.2), (3.3, 2.7, 0.12), pitch=28, gable=m.red_brick)
    for i in range(5):
        for j in range(3):
            x, y = -3.6 + i * 1.8, -1.6 + j * 1.4
            p.append(g.cyl(f"rod{i}{j}", (x, y, 2.4 + (i + j) * 0.15), 0.07, 3.4 + (i + j) * 0.2, m.iron, c, verts=8))
            p.append(g.uv_sphere(f"b{i}{j}", (x, y, 4.2 + (i + j) * 0.25), 0.12, m.gold, c, segs=8, rings=6))
    door(g, p, c, m.wood, m.iron, (0, 1.05, 0.95), (0.5, 0.1, 1.3))
    return finish(g, "building_university_weather_rods", p, c, 0.03)


def bld_far_glass(g, m, c):
    """Seeing-stone on a coursed limestone pier."""
    p = []
    stone = m.brick
    stone_drum(g, p, c, stone, 0.1, 3.35, 1.45, verts=18, course_h=0.22, uv=0.4)
    p.append(g.cyl("yoke", (0, 0, 3.55), 1.05, 0.28, m.gold, c, verts=16, uv=0.5))
    p.append(g.uv_sphere("stone", (0, -0.12, 4.55), 0.82, m.crystal, c, segs=22, rings=14))
    p.append(g.cyl("halo", (0, -0.12, 4.55), 1.12, 0.04, m.gold, c, verts=24, rot=(math.radians(75), 0, 0)))
    p.append(g.cone("fin", (0, 0, 5.48), 0.1, 0.48, m.gold, c, verts=8))
    p.append(g.cube("porch", (0, -1.85, 1.05), (2.05, 1.35, 1.55), stone, c, uv=0.4))
    door(g, p, c, m.wood, m.iron, (0, -2.52, 0.95), (0.52, 0.1, 1.45))
    stairs(g, p, c, stone, (0, -3.22, 0.18), count=5, width=1.75, step=(0.22, 0.13))
    return finish(g, "building_university_far_glass", p, c, 0.014, 4)


# --- Church: renaissance ---

def bld_monastery(g, m, c):
    """Cloister square."""
    p = []
    _earth(g, p, c, m.marble, (14, 14, 0.4))
    p.append(g.cube("garth", (0, 0, 0.4), (6.0, 6.0, 0.16), m.marble, c))
    for dx, dy, sx, sy in ((0, 4.4, 11.2, 2.6), (0, -4.4, 11.2, 2.6), (4.4, 0, 2.6, 6.4), (-4.4, 0, 2.6, 6.4)):
        p.append(g.cube("walk", (dx, dy, 1.55), (sx, sy, 2.2), m.marble, c))
    pitched(g, p, c, m.slate, (0, 4.4, 2.75), (11.6, 3.0, 0.14), pitch=20, gable=m.marble, tiles=True)
    pitched(g, p, c, m.slate, (0, -4.4, 2.75), (11.6, 3.0, 0.14), pitch=20, gable=m.marble, tiles=True)
    for x in (-2.4, 0, 2.4):
        p.append(g.taper(f"col{x}", (x, -2.85, 1.35), 0.16, 0.14, 1.8, m.marble, c, verts=8))
    door(g, p, c, m.wood, m.iron, (0, -5.7, 1.05), (0.7, 0.12, 1.6))
    p.append(g.cyl("well", (0, 0, 0.7), 0.7, 0.4, m.marble, c, verts=12))
    stairs(g, p, c, m.marble, (0, -6.5, 0.25), count=5, width=2.4)
    return finish(g, "building_church_warrior_monastery", p, c, 0.04, 3)


def bld_sun_temple(g, m, c):
    """Round sun rotunda of marble courses and a gold dome."""
    p = []
    stone_drum(g, p, c, m.marble, 0.25, 4.25, 3.55, verts=18, course_h=0.22, uv=0.38)
    p.append(g.cyl("entab", (0, 0.0, 4.38), 3.78, 0.16, m.gold, c, verts=18, uv=0.45))
    p.append(g.ico("dome", (0, 0.0, 5.55), 2.65, m.gold, c, subdiv=2, scale=(1.0, 1.0, 0.55)))
    p.append(g.cyl("sun", (0, -3.45, 3.05), 0.68, 0.08, m.gold, c, verts=16, rot=(math.radians(90), 0, 0)))
    for k in range(8):
        ang = k * math.pi / 4 + 0.2
        p.append(g.taper(f"col{k}", (math.cos(ang) * 4.05, math.sin(ang) * 4.05, 1.65), 0.2, 0.16, 2.25, m.marble, c, verts=8))
    p.append(g.cube("portico", (0, -4.45, 0.65), (5.2, 2.05, 0.38), m.marble, c, uv=0.4))
    door(g, p, c, m.wood, m.iron, (0, -3.75, 1.2), (0.72, 0.14, 1.85))
    stairs(g, p, c, m.marble, (0, -5.55, 0.22), count=6, width=4.2)
    return finish(g, "building_church_sun_temple", p, c, 0.016, 4)


def bld_sacred_site(g, m, c):
    """Obelisk on a plaza."""
    p = []
    p.append(g.cube("plaza", (0, 0, 0.22), (10, 10, 0.36), m.marble, c))
    p.append(g.cube("plinth", (0, 0, 0.7), (3.4, 3.4, 0.5), m.marble, c))
    p.append(g.cube("obelisk", (0, 0, 4.4), (1.1, 1.1, 6.8), m.marble, c))
    p.append(g.cone("cap", (0, 0, 8.0), 0.75, 0.9, m.gold, c, verts=4))
    for x, y in ((-4.2, -4.2), (4.2, -4.2), (-4.2, 4.2), (4.2, 4.2)):
        p.append(g.taper(f"stela{x}", (x, y, 1.4), 0.28, 0.22, 2.0, m.marble, c, verts=6))
    p.append(g.cyl("sun", (0, -0.7, 6.4), 0.45, 0.08, m.gold, c, verts=12, rot=(math.radians(90), 0, 0)))
    return finish(g, "building_church_sacred_site", p, c, 0.04, 3)


def bld_scorched_tower(g, m, c):
    """Blackened broken stone tower."""
    p = []
    stone_shaft(g, p, c, m.brick, 0.2, 6.15, 1.82, 1.12, verts=14, course_h=0.22, uv=0.35)
    p.append(g.cube("break", (0.55, -0.35, 6.35), (1.65, 1.45, 1.25), m.brick, c, rot=(math.radians(12), 0, math.radians(18))))
    p.append(g.cube("rubble", (1.75, -1.35, 0.65), (2.05, 1.65, 0.75), m.brick, c))
    window(g, p, c, m.brick, m.slate, (0, -1.35, 2.65), (0.55, 0.14, 1.05))
    p.append(g.cube("char", (0, 0, 5.45), (1.25, 1.25, 0.28), m.dark_stone, c))
    door(g, p, c, m.wood, m.iron, (0, -1.62, 0.95), (0.52, 0.12, 1.45))
    stairs(g, p, c, m.brick, (0, -2.35, 0.22), count=4, width=1.55)
    return finish(g, "building_church_scorched_tower", p, c, 0.016, 4)


def bld_shrine(g, m, c):
    """Small aedicule."""
    p = []
    p.append(g.cube("plaza", (0, 0, 0.18), (6, 6, 0.28), m.marble, c))
    p.append(g.cube("styl", (0, 0, 0.55), (3.6, 3.2, 0.35), m.marble, c))
    for x in (-1.15, 1.15):
        p.append(g.taper(f"c{x}", (x, -0.9, 1.55), 0.16, 0.13, 1.8, m.marble, c, verts=8))
        p.append(g.taper(f"cb{x}", (x, 0.9, 1.55), 0.16, 0.13, 1.8, m.marble, c, verts=8))
    p.append(g.cube("entab", (0, 0, 2.5), (3.4, 2.6, 0.18), m.gold, c))
    p.append(g.cube("ped_a", (0, -0.35, 2.95), (3.2, 1.4, 0.12), m.marble, c, rot=(math.radians(20), 0, 0)))
    p.append(g.cube("ped_b", (0, 0.35, 2.95), (3.2, 1.4, 0.12), m.marble, c, rot=(math.radians(-20), 0, 0)))
    p.append(g.cyl("altar", (0, 0, 1.05), 0.55, 0.4, m.marble, c, verts=12))
    p.append(g.cyl("sun", (0, -1.15, 1.85), 0.35, 0.06, m.gold, c, verts=12, rot=(math.radians(90), 0, 0)))
    return finish(g, "building_church_offering_shrine", p, c, 0.03)


def bld_sacred_walls(g, m, c):
    p = []
    p.append(g.cube("base", (0, 0, 0.95), (9.0, 1.5, 1.85), m.marble, c))
    p.append(g.cube("walk", (0, 0, 2.0), (9.0, 1.65, 0.2), m.marble, c))
    wall_merlons(g, p, c, m.marble, -0.7, 2.45, [i * 1.15 for i in range(-3, 4) if i != 0], depth=0.45, h=0.72)
    wall_merlons(g, p, c, m.marble, 0.7, 2.45, [i * 1.15 for i in range(-3, 4) if i != 0], depth=0.45, h=0.72)
    p.append(g.taper("pil_l", (-4.2, 0, 1.5), 0.3, 0.22, 2.5, m.marble, c, verts=8))
    p.append(g.taper("pil_r", (4.2, 0, 1.5), 0.3, 0.22, 2.5, m.marble, c, verts=8))
    return finish(g, "building_church_sacred_walls", p, c, 0.03)


def bld_stone_wall_id(g, m, c):
    ob = bld_royal_wall(g, m, c)
    ob.name = "building_stone_wall"
    ob["definition_id"] = "building_stone_wall"
    return ob


BUILDINGS = {
    "building_arcane_academy": bld_arcane_academy,
    "building_blackroot_conservatory": bld_conservatory,
    "building_ancient_ruins": bld_ruins,
    "building_conjuring_hall": bld_conjuring_hall,
    "building_high_temple": bld_high_temple,
    "building_portal_gate": bld_portal_gate,
    "building_shadowed_gate": bld_shadowed_gate,
    "building_watchtower": bld_watchtower,
    "building_palisade": bld_palisade,
    "building_outpost": bld_outpost,
    "building_royal_barracks": bld_barracks,
    "building_royal_court": bld_court,
    "building_royal_farm": bld_farm,
    "building_royal_outpost_tower": bld_outpost_tower,
    "building_royal_wall": bld_royal_wall,
    "building_keep_turret": bld_keep_turret,
    "building_bridge": bld_bridge,
    "building_stone_wall": bld_stone_wall_id,
    "building_outcast_burrows": bld_burrows,
    "building_outcast_aerie": bld_aerie,
    "building_outcast_village_hall": bld_village_hall,
    "building_outcast_mine": bld_mine,
    "building_outcast_ground_works": bld_ground_works,
    "building_outcast_treetop_watch": lambda g, m, c: bld_aerie(g, m, c, "building_outcast_treetop_watch"),
    "building_freetown_smugglers_den": bld_smugglers_den,
    "building_freetown_hut": bld_hut,
    "building_freetown_black_market": bld_black_market,
    "building_freetown_crows_nest": bld_crows_nest,
    "building_freetown_barricades": bld_barricade,
    "building_barricade": lambda g, m, c: bld_barricade(g, m, c, "building_barricade"),
    "building_ferry_dock": bld_ferry_dock,
    "building_university_workshop": bld_workshop,
    "building_university_forbidden_library": bld_library,
    "building_university_alchemist": bld_alchemist,
    "building_university_clockwork_tower": bld_clockwork_tower,
    "building_university_moat": bld_moat,
    "building_university_grand_observatory": bld_observatory,
    "building_university_weather_rods": bld_weather_rods,
    "building_university_far_glass": bld_far_glass,
    "building_church_warrior_monastery": bld_monastery,
    "building_church_sun_temple": bld_sun_temple,
    "building_church_sacred_site": bld_sacred_site,
    "building_church_scorched_tower": bld_scorched_tower,
    "building_church_offering_shrine": bld_shrine,
    "building_church_sacred_walls": bld_sacred_walls,
}
