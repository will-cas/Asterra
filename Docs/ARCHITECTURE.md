# Asterra — Technical Architecture

## Locked targets

| Parameter | Decision |
|-----------|----------|
| Unity | **6.3 LTS** (`6000.3.x`) |
| Units / match | ~**1000** |
| Map scale | ~**1 km** skirmish |
| Factions | **3** (asymmetric) |
| Players | up to **8** |
| First vertical slice | **Skirmish** |
| VCS | **Git** (this repo) |
| Net model | **Deterministic lockstep** (RTS industry standard) over NGO + Relay/Lobby |

## Networking (genre standard)

Classic competitive RTS (StarCraft, AoE, CoH-style) does **not** replicate every unit transform each frame. Instead:

1. **Unity Lobby + Relay + NGO** — session lifecycle, connect, host migration later.
2. **Lockstep command stream** — players submit orders (`Move`, `Attack`, `Build`, `Train`) for tick `N+k`.
3. **All peers simulate the same tick** with a fixed `SimulationTick` (e.g. 20 Hz).
4. **Hash sync** — periodic world-hash RPC; desync → soft reconnect / return to lobby.

NGO `NetworkBehaviour` is used for **match meta** (ready state, faction pick, chat), **not** for 1000 unit NetworkTransforms.

Server-authoritative full-state sync is a fallback path later (spectators, mid-join) — do not build the skirmish slice on it.

## ECS vs GameObjects

| Layer | Approach | Why |
|-------|----------|-----|
| Units (move/combat/perception) | **Entities / DOTS** (`Asterra.Simulation`) | 1k agents, cache-friendly combat, deterministic-friendly math |
| Buildings, resource nodes, commanders | **GameObjects** | Sparse, authored, easier tooling |
| UI / camera / selection / placement | **GameObjects** | Input & presentation |
| Faction / economy / territory rules | **Pure C# services** in `Asterra.Core` / `.Gameplay` | Testable without scene |

**Hybrid bridge:** `UnitPresentationBridge` maps Entity ↔ thin view prefab (selection ring, VFX, audio). Sim stays headless-capable.

At 1k units, pure GO + Animator is survivable for a prototype, but the asmdef split assumes Entities early so we do not rewrite networking around `NetworkObject` per unit.

## Assembly definitions

```
Asterra.Core          — IDs, interfaces, commands, deterministic RNG, pure data
Asterra.Gameplay      — GO gameplay: buildings, resources, factions, commanders, territory
Asterra.Simulation    — DOTS unit sim, path requests, combat jobs
Asterra.Net           — Lobby/Relay/NGO session + lockstep transport
Asterra.AI            — strategy / army group behaviour (consumes Core commands)
Asterra.UI            — HUD, selection, build menus
Asterra.Editor        — custom inspectors / validators
```

Dependency direction: `UI → Gameplay → Core`; `Net → Core`; `Simulation → Core`; `AI → Core`. No cycles. `Gameplay` may reference `Simulation` only via bridge interfaces in Core.

## Key managers / services (not God-singletons)

Prefer **composition root** + interfaces injected at match start. One `MatchBootstrap` MonoBehaviour wires services.

| Service | Responsibility |
|---------|----------------|
| `IMatchSession` | Lobby → in-match lifecycle |
| `ILockstepClock` | Tick index, command delay, pause |
| `ICommandBus` | Queue / broadcast / apply player commands |
| `IWorldSim` | Advance one deterministic tick |
| `IResourceWallet` | Per-player resource balances |
| `ITerritoryMap` | Capture nodes, control %, adjacency |
| `IFactionCatalog` | 3 faction definitions (ScriptableObjects) |
| `IProductionQueue` | Building train queues |
| `ISelectionSystem` | Local-only selection (not networked) |
| `IPathfindingService` | Flow-field / NavMesh query API |

Avoid `DontDestroyOnLoad` static singletons except a thin `AppHost` for scene transitions & Unity Gaming Services init.

## Folder structure

```
Assets/
  Asterra/
    Core/           # asmdef + scripts
    Gameplay/
    Simulation/
    Net/
    AI/
    UI/
    Shared/
      ScriptableObjects/   # Faction, Unit, Building, Resource defs
      Prefabs/
      Art/                 # imported Blender meshes/materials
      Audio/
      Scenes/
        Boot.unity
        MainMenu.unity
        Skirmish.unity
      Settings/            # URP, Input Actions
  ThirdParty/              # optional vendor packages
Packages/manifest.json
ProjectSettings/
Docs/
  ARCHITECTURE.md
  ROADMAP.md
```

## Packages beyond Netcode

| Package | Role |
|---------|------|
| Netcode for GameObjects + Transport | Session / RPCs / lockstep payload |
| Lobby + Relay + Authentication | 8-player matchmaking |
| Entities + Collections + Burst + Mathematics | Unit sim jobs |
| AI Navigation | Coarse building/area NavMesh; flow-fields later for armies |
| Input System | RTS hotkeys, edge pan, box select |
| URP | Target rendering for Steam |
| Analytics | Funnels / balance telemetry |
| Addressables *(add when art volume grows)* | Faction content packs |
| Cinemachine *(optional)* | Commander / cinematic cams |

**Not required day one:** Full Netcode for Entities (use Entities for **local sim**, NGO for **commands**). Revisit if you move to a dedicated server model.

## Multiplayer transport notes

- `CommandCodec` quantizes positions to millimeters and round-trips all Phase-1 command types.
- `LockstepNetworkBridge` serializes `CommandFrame`s over NGO RPCs (no per-unit NetworkObjects).
- `ReplayBuffer` / `DesyncDetector` sit in Core for local + networked use.

- **Slice 1:** Unity NavMesh for small groups + simple steering.
- **Slice 2:** Grid **flow fields** per army destination (RTS-standard at this density).
- Keep path API behind `IPathfindingService` so the swap does not touch combat code.
