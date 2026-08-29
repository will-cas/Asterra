using System;
using Asterra.Core;
using Asterra.Core.World;

namespace Asterra.Gameplay.Sim
{
    public sealed class SimUnit : IUnit
    {
        public SimEntityId Id { get; }
        public PlayerId Owner { get; set; }
        public FactionId Faction { get; }
        public string DefinitionId { get; }
        public float Health { get; set; }
        public float MaxHealth { get; }
        public UnitStance Stance { get; set; } = UnitStance.Aggressive;
        public bool IsAlive => Health > 0f;

        public float X;
        public float Z;
        public float MoveSpeed;
        public float AttackDamage;
        public float AttackRange;
        public float AttackCooldown;
        public float AttackCooldownRemaining;
        public bool CanGather;
        public int CarryCapacity;
        public float GatherRate;
        public float BuildingDamageMultiplier;
        public UnitRole Role;
        public float Armor;
        public float ProjectileSpeed;
        /// <summary>Path capability mask from def.</summary>
        public TraversalCapability TraversalCapabilities;
        /// <summary>Active Iron Wall (or similar) armor bonus currently applied to this unit.</summary>
        public float CommanderArmorBonus;
        /// <summary>Temporary move-speed bonus from a commander power.</summary>
        public float CommanderMoveBonus;
        /// <summary>Temporary flat damage bonus from a commander power.</summary>
        public float CommanderDamageBonus;
        /// <summary>Legacy single-slot id (first applied equipment); prefer AppliedEquipmentIds.</summary>
        public string AppliedUpgradeId;
        public const int MaxAppliedEquipment = 4;
        public readonly string[] AppliedEquipmentIds = new string[MaxAppliedEquipment];
        public int AppliedEquipmentCount;
        public float SightRadius = 110f;
        public float CollisionRadius = 1.6f;
        public float PortalCooldownRemaining;
        public float MindControlRemaining;
        public bool HasMindControlOriginal;
        public PlayerId MindControlOriginalOwner;
        public bool Airborne;
        public float FlightRemaining;
        public float LifetimeRemaining;
        public bool ExplosiveCart;
        public float CartEndX;
        public float CartEndZ;
        public float StunRemaining;
        public float DaySunPercent;

        /// <summary>Active traversal link id, or -1 when not traversing.</summary>
        public int ActiveTraversalLinkId = -1;
        /// <summary>0..1 progress along the active link.</summary>
        public float TraversalProgress;
        /// <summary>True = Start→End, false = End→Start.</summary>
        public bool TraversalForward = true;

        public const int MaxPathPoints = 48;
        public readonly float[] PathPointsX = new float[MaxPathPoints];
        public readonly float[] PathPointsZ = new float[MaxPathPoints];
        public int PathCount;
        public int PathIndex;

        public float? MoveTargetX;
        public float? MoveTargetZ;
        public SimEntityId? AttackTargetId;
        public SimEntityId? GatherTargetId;
        public SimEntityId? GarrisonBuildingId;
        public ResourceType? CarryType;
        public int CarryAmount;
        public bool ReturningToDeposit;
        public bool AttackMoving;
        public bool Patrolling;
        public float PatrolAX;
        public float PatrolAZ;
        public float PatrolBX;
        public float PatrolBZ;
        public bool PatrolToB = true;
        /// <summary>Fractional gather accrual so GatherRate is units/second, not ≥1 per tick.</summary>
        public float GatherProgress;

        public SimUnit(SimEntityId id, PlayerId owner, FactionId faction, UnitDefData def, float x, float z)
        {
            Id = id;
            Owner = owner;
            Faction = faction;
            DefinitionId = def.Id;
            MaxHealth = def.MaxHealth;
            Health = def.MaxHealth;
            MoveSpeed = def.MoveSpeed;
            AttackDamage = def.AttackDamage;
            AttackRange = def.AttackRange;
            AttackCooldown = def.AttackCooldown;
            CanGather = def.CanGather || def.IsBuilder;
            CarryCapacity = def.CarryCapacity > 0 ? def.CarryCapacity : 10;
            GatherRate = def.GatherRate > 0f ? def.GatherRate : 4f;
            BuildingDamageMultiplier = def.BuildingDamageMultiplier > 0f ? def.BuildingDamageMultiplier : 1f;
            Role = def.IsBuilder ? UnitRole.Builder : def.Role;
            Armor = def.Armor;
            ProjectileSpeed = def.ProjectileSpeed;
            TraversalCapabilities = def.TraversalCapabilities != TraversalCapability.None
                ? def.TraversalCapabilities
                : TraversalCapability.Land;
            SightRadius = def.SightRadius > 1f ? def.SightRadius : 110f;
            CollisionRadius = def.CollisionRadius > 0.1f ? def.CollisionRadius : 1.6f;
            X = x;
            Z = z;
        }

