using System.Collections.Generic;
using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// Runtime low-poly meshes matching Assets/Asterra/Shared/Art/Meshes/*.obj
    /// (regenerate OBJs via tools/meshgen for Blender editing).
    /// </summary>
    public static class AsterraMeshLibrary
    {
        private static readonly Dictionary<string, Mesh> Cache = new();

        public static Mesh GetUnitMesh(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return GetOrCreate("unit_militia", BuildMilitia);

            if (definitionId.Contains("lucien") || definitionId.Contains("captain") || definitionId.Contains("hierophant"))
                return GetOrCreate("unit_leader", BuildLeader);
            if (definitionId.Contains("builder"))
                return GetOrCreate("unit_builder", BuildBuilder);
            if (definitionId.Contains("archer") || definitionId.Contains("ranger") || definitionId.Contains("acolyte"))
                return GetOrCreate("unit_archer", BuildArcher);
            if (definitionId.Contains("ashen_knight") || definitionId.Contains("mage"))
                return GetOrCreate("unit_mage", BuildMage);
            if (definitionId.Contains("knight") || definitionId.Contains("cavalry") || definitionId.Contains("rider"))
                return GetOrCreate("unit_cavalry", BuildCavalry);
            if (definitionId.Contains("catapult") || definitionId.Contains("siege") || definitionId.Contains("mortar")
                || definitionId.Contains("ballista") || definitionId.Contains("guardian"))
                return GetOrCreate("unit_siege", BuildSiege);
            if (definitionId.Contains("dryad"))
                return GetOrCreate("unit_dryad", BuildDryad);
            if (definitionId.Contains("ember"))
                return GetOrCreate("unit_ember_raider", BuildEmberRaider);
            return GetOrCreate("unit_militia", BuildMilitia);
        }

        public static Mesh GetBuildingMesh(string definitionId)
        {
            if (!string.IsNullOrEmpty(definitionId) && definitionId.Contains("turret"))
            {
                // Prefer enlarged procedural mesh; the Shared OBJ silhouette is undersized for keep pads.
                return GetOrCreate("building_turret_lg", BuildTurret);
            }
            if (IsKeep(definitionId))
                return GetOrCreate("building_keep", BuildKeep);
            if (!string.IsNullOrEmpty(definitionId))
            {
                if (definitionId.Contains("tower") || definitionId.Contains("watchtower"))
                    return GetOrCreate("building_tower", BuildTower);
                if (definitionId.Contains("palisade") || definitionId.Contains("wall"))
                    return GetOrCreate("building_wall", BuildWall);
                if (definitionId.Contains("outpost") || definitionId.Contains("mine"))
                    return GetOrCreate("building_outpost", BuildOutpost);
                if (definitionId.Contains("grove") || definitionId.Contains("forge") || definitionId.Contains("barracks"))
                    return GetOrCreate("building_producer", BuildProducer);
            }

            return GetOrCreate("building_producer", BuildProducer);
        }

        public static Mesh GetResourceMesh(ResourceType type)
        {
            return type == ResourceType.Gold
                ? GetOrCreate("resource_gold", BuildGoldNugget)
                : GetOrCreate("resource_timber", BuildTimberLog);
        }

        public static Mesh GetDestructibleMesh(string definitionId)
        {
            if (!string.IsNullOrEmpty(definitionId))
            {
                if (definitionId.Contains("bridge"))
                    return GetOrCreate("prop_bridge", BuildBridge);
                if (definitionId.Contains("rock"))
                    return GetOrCreate("prop_rock", BuildRock);
            }

            return GetOrCreate("prop_tree", BuildTree);
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
                   || definitionId.Contains("citadel");
        }

        public static UnitRole InferRole(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return UnitRole.Infantry;
            if (definitionId.Contains("builder"))
                return UnitRole.Builder;
            // Notion-aligned roles that still use legacy ids.
            if (definitionId.Contains("iron_knight"))
                return UnitRole.Infantry; // Iron Guard
            if (definitionId.Contains("ashen_knight"))
                return UnitRole.Ranged; // Fire Mage
            if (definitionId.Contains("archer") || definitionId.Contains("bow") || definitionId.Contains("acolyte"))
                return UnitRole.Ranged;
            if (definitionId.Contains("knight") || definitionId.Contains("cavalry") || definitionId.Contains("rider"))
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
                case 0: return new Color(0.55f, 0.58f, 0.65f); // iron
                case 1: return new Color(0.25f, 0.55f, 0.32f); // verdant
                case 2: return new Color(0.65f, 0.28f, 0.18f); // ashen
                default: return Color.gray;
            }
        }

        public static Color FactionBodyColor(byte factionIndex, bool isUnit, string definitionId)
        {
            Color faction = FactionColor(factionIndex);
            Color trim = factionIndex switch
            {
                0 => new Color(0.75f, 0.78f, 0.85f), // steel
                1 => new Color(0.45f, 0.7f, 0.4f), // leaf
                2 => new Color(0.9f, 0.45f, 0.2f), // ember
                _ => Color.gray,
            };

            if (!isUnit)
                return Color.Lerp(faction, trim, 0.22f);

            var role = InferRole(definitionId);
            if (definitionId != null
                && (definitionId.Contains("lucien") || definitionId.Contains("captain") || definitionId.Contains("hierophant")))
                return Color.Lerp(faction, new Color(0.95f, 0.85f, 0.35f), 0.45f);

            return Color.Lerp(faction, Color.Lerp(RoleAccent(role), trim, 0.35f), 0.4f);
        }

        public static Color ResourceColor(ResourceType type)
        {
            return type == ResourceType.Gold
                ? new Color(0.98f, 0.84f, 0.18f)
                : new Color(0.48f, 0.3f, 0.14f);
        }

        private static Mesh GetOrCreate(string key, System.Func<Mesh> builder)
        {
            if (Cache.TryGetValue(key, out var mesh) && mesh != null)
                return mesh;
            if (ObjMeshLoader.TryLoad(key, out mesh) && mesh != null)
            {
                Cache[key] = mesh;
                return mesh;
            }

            mesh = builder();
            mesh.name = key;
            Cache[key] = mesh;
            return mesh;
        }

        // --- Units: readable silhouettes at RTS camera distance ---

        private static Mesh BuildMilitia()
        {
            // Compact infantry with shield + short spear.
            return Combine(
                Box(0, 0, 0, 0.75f, 1.35f, 0.55f),
                Box(0, 1.35f, 0, 0.48f, 0.48f, 0.48f),
                Box(-0.55f, 0.55f, 0.05f, 0.55f, 0.7f, 0.12f),
                Box(0.5f, 0.75f, 0, 0.14f, 0.14f, 1.35f));
        }

        private static Mesh BuildBuilder()
        {
            // Worker with hammer / tool silhouette.
            return Combine(
                Box(0, 0, 0, 0.7f, 1.15f, 0.55f),
                Box(0, 1.15f, 0, 0.42f, 0.42f, 0.42f),
                Box(0.7f, 0.55f, 0, 1.05f, 0.16f, 0.16f),
                Box(1.15f, 0.55f, 0, 0.35f, 0.55f, 0.28f),
                Box(-0.45f, 0.35f, 0.2f, 0.35f, 0.35f, 0.35f));
        }

        private static Mesh BuildArcher()
        {
            // Slim body + vertical bow + horizontal string.
            return Combine(
                Box(0, 0, 0, 0.5f, 1.4f, 0.42f),
                Box(0, 1.4f, 0, 0.38f, 0.38f, 0.38f),
                Box(0.05f, 0.85f, 0.55f, 0.1f, 1.15f, 0.1f),
                Box(0.05f, 0.85f, -0.55f, 0.1f, 1.15f, 0.1f),
                Box(0.05f, 1.35f, 0, 0.08f, 0.08f, 1.1f),
                Box(0.05f, 0.35f, 0, 0.08f, 0.08f, 1.1f),
                Box(0.45f, 0.9f, 0, 0.55f, 0.08f, 0.08f));
        }

        private static Mesh BuildCavalry()
        {
            // Longer horse body + rider.
            return Combine(
                Box(0, 0.05f, 0, 1.55f, 0.65f, 0.55f),
                Box(0.7f, 0.55f, 0, 0.4f, 0.4f, 0.4f),
                Box(-0.15f, 0.7f, 0, 0.55f, 0.85f, 0.42f),
                Box(-0.15f, 1.5f, 0, 0.38f, 0.35f, 0.38f),
                Box(0.95f, 0.15f, 0, 0.25f, 0.2f, 0.2f),
                Box(-0.7f, 0.0f, 0.22f, 0.18f, 0.35f, 0.18f),
                Box(-0.7f, 0.0f, -0.22f, 0.18f, 0.35f, 0.18f),
                Box(0.55f, 0.0f, 0.22f, 0.18f, 0.35f, 0.18f),
                Box(0.55f, 0.0f, -0.22f, 0.18f, 0.35f, 0.18f));
        }

        private static Mesh BuildSiege()
        {
            // Wagon chassis, wheels, and angled arm.
            return Combine(
                Box(0, 0.35f, 0, 1.6f, 0.5f, 1.0f),
                Box(0, 0.85f, 0, 0.75f, 0.65f, 0.75f),
                Box(0.35f, 1.15f, 0, 1.35f, 0.16f, 0.16f),
                Box(0.95f, 1.25f, 0, 0.35f, 0.35f, 0.35f),
                Box(-0.65f, 0.0f, 0.55f, 0.35f, 0.35f, 0.18f),
                Box(-0.65f, 0.0f, -0.55f, 0.35f, 0.35f, 0.18f),
                Box(0.65f, 0.0f, 0.55f, 0.35f, 0.35f, 0.18f),
                Box(0.65f, 0.0f, -0.55f, 0.35f, 0.35f, 0.18f));
        }

        private static Mesh BuildDryad()
        {
            return Combine(
                Box(0, 0, 0, 0.55f, 1.6f, 0.45f),
                Box(0, 1.55f, 0, 0.85f, 0.4f, 0.85f),
                Box(0, 1.9f, 0, 0.45f, 0.35f, 0.45f));
        }

        private static Mesh BuildEmberRaider()
        {
            return Combine(
                Box(0, 0, 0, 0.85f, 1.3f, 0.55f),
                Box(0, 1.25f, 0, 0.42f, 0.42f, 0.42f),
                Box(-0.6f, 1.0f, 0, 0.4f, 0.28f, 0.55f),
                Box(0.6f, 1.0f, 0, 0.4f, 0.28f, 0.55f));
        }

        private static Mesh BuildLeader()
        {
            // Caped commander with taller helm / banner spike.
            return Combine(
                Box(0, 0, 0, 0.85f, 1.55f, 0.6f),
                Box(0, 1.55f, 0, 0.55f, 0.55f, 0.55f),
                Box(0, 2.05f, 0, 0.25f, 0.55f, 0.25f),
                Box(-0.15f, 0.7f, -0.45f, 0.95f, 1.2f, 0.12f),
                Box(0.65f, 0.95f, 0, 0.18f, 0.18f, 1.5f),
                Box(0.65f, 1.55f, 0.55f, 0.35f, 0.55f, 0.12f));
        }

        private static Mesh BuildMage()
        {
            return Combine(
                Box(0, 0, 0, 0.55f, 1.45f, 0.5f),
                Box(0, 1.45f, 0, 0.7f, 0.35f, 0.7f),
                Box(0, 1.8f, 0, 0.35f, 0.45f, 0.35f),
                Box(0.7f, 0.85f, 0, 0.18f, 1.4f, 0.18f),
                Box(0.7f, 1.55f, 0, 0.35f, 0.35f, 0.35f));
        }

        // --- Buildings ---

        private static Mesh BuildKeep()
        {
            // Fortress: wide bailey, keep tower, gatehouse, corner turrets.
            return Combine(
                Box(0, 0, 0, 8.2f, 2.6f, 8.2f),
                Box(0, 2.6f, 0, 5.2f, 6.2f, 5.2f),
                Box(0, 8.6f, 0, 2.6f, 2.4f, 2.6f),
                Box(0, 2.2f, 4.4f, 3.2f, 3.4f, 1.4f),
                Box(-3.4f, 8.2f, -3.4f, 1.6f, 2.2f, 1.6f),
                Box(3.4f, 8.2f, -3.4f, 1.6f, 2.2f, 1.6f),
                Box(-3.4f, 8.2f, 3.4f, 1.6f, 2.2f, 1.6f),
                Box(3.4f, 8.2f, 3.4f, 1.6f, 2.2f, 1.6f),
                Box(0, 10.8f, 0, 0.35f, 1.6f, 0.35f));
        }

        private static Mesh BuildProducer()
        {
            // Barracks / hall: wide roofed hall with twin chimneys and drill yard porch.
            return Combine(
                Box(0, 0, 0, 6.2f, 2.6f, 4.8f),
                Box(0, 2.6f, 0, 4.6f, 1.4f, 3.6f),
                Box(0, 3.8f, 0, 6.6f, 0.5f, 1.0f),
                Box(-2.6f, 0, -1.8f, 1.2f, 4.6f, 1.2f),
                Box(2.6f, 0, -1.8f, 1.2f, 4.6f, 1.2f),
                Box(0, 0.15f, 2.6f, 2.2f, 2.2f, 0.4f),
                Box(-1.8f, 0.1f, 1.6f, 0.8f, 1.1f, 0.8f),
                Box(1.8f, 0.1f, 1.6f, 0.8f, 1.1f, 0.8f));
        }

        private static Mesh BuildTower()
        {
            return Combine(
                Box(0, 0, 0, 2.8f, 1.8f, 2.8f),
                Box(0, 1.8f, 0, 1.8f, 9.2f, 1.8f),
                Box(0, 10.8f, 0, 2.8f, 1.1f, 2.8f),
                Box(0, 11.8f, 0, 1.3f, 1.5f, 1.3f),
                Box(0, 13.2f, 0, 0.35f, 1.3f, 0.35f),
                Box(-1.1f, 10.9f, -1.1f, 0.7f, 1.0f, 0.7f),
                Box(1.1f, 10.9f, 1.1f, 0.7f, 1.0f, 0.7f));
        }

        private static Mesh BuildTurret()
        {
            return Combine(
                Box(0, 0, 0, 3.4f, 1.6f, 3.4f),
                Box(0, 1.6f, 0, 2.2f, 6.2f, 2.2f),
                Box(0, 7.6f, 0, 3.2f, 1.1f, 3.2f),
                Box(-1.2f, 8.5f, -1.2f, 0.85f, 1.2f, 0.85f),
                Box(1.2f, 8.5f, -1.2f, 0.85f, 1.2f, 0.85f),
                Box(-1.2f, 8.5f, 1.2f, 0.85f, 1.2f, 0.85f),
                Box(1.2f, 8.5f, 1.2f, 0.85f, 1.2f, 0.85f),
                Box(1.4f, 8.0f, 0, 2.4f, 0.45f, 0.45f));
        }

        private static Mesh BuildWall()
        {
            return Combine(
                Box(0, 0, 0, 11f, 3.6f, 1.4f),
                Box(-4.5f, 3.6f, 0, 0.9f, 1.4f, 0.9f),
                Box(-1.5f, 3.6f, 0, 0.9f, 1.6f, 0.9f),
                Box(1.5f, 3.6f, 0, 0.9f, 1.4f, 0.9f),
                Box(4.5f, 3.6f, 0, 0.9f, 1.6f, 0.9f),
                Box(0, 1.4f, 0.55f, 2.2f, 1.6f, 0.25f));
        }

        private static Mesh BuildOutpost()
        {
            return Combine(
                Box(0, 0, 0, 4.0f, 1.8f, 4.0f),
                Box(0, 1.8f, 0, 2.6f, 3.0f, 2.6f),
                Box(0, 4.8f, 0, 0.4f, 3.4f, 0.4f),
                Box(0.85f, 7.0f, 0, 1.7f, 1.0f, 0.14f),
                Box(0.2f, 7.7f, 0, 0.3f, 0.3f, 0.3f),
                Box(-1.4f, 0.1f, 1.4f, 0.9f, 1.2f, 0.9f));
        }

        // --- Resources ---

        private static Mesh BuildGoldNugget()
        {
            return Combine(
                Box(0, 0, 0, 1.5f, 0.85f, 1.2f),
                Box(0.55f, 0.75f, 0.2f, 0.95f, 1.0f, 0.8f),
                Box(-0.5f, 0.6f, -0.3f, 0.8f, 0.85f, 0.7f),
                Box(0.05f, 1.45f, 0, 0.6f, 0.7f, 0.55f),
                Box(-0.15f, 1.9f, 0.1f, 0.35f, 0.45f, 0.35f));
        }

        private static Mesh BuildTimberLog()
        {
            return Combine(
                Box(0, 0.4f, 0, 2.6f, 0.75f, 0.75f),
                Box(-1.05f, 0.0f, 0, 0.6f, 0.8f, 0.6f),
                Box(1.05f, 0.0f, 0, 0.6f, 0.8f, 0.6f),
                Box(0.25f, 0.95f, 0.2f, 1.5f, 0.5f, 0.5f),
                Box(-0.2f, 1.2f, -0.15f, 0.9f, 0.35f, 0.35f));
        }

        private static Mesh Combine(params MeshPart[] parts)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            foreach (var part in parts)
            {
                int offset = verts.Count;
                verts.AddRange(part.Verts);
                for (int i = 0; i < part.Tris.Length; i++)
                    tris.Add(part.Tris[i] + offset);
            }

            var mesh = new Mesh { name = "combined" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static MeshPart Box(float cx, float cy, float cz, float sx, float sy, float sz)
        {
            float hx = sx * 0.5f;
            float hz = sz * 0.5f;
            var v = new[]
            {
                new Vector3(cx - hx, cy, cz - hz),
                new Vector3(cx + hx, cy, cz - hz),
                new Vector3(cx + hx, cy, cz + hz),
                new Vector3(cx - hx, cy, cz + hz),
                new Vector3(cx - hx, cy + sy, cz - hz),
                new Vector3(cx + hx, cy + sy, cz - hz),
                new Vector3(cx + hx, cy + sy, cz + hz),
                new Vector3(cx - hx, cy + sy, cz + hz),
            };
            var t = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                3, 6, 2, 3, 7, 6,
                0, 4, 7, 0, 7, 3,
                1, 2, 6, 1, 6, 5,
            };
            return new MeshPart(v, t);
        }

        private readonly struct MeshPart
        {
            public readonly Vector3[] Verts;
            public readonly int[] Tris;

            public MeshPart(Vector3[] verts, int[] tris)
            {
                Verts = verts;
                Tris = tris;
            }
        }

        private static Mesh BuildTree()
        {
            return Combine(
                Box(0f, 1.1f, 0f, 0.35f, 2.2f, 0.35f),
                Box(0f, 3.0f, 0f, 1.6f, 1.4f, 1.6f),
                Box(0f, 4.1f, 0f, 1.1f, 1.0f, 1.1f));
        }

        private static Mesh BuildRock()
        {
            return Combine(
                Box(0f, 0.55f, 0f, 1.4f, 1.1f, 1.2f),
                Box(0.35f, 0.85f, 0.2f, 0.8f, 0.7f, 0.7f));
        }

        private static Mesh BuildBridge()
        {
            return Combine(
                Box(0f, 0.35f, 0f, 6.5f, 0.35f, 2.2f),
                Box(-3.0f, 0.9f, -1.0f, 0.35f, 1.4f, 0.35f),
                Box(-3.0f, 0.9f, 1.0f, 0.35f, 1.4f, 0.35f),
                Box(3.0f, 0.9f, -1.0f, 0.35f, 1.4f, 0.35f),
                Box(3.0f, 0.9f, 1.0f, 0.35f, 1.4f, 0.35f));
        }
    }
}
