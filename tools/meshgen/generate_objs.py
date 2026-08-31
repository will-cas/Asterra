#!/usr/bin/env python3
"""Regenerate Asterra low-poly OBJ meshes for Blender/Unity.

Shapes match runtime AsterraMeshLibrary silhouettes (readable at RTS camera distance).
Edit in Blender after import; re-export Wavefront .obj back into Assets/Asterra/Shared/Art/Meshes.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Asterra/Shared/Art/Meshes'
OUT.mkdir(parents=True, exist_ok=True)


def write_obj(name, verts, faces, comment):
    path = OUT / f'{name}.obj'
    lines = [f'# Asterra {comment}', f'o {name}']
    for x, y, z in verts:
        lines.append(f'v {x:.5f} {y:.5f} {z:.5f}')
    for f in faces:
        lines.append('f ' + ' '.join(str(i + 1) for i in f))
    path.write_text('\n'.join(lines) + '\n')
    print('wrote', path.relative_to(ROOT), f'({len(verts)}v)')


def box(cx, cy, cz, sx, sy, sz):
    hx, hz = sx / 2, sz / 2
    v = [
        (cx - hx, cy, cz - hz), (cx + hx, cy, cz - hz),
        (cx + hx, cy, cz + hz), (cx - hx, cy, cz + hz),
        (cx - hx, cy + sy, cz - hz), (cx + hx, cy + sy, cz - hz),
        (cx + hx, cy + sy, cz + hz), (cx - hx, cy + sy, cz + hz),
    ]
    f = [
        (0, 1, 2), (0, 2, 3), (4, 7, 6), (4, 6, 5),
        (0, 4, 5), (0, 5, 1), (3, 2, 6), (3, 6, 7),
        (0, 3, 7), (0, 7, 4), (1, 5, 6), (1, 6, 2),
    ]
    return v, f


def merge(parts):
    verts, faces, off = [], [], 0
    for v, f in parts:
        verts.extend(v)
        faces.extend([tuple(i + off for i in tri) for tri in f])
        off += len(v)
    return verts, faces


def emit(name, parts, comment):
    write_obj(name, *merge(parts), comment)


# --- Units ---
emit('unit_militia', [
    box(0, 0, 0, 0.75, 1.35, 0.55),
    box(0, 1.35, 0, 0.48, 0.48, 0.48),
    box(-0.55, 0.55, 0.05, 0.55, 0.7, 0.12),
    box(0.5, 0.75, 0, 0.14, 0.14, 1.35),
], 'infantry shield+spear')

emit('unit_builder', [
    box(0, 0, 0, 0.7, 1.15, 0.55),
    box(0, 1.15, 0, 0.42, 0.42, 0.42),
    box(0.7, 0.55, 0, 1.05, 0.16, 0.16),
    box(1.15, 0.55, 0, 0.35, 0.55, 0.28),
    box(-0.45, 0.35, 0.2, 0.35, 0.35, 0.35),
], 'builder with hammer')

emit('unit_archer', [
    box(0, 0, 0, 0.5, 1.4, 0.42),
    box(0, 1.4, 0, 0.38, 0.38, 0.38),
    box(0.05, 0.85, 0.55, 0.1, 1.15, 0.1),
    box(0.05, 0.85, -0.55, 0.1, 1.15, 0.1),
    box(0.05, 1.35, 0, 0.08, 0.08, 1.1),
    box(0.05, 0.35, 0, 0.08, 0.08, 1.1),
    box(0.45, 0.9, 0, 0.55, 0.08, 0.08),
], 'archer with bow')

emit('unit_cavalry', [
    box(0, 0.05, 0, 1.55, 0.65, 0.55),
    box(0.7, 0.55, 0, 0.4, 0.4, 0.4),
    box(-0.15, 0.7, 0, 0.55, 0.85, 0.42),
    box(-0.15, 1.5, 0, 0.38, 0.35, 0.38),
    box(0.95, 0.15, 0, 0.25, 0.2, 0.2),
    box(-0.7, 0.0, 0.22, 0.18, 0.35, 0.18),
    box(-0.7, 0.0, -0.22, 0.18, 0.35, 0.18),
    box(0.55, 0.0, 0.22, 0.18, 0.35, 0.18),
    box(0.55, 0.0, -0.22, 0.18, 0.35, 0.18),
], 'cavalry horse+rider')

emit('unit_siege', [
    box(0, 0.35, 0, 1.6, 0.5, 1.0),
    box(0, 0.85, 0, 0.75, 0.65, 0.75),
    box(0.35, 1.15, 0, 1.35, 0.16, 0.16),
    box(0.95, 1.25, 0, 0.35, 0.35, 0.35),
    box(-0.65, 0.0, 0.55, 0.35, 0.35, 0.18),
    box(-0.65, 0.0, -0.55, 0.35, 0.35, 0.18),
    box(0.65, 0.0, 0.55, 0.35, 0.35, 0.18),
    box(0.65, 0.0, -0.55, 0.35, 0.35, 0.18),
], 'siege wagon')

emit('unit_dryad', [
    box(0, 0, 0, 0.55, 1.6, 0.45),
    box(0, 1.55, 0, 0.85, 0.4, 0.85),
    box(0, 1.9, 0, 0.45, 0.35, 0.45),
], 'concord spearman')

emit('unit_ember_raider', [
    box(0, 0, 0, 0.85, 1.3, 0.55),
    box(0, 1.25, 0, 0.42, 0.42, 0.42),
    box(-0.6, 1.0, 0, 0.4, 0.28, 0.55),
    box(0.6, 1.0, 0, 0.4, 0.28, 0.55),
], 'flame warrior')

emit('unit_leader', [
    box(0, 0, 0, 0.85, 1.55, 0.6),
    box(0, 1.55, 0, 0.55, 0.55, 0.55),
    box(0, 2.05, 0, 0.25, 0.55, 0.25),
    box(-0.15, 0.7, -0.45, 0.95, 1.2, 0.12),
    box(0.65, 0.95, 0, 0.18, 0.18, 1.5),
    box(0.65, 1.55, 0.55, 0.35, 0.55, 0.12),
], 'faction leader')

emit('unit_mage', [
    box(0, 0, 0, 0.55, 1.45, 0.5),
    box(0, 1.45, 0, 0.7, 0.35, 0.7),
    box(0, 1.8, 0, 0.35, 0.45, 0.35),
    box(0.7, 0.85, 0, 0.18, 1.4, 0.18),
    box(0.7, 1.55, 0, 0.35, 0.35, 0.35),
], 'fire mage')

# --- Buildings ---
emit('building_keep', [
    box(0, 0, 0, 8.2, 2.6, 8.2),
    box(0, 2.6, 0, 5.2, 6.2, 5.2),
    box(0, 8.6, 0, 2.6, 2.4, 2.6),
    box(0, 2.2, 4.4, 3.2, 3.4, 1.4),
    box(-3.4, 8.2, -3.4, 1.6, 2.2, 1.6),
    box(3.4, 8.2, -3.4, 1.6, 2.2, 1.6),
    box(-3.4, 8.2, 3.4, 1.6, 2.2, 1.6),
    box(3.4, 8.2, 3.4, 1.6, 2.2, 1.6),
    box(0, 10.8, 0, 0.35, 1.6, 0.35),
], 'fortress keep')

emit('building_producer', [
    box(0, 0, 0, 6.2, 2.6, 4.8),
    box(0, 2.6, 0, 4.6, 1.4, 3.6),
    box(0, 3.8, 0, 6.6, 0.5, 1.0),
    box(-2.6, 0, -1.8, 1.2, 4.6, 1.2),
    box(2.6, 0, -1.8, 1.2, 4.6, 1.2),
    box(0, 0.15, 2.6, 2.2, 2.2, 0.4),
    box(-1.8, 0.1, 1.6, 0.8, 1.1, 0.8),
    box(1.8, 0.1, 1.6, 0.8, 1.1, 0.8),
], 'barracks / hall / forge')

emit('building_tower', [
    box(0, 0, 0, 2.8, 1.8, 2.8),
    box(0, 1.8, 0, 1.8, 9.2, 1.8),
    box(0, 10.8, 0, 2.8, 1.1, 2.8),
    box(0, 11.8, 0, 1.3, 1.5, 1.3),
    box(0, 13.2, 0, 0.35, 1.3, 0.35),
    box(-1.1, 10.9, -1.1, 0.7, 1.0, 0.7),
    box(1.1, 10.9, 1.1, 0.7, 1.0, 0.7),
], 'watchtower')

emit('building_turret', [
    box(0, 0, 0, 2.2, 1.2, 2.2),
    box(0, 1.2, 0, 1.4, 3.8, 1.4),
    box(0, 4.8, 0, 2.0, 0.7, 2.0),
    box(0.9, 5.0, 0, 1.6, 0.35, 0.35),
], 'keep turret')

emit('building_wall', [
    box(0, 0, 0, 11, 3.6, 1.4),
    box(-4.5, 3.6, 0, 0.9, 1.4, 0.9),
    box(-1.5, 3.6, 0, 0.9, 1.6, 0.9),
    box(1.5, 3.6, 0, 0.9, 1.4, 0.9),
    box(4.5, 3.6, 0, 0.9, 1.6, 0.9),
    box(0, 1.4, 0.55, 2.2, 1.6, 0.25),
], 'palisade')

emit('building_outpost', [
    box(0, 0, 0, 4.0, 1.8, 4.0),
    box(0, 1.8, 0, 2.6, 3.0, 2.6),
    box(0, 4.8, 0, 0.4, 3.4, 0.4),
    box(0.85, 7.0, 0, 1.7, 1.0, 0.14),
    box(0.2, 7.7, 0, 0.3, 0.3, 0.3),
    box(-1.4, 0.1, 1.4, 0.9, 1.2, 0.9),
], 'outpost')

emit('resource_gold', [
    box(0, 0, 0, 1.5, 0.85, 1.2),
    box(0.55, 0.75, 0.2, 0.95, 1.0, 0.8),
    box(-0.5, 0.6, -0.3, 0.8, 0.85, 0.7),
    box(0.05, 1.45, 0, 0.6, 0.7, 0.55),
    box(-0.15, 1.9, 0.1, 0.35, 0.45, 0.35),
], 'gold crystals')

emit('resource_timber', [
    box(0, 0.4, 0, 2.6, 0.75, 0.75),
    box(-1.05, 0.0, 0, 0.6, 0.8, 0.6),
    box(1.05, 0.0, 0, 0.6, 0.8, 0.6),
    box(0.25, 0.95, 0.2, 1.5, 0.5, 0.5),
    box(-0.2, 1.2, -0.15, 0.9, 0.35, 0.35),
], 'timber logs')

# --- Map scenery (non-interactive) ---
emit('scenery_farm', [
    box(0, 0, 0, 5.2, 2.2, 4.4),
    box(0, 2.2, 0, 5.6, 1.1, 4.8),
    box(2.0, 2.6, -0.4, 0.7, 1.6, 0.7),
    box(0, 0.15, -3.6, 6.2, 0.7, 2.4),
    box(1.8, 0.7, -3.5, 1.5, 0.9, 1.2),
], 'farmhouse and pen')

emit('scenery_crumbling_tower', [
    box(0, 0, 0, 3.6, 2.4, 3.6),
    box(0, 2.4, 0, 3.0, 2.8, 3.0),
    box(-0.2, 5.2, 0.15, 2.2, 2.2, 2.0),
    box(0.4, 7.2, -0.2, 1.4, 1.6, 1.2),
    box(-0.9, 2.0, 1.4, 0.7, 1.8, 0.55),
    box(1.2, 0.4, 1.3, 0.9, 0.7, 0.8),
], 'crumbling tower')

emit('scenery_cottage', [
    box(0, 0, 0, 3.6, 1.8, 3.2),
    box(0, 1.8, 0, 4.0, 0.95, 3.6),
    box(1.4, 2.2, -0.9, 0.45, 1.1, 0.45),
    box(0, 0.7, 1.6, 1.1, 1.2, 0.2),
], 'cottage')

emit('scenery_mill', [
    box(0, 0, 0, 3.4, 2.0, 3.4),
    box(0, 2.0, 0, 2.4, 3.4, 2.4),
    box(0, 5.4, 0, 1.6, 1.2, 1.6),
    box(1.8, 4.4, 0, 0.25, 2.4, 0.25),
    box(1.8, 5.5, 0, 2.6, 0.2, 0.2),
    box(1.8, 5.5, 0, 0.2, 0.2, 2.6),
], 'windmill')

emit('scenery_shrine', [
    box(0, 0, 0, 3.2, 0.45, 3.2),
    box(0, 0.45, 0, 2.2, 0.35, 2.2),
    box(0, 0.8, 0, 0.7, 2.4, 0.7),
    box(0, 3.2, 0, 1.4, 0.35, 1.4),
    box(-1.3, 0.2, -1.3, 0.35, 1.4, 0.35),
    box(1.3, 0.2, -1.3, 0.35, 1.4, 0.35),
    box(-1.3, 0.2, 1.3, 0.35, 1.4, 0.35),
    box(1.3, 0.2, 1.3, 0.35, 1.4, 0.35),
], 'wayside shrine')

emit('scenery_barn', [
    box(0, 0, 0, 6.4, 2.6, 4.2),
    box(0, 2.6, 0, 6.8, 1.2, 4.6),
    box(0, 0.9, 2.15, 1.6, 1.6, 0.2),
    box(-2.4, 2.8, 0, 0.5, 1.4, 0.5),
], 'barn')

print('done — open Meshes/*.obj in Blender to sculpt further')