        public bool HasAppliedEquipment(string upgradeId)
        {
            if (string.IsNullOrEmpty(upgradeId))
                return false;
            for (int i = 0; i < AppliedEquipmentCount; i++)
            {
                if (AppliedEquipmentIds[i] == upgradeId)
                    return true;
            }

            return AppliedUpgradeId == upgradeId;
        }

        public bool TryRecordEquipment(string upgradeId)
        {
            if (HasAppliedEquipment(upgradeId) || AppliedEquipmentCount >= MaxAppliedEquipment)
                return false;
            AppliedEquipmentIds[AppliedEquipmentCount++] = upgradeId;
            if (string.IsNullOrEmpty(AppliedUpgradeId))
                AppliedUpgradeId = upgradeId;
            return true;
        }

        public bool IsGarrisoned => GarrisonBuildingId.HasValue;

        public void ClearPath()
        {
            PathCount = 0;
            PathIndex = 0;
        }

        public void SetPath(System.Collections.Generic.IReadOnlyList<(float x, float z)> points)
        {
            ClearPath();
            if (points == null)
                return;
            int n = points.Count < MaxPathPoints ? points.Count : MaxPathPoints;
            for (int i = 0; i < n; i++)
            {
                PathPointsX[i] = points[i].x;
                PathPointsZ[i] = points[i].z;
            }

            PathCount = n;
            PathIndex = 0;
        }

        public bool TryGetPathWaypoint(out float x, out float z)
        {
            if (PathIndex >= PathCount)
            {
                x = 0f;
                z = 0f;
                return false;
            }

            x = PathPointsX[PathIndex];
            z = PathPointsZ[PathIndex];
            return true;
        }

        public UnitSnapshot ToSnapshot()
        {
            bool hasCarry = CarryAmount > 0 && CarryType.HasValue;
            bool idle = IsAlive
                        && !IsGarrisoned
                        && !MoveTargetX.HasValue
                        && !AttackTargetId.HasValue
                        && !GatherTargetId.HasValue
                        && !Patrolling
                        && !AttackMoving
                        && !ReturningToDeposit
                        && ActiveTraversalLinkId < 0;
            string eq0 = AppliedEquipmentCount > 0 ? AppliedEquipmentIds[0] : null;
            string eq1 = AppliedEquipmentCount > 1 ? AppliedEquipmentIds[1] : null;
            return new UnitSnapshot(
                Id,
                Owner,
                Faction,
                DefinitionId,
                X,
                Z,
                Health,
                MaxHealth,
                IsAlive,
                CarryAmount,
                hasCarry ? CarryType.Value : ResourceType.Gold,
                hasCarry,
                idle,
                Stance,
                IsGarrisoned,
                SightRadius,
                eq0,
                eq1,
                ComputeEquipmentVisualFlags(),
                MoveTargetX.HasValue,
                MoveTargetX ?? X,
                MoveTargetZ ?? Z,
                AttackTargetId.HasValue,
                AttackTargetId.HasValue ? AttackTargetId.Value.Value : 0u,
                AttackMoving,
                Patrolling);
        }

        public byte ComputeEquipmentVisualFlags()
        {
            byte flags = 0;
            for (int i = 0; i < AppliedEquipmentCount; i++)
            {
                string id = AppliedEquipmentIds[i];
                if (string.IsNullOrEmpty(id))
                    continue;
                if (id.Contains("fire") || id.Contains("thorn") || id.Contains("sacred") || id.Contains("blade"))
                    flags |= 1; // flame weapon visual
                if (id.Contains("armour") || id.Contains("armor") || id.Contains("bark") || id.Contains("plate"))
                    flags |= 2; // reinforced armour visual
            }

            return flags;
        }
    }

    public sealed class SimBuilding : IBuilding
    {
        public const int MaxQueue = 4;

