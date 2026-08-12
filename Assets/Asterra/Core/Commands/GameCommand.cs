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
        public EntityId[] UnitIds;
        public float TargetX;
        public float TargetZ;
    }

    public sealed class AttackCommand : GameCommand
    {
        public EntityId[] UnitIds;
        public EntityId TargetId;
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
        public EntityId BuildingId;
        public string UnitDefId;
    }

    public sealed class CaptureTerritoryCommand : GameCommand
    {
        public EntityId TerritoryNodeId;
    }

    public sealed class ChooseUpgradeCommand : GameCommand
    {
        public string UpgradeDefId;
    }

    public sealed class SetStanceCommand : GameCommand
    {
        public EntityId[] UnitIds;
        public UnitStance Stance;
    }

    /// <summary>Envelope for one player's inputs for a future simulation tick.</summary>
    public sealed class CommandFrame
    {
        public Tick TargetTick;
        public PlayerId Player;
        public GameCommand[] Commands = Array.Empty<GameCommand>();
    }
}
