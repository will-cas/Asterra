"""Shared join/unwrap and architectural details. Citadel construction language."""
from __future__ import annotations

import math

import bpy
from mathutils import Vector


def finish(g, name, parts, collection, bevel_w=0.016, segments=4, subdiv=0):
    ob = g.join(name, parts, collection)
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    if bevel_w:
        g.bevel(ob, bevel_w, segments)
    if subdiv and hasattr(g, "subdiv"):
        g.subdiv(ob, subdiv)
    g.unwrap_smart(ob)
    ob["definition_id"] = name
    return ob


def window(g, p, c, frame, glass, loc, size, yaw=0.0, mullions=True):
    """Recessed opening: frame, dark void, glass, mullion, sill."""
    p.append(g.cube("wf", loc, size, frame, c, rot=(0, 0, yaw)))
    inset = list(loc)
    if abs(yaw) < 0.1:
        inset[1] += 0.08 if loc[1] >= 0 else -0.08
    else:
        inset[0] += 0.08 if loc[0] >= 0 else -0.08
    gs = (size[0] * 0.68, size[1] * 0.45, size[2] * 0.68)
    p.append(g.cube("wv", tuple(inset), (size[0] * 0.78, size[1] * 0.5, size[2] * 0.78), frame, c, rot=(0, 0, yaw)))
    p.append(g.cube("wg", tuple(inset), gs, glass, c, rot=(0, 0, yaw)))
    if mullions:
        p.append(g.cube("wm", tuple(inset), (0.06, size[1] * 0.52, gs[2]), frame, c, rot=(0, 0, yaw)))
        p.append(g.cube("wh", tuple(inset), (gs[0], size[1] * 0.52, 0.05), frame, c, rot=(0, 0, yaw)))
    sill = list(loc)
    sill[2] -= size[2] * 0.52
    if abs(yaw) < 0.1:
        sill[1] += -0.06 if loc[1] < 0 else 0.06
        p.append(g.cube("sill", tuple(sill), (size[0] * 1.12, 0.28, 0.08), frame, c, rot=(0, 0, yaw)))
    else:
        sill[0] += -0.06 if loc[0] < 0 else 0.06
        p.append(g.cube("sill", tuple(sill), (0.28, size[1] * 1.12, 0.08), frame, c, rot=(0, 0, yaw)))


def stairs(g, p, c, mat, origin, count=8, width=2.2, step=(0.28, 0.18)):
    ox, oy, oz = origin
    for s in range(count):
        p.append(g.cube(f"st{s}", (ox, oy - s * step[0], oz + s * step[1]), (width, 0.38, 0.18), mat, c))


def string_course(g, p, c, mat, loc, size):
    p.append(g.cube("string", loc, size, mat, c))


def cornice(g, p, c, mat, loc, size):
    p.append(g.cube("cornice", loc, size, mat, c))


def quoins(g, p, c, mat, cx, cy, z, hx, hy, h, inset=0.15):
    for x, y in ((hx, hy), (hx, -hy), (-hx, hy), (-hx, -hy)):
        p.append(g.cube("quoin", (cx + x, cy + y, z), (0.85, 0.85, h), mat, c))


def chimney(g, p, c, brick, cap, loc, h=1.2, r=0.24):
    x, y, z = loc
    p.append(g.cyl("chim", (x, y, z), r, h, brick, c, verts=10))
    p.append(g.cube("chim_cap", (x, y, z + h * 0.42), (r * 2.4, r * 2.4, 0.1), cap, c))


def wall_merlons(g, p, c, mat, y, z, xs, depth=0.5, h=0.7):
    for x in xs:
        p.append(g.cube("mer", (x, y, z), (0.5, depth, h), mat, c))


def slit_window(g, p, c, frame, glass, loc, size, yaw=0.0):
    """Narrow lancet: thin iron, glass, almost no sill."""
    p.append(g.cube("wf", loc, size, frame, c, rot=(0, 0, yaw)))
    inset = list(loc)
    if abs(yaw) < 0.1:
        inset[1] += 0.04 if loc[1] >= 0 else -0.04
    else:
        inset[0] += 0.04 if loc[0] >= 0 else -0.04
    gs = (size[0] * 0.55, size[1] * 0.35, size[2] * 0.72)
    p.append(g.cube("wg", tuple(inset), gs, glass, c, rot=(0, 0, yaw)))


def banner(g, p, c, pole, cloth, loc, h=3.2, fly=1.4):
    x, y, z = loc
    p.append(g.cyl("pole", (x, y, z), 0.06, h, pole, c, verts=8))
    p.append(g.cube("ban", (x + fly * 0.45, y, z + h * 0.18), (fly, 0.05, 0.85), cloth, c))
    p.append(g.cube("ban2", (x + fly * 0.42, y + 0.04, z + h * 0.02), (fly * 0.88, 0.04, 0.55), cloth, c, rot=(0, 0, math.radians(8))))


