"""Faction keeps — same construction language as the Mundor citadel."""
from __future__ import annotations

import math

from asterra_detail import arch, door, finish, pitched, stairs, window


def keep_arcaneum(g, m, c):
    """Uncrowned: iron-and-glass keep. Layered shaft, recessed bays, gate with hinges."""
    p = []
    p.append(g.cube("earth", (0, 0, 0.28), (12.0, 12.0, 0.56), m.dark_stone, c))
    p.append(g.cube("plinth", (0, 0, 0.85), (16.2, 16.2, 0.7), m.steel, c))
    p.append(g.cube("plinth_cap", (0, 0, 1.22), (16.5, 16.5, 0.16), m.iron, c))
    p.append(g.cube("shaft", (0, 0, 5.55), (7.6, 7.6, 8.6), m.dark_stone, c))
    p.append(g.cube("string", (0, 0, 7.4), (7.95, 7.95, 0.18), m.steel, c))
    p.append(g.cube("cornice", (0, 0, 9.85), (8.15, 8.15, 0.22), m.steel, c))
    p.append(g.cube("walk", (0, 0, 10.08), (8.4, 8.4, 0.2), m.iron, c))
    for i in range(-2, 3):
        x = i * 1.25
        for z in (3.15, 4.95, 6.75, 8.45):
            window(g, p, c, m.steel, m.glass, (x, -3.85, z), (1.05, 0.22, 1.45))
            window(g, p, c, m.steel, m.glass, (x, 3.85, z), (1.05, 0.22, 1.45))
            window(g, p, c, m.steel, m.glass, (3.85, x, z), (0.22, 1.05, 1.45), yaw=math.radians(90))
            window(g, p, c, m.steel, m.glass, (-3.85, x, z), (0.22, 1.05, 1.45), yaw=math.radians(90))
    for i in range(-3, 4):
        p.append(g.cube(f"rib_v{i}", (i * 1.25, -3.95, 5.55), (0.12, 0.12, 8.4), m.steel, c))
    p.append(g.cube("rib_h1", (0, -3.95, 4.05), (7.7, 0.12, 0.12), m.steel, c))
    p.append(g.cube("rib_h2", (0, -3.95, 7.55), (7.7, 0.12, 0.12), m.steel, c))
    p.append(g.cube("lantern", (0, 0, 11.35), (5.1, 5.1, 2.2), m.glass, c))
    for i in range(-2, 3):
        p.append(g.cube(f"lrib_x{i}", (i * 1.05, 0, 11.35), (0.1, 5.2, 2.2), m.steel, c))
        p.append(g.cube(f"lrib_y{i}", (0, i * 1.05, 11.35), (5.2, 0.1, 2.2), m.steel, c))
    p.append(g.cube("lcap", (0, 0, 12.55), (5.5, 5.5, 0.24), m.steel, c))
    p.append(g.cube("lfin", (0, 0, 13.05), (0.16, 0.16, 0.7), m.steel, c))
    p.append(g.uv_sphere("crystal", (0, 0, 13.5), 0.26, m.crystal, c, segs=12, rings=8))
    for i, (x, y) in enumerate(((4.85, 4.85), (4.85, -4.85), (-4.85, 4.85), (-4.85, -4.85))):
        p.append(g.taper(f"butt_{i}", (x, y, 3.4), 0.95, 0.4, 5.6, m.steel, c, verts=8, rot=(math.radians(6), 0, math.atan2(y, x))))
        p.append(g.cube(f"bcap_{i}", (x * 0.92, y * 0.92, 6.15), (0.7, 0.7, 0.16), m.iron, c))
    p.append(g.cube("gatehouse", (0, -5.85, 3.05), (5.6, 3.6, 4.6), m.dark_stone, c))
    p.append(g.cube("gate_roof_a", (0, -6.55, 5.55), (6.0, 2.2, 0.14), m.steel, c, rot=(math.radians(16), 0, 0)))
    p.append(g.cube("gate_roof_b", (0, -5.15, 5.55), (6.0, 2.2, 0.14), m.steel, c, rot=(math.radians(-16), 0, 0)))
    p.append(g.cube("pier_l", (-1.45, -7.55, 1.55), (0.65, 1.0, 2.9), m.steel, c))
    p.append(g.cube("pier_r", (1.45, -7.55, 1.55), (0.65, 1.0, 2.9), m.steel, c))
    arch(g, p, c, m.steel, (0, -7.6, 0), radius=1.4, depth=0.9, z0=2.45, count=9)
    door(g, p, c, m.steel, m.iron, (0, -7.72, 1.35), (0.95, 0.12, 2.4))
    p.append(g.cube("dglass", (0, -7.82, 1.55), (1.25, 0.05, 1.45), m.glass, c))
    for k in range(6):
        p.append(g.cyl(f"port{k}", (-0.75 + k * 0.3, -7.35, 2.35), 0.035, 2.3, m.iron, c, verts=8))
    stairs(g, p, c, m.dark_stone, (0, -4.55, 0.55), count=7, width=2.4, step=(0.26, 0.16))
    p.append(g.cyl("pole", (2.35, -6.2, 7.4), 0.06, 3.2, m.steel, c, verts=8))
    p.append(g.cube("ban1", (3.15, -6.2, 8.15), (1.5, 0.05, 0.85), m.cloth_purple, c))
    p.append(g.cube("ban2", (3.05, -6.18, 7.45), (1.35, 0.04, 0.5), m.steel, c))
    return finish(g, "building_arcaneum", p, c, 0.05, 3)


