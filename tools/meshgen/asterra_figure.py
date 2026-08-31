"""Dense humanoid construction language. Shared joints only — never a role kit."""
from __future__ import annotations

import math


def legs(g, p, c, mat, s=1.0, spread=0.12, boot=None, boot_s=(0.18, 0.32, 0.12)):
    boot = boot or mat
    p.append(g.cyl("thigh_l", (-spread * s, 0, 0.64 * s), 0.09 * s, 0.44 * s, mat, c, verts=14))
    p.append(g.cyl("thigh_r", (spread * s, 0, 0.64 * s), 0.09 * s, 0.44 * s, mat, c, verts=14))
    p.append(g.uv_sphere("knee_l", (-spread * s, 0.02 * s, 0.44 * s), 0.078 * s, mat, c, segs=12, rings=8))
    p.append(g.uv_sphere("knee_r", (spread * s, 0.02 * s, 0.44 * s), 0.078 * s, mat, c, segs=12, rings=8))
    p.append(g.cyl("calf_l", (-spread * s, 0.03 * s, 0.28 * s), 0.072 * s, 0.36 * s, mat, c, verts=14))
    p.append(g.cyl("calf_r", (spread * s, 0.03 * s, 0.28 * s), 0.072 * s, 0.36 * s, mat, c, verts=14))
    p.append(g.cube("boot_l", (-spread * s, 0.09 * s, 0.07 * s), (boot_s[0] * s, boot_s[1] * s, boot_s[2] * s), boot, c))
    p.append(g.cube("boot_r", (spread * s, 0.09 * s, 0.07 * s), (boot_s[0] * s, boot_s[1] * s, boot_s[2] * s), boot, c))
    p.append(g.cube("toe_l", (-spread * s, 0.22 * s, 0.05 * s), (boot_s[0] * 0.7 * s, 0.12 * s, 0.06 * s), boot, c))
    p.append(g.cube("toe_r", (spread * s, 0.22 * s, 0.05 * s), (boot_s[0] * 0.7 * s, 0.12 * s, 0.06 * s), boot, c))


def arms(g, p, c, upper, lower, hands, s=1.0, drop=0.0):
    z_sh = (1.42 - drop) * s
    p.append(g.uv_sphere("sh_l", (-0.26 * s, 0, z_sh), 0.11 * s, upper, c, segs=12, rings=8))
    p.append(g.uv_sphere("sh_r", (0.26 * s, 0, z_sh), 0.11 * s, upper, c, segs=12, rings=8))
    p.append(g.cyl("uarm_l", (-0.36 * s, 0, (1.28 - drop) * s), 0.072 * s, 0.36 * s, upper, c, verts=14, rot=(0, math.radians(16), 0)))
    p.append(g.cyl("uarm_r", (0.36 * s, 0, (1.28 - drop) * s), 0.072 * s, 0.36 * s, upper, c, verts=14, rot=(0, math.radians(-16), 0)))
    p.append(g.uv_sphere("el_l", (-0.44 * s, 0.03 * s, (1.12 - drop) * s), 0.058 * s, upper, c, segs=10, rings=8))
    p.append(g.uv_sphere("el_r", (0.44 * s, 0.03 * s, (1.12 - drop) * s), 0.058 * s, upper, c, segs=10, rings=8))
    p.append(g.cyl("larm_l", (-0.48 * s, 0.05 * s, (1.0 - drop) * s), 0.06 * s, 0.32 * s, lower, c, verts=14, rot=(0, math.radians(10), 0)))
    p.append(g.cyl("larm_r", (0.48 * s, 0.05 * s, (1.0 - drop) * s), 0.06 * s, 0.32 * s, lower, c, verts=14, rot=(0, math.radians(-10), 0)))
    hx, hy, hz = -0.54 * s, 0.08 * s, (0.82 - drop) * s
    p.append(g.uv_sphere("palm_l", (hx, hy, hz), 0.055 * s, hands, c, segs=10, rings=6))
    p.append(g.uv_sphere("palm_r", (-hx, hy, hz), 0.055 * s, hands, c, segs=10, rings=6))
    for i, off in enumerate((-0.035, -0.012, 0.012, 0.035)):
        p.append(g.cube(f"fl{i}", (hx + off * s, hy + 0.07 * s, hz), (0.028 * s, 0.09 * s, 0.028 * s), hands, c))
        p.append(g.cube(f"fr{i}", (-hx + off * s, hy + 0.07 * s, hz), (0.028 * s, 0.09 * s, 0.028 * s), hands, c))


def head(g, p, c, m, s=1.0, z=1.66, hair=None, hair_mat=None):
    p.append(g.cyl("neck", (0, 0, (z - 0.16) * s), 0.072 * s, 0.16 * s, m.skin, c, verts=12))
    p.append(g.uv_sphere("skull", (0, 0.02 * s, z * s), 0.135 * s, m.skin, c, segs=18, rings=14))
    p.append(g.cube("nose", (0, -0.125 * s, (z - 0.015) * s), (0.04 * s, 0.065 * s, 0.05 * s), m.skin, c))
    p.append(g.cube("brow", (0, -0.1 * s, (z + 0.055) * s), (0.17 * s, 0.035 * s, 0.028 * s), m.skin, c))
    p.append(g.uv_sphere("eye_l", (-0.048 * s, -0.108 * s, (z + 0.02) * s), 0.018 * s, m.slate, c, segs=8, rings=6))
    p.append(g.uv_sphere("eye_r", (0.048 * s, -0.108 * s, (z + 0.02) * s), 0.018 * s, m.slate, c, segs=8, rings=6))
    p.append(g.cube("jaw", (0, -0.04 * s, (z - 0.09) * s), (0.125 * s, 0.1 * s, 0.07 * s), m.skin, c))
    p.append(g.cube("mouth", (0, -0.11 * s, (z - 0.055) * s), (0.07 * s, 0.025 * s, 0.02 * s), m.leather, c))
    p.append(g.uv_sphere("ear_l", (-0.13 * s, 0.02 * s, z * s), 0.035 * s, m.skin, c, segs=8, rings=6))
    p.append(g.uv_sphere("ear_r", (0.13 * s, 0.02 * s, z * s), 0.035 * s, m.skin, c, segs=8, rings=6))
    hm = hair_mat or getattr(m, "leather", m.skin)
    if hair == "crop":
        p.append(g.uv_sphere("hair", (0, 0.04 * s, (z + 0.06) * s), 0.14 * s, hm, c, segs=12, rings=8))
    elif hair == "long":
        p.append(g.uv_sphere("hair", (0, 0.05 * s, (z + 0.05) * s), 0.145 * s, hm, c, segs=12, rings=8))
        p.append(g.cube("lock", (0, 0.16 * s, (z - 0.08) * s), (0.16 * s, 0.08 * s, 0.28 * s), hm, c))
    elif hair == "beard":
        p.append(g.ico("beard", (0, -0.08 * s, (z - 0.12) * s), 0.1 * s, hm, c, subdiv=1, scale=(1.1, 0.7, 0.85)))
    elif hair == "wild":
        p.append(g.ico("mane", (0, 0.06 * s, (z + 0.08) * s), 0.18 * s, hm, c, subdiv=1, scale=(1.15, 1.05, 0.7)))
