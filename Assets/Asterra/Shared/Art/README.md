# Asterra Art

Low-poly meshes live in `Meshes/*.obj` (Unity-importable and Blender-editable).

Runtime fallback: `AsterraMeshLibrary` builds the same shapes in code so the demo works before OBJ import.

Regenerate OBJs:

```bash
python3 tools/meshgen/generate_objs.py
```

In Blender: File → Import → Wavefront (.obj), edit, export back to this folder.
