using System;
using System.Collections.Generic;
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
            Apply(
                world,
                ids,
                new[]
                {
                    new PlayerSlotState
                    {
                        Player = westPlayer,
                        FactionIndex = westFaction != null ? westFaction.Id.Value : (byte)0,
                        IsReady = true,
                    },
                    new PlayerSlotState
                    {
                        Player = eastPlayer,
                        FactionIndex = eastFaction != null ? eastFaction.Id.Value : (byte)1,
                        IsReady = true,
                    },
                },
                map);
        }

        public static void Apply(
            SkirmishWorldSim world,
            IIdFactory ids,
            PlayerSlotState[] seats,
            MapDefinition map)
        {
            if (world == null || map == null)
                throw new ArgumentNullException(map == null ? nameof(map) : nameof(world));
            if (seats == null || seats.Length < 2)
                throw new ArgumentException("Need at least two player seats.", nameof(seats));
            map.EnsureArrays();

            ApplyTerrain(world.Environment, map);
            ApplyTraversal(world.Environment, map);
            ApplySpawns(world, ids, seats, map);
            var armed = new HashSet<byte>();
            for (int i = 0; i < seats.Length; i++)
            {
                var slot = seats[i];
                if (!armed.Add(slot.Player.Value))
                    continue;
                var faction = FactionDefaultContent.Get(new FactionId(slot.FactionIndex));
                EnsureMinimumStartingArmy(world, ids, slot.Player, faction);
            }

            ApplyDestructibles(world, ids, map);
        }

        /// <summary>
        /// Ensure each seat has a starting worker near the keep. Combat is trained in-match, not gifted.
        /// </summary>
        public static void EnsureMinimumStartingArmy(
            SkirmishWorldSim world,
            IIdFactory ids,
            PlayerId player,
            FactionRoster faction)
        {
            if (world == null || faction == null || ids == null)
                return;
            if (string.IsNullOrEmpty(faction.BuilderUnitId))
                return;

            for (int k = 0; k < world.Buildings.Count; k++)
            {
                var b = world.Buildings[k];
                if (b.Owner != player || b.State == BuildingState.Destroyed)
                    continue;
                if (!FactionDefaultContent.IsKeepBuildingId(b.DefinitionId))
                    continue;

                float keepX = b.X;
                float keepZ = b.Z;
                int buildersNear = 0;
                for (int i = 0; i < world.Units.Count; i++)
                {
                    var u = world.Units[i];
                    if (u.Owner != player || !u.IsAlive || !FactionDefaultContent.IsBuilderUnitId(u.DefinitionId))
                        continue;
                    float dx = u.X - keepX;
                    float dz = u.Z - keepZ;
                    if (dx * dx + dz * dz <= 48f * 48f)
                        buildersNear++;
                }

                if (buildersNear >= 1)
                    continue;

                float side = keepX < 0f ? 1f : -1f;
                if (Math.Abs(keepX) < 1f)
                    side = -1f;
                world.SpawnUnit(
                    ids.Next(),
                    player,
                    faction.Id,
                    faction.BuilderUnitId,
                    keepX + side * 28f,
                    keepZ);
            }
        }

        public static void ApplyTerrain(WorldEnvironmentSim environment, MapDefinition map)
        {
            if (environment?.Grid == null || map == null)
                return;

            var grid = environment.Grid;
            map.EnsureArrays();

            for (int i = 0; i < map.terrain.Length; i++)
                ApplyPaint(grid, map.terrain[i]);

            if (map.texturePaint != null)
            {
                for (int i = 0; i < map.texturePaint.Length; i++)
                    ApplyTextureAsTerrain(grid, map.texturePaint[i]);
            }

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
                    CapabilitiesFor(type),
                    link.durationSeconds > 0.05f ? link.durationSeconds : 1.25f,
                    allowsCombat: false,
                    enabled: link.enabled,
                    isDestructible: true,
                    canBeBlocked: true,
                    requiresAnimation: false,
                    approachRadius: link.approachRadius > 0.5f ? link.approachRadius : 8f);
            }
        }

        private static TraversalCapability CapabilitiesFor(TraversalLinkType type)
        {
            switch (type)
            {
                case TraversalLinkType.MagicCrossing:
                    return TraversalCapability.Magic;
                case TraversalLinkType.JumpUp:
                    return TraversalCapability.Jump;
                case TraversalLinkType.ShoreTransition:
                    return TraversalCapability.Amphibious;
                default:
                    return TraversalCapability.Land;
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

        private static void ApplyTextureAsTerrain(WorldTerrainGrid grid, MapTexturePaint paint)
        {
            if (paint == null)
                return;
            ushort def = TerrainSplat.DefIndexForLayer(paint.layer);
            string shape = string.IsNullOrEmpty(paint.shape) ? "disk" : paint.shape.ToLowerInvariant();
            if (shape == "disk")
            {
                float r = paint.radius > 0.5f ? paint.radius : 16f;
                grid.FillWorldRect(paint.x - r, paint.z - r, paint.x + r, paint.z + r, def);
                return;
            }

            grid.FillWorldRect(paint.minX, paint.minZ, paint.maxX, paint.maxZ, def);
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
            PlayerSlotState[] seats,
            MapDefinition map)
        {
            for (int i = 0; i < map.keeps.Length; i++)
            {
                var k = map.keeps[i];
                if (!TrySeat(seats, k.seatIndex, out var player, out var faction))
                    continue;
                world.SpawnBuilding(
                    ids.Next(), player, faction.Id, faction.KeepBuildingId, k.x, k.z, startActive: true);
            }

            for (int i = 0; i < map.buildings.Length; i++)
            {
                var b = map.buildings[i];
                if (!TrySeat(seats, b.seatIndex, out var player, out var faction))
                    continue;
                string defId = ResolveBuildingRole(b.role, faction);
                if (string.IsNullOrEmpty(defId))
                    continue;
                var building = world.SpawnBuilding(
                    ids.Next(), player, faction.Id, defId, b.x, b.z, startActive: true);
                if (building != null)
                    building.YawDegrees = b.yawDegrees;
            }

            for (int i = 0; i < map.units.Length; i++)
            {
                var u = map.units[i];
                if (!TrySeat(seats, u.seatIndex, out var player, out var faction))
                    continue;
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
        }

        public static void ApplyDestructibles(SkirmishWorldSim world, IIdFactory ids, MapDefinition map)
        {
            for (int i = 0; i < map.destructibles.Length; i++)
            {
                var d = map.destructibles[i];
                var def = ResolveDestructible(d.catalogId);
                if (def == null)
                    continue;
                int linkId = d.linkedTraversalLinkId;
                if (linkId >= 0 && world.Environment?.TraversalGraph != null)
                {
                    var links = world.Environment.TraversalGraph.Links;
                    if (linkId < links.Count)
                        linkId = links[linkId].Id;
                    else
                        linkId = -1;
                }

                world.SpawnDestructible(ids.Next(), def, d.x, d.z, linkId, d.yawDegrees);
            }
        }

        private static bool TrySeat(
            PlayerSlotState[] seats,
            int seatIndex,
            out PlayerId player,
            out FactionRoster faction)
        {
            player = default;
            faction = null;
            if (seats == null || seatIndex < 0 || seatIndex >= seats.Length)
                return false;
            var slot = seats[seatIndex];
            faction = FactionDefaultContent.Get(new FactionId(slot.FactionIndex));
            if (faction == null)
                return false;
            player = slot.Player;
            return true;
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
                    return faction.CavalryUnitId;
                case "elite":
                    return !string.IsNullOrEmpty(faction.EliteUnitId)
                        ? faction.EliteUnitId
                        : faction.CavalryUnitId;
                case "siege":
                    return faction.SiegeUnitId;
                case "leader":
                    return faction.LeaderUnitId;
                case "boat":
                    return FactionDefaultContent.RiverBoatId;
                case "pathfinder":
                case "scout":
                    return !string.IsNullOrEmpty(faction.ScoutUnitId)
                        ? faction.ScoutUnitId
                        : FactionDefaultContent.PathfinderId;
                case "sapper":
                    return !string.IsNullOrEmpty(faction.SapperUnitId)
                        ? faction.SapperUnitId
                        : FactionDefaultContent.SapperId;
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
            return DefaultDestructibleCatalog.FromCatalogId(catalogId);
        }
    }
}
