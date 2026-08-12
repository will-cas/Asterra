# Asterra

Large-scale fantasy RTS in Unity — territory warfare, asymmetric factions, commander-led armies, and a world shaped by player choices.

**Core loop:** Battle → Capture Territory → Gather Resources → Upgrade Army → Make Choices → Change World

## Locked design targets

| Parameter | Value |
|-----------|-------|
| Unity | **6.3 LTS** (`6000.3.x`) |
| Units / match | ~**1000** |
| Map scale | ~**1 km** skirmish |
| Factions | **3** asymmetric |
| Players | up to **8** |
| First slice | **Skirmish** |
| Multiplayer | **Deterministic lockstep** over NGO + Relay/Lobby |
| VCS | **Git** (this repo) |

## Open in Unity

1. Install **Unity 6.3 LTS** via Hub.
2. Add this project (`Open` → select repo root).
3. Allow package resolve; Unity will create `.meta` files.
4. Open `Docs/ARCHITECTURE.md` and `Docs/ROADMAP.md`.

## Repo layout

```
Assets/Asterra/
  Core/         Shared IDs, commands, interfaces, deterministic RNG
  Gameplay/     Buildings, resources, factions, commanders, MatchBootstrap
  Simulation/   DOTS unit components (Phase 2)
  Net/          NGO lockstep bridge + session placeholders
  AI/           Army brain interface
  UI/           Skirmish HUD shell
Docs/           Architecture + roadmap
```

## Packages (see `Packages/manifest.json`)

Netcode for GameObjects, Lobby, Relay, Authentication, Entities/Burst/Collections/Mathematics, AI Navigation, Input System, URP, Analytics.

## Status

Phase 1 **simulation** is in place (build / train / move / attack / capture / income / upgrade + dummy enemy). Scenes and presentation still need the Editor.

Headless check (needs [.NET 8 SDK](https://dotnet.microsoft.com/download)):

```bash
dotnet run --project tools/Asterra.Smoke -- 2000
```

See `Docs/ARCHITECTURE.md`, `Docs/ROADMAP.md`, `Docs/CONTROLS.md`.

**GitHub:** https://github.com/will-cas/Asterra
