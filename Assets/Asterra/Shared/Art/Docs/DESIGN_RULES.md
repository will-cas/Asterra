# Asterra mesh design rules

Use this when authoring or iterating a keep, building, unit, or prop.

- **Queue:** [STATUS.md](STATUS.md)
- **Catalog:** [INDEX.md](INDEX.md)
- **Faction notes:** `factions/` (plus `world.md` for map dressing)
- **Per mesh:** `models/<id>.md`

## Capture layout

Canonical stills: `Blender/Renders/models/<id>/<camera>.png`

Cameras: `front`, `three-quarter`, `side`, `rear`, `low`, `detail`, `high`, `top`.

Comparison copies (generated from canonical): `Blender/Renders/angles/<camera>/<id>.png`

## Loop

1. Open [STATUS.md](STATUS.md), pick an id.
2. Read the faction (or world) file, then `models/<id>.md`.
3. Look at `Blender/Renders/models/<id>/`.
4. Write defects under **Notes**. Set **Status** to `iterate` (or `done`).
5. Edit the author Python, then:

```bash
/Applications/Blender.app/Contents/MacOS/Blender --background --python tools/meshgen/export_art_review.py -- --only <definition_id>
```

6. Stills replace in place. `write_art_docs.py` will not wipe Notes, `iterate`, or `done`.

Walk a slice (skips ids that already have eight cameras):

```bash
/Applications/Blender.app/Contents/MacOS/Blender --background --python tools/meshgen/export_art_review.py -- --kind keeps
```

`--only` or `--kind` is required. A bare run does nothing.

## Look (all meshes)

- High fantasy that reads as real, not a cartoon toy and not a photoreal scan dump.
- Unique silhouette per definition id. No shared hall kit, no hat-swap infantry.
- Metals are restrained brass / iron. Gold is a material, not a vertex-tint afterthought.
- Glass and crystal are transmissive (ice-sapphire crystal, pale glass).

## Buildings

- Landmark towers are tall stone shafts with a function at the top — never a squat drum plus a gold ball.
- Coursed masonry, string courses, buttresses, slit windows, walks, rails, courtyard or steps.
- Window frames are iron (or faction metal), not gold shutters.
- Banners belong on keeps. `_earth()` is a no-op; do not drop a ground slab into the game mesh.

## Units

- Author in `asterra_units.py` / `asterra_unique_humans.py`.
- Must read at RTS distance: weapon, headgear, and stance in `front` and `detail`.
- Beasts, engines, boats, and constructs are not humans with a prop glued on.
- Do not run `generate_objs.py` on existing unique unit meshes.

## Props, resources, scenery

- Authored: `prop_tree`, `prop_rock`, `prop_bridge`, `resource_gold`, `resource_timber`.
- `scenery_*` are map placeholders until a unique art pass. Screenshots only; do not overwrite those OBJs from the review exporter.

## What to avoid

- Height-based albedo paint or “fantasy remap” vertex colours.
- Toy gyroscopes, NASA brick cylinders, plastic octagons, gold as the whole silhouette.
- Remapping one unit mesh onto another id.

## Runtime

- Unity loads `Meshes/<id>.obj` via `AsterraMeshLibrary.TryExact`.
- LitPBR is texture-first. Team dye stays a light vertex mix.
- Keep `UsePass` names on Unity 6 Lit: `ShadowCaster`, `DepthOnly`, `DepthNormals`.
- Presentation-only motion. Do not put look into lockstep sim.
