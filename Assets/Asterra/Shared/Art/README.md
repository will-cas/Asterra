# Asterra Art

Low-poly meshes live in `Meshes/*.obj` (Unity-importable and Blender-editable).

Runtime: `AsterraMeshLibrary` loads these OBJs via `ObjMeshLoader` only. Missing keys log an error and return an empty mesh. Free CC0 OBJs only — no procedural mesh fallback.

Sources: Quaternius (Ultimate Fantasy RTS buildings + RPG Character Pack units) and Kenney (Nature Kit / Castle Kit). See `ThirdParty/CREDITS.md`.

Basenames: `unit_*`, `building_*`, `resource_*`, `prop_*`.