def keep_great_camp(g, m, c):
    """Outcast longhouse: stacked logs to the eave, timber gable, snow tiles, door with hinges."""
    p = []
    p.append(g.cube("earth", (0, 0, 0.22), (11.0, 15.0, 0.44), m.ice, c))
    p.append(g.cube("plinth", (0, 0.3, 0.52), (8.2, 15.2, 0.28), m.bark, c))
    p.append(g.cube("floor", (0, 0.3, 0.68), (7.4, 14.4, 0.16), m.wood, c))
    for i in range(16):
        z = 0.85 + i * 0.24
        p.append(g.cyl(f"lw{i}", (-3.55, 0.3, z), 0.15, 14.2, m.wood, c, verts=10, rot=(math.radians(90), 0, 0)))
        p.append(g.cyl(f"le{i}", (3.55, 0.3, z), 0.15, 14.2, m.wood, c, verts=10, rot=(math.radians(90), 0, 0)))
    for y in (-6.2, -3.1, 0.3, 3.7, 6.8):
        p.append(g.cyl(f"postw{y}", (-3.7, y, 2.55), 0.2, 4.2, m.bark, c, verts=8))
        p.append(g.cyl(f"poste{y}", (3.7, y, 2.55), 0.2, 4.2, m.bark, c, verts=8))
        p.append(g.cube(f"knee_w{y}", (-3.7, y, 4.55), (0.35, 0.35, 0.22), m.bark, c))
        p.append(g.cube(f"knee_e{y}", (3.7, y, 4.55), (0.35, 0.35, 0.22), m.bark, c))
    p.append(g.cube("front", (0, -6.75, 2.45), (7.0, 0.32, 3.6), m.wood, c))
    p.append(g.cube("back", (0, 7.35, 2.45), (7.0, 0.32, 3.6), m.wood, c))
    pitched(g, p, c, m.bark, (0, 0.3, 4.65), (8.0, 15.2, 0.18), pitch=40, gable=m.wood, axis="y", tiles=True)
    pitched(g, p, c, m.ice, (0, 0.3, 4.82), (7.4, 14.6, 0.07), pitch=40, gable=m.ice, axis="y", tiles=False)
    p.append(g.cube("lintel", (0, -6.95, 2.85), (2.6, 0.28, 0.28), m.ice, c))
    door(g, p, c, m.leather, m.iron, (0, -6.98, 1.45), (0.85, 0.12, 2.2))
    p.append(g.cyl("post_l", (-1.25, -6.92, 1.7), 0.16, 2.6, m.bark, c, verts=8))
    p.append(g.cyl("post_r", (1.25, -6.92, 1.7), 0.16, 2.6, m.bark, c, verts=8))
    for i in range(4):
        p.append(g.cube(f"icicle{i}", (-1.8 + i * 1.2, -6.9, 4.55), (0.07, 0.07, 0.4 + (i % 2) * 0.15), m.ice, c))
    p.append(g.cyl("smoke", (0.55, 3.6, 7.15), 0.24, 1.35, m.bark, c, verts=10))
    p.append(g.cube("smoke_cap", (0.55, 3.6, 7.85), (0.5, 0.5, 0.1), m.ice, c))
    for i in range(18):
        ang = i * math.pi / 9 + 0.15
        if math.sin(ang) < -0.72:
            continue
        r = 8.8
        p.append(g.cyl(f"pal{i}", (math.cos(ang) * r, math.sin(ang) * r, 1.05), 0.14, 1.85, m.wood, c, verts=6))
        p.append(g.cone(f"pt{i}", (math.cos(ang) * r, math.sin(ang) * r, 2.05), 0.12, 0.38, m.ice, c, verts=5))
    p.append(g.cyl("pole", (-2.35, -6.9, 5.15), 0.07, 2.6, m.wood, c, verts=8))
    p.append(g.cube("ban1", (-3.15, -6.9, 5.85), (1.45, 0.05, 0.8), m.cloth_green, c))
    p.append(g.cube("ban2", (-3.05, -6.88, 5.25), (1.25, 0.04, 0.45), m.bark, c))
    stairs(g, p, c, m.wood, (0, -7.7, 0.4), count=6, width=2.2, step=(0.28, 0.16))
    return finish(g, "building_outcast_great_camp", p, c, 0.04, 3)