        public SimEntityId Id { get; }
        public PlayerId Owner { get; }
        public FactionId Faction { get; }
        public string DefinitionId { get; set; }
        public BuildingState State { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public bool CanProduce => State == BuildingState.Active && _canProduce;

        public float X;
        public float Z;
        public float FootprintRadius;
        public float FootprintHalfX;
        public float FootprintHalfZ;
        public float BuildSecondsTotal;
        public float BuildSecondsRemaining;
        public string[] TrainableUnitIds;
        public string ProductionUnitDefId;
        public float ProductionSecondsRemaining;
        public float ProductionSecondsTotal;
        public string ResearchUpgradeDefId;
        public float ResearchSecondsRemaining;
        public float ResearchSecondsTotal;
        public readonly string[] Queue = new string[MaxQueue];
        public int QueueCount;
        public int QueueCapacity;
        public float? RallyX;
        public float? RallyZ;
        public BuildingKind Kind;
        public float AttackDamage;
        public float AttackRange;
        public float AttackCooldown;
        public float AttackCooldownRemaining;
        public float SightRadius;
        public int GoldPerSecond;
        public BuildingCategory Category;
        public bool AllowsGarrison;
        public int GarrisonCapacity;
        public float CommandRadius;
        public bool SnapToWallGrid;
        public float WallSegmentLength;
        /// <summary>Bit0=N,1=E,2=S,3=W — neighbour wall segments.</summary>
        public byte WallLinks;
        /// <summary>Placement yaw snapped to 0/90/180/270. Swaps wall footprint axes at 90/270.</summary>
        public float YawDegrees;
        /// <summary>Paired portal gate id (0 = unpaired).</summary>
        public uint LinkedPortalId;
        /// <summary>Faction-built bridge prop id (0 = none).</summary>
        public uint LinkedDestructibleId;
        /// <summary>Faction-built bridge traversal link (-1 = none).</summary>
        public int LinkedTraversalLinkId = -1;
        public const int MaxGarrison = 16;
        public readonly uint[] GarrisonUnitIds = new uint[MaxGarrison];
        public int GarrisonCount;
        public const int MaxAttachmentSlots = 4;
        public int AttachmentSlotCount;
        public float AttachmentRadius = 22f;
        public string[] AttachmentAllowedBuildingIds = System.Array.Empty<string>();
        public readonly uint[] AttachmentOccupantIds = new uint[MaxAttachmentSlots];
        public SimEntityId? ParentBuildingId;
        public byte AttachmentSlotIndex;
        public bool IsProducing => !string.IsNullOrEmpty(ProductionUnitDefId);
        public bool IsResearching => !string.IsNullOrEmpty(ResearchUpgradeDefId);

        private readonly bool _canProduce;

        public SimBuilding(
            SimEntityId id,
            PlayerId owner,
            FactionId faction,
            BuildingDefData def,
            float x,
            float z,
            bool startActive)
        {
            Id = id;
            Owner = owner;
            Faction = faction;
            DefinitionId = def.Id;
            MaxHealth = def.MaxHealth;
            Health = def.MaxHealth;
            X = x;
            Z = z;
            FootprintHalfX = def.FootprintX > 0.5f ? def.FootprintX * 0.5f : 2f;
            FootprintHalfZ = def.FootprintZ > 0.5f ? def.FootprintZ * 0.5f : 2f;
            FootprintRadius = MathF.Max(FootprintHalfX, FootprintHalfZ);
            if (def.Kind == BuildingKind.Wall)
                FootprintRadius = MathF.Max(FootprintRadius, MathF.Min(FootprintHalfX, FootprintHalfZ) + 1f);
            _canProduce = def.CanProduce;
            TrainableUnitIds = def.TrainableUnitIds ?? System.Array.Empty<string>();
            QueueCapacity = def.QueueCapacity > 0 ? System.Math.Min(def.QueueCapacity, MaxQueue) : 3;
            BuildSecondsTotal = def.BuildSeconds;
            Kind = def.Kind;
            Category = ResolveCategory(def);
            AllowsGarrison = def.AllowsGarrison || def.Kind == BuildingKind.Keep || def.Kind == BuildingKind.Tower;
            GarrisonCapacity = def.GarrisonCapacity > 0
                ? def.GarrisonCapacity
                : (def.Kind == BuildingKind.Keep ? 8 : def.Kind == BuildingKind.Tower ? 2 : 0);
            CommandRadius = def.CommandRadius > 0f
                ? def.CommandRadius
                : (def.Kind == BuildingKind.Keep ? 120f : 0f);
            SnapToWallGrid = def.SnapToWallGrid || def.Kind == BuildingKind.Wall || def.Kind == BuildingKind.Gate;
            WallSegmentLength = def.WallSegmentLength > 1f ? def.WallSegmentLength : 14f;
            AttackDamage = def.AttackDamage;
            AttackRange = def.AttackRange;
            AttackCooldown = def.AttackCooldown > 0f ? def.AttackCooldown : 1.5f;
            SightRadius = def.SightRadius;
            GoldPerSecond = def.GoldPerSecond;
            AttachmentSlotCount = def.AttachmentSlotCount > 0
                ? Math.Min(def.AttachmentSlotCount, MaxAttachmentSlots)
                : 0;
            AttachmentRadius = def.AttachmentRadius > 1f ? def.AttachmentRadius : 22f;
            AttachmentAllowedBuildingIds = def.AttachmentAllowedBuildingIds ?? System.Array.Empty<string>();
            if (startActive)
            {
                State = BuildingState.Active;
                BuildSecondsRemaining = 0f;
            }
            else
            {
                State = BuildingState.Constructing;
                BuildSecondsRemaining = def.BuildSeconds;
            }
        }

        private static BuildingCategory ResolveCategory(BuildingDefData def)
        {
            switch (def.Kind)
            {
                case BuildingKind.Keep:
                    return BuildingCategory.Castle;
                case BuildingKind.Producer:
                    return BuildingCategory.Troop;
                case BuildingKind.Tower:
                    return BuildingCategory.Tower;
                case BuildingKind.Wall:
                case BuildingKind.Gate:
                    return BuildingCategory.Wall;
                case BuildingKind.Outpost:
                    return BuildingCategory.Resource;
                case BuildingKind.Special:
                    return BuildingCategory.Special;
                default:
                    return def.Category;
            }
        }

        public BuildingSnapshot ToSnapshot()
        {
            float prodProgress = 0f;
            if (IsProducing && ProductionSecondsTotal > 0.001f)
                prodProgress = 1f - (ProductionSecondsRemaining / ProductionSecondsTotal);
            float buildProgress = 1f;
            if (State == BuildingState.Constructing && BuildSecondsTotal > 0.001f)
                buildProgress = 1f - (BuildSecondsRemaining / BuildSecondsTotal);
            float researchProgress = 0f;
            if (IsResearching && ResearchSecondsTotal > 0.001f)
                researchProgress = 1f - (ResearchSecondsRemaining / ResearchSecondsTotal);

            return new BuildingSnapshot(
                Id,
                Owner,
                Faction,
                DefinitionId,
                X,
                Z,
                State,
                CanProduce,
                Health,
                MaxHealth,
                ProductionUnitDefId,
                prodProgress,
                QueueCount + (IsProducing ? 1 : 0),
                QueueCount > 0 ? Queue[0] : null,
                QueueCount > 1 ? Queue[1] : null,
                QueueCount > 2 ? Queue[2] : null,
                QueueCount > 3 ? Queue[3] : null,
                RallyX ?? (X + 18f),
                RallyZ ?? Z,
                RallyX.HasValue,
                buildProgress,
                SightRadius,
                Kind,
                WallLinks,
                GarrisonCount,
                GarrisonCapacity,
                AllowsGarrison,
                AttachmentSlotCount,
                AttachmentOccupiedMask(),
                ResearchUpgradeDefId,
                researchProgress,
                YawDegrees);
        }

        public byte AttachmentOccupiedMask()
        {
            byte mask = 0;
            for (int i = 0; i < AttachmentSlotCount; i++)
            {
                if (AttachmentOccupantIds[i] != 0)
                    mask |= (byte)(1 << i);
            }

            return mask;
        }

        public bool TryAddGarrison(uint unitId)
        {
            if (!AllowsGarrison || GarrisonCount >= GarrisonCapacity || GarrisonCount >= MaxGarrison)
                return false;
            for (int i = 0; i < GarrisonCount; i++)
            {
                if (GarrisonUnitIds[i] == unitId)
                    return false;
            }

            GarrisonUnitIds[GarrisonCount++] = unitId;
            return true;
        }

        public bool TryRemoveGarrison(uint unitId)
        {
            for (int i = 0; i < GarrisonCount; i++)
            {
                if (GarrisonUnitIds[i] != unitId)
                    continue;
                GarrisonUnitIds[i] = GarrisonUnitIds[GarrisonCount - 1];
                GarrisonCount--;
                return true;
            }

            return false;
        }
    }

