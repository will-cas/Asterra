# Asterra controls

Local offline / skirmish input (keyboard + mouse).

## Selection & camera

- LMB click / drag: select units (Shift/Cmd additive)
- RMB: move / attack / gather / set rally (context)
- Middle-mouse / edge pan via camera rig
- WASD + arrow keys also pan (A/S suppressed while attack-move / place / patrol armed)
- Minimap click: pan camera
- Minimap cyan view box + yellow crosshair: current camera focus / approximate view

## Orders

| Key | Action |
| --- | --- |
| A then click | Attack-move |
| S | Stop |
| P then click | Patrol |
| F | Stance Aggressive |
| G | Stance Defensive |
| H | Stance Hold |
| B | Place producer (barracks / grove / forge) — requires builder selected |
| N | Place watchtower — requires builder selected |
| M | Place palisade wall — requires builder selected |
| O | Place gold mine (passive gold income + vision) — requires builder selected |
| Q / E / [ / ] / scroll | Rotate building ghost 90° while placing (scroll zoom disabled) |
| Esc | Cancel armed order / place mode |
| Shift while placing | Keep place mode after one building |
| . or I | Select idle workers |
| T | Train from selected / auto-pick keep or producer |
| X | Cancel production (when building is producing/queued) |
| C | Capture nearest territory order |
| U | Buy faction upgrade |
| Q | Iron Wall (Aurelian / Lucien Vale) — outside place mode |
| R | Reselect all owned units |

## Control groups

- Ctrl/Cmd + 1–9: assign current selection to group
- 1–9: select group
- Double-tap 1–9: select group and center camera

## Production

- Select a producer / keep, then use train buttons
- Keep → train builders; producer → train combat units
- Repeated train (or Shift while training) queues units
- Queue portraits jump the camera to that building
- Cancel appears only while a building has active/queued production

## Contextual HUD

The bottom panel only shows actions valid for the current selection:

| Context | What appears |
| --- | --- |
| Always | Idle workers button |
| Builder(s) selected (or place mode) | Barracks / Tower / Wall / Gold Mine (or Cancel Build) |
| Combat unit(s) selected | Stop, Aggro / Defend / Hold |
| Keep selected | Builder train |
| Producer selected | Soldier / Archer / Cavalry / Siege train |
| Building producing / queued | Production queue + Cancel |

Context line (top-left) summarizes selection, e.g. “Builder selected — B/N/M/O build”, “Keep selected — train”, “N combat units selected”.
Hotkey hints on the next line also switch with context.
