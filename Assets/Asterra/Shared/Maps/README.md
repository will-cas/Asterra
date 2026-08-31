# Asterra Map Creator

Designers author skirmish maps in a **3D Scene View world**. The palette lives in **Asterra → Map Creator**; sculpting and placement happen on the terrain mesh.

## Quick start

1. Unity: **Asterra → Map Creator** (palette + isolated 3D preview in the Scene View). Tools also appear as an overlay.
2. Orbit **Alt+LMB**, pan **MMB**, zoom the scroll wheel
3. **Raise / Lower** sculpt a real heightfield (smooth falloff). **Terrain** / **Texture** paint gameplay types and splats
4. Place keeps, **tower / wall / producer / outpost**, trees, rocks, bridges, **farm / crumbling tower / cottage / mill / shrine / barn** (scenery: blocks movement, cannot be attacked), units. **Q/E** rotate 15°, **[ ]** 90°
5. **Move** to drag; **Delete** removes selection; **Undo / Redo** (Cmd+Z / Shift+Cmd+Z)
6. Set **Id** / **Name**, **Save**
7. Play → Offline Skirmish → cycle **Map** until you see `Your Map ★`

## Sync layout

| Path | Role |
|------|------|
| `Assets/Asterra/Shared/Maps/*.map.json` | Source of truth (git) |
| `Assets/StreamingAssets/Asterra/Maps/*.map.json` | Build/runtime mirror (auto-written on Save) |

Built-in maps (`mundor_capital`, `outcast_camp`, `river_crossing`, `frozen_wastes`, `lush_forest`, `twin_cities`, `ancient_relic`) stay in C#. Custom ids must not reuse those names.

## Format (v1)

`MapDefinition` fields: terrain paint, **heightPaint** (additive height disks), **texturePaint**, blocked rects, keeps, units, buildings (`yawDegrees`), resources, territories, destructibles (`yawDegrees`), camera focus.

Unit roles: `basic`, `builder`, `ranged`, `cavalry`, `siege`, `leader`, `boat`, `pathfinder`  
Building roles: `tower`, `wall`, `producer`, `outpost`, `keep`  
Resource types: `gold`, `timber`  
Destructibles: `tree`, `rock`, `bridge`  
Scenery (invulnerable, movement-blocking dress): `farm`, `crumbling_tower`, `cottage`, `mill`, `shrine`, `barn`

Objectives (place with **Objective** tool): `destroy_keeps`, `hold`, `optional_hold`, `reach`, `destroy_near`, `survive` (Hold seconds), `protect` (building in the ring — required protect fails the match if it dies). Required reach / destroy_near / survive can end a match if the map has no keep/hold primary. **Campaign matches only** — skirmish ignores objectives and does not show the HUD list.

Conversations: add lines in the palette (shared `id`), place **Talk** enter-zones, or `when=start` in JSON. Campaign missions also play opening talk at match start. **The match pauses** (no sim, no orders) until you Continue / Space / Esc through the box.

## Lockstep

All peers must load the **same map id + bytes** before tick 0. Commit Shared/Maps in the same PR as any match that depends on a new custom map.
