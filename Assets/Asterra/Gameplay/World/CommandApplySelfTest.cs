using System.Collections.Generic;
using System.Text;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>Every CommandType applied against a live sim (smoke + side effects).</summary>
    public static class CommandApplySelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "move", ApplyMove());
            Expect(ref fails, sb, "attack", ApplyAttack());
            Expect(ref fails, sb, "place", ApplyPlace());
            Expect(ref fails, sb, "train", ApplyTrain());
            Expect(ref fails, sb, "gather", ApplyGather());
            Expect(ref fails, sb, "rally", ApplyRally());
            Expect(ref fails, sb, "stance", ApplyStance());
            Expect(ref fails, sb, "stop", ApplyStop());
            Expect(ref fails, sb, "attack-move", ApplyAttackMove());
            Expect(ref fails, sb, "patrol", ApplyPatrol());
            Expect(ref fails, sb, "cancel production", ApplyCancel());
            Expect(ref fails, sb, "attach", ApplyAttach());
            Expect(ref fails, sb, "garrison enter/exit", ApplyGarrison());
            Expect(ref fails, sb, "capture order", ApplyCapture());
            Expect(ref fails, sb, "research", ApplyResearch());
            Expect(ref fails, sb, "unlock+activate power", ApplyPower());
            Expect(ref fails, sb, "codec all command types", CodecAllTypes());

            sb.Append(fails == 0 ? "CommandApplySelfTest: OK" : $"CommandApplySelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool ApplyMove()
        {
            Setup(out var sim, out var ids, out var wallet, out var p, out var unit, out _);
            _ = ids;
            _ = wallet;
            sim.ApplyCommands(new GameCommand[]
            {
                new MoveCommand { Issuer = p, UnitIds = new[] { unit.Id }, TargetX = 50f, TargetZ = 0f },
            });
            return unit.PathCount > 0 || unit.MoveTargetX.HasValue;
        }

        private static bool ApplyAttack()
        {
            Setup(out var sim, out var ids, out _, out var p, out var unit, out _);
            var foe = sim.SpawnUnit(
                ids.Next(), new PlayerId(1), new FactionId(1), FactionDefaultContent.RoyalPeasantId, 8f, 0f);
            sim.ApplyCommands(new GameCommand[]
            {
                new AttackCommand { Issuer = p, UnitIds = new[] { unit.Id }, TargetId = foe.Id },
            });
            return unit.AttackTargetId.HasValue && unit.AttackTargetId.Value.Value == foe.Id.Value;
        }

        private static bool ApplyPlace()
        {
            Setup(out var sim, out _, out var wallet, out var p, out _, out _);
            wallet.Seed(p, ResourceType.Gold, 2000);
            wallet.Seed(p, ResourceType.Timber, 2000);
            int before = sim.Buildings.Count;
            sim.ApplyCommands(new GameCommand[]
            {
                new PlaceBuildingCommand
                {
                    Issuer = p,
                    BuildingDefId = FactionDefaultContent.ArcaneAcademyId,
                    X = 55f,
                    Z = 20f,
                },
            });
            return sim.Buildings.Count == before + 1;
        }

        private static bool ApplyTrain()
        {
            Setup(out var sim, out var ids, out var wallet, out var p, out _, out var keep);
            wallet.Seed(p, ResourceType.Gold, 500);
            sim.ApplyCommands(new GameCommand[]
            {
                new TrainUnitCommand
                {
                    Issuer = p,
                    BuildingId = keep.Id,
                    UnitDefId = FactionDefaultContent.VeiledBuilderId,
                },
            });
            return keep.IsProducing;
        }

        private static bool ApplyGather()
        {
            Setup(out var sim, out var ids, out var wallet, out var p, out _, out _);
            var builder = sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledBuilderId, 20f, 0f);
            var node = ids.Next();
            sim.AddResourceNode(node, ResourceType.Gold, 100, 25f, 0f);
            sim.ApplyCommands(new GameCommand[]
            {
                new GatherCommand { Issuer = p, UnitIds = new[] { builder.Id }, ResourceNodeId = node },
            });
            return builder.GatherTargetId.HasValue;
        }

        private static bool ApplyRally()
        {
            Setup(out var sim, out var ids, out var wallet, out var p, out _, out _);
            wallet.Seed(p, ResourceType.Gold, 500);
            var barracks = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneAcademyId, 30f, 30f, startActive: true);
            sim.ApplyCommands(new GameCommand[]
            {
                new SetRallyCommand
                {
                    Issuer = p,
                    BuildingId = barracks.Id,
                    TargetX = 70f,
                    TargetZ = -10f,
                },
            });
            return barracks.RallyX.HasValue && barracks.RallyX.Value > 60f;
        }

        private static bool ApplyStance()
        {
            Setup(out var sim, out _, out _, out var p, out var unit, out _);
            sim.ApplyCommands(new GameCommand[]
            {
                new SetStanceCommand
                {
                    Issuer = p,
                    UnitIds = new[] { unit.Id },
                    Stance = UnitStance.Passive,
                },
            });
            return unit.Stance == UnitStance.Passive;
        }

        private static bool ApplyStop()
        {
            Setup(out var sim, out _, out _, out var p, out var unit, out _);
            sim.ApplyCommands(new GameCommand[]
            {
                new MoveCommand { Issuer = p, UnitIds = new[] { unit.Id }, TargetX = 90f, TargetZ = 0f },
                new StopCommand { Issuer = p, UnitIds = new[] { unit.Id } },
            });
            return !unit.MoveTargetX.HasValue && !unit.AttackMoving;
        }

        private static bool ApplyAttackMove()
        {
            Setup(out var sim, out _, out _, out var p, out var unit, out _);
            sim.ApplyCommands(new GameCommand[]
            {
                new AttackMoveCommand
                {
                    Issuer = p,
                    UnitIds = new[] { unit.Id },
                    TargetX = 40f,
                    TargetZ = -15f,
                },
            });
            return unit.AttackMoving;
        }

        private static bool ApplyPatrol()
        {
            Setup(out var sim, out _, out _, out var p, out var unit, out _);
            sim.ApplyCommands(new GameCommand[]
            {
                new PatrolCommand
                {
                    Issuer = p,
                    UnitIds = new[] { unit.Id },
                    TargetX = 35f,
                    TargetZ = 35f,
                },
            });
            return unit.Patrolling;
        }

        private static bool ApplyCancel()
        {
            Setup(out var sim, out _, out var wallet, out var p, out _, out var keep);
            wallet.Seed(p, ResourceType.Gold, 500);
            sim.ApplyCommands(new GameCommand[]
            {
                new TrainUnitCommand
                {
                    Issuer = p,
                    BuildingId = keep.Id,
                    UnitDefId = FactionDefaultContent.VeiledBuilderId,
                },
            });
            sim.ApplyCommands(new GameCommand[]
            {
                new CancelProductionCommand { Issuer = p, BuildingId = keep.Id },
            });
            return !keep.IsProducing;
        }

        private static bool ApplyAttach()
        {
            Setup(out var sim, out _, out var wallet, out var p, out _, out var keep);
            wallet.Seed(p, ResourceType.Gold, 500);
            wallet.Seed(p, ResourceType.Timber, 500);
            sim.ApplyCommands(new GameCommand[]
            {
                new AttachBuildingCommand
                {
                    Issuer = p,
                    ParentBuildingId = keep.Id,
                    SlotIndex = 0,
                    BuildingDefId = FactionDefaultContent.KeepTurretId,
                },
            });
            return keep.AttachmentOccupantIds[0] != 0;
        }

        private static bool ApplyGarrison()
        {
            Setup(out var sim, out var ids, out _, out var p, out var unit, out _);
            var tower = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.WatchtowerId, unit.X + 2f, unit.Z, startActive: true);
            sim.ApplyCommands(new GameCommand[]
            {
                new EnterGarrisonCommand
                {
                    Issuer = p,
                    UnitIds = new[] { unit.Id },
                    BuildingId = tower.Id,
                },
            });
            if (!unit.IsGarrisoned)
                return false;
            sim.ApplyCommands(new GameCommand[]
            {
                new ExitGarrisonCommand { Issuer = p, BuildingId = tower.Id },
            });
            return !unit.IsGarrisoned;
        }

        private static bool ApplyCapture()
        {
            Setup(out var sim, out var ids, out _, out var p, out var unit, out _);
            var tid = ids.Next();
            sim.AddTerritory(tid, 100f, 0f, 40f, goldPerSecond: 1);
            sim.ApplyCommands(new GameCommand[]
            {
                new CaptureTerritoryCommand
                {
                    Issuer = p,
                    TerritoryNodeId = tid,
                    UnitIds = new[] { unit.Id },
                },
            });
            return unit.AttackMoving;
        }

        private static bool ApplyResearch()
        {
            Setup(out var sim, out var ids, out var wallet, out var p, out _, out _);
            wallet.Seed(p, ResourceType.Gold, 2000);
            var barracks = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneAcademyId, 20f, -20f, startActive: true);
            sim.ApplyCommands(new GameCommand[]
            {
                new ChooseUpgradeCommand
                {
                    Issuer = p,
                    BuildingId = barracks.Id,
                    UpgradeDefId = FactionDefaultContent.VeiledMailId,
                },
            });
            return barracks.IsResearching;
        }

        private static bool ApplyPower()
        {
            Setup(out var sim, out _, out var wallet, out var p, out _, out _);
            wallet.Seed(p, ResourceType.Gold, 500);
            sim.ApplyCommands(new GameCommand[]
            {
                new UnlockPowerCommand
                {
                    Issuer = p,
                    PowerDefId = FactionDefaultContent.BeastChorusAbilityId,
                },
            });
            if (!sim.HasPower(p, FactionDefaultContent.BeastChorusAbilityId))
                return false;
            sim.ApplyCommands(new GameCommand[]
            {
                new ActivateCommanderAbilityCommand
                {
                    Issuer = p,
                    PowerDefId = FactionDefaultContent.BeastChorusAbilityId,
                },
            });
            return sim.TryGetCommanderAbilityStatus(
                p,
                FactionDefaultContent.BeastChorusAbilityId,
                out float cd,
                out _) && cd > 0f;
        }

        private static bool CodecAllTypes()
        {
            var frame = new CommandFrame
            {
                TargetTick = new Tick(7),
                Player = new PlayerId(0),
                Commands = new GameCommand[]
                {
                    new MoveCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        UnitIds = new[] { new SimEntityId(1) },
                        TargetX = 1f,
                        TargetZ = 2f,
                    },
                    new AttackCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        UnitIds = new[] { new SimEntityId(1) },
                        TargetId = new SimEntityId(2),
                    },
                    new PlaceBuildingCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        BuildingDefId = "building_barracks",
                        X = 3f,
                        Z = 4f,
                        YawDegrees = 90f,
                    },
                    new TrainUnitCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        BuildingId = new SimEntityId(3),
                        UnitDefId = "unit_militia",
                    },
                    new CaptureTerritoryCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        TerritoryNodeId = new SimEntityId(4),
                        UnitIds = new[] { new SimEntityId(1) },
                    },
                    new ChooseUpgradeCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        BuildingId = new SimEntityId(3),
                        UpgradeDefId = "upgrade_heavy_armour",
                    },
                    new SetStanceCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        UnitIds = new[] { new SimEntityId(1) },
                        Stance = UnitStance.Hold,
                    },
                    new GatherCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        UnitIds = new[] { new SimEntityId(1) },
                        ResourceNodeId = new SimEntityId(5),
                    },
                    new SetRallyCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        BuildingId = new SimEntityId(3),
                        TargetX = 9f,
                        TargetZ = 8f,
                    },
                    new CancelProductionCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        BuildingId = new SimEntityId(3),
                    },
                    new AttackMoveCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        UnitIds = new[] { new SimEntityId(1) },
                        TargetX = 11f,
                        TargetZ = 12f,
                    },
                    new StopCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        UnitIds = new[] { new SimEntityId(1) },
                    },
                    new PatrolCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        UnitIds = new[] { new SimEntityId(1) },
                        TargetX = 13f,
                        TargetZ = 14f,
                    },
                    new ActivateCommanderAbilityCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        PowerDefId = "ability_lucien_iron_wall",
                    },
                    new EnterGarrisonCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        UnitIds = new[] { new SimEntityId(1) },
                        BuildingId = new SimEntityId(6),
                    },
                    new ExitGarrisonCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        BuildingId = new SimEntityId(6),
                    },
                    new ApplyUnitUpgradeCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        UpgradeDefId = "upgrade_fire_swords",
                        UnitIds = new[] { new SimEntityId(1) },
                    },
                    new UnlockPowerCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        PowerDefId = "ability_lucien_iron_wall",
                    },
                    new AttachBuildingCommand
                    {
                        Issuer = new PlayerId(0),
                        IssueTick = new Tick(7),
                        ParentBuildingId = new SimEntityId(3),
                        SlotIndex = 2,
                        BuildingDefId = "building_keep_turret",
                    },
                },
            };

            byte[] payload = CommandCodec.SerializeFrame(frame);
            var round = CommandCodec.DeserializeFrame(payload);
            if (round.Commands.Length != frame.Commands.Length)
                return false;
            for (int i = 0; i < frame.Commands.Length; i++)
            {
                if (round.Commands[i].GetType() != frame.Commands[i].GetType())
                    return false;
            }

            // Ensure we covered every enum value.
            var seen = new HashSet<System.Type>();
            for (int i = 0; i < frame.Commands.Length; i++)
                seen.Add(frame.Commands[i].GetType());
            return seen.Count >= 19;
        }

        private static void Setup(
            out SkirmishWorldSim sim,
            out SequentialIdFactory ids,
            out ResourceWallet wallet,
            out PlayerId p,
            out SimUnit unit,
            out SimBuilding keep)
        {
            ids = new SequentialIdFactory();
            wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            sim = new SkirmishWorldSim(wallet, ids, defs);
            p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 200);
            wallet.Seed(p, ResourceType.Timber, 100);
            keep = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, 0f, 0f, startActive: true);
            unit = sim.SpawnUnit(ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledApprenticeId, 10f, 0f);
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