def facade_windows(g, p, c, frame, glass, y, z, xs, size=(1.05, 0.18, 1.35), yaw=0.0):
    for x in xs:
        window(g, p, c, frame, glass, (x, y, z), size, yaw=yaw)


def side_windows(g, p, c, frame, glass, x, z, ys, size=(0.18, 1.05, 1.35)):
    yaw = math.radians(90)
    for y in ys:
        window(g, p, c, frame, glass, (x, y, z), size, yaw=yaw)


def timber_posts(g, p, c, mat, xs, y, z, h, r=0.14):
    for x in xs:
        p.append(g.cyl("tpost", (x, y, z), r, h, mat, c, verts=8))


def log_wall(g, p, c, mat, x, y, z0, length, count=12, r=0.13, axis="y"):
    for i in range(count):
        z = z0 + i * (r * 1.7)
        if axis == "y":
            p.append(g.cyl("log", (x, y, z), r, length, mat, c, verts=8, rot=(math.radians(90), 0, 0)))
        else:
            p.append(g.cyl("log", (x, y, z), r, length, mat, c, verts=8, rot=(0, math.radians(90), 0)))


def ladder_to(g, p, c, mat, y0, z0, y1, z1, count=12, width=1.15, rail_x=0.5):
    """Stairs that actually meet a deck instead of walking off into space."""
    for s in range(count):
        t = s / max(count - 1, 1)
        y = y0 + (y1 - y0) * t
        z = z0 + (z1 - z0) * t
        p.append(g.cube("lad", (0, y, z), (width, 0.38, 0.1), mat, c))
        if s % 2 == 0:
            p.append(g.cyl("rail", (rail_x, y, z - 0.15), 0.06, 0.55, mat, c, verts=6))


def merlons(g, p, c, mat, cx, cy, z, half, count=5, skip_gate=True):
    for i in range(-count, count + 1):
        x = i * 1.28
        if skip_gate and abs(x) < 1.6 and cy > 0:
            continue
        h = 0.85 + (0.12 if i % 2 == 0 else 0)
        p.append(g.cube("mn", (cx + x, cy + half, z), (0.52, 0.48, h), mat, c))
        p.append(g.cube("ms", (cx + x, cy - half, z), (0.52, 0.48, h), mat, c))
        p.append(g.cube("me", (cx + half, cy + x, z), (0.48, 0.52, h), mat, c))
        p.append(g.cube("mw", (cx - half, cy + x, z), (0.48, 0.52, h), mat, c))


def stone_drum(g, p, c, mat, z0, z1, radius, verts=24, course_h=0.26, uv=0.45, cx=0.0, cy=0.0):
    """Stacked masonry rings so a tower reads as cut stone, not a plastic cylinder."""
    z = z0
    i = 0
    while z + course_h * 0.4 < z1:
        h = min(course_h, z1 - z)
        rr = radius * (0.992 if i % 2 else 1.0)
        p.append(g.cyl(f"course{i}", (cx, cy, z + h * 0.5), rr, h * 0.94, mat, c, verts=verts, uv=uv))
        z += course_h
        i += 1
    return i


def stone_shaft(g, p, c, mat, z0, z1, r0, r1, verts=20, course_h=0.2, uv=0.35, cx=0.0, cy=0.0):
    """Tapered coursed tower shaft."""
    z = z0
    i = 0
    htot = max(z1 - z0, 0.01)
    while z + course_h * 0.35 < z1:
        h = min(course_h, z1 - z)
        t = (z - z0) / htot
        rr = (r0 + (r1 - r0) * t) * (0.992 if i % 2 else 1.0)
        p.append(g.cyl(f"shaft{i}", (cx, cy, z + h * 0.5), rr, h * 0.94, mat, c, verts=verts, uv=uv))
        z += course_h
        i += 1
    return i


def ashlar_face(g, p, c, mat, y, z0, z1, x0, x1, depth=0.08, bw=0.52, bh=0.22):
    """Running bond on a facade so a hall is not a blank plaster slab."""
    row = 0
    z = z0
    while z < z1:
        off = (row % 2) * (bw * 0.48)
        x = x0 + off
        while x < x1:
            p.append(g.cube("ash", (x, y, z), (bw, depth, bh), mat, c))
            x += bw + 0.05
        z += bh + 0.05
        row += 1


def arch(g, p, c, mat, loc, radius=1.45, depth=0.95, z0=2.55, count=9, yaw=0.0, block=(0.42, None, 0.36)):
    x, y, _ = loc
    bw, _, bh = block
    bd = depth if block[1] is None else block[1]
    for k in range(count):
        t = math.pi * k / (count - 1)
        ax = math.cos(t) * radius
        az = z0 + math.sin(t) * (radius * 0.92)
        if abs(yaw) < 0.1:
            p.append(g.cube("vous", (x + ax, y, az), (bw, bd, bh), mat, c, rot=(0, math.pi / 2 - t, 0)))
        else:
            p.append(g.cube("vous", (x, y + ax, az), (bd, bw, bh), mat, c, rot=(math.pi / 2 - t, 0, yaw)))


