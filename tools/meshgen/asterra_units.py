"""Fully unique unit meshes. No shared role kit, no hat-swap infantry."""
from __future__ import annotations

import math

from mathutils import Vector

from asterra_detail import finish
from asterra_figure import arms as _arms
from asterra_figure import head as _head
from asterra_figure import legs as _legs
from asterra_unique_humans import UNIQUE_HUMANS


# --- Uncrowned ---

def veiled_apprentice(g, m, c):
    p = []
    p.append(g.cyl("hips", (0, 0, 0.94), 0.15, 0.22, m.cloth_purple, c, verts=10))
    p.append(g.cyl("robe", (0, 0.04, 1.15), 0.28, 1.15, m.cloth_purple, c, verts=12))
    p.append(g.cube("panel", (0, -0.22, 1.05), (0.22, 0.05, 0.95), m.steel, c))
    _head(g, p, c, m)
    p.append(g.cone("hood", (0, 0.06, 1.92), 0.24, 0.42, m.cloth_purple, c, verts=8))
    _arms(g, p, c, m.cloth_purple, m.steel, m.skin)
    p.append(g.cyl("staff", (0.58, -0.04, 1.2), 0.03, 2.15, m.steel, c, verts=8))
    p.append(g.uv_sphere("orb", (0.58, -0.04, 2.28), 0.11, m.glass, c, segs=10, rings=6))
    p.append(g.cube("bracer_l", (-0.48, 0.05, 1.05), (0.14, 0.12, 0.16), m.steel, c))
    p.append(g.cube("bracer_r", (0.48, 0.05, 1.05), (0.14, 0.12, 0.16), m.steel, c))
    p.append(g.cube("boot_l", (-0.12, 0.08, 0.08), (0.16, 0.28, 0.12), m.steel, c))
    p.append(g.cube("boot_r", (0.12, 0.08, 0.08), (0.16, 0.28, 0.12), m.steel, c))
    return finish(g, "unit_veiled_apprentice", p, c, 0.012)


def veiled_builder(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0.02, 1.22), 0.2, 0.5, m.steel, c, verts=10))
    _legs(g, p, c, m.leather, boot=m.steel)
    _arms(g, p, c, m.steel, m.skin, m.skin)
    _head(g, p, c, m)
    p.append(g.cube("goggles", (0, -0.12, 1.7), (0.22, 0.08, 0.08), m.glass, c))
    p.append(g.cube("pack", (0, 0.32, 1.28), (0.55, 0.22, 0.45), m.steel, c))
    p.append(g.cube("pane", (0.12, 0.42, 1.38), (0.28, 0.04, 0.32), m.glass, c))
    p.append(g.cyl("torch", (0.7, 0, 1.05), 0.03, 0.9, m.steel, c, verts=8, rot=(0, math.radians(90), 0)))
    p.append(g.cube("bit", (1.12, 0, 1.05), (0.16, 0.1, 0.16), m.iron, c))
    return finish(g, "unit_veiled_builder", p, c, 0.012)


def veiled_rune_caster(g, m, c):
    p = []
    p.append(g.cyl("robe", (0, 0.02, 1.1), 0.3, 1.25, m.cloth_purple, c, verts=12))
    _head(g, p, c, m, z=1.68)
    p.append(g.cone("hat", (0, 0, 2.05), 0.18, 0.55, m.steel, c, verts=6))
    p.append(g.uv_sphere("gem", (0, 0, 2.38), 0.08, m.crystal, c, segs=8, rings=6))
    _arms(g, p, c, m.cloth_purple, m.cloth_purple, m.skin)
    p.append(g.cyl("staff", (0.55, -0.06, 1.2), 0.04, 2.0, m.steel, c, verts=8))
    p.append(g.cube("rune0", (0.55, -0.06, 2.25), (0.22, 0.04, 0.22), m.glass, c, rot=(0, 0, math.radians(45))))
    for i in range(5):
        ang = i * (2 * math.pi / 5)
        p.append(g.cube(f"ring{i}", (math.cos(ang) * 0.55, math.sin(ang) * 0.55, 1.45), (0.08, 0.08, 0.08), m.crystal, c))
    p.append(g.cube("boot_l", (-0.12, 0.06, 0.08), (0.16, 0.26, 0.12), m.steel, c))
    p.append(g.cube("boot_r", (0.12, 0.06, 0.08), (0.16, 0.26, 0.12), m.steel, c))
    return finish(g, "unit_veiled_rune_caster", p, c, 0.012)


def veiled_elemental(g, m, c):
    p = []
    p.append(g.uv_sphere("core", (0, 0, 1.15), 0.38, m.crystal, c, segs=12, rings=8))
    p.append(g.uv_sphere("core2", (0, 0, 1.55), 0.22, m.glass, c, segs=10, rings=6))
    for i in range(10):
        ang = i * (math.pi / 5)
        z = 0.45 + (i % 4) * 0.35
        p.append(g.cube(f"sh{i}", (math.cos(ang) * (0.45 + i % 3 * 0.08), math.sin(ang) * 0.4, z), (0.12, 0.04, 0.55), m.glass, c, rot=(0, math.radians(20), ang)))
        p.append(g.cone(f"sp{i}", (math.cos(ang) * 0.2, math.sin(ang) * 0.2, 2.05), 0.08, 0.45, m.steel, c, verts=5))
    p.append(g.cyl("base", (0, 0, 0.22), 0.35, 0.18, m.steel, c, verts=8))
    return finish(g, "unit_veiled_elemental", p, c, 0.02)


def veiled_golem(g, m, c):
    p = []
    p.append(g.cube("pelvis", (0, 0, 0.65), (0.85, 0.5, 0.55), m.dark_stone, c))
    p.append(g.cube("chest", (0, 0, 1.5), (1.15, 0.62, 1.05), m.steel, c))
    p.append(g.cube("glass", (0, -0.28, 1.52), (0.55, 0.12, 0.55), m.glass, c))
    p.append(g.cube("head", (0, 0.05, 2.22), (0.48, 0.42, 0.48), m.dark_stone, c))
    p.append(g.uv_sphere("eye", (0, -0.18, 2.24), 0.1, m.crystal, c, segs=8, rings=6))
    p.append(g.cube("al", (-0.78, 0, 1.35), (0.32, 0.32, 1.2), m.steel, c))
    p.append(g.cube("ar", (0.78, 0, 1.35), (0.32, 0.32, 1.2), m.steel, c))
    p.append(g.cube("fist_l", (-0.78, 0.05, 0.65), (0.38, 0.38, 0.35), m.dark_stone, c))
    p.append(g.cube("fist_r", (0.78, 0.05, 0.65), (0.38, 0.38, 0.35), m.dark_stone, c))
    p.append(g.cube("ll", (-0.32, 0, 0.28), (0.32, 0.36, 0.55), m.dark_stone, c))
    p.append(g.cube("lr", (0.32, 0, 0.28), (0.32, 0.36, 0.55), m.dark_stone, c))
    p.append(g.cyl("pipe", (0.35, 0.28, 1.85), 0.06, 0.7, m.steel, c, verts=6, rot=(math.radians(40), 0, 0)))
    return finish(g, "unit_veiled_golem", p, c, 0.03)


def veiled_priest_guard(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0.02, 1.28), 0.22, 0.52, m.steel, c, verts=10))
    p.append(g.cube("tabard", (0, -0.16, 1.12), (0.28, 0.05, 0.85), m.cloth_purple, c))
    _legs(g, p, c, m.steel)
    _arms(g, p, c, m.steel, m.steel, m.skin)
    _head(g, p, c, m)
    p.append(g.cyl("helm", (0, 0, 1.82), 0.16, 0.16, m.steel, c, verts=10))
    p.append(g.cube("visor", (0, -0.12, 1.72), (0.2, 0.06, 0.08), m.glass, c))
    p.append(g.cube("pl", (-0.4, 0, 1.45), (0.24, 0.24, 0.18), m.steel, c))
    p.append(g.cube("pr", (0.4, 0, 1.45), (0.24, 0.24, 0.18), m.steel, c))
    p.append(g.cyl("pole", (0.62, -0.05, 1.3), 0.03, 2.3, m.steel, c, verts=8))
    p.append(g.cube("blade", (0.62, -0.05, 2.4), (0.18, 0.04, 0.45), m.glass, c))
    p.append(g.cube("kite", (-0.62, 0.16, 1.12), (0.55, 0.08, 0.85), m.steel, c, rot=(math.radians(8), 0, math.radians(12))))
    return finish(g, "unit_veiled_priest_guard", p, c, 0.012)


def veiled_shadow(g, m, c):
    """Glass-steel wraith hound, not a horse."""
    p = []
    p.append(g.cyl("body", (0, 0, 0.72), 0.28, 1.35, m.steel, c, verts=10, rot=(0, math.radians(90), 0)))
    p.append(g.uv_sphere("chest", (0.55, 0, 0.78), 0.32, m.dark_stone, c, segs=10, rings=6))
    p.append(g.cone("snout", (0.95, 0, 0.72), 0.12, 0.35, m.glass, c, verts=6, rot=(0, math.radians(90), 0)))
    p.append(g.uv_sphere("eye_l", (0.72, 0.12, 0.88), 0.05, m.crystal, c, segs=6, rings=4))
    p.append(g.uv_sphere("eye_r", (0.72, -0.12, 0.88), 0.05, m.crystal, c, segs=6, rings=4))
    for i, (x, y) in enumerate(((0.4, 0.18), (0.4, -0.18), (-0.4, 0.18), (-0.4, -0.18))):
        p.append(g.cyl(f"leg{i}", (x, y, 0.38), 0.07, 0.7, m.steel, c, verts=6))
        p.append(g.cube(f"paw{i}", (x, y + 0.08, 0.08), (0.12, 0.18, 0.08), m.glass, c))
    p.append(g.cube("ridge", (0, 0, 1.02), (1.1, 0.08, 0.12), m.glass, c))
    p.append(g.cone("tail", (-0.85, 0, 0.85), 0.1, 0.55, m.steel, c, verts=6, rot=(0, math.radians(-70), 0)))
    return finish(g, "unit_veiled_shadow", p, c, 0.02)


