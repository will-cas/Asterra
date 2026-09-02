# World (props and scenery)

Neutral map dressing. Not a playable faction.

**Authored:** `prop_tree`, `prop_rock`, `prop_bridge`, `resource_gold`, `resource_timber`  
**Placeholders:** `scenery_farm`, `scenery_crumbling_tower`, `scenery_cottage`, `scenery_mill`, `scenery_shrine`, `scenery_barn`

## Silhouette

- Tree: roots, forked trunk, clumped canopy — not a lollipop.
- Rock: a small pile of stones.
- Resources must read from the RTS camera as nodes to gather.
- Scenery boxes are stand-ins. Replace before calling the map “art complete”.

## Iterate toward

The same PBR and bevel language as buildings. Do not leave `generate_objs.py` boxes next to observatory-grade keeps without a pass.