def keep_tavern(g, m, c):
    """Freetown pub: stone ground, jettied timber, slate tiles, dock, hanging sign."""
    p = []
    p.append(g.cube("earth", (0, 0, 0.22), (12.0, 14.0, 0.44), m.brick, c))
    p.append(g.cube("quay", (0, 0.2, 0.52), (11.5, 15.2, 0.28), m.brick, c))
    p.append(g.cube("stone", (0, 0.7, 1.65), (7.2, 10.2, 2.15), m.brick, c))
    p.append(g.cube("quoin_l", (-3.45, -4.2, 1.65), (0.7, 0.7, 2.2), m.brick, c))
    p.append(g.cube("quoin_r", (3.45, -4.2, 1.65), (0.7, 0.7, 2.2), m.brick, c))
    p.append(g.cube("timber", (0, 0.7, 3.75), (7.7, 10.7, 2.2), m.pale_wood, c))
    p.append(g.cube("jetty", (0, -4.55, 2.72), (7.7, 0.85, 0.16), m.wood, c))
    for x in (-2.8, -0.9, 0.9, 2.8):
        p.append(g.cube(f"brkt{x}", (x, -4.7, 2.45), (0.16, 0.55, 0.4), m.wood, c, rot=(math.radians(18), 0, 0)))
    pitched(g, p, c, m.slate, (0, 0.7, 5.0), (8.2, 11.4, 0.18), pitch=38, gable=m.pale_wood, axis="y", tiles=True)
    p.append(g.cyl("chim", (1.25, 2.6, 7.05), 0.34, 1.7, m.brick, c, verts=12))
    p.append(g.cube("chim_cap", (1.25, 2.6, 7.95), (0.62, 0.62, 0.12), m.slate, c))
    p.append(g.cube("pot", (1.25, 2.6, 8.15), (0.22, 0.22, 0.28), m.brick, c))
    p.append(g.cube("front", (0, -4.5, 3.75), (7.3, 0.28, 2.15), m.pale_wood, c))
    for x in (-1.7, 1.7):
        window(g, p, c, m.wood, m.glass, (x, -4.62, 3.85), (1.2, 0.2, 1.25))
        window(g, p, c, m.wood, m.glass, (x, -5.05, 1.7), (1.05, 0.2, 1.05))
    p.append(g.cube("lintel", (0, -5.0, 2.45), (2.1, 0.28, 0.22), m.wood, c))
    door(g, p, c, m.wood, m.iron, (0, -5.08, 1.4), (0.75, 0.12, 1.95))
    p.append(g.cube("signp", (1.65, -4.95, 3.35), (0.08, 0.08, 1.05), m.wood, c))
    p.append(g.cube("sign", (1.65, -5.2, 2.95), (1.25, 0.07, 0.6), m.cloth_blue, c))
    p.append(g.cube("dock", (0, -7.15, 0.2), (10.4, 4.0, 0.18), m.wood, c))
    for i in range(-4, 5):
        p.append(g.cube(f"plank{i}", (i * 1.05, -7.15, 0.32), (0.95, 3.9, 0.05), m.pale_wood, c))
        p.append(g.cyl(f"pile{i}", (i * 1.05, -8.95, 0.2), 0.15, 1.25, m.wood, c, verts=8))
        p.append(g.cyl(f"band{i}", (i * 1.05, -8.95, 0.55), 0.17, 0.05, m.iron, c, verts=8))
    for i in range(4):
        p.append(g.cyl(f"barrel{i}", (-2.2 + i * 0.85, -5.45, 0.62), 0.26, 0.52, m.wood, c, verts=12))
        p.append(g.cyl(f"hoop{i}", (-2.2 + i * 0.85, -5.45, 0.78), 0.27, 0.04, m.iron, c, verts=12))
    p.append(g.cube("net", (2.8, -7.85, 1.15), (1.7, 0.05, 1.25), m.cloth_blue, c, rot=(math.radians(16), 0, 0)))
    stairs(g, p, c, m.brick, (0, -5.55, 0.4), count=6, width=2.2, step=(0.26, 0.16))
    return finish(g, "building_freetown_tavern", p, c, 0.045, 3)


