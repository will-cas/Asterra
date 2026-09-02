#!/usr/bin/env python3
"""Write per-building description markdown under Assets/Asterra/Shared/Art/Docs/models/."""
from __future__ import annotations

from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import art_review_layout as layout

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/Asterra/Shared/Art/Docs/models"
INDEX = ROOT / "Assets/Asterra/Shared/Art/Docs/INDEX.md"
STATUS = ROOT / "Assets/Asterra/Shared/Art/Docs/STATUS.md"

# faction slug, role, one-line, iterate note
MODELS = [
    ("building_arcaneum", "uncrowned", "keep", "Iron-and-glass layered keep with recessed window bays, steel gate, crystal finial.", "Push masonry courses on the shaft; keep the lantern as glass + iron, not a gold cage."),
    ("building_arcane_academy", "uncrowned", "building", "Two dark-stone wings around a crystal court.", "Ashlar the wings; crystal orb should sit in the court, not float as a toy."),
    ("building_blackroot_conservatory", "uncrowned", "building", "Iron greenhouse: glass barrel vault, steel ribs, no house roof.", "Rib spacing and porch should read as a conservatory at RTS distance."),
    ("building_ancient_ruins", "uncrowned", "building", "Broken colonnade and rubble, no roof.", "Keep the missing column and fallen entablature; do not complete it into a temple."),
    ("building_conjuring_hall", "uncrowned", "building", "Round ritual chamber of dark stone with a crystal dome.", "Coursed drum, tighter gate arch, less gold window frames."),
    ("building_high_temple", "uncrowned", "building", "Tall dark-stone spire with a crystal crown.", "Observatory-grade masonry and iron frames. Stay a heretical temple-spire — not a University lantern tower."),
    ("building_portal_gate", "uncrowned", "building", "Standing ring gate with glass throat, not a hall.", "Piers and ring should feel like a machine; crystal spark stays small."),
    ("building_shadowed_gate", "uncrowned", "building", "Dark twin of the portal: iron ring and void cloth.", "Thorn lintel; avoid looking like a copy of the steel portal with a recolour."),
    ("building_watchtower", "uncrowned", "building", "Coursed lookout with a glass lantern and slate cone.", "Add walk rail; iron mullions on the lantern."),
    ("building_palisade", "uncrowned", "building", "Steel-and-glass wall segment with walk.", "Reads as a barrier, not a building. Keep it thin."),
    ("building_outpost", "uncrowned", "building", "Small steel blockhouse with a roof turret.", "Avoid cube-on-cube; break the box with cap, door, merlons."),
    ("building_royal_barracks", "mundor_crown", "building", "Long two-door barracks with chimneys, not a square house.", "Window rhythm and dual doors are the identity."),
    ("building_royal_court", "mundor_crown", "building", "Portico hall of justice with pediment.", "Columns and steps must read from the gate camera."),
    ("building_royal_farm", "mundor_crown", "building", "Barn, silo, and livestock pen.", "Three volumes, not one barn stretched."),
    ("building_royal_outpost_tower", "mundor_crown", "building", "Round stone tower, merlons, slate cone.", "Coursed shaft; iron-framed windows."),
    ("building_royal_wall", "mundor_crown", "building", "Curtain wall with merlons and a postern arch.", "Wall piece, not a keep."),
    ("building_keep_turret", "mundor_crown", "building", "Small wall turret of coursed stone.", "Sister of the outpost tower, shorter."),
    ("building_bridge", "mundor_crown", "building", "Stone piers and a timber deck with rails.", "Deck planks should read in the side shot."),
    ("building_stone_wall", "mundor_crown", "building", "Same language as the royal wall under a second id.", "Keep in sync with building_royal_wall."),
    ("building_outcast_great_camp", "outcast_host", "keep", "Stacked-log longhouse, snow tiles, palisade, leather door.", "Logs to the eave; ice on the ridge; do not masonry this."),
    ("building_outcast_burrows", "outcast_host", "building", "Earth-mound village with a tunnel mouth.", "Mounds, not cottages."),
    ("building_outcast_aerie", "outcast_host", "building", "Timber hut in a three-trunk canopy nest.", "Ladder and deck are required; hut stays small."),
    ("building_outcast_treetop_watch", "outcast_host", "building", "Aerie variant: basket lookout instead of a hut.", "Must stay distinguishable from the aerie in the crown shot."),
    ("building_outcast_village_hall", "outcast_host", "building", "Smaller longhouse than the great camp.", "Same log language, shorter."),
    ("building_outcast_mine", "outcast_host", "building", "Timber headframe over a pit, side hut.", "Headframe is the silhouette."),
    ("building_outcast_ground_works", "outcast_host", "building", "Ice bank with a palisade rail.", "Barrier piece."),
    ("building_freetown_tavern", "freetown", "keep", "Stone ground, jettied timber, slate, dock, hanging sign.", "Dock and jetty are as important as the pub block."),
    ("building_freetown_smugglers_den", "freetown", "building", "Low stone cellar opening onto a dock.", "Cellar + piles + barrels."),
    ("building_freetown_hut", "freetown", "building", "Tiny steep cottage, stone then timber.", "Steep slate is the read."),
    ("building_freetown_black_market", "freetown", "building", "Open stalls under awnings, no closed hall.", "Three awnings; do not enclose them."),
    ("building_freetown_crows_nest", "freetown", "building", "Ship-mast lookout with yard and sail.", "Mast height is the identity."),
    ("building_freetown_barricades", "freetown", "building", "Crate-and-barrel street barricade.", "Cargo wall, not palisade logs."),
    ("building_barricade", "freetown", "building", "Same barricade mesh under a short id.", "Keep in sync with freetown_barricades."),
    ("building_ferry_dock", "freetown", "building", "The pier is the building: quay, planks, shed.", "Side and rear shots must show piles."),
    ("building_university_grand_college", "university_guild", "keep", "Buttressed brick hall, marble portico, clock turret.", "Window rhythm and clock face; not a second observatory."),
    ("building_university_workshop", "university_guild", "building", "Sawtooth factory roof on red brick.", "Teeth must read in the high shot."),
    ("building_university_forbidden_library", "university_guild", "building", "Long stacks under a reading dome.", "Dome is glass/slate, not a gold ball."),
    ("building_university_alchemist", "university_guild", "building", "Chimney cluster and glass vats.", "Vats and stacks, not a house."),
    ("building_university_clockwork_tower", "university_guild", "building", "Octagon keep, coursed shaft, four clock faces, glass lantern.", "Quality bar with the observatory. No toy orrery. Dials need ticks and hands."),
    ("building_university_moat", "university_guild", "building", "Water channel between brick banks.", "Horizontal water read in hero and side."),
    ("building_university_grand_observatory", "university_guild", "building", "Tall octagon keep, shaft, brass hemisphere dome, fork-mounted telescope.", "Reference landmark. Do not flatten into a drum + gold sphere."),
    ("building_university_weather_rods", "university_guild", "building", "Field of rods and a tiny instrument hut.", "The field is the building."),
    ("building_university_far_glass", "university_guild", "building", "Seeing-stone on a coursed limestone pier.", "Next candidate for observatory-grade masonry."),
    ("building_church_grand_temple", "rising_sun", "keep", "Portico, nave, drum, gold dome, sun disc.", "Classical church, not a wizard needle."),
    ("building_church_warrior_monastery", "rising_sun", "building", "Cloister square.", "Courtyard must read in high/top; gate camera should not clip a single wall."),
    ("building_church_sun_temple", "rising_sun", "building", "Round marble rotunda with a gold onion dome.", "Coursed drum; posts should ground, not float."),
    ("building_church_sacred_site", "rising_sun", "building", "Obelisk on a plaza with corner posts.", "Monument, not a tower keep."),
    ("building_church_scorched_tower", "rising_sun", "building", "Blackened broken stone tower.", "Break the crown; soot in material, not a smooth black cylinder."),
    ("building_church_offering_shrine", "rising_sun", "building", "Small aedicule shrine.", "Human-scale; do not scale it like a keep."),
    ("building_church_sacred_walls", "rising_sun", "building", "Sacred precinct wall.", "Wall piece with church trim, not a Mundor copy."),
    ("unit_veiled_apprentice", "uncrowned", "unit", "Hooded scholar with steel staff and glass orb.", "Robe + staff silhouette; orb is glass, not a yellow gem."),
    ("unit_veiled_builder", "uncrowned", "unit", "Veiled labourer with tools.", "Must read as a worker, not a second apprentice."),
    ("unit_veiled_rune_caster", "uncrowned", "unit", "Caster with rune plates and focus.", "Hands and plates must show in the crown shot."),
    ("unit_veiled_elemental", "uncrowned", "unit", "Bound elemental mass, not a human in a robe.", "Volume and crystal, not a peasant scale."),
    ("unit_veiled_golem", "uncrowned", "unit", "Stone-and-iron construct.", "Blocky limbs; taller than infantry."),
    ("unit_veiled_priest_guard", "uncrowned", "unit", "Temple guard in dark plate.", "Shield/weapon read from side."),
    ("unit_veiled_shadow", "uncrowned", "unit", "Low, stretched stalker.", "Stay thin; do not become a second assassin."),
    ("unit_veiled_assassin", "uncrowned", "unit", "Bladed infiltrator.", "Knives and hood; distinct from shadow."),
    ("unit_veiled_massed", "uncrowned", "unit", "Rank-and-file veiled troop.", "Still unique kit, not a hat-swap legion."),
    ("unit_veiled_souling", "uncrowned", "unit", "Small spirit form.", "Tiny; do not scale like a soldier."),
    ("unit_veiled_heir", "uncrowned", "unit", "Faction heir / commander figure.", "Richer cloth and metal; readable at RTS."),
    ("unit_veiled_colossus", "uncrowned", "unit", "Huge veiled war-construct.", "Must dwarf infantry in hero and high."),
    ("unit_veiled_thorn_speaker", "uncrowned", "unit", "Plant-thorn caster.", "Thorns vs steel of the apprentice."),
    ("unit_veiled_night_abbot", "uncrowned", "unit", "Abbot / high priest of the veil.", "Distinct from night-and-heretic."),
    ("unit_veiled_first_heretic", "uncrowned", "unit", "Named heretic champion.", "One-off silhouette."),
    ("unit_veiled_dark_spy", "uncrowned", "unit", "Spy in dark leathers.", "Not a recolour of royal spy."),
    ("unit_veiled_shade", "uncrowned", "unit", "Insubstantial shade.", "Lighter mass than the shadow stalker."),
    ("unit_royal_builder", "mundor_crown", "unit", "Mundor labourer.", "Tools and cloth; not a legionary."),
    ("unit_royal_legion", "mundor_crown", "unit", "Shield-and-spear legionary.", "Shield disc must read in three-quarter and side."),
    ("unit_royal_guard", "mundor_crown", "unit", "Heavier palace guard.", "Richer armour than legion."),
    ("unit_royal_longbow", "mundor_crown", "unit", "Longbow levies.", "Bow length is the identity."),
    ("unit_royal_commander", "mundor_crown", "unit", "Field officer on a distinct kit.", "Not a recolour of the king."),
    ("unit_royal_spy", "mundor_crown", "unit", "Cloaked crown spy.", "Low profile, no plate."),
    ("unit_royal_crown_eye", "mundor_crown", "unit", "Watcher / scout of the crown.", "Optics or spyglass in crown shot."),
    ("unit_royal_pioneer", "mundor_crown", "unit", "Pioneer with pack and tools.", "Pack silhouette."),
    ("unit_royal_onager", "mundor_crown", "unit", "Torsion siege engine.", "Frame + arm; not a cart."),
    ("unit_royal_king", "mundor_crown", "unit", "Crowned monarch.", "Cape and crown at RTS distance."),
    ("unit_royal_legion_marshal", "mundor_crown", "unit", "Marshal of the legion.", "Between commander and king."),
    ("unit_royal_spymaster", "mundor_crown", "unit", "Spymaster, not a line spy.", "Richer cloak, distinct hat/hood."),
    ("unit_royal_tomb_warden", "mundor_crown", "unit", "Tomb warden in heavier funerary kit.", "Not a recolour of the guard."),
    ("unit_royal_justiciar", "mundor_crown", "unit", "Justiciar with rod or book.", "Civic, not a second king."),
    ("unit_pathfinder", "mundor_crown", "unit", "Shared-path Mundor scout.", "Lean kit, bow or staff."),
    ("unit_sapper", "mundor_crown", "unit", "Sapper with cask and pick.", "Explosives pack must read."),
    ("unit_outcast_villager", "outcast_host", "unit", "Host villager in hides.", "Not a Mundor peasant."),
    ("unit_outcast_hobgoblin", "outcast_host", "unit", "Hobgoblin warrior.", "Broader, crouched mass."),
    ("unit_outcast_hunter", "outcast_host", "unit", "Bow hunter.", "Lean, fur, bow."),
    ("unit_outcast_ranger", "outcast_host", "unit", "Ranger, longer kit than hunter.", "Must differ from hunter in crown."),
    ("unit_outcast_beast_rider", "outcast_host", "unit", "Rider on a beast, not a horse knight.", "Mount is the silhouette."),
    ("unit_outcast_frost_giant", "outcast_host", "unit", "Frost giant.", "Huge; ice and bark, not marble."),
    ("unit_outcast_sprite", "outcast_host", "unit", "Tiny flying sprite.", "Do not scale like a soldier."),
    ("unit_outcast_nature_cub", "outcast_host", "unit", "Young beast cub.", "Quadruped, small."),
    ("unit_outcast_sky_eye", "outcast_host", "unit", "Aerial scout creature.", "Wings / float; tiny body."),
    ("unit_outcast_great_wold", "outcast_host", "unit", "Great wold / wolf-beast.", "Quad, large."),
    ("unit_outcast_snarer", "outcast_host", "unit", "Trap-snarer with lines.", "Coils/nets in side shot."),
    ("unit_outcast_wind_rider", "outcast_host", "unit", "Wind rider, aerial mount.", "Not a copy of beast rider."),
    ("unit_outcast_exiled_heir", "outcast_host", "unit", "Exiled heir champion.", "One-off, richer hides."),
    ("unit_outcast_village_elder", "outcast_host", "unit", "Elder with staff.", "Age and staff vs hunter."),
    ("unit_outcast_hunt_caller", "outcast_host", "unit", "Hunt caller / horn.", "Horn is the read."),
    ("unit_freetown_drunk", "freetown", "unit", "Drunk with bottle, rag kit.", "Comedy silhouette, still readable."),
    ("unit_freetown_crow", "freetown", "unit", "Crow familiar / scout bird.", "Tiny bird, not a man."),
    ("unit_freetown_hound", "freetown", "unit", "Fighting hound.", "Quad, not a wolf copy of the wold."),
    ("unit_freetown_warrior_crab", "freetown", "unit", "Armoured crab.", "Carapace and claws."),
    ("unit_freetown_flamer", "freetown", "unit", "Fire-spitter with tank.", "Tank + nozzle."),
    ("unit_freetown_powder_cart", "freetown", "unit", "Powder cart.", "Wheels and kegs."),
    ("unit_river_boat", "freetown", "unit", "River boat.", "Hull is the unit, not a man on a plank."),
    ("unit_freetown_builder", "freetown", "unit", "Dock builder.", "Tools, not drunk."),
    ("unit_freetown_mudslinger", "freetown", "unit", "Mudslinger / slinger.", "Pouch and arm."),
    ("unit_freetown_privateer", "freetown", "unit", "Privateer with cutlass.", "Sash and blade."),
    ("unit_freetown_highwayman", "freetown", "unit", "Highwayman cloak and pistol/bow.", "Hat/cloak vs privateer."),
    ("unit_freetown_brute", "freetown", "unit", "Heavy brute.", "Mass, not armour like Mundor."),
    ("unit_freetown_jump_imp", "freetown", "unit", "Jumping imp.", "Small, crouched, not a crow."),
    ("unit_freetown_cannon_fodder", "freetown", "unit", "Rag-tag fodder.", "Poorest kit in the port."),
    ("unit_freetown_improvised_explosive", "freetown", "unit", "Walking bomb / keg man.", "Keg is the body."),
    ("unit_freetown_brewmaster", "freetown", "unit", "Brewmaster with kegs.", "Not the drunk."),
    ("unit_freetown_captain", "freetown", "unit", "Ship captain.", "Coat and hat."),
    ("unit_freetown_dockmaster", "freetown", "unit", "Dockmaster with ledger/hook.", "Civic port, not captain."),
    ("unit_freetown_fence", "freetown", "unit", "Fence / dealer.", "Coat, no cutlass primacy."),
    ("unit_freetown_island_speaker", "freetown", "unit", "Island speaker / mystic.", "Staff and cloth, not church gold."),
    ("unit_university_fellow", "university_guild", "unit", "Gowned fellow.", "Gown + book or wand."),
    ("unit_university_mechanical_spider", "university_guild", "unit", "Brass mechanical spider.", "Legs and joints; not a blob."),
    ("unit_university_airship", "university_guild", "unit", "Small airship.", "Hull + envelope."),
    ("unit_university_trebuchet", "university_guild", "unit", "Trebuchet.", "Arm and counterweight."),
    ("unit_university_earth_breaker", "university_guild", "unit", "Earth-breaker engine.", "Drill/bore, not a cart."),
    ("unit_university_practitioner", "university_guild", "unit", "Field practitioner.", "Between fellow and dean."),
    ("unit_university_poison_specialist", "university_guild", "unit", "Poison specialist with vials.", "Glass vials in crown."),
    ("unit_university_chancellor", "university_guild", "unit", "Chancellor.", "Richest gown."),
    ("unit_university_arms_dean", "university_guild", "unit", "Dean of arms.", "Martial + academic."),
    ("unit_university_climate_dean", "university_guild", "unit", "Dean of climate.", "Rods/instruments."),
    ("unit_university_archivist", "university_guild", "unit", "Archivist with stacks.", "Scrolls, not a fighter."),
    ("unit_university_provost", "university_guild", "unit", "Provost.", "Distinct from chancellor."),
    ("unit_church_dawn_zealot", "rising_sun", "unit", "Dawn zealot with sun disc.", "Cloth + disc, not full plate."),
    ("unit_church_dawn_rider", "rising_sun", "unit", "Dawn rider with lance.", "Mount + gold helm."),
    ("unit_church_radiant_guard", "rising_sun", "unit", "Radiant guard in pale plate.", "Sun metal, not Mundor iron."),
    ("unit_church_solar_engine", "rising_sun", "unit", "Solar engine construct.", "Machine, not a man."),
    ("unit_church_high_priest", "rising_sun", "unit", "High priest.", "Mitre/staff, gold restrained."),
    ("unit_church_mason", "rising_sun", "unit", "Church mason.", "Builder of the faith."),
    ("unit_church_sun_priest", "rising_sun", "unit", "Sun priest, lesser than high priest.", "Disc and robe."),
    ("unit_church_sun_stalker", "rising_sun", "unit", "Sun stalker / hunter of heresy.", "Lean vs radiant guard."),
    ("unit_church_purifier", "rising_sun", "unit", "Purifier with censer or flame.", "Censer read."),
    ("unit_church_inquisitor", "rising_sun", "unit", "Inquisitor.", "Book and blade."),
    ("unit_church_eclipse_warden", "rising_sun", "unit", "Eclipse warden, darker sun kit.", "Not a veiled copy."),
    ("unit_church_dawn_herald", "rising_sun", "unit", "Dawn herald with banner or horn.", "Signal, not a zealot."),
    ("unit_church_reliquary", "rising_sun", "unit", "Walking reliquary.", "Shrine-on-legs, not a priest."),
    ("prop_tree", "world", "prop", "Hero tree: trunk, roots, clustered canopy.", "Canopy clumps, not a lollipop. Roots on the ground."),
    ("prop_rock", "world", "prop", "Cluster of weathered stones.", "Three volumes, not one egg."),
    ("prop_bridge", "world", "prop", "Short timber world-bridge (map dressing).", "Not a replacement for building_bridge."),
    ("resource_gold", "world", "resource", "Crystal gold node.", "Ice-sapphire crystal shards, stacked."),
    ("resource_timber", "world", "resource", "Log pile node.", "Bark cylinders with cut ends."),
    ("scenery_farm", "world", "scenery", "Map farmhouse and pen (placeholder box kit).", "Replace with unique authored farm when art-passed."),
    ("scenery_crumbling_tower", "world", "scenery", "Map ruin tower (placeholder).", "Do not confuse with church scorched tower."),
    ("scenery_cottage", "world", "scenery", "Map cottage (placeholder).", "Author unique if it stays on campaign maps."),
    ("scenery_mill", "world", "scenery", "Map windmill (placeholder).", "Sails must eventually read."),
    ("scenery_shrine", "world", "scenery", "Wayside shrine (placeholder).", "Not the church offering shrine."),
    ("scenery_barn", "world", "scenery", "Map barn (placeholder).", "Not the royal farm."),
]


