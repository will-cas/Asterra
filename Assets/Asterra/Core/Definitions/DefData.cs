using Asterra.Core.World;

namespace Asterra.Core
{
    public enum UnitRole : byte
    {
        Infantry = 0,
        Ranged = 1,
        Cavalry = 2,
        Siege = 3,
        Builder = 4,
    }

    public enum BuildingKind : byte
    {
        Generic = 0,
        Keep = 1,
        Producer = 2,
        Tower = 3,
        Wall = 4,
        Outpost = 5,
        Gate = 6,
        Special = 7,
    }

    /// <summary>Coarse designer category for UI / filters (maps onto Kind).</summary>
    public enum BuildingCategory : byte
    {
        Castle = 0,
        Troop = 1,
        Tower = 2,
        Wall = 3,
        Resource = 4,
        Capture = 5,
        Special = 6,
    }

    /// <summary>Plain-data unit stats used by the lockstep sim (SO wrappers copy into these).</summary>
    public sealed class UnitDefData
    {
        public string Id;
        public string DisplayName;
        public float MaxHealth = 100f;
        public float MoveSpeed = 4f;
        public float AttackDamage = 10f;
        public float AttackRange = 2f;
        public float AttackCooldown = 1f;
        public int GoldCost = 50;
        public float TrainSeconds = 5f;
        public bool IsBuilder;
        public bool CanGather;
        public int CarryCapacity = 10;
        public float GatherRate = 4f;
        public UnitRole Role = UnitRole.Infantry;
        public float BuildingDamageMultiplier = 1f;
        public float Armor;
        public float ProjectileSpeed; // 0 = hitscan
        /// <summary>
        /// Where this unit may path. Default Land. Boats = Water; amphibious = Amphibious; flyers = Flying.
        /// </summary>
        public TraversalCapability TraversalCapabilities = TraversalCapability.Land;
        public float SightRadius = 110f;
        /// <summary>Faction leader / hero. Trained from the keep; more than one may be fielded.</summary>
        public bool IsLeader;
        /// <summary>Hard collision radius vs units and building footprints.</summary>
        public float CollisionRadius = 2.2f;
        /// <summary>
        /// How many troop meshes to show for this unit (presentation only).
        /// 0 = <see cref="BattalionRules"/> role default (infantry 16, ranged 12, cavalry 6, else 1).
        /// Leaders, builders, and siege stay a single figure.
        /// </summary>
        public int SquadSize;
        /// <summary>Wind-riders and similar: flying only after launching from height / perch.</summary>
        public bool RequiresHeightLaunch;
        /// <summary>Airborne seconds after a legal launch. 0 = stay up until landed by other rules.</summary>
        public float FlightDurationSeconds;
    }

    public enum UpgradeKind : byte
    {
        /// <summary>Researched at the keep; affects keeps / faction keep bonuses.</summary>
        Keep = 0,
        /// <summary>Equipment researched at barracks/workshop; applied to combat units.</summary>
        Equipment = 1,
        /// <summary>Researched fortification applied to owned walls / buildings.</summary>
        Fortification = 2,
    }

    public sealed class BuildingDefData
    {
        public string Id;
        public string DisplayName;
        public float MaxHealth = 500f;
        public int GoldCost = 100;
        public int TimberCost = 50;
        public float BuildSeconds = 8f;
        public float FootprintX = 4f;
        public float FootprintZ = 4f;
        public bool CanProduce;
        public string[] TrainableUnitIds = System.Array.Empty<string>();
        public int QueueCapacity = 3;
        public BuildingKind Kind = BuildingKind.Generic;
        public float AttackDamage;
        public float AttackRange;
        public float AttackCooldown = 1.5f;
        public float SightRadius;
        public int GoldPerSecond;
        public BuildingCategory Category = BuildingCategory.Special;
        public bool AllowsGarrison;
        public int GarrisonCapacity;
        public float CommandRadius;
        public bool SnapToWallGrid;
        public float WallSegmentLength = 14f;
        /// <summary>How many attachment pads this building exposes (keeps use 4 cardinal pads).</summary>
        public int AttachmentSlotCount;
        /// <summary>Building defs allowed on attachment pads (e.g. watchtower).</summary>
        public string[] AttachmentAllowedBuildingIds = System.Array.Empty<string>();
        /// <summary>World-space offset from keep center to each slot (slot 0 = north).</summary>
        public float AttachmentRadius = 22f;
    }

