using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Asterra.Core
{
    /// <summary>
    /// Deterministic binary codec for lockstep transport. Positions are quantized to millimeters.
    /// </summary>
    public static class CommandCodec
    {
        private const int PositionScale = 1000;

        public static byte[] SerializeFrame(CommandFrame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            using var ms = new MemoryStream(128);
            using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
            writer.Write(frame.TargetTick.Value);
            writer.Write(frame.Player.Value);
            var commands = frame.Commands ?? Array.Empty<GameCommand>();
            writer.Write(commands.Length);
            for (int i = 0; i < commands.Length; i++)
                WriteCommand(writer, commands[i]);
            writer.Flush();
            return ms.ToArray();
        }

        public static CommandFrame DeserializeFrame(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
                throw new ArgumentException("Payload is empty.", nameof(payload));

            using var ms = new MemoryStream(payload, writable: false);
            using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);
            var frame = new CommandFrame
            {
                TargetTick = new Tick(reader.ReadUInt32()),
                Player = new PlayerId(reader.ReadByte()),
            };
            int count = reader.ReadInt32();
            if (count < 0 || count > 512)
                throw new InvalidDataException($"Command count out of range: {count}");
            var commands = new GameCommand[count];
            for (int i = 0; i < count; i++)
                commands[i] = ReadCommand(reader);
            frame.Commands = commands;
            return frame;
        }

        public static byte[] SerializeCommands(IReadOnlyList<GameCommand> commands)
        {
            using var ms = new MemoryStream(64);
            using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
            int count = commands?.Count ?? 0;
            writer.Write(count);
            for (int i = 0; i < count; i++)
                WriteCommand(writer, commands[i]);
            writer.Flush();
            return ms.ToArray();
        }

        public static GameCommand[] DeserializeCommands(byte[] payload)
        {
            using var ms = new MemoryStream(payload, writable: false);
            using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);
            int count = reader.ReadInt32();
            var commands = new GameCommand[count];
            for (int i = 0; i < count; i++)
                commands[i] = ReadCommand(reader);
            return commands;
        }

        private static void WriteCommand(BinaryWriter writer, GameCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            writer.Write(command.Issuer.Value);
            writer.Write(command.IssueTick.Value);

            switch (command)
            {
                case MoveCommand move:
                    writer.Write((byte)CommandType.Move);
                    WriteEntityIds(writer, move.UnitIds);
                    WriteQuantized(writer, move.TargetX);
                    WriteQuantized(writer, move.TargetZ);
                    break;
                case AttackCommand attack:
                    writer.Write((byte)CommandType.Attack);
                    WriteEntityIds(writer, attack.UnitIds);
                    writer.Write(attack.TargetId.Value);
                    break;
                case PlaceBuildingCommand place:
                    writer.Write((byte)CommandType.PlaceBuilding);
                    WriteString(writer, place.BuildingDefId);
                    WriteQuantized(writer, place.X);
                    WriteQuantized(writer, place.Z);
                    WriteQuantized(writer, place.YawDegrees);
                    break;
                case TrainUnitCommand train:
                    writer.Write((byte)CommandType.TrainUnit);
                    writer.Write(train.BuildingId.Value);
                    WriteString(writer, train.UnitDefId);
                    break;
                case CaptureTerritoryCommand capture:
                    writer.Write((byte)CommandType.CaptureTerritory);
                    writer.Write(capture.TerritoryNodeId.Value);
                    WriteEntityIds(writer, capture.UnitIds);
                    break;
                case ChooseUpgradeCommand upgrade:
                    writer.Write((byte)CommandType.ChooseUpgrade);
                    WriteString(writer, upgrade.UpgradeDefId);
                    writer.Write(upgrade.BuildingId.Value);
                    break;
                case SetStanceCommand stance:
                    writer.Write((byte)CommandType.SetStance);
                    WriteEntityIds(writer, stance.UnitIds);
                    writer.Write((byte)stance.Stance);
                    break;
                case GatherCommand gather:
                    writer.Write((byte)CommandType.Gather);
                    WriteEntityIds(writer, gather.UnitIds);
                    writer.Write(gather.ResourceNodeId.Value);
                    break;
                case SetRallyCommand rally:
                    writer.Write((byte)CommandType.SetRally);
                    writer.Write(rally.BuildingId.Value);
                    WriteQuantized(writer, rally.TargetX);
                    WriteQuantized(writer, rally.TargetZ);
                    break;
                case CancelProductionCommand cancel:
                    writer.Write((byte)CommandType.CancelProduction);
                    writer.Write(cancel.BuildingId.Value);
                    break;
                case AttackMoveCommand attackMove:
                    writer.Write((byte)CommandType.AttackMove);
                    WriteEntityIds(writer, attackMove.UnitIds);
                    WriteQuantized(writer, attackMove.TargetX);
                    WriteQuantized(writer, attackMove.TargetZ);
                    break;
                case StopCommand stop:
                    writer.Write((byte)CommandType.Stop);
                    WriteEntityIds(writer, stop.UnitIds);
                    break;
                case PatrolCommand patrol:
                    writer.Write((byte)CommandType.Patrol);
                    WriteEntityIds(writer, patrol.UnitIds);
                    WriteQuantized(writer, patrol.TargetX);
                    WriteQuantized(writer, patrol.TargetZ);
                    break;
                case ActivateCommanderAbilityCommand ability:
                    writer.Write((byte)CommandType.ActivateCommanderAbility);
                    WriteString(writer, ability.PowerDefId);
                    break;
                case ApplyUnitUpgradeCommand applyUpgrade:
                    writer.Write((byte)CommandType.ApplyUnitUpgrade);
                    WriteString(writer, applyUpgrade.UpgradeDefId);
                    WriteEntityIds(writer, applyUpgrade.UnitIds);
                    break;
                case UnlockPowerCommand unlockPower:
                    writer.Write((byte)CommandType.UnlockPower);
                    WriteString(writer, unlockPower.PowerDefId);
                    break;
                case AttachBuildingCommand attach:
                    writer.Write((byte)CommandType.AttachBuilding);
                    writer.Write(attach.ParentBuildingId.Value);
                    writer.Write(attach.SlotIndex);
                    WriteString(writer, attach.BuildingDefId);
                    break;
                case EnterGarrisonCommand enter:
                    writer.Write((byte)CommandType.EnterGarrison);
                    WriteEntityIds(writer, enter.UnitIds);
                    writer.Write(enter.BuildingId.Value);
                    break;
                case ExitGarrisonCommand exit:
                    writer.Write((byte)CommandType.ExitGarrison);
                    writer.Write(exit.BuildingId.Value);
                    break;
                case DigTrenchCommand dig:
                    writer.Write((byte)CommandType.DigTrench);
                    WriteQuantized(writer, dig.X);
                    WriteQuantized(writer, dig.Z);
                    WriteQuantized(writer, dig.HalfExtent);
                    break;
                case DemolishBuildingCommand demolish:
                    writer.Write((byte)CommandType.DemolishBuilding);
                    writer.Write(demolish.BuildingId.Value);
                    writer.Write(demolish.RazeForMaterials);
                    break;
                case TerrainWorkCommand work:
                    writer.Write((byte)CommandType.TerrainWork);
                    writer.Write((byte)work.Kind);
                    WriteQuantized(writer, work.X);
                    WriteQuantized(writer, work.Z);
                    WriteQuantized(writer, work.HalfExtent);
                    break;
                case RepairBridgeCommand repair:
                    writer.Write((byte)CommandType.RepairBridge);
                    WriteQuantized(writer, repair.X);
                    WriteQuantized(writer, repair.Z);
                    break;
                case UpgradeBuildingCommand upgradeBuilding:
                    writer.Write((byte)CommandType.UpgradeBuilding);
                    writer.Write(upgradeBuilding.BuildingId.Value);
                    WriteString(writer, upgradeBuilding.UpgradeDefId);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported command type: {command.GetType().Name}");
            }
        }

        private static GameCommand ReadCommand(BinaryReader reader)
        {
            var issuer = new PlayerId(reader.ReadByte());
            var issueTick = new Tick(reader.ReadUInt32());
            var type = (CommandType)reader.ReadByte();

            GameCommand command;
            switch (type)
            {
                case CommandType.Move:
                    command = new MoveCommand
                    {
                        UnitIds = ReadEntityIds(reader),
                        TargetX = ReadQuantized(reader),
                        TargetZ = ReadQuantized(reader),
                    };
                    break;
                case CommandType.Attack:
                    command = new AttackCommand
                    {
                        UnitIds = ReadEntityIds(reader),
                        TargetId = new SimEntityId(reader.ReadUInt32()),
                    };
                    break;
                case CommandType.PlaceBuilding:
                    command = new PlaceBuildingCommand
                    {
                        BuildingDefId = ReadString(reader),
                        X = ReadQuantized(reader),
                        Z = ReadQuantized(reader),
                        YawDegrees = ReadQuantized(reader),
                    };
                    break;
                case CommandType.TrainUnit:
                    command = new TrainUnitCommand
                    {
                        BuildingId = new SimEntityId(reader.ReadUInt32()),
                        UnitDefId = ReadString(reader),
                    };
                    break;
                case CommandType.CaptureTerritory:
                    command = new CaptureTerritoryCommand
                    {
                        TerritoryNodeId = new SimEntityId(reader.ReadUInt32()),
                        UnitIds = ReadEntityIds(reader),
                    };
                    break;
                case CommandType.ChooseUpgrade:
                    command = new ChooseUpgradeCommand
                    {
                        UpgradeDefId = ReadString(reader),
                        BuildingId = new SimEntityId(reader.ReadUInt32()),
                    };
                    break;
                case CommandType.SetStance:
                    command = new SetStanceCommand
                    {
                        UnitIds = ReadEntityIds(reader),
                        Stance = (UnitStance)reader.ReadByte(),
                    };
                    break;
                case CommandType.Gather:
                    command = new GatherCommand
                    {
                        UnitIds = ReadEntityIds(reader),
                        ResourceNodeId = new SimEntityId(reader.ReadUInt32()),
                    };
                    break;
                case CommandType.SetRally:
                    command = new SetRallyCommand
                    {
                        BuildingId = new SimEntityId(reader.ReadUInt32()),
                        TargetX = ReadQuantized(reader),
                        TargetZ = ReadQuantized(reader),
                    };
                    break;
                case CommandType.CancelProduction:
                    command = new CancelProductionCommand
                    {
                        BuildingId = new SimEntityId(reader.ReadUInt32()),
                    };
                    break;
                case CommandType.AttackMove:
                    command = new AttackMoveCommand
                    {
                        UnitIds = ReadEntityIds(reader),
                        TargetX = ReadQuantized(reader),
                        TargetZ = ReadQuantized(reader),
                    };
                    break;
                case CommandType.Stop:
                    command = new StopCommand
                    {
                        UnitIds = ReadEntityIds(reader),
                    };
                    break;
                case CommandType.Patrol:
                    command = new PatrolCommand
                    {
                        UnitIds = ReadEntityIds(reader),
                        TargetX = ReadQuantized(reader),
                        TargetZ = ReadQuantized(reader),
                    };
                    break;
                case CommandType.ActivateCommanderAbility:
                    command = new ActivateCommanderAbilityCommand
                    {
                        PowerDefId = ReadString(reader),
                    };
                    break;
                case CommandType.ApplyUnitUpgrade:
                    command = new ApplyUnitUpgradeCommand
                    {
                        UpgradeDefId = ReadString(reader),
                        UnitIds = ReadEntityIds(reader),
                    };
                    break;
                case CommandType.UnlockPower:
                    command = new UnlockPowerCommand
                    {
                        PowerDefId = ReadString(reader),
                    };
                    break;
                case CommandType.AttachBuilding:
                    command = new AttachBuildingCommand
                    {
                        ParentBuildingId = new SimEntityId(reader.ReadUInt32()),
                        SlotIndex = reader.ReadByte(),
                        BuildingDefId = ReadString(reader),
                    };
                    break;
                case CommandType.EnterGarrison:
                    command = new EnterGarrisonCommand
                    {
                        UnitIds = ReadEntityIds(reader),
                        BuildingId = new SimEntityId(reader.ReadUInt32()),
                    };
                    break;
                case CommandType.ExitGarrison:
                    command = new ExitGarrisonCommand
                    {
                        BuildingId = new SimEntityId(reader.ReadUInt32()),
                    };
                    break;
                case CommandType.DigTrench:
                    command = new DigTrenchCommand
                    {
                        X = ReadQuantized(reader),
                        Z = ReadQuantized(reader),
                        HalfExtent = ReadQuantized(reader),
                    };
                    break;
                case CommandType.DemolishBuilding:
                    command = new DemolishBuildingCommand
                    {
                        BuildingId = new SimEntityId(reader.ReadUInt32()),
                        RazeForMaterials = reader.ReadBoolean(),
                    };
                    break;
                case CommandType.TerrainWork:
                    command = new TerrainWorkCommand
                    {
                        Kind = (TerrainWorkKind)reader.ReadByte(),
                        X = ReadQuantized(reader),
                        Z = ReadQuantized(reader),
                        HalfExtent = ReadQuantized(reader),
                    };
                    break;
                case CommandType.RepairBridge:
                    command = new RepairBridgeCommand
                    {
                        X = ReadQuantized(reader),
                        Z = ReadQuantized(reader),
                    };
                    break;
                case CommandType.UpgradeBuilding:
                    command = new UpgradeBuildingCommand
                    {
                        BuildingId = new SimEntityId(reader.ReadUInt32()),
                        UpgradeDefId = ReadString(reader),
                    };
                    break;
                default:
                    throw new InvalidDataException($"Unknown command type byte: {(byte)type}");
            }

            command.Issuer = issuer;
            command.IssueTick = issueTick;
            return command;
        }

        private static void WriteEntityIds(BinaryWriter writer, SimEntityId[] ids)
        {
            int count = ids?.Length ?? 0;
            if (count > 1024)
                throw new InvalidDataException("Unit id list too large.");
            writer.Write(count);
            for (int i = 0; i < count; i++)
                writer.Write(ids[i].Value);
        }

        private static SimEntityId[] ReadEntityIds(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > 1024)
                throw new InvalidDataException($"Unit id count out of range: {count}");
            var ids = new SimEntityId[count];
            for (int i = 0; i < count; i++)
                ids[i] = new SimEntityId(reader.ReadUInt32());
            return ids;
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            writer.Write(value ?? string.Empty);
        }

        private static string ReadString(BinaryReader reader) => reader.ReadString();

        private static void WriteQuantized(BinaryWriter writer, float value)
        {
            writer.Write((int)Math.Round(value * PositionScale));
        }

        private static float ReadQuantized(BinaryReader reader)
        {
            return reader.ReadInt32() / (float)PositionScale;
        }
    }
}