def veiled_assassin(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0.02, 1.22), 0.16, 0.42, m.cloth_purple, c, verts=10))
    _legs(g, p, c, m.leather, spread=0.1)
    _arms(g, p, c, m.cloth_purple, m.skin, m.skin)
    _head(g, p, c, m)
    p.append(g.cube("mask", (0, -0.12, 1.66), (0.16, 0.05, 0.12), m.steel, c))
    p.append(g.cube("hood", (0, 0.08, 1.78), (0.28, 0.18, 0.16), m.cloth_purple, c))
    p.append(g.cube("d1", (0.5, -0.1, 0.95), (0.04, 0.02, 0.42), m.glass, c, rot=(0, math.radians(25), 0)))
    p.append(g.cube("d2", (-0.5, 0.08, 0.92), (0.04, 0.02, 0.38), m.glass, c, rot=(0, math.radians(-20), 0)))
    p.append(g.cube("cape", (0, 0.2, 1.2), (0.32, 0.04, 0.85), m.dark_stone, c))
    return finish(g, "unit_veiled_assassin", p, c, 0.012)


def veiled_massed(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0, 1.18), 0.18, 0.4, m.cloth_purple, c, verts=10))
    _legs(g, p, c, m.leather)
    _arms(g, p, c, m.cloth_purple, m.skin, m.skin)
    _head(g, p, c, m)
    p.append(g.cube("wrap", (0, 0.12, 1.7), (0.26, 0.16, 0.1), m.cloth_purple, c))
    p.append(g.cyl("club", (0.58, -0.04, 1.05), 0.04, 1.15, m.steel, c, verts=8))
    p.append(g.cube("headc", (0.58, -0.04, 1.65), (0.14, 0.14, 0.18), m.dark_stone, c))
    return finish(g, "unit_veiled_massed", p, c, 0.012)


def veiled_souling(g, m, c):
    p = []
    p.append(g.taper("body", (0, 0, 1.15), 0.22, 0.08, 1.7, m.glass, c, verts=8))
    p.append(g.uv_sphere("head", (0, 0, 2.05), 0.16, m.glass, c, segs=10, rings=6))
    p.append(g.uv_sphere("heart", (0, 0, 1.2), 0.12, m.crystal, c, segs=8, rings=6))
    for i in range(6):
        ang = i * math.pi / 3
        p.append(g.cube(f"tend{i}", (math.cos(ang) * 0.25, math.sin(ang) * 0.25, 0.55), (0.06, 0.06, 0.7), m.steel, c))
    return finish(g, "unit_veiled_souling", p, c, 0.015)


def veiled_heir(g, m, c):
    p = []
    p.append(g.cyl("robe", (0, 0.03, 1.15), 0.32, 1.3, m.cloth_purple, c, verts=12))
    p.append(g.cube("steel_cape", (0, 0.22, 1.25), (0.5, 0.06, 1.05), m.steel, c))
    _head(g, p, c, m, z=1.7)
    p.append(g.cyl("crown", (0, 0, 1.92), 0.16, 0.1, m.steel, c, verts=10))
    for i in range(6):
        ang = i * math.pi / 3
        p.append(g.cone(f"spk{i}", (math.sin(ang) * 0.14, -math.cos(ang) * 0.14, 2.05), 0.04, 0.22, m.glass, c, verts=5))
    _arms(g, p, c, m.cloth_purple, m.steel, m.skin)
    p.append(g.cyl("scepter", (0.58, -0.04, 1.2), 0.035, 1.85, m.steel, c, verts=8))
    p.append(g.uv_sphere("gem", (0.58, -0.04, 2.15), 0.12, m.crystal, c, segs=10, rings=6))
    p.append(g.cube("boot_l", (-0.12, 0.08, 0.08), (0.16, 0.28, 0.12), m.steel, c))
    p.append(g.cube("boot_r", (0.12, 0.08, 0.08), (0.16, 0.28, 0.12), m.steel, c))
    return finish(g, "unit_veiled_heir", p, c, 0.012)


def veiled_colossus(g, m, c):
    p = []
    p.append(g.cube("hips", (0, 0, 1.1), (1.4, 0.8, 0.9), m.dark_stone, c))
    p.append(g.cube("chest", (0, 0, 2.5), (1.8, 0.95, 1.7), m.steel, c))
    p.append(g.cube("glass", (0, -0.42, 2.55), (0.8, 0.16, 0.8), m.glass, c))
    p.append(g.cube("head", (0, 0.1, 3.6), (0.7, 0.6, 0.7), m.dark_stone, c))
    p.append(g.uv_sphere("eye", (0, -0.22, 3.62), 0.14, m.crystal, c, segs=8, rings=6))
    p.append(g.cube("al", (-1.2, 0, 2.2), (0.5, 0.5, 2.0), m.steel, c))
    p.append(g.cube("ar", (1.2, 0, 2.2), (0.5, 0.5, 2.0), m.steel, c))
    p.append(g.cube("ll", (-0.5, 0, 0.45), (0.5, 0.55, 0.9), m.dark_stone, c))
    p.append(g.cube("lr", (0.5, 0, 0.45), (0.5, 0.55, 0.9), m.dark_stone, c))
    return finish(g, "unit_veiled_colossus", p, c, 0.04)


def veiled_thorn_speaker(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0, 1.2), 0.2, 0.5, m.bark, c, verts=10))
    _legs(g, p, c, m.bark)
    _arms(g, p, c, m.bark, m.bark, m.skin)
    _head(g, p, c, m)
    p.append(g.ico("crown", (0, 0.02, 1.92), 0.28, m.leaf_d, c, subdiv=1, scale=(1.2, 1.1, 0.55)))
    p.append(g.cyl("staff", (0.55, -0.04, 1.15), 0.04, 1.9, m.bark, c, verts=8))
    p.append(g.ico("bloom", (0.55, -0.04, 2.15), 0.16, m.cloth_purple, c, subdiv=1))
    return finish(g, "unit_veiled_thorn_speaker", p, c, 0.012)


def veiled_night_abbot(g, m, c):
    p = []
    p.append(g.cyl("robe", (0, 0.04, 1.1), 0.36, 1.35, m.cloth_purple, c, verts=12))
    p.append(g.cube("stole", (0, -0.28, 1.2), (0.18, 0.05, 1.0), m.steel, c))
    _head(g, p, c, m)
    p.append(g.cube("cowl", (0, 0.1, 1.85), (0.32, 0.22, 0.2), m.cloth_purple, c))
    _arms(g, p, c, m.cloth_purple, m.cloth_purple, m.skin)
    p.append(g.cyl("book", (0.35, -0.25, 1.05), 0.08, 0.28, m.leather, c, verts=8, rot=(math.radians(90), 0, 0)))
    p.append(g.cube("boot_l", (-0.14, 0.08, 0.08), (0.16, 0.26, 0.12), m.leather, c))
    p.append(g.cube("boot_r", (0.14, 0.08, 0.08), (0.16, 0.26, 0.12), m.leather, c))
    return finish(g, "unit_veiled_night_abbot", p, c, 0.012)


def veiled_first_heretic(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0, 1.25), 0.21, 0.5, m.dark_stone, c, verts=10))
    _legs(g, p, c, m.steel)
    _arms(g, p, c, m.dark_stone, m.steel, m.skin)
    _head(g, p, c, m)
    p.append(g.cone("helm", (0, 0, 2.05), 0.2, 0.55, m.steel, c, verts=6))
    p.append(g.cube("blade", (0.6, -0.04, 1.45), (0.06, 0.02, 1.05), m.glass, c))
    p.append(g.cube("guard", (0.6, -0.04, 1.15), (0.22, 0.05, 0.05), m.steel, c))
    return finish(g, "unit_veiled_first_heretic", p, c, 0.012)


def veiled_dark_spy(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0, 1.2), 0.17, 0.42, m.dark_stone, c, verts=10))
    _legs(g, p, c, m.leather)
    _arms(g, p, c, m.dark_stone, m.skin, m.skin)
    _head(g, p, c, m)
    p.append(g.cube("hood", (0, 0.06, 1.8), (0.26, 0.2, 0.18), m.cloth_purple, c))
    p.append(g.cyl("scope", (0.15, -0.28, 1.68), 0.04, 0.28, m.steel, c, verts=8, rot=(math.radians(90), 0, 0)))
    p.append(g.cube("glass", (0.15, -0.42, 1.68), (0.08, 0.04, 0.08), m.glass, c))
    return finish(g, "unit_veiled_dark_spy", p, c, 0.012)


def veiled_shade(g, m, c):
    p = []
    p.append(g.taper("body", (0, 0, 1.2), 0.18, 0.02, 1.9, m.glass, c, verts=8))
    p.append(g.uv_sphere("head", (0, 0.05, 2.15), 0.14, m.glass, c, segs=10, rings=6))
    p.append(g.cube("wl", (-0.55, 0.08, 1.5), (0.7, 0.04, 0.45), m.steel, c, rot=(0, 0, math.radians(18))))
    p.append(g.cube("wr", (0.55, 0.08, 1.5), (0.7, 0.04, 0.45), m.steel, c, rot=(0, 0, math.radians(-18))))
    p.append(g.uv_sphere("core", (0, 0, 1.15), 0.1, m.crystal, c, segs=8, rings=6))
    return finish(g, "unit_veiled_shade", p, c, 0.015)


# --- Mundor medieval English ---

def royal_builder(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0.02, 1.24), 0.2, 0.48, m.leather, c, verts=10))
    p.append(g.cube("apron", (0, -0.14, 1.05), (0.32, 0.05, 0.7), m.cloth, c))
    _legs(g, p, c, m.cloth, boot=m.leather)
    _arms(g, p, c, m.cloth, m.skin, m.skin)
    _head(g, p, c, m)
    p.append(g.cyl("cap", (0, 0, 1.8), 0.16, 0.1, m.cloth, c, verts=10))
    p.append(g.cube("sack", (0, 0.28, 1.2), (0.3, 0.2, 0.28), m.leather, c))
    p.append(g.cyl("mallet", (0.72, 0, 0.95), 0.035, 0.85, m.wood, c, verts=8, rot=(0, math.radians(90), 0)))
    p.append(g.cube("headh", (1.12, 0, 0.95), (0.2, 0.14, 0.32), m.iron, c))
    return finish(g, "unit_royal_builder", p, c, 0.012)


