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
4. [x] `SkirmishOpponentBrain` scripted opponent
5. [x] Territory capture + gold income tick
6. [x] `upgrade_militia_training` (faster train + damage buff)
7. [x] `SkirmishSmokeTest` headless driver (enable `runSmokeOnAwake` on `MatchBootstrap`)
8. [x] Three faction rosters as data (`FactionDefaultContent`)
9. [x] `CommandCodec` + `ReplayBuffer` + `DesyncDetector` + NGO bridge wiring
10. [x] Create `Skirmish.unity`, ground, camera; smoke off for play (`Asterra/Build Skirmish Scene`)

**Exit criteria:** 5-minute loop: build → train → fight → capture → gather → upgrade.

Builder world mutations (persistent in-match + save format v3 terrain cells): dig trenches (`J` / DigTrench), chop trees (RMB on props), faction timber bridges (`V`) with demolish.

## Phase 2 — Deterministic sim + 1k stress

1. [x] DOTS unit components + `UnitMoveSystem` / combat cooldown + strike + `DotsWorldHash` (`Asterra.Simulation`)
2. Port remaining combat from `SkirmishWorldSim` into Entities jobs
3. Spawn 1k units, measure frame time / Burst jobs
4. [x] World-hash path for dual-sim / lockstep soak (`DotsWorldHash` + `LockstepSoakSelfTest`)

## Phase 3 — Multiplayer skirmish (2 → 8)

1. [x] UGS Auth + Lobby + Relay session service (`UnityGamingServicesSession`)
2. [x] NGO lockstep bridge + `LockstepMatchCoordinator` + frame gate
3. [x] Faction pick / ready-up protocol (`MatchLobbyState` + `MatchLobbyController`)
4. [x] Unified offline+online path through `MatchBootstrap` + coordinator
5. [x] Headless lockstep soak 2→8 (`LockstepSoakSelfTest` + loopback session + **Asterra → Run Lockstep Soak**); live NGO Editor peers still open when UGS packages restore
6. [x] Desync detection hooks (`DesyncDetector` + hash RPC)

## Phase 4 — AI & identity

1. [x] Army groups + utility scoring (`ArmyGroup` / `ArmyGroupUtility` on `SkirmishOpponentBrain`)
2. [x] Faction ScriptableObject wrappers (`FactionDefinition` + `PowerDefinition` + roster fallback)
3. [x] Commander kit (passive unlock + actives) in defs/sim/HUD

## Phase 5 — Steam & live ops

1. Steam page / app ID, achievements stub.
2. Unity Cloud Build → Steam depot.
3. Analytics events for funnel + balance.
4. Only then: campaign map, persistent world changes, commander progression saves.
