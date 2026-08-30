using Asterra.Core;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay.Content
{
    /// <summary>Plain-data roster for one launch faction.</summary>
    public sealed class FactionRoster
    {
        public FactionId Id;
        public string DefinitionId;
        public string DisplayName;
        public string KeepBuildingId;
        public string ProducerBuildingId;
        public string BasicUnitId;
        public string BuilderUnitId;
        public string RangedUnitId;
        public string CavalryUnitId;
        /// <summary>Faction specialty unit. Empty = none.</summary>
        public string EliteUnitId;
        public string SiegeUnitId;
        /// <summary>Shared scout (Pathfinder). Trainable at producer.</summary>
        public string ScoutUnitId;
        /// <summary>Shared anti-structure sapper. Trainable at producer.</summary>
        public string SapperUnitId;
        public string TowerBuildingId;
        public string WallBuildingId;
        public string OutpostBuildingId;
        /// <summary>Additional placeable buildings (extra producers, unique economy, portals).</summary>
        public string[] ExtraBuildingIds = System.Array.Empty<string>();
        /// <summary>Keep-adjacent signature building (BFME citadel unique).</summary>
        public string SignatureBuildingId;
        public string BasicUpgradeId;
        /// <summary>Keep-only techs (bastion, keepward, etc.).</summary>
        public string[] KeepUpgradeIds = System.Array.Empty<string>();
        /// <summary>Equipment researched at barracks/workshop (armour, weapons).</summary>
        public string[] EquipmentUpgradeIds = System.Array.Empty<string>();
        /// <summary>Legacy alias — keep upgrades.</summary>
        public string[] UpgradeIds
        {
            get => KeepUpgradeIds;
            set => KeepUpgradeIds = value ?? System.Array.Empty<string>();
        }
        public string LeaderUnitId;
        public string PowerId;
        public string PowerDisplayName;
        public string[] PowerIds = System.Array.Empty<string>();
        public string LoreBlurb;
    }

    /// <summary>
    /// Launch factions: Uncrowned, Mundor Crown, Outcast Host, Freetown, University Guild, Church of the Rising Sun.
    /// Internal definition ids stay stable; display names match the wiki.
    /// </summary>
    public static class FactionDefaultContent
    {
        public const string KeepTurretId = "building_keep_turret";
        public const string RiverBoatId = "unit_river_boat";
        public const string PathfinderId = "unit_pathfinder";
        public const string SapperId = "unit_sapper";
        public const string WatchtowerId = "building_watchtower";
        public const string PalisadeId = "building_palisade";
        public const string BridgeId = "building_bridge";
        public const string TrenchWorksId = "building_trench_works";
        public const string BermWorksId = "building_berm_works";
        public const string MoatWorksId = "building_moat_works";
        public const string FillWorksId = "building_fill_works";
        public const string ClearWorksId = "building_clear_works";
        public const string BurnWorksId = "building_burn_works";
        public const string QuarryWorksId = "building_quarry_works";
        public const string SpikesWorksId = "building_spikes_works";
        public const string DebrisWorksId = "building_debris_works";
        public const string BarricadeId = "building_barricade";
        public const string FerryDockId = "building_ferry_dock";
        public const string StoneWallId = "building_stone_wall";
        public const string StoneWallsUpgradeId = "upgrade_stone_walls";
        public const string OutpostId = "building_outpost";

        // The Uncrowned (forbidden-magic schism)
        public const string VeiledInheritanceId = "faction_veiled_inheritance";
        public const string VeiledApprenticeId = "unit_veiled_apprentice";
        public const string VeiledBuilderId = "unit_veiled_builder";
        public const string VeiledRuneCasterId = "unit_veiled_rune_caster";
        public const string VeiledElementalId = "unit_veiled_elemental";
        public const string VeiledGolemId = "unit_veiled_golem";
        public const string VeiledPriestGuardId = "unit_veiled_priest_guard";
        public const string VeiledShadowId = "unit_veiled_shadow";
        public const string VeiledAssassinId = "unit_veiled_assassin";
        public const string VeiledMassedId = "unit_veiled_massed";
        public const string VeiledSoulingId = "unit_veiled_souling";
        public const string VeiledHeirId = "unit_veiled_heir";
        public const string VeiledColossusLeaderId = "unit_veiled_colossus";
        public const string VeiledThornSpeakerId = "unit_veiled_thorn_speaker";
        public const string VeiledNightAbbotId = "unit_veiled_night_abbot";
        public const string VeiledFirstHereticId = "unit_veiled_first_heretic";
        public const string VeiledDarkSpyId = "unit_veiled_dark_spy";
        public const string VeiledShadeId = "unit_veiled_shade";
        public const string ArcaneumId = "building_arcaneum";
        public const string ArcaneAcademyId = "building_arcane_academy";
        public const string BlackrootConservatoryId = "building_blackroot_conservatory";
        public const string AncientRuinsId = "building_ancient_ruins";
        public const string ConjuringHallId = "building_conjuring_hall";
        public const string HighTempleId = "building_high_temple";
        public const string PortalGateId = "building_portal_gate";
        public const string ShadowedGateBuildingId = "building_shadowed_gate";
        public const string VeiledMailId = "upgrade_veiled_mail";
        public const string DesertStormUpgradeId = "upgrade_desert_storm";
        public const string RainfallUpgradeId = "upgrade_rainfall";
        public const string FogOfWarUpgradeId = "upgrade_fog_of_war";
        public const string IceFormationUpgradeId = "upgrade_ice_formation";
        public const string BoundCoreUpgradeId = "upgrade_bound_core";
        public const string NightStrideUpgradeId = "upgrade_night_stride";
        public const string SilentVenomUpgradeId = "upgrade_silent_venom";
        public const string SwarmHymnUpgradeId = "upgrade_swarm_hymn";
        public const string UnquietHungerUpgradeId = "upgrade_unquiet_hunger";
        public const string ArtificerPlatingUpgradeId = "upgrade_artificer_plating";
        public const string ForbiddenCurriculumId = "upgrade_forbidden_curriculum";
        public const string VeiledBastionId = "upgrade_veiled_bastion";
        public const string ForbiddenGiftPassiveId = "passive_forbidden_gift";
        public const string WrathOfSkiesAbilityId = "ability_wrath_of_skies";
        public const string ShadowedGateAbilityId = "ability_shadowed_gate";
        public const string TwinGatesAbilityId = "ability_twin_gates";
        public const string MagicalAbundanceAbilityId = "ability_magical_abundance";
        public const string DarkSpiesAbilityId = "ability_dark_spies";
        public const string RunebindAbilityId = "ability_runebind";
        public const string ThrallbindAbilityId = "ability_thrallbind";
        public const string BeastChorusAbilityId = "ability_beast_chorus";

        // The Mundor Crown (royal tomb-kings)
        public const string MundorCrownId = "faction_mundor_crown";
        public const string RoyalPeasantId = "unit_royal_peasant";
        public const string RoyalBuilderId = "unit_royal_builder";
        public const string RoyalLegionId = "unit_royal_legion";
        public const string RoyalGuardId = "unit_royal_guard";
        public const string RoyalLongbowId = "unit_royal_longbow";
        public const string RoyalCommanderId = "unit_royal_commander";
        public const string RoyalSpyId = "unit_royal_spy";
        public const string RoyalCrownEyeId = "unit_royal_crown_eye";
        public const string RoyalPioneerId = "unit_royal_pioneer";
        public const string RoyalOnagerId = "unit_royal_onager";
        public const string RoyalKingId = "unit_royal_king";
        public const string RoyalLegionMarshalId = "unit_royal_legion_marshal";
        public const string RoyalSpymasterId = "unit_royal_spymaster";
        public const string RoyalTombWardenId = "unit_royal_tomb_warden";
        public const string RoyalJusticiarId = "unit_royal_justiciar";
        public const string RoyalCitadelId = "building_royal_citadel";
        public const string RoyalBarracksId = "building_royal_barracks";
        public const string RoyalCourtId = "building_royal_court";
        public const string RoyalFarmId = "building_royal_farm";
        public const string RoyalOutpostTowerId = "building_royal_outpost_tower";
        public const string RoyalWallId = "building_royal_wall";
        public const string MundorArmourId = "upgrade_mundor_armour";
        public const string FineSteelId = "upgrade_fine_steel";
        public const string MasterTrainingId = "upgrade_master_training";
        public const string YewNocksId = "upgrade_yew_nocks";
        public const string WhisperCloakId = "upgrade_whisper_cloak";
        public const string TombStonesId = "upgrade_tomb_stones";
        public const string PioneerMaulId = "upgrade_pioneer_maul";
        public const string MusterRollsId = "upgrade_muster_rolls";
        public const string TombRightsId = "upgrade_tomb_rights";
        public const string TombOathPassiveId = "passive_tomb_oath";
        public const string RoyalStandardAbilityId = "ability_royal_standard";
        public const string LevyHornAbilityId = "ability_levy_horn";
        public const string RainOfArrowsAbilityId = "ability_rain_of_arrows";
        public const string LastMarchAbilityId = "ability_last_march";
        public const string HarvestTitheAbilityId = "ability_harvest_tithe";
        public const string EyesOfTheCrownAbilityId = "ability_eyes_of_the_crown";
        public const string KingsChargeAbilityId = "ability_kings_charge";

        // The Outcast Host (free camps, beasts, villages)
        public const string OutcastId = "faction_outcast_host";
        public const string OutcastVillagerId = "unit_outcast_villager";
        public const string OutcastBuilderId = "unit_outcast_hobgoblin";
        public const string OutcastHunterId = "unit_outcast_hunter";
        public const string OutcastRangerId = "unit_outcast_ranger";
        public const string OutcastBeastRiderId = "unit_outcast_beast_rider";
        public const string OutcastGiantId = "unit_outcast_frost_giant";
        public const string OutcastSnarerId = "unit_outcast_snarer";
        public const string OutcastWindRiderId = "unit_outcast_wind_rider";
        public const string OutcastSpriteId = "unit_outcast_sprite";
        public const string OutcastNatureCubId = "unit_outcast_nature_cub";
        public const string OutcastSkyEyeId = "unit_outcast_sky_eye";
        public const string OutcastHeirId = "unit_outcast_exiled_heir";
        public const string OutcastWoldId = "unit_outcast_great_wold";
        public const string OutcastElderId = "unit_outcast_village_elder";
        public const string OutcastHuntCallerId = "unit_outcast_hunt_caller";
        public const string OutcastGreatCampId = "building_outcast_great_camp";
        public const string OutcastBurrowsId = "building_outcast_burrows";
        public const string OutcastAerieId = "building_outcast_aerie";
        public const string OutcastVillageHallId = "building_outcast_village_hall";
        public const string OutcastMineId = "building_outcast_mine";
        public const string OutcastGroundWorksId = "building_outcast_ground_works";
        public const string OutcastTreetopWatchId = "building_outcast_treetop_watch";
        public const string CloakedUpgradeId = "upgrade_cloaked";
        public const string GreatPerchUpgradeId = "upgrade_great_perch";
        public const string RollingFogUpgradeId = "upgrade_rolling_fog";
        public const string NaturesCamouflageUpgradeId = "upgrade_natures_camouflage";
        public const string GiantHideUpgradeId = "upgrade_giant_hide";
        public const string SaddleBindUpgradeId = "upgrade_saddle_bind";
        public const string SnareCordsUpgradeId = "upgrade_snare_cords";
        public const string WindHarnessUpgradeId = "upgrade_wind_harness";
        public const string WildBondPassiveId = "passive_wild_bond";
        public const string NaturesAidAbilityId = "ability_natures_aid";
        public const string DamBreakerAbilityId = "ability_dam_breaker";
        public const string EyesInSkyAbilityId = "ability_eyes_in_sky";
        public const string CampfireAbilityId = "ability_campfire";
        public const string StampedeAbilityId = "ability_stampede";
        public const string GreenTitheAbilityId = "ability_green_tithe";

        // Freetown (seaside mix, southern island outposts)
        public const string FreetownId = "faction_freetown";
        public const string FreetownDrunkId = "unit_freetown_drunk";
        public const string FreetownBuilderId = "unit_freetown_builder";
        public const string FreetownMudslingerId = "unit_freetown_mudslinger";
        public const string FreetownPrivateerId = "unit_freetown_privateer";
        public const string FreetownHighwaymanId = "unit_freetown_highwayman";
        public const string FreetownCrowId = "unit_freetown_crow";
        public const string FreetownHoundId = "unit_freetown_hound";
        public const string FreetownCrabId = "unit_freetown_warrior_crab";
        public const string FreetownBruteId = "unit_freetown_brute";
        public const string FreetownImpId = "unit_freetown_jump_imp";
        public const string FreetownFodderId = "unit_freetown_cannon_fodder";
        public const string FreetownSapperId = "unit_freetown_improvised_explosive";
        public const string FreetownFlamerId = "unit_freetown_flamer";
        public const string FreetownPowderCartId = "unit_freetown_powder_cart";
        public const string FreetownBrewmasterId = "unit_freetown_brewmaster";
        public const string FreetownCaptainId = "unit_freetown_captain";
        public const string FreetownDockmasterId = "unit_freetown_dockmaster";
        public const string FreetownFenceId = "unit_freetown_fence";
        public const string FreetownIslandSpeakerId = "unit_freetown_island_speaker";
        public const string FreetownTavernId = "building_freetown_tavern";
        public const string FreetownSmugglersDenId = "building_freetown_smugglers_den";
        public const string FreetownHutId = "building_freetown_hut";
        public const string FreetownBlackMarketId = "building_freetown_black_market";
        public const string FreetownCrowsNestId = "building_freetown_crows_nest";
        public const string FreetownBarricadesId = "building_freetown_barricades";
        public const string GrappleHooksUpgradeId = "upgrade_grapple_hooks";
        public const string RageUpgradeId = "upgrade_rage";
        public const string RareLootUpgradeId = "upgrade_rare_loot";
        public const string FirebrewUpgradeId = "upgrade_firebrew";
        public const string BlastFuseUpgradeId = "upgrade_blast_fuse";
        public const string CrabPlateUpgradeId = "upgrade_crab_plate";
        public const string ShipNailsUpgradeId = "upgrade_ship_nails";
        public const string PortCallPassiveId = "passive_port_call";
        public const string TradeSurplusAbilityId = "ability_trade_surplus";
        public const string ExplosiveConvoyAbilityId = "ability_explosive_convoy";
        public const string MercenariesAbilityId = "ability_mercenaries";
        public const string SurprisedDeliveryAbilityId = "ability_surprised_delivery";
        public const string SeaLegsAbilityId = "ability_sea_legs";
        public const string RiotAbilityId = "ability_riot";
        public const string CrowStormAbilityId = "ability_crow_storm";

        // University Guild (ancients, climate, weapons, literature)
        public const string UniversityId = "faction_university_guild";
        public const string UniversityFellowId = "unit_university_fellow";
        public const string UniversityBuilderId = "unit_university_practitioner";
        public const string UniversityPoisonId = "unit_university_poison_specialist";
        public const string UniversitySpiderId = "unit_university_mechanical_spider";
        public const string UniversityAirshipId = "unit_university_airship";
        public const string UniversityTrebuchetId = "unit_university_trebuchet";
        public const string UniversityEarthBreakerId = "unit_university_earth_breaker";
        public const string UniversityChancellorId = "unit_university_chancellor";
        public const string UniversityArmsDeanId = "unit_university_arms_dean";
        public const string UniversityClimateDeanId = "unit_university_climate_dean";
        public const string UniversityArchivistId = "unit_university_archivist";
        public const string UniversityProvostId = "unit_university_provost";
        public const string UniversityCollegeId = "building_university_grand_college";
        public const string UniversityWorkshopId = "building_university_workshop";
        public const string UniversityLibraryId = "building_university_forbidden_library";
        public const string UniversityAlchemistId = "building_university_alchemist";
        public const string UniversityClockworkTowerId = "building_university_clockwork_tower";
        public const string UniversityMoatId = "building_university_moat";
        public const string UniversityObservatoryId = "building_university_grand_observatory";
        public const string UniversityWeatherRodsId = "building_university_weather_rods";
        public const string UniversityFarGlassId = "building_university_far_glass";
        public const string GreatSpyglassUpgradeId = "upgrade_great_spyglass";
        public const string AdvancedCogsUpgradeId = "upgrade_advanced_cogs";
        public const string AdvancedConstructionUpgradeId = "upgrade_advanced_construction";
        public const string AlchemicalTipsUpgradeId = "upgrade_alchemical_tips";
        public const string CounterweightUpgradeId = "upgrade_counterweight";
        public const string ForecastAbilityId = "ability_forecast";
        public const string FarGlassAbilityId = "ability_far_glass";
        public const string OpenLectureAbilityId = "ability_open_lecture";
        public const string FieldExerciseAbilityId = "ability_field_exercise";
        public const string PrecisionDrillAbilityId = "ability_precision_drill";
        public const string TenureAbilityId = "ability_tenure";
        public const string PublishedPaperAbilityId = "ability_published_paper";
        public const string ClockworkMusterAbilityId = "ability_clockwork_muster";

        // Church of the Rising Sun (false-ancient light cult)
        public const string ChurchId = "faction_rising_sun";
        public const string ChurchZealotId = "unit_church_dawn_zealot";
        public const string ChurchMasonId = "unit_church_mason";
        public const string ChurchPriestId = "unit_church_sun_priest";
        public const string ChurchStalkerId = "unit_church_sun_stalker";
        public const string ChurchRiderId = "unit_church_dawn_rider";
        public const string ChurchGuardId = "unit_church_radiant_guard";
        public const string ChurchEngineId = "unit_church_solar_engine";
        public const string ChurchPurifierId = "unit_church_purifier";
        public const string ChurchHighPriestId = "unit_church_high_priest";
        public const string ChurchInquisitorId = "unit_church_inquisitor";
        public const string ChurchEclipseWardenId = "unit_church_eclipse_warden";
        public const string ChurchDawnHeraldId = "unit_church_dawn_herald";
        public const string ChurchReliquaryId = "unit_church_reliquary";
        public const string ChurchGrandTempleId = "building_church_grand_temple";
        public const string ChurchMonasteryId = "building_church_warrior_monastery";
        public const string ChurchSunTempleId = "building_church_sun_temple";
        public const string ChurchSacredSiteId = "building_church_sacred_site";
        public const string ChurchScorchedTowerId = "building_church_scorched_tower";
        public const string ChurchShrineId = "building_church_offering_shrine";
        public const string ChurchSacredWallsId = "building_church_sacred_walls";
        public const string SolarVestmentsUpgradeId = "upgrade_solar_vestments";
        public const string ScorchedShotUpgradeId = "upgrade_scorched_shot";
        public const string SacredMasonryUpgradeId = "upgrade_sacred_masonry";
        public const string StalkerVeilUpgradeId = "upgrade_stalker_veil";
        public const string SunRayAbilityId = "ability_sun_ray";
        public const string DayOfTheSunAbilityId = "ability_day_of_the_sun";
        public const string BlindAbilityId = "ability_blind";
        public const string TitheAbilityId = "ability_tithe";
        public const string SolarOathAbilityId = "ability_solar_oath";
        public const string ProcessionAbilityId = "ability_procession";
        public const string PurgeTheDarkAbilityId = "ability_purge_the_dark";
        public const string FalseChroniclePassiveId = "passive_false_chronicle";

        public static readonly FactionRoster VeiledInheritance = new FactionRoster
        {
            Id = new FactionId(0),
            DefinitionId = VeiledInheritanceId,
            DisplayName = "The Uncrowned",
            KeepBuildingId = ArcaneumId,
            ProducerBuildingId = ArcaneAcademyId,
            BasicUnitId = VeiledApprenticeId,
            BuilderUnitId = VeiledBuilderId,
            RangedUnitId = VeiledRuneCasterId,
            CavalryUnitId = VeiledShadowId,
            EliteUnitId = VeiledPriestGuardId,
            SiegeUnitId = VeiledGolemId,
            ScoutUnitId = VeiledAssassinId,
            SapperUnitId = VeiledSoulingId,
            TowerBuildingId = WatchtowerId,
            WallBuildingId = PalisadeId,
            OutpostBuildingId = OutpostId,
            ExtraBuildingIds = new[]
            {
                BlackrootConservatoryId,
                AncientRuinsId,
                ConjuringHallId,
                HighTempleId,
                PortalGateId,
            },
            SignatureBuildingId = PortalGateId,
            BasicUpgradeId = VeiledMailId,
            KeepUpgradeIds = new[]
            {
                ForbiddenCurriculumId,
                VeiledBastionId,
                StoneWallsUpgradeId,
                DesertStormUpgradeId,
                RainfallUpgradeId,
                FogOfWarUpgradeId,
                IceFormationUpgradeId,
            },
            EquipmentUpgradeIds = new[]
            {
                VeiledMailId,
                BoundCoreUpgradeId,
                NightStrideUpgradeId,
                SilentVenomUpgradeId,
                SwarmHymnUpgradeId,
                UnquietHungerUpgradeId,
                ArtificerPlatingUpgradeId,
            },
            LeaderUnitId = VeiledHeirId,
            PowerId = WrathOfSkiesAbilityId,
            PowerDisplayName = "Wrath of Skies",
            PowerIds = new[]
            {
                ForbiddenGiftPassiveId,
                WrathOfSkiesAbilityId,
                ShadowedGateAbilityId,
                TwinGatesAbilityId,
                MagicalAbundanceAbilityId,
                DarkSpiesAbilityId,
                ThrallbindAbilityId,
                BeastChorusAbilityId,
            },
            LoreBlurb = "Public heretics of forbidden weather-magic, hidden in enclaves abroad. Blackroot gold rites. Their heir cannot inherit the old throne.",
        };

        public static readonly FactionRoster MundorCrown = new FactionRoster
        {
            Id = new FactionId(1),
            DefinitionId = MundorCrownId,
            DisplayName = "The Mundor Crown",
            KeepBuildingId = RoyalCitadelId,
            ProducerBuildingId = RoyalBarracksId,
            BasicUnitId = RoyalPeasantId,
            BuilderUnitId = RoyalBuilderId,
            RangedUnitId = RoyalLongbowId,
            CavalryUnitId = RoyalCommanderId,
            EliteUnitId = RoyalGuardId,
            SiegeUnitId = RoyalOnagerId,
            ScoutUnitId = RoyalSpyId,
            SapperUnitId = RoyalPioneerId,
            TowerBuildingId = RoyalOutpostTowerId,
            WallBuildingId = RoyalWallId,
            OutpostBuildingId = RoyalFarmId,
            ExtraBuildingIds = new[] { RoyalCourtId },
            SignatureBuildingId = RoyalCourtId,
            BasicUpgradeId = MundorArmourId,
            KeepUpgradeIds = new[] { MusterRollsId, TombRightsId, StoneWallsUpgradeId },
            EquipmentUpgradeIds = new[]
            {
                MundorArmourId,
                FineSteelId,
                MasterTrainingId,
                YewNocksId,
                WhisperCloakId,
                TombStonesId,
                PioneerMaulId,
            },
            LeaderUnitId = RoyalKingId,
            PowerId = RoyalStandardAbilityId,
            PowerDisplayName = "Royal Standard",
            PowerIds = new[]
            {
                TombOathPassiveId,
                RoyalStandardAbilityId,
                LevyHornAbilityId,
                RainOfArrowsAbilityId,
                LastMarchAbilityId,
                HarvestTitheAbilityId,
                EyesOfTheCrownAbilityId,
                KingsChargeAbilityId,
            },
            LoreBlurb = "Oldest sons inherit Mundor's throne. Kings live to earn a place in the Great Tombs of Rest.",
        };

        public static readonly FactionRoster Outcast = new FactionRoster
        {
            Id = new FactionId(2),
            DefinitionId = OutcastId,
            DisplayName = "The Outcast Host",
            KeepBuildingId = OutcastGreatCampId,
            ProducerBuildingId = OutcastBurrowsId,
            BasicUnitId = OutcastVillagerId,
            BuilderUnitId = OutcastBuilderId,
            RangedUnitId = OutcastRangerId,
            CavalryUnitId = OutcastBeastRiderId,
            EliteUnitId = OutcastGiantId,
            SiegeUnitId = OutcastGiantId,
            ScoutUnitId = OutcastHunterId,
            SapperUnitId = OutcastSnarerId,
            TowerBuildingId = OutcastTreetopWatchId,
            WallBuildingId = OutcastGroundWorksId,
            OutpostBuildingId = OutcastMineId,
            ExtraBuildingIds = new[] { OutcastAerieId, OutcastVillageHallId },
            SignatureBuildingId = OutcastAerieId,
            BasicUpgradeId = CloakedUpgradeId,
            KeepUpgradeIds = new[] { GreatPerchUpgradeId, RollingFogUpgradeId, StoneWallsUpgradeId },
            EquipmentUpgradeIds = new[]
            {
                CloakedUpgradeId,
                NaturesCamouflageUpgradeId,
                GiantHideUpgradeId,
                SaddleBindUpgradeId,
                SnareCordsUpgradeId,
                WindHarnessUpgradeId,
            },
            LeaderUnitId = OutcastHeirId,
            PowerId = NaturesAidAbilityId,
            PowerDisplayName = "Nature's Aid",
            PowerIds = new[]
            {
                WildBondPassiveId,
                NaturesAidAbilityId,
                DamBreakerAbilityId,
                EyesInSkyAbilityId,
                CampfireAbilityId,
                StampedeAbilityId,
                GreenTitheAbilityId,
            },
            LoreBlurb = "Free-spirit villages and beast-camps. The king's youngest child, denied the tomb-crown, walks with the wold.",
        };

        public static readonly FactionRoster Freetown = new FactionRoster
        {
            Id = new FactionId(3),
            DefinitionId = FreetownId,
            DisplayName = "Freetown",
            KeepBuildingId = FreetownTavernId,
            ProducerBuildingId = FreetownSmugglersDenId,
            BasicUnitId = FreetownDrunkId,
            BuilderUnitId = FreetownBuilderId,
            RangedUnitId = FreetownMudslingerId,
            CavalryUnitId = FreetownPrivateerId,
            EliteUnitId = FreetownBruteId,
            SiegeUnitId = FreetownFlamerId,
            ScoutUnitId = FreetownHighwaymanId,
            SapperUnitId = FreetownSapperId,
            TowerBuildingId = FreetownCrowsNestId,
            WallBuildingId = FreetownBarricadesId,
            OutpostBuildingId = FreetownBlackMarketId,
            ExtraBuildingIds = new[] { FreetownHutId },
            SignatureBuildingId = FreetownHutId,
            BasicUpgradeId = RageUpgradeId,
            KeepUpgradeIds = new[] { RareLootUpgradeId, StoneWallsUpgradeId },
            EquipmentUpgradeIds = new[]
            {
                GrappleHooksUpgradeId,
                RageUpgradeId,
                FirebrewUpgradeId,
                BlastFuseUpgradeId,
                CrabPlateUpgradeId,
                ShipNailsUpgradeId,
            },
            LeaderUnitId = FreetownBrewmasterId,
            PowerId = TradeSurplusAbilityId,
            PowerDisplayName = "Trade Surplus",
            PowerIds = new[]
            {
                PortCallPassiveId,
                TradeSurplusAbilityId,
                ExplosiveConvoyAbilityId,
                MercenariesAbilityId,
                SurprisedDeliveryAbilityId,
                SeaLegsAbilityId,
                RiotAbilityId,
                CrowStormAbilityId,
            },
            LoreBlurb = "Seaside mix of runaways and hired blades. Shipwrights, privateers, and fishermen with rocky and frozen island outposts in the south.",
        };

        public static readonly FactionRoster UniversityGuild = new FactionRoster
        {
            Id = new FactionId(4),
            DefinitionId = UniversityId,
            DisplayName = "University Guild",
            KeepBuildingId = UniversityCollegeId,
            ProducerBuildingId = UniversityWorkshopId,
            BasicUnitId = UniversityFellowId,
            BuilderUnitId = UniversityBuilderId,
            RangedUnitId = UniversityPoisonId,
            CavalryUnitId = UniversitySpiderId,
            EliteUnitId = UniversityAirshipId,
            SiegeUnitId = UniversityTrebuchetId,
            ScoutUnitId = UniversityAirshipId,
            SapperUnitId = UniversityEarthBreakerId,
            TowerBuildingId = UniversityClockworkTowerId,
            WallBuildingId = UniversityMoatId,
            OutpostBuildingId = UniversityObservatoryId,
            ExtraBuildingIds = new[] { UniversityLibraryId, UniversityAlchemistId, UniversityWeatherRodsId },
            SignatureBuildingId = UniversityWeatherRodsId,
            BasicUpgradeId = AdvancedCogsUpgradeId,
            KeepUpgradeIds = new[] { GreatSpyglassUpgradeId, AdvancedConstructionUpgradeId, StoneWallsUpgradeId },
            EquipmentUpgradeIds = new[]
            {
                AdvancedCogsUpgradeId,
                AlchemicalTipsUpgradeId,
                CounterweightUpgradeId,
            },
            LeaderUnitId = UniversityChancellorId,
            PowerId = ForecastAbilityId,
            PowerDisplayName = "Forecast",
            PowerIds = new[]
            {
                ForecastAbilityId,
                FarGlassAbilityId,
                OpenLectureAbilityId,
                FieldExerciseAbilityId,
                PrecisionDrillAbilityId,
                TenureAbilityId,
                PublishedPaperAbilityId,
                ClockworkMusterAbilityId,
            },
            LoreBlurb = "Study of ancients, climate, weapons, and literature. Workshops, libraries, and weather rods around the Grand College.",
        };

        public static readonly FactionRoster RisingSun = new FactionRoster
        {
            Id = new FactionId(5),
            DefinitionId = ChurchId,
            DisplayName = "Church of the Rising Sun",
            KeepBuildingId = ChurchGrandTempleId,
            ProducerBuildingId = ChurchMonasteryId,
            BasicUnitId = ChurchZealotId,
            BuilderUnitId = ChurchMasonId,
            RangedUnitId = ChurchPriestId,
            CavalryUnitId = ChurchRiderId,
            EliteUnitId = ChurchGuardId,
            SiegeUnitId = ChurchEngineId,
            ScoutUnitId = ChurchStalkerId,
            SapperUnitId = ChurchPurifierId,
            TowerBuildingId = ChurchScorchedTowerId,
            WallBuildingId = ChurchSacredWallsId,
            OutpostBuildingId = ChurchShrineId,
            ExtraBuildingIds = new[] { ChurchSunTempleId, ChurchSacredSiteId },
            SignatureBuildingId = ChurchSunTempleId,
            BasicUpgradeId = SolarVestmentsUpgradeId,
            KeepUpgradeIds = new[] { SacredMasonryUpgradeId, StoneWallsUpgradeId },
            EquipmentUpgradeIds = new[]
            {
                SolarVestmentsUpgradeId,
                ScorchedShotUpgradeId,
                StalkerVeilUpgradeId,
            },
            LeaderUnitId = ChurchHighPriestId,
            PowerId = SunRayAbilityId,
            PowerDisplayName = "Sun Ray",
            PowerIds = new[]
            {
                FalseChroniclePassiveId,
                SunRayAbilityId,
                DayOfTheSunAbilityId,
                BlindAbilityId,
                TitheAbilityId,
                SolarOathAbilityId,
                ProcessionAbilityId,
                PurgeTheDarkAbilityId,
            },
            LoreBlurb = "Light-cult that plays the savior. They purge non-humans and darkness, rewrite their age, and dread the next eclipse.",
        };

        public static FactionRoster[] All { get; } =
        {
            VeiledInheritance,
            MundorCrown,
            Outcast,
            Freetown,
            UniversityGuild,
            RisingSun,
        };

        public static FactionRoster Get(FactionId id)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id)
                    return All[i];
            }

            return VeiledInheritance;
        }

        public static bool IsBuilderUnitId(string definitionId)
        {
            return definitionId == VeiledBuilderId
                   || definitionId == RoyalBuilderId
                   || definitionId == OutcastBuilderId
                   || definitionId == FreetownBuilderId
                   || definitionId == UniversityBuilderId
                   || definitionId == ChurchMasonId
                   || (definitionId != null && definitionId.Contains("builder"));
        }

        public static bool IsKeepBuildingId(string definitionId)
        {
            return definitionId == ArcaneumId
                   || definitionId == RoyalCitadelId
                   || definitionId == OutcastGreatCampId
                   || definitionId == FreetownTavernId
                   || definitionId == UniversityCollegeId
                   || definitionId == ChurchGrandTempleId;
        }

        public static bool IsNonHumanUnitId(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return false;
            return definitionId.Contains("elemental")
                   || definitionId.Contains("golem")
                   || definitionId.Contains("shade")
                   || definitionId.Contains("sprite")
                   || definitionId.Contains("giant")
                   || definitionId.Contains("cub")
                   || definitionId.Contains("wold")
                   || definitionId.Contains("crab")
                   || definitionId.Contains("imp")
                   || definitionId.Contains("hound")
                   || definitionId.Contains("crow")
                   || definitionId.Contains("spider")
                   || definitionId.Contains("beast")
                   || definitionId.Contains("shadow")
                   || definitionId.Contains("souling")
                   || definitionId.Contains("wind_rider");
        }

        public static bool IsPriestUnitId(string definitionId)
        {
            return definitionId == VeiledApprenticeId
                   || definitionId == VeiledRuneCasterId
                   || definitionId == VeiledElementalId
                   || definitionId == VeiledPriestGuardId;
        }

        public static bool IsPortalGateId(string definitionId)
        {
            return definitionId == PortalGateId;
        }

        public static bool IsEarthworkBuildingId(string definitionId)
        {
            return definitionId == TrenchWorksId
                   || definitionId == BermWorksId
                   || definitionId == MoatWorksId
                   || definitionId == FillWorksId
                   || definitionId == ClearWorksId
                   || definitionId == BurnWorksId
                   || definitionId == QuarryWorksId
                   || definitionId == SpikesWorksId
                   || definitionId == DebrisWorksId;
        }

        public static string EarthworkBuildingId(TerrainWorkKind kind) => kind switch
        {
            TerrainWorkKind.DigTrench => TrenchWorksId,
            TerrainWorkKind.RaiseBerm => BermWorksId,
            TerrainWorkKind.DigMoat => MoatWorksId,
            TerrainWorkKind.FillTrench => FillWorksId,
            TerrainWorkKind.ClearForest => ClearWorksId,
            TerrainWorkKind.BurnBrush => BurnWorksId,
            TerrainWorkKind.QuarryRock => QuarryWorksId,
            TerrainWorkKind.PlaceSpikes => SpikesWorksId,
            TerrainWorkKind.ClearDebris => DebrisWorksId,
            TerrainWorkKind.FlattenHill => FillWorksId,
            _ => null,
        };

        public static bool TryGetEarthworkKind(string buildingId, out TerrainWorkKind kind)
        {
            switch (buildingId)
            {
                case TrenchWorksId:
                    kind = TerrainWorkKind.DigTrench;
                    return true;
                case BermWorksId:
                    kind = TerrainWorkKind.RaiseBerm;
                    return true;
                case MoatWorksId:
                    kind = TerrainWorkKind.DigMoat;
                    return true;
                case FillWorksId:
                    kind = TerrainWorkKind.FillTrench;
                    return true;
                case ClearWorksId:
                    kind = TerrainWorkKind.ClearForest;
                    return true;
                case BurnWorksId:
                    kind = TerrainWorkKind.BurnBrush;
                    return true;
                case QuarryWorksId:
                    kind = TerrainWorkKind.QuarryRock;
                    return true;
                case SpikesWorksId:
                    kind = TerrainWorkKind.PlaceSpikes;
                    return true;
                case DebrisWorksId:
                    kind = TerrainWorkKind.ClearDebris;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }

        public static void RegisterAll(DefinitionRegistry registry)
        {
            RegisterVeiledInheritance(registry);
            RegisterMundorCrown(registry);
            RegisterOutcast(registry);
            RegisterFreetown(registry);
            RegisterUniversity(registry);
            RegisterRisingSun(registry);

            // Shared fortifications / economy
            registry.Register(new BuildingDefData
            {
                Id = KeepTurretId,
                DisplayName = "Keep Turret",
                MaxHealth = 320f,
                GoldCost = 120,
                TimberCost = 0,
                BuildSeconds = 3.5f,
                FootprintX = 2.5f,
                FootprintZ = 2.5f,
                Kind = BuildingKind.Tower,
                Category = BuildingCategory.Tower,
                AttackDamage = 14f,
                AttackRange = 48f,
                AttackCooldown = 1.1f,
                SightRadius = 90f,
            });
            registry.Register(new UnitDefData
            {
                Id = RiverBoatId,
                DisplayName = "River Boat",
                MaxHealth = 220f,
                MoveSpeed = 6.5f,
                AttackDamage = 8f,
                AttackRange = 10f,
                AttackCooldown = 1.6f,
                GoldCost = 120,
                TrainSeconds = 8f,
                Role = UnitRole.Siege,
                SquadSize = 1,
                Armor = 2f,
                ProjectileSpeed = 28f,
                TraversalCapabilities = Asterra.Core.World.TraversalCapability.Water,
            });
            registry.Register(new UnitDefData
            {
                Id = PathfinderId,
                DisplayName = "Pathfinder",
                MaxHealth = 70f,
                MoveSpeed = 5.8f,
                AttackDamage = 9f,
                AttackRange = 2f,
                AttackCooldown = 1f,
                GoldCost = 70,
                TrainSeconds = 5f,
                Role = UnitRole.Infantry,
                SquadSize = 4, // scout party, not a full company
                SightRadius = 165f,
                TraversalCapabilities = Asterra.Core.World.TraversalCapability.Land
                    | Asterra.Core.World.TraversalCapability.Jump,
            });
            registry.Register(new UnitDefData
            {
                Id = SapperId,
                DisplayName = "Sapper",
                MaxHealth = 85f,
                MoveSpeed = 4.7f,
                AttackDamage = 11f,
                AttackRange = 1.6f,
                AttackCooldown = 1.05f,
                GoldCost = 85,
                TrainSeconds = 5.5f,
                Role = UnitRole.Infantry,
                SquadSize = 8,
                Armor = 2f,
                BuildingDamageMultiplier = 2.6f,
                SightRadius = 100f,
            });
            registry.Register(new BuildingDefData
            {
                Id = WatchtowerId,
                DisplayName = "Watchtower",
                MaxHealth = 450f,
                GoldCost = 160,
                TimberCost = 0,
                BuildSeconds = 5f,
                FootprintX = 5f,
                FootprintZ = 5f,
                Kind = BuildingKind.Tower,
                Category = BuildingCategory.Tower,
                AllowsGarrison = true,
                GarrisonCapacity = 2,
                AttackDamage = 14f,
                AttackRange = 22f,
                AttackCooldown = 1.4f,
                SightRadius = 150f,
            });
            registry.Register(new BuildingDefData
            {
                Id = PalisadeId,
                DisplayName = "Palisade",
                MaxHealth = 700f,
                GoldCost = 130,
                TimberCost = 0,
                BuildSeconds = 4f,
                FootprintX = 14f,
                FootprintZ = 4f,
                Kind = BuildingKind.Wall,
                Category = BuildingCategory.Wall,
                SnapToWallGrid = true,
                WallSegmentLength = 14f,
                SightRadius = 40f,
            });
            registry.Register(new BuildingDefData
            {
                Id = OutpostId,
                DisplayName = "Gold Mine",
                MaxHealth = 420f,
                GoldCost = 140,
                TimberCost = 0,
                BuildSeconds = 5.5f,
                FootprintX = 6f,
                FootprintZ = 6f,
                Kind = BuildingKind.Outpost,
                Category = BuildingCategory.Resource,
                SightRadius = 130f,
                GoldPerSecond = 8,
            });
            registry.Register(new BuildingDefData
            {
                Id = BridgeId,
                DisplayName = "Timber Bridge",
                MaxHealth = 380f,
                GoldCost = 220,
                TimberCost = 0,
                BuildSeconds = 8f,
                FootprintX = 12f,
                FootprintZ = 36f,
                Kind = BuildingKind.Special,
                Category = BuildingCategory.Wall,
                SightRadius = 50f,
            });
            registry.Register(new BuildingDefData
            {
                Id = TrenchWorksId,
                DisplayName = "Trench Works",
                MaxHealth = 80f,
                GoldCost = 65,
                TimberCost = 0,
                BuildSeconds = 9f,
                FootprintX = 14f,
                FootprintZ = 10f,
                Kind = BuildingKind.Special,
                Category = BuildingCategory.Wall,
                SightRadius = 20f,
            });
            RegisterEarthworkSite(registry, BermWorksId, "Berm Works", gold: 68, timber: 0, seconds: 10f, fx: 14f, fz: 10f);
            RegisterEarthworkSite(registry, MoatWorksId, "Moat Works", gold: 77, timber: 0, seconds: 12f, fx: 14f, fz: 10f);
            RegisterEarthworkSite(registry, FillWorksId, "Fill Works", gold: 42, timber: 0, seconds: 7f, fx: 12f, fz: 12f);
            RegisterEarthworkSite(registry, ClearWorksId, "Clear Works", gold: 25, timber: 0, seconds: 8f, fx: 14f, fz: 14f);
            RegisterEarthworkSite(registry, BurnWorksId, "Burn Works", gold: 35, timber: 0, seconds: 8f, fx: 14f, fz: 14f);
            RegisterEarthworkSite(registry, QuarryWorksId, "Quarry Works", gold: 30, timber: 0, seconds: 9f, fx: 12f, fz: 12f);
            RegisterEarthworkSite(registry, SpikesWorksId, "Spike Works", gold: 85, timber: 0, seconds: 8f, fx: 10f, fz: 10f);
            RegisterEarthworkSite(registry, DebrisWorksId, "Clear Debris", gold: 15, timber: 0, seconds: 6f, fx: 12f, fz: 12f);
            registry.Register(new BuildingDefData
            {
                Id = BarricadeId,
                DisplayName = "Barricade",
                MaxHealth = 220f,
                GoldCost = 85,
                TimberCost = 0,
                BuildSeconds = 3f,
                FootprintX = 10f,
                FootprintZ = 4f,
                Kind = BuildingKind.Wall,
                Category = BuildingCategory.Wall,
                SnapToWallGrid = true,
                WallSegmentLength = 10f,
                SightRadius = 30f,
            });
            registry.Register(new BuildingDefData
            {
                Id = FerryDockId,
                DisplayName = "Ferry Dock",
                MaxHealth = 280f,
                GoldCost = 200,
                TimberCost = 0,
                BuildSeconds = 7f,
                FootprintX = 8f,
                FootprintZ = 8f,
                Kind = BuildingKind.Special,
                Category = BuildingCategory.Resource,
                SightRadius = 70f,
            });
            registry.Register(new BuildingDefData
            {
                Id = StoneWallId,
                DisplayName = "Stone Wall",
                MaxHealth = 1400f,
                GoldCost = 130,
                TimberCost = 0,
                BuildSeconds = 6f,
                FootprintX = 14f,
                FootprintZ = 4.5f,
                Kind = BuildingKind.Wall,
                Category = BuildingCategory.Wall,
                SnapToWallGrid = true,
                WallSegmentLength = 14f,
                SightRadius = 45f,
            });
            registry.Register(new UpgradeDefData
            {
                Id = StoneWallsUpgradeId,
                DisplayName = "Stone Masonry",
                GoldCost = 220,
                ResearchSeconds = 14f,
                Kind = UpgradeKind.Fortification,
            });
        }

        private static void RegisterVeiledInheritance(DefinitionRegistry registry)
        {
            registry.Register(new UnitDefData
            {
                Id = VeiledApprenticeId,
                DisplayName = "Apprentice",
                MaxHealth = 88f,
                MoveSpeed = 5.2f,
                AttackDamage = 11f,
                AttackRange = 1.8f,
                AttackCooldown = 1f,
                GoldCost = 48,
                TrainSeconds = 3.8f,
                Role = UnitRole.Infantry,
                SquadSize = 16,
                Armor = 0.5f,
            });
            registry.Register(new UnitDefData
            {
                Id = VeiledBuilderId,
                DisplayName = "Arcane Engineer",
                MaxHealth = 58f,
                MoveSpeed = 4.7f,
                AttackDamage = 0f,
                AttackRange = 0f,
                AttackCooldown = 1f,
                GoldCost = 30,
                TrainSeconds = 2.8f,
                IsBuilder = true,
                CanGather = false,
                CarryCapacity = 14,
                GatherRate = 5.5f,
                Role = UnitRole.Builder,
                SquadSize = 1,
            });
            registry.Register(new UnitDefData
            {
                Id = VeiledRuneCasterId,
                DisplayName = "Rune Caster",
                MaxHealth = 58f,
                MoveSpeed = 4.7f,
                AttackDamage = 16f,
                AttackRange = 13.5f,
                AttackCooldown = 1.25f,
                GoldCost = 70,
                TrainSeconds = 5f,
                Role = UnitRole.Ranged,
                SquadSize = 12,
                ProjectileSpeed = 46f,
            });
            registry.Register(new UnitDefData
            {
                Id = VeiledElementalId,
                DisplayName = "Elemental Wielder",
                MaxHealth = 78f,
                MoveSpeed = 4.5f,
                AttackDamage = 20f,
                AttackRange = 11f,
                AttackCooldown = 1.4f,
                GoldCost = 125,
                TrainSeconds = 7f,
                Role = UnitRole.Ranged,
                SquadSize = 10,
                Armor = 1f,
                ProjectileSpeed = 38f,
            });
            registry.Register(new UnitDefData
            {
                Id = VeiledGolemId,
                DisplayName = "Golem",
                MaxHealth = 165f,
                MoveSpeed = 2.5f,
                AttackDamage = 24f,
                AttackRange = 7.5f,
                AttackCooldown = 2.4f,
                GoldCost = 155,
                TrainSeconds = 10.5f,
                Role = UnitRole.Siege,
                SquadSize = 1,
                BuildingDamageMultiplier = 3.4f,
                Armor = 4f,
                ProjectileSpeed = 26f,
            });
            registry.Register(new UnitDefData
            {
                Id = VeiledPriestGuardId,
                DisplayName = "High Priest Guard",
                MaxHealth = 145f,
                MoveSpeed = 3.7f,
                AttackDamage = 14f,
                AttackRange = 1.6f,
                AttackCooldown = 1.1f,
                GoldCost = 115,
                TrainSeconds = 7f,
                Role = UnitRole.Infantry,
                SquadSize = 16,
                Armor = 3.5f,
            });
            registry.Register(new UnitDefData
            {
                Id = VeiledShadowId,
                DisplayName = "Shadow Form",
                MaxHealth = 92f,
                MoveSpeed = 8.6f,
                AttackDamage = 13f,
                AttackRange = 1.7f,
                AttackCooldown = 0.9f,
                GoldCost = 105,
                TrainSeconds = 5.8f,
                Role = UnitRole.Cavalry,
                SquadSize = 6,
                Armor = 1f,
            });
            registry.Register(new UnitDefData
            {
                Id = VeiledAssassinId,
                DisplayName = "Cloaked Assassin",
                MaxHealth = 55f,
                MoveSpeed = 6.5f,
                AttackDamage = 12f,
                AttackRange = 1.6f,
                AttackCooldown = 0.85f,
                GoldCost = 75,
                TrainSeconds = 5.2f,
                Role = UnitRole.Infantry,
                SquadSize = 4,
                SightRadius = 175f,
            });
            registry.Register(new UnitDefData
            {
                Id = VeiledMassedId,
                DisplayName = "Massed Form",
                MaxHealth = 68f,
                MoveSpeed = 5.5f,
                AttackDamage = 8f,
                AttackRange = 1.7f,
                AttackCooldown = 0.85f,
                GoldCost = 40,
                TrainSeconds = 3.2f,
                Role = UnitRole.Infantry,
                SquadSize = 20,
            });
            registry.Register(new UnitDefData
            {
                Id = VeiledSoulingId,
                DisplayName = "Souling",
                MaxHealth = 48f,
                MoveSpeed = 5.6f,
                AttackDamage = 8f,
                AttackRange = 1.5f,
                AttackCooldown = 1f,
                GoldCost = 80,
                TrainSeconds = 5.5f,
                Role = UnitRole.Infantry,
                SquadSize = 8,
                BuildingDamageMultiplier = 4.4f,
            });
            registry.Register(new UnitDefData
            {
                Id = VeiledDarkSpyId,
                DisplayName = "Dark Spy",
                MaxHealth = 22f,
                MoveSpeed = 7.2f,
                AttackDamage = 3f,
                AttackRange = 1.4f,
                AttackCooldown = 1.2f,
                GoldCost = 0,
                TrainSeconds = 0.5f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
                SightRadius = 210f,
            });
            registry.Register(new UnitDefData
            {
                Id = VeiledShadeId,
                DisplayName = "Shade",
                MaxHealth = 18f,
                MoveSpeed = 5.8f,
                AttackDamage = 4f,
                AttackRange = 1.4f,
                AttackCooldown = 1.1f,
                GoldCost = 0,
                TrainSeconds = 0.4f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
            });
            registry.Register(new UnitDefData
            {
                Id = VeiledHeirId,
                DisplayName = "The Veiled Heir",
                MaxHealth = 250f,
                MoveSpeed = 5.3f,
                AttackDamage = 20f,
                AttackRange = 8f,
                AttackCooldown = 1.05f,
                GoldCost = 250,
                TrainSeconds = 14f,
                Role = UnitRole.Ranged,
                SquadSize = 1,
                Armor = 2.5f,
                IsLeader = true,
                SightRadius = 145f,
                ProjectileSpeed = 44f,
            });
            registry.Register(new UnitDefData
            {
                Id = VeiledColossusLeaderId,
                DisplayName = "Masterwork Colossus",
                MaxHealth = 340f,
                MoveSpeed = 3.4f,
                AttackDamage = 28f,
                AttackRange = 2.4f,
                AttackCooldown = 1.2f,
                GoldCost = 280,
                TrainSeconds = 16f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
                Armor = 6f,
                IsLeader = true,
                SightRadius = 120f,
            });
            registry.Register(new UnitDefData
            {
                Id = VeiledThornSpeakerId,
                DisplayName = "Thorn-Speaker Veyra",
                MaxHealth = 230f,
                MoveSpeed = 6.2f,
                AttackDamage = 18f,
                AttackRange = 2.2f,
                AttackCooldown = 0.85f,
                GoldCost = 245,
                TrainSeconds = 13.5f,
                Role = UnitRole.Cavalry,
                SquadSize = 1,
                Armor = 2f,
                IsLeader = true,
                SightRadius = 150f,
            });
            registry.Register(new UnitDefData
            {
                Id = VeiledNightAbbotId,
                DisplayName = "Night-Abbot Cael",
                MaxHealth = 255f,
                MoveSpeed = 4.8f,
                AttackDamage = 22f,
                AttackRange = 9.5f,
                AttackCooldown = 1.15f,
                GoldCost = 255,
                TrainSeconds = 14.5f,
                Role = UnitRole.Ranged,
                SquadSize = 1,
                Armor = 2f,
                IsLeader = true,
                SightRadius = 140f,
                ProjectileSpeed = 40f,
            });
            registry.Register(new UnitDefData
            {
                Id = VeiledFirstHereticId,
                DisplayName = "The First Heretic",
                MaxHealth = 240f,
                MoveSpeed = 5f,
                AttackDamage = 19f,
                AttackRange = 2f,
                AttackCooldown = 0.9f,
                GoldCost = 250,
                TrainSeconds = 14f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
                Armor = 3f,
                IsLeader = true,
                SightRadius = 155f,
            });

            registry.Register(new BuildingDefData
            {
                Id = ArcaneAcademyId,
                DisplayName = "Arcane Academy",
                MaxHealth = 580f,
                GoldCost = 200,
                TimberCost = 0,
                BuildSeconds = 6.2f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                Category = BuildingCategory.Troop,
                SightRadius = 85f,
                FootprintX = 12f,
                FootprintZ = 12f,
                TrainableUnitIds = new[]
                {
                    VeiledApprenticeId,
                    VeiledRuneCasterId,
                    VeiledShadowId,
                    VeiledPriestGuardId,
                    VeiledGolemId,
                    VeiledAssassinId,
                    VeiledSoulingId,
                },
            });
            registry.Register(new BuildingDefData
            {
                Id = AncientRuinsId,
                DisplayName = "Ancient Ruins",
                MaxHealth = 640f,
                GoldCost = 240,
                TimberCost = 0,
                BuildSeconds = 7f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                Category = BuildingCategory.Troop,
                SightRadius = 80f,
                FootprintX = 14f,
                FootprintZ = 14f,
                TrainableUnitIds = new[] { VeiledGolemId, VeiledSoulingId, VeiledMassedId },
            });
            registry.Register(new BuildingDefData
            {
                Id = ConjuringHallId,
                DisplayName = "Conjuring Hall",
                MaxHealth = 560f,
                GoldCost = 215,
                TimberCost = 0,
                BuildSeconds = 6.5f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                Category = BuildingCategory.Troop,
                SightRadius = 80f,
                FootprintX = 12f,
                FootprintZ = 12f,
                TrainableUnitIds = new[] { VeiledShadowId, VeiledAssassinId, VeiledMassedId },
            });
            registry.Register(new BuildingDefData
            {
                Id = HighTempleId,
                DisplayName = "High Temple",
                MaxHealth = 700f,
                GoldCost = 230,
                TimberCost = 0,
                BuildSeconds = 7.5f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                Category = BuildingCategory.Troop,
                SightRadius = 90f,
                FootprintX = 13f,
                FootprintZ = 13f,
                TrainableUnitIds = new[] { VeiledPriestGuardId, VeiledElementalId, VeiledRuneCasterId, VeiledApprenticeId },
            });
            registry.Register(new BuildingDefData
            {
                Id = BlackrootConservatoryId,
                DisplayName = "Blackroot Conservatory",
                MaxHealth = 450f,
                GoldCost = 190,
                TimberCost = 0,
                BuildSeconds = 6f,
                FootprintX = 8f,
                FootprintZ = 8f,
                Kind = BuildingKind.Outpost,
                Category = BuildingCategory.Resource,
                SightRadius = 100f,
                GoldPerSecond = 10,
            });
            registry.Register(new BuildingDefData
            {
                Id = PortalGateId,
                DisplayName = "Portal Gate",
                MaxHealth = 380f,
                GoldCost = 220,
                TimberCost = 0,
                BuildSeconds = 8f,
                FootprintX = 8f,
                FootprintZ = 8f,
                Kind = BuildingKind.Special,
                Category = BuildingCategory.Special,
                SightRadius = 70f,
                CommandRadius = 40f,
            });
            registry.Register(new BuildingDefData
            {
                Id = ShadowedGateBuildingId,
                DisplayName = "Shadowed Gate",
                MaxHealth = 260f,
                GoldCost = 0,
                TimberCost = 0,
                BuildSeconds = 0.5f,
                FootprintX = 6f,
                FootprintZ = 6f,
                Kind = BuildingKind.Special,
                Category = BuildingCategory.Special,
                SightRadius = 55f,
            });
            registry.Register(new BuildingDefData
            {
                Id = ArcaneumId,
                DisplayName = "Arcaneum",
                MaxHealth = 1150f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Keep,
                GoldPerSecond = 2,
                SightRadius = 160f,
                FootprintX = 18f,
                FootprintZ = 18f,
                TrainableUnitIds = new[]
                {
                    VeiledBuilderId,
                    VeiledHeirId,
                    VeiledColossusLeaderId,
                    VeiledThornSpeakerId,
                    VeiledNightAbbotId,
                    VeiledFirstHereticId,
                },
                AttachmentSlotCount = 4,
                AttachmentAllowedBuildingIds = new[] { KeepTurretId },
                AttachmentRadius = 14f,
            });

            registry.Register(new UpgradeDefData
            {
                Id = ForbiddenCurriculumId,
                DisplayName = "Forbidden Curriculum",
                GoldCost = 155,
                TrainTimeMultiplier = 0.9f,
                Kind = UpgradeKind.Keep,
                ResearchSeconds = 10f,
            });
            registry.Register(new UpgradeDefData
            {
                Id = VeiledBastionId,
                DisplayName = "Veiled Bastion",
                GoldCost = 190,
                KeepHealthBonus = 210f,
                Kind = UpgradeKind.Keep,
                ResearchSeconds = 11f,
            });
            registry.Register(new UpgradeDefData
            {
                Id = VeiledMailId,
                DisplayName = "Veiled Mail",
                GoldCost = 155,
                EquipGoldCost = 38,
                ArmorBonus = 3f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 10f,
            });
            registry.Register(new UpgradeDefData
            {
                Id = DesertStormUpgradeId,
                DisplayName = "Desert Storm",
                GoldCost = 170,
                Kind = UpgradeKind.Keep,
                ResearchSeconds = 11f,
            });
            registry.Register(new UpgradeDefData
            {
                Id = RainfallUpgradeId,
                DisplayName = "Rainfall",
                GoldCost = 160,
                Kind = UpgradeKind.Keep,
                ResearchSeconds = 10f,
            });
            registry.Register(new UpgradeDefData
            {
                Id = FogOfWarUpgradeId,
                DisplayName = "Fog of War",
                GoldCost = 165,
                Kind = UpgradeKind.Keep,
                ResearchSeconds = 11f,
            });
            registry.Register(new UpgradeDefData
            {
                Id = IceFormationUpgradeId,
                DisplayName = "Ice Formation",
                GoldCost = 175,
                Kind = UpgradeKind.Keep,
                ResearchSeconds = 12f,
            });
            registry.Register(new UpgradeDefData
            {
                Id = BoundCoreUpgradeId,
                DisplayName = "Bound Core",
                GoldCost = 180,
                EquipGoldCost = 50,
                ArmorBonus = 4f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 12f,
                CompatibleUnitIds = new[] { VeiledGolemId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = NightStrideUpgradeId,
                DisplayName = "Night Stride",
                GoldCost = 165,
                EquipGoldCost = 42,
                AttackDamageBonus = 4f,
                UnitDamageMultiplier = 1.1f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 11f,
                CompatibleUnitIds = new[] { VeiledShadowId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = SilentVenomUpgradeId,
                DisplayName = "Silent Venom",
                GoldCost = 150,
                EquipGoldCost = 38,
                AttackDamageBonus = 6f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 10f,
                CompatibleUnitIds = new[] { VeiledAssassinId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = SwarmHymnUpgradeId,
                DisplayName = "Swarm Hymn",
                GoldCost = 130,
                EquipGoldCost = 28,
                ArmorBonus = 2f,
                UnitDamageMultiplier = 1.12f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 9f,
                CompatibleUnitIds = new[] { VeiledMassedId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = UnquietHungerUpgradeId,
                DisplayName = "Unquiet Hunger",
                GoldCost = 155,
                EquipGoldCost = 40,
                AttackDamageBonus = 3f,
                UnitDamageMultiplier = 1.15f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 10f,
                CompatibleUnitIds = new[] { VeiledSoulingId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = ArtificerPlatingUpgradeId,
                DisplayName = "Artificer Plating",
                GoldCost = 120,
                EquipGoldCost = 30,
                ArmorBonus = 4f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 8f,
                CompatibleRoleMask = UpgradeDefData.RoleMask(UnitRole.Builder),
            });

            registry.Register(new PowerDefData
            {
                Id = ForbiddenGiftPassiveId,
                DisplayName = "Forbidden Gift",
                UnlockGoldCost = 80,
                IsPassive = true,
                Effect = PowerEffectKind.DamageAura,
                EffectMagnitude = 1.2f,
            });
            registry.Register(new PowerDefData
            {
                Id = WrathOfSkiesAbilityId,
                DisplayName = "Wrath of Skies",
                UnlockGoldCost = 200,
                CooldownSeconds = 55f,
                DurationSeconds = 18f,
                Effect = PowerEffectKind.ForceWeather,
                EffectMagnitude = 1f,
                HeroMoment = true,
            });
            registry.Register(new PowerDefData
            {
                Id = ShadowedGateAbilityId,
                DisplayName = "Shadowed Gate",
                UnlockGoldCost = 220,
                CooldownSeconds = 70f,
                DurationSeconds = 0f,
                Effect = PowerEffectKind.SpawnSwarm,
                EffectMagnitude = 1f,
            });
            registry.Register(new PowerDefData
            {
                Id = TwinGatesAbilityId,
                DisplayName = "Twin Gates",
                UnlockGoldCost = 180,
                CooldownSeconds = 60f,
                DurationSeconds = 0f,
                Effect = PowerEffectKind.PlaceGate,
                EffectMagnitude = 1f,
            });
            registry.Register(new PowerDefData
            {
                Id = MagicalAbundanceAbilityId,
                DisplayName = "Magical Abundance",
                UnlockGoldCost = 160,
                CooldownSeconds = 50f,
                DurationSeconds = 20f,
                Effect = PowerEffectKind.EconomyBoost,
                EffectMagnitude = 12f,
            });
            registry.Register(new PowerDefData
            {
                Id = DarkSpiesAbilityId,
                DisplayName = "Dark Spies",
                UnlockGoldCost = 140,
                CooldownSeconds = 48f,
                DurationSeconds = 0f,
                Effect = PowerEffectKind.SpawnScouts,
                EffectMagnitude = 6f,
            });
            registry.Register(new PowerDefData
            {
                Id = ThrallbindAbilityId,
                DisplayName = "Thrallbind",
                UnlockGoldCost = 150,
                CooldownSeconds = 45f,
                DurationSeconds = 30f,
                Effect = PowerEffectKind.MindControl,
                EffectMagnitude = 1f,
            });
            registry.Register(new PowerDefData
            {
                Id = BeastChorusAbilityId,
                DisplayName = "Beast Chorus",
                UnlockGoldCost = 120,
                CooldownSeconds = 36f,
                DurationSeconds = 9f,
                Effect = PowerEffectKind.MoveSpeedAura,
                EffectMagnitude = 2.4f,
            });
        }

        private static void RegisterMundorCrown(DefinitionRegistry registry)
        {
            registry.Register(new UnitDefData
            {
                Id = RoyalPeasantId,
                DisplayName = "Peasant",
                MaxHealth = 78f,
                MoveSpeed = 5.1f,
                AttackDamage = 9f,
                AttackRange = 1.7f,
                AttackCooldown = 1.05f,
                GoldCost = 38,
                TrainSeconds = 3.2f,
                Role = UnitRole.Infantry,
                SquadSize = 16,
            });
            registry.Register(new UnitDefData
            {
                Id = RoyalBuilderId,
                DisplayName = "Royal Engineer",
                MaxHealth = 62f,
                MoveSpeed = 4.8f,
                AttackDamage = 0f,
                AttackRange = 0f,
                AttackCooldown = 1f,
                GoldCost = 32,
                TrainSeconds = 2.8f,
                IsBuilder = true,
                CanGather = false,
                CarryCapacity = 15,
                GatherRate = 5.6f,
                Role = UnitRole.Builder,
                SquadSize = 1,
            });
            registry.Register(new UnitDefData
            {
                Id = RoyalLegionId,
                DisplayName = "Legion",
                MaxHealth = 108f,
                MoveSpeed = 4.9f,
                AttackDamage = 13f,
                AttackRange = 1.8f,
                AttackCooldown = 1f,
                GoldCost = 58,
                TrainSeconds = 4.4f,
                Role = UnitRole.Infantry,
                SquadSize = 16,
                Armor = 1.5f,
            });
            registry.Register(new UnitDefData
            {
                Id = RoyalGuardId,
                DisplayName = "Royal Guard",
                MaxHealth = 158f,
                MoveSpeed = 4.2f,
                AttackDamage = 16f,
                AttackRange = 1.7f,
                AttackCooldown = 1.05f,
                GoldCost = 128,
                TrainSeconds = 7.2f,
                Role = UnitRole.Infantry,
                SquadSize = 16,
                Armor = 4.5f,
            });
            registry.Register(new UnitDefData
            {
                Id = RoyalLongbowId,
                DisplayName = "Longbowman",
                MaxHealth = 64f,
                MoveSpeed = 4.8f,
                AttackDamage = 15f,
                AttackRange = 16f,
                AttackCooldown = 1.35f,
                GoldCost = 68,
                TrainSeconds = 5.2f,
                Role = UnitRole.Ranged,
                SquadSize = 12,
                ProjectileSpeed = 52f,
            });
            registry.Register(new UnitDefData
            {
                Id = RoyalCommanderId,
                DisplayName = "Commander",
                MaxHealth = 118f,
                MoveSpeed = 7.4f,
                AttackDamage = 15f,
                AttackRange = 1.9f,
                AttackCooldown = 0.95f,
                GoldCost = 112,
                TrainSeconds = 6.2f,
                Role = UnitRole.Cavalry,
                SquadSize = 4,
                Armor = 3f,
            });
            registry.Register(new UnitDefData
            {
                Id = RoyalSpyId,
                DisplayName = "Spy",
                MaxHealth = 48f,
                MoveSpeed = 6.6f,
                AttackDamage = 8f,
                AttackRange = 1.5f,
                AttackCooldown = 0.9f,
                GoldCost = 78,
                TrainSeconds = 5f,
                Role = UnitRole.Infantry,
                SquadSize = 2,
                SightRadius = 190f,
            });
            registry.Register(new UnitDefData
            {
                Id = RoyalCrownEyeId,
                DisplayName = "Crown Eye",
                MaxHealth = 20f,
                MoveSpeed = 7.4f,
                AttackDamage = 3f,
                AttackRange = 1.4f,
                AttackCooldown = 1.2f,
                GoldCost = 0,
                TrainSeconds = 0.4f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
                SightRadius = 205f,
            });
            registry.Register(new UnitDefData
            {
                Id = RoyalPioneerId,
                DisplayName = "Pioneer",
                MaxHealth = 82f,
                MoveSpeed = 4.8f,
                AttackDamage = 11f,
                AttackRange = 1.6f,
                AttackCooldown = 1.05f,
                GoldCost = 85,
                TrainSeconds = 5.5f,
                Role = UnitRole.Infantry,
                SquadSize = 8,
                Armor = 2f,
                BuildingDamageMultiplier = 2.6f,
                SightRadius = 100f,
            });
            registry.Register(new UnitDefData
            {
                Id = RoyalOnagerId,
                DisplayName = "Onager",
                MaxHealth = 95f,
                MoveSpeed = 2.7f,
                AttackDamage = 23f,
                AttackRange = 10.5f,
                AttackCooldown = 2.3f,
                GoldCost = 145,
                TrainSeconds = 9.5f,
                Role = UnitRole.Siege,
                SquadSize = 1,
                BuildingDamageMultiplier = 3.4f,
                Armor = 1f,
                ProjectileSpeed = 30f,
            });
            registry.Register(new UnitDefData
            {
                Id = RoyalKingId,
                DisplayName = "King of Mundor",
                MaxHealth = 270f,
                MoveSpeed = 5.1f,
                AttackDamage = 22f,
                AttackRange = 2.1f,
                AttackCooldown = 0.9f,
                GoldCost = 260,
                TrainSeconds = 14.5f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
                Armor = 4.5f,
                IsLeader = true,
                SightRadius = 145f,
            });
            registry.Register(new UnitDefData
            {
                Id = RoyalLegionMarshalId,
                DisplayName = "Legion Commander",
                MaxHealth = 245f,
                MoveSpeed = 5.4f,
                AttackDamage = 21f,
                AttackRange = 2f,
                AttackCooldown = 0.88f,
                GoldCost = 250,
                TrainSeconds = 13.5f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
                Armor = 3.5f,
                IsLeader = true,
                SightRadius = 140f,
            });
            registry.Register(new UnitDefData
            {
                Id = RoyalSpymasterId,
                DisplayName = "Spy Master",
                MaxHealth = 210f,
                MoveSpeed = 6.4f,
                AttackDamage = 16f,
                AttackRange = 1.8f,
                AttackCooldown = 0.8f,
                GoldCost = 245,
                TrainSeconds = 13f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
                Armor = 2f,
                IsLeader = true,
                SightRadius = 200f,
            });
            registry.Register(new UnitDefData
            {
                Id = RoyalTombWardenId,
                DisplayName = "Tomb Warden",
                MaxHealth = 300f,
                MoveSpeed = 3.8f,
                AttackDamage = 24f,
                AttackRange = 2.2f,
                AttackCooldown = 1.15f,
                GoldCost = 270,
                TrainSeconds = 15.5f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
                Armor = 6f,
                IsLeader = true,
                SightRadius = 120f,
            });
            registry.Register(new UnitDefData
            {
                Id = RoyalJusticiarId,
                DisplayName = "Royal Justiciar",
                MaxHealth = 235f,
                MoveSpeed = 5f,
                AttackDamage = 19f,
                AttackRange = 2f,
                AttackCooldown = 0.92f,
                GoldCost = 250,
                TrainSeconds = 14f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
                Armor = 3.5f,
                IsLeader = true,
                SightRadius = 150f,
            });

            registry.Register(new BuildingDefData
            {
                Id = RoyalBarracksId,
                DisplayName = "Barracks",
                MaxHealth = 620f,
                GoldCost = 200,
                TimberCost = 0,
                BuildSeconds = 6f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                Category = BuildingCategory.Troop,
                SightRadius = 85f,
                FootprintX = 12f,
                FootprintZ = 12f,
                TrainableUnitIds = new[]
                {
                    RoyalPeasantId,
                    RoyalLegionId,
                    RoyalLongbowId,
                    RoyalCommanderId,
                    RoyalGuardId,
                    RoyalOnagerId,
                    RoyalSpyId,
                    RoyalPioneerId,
                },
            });
            registry.Register(new BuildingDefData
            {
                Id = RoyalCourtId,
                DisplayName = "Royal Court",
                MaxHealth = 640f,
                GoldCost = 230,
                TimberCost = 0,
                BuildSeconds = 6.5f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                Category = BuildingCategory.Troop,
                SightRadius = 90f,
                FootprintX = 13f,
                FootprintZ = 13f,
                TrainableUnitIds = new[]
                {
                    RoyalPeasantId,
                    RoyalLegionId,
                    RoyalGuardId,
                    RoyalCommanderId,
                    RoyalSpyId,
                },
            });
            registry.Register(new BuildingDefData
            {
                Id = RoyalFarmId,
                DisplayName = "Farm",
                MaxHealth = 430f,
                GoldCost = 155,
                TimberCost = 0,
                BuildSeconds = 5.5f,
                FootprintX = 8f,
                FootprintZ = 8f,
                Kind = BuildingKind.Outpost,
                Category = BuildingCategory.Resource,
                SightRadius = 95f,
                GoldPerSecond = 9,
            });
            registry.Register(new BuildingDefData
            {
                Id = RoyalOutpostTowerId,
                DisplayName = "Royal Outpost",
                MaxHealth = 480f,
                GoldCost = 175,
                TimberCost = 0,
                BuildSeconds = 5.2f,
                FootprintX = 5f,
                FootprintZ = 5f,
                Kind = BuildingKind.Tower,
                Category = BuildingCategory.Tower,
                AllowsGarrison = true,
                GarrisonCapacity = 2,
                AttackDamage = 15f,
                AttackRange = 24f,
                AttackCooldown = 1.35f,
                SightRadius = 155f,
            });
            registry.Register(new BuildingDefData
            {
                Id = RoyalWallId,
                DisplayName = "Royal Walls",
                MaxHealth = 860f,
                GoldCost = 150,
                TimberCost = 0,
                BuildSeconds = 4.5f,
                FootprintX = 14f,
                FootprintZ = 4.2f,
                Kind = BuildingKind.Wall,
                Category = BuildingCategory.Wall,
                SnapToWallGrid = true,
                WallSegmentLength = 14f,
                SightRadius = 42f,
            });
            registry.Register(new BuildingDefData
            {
                Id = RoyalCitadelId,
                DisplayName = "Citadel",
                MaxHealth = 1280f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Keep,
                GoldPerSecond = 2,
                SightRadius = 165f,
                FootprintX = 18f,
                FootprintZ = 18f,
                TrainableUnitIds = new[]
                {
                    RoyalBuilderId,
                    RoyalKingId,
                    RoyalLegionMarshalId,
                    RoyalSpymasterId,
                    RoyalTombWardenId,
                    RoyalJusticiarId,
                },
                AttachmentSlotCount = 4,
                AttachmentAllowedBuildingIds = new[] { KeepTurretId },
                AttachmentRadius = 14f,
            });

            registry.Register(new UpgradeDefData
            {
                Id = MusterRollsId,
                DisplayName = "Muster Rolls",
                GoldCost = 155,
                TrainTimeMultiplier = 0.9f,
                Kind = UpgradeKind.Keep,
                ResearchSeconds = 10f,
            });
            registry.Register(new UpgradeDefData
            {
                Id = TombRightsId,
                DisplayName = "Tomb Rights",
                GoldCost = 195,
                KeepHealthBonus = 230f,
                Kind = UpgradeKind.Keep,
                ResearchSeconds = 11f,
            });
            registry.Register(new UpgradeDefData
            {
                Id = MundorArmourId,
                DisplayName = "Mundor Armour",
                GoldCost = 165,
                EquipGoldCost = 40,
                ArmorBonus = 3.5f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 10f,
            });
            registry.Register(new UpgradeDefData
            {
                Id = FineSteelId,
                DisplayName = "Fine Steel",
                GoldCost = 175,
                EquipGoldCost = 42,
                AttackDamageBonus = 4f,
                UnitDamageMultiplier = 1.08f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 11f,
                CompatibleRoleMask = UpgradeDefData.RoleMask(UnitRole.Infantry, UnitRole.Cavalry),
            });
            registry.Register(new UpgradeDefData
            {
                Id = MasterTrainingId,
                DisplayName = "Master Training",
                GoldCost = 125,
                EquipGoldCost = 32,
                ArmorBonus = 3f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 8f,
                CompatibleRoleMask = UpgradeDefData.RoleMask(UnitRole.Builder),
            });
            registry.Register(new UpgradeDefData
            {
                Id = YewNocksId,
                DisplayName = "Yew Nocks",
                GoldCost = 150,
                EquipGoldCost = 36,
                AttackDamageBonus = 4f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 10f,
                CompatibleUnitIds = new[] { RoyalLongbowId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = WhisperCloakId,
                DisplayName = "Whisper Cloak",
                GoldCost = 140,
                EquipGoldCost = 34,
                SightBonus = 25f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 9f,
                CompatibleUnitIds = new[] { RoyalSpyId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = TombStonesId,
                DisplayName = "Tomb Stones",
                GoldCost = 160,
                EquipGoldCost = 40,
                AttackDamageBonus = 4f,
                UnitDamageMultiplier = 1.2f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 10f,
                CompatibleUnitIds = new[] { RoyalOnagerId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = PioneerMaulId,
                DisplayName = "Pioneer Maul",
                GoldCost = 135,
                EquipGoldCost = 32,
                AttackDamageBonus = 3f,
                UnitDamageMultiplier = 1.15f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 9f,
                CompatibleUnitIds = new[] { RoyalPioneerId },
            });

            registry.Register(new PowerDefData
            {
                Id = TombOathPassiveId,
                DisplayName = "Tomb Oath",
                UnlockGoldCost = 80,
                IsPassive = true,
                Effect = PowerEffectKind.ArmorAura,
                EffectMagnitude = 1.4f,
                BuildingMitigation = 1.5f,
            });
            registry.Register(new PowerDefData
            {
                Id = RoyalStandardAbilityId,
                DisplayName = "Royal Standard",
                UnlockGoldCost = 150,
                CooldownSeconds = 45f,
                DurationSeconds = 12f,
                Effect = PowerEffectKind.ArmorAura,
                EffectMagnitude = 3f,
                BuildingMitigation = 3f,
                HeroMoment = true,
            });
            registry.Register(new PowerDefData
            {
                Id = LevyHornAbilityId,
                DisplayName = "Levy Horn",
                UnlockGoldCost = 120,
                CooldownSeconds = 36f,
                DurationSeconds = 9f,
                Effect = PowerEffectKind.MoveSpeedAura,
                EffectMagnitude = 2.2f,
            });
            registry.Register(new PowerDefData
            {
                Id = RainOfArrowsAbilityId,
                DisplayName = "Rain of Arrows",
                UnlockGoldCost = 165,
                CooldownSeconds = 42f,
                DurationSeconds = 8f,
                Effect = PowerEffectKind.DamageAura,
                EffectMagnitude = 7f,
            });
            registry.Register(new PowerDefData
            {
                Id = LastMarchAbilityId,
                DisplayName = "Last March",
                UnlockGoldCost = 180,
                CooldownSeconds = 50f,
                DurationSeconds = 10f,
                Effect = PowerEffectKind.ArmorAura,
                EffectMagnitude = 2.5f,
                BuildingMitigation = 5f,
            });
            registry.Register(new PowerDefData
            {
                Id = HarvestTitheAbilityId,
                DisplayName = "Harvest Tithe",
                UnlockGoldCost = 155,
                CooldownSeconds = 48f,
                DurationSeconds = 18f,
                Effect = PowerEffectKind.EconomyBoost,
                EffectMagnitude = 10f,
            });
            registry.Register(new PowerDefData
            {
                Id = EyesOfTheCrownAbilityId,
                DisplayName = "Eyes of the Crown",
                UnlockGoldCost = 140,
                CooldownSeconds = 48f,
                DurationSeconds = 0f,
                Effect = PowerEffectKind.SpawnScouts,
                EffectMagnitude = 6f,
                SpawnUnitDefinitionId = RoyalCrownEyeId,
            });
            registry.Register(new PowerDefData
            {
                Id = KingsChargeAbilityId,
                DisplayName = "King's Charge",
                UnlockGoldCost = 130,
                CooldownSeconds = 38f,
                DurationSeconds = 8f,
                Effect = PowerEffectKind.MoveSpeedAura,
                EffectMagnitude = 2.8f,
            });
        }

        private static void RegisterOutcast(DefinitionRegistry registry)
        {
            registry.Register(new UnitDefData
            {
                Id = OutcastVillagerId,
                DisplayName = "Mountain Villager",
                MaxHealth = 82f,
                MoveSpeed = 5.2f,
                AttackDamage = 10f,
                AttackRange = 1.7f,
                AttackCooldown = 1f,
                GoldCost = 42,
                TrainSeconds = 3.4f,
                Role = UnitRole.Infantry,
                SquadSize = 16,
            });
            registry.Register(new UnitDefData
            {
                Id = OutcastBuilderId,
                DisplayName = "Hobgoblin",
                MaxHealth = 58f,
                MoveSpeed = 5f,
                AttackDamage = 0f,
                AttackRange = 0f,
                AttackCooldown = 1f,
                GoldCost = 28,
                TrainSeconds = 2.6f,
                IsBuilder = true,
                CanGather = false,
                CarryCapacity = 16,
                GatherRate = 5.8f,
                Role = UnitRole.Builder,
                SquadSize = 1,
            });
            registry.Register(new UnitDefData
            {
                Id = OutcastHunterId,
                DisplayName = "Hunter",
                MaxHealth = 62f,
                MoveSpeed = 6.2f,
                AttackDamage = 11f,
                AttackRange = 12f,
                AttackCooldown = 1.15f,
                GoldCost = 62,
                TrainSeconds = 4.6f,
                Role = UnitRole.Ranged,
                SquadSize = 8,
                SightRadius = 175f,
                ProjectileSpeed = 46f,
            });
            registry.Register(new UnitDefData
            {
                Id = OutcastRangerId,
                DisplayName = "Woodland Ranger",
                MaxHealth = 68f,
                MoveSpeed = 5.4f,
                AttackDamage = 13f,
                AttackRange = 15f,
                AttackCooldown = 1.2f,
                GoldCost = 64,
                TrainSeconds = 4.8f,
                Role = UnitRole.Ranged,
                SquadSize = 12,
                ProjectileSpeed = 50f,
            });
            registry.Register(new UnitDefData
            {
                Id = OutcastBeastRiderId,
                DisplayName = "Beast Rider",
                MaxHealth = 108f,
                MoveSpeed = 8.2f,
                AttackDamage = 14f,
                AttackRange = 1.8f,
                AttackCooldown = 0.95f,
                GoldCost = 108,
                TrainSeconds = 5.8f,
                Role = UnitRole.Cavalry,
                SquadSize = 6,
                Armor = 2f,
            });
            registry.Register(new UnitDefData
            {
                Id = OutcastGiantId,
                DisplayName = "Frost Giant",
                MaxHealth = 220f,
                MoveSpeed = 3.2f,
                AttackDamage = 26f,
                AttackRange = 2.4f,
                AttackCooldown = 1.35f,
                GoldCost = 160,
                TrainSeconds = 9f,
                Role = UnitRole.Siege,
                SquadSize = 1,
                Armor = 5f,
                BuildingDamageMultiplier = 2.8f,
            });
            registry.Register(new UnitDefData
            {
                Id = OutcastSnarerId,
                DisplayName = "Snarer",
                MaxHealth = 72f,
                MoveSpeed = 5.1f,
                AttackDamage = 10f,
                AttackRange = 1.6f,
                AttackCooldown = 1.05f,
                GoldCost = 80,
                TrainSeconds = 5.2f,
                Role = UnitRole.Infantry,
                SquadSize = 8,
                BuildingDamageMultiplier = 2.8f,
                SightRadius = 105f,
            });
            registry.Register(new UnitDefData
            {
                Id = OutcastWindRiderId,
                DisplayName = "Wind Rider",
                MaxHealth = 70f,
                MoveSpeed = 9.4f,
                AttackDamage = 12f,
                AttackRange = 1.8f,
                AttackCooldown = 0.9f,
                GoldCost = 95,
                TrainSeconds = 6f,
                Role = UnitRole.Cavalry,
                SquadSize = 4,
                TraversalCapabilities = Asterra.Core.World.TraversalCapability.Land
                    | Asterra.Core.World.TraversalCapability.Flying,
                RequiresHeightLaunch = true,
                FlightDurationSeconds = 14f,
                SightRadius = 150f,
            });
            registry.Register(new UnitDefData
            {
                Id = OutcastSpriteId,
                DisplayName = "Sprite",
                MaxHealth = 28f,
                MoveSpeed = 6.8f,
                AttackDamage = 5f,
                AttackRange = 1.4f,
                AttackCooldown = 0.75f,
                GoldCost = 22,
                TrainSeconds = 2.2f,
                Role = UnitRole.Infantry,
                SquadSize = 20,
            });
            registry.Register(new UnitDefData
            {
                Id = OutcastNatureCubId,
                DisplayName = "Wild Cub",
                MaxHealth = 36f,
                MoveSpeed = 6.4f,
                AttackDamage = 7f,
                AttackRange = 1.5f,
                AttackCooldown = 0.85f,
                GoldCost = 0,
                TrainSeconds = 0.4f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
            });
            registry.Register(new UnitDefData
            {
                Id = OutcastSkyEyeId,
                DisplayName = "Sky Eye",
                MaxHealth = 18f,
                MoveSpeed = 8.5f,
                AttackDamage = 2f,
                AttackRange = 1.2f,
                AttackCooldown = 1.2f,
                GoldCost = 0,
                TrainSeconds = 0.3f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
                TraversalCapabilities = Asterra.Core.World.TraversalCapability.Flying,
                SightRadius = 220f,
            });
            registry.Register(new UnitDefData
            {
                Id = OutcastHeirId,
                DisplayName = "Exiled Heir",
                MaxHealth = 240f,
                MoveSpeed = 5.4f,
                AttackDamage = 19f,
                AttackRange = 2f,
                AttackCooldown = 0.9f,
                GoldCost = 250,
                TrainSeconds = 14f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
                Armor = 3f,
                IsLeader = true,
                SightRadius = 150f,
            });
            registry.Register(new UnitDefData
            {
                Id = OutcastWoldId,
                DisplayName = "Great Wold",
                MaxHealth = 280f,
                MoveSpeed = 7.2f,
                AttackDamage = 24f,
                AttackRange = 2.1f,
                AttackCooldown = 0.85f,
                GoldCost = 265,
                TrainSeconds = 15f,
                Role = UnitRole.Cavalry,
                SquadSize = 1,
                Armor = 3.5f,
                IsLeader = true,
                SightRadius = 160f,
            });
            registry.Register(new UnitDefData
            {
                Id = OutcastElderId,
                DisplayName = "Village Elder",
                MaxHealth = 210f,
                MoveSpeed = 4.6f,
                AttackDamage = 14f,
                AttackRange = 10f,
                AttackCooldown = 1.2f,
                GoldCost = 240,
                TrainSeconds = 13.5f,
                Role = UnitRole.Ranged,
                SquadSize = 1,
                Armor = 2f,
                IsLeader = true,
                SightRadius = 155f,
                ProjectileSpeed = 40f,
            });
            registry.Register(new UnitDefData
            {
                Id = OutcastHuntCallerId,
                DisplayName = "Hunt-Caller",
                MaxHealth = 225f,
                MoveSpeed = 6f,
                AttackDamage = 17f,
                AttackRange = 12f,
                AttackCooldown = 1.05f,
                GoldCost = 245,
                TrainSeconds = 13.5f,
                Role = UnitRole.Ranged,
                SquadSize = 1,
                Armor = 2f,
                IsLeader = true,
                SightRadius = 185f,
                ProjectileSpeed = 48f,
            });

            registry.Register(new BuildingDefData
            {
                Id = OutcastBurrowsId,
                DisplayName = "Burrows",
                MaxHealth = 560f,
                GoldCost = 205,
                TimberCost = 0,
                BuildSeconds = 5.8f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                Category = BuildingCategory.Troop,
                SightRadius = 85f,
                FootprintX = 12f,
                FootprintZ = 12f,
                TrainableUnitIds = new[]
                {
                    OutcastVillagerId,
                    OutcastHunterId,
                    OutcastRangerId,
                    OutcastBeastRiderId,
                    OutcastGiantId,
                    OutcastSnarerId,
                    OutcastSpriteId,
                },
            });
            registry.Register(new BuildingDefData
            {
                Id = OutcastAerieId,
                DisplayName = "Aerie",
                MaxHealth = 520f,
                GoldCost = 200,
                TimberCost = 0,
                BuildSeconds = 6.2f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                Category = BuildingCategory.Troop,
                SightRadius = 110f,
                FootprintX = 12f,
                FootprintZ = 12f,
                TrainableUnitIds = new[] { OutcastWindRiderId, OutcastSpriteId, OutcastHunterId },
            });
            registry.Register(new BuildingDefData
            {
                Id = OutcastVillageHallId,
                DisplayName = "Village Hall",
                MaxHealth = 600f,
                GoldCost = 210,
                TimberCost = 0,
                BuildSeconds = 6f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                Category = BuildingCategory.Troop,
                SightRadius = 90f,
                FootprintX = 13f,
                FootprintZ = 13f,
                TrainableUnitIds = new[]
                {
                    OutcastVillagerId,
                    OutcastRangerId,
                    OutcastSpriteId,
                    OutcastSnarerId,
                },
            });
            registry.Register(new BuildingDefData
            {
                Id = OutcastMineId,
                DisplayName = "Mine",
                MaxHealth = 440f,
                GoldCost = 145,
                TimberCost = 0,
                BuildSeconds = 5.5f,
                FootprintX = 7f,
                FootprintZ = 7f,
                Kind = BuildingKind.Outpost,
                Category = BuildingCategory.Resource,
                SightRadius = 95f,
                GoldPerSecond = 9,
            });
            registry.Register(new BuildingDefData
            {
                Id = OutcastTreetopWatchId,
                DisplayName = "Treetopped Watch",
                MaxHealth = 400f,
                GoldCost = 175,
                TimberCost = 0,
                BuildSeconds = 5f,
                FootprintX = 5f,
                FootprintZ = 5f,
                Kind = BuildingKind.Tower,
                Category = BuildingCategory.Tower,
                AllowsGarrison = true,
                GarrisonCapacity = 2,
                AttackDamage = 13f,
                AttackRange = 26f,
                AttackCooldown = 1.3f,
                SightRadius = 170f,
            });
            registry.Register(new BuildingDefData
            {
                Id = OutcastGroundWorksId,
                DisplayName = "Ground Works",
                MaxHealth = 720f,
                GoldCost = 120,
                TimberCost = 0,
                BuildSeconds = 4f,
                FootprintX = 14f,
                FootprintZ = 4f,
                Kind = BuildingKind.Wall,
                Category = BuildingCategory.Wall,
                SnapToWallGrid = true,
                WallSegmentLength = 14f,
                SightRadius = 38f,
            });
            registry.Register(new BuildingDefData
            {
                Id = OutcastGreatCampId,
                DisplayName = "Great Camp",
                MaxHealth = 1180f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Keep,
                GoldPerSecond = 2,
                SightRadius = 155f,
                FootprintX = 18f,
                FootprintZ = 18f,
                TrainableUnitIds = new[]
                {
                    OutcastBuilderId,
                    OutcastHeirId,
                    OutcastWoldId,
                    OutcastElderId,
                    OutcastHuntCallerId,
                },
                AttachmentSlotCount = 4,
                AttachmentAllowedBuildingIds = new[] { KeepTurretId },
                AttachmentRadius = 14f,
            });

            registry.Register(new UpgradeDefData
            {
                Id = GreatPerchUpgradeId,
                DisplayName = "Great Perch",
                GoldCost = 180,
                KeepHealthBonus = 80f,
                KeepSightBonus = 35f,
                Kind = UpgradeKind.Keep,
                ResearchSeconds = 11f,
            });
            registry.Register(new UpgradeDefData
            {
                Id = RollingFogUpgradeId,
                DisplayName = "Rolling Fog",
                GoldCost = 165,
                Kind = UpgradeKind.Keep,
                ResearchSeconds = 10f,
            });
            registry.Register(new UpgradeDefData
            {
                Id = CloakedUpgradeId,
                DisplayName = "Cloaked",
                GoldCost = 140,
                EquipGoldCost = 32,
                SightBonus = 30f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 9f,
                CompatibleUnitIds = new[] { OutcastHunterId, OutcastRangerId, OutcastSnarerId, OutcastSpriteId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = NaturesCamouflageUpgradeId,
                DisplayName = "Nature's Camouflage",
                GoldCost = 130,
                EquipGoldCost = 28,
                ArmorBonus = 2.5f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 8f,
                CompatibleUnitIds = new[] { OutcastVillagerId, OutcastBuilderId },
                CompatibleRoleMask = UpgradeDefData.RoleMask(UnitRole.Infantry, UnitRole.Builder),
            });
            registry.Register(new UpgradeDefData
            {
                Id = GiantHideUpgradeId,
                DisplayName = "Giant Hide",
                GoldCost = 155,
                EquipGoldCost = 40,
                ArmorBonus = 4f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 10f,
                CompatibleUnitIds = new[] { OutcastGiantId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = SaddleBindUpgradeId,
                DisplayName = "Saddle Bind",
                GoldCost = 145,
                EquipGoldCost = 34,
                ArmorBonus = 2f,
                AttackDamageBonus = 3f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 9f,
                CompatibleUnitIds = new[] { OutcastBeastRiderId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = SnareCordsUpgradeId,
                DisplayName = "Snare Cords",
                GoldCost = 135,
                EquipGoldCost = 30,
                AttackDamageBonus = 3f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 9f,
                CompatibleUnitIds = new[] { OutcastSnarerId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = WindHarnessUpgradeId,
                DisplayName = "Wind Harness",
                GoldCost = 145,
                EquipGoldCost = 34,
                ArmorBonus = 1.5f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 9f,
                CompatibleUnitIds = new[] { OutcastWindRiderId },
            });

            registry.Register(new PowerDefData
            {
                Id = WildBondPassiveId,
                DisplayName = "Wild Bond",
                UnlockGoldCost = 80,
                IsPassive = true,
                Effect = PowerEffectKind.MoveSpeedAura,
                EffectMagnitude = 0.7f,
            });
            registry.Register(new PowerDefData
            {
                Id = NaturesAidAbilityId,
                DisplayName = "Nature's Aid",
                UnlockGoldCost = 170,
                CooldownSeconds = 50f,
                DurationSeconds = 0f,
                Effect = PowerEffectKind.SpawnRandomBeasts,
                EffectMagnitude = 8f,
                HeroMoment = true,
            });
            registry.Register(new PowerDefData
            {
                Id = DamBreakerAbilityId,
                DisplayName = "Dam Breaker",
                UnlockGoldCost = 190,
                CooldownSeconds = 60f,
                DurationSeconds = 22f,
                Effect = PowerEffectKind.FloodArea,
                EffectMagnitude = 28f,
            });
            registry.Register(new PowerDefData
            {
                Id = EyesInSkyAbilityId,
                DisplayName = "Eyes in the Sky",
                UnlockGoldCost = 150,
                CooldownSeconds = 45f,
                DurationSeconds = 22f,
                Effect = PowerEffectKind.EyesInSky,
                EffectMagnitude = 4f,
            });
            registry.Register(new PowerDefData
            {
                Id = CampfireAbilityId,
                DisplayName = "Campfire",
                UnlockGoldCost = 130,
                CooldownSeconds = 40f,
                DurationSeconds = 10f,
                Effect = PowerEffectKind.ArmorAura,
                EffectMagnitude = 2.4f,
            });
            registry.Register(new PowerDefData
            {
                Id = StampedeAbilityId,
                DisplayName = "Stampede",
                UnlockGoldCost = 125,
                CooldownSeconds = 36f,
                DurationSeconds = 8f,
                Effect = PowerEffectKind.MoveSpeedAura,
                EffectMagnitude = 2.6f,
            });
            registry.Register(new PowerDefData
            {
                Id = GreenTitheAbilityId,
                DisplayName = "Green Tithe",
                UnlockGoldCost = 150,
                CooldownSeconds = 48f,
                DurationSeconds = 16f,
                Effect = PowerEffectKind.EconomyBoost,
                EffectMagnitude = 9f,
            });
        }

        private static void RegisterFreetown(DefinitionRegistry registry)
        {
            registry.Register(new UnitDefData
            {
                Id = FreetownDrunkId,
                DisplayName = "Tavern Drunk",
                MaxHealth = 85f,
                MoveSpeed = 4.8f,
                AttackDamage = 11f,
                AttackRange = 1.7f,
                AttackCooldown = 1.05f,
                GoldCost = 40,
                TrainSeconds = 3.2f,
                Role = UnitRole.Infantry,
                SquadSize = 16,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownBuilderId,
                DisplayName = "Shipwright",
                MaxHealth = 60f,
                MoveSpeed = 4.9f,
                AttackDamage = 0f,
                AttackRange = 0f,
                AttackCooldown = 1f,
                GoldCost = 30,
                TrainSeconds = 2.7f,
                IsBuilder = true,
                CanGather = false,
                CarryCapacity = 16,
                GatherRate = 5.7f,
                Role = UnitRole.Builder,
                SquadSize = 1,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownMudslingerId,
                DisplayName = "Mudslinger",
                MaxHealth = 58f,
                MoveSpeed = 5f,
                AttackDamage = 12f,
                AttackRange = 11.5f,
                AttackCooldown = 1.2f,
                GoldCost = 55,
                TrainSeconds = 4.4f,
                Role = UnitRole.Ranged,
                SquadSize = 12,
                ProjectileSpeed = 32f,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownPrivateerId,
                DisplayName = "Privateer",
                MaxHealth = 105f,
                MoveSpeed = 7.2f,
                AttackDamage = 14f,
                AttackRange = 1.8f,
                AttackCooldown = 0.95f,
                GoldCost = 100,
                TrainSeconds = 5.6f,
                Role = UnitRole.Cavalry,
                SquadSize = 6,
                Armor = 2f,
                TraversalCapabilities = Asterra.Core.World.TraversalCapability.Amphibious,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownHighwaymanId,
                DisplayName = "Highwayman",
                MaxHealth = 52f,
                MoveSpeed = 6.8f,
                AttackDamage = 10f,
                AttackRange = 1.6f,
                AttackCooldown = 0.9f,
                GoldCost = 72,
                TrainSeconds = 4.8f,
                Role = UnitRole.Infantry,
                SquadSize = 4,
                SightRadius = 180f,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownCrowId,
                DisplayName = "Crow",
                MaxHealth = 32f,
                MoveSpeed = 8.8f,
                AttackDamage = 5f,
                AttackRange = 1.3f,
                AttackCooldown = 0.8f,
                GoldCost = 35,
                TrainSeconds = 3f,
                Role = UnitRole.Infantry,
                SquadSize = 8,
                TraversalCapabilities = Asterra.Core.World.TraversalCapability.Flying,
                SightRadius = 200f,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownHoundId,
                DisplayName = "Hound",
                MaxHealth = 70f,
                MoveSpeed = 8.4f,
                AttackDamage = 11f,
                AttackRange = 1.5f,
                AttackCooldown = 0.85f,
                GoldCost = 48,
                TrainSeconds = 3.6f,
                Role = UnitRole.Cavalry,
                SquadSize = 6,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownCrabId,
                DisplayName = "Warrior Crab",
                MaxHealth = 140f,
                MoveSpeed = 3.6f,
                AttackDamage = 16f,
                AttackRange = 1.8f,
                AttackCooldown = 1.15f,
                GoldCost = 110,
                TrainSeconds = 6.5f,
                Role = UnitRole.Infantry,
                SquadSize = 4,
                Armor = 5f,
                TraversalCapabilities = Asterra.Core.World.TraversalCapability.Amphibious,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownBruteId,
                DisplayName = "Brute",
                MaxHealth = 170f,
                MoveSpeed = 4.1f,
                AttackDamage = 18f,
                AttackRange = 1.8f,
                AttackCooldown = 1.1f,
                GoldCost = 125,
                TrainSeconds = 7f,
                Role = UnitRole.Infantry,
                SquadSize = 8,
                Armor = 3.5f,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownImpId,
                DisplayName = "Jump Imp",
                MaxHealth = 48f,
                MoveSpeed = 6.5f,
                AttackDamage = 8f,
                AttackRange = 1.4f,
                AttackCooldown = 0.85f,
                GoldCost = 58,
                TrainSeconds = 4.2f,
                Role = UnitRole.Infantry,
                SquadSize = 10,
                TraversalCapabilities = Asterra.Core.World.TraversalCapability.Land
                    | Asterra.Core.World.TraversalCapability.Jump,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownFodderId,
                DisplayName = "Cannon Fodder",
                MaxHealth = 55f,
                MoveSpeed = 5.3f,
                AttackDamage = 7f,
                AttackRange = 1.6f,
                AttackCooldown = 1f,
                GoldCost = 22,
                TrainSeconds = 2.2f,
                Role = UnitRole.Infantry,
                SquadSize = 20,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownSapperId,
                DisplayName = "Improvised Explosive",
                MaxHealth = 65f,
                MoveSpeed = 4.9f,
                AttackDamage = 12f,
                AttackRange = 1.7f,
                AttackCooldown = 1.1f,
                GoldCost = 82,
                TrainSeconds = 5.4f,
                Role = UnitRole.Infantry,
                SquadSize = 6,
                BuildingDamageMultiplier = 3.2f,
                SightRadius = 95f,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownFlamerId,
                DisplayName = "Flamer",
                MaxHealth = 88f,
                MoveSpeed = 4.2f,
                AttackDamage = 18f,
                AttackRange = 8.5f,
                AttackCooldown = 1.6f,
                GoldCost = 125,
                TrainSeconds = 7.5f,
                Role = UnitRole.Siege,
                SquadSize = 4,
                Armor = 1f,
                BuildingDamageMultiplier = 2.8f,
                ProjectileSpeed = 22f,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownPowderCartId,
                DisplayName = "Powder Cart",
                MaxHealth = 55f,
                MoveSpeed = 6.8f,
                AttackDamage = 40f,
                AttackRange = 3f,
                AttackCooldown = 10f,
                GoldCost = 0,
                TrainSeconds = 0.3f,
                Role = UnitRole.Siege,
                SquadSize = 1,
                BuildingDamageMultiplier = 4f,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownBrewmasterId,
                DisplayName = "Brewmaster",
                MaxHealth = 235f,
                MoveSpeed = 4.8f,
                AttackDamage = 20f,
                AttackRange = 6.5f,
                AttackCooldown = 1.1f,
                GoldCost = 250,
                TrainSeconds = 14f,
                Role = UnitRole.Ranged,
                SquadSize = 1,
                Armor = 2.5f,
                IsLeader = true,
                SightRadius = 140f,
                ProjectileSpeed = 24f,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownCaptainId,
                DisplayName = "Privateer Captain",
                MaxHealth = 250f,
                MoveSpeed = 6.2f,
                AttackDamage = 21f,
                AttackRange = 2f,
                AttackCooldown = 0.9f,
                GoldCost = 255,
                TrainSeconds = 14f,
                Role = UnitRole.Cavalry,
                SquadSize = 1,
                Armor = 3f,
                IsLeader = true,
                SightRadius = 155f,
                TraversalCapabilities = Asterra.Core.World.TraversalCapability.Amphibious,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownDockmasterId,
                DisplayName = "Dockmaster",
                MaxHealth = 220f,
                MoveSpeed = 5f,
                AttackDamage = 16f,
                AttackRange = 2f,
                AttackCooldown = 0.95f,
                GoldCost = 240,
                TrainSeconds = 13.5f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
                Armor = 2.5f,
                IsLeader = true,
                SightRadius = 150f,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownFenceId,
                DisplayName = "The Fence",
                MaxHealth = 200f,
                MoveSpeed = 5.6f,
                AttackDamage = 15f,
                AttackRange = 1.8f,
                AttackCooldown = 0.85f,
                GoldCost = 245,
                TrainSeconds = 13.5f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
                Armor = 2f,
                IsLeader = true,
                SightRadius = 170f,
            });
            registry.Register(new UnitDefData
            {
                Id = FreetownIslandSpeakerId,
                DisplayName = "Island Speaker",
                MaxHealth = 215f,
                MoveSpeed = 5.2f,
                AttackDamage = 17f,
                AttackRange = 10f,
                AttackCooldown = 1.15f,
                GoldCost = 248,
                TrainSeconds = 14f,
                Role = UnitRole.Ranged,
                SquadSize = 1,
                Armor = 2f,
                IsLeader = true,
                SightRadius = 165f,
                ProjectileSpeed = 36f,
            });

            registry.Register(new BuildingDefData
            {
                Id = FreetownSmugglersDenId,
                DisplayName = "Smugglers Den",
                MaxHealth = 580f,
                GoldCost = 190,
                TimberCost = 0,
                BuildSeconds = 6f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                Category = BuildingCategory.Troop,
                SightRadius = 85f,
                FootprintX = 12f,
                FootprintZ = 12f,
                TrainableUnitIds = new[]
                {
                    FreetownDrunkId,
                    FreetownMudslingerId,
                    FreetownPrivateerId,
                    FreetownHighwaymanId,
                    FreetownBruteId,
                    FreetownFlamerId,
                    FreetownSapperId,
                    FreetownImpId,
                    FreetownCrabId,
                    FreetownCrowId,
                    FreetownHoundId,
                    FreetownFodderId,
                },
            });
            registry.Register(new BuildingDefData
            {
                Id = FreetownHutId,
                DisplayName = "Hut",
                MaxHealth = 420f,
                GoldCost = 130,
                TimberCost = 0,
                BuildSeconds = 4.5f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                Category = BuildingCategory.Troop,
                SightRadius = 70f,
                FootprintX = 9f,
                FootprintZ = 9f,
                TrainableUnitIds = new[]
                {
                    FreetownDrunkId,
                    FreetownFodderId,
                    FreetownHoundId,
                    FreetownCrowId,
                },
            });
            registry.Register(new BuildingDefData
            {
                Id = FreetownBlackMarketId,
                DisplayName = "Black Market",
                MaxHealth = 400f,
                GoldCost = 150,
                TimberCost = 0,
                BuildSeconds = 5.2f,
                FootprintX = 7f,
                FootprintZ = 7f,
                Kind = BuildingKind.Outpost,
                Category = BuildingCategory.Resource,
                SightRadius = 90f,
                GoldPerSecond = 10,
            });
            registry.Register(new BuildingDefData
            {
                Id = FreetownCrowsNestId,
                DisplayName = "Crows Nest",
                MaxHealth = 380f,
                GoldCost = 165,
                TimberCost = 0,
                BuildSeconds = 4.8f,
                FootprintX = 4.5f,
                FootprintZ = 4.5f,
                Kind = BuildingKind.Tower,
                Category = BuildingCategory.Tower,
                AllowsGarrison = true,
                GarrisonCapacity = 2,
                AttackDamage = 12f,
                AttackRange = 26f,
                AttackCooldown = 1.25f,
                SightRadius = 185f,
            });
            registry.Register(new BuildingDefData
            {
                Id = FreetownBarricadesId,
                DisplayName = "Barricades",
                MaxHealth = 640f,
                GoldCost = 100,
                TimberCost = 0,
                BuildSeconds = 3.5f,
                FootprintX = 12f,
                FootprintZ = 3.8f,
                Kind = BuildingKind.Wall,
                Category = BuildingCategory.Wall,
                SnapToWallGrid = true,
                WallSegmentLength = 12f,
                SightRadius = 32f,
            });
            registry.Register(new BuildingDefData
            {
                Id = FreetownTavernId,
                DisplayName = "Tavern",
                MaxHealth = 1120f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Keep,
                GoldPerSecond = 2,
                SightRadius = 150f,
                FootprintX = 16f,
                FootprintZ = 16f,
                TrainableUnitIds = new[]
                {
                    FreetownBuilderId,
                    FreetownBrewmasterId,
                    FreetownCaptainId,
                    FreetownDockmasterId,
                    FreetownFenceId,
                    FreetownIslandSpeakerId,
                },
                AttachmentSlotCount = 4,
                AttachmentAllowedBuildingIds = new[] { KeepTurretId },
                AttachmentRadius = 14f,
            });

            registry.Register(new UpgradeDefData
            {
                Id = RareLootUpgradeId,
                DisplayName = "Rare Loot",
                GoldCost = 170,
                KeepHealthBonus = 120f,
                Kind = UpgradeKind.Keep,
                ResearchSeconds = 10f,
            });
            registry.Register(new UpgradeDefData
            {
                Id = GrappleHooksUpgradeId,
                DisplayName = "Grapple Hooks",
                GoldCost = 140,
                EquipGoldCost = 32,
                SightBonus = 12f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 9f,
                CompatibleUnitIds = new[] { FreetownImpId, FreetownHighwaymanId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = RageUpgradeId,
                DisplayName = "Rage",
                GoldCost = 155,
                EquipGoldCost = 38,
                AttackDamageBonus = 4f,
                UnitDamageMultiplier = 1.1f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 10f,
                CompatibleRoleMask = UpgradeDefData.RoleMask(UnitRole.Infantry, UnitRole.Cavalry),
            });
            registry.Register(new UpgradeDefData
            {
                Id = FirebrewUpgradeId,
                DisplayName = "Firebrew",
                GoldCost = 150,
                EquipGoldCost = 36,
                AttackDamageBonus = 5f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 10f,
                CompatibleUnitIds = new[] { FreetownFlamerId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = BlastFuseUpgradeId,
                DisplayName = "Blast Fuse",
                GoldCost = 135,
                EquipGoldCost = 32,
                AttackDamageBonus = 3f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 9f,
                CompatibleUnitIds = new[] { FreetownSapperId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = CrabPlateUpgradeId,
                DisplayName = "Crab Plate",
                GoldCost = 145,
                EquipGoldCost = 34,
                ArmorBonus = 3.5f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 9f,
                CompatibleUnitIds = new[] { FreetownCrabId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = ShipNailsUpgradeId,
                DisplayName = "Ship Nails",
                GoldCost = 120,
                EquipGoldCost = 28,
                ArmorBonus = 3f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 8f,
                CompatibleRoleMask = UpgradeDefData.RoleMask(UnitRole.Builder),
            });

            registry.Register(new PowerDefData
            {
                Id = PortCallPassiveId,
                DisplayName = "Port Call",
                UnlockGoldCost = 80,
                IsPassive = true,
                Effect = PowerEffectKind.EconomyBoost,
                EffectMagnitude = 2f,
            });
            registry.Register(new PowerDefData
            {
                Id = TradeSurplusAbilityId,
                DisplayName = "Trade Surplus",
                UnlockGoldCost = 155,
                CooldownSeconds = 48f,
                DurationSeconds = 18f,
                Effect = PowerEffectKind.EconomyBoost,
                EffectMagnitude = 14f,
            });
            registry.Register(new PowerDefData
            {
                Id = ExplosiveConvoyAbilityId,
                DisplayName = "Explosive Convoy",
                UnlockGoldCost = 185,
                CooldownSeconds = 58f,
                DurationSeconds = 0f,
                Effect = PowerEffectKind.ExplosiveStrip,
                EffectMagnitude = 5f,
                HeroMoment = true,
            });
            registry.Register(new PowerDefData
            {
                Id = MercenariesAbilityId,
                DisplayName = "Mercenaries",
                UnlockGoldCost = 165,
                CooldownSeconds = 50f,
                DurationSeconds = 0f,
                Effect = PowerEffectKind.SpawnRandomBeasts,
                EffectMagnitude = 7f,
            });
            registry.Register(new PowerDefData
            {
                Id = SurprisedDeliveryAbilityId,
                DisplayName = "Surprised Delivery",
                UnlockGoldCost = 180,
                CooldownSeconds = 55f,
                DurationSeconds = 0f,
                Effect = PowerEffectKind.ExplosiveStrip,
                EffectMagnitude = 3f,
            });
            registry.Register(new PowerDefData
            {
                Id = SeaLegsAbilityId,
                DisplayName = "Sea Legs",
                UnlockGoldCost = 120,
                CooldownSeconds = 36f,
                DurationSeconds = 9f,
                Effect = PowerEffectKind.MoveSpeedAura,
                EffectMagnitude = 2.4f,
            });
            registry.Register(new PowerDefData
            {
                Id = RiotAbilityId,
                DisplayName = "Riot",
                UnlockGoldCost = 130,
                CooldownSeconds = 40f,
                DurationSeconds = 10f,
                Effect = PowerEffectKind.ArmorAura,
                EffectMagnitude = 2.2f,
            });
            registry.Register(new PowerDefData
            {
                Id = CrowStormAbilityId,
                DisplayName = "Crow Storm",
                UnlockGoldCost = 140,
                CooldownSeconds = 44f,
                DurationSeconds = 0f,
                Effect = PowerEffectKind.SpawnScouts,
                EffectMagnitude = 8f,
                SpawnUnitDefinitionId = FreetownCrowId,
            });
        }

        private static void RegisterUniversity(DefinitionRegistry registry)
        {
            registry.Register(new UnitDefData
            {
                Id = UniversityFellowId,
                DisplayName = "Fellow",
                MaxHealth = 82f,
                MoveSpeed = 4.7f,
                AttackDamage = 10f,
                AttackRange = 1.7f,
                AttackCooldown = 1.05f,
                GoldCost = 42,
                TrainSeconds = 3.4f,
                Role = UnitRole.Infantry,
                SquadSize = 14,
            });
            registry.Register(new UnitDefData
            {
                Id = UniversityBuilderId,
                DisplayName = "Practitioner",
                MaxHealth = 58f,
                MoveSpeed = 4.8f,
                AttackDamage = 0f,
                AttackRange = 0f,
                AttackCooldown = 1f,
                GoldCost = 32,
                TrainSeconds = 2.8f,
                IsBuilder = true,
                CanGather = false,
                CarryCapacity = 16,
                GatherRate = 5.5f,
                Role = UnitRole.Builder,
                SquadSize = 1,
            });
            registry.Register(new UnitDefData
            {
                Id = UniversityPoisonId,
                DisplayName = "Poison Specialist",
                MaxHealth = 56f,
                MoveSpeed = 4.9f,
                AttackDamage = 13f,
                AttackRange = 12f,
                AttackCooldown = 1.25f,
                GoldCost = 62,
                TrainSeconds = 4.6f,
                Role = UnitRole.Ranged,
                SquadSize = 10,
                ProjectileSpeed = 30f,
            });
            registry.Register(new UnitDefData
            {
                Id = UniversitySpiderId,
                DisplayName = "Mechanical Spider",
                MaxHealth = 100f,
                MoveSpeed = 7.4f,
                AttackDamage = 13f,
                AttackRange = 1.6f,
                AttackCooldown = 0.9f,
                GoldCost = 98,
                TrainSeconds = 5.5f,
                Role = UnitRole.Cavalry,
                SquadSize = 5,
                Armor = 2.5f,
                TraversalCapabilities = Asterra.Core.World.TraversalCapability.Land
                    | Asterra.Core.World.TraversalCapability.Jump,
            });
            registry.Register(new UnitDefData
            {
                Id = UniversityAirshipId,
                DisplayName = "Airship",
                MaxHealth = 95f,
                MoveSpeed = 6.2f,
                AttackDamage = 11f,
                AttackRange = 10f,
                AttackCooldown = 1.4f,
                GoldCost = 145,
                TrainSeconds = 8f,
                Role = UnitRole.Cavalry,
                SquadSize = 1,
                Armor = 1.5f,
                TraversalCapabilities = Asterra.Core.World.TraversalCapability.Flying,
                SightRadius = 210f,
                ProjectileSpeed = 28f,
            });
            registry.Register(new UnitDefData
            {
                Id = UniversityTrebuchetId,
                DisplayName = "Trebuchet",
                MaxHealth = 90f,
                MoveSpeed = 3.2f,
                AttackDamage = 22f,
                AttackRange = 28f,
                AttackCooldown = 2.4f,
                GoldCost = 140,
                TrainSeconds = 8.5f,
                Role = UnitRole.Siege,
                SquadSize = 1,
                Armor = 1f,
                BuildingDamageMultiplier = 2.6f,
                ProjectileSpeed = 18f,
            });
            registry.Register(new UnitDefData
            {
                Id = UniversityEarthBreakerId,
                DisplayName = "Earth Breaker",
                MaxHealth = 110f,
                MoveSpeed = 3.8f,
                AttackDamage = 16f,
                AttackRange = 2.2f,
                AttackCooldown = 1.35f,
                GoldCost = 95,
                TrainSeconds = 6.2f,
                Role = UnitRole.Siege,
                SquadSize = 2,
                Armor = 3f,
                BuildingDamageMultiplier = 3.4f,
            });
            registry.Register(new UnitDefData
            {
                Id = UniversityChancellorId,
                DisplayName = "Chancellor",
                MaxHealth = 230f,
                MoveSpeed = 4.9f,
                AttackDamage = 16f,
                AttackRange = 9f,
                AttackCooldown = 1.15f,
                GoldCost = 250,
                TrainSeconds = 14f,
                Role = UnitRole.Ranged,
                SquadSize = 1,
                Armor = 2f,
                IsLeader = true,
                SightRadius = 160f,
                ProjectileSpeed = 32f,
            });
            registry.Register(new UnitDefData
            {
                Id = UniversityArmsDeanId,
                DisplayName = "Dean of Arms",
                MaxHealth = 245f,
                MoveSpeed = 5.1f,
                AttackDamage = 20f,
                AttackRange = 2f,
                AttackCooldown = 0.95f,
                GoldCost = 252,
                TrainSeconds = 14f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
                Armor = 3.5f,
                IsLeader = true,
                SightRadius = 145f,
            });
            registry.Register(new UnitDefData
            {
                Id = UniversityClimateDeanId,
                DisplayName = "Dean of Climate",
                MaxHealth = 215f,
                MoveSpeed = 5f,
                AttackDamage = 15f,
                AttackRange = 11f,
                AttackCooldown = 1.2f,
                GoldCost = 248,
                TrainSeconds = 13.5f,
                Role = UnitRole.Ranged,
                SquadSize = 1,
                Armor = 2f,
                IsLeader = true,
                SightRadius = 175f,
                ProjectileSpeed = 34f,
            });
            registry.Register(new UnitDefData
            {
                Id = UniversityArchivistId,
                DisplayName = "Archivist",
                MaxHealth = 205f,
                MoveSpeed = 5.3f,
                AttackDamage = 14f,
                AttackRange = 8f,
                AttackCooldown = 1.05f,
                GoldCost = 242,
                TrainSeconds = 13.5f,
                Role = UnitRole.Ranged,
                SquadSize = 1,
                Armor = 1.5f,
                IsLeader = true,
                SightRadius = 185f,
                ProjectileSpeed = 30f,
            });
            registry.Register(new UnitDefData
            {
                Id = UniversityProvostId,
                DisplayName = "Provost",
                MaxHealth = 225f,
                MoveSpeed = 5f,
                AttackDamage = 17f,
                AttackRange = 2f,
                AttackCooldown = 1f,
                GoldCost = 246,
                TrainSeconds = 14f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
                Armor = 2.5f,
                IsLeader = true,
                SightRadius = 155f,
            });

            registry.Register(new BuildingDefData
            {
                Id = UniversityWorkshopId,
                DisplayName = "Workshop",
                MaxHealth = 600f,
                GoldCost = 205,
                TimberCost = 0,
                BuildSeconds = 6.2f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                Category = BuildingCategory.Troop,
                SightRadius = 85f,
                FootprintX = 12f,
                FootprintZ = 12f,
                TrainableUnitIds = new[]
                {
                    UniversityFellowId,
                    UniversityPoisonId,
                    UniversitySpiderId,
                    UniversityAirshipId,
                    UniversityTrebuchetId,
                    UniversityEarthBreakerId,
                },
            });
            registry.Register(new BuildingDefData
            {
                Id = UniversityLibraryId,
                DisplayName = "Forbidden Library",
                MaxHealth = 480f,
                GoldCost = 180,
                TimberCost = 0,
                BuildSeconds = 5.5f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                Category = BuildingCategory.Troop,
                SightRadius = 90f,
                FootprintX = 11f,
                FootprintZ = 11f,
                TrainableUnitIds = new[]
                {
                    UniversityFellowId,
                    UniversityPoisonId,
                    UniversityBuilderId,
                },
            });
            registry.Register(new BuildingDefData
            {
                Id = UniversityAlchemistId,
                DisplayName = "Alchemist",
                MaxHealth = 440f,
                GoldCost = 155,
                TimberCost = 0,
                BuildSeconds = 5f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                Category = BuildingCategory.Troop,
                SightRadius = 80f,
                FootprintX = 9f,
                FootprintZ = 9f,
                TrainableUnitIds = new[]
                {
                    UniversityPoisonId,
                    UniversityFellowId,
                },
            });
            registry.Register(new BuildingDefData
            {
                Id = UniversityObservatoryId,
                DisplayName = "Grand Observatory",
                MaxHealth = 420f,
                GoldCost = 175,
                TimberCost = 0,
                BuildSeconds = 5.8f,
                FootprintX = 8f,
                FootprintZ = 8f,
                Kind = BuildingKind.Outpost,
                Category = BuildingCategory.Resource,
                SightRadius = 220f,
                GoldPerSecond = 9,
            });
            registry.Register(new BuildingDefData
            {
                Id = UniversityClockworkTowerId,
                DisplayName = "Clockwork Tower",
                MaxHealth = 400f,
                GoldCost = 175,
                TimberCost = 0,
                BuildSeconds = 5f,
                FootprintX = 4.5f,
                FootprintZ = 4.5f,
                Kind = BuildingKind.Tower,
                Category = BuildingCategory.Tower,
                AllowsGarrison = true,
                GarrisonCapacity = 2,
                AttackDamage = 13f,
                AttackRange = 28f,
                AttackCooldown = 1.2f,
                SightRadius = 175f,
            });
            registry.Register(new BuildingDefData
            {
                Id = UniversityMoatId,
                DisplayName = "College Moat",
                MaxHealth = 580f,
                GoldCost = 95,
                TimberCost = 0,
                BuildSeconds = 4.2f,
                FootprintX = 14f,
                FootprintZ = 8f,
                Kind = BuildingKind.Wall,
                Category = BuildingCategory.Wall,
                SnapToWallGrid = true,
                WallSegmentLength = 14f,
                SightRadius = 28f,
            });
            registry.Register(new BuildingDefData
            {
                Id = UniversityWeatherRodsId,
                DisplayName = "Weather Rods",
                MaxHealth = 280f,
                GoldCost = 130,
                TimberCost = 0,
                BuildSeconds = 4.5f,
                FootprintX = 5f,
                FootprintZ = 5f,
                Kind = BuildingKind.Special,
                Category = BuildingCategory.Special,
                SightRadius = 70f,
            });
            registry.Register(new BuildingDefData
            {
                Id = UniversityFarGlassId,
                DisplayName = "Far Glass",
                MaxHealth = 80f,
                GoldCost = 0,
                TimberCost = 0,
                BuildSeconds = 0.2f,
                FootprintX = 3f,
                FootprintZ = 3f,
                Kind = BuildingKind.Special,
                Category = BuildingCategory.Special,
                SightRadius = 240f,
            });
            registry.Register(new BuildingDefData
            {
                Id = UniversityCollegeId,
                DisplayName = "Grand College",
                MaxHealth = 1150f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Keep,
                GoldPerSecond = 2,
                SightRadius = 155f,
                FootprintX = 16f,
                FootprintZ = 16f,
                TrainableUnitIds = new[]
                {
                    UniversityBuilderId,
                    UniversityChancellorId,
                    UniversityArmsDeanId,
                    UniversityClimateDeanId,
                    UniversityArchivistId,
                    UniversityProvostId,
                },
                AttachmentSlotCount = 4,
                AttachmentAllowedBuildingIds = new[] { KeepTurretId },
                AttachmentRadius = 14f,
            });

            registry.Register(new UpgradeDefData
            {
                Id = GreatSpyglassUpgradeId,
                DisplayName = "Great Spyglass",
                GoldCost = 165,
                KeepSightBonus = 55f,
                Kind = UpgradeKind.Keep,
                ResearchSeconds = 10f,
            });
            registry.Register(new UpgradeDefData
            {
                Id = AdvancedConstructionUpgradeId,
                DisplayName = "Advanced Construction",
                GoldCost = 160,
                Kind = UpgradeKind.Keep,
                ResearchSeconds = 11f,
            });
            registry.Register(new UpgradeDefData
            {
                Id = AdvancedCogsUpgradeId,
                DisplayName = "Advanced Cogs",
                GoldCost = 150,
                EquipGoldCost = 36,
                ArmorBonus = 2.5f,
                AttackDamageBonus = 2f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 10f,
                CompatibleUnitIds = new[]
                {
                    UniversitySpiderId,
                    UniversityAirshipId,
                    UniversityTrebuchetId,
                    UniversityEarthBreakerId,
                },
            });
            registry.Register(new UpgradeDefData
            {
                Id = AlchemicalTipsUpgradeId,
                DisplayName = "Alchemical Tips",
                GoldCost = 135,
                EquipGoldCost = 30,
                AttackDamageBonus = 4f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 9f,
                CompatibleUnitIds = new[] { UniversityPoisonId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = CounterweightUpgradeId,
                DisplayName = "Counterweight",
                GoldCost = 140,
                EquipGoldCost = 34,
                AttackDamageBonus = 5f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 9f,
                CompatibleUnitIds = new[] { UniversityTrebuchetId },
            });

            registry.Register(new PowerDefData
            {
                Id = ForecastAbilityId,
                DisplayName = "Forecast",
                UnlockGoldCost = 90,
                IsPassive = true,
                Effect = PowerEffectKind.Forecast,
                EffectMagnitude = 1f,
            });
            registry.Register(new PowerDefData
            {
                Id = FarGlassAbilityId,
                DisplayName = "Far Glass",
                UnlockGoldCost = 170,
                CooldownSeconds = 90f,
                DurationSeconds = 0f,
                Effect = PowerEffectKind.RelocateSight,
                EffectMagnitude = 1f,
                HeroMoment = true,
            });
            registry.Register(new PowerDefData
            {
                Id = OpenLectureAbilityId,
                DisplayName = "Open Lecture",
                UnlockGoldCost = 150,
                CooldownSeconds = 46f,
                DurationSeconds = 16f,
                Effect = PowerEffectKind.EconomyBoost,
                EffectMagnitude = 12f,
            });
            registry.Register(new PowerDefData
            {
                Id = FieldExerciseAbilityId,
                DisplayName = "Field Exercise",
                UnlockGoldCost = 155,
                CooldownSeconds = 48f,
                DurationSeconds = 0f,
                Effect = PowerEffectKind.SpawnRandomBeasts,
                EffectMagnitude = 6f,
            });
            registry.Register(new PowerDefData
            {
                Id = PrecisionDrillAbilityId,
                DisplayName = "Precision Drill",
                UnlockGoldCost = 140,
                CooldownSeconds = 40f,
                DurationSeconds = 10f,
                Effect = PowerEffectKind.DamageAura,
                EffectMagnitude = 3f,
            });
            registry.Register(new PowerDefData
            {
                Id = TenureAbilityId,
                DisplayName = "Tenure",
                UnlockGoldCost = 130,
                CooldownSeconds = 38f,
                DurationSeconds = 10f,
                Effect = PowerEffectKind.ArmorAura,
                EffectMagnitude = 2.4f,
            });
            registry.Register(new PowerDefData
            {
                Id = PublishedPaperAbilityId,
                DisplayName = "Published Paper",
                UnlockGoldCost = 120,
                CooldownSeconds = 36f,
                DurationSeconds = 9f,
                Effect = PowerEffectKind.MoveSpeedAura,
                EffectMagnitude = 2.2f,
            });
            registry.Register(new PowerDefData
            {
                Id = ClockworkMusterAbilityId,
                DisplayName = "Clockwork Muster",
                UnlockGoldCost = 160,
                CooldownSeconds = 52f,
                DurationSeconds = 0f,
                Effect = PowerEffectKind.SpawnScouts,
                EffectMagnitude = 4f,
                SpawnUnitDefinitionId = UniversitySpiderId,
            });
        }

        private static void RegisterRisingSun(DefinitionRegistry registry)
        {
            registry.Register(new UnitDefData
            {
                Id = ChurchZealotId,
                DisplayName = "Dawn Zealot",
                MaxHealth = 90f,
                MoveSpeed = 4.9f,
                AttackDamage = 12f,
                AttackRange = 1.7f,
                AttackCooldown = 1f,
                GoldCost = 45,
                TrainSeconds = 3.4f,
                Role = UnitRole.Infantry,
                SquadSize = 14,
            });
            registry.Register(new UnitDefData
            {
                Id = ChurchMasonId,
                DisplayName = "Temple Mason",
                MaxHealth = 62f,
                MoveSpeed = 4.7f,
                AttackDamage = 0f,
                AttackRange = 0f,
                AttackCooldown = 1f,
                GoldCost = 32,
                TrainSeconds = 2.8f,
                IsBuilder = true,
                CanGather = false,
                CarryCapacity = 16,
                GatherRate = 5.4f,
                Role = UnitRole.Builder,
                SquadSize = 1,
            });
            registry.Register(new UnitDefData
            {
                Id = ChurchPriestId,
                DisplayName = "Sun Priest",
                MaxHealth = 68f,
                MoveSpeed = 4.6f,
                AttackDamage = 14f,
                AttackRange = 12.5f,
                AttackCooldown = 1.2f,
                GoldCost = 70,
                TrainSeconds = 5f,
                Role = UnitRole.Ranged,
                SquadSize = 8,
                SightRadius = 140f,
                ProjectileSpeed = 36f,
            });
            registry.Register(new UnitDefData
            {
                Id = ChurchStalkerId,
                DisplayName = "Sun Stalker",
                MaxHealth = 58f,
                MoveSpeed = 7.1f,
                AttackDamage = 11f,
                AttackRange = 1.6f,
                AttackCooldown = 0.85f,
                GoldCost = 78,
                TrainSeconds = 5f,
                Role = UnitRole.Infantry,
                SquadSize = 4,
                SightRadius = 195f,
            });
            registry.Register(new UnitDefData
            {
                Id = ChurchRiderId,
                DisplayName = "Dawn Rider",
                MaxHealth = 108f,
                MoveSpeed = 7.4f,
                AttackDamage = 14f,
                AttackRange = 1.8f,
                AttackCooldown = 0.95f,
                GoldCost = 105,
                TrainSeconds = 5.8f,
                Role = UnitRole.Cavalry,
                SquadSize = 6,
                Armor = 2f,
            });
            registry.Register(new UnitDefData
            {
                Id = ChurchGuardId,
                DisplayName = "Radiant Guard",
                MaxHealth = 165f,
                MoveSpeed = 4.4f,
                AttackDamage = 17f,
                AttackRange = 1.8f,
                AttackCooldown = 1.05f,
                GoldCost = 130,
                TrainSeconds = 7.2f,
                Role = UnitRole.Infantry,
                SquadSize = 8,
                Armor = 4f,
            });
            registry.Register(new UnitDefData
            {
                Id = ChurchEngineId,
                DisplayName = "Solar Engine",
                MaxHealth = 95f,
                MoveSpeed = 3.4f,
                AttackDamage = 20f,
                AttackRange = 22f,
                AttackCooldown = 2.2f,
                GoldCost = 135,
                TrainSeconds = 8.2f,
                Role = UnitRole.Siege,
                SquadSize = 1,
                Armor = 1.5f,
                BuildingDamageMultiplier = 2.5f,
                ProjectileSpeed = 26f,
            });
            registry.Register(new UnitDefData
            {
                Id = ChurchPurifierId,
                DisplayName = "Purifier",
                MaxHealth = 72f,
                MoveSpeed = 4.8f,
                AttackDamage = 13f,
                AttackRange = 1.7f,
                AttackCooldown = 1.1f,
                GoldCost = 88,
                TrainSeconds = 5.6f,
                Role = UnitRole.Infantry,
                SquadSize = 6,
                BuildingDamageMultiplier = 3.3f,
            });
            registry.Register(new UnitDefData
            {
                Id = ChurchHighPriestId,
                DisplayName = "High Priest",
                MaxHealth = 240f,
                MoveSpeed = 4.8f,
                AttackDamage = 18f,
                AttackRange = 11f,
                AttackCooldown = 1.1f,
                GoldCost = 255,
                TrainSeconds = 14f,
                Role = UnitRole.Ranged,
                SquadSize = 1,
                Armor = 2.5f,
                IsLeader = true,
                SightRadius = 170f,
                ProjectileSpeed = 38f,
            });
            registry.Register(new UnitDefData
            {
                Id = ChurchInquisitorId,
                DisplayName = "Inquisitor",
                MaxHealth = 235f,
                MoveSpeed = 5.4f,
                AttackDamage = 19f,
                AttackRange = 2f,
                AttackCooldown = 0.9f,
                GoldCost = 250,
                TrainSeconds = 14f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
                Armor = 3f,
                IsLeader = true,
                SightRadius = 160f,
            });
            registry.Register(new UnitDefData
            {
                Id = ChurchEclipseWardenId,
                DisplayName = "Eclipse Warden",
                MaxHealth = 250f,
                MoveSpeed = 4.7f,
                AttackDamage = 16f,
                AttackRange = 2f,
                AttackCooldown = 1f,
                GoldCost = 252,
                TrainSeconds = 14f,
                Role = UnitRole.Infantry,
                SquadSize = 1,
                Armor = 4f,
                IsLeader = true,
                SightRadius = 150f,
            });
            registry.Register(new UnitDefData
            {
                Id = ChurchDawnHeraldId,
                DisplayName = "Dawn Herald",
                MaxHealth = 220f,
                MoveSpeed = 6f,
                AttackDamage = 17f,
                AttackRange = 1.9f,
                AttackCooldown = 0.9f,
                GoldCost = 248,
                TrainSeconds = 13.5f,
                Role = UnitRole.Cavalry,
                SquadSize = 1,
                Armor = 2.5f,
                IsLeader = true,
                SightRadius = 165f,
            });
            registry.Register(new UnitDefData
            {
                Id = ChurchReliquaryId,
                DisplayName = "Reliquary",
                MaxHealth = 210f,
                MoveSpeed = 5f,
                AttackDamage = 15f,
                AttackRange = 9f,
                AttackCooldown = 1.15f,
                GoldCost = 245,
                TrainSeconds = 13.5f,
                Role = UnitRole.Ranged,
                SquadSize = 1,
                Armor = 2f,
                IsLeader = true,
                SightRadius = 180f,
                ProjectileSpeed = 32f,
            });

            registry.Register(new BuildingDefData
            {
                Id = ChurchMonasteryId,
                DisplayName = "Warrior Monastery",
                MaxHealth = 600f,
                GoldCost = 200,
                TimberCost = 0,
                BuildSeconds = 6.2f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                Category = BuildingCategory.Troop,
                SightRadius = 85f,
                FootprintX = 12f,
                FootprintZ = 12f,
                TrainableUnitIds = new[]
                {
                    ChurchZealotId,
                    ChurchStalkerId,
                    ChurchRiderId,
                    ChurchGuardId,
                    ChurchEngineId,
                    ChurchPurifierId,
                    ChurchPriestId,
                },
            });
            registry.Register(new BuildingDefData
            {
                Id = ChurchSunTempleId,
                DisplayName = "Sun Temple",
                MaxHealth = 500f,
                GoldCost = 185,
                TimberCost = 0,
                BuildSeconds = 5.6f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                Category = BuildingCategory.Troop,
                SightRadius = 95f,
                FootprintX = 11f,
                FootprintZ = 11f,
                TrainableUnitIds = new[]
                {
                    ChurchPriestId,
                    ChurchStalkerId,
                    ChurchZealotId,
                },
            });
            registry.Register(new BuildingDefData
            {
                Id = ChurchSacredSiteId,
                DisplayName = "Sacred Site",
                MaxHealth = 380f,
                GoldCost = 130,
                TimberCost = 0,
                BuildSeconds = 4.4f,
                CanProduce = true,
                QueueCapacity = 2,
                Kind = BuildingKind.Producer,
                Category = BuildingCategory.Troop,
                SightRadius = 110f,
                FootprintX = 8f,
                FootprintZ = 8f,
                TrainableUnitIds = new[]
                {
                    ChurchZealotId,
                    ChurchPriestId,
                },
            });
            registry.Register(new BuildingDefData
            {
                Id = ChurchShrineId,
                DisplayName = "Offering Shrine",
                MaxHealth = 400f,
                GoldCost = 155,
                TimberCost = 0,
                BuildSeconds = 5.2f,
                FootprintX = 7f,
                FootprintZ = 7f,
                Kind = BuildingKind.Outpost,
                Category = BuildingCategory.Resource,
                SightRadius = 100f,
                GoldPerSecond = 10,
            });
            registry.Register(new BuildingDefData
            {
                Id = ChurchScorchedTowerId,
                DisplayName = "Scorched Tower",
                MaxHealth = 390f,
                GoldCost = 173,
                TimberCost = 0,
                BuildSeconds = 5f,
                FootprintX = 4.5f,
                FootprintZ = 4.5f,
                Kind = BuildingKind.Tower,
                Category = BuildingCategory.Tower,
                AllowsGarrison = true,
                GarrisonCapacity = 2,
                AttackDamage = 14f,
                AttackRange = 28f,
                AttackCooldown = 1.15f,
                SightRadius = 170f,
            });
            registry.Register(new BuildingDefData
            {
                Id = ChurchSacredWallsId,
                DisplayName = "Sacred Walls",
                MaxHealth = 680f,
                GoldCost = 110,
                TimberCost = 0,
                BuildSeconds = 3.8f,
                FootprintX = 12f,
                FootprintZ = 3.8f,
                Kind = BuildingKind.Wall,
                Category = BuildingCategory.Wall,
                SnapToWallGrid = true,
                WallSegmentLength = 12f,
                SightRadius = 30f,
            });
            registry.Register(new BuildingDefData
            {
                Id = ChurchGrandTempleId,
                DisplayName = "Grand Temple",
                MaxHealth = 1180f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Keep,
                GoldPerSecond = 2,
                SightRadius = 155f,
                FootprintX = 16f,
                FootprintZ = 16f,
                TrainableUnitIds = new[]
                {
                    ChurchMasonId,
                    ChurchHighPriestId,
                    ChurchInquisitorId,
                    ChurchEclipseWardenId,
                    ChurchDawnHeraldId,
                    ChurchReliquaryId,
                },
                AttachmentSlotCount = 4,
                AttachmentAllowedBuildingIds = new[] { KeepTurretId },
                AttachmentRadius = 14f,
            });

            registry.Register(new UpgradeDefData
            {
                Id = SacredMasonryUpgradeId,
                DisplayName = "Sacred Masonry",
                GoldCost = 160,
                KeepHealthBonus = 140f,
                Kind = UpgradeKind.Keep,
                ResearchSeconds = 10f,
            });
            registry.Register(new UpgradeDefData
            {
                Id = SolarVestmentsUpgradeId,
                DisplayName = "Solar Vestments",
                GoldCost = 145,
                EquipGoldCost = 34,
                ArmorBonus = 2.5f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 9f,
                CompatibleRoleMask = UpgradeDefData.RoleMask(UnitRole.Infantry, UnitRole.Ranged),
            });
            registry.Register(new UpgradeDefData
            {
                Id = ScorchedShotUpgradeId,
                DisplayName = "Scorched Shot",
                GoldCost = 150,
                EquipGoldCost = 36,
                AttackDamageBonus = 5f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 10f,
                CompatibleUnitIds = new[] { ChurchPriestId, ChurchEngineId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = StalkerVeilUpgradeId,
                DisplayName = "Stalker Veil",
                GoldCost = 130,
                EquipGoldCost = 30,
                SightBonus = 20f,
                Kind = UpgradeKind.Equipment,
                ResearchSeconds = 8f,
                CompatibleUnitIds = new[] { ChurchStalkerId },
            });

            registry.Register(new PowerDefData
            {
                Id = FalseChroniclePassiveId,
                DisplayName = "False Chronicle",
                UnlockGoldCost = 85,
                IsPassive = true,
                Effect = PowerEffectKind.EconomyBoost,
                EffectMagnitude = 2f,
            });
            registry.Register(new PowerDefData
            {
                Id = SunRayAbilityId,
                DisplayName = "Sun Ray",
                UnlockGoldCost = 175,
                CooldownSeconds = 42f,
                DurationSeconds = 0f,
                Effect = PowerEffectKind.SunRay,
                EffectMagnitude = 55f,
                HeroMoment = true,
            });
            registry.Register(new PowerDefData
            {
                Id = DayOfTheSunAbilityId,
                DisplayName = "Day of the Sun",
                UnlockGoldCost = 190,
                CooldownSeconds = 70f,
                DurationSeconds = 22f,
                Effect = PowerEffectKind.DayOfTheSun,
                EffectMagnitude = 0.25f,
            });
            registry.Register(new PowerDefData
            {
                Id = BlindAbilityId,
                DisplayName = "Blind",
                UnlockGoldCost = 155,
                CooldownSeconds = 40f,
                DurationSeconds = 4f,
                Effect = PowerEffectKind.BlindRadius,
                EffectMagnitude = 18f,
            });
            registry.Register(new PowerDefData
            {
                Id = TitheAbilityId,
                DisplayName = "Tithe",
                UnlockGoldCost = 145,
                CooldownSeconds = 46f,
                DurationSeconds = 16f,
                Effect = PowerEffectKind.EconomyBoost,
                EffectMagnitude = 12f,
            });
            registry.Register(new PowerDefData
            {
                Id = SolarOathAbilityId,
                DisplayName = "Solar Oath",
                UnlockGoldCost = 125,
                CooldownSeconds = 38f,
                DurationSeconds = 10f,
                Effect = PowerEffectKind.ArmorAura,
                EffectMagnitude = 2.4f,
            });
            registry.Register(new PowerDefData
            {
                Id = ProcessionAbilityId,
                DisplayName = "Procession",
                UnlockGoldCost = 120,
                CooldownSeconds = 36f,
                DurationSeconds = 9f,
                Effect = PowerEffectKind.MoveSpeedAura,
                EffectMagnitude = 2.2f,
            });
            registry.Register(new PowerDefData
            {
                Id = PurgeTheDarkAbilityId,
                DisplayName = "Purge the Dark",
                UnlockGoldCost = 140,
                CooldownSeconds = 40f,
                DurationSeconds = 10f,
                Effect = PowerEffectKind.DamageAura,
                EffectMagnitude = 3.5f,
            });
        }

        private static void RegisterEarthworkSite(
            DefinitionRegistry registry,
            string id,
            string name,
            int gold,
            int timber,
            float seconds,
            float fx,
            float fz)
        {
            registry.Register(new BuildingDefData
            {
                Id = id,
                DisplayName = name,
                MaxHealth = 60f,
                GoldCost = gold,
                TimberCost = timber,
                BuildSeconds = seconds,
                FootprintX = fx,
                FootprintZ = fz,
                Kind = BuildingKind.Special,
                Category = BuildingCategory.Wall,
                SightRadius = 18f,
            });
        }
    }
}