def royal_legion(g, m, c):
    p = []
    p.append(g.cyl("mail", (0, 0.02, 1.26), 0.22, 0.5, m.iron, c, verts=12))
    p.append(g.cube("tabard", (0, -0.16, 1.12), (0.26, 0.05, 0.82), m.cloth, c))
    _legs(g, p, c, m.iron, boot=m.leather)
    _arms(g, p, c, m.iron, m.iron, m.leather)
    _head(g, p, c, m)
    p.append(g.cyl("coif", (0, 0, 1.78), 0.15, 0.16, m.iron, c, verts=10))
    p.append(g.cyl("nasal", (0, -0.13, 1.68), 0.02, 0.14, m.iron, c, verts=6))
    p.append(g.cyl("spear", (0.62, -0.04, 1.25), 0.028, 2.2, m.wood, c, verts=8))
    p.append(g.cone("tip", (0.62, -0.04, 2.38), 0.05, 0.22, m.iron, c, verts=6))
    p.append(g.cube("kite", (-0.62, 0.14, 1.1), (0.5, 0.08, 0.95), m.wood, c, rot=(math.radians(10), 0, math.radians(8))))
    p.append(g.cube("boss", (-0.58, 0.2, 1.15), (0.12, 0.06, 0.12), m.iron, c))
    return finish(g, "unit_royal_legion", p, c, 0.012)


def royal_guard(g, m, c):
    p = []
    p.append(g.cyl("plate", (0, 0.02, 1.28), 0.23, 0.52, m.iron, c, verts=12))
    p.append(g.cube("surcoat", (0, -0.18, 1.1), (0.3, 0.05, 0.9), m.cloth_deep, c))
    _legs(g, p, c, m.iron)
    _arms(g, p, c, m.iron, m.iron, m.iron)
    _head(g, p, c, m)
    p.append(g.cyl("greathelm", (0, 0, 1.86), 0.16, 0.22, m.iron, c, verts=10))
    p.append(g.cube("slot", (0, -0.14, 1.78), (0.16, 0.04, 0.04), m.slate, c))
    p.append(g.cube("pl", (-0.42, 0, 1.48), (0.26, 0.26, 0.2), m.iron, c))
    p.append(g.cube("pr", (0.42, 0, 1.48), (0.26, 0.26, 0.2), m.iron, c))
    p.append(g.cube("sword", (0.58, -0.04, 1.4), (0.05, 0.02, 0.95), m.iron, c))
    p.append(g.cube("heater", (-0.65, 0.12, 1.15), (0.48, 0.08, 0.7), m.wood, c))
    return finish(g, "unit_royal_guard", p, c, 0.012)


def royal_longbow(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0.02, 1.22), 0.19, 0.46, m.leather, c, verts=10))
    _legs(g, p, c, m.cloth, boot=m.leather)
    _arms(g, p, c, m.leather, m.skin, m.skin)
    _head(g, p, c, m)
    p.append(g.cyl("coif", (0, 0, 1.76), 0.14, 0.1, m.iron, c, verts=10))
    p.append(g.cyl("bow", (0.08, -0.48, 1.15), 0.028, 1.85, m.wood, c, verts=8))
    p.append(g.cyl("bow2", (0.08, -0.42, 1.15), 0.02, 1.7, m.wood, c, verts=8, rot=(0, 0, math.radians(8))))
    p.append(g.cube("quiver", (-0.28, 0.22, 1.15), (0.1, 0.1, 0.65), m.leather, c))
    p.append(g.cyl("arrow", (-0.28, 0.22, 1.55), 0.015, 0.7, m.wood, c, verts=6))
    p.append(g.cube("bracer", (-0.46, 0.05, 1.05), (0.12, 0.1, 0.14), m.leather, c))
    return finish(g, "unit_royal_longbow", p, c, 0.012)


def royal_commander(g, m, c):
    p = []
    p.append(g.cyl("body", (0, 0.08, 1.05), 0.42, 1.25, m.leather, c, verts=12, rot=(0, math.radians(90), 0)))
    p.append(g.uv_sphere("chest", (0.55, 0.06, 1.12), 0.36, m.leather, c, segs=10, rings=6))
    p.append(g.cyl("neck", (0.72, 0.04, 1.48), 0.14, 0.5, m.leather, c, verts=8, rot=(math.radians(-28), 0, 0)))
    p.append(g.uv_sphere("headh", (0.85, -0.12, 1.78), 0.16, m.leather, c, segs=8, rings=6))
    for x, y in ((0.4, 0.22), (0.4, -0.14), (-0.4, 0.22), (-0.4, -0.14)):
        p.append(g.cyl(f"leg{x}{y}", (x, y, 0.5), 0.09, 0.95, m.leather, c, verts=8))
    p.append(g.cube("caparison", (0, 0.05, 1.28), (1.15, 0.7, 0.08), m.cloth_deep, c))
    p.append(g.cube("saddle", (0, 0.05, 1.42), (0.5, 0.4, 0.14), m.leather, c))
    p.append(g.cyl("rider", (0.05, 0.02, 1.85), 0.16, 0.4, m.iron, c, verts=10))
    p.append(g.uv_sphere("rhead", (0.05, 0.04, 2.18), 0.12, m.skin, c, segs=8, rings=6))
    p.append(g.cyl("helm", (0.05, 0.04, 2.3), 0.13, 0.12, m.iron, c, verts=8))
    p.append(g.cyl("lance", (0.55, -0.15, 2.05), 0.03, 2.4, m.wood, c, verts=8, rot=(0, math.radians(55), 0)))
    return finish(g, "unit_royal_commander", p, c, 0.02)


def royal_onager(g, m, c):
    p = []
    p.append(g.cube("bed", (0, 0, 0.55), (2.15, 1.35, 0.28), m.wood, c))
    p.append(g.cube("side_l", (0, 0.62, 0.95), (1.9, 0.12, 0.85), m.wood, c))
    p.append(g.cube("side_r", (0, -0.62, 0.95), (1.9, 0.12, 0.85), m.wood, c))
    for x, y in ((0.75, 0.72), (0.75, -0.72), (-0.75, 0.72), (-0.75, -0.72)):
        p.append(g.cyl(f"w{x}{y}", (x, y, 0.35), 0.32, 0.14, m.wood, c, verts=12, rot=(math.radians(90), 0, 0)))
        p.append(g.cyl(f"hub{x}", (x, y, 0.35), 0.08, 0.18, m.iron, c, verts=8, rot=(math.radians(90), 0, 0)))
    p.append(g.cyl("arm", (0.2, 0, 1.25), 0.09, 1.85, m.wood, c, verts=8, rot=(0, math.radians(50), 0)))
    p.append(g.cube("sling", (0.95, 0, 1.85), (0.4, 0.35, 0.18), m.leather, c))
    p.append(g.uv_sphere("rock", (0.95, 0, 1.95), 0.18, m.slate, c, segs=8, rings=6))
    p.append(g.cyl("winch", (-0.55, 0, 0.95), 0.12, 1.15, m.wood, c, verts=10, rot=(math.radians(90), 0, 0)))
    return finish(g, "unit_royal_onager", p, c, 0.025)


def royal_king(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0.02, 1.28), 0.24, 0.52, m.cloth_deep, c, verts=12))
    p.append(g.cube("ermine", (0, -0.2, 1.05), (0.4, 0.06, 0.95), m.marble, c))
    _legs(g, p, c, m.cloth_deep, boot=m.leather)
    _arms(g, p, c, m.cloth_deep, m.cloth_deep, m.skin)
    _head(g, p, c, m)
    p.append(g.cyl("crown", (0, 0, 1.9), 0.16, 0.12, m.gold, c, verts=12))
    for i in range(5):
        ang = i * (2 * math.pi / 5)
        p.append(g.cube(f"fl{i}", (math.sin(ang) * 0.14, -math.cos(ang) * 0.14, 2.02), (0.05, 0.05, 0.18), m.gold, c))
    p.append(g.cyl("scepter", (0.58, -0.04, 1.2), 0.03, 1.7, m.gold, c, verts=8))
    p.append(g.uv_sphere("orb", (0.58, -0.04, 2.08), 0.1, m.crystal, c, segs=8, rings=6))
    return finish(g, "unit_royal_king", p, c, 0.012)


def royal_spy(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0.12, 1.05), 0.17, 0.42, m.cloth_deep, c, verts=10))
    _legs(g, p, c, m.leather, s=0.92, spread=0.16)
    _arms(g, p, c, m.cloth_deep, m.skin, m.skin, s=0.95, drop=0.08)
    _head(g, p, c, m, s=0.95, z=1.58)
    p.append(g.cone("hood", (0, 0.14, 1.78), 0.22, 0.38, m.cloth_deep, c, verts=8))
    p.append(g.cube("cloak", (0, 0.32, 1.05), (0.42, 0.08, 0.95), m.cloth, c))
    p.append(g.cyl("glass", (0.12, -0.28, 1.52), 0.04, 0.32, m.iron, c, verts=8, rot=(math.radians(90), 0, 0)))
    p.append(g.cube("d", (0.48, -0.06, 0.82), (0.04, 0.02, 0.32), m.iron, c))
    return finish(g, "unit_royal_spy", p, c, 0.012)


def royal_crown_eye(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0.02, 1.24), 0.2, 0.48, m.cloth_deep, c, verts=10))
    _legs(g, p, c, m.leather)
    _arms(g, p, c, m.cloth_deep, m.skin, m.skin)
    _head(g, p, c, m)
    p.append(g.cone("hood", (0, 0.06, 1.95), 0.24, 0.4, m.cloth_deep, c, verts=8))
    p.append(g.cyl("scope", (0.18, -0.38, 1.72), 0.05, 0.42, m.iron, c, verts=8, rot=(math.radians(90), 0, 0)))
    p.append(g.uv_sphere("lens", (0.18, -0.58, 1.72), 0.07, m.glass, c, segs=8, rings=6))
    p.append(g.cube("cloak", (0, 0.22, 1.12), (0.4, 0.06, 0.9), m.cloth, c))
    return finish(g, "unit_royal_crown_eye", p, c, 0.012)