def keep_college(g, m, c):
    """University hall: buttressed brick, window rhythm, supporting portico, clock tower."""
    p = []
    p.append(g.cube("earth", (0, 0, 0.22), (16.0, 12.0, 0.44), m.brick, c))
    p.append(g.cube("plinth", (0, 0.4, 0.55), (16.4, 9.2, 0.32), m.marble, c))
    p.append(g.cube("hall", (0, 0.5, 2.85), (14.6, 7.8, 4.7), m.red_brick, c))
    p.append(g.cube("string", (0, 0.5, 5.15), (15.1, 8.25, 0.18), m.marble, c))
    p.append(g.cube("cornice", (0, 0.5, 5.35), (15.3, 8.45, 0.16), m.slate, c))
    for i in range(-3, 4):
        x = i * 2.05
        p.append(g.taper(f"butt{i}", (x, -3.85, 2.2), 0.38, 0.22, 3.6, m.red_brick, c, verts=8, rot=(math.radians(8), 0, 0)))
    pitched(g, p, c, m.slate, (0, 0.5, 5.45), (15.4, 8.6, 0.18), pitch=34, gable=m.red_brick, tiles=True)
    for i in range(6):
        x = -5.4 + i * 2.15
        window(g, p, c, m.marble, m.glass, (x, -3.45, 3.65), (1.25, 0.22, 2.0))
        window(g, p, c, m.wood, m.glass, (x, -3.45, 1.55), (1.05, 0.2, 1.15))
    p.append(g.cube("portico", (0, -4.15, 0.95), (6.8, 1.9, 0.24), m.marble, c))
    p.append(g.taper("col_l", (-2.45, -4.35, 2.15), 0.24, 0.2, 2.5, m.marble, c, verts=10))
    p.append(g.taper("col_r", (2.45, -4.35, 2.15), 0.24, 0.2, 2.5, m.marble, c, verts=10))
    p.append(g.cube("entab", (0, -4.35, 3.45), (6.6, 1.3, 0.22), m.marble, c))
    p.append(g.cube("lintel", (0, -3.55, 2.65), (2.4, 0.28, 0.22), m.marble, c))
    door(g, p, c, m.wood, m.iron, (0, -3.62, 1.5), (0.85, 0.14, 2.15))
    stairs(g, p, c, m.marble, (0, -5.15, 0.35), count=7, width=5.8, step=(0.26, 0.14))
    p.append(g.cube("tower", (6.55, -1.55, 5.35), (3.5, 3.5, 7.8), m.red_brick, c))
    p.append(g.cube("tstring", (6.55, -1.55, 8.55), (3.8, 3.8, 0.18), m.marble, c))
    p.append(g.cube("tcornice", (6.55, -1.55, 9.15), (3.95, 3.95, 0.22), m.marble, c))
    p.append(g.cube("twalk", (6.55, -1.55, 9.35), (4.15, 4.15, 0.18), m.slate, c))
    for k in range(8):
        ang = k * math.pi / 4
        p.append(g.cube(f"tmer{k}", (6.55 + math.cos(ang) * 1.85, -1.55 + math.sin(ang) * 1.85, 9.75), (0.4, 0.4, 0.65), m.slate, c))
    p.append(g.cube("troof", (6.55, -1.55, 9.45), (3.4, 3.4, 0.16), m.slate, c))
    p.append(g.cyl("clock", (6.55, -3.35, 6.85), 0.7, 0.12, m.gold, c, verts=16, rot=(math.radians(90), 0, 0)))
    p.append(g.cyl("clock_rim", (6.55, -3.4, 6.85), 0.76, 0.05, m.iron, c, verts=16, rot=(math.radians(90), 0, 0)))
    p.append(g.cube("hand_h", (6.55, -3.45, 6.85), (0.42, 0.04, 0.05), m.iron, c))
    p.append(g.cube("hand_m", (6.55, -3.45, 7.05), (0.05, 0.04, 0.28), m.iron, c))
    for i, x in enumerate((-4.4, -1.5, 1.4, 4.2)):
        p.append(g.cyl(f"chim{i}", (x, 2.55, 7.15), 0.26, 1.35, m.red_brick, c, verts=10))
        p.append(g.cube(f"cc{i}", (x, 2.55, 7.85), (0.48, 0.48, 0.1), m.slate, c))
        p.append(g.cube(f"pot{i}", (x, 2.55, 8.05), (0.16, 0.16, 0.22), m.red_brick, c))
    p.append(g.cyl("pole", (4.4, -3.6, 6.4), 0.06, 2.8, m.wood, c, verts=8))
    p.append(g.cube("ban", (5.15, -3.6, 7.15), (1.35, 0.05, 0.75), m.cloth_deep, c))
    return finish(g, "building_university_grand_college", p, c, 0.05, 3)


