# unit_royal_peasant

- **Faction:** [mundor_crown](../factions/mundor_crown.md)
- **Role:** unit
- **Status:** captured
- **Author:** `tools/meshgen/build_asterra_art_blend.py` (`build_peasant_rigged`)
- **Mesh:** `Meshes/unit_royal_peasant.obj` / `.fbx`

## Intent

Levy / worker silhouette for Mundor Crown — humble cloth, pack or tools readable at RTS camera, distinct from Legion and Guard.

## Iterate

Keep height and mass lighter than Legion; no heavy shield disc.

## Review stills

Canonical: `Blender/Renders/models/unit_royal_peasant/` (`front`, `three-quarter`, `side`, `rear`, `low`, `detail`, `high`, `top`).

Comparison copies: `Blender/Renders/angles/<front|three-quarter|side|rear|low|detail|high|top>/unit_royal_peasant.png`

## Notes

Special-case export (skipped in `asterra_roster.SKIP_IDS`); use `_export_mundor_roster_m1.py` or `build_asterra_art_blend` peasant path.
