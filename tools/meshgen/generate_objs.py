#!/usr/bin/env python3
"""Regenerate Asterra low-poly OBJ meshes for Blender/Unity."""
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
    print('wrote', path.relative_to(ROOT))

def box(cx, cy, cz, sx, sy, sz):
    hx, hz = sx / 2, sz / 2
    v = [
        (cx - hx, cy, cz - hz), (cx + hx, cy, cz - hz), (cx + hx, cy, cz + hz), (cx - hx, cy, cz + hz),
        (cx - hx, cy + sy, cz - hz), (cx + hx, cy + sy, cz - hz), (cx + hx, cy + sy, cz + hz), (cx - hx, cy + sy, cz + hz),
    ]
    f = [(0,1,2),(0,2,3),(4,7,6),(4,6,5),(0,4,5),(0,5,1),(3,2,6),(3,6,7),(0,3,7),(0,7,4),(1,5,6),(1,6,2)]
    return v, f

def merge(parts):
    verts, faces, off = [], [], 0
    for v, f in parts:
        verts.extend(v)
        faces.extend([tuple(i + off for i in tri) for tri in f])
        off += len(v)
    return verts, faces

write_obj('unit_militia', *merge([box(0,0,0,0.7,1.4,0.5), box(0,1.4,0,0.45,0.45,0.45), box(0.45,0.7,0,0.15,0.15,1.2)]), 'infantry')
write_obj('unit_dryad', *merge([box(0,0,0,0.55,1.6,0.45), box(0,1.55,0,0.7,0.35,0.7)]), 'dryad')
write_obj('unit_ember_raider', *merge([box(0,0,0,0.8,1.3,0.55), box(0,1.25,0,0.4,0.4,0.4), box(-0.55,1.0,0,0.35,0.25,0.5), box(0.55,1.0,0,0.35,0.25,0.5)]), 'raider')
write_obj('building_keep', *merge([box(0,0,0,6,3,6), box(0,3,0,3.5,5,3.5), box(-1.5,8,-1.5,1.2,1.2,1.2), box(1.5,8,-1.5,1.2,1.2,1.2), box(-1.5,8,1.5,1.2,1.2,1.2), box(1.5,8,1.5,1.2,1.2,1.2)]), 'keep')
write_obj('building_producer', *merge([box(0,0,0,5,2.2,4), box(0,2.2,0,3,1.5,3), box(-2.2,0,-1.5,1,3.5,1), box(2.2,0,-1.5,1,3.5,1)]), 'producer')