    public sealed class SimTerritory : ITerritoryNode
    {
        public SimEntityId Id { get; }
        public TerritoryState State { get; set; } = TerritoryState.Neutral;
        public PlayerId? Controller { get; set; }
        public float CaptureProgress { get; set; }

        public float X;
        public float Z;
        public float Radius;
        public int GoldPerSecondWhenControlled = 5;

        public SimTerritory(SimEntityId id, float x, float z, float radius)
        {
            Id = id;
            X = x;
            Z = z;
            Radius = radius;
        }

        public TerritorySnapshot ToSnapshot()
        {
            var has = Controller.HasValue;
            return new TerritorySnapshot(
                Id,
                X,
                Z,
                Radius,
                State,
                has ? Controller.Value : new PlayerId(0),
                has,
                CaptureProgress);
        }
    }

    public sealed class SimResourceNode : IResourceNode
    {
        public SimEntityId Id { get; }
        public ResourceType Type { get; }
        public int Remaining { get; private set; }
        public bool IsDepleted => Remaining <= 0;

        public float X;
        public float Z;

        public SimResourceNode(SimEntityId id, ResourceType type, int amount, float x, float z)
        {
            Id = id;
            Type = type;
            Remaining = amount;
            X = x;
            Z = z;
        }

        public int Extract(int requested)
        {
            int taken = requested > Remaining ? Remaining : requested;
            Remaining -= taken;
            return taken;
        }

        public ResourceSnapshot ToSnapshot()
        {
            return new ResourceSnapshot(Id, Type, Remaining, X, Z);
        }
    }
}