def author_for(role):
    if role == "keep":
        return "`tools/meshgen/asterra_keeps.py`"
    if role == "building":
        return "`tools/meshgen/asterra_buildings.py`"
    if role == "unit":
        return "`tools/meshgen/asterra_units.py` / `asterra_unique_humans.py`"
    if role in ("prop", "resource"):
        return "`tools/meshgen/asterra_props.py` (`build_asterra_art_blend.py`)"
    return "`tools/meshgen/generate_objs.py` (placeholder until unique-authored)"


def read_preserved(path: Path) -> tuple[str, str]:
    if not path.exists():
        return "missing-stills", layout.NOTES_PLACEHOLDER
    text = path.read_text(encoding="utf-8")
    status = "missing-stills"
    for line in text.splitlines():
        if line.startswith("- **Status:**"):
            status = line.split(":", 1)[1].strip()
            break
    notes = layout.NOTES_PLACEHOLDER
    if "## Notes" in text:
        notes = text.split("## Notes", 1)[1].strip() or layout.NOTES_PLACEHOLDER
    return status, notes


def notes_are_custom(notes: str) -> bool:
    return notes.strip() != layout.NOTES_PLACEHOLDER


def resolve_status(def_id: str, previous: str, notes: str) -> str:
    if previous in ("iterate", "done"):
        return previous
    if notes_are_custom(notes):
        return "iterate"
    if layout.captured(def_id):
        return "captured"
    return "missing-stills"