def door(g, p, c, wood, iron, loc, size=(1.0, 0.14, 2.5)):
    x, y, z = loc
    sx, sy, sz = size
    p.append(g.cube("door_l", (x - sx * 0.52, y, z), (sx, sy, sz), wood, c))
    p.append(g.cube("door_r", (x + sx * 0.52, y, z), (sx, sy, sz), wood, c))
    hy = y - 0.08 if y < 0 else y + 0.08
    for k in range(5):
        p.append(g.cyl(f"hl{k}", (x - sx, hy, z - sz * 0.35 + k * 0.42), 0.035, 0.05, iron, c, verts=8, rot=(math.radians(90), 0, 0)))
        p.append(g.cyl(f"hr{k}", (x + sx, hy, z - sz * 0.35 + k * 0.42), 0.035, 0.05, iron, c, verts=8, rot=(math.radians(90), 0, 0)))


def _slab(g, p, c, mat, corners, thick, uv=0.85):
    a, b, c0, d = [Vector(v) for v in corners]
    n = (b - a).cross(d - a)
    if n.length < 1e-6:
        return
    n.normalize()
    n *= thick
    top = [a, b, c0, d]
    bot = [v - n for v in top]
    verts = [(v.x, v.y, v.z) for v in top + bot]
    faces = (
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    )
    p.append(g.solid("roof", verts, faces, mat, c, uv=uv))


def _tri_slab(g, p, c, mat, corners, thick, uv=0.7):
    a, b, d = [Vector(v) for v in corners]
    n = (b - a).cross(d - a)
    if n.length < 1e-6:
        return
    n.normalize()
    n *= thick
    top = [a, b, d]
    bot = [v - n for v in top]
    verts = [(v.x, v.y, v.z) for v in top + bot]
    faces = (
        (0, 1, 2),
        (3, 5, 4),
        (0, 3, 4, 1),
        (1, 4, 5, 2),
        (2, 5, 3, 0),
    )
    p.append(g.solid("gable", verts, faces, mat, c, uv=uv))


def pitched(g, p, c, mat, loc, size, pitch=32, gable=None, axis="x", tiles=False):
    """Closed gable: two roof planes that meet the ridge, triangular gable walls.

    loc.z is wall-top / eave at the wall line. Planes continue a short overhang.
    `tiles` is kept for callers; slate UVs on the planes do the work.
    """
    x, y, z = loc
    sx, sy, _sz = size
    rad = math.radians(pitch)
    fill = gable or mat
    ov = 0.16
    thick = 0.09
    wall_t = 0.12
    if axis == "y":
        half = sx * 0.5
        rise = math.tan(rad) * half
        hx = half + ov
        hy = sy * 0.5 + ov
        ze = z - math.tan(rad) * ov
        zr = z + rise
        _slab(
            g, p, c, mat,
            ((x - hx, y - hy, ze), (x - hx, y + hy, ze), (x, y + hy, zr), (x, y - hy, zr)),
            thick,
        )
        _slab(
            g, p, c, mat,
            ((x + hx, y + hy, ze), (x + hx, y - hy, ze), (x, y - hy, zr), (x, y + hy, zr)),
            thick,
        )
        gy = sy * 0.5
        _tri_slab(
            g, p, c, fill,
            ((x - half, y - gy, z), (x + half, y - gy, z), (x, y - gy, zr)),
            wall_t,
        )
        _tri_slab(
            g, p, c, fill,
            ((x + half, y + gy, z), (x - half, y + gy, z), (x, y + gy, zr)),
            wall_t,
        )
        return
    half = sy * 0.5
    rise = math.tan(rad) * half
    hy = half + ov
    hx = sx * 0.5 + ov
    ze = z - math.tan(rad) * ov
    zr = z + rise
    _slab(
        g, p, c, mat,
        ((x - hx, y - hy, ze), (x + hx, y - hy, ze), (x + hx, y, zr), (x - hx, y, zr)),
        thick,
    )
    _slab(
        g, p, c, mat,
        ((x + hx, y + hy, ze), (x - hx, y + hy, ze), (x - hx, y, zr), (x + hx, y, zr)),
        thick,
    )
    gx = sx * 0.5
    _tri_slab(
        g, p, c, fill,
        ((x - gx, y - half, z), (x - gx, y + half, z), (x - gx, y, zr)),
        wall_t,
    )
    _tri_slab(
        g, p, c, fill,
        ((x + gx, y + half, z), (x + gx, y - half, z), (x + gx, y, zr)),
        wall_t,
    )
