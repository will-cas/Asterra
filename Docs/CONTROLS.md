# Phase 1 — Local controls (dev)

Add `MatchBootstrap` to any scene GameObject. Offline AI skirmish auto-starts (play mode OfflineVsAi). `LocalOrderController` attaches automatically.

| Key | Order |
|-----|--------|
| **B** | Place your faction's producer near the keep |
| **T** | Train your faction's basic unit |
| **M** | Move selected (or all owned) units to map center |
| **C** | Capture order → march to center territory |
| **A** | Attack first hostile unit/building |
| **U** | Buy your faction's basic upgrade |
| **R** | Reselect all owned living units |

On `MatchBootstrap`, set **Player Faction Index** / **Enemy Faction Index** (0 Iron Covenant, 1 Verdant Court, 2 Ashen Legion).

## Lobby (online)

Use `LobbyHud` / `MatchLobbyController`: Claim → Faction → Ready → Host Start (after `MultiplayerMenu` host/join).
