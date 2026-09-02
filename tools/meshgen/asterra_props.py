"""World props, resources, and map scenery."""
from __future__ import annotations

from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[2]
UNITY_MESH = ROOT / "Assets/Asterra/Shared/Art/Meshes"


def _stamp(ob, def_id):
    ob.name = def_id
    ob["definition_id"] = def_id
    return ob


def prop_tree(g, m, c):
    return _stamp(g.build_tree(m), "prop_tree")


def prop_rock(g, m, c):
    return _stamp(g.build_rock(m), "prop_rock")


def prop_bridge(g, m, c):
    return _stamp(g.build_bridge(m), "prop_bridge")


def resource_gold(g, m, c):
    return _stamp(g.build_gold(m), "resource_gold")


def resource_timber(g, m, c):
    return _stamp(g.build_timber(m), "resource_timber")


def _import_obj(g, def_id, collection):
    path = UNITY_MESH / f"{def_id}.obj"
    if not path.exists():
        raise FileNotFoundError(path)
    bpy.ops.wm.obj_import(filepath=str(path))
    ob = bpy.context.selected_objects[0]
    g.move_to(ob, collection)
    return _stamp(ob, def_id)


def scenery_farm(g, m, c):
    return _import_obj(g, "scenery_farm", c)


def scenery_crumbling_tower(g, m, c):
    return _import_obj(g, "scenery_crumbling_tower", c)


def scenery_cottage(g, m, c):
    return _import_obj(g, "scenery_cottage", c)


def scenery_mill(g, m, c):
    return _import_obj(g, "scenery_mill", c)


def scenery_shrine(g, m, c):
    return _import_obj(g, "scenery_shrine", c)


def scenery_barn(g, m, c):
    return _import_obj(g, "scenery_barn", c)


PROPS = {
    "prop_tree": prop_tree,
    "prop_rock": prop_rock,
    "prop_bridge": prop_bridge,
    "resource_gold": resource_gold,
    "resource_timber": resource_timber,
    "scenery_farm": scenery_farm,
    "scenery_crumbling_tower": scenery_crumbling_tower,
    "scenery_cottage": scenery_cottage,
    "scenery_mill": scenery_mill,
    "scenery_shrine": scenery_shrine,
    "scenery_barn": scenery_barn,
}
