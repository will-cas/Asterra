# Asterra — Implementation Roadmap

Skirmish-first vertical slice. Persist campaign / world mutation **after** multiplayer skirmish feels good.

## Phase 0 — Project hygiene

- [x] Unity 6.3 LTS project stub + Git
- [x] Asmdefs + Core interfaces
- [x] Package manifest (NGO, Lobby, Relay, Entities, Input, URP)
- [ ] Open in Hub, let Unity regenerate `.meta` / resolve packages
- [ ] Steamworks.NET / Facepunch Steamworks (defer until build pipeline)

## Phase 1 — Local skirmish sandbox (code complete; needs Editor scene)

1. [x] ~1 km play space data + keeps at ±350, center territory (no scene asset yet)
2. [x] Place barracks + train militia via commands / `SkirmishDefaultContent`
3. [x] Select / move / attack through `LocalOrderController` → lockstep bus → `SkirmishWorldSim`
4. [x] `DummyEnemyCamp` scripted opponent
5. [x] Territory capture + gold income tick
6. [x] `upgrade_militia_training` (faster train + damage buff)
7. [x] `SkirmishSmokeTest` headless driver (enable `runSmokeOnAwake` on `MatchBootstrap`)
8. [ ] Create `Skirmish.unity`, ground, camera; flip smoke off for play

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
