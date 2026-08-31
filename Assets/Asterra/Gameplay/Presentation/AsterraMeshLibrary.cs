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
            if (TryExact(definitionId, out var exact))
                return exact;

            if (definitionId.Contains("heir") || definitionId.Contains("captain")
                || definitionId.Contains("colossus")
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
                || definitionId.Contains("mage"))
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
            if (definitionId.Contains("knight"))
                return GetOrCreate("unit_cavalry");
            if (definitionId.Contains("golem") || definitionId.Contains("catapult") || definitionId.Contains("siege")
                || definitionId.Contains("mortar") || definitionId.Contains("ballista") || definitionId.Contains("guardian")
                || definitionId.Contains("onager") || definitionId.Contains("flamer") || definitionId.Contains("cart")
                || definitionId.Contains("giant") || definitionId.Contains("crab")
                || definitionId.Contains("trebuchet") || definitionId.Contains("earth_breaker")
                || definitionId.Contains("airship") || definitionId.Contains("solar_engine"))
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
            return GetBuildingMesh(definitionId, 0);
        }

        public static Mesh GetBuildingMesh(string definitionId, byte factionIndex)
        {
            if (TryExact(definitionId, out var exact))
                return exact;
            string key = ResolveBuildingMeshKey(definitionId, factionIndex);
            return GetOrCreate(key);
        }

        public static float BuildingVisualMultiplier(string definitionId, byte factionIndex)
        {
            if (string.IsNullOrEmpty(definitionId))
                return 1f;
            if (definitionId.Contains("turret"))
                return 1.65f;
            if (IsKeep(definitionId))
                return 1.12f + (factionIndex % 6) * 0.04f;
            if (IsWall(definitionId))
                return 1f;
            return 0.95f + (factionIndex % 3) * 0.06f;
        }

        public static Texture2D GetFactionAlbedo(byte factionIndex)
        {
            EnsureAlbedos();
            int i = factionIndex % BuildingAlbedos.Length;
            return BuildingAlbedos[i];
        }

        public static Texture2D GetBodyAlbedo(bool isUnit, string definitionId, byte factionIndex)
        {
            EnsureAlbedos();
            if (!isUnit)
            {
                if (IsWall(definitionId))
                    return BuildingAlbedos[1];
                if (IsTower(definitionId) || (!string.IsNullOrEmpty(definitionId) && definitionId.Contains("turret")))
                    return BuildingAlbedos[3];
                if (IsOutpost(definitionId))
                    return BuildingAlbedos[4];
                return BuildingAlbedos[factionIndex % BuildingAlbedos.Length];
            }

            var role = InferRole(definitionId);
            int idx = role switch
            {
                UnitRole.Ranged => 1,
                UnitRole.Cavalry => 2,
                UnitRole.Siege => 3,
                UnitRole.Builder => 4,
                _ => 0,
            };
            if (definitionId != null
                && (definitionId.Contains("heir") || definitionId.Contains("leader")
                    || definitionId.Contains("priest") || definitionId.Contains("mage")
                    || definitionId.Contains("caster") || definitionId.Contains("king")))
                idx = 5;
            return UnitAlbedos[idx];
        }

        public static float BodyUvScale(bool isUnit)
        {
            return isUnit ? 0.42f : 0.11f;
        }

        public static Texture2D GetPropAlbedo(string kind)
        {
            EnsureAlbedos();
            if (kind == "leaf" || kind == "canopy" || kind == "bush")
                return FoliageAlbedo;
            if (kind == "rock")
                return BuildingAlbedos[0];
            if (kind == "gold")
                return BuildingAlbedos[3];
            if (kind == "bridge" || kind == "bark" || kind == "timber")
                return BuildingAlbedos[1];
            return BuildingAlbedos[0];
        }

        public static Texture2D GetTerrainAlbedo(string layer)
        {
            EnsureAlbedos();
            if (layer == "dirt")
                return TerrainAlbedos[1];
            if (layer == "rock")
                return TerrainAlbedos[2];
            if (layer == "sand")
                return TerrainAlbedos[3];
            return TerrainAlbedos[0];
        }

        private static string ResolveBuildingMeshKey(string definitionId, byte factionIndex)
        {
            int f = factionIndex % 6;
            if (!string.IsNullOrEmpty(definitionId) && definitionId.Contains("turret"))
                return "building_turret";
            if (IsKeep(definitionId))
                return KeepMeshes[f];
            if (IsWall(definitionId))
                return "building_wall";
            if (IsTower(definitionId))
                return TowerMeshes[f];
            if (IsOutpost(definitionId))
                return OutpostMeshes[f];
            if (IsProducer(definitionId))
                return ProducerMeshes[f];
            return ProducerMeshes[f];
        }

        public static bool IsTower(string definitionId)
        {
            return !string.IsNullOrEmpty(definitionId)
                   && (definitionId.Contains("tower") || definitionId.Contains("watchtower")
                       || definitionId.Contains("watch") || definitionId.Contains("nest")
                       || definitionId.Contains("clockwork") || definitionId.Contains("far_glass"));
        }

        public static bool IsWall(string definitionId)
        {
            return !string.IsNullOrEmpty(definitionId)
                   && (definitionId.Contains("palisade") || definitionId.Contains("wall")
                       || definitionId.Contains("ground_works") || definitionId.Contains("barricade")
                       || definitionId.Contains("moat"));
        }

        private static bool IsProducer(string definitionId)
        {
            return !string.IsNullOrEmpty(definitionId)
                   && (definitionId.Contains("grove") || definitionId.Contains("forge")
                       || definitionId.Contains("barracks") || definitionId.Contains("academy")
                       || definitionId.Contains("temple") || definitionId.Contains("conjuring")
                       || definitionId.Contains("ruins") || definitionId.Contains("court")
                       || definitionId.Contains("burrow") || definitionId.Contains("aerie")
                       || definitionId.Contains("village") || definitionId.Contains("smuggler")
                       || definitionId.Contains("hut") || definitionId.Contains("workshop")
                       || definitionId.Contains("library") || definitionId.Contains("alchemist")
                       || definitionId.Contains("weather_rod") || definitionId.Contains("monastery")
                       || definitionId.Contains("sacred_site"));
        }

        private static bool IsOutpost(string definitionId)
        {
            return !string.IsNullOrEmpty(definitionId)
                   && (definitionId.Contains("conservatory") || definitionId.Contains("outpost")
                       || definitionId.Contains("mine") || definitionId.Contains("farm")
                       || definitionId.Contains("market") || definitionId.Contains("observatory")
                       || definitionId.Contains("shrine"));
        }

        private static readonly string[] KeepMeshes =
        {
            "building_keep", "building_producer", "building_tower",
            "building_outpost", "building_producer", "building_keep",
        };

        private static readonly string[] ProducerMeshes =
        {
            "building_producer", "building_keep", "building_outpost",
            "building_tower", "building_keep", "building_producer",
        };

        private static readonly string[] TowerMeshes =
        {
            "building_tower", "building_turret", "building_keep",
            "building_producer", "building_outpost", "building_tower",
        };

        private static readonly string[] OutpostMeshes =
        {
            "building_outpost", "building_producer", "building_keep",
            "building_turret", "building_tower", "building_outpost",
        };

        private static readonly Texture2D[] BuildingAlbedos = new Texture2D[6];
        private static readonly Texture2D[] UnitAlbedos = new Texture2D[6];
        private static readonly Texture2D[] TerrainAlbedos = new Texture2D[4];
        private static Texture2D FoliageAlbedo;

        private static void EnsureAlbedos()
        {
            if (BuildingAlbedos[0] != null)
                return;
            BuildingAlbedos[0] = MakeAlbedo(0, "bldg_stone");
            BuildingAlbedos[1] = MakeAlbedo(1, "bldg_timber");
            BuildingAlbedos[2] = MakeAlbedo(2, "bldg_canvas");
            BuildingAlbedos[3] = MakeAlbedo(3, "bldg_brass");
            BuildingAlbedos[4] = MakeAlbedo(4, "bldg_earth");
            BuildingAlbedos[5] = MakeAlbedo(5, "bldg_marble");
            UnitAlbedos[0] = MakeAlbedo(6, "unit_cloth");
            UnitAlbedos[1] = MakeAlbedo(7, "unit_leather");
            UnitAlbedos[2] = MakeAlbedo(8, "unit_barding");
            UnitAlbedos[3] = MakeAlbedo(9, "unit_plate");
            UnitAlbedos[4] = MakeAlbedo(10, "unit_work");
            UnitAlbedos[5] = MakeAlbedo(11, "unit_robe");
            FoliageAlbedo = MakeAlbedo(12, "prop_leaf");
            TerrainAlbedos[0] = MakeAlbedo(13, "terrain_grass");
            TerrainAlbedos[1] = MakeAlbedo(4, "terrain_dirt");
            TerrainAlbedos[2] = MakeAlbedo(0, "terrain_rock");
            TerrainAlbedos[3] = MakeAlbedo(14, "terrain_sand");
        }

        private static Texture2D MakeAlbedo(int kind, string name)
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                name = "asterra_" + name,
            };
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n;
                    float v = y / (float)n;
                    Color c;
                    switch (kind)
                    {
                        case 1: // timber planks
                            c = Color.Lerp(new Color(0.28f, 0.16f, 0.07f), new Color(0.78f, 0.55f, 0.28f),
                                Mathf.Abs(Mathf.Sin(v * 36f)));
                            if (x % 20 < 3)
                                c *= 0.45f;
                            break;
                        case 2: // canvas weave
                            c = new Color(0.9f, 0.84f, 0.7f);
                            if (((x / 4) + (y / 4)) % 2 == 0)
                                c = new Color(0.62f, 0.56f, 0.42f);
                            break;
                        case 3: // brass plates
                            c = Color.Lerp(new Color(0.35f, 0.24f, 0.08f), new Color(0.95f, 0.82f, 0.35f),
                                Mathf.Abs(Mathf.Sin(u * 18f) * Mathf.Cos(v * 18f)));
                            if (x % 16 < 2 || y % 16 < 2)
                                c *= 0.4f;
                            break;
                        case 4: // packed earth
                            c = Color.Lerp(new Color(0.22f, 0.14f, 0.07f), new Color(0.72f, 0.52f, 0.28f),
                                Mathf.PerlinNoise(x * 0.12f, y * 0.12f));
                            break;
                        case 5: // marble
                            c = Color.Lerp(new Color(0.95f, 0.93f, 0.88f), new Color(0.35f, 0.32f, 0.3f),
                                Mathf.Abs(Mathf.Sin((u * 4f + v) * 10f + Mathf.PerlinNoise(u * 10f, v * 10f) * 6f)));
                            break;
                        case 6: // infantry cloth / tabard
                            c = Color.Lerp(new Color(0.25f, 0.22f, 0.2f), new Color(0.95f, 0.9f, 0.82f),
                                (Mathf.Sin(u * 40f) * 0.5f + 0.5f));
                            if (y % 12 < 3)
                                c = Color.Lerp(c, Color.white, 0.35f);
                            break;
                        case 7: // ranger leather
                            c = Color.Lerp(new Color(0.18f, 0.12f, 0.08f), new Color(0.7f, 0.48f, 0.28f),
                                Mathf.PerlinNoise(x * 0.2f, y * 0.35f));
                            if ((x + y) % 18 < 2)
                                c *= 0.5f;
                            break;
                        case 8: // cavalry barding
                            c = Color.Lerp(new Color(0.15f, 0.15f, 0.16f), new Color(0.85f, 0.78f, 0.55f),
                                Mathf.Abs(Mathf.Sin(v * 22f)));
                            if (x % 10 < 2)
                                c = Color.Lerp(c, new Color(0.9f, 0.75f, 0.25f), 0.4f);
                            break;
                        case 9: // siege plate
                            c = Color.Lerp(new Color(0.2f, 0.2f, 0.22f), new Color(0.75f, 0.75f, 0.72f),
                                ((x / 16) + (y / 16)) % 2);
                            if (x % 16 < 1 || y % 16 < 1)
                                c *= 0.35f;
                            break;
                        case 10: // builder workwear
                            c = Color.Lerp(new Color(0.3f, 0.28f, 0.18f), new Color(0.85f, 0.72f, 0.35f),
                                Mathf.PerlinNoise(x * 0.25f, y * 0.25f));
                            if (y % 8 < 2)
                                c *= 0.7f;
                            break;
                        case 11: // mage / leader robe
                            c = Color.Lerp(new Color(0.12f, 0.08f, 0.22f), new Color(0.85f, 0.7f, 0.95f),
                                Mathf.Abs(Mathf.Sin((u + v) * 14f)));
                            if (((int)(u * 24f) + (int)(v * 24f)) % 5 == 0)
                                c = Color.Lerp(c, Color.white, 0.45f);
                            break;
                        case 12: // foliage
                            c = Color.Lerp(new Color(0.08f, 0.22f, 0.08f), new Color(0.35f, 0.72f, 0.22f),
                                Mathf.PerlinNoise(x * 0.22f, y * 0.22f));
                            if (((x / 8) + (y / 6)) % 2 == 0)
                                c *= 0.65f;
                            break;
                        case 13: // grass
                            c = Color.Lerp(new Color(0.16f, 0.32f, 0.1f), new Color(0.42f, 0.68f, 0.22f),
                                Mathf.PerlinNoise(x * 0.18f, y * 0.18f));
                            if (((int)(u * 32f) + (int)(v * 18f)) % 4 == 0)
                                c = Color.Lerp(c, new Color(0.55f, 0.72f, 0.22f), 0.45f);
                            break;
                        case 14: // sand
                            c = Color.Lerp(new Color(0.62f, 0.5f, 0.28f), new Color(0.92f, 0.82f, 0.55f),
                                Mathf.PerlinNoise(x * 0.28f, y * 0.28f));
                            break;
                        default: // stone mortar
                            c = Color.Lerp(new Color(0.28f, 0.27f, 0.25f), new Color(0.82f, 0.8f, 0.74f),
                                Mathf.PerlinNoise(x * 0.15f, y * 0.15f));
                            if (x % 16 < 2 || y % 12 < 2)
                                c *= 0.4f;
                            break;
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply(false, true);
            return tex;
        }

        public static Mesh GetResourceMesh(ResourceType type)
        {
            return type == ResourceType.Gold
                ? GetOrCreate("resource_gold")
                : GetOrCreate("resource_timber");
        }

        public static Mesh GetDestructibleMesh(string definitionId)
        {
            if (TryExact(definitionId, out var exact))
                return exact;
            if (!string.IsNullOrEmpty(definitionId))
            {
                if (definitionId.Contains("bridge"))
                    return GetOrCreate("prop_bridge");
                if (definitionId.Contains("farm"))
                    return GetOrCreate("scenery_farm");
                if (definitionId.Contains("crumbling") || definitionId.Contains("ruin_tower"))
                    return GetOrCreate("scenery_crumbling_tower");
                if (definitionId.Contains("cottage"))
                    return GetOrCreate("scenery_cottage");
                if (definitionId.Contains("mill"))
                    return GetOrCreate("scenery_mill");
                if (definitionId.Contains("shrine"))
                    return GetOrCreate("scenery_shrine");
                if (definitionId.Contains("barn"))
                    return GetOrCreate("scenery_barn");
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
                if (definitionId.Contains("farm") || definitionId.Contains("barn") || definitionId.Contains("cottage"))
                    return new Color(0.62f, 0.48f, 0.28f);
                if (definitionId.Contains("crumbling") || definitionId.Contains("shrine"))
                    return new Color(0.52f, 0.5f, 0.46f);
                if (definitionId.Contains("mill"))
                    return new Color(0.58f, 0.5f, 0.38f);
                if (definitionId.Contains("rock"))
                    return new Color(0.55f, 0.55f, 0.58f);
            }

            return new Color(0.18f, 0.42f, 0.22f);
        }

        public static string DestructibleTexKey(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return "rock";
            if (definitionId.Contains("bridge") || definitionId.Contains("farm")
                || definitionId.Contains("barn") || definitionId.Contains("cottage")
                || definitionId.Contains("mill"))
                return "bridge";
            if (definitionId.Contains("tree"))
                return "leaf";
            return "rock";
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
                && (definitionId.Contains("heir") || definitionId.Contains("captain")
                    || definitionId.Contains("colossus")
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

        private static bool TryExact(string key, out Mesh mesh)
        {
            mesh = null;
            if (string.IsNullOrEmpty(key))
                return false;
            if (Cache.TryGetValue(key, out mesh) && mesh != null && mesh.vertexCount > 0)
                return true;
            if (ObjMeshLoader.TryLoad(key, out mesh) && mesh != null && mesh.vertexCount > 0)
            {
                Cache[key] = mesh;
                return true;
            }

            mesh = null;
            return false;
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
