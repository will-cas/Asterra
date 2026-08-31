"""Tileable albedo / roughness / normal maps for Asterra Blender art."""
from __future__ import annotations

import math
from pathlib import Path


def _h(x, y, s):
    n = math.sin(x * 127.1 + y * 311.7 + s * 74.7) * 43758.5453
    return n - math.floor(n)


def _vnoise(x, y, s):
    ix, iy = math.floor(x), math.floor(y)
    fx, fy = x - ix, y - iy
    ux = fx * fx * (3.0 - 2.0 * fx)
    uy = fy * fy * (3.0 - 2.0 * fy)
    a = _h(ix, iy, s)
    b = _h(ix + 1, iy, s)
    c = _h(ix, iy + 1, s)
    d = _h(ix + 1, iy + 1, s)
    return a + (b - a) * ux + (c - a) * uy + (a - b - c + d) * ux * uy


def fbm(x, y, s=0.0, octaves=5, scale=1.0):
    amp, freq, total, norm = 1.0, 1.0, 0.0, 0.0
    for i in range(octaves):
        total += amp * _vnoise(x * freq * scale, y * freq * scale, s + i * 19.0)
        norm += amp
        amp *= 0.5
        freq *= 2.05
    return total / max(norm, 1e-6)


def _lerp(a, b, t):
    return a + (b - a) * t


def _mix3(a, b, t):
    return (_lerp(a[0], b[0], t), _lerp(a[1], b[1], t), _lerp(a[2], b[2], t))


def _clamp(v, lo=0.0, hi=1.0):
    return lo if v < lo else hi if v > hi else v


def _height_normal(height_fn, x, y, w, h, strength=4.0):
    e = 1.0
    dx = height_fn(min(x + e, w - 1), y) - height_fn(max(x - e, 0), y)
    dy = height_fn(x, min(y + e, h - 1)) - height_fn(x, max(y - e, 0))
    nx, ny, nz = -dx * strength, -dy * strength, 1.0
    length = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
    return (nx / length * 0.5 + 0.5, ny / length * 0.5 + 0.5, nz / length * 0.5 + 0.5)


def fill_maps(w, h, sample):
    albedo = [0.0] * (w * h * 4)
    rough = [0.0] * (w * h * 4)
    height = [[0.0] * w for _ in range(h)]
    for y in range(h):
        row = height[y]
        for x in range(w):
            r, g, b, rg, ht = sample(x, y, w, h)
            i = (y * w + x) * 4
            albedo[i] = _clamp(r)
            albedo[i + 1] = _clamp(g)
            albedo[i + 2] = _clamp(b)
            albedo[i + 3] = 1.0
            rough[i] = rough[i + 1] = rough[i + 2] = _clamp(rg)
            rough[i + 3] = 1.0
            row[x] = ht

    def hfn(x, y):
        return height[int(y)][int(x)]

    nrm = [0.0] * (w * h * 4)
    for y in range(h):
        for x in range(w):
            nx, ny, nz = _height_normal(hfn, x, y, w, h)
            i = (y * w + x) * 4
            nrm[i], nrm[i + 1], nrm[i + 2], nrm[i + 3] = nx, ny, nz, 1.0
    return albedo, rough, nrm


def sample_brick(x, y, w, h, c1=(0.52, 0.48, 0.42), c2=(0.40, 0.37, 0.32), mortar=(0.22, 0.21, 0.18)):
    cols, rows = 5.0, 8.0
    u = x / w * cols
    v = y / h * rows
    row = math.floor(v)
    u += 0.5 if row % 2 else 0.0
    fx, fy = u - math.floor(u), v - math.floor(v)
    mortar_w = 0.07
    in_mortar = fx < mortar_w or fy < mortar_w
    n = fbm(x / w, y / h, 3.0, 5, 7.0)
    moss = max(0.0, fbm(x / w, y / h, 41.0, 3, 3.0) - 0.62) * 2.0
    dirt = fbm(x / w, y / h, 17.0, 3, 2.4)
    brick = _mix3(c1, c2, _clamp(n * 1.2 - 0.15 + _h(math.floor(u), row, 2.0) * 0.3))
    col = mortar if in_mortar else brick
    col = _mix3(col, (0.18, 0.24, 0.12), moss * (0.55 if in_mortar else 0.2))
    col = _mix3(col, (0.28, 0.24, 0.18), dirt * 0.28)
    ht = (0.12 if in_mortar else 0.62 + n * 0.22)
    rg = 0.88 if in_mortar else 0.70 + n * 0.1
    return (*col, rg, ht)


