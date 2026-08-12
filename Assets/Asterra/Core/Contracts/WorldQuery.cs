using System.Collections.Generic;

namespace Asterra.Core
{
    public readonly struct UnitSnapshot
    {
        public readonly SimEntityId Id;
        public readonly PlayerId Owner;
        public readonly FactionId Faction;
        public readonly string DefinitionId;
        public readonly float X;
        public readonly float Z;
        public readonly float Health;
        public readonly float MaxHealth;
        public readonly bool IsAlive;

        public UnitSnapshot(
            SimEntityId id,
            PlayerId owner,
            FactionId faction,
            string definitionId,
            float x,
            float z,
            float health,
            float maxHealth,
            bool isAlive)
        {
            Id = id;
            Owner = owner;
            Faction = faction;
            DefinitionId = definitionId;
            X = x;
            Z = z;
            Health = health;
            MaxHealth = maxHealth;
            IsAlive = isAlive;
        }
    }

    public readonly struct BuildingSnapshot
    {
        public readonly SimEntityId Id;
        public readonly PlayerId Owner;
        public readonly string DefinitionId;
        public readonly float X;
        public readonly float Z;
        public readonly BuildingState State;
        public readonly bool CanProduce;

        public BuildingSnapshot(
            SimEntityId id,
            PlayerId owner,
            string definitionId,
            float x,
            float z,
            BuildingState state,
            bool canProduce)
        {
            Id = id;
            Owner = owner;
            DefinitionId = definitionId;
            X = x;
            Z = z;
            State = state;
            CanProduce = canProduce;
        }
    }

    public readonly struct TerritorySnapshot
    {
        public readonly SimEntityId Id;
        public readonly float X;
        public readonly float Z;
        public readonly float Radius;
        public readonly TerritoryState State;
        public readonly PlayerId Controller;
        public readonly bool HasController;
        public readonly float CaptureProgress;

        public TerritorySnapshot(
            SimEntityId id,
            float x,
            float z,
            float radius,
            TerritoryState state,
            PlayerId controller,
            bool hasController,
            float captureProgress)
        {
            Id = id;
            X = x;
            Z = z;
            Radius = radius;
            State = state;
            Controller = controller;
            HasController = hasController;
            CaptureProgress = captureProgress;
        }
    }

    public interface IWorldQuery
    {
        IReadOnlyList<UnitSnapshot> Units { get; }
        IReadOnlyList<BuildingSnapshot> Buildings { get; }
        IReadOnlyList<TerritorySnapshot> Territories { get; }
        bool HasUpgrade(PlayerId player, string upgradeDefId);
    }

    public interface IUpgradeState
    {
        bool Has(PlayerId player, string upgradeDefId);
        bool TryUnlock(PlayerId player, string upgradeDefId, int goldCost);
    }
}
