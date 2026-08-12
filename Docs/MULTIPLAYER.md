# Asterra — Multiplayer (Phase 3)

## Stack

- Unity Authentication (anonymous for now)
- Lobby (create/join by code)
- Relay (DTLS) + Netcode for GameObjects `UnityTransport`
- Lockstep command frames via `CommandCodec` / `LockstepNetworkBridge`
- Readiness via `LockstepFrameGate` (all players submit per tick, empty allowed)

## Scene setup (Editor)

1. Empty object with `NetworkManager` + `UnityTransport`.
2. Same object (or child): `UnityGamingServicesSession`, `MultiplayerSessionHost`, `LockstepNetworkBridge` (on a NetworkObject).
3. Gameplay object: `MatchBootstrap`, `LockstepMatchCoordinator`, optional `SimPresentationBridge`.
4. UI object: `MultiplayerMenu` referencing the session host.

## Flow

1. Host calls `MultiplayerSessionHost.HostSkirmishAsync` → Auth → Relay allocation → Lobby (stores relay + seed) → `StartHost`.
2. Client calls `JoinSkirmishAsync(code)` → join lobby → Relay join → `StartClient`.
3. `LockstepMatchCoordinator.Initialize(...)` with all `PlayerId`s once the roster is known.
4. Each tick: schedule local orders → broadcast frame → gate waits for all players → `IWorldSim` steps → periodic world hash RPC.

## Offline / pre-package

If UGS assemblies are not present, `ASTERRA_UGS` is undefined and session methods log a stub warning. Core lockstep (`CommandCodec`, `LockstepFrameGate`) still runs in local skirmish / smoke tests.
