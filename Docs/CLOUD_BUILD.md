# Unity Cloud Build + Analytics (M1 stubs)

## Goal
Stand up Cloud Build for the Asterra vertical slice and stub Analytics for `match_start` / `match_win` / `match_lose`.

## Code stubs (in repo)
- `Assets/Asterra/Gameplay/Analytics/MatchAnalytics.cs` — emits stub logs; set `MatchAnalytics.UseLiveAnalytics = true` and define `ASTERRA_UGS_ANALYTICS` once UGS Analytics is initialized in the player.
- `Assets/Asterra/Gameplay/Analytics/CloudBuildInfo.cs` — reads Cloud Build env vars (`UNITY_CLOUD_BUILD_NUMBER`, etc.) and logs at match bootstrap.
- Hooked from `MatchBootstrap` on match start and match over.

## Unity Dashboard (manual once)
1. Open [Unity Cloud Build](https://cloud.unity.com/) for the Asterra project linked to `will-cas/Asterra`.
2. Create a **Standalone macOS** (and optionally Windows) target pointing at this repo / `main`.
3. Unity version: **6000.3.21f1** (see `ProjectSettings/ProjectVersion.txt`).
4. Advanced options: enable Advanced / Custom build and keep Library cache on.
5. First green build is the M1 acceptance signal for this task.

## Analytics live path (post-stub)
1. Package already listed: `com.unity.services.analytics`.
2. Initialize Unity Services + Analytics at boot (Auth anonymous is fine for M1).
3. Add scripting define `ASTERRA_UGS_ANALYTICS` on player builds.
4. Set `MatchAnalytics.UseLiveAnalytics = true` in a boot config or Inspector toggle.

## Acceptance (M1)
- [x] Stub events fire in Editor logs on match start / win / lose.
- [x] Cloud Build env reader + setup doc present.
- [ ] First Cloud Build target configured in Unity Dashboard (manual).
