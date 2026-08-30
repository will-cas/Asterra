# Asterra Map Creator

Designers author skirmish maps that sync into the main game via JSON.

## Quick start

1. In Unity: **Asterra → Map Creator**
2. Paint **Terrain** (gameplay types) and **Texture** (grass / dirt / rock / sand overlays), place **Keep W / Keep E**, gold/timber, territory, units
3. Set **Id** / **Name**, click **Save**
4. Play mode → Offline Skirmish → cycle **Map** until you see `Your Map ★`
5. Start skirmish — the custom layout loads through the same lockstep boot path as built-ins

## Sync layout

| Path | Role |
|------|------|
| `Assets/Asterra/Shared/Maps/*.map.json` | Source of truth (git) |
| `Assets/StreamingAssets/Asterra/Maps/*.map.json` | Build/runtime mirror (auto-written on Save) |

Built-in maps (`twin_keeps`, `river_crossing`, `blackridge_pass`) stay in C#. Custom ids must not reuse those names.

## Format (v1)

`MapDefinition` fields: terrain paint ops (`rect` / `disk`), **texturePaint** cosmetic splat overlays (`grass` / `dirt` / `rock` / `sand`), blocked rects, keeps (seat 0/1), units, buildings, resources, territories, destructibles, camera focus.

Unit roles: `basic`, `builder`, `ranged`, `cavalry`, `siege`, `leader`, `boat`, `pathfinder`  
Building roles: `tower`, `wall`, `producer`, `outpost`, `keep`  
Resource types: `gold`, `timber`  
Destructibles: `tree`, `rock`, `bridge`

## Lockstep

All peers must load the **same map id + bytes** before tick 0. Commit Shared/Maps in the same PR as any match that depends on a new custom map.