def keep_temple(g, m, c):
    """Renaissance church: true portico roof as pediment, drum, dome, nave windows."""
    p = []
    p.append(g.cube("earth", (0, 0, 0.22), (14.0, 14.0, 0.44), m.marble, c))
    p.append(g.cube("plaza", (0, 0, 0.52), (13.0, 13.0, 0.24), m.marble, c))
    p.append(g.cube("nave", (0, 1.7, 3.25), (8.8, 12.8, 5.4), m.marble, c))
    p.append(g.cube("string", (0, 1.7, 5.85), (9.25, 13.25, 0.18), m.gold, c))
    p.append(g.cube("cornice", (0, 1.7, 6.05), (9.45, 13.45, 0.16), m.marble, c))
    pitched(g, p, c, m.slate, (0, 1.7, 6.2), (9.6, 13.6, 0.18), pitch=20, gable=m.marble, tiles=True)
    for i in range(4):
        y = -2.2 + i * 2.4
        window(g, p, c, m.marble, m.glass, (4.5, y, 3.55), (0.2, 1.15, 2.15), yaw=math.radians(90))
        window(g, p, c, m.marble, m.glass, (-4.5, y, 3.55), (0.2, 1.15, 2.15), yaw=math.radians(90))
    p.append(g.cube("stylobate", (0, -5.85, 0.78), (10.2, 3.6, 0.5), m.marble, c))
    for i, x in enumerate((-3.7, -2.22, -0.74, 0.74, 2.22, 3.7)):
        p.append(g.cube(f"base{i}", (x, -6.05, 1.05), (0.5, 0.5, 0.32), m.marble, c))
        p.append(g.taper(f"col{i}", (x, -6.05, 2.7), 0.28, 0.22, 3.15, m.marble, c, verts=12))
        p.append(g.cube(f"cap{i}", (x, -6.05, 4.35), (0.52, 0.48, 0.2), m.gold, c))
    p.append(g.cube("architrave", (0, -6.05, 4.58), (9.6, 1.2, 0.26), m.marble, c))
    p.append(g.cube("frieze", (0, -6.05, 4.82), (9.5, 1.15, 0.22), m.gold, c))
    p.append(g.cube("pcornice", (0, -6.05, 5.02), (9.8, 1.35, 0.18), m.marble, c))
    p.append(g.cube("ped_a", (0, -6.35, 5.55), (9.4, 1.6, 0.14), m.marble, c, rot=(math.radians(22), 0, 0)))
    p.append(g.cube("ped_b", (0, -5.75, 5.55), (9.4, 1.6, 0.14), m.marble, c, rot=(math.radians(-22), 0, 0)))
    p.append(g.cyl("sun", (0, -6.55, 5.55), 0.48, 0.08, m.gold, c, verts=16, rot=(math.radians(90), 0, 0)))
    arch(g, p, c, m.marble, (0, -4.85, 0), radius=1.15, depth=0.55, z0=2.55, count=9)
    door(g, p, c, m.wood, m.iron, (0, -4.92, 1.65), (0.9, 0.14, 2.4))
    stairs(g, p, c, m.marble, (0, -7.65, 0.3), count=8, width=7.4, step=(0.26, 0.14))
    p.append(g.cyl("drum", (0, 3.55, 7.85), 2.25, 1.7, m.marble, c, verts=18))
    p.append(g.cube("drum_ring", (0, 3.55, 8.65), (4.7, 4.7, 0.16), m.gold, c))
    p.append(g.uv_sphere("dome", (0, 3.55, 9.15), 2.35, m.gold, c, segs=18, rings=12))
    p.append(g.cyl("lantern", (0, 3.55, 11.0), 0.38, 0.75, m.marble, c, verts=10))
    p.append(g.cone("lantern_c", (0, 3.55, 11.5), 0.42, 0.5, m.gold, c, verts=8))
    p.append(g.cyl("pole", (3.4, -5.9, 7.2), 0.06, 2.8, m.wood, c, verts=8))
    p.append(g.cube("ban", (4.15, -5.9, 7.9), (1.4, 0.05, 0.8), m.cloth, c))
    return finish(g, "building_church_grand_temple", p, c, 0.05, 3)


KEEPS = {
    "building_arcaneum": keep_arcaneum,
    "building_outcast_great_camp": keep_great_camp,
    "building_freetown_tavern": keep_tavern,
    "building_university_grand_college": keep_college,
    "building_church_grand_temple": keep_temple,
}
