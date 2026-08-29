# Asterra — Multiplayer (Phase 3)

## Stack

- Unity Authentication (anonymous for now)
- Lobby (create/join by code)
- Relay (DTLS) + Netcode for GameObjects `UnityTransport`
- Lockstep command frames via `CommandCodec` / `LockstepNetworkBridge`
- Readiness via `LockstepFrameGate` (all players submit per tick, empty allowed)

## Editor soak (no UGS required)

Headless path exercises the same gate / dual-sim / desync / loopback session logic online would use:

1. Menu **Asterra → Run Lockstep Soak (2→8)** (or batchmode `-executeMethod Asterra.Editor.LockstepSoakMenu.RunFromCommandLine`).
2. Smoke already includes `LockstepSoakSelfTest` + `DualSimSoakSelfTest`.

`MultiplayerSessionHost` defaults to **LocalLoopback** (`IsConnected = true`). Switch backend to **UnityGamingServicesStub** only when restoring live Lobby/Relay packages.

## Scene setup (Editor)

1. Empty object with `NetworkManager` + `UnityTransport` (needed for real NGO only).
2. Same object (or child): `MultiplayerSessionHost` (loopback by default), optional `LockstepNetworkBridge` (on a NetworkObject).
3. Gameplay object: `MatchBootstrap`, `LockstepMatchCoordinator`, optional `SimPresentationBridge`.
4. UI object: `MultiplayerMenu` referencing the session host.

## Flow

### Offline
1. Open `Skirmish.unity` → Play → offline menu → Start.
2. Builds lobby seats for P0/P1, populates world from sorted slots, starts `LockstepMatchCoordinator`.
3. AI seats feed frames via `ArmyBrainFrameContributor`.

### Online (loopback soak today / UGS later)
1. Host/Join via `MultiplayerSessionHost`.
2. Clients `MatchLobbyController.ClaimLocalSlot` → pick faction → ready.
3. Host `HostStart` → `MatchLobbyNetworkBridge` StartMatch → `MatchBootstrap.StartOnlineFromLobby`.
4. All peers populate from the same `MatchStartInfo.Players` order (west = lowest player id).
5. `LockstepMatchCoordinator` runs gated ticks + hash RPCs.

## Offline / package status

`UnityGamingServicesSession` remains a **compile-safe stub** (Host/Join do not set connected). Prefer `LocalLoopbackSession` / `LoopbackSession` until Lobby + Relay resolve cleanly on Unity 6.3.

When restoring live UGS:
1. Package Manager → confirm **Multiplayer Services / Lobby / Relay / Authentication** installed.
2. Restore live UGS host/join wiring (see git history for `UnityGamingServicesSession`).
3. Add `Unity.Services.*` refs back to `Asterra.Net.asmdef`.
4. Set `MultiplayerSessionHost` backend to UGS (or replace stub).
