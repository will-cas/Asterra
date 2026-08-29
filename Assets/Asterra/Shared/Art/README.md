# Asterra Art

Low-poly meshes live in `Meshes/*.obj` (Unity-importable and Blender-editable).

Runtime: `AsterraMeshLibrary` prefers these OBJs via `ObjMeshLoader`, then falls back to code-built silhouettes.

## Regenerate base shapes

```bash
python3 tools/meshgen/generate_objs.py
```

## Blender art pass (bevel + detail)

Requires Blender on PATH or at `/Applications/Blender.app`:

```bash
/Applications/Blender.app/Contents/MacOS/Blender --background --python tools/meshgen/blender_art_pass.py
```

Then open any `Meshes/*.obj` in Blender to sculpt further (File → Import → Wavefront). Export back to the same folder.
