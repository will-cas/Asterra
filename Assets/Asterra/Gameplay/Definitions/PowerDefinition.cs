using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;
using UnityEngine;

namespace Asterra.Gameplay
{
    [CreateAssetMenu(menuName = "Asterra/Power Definition", fileName = "Power_")]
    public sealed class PowerDefinition : ScriptableObject
    {
        public string Id = "ability_id";
        public string DisplayName = "Ability";
        public int UnlockGoldCost = 150;
        public float CooldownSeconds = 45f;
        public float DurationSeconds = 12f;
        public PowerEffectKind Effect = PowerEffectKind.ArmorAura;
        public float EffectMagnitude = 3f;
        public float BuildingMitigation;
        public bool IsPassive;
        public bool HeroMoment;

        public PowerDefData ToData()
        {
            return new PowerDefData
            {
                Id = Id,
                DisplayName = DisplayName,
                UnlockGoldCost = UnlockGoldCost,
                CooldownSeconds = CooldownSeconds,
                DurationSeconds = DurationSeconds,
                Effect = Effect,
                EffectMagnitude = EffectMagnitude,
                BuildingMitigation = BuildingMitigation,
                IsPassive = IsPassive,
                HeroMoment = HeroMoment,
            };
        }
    }

    /// <summary>
    /// Designer-facing faction roster. Author in the Inspector; falls back to
    /// <see cref="FactionDefaultContent"/> when fields are empty.
    /// </summary>
    public static class FactionDefinitionExtensions
    {
        public static FactionRoster ToRoster(this FactionDefinition def)
        {
            if (def == null)
                return FactionDefaultContent.VeiledInheritance;

            var fallback = FactionDefaultContent.Get(new FactionId(def.FactionIndex));
            var roster = new FactionRoster
            {
                Id = new FactionId(def.FactionIndex),
                DefinitionId = string.IsNullOrEmpty(def.Id) ? fallback.DefinitionId : def.Id,
                DisplayName = string.IsNullOrEmpty(def.DisplayName) ? fallback.DisplayName : def.DisplayName,
                LoreBlurb = string.IsNullOrEmpty(def.LoreBlurb) ? fallback.LoreBlurb : def.LoreBlurb,
                KeepBuildingId = fallback.KeepBuildingId,
                ProducerBuildingId = fallback.ProducerBuildingId,
                BasicUnitId = fallback.BasicUnitId,
                BuilderUnitId = fallback.BuilderUnitId,
                RangedUnitId = fallback.RangedUnitId,
                CavalryUnitId = fallback.CavalryUnitId,
                EliteUnitId = fallback.EliteUnitId,
                SiegeUnitId = fallback.SiegeUnitId,
                ScoutUnitId = fallback.ScoutUnitId,
                SapperUnitId = fallback.SapperUnitId,
                TowerBuildingId = fallback.TowerBuildingId,
                WallBuildingId = fallback.WallBuildingId,
                OutpostBuildingId = fallback.OutpostBuildingId,
                BasicUpgradeId = fallback.BasicUpgradeId,
                KeepUpgradeIds = fallback.KeepUpgradeIds,
                EquipmentUpgradeIds = fallback.EquipmentUpgradeIds,
                LeaderUnitId = fallback.LeaderUnitId,
                PowerId = fallback.PowerId,
                PowerDisplayName = fallback.PowerDisplayName,
                PowerIds = fallback.PowerIds,
                ExtraBuildingIds = fallback.ExtraBuildingIds,
                SignatureBuildingId = fallback.SignatureBuildingId,
            };

            if (def.DefaultCommander != null && !string.IsNullOrEmpty(def.DefaultCommander.ActiveAbilityId))
            {
                roster.PowerId = def.DefaultCommander.ActiveAbilityId;
                roster.PowerDisplayName = def.DefaultCommander.DisplayName;
            }

            if (def.DefaultCommander != null && !string.IsNullOrEmpty(def.DefaultCommander.PassivePowerId))
            {
                string passive = def.DefaultCommander.PassivePowerId;
                if (!ContainsId(roster.PowerIds, passive))
                {
                    var merged = new string[(roster.PowerIds?.Length ?? 0) + 1];
                    merged[0] = passive;
                    if (roster.PowerIds != null)
                        System.Array.Copy(roster.PowerIds, 0, merged, 1, roster.PowerIds.Length);
                    roster.PowerIds = merged;
                }
            }

            return roster;
        }

        private static bool ContainsId(string[] ids, string id)
        {
            if (ids == null || string.IsNullOrEmpty(id))
                return false;
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] == id)
                    return true;
            }

            return false;
        }

        public static void RegisterPowers(this FactionDefinition def, DefinitionRegistry registry)
        {
            if (def?.Powers == null || registry == null)
                return;
            for (int i = 0; i < def.Powers.Length; i++)
            {
                if (def.Powers[i] != null)
                    registry.Register(def.Powers[i].ToData());
            }
        }
    }
}
