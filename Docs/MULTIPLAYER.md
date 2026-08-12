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

### Offline
1. `MatchBootstrap` (play mode OfflineVsAi) auto-starts.
2. Builds lobby seats for P0/P1, populates world from sorted slots, starts `LockstepMatchCoordinator`.
3. AI seats feed frames via `ArmyBrainFrameContributor`.

### Online
1. Host/Join via `MultiplayerSessionHost` (see session objects above).
2. Clients `MatchLobbyController.ClaimLocalSlot` → pick faction → ready.
3. Host `HostStart` → `MatchLobbyNetworkBridge` StartMatch → `MatchBootstrap.StartOnlineFromLobby`.
4. All peers populate from the same `MatchStartInfo.Players` order (west = lowest player id).
5. `LockstepMatchCoordinator` runs gated ticks + hash RPCs.

## Offline / package status

`UnityGamingServicesSession` is currently a **compile-safe stub** so offline 1v1 works even if Lobby/Relay assemblies fail to resolve on Unity 6.3.

When you are ready for online:
1. Package Manager → confirm **Multiplayer Services / Lobby / Relay / Authentication** installed.
2. Restore live UGS host/join wiring (see git history for `UnityGamingServicesSession`).
3. Add `Unity.Services.*` refs back to `Asterra.Net.asmdef`.
