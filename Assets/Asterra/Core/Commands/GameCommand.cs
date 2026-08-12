using System;

namespace Asterra.Core
{
    /// <summary>
    /// Orders issued by players or AI. Serialized for lockstep; keep fields blittable / deterministic.
    /// </summary>
    public abstract class GameCommand
    {
        public PlayerId Issuer;
        public Tick IssueTick;
    }

    public sealed class MoveCommand : GameCommand
    {
        public SimEntityId[] UnitIds;
        public float TargetX;
        public float TargetZ;
    }

    public sealed class AttackCommand : GameCommand
    {
        public SimEntityId[] UnitIds;
        public SimEntityId TargetId;
    }

    public sealed class PlaceBuildingCommand : GameCommand
    {
        public string BuildingDefId;
        public float X;
        public float Z;
        public float YawDegrees;
    }

    public sealed class TrainUnitCommand : GameCommand
    {
        public SimEntityId BuildingId;
        public string UnitDefId;
    }

    public sealed class CaptureTerritoryCommand : GameCommand
    {
        public SimEntityId TerritoryNodeId;
    }

    public sealed class ChooseUpgradeCommand : GameCommand
    {
        public string UpgradeDefId;
    }

    public sealed class SetStanceCommand : GameCommand
    {
        public SimEntityId[] UnitIds;
        public UnitStance Stance;
    }

    public sealed class GatherCommand : GameCommand
    {
        public SimEntityId[] UnitIds;
        public SimEntityId ResourceNodeId;
    }

    public sealed class SetRallyCommand : GameCommand
    {
        public SimEntityId BuildingId;
        public float TargetX;
        public float TargetZ;
    }

    public sealed class CancelProductionCommand : GameCommand
    {
        public SimEntityId BuildingId;
    }

    public sealed class AttackMoveCommand : GameCommand
    {
        public SimEntityId[] UnitIds;
        public float TargetX;
        public float TargetZ;
    }

    /// <summary>Envelope for one player's inputs for a future simulation tick.</summary>
    public sealed class CommandFrame
    {
        public Tick TargetTick;
        public PlayerId Player;
        public GameCommand[] Commands = Array.Empty<GameCommand>();
    }

    public enum CombatEventKind : byte
    {
        Hit = 1,
        Death = 2,
        Deposit = 3,
        BuildComplete = 4,
    }

    public readonly struct CombatEvent
    {
        public readonly CombatEventKind Kind;
        public readonly SimEntityId TargetId;
        public readonly float X;
        public readonly float Z;
        public readonly bool IsBuilding;

        public CombatEvent(CombatEventKind kind, SimEntityId targetId, float x, float z, bool isBuilding)
        {
            Kind = kind;
            TargetId = targetId;
            X = x;
            Z = z;
            IsBuilding = isBuilding;
        }
    }
}