def royal_pioneer(g, m, c):
    p = []
    p.append(g.cyl("hips", (0, 0, 0.94), 0.17, 0.22, m.leather, c, verts=10))
    p.append(g.cyl("torso", (0, 0.02, 1.24), 0.22, 0.5, m.iron, c, verts=12))
    _legs(g, p, c, m.leather, boot=m.iron)
    _arms(g, p, c, m.iron, m.skin, m.skin)
    _head(g, p, c, m)
    p.append(g.cyl("helm", (0, 0, 1.8), 0.155, 0.15, m.iron, c, verts=12))
    p.append(g.cyl("brim", (0, -0.02, 1.71), 0.22, 0.04, m.iron, c, verts=14))
    p.append(g.cube("pack", (0, 0.34, 1.22), (0.5, 0.28, 0.42), m.wood, c))
    p.append(g.cube("haft", (0.72, 0, 1.05), (0.05, 0.05, 1.15), m.wood, c))
    p.append(g.cube("blade", (0.72, 0.02, 0.48), (0.24, 0.05, 0.22), m.iron, c))
    return finish(g, "unit_royal_pioneer", p, c, 0.012)


def royal_legion_marshal(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0.02, 1.26), 0.23, 0.52, m.iron, c, verts=12))
    p.append(g.cube("tabard", (0, -0.16, 1.12), (0.22, 0.05, 0.8), m.cloth, c))
    _legs(g, p, c, m.iron, boot=m.iron)
    _arms(g, p, c, m.iron, m.iron, m.skin)
    _head(g, p, c, m)
    p.append(g.cyl("helm", (0, 0, 1.82), 0.16, 0.16, m.iron, c, verts=12))
    p.append(g.cube("crest", (0, -0.02, 1.98), (0.04, 0.22, 0.18), m.cloth, c))
    p.append(g.cube("sw", (0.62, -0.04, 1.38), (0.055, 0.03, 1.05), m.iron, c))
    p.append(g.cube("kite", (-0.7, 0.14, 1.12), (0.58, 0.08, 1.0), m.wood, c, rot=(math.radians(8), 0, math.radians(8))))
    p.append(g.cyl("banner", (-0.05, 0.28, 1.7), 0.035, 1.6, m.wood, c, verts=8))
    p.append(g.cube("flag", (0.22, 0.28, 2.35), (0.55, 0.04, 0.42), m.cloth, c))
    return finish(g, "unit_royal_legion_marshal", p, c, 0.012)


def royal_spymaster(g, m, c):
    p = []
    p.append(g.cyl("robe", (0, 0.04, 1.12), 0.28, 1.25, m.cloth_deep, c, verts=12))
    p.append(g.cube("inner", (0, -0.22, 1.1), (0.2, 0.05, 1.05), m.iron, c))
    _head(g, p, c, m)
    p.append(g.cone("hood", (0, 0.08, 1.98), 0.26, 0.48, m.cloth_deep, c, verts=8))
    _arms(g, p, c, m.cloth_deep, m.cloth_deep, m.skin)
    p.append(g.cube("dl", (-0.52, -0.08, 0.95), (0.04, 0.02, 0.38), m.iron, c))
    p.append(g.cube("dr", (0.52, -0.08, 0.95), (0.04, 0.02, 0.38), m.iron, c))
    p.append(g.cube("cloak", (0, 0.28, 1.15), (0.5, 0.08, 1.15), m.cloth, c))
    return finish(g, "unit_royal_spymaster", p, c, 0.012)


def royal_tomb_warden(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0.02, 1.26), 0.24, 0.54, m.iron, c, verts=12))
    p.append(g.cube("surcoat", (0, -0.16, 1.1), (0.24, 0.05, 0.85), m.cloth_deep, c))
    _legs(g, p, c, m.iron, boot=m.iron)
    _arms(g, p, c, m.iron, m.iron, m.iron)
    _head(g, p, c, m)
    p.append(g.cyl("helm", (0, 0, 1.84), 0.16, 0.18, m.iron, c, verts=12))
    p.append(g.cube("nasal", (0, -0.15, 1.68), (0.04, 0.03, 0.14), m.iron, c))
    p.append(g.cube("sw", (0.64, -0.04, 1.4), (0.055, 0.03, 1.1), m.iron, c))
    p.append(g.cube("heater", (-0.68, 0.12, 1.15), (0.52, 0.1, 0.85), m.iron, c))
    return finish(g, "unit_royal_tomb_warden", p, c, 0.012)


def royal_justiciar(g, m, c):
    p = []
    p.append(g.cyl("robe", (0, 0.04, 1.1), 0.3, 1.35, m.cloth_deep, c, verts=12))
    p.append(g.cube("chain", (0, -0.26, 1.28), (0.16, 0.05, 0.55), m.gold, c))
    _legs(g, p, c, m.leather)
    _arms(g, p, c, m.cloth_deep, m.skin, m.skin)
    _head(g, p, c, m)
    p.append(g.cyl("coif", (0, 0, 1.82), 0.16, 0.14, m.iron, c, verts=12))
    p.append(g.cyl("haft", (0.7, 0, 1.05), 0.035, 0.85, m.wood, c, verts=8, rot=(0, math.radians(90), 0)))
    p.append(g.cube("gavel", (1.1, 0, 1.05), (0.18, 0.14, 0.28), m.iron, c))
    return finish(g, "unit_royal_justiciar", p, c, 0.012)


# --- Outcast ice / wood ---

def outcast_villager(g, m, c):
    p = []
    p.append(g.cyl("fur", (0, 0.04, 1.2), 0.24, 0.55, m.leather, c, verts=10))
    p.append(g.cube("pelt", (0, 0.22, 1.2), (0.4, 0.08, 0.7), m.ice, c))
    _legs(g, p, c, m.leather, boot=m.fur if hasattr(m, "fur") else m.leather, boot_s=(0.2, 0.32, 0.14))
    _arms(g, p, c, m.leather, m.leather, m.skin)
    _head(g, p, c, m)
    p.append(g.cube("hood", (0, 0.06, 1.82), (0.3, 0.22, 0.18), m.leather, c))
    p.append(g.cyl("spear", (0.58, -0.04, 1.2), 0.03, 1.9, m.wood, c, verts=8))
    p.append(g.cone("ice_tip", (0.58, -0.04, 2.18), 0.05, 0.22, m.ice, c, verts=6))
    return finish(g, "unit_outcast_villager", p, c, 0.012)


def outcast_hobgoblin(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0.08, 1.05), 0.22, 0.42, m.leather, c, verts=10))
    _legs(g, p, c, m.leather, s=0.85, spread=0.16)
    _arms(g, p, c, m.leather, m.skin, m.skin, s=0.95, drop=0.12)
    p.append(g.uv_sphere("head", (0, 0.12, 1.42), 0.16, m.skin, c, segs=10, rings=6))
    p.append(g.cone("ear_l", (-0.14, 0.12, 1.55), 0.04, 0.16, m.skin, c, verts=5, rot=(0, 0, math.radians(-25))))
    p.append(g.cone("ear_r", (0.14, 0.12, 1.55), 0.04, 0.16, m.skin, c, verts=5, rot=(0, 0, math.radians(25))))
    p.append(g.cube("pack", (0, 0.32, 1.05), (0.4, 0.22, 0.35), m.wood, c))
    p.append(g.cyl("adze", (0.65, 0, 0.85), 0.03, 0.7, m.wood, c, verts=8, rot=(0, math.radians(90), 0)))
    p.append(g.cube("bit", (0.98, 0, 0.85), (0.14, 0.08, 0.22), m.ice, c))
    return finish(g, "unit_outcast_hobgoblin", p, c, 0.012)


def outcast_hunter(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0, 1.22), 0.19, 0.46, m.leather, c, verts=10))
    _legs(g, p, c, m.leather)
    _arms(g, p, c, m.leather, m.skin, m.skin)
    _head(g, p, c, m)
    p.append(g.ico("furhat", (0, 0.02, 1.86), 0.2, m.ice, c, subdiv=1, scale=(1.15, 1.05, 0.5)))
    p.append(g.cyl("bow", (0.05, -0.45, 1.1), 0.025, 1.55, m.wood, c, verts=8))
    p.append(g.cube("quiver", (-0.26, 0.22, 1.15), (0.1, 0.1, 0.55), m.bark, c))
    return finish(g, "unit_outcast_hunter", p, c, 0.012)


def outcast_ranger(g, m, c):
    p = []
    p.append(g.cyl("torso", (0, 0, 1.24), 0.2, 0.48, m.bark, c, verts=10))
    _legs(g, p, c, m.leather)
    _arms(g, p, c, m.bark, m.skin, m.skin)
    _head(g, p, c, m)
    p.append(g.cube("hood", (0, 0.08, 1.82), (0.28, 0.2, 0.16), m.leaf, c))
    p.append(g.cyl("longbow", (0.06, -0.5, 1.2), 0.03, 2.0, m.wood, c, verts=8))
    p.append(g.cube("ice_arrow", (-0.28, 0.22, 1.5), (0.04, 0.04, 0.55), m.ice, c))
    return finish(g, "unit_outcast_ranger", p, c, 0.012)


