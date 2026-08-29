# Asterra audio (Resources)

Ogg clips loaded by `AsterraAudio` via `Resources.Load("Asterra/Audio/<name>")` only — no procedural synth fallback. Missing clips log an error.

| File | Used for |
| --- | --- |
| `ui_click` | UI click |
| `select` | Selection |
| `order_move` / `order_attack` / `order_build` / `order_gather` | Orders |
| `order_train` / `order_research` | Train / research |
| `hit` / `death` | Combat |
| `build_done` / `deposit` / `capture` | Economy / territory |
| `victory` / `defeat` | End screen |
| `invalid` | Rejected action |
| `music_bed` | Looping music |
| `ambience` | Looping ambience |

All current files are from **Kenney** packs (CC0). See `ThirdParty/CREDITS.md`.