    public sealed class UpgradeDefData
    {
        public string Id;
        public string DisplayName;
        public int GoldCost = 200;
        public float TrainTimeMultiplier = 1f;
        public float UnitDamageMultiplier = 1f;
        /// <summary>Flat armor added when this equipment is applied to a unit.</summary>
        public float ArmorBonus;
        /// <summary>Flat attack damage added when this equipment is applied to a unit.</summary>
        public float AttackDamageBonus;
        /// <summary>Flat max-health added to owned keeps when researched.</summary>
        public float KeepHealthBonus;
        /// <summary>Extra keep sight radius when researched.</summary>
        public float KeepSightBonus;
        /// <summary>Seconds to research (0 = instant unlock).</summary>
        public float ResearchSeconds = 8f;
        public UpgradeKind Kind = UpgradeKind.Equipment;
        /// <summary>
        /// Gold charged each time this equipment is applied to one unit (after research).
        /// 0 = default to max(25, GoldCost / 4).
        /// </summary>
        public int EquipGoldCost;
        /// <summary>
        /// Bitmask of <see cref="UnitRole"/> values that may receive this equipment.
        /// 0 = all non-builder combat roles.
        /// </summary>
        public int CompatibleRoleMask;
        /// <summary>
        /// When set, only these unit definition ids may receive the upgrade.
        /// Empty = use <see cref="CompatibleRoleMask"/> / role rules.
        /// </summary>
        public string[] CompatibleUnitIds = System.Array.Empty<string>();
        /// <summary>Flat sight radius added when this equipment is applied.</summary>
        public float SightBonus;

        public int ResolvedEquipGoldCost =>
            EquipGoldCost > 0 ? EquipGoldCost : System.Math.Max(25, GoldCost / 4);

        /// <summary>Field-banner price: researched cost, or a premium if the tech is still locked.</summary>
        public int FieldEquipGoldCost(bool researched)
        {
            int cost = ResolvedEquipGoldCost;
            if (researched)
                return cost;
            return cost + System.Math.Max(15, cost / 2);
        }

        public bool FitsUnit(string unitDefId, UnitRole role)
        {
            if (CompatibleUnitIds != null && CompatibleUnitIds.Length > 0)
            {
                if (string.IsNullOrEmpty(unitDefId))
                    return false;
                bool listed = false;
                for (int i = 0; i < CompatibleUnitIds.Length; i++)
                {
                    if (CompatibleUnitIds[i] == unitDefId)
                    {
                        listed = true;
                        break;
                    }
                }

                if (!listed)
                    return false;
            }

            return FitsUnitRole(role);
        }

        public bool FitsUnitRole(UnitRole role)
        {
            if (CompatibleRoleMask == 0)
                return role != UnitRole.Builder;
            return (CompatibleRoleMask & (1 << (int)role)) != 0;
        }

        public static int RoleMask(params UnitRole[] roles)
        {
            int mask = 0;
            if (roles == null)
                return 0;
            for (int i = 0; i < roles.Length; i++)
                mask |= 1 << (int)roles[i];
            return mask;
        }
    }

    public enum PowerEffectKind : byte
    {
        ArmorAura = 0,
        MoveSpeedAura = 1,
        DamageAura = 2,
        ForceWeather = 3,
        SpawnSwarm = 4,
        PlaceGate = 5,
        EconomyBoost = 6,
        SpawnScouts = 7,
        MindControl = 8,
        SpawnRandomBeasts = 9,
        FloodArea = 10,
        EyesInSky = 11,
        ExplosiveStrip = 12,
        Forecast = 13,
        RelocateSight = 14,
        SunRay = 15,
        DayOfTheSun = 16,
        BlindRadius = 17,
    }

    /// <summary>Unlockable commander power with cooldown (sim plain-data).</summary>
    public sealed class PowerDefData
    {
        public string Id;
        public string DisplayName;
        public int UnlockGoldCost = 150;
        public float CooldownSeconds = 45f;
        public float DurationSeconds = 12f;
        public PowerEffectKind Effect = PowerEffectKind.ArmorAura;
        public float EffectMagnitude = 3f;
        public float BuildingMitigation;
        /// <summary>
        /// When true, unlocking applies a permanent buff (no activate / cooldown).
        /// Commander kit: one passive + one or more actives.
        /// </summary>
        public bool IsPassive;
        /// <summary>Named map-scale chapter (Halo Wars / BFME hero beat). HUD highlights this.</summary>
        public bool HeroMoment;
        /// <summary>Optional unit spawned by <see cref="PowerEffectKind.SpawnScouts"/>.</summary>
        public string SpawnUnitDefinitionId;
    }

    /// <summary>Deterministic role vs role / building damage multipliers.</summary>
    public static class CombatMath
    {
        public static float RoleMultiplier(UnitRole attacker, UnitRole defender)
        {
            switch (attacker)
            {
                case UnitRole.Infantry:
                    return defender == UnitRole.Cavalry ? 0.85f
                        : defender == UnitRole.Siege ? 1.25f
                        : 1f;
                case UnitRole.Ranged:
                    return defender == UnitRole.Infantry ? 1.2f
                        : defender == UnitRole.Cavalry ? 0.75f
                        : defender == UnitRole.Siege ? 1.15f
                        : 1f;
                case UnitRole.Cavalry:
                    return defender == UnitRole.Ranged ? 1.35f
                        : defender == UnitRole.Siege ? 1.2f
                        : defender == UnitRole.Infantry ? 0.9f
                        : 1f;
                case UnitRole.Siege:
                    return defender == UnitRole.Infantry ? 0.7f
                        : defender == UnitRole.Cavalry ? 0.65f
                        : 1f;
                case UnitRole.Builder:
                    return 0.25f;
                default:
                    return 1f;
            }
        }

        public static float ApplyArmor(float damage, float armor)
        {
            if (armor <= 0f)
                return damage;
            return System.Math.Max(1f, damage - armor);
        }
    }
}