def body(def_id, faction, role, summary, iterate, status, notes):
    cams = "|".join(layout.CAMERAS)
    return f"""# {def_id}

- **Faction:** [{faction}](../factions/{faction}.md)
- **Role:** {role}
- **Status:** {status}
- **Author:** {author_for(role)}
- **Mesh:** `Meshes/{def_id}.obj` / `.fbx`

## Intent

{summary}

## Iterate

{iterate}

## Review stills

Canonical: `Blender/Renders/models/{def_id}/` (`front`, `three-quarter`, `side`, `rear`, `low`, `detail`, `high`, `top`).

Comparison copies: `Blender/Renders/angles/<{cams}>/{def_id}.png`

## Notes

{notes}
"""


def write_status(rows):
    buckets = {key: [] for key in layout.STATUSES}
    for def_id, faction, role, status, missing in rows:
        buckets.setdefault(status, []).append((def_id, faction, role, missing))
    lines = [
        "# Art review queue",
        "",
        "Edit **Status** on a model file (`missing-stills`, `captured`, `iterate`, `done`).",
        "`iterate` and `done` are never overwritten by `write_art_docs.py`.",
        "",
        f"Canonical stills: `Blender/Renders/models/<id>/`",
        "",
    ]
    for key in layout.STATUSES:
        items = buckets.get(key, [])
        lines.append(f"## {key} ({len(items)})")
        lines.append("")
        if not items:
            lines.append("_None._")
            lines.append("")
            continue
        for def_id, faction, role, missing in items:
            extra = f" — missing {', '.join(missing)}" if missing else ""
            lines.append(f"- [{def_id}](models/{def_id}.md) ({faction}, {role}){extra}")
        lines.append("")
    STATUS.write_text("\n".join(lines), encoding="utf-8")


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    lines = [
        "# Art catalog",
        "",
        "Rules: [DESIGN_RULES.md](DESIGN_RULES.md) · Queue: [STATUS.md](STATUS.md)",
        "",
    ]
    current = None
    status_rows = []
    for def_id, faction, role, summary, iterate in MODELS:
        path = OUT / f"{def_id}.md"
        previous, notes = read_preserved(path)
        status = resolve_status(def_id, previous, notes)
        path.write_text(body(def_id, faction, role, summary, iterate, status, notes), encoding="utf-8")
        missing = layout.missing_cameras(def_id)
        status_rows.append((def_id, faction, role, status, missing))
        if faction != current:
            current = faction
            lines.append(f"## {faction.replace('_', ' ')}")
            lines.append("")
        lines.append(f"- [{def_id}](models/{def_id}.md) — {role}, {status}. {summary}")
    lines.append("")
    INDEX.write_text("\n".join(lines), encoding="utf-8")
    write_status(status_rows)
    print("wrote", len(MODELS), "description files and STATUS.md")


if __name__ == "__main__":
    main()
