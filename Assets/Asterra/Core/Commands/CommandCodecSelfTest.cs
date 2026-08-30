using System;
using System.Text;

namespace Asterra.Core
{
    public static class CommandCodecSelfTest
    {
        public static string Run()
        {
            var original = new CommandFrame
            {
                TargetTick = new Tick(42),
                Player = new PlayerId(3),
                Commands = new GameCommand[]
                {
                    new MoveCommand
                    {
                        Issuer = new PlayerId(3),
                        IssueTick = new Tick(42),
                        UnitIds = new[] { new SimEntityId(10), new SimEntityId(11) },
                        TargetX = 12.5f,
                        TargetZ = -3.25f,
                    },
                    new AttackCommand
                    {
                        Issuer = new PlayerId(3),
                        IssueTick = new Tick(42),
                        UnitIds = new[] { new SimEntityId(10) },
                        TargetId = new SimEntityId(99),
                    },
                    new PlaceBuildingCommand
                    {
                        Issuer = new PlayerId(3),
                        IssueTick = new Tick(42),
                        BuildingDefId = "building_barracks",
                        X = -300.1f,
                        Z = 30f,
                        YawDegrees = 90f,
                    },
                    new TrainUnitCommand
                    {
                        Issuer = new PlayerId(3),
                        IssueTick = new Tick(42),
                        BuildingId = new SimEntityId(7),
                        UnitDefId = "unit_veiled_apprentice",
                    },
                    new CaptureTerritoryCommand
                    {
                        Issuer = new PlayerId(3),
                        IssueTick = new Tick(42),
                        TerritoryNodeId = new SimEntityId(5),
                    },
                    new ChooseUpgradeCommand
                    {
                        Issuer = new PlayerId(3),
                        IssueTick = new Tick(42),
                        UpgradeDefId = "upgrade_militia_training",
                        BuildingId = new SimEntityId(7),
                    },
                    new SetStanceCommand
                    {
                        Issuer = new PlayerId(3),
                        IssueTick = new Tick(42),
                        UnitIds = new[] { new SimEntityId(10) },
                        Stance = UnitStance.Hold,
                    },
                    new GatherCommand
                    {
                        Issuer = new PlayerId(3),
                        IssueTick = new Tick(42),
                        UnitIds = new[] { new SimEntityId(10) },
                        ResourceNodeId = new SimEntityId(50),
                    },
                    new SetRallyCommand
                    {
                        Issuer = new PlayerId(3),
                        IssueTick = new Tick(42),
                        BuildingId = new SimEntityId(7),
                        TargetX = 10f,
                        TargetZ = 20f,
                    },
                    new CancelProductionCommand
                    {
                        Issuer = new PlayerId(3),
                        IssueTick = new Tick(42),
                        BuildingId = new SimEntityId(7),
                    },
                    new AttackMoveCommand
                    {
                        Issuer = new PlayerId(3),
                        IssueTick = new Tick(42),
                        UnitIds = new[] { new SimEntityId(10) },
                        TargetX = 50f,
                        TargetZ = -20f,
                    },
                    new StopCommand
                    {
                        Issuer = new PlayerId(3),
                        IssueTick = new Tick(42),
                        UnitIds = new[] { new SimEntityId(10) },
                    },
                    new PatrolCommand
                    {
                        Issuer = new PlayerId(3),
                        IssueTick = new Tick(42),
                        UnitIds = new[] { new SimEntityId(10) },
                        TargetX = 80f,
                        TargetZ = 12f,
                    },
                    new ActivateCommanderAbilityCommand
                    {
                        Issuer = new PlayerId(3),
                        IssueTick = new Tick(42),
                        PowerDefId = "ability_wrath_of_skies",
                    },
                    new AttachBuildingCommand
                    {
                        Issuer = new PlayerId(3),
                        IssueTick = new Tick(42),
                        ParentBuildingId = new SimEntityId(7),
                        SlotIndex = 1,
                        BuildingDefId = "building_keep_turret",
                    },
                    new SetTerritoryJobCommand
                    {
                        Issuer = new PlayerId(3),
                        IssueTick = new Tick(42),
                        TerritoryId = new SimEntityId(99),
                        Job = TerritoryJob.Vision,
                    },
                },
            };

            byte[] payload = CommandCodec.SerializeFrame(original);
            CommandFrame roundtrip = CommandCodec.DeserializeFrame(payload);

            var sb = new StringBuilder();
            sb.AppendLine("[Asterra Codec]");
            sb.AppendLine($"bytes={payload.Length} commands={roundtrip.Commands.Length}");

            if (roundtrip.TargetTick != original.TargetTick || roundtrip.Player != original.Player)
                throw new InvalidOperationException("Frame header mismatch.");
            if (roundtrip.Commands.Length != original.Commands.Length)
                throw new InvalidOperationException("Command count mismatch.");

            for (int i = 0; i < original.Commands.Length; i++)
            {
                if (roundtrip.Commands[i].GetType() != original.Commands[i].GetType())
                    throw new InvalidOperationException($"Type mismatch at {i}");
                if (roundtrip.Commands[i].Issuer != original.Commands[i].Issuer)
                    throw new InvalidOperationException($"Issuer mismatch at {i}");
            }

            var move = (MoveCommand)roundtrip.Commands[0];
            if (move.UnitIds.Length != 2 || Math.Abs(move.TargetX - 12.5f) > 0.001f)
                throw new InvalidOperationException("Move roundtrip failed.");

            var place = (PlaceBuildingCommand)roundtrip.Commands[2];
            if (place.BuildingDefId != "building_barracks")
                throw new InvalidOperationException("PlaceBuilding string roundtrip failed.");

            sb.AppendLine("status=ok");
            return sb.ToString();
        }
    }
}
