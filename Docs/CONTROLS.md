# Phase 1 — Local controls (dev)

Add `MatchBootstrap` to any scene GameObject. `LocalOrderController` attaches automatically.

| Key | Order |
|-----|--------|
| **B** | Place Barracks near player keep |
| **T** | Train Militia from first owned producer |
| **M** | Move selected (or all owned) units to map center |
| **C** | Capture order → march to center territory |
| **A** | Attack first hostile unit/building |
| **U** | Buy Militia Training upgrade |
| **R** | Reselect all owned living units |

Optional: enable **Run Smoke On Awake** on `MatchBootstrap` to log `SkirmishSmokeTest` results to the Console (no play needed beyond entering Play Mode once).
