# Third-party credits

Asterra uses free (CC0 / public domain) packs. Attribution is not required for CC0, but we credit authors here.

## Audio — Kenney (CC0)

Source: [kenney.nl](https://kenney.nl/assets/category:Audio)

Packs used:

- [Interface Sounds](https://kenney.nl/assets/interface-sounds)
- [UI Audio](https://kenney.nl/assets/ui-audio)
- [RPG Audio](https://kenney.nl/assets/rpg-audio)
- [Impact Sounds](https://kenney.nl/assets/impact-sounds)
- [Music Jingles](https://kenney.nl/assets/music-jingles)
- [Music Loops](https://kenney.nl) (Mission Plausible, Night at the Beach)

Mapped clips live in `Assets/Resources/Asterra/Audio/`. `AsterraAudio` loads these via `Resources.Load` only (no procedural synth).

## Meshes — Quaternius + Kenney (CC0)

### Quaternius

- [Ultimate Fantasy RTS](https://quaternius.com/packs/ultimatefantasyrts.html) — buildings / props (Barracks, Archery Range, Market, Logs). Google Drive folder download was partial (rate limits / permission errors on some files); usable `.blend` assets were exported to OBJ with Blender. Rocks / trees also from [poly.pizza](https://poly.pizza/u/Quaternius) GLB mirrors converted to OBJ.
- [RPG Character Pack](https://quaternius.com/packs/rpgcharacters.html) — fantasy unit meshes (Warrior, Ranger, Wizard, Monk, Cleric, Rogue) scaled into `Assets/Asterra/Shared/Art/Meshes/unit_*.obj`.

### Kenney

- [Nature Kit](https://kenney.nl/assets/nature-kit) — trees, rocks
- [Castle Kit](https://kenney.nl/assets/castle-kit) — walls, towers, bridges, siege catapult
- [Fantasy Town Kit](https://kenney.nl/assets/fantasy-town-kit) — modular town pieces (kept under `ThirdParty/`)

Runtime OBJs: `Assets/Asterra/Shared/Art/Meshes/<key>.obj`. `AsterraMeshLibrary` loads via `ObjMeshLoader` only (no procedural mesh builders).

## Icons — Kenney (CC0)

- [Board Game Icons](https://kenney.nl/assets/board-game-icons)
- [Game Icons](https://kenney.nl/assets/game-icons)

White tint-friendly PNGs in `Assets/Resources/Asterra/Icons/`. `HudStyle` multiplies by accent when caching.

## Cursors — Kenney (CC0)

- [Cursor Pack](https://kenney.nl/assets/cursor-pack)

Files in `Assets/Resources/Asterra/Cursors/` (`select`, `move`, `attack`, `build`, `invalid`, `train`, `gather`). `RtsCursorController` loads these only (no procedural cursor drawing).

## License note

CC0 = public domain dedication. Free for commercial use; no attribution required. Prefer keeping this CREDITS file updated when adding packs.