def outcast_beast_rider(g, m, c):
    """Ice wolf mount — not a horse."""
    p = []
    p.append(g.cyl("body", (0, 0, 0.68), 0.36, 1.35, m.ice, c, verts=12, rot=(0, math.radians(90), 0)))
    p.append(g.uv_sphere("chest", (0.62, 0, 0.78), 0.32, m.leather, c, segs=12, rings=8))
    p.append(g.cyl("neck", (0.82, 0, 1.05), 0.12, 0.45, m.ice, c, verts=10, rot=(0, math.radians(55), 0)))
    p.append(g.uv_sphere("head", (1.05, -0.02, 1.22), 0.18, m.ice, c, segs=12, rings=8))
    p.append(g.cone("snout", (1.28, -0.04, 1.12), 0.08, 0.26, m.leather, c, verts=8, rot=(0, math.radians(90), 0)))
    p.append(g.cube("ear_l", (0.98, 0.12, 1.4), (0.05, 0.04, 0.16), m.ice, c))
    p.append(g.cube("ear_r", (0.98, -0.14, 1.4), (0.05, 0.04, 0.16), m.ice, c))
    for x, y in ((0.38, 0.2), (0.38, -0.2), (-0.38, 0.2), (-0.38, -0.2)):
        p.append(g.cyl(f"ul{x}", (x, y, 0.48), 0.09, 0.45, m.leather, c, verts=10))
        p.append(g.cyl(f"ll{x}", (x, y + 0.04, 0.18), 0.07, 0.32, m.leather, c, verts=8))
    p.append(g.cone("tail", (-0.82, 0, 0.85), 0.1, 0.55, m.ice, c, verts=6, rot=(0, math.radians(-65), 0)))
    p.append(g.cyl("rider", (0.05, 0, 1.22), 0.16, 0.38, m.leather, c, verts=10))
    p.append(g.uv_sphere("rhead", (0.05, 0.02, 1.55), 0.11, m.skin, c, segs=12, rings=8))
    p.append(g.ico("hood", (0.05, 0.04, 1.68), 0.16, m.ice, c, subdiv=1, scale=(1.1, 1.0, 0.55)))
    p.append(g.cyl("spear", (0.45, -0.12, 1.45), 0.025, 1.6, m.wood, c, verts=6, rot=(0, math.radians(40), 0)))
    return finish(g, "unit_outcast_beast_rider", p, c, 0.02)


def outcast_frost_giant(g, m, c):
    p = []
    s = 2.15
    p.append(g.cyl("hips", (0, 0.04, 0.95 * s), 0.28 * s, 0.32 * s, m.leather, c, verts=12))
    p.append(g.cyl("torso", (0, 0.06, 1.22 * s), 0.34 * s, 0.7 * s, m.ice, c, verts=14))
    p.append(g.cube("plastron", (0, -0.22 * s, 1.22 * s), (0.42 * s, 0.12 * s, 0.55 * s), m.ice, c))
    p.append(g.cube("pelts", (0, 0.38 * s, 1.18 * s), (0.7 * s, 0.18 * s, 0.85 * s), m.leather, c))
    _legs(g, p, c, m.ice, s=s, boot=m.leather, boot_s=(0.22, 0.38, 0.14))
    _arms(g, p, c, m.ice, m.ice, m.skin, s=s)
    _head(g, p, c, m, s=s, z=1.68)
    p.append(g.ico("beard", (0, -0.12 * s, 1.55 * s), 0.16 * s, m.ice, c, subdiv=1, scale=(1.1, 0.7, 0.9)))
    p.append(g.cube("helm", (0, 0.04 * s, 1.86 * s), (0.32 * s, 0.28 * s, 0.18 * s), m.ice, c))
    p.append(g.cyl("club", (0.78 * s, -0.08 * s, 1.15 * s), 0.08 * s, 1.55 * s, m.wood, c, verts=10))
    p.append(g.ico("icehead", (0.78 * s, -0.08 * s, 1.95 * s), 0.22 * s, m.ice, c, subdiv=1, scale=(1.1, 0.9, 1.0)))
    return finish(g, "unit_outcast_frost_giant", p, c, 0.022)


def outcast_great_wold(g, m, c):
    p = []
    p.append(g.cyl("body", (0, 0, 0.85), 0.48, 1.7, m.ice, c, verts=12, rot=(0, math.radians(90), 0)))
    p.append(g.uv_sphere("head", (0.95, 0, 1.15), 0.32, m.leather, c, segs=10, rings=6))
    p.append(g.cone("snout", (1.35, 0, 1.0), 0.14, 0.35, m.ice, c, verts=6, rot=(0, math.radians(90), 0)))
    p.append(g.cube("ear_l", (0.85, 0.18, 1.4), (0.08, 0.05, 0.18), m.ice, c))
    p.append(g.cube("ear_r", (0.85, -0.18, 1.4), (0.08, 0.05, 0.18), m.ice, c))
    for x, y in ((0.45, 0.25), (0.45, -0.25), (-0.45, 0.25), (-0.45, -0.25)):
        p.append(g.cyl(f"leg{x}", (x, y, 0.42), 0.12, 0.8, m.leather, c, verts=8))
    p.append(g.cone("tail", (-1.0, 0, 1.0), 0.14, 0.7, m.ice, c, verts=6, rot=(0, math.radians(-60), 0)))
    return finish(g, "unit_outcast_great_wold", p, c, 0.025)


def outcast_nature_cub(g, m, c):
    p = []
    p.append(g.uv_sphere("body", (0, 0, 0.42), 0.34, m.ice, c, segs=10, rings=6))
    p.append(g.uv_sphere("head", (0.32, 0, 0.55), 0.2, m.leather, c, segs=8, rings=6))
    p.append(g.cube("ear_l", (0.26, 0.12, 0.72), (0.07, 0.04, 0.12), m.ice, c))
    p.append(g.cube("ear_r", (0.26, -0.12, 0.72), (0.07, 0.04, 0.12), m.ice, c))
    p.append(g.cube("paw_l", (-0.12, 0.18, 0.12), (0.14, 0.16, 0.1), m.leather, c))
    p.append(g.cube("paw_r", (0.12, 0.18, 0.12), (0.14, 0.16, 0.1), m.leather, c))
    return finish(g, "unit_outcast_nature_cub", p, c, 0.015)


def outcast_sprite(g, m, c):
    p = []
    p.append(g.uv_sphere("body", (0, 0, 0.85), 0.12, m.ice, c, segs=8, rings=6))
    p.append(g.uv_sphere("head", (0, 0, 1.05), 0.1, m.skin, c, segs=8, rings=6))
    p.append(g.cube("wl", (-0.22, 0.02, 0.9), (0.32, 0.02, 0.18), m.glass, c, rot=(0, 0, math.radians(15))))
    p.append(g.cube("wr", (0.22, 0.02, 0.9), (0.32, 0.02, 0.18), m.glass, c, rot=(0, 0, math.radians(-15))))
    p.append(g.cyl("wand", (0.18, -0.08, 0.75), 0.015, 0.45, m.wood, c, verts=6))
    return finish(g, "unit_outcast_sprite", p, c, 0.01)


def outcast_sky_eye(g, m, c):
    p = []
    p.append(g.uv_sphere("body", (0, 0, 0.55), 0.2, m.ice, c, segs=10, rings=6))
    p.append(g.cone("beak", (0, -0.28, 0.58), 0.04, 0.16, m.gold, c, verts=5, rot=(math.radians(90), 0, 0)))
    p.append(g.cube("wl", (-0.4, 0.04, 0.58), (0.55, 0.05, 0.2), m.ice, c, rot=(0, 0, math.radians(12))))
    p.append(g.cube("wr", (0.4, 0.04, 0.58), (0.55, 0.05, 0.2), m.ice, c, rot=(0, 0, math.radians(-12))))
    p.append(g.cyl("leg_l", (-0.05, 0.02, 0.22), 0.02, 0.25, m.leather, c, verts=5))
    p.append(g.cyl("leg_r", (0.05, 0.02, 0.22), 0.02, 0.25, m.leather, c, verts=5))
    return finish(g, "unit_outcast_sky_eye", p, c, 0.01)


# --- Freetown fishing ---

def freetown_drunk(g, m, c):
    p = []
    p.append(g.uv_sphere("belly", (0, 0.08, 1.05), 0.32, m.cloth_blue, c, segs=10, rings=6))
    p.append(g.cyl("torso", (0, 0.04, 1.32), 0.2, 0.35, m.cloth_blue, c, verts=10))
    _legs(g, p, c, m.cloth_blue, boot=m.leather)
    _arms(g, p, c, m.cloth_blue, m.skin, m.skin)
    _head(g, p, c, m)
    p.append(g.cyl("hat", (0, -0.02, 1.84), 0.2, 0.08, m.leather, c, verts=10))
    p.append(g.cube("brim", (0, -0.16, 1.8), (0.18, 0.16, 0.04), m.leather, c))
    p.append(g.cyl("bottle", (0.42, -0.12, 1.05), 0.06, 0.28, m.glass, c, verts=8))
    p.append(g.cyl("keg", (0, 0.32, 1.15), 0.16, 0.28, m.wood, c, verts=10, rot=(0, math.radians(90), 0)))
    return finish(g, "unit_freetown_drunk", p, c, 0.012)


def freetown_crow(g, m, c):
    p = []
    p.append(g.ico("body", (0, 0.08, 0.52), 0.22, m.slate, c, subdiv=2, scale=(0.7, 1.35, 0.85)))
    p.append(g.uv_sphere("head", (0, -0.28, 0.68), 0.11, m.slate, c, segs=12, rings=8))
    p.append(g.cone("beak", (0, -0.42, 0.62), 0.035, 0.16, m.gold, c, verts=6, rot=(math.radians(90), 0, 0)))
    p.append(g.uv_sphere("eye_l", (-0.05, -0.34, 0.72), 0.02, m.gold, c, segs=6, rings=4))
    p.append(g.uv_sphere("eye_r", (0.05, -0.34, 0.72), 0.02, m.gold, c, segs=6, rings=4))
    for i, spread in enumerate((0.32, 0.48, 0.62)):
        p.append(g.cube(f"wl{i}", (-spread, 0.02, 0.52 - i * 0.04), (0.42, 0.04, 0.16), m.slate, c, rot=(0, 0, math.radians(18 + i * 6))))
        p.append(g.cube(f"wr{i}", (spread, 0.02, 0.52 - i * 0.04), (0.42, 0.04, 0.16), m.slate, c, rot=(0, 0, math.radians(-18 - i * 6))))
    for i in range(4):
        p.append(g.cube(f"tail{i}", ((i - 1.5) * 0.05, 0.38, 0.4), (0.06, 0.28, 0.04), m.slate, c, rot=(math.radians(12), 0, 0)))
    p.append(g.cyl("leg_l", (-0.06, 0.04, 0.22), 0.022, 0.28, m.iron, c, verts=6))
    p.append(g.cyl("leg_r", (0.06, 0.04, 0.22), 0.022, 0.28, m.iron, c, verts=6))
    p.append(g.cube("claw_l", (-0.06, 0.1, 0.06), (0.08, 0.14, 0.04), m.iron, c))
    p.append(g.cube("claw_r", (0.06, 0.1, 0.06), (0.08, 0.14, 0.04), m.iron, c))
    return finish(g, "unit_freetown_crow", p, c, 0.008)


