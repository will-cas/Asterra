using System;
using Asterra.Core;
using Asterra.Core.World;
using Asterra.Gameplay.Sim;
using Asterra.Gameplay.World;

namespace Asterra.Gameplay.Content
{
    /// <summary>Applies a <see cref="MapDefinition"/> into the lockstep world (deterministic order).</summary>
    public static class SkirmishMapLoader
    {
        public static void Apply(
            SkirmishWorldSim world,
            IIdFactory ids,
            PlayerId westPlayer,
            FactionRoster westFaction,
            PlayerId eastPlayer,
            FactionRoster eastFaction,
            MapDefinition map)
        {
            if (world == null || map == null)
                throw new ArgumentNullException(map == null ? nameof(map) : nameof(world));
            map.EnsureArrays();

            ApplyTerrain(world.Environment, map);
            ApplyTraversal(world.Environment, map);
            ApplySpawns(world, ids, westPlayer, westFaction, eastPlayer, eastFaction, map);
            ApplyDestructibles(world, ids, map);
        }

        public static void ApplyTerrain(WorldEnvironmentSim environment, MapDefinition map)
        {
            if (environment?.Grid == null || map == null)
                return;

            var grid = environment.Grid;
            map.EnsureArrays();

            for (int i = 0; i < map.terrain.Length; i++)
                ApplyPaint(grid, map.terrain[i]);

            for (int i = 0; i < map.blocked.Length; i++)
            {
                var b = map.blocked[i];
                grid.SetBlockedRect(b.minX, b.minZ, b.maxX, b.maxZ, b.blocked);
            }

            environment.RebuildFeatureIndex();
        }

        public static void ApplyTraversal(WorldEnvironmentSim environment, MapDefinition map)
        {
            if (environment?.TraversalGraph == null || map == null)
                return;
            map.EnsureArrays();
            for (int i = 0; i < map.traversalLinks.Length; i++)
            {
                var link = map.traversalLinks[i];
                if (link == null)
                    continue;
                var type = ResolveLinkType(link.type);
                environment.TraversalGraph.AddLink(
                    link.startX,
                    link.startZ,
                    link.endX,
                    link.endZ,
                    type,
                    TraversalCapability.Land,
                    link.durationSeconds > 0.05f ? link.durationSeconds : 1.25f,
                    allowsCombat: false,
                    enabled: link.enabled,
                    isDestructible: true,
                    canBeBlocked: true,
                    requiresAnimation: false,
                    approachRadius: link.approachRadius > 0.5f ? link.approachRadius : 8f);
            }
        }

        private static TraversalLinkType ResolveLinkType(string type)
        {
            switch ((type ?? "bridge").ToLowerInvariant())
            {
                case "jump":
                case "jumpover":
                    return TraversalLinkType.JumpOver;
                case "jumpdown":
                    return TraversalLinkType.JumpDown;
                case "jumpup":
                    return TraversalLinkType.JumpUp;
                case "shore":
                case "ford":
                    return TraversalLinkType.ShoreTransition;
                case "magic":
                    return TraversalLinkType.MagicCrossing;
                case "treegap":
                    return TraversalLinkType.TreeGap;
                default:
                    return TraversalLinkType.Bridge;
            }
        }

        private static void ApplyPaint(WorldTerrainGrid grid, MapTerrainPaint paint)
        {
            if (paint == null)
                return;
            ushort def = ResolveTerrain(grid, paint);
            string shape = string.IsNullOrEmpty(paint.shape) ? "rect" : paint.shape.ToLowerInvariant();
            if (shape == "disk")
            {
                float cx = paint.x;
                float cz = paint.z;
                if (Math.Abs(cx) < 0.0001f && Math.Abs(cz) < 0.0001f
                    && (Math.Abs(paint.minX) > 0.01f || Math.Abs(paint.maxX) > 0.01f))
                {
                    cx = (paint.minX + paint.maxX) * 0.5f;
                    cz = (paint.minZ + paint.maxZ) * 0.5f;
                }

                float r = paint.radius > 0.5f ? paint.radius : 10f;
                grid.FillWorldRect(cx - r, cz - r, cx + r, cz + r, def);
                return;
            }

            grid.FillWorldRect(paint.minX, paint.minZ, paint.maxX, paint.maxZ, def);
        }

        private static ushort ResolveTerrain(WorldTerrainGrid grid, MapTerrainPaint paint)
        {
            if (!string.IsNullOrEmpty(paint.terrainId) && grid.TryGetDefIndex(paint.terrainId, out int idx))
                return (ushort)idx;
            return paint.terrainIndex;
        }