def sample_plaster(x, y, w, h, c=(0.56, 0.52, 0.45)):
    n = fbm(x / w, y / h, 8.0, 6, 5.5)
    stains = fbm(x / w, y / h, 21.0, 4, 1.8)
    cracks = abs(math.sin((x / w) * 22.0 + n * 4.0))
    col = _mix3(c, (0.38, 0.36, 0.30), stains * 0.55)
    col = _mix3(col, (0.30, 0.28, 0.24), max(0.0, n - 0.58) * 0.7)
    col = _mix3(col, (0.22, 0.21, 0.18), max(0.0, 0.12 - cracks) * 1.4)
    return (*col, 0.78 + n * 0.08, n * 0.7)


def sample_slate(x, y, w, h):
    n = fbm(x / w, y / h, 4.0, 5, 9.0)
    lines = abs(math.sin((y / h) * 28.0 + n * 1.6))
    c1, c2 = (0.18, 0.19, 0.21), (0.10, 0.11, 0.12)
    col = _mix3(c1, c2, n)
    col = _mix3(col, (0.28, 0.26, 0.22), max(0.0, 0.22 - lines) * 1.6)
    wet = max(0.0, fbm(x / w, y / h, 33.0, 2, 2.0) - 0.55)
    col = _mix3(col, (0.12, 0.14, 0.13), wet * 0.5)
    return (*col, 0.52 + n * 0.18 - wet * 0.15, n * 0.45 + (1.0 - lines) * 0.25)


def sample_gold(x, y, w, h):
    n = fbm(x / w, y / h, 11.0, 5, 10.0)
    scratches = abs(math.sin((x / w) * 54.0 + n * 6.0))
    c1, c2 = (0.58, 0.44, 0.20), (0.28, 0.20, 0.09)
    col = _mix3(c1, c2, n * 0.75)
    col = _mix3(col, (0.16, 0.12, 0.06), max(0.0, 0.18 - scratches) * 1.3)
    oxid = max(0.0, fbm(x / w, y / h, 29.0, 3, 3.0) - 0.6) * 1.8
    col = _mix3(col, (0.32, 0.38, 0.18), oxid * 0.35)
    return (*col, 0.42 + n * 0.2 + oxid * 0.15, n)


def sample_wood(x, y, w, h):
    grain = math.sin((x / w) * 28.0 + fbm(x / w, y / h, 1.0, 4, 18.0) * 6.0)
    n = fbm(x / w, y / h, 6.0, 4, 8.0)
    c1, c2 = (0.42, 0.26, 0.12), (0.22, 0.12, 0.06)
    col = _mix3(c1, c2, n * 0.5 + grain * 0.25 + 0.25)
    rings = abs(math.sin((y / h) * 9.0 + n))
    col = _mix3(col, (0.16, 0.09, 0.04), rings * 0.2)
    return (*col, 0.72 + n * 0.1, n * 0.4 + rings * 0.2)


def sample_leather(x, y, w, h):
    n = fbm(x / w, y / h, 9.0, 5, 16.0)
    pores = _vnoise(x / w * 40.0, y / h * 40.0, 3.0)
    c1, c2 = (0.32, 0.20, 0.11), (0.16, 0.09, 0.05)
    col = _mix3(c1, c2, n)
    col = _mix3(col, (0.1, 0.06, 0.03), pores * 0.25)
    return (*col, 0.7 + n * 0.1, n * 0.6 + pores * 0.2)