def freetown_hound(g, m, c):
    p = []
    p.append(g.cyl("body", (0, 0, 0.55), 0.22, 0.95, m.leather, c, verts=10, rot=(0, math.radians(90), 0)))
    p.append(g.uv_sphere("head", (0.55, 0, 0.62), 0.16, m.leather, c, segs=8, rings=6))
    p.append(g.cube("ear_l", (0.48, 0.1, 0.78), (0.05, 0.12, 0.04), m.leather, c, rot=(0, 0, math.radians(20))))
    p.append(g.cube("ear_r", (0.48, -0.1, 0.78), (0.05, 0.12, 0.04), m.leather, c, rot=(0, 0, math.radians(-20))))
    for x, y in ((0.28, 0.14), (0.28, -0.14), (-0.28, 0.14), (-0.28, -0.14)):
        p.append(g.cyl(f"leg{x}", (x, y, 0.28), 0.05, 0.5, m.leather, c, verts=6))
    p.append(g.cone("tail", (-0.55, 0, 0.7), 0.05, 0.35, m.leather, c, verts=5, rot=(0, math.radians(-50), 0)))
    p.append(g.cyl("collar", (0.35, 0, 0.68), 0.14, 0.06, m.cloth_blue, c, verts=10, rot=(0, math.radians(90), 0)))
    return finish(g, "unit_freetown_hound", p, c, 0.015)


def freetown_warrior_crab(g, m, c):
    p = []
    p.append(g.ico("carapace", (0, 0.05, 0.62), 0.72, m.brick, c, subdiv=2, scale=(1.35, 1.05, 0.42)))
    p.append(g.cube("ridge", (0, 0.05, 0.92), (0.85, 0.55, 0.1), m.iron, c))
    p.append(g.cube("ridge2", (0, 0.05, 0.82), (0.55, 0.85, 0.08), m.iron, c))
    for side, yaw in ((-1, 28), (1, -28)):
        p.append(g.cyl(f"arm{side}", (side * 0.7, 0.35, 0.52), 0.1, 0.55, m.brick, c, verts=8, rot=(0, math.radians(70), math.radians(side * 35))))
        p.append(g.ico(f"claw{side}", (side * 1.15, 0.62, 0.52), 0.22, m.iron, c, subdiv=1, scale=(1.4, 0.7, 0.55)))
        p.append(g.cube(f"pin_a{side}", (side * 1.38, 0.78, 0.52), (0.32, 0.1, 0.1), m.iron, c, rot=(0, 0, math.radians(yaw))))
        p.append(g.cube(f"pin_b{side}", (side * 1.32, 0.55, 0.48), (0.28, 0.08, 0.08), m.iron, c, rot=(0, 0, math.radians(yaw * 0.4))))
    p.append(g.cyl("stalk_l", (-0.16, 0.42, 0.95), 0.03, 0.28, m.brick, c, verts=6, rot=(math.radians(35), 0, 0)))
    p.append(g.cyl("stalk_r", (0.16, 0.42, 0.95), 0.03, 0.28, m.brick, c, verts=6, rot=(math.radians(35), 0, 0)))
    p.append(g.uv_sphere("eye_l", (-0.16, 0.55, 1.12), 0.07, m.gold, c, segs=10, rings=6))
    p.append(g.uv_sphere("eye_r", (0.16, 0.55, 1.12), 0.07, m.gold, c, segs=10, rings=6))
    for i in range(8):
        side = -1 if i < 4 else 1
        slot = i % 4
        x = side * (0.45 + slot * 0.12)
        y = -0.55 + slot * 0.28
        p.append(g.cyl(f"th{i}", (x, y, 0.38), 0.05, 0.45, m.brick, c, verts=8, rot=(math.radians(40), 0, math.radians(side * 18))))
        p.append(g.cyl(f"sh{i}", (x + side * 0.28, y - 0.12, 0.18), 0.04, 0.32, m.brick, c, verts=6, rot=(math.radians(70), 0, 0)))
        p.append(g.cube(f"ft{i}", (x + side * 0.35, y - 0.22, 0.06), (0.12, 0.16, 0.05), m.iron, c))
    return finish(g, "unit_freetown_warrior_crab", p, c, 0.018)


def freetown_flamer(g, m, c):
    p = []
    p.append(g.cyl("tank", (0, 0.15, 1.15), 0.28, 0.85, m.iron, c, verts=12))
    p.append(g.cyl("torso", (0, 0, 1.22), 0.18, 0.42, m.leather, c, verts=10))
    _legs(g, p, c, m.leather)
    _arms(g, p, c, m.leather, m.skin, m.skin)
    _head(g, p, c, m)
    p.append(g.cube("mask", (0, -0.12, 1.68), (0.18, 0.08, 0.12), m.iron, c))
    p.append(g.cyl("hose", (0.15, 0.2, 1.35), 0.04, 0.7, m.leather, c, verts=8, rot=(0, math.radians(50), 0)))
    p.append(g.cyl("nozzle", (0.65, -0.05, 1.15), 0.05, 0.55, m.iron, c, verts=8, rot=(0, math.radians(90), 0)))
    p.append(g.uv_sphere("pilot", (0.95, -0.05, 1.15), 0.08, m.gold, c, segs=8, rings=6))
    return finish(g, "unit_freetown_flamer", p, c, 0.015)


