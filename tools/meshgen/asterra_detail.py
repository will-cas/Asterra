"""Shared join/unwrap and architectural details. Citadel construction language."""
from __future__ import annotations

import math

import bpy


def finish(g, name, parts, collection, bevel_w=0.045, segments=2):
    ob = g.join(name, parts, collection)
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    if bevel_w:
        g.bevel(ob, bevel_w, segments)
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


def banner(g, p, c, pole, cloth, loc, h=3.2, fly=1.4):
    x, y, z = loc
    p.append(g.cyl("pole", (x, y, z), 0.06, h, pole, c, verts=8))
    p.append(g.cube("ban", (x + fly * 0.45, y, z + h * 0.18), (fly, 0.05, 0.85), cloth, c))


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


def arch(g, p, c, mat, loc, radius=1.45, depth=0.95, z0=2.55, count=9, yaw=0.0):
    x, y, _ = loc
    for k in range(count):
        t = math.pi * k / (count - 1)
        ax = math.cos(t) * radius
        az = z0 + math.sin(t) * (radius * 0.92)
        if abs(yaw) < 0.1:
            p.append(g.cube("vous", (x + ax, y, az), (0.42, depth, 0.36), mat, c, rot=(0, math.pi / 2 - t, 0)))
        else:
            p.append(g.cube("vous", (x, y + ax, az), (depth, 0.42, 0.36), mat, c, rot=(math.pi / 2 - t, 0, yaw)))


def door(g, p, c, wood, iron, loc, size=(1.0, 0.14, 2.5)):
    x, y, z = loc
    sx, sy, sz = size
    p.append(g.cube("door_l", (x - sx * 0.52, y, z), (sx, sy, sz), wood, c))
    p.append(g.cube("door_r", (x + sx * 0.52, y, z), (sx, sy, sz), wood, c))
    hy = y - 0.08 if y < 0 else y + 0.08
    for k in range(5):
        p.append(g.cyl(f"hl{k}", (x - sx, hy, z - sz * 0.35 + k * 0.42), 0.035, 0.05, iron, c, verts=8, rot=(math.radians(90), 0, 0)))
        p.append(g.cyl(f"hr{k}", (x + sx, hy, z - sz * 0.35 + k * 0.42), 0.035, 0.05, iron, c, verts=8, rot=(math.radians(90), 0, 0)))


def pitched(g, p, c, mat, loc, size, pitch=32, gable=None, axis="x", tiles=False):
    """Closed gable with overhang, ridge, and slate tiles. No stair-step ends.

    loc.z is eave height. Roof planes overshoot the walls like the citadel.
    A single low gable slab sits under the triangle so the end is not a tent.
    """
    x, y, z = loc
    sx, sy, sz = size
    rad = math.radians(pitch)
    fill = gable or mat
    thick = max(sz, 0.16)
    if axis == "y":
        half = sx * 0.5
        rise = math.tan(rad) * half
        span = half / max(math.cos(rad), 0.2)
        p.append(g.cube("west", (x - half * 0.5, y, z + rise * 0.5), (span, sy * 1.04, thick), mat, c, rot=(0, -rad, 0)))
        p.append(g.cube("east", (x + half * 0.5, y, z + rise * 0.5), (span, sy * 1.04, thick), mat, c, rot=(0, rad, 0)))
        p.append(g.cube("ridge", (x, y, z + rise + 0.06), (0.2, sy * 0.98, 0.16), mat, c))
        p.append(g.cube("gable_s", (x, y - sy * 0.49, z + rise * 0.32), (sx * 0.72, 0.26, rise * 0.62), fill, c))
        p.append(g.cube("gable_n", (x, y + sy * 0.49, z + rise * 0.32), (sx * 0.72, 0.26, rise * 0.62), fill, c))
        if tiles:
            cols, rows = 6, 8
            for side in (-1.0, 1.0):
                for col in range(cols):
                    for row in range(rows):
                        t = (row + 0.5) / rows
                        along = y + (t - 0.5) * sy * 0.92
                        out = (0.35 + col * 0.42)
                        p.append(
                            g.cube(
                                "tile",
                                (x + side * out, along, z + 0.08 + (half - out) * math.tan(rad)),
                                (0.44, sy / rows + 0.04, 0.05),
                                mat,
                                c,
                                rot=(0, side * rad, 0),
                            )
                        )
        return
    half = sy * 0.5
    rise = math.tan(rad) * half
    span = half / max(math.cos(rad), 0.2)
    p.append(g.cube("south", (x, y - half * 0.5, z + rise * 0.5), (sx * 1.04, span, thick), mat, c, rot=(rad, 0, 0)))
    p.append(g.cube("north", (x, y + half * 0.5, z + rise * 0.5), (sx * 1.04, span, thick), mat, c, rot=(-rad, 0, 0)))
    p.append(g.cube("ridge", (x, y, z + rise + 0.06), (sx * 0.98, 0.2, 0.16), mat, c))
    p.append(g.cube("gable_e", (x + sx * 0.49, y, z + rise * 0.32), (0.26, sy * 0.72, rise * 0.62), fill, c))
    p.append(g.cube("gable_w", (x - sx * 0.49, y, z + rise * 0.32), (0.26, sy * 0.72, rise * 0.62), fill, c))
    if tiles:
        cols, rows = 8, 4
        for side, srot in ((-1.0, rad), (1.0, -rad)):
            for col in range(cols):
                along = x + (col - (cols - 1) * 0.5) * (sx * 0.9 / cols)
                for row in range(rows):
                    out = 0.45 + row * (half * 0.78 / rows)
                    p.append(
                        g.cube(
                            "tile",
                            (along, y + side * out, z + 0.08 + (half - out) * math.tan(rad)),
                            (sx / cols + 0.06, 0.52, 0.05),
                            mat,
                            c,
                            rot=(srot, 0, 0),
                        )
                    )
