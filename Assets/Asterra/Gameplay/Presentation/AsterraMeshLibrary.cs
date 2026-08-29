using System.Collections.Generic;
using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// Loads low-poly meshes from Assets/Asterra/Shared/Art/Meshes/*.obj (Kenney / Quaternius CC0).
    /// No procedural mesh builders — missing files log an error and return an empty mesh.
    /// </summary>
    public static class AsterraMeshLibrary
    {
        private static readonly Dictionary<string, Mesh> Cache = new();

        public static Mesh GetUnitMesh(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return GetOrCreate("unit_militia");

            if (definitionId.Contains("lucien") || definitionId.Contains("captain") || definitionId.Contains("hierophant")
                || definitionId.Contains("heir") || definitionId.Contains("colossus")
                || definitionId.Contains("thorn_speaker") || definitionId.Contains("night_abbot")
                || definitionId.Contains("first_heretic")
                || definitionId.Contains("king") || definitionId.Contains("spymaster")
                || definitionId.Contains("justiciar") || definitionId.Contains("tomb_warden")
                || definitionId.Contains("marshal")
                || definitionId.Contains("elder") || definitionId.Contains("hunt_caller")
                || definitionId.Contains("brewmaster") || definitionId.Contains("dockmaster")
                || definitionId.Contains("island_speaker") || definitionId.Contains("fence")
                || definitionId.Contains("chancellor") || definitionId.Contains("dean")
                || definitionId.Contains("archivist") || definitionId.Contains("provost")
                || definitionId.Contains("high_priest") || definitionId.Contains("inquisitor")
                || definitionId.Contains("eclipse") || definitionId.Contains("herald")
                || definitionId.Contains("reliquary"))
                return GetOrCreate("unit_leader");
            if (definitionId.Contains("builder") || definitionId.Contains("hobgoblin")
                || definitionId.Contains("practitioner") || definitionId.Contains("mason"))
                return GetOrCreate("unit_builder");
            if (definitionId.Contains("caster") || definitionId.Contains("elemental") || definitionId.Contains("priest")
                || definitionId.Contains("mage") || definitionId.Contains("ashen_knight"))
                return GetOrCreate("unit_mage");
            if (definitionId.Contains("archer") || definitionId.Contains("ranger") || definitionId.Contains("acolyte")
                || definitionId.Contains("longbow") || definitionId.Contains("hunter") || definitionId.Contains("mudslinger")
                || definitionId.Contains("poison"))
                return GetOrCreate("unit_archer");
            if (definitionId.Contains("shadow") || definitionId.Contains("cavalry") || definitionId.Contains("rider")
                || definitionId.Contains("commander") || definitionId.Contains("wold") || definitionId.Contains("beast")
                || definitionId.Contains("hound") || definitionId.Contains("privateer")
                || definitionId.Contains("spider"))
                return GetOrCreate("unit_cavalry");
            if (definitionId.Contains("knight") && !definitionId.Contains("ashen_knight") && !definitionId.Contains("iron_knight"))
                return GetOrCreate("unit_cavalry");
            if (definitionId.Contains("golem") || definitionId.Contains("catapult") || definitionId.Contains("siege")
                || definitionId.Contains("mortar") || definitionId.Contains("ballista") || definitionId.Contains("guardian")
                || definitionId.Contains("onager") || definitionId.Contains("flamer") || definitionId.Contains("cart")
                || definitionId.Contains("giant") || definitionId.Contains("crab")
                || definitionId.Contains("trebuchet") || definitionId.Contains("earth_breaker")
                || definitionId.Contains("airship") || definitionId.Contains("solar_engine"))
                return GetOrCreate("unit_siege");
                return GetOrCreate("unit_siege");
            if (definitionId.Contains("crow"))
                return GetOrCreate("unit_dryad");
            if (definitionId.Contains("dryad") || definitionId.Contains("sprite"))
                return GetOrCreate("unit_dryad");
            if (definitionId.Contains("ember"))
                return GetOrCreate("unit_ember_raider");
            return GetOrCreate("unit_militia");
        }

        public static Mesh GetBuildingMesh(string definitionId)
        {
            if (!string.IsNullOrEmpty(definitionId) && definitionId.Contains("turret"))
                return GetOrCreate("building_turret");
            if (IsKeep(definitionId))
                return GetOrCreate("building_keep");
            if (!string.IsNullOrEmpty(definitionId))
            {
                if (definitionId.Contains("tower") || definitionId.Contains("watchtower") || definitionId.Contains("watch")
                    || definitionId.Contains("nest") || definitionId.Contains("clockwork") || definitionId.Contains("far_glass"))
                    return GetOrCreate("building_tower");
                if (definitionId.Contains("palisade") || definitionId.Contains("wall") || definitionId.Contains("ground_works")
                    || definitionId.Contains("barricade") || definitionId.Contains("moat"))
                    return GetOrCreate("building_wall");
                if (definitionId.Contains("grove") || definitionId.Contains("forge") || definitionId.Contains("barracks")
                    || definitionId.Contains("academy") || definitionId.Contains("temple")
                    || definitionId.Contains("conjuring") || definitionId.Contains("ruins")
                    || definitionId.Contains("court") || definitionId.Contains("burrow")
                    || definitionId.Contains("aerie") || definitionId.Contains("village")
                    || definitionId.Contains("smuggler") || definitionId.Contains("hut")
                    || definitionId.Contains("workshop") || definitionId.Contains("library")
                    || definitionId.Contains("alchemist") || definitionId.Contains("weather_rod")
                    || definitionId.Contains("monastery") || definitionId.Contains("sacred_site"))
                    return GetOrCreate("building_producer");
                if (definitionId.Contains("conservatory") || definitionId.Contains("outpost") || definitionId.Contains("mine")
                    || definitionId.Contains("farm") || definitionId.Contains("market")
                    || definitionId.Contains("observatory") || definitionId.Contains("shrine"))
                    return GetOrCreate("building_outpost");
            }

            return GetOrCreate("building_producer");
        }

        public static Mesh GetResourceMesh(ResourceType type)
        {
            return type == ResourceType.Gold
                ? GetOrCreate("resource_gold")
                : GetOrCreate("resource_timber");
        }

        public static Mesh GetDestructibleMesh(string definitionId)
        {
            if (!string.IsNullOrEmpty(definitionId))
            {
                if (definitionId.Contains("bridge"))
                    return GetOrCreate("prop_bridge");
                if (definitionId.Contains("rock"))
                    return GetOrCreate("prop_rock");
            }

            return GetOrCreate("prop_tree");
        }

        public static Color DestructibleColor(string definitionId)
        {
            if (!string.IsNullOrEmpty(definitionId))
            {
                if (definitionId.Contains("bridge"))
                    return new Color(0.45f, 0.32f, 0.18f);
                if (definitionId.Contains("rock"))
                    return new Color(0.55f, 0.55f, 0.58f);
            }

            return new Color(0.18f, 0.42f, 0.22f);
        }

        public static bool IsKeep(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return false;
            if (definitionId.Contains("turret"))
                return false;
            return definitionId.Contains("keep")
                   || definitionId.Contains("heartwood")
                   || definitionId.Contains("citadel")
                   || definitionId.Contains("arcaneum")
                   || definitionId.Contains("great_camp")
                   || definitionId.Contains("tavern")
                   || definitionId.Contains("college")
                   || definitionId.Contains("grand_temple");
        }

        public static UnitRole InferRole(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return UnitRole.Infantry;
            if (definitionId.Contains("builder"))
                return UnitRole.Builder;
            if (definitionId.Contains("sapper"))
                return UnitRole.Infantry;
            if (definitionId.Contains("pathfinder"))
                return UnitRole.Infantry;
            // Notion-aligned roles that still use legacy ids.
            if (definitionId.Contains("iron_knight"))
                return UnitRole.Infantry; // Iron Guard
            if (definitionId.Contains("ashen_knight"))
                return UnitRole.Ranged; // Fire Mage
            if (definitionId.Contains("caster") || definitionId.Contains("elemental") || definitionId.Contains("priest"))
                return UnitRole.Ranged;
            if (definitionId.Contains("shadow"))
                return UnitRole.Cavalry;
            if (definitionId.Contains("golem"))
                return UnitRole.Siege;
            if (definitionId.Contains("archer") || definitionId.Contains("bow") || definitionId.Contains("acolyte"))
                return UnitRole.Ranged;
            if (definitionId.Contains("cavalry") || definitionId.Contains("rider"))
                return UnitRole.Cavalry;
            if (definitionId.Contains("knight"))
                return UnitRole.Cavalry;
            if (definitionId.Contains("catapult") || definitionId.Contains("siege") || definitionId.Contains("mortar")
                || definitionId.Contains("engine"))
                return UnitRole.Siege;
            return UnitRole.Infantry;
        }

        public static Color RoleAccent(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Infantry:
                    return new Color(0.92f, 0.93f, 0.96f);
                case UnitRole.Ranged:
                    return new Color(0.25f, 0.85f, 0.95f);
                case UnitRole.Cavalry:
                    return new Color(0.95f, 0.78f, 0.22f);
                case UnitRole.Siege:
                    return new Color(0.95f, 0.5f, 0.18f);
                case UnitRole.Builder:
                    return new Color(0.95f, 0.88f, 0.25f);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }

        public static float RoleScaleMultiplier(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Cavalry:
                    return 1.15f;
                case UnitRole.Siege:
                    return 1.25f;
                case UnitRole.Ranged:
                    return 0.95f;
                case UnitRole.Builder:
                    return 0.9f;
                case UnitRole.Infantry:
                    return 1f;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }

        public static Color FactionColor(byte factionIndex)
        {
            switch (factionIndex)
            {
                case 0: return new Color(0.42f, 0.22f, 0.55f); // uncrowned
                case 1: return new Color(0.72f, 0.58f, 0.18f); // mundor crown
                case 2: return new Color(0.38f, 0.48f, 0.22f); // outcast host
                case 3: return new Color(0.22f, 0.42f, 0.58f); // freetown
                case 4: return new Color(0.55f, 0.48f, 0.28f); // university guild
                case 5: return new Color(0.85f, 0.62f, 0.18f); // rising sun
                default: return Color.gray;
            }
        }

        public static Color FactionBodyColor(byte factionIndex, bool isUnit, string definitionId)
        {
            Color faction = FactionColor(factionIndex);
            Color trim = factionIndex switch
            {
                0 => new Color(0.72f, 0.48f, 0.88f), // amethyst
                1 => new Color(0.92f, 0.78f, 0.32f), // gold
                2 => new Color(0.55f, 0.62f, 0.32f), // moss
                3 => new Color(0.45f, 0.72f, 0.85f), // harbor
                4 => new Color(0.82f, 0.72f, 0.42f), // brass
                5 => new Color(1f, 0.92f, 0.55f), // sunlight
                _ => Color.gray,
            };

            if (!isUnit)
                return Color.Lerp(faction, trim, 0.22f);

            var role = InferRole(definitionId);
            if (definitionId != null
                && (definitionId.Contains("lucien") || definitionId.Contains("captain") || definitionId.Contains("hierophant")
                    || definitionId.Contains("heir") || definitionId.Contains("colossus")
                    || definitionId.Contains("thorn_speaker") || definitionId.Contains("night_abbot")
                    || definitionId.Contains("first_heretic")))
                return Color.Lerp(faction, new Color(0.95f, 0.85f, 0.35f), 0.45f);

            return Color.Lerp(faction, Color.Lerp(RoleAccent(role), trim, 0.35f), 0.4f);
        }

        public static Color ResourceColor(ResourceType type)
        {
            return type == ResourceType.Gold
                ? new Color(0.98f, 0.84f, 0.18f)
                : new Color(0.48f, 0.3f, 0.14f);
        }

        private static Mesh GetOrCreate(string key)
        {
            if (Cache.TryGetValue(key, out var mesh) && mesh != null)
                return mesh;
            if (ObjMeshLoader.TryLoad(key, out mesh) && mesh != null)
            {
                Cache[key] = mesh;
                return mesh;
            }

            Debug.LogError($"[Asterra] Missing mesh OBJ for key '{key}' (expected under Assets/Asterra/Shared/Art/Meshes/{key}.obj).");
            mesh = new Mesh { name = key };
            Cache[key] = mesh;
            return mesh;
        }
    }
}
