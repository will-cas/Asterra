# Asterra Art

Unique roster meshes: `Meshes/*.obj` (Blender Y-up) loaded by `AsterraMeshLibrary`.

## Design docs

- Rules and loop: [`Docs/DESIGN_RULES.md`](Docs/DESIGN_RULES.md)
- Queue: [`Docs/STATUS.md`](Docs/STATUS.md)
- Catalog: [`Docs/INDEX.md`](Docs/INDEX.md)
- Factions: `Docs/factions/`
- Per mesh: `Docs/models/`

## Review stills

Canonical files: `Blender/Renders/models/<id>/<camera>.png`

Cameras: `front`, `three-quarter`, `side`, `rear`, `low`, `detail`, `high`, `top`.

```bash
/Applications/Blender.app/Contents/MacOS/Blender --background --python tools/meshgen/export_art_review.py -- --only unit_royal_legion
```

```bash
/Applications/Blender.app/Contents/MacOS/Blender --background --python tools/meshgen/export_art_review.py -- --kind keeps
```

Refresh catalog Intent text without wiping Notes / iterate / done:

```bash
python3 tools/meshgen/write_art_docs.py
```

## Low-poly fallback (do not run on unique units)

```bash
python3 tools/meshgen/generate_objs.py
```