def freetown_powder_cart(g, m, c):
    p = []
    p.append(g.cube("bed", (0, 0, 0.55), (1.9, 1.15, 0.22), m.wood, c))
    for x, y in ((0.65, 0.62), (0.65, -0.62), (-0.65, 0.62), (-0.65, -0.62)):
        p.append(g.cyl(f"w{x}", (x, y, 0.32), 0.28, 0.12, m.wood, c, verts=12, rot=(math.radians(90), 0, 0)))
    for i in range(4):
        p.append(g.cyl(f"keg{i}", (-0.45 + (i % 2) * 0.7, -0.25 + (i // 2) * 0.5, 0.95), 0.28, 0.5, m.wood, c, verts=10))
    p.append(g.cube("fuse", (0.2, 0, 1.22), (0.04, 0.04, 0.18), m.cloth, c))
    p.append(g.cube("tongue", (1.15, 0, 0.45), (0.7, 0.12, 0.08), m.wood, c))
    return finish(g, "unit_freetown_powder_cart", p, c, 0.02)


def unit_river_boat(g, m, c):
    p = []
    p.append(g.cube("hull", (0, 0, 0.32), (2.6, 0.75, 0.38), m.wood, c))
    p.append(g.taper("bow", (1.4, 0, 0.38), 0.38, 0.08, 0.75, m.pale_wood, c, verts=8, rot=(0, math.radians(90), 0)))
    p.append(g.taper("stern", (-1.35, 0, 0.38), 0.32, 0.12, 0.5, m.wood, c, verts=8, rot=(0, math.radians(-90), 0)))
    p.append(g.cyl("mast", (-0.1, 0, 1.25), 0.05, 1.6, m.wood, c, verts=8))
    p.append(g.cube("sail", (0.02, 0, 1.45), (0.04, 0.85, 1.05), m.cloth_blue, c))
    p.append(g.cube("thwart", (0.2, 0, 0.55), (0.12, 0.7, 0.08), m.pale_wood, c))
    p.append(g.cyl("oar_l", (0.2, 0.55, 0.7), 0.03, 1.1, m.wood, c, verts=6, rot=(math.radians(70), 0, 0)))
    p.append(g.cyl("oar_r", (0.2, -0.55, 0.7), 0.03, 1.1, m.wood, c, verts=6, rot=(math.radians(-70), 0, 0)))
    return finish(g, "unit_river_boat", p, c, 0.02)


# --- University ---

def university_fellow(g, m, c):
    p = []
    p.append(g.cyl("gown", (0, 0.04, 1.12), 0.3, 1.28, m.cloth_deep, c, verts=12))
    p.append(g.cube("facing", (0, -0.26, 1.15), (0.16, 0.05, 1.0), m.red_brick, c))
    _head(g, p, c, m)
    p.append(g.cyl("board", (0, 0, 1.88), 0.16, 0.08, m.cloth_deep, c, verts=8))
    p.append(g.cube("tassel", (0.18, 0, 1.82), (0.04, 0.04, 0.22), m.gold, c))
    _arms(g, p, c, m.cloth_deep, m.cloth_deep, m.skin)
    p.append(g.cube("book", (0.32, -0.22, 1.05), (0.16, 0.05, 0.22), m.leather, c))
    p.append(g.cube("boot_l", (-0.12, 0.08, 0.08), (0.16, 0.26, 0.12), m.leather, c))
    p.append(g.cube("boot_r", (0.12, 0.08, 0.08), (0.16, 0.26, 0.12), m.leather, c))
    return finish(g, "unit_university_fellow", p, c, 0.012)


def _bone(g, p, c, name, a, b, r, mat, verts=10):
    """Cylinder whose +Z axis actually runs from point a to point b."""
    ax, ay, az = a
    bx, by, bz = b
    d = Vector((bx - ax, by - ay, bz - az))
    length = d.length or 0.05
    eul = Vector((0.0, 0.0, 1.0)).rotation_difference(d.normalized()).to_euler("XYZ")
    p.append(
        g.cyl(
            name,
            ((ax + bx) * 0.5, (ay + by) * 0.5, (az + bz) * 0.5),
            r,
            length,
            mat,
            c,
            verts=verts,
            rot=(eul.x, eul.y, eul.z),
        )
    )


def university_mechanical_spider(g, m, c):
    """Raised brass-and-brick arachnid. Head faces the close-up camera (-Y)."""
    p = []
    # Three separate masses so it does not read as one soap bar
    p.append(g.ico("abdomen", (0.0, 0.55, 0.88), 0.42, m.steel, c, subdiv=3, scale=(0.95, 1.35, 0.78)))
    p.append(g.cube("thorax", (0.0, 0.02, 0.92), (0.62, 0.55, 0.48), m.iron, c))
    p.append(g.uv_sphere("head", (0.0, -0.42, 0.86), 0.22, m.steel, c, segs=14, rings=10))
    for i in range(4):
        y = 0.28 + i * 0.14
        p.append(g.cube(f"seg{i}", (0.0, y, 1.16), (0.55 - i * 0.06, 0.1, 0.06), m.iron, c))
        p.append(g.cube(f"rivl{i}", (0.22, y, 1.2), (0.05, 0.05, 0.05), m.gold, c))
        p.append(g.cube(f"rivr{i}", (-0.22, y, 1.2), (0.05, 0.05, 0.05), m.gold, c))
    p.append(g.cyl("boiler", (0.0, 0.58, 1.28), 0.18, 0.28, m.red_brick, c, verts=14))
    p.append(g.cube("firebox", (0.0, 0.38, 1.18), (0.2, 0.08, 0.12), m.iron, c))
    _bone(g, p, c, "exhaust_l", (0.1, 0.7, 1.4), (0.16, 0.92, 1.52), 0.03, m.iron, 8)
    _bone(g, p, c, "exhaust_r", (-0.1, 0.7, 1.4), (-0.16, 0.92, 1.52), 0.03, m.iron, 8)
    p.append(g.cyl("gear_l", (0.34, 0.02, 0.92), 0.16, 0.08, m.gold, c, verts=16, rot=(0, math.radians(90), 0)))
    p.append(g.cyl("gear_r", (-0.34, 0.02, 0.92), 0.16, 0.08, m.gold, c, verts=16, rot=(0, math.radians(90), 0)))
    p.append(g.cyl("gear_top", (0.0, 0.02, 1.18), 0.12, 0.07, m.gold, c, verts=14))
    p.append(g.cube("brow", (0.0, -0.58, 1.0), (0.32, 0.06, 0.06), m.iron, c))
    for x, z, r in ((0.1, 0.9, 0.055), (-0.1, 0.9, 0.055), (0.16, 0.8, 0.035), (-0.16, 0.8, 0.035), (0.08, 0.76, 0.03), (-0.08, 0.76, 0.03)):
        p.append(g.cyl("lens", (x, -0.58, z), r, 0.08, m.glass, c, verts=12, rot=(math.radians(90), 0, 0)))
        p.append(g.uv_sphere("pupil", (x, -0.63, z), r * 0.4, m.crystal, c, segs=8, rings=6))
    _bone(g, p, c, "mand_l", (0.1, -0.52, 0.7), (0.18, -0.78, 0.5), 0.032, m.iron, 8)
    _bone(g, p, c, "mand_r", (-0.1, -0.52, 0.7), (-0.18, -0.78, 0.5), 0.032, m.iron, 8)
    p.append(g.cube("fang_l", (0.2, -0.82, 0.46), (0.08, 0.1, 0.06), m.steel, c))
    p.append(g.cube("fang_r", (-0.2, -0.82, 0.46), (0.08, 0.1, 0.06), m.steel, c))
    # Feet on an ellipse; knees high so the stance reads as a spider
    lat = (0.95, 1.2, 1.22, 0.98)
    along = (-0.85, -0.28, 0.32, 0.9)
    for i in range(8):
        side = 1.0 if i < 4 else -1.0
        slot = i % 4
        hip = (side * 0.28, 0.08 - slot * 0.12, 0.82)
        foot = (side * lat[slot], along[slot], 0.05)
        knee = (side * (0.72 + slot * 0.05), (hip[1] + foot[1]) * 0.45, 1.32)
        ankle = (foot[0] * 0.82, foot[1] * 0.82 + hip[1] * 0.1, 0.4)
        p.append(g.uv_sphere(f"hip{i}", hip, 0.075, m.iron, c, segs=10, rings=6))
        _bone(g, p, c, f"femur{i}", hip, knee, 0.06, m.steel, 10)
        p.append(g.uv_sphere(f"knee{i}", knee, 0.075, m.gold, c, segs=10, rings=6))
        _bone(g, p, c, f"tibia{i}", knee, ankle, 0.048, m.iron, 10)
        p.append(g.uv_sphere(f"ankle{i}", ankle, 0.055, m.gold, c, segs=8, rings=6))
        _bone(g, p, c, f"tarsus{i}", ankle, foot, 0.038, m.steel, 8)
        p.append(g.cube(f"foot{i}", foot, (0.14, 0.12, 0.06), m.iron, c))
        _bone(g, p, c, f"piston{i}", (hip[0], hip[1], hip[2] + 0.1), ((hip[0] + knee[0]) * 0.5, (hip[1] + knee[1]) * 0.5, (hip[2] + knee[2]) * 0.5), 0.02, m.gold, 6)
    p.append(g.cube("saddle", (0.0, 0.02, 1.18), (0.28, 0.4, 0.07), m.leather, c))
    return finish(g, "unit_university_mechanical_spider", p, c, 0.008, 2)


def university_airship(g, m, c):
    p = []
    p.append(g.cyl("bag", (0, 0, 2.25), 0.62, 2.85, m.cloth_deep, c, verts=16, rot=(0, math.radians(90), 0)))
    p.append(g.uv_sphere("nose", (1.45, 0, 2.25), 0.58, m.cloth_deep, c, segs=14, rings=10))
    p.append(g.uv_sphere("tail", (-1.45, 0, 2.25), 0.5, m.cloth_deep, c, segs=12, rings=8))
    p.append(g.cyl("band_a", (0.45, 0, 2.25), 0.64, 0.08, m.gold, c, verts=16, rot=(0, math.radians(90), 0)))
    p.append(g.cyl("band_b", (-0.45, 0, 2.25), 0.64, 0.08, m.gold, c, verts=16, rot=(0, math.radians(90), 0)))
    p.append(g.cube("gondola", (0.1, 0, 1.15), (1.55, 0.55, 0.42), m.wood, c))
    p.append(g.cube("cabin", (0.15, 0, 1.42), (0.95, 0.48, 0.28), m.pale_wood, c))
    p.append(g.cube("pane", (0.15, -0.28, 1.42), (0.55, 0.04, 0.16), m.glass, c))
    p.append(g.cube("rail", (0.1, 0, 1.4), (1.5, 0.52, 0.06), m.gold, c))
    p.append(g.cube("fin_v", (-1.55, 0, 2.55), (0.12, 0.08, 0.7), m.cloth_deep, c))
    p.append(g.cube("fin_h", (-1.5, 0, 2.15), (0.12, 0.85, 0.1), m.cloth_deep, c))
    p.append(g.cyl("prop", (-1.85, 0, 2.15), 0.08, 0.14, m.iron, c, verts=10, rot=(0, math.radians(90), 0)))
    p.append(g.cube("blade_a", (-1.95, 0, 2.15), (0.04, 0.7, 0.08), m.wood, c))
    p.append(g.cube("blade_b", (-1.95, 0, 2.15), (0.04, 0.08, 0.7), m.wood, c))
    p.append(g.cyl("stay_l", (-0.35, 0.18, 1.65), 0.02, 1.05, m.iron, c, verts=6, rot=(math.radians(28), 0, 0)))
    p.append(g.cyl("stay_r", (0.35, 0.18, 1.65), 0.02, 1.05, m.iron, c, verts=6, rot=(math.radians(28), 0, 0)))
    p.append(g.cyl("stack", (-0.45, 0, 1.55), 0.06, 0.4, m.iron, c, verts=8))
    return finish(g, "unit_university_airship", p, c, 0.016)


def university_trebuchet(g, m, c):
    p = []
    p.append(g.cube("base", (0, 0, 0.45), (2.4, 1.5, 0.28), m.wood, c))
    p.append(g.cube("up_l", (-0.15, 0.55, 1.35), (0.18, 0.18, 1.7), m.wood, c))
    p.append(g.cube("up_r", (-0.15, -0.55, 1.35), (0.18, 0.18, 1.7), m.wood, c))
    p.append(g.cube("axle", (-0.15, 0, 2.15), (0.15, 1.2, 0.15), m.iron, c))
    p.append(g.cyl("arm", (0.35, 0, 1.85), 0.1, 2.4, m.wood, c, verts=8, rot=(0, math.radians(38), 0)))
    p.append(g.cube("counter", (-0.85, 0, 1.15), (0.55, 0.45, 0.45), m.slate, c))
    p.append(g.cube("sling", (1.25, 0, 2.45), (0.35, 0.25, 0.12), m.leather, c))
    for x, y in ((0.9, 0.7), (0.9, -0.7), (-0.9, 0.7), (-0.9, -0.7)):
        p.append(g.cyl(f"w{x}", (x, y, 0.32), 0.3, 0.12, m.wood, c, verts=12, rot=(math.radians(90), 0, 0)))
    return finish(g, "unit_university_trebuchet", p, c, 0.025)


def university_earth_breaker(g, m, c):
    p = []
    p.append(g.cube("body", (0, 0, 0.7), (1.6, 1.1, 0.7), m.steel, c))
    p.append(g.cyl("boiler", (0, 0, 1.25), 0.42, 0.55, m.iron, c, verts=12))
    p.append(g.cyl("stack", (-0.45, 0, 1.7), 0.1, 0.55, m.steel, c, verts=8))
    p.append(g.cyl("drill", (1.05, 0, 0.55), 0.18, 1.1, m.iron, c, verts=8, rot=(0, math.radians(90), 0)))
    p.append(g.cone("bit", (1.65, 0, 0.55), 0.2, 0.35, m.steel, c, verts=6, rot=(0, math.radians(90), 0)))
    for i in range(6):
        ang = i * math.pi / 3
        p.append(g.cube(f"fl{i}", (1.15 + math.cos(ang) * 0.05, math.sin(ang) * 0.2, 0.55), (0.08, 0.08, 0.08), m.gold, c))
    for x, y in ((0.5, 0.55), (0.5, -0.55), (-0.5, 0.55), (-0.5, -0.55)):
        p.append(g.cyl(f"w{x}", (x, y, 0.28), 0.26, 0.12, m.iron, c, verts=10, rot=(math.radians(90), 0, 0)))
    return finish(g, "unit_university_earth_breaker", p, c, 0.02)




# --- Church renaissance ---

def church_dawn_zealot(g, m, c):
    p = []
    p.append(g.cyl("brig", (0, 0.02, 1.26), 0.21, 0.5, m.leather, c, verts=12))
    p.append(g.cube("sun_tab", (0, -0.16, 1.12), (0.28, 0.05, 0.85), m.cloth_sun, c))
    _legs(g, p, c, m.leather, boot=m.iron)
    _arms(g, p, c, m.leather, m.skin, m.skin)
    _head(g, p, c, m)
    p.append(g.cyl("morion", (0, 0, 1.82), 0.16, 0.12, m.iron, c, verts=10))
    p.append(g.cube("comb", (0, 0, 1.94), (0.04, 0.22, 0.16), m.iron, c))
    p.append(g.cyl("spear", (0.6, -0.04, 1.25), 0.028, 2.15, m.wood, c, verts=8))
    p.append(g.cube("sun_head", (0.6, -0.04, 2.32), (0.16, 0.04, 0.16), m.gold, c))
    return finish(g, "unit_church_dawn_zealot", p, c, 0.012)


def church_solar_engine(g, m, c):
    p = []
    p.append(g.cube("carriage", (0, 0, 0.55), (1.7, 1.2, 0.3), m.wood, c))
    p.append(g.cyl("disc", (0.15, 0, 1.35), 0.85, 0.12, m.gold, c, verts=16, rot=(math.radians(75), 0, 0)))
    p.append(g.cyl("hub", (0.15, 0, 1.35), 0.18, 0.22, m.marble, c, verts=10, rot=(math.radians(75), 0, 0)))
    for k in range(8):
        ang = k * math.pi / 4
        p.append(g.cube(f"ray{k}", (0.15 + math.cos(ang) * 0.55, math.sin(ang) * 0.15, 1.35 + math.sin(ang) * 0.55), (0.08, 0.08, 0.45), m.gold, c))
    p.append(g.cube("lens", (0.55, 0, 1.45), (0.12, 0.35, 0.35), m.glass, c))
    for x, y in ((0.55, 0.58), (0.55, -0.58), (-0.55, 0.58), (-0.55, -0.58)):
        p.append(g.cyl(f"w{x}", (x, y, 0.3), 0.26, 0.12, m.wood, c, verts=10, rot=(math.radians(90), 0, 0)))
    return finish(g, "unit_church_solar_engine", p, c, 0.02)


def church_high_priest(g, m, c):
    p = []
    p.append(g.cyl("chasuble", (0, 0.04, 1.15), 0.34, 1.32, m.cloth_sun, c, verts=12))
    p.append(g.cube("orphrey", (0, -0.28, 1.2), (0.18, 0.05, 1.05), m.gold, c))
    _head(g, p, c, m)
    p.append(g.cone("mitre", (0, 0, 2.05), 0.16, 0.42, m.cloth_sun, c, verts=6))
    p.append(g.cube("mitre_band", (0, 0, 1.86), (0.28, 0.12, 0.06), m.gold, c))
    _arms(g, p, c, m.cloth_sun, m.cloth_sun, m.skin)
    p.append(g.cyl("crozier", (0.58, -0.04, 1.25), 0.03, 1.9, m.gold, c, verts=8))
    p.append(g.cyl("hook", (0.7, -0.04, 2.15), 0.12, 0.08, m.gold, c, verts=10))
    p.append(g.cube("boot_l", (-0.12, 0.08, 0.08), (0.16, 0.26, 0.12), m.leather, c))
    p.append(g.cube("boot_r", (0.12, 0.08, 0.08), (0.16, 0.26, 0.12), m.leather, c))
    return finish(g, "unit_church_high_priest", p, c, 0.012)


def church_radiant_guard(g, m, c):
    p = []
    p.append(g.cyl("plate", (0, 0.02, 1.28), 0.23, 0.52, m.marble, c, verts=12))
    p.append(g.cube("sun", (0, -0.18, 1.35), (0.22, 0.05, 0.22), m.gold, c))
    _legs(g, p, c, m.marble, boot=m.gold)
    _arms(g, p, c, m.marble, m.marble, m.gold)
    _head(g, p, c, m)
    p.append(g.cyl("helm", (0, 0, 1.86), 0.16, 0.2, m.gold, c, verts=12))
    p.append(g.cyl("disc", (0, -0.16, 1.86), 0.18, 0.04, m.gold, c, verts=12, rot=(math.radians(80), 0, 0)))
    p.append(g.cyl("spear", (0.62, -0.04, 1.3), 0.028, 2.2, m.gold, c, verts=8))
    p.append(g.cube("heater", (-0.62, 0.14, 1.12), (0.48, 0.08, 0.75), m.marble, c))
    return finish(g, "unit_church_radiant_guard", p, c, 0.012)


def church_dawn_rider(g, m, c):
    p = []
    p.append(g.cyl("body", (0, 0.08, 1.05), 0.38, 1.25, m.leather, c, verts=14, rot=(0, math.radians(90), 0)))
    p.append(g.uv_sphere("chest", (0.62, 0.06, 1.08), 0.32, m.leather, c, segs=12, rings=8))
    p.append(g.cyl("neck", (0.78, 0.02, 1.38), 0.11, 0.48, m.leather, c, verts=10, rot=(0, math.radians(48), 0)))
    p.append(g.uv_sphere("hhead", (0.98, -0.04, 1.62), 0.15, m.leather, c, segs=12, rings=8))
    p.append(g.cube("caparison", (0, 0.05, 1.22), (1.15, 0.7, 0.08), m.cloth_sun, c))
    for x, y in ((0.38, 0.2), (0.38, -0.14), (-0.38, 0.2), (-0.38, -0.14)):
        p.append(g.cyl(f"ul{x}", (x, y, 0.62), 0.085, 0.55, m.leather, c, verts=10))
        p.append(g.cyl(f"ll{x}", (x, y + 0.03, 0.22), 0.07, 0.38, m.leather, c, verts=8))
    p.append(g.cyl("rider", (0.05, 0.02, 1.82), 0.16, 0.38, m.marble, c, verts=12))
    p.append(g.uv_sphere("rhead", (0.05, 0.04, 2.15), 0.12, m.skin, c, segs=12, rings=8))
    p.append(g.cyl("helm", (0.05, 0.04, 2.28), 0.13, 0.12, m.gold, c, verts=10))
    p.append(g.cyl("lance", (0.5, -0.12, 2.0), 0.028, 2.2, m.gold, c, verts=8, rot=(0, math.radians(52), 0)))
    return finish(g, "unit_church_dawn_rider", p, c, 0.02)



UNITS = {
    "unit_veiled_apprentice": veiled_apprentice,
    "unit_veiled_builder": veiled_builder,
    "unit_veiled_rune_caster": veiled_rune_caster,
    "unit_veiled_elemental": veiled_elemental,
    "unit_veiled_golem": veiled_golem,
    "unit_veiled_priest_guard": veiled_priest_guard,
    "unit_veiled_shadow": veiled_shadow,
    "unit_veiled_assassin": veiled_assassin,
    "unit_veiled_massed": veiled_massed,
    "unit_veiled_souling": veiled_souling,
    "unit_veiled_heir": veiled_heir,
    "unit_veiled_colossus": veiled_colossus,
    "unit_veiled_thorn_speaker": veiled_thorn_speaker,
    "unit_veiled_night_abbot": veiled_night_abbot,
    "unit_veiled_first_heretic": veiled_first_heretic,
    "unit_veiled_dark_spy": veiled_dark_spy,
    "unit_veiled_shade": veiled_shade,
    "unit_royal_builder": royal_builder,
    "unit_royal_legion": royal_legion,
    "unit_royal_guard": royal_guard,
    "unit_royal_longbow": royal_longbow,
    "unit_royal_commander": royal_commander,
    "unit_royal_spy": royal_spy,
    "unit_royal_crown_eye": royal_crown_eye,
    "unit_royal_pioneer": royal_pioneer,
    "unit_royal_onager": royal_onager,
    "unit_royal_king": royal_king,
    "unit_royal_legion_marshal": royal_legion_marshal,
    "unit_royal_spymaster": royal_spymaster,
    "unit_royal_tomb_warden": royal_tomb_warden,
    "unit_royal_justiciar": royal_justiciar,
    "unit_outcast_villager": outcast_villager,
    "unit_outcast_hobgoblin": outcast_hobgoblin,
    "unit_outcast_hunter": outcast_hunter,
    "unit_outcast_ranger": outcast_ranger,
    "unit_outcast_beast_rider": outcast_beast_rider,
    "unit_outcast_frost_giant": outcast_frost_giant,
    "unit_outcast_sprite": outcast_sprite,
    "unit_outcast_nature_cub": outcast_nature_cub,
    "unit_outcast_sky_eye": outcast_sky_eye,
    "unit_outcast_great_wold": outcast_great_wold,
    "unit_freetown_drunk": freetown_drunk,
    "unit_freetown_crow": freetown_crow,
    "unit_freetown_hound": freetown_hound,
    "unit_freetown_warrior_crab": freetown_warrior_crab,
    "unit_freetown_flamer": freetown_flamer,
    "unit_freetown_powder_cart": freetown_powder_cart,
    "unit_university_fellow": university_fellow,
    "unit_university_mechanical_spider": university_mechanical_spider,
    "unit_university_airship": university_airship,
    "unit_university_trebuchet": university_trebuchet,
    "unit_university_earth_breaker": university_earth_breaker,
    "unit_church_dawn_zealot": church_dawn_zealot,
    "unit_church_dawn_rider": church_dawn_rider,
    "unit_church_radiant_guard": church_radiant_guard,
    "unit_church_solar_engine": church_solar_engine,
    "unit_church_high_priest": church_high_priest,
    "unit_river_boat": unit_river_boat,
}
UNITS.update(UNIQUE_HUMANS)