def sample_cloth(x, y, w, h, c=(0.42, 0.34, 0.22)):
    # Warp/weft threads, not a dotted grid.
    warp = 0.5 + 0.5 * math.sin((x / w) * 48.0 + fbm(x / w, y / h, 4.0, 2, 3.0))
    weft = 0.5 + 0.5 * math.sin((y / h) * 36.0)
    n = fbm(x / w, y / h, 2.0, 4, 3.5)
    folds = fbm(x / w, y / h, 9.0, 3, 1.6)
    weave = warp * 0.55 + weft * 0.45
    col = _mix3(c, (c[0] * 0.78, c[1] * 0.76, c[2] * 0.7), weave * 0.22 + n * 0.18)
    col = _mix3(col, (c[0] * 0.62, c[1] * 0.55, c[2] * 0.42), max(0.0, folds - 0.55) * 0.5)
    return (*col, 0.76, n * 0.25 + weave * 0.1)


def sample_iron(x, y, w, h):
    n = fbm(x / w, y / h, 14.0, 5, 12.0)
    rust = max(0.0, fbm(x / w, y / h, 30.0, 3, 3.5) - 0.55) * 2.0
    c1, c2 = (0.48, 0.50, 0.52), (0.22, 0.23, 0.25)
    col = _mix3(c1, c2, n)
    col = _mix3(col, (0.42, 0.22, 0.10), rust)
    return (*col, 0.35 + rust * 0.4 + n * 0.1, n + rust)


def sample_skin(x, y, w, h):
    n = fbm(x / w, y / h, 5.0, 4, 20.0)
    col = _mix3((0.64, 0.47, 0.36), (0.50, 0.34, 0.26), n)
    return (*col, 0.52, n * 0.25)


def sample_bark(x, y, w, h):
    ridges = abs(math.sin((x / w) * 36.0 + fbm(x / w, y / h, 7.0, 4, 8.0) * 4.0))
    n = fbm(x / w, y / h, 18.0, 5, 7.0)
    c1, c2 = (0.34, 0.22, 0.12), (0.12, 0.08, 0.04)
    col = _mix3(c1, c2, ridges * 0.6 + n * 0.3)
    moss = max(0.0, fbm(x / w, y / h, 40.0, 3, 4.0) - 0.6) * 2.2
    col = _mix3(col, (0.18, 0.32, 0.10), moss)
    return (*col, 0.86, ridges * 0.7 + n * 0.2)


def sample_leaf(x, y, w, h, c=(0.24, 0.46, 0.16)):
    n = fbm(x / w, y / h, 12.0, 5, 9.0)
    veins = abs(math.sin((x / w) * 18.0 + (y / h) * 6.0))
    col = _mix3(c, (0.10, 0.22, 0.08), n * 0.55)
    col = _mix3(col, (0.18, 0.36, 0.12), veins * 0.2)
    return (*col, 0.62, n)


def sample_grass(x, y, w, h):
    n = fbm(x / w, y / h, 1.0, 5, 14.0)
    blades = abs(math.sin((x / w) * 90.0 + n * 8.0))
    col = _mix3((0.28, 0.40, 0.16), (0.16, 0.26, 0.10), n)
    col = _mix3(col, (0.35, 0.42, 0.14), blades * 0.15)
    return (*col, 0.88, n)


def sample_crystal(x, y, w, h):
    n = fbm(x / w, y / h, 22.0, 4, 11.0)
    facet = abs(math.sin((x + y) / w * 40.0))
    col = _mix3((0.95, 0.82, 0.22), (0.75, 0.55, 0.10), n)
    col = _mix3(col, (1.0, 0.95, 0.55), facet * 0.35)
    return (*col, 0.22 + n * 0.1, facet)


def sample_ice(x, y, w, h):
    n = fbm(x / w, y / h, 15.0, 5, 8.0)
    cracks = abs(math.sin((x / w) * 36.0 + (y / h) * 9.0 + n * 4.0))
    col = _mix3((0.78, 0.88, 0.94), (0.55, 0.68, 0.78), n)
    col = _mix3(col, (0.92, 0.96, 1.0), max(0.0, 0.2 - cracks) * 2.0)
    return (*col, 0.18 + n * 0.12, n * 0.4 + cracks * 0.2)


