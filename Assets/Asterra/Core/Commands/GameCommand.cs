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
        /// <summary>When null/empty, all owned combat units march (AI). Player should pass selection.</summary>
        public SimEntityId[] UnitIds;
    }

    public sealed class ChooseUpgradeCommand : GameCommand
    {
        public string UpgradeDefId;
        /// <summary>Keep or producer performing the research.</summary>
        public SimEntityId BuildingId;
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

    public sealed class StopCommand : GameCommand
    {
        public SimEntityId[] UnitIds;
    }

    public sealed class PatrolCommand : GameCommand
    {
        public SimEntityId[] UnitIds;
        public float TargetX;
        public float TargetZ;
    }

    /// <summary>Faction commander active ability (requires unlocked power).</summary>
    public sealed class ActivateCommanderAbilityCommand : GameCommand
    {
        public string PowerDefId;
        public float TargetX;
        public float TargetZ;
        public float SecondaryX;
        public float SecondaryZ;
        public SimEntityId TargetId;
    }

    /// <summary>Research complete: apply a researched upgrade to selected units.</summary>
    public sealed class ApplyUnitUpgradeCommand : GameCommand
    {
        public string UpgradeDefId;
        public SimEntityId[] UnitIds;
    }

    /// <summary>Spend gold at the keep to unlock a faction power.</summary>
    public sealed class UnlockPowerCommand : GameCommand
    {
        public string PowerDefId;
    }

    /// <summary>Build an attachment (e.g. tower) onto a keep attachment slot.</summary>
    public sealed class AttachBuildingCommand : GameCommand
    {
        public SimEntityId ParentBuildingId;
        public byte SlotIndex;
        public string BuildingDefId;
    }

    public sealed class EnterGarrisonCommand : GameCommand
    {
        public SimEntityId[] UnitIds;
        public SimEntityId BuildingId;
    }

    public sealed class ExitGarrisonCommand : GameCommand
    {
        public SimEntityId BuildingId;
    }

    /// <summary>Builder / siege earthworks and vegetation work.</summary>
    public enum TerrainWorkKind : byte
    {
        DigTrench = 0,
        FillTrench = 1,
        FlattenHill = 2,
        RaiseBerm = 3,
        DigMoat = 4,
        ClearForest = 5,
        QuarryRock = 6,
        BurnBrush = 7,
        PlaceSpikes = 8,
        ClearDebris = 9,
    }

    public sealed class DigTrenchCommand : GameCommand
    {
        public float X;
        public float Z;
        public float HalfExtent = 8f;
    }

    public sealed class DemolishBuildingCommand : GameCommand
    {
        public SimEntityId BuildingId;
        /// <summary>When true, walls refund full timber (raze); otherwise half gold+timber.</summary>
        public bool RazeForMaterials;
    }

    public sealed class TerrainWorkCommand : GameCommand
    {
        public TerrainWorkKind Kind;
        public float X;
        public float Z;
        public float HalfExtent = 8f;
    }

    /// <summary>Re-enable a collapsed bridge link near X/Z and rebuild the prop.</summary>
    public sealed class RepairBridgeCommand : GameCommand
    {
        public float X;
        public float Z;
    }

    /// <summary>Mutate an owned building in place (e.g. palisade → stone).</summary>
    public sealed class UpgradeBuildingCommand : GameCommand
    {
        public SimEntityId BuildingId;
        public string UpgradeDefId;
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
        /// <summary>World prop / bridge / tree destroyed (TargetId is the destructible).</summary>
        WorldDestroyed = 5,
        CaptureStarted = 6,
        CaptureContested = 7,
        CaptureCompleted = 8,
        CaptureLost = 9,
        ResearchComplete = 10,
        TrainComplete = 11,
        PowerActivated = 12,
        UpgradeApplied = 13,
    }

    public readonly struct CombatEvent
    {
        public readonly CombatEventKind Kind;
        public readonly SimEntityId TargetId;
        public readonly float X;
        public readonly float Z;
        public readonly bool IsBuilding;
        /// <summary>255 = none / unknown.</summary>
        public readonly byte IssuerPlayer;

        public CombatEvent(CombatEventKind kind, SimEntityId targetId, float x, float z, bool isBuilding, byte issuerPlayer = 255)
        {
            Kind = kind;
            TargetId = targetId;
            X = x;
            Z = z;
            IsBuilding = isBuilding;
            IssuerPlayer = issuerPlayer;
        }
    }
}
