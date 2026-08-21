using System.Text;
using Asterra.Core;
using Asterra.Core.World;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>
    /// Editor-free regression driver: advances a skirmish for N ticks and reports summary stats.
    /// Call from MatchBootstrap (runSmokeOnAwake) or later from an Editor menu.
    /// </summary>
    public static class SkirmishSmokeTest
    {
        public static string Run(int ticks = 2000)
        {
            var worldReport = WorldTerrainGridSelfTest.Run();
            if (worldReport.IndexOf("FAIL", System.StringComparison.Ordinal) >= 0)
                return worldReport + "\nWorld terrain self-test failed — aborting further smoke.";

            var envReport = WorldEnvironmentSelfTest.Run();
            if (envReport.IndexOf("FAIL", System.StringComparison.Ordinal) >= 0)
                return envReport + "\nWorld environment self-test failed — aborting further smoke.";

            var mapTerrainReport = SkirmishMapTerrainSelfTest.Run();
            if (mapTerrainReport.IndexOf("FAIL", System.StringComparison.Ordinal) >= 0)
                return mapTerrainReport + "\nMap terrain self-test failed — aborting further smoke.";

            var traversalReport = TraversalSelfTest.Run();
            if (traversalReport.IndexOf("FAIL", System.StringComparison.Ordinal) >= 0)
                return traversalReport + "\nTraversal self-test failed — aborting further smoke.";

            var destructionReport = DestructionSelfTest.Run();
            if (destructionReport.IndexOf("FAIL", System.StringComparison.Ordinal) >= 0)
                return destructionReport + "\nDestruction self-test failed — aborting further smoke.";

            var weatherReport = WeatherSelfTest.Run();
            if (weatherReport.IndexOf("FAIL", System.StringComparison.Ordinal) >= 0)
                return weatherReport + "\nWeather self-test failed — aborting further smoke.";

            var timeReport = TimeOfDaySelfTest.Run();
            if (timeReport.IndexOf("FAIL", System.StringComparison.Ordinal) >= 0)
                return timeReport + "\nTime-of-day self-test failed — aborting further smoke.";

            var buildingReport = BuildingSystemsSelfTest.Run();
            if (buildingReport.IndexOf("FAIL", System.StringComparison.Ordinal) >= 0)
                return buildingReport + "\nBuilding systems self-test failed — aborting further smoke.";

            var pathReport = PathfindingSelfTest.Run();
            if (pathReport.IndexOf("FAIL", System.StringComparison.Ordinal) >= 0)
                return pathReport + "\nPathfinding self-test failed — aborting further smoke.";

            var codecReport = CommandCodecSelfTest.Run();

            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = SkirmishDefaultContent.CreateRegistry();
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var clock = new LockstepClock(0.05f, commandDelayTicks: 2);
            var bus = new CommandBus();
            var replay = new ReplayBuffer();

            var player = new PlayerId(0);
            var enemy = new PlayerId(1);
            var playerFaction = FactionDefaultContent.IronCovenant;
            var enemyFaction = FactionDefaultContent.VerdantCourt;
            wallet.Seed(player, ResourceType.Gold, 500);
            wallet.Seed(player, ResourceType.Timber, 300);
            wallet.Seed(enemy, ResourceType.Gold, 500);
            wallet.Seed(enemy, ResourceType.Timber, 300);
            SkirmishDefaultContent.PopulateInitialWorld(sim, ids, playerFaction, enemyFaction);

            var owned = new System.Collections.Generic.List<SimEntityId>();
            for (int i = 0; i < sim.Units.Count; i++)
            {
                if (sim.Units[i].Owner == player)
                    owned.Add(sim.Units[i].Id);
            }

            var openers = new CommandFrame
            {
                TargetTick = new Tick(2),
                Player = player,
                Commands = new GameCommand[]
                {
                    new PlaceBuildingCommand
                    {
                        Issuer = player,
                        BuildingDefId = playerFaction.ProducerBuildingId,
                        X = -300f,
                        Z = 25f,
                    },
                    new MoveCommand
                    {
                        Issuer = player,
                        UnitIds = owned.ToArray(),
                        TargetX = 0f,
                        TargetZ = 0f,
                    },
                },
            };
            replay.Record(openers);
            // Prove codec path used by NGO bridge.
            bus.EnqueueRemote(CommandCodec.DeserializeFrame(CommandCodec.SerializeFrame(openers)));
            bus.EnqueueRemote(new CommandFrame
            {
                TargetTick = new Tick(5),
                Player = player,
                Commands = new GameCommand[]
                {
                    new ActivateCommanderAbilityCommand
                    {
                        Issuer = player,
                    },
                },
            });
            bus.EnqueueRemote(new CommandFrame
            {
                TargetTick = new Tick(40),
                Player = player,
                Commands = new GameCommand[]
                {
                    new CaptureTerritoryCommand
                    {
                        Issuer = player,
                        TerritoryNodeId = sim.Territories[0].Id,
                    },
                },
            });
            bus.EnqueueRemote(new CommandFrame
            {
                TargetTick = new Tick(200),
                Player = player,
                Commands = new GameCommand[]
                {
                    new ChooseUpgradeCommand
                    {
                        Issuer = player,
                        UpgradeDefId = playerFaction.BasicUpgradeId,
                    },
                },
            });

            for (int i = 0; i < ticks; i++)
            {
                if (clock.CurrentTick.Value == 160)
                {
                    SimEntityId barracks = default;
                    bool found = false;
                    for (int b = 0; b < sim.Buildings.Count; b++)
                    {
                        if (sim.Buildings[b].DefinitionId == playerFaction.ProducerBuildingId
                            && sim.Buildings[b].Owner == player)
                        {
                            barracks = sim.Buildings[b].Id;
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        bus.EnqueueRemote(new CommandFrame
                        {
                            TargetTick = clock.CurrentTick,
                            Player = player,
                            Commands = new GameCommand[]
                            {
                                new TrainUnitCommand
                                {
                                    Issuer = player,
                                    BuildingId = barracks,
                                    UnitDefId = playerFaction.BasicUnitId,
                                },
                            },
                        });
                    }
                }

                var commands = bus.DrainForTick(clock.CurrentTick);
                sim.ApplyCommands(commands);
                sim.Tick(clock.FixedDeltaSeconds);
                clock.Advance();
            }

            var sb = new StringBuilder();
            sb.AppendLine(worldReport);
            sb.AppendLine(envReport);
            sb.AppendLine(mapTerrainReport);
            sb.AppendLine(traversalReport);
            sb.AppendLine(destructionReport);
            sb.AppendLine(weatherReport);
            sb.AppendLine(timeReport);
            sb.AppendLine(buildingReport);
            sb.AppendLine(pathReport);
            sb.Append(codecReport);
            sb.Append(LockstepFrameGateSelfTest.Run());
            sb.Append(MatchLobbyStateSelfTest.Run());
            sb.Append(VictoryEvaluatorSelfTest.Run());
            sb.AppendLine("[Asterra Smoke]");
            sb.AppendLine($"factions={playerFaction.DisplayName} vs {enemyFaction.DisplayName}");
            sb.AppendLine($"ticks={ticks} hash={sim.ComputeWorldHash()} replayFrames={replay.Count}");
            sb.AppendLine($"units={sim.Units.Count} buildings={sim.Buildings.Count}");
            sb.AppendLine($"goldP0={wallet.Get(player, ResourceType.Gold)} goldP1={wallet.Get(enemy, ResourceType.Gold)}");
            if (sim.Territories.Count > 0)
            {
                var t = sim.Territories[0];
                sb.AppendLine(
                    $"territory state={t.State} controller={(t.HasController ? t.Controller.ToString() : "none")} progress={t.CaptureProgress:0.00}");
            }

            sb.AppendLine($"upgradeP0={sim.HasUpgrade(player, playerFaction.BasicUpgradeId)}");
            sim.TryGetCommanderAbilityStatus(player, out float cd, out float buff);
            sb.AppendLine($"ironWallP0 cd={cd:0.0} buff={buff:0.0}");
            sb.AppendLine($"defsRegistered=iron+verdant+ashen");
            return sb.ToString();
        }
    }
}