def sample_glass(x, y, w, h):
    n = fbm(x / w, y / h, 19.0, 3, 6.0)
    streak = abs(math.sin((x / w) * 14.0 + n))
    col = _mix3((0.42, 0.52, 0.62), (0.22, 0.18, 0.32), n * 0.45)
    col = _mix3(col, (0.70, 0.78, 0.88), streak * 0.25)
    return (*col, 0.08 + n * 0.08, streak * 0.15)


def sample_steel(x, y, w, h):
    n = fbm(x / w, y / h, 16.0, 5, 14.0)
    panel = abs(math.sin((x / w) * 8.0) * math.sin((y / h) * 8.0))
    c1, c2 = (0.38, 0.40, 0.44), (0.14, 0.15, 0.17)
    col = _mix3(c1, c2, n)
    col = _mix3(col, (0.55, 0.58, 0.62), panel * 0.2)
    return (*col, 0.28 + n * 0.12, n * 0.5 + panel * 0.2)


def sample_dark_stone(x, y, w, h):
    n = fbm(x / w, y / h, 7.0, 5, 6.0)
    col = _mix3((0.16, 0.15, 0.18), (0.07, 0.07, 0.09), n)
    col = _mix3(col, (0.22, 0.16, 0.28), max(0.0, n - 0.55) * 0.5)
    return (*col, 0.72 + n * 0.1, n)


def sample_red_brick(x, y, w, h):
    return sample_brick(x, y, w, h, c1=(0.58, 0.28, 0.18), c2=(0.42, 0.18, 0.12), mortar=(0.32, 0.28, 0.24))


def sample_marble(x, y, w, h):
    n = fbm(x / w, y / h, 12.0, 6, 4.0)
    vein = abs(math.sin((x / w) * 9.0 + n * 8.0))
    col = _mix3((0.82, 0.78, 0.70), (0.62, 0.58, 0.52), n)
    col = _mix3(col, (0.45, 0.40, 0.34), max(0.0, 0.18 - vein) * 1.5)
    return (*col, 0.42 + n * 0.12, n * 0.35)


def sample_pale_wood(x, y, w, h):
    grain = math.sin((x / w) * 22.0 + fbm(x / w, y / h, 2.0, 4, 14.0) * 5.0)
    n = fbm(x / w, y / h, 8.0, 4, 7.0)
    col = _mix3((0.62, 0.48, 0.30), (0.42, 0.30, 0.16), n * 0.5 + grain * 0.2 + 0.2)
    return (*col, 0.7 + n * 0.1, n * 0.35)


SAMPLERS = {
    "stone_brick": sample_brick,
    "plaster": sample_plaster,
    "slate": sample_slate,
    "gold": sample_gold,
    "wood": sample_wood,
    "leather": sample_leather,
    "cloth": sample_cloth,
    "iron": sample_iron,
    "skin": sample_skin,
    "bark": sample_bark,
    "leaf": lambda x, y, w, h: sample_leaf(x, y, w, h),
    "leaf_dark": lambda x, y, w, h: sample_leaf(x, y, w, h, (0.12, 0.28, 0.10)),
    "grass": sample_grass,
    "crystal": sample_crystal,
    "cloth_deep": lambda x, y, w, h: sample_cloth(x, y, w, h, (0.38, 0.22, 0.14)),
    "cloth_purple": lambda x, y, w, h: sample_cloth(x, y, w, h, (0.42, 0.22, 0.55)),
    "cloth_green": lambda x, y, w, h: sample_cloth(x, y, w, h, (0.32, 0.48, 0.22)),
    "cloth_blue": lambda x, y, w, h: sample_cloth(x, y, w, h, (0.22, 0.42, 0.58)),
    "cloth_sun": lambda x, y, w, h: sample_cloth(x, y, w, h, (0.85, 0.62, 0.18)),
    "ice": sample_ice,
    "glass": sample_glass,
    "steel": sample_steel,
    "dark_stone": sample_dark_stone,
    "red_brick": sample_red_brick,
    "marble": sample_marble,
    "pale_wood": sample_pale_wood,
}


def write_maps(out_dir: Path, name: str, sampler, size=1024):
    out_dir.mkdir(parents=True, exist_ok=True)
    albedo, rough, nrm = fill_maps(size, size, sampler)
    return albedo, rough, nrm, size