        private static void ApplySpawns(
            SkirmishWorldSim world,
            IIdFactory ids,
            PlayerId westPlayer,
            FactionRoster westFaction,
            PlayerId eastPlayer,
            FactionRoster eastFaction,
            MapDefinition map)
        {
            for (int i = 0; i < map.keeps.Length; i++)
            {
                var k = map.keeps[i];
                ResolveSeat(k.seatIndex, westPlayer, westFaction, eastPlayer, eastFaction,
                    out var player, out var faction);
                world.SpawnBuilding(
                    ids.Next(), player, faction.Id, faction.KeepBuildingId, k.x, k.z, startActive: true);
            }

            for (int i = 0; i < map.buildings.Length; i++)
            {
                var b = map.buildings[i];
                ResolveSeat(b.seatIndex, westPlayer, westFaction, eastPlayer, eastFaction,
                    out var player, out var faction);
                string defId = ResolveBuildingRole(b.role, faction);
                if (string.IsNullOrEmpty(defId))
                    continue;
                var building = world.SpawnBuilding(
                    ids.Next(), player, faction.Id, defId, b.x, b.z, startActive: true);
                if (building != null)
                {
                    building.YawDegrees = b.yawDegrees;
                }
            }

            for (int i = 0; i < map.units.Length; i++)
            {
                var u = map.units[i];
                ResolveSeat(u.seatIndex, westPlayer, westFaction, eastPlayer, eastFaction,
                    out var player, out var faction);
                string defId = ResolveUnitRole(u.role, faction);
                if (string.IsNullOrEmpty(defId))
                    continue;
                world.SpawnUnit(ids.Next(), player, faction.Id, defId, u.x, u.z);
            }

            for (int i = 0; i < map.territories.Length; i++)
            {
                var t = map.territories[i];
                world.AddTerritory(
                    ids.Next(),
                    t.x,
                    t.z,
                    t.radius > 1f ? t.radius : 40f,
                    t.goldPerSecond > 0 ? t.goldPerSecond : 8);
            }

            for (int i = 0; i < map.resources.Length; i++)
            {
                var r = map.resources[i];
                var type = string.Equals(r.type, "timber", StringComparison.OrdinalIgnoreCase)
                    ? ResourceType.Timber
                    : ResourceType.Gold;
                int amount = r.amount > 0 ? r.amount : 500;
                world.AddResourceNode(ids.Next(), type, amount, r.x, r.z);
            }
        }

        private static void ApplyDestructibles(SkirmishWorldSim world, IIdFactory ids, MapDefinition map)
        {
            for (int i = 0; i < map.destructibles.Length; i++)
            {
                var d = map.destructibles[i];
                var def = ResolveDestructible(d.catalogId);
                if (def == null)
                    continue;
                world.SpawnDestructible(ids.Next(), def, d.x, d.z, d.linkedTraversalLinkId);
            }
        }

        private static void ResolveSeat(
            int seatIndex,
            PlayerId westPlayer,
            FactionRoster westFaction,
            PlayerId eastPlayer,
            FactionRoster eastFaction,
            out PlayerId player,
            out FactionRoster faction)
        {
            if (seatIndex <= 0)
            {
                player = westPlayer;
                faction = westFaction;
            }
            else
            {
                player = eastPlayer;
                faction = eastFaction;
            }
        }

        private static string ResolveUnitRole(string role, FactionRoster faction)
        {
            if (faction == null)
                return null;
            switch ((role ?? "basic").ToLowerInvariant())
            {
                case "builder":
                    return faction.BuilderUnitId;
                case "ranged":
                    return faction.RangedUnitId;
                case "cavalry":
                case "elite":
                    return faction.CavalryUnitId;
                case "siege":
                    return faction.SiegeUnitId;
                case "leader":
                    return faction.LeaderUnitId;
                case "boat":
                    return FactionDefaultContent.RiverBoatId;
                case "pathfinder":
                    return FactionDefaultContent.PathfinderId;
                default:
                    return faction.BasicUnitId;
            }
        }

        private static string ResolveBuildingRole(string role, FactionRoster faction)
        {
            if (faction == null)
                return null;
            switch ((role ?? "tower").ToLowerInvariant())
            {
                case "wall":
                case "palisade":
                    return faction.WallBuildingId;
                case "producer":
                case "barracks":
                    return faction.ProducerBuildingId;
                case "outpost":
                case "mine":
                    return faction.OutpostBuildingId;
                case "keep":
                    return faction.KeepBuildingId;
                default:
                    return faction.TowerBuildingId;
            }
        }

        private static DestructibleDefData ResolveDestructible(string catalogId)
        {
            if (string.IsNullOrEmpty(catalogId))
                return DefaultDestructibleCatalog.Rock();
            string id = catalogId.ToLowerInvariant();
            if (id.Contains("tree"))
                return DefaultDestructibleCatalog.Tree();
            if (id.Contains("bridge"))
                return DefaultDestructibleCatalog.Bridge();
            return DefaultDestructibleCatalog.Rock();
        }
    }
}
