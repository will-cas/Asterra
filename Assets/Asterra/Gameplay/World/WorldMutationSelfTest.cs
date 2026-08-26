using System.Text;
using Asterra.Core;
using Asterra.Core.World;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>Builder world mutations: earthworks, vegetation, hydro, fortification, aftermath.</summary>
    public static class WorldMutationSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "dig trench paints cells", DigTrenchPaints());
            Expect(ref fails, sb, "fill trench restores grass", FillTrench());
            Expect(ref fails, sb, "raise berm", RaiseBerm());
            Expect(ref fails, sb, "clear forest drops timber", ClearForest());
            Expect(ref fails, sb, "quarry rock drops gold", QuarryRock());
            Expect(ref fails, sb, "chop tree drops timber", ChopTreeDropsTimber());
            Expect(ref fails, sb, "faction bridge adds link", FactionBridgeAddsLink());
            Expect(ref fails, sb, "demolish bridge disables link", DemolishBridgeDisablesLink());
            Expect(ref fails, sb, "repair collapsed bridge", RepairBridge());
            Expect(ref fails, sb, "raze wall refunds timber", RazeWall());
            Expect(ref fails, sb, "building death leaves debris", AftermathDebris());

            sb.Append(fails == 0 ? "WorldMutationSelfTest: OK" : $"WorldMutationSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool DigTrenchPaints()
        {
            Boot(out var sim, out _, out var wallet, out var p, out var builder);
            wallet.Seed(p, ResourceType.Gold, 200);
            wallet.Seed(p, ResourceType.Timber, 100);
            builder.X = 10f;
            builder.Z = 10f;
            sim.ApplyCommands(new GameCommand[]
            {
                new DigTrenchCommand { Issuer = p, X = 10f, Z = 10f, HalfExtent = 6f },
            });
            return sim.Environment.Grid.TryGetCell(10f, 10f, out var cell)
                   && cell.TerrainDefIndex == DefaultTerrainCatalog.Trench;
        }

        private static bool FillTrench()
        {
            Boot(out var sim, out _, out var wallet, out var p, out var builder);
            wallet.Seed(p, ResourceType.Gold, 400);
            wallet.Seed(p, ResourceType.Timber, 200);
            builder.X = 12f;
            builder.Z = 12f;
            sim.ApplyCommands(new GameCommand[]
            {
                new TerrainWorkCommand { Issuer = p, Kind = TerrainWorkKind.DigTrench, X = 12f, Z = 12f, HalfExtent = 6f },
                new TerrainWorkCommand { Issuer = p, Kind = TerrainWorkKind.FillTrench, X = 12f, Z = 12f, HalfExtent = 6f },
            });
            return sim.Environment.Grid.TryGetCell(12f, 12f, out var cell)
                   && cell.TerrainDefIndex == DefaultTerrainCatalog.GrassShort;
        }

        private static bool RaiseBerm()
        {
            Boot(out var sim, out _, out var wallet, out var p, out var builder);
            wallet.Seed(p, ResourceType.Gold, 200);
            wallet.Seed(p, ResourceType.Timber, 100);
            builder.X = 20f;
            builder.Z = 20f;
            sim.ApplyCommands(new GameCommand[]
            {
                new TerrainWorkCommand { Issuer = p, Kind = TerrainWorkKind.RaiseBerm, X = 20f, Z = 20f, HalfExtent = 5f },
            });
            return sim.Environment.Grid.TryGetCell(20f, 20f, out var cell)
                   && cell.TerrainDefIndex == DefaultTerrainCatalog.Berm;
        }

        private static bool ClearForest()
        {
            Boot(out var sim, out _, out var wallet, out var p, out var builder);
            wallet.Seed(p, ResourceType.Gold, 200);
            sim.Environment.Grid.FillWorldRect(50f, 50f, 70f, 70f, DefaultTerrainCatalog.Forest);
            builder.X = 60f;
            builder.Z = 60f;
            int timber0 = CountTimber(sim);
            sim.ApplyCommands(new GameCommand[]
            {
                new TerrainWorkCommand { Issuer = p, Kind = TerrainWorkKind.ClearForest, X = 60f, Z = 60f, HalfExtent = 8f },
            });
            return CountTimber(sim) > timber0
                   && sim.Environment.Grid.TryGetCell(60f, 60f, out var cell)
                   && cell.TerrainDefIndex == DefaultTerrainCatalog.GrassShort;
        }

        private static bool QuarryRock()
        {
            Boot(out var sim, out _, out var wallet, out var p, out var builder);
            wallet.Seed(p, ResourceType.Gold, 200);
            wallet.Seed(p, ResourceType.Timber, 50);
            sim.Environment.Grid.FillWorldRect(80f, 80f, 100f, 100f, DefaultTerrainCatalog.Rock);
            builder.X = 90f;
            builder.Z = 90f;
            int goldNodes = CountGold(sim);
            sim.ApplyCommands(new GameCommand[]
            {
                new TerrainWorkCommand { Issuer = p, Kind = TerrainWorkKind.QuarryRock, X = 90f, Z = 90f, HalfExtent = 6f },
            });
            return CountGold(sim) > goldNodes;
        }

        private static bool ChopTreeDropsTimber()
        {
            Boot(out var sim, out var ids, out _, out _, out var builder);
            var tree = sim.SpawnDestructible(ids.Next(), DefaultDestructibleCatalog.Tree(), 20f, 0f);
            int timberNodes = CountTimber(sim);
            builder.X = 20f;
            builder.Z = 0f;
            for (int i = 0; i < 40; i++)
                sim.ApplyWorldDamage(tree.Id, 20f, vsStructure: true);
            return !tree.IsAlive && CountTimber(sim) > timberNodes;
        }

        private static bool FactionBridgeAddsLink()
        {
            Boot(out var sim, out _, out var wallet, out var p, out var builder);
            wallet.Seed(p, ResourceType.Gold, 500);
            wallet.Seed(p, ResourceType.Timber, 500);
            sim.Environment.Grid.FillWorldRect(40f, -8f, 80f, 8f, DefaultTerrainCatalog.WaterDeep);
            sim.Environment.Grid.FillWorldRect(28f, -8f, 38f, 8f, DefaultTerrainCatalog.GrassShort);
            builder.X = 32f;
            builder.Z = 0f;
            int linksBefore = sim.Environment.TraversalGraph.Links.Count;
            sim.ApplyCommands(new GameCommand[]
            {
                new PlaceBuildingCommand
                {
                    Issuer = p,
                    BuildingDefId = FactionDefaultContent.BridgeId,
                    X = 32f,
                    Z = 0f,
                    YawDegrees = 90f,
                },
            });
            for (int i = 0; i < 200; i++)
                sim.Tick(0.05f);

            bool hasBridge = false;
            for (int i = 0; i < sim.Buildings.Count; i++)
            {
                var b = sim.Buildings[i];
                if (b.DefinitionId == FactionDefaultContent.BridgeId && b.State == BuildingState.Active)
                    hasBridge = true;
            }

            bool hasLiveBridgeProp = false;
            for (int i = 0; i < sim.Destructibles.Count; i++)
            {
                if (sim.Destructibles[i].DefinitionId == DefaultDestructibleCatalog.BridgeId
                    && sim.Destructibles[i].State != DestructibleState.Destroyed
                    && sim.Destructibles[i].LinkedTraversalLinkId >= 0)
                    hasLiveBridgeProp = true;
            }

            return hasBridge && hasLiveBridgeProp && sim.Environment.TraversalGraph.Links.Count > linksBefore;
        }

        private static bool DemolishBridgeDisablesLink()
        {
            Boot(out var sim, out _, out var wallet, out var p, out var builder);
            wallet.Seed(p, ResourceType.Gold, 500);
            wallet.Seed(p, ResourceType.Timber, 500);
            sim.Environment.Grid.FillWorldRect(40f, -8f, 80f, 8f, DefaultTerrainCatalog.WaterDeep);
            sim.Environment.Grid.FillWorldRect(28f, -8f, 38f, 8f, DefaultTerrainCatalog.GrassShort);
            builder.X = 32f;
            builder.Z = 0f;
            sim.ApplyCommands(new GameCommand[]
            {
                new PlaceBuildingCommand
                {
                    Issuer = p,
                    BuildingDefId = FactionDefaultContent.BridgeId,
                    X = 32f,
                    Z = 0f,
                    YawDegrees = 90f,
                },
            });
            for (int i = 0; i < 200; i++)
                sim.Tick(0.05f);

            SimEntityId bridgeId = default;
            bool found = false;
            for (int i = 0; i < sim.Buildings.Count; i++)
            {
                var b = sim.Buildings[i];
                if (b.DefinitionId == FactionDefaultContent.BridgeId && b.State == BuildingState.Active)
                {
                    bridgeId = b.Id;
                    found = true;
                    break;
                }
            }

            if (!found)
                return false;

            int linkId = -1;
            for (int i = 0; i < sim.Destructibles.Count; i++)
            {
                if (sim.Destructibles[i].DefinitionId == DefaultDestructibleCatalog.BridgeId
                    && sim.Destructibles[i].State != DestructibleState.Destroyed)
                {
                    linkId = sim.Destructibles[i].LinkedTraversalLinkId;
                    break;
                }
            }

            sim.ApplyCommands(new GameCommand[]
            {
                new DemolishBuildingCommand { Issuer = p, BuildingId = bridgeId },
            });

            if (linkId < 0 || linkId >= sim.Environment.TraversalGraph.Links.Count)
                return false;
            return !sim.Environment.TraversalGraph.Links[linkId].Enabled;
        }

        private static bool RepairBridge()
        {
            Boot(out var sim, out _, out var wallet, out var p, out var builder);
            wallet.Seed(p, ResourceType.Gold, 800);
            wallet.Seed(p, ResourceType.Timber, 800);
            int linkId = sim.Environment.TraversalGraph.AddLink(
                -10f, 0f, 10f, 0f, TraversalLinkType.Bridge, TraversalCapability.Land,
                enabled: false, isDestructible: true);
            builder.X = 0f;
            builder.Z = 0f;
            sim.ApplyCommands(new GameCommand[]
            {
                new RepairBridgeCommand { Issuer = p, X = 0f, Z = 0f },
            });
            return sim.Environment.TraversalGraph.Links[linkId].Enabled;
        }

        private static bool RazeWall()
        {
            Boot(out var sim, out var ids, out var wallet, out var p, out _);
            wallet.Seed(p, ResourceType.Gold, 100);
            wallet.Seed(p, ResourceType.Timber, 0);
            var wall = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.PalisadeId, 5f, 5f, startActive: true);
            int timber0 = wallet.Get(p, ResourceType.Timber);
            sim.ApplyCommands(new GameCommand[]
            {
                new DemolishBuildingCommand { Issuer = p, BuildingId = wall.Id, RazeForMaterials = true },
            });
            return wallet.Get(p, ResourceType.Timber) > timber0;
        }

        private static bool AftermathDebris()
        {
            Boot(out var sim, out var ids, out _, out var p, out _);
            var tower = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.WatchtowerId, 15f, 15f, startActive: true);
            sim.ApplyWorldDamage(tower.Id, 9999f, vsStructure: true);
            return sim.Environment.Grid.TryGetCell(15f, 15f, out var cell)
                   && (cell.TerrainDefIndex == DefaultTerrainCatalog.Debris
                       || cell.TerrainDefIndex == DefaultTerrainCatalog.Crater);
        }

        private static int CountTimber(SkirmishWorldSim sim)
        {
            int n = 0;
            for (int i = 0; i < sim.Resources.Count; i++)
            {
                if (sim.Resources[i].Type == ResourceType.Timber && sim.Resources[i].Remaining > 0)
                    n++;
            }

            return n;
        }

        private static int CountGold(SkirmishWorldSim sim)
        {
            int n = 0;
            for (int i = 0; i < sim.Resources.Count; i++)
            {
                if (sim.Resources[i].Type == ResourceType.Gold && sim.Resources[i].Remaining > 0)
                    n++;
            }

            return n;
        }

        private static void Boot(
            out SkirmishWorldSim sim,
            out SequentialIdFactory ids,
            out ResourceWallet wallet,
            out PlayerId p,
            out SimUnit builder)
        {
            ids = new SequentialIdFactory();
            wallet = new ResourceWallet();
            var defs = SkirmishDefaultContent.CreateRegistry();
            sim = new SkirmishWorldSim(wallet, ids, defs);
            p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 100);
            wallet.Seed(p, ResourceType.Timber, 100);
            builder = sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.IronBuilderId, 0f, 0f);
        }

        private static void Expect(ref int fails, StringBuilder sb, string name, bool ok)
        {
            if (ok)
                return;
            fails++;
            sb.AppendLine("  fail: " + name);
        }
    }
}
