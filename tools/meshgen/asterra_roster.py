"""Dispatch unique faction keeps, buildings, and units."""
from __future__ import annotations

from asterra_buildings import BUILDINGS
from asterra_keeps import KEEPS
from asterra_props import PROPS
from asterra_units import UNITS

FACTIONS = {
    "building_arcaneum": "Uncrowned",
    "building_outcast_great_camp": "OutcastHost",
    "building_freetown_tavern": "Freetown",
    "building_university_grand_college": "UniversityGuild",
    "building_church_grand_temple": "RisingSun",
}

SKIP_IDS = {"building_royal_citadel", "unit_royal_peasant"}


def _coll_for(g, def_id):
    if def_id.startswith("prop_tree"):
        return g.coll("03_World/Trees")
    if def_id.startswith("prop_rock"):
        return g.coll("03_World/Rocks")
    if def_id.startswith("prop_bridge"):
        return g.coll("03_World/Bridges")
    if def_id.startswith("resource_"):
        return g.coll("03_World/Resources")
    if def_id.startswith("scenery_"):
        return g.coll("03_World/Trees")
    if def_id.startswith("unit_veiled") or def_id.startswith("building_arcane") or def_id in (
        "building_watchtower", "building_palisade", "building_outpost",
        "building_blackroot_conservatory", "building_ancient_ruins", "building_conjuring_hall",
        "building_high_temple", "building_portal_gate", "building_shadowed_gate",
    ):
        folder = "Uncrowned"
    elif def_id.startswith("unit_royal") or def_id.startswith("building_royal") or def_id in (
        "building_keep_turret", "building_bridge", "building_stone_wall", "unit_pathfinder", "unit_sapper",
    ):
        folder = "MundorCrown"
    elif def_id.startswith("unit_outcast") or def_id.startswith("building_outcast"):
        folder = "OutcastHost"
    elif def_id.startswith("unit_freetown") or def_id.startswith("building_freetown") or def_id in (
        "building_barricade", "building_ferry_dock", "unit_river_boat",
    ):
        folder = "Freetown"
    elif def_id.startswith("unit_university") or def_id.startswith("building_university"):
        folder = "UniversityGuild"
    else:
        folder = "RisingSun"
    kind = "01_Units" if def_id.startswith("unit_") else "02_Buildings"
    return g.coll(f"{kind}/{folder}")


def build_all(g, m):
    keeps, buildings, units = [], [], []
    for def_id, fn in KEEPS.items():
        print("keep", def_id)
        keeps.append(fn(g, m, _coll_for(g, def_id)))
    for def_id, fn in BUILDINGS.items():
        if def_id in SKIP_IDS:
            continue
        print("building", def_id)
        buildings.append(fn(g, m, _coll_for(g, def_id)))
    for def_id, fn in UNITS.items():
        if def_id in SKIP_IDS:
            continue
        print("unit", def_id)
        units.append(fn(g, m, _coll_for(g, def_id)))
    return {"keeps": keeps, "buildings": buildings, "units": units}
