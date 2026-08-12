# Asterra — Implementation Roadmap

Skirmish-first vertical slice. Persist campaign / world mutation **after** multiplayer skirmish feels good.

## Phase 0 — Project hygiene (done in repo scaffold)

- [x] Unity 6.3 LTS project stub + Git
- [x] Asmdefs + Core interfaces
- [x] Package manifest (NGO, Lobby, Relay, Entities, Input, URP)
- [ ] Open in Hub, let Unity regenerate `.meta` / resolve packages
- [ ] Steamworks.NET / Facepunch Steamworks (defer until build pipeline)

## Phase 1 — Local skirmish sandbox (playable solo)

1. Boot → Skirmish scene, fixed 1 km-ish play space, 3 spawn points.
2. One faction playable: place 1 building, train 1 unit type.
3. Select / move units (local commands → `IWorldSim`).
4. One enemy dummy camp (scripted, not full AI).
5. Capture **one** territory node; tick **one** resource type.
6. One upgrade choice that buffs production or unit stats (choice → world flag).

**Exit criteria:** 5-minute loop: build → train → fight → capture → gather → upgrade.

## Phase 2 — Deterministic sim + 1k stress

1. Port unit move/combat into Entities (`Asterra.Simulation`).
2. Fixed tick, seeded `DeterministicRandom`.
3. Spawn 1k units, measure frame time / Burst jobs.
4. World-hash for single-player “desync” tests (replay).

## Phase 3 — Multiplayer skirmish (2 → 8)

1. UGS Auth + Lobby + Relay join/host.
2. NGO session; lockstep command RPCs.
3. Faction pick (3 factions), ready-up, start match.
4. 2-player lockstep soak; then 4; then 8.
5. Desync detection UX (hash mismatch → leave).

## Phase 4 — AI & identity

1. Army groups + simple strategy utility AI (`Asterra.AI`).
2. Asymmetric faction data (units/buildings/passives) via ScriptableObjects.
3. Commander kit (1 active + 1 passive) affecting army buffs.

## Phase 5 — Steam & live ops

1. Steam page / app ID, achievements stub.
2. Unity Cloud Build → Steam depot.
3. Analytics events for funnel + balance.
4. Only then: campaign map, persistent world changes, commander progression saves.

## What to build first (short list)

| Order | Deliverable |
|------:|-------------|
| 1 | `MatchBootstrap` + Core command types |
| 2 | Resource wallet + territory node |
| 3 | Building placement + production queue |
| 4 | Unit spawn/move/attack (GO, then Entities) |
| 5 | Selection + camera |
| 6 | Lockstep shell (local multi-peer in editor) |
| 7 | Lobby/Relay host-join |
| 8 | Second & third factions as data only |
