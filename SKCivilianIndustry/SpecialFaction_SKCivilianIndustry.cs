﻿using System;
using System.Collections.Generic;
using System.Linq;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using SKCivilianIndustry.Notifications;
using SKCivilianIndustry.Persistence;

namespace SKCivilianIndustry
{
    // The main faction class.
    public class SpecialFaction_SKCivilianIndustry : BaseSpecialFaction, IBulkPathfinding
    {
        // Information required for our faction.
        // General identifier for our faction.
        protected override string TracingName => "SKCivilianIndustry";

        // Let the game know we're going to want to use the DoLongRangePlanning_OnBackgroundNonSimThread_Subclass function.
        // This function is generally used for things that do not need to always run, such as navigation requests.
        protected override bool EverNeedsToRunLongRangePlanning => true;

        public ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> WormholeCommands { get; set; }
        public ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> MovementCommands { get; set; }

        // The following can be set to limit the number of times the background thread can be ran.
        //protected override int MinimumSecondsBetweenLongRangePlannings => 5;

        // When was the last time we sent a journel message? To update the player about civies are doing.
        protected ArcenSparseLookup<Planet, int> LastGameSecondForMessageAboutThisPlanet;
        protected ArcenSparseLookup<Planet, int> LastGameSecondForLastTachyonBurstOnThisPlanet;

        // General data used by the faction. Not required, but makes referencing it much easier.
        public CivilianFaction factionData;

        // Constants and/or game settings.
        private bool SettingsInitialized;
        public bool PlayerAligned;
        protected int MinimumOutpostDeploymentRange;
        protected FInt MilitiaAttackOverkillPercentage;
        protected int SecondsBetweenMilitiaUpgrades;
        protected bool DefensiveBattlestationForces;
        public int MinTechToProcess;
        public bool[] IgnoreResource;
        public FInt MilitiaStockpilePercentage;
        protected bool MilitiaExpandWithAllAllies;
        protected int MilitiaAttackMinimumStrength;

        // Note: We clear all variables on the faction in the constructor.
        // This is the (current) best way to make sure data is not carried between saves, especially statics.
        public SpecialFaction_SKCivilianIndustry() : base()
        {
            SettingsInitialized = false;
            PlayerAligned = false;
            factionData = null;
            LastGameSecondForMessageAboutThisPlanet = new ArcenSparseLookup<Planet, int>();
            LastGameSecondForLastTachyonBurstOnThisPlanet = new ArcenSparseLookup<Planet, int>();
            IgnoreResource = new bool[(int)CivilianResource.Length];
        }

        public enum Commands
        {
            SetMilitiaCaps,
            SetMilitiaAtEase,
            AttemptedToStoreAtEaseUnit,
            SetNextTargetForTradeStation
        }

        // Scale ship costs based on intensity. 5 is 100%, with a 10% step up or down based on intensity.
        public static FInt CostIntensityModifier( Faction faction )
        {
            int intensity = faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity;
            return FInt.FromParts( 1, 500 ) - (intensity * FInt.FromParts( 0, 100 ));
        }

        /// <summary>
        /// Returns a Civilian faction that is friendly.
        /// </summary>
        /// <param name="faction">The faction that we want our found Civilian faction to be friendly to.</param>
        /// <returns></returns>
        public static Faction GetFriendlyIndustry( Faction faction )
        {
            Faction alliedFaction = null;
            World_AIW2.Instance.DoForFactions( delegate ( Faction foundFactiion )
             {
                 if ( foundFactiion.Implementation is SpecialFaction_SKCivilianIndustry && faction.GetIsFriendlyTowards( foundFactiion ) )
                 {
                     alliedFaction = foundFactiion;
                     return DelReturn.Break;
                 }

                 return DelReturn.Continue;
             } );
            return alliedFaction;
        }

        public override void UpdatePowerLevel( Faction faction )
        {
            faction.OverallPowerLevel = FInt.Zero;

            if ( factionData == null )
                return;

            faction.OverallPowerLevel = FInt.FromParts( 0, 002 ) * factionData.MilitiaLeaders.Count;

            if ( faction.OverallPowerLevel > 1 )
                faction.OverallPowerLevel = FInt.One;
        }

        // Set up initial relationships.
        public override void SetStartingFactionRelationships( Faction faction )
        {
            base.SetStartingFactionRelationships( faction );

            // Start by becoming hostile to everybody.
            enemyThisFactionToAll( faction );

            // Than do our intial relationship step.
            UpdateAllegianceAndSettings( faction );
        }

        // Update relationships and settings.
        protected virtual void UpdateAllegianceAndSettings( Faction faction )
        {
            // Reset settings if needed.
            if ( !SettingsInitialized )
            {
                MinimumOutpostDeploymentRange = AIWar2GalaxySettingTable.Instance.GetRowByName( "MinimumOutpostDeploymentRange" ).DefaultIntValue;
                MilitiaAttackOverkillPercentage = FInt.FromParts( AIWar2GalaxySettingTable.Instance.GetRowByName( "MilitiaAttackOverkillPercentage" ).DefaultIntValue, 000 ) / 100;
                SecondsBetweenMilitiaUpgrades = AIWar2GalaxySettingTable.Instance.GetRowByName( "SecondsBetweenMilitiaUpgrades" ).DefaultIntValue;
                MinTechToProcess = AIWar2GalaxySettingTable.Instance.GetRowByName( "MinTechToProcess" ).DefaultIntValue;
                DefensiveBattlestationForces = false; // Can't get a default boolean from xml, apparently.
                MilitiaStockpilePercentage = FInt.FromParts( AIWar2GalaxySettingTable.Instance.GetRowByName( "MilitiaStockpilePercentage" ).DefaultIntValue, 000 ) / 100;
                MilitiaExpandWithAllAllies = false;
                SettingsInitialized = true;
                MilitiaAttackMinimumStrength = AIWar2GalaxySettingTable.Instance.GetRowByName( "MilitiaAttackMinimumStrength" ).DefaultIntValue * 1000;
            }

            // Set relationships.
            switch ( faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Allegiance )
            {
                case "AI Team":
                    allyThisFactionToAI( faction );
                    PlayerAligned = false;
                    break;
                case "Minor Faction Team Red":
                case "Minor Faction Team Blue":
                case "Minor Faction Team Green":
                    allyThisFactionToMinorFactionTeam( faction, faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Allegiance );
                    PlayerAligned = false;
                    break;
                default:
                    allyThisFactionToHumans( faction );
                    // If human related, also reload settings in case they changed them.
                    MinimumOutpostDeploymentRange = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "MinimumOutpostDeploymentRange" );
                    MilitiaAttackOverkillPercentage = FInt.FromParts( AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "MilitiaAttackOverkillPercentage" ), 000 ) / 100;
                    SecondsBetweenMilitiaUpgrades = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "SecondsBetweenMilitiaUpgrades" );
                    MinTechToProcess = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "MinTechToProcess" );
                    DefensiveBattlestationForces = AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "DefensiveBattlestationForces" );
                    MilitiaStockpilePercentage = FInt.FromParts( AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "MilitiaStockpilePercentage" ), 000 ) / 100;
                    MilitiaExpandWithAllAllies = AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "MilitiaExpandWithAllAllies" );
                    MilitiaAttackMinimumStrength = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "MilitiaAttackMinimumStrength" ) * 1000;
                    PlayerAligned = true;
                    break;
            }
        }

        public override void SeedStartingEntities_LaterEverythingElse( Faction faction, Galaxy galaxy, ArcenSimContext Context, MapTypeData mapType )
        {
            if ( faction.MustBeAwakenedByPlayer )
            {
                Mapgen_Base.Mapgen_SeedSpecialEntities( Context, galaxy, faction, SpecialEntityType.None, "CivilianIndustryBeacon", SeedingType.HardcodedCount, 1,
                    MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 3, 3, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal );
            }
        }

        // Yoinked from Scourge.
        public override void ReactToHacking_AsPartOfMainSim( GameEntity_Squad entityBeingHacked, FInt WaveMultiplier, ArcenSimContext Context, Faction overrideFaction = null )
        {
            Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );
            SpecialFaction_AI implementationAsType = aiFaction.Implementation as SpecialFaction_AI;
            if ( implementationAsType != null )
            {
                //First compute the base strength of the hacking response
                AISentinelsExternalData factionExternal = aiFaction.GetSentinelsExternal( ExternalDataRetrieval.ReturnNullIfNotFound );
                AICommonExternalData aiFactionExternal = aiFaction.GetAICommonExternalData( ExternalDataRetrieval.ReturnNullIfNotFound );
                int HackingWaveSize = factionExternal.AIDifficulty.BaseHackingWaveSize;
                int strength = (WaveMultiplier * HackingWaveSize).IntValue;

                //if there's an AIP multiplier, handle that now
                //If the AIP multiplier is .01 and the AIP is 200 then we do
                //newStrength = oldStrength + (oldStrength * AIPMultiplier*AIP)
                //a straight multiplier would allow the resulting waves to have too much variance
                FInt aipMultiplier = factionExternal.AIDifficulty.HackingAipMultiplier;
                int bonusStrength = 0;
                if ( aipMultiplier > FInt.Zero )
                {
                    FInt AIP = aiFactionExternal.AIProgress_Effective;
                    bonusStrength = (aipMultiplier * AIP * strength).IntValue;
                    strength += bonusStrength;
                }

                bool allowedGuardians = true;
                implementationAsType.SendWave( Context, aiFaction, strength, entityBeingHacked, null, -1, allowedGuardians );
                return;
            }
        }

        // Upgrade and kill units where applicable.
        public void UpgradeAndPurgeUnitsAsNeeded( Faction faction, ArcenSimContext Context )
        {
            if ( PlayerAligned )
                faction.InheritsTechUpgradesFromPlayerFactions = true;
            else
                faction.InheritsTechUpgradesFromPlayerFactions = false;
            faction.RecalculateMarkLevelsAndInheritedTechUnlocks();

            byte globalAIMark = World_AIW2.GetRandomAIFaction( Context ).CurrentGeneralMarkLevel;

            faction.DoForEntities( delegate ( GameEntity_Squad entity )
            {
                if ( entity.TypeData.GetHasTag( "CivMilitiaSpawn" ) && World_AIW2.Instance.GetEntityByID_Squad( entity.MinorFactionStackingID ) == null )
                {
                    entity.Die( Context, true );
                    return DelReturn.Continue;
                }

                byte requestedMark = faction.GetGlobalMarkLevelForShipLine( entity.TypeData );
                if ( !PlayerAligned )
                {
                    requestedMark = Math.Max( requestedMark, globalAIMark );
                    if ( !entity.TypeData.IsMobile )
                        requestedMark = Math.Max( requestedMark, entity.Planet.MarkLevelForAIOnly.Ordinal );
                    else if ( World_AIW2.Instance.GetEntityByID_Squad( entity.MinorFactionStackingID ) != null )
                        requestedMark = Math.Max( requestedMark, World_AIW2.Instance.GetEntityByID_Squad( entity.MinorFactionStackingID ).Planet.MarkLevelForAIOnly.Ordinal );
                }
                entity.SetCurrentMarkLevel( requestedMark, Context );
                return DelReturn.Continue;
            } );

            // Update resource ignoring.
            // Figure out what resources we should be ignoring.
            for ( int x = 0; x < IgnoreResource.Length; x++ )
            {
                List<TechUpgrade> upgrades = TechUpgradeTable.Instance.Rows;
                for ( int i = 0; i < upgrades.Count; i++ )
                {
                    TechUpgrade upgrade = upgrades[i];
                    if ( upgrade.InternalName == ((CivilianTech)x).ToString() )
                    {
                        int unlocked = faction.TechUnlocks[upgrade.RowIndexNonSim];
                        unlocked += faction.FreeTechUnlocks[upgrade.RowIndexNonSim];
                        unlocked += faction.CalculatedInheritedTechUnlocks[upgrade.RowIndexNonSim];
                        if ( unlocked < MinTechToProcess )
                            IgnoreResource[x] = true;
                        else
                            IgnoreResource[x] = false;
                        break;
                    }
                }
            }
        }

        // Handle stack splitting logic.
        public override void DoOnStackSplit( GameEntity_Squad originalSquad, GameEntity_Squad newSquad )
        {
            // If we have no world data, uh-oh, we won't be able to find where they're supposed to go.
            // Eeventually, add some sort of fallback logic for militia ships. For now, just skip em.
            if ( factionData != null )
            {
                for ( int y = 0; y < factionData.MilitiaLeaders.Count; y++ )
                {
                    GameEntity_Squad militiaLeader = World_AIW2.Instance.GetEntityByID_Squad( factionData.MilitiaLeaders[y] );
                    if ( militiaLeader == null )
                        continue;
                    CivilianMilitia militiaData = militiaLeader.GetCivilianMilitiaExt( ExternalDataRetrieval.CreateIfNotFound );
                    if ( militiaData.Ships != null )
                    {
                        for ( int z = 0; z < militiaData.Ships.GetPairCount(); z++ )
                        {
                            if ( militiaData.Ships[z].Contains( originalSquad.PrimaryKeyID ) )
                            {
                                militiaData.Ships[z].Add( newSquad.PrimaryKeyID );
                            }
                        }
                    }
                }
            }
        }

        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking( Faction faction, ArcenSimContext Context, bool IsFirstCallToFactionOfThisTypeThisCycle )
        {
            if ( faction.MustBeAwakenedByPlayer && !faction.HasBeenAwakenedByPlayer )
                return;

            if ( !PlayerAligned || factionData == null )
                return;

            if ( factionData.NextRaidInThisSeconds < 300 )
            {
                AIRaidNotifier notifier = new AIRaidNotifier();
                notifier.raidingWormholes = factionData.NextRaidWormholes;
                notifier.RaidedPlanets = new List<Planet>();
                for ( int x = 0; x < notifier.RaidingWormholes.Count; x++ )
                    if ( !notifier.RaidedPlanets.Contains( notifier.RaidingWormholes[x].Planet ) )
                        notifier.RaidedPlanets.Add( notifier.RaidingWormholes[x].Planet );
                notifier.faction = World_AIW2.GetRandomAIFaction( Context );
                notifier.SecondsLeft = factionData.NextRaidInThisSeconds;

                NotificationNonSim notification = Engine_AIW2.NonSimNotificationList_Building.GetOrAddEntry();
                notification.Assign( notifier.ClickHandler, notifier.ContentGetter, notifier.MouseoverHandler, "", 0, "AICargoShipRaiders", SortedNotificationPriorityLevel.Medium );
            }
        }

        // Handle the creation of the Grand Station.
        public void CreateGrandStation( Faction faction, Faction alignedFaction, ArcenSimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            // If human or ai alligned, spawn based on king units.
            if ( alignedFaction.Type == FactionType.Player || alignedFaction.Type == FactionType.AI )
            {
                World_AIW2.Instance.DoForEntities( EntityRollupType.KingUnitsOnly, delegate ( GameEntity_Squad kingEntity )
                {
                    // Make sure its the correct faction.
                    if ( kingEntity.PlanetFaction.Faction.FactionIndex != alignedFaction.FactionIndex )
                        return DelReturn.Continue;

                    // Load in our Grand Station's TypeData.
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "GrandStation" );

                    // Get the total radius of both our grand station and the king unit.
                    // This will be used to find a safe spawning location.
                    int radius = entityData.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius + kingEntity.TypeData.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius;

                    // Get the spawning coordinates for our start station.
                    ArcenPoint spawnPoint = ArcenPoint.ZeroZeroPoint;
                    int outerMax = 0;
                    do
                    {
                        outerMax++;
                        spawnPoint = kingEntity.Planet.GetSafePlacementPoint( Context, entityData, kingEntity.WorldLocation, radius, radius * outerMax );
                    } while ( spawnPoint == ArcenPoint.ZeroZeroPoint );

                    // Get the planetary faction to spawn our station in as.
                    PlanetFaction pFaction = kingEntity.Planet.GetPlanetFactionForFaction( faction );

                    // Spawn in the station.
                    GameEntity_Squad grandStation = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0, spawnPoint, Context );

                    // Add in our grand station to our faction's data
                    factionData.GrandStation = grandStation;

                    return DelReturn.Break;
                } );
                return;
            }
            else if ( !PlayerAligned )
            {
                // Not player or ai, see if they have a 'safe' planet for us to spawn on.
                World_AIW2.Instance.DoForPlanets( false, workingPlanet =>
                {
                    LongRangePlanningData_PlanetFaction workingData = workingPlanet.LongRangePlanningData.PlanetFactionDataByIndex[alignedFaction.FactionIndex];
                    if ( workingData == null )
                        return DelReturn.Break;
                    if ( workingData.DataByStance[FactionStance.Self].TotalStrength / 2 > workingData.DataByStance[FactionStance.Hostile].TotalStrength )
                    {
                        // Found a planet that they have majority control over. Spawn around a strong stationary friendly unit.
                        GameEntity_Squad bestEntity = null;
                        bool foundCenterpiece = false, foundStationary = false;
                        PlanetFaction workingPFaction = workingPlanet.GetPlanetFactionForFaction( alignedFaction );
                        workingPFaction.Entities.DoForEntities( delegate ( GameEntity_Squad allignedSquad )
                        {
                            // Default to the first if stationary.
                            if ( bestEntity == null && !allignedSquad.TypeData.IsMobile )
                                bestEntity = allignedSquad;

                            // If found is a centerpiece, pick it.
                            if ( allignedSquad.TypeData.SpecialType == SpecialEntityType.NPCFactionCenterpiece )
                            {
                                if ( !foundCenterpiece )
                                {
                                    bestEntity = allignedSquad;
                                    foundCenterpiece = true;
                                }
                                else if ( allignedSquad.GetStrengthOfSelfAndContents() > bestEntity.GetStrengthOfSelfAndContents() )
                                    bestEntity = allignedSquad;
                            }
                            else if ( !foundCenterpiece )
                            {
                                // No centerpiece, default to strongest, preferring stationary.
                                if ( !allignedSquad.TypeData.IsMobile )
                                {
                                    if ( !foundStationary )
                                    {
                                        bestEntity = allignedSquad;
                                        foundStationary = true;
                                    }
                                    else if ( allignedSquad.GetStrengthOfSelfAndContents() > bestEntity.GetStrengthOfSelfAndContents() )
                                        bestEntity = allignedSquad;
                                }
                            }

                            return DelReturn.Continue;
                        } );

                        if ( bestEntity == null )
                            return DelReturn.Continue;

                        // Load in our Grand Station's TypeData.
                        GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "GrandStation" );

                        // Get the total radius of both our grand station and the king unit.
                        // This will be used to find a safe spawning location.
                        int radius = entityData.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius + bestEntity.TypeData.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius;

                        // Get the spawning coordinates for our start station.
                        ArcenPoint spawnPoint = ArcenPoint.ZeroZeroPoint;
                        int outerMax = 0;
                        do
                        {
                            outerMax++;
                            spawnPoint = bestEntity.Planet.GetSafePlacementPoint( Context, entityData, bestEntity.WorldLocation, radius, radius * outerMax );
                        } while ( spawnPoint == ArcenPoint.ZeroZeroPoint );

                        // Get the planetary faction to spawn our station in as.
                        PlanetFaction pFaction = bestEntity.Planet.GetPlanetFactionForFaction( faction );

                        // Spawn in the station.
                        GameEntity_Squad grandStation = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0, spawnPoint, Context );

                        // Add in our grand station to our faction's data
                        factionData.GrandStation = grandStation;

                        return DelReturn.Break;
                    }
                    return DelReturn.Continue;
                } );
            }
        }

        // Handle creation of trade stations.
        public void CreateTradeStations( Faction faction, Faction alignedFaction, ArcenSimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            if ( factionData.NextTradeStationTarget == null )
                return;

            // Make sure we aren't trying to build a second one.
            // Skip if we already have a trade station on the planet.
            for ( int x = 0; x < factionData.TradeStations.Count; x++ )
            {
                GameEntity_Squad station = World_AIW2.Instance.GetEntityByID_Squad( factionData.TradeStations[x] );
                if ( station == null || station.TypeData.InternalName != "TradeStation" )
                    continue;
                if ( station.Planet == factionData.NextTradeStationTarget.Planet )
                {
                    factionData.NextTradeStationTarget = null;
                    return;
                }
            }

            // No trade station found for this planet. Create one.
            // Load in our trade station's data.
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "TradeStation" );

            // Get the total radius of both our trade station, and the target
            // This will be used to find a safe spawning location.
            int radius = entityData.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius + factionData.NextTradeStationTarget.TypeData.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius;

            // Get the spawning coordinates for our trade station.
            ArcenPoint spawnPoint = ArcenPoint.ZeroZeroPoint;
            int outerMax = 0;
            do
            {
                outerMax++;
                spawnPoint = factionData.NextTradeStationTarget.Planet.GetSafePlacementPoint( Context, entityData, factionData.NextTradeStationTarget.WorldLocation, radius, radius * outerMax );
            } while ( spawnPoint == ArcenPoint.ZeroZeroPoint );

            // Get the planetary faction to spawn our trade station in as.
            PlanetFaction pFaction = factionData.NextTradeStationTarget.Planet.GetPlanetFactionForFaction( faction );

            // Spawn in the station's construction point.
            GameEntity_Squad tradeStation = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0, spawnPoint, Context );

            // Add in our trade station to our faction's data
            factionData.TradeStations.Add( tradeStation.PrimaryKeyID );

            // Initialize cargo.
            CivilianCargo tradeCargo = tradeStation.GetCivilianCargoExt( ExternalDataRetrieval.CreateIfNotFound );
            // Large capacity.
            for ( int y = 0; y < tradeCargo.Capacity.Length; y++ )
                tradeCargo.Capacity[y] *= 25;
            // Give resources based on mine count.
            int mines = 0;
            tradeStation.Planet.DoForEntities( EntityRollupType.MetalProducers, delegate ( GameEntity_Squad mineEntity )
            {
                if ( mineEntity.TypeData.GetHasTag( "MetalGenerator" ) )
                    mines++;

                return DelReturn.Continue;
            } );
            tradeCargo.PerSecond[(int)factionData.NextTradeStationTarget.Planet.GetCivResourceForPlanet()] = (int)(mines * 1.5);

            // Remove rebuild counter, if applicable.
            if ( factionData.TradeStationRebuildTimerInSecondsByPlanet.GetHasKey( factionData.NextTradeStationTarget.Planet.Index ) )
                factionData.TradeStationRebuildTimerInSecondsByPlanet.RemovePairByKey( factionData.NextTradeStationTarget.Planet.Index );

            factionData.NextTradeStationTarget = null;
        }

        // Add buildings for the player to build.
        public void AddMilitiaBuildings( Faction faction, Faction alignedFaction, ArcenSimContext Context )
        {
            alignedFaction.DoForEntities( EntityRollupType.Battlestation, delegate ( GameEntity_Squad battlestation )
            {
                if ( battlestation.TypeData.IsBattlestation ) // Will hopefully fix a weird bug where planets could get battlestation buildings.
                {
                    // Add buildings to the battlestation/citadel's build list.
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MilitiaHeadquarters" );
                    Fleet.Membership mem = battlestation.FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( entityData );
                    mem.ExplicitBaseSquadCap = 1;

                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "TradePost" );
                    mem = battlestation.FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( entityData );
                    mem.ExplicitBaseSquadCap = 3;

                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MilitiaProtectorShipyards" );
                    mem = battlestation.FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( entityData );
                    mem.ExplicitBaseSquadCap = 1;
                }

                return DelReturn.Continue;
            } );
            alignedFaction.DoForControlledPlanets( delegate ( Planet planet )
            {
                // Add buildings to the planet's build list.
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MilitiaBarracks" );

                // Attempt to add to the planet's build list.
                Fleet.Membership mem = planet.GetCommandStationOrNull().FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( entityData );

                // Set the building caps.
                mem.ExplicitBaseSquadCap = 1;

                // Remove anything that planets shouldn't get.
                entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MilitiaHeadquarters" );
                mem = planet.GetCommandStationOrNull().FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( entityData );
                mem.ExplicitBaseSquadCap = 0;

                entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "TradePost" );
                mem = planet.GetCommandStationOrNull().FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( entityData );
                mem.ExplicitBaseSquadCap = 0;

                entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MilitiaProtectorShipyards" );
                mem = planet.GetCommandStationOrNull().FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( entityData );
                mem.ExplicitBaseSquadCap = 0;

                return DelReturn.Continue;
            } );
        }

        // Look for militia buildings placed by the player, and deal with them.
        public void ScanForMilitiaBuildings( Faction faction, Faction alignedFaction, ArcenSimContext Context )
        {
            alignedFaction.DoForEntities( "CivilianIndustryEntity", delegate ( GameEntity_Squad entity )
            {
                if ( entity.SecondsSpentAsRemains <= 0 && entity.SelfBuildingMetalRemaining <= 0 )
                {
                    if ( entity.TypeData.GetHasTag( "TradePost" ) && !factionData.TradeStations.Contains( entity.PrimaryKeyID ) )
                    {
                        // Trade Post. Add it to our list and give it resources.
                        factionData.TradeStations.Add( entity.PrimaryKeyID );

                        // Initialize cargo.
                        CivilianCargo tradeCargo = entity.GetCivilianCargoExt( ExternalDataRetrieval.CreateIfNotFound );
                        // Large capacity.
                        for ( int y = 0; y < tradeCargo.Capacity.Length; y++ )
                            tradeCargo.Capacity[y] *= 25;
                        // Give resources based on mine count.
                        int mines = 0;
                        entity.Planet.DoForEntities( EntityRollupType.MetalProducers, delegate ( GameEntity_Squad mineEntity )
                        {
                            if ( mineEntity.TypeData.GetHasTag( "MetalGenerator" ) )
                                mines++;

                            return DelReturn.Continue;
                        } );
                        tradeCargo.PerSecond[(int)entity.Planet.GetCivResourceForPlanet()] = mines;

                        entity.SetCivilianCargoExt( tradeCargo );
                    }
                    else if ( entity.TypeData.GetHasTag( "MilitiaHeadquarters" ) && !factionData.MilitiaLeaders.Contains( entity.PrimaryKeyID ) )
                    {
                        if ( entity.FleetMembership.Fleet.Centerpiece != null )
                        {
                            // Miltia Headquarters. Add it to our militia list and set it to patrol logic
                            factionData.MilitiaLeaders.Add( entity.PrimaryKeyID );

                            CivilianMilitia militiaStatus = entity.GetCivilianMilitiaExt( ExternalDataRetrieval.CreateIfNotFound );

                            militiaStatus.Centerpiece = entity.FleetMembership.Fleet.Centerpiece.PrimaryKeyID;
                            militiaStatus.CapMultiplier = 300; // 300%
                            militiaStatus.CostMultiplier = 33; // 33%

                            militiaStatus.Status = CivilianMilitiaStatus.Patrolling;
                            militiaStatus.PlanetFocus = entity.Planet.Index;

                            entity.SetCivilianMilitiaExt( militiaStatus );
                        }
                    }
                    else if ( entity.TypeData.GetHasTag( "MilitiaProtectorShipyards" ) && !factionData.MilitiaLeaders.Contains( entity.PrimaryKeyID ) )
                    {
                        if ( entity.FleetMembership.Fleet.Centerpiece != null )
                        {
                            // Militia Protector Shipyards. Add it to our militia list and set it to patrol logic.
                            factionData.MilitiaLeaders.Add( entity.PrimaryKeyID );

                            CivilianMilitia militiaStatus = entity.GetCivilianMilitiaExt( ExternalDataRetrieval.CreateIfNotFound );

                            militiaStatus.Centerpiece = entity.FleetMembership.Fleet.Centerpiece.PrimaryKeyID;
                            militiaStatus.Status = CivilianMilitiaStatus.Patrolling;

                            entity.SetCivilianMilitiaExt( militiaStatus );
                        }
                    }
                }
                return DelReturn.Continue;
            } );
        }

        // Handle resource processing.
        public void DoResources( Faction faction, ArcenSimContext Context )
        {
            // For every TradeStation we have defined in our faction data, deal with it.
            for ( int x = 0; x < factionData.TradeStations.Count; x++ )
            {
                // Load the entity, and its cargo data.
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( factionData.TradeStations[x] );
                if ( entity == null )
                {
                    factionData.TradeStations.RemoveAt( x );
                    x--;
                    continue;
                }

                if ( entity.SecondsSpentAsRemains > 0 )
                    continue; // Skip crippled stations.

                CivilianCargo entityCargo = entity.GetCivilianCargoExt( ExternalDataRetrieval.CreateIfNotFound );
                if ( entityCargo == null )
                    continue;

                CivilianResource resourceOnPlanet = entity.Planet.GetCivResourceForPlanet();

                // Deal with its per second values.
                for ( int y = 0; y < entityCargo.PerSecond.Length; y++ )
                {
                    // Update its income.
                    if ( (int)resourceOnPlanet != y )
                    {
                        if ( entityCargo.PerSecond[y] > 0 )
                            entityCargo.PerSecond[y] = 0;
                    }
                    else
                    {
                        int mineCount = 0;
                        entity.Planet.DoForEntities( "MetalGenerator", mine =>
                        {
                            mineCount++;

                            return DelReturn.Continue;
                        } );

                        entityCargo.PerSecond[y] = entity.CurrentMarkLevel + ((int)(mineCount * 1.5));
                    }


                    if ( entityCargo.PerSecond[y] != 0 )
                    {
                        // Update the resource, if able.
                        if ( entityCargo.PerSecond[y] > 0 )
                        {
                            int income = entityCargo.PerSecond[y] + entity.CurrentMarkLevel;
                            entityCargo.Amount[y] = Math.Min( entityCargo.Capacity[y], entityCargo.Amount[y] + income );
                        }
                    }
                }

                // Save its resources.
                entity.SetCivilianCargoExt( entityCargo );
            }
        }

        // Handle the creation of ships.
        public void DoShipSpawns( Faction faction, ArcenSimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            // Continue only if starting station is valid.
            if ( factionData.GrandStation == null )
                return;

            int cargoShipCapacity = factionData.TradeStations.Count + factionData.MilitiaLeaders.Count;
            cargoShipCapacity *= Math.Max(1, faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity / 2);

            // Build a cargo ship if we have enough requests for them.
            if ( factionData.CargoShipsIdle.Count < 5 || factionData.CargoShips.Count < cargoShipCapacity )
            {
                // Load our cargo ship's data.
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "CargoShip" );

                // Get the planet faction to spawn it in as.
                PlanetFaction pFaction = factionData.GrandStation.Planet.GetPlanetFactionForFaction( faction );

                // Get the spawning coordinates for our cargo ship.
                // We'll simply spawn it right on top of our grand station, and it'll dislocate itself.
                ArcenPoint spawnPoint = factionData.GrandStation.WorldLocation;

                // Spawn in the ship.
                GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0, spawnPoint, Context );

                // Add the cargo ship to our faction data.
                factionData.CargoShips.Add( entity.PrimaryKeyID );
                factionData.ChangeCargoShipStatus( entity, Status.Idle );
            }

            // Build mitia ship if we have enough requets for them.
            if ( factionData.MilitiaLeaders.Count < 1 || factionData.MilitiaCounter >= (factionData.GetResourceCost( faction ) + (factionData.GetResourceCost( faction ) * (factionData.MilitiaLeaders.Count / 10.0))) )
            {
                // Load our militia ship's data.
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MilitiaConstructionShip" );

                // Get the planet faction to spawn it in as.
                PlanetFaction pFaction = factionData.GrandStation.Planet.GetPlanetFactionForFaction( faction );

                // Get the spawning coordinates for our militia ship.
                // We'll simply spawn it right on top of our grand station, and it'll dislocate itself.
                ArcenPoint spawnPoint = factionData.GrandStation.WorldLocation;

                // Spawn in the ship.
                GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0, spawnPoint, Context );

                // Add the militia ship to our faction data.
                factionData.MilitiaLeaders.Add( entity.PrimaryKeyID );

                // Reset the build counter.
                factionData.MilitiaCounter = 0;
            }
        }

        // Check for ship arrival.
        public void DoShipArrival( Faction faction, ArcenSimContext Context )
        {
            // Pathing logic, detect arrival at trade station.
            for ( int x = 0; x < factionData.CargoShipsEnroute.Count; x++ )
            {
                GameEntity_Squad cargoShip = World_AIW2.Instance.GetEntityByID_Squad( factionData.CargoShipsEnroute[x] );
                if ( cargoShip == null )
                    continue;

                // Load the cargo ship's status.
                CivilianStatus shipStatus = cargoShip.GetCivilianStatusExt( ExternalDataRetrieval.CreateIfNotFound );

                // Heading towards destination station
                // Confirm its destination station still exists.
                GameEntity_Squad destinationStation = World_AIW2.Instance.GetEntityByID_Squad( shipStatus.Destination );

                // If station not found or is crippled, idle the cargo ship.
                if ( destinationStation == null || destinationStation.SecondsSpentAsRemains > 0 )
                {
                    factionData.ChangeCargoShipStatus( cargoShip, Status.Idle );
                    x--;
                    continue;
                }

                // If ship not at destination planet yet, do nothing.
                if ( cargoShip.Planet.Index != destinationStation.Planet.Index )
                    continue;

                // If ship is close to destination station, start unloading.
                if ( cargoShip.GetDistanceTo_ExpensiveAccurate( destinationStation.WorldLocation, true, true ) < 2000 )
                {
                    if ( factionData.TradeStations.Contains( destinationStation.PrimaryKeyID ) )
                    {
                        factionData.ChangeCargoShipStatus( cargoShip, Status.Unloading );
                        shipStatus.LoadTimer = 120;
                    }
                    else if ( factionData.MilitiaLeaders.Contains( destinationStation.PrimaryKeyID ) )
                    {
                        factionData.ChangeCargoShipStatus( cargoShip, Status.Building );
                        shipStatus.LoadTimer = 120;
                    }
                    else
                    {
                        factionData.ChangeCargoShipStatus( cargoShip, Status.Idle );
                    }
                    cargoShip.SetCivilianStatusExt( shipStatus );
                    x--;
                }
            }
            for ( int x = 0; x < factionData.CargoShipsPathing.Count; x++ )
            {
                GameEntity_Squad cargoShip = World_AIW2.Instance.GetEntityByID_Squad( factionData.CargoShipsPathing[x] );
                if ( cargoShip == null )
                    continue;

                // Load the cargo ship's status.
                CivilianStatus shipStatus = cargoShip.GetCivilianStatusExt( ExternalDataRetrieval.CreateIfNotFound );

                // Heading towads origin station.
                // Confirm its origin station still exists.
                GameEntity_Squad originStation = World_AIW2.Instance.GetEntityByID_Squad( shipStatus.Origin );

                // If station not found or is crippled, idle the cargo ship.
                if ( originStation == null || originStation.SecondsSpentAsRemains > 0 )
                {
                    factionData.ChangeCargoShipStatus( cargoShip, Status.Idle );
                    x--;
                    continue;
                }

                // If ship not at origin planet yet, do nothing.
                if ( cargoShip.Planet.Index != originStation.Planet.Index )
                    continue;

                // If ship is close to origin station, start loading.
                if ( cargoShip.GetDistanceTo_ExpensiveAccurate( originStation.WorldLocation, true, true ) < 2000 )
                {
                    factionData.ChangeCargoShipStatus( cargoShip, Status.Loading );
                    shipStatus.LoadTimer = 120;
                    x--;
                    cargoShip.SetCivilianStatusExt( shipStatus );
                }
            }
        }

        // Handle resource transferring.
        public void DoResourceTransfer( Faction faction, ArcenSimContext Context )
        {
            // Loop through every cargo ship.
            for ( int x = 0; x < factionData.CargoShipsLoading.Count; x++ )
            {
                // Get the ship.
                GameEntity_Squad cargoShip = World_AIW2.Instance.GetEntityByID_Squad( factionData.CargoShipsLoading[x] );
                if ( cargoShip == null )
                    continue;

                // Load the cargo ship's status.
                CivilianStatus shipStatus = cargoShip.GetCivilianStatusExt( ExternalDataRetrieval.CreateIfNotFound );

                // Decrease its wait timer.
                shipStatus.LoadTimer--;

                // Load the cargo ship's cargo.
                CivilianCargo shipCargo = cargoShip.GetCivilianCargoExt( ExternalDataRetrieval.CreateIfNotFound );

                // Load the origin station and its cargo.
                GameEntity_Squad originStation = World_AIW2.Instance.GetEntityByID_Squad( shipStatus.Origin );
                // If the station has died or been crippled, free the cargo ship.
                if ( originStation == null || originStation.SecondsSpentAsRemains > 0 )
                {
                    factionData.ChangeCargoShipStatus( cargoShip, Status.Idle );
                    x--;
                    continue;
                }
                CivilianCargo originCargo = originStation.GetCivilianCargoExt( ExternalDataRetrieval.CreateIfNotFound );

                // Load the destination station and its cargo.
                GameEntity_Squad destinationStation = World_AIW2.Instance.GetEntityByID_Squad( shipStatus.Destination );
                // If the station has died or been crippled, free the cargo ship.
                if ( destinationStation == null || destinationStation.SecondsSpentAsRemains > 0 )
                {
                    factionData.ChangeCargoShipStatus( cargoShip, Status.Idle );
                    x--;
                    continue;
                }
                CivilianCargo destinationCargo = destinationStation.GetCivilianCargoExt( ExternalDataRetrieval.CreateIfNotFound );

                // Send the resources, if the station has any left.
                for ( int y = 0; y < (int)CivilianResource.Length; y++ )
                {
                    // If its something that our destination produces, take none, and, in fact, give back if we have some.
                    if ( destinationCargo.PerSecond[y] > 0 )
                    {
                        if ( shipCargo.Amount[y] > 0 && originCargo.Amount[y] < originCargo.Capacity[y] )
                        {
                            shipCargo.Amount[y]--;
                            originCargo.Amount[y]++;
                        }
                    }
                    else
                    {
                        // When spreading resources, we should only spread them once stockpiled high enough.
                        if ( originCargo.PerSecond[y] <= 0 )
                        {
                            if ( destinationStation.TypeData.GetHasTag( "CenterOfTrade" ) && originCargo.Amount[y] < originCargo.Capacity[y] * 0.75 )
                                continue; // Only export after we build up enough resources.
                            if ( originCargo.Amount[y] > 0 && shipCargo.Amount[y] < shipCargo.Capacity[y] )
                            {
                                shipCargo.Amount[y]++;
                                originCargo.Amount[y]--;
                            }
                        }
                        // Otherwise, do Loading logic.
                        else
                        {
                            // Stop if there are no resources left to load, if its a resource the station uses, or if the ship is full.
                            if ( originCargo.Amount[y] <= 0 || originCargo.PerSecond[y] < 0 || shipCargo.Amount[y] >= shipCargo.Capacity[y] )
                                continue;

                            // Transfer a single resource per second.
                            originCargo.Amount[y]--;
                            shipCargo.Amount[y]++;
                        }
                    }
                }

                // If load timer hit 0, see if we should head out.
                if ( shipStatus.LoadTimer <= 0 )
                {
                    // If none of our resources are full, stop.
                    bool hasEnough = false;
                    for ( int y = 0; y < (int)CivilianResource.Length; y++ )
                        if ( shipCargo.Amount[y] >= shipCargo.Capacity[y] )
                        {
                            hasEnough = true;
                            break;
                        }
                    if ( hasEnough )
                    {
                        factionData.ChangeCargoShipStatus( cargoShip, Status.Enroute );
                    }
                    else
                    {
                        factionData.ChangeCargoShipStatus( cargoShip, Status.Idle );
                    }
                    shipStatus.LoadTimer = 0;
                    cargoShip.SetCivilianStatusExt( shipStatus );
                    x--;
                }

                // Save the resources.
                originStation.SetCivilianCargoExt( originCargo );
                cargoShip.SetCivilianCargoExt( shipCargo );
            }
            for ( int x = 0; x < factionData.CargoShipsUnloading.Count; x++ )
            {
                // Get the ship.
                GameEntity_Squad cargoShip = World_AIW2.Instance.GetEntityByID_Squad( factionData.CargoShipsUnloading[x] );
                if ( cargoShip == null )
                    continue;

                // Load the cargo ship's status.
                CivilianStatus shipStatus = cargoShip.GetCivilianStatusExt( ExternalDataRetrieval.CreateIfNotFound );

                // Load the cargo ship's cargo.
                CivilianCargo shipCargo = cargoShip.GetCivilianCargoExt( ExternalDataRetrieval.CreateIfNotFound );

                // Load the destination station and its cargo.
                GameEntity_Squad destinationStation = World_AIW2.Instance.GetEntityByID_Squad( shipStatus.Destination );
                // If the station has died or been crippled, free the cargo ship.
                if ( destinationStation == null || destinationStation.SecondsSpentAsRemains > 0 )
                {
                    factionData.ChangeCargoShipStatus( cargoShip, Status.Idle );
                    x--;
                    continue;
                }
                CivilianCargo destinationCargo = destinationStation.GetCivilianCargoExt( ExternalDataRetrieval.CreateIfNotFound );

                // Send the resources, if the ship has any left.
                // Check for completion as well here.
                bool isFinished = true;
                for ( int y = 0; y < (int)CivilianResource.Length; y++ )
                {
                    // Don't transfer if it produces it.
                    if ( destinationCargo.PerSecond[y] > 0 )
                        continue;

                    // Otherwise, do ship unloading logic.
                    // If empty, do nothing.
                    if ( shipCargo.Amount[y] <= 0 )
                        continue;

                    // If station is full, do nothing.
                    if ( destinationCargo.Amount[y] >= destinationCargo.Capacity[y] )
                        continue;

                    // Transfer a single resource per second.
                    shipCargo.Amount[y]--;
                    destinationCargo.Amount[y]++;
                    isFinished = false;

                }

                // Save the resources.
                destinationStation.SetCivilianCargoExt( destinationCargo );
                cargoShip.SetCivilianCargoExt( shipCargo );

                // If ship finished, have it go back to being Idle.
                if ( isFinished )
                {
                    factionData.ChangeCargoShipStatus( cargoShip, Status.Idle );
                    x--;
                }
            }
            for ( int x = 0; x < factionData.CargoShipsBuilding.Count; x++ )
            {
                // Get the ship.
                GameEntity_Squad cargoShip = World_AIW2.Instance.GetEntityByID_Squad( factionData.CargoShipsBuilding[x] );
                if ( cargoShip == null )
                    continue;

                // Load the cargo ship's status.
                CivilianStatus shipStatus = cargoShip.GetCivilianStatusExt( ExternalDataRetrieval.CreateIfNotFound );

                // Load the cargo ship's cargo.
                CivilianCargo shipCargo = cargoShip.GetCivilianCargoExt( ExternalDataRetrieval.CreateIfNotFound );

                // Load the destination station and its cargo.
                GameEntity_Squad destinationStation = World_AIW2.Instance.GetEntityByID_Squad( shipStatus.Destination );
                // If the station has died or been crippled, free the cargo ship.
                if ( destinationStation == null || destinationStation.SecondsSpentAsRemains > 0 )
                {
                    factionData.ChangeCargoShipStatus( cargoShip, Status.Idle );
                    x--;
                    continue;
                }
                CivilianCargo destinationCargo = destinationStation.GetCivilianCargoExt( ExternalDataRetrieval.CreateIfNotFound );

                // Send the resources, if the ship has any left.
                // Check for completion as well here.
                bool isFinished = true;
                for ( int y = 0; y < (int)CivilianResource.Length; y++ )
                {
                    // If empty, do nothing.
                    if ( shipCargo.Amount[y] <= 0 )
                        continue;

                    // Stop if at capacity.
                    if ( destinationCargo.Amount[y] > destinationCargo.Capacity[y] )
                        continue;

                    // Transfer a single resource per second.
                    shipCargo.Amount[y]--;
                    destinationCargo.Amount[y]++;
                    isFinished = false;
                }

                // Save the resources.
                destinationStation.SetCivilianCargoExt( destinationCargo );
                cargoShip.SetCivilianCargoExt( shipCargo );

                // If ship finished, have it go back to being Idle.
                if ( isFinished )
                {
                    factionData.ChangeCargoShipStatus( cargoShip, Status.Idle );
                    x--;
                }
            }
        }

        // Handle assigning militia to our ThreatReports.
        public void DoMilitiaAssignment( Faction faction, ArcenSimContext Context )
        {
            Planet grandPlanet = factionData.GrandStation?.Planet;
            if ( grandPlanet == null )
                return;

            Engine_Universal.NewTimingsBeingBuilt.StartRememberingFrame( FramePartTimings.TimingType.MainSimThreadNormal, "DoMilitiaAssignment" );

            // Get a list of free militia leaders.
            List<GameEntity_Squad> freeMilitia = new List<GameEntity_Squad>();

            // Find any free militia leaders and add them to our list.
            for ( int x = 0; x < factionData.MilitiaLeaders.Count; x++ )
            {
                GameEntity_Squad militiaLeader = World_AIW2.Instance.GetEntityByID_Squad( factionData.MilitiaLeaders[x] );
                if ( militiaLeader == null )
                    continue;

                CivilianMilitia militiaStatus = militiaLeader.GetCivilianMilitiaExt( ExternalDataRetrieval.CreateIfNotFound );
                if ( militiaStatus == null )
                    continue;

                if ( militiaStatus.Status == CivilianMilitiaStatus.Idle )
                    freeMilitia.Add( militiaLeader );
            }

            // Deal with militia requests.
            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                // If we ran out of free militia, update our request.
                if ( freeMilitia.Count == 0 )
                {
                    factionData.MilitiaCounter += factionData.TradeStations.Count;
                    return DelReturn.Break;
                }

                // Skip if we don't have a post on this  planet.
                GameEntity_Squad foundTradePost = null;

                planet.DoForEntities( "CenterOfTrade", entity =>
                {
                    if ( entity.PlanetFaction.Faction.GetIsFriendlyTowards( faction ) )
                    {
                        foundTradePost = entity;
                        return DelReturn.Break;
                    }

                    return DelReturn.Continue;
                } );

                if ( foundTradePost == null )
                    return DelReturn.Continue;

                // If we're on a trade planet, see if any wormholes are still unassigned.
                GameEntity_Other foundWormhole = null;
                planet.DoForLinkedNeighbors( false, delegate ( Planet otherPlanet )
                {
                    // Get its wormhole.
                    GameEntity_Other wormhole = planet.GetWormholeTo( otherPlanet );
                    if ( wormhole == null )
                        return DelReturn.Continue;

                    // Skip if too close to the post
                    if ( foundTradePost.WorldLocation.GetDistanceTo( wormhole.WorldLocation, true ) <= MinimumOutpostDeploymentRange * 2 )
                        return DelReturn.Continue;

                    // If its not been claimed by another militia, claim it.
                    bool claimed = false;
                    for ( int y = 0; y < factionData.MilitiaLeaders.Count; y++ )
                    {
                        GameEntity_Squad tempSquad = World_AIW2.Instance.GetEntityByID_Squad( factionData.MilitiaLeaders[y] );
                        if ( tempSquad == null )
                            continue;
                        CivilianMilitia tempStatus = tempSquad.GetCivilianMilitiaExt( ExternalDataRetrieval.CreateIfNotFound );
                        if ( tempStatus == null )
                            continue;
                        if ( tempStatus.EntityFocus == wormhole.PrimaryKeyID )
                            claimed = true;
                    }
                    if ( !claimed )
                    {
                        // If its not a hostile wormhole, assign it, but keep trying to find a hostile one.
                        if ( otherPlanet.GetControllingOrInfluencingFaction().GetIsHostileTowards( faction ) )
                        {
                            foundWormhole = wormhole;
                            return DelReturn.Break;
                        }
                        else
                        {
                            foundWormhole = wormhole;
                        }
                    }
                    return DelReturn.Continue;
                } );

                // If no free wormhole, try to find a free mine.
                GameEntity_Squad foundMine = null;
                planet.DoForEntities( EntityRollupType.MetalProducers, delegate ( GameEntity_Squad mineEntity )
                {
                    if ( mineEntity.TypeData.GetHasTag( "MetalGenerator" ) )
                    {
                        bool claimed = false;
                        for ( int y = 0; y < factionData.MilitiaLeaders.Count; y++ )
                        {
                            GameEntity_Squad tempSquad = World_AIW2.Instance.GetEntityByID_Squad( factionData.MilitiaLeaders[y] );
                            if ( tempSquad == null )
                                continue;
                            CivilianMilitia tempStatus = tempSquad.GetCivilianMilitiaExt( ExternalDataRetrieval.CreateIfNotFound );
                            if ( tempStatus == null )
                                continue;
                            if ( tempStatus.EntityFocus == mineEntity.PrimaryKeyID )
                                claimed = true;
                        }
                        if ( !claimed )
                        {
                            foundMine = mineEntity;
                            return DelReturn.Break;
                        }
                    }

                    return DelReturn.Continue;
                } );

                bool advancedShipyardBuilt = false;
                // If no free mine, see if we already have an advanced technology center on the planet, or queued to be built.
                if ( foundTradePost != null )
                    for ( int y = 0; y < factionData.MilitiaLeaders.Count && !advancedShipyardBuilt; y++ )
                    {
                        GameEntity_Squad workingMilitia = World_AIW2.Instance.GetEntityByID_Squad( factionData.MilitiaLeaders[y] );
                        if ( workingMilitia == null )
                            continue;
                        CivilianMilitia workingStatus = workingMilitia.GetCivilianMilitiaExt( ExternalDataRetrieval.CreateIfNotFound );
                        if ( workingStatus == null )
                            continue;
                        if ( (workingMilitia.Planet == planet && workingMilitia.TypeData.GetHasTag( "AdvancedCivilianShipyard" ))
                            || workingStatus.Status == CivilianMilitiaStatus.PathingForShipyard && workingStatus.PlanetFocus == planet.Index )
                            advancedShipyardBuilt = true;
                    }

                // Stop if nothing is free.
                if ( foundWormhole == null && foundMine == null && advancedShipyardBuilt )
                    return DelReturn.Continue;

                // Find the closest militia ship. Default to first in the list.
                GameEntity_Squad militia = freeMilitia[0];
                // If there is at least one more ship, find the closest to our planet, and pick that one.
                if ( freeMilitia.Count > 1 )
                {
                    for ( int y = 1; y < freeMilitia.Count; y++ )
                    {
                        if ( freeMilitia[y].Planet.GetHopsTo( planet ) < militia.Planet.GetHopsTo( planet ) )
                            militia = freeMilitia[y];
                    }
                }

                // Remove our found militia from our list.
                freeMilitia.Remove( militia );

                // Update the militia's status.
                CivilianMilitia militiaStatus = militia.GetCivilianMilitiaExt( ExternalDataRetrieval.CreateIfNotFound );
                if ( militiaStatus == null )
                    return DelReturn.Continue;
                militiaStatus.PlanetFocus = planet.Index;

                // Assign our mine or wormhole.
                if ( foundWormhole != null )
                {
                    militiaStatus.EntityFocus = foundWormhole.PrimaryKeyID;
                    militiaStatus.Status = CivilianMilitiaStatus.PathingForWormhole;
                }
                else if ( foundMine != null )
                {
                    militiaStatus.EntityFocus = foundMine.PrimaryKeyID;
                    militiaStatus.Status = CivilianMilitiaStatus.PathingForMine;
                }
                else if ( foundTradePost != null )
                {
                    militiaStatus.Status = CivilianMilitiaStatus.PathingForShipyard;
                }

                // Save its status.
                militia.SetCivilianMilitiaExt( militiaStatus );

                return DelReturn.Continue;
            } );
            Engine_Universal.NewTimingsBeingBuilt.FinishRememberingFrame( FramePartTimings.TimingType.MainSimThreadNormal, "DoMilitiaAssignment" );
        }

        public void DoMilitiaDeployment( Faction faction, ArcenSimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            Engine_Universal.NewTimingsBeingBuilt.StartRememberingFrame( FramePartTimings.TimingType.MainSimThreadNormal, "DoMilitiaDeployment" );
            // Handle once for each militia leader.
            List<int> toRemove = new List<int>();
            List<int> toAdd = new List<int>();
            List<int> processed = new List<int>();
            for ( int x = 0; x < factionData.MilitiaLeaders.Count; x++ )
            {
                // Load its ship and status.
                GameEntity_Squad militiaShip = World_AIW2.Instance.GetEntityByID_Squad( factionData.MilitiaLeaders[x] );
                if ( militiaShip == null || processed.Contains( militiaShip.PrimaryKeyID ) )
                {
                    factionData.MilitiaLeaders.RemoveAt( x );
                    x--;
                    continue;
                }
                CivilianMilitia militiaStatus = militiaShip.GetCivilianMilitiaExt( ExternalDataRetrieval.CreateIfNotFound );
                CivilianCargo militiaCargo = militiaShip.GetCivilianCargoExt( ExternalDataRetrieval.CreateIfNotFound );
                if ( militiaStatus.Status != CivilianMilitiaStatus.Defending && militiaStatus.Status != CivilianMilitiaStatus.Patrolling )
                {
                    // Load its goal.
                    GameEntity_Squad goalStation = null;
                    // Get its planet.
                    Planet planet = World_AIW2.Instance.GetPlanetByIndex( militiaStatus.PlanetFocus );
                    // If planet not found, and not already deployed, idle the militia ship.
                    if ( planet == null && militiaShip.TypeData.IsMobile )
                    {
                        militiaStatus.Status = CivilianMilitiaStatus.Idle;
                        militiaShip.SetCivilianMilitiaExt( militiaStatus );
                        continue;
                    }
                    // Skip if not at planet yet.
                    if ( militiaShip.Planet.Index != militiaStatus.PlanetFocus )
                        continue;
                    // Get its goal's station.
                    planet.DoForEntities( "CenterOfTrade", delegate ( GameEntity_Squad entity )
                    {
                        // If we find its index in our records, thats our goal station.
                        if ( factionData.TradeStations.Contains( entity.PrimaryKeyID ) )
                        {
                            goalStation = entity;
                            return DelReturn.Break;
                        }

                        return DelReturn.Continue;
                    } );
                    // If goal station not found, and not already deployed, idle the militia ship.
                    if ( goalStation == null && militiaShip.TypeData.IsMobile )
                    {
                        militiaStatus.Status = CivilianMilitiaStatus.Idle;
                        militiaShip.SetCivilianMilitiaExt( militiaStatus );
                        continue;
                    }

                    // If pathing, check for arrival.
                    if ( militiaStatus.Status == CivilianMilitiaStatus.PathingForMine )
                    {
                        // If nearby, advance status.
                        if ( militiaShip.GetDistanceTo_ExpensiveAccurate( goalStation.WorldLocation, true, true ) < 500 )
                        {
                            militiaStatus.Status = CivilianMilitiaStatus.EnrouteMine;
                        }
                    }
                    else if ( militiaStatus.Status == CivilianMilitiaStatus.PathingForWormhole )
                    {
                        // If nearby, advance status.
                        if ( militiaShip.GetDistanceTo_ExpensiveAccurate( goalStation.WorldLocation, true, true ) < 500 )
                        {
                            militiaStatus.Status = CivilianMilitiaStatus.EnrouteWormhole;
                        }
                    }
                    else if ( militiaStatus.Status == CivilianMilitiaStatus.PathingForShipyard )
                    {
                        if ( militiaShip.GetDistanceTo_ExpensiveAccurate( goalStation.WorldLocation, true, true ) < 500 )
                        {
                            // Prepare its old id to be removed.
                            toRemove.Add( militiaShip.PrimaryKeyID );

                            // Converting to an Advanced Civilian Shipyard, upgrade the fleet status to a mobile patrol status.
                            militiaStatus.Status = CivilianMilitiaStatus.Patrolling;

                            // Load its station data.
                            GameEntityTypeData outpostData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AdvancedCivilianShipyard" );

                            // Transform it.
                            GameEntity_Squad newMilitiaShip = militiaShip.TransformInto( Context, outpostData, 1 );

                            // Make sure its not overlapping.
                            newMilitiaShip.SetWorldLocation( newMilitiaShip.Planet.GetSafePlacementPoint( Context, outpostData, newMilitiaShip.WorldLocation, 0, 1000 ) );

                            // Update centerpiece to it.
                            militiaStatus.Centerpiece = newMilitiaShip.PrimaryKeyID;

                            // Move the information to our new ship.
                            newMilitiaShip.SetCivilianMilitiaExt( militiaStatus );

                            // Prepare its new id to be added.
                            toAdd.Add( newMilitiaShip.PrimaryKeyID );
                        }
                    }
                    // If enroute, check for sweet spot.
                    if ( militiaStatus.Status == CivilianMilitiaStatus.EnrouteWormhole )
                    {
                        if ( militiaStatus.getWormhole() == null )
                        {
                            militiaStatus.Status = CivilianMilitiaStatus.Idle;
                            militiaShip.SetCivilianMilitiaExt( militiaStatus );
                            continue;
                        }
                        int stationDist = militiaShip.GetDistanceTo_ExpensiveAccurate( goalStation.WorldLocation, true, true );
                        int wormDist = militiaShip.GetDistanceTo_ExpensiveAccurate( militiaStatus.getWormhole().WorldLocation, true, true );
                        int range = MinimumOutpostDeploymentRange;
                        if ( stationDist > range || wormDist < range )
                        {
                            // Prepare its old id to be removed.
                            toRemove.Add( militiaShip.PrimaryKeyID );

                            // Optimal distance. Transform the ship and update its status.
                            militiaStatus.Status = CivilianMilitiaStatus.Defending;

                            // Load its station data.
                            GameEntityTypeData outpostData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MilitiaOutpost" );

                            // Transform it.
                            GameEntity_Squad newMilitiaShip = militiaShip.TransformInto( Context, outpostData, 1 );

                            // Update centerpiece to it.
                            militiaStatus.Centerpiece = newMilitiaShip.PrimaryKeyID;

                            // Move the information to our new ship.
                            newMilitiaShip.SetCivilianMilitiaExt( militiaStatus );

                            // Prepare its new id to be added.
                            toAdd.Add( newMilitiaShip.PrimaryKeyID );
                        }
                    }
                    // If enroute, check for sweet spot.
                    else if ( militiaStatus.Status == CivilianMilitiaStatus.EnrouteMine )
                    {
                        if ( militiaStatus.getMine() == null )
                        {
                            militiaStatus.Status = CivilianMilitiaStatus.Idle;
                            militiaShip.SetCivilianMilitiaExt( militiaStatus );
                            continue;
                        }
                        int mineDist = militiaShip.GetDistanceTo_ExpensiveAccurate( militiaStatus.getMine().WorldLocation, true, true );
                        int range = 1000;
                        if ( mineDist < range )
                        {
                            // Prepare its old id to be removed.
                            toRemove.Add( militiaShip.PrimaryKeyID );

                            // Converting to a Patrol Post, upgrade the fleet status to a mobile patrol status.
                            militiaStatus.Status = CivilianMilitiaStatus.Patrolling;

                            // Load its station data.
                            GameEntityTypeData outpostData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MilitiaPatrolPost" );

                            // Transform it.
                            GameEntity_Squad newMilitiaShip = militiaShip.TransformInto( Context, outpostData, 1 );

                            // Make sure its not overlapping.
                            newMilitiaShip.SetWorldLocation( newMilitiaShip.Planet.GetSafePlacementPoint( Context, outpostData, newMilitiaShip.WorldLocation, 0, 1000 ) );

                            // Update centerpiece to it.
                            militiaStatus.Centerpiece = newMilitiaShip.PrimaryKeyID;

                            // Move the information to our new ship.
                            newMilitiaShip.SetCivilianMilitiaExt( militiaStatus );

                            // Prepare its new id to be added.
                            toAdd.Add( newMilitiaShip.PrimaryKeyID );
                        }
                    }
                }
                else if ( militiaStatus.Status == CivilianMilitiaStatus.Defending ) // Do turret spawning.
                {
                    if ( militiaStatus.getWormhole() == null )
                    {
                        militiaShip.Die( Context, true );
                        continue;
                    }
                    // For each type of unit, process.
                    for ( int y = 0; y < (int)CivilianResource.Length; y++ )
                    {
                        if ( militiaCargo.Amount[y] <= 0 )
                            continue;

                        // Skip if we're under the minimum tech requirement.
                        if ( IgnoreResource[y] )
                            continue;

                        if ( militiaStatus.ShipTypeDataNames[y] == "none" )
                        {
                            // Get our tag to search for based on resource type.
                            string typeTag = "Civ" + ((CivilianTech)y).ToString() + "Turret";
                            // Attempt to find entitydata for our type.
                            if ( GameEntityTypeDataTable.Instance.RowsByTag.GetHasKey( typeTag ) )
                            {
                                GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, typeTag );
                                if ( typeData != null )
                                    militiaStatus.ShipTypeDataNames[y] = typeData.InternalName;
                            }
                            else
                            {
                                // No matching tag; get a random turret type.
                                GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "CivTurret" );
                                if ( typeData != null )
                                    militiaStatus.ShipTypeDataNames[y] = typeData.InternalName;
                            }
                        }

                        if ( !militiaStatus.ShipTypeData.GetHasKey( y ) )
                            militiaStatus.ShipTypeData.AddPair( y, GameEntityTypeDataTable.Instance.GetRowByName( militiaStatus.ShipTypeDataNames[y] ) );

                        GameEntityTypeData turretData = militiaStatus.ShipTypeData[y];

                        int count = militiaStatus.GetShipCount( turretData );
                        if ( count < militiaStatus.ShipCapacity[y] )
                        {
                            int baseCost = turretData.CostForAIToPurchase;
                            int cost = (CostIntensityModifier( faction ) * baseCost).GetNearestIntPreferringHigher();

                            if ( militiaCargo.Capacity[y] < cost )
                                militiaCargo.Capacity[y] = (int)(cost * MilitiaStockpilePercentage); // Stockpile some resources.

                            if ( militiaCargo.Amount[y] >= cost )
                            {
                                // Remove cost.
                                militiaCargo.Amount[y] -= cost;
                                // Spawn turret.
                                // Get a focal point directed towards the wormhole.
                                ArcenPoint basePoint = militiaShip.WorldLocation.GetPointAtAngleAndDistance( militiaShip.WorldLocation.GetAngleToDegrees( militiaStatus.getWormhole().WorldLocation ), Math.Min( 5000, militiaShip.GetDistanceTo_ExpensiveAccurate( militiaStatus.getWormhole().WorldLocation, true, true ) / 2 ) );
                                // Get a point around it, as close as possible.
                                ArcenPoint spawnPoint = basePoint.GetRandomPointWithinDistance( Context.RandomToUse, Math.Min( 500, militiaShip.GetDistanceTo_ExpensiveAccurate( militiaStatus.getWormhole().WorldLocation, true, true ) / 4 ), Math.Min( 2500, militiaShip.GetDistanceTo_ExpensiveAccurate( militiaStatus.getWormhole().WorldLocation, true, true ) / 2 ) );

                                // Get the planet faction to spawn it in as.
                                PlanetFaction pFaction = militiaShip.Planet.GetPlanetFactionForFaction( faction );

                                // Spawn in the ship.
                                GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, turretData, faction.GetGlobalMarkLevelForShipLine( turretData ), pFaction.FleetUsedAtPlanet, 0, spawnPoint, Context );

                                // Only let it stack with its own fleet.
                                entity.MinorFactionStackingID = militiaShip.PrimaryKeyID;

                                // Add the turret to our militia's fleet.
                                militiaStatus.Ships[y].Add( entity.PrimaryKeyID );
                            }
                        }
                        else if ( count > militiaStatus.ShipCapacity[y] && militiaStatus.Ships[y].Count > 0 )
                        {
                            GameEntity_Squad squad = World_AIW2.Instance.GetEntityByID_Squad( militiaStatus.Ships[y][0] );
                            if ( squad == null )
                                militiaStatus.Ships[y].RemoveAt( 0 );
                            else
                            {
                                squad.Despawn( Context, true, InstancedRendererDeactivationReason.SelfDestructOnTooHighOfCap );
                                militiaStatus.Ships[y].RemoveAt( 0 );
                            }
                        }
                    }
                }
                else if ( militiaStatus.Status == CivilianMilitiaStatus.Patrolling ) // If patrolling, do unit spawning.
                {
                    // Reset strength bonus.
                    militiaShip.AdditionalStrengthFromFactions = 0;
                    // For each type of unit, get ship count.
                    for ( int y = 0; y < (int)CivilianResource.Length; y++ )
                    {
                        if ( militiaCargo.Amount[y] <= 0 )
                            continue;

                        // Skip if we're under the minimum tech requirement.
                        if ( IgnoreResource[y] )
                            continue;

                        // If we're an advanced shipyard, use alternate logic.
                        bool buildingProtectors = false;
                        if ( militiaShip.TypeData.GetHasTag( "BuildsProtectors" ) )
                            buildingProtectors = true;

                        if ( militiaStatus.ShipTypeDataNames[y] == "none" )
                        {
                            // Get our tag to search for based on resource type.
                            string typeTag = "Civ" + ((CivilianTech)y).ToString() + "Mobile";
                            if ( buildingProtectors )
                                typeTag = "Civ" + ((CivilianTech)y).ToString() + "Protector";
                            // Attempt to find entitydata for our type.
                            if ( GameEntityTypeDataTable.Instance.RowsByTag.GetHasKey( typeTag ) )
                            {
                                GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, typeTag );
                                if ( typeData != null )
                                    militiaStatus.ShipTypeDataNames[y] = typeData.InternalName;
                            }
                            else
                            {
                                // No matching tag; get a random turret type.
                                GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "CivMobile" );
                                if ( typeData != null )
                                    militiaStatus.ShipTypeDataNames[y] = typeData.InternalName;
                            }

                        }

                        if ( !militiaStatus.ShipTypeData.GetHasKey( y ) )
                            militiaStatus.ShipTypeData.AddPair( y, GameEntityTypeDataTable.Instance.GetRowByName( militiaStatus.ShipTypeDataNames[y] ) );

                        GameEntityTypeData shipData = militiaStatus.ShipTypeData[y];

                        // Update strength to account for any stored entities.
                        if ( militiaStatus.StoredShips[y] > 0 )
                            militiaShip.AdditionalStrengthFromFactions = militiaStatus.StoredShips[y] * shipData.GetForMark( faction.GetGlobalMarkLevelForShipLine( shipData ) ).StrengthPerSquad_CalculatedWithNullFleetMembership;

                        int count = militiaStatus.GetShipCount( shipData );
                        if ( count < militiaStatus.ShipCapacity[y] )
                        {
                            int cost = 0;
                            if ( buildingProtectors )
                                cost = (int)(7000 * CostIntensityModifier( faction ));
                            else
                            {
                                int baseCost = shipData.CostForAIToPurchase;
                                cost = (CostIntensityModifier( faction ) * baseCost).GetNearestIntPreferringHigher();
                            }

                            if ( militiaCargo.Capacity[y] < cost )
                                militiaCargo.Capacity[y] = (int)(cost * MilitiaStockpilePercentage); // Stockpile some resources.

                            if ( militiaCargo.Amount[y] >= cost )
                            {
                                // Remove cost.
                                militiaCargo.Amount[y] -= cost;

                                // If we're AtEase, simply add to our internal count.
                                if ( militiaStatus.AtEase )
                                    militiaStatus.StoredShips[y]++;
                                else
                                {
                                    // Get the planet faction to spawn it in as.
                                    PlanetFaction pFaction = militiaShip.Planet.GetPlanetFactionForFaction( faction );

                                    // Spawn in the ship.
                                    GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, shipData, faction.GetGlobalMarkLevelForShipLine( shipData ), pFaction.FleetUsedAtPlanet, 0, militiaShip.WorldLocation, Context );

                                    // Only let it stack with its own fleet.
                                    entity.MinorFactionStackingID = militiaShip.PrimaryKeyID;

                                    // Make it attack nearby hostiles.
                                    entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );

                                    // Add the turret to our militia's fleet.
                                    militiaStatus.Ships[y].Add( entity.PrimaryKeyID );
                                }
                            }
                        }
                        else if ( count > militiaStatus.ShipCapacity[y] )
                        {
                            if ( militiaStatus.AtEase )
                                militiaStatus.StoredShips[y]--;
                            else if ( militiaStatus.Ships[y].Count > 0 )
                            {
                                GameEntity_Squad squad = World_AIW2.Instance.GetEntityByID_Squad( militiaStatus.Ships[y][0] );
                                if ( squad == null )
                                    militiaStatus.Ships[y].RemoveAt( 0 );
                                else
                                {
                                    squad.Despawn( Context, true, InstancedRendererDeactivationReason.SelfDestructOnTooHighOfCap );
                                    militiaStatus.Ships[y].RemoveAt( 0 );
                                }
                            }
                        }

                        // If we're active and have some stored ships, slowly release them.
                        if ( !militiaStatus.AtEase && militiaStatus.StoredShips[y] > 0 )
                        {
                            // Get the planet faction to spawn it in as.
                            PlanetFaction pFaction = militiaShip.Planet.GetPlanetFactionForFaction( faction );

                            // Spawn in the ship.
                            GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, shipData, faction.GetGlobalMarkLevelForShipLine( shipData ), pFaction.FleetUsedAtPlanet, 0, militiaShip.WorldLocation, Context );

                            // Only let it stack with its own fleet.
                            entity.MinorFactionStackingID = militiaShip.PrimaryKeyID;

                            // Make it attack nearby hostiles.
                            entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );

                            // Add the turret to our militia's fleet.
                            militiaStatus.Ships[y].Add( entity.PrimaryKeyID );

                            // Remove it from our internal count.
                            militiaStatus.StoredShips[y]--;
                        }
                    }
                }

                processed.Add( militiaShip.PrimaryKeyID );
            }
            for ( int x = 0; x < toRemove.Count; x++ )
            {
                factionData.MilitiaLeaders.Remove( toRemove[x] );
                factionData.MilitiaLeaders.Add( toAdd[x] );
            }
            Engine_Universal.NewTimingsBeingBuilt.FinishRememberingFrame( FramePartTimings.TimingType.MainSimThreadNormal, "DoMilitiaDeployment" );
        }

        // AI response warning.
        public void PrepareAIRaid( Faction faction, ArcenSimContext Context )
        {
            // If no trade stations, put off the raid.
            if ( factionData.TradeStations == null || factionData.TradeStations.Count == 0 )
            {
                factionData.NextRaidInThisSeconds = 600;
                return;
            }

            // Pick a random trade station.
            GameEntity_Squad targetStation = World_AIW2.Instance.GetEntityByID_Squad( factionData.TradeStations[Context.RandomToUse.Next( factionData.TradeStations.Count )] );
            if ( targetStation != null )
            {
                Faction aifaction = World_AIW2.GetRandomAIFaction( Context );
                if ( aifaction == null )
                    return;

                // Set up raiding wormholes.
                targetStation.Planet.DoForLinkedNeighborsAndSelf( false, delegate ( Planet planet )
                {
                    // Toss down a wormhole on each planet.
                    GameEntity_Squad wormhole = planet.Mapgen_SeedEntity( Context, aifaction, GameEntityTypeDataTable.Instance.GetRowByName( "AIRaidingWormhole" ), PlanetSeedingZone.OuterSystem );

                    if ( wormhole != null )
                        factionData.NextRaidWormholes.Add( wormhole.PrimaryKeyID );

                    return DelReturn.Continue;
                } );
                if ( PlayerAligned )
                    World_AIW2.Instance.QueueChatMessageOrCommand( $"The AI is preparing to raid cargo ships on planets near {targetStation.Planet.Name}.", ChatType.LogToCentralChat, Context );

                // Start timer.
                factionData.NextRaidInThisSeconds = 299;
            }
        }

        // AI response.
        public void SpawnRaidGroup( GameEntity_Squad target, List<int> availableWormholes, ref List<int> attackedTargets, ref int raidBudget, Faction aiFaction, Faction faction, ArcenSimContext Context )
        {
            // Get a random wormhole thats on the target's planet.
            List<GameEntity_Squad> validWormholes = new List<GameEntity_Squad>();
            for ( int x = 0; x < availableWormholes.Count; x++ )
            {
                GameEntity_Squad wormhole = World_AIW2.Instance.GetEntityByID_Squad( availableWormholes[x] );
                if ( wormhole != null && wormhole.Planet.Index == target.Planet.Index )
                    validWormholes.Add( wormhole );
            }
            if ( validWormholes.Count <= 0 )
                return; // No valid wormholes for this target.
            GameEntity_Squad wormholeToSpawnAt = validWormholes[Context.RandomToUse.Next( validWormholes.Count )];
            if ( target != null && target.Planet == wormholeToSpawnAt.Planet && !attackedTargets.Contains( target.PrimaryKeyID ) )
            {
                int thisBudget = 2500;
                int spentBudget = 0;
                // Spawn random fast ships that the ai is allowed to have.
                List<GameEntityTypeData> shipTypes = new List<GameEntityTypeData>();
                FactionUtilityMethods.getEntitesInAIShipGroup( AIShipGroupTable.Instance.GetRowByName( "SneakyStrikecraft" ), shipTypes );

                List<GameEntity_Squad> spawntRaidShips = new List<GameEntity_Squad>();
                ArcenSparseLookup<GameEntityTypeData, int> raidingShips = new ArcenSparseLookup<GameEntityTypeData, int>();
                int attempts = 100;
                while ( spentBudget < thisBudget && attempts > 0 )
                {
                    GameEntityTypeData workingType = shipTypes[Context.RandomToUse.Next( shipTypes.Count )];
                    if ( !raidingShips.GetHasKey( workingType ) )
                        raidingShips.AddPair( workingType, 1 );
                    else
                        raidingShips[workingType]++;
                    spentBudget += workingType.CostForAIToPurchase;
                    attempts--;
                }
                BaseAIFaction.DeployComposition( Context, aiFaction, null, faction.FactionIndex, raidingShips,
                    ref spawntRaidShips, wormholeToSpawnAt.WorldLocation, wormholeToSpawnAt.Planet );

                for ( int shipCount = 0; shipCount < spawntRaidShips.Count; shipCount++ )
                {
                    spawntRaidShips[shipCount].ExoGalacticAttackTarget = SquadWrapper.Create( target );

                    spawntRaidShips[shipCount].ExoGalacticAttackPlanetIdx = target.Planet.Index; //set the planet index so that AI long term planning knows we are in an Exo
                }

                GameCommand speedCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.CreateSpeedGroup_ExoAttack], GameCommandSource.AnythingElse );
                for ( int shipCount = 0; shipCount < spawntRaidShips.Count; shipCount++ )
                    speedCommand.RelatedEntityIDs.Add( spawntRaidShips[shipCount].PrimaryKeyID );
                int exoGroupSpeed = 2200;
                speedCommand.RelatedBool = true;
                speedCommand.RelatedIntegers.Add( exoGroupSpeed );
                World_AIW2.Instance.QueueGameCommand( speedCommand, false );

                attackedTargets.Add( target.PrimaryKeyID );

                raidBudget -= spentBudget;
            }
        }
        public void DoAIRaid( Faction faction, ArcenSimContext Context )
        {
            if ( factionData.NextRaidWormholes.Count == 0 )
            {
                PrepareAIRaid( faction, Context );
                return;
            }
            Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );

            // Don't attempt to send multiple fleets after a single target.
            List<int> attackedTargets = new List<int>();

            // Raid strength increases based on the AI's normal wave budget, increased by the number of trade stations we have.
            int timeFactor = 900; // Minimum delay between raid waves.
            int budgetFactor = SpecialFaction_AI.Instance.GetSpecificBudgetAIPurchaseCostGainPerSecond( aiFaction, AIBudgetType.Wave, true, true ).GetNearestIntPreferringHigher();
            int tradeFactor = factionData.TradeStations.Count * 2;
            FInt intensityMult = FInt.FromParts( 0, 500 ) + ((FInt.FromParts( 0, 050 ) * faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity));
            int raidBudget = ((budgetFactor + tradeFactor) * timeFactor * intensityMult).GetNearestIntPreferringHigher();

            // Stop once we're over budget. (Though allow our last wave to exceed it if needed.)
            for ( int x = 0; x < factionData.CargoShips.Count && raidBudget > 0; x++ )
            {
                GameEntity_Squad target = World_AIW2.Instance.GetEntityByID_Squad( factionData.CargoShips[x] );
                if ( target != null )
                    SpawnRaidGroup( target, factionData.NextRaidWormholes, ref attackedTargets, ref raidBudget, aiFaction, faction, Context );
            }


            // Let the player know they're about to lose money.
            if ( PlayerAligned )
                World_AIW2.Instance.QueueChatMessageOrCommand( "The AI has begun their raid.", ChatType.LogToCentralChat, Context );

            // Reset raid information.
            factionData.NextRaidInThisSeconds = 1800;
            for ( int x = 0; x < factionData.NextRaidWormholes.Count; x++ )
            {
                GameEntity_Squad wormhole = World_AIW2.Instance.GetEntityByID_Squad( factionData.NextRaidWormholes[x] );
                if ( wormhole != null )
                    wormhole.Despawn( Context, true, InstancedRendererDeactivationReason.IFinishedMyJob );
            }
            factionData.NextRaidWormholes = new List<int>();
        }

        // Handle station requests.
        private const int BASE_MIL_URGENCY = 20;
        private const int MIL_URGENCY_REDUCTION_PER_REGULAR = 8;
        private const int MIL_URGENCY_REDUCTION_PER_LARGE = 4;

        private const int BASE_CIV_URGENCY = 5;
        private const int CIV_URGENCY_REDUCTION_PER_REGULAR = 1;
        public void DoTradeRequests( Faction faction, ArcenSimContext Context )
        {
            Engine_Universal.NewTimingsBeingBuilt.StartRememberingFrame( FramePartTimings.TimingType.MainSimThreadNormal, "DoTradeRequests" );

            #region Preparation
            // Clear our lists.
            factionData.ImportRequests = new List<TradeRequest>();
            factionData.ExportRequests = new List<TradeRequest>();

            // Preload two lists, one for ships' origin station, and one for ships' destination station.
            ArcenSparseLookup<int, int> AnsweringImport = new ArcenSparseLookup<int, int>();
            ArcenSparseLookup<int, int> AnsweringExport = new ArcenSparseLookup<int, int>();

            ProcessTradingResponse( factionData.CargoShipsPathing, ref AnsweringImport, ref AnsweringExport );
            ProcessTradingResponse( factionData.CargoShipsLoading, ref AnsweringImport, ref AnsweringExport );
            ProcessTradingResponse( factionData.CargoShipsEnroute, ref AnsweringImport );
            ProcessTradingResponse( factionData.CargoShipsUnloading, ref AnsweringImport );
            ProcessTradingResponse( factionData.CargoShipsBuilding, ref AnsweringImport );
            #endregion

            #region Planet Level Trading
            // See if any militia stations don't have a trade in progress.
            for ( int x = 0; x < factionData.MilitiaLeaders.Count; x++ )
            {
                GameEntity_Squad militia = World_AIW2.Instance.GetEntityByID_Squad( factionData.MilitiaLeaders[x] );
                if ( militia == null )
                {
                    factionData.MilitiaLeaders.RemoveAt( x );
                    x--;
                    continue;
                }

                if ( militia.GetCivilianMilitiaExt( ExternalDataRetrieval.CreateIfNotFound ).Status != CivilianMilitiaStatus.Defending && militia.GetCivilianMilitiaExt( ExternalDataRetrieval.CreateIfNotFound ).Status != CivilianMilitiaStatus.Patrolling )
                    continue;

                // Don't request resources we're full on, or that we are ignoring.
                List<CivilianResource> toIgnore = new List<CivilianResource>();
                CivilianCargo militiaCargo = militia.GetCivilianCargoExt( ExternalDataRetrieval.CreateIfNotFound );
                for ( int y = 0; y < militiaCargo.Amount.Length; y++ )
                    if ( IgnoreResource[y] || (militiaCargo.Amount[y] > 0 && militiaCargo.Amount[y] >= militiaCargo.Capacity[y]) )
                        toIgnore.Add( (CivilianResource)y );

                // Stop if we're full of everything.
                if ( toIgnore.Count == (int)CivilianResource.Length )
                    continue;

                int incoming = AnsweringImport.GetHasKey( militia.PrimaryKeyID ) ? AnsweringImport[militia.PrimaryKeyID] : 0;
                int urgency = BASE_MIL_URGENCY;
                if ( militia.TypeData.GetHasTag( "BuildsProtectors" ) ) // Allow more inbound ships for larger projects.
                    urgency -= MIL_URGENCY_REDUCTION_PER_LARGE * incoming;
                else
                    urgency -= MIL_URGENCY_REDUCTION_PER_REGULAR * incoming;

                // Add a request for any resource.
                factionData.ImportRequests.Add( new TradeRequest( CivilianResource.Length, toIgnore, urgency, militia, 0 ) );
            }
            #endregion

            #region Trade Station Imports and Exports

            // Populate our list with trade stations.
            for ( int x = 0; x < factionData.TradeStations.Count; x++ )
            {
                GameEntity_Squad requester = World_AIW2.Instance.GetEntityByID_Squad( factionData.TradeStations[x] );
                if ( requester == null )
                {
                    // Remove invalid ResourcePoints.
                    factionData.TradeStations.RemoveAt( x );
                    x--;
                    continue;
                }
                if ( requester.SecondsSpentAsRemains > 0 )
                    continue; // Skip crippled stations.
                CivilianCargo requesterCargo = requester.GetCivilianCargoExt( ExternalDataRetrieval.CreateIfNotFound );
                if ( requesterCargo == null )
                    continue;

                // Check each type of cargo seperately.
                for ( int y = 0; y < requesterCargo.PerSecond.Length; y++ )
                {
                    // Skip if we don't accept it.
                    if ( requesterCargo.Capacity[y] <= 0 )
                        continue;

                    // Skip if we're supposed to.
                    if ( IgnoreResource[y] )
                        continue;

                    // Resources we generate.
                    if ( requesterCargo.PerSecond[y] > 0 )
                    {
                        // Generates urgency based on how close to full capacity we are.
                        if ( requesterCargo.Amount[y] > 100 )
                        {
                            // Absolute max export cap is the per second generation times 5.
                            // This may cause some shortages, but that fits in with the whole trading theme so is a net positive regardless.
                            int incoming = AnsweringExport.GetHasKey( requester.PrimaryKeyID ) ? AnsweringExport[requester.PrimaryKeyID] : 0;
                            int urgency = ((int)Math.Ceiling( ((500.0 + requesterCargo.Amount[y]) / requesterCargo.Capacity[y]) * (requesterCargo.PerSecond[y] * 5) )) - incoming;

                            if ( urgency > 0 )
                                factionData.ExportRequests.Add( new TradeRequest( (CivilianResource)y, urgency, requester, 4 ) );
                        }
                    }
                    // Resource we store. Simply put out a super tiny order to import/export based on current stores.
                    else if ( requesterCargo.Amount[y] >= requesterCargo.Capacity[y] * 0.75 )
                    {
                        int incoming = AnsweringExport.GetHasKey( requester.PrimaryKeyID ) ? AnsweringExport[requester.PrimaryKeyID] : 0;
                        int urgency = BASE_CIV_URGENCY;
                        urgency -= incoming * CIV_URGENCY_REDUCTION_PER_REGULAR;

                        if ( urgency > 0 )
                            factionData.ExportRequests.Add( new TradeRequest( (CivilianResource)y, 0, requester, 2 ) );
                    }
                    else
                    {
                        int incoming = AnsweringImport.GetHasKey( requester.PrimaryKeyID ) ? AnsweringImport[requester.PrimaryKeyID] : 0;
                        int urgency = BASE_CIV_URGENCY;
                        urgency -= incoming * CIV_URGENCY_REDUCTION_PER_REGULAR;

                        factionData.ImportRequests.Add( new TradeRequest( (CivilianResource)y, urgency, requester, 4 ) );
                    }

                }
            }

            #endregion

            // If no import or export requests, stop.
            if ( factionData.ImportRequests.Count == 0 || factionData.ExportRequests.Count == 0 )
            {
                Engine_Universal.NewTimingsBeingBuilt.FinishRememberingFrame( FramePartTimings.TimingType.MainSimThreadNormal, "DoTradeRequests" );
                return;
            }

            // Sort our lists.
            factionData.ImportRequests.Sort();
            factionData.ExportRequests.Sort();

            #region Execute Trade

            // While we have free ships left, assign our requests away.
            for ( int x = 0; x < factionData.ImportRequests.Count && factionData.CargoShipsIdle.Count > 0; x++ )
            {
                // If no free cargo ships, stop.
                if ( factionData.CargoShipsIdle.Count == 0 )
                    break;
                TradeRequest importRequest = factionData.ImportRequests[x];
                // If processed, remove.
                if ( importRequest.Processed == true )
                {
                    factionData.ImportRequests.RemoveAt( x );
                    x--;
                    continue;
                }
                int requestedMaxHops = importRequest.MaxSearchHops;
                GameEntity_Squad requestingEntity = importRequest.Station;
                if ( requestingEntity == null )
                {
                    factionData.ImportRequests.RemoveAt( x );
                    x--;
                    continue;
                }
                // Get a free cargo ship, prefering nearest.
                GameEntity_Squad foundCargoShip = null;
                for ( int y = 0; y < factionData.CargoShipsIdle.Count; y++ )
                {
                    GameEntity_Squad cargoShip = World_AIW2.Instance.GetEntityByID_Squad( factionData.CargoShipsIdle[y] );
                    if ( cargoShip == null )
                    {
                        factionData.RemoveCargoShip( factionData.CargoShipsIdle[y] );
                        y--;
                        continue;
                    }
                    // If few enough hops away for this attempt, assign.
                    if ( foundCargoShip == null || cargoShip.Planet.GetHopsTo( requestingEntity.Planet ) <= foundCargoShip.Planet.GetHopsTo( requestingEntity.Planet ) )
                    {
                        foundCargoShip = cargoShip;
                        continue;
                    }
                }
                if ( foundCargoShip == null )
                    continue;
                // If the cargo ship over 75% of the resource already on it, skip the origin station search, and just have it start heading right towards our requesting station.
                bool hasEnough = false;
                CivilianCargo foundCargo = foundCargoShip.GetCivilianCargoExt( ExternalDataRetrieval.CreateIfNotFound );
                for ( int y = 0; y < (int)CivilianResource.Length; y++ )
                    if ( (importRequest.Requested == CivilianResource.Length && !importRequest.Declined.Contains( (CivilianResource)y )) || importRequest.Requested == (CivilianResource)y )
                        if ( foundCargo.Amount[y] > foundCargo.Capacity[y] * 0.75 )
                        {
                            hasEnough = true;
                            break;
                        }

                if ( hasEnough )
                {
                    CivilianStatus cargoShipStatus = foundCargoShip.GetCivilianStatusExt( ExternalDataRetrieval.CreateIfNotFound );
                    cargoShipStatus.Origin = -1;    // No origin station required.
                    cargoShipStatus.Destination = requestingEntity.PrimaryKeyID;
                    factionData.ChangeCargoShipStatus( foundCargoShip, Status.Enroute );

                    // Remove the completed entities from processing.
                    importRequest.Processed = true;
                    continue;
                }

                // Find a trade request of the same resource type and opposing Import/Export status thats within our hop limit.
                GameEntity_Squad otherStation = null;
                TradeRequest otherRequest = null;
                for ( int z = 0; z < factionData.ExportRequests.Count; z++ )
                {
                    // Skip if same.
                    if ( x == z )
                        continue;
                    TradeRequest exportRequest = factionData.ExportRequests[z];
                    // If processed, skip.
                    if ( exportRequest.Processed )
                        continue;
                    int otherRequestedMaxHops = exportRequest.MaxSearchHops;

                    if ( (importRequest.Requested == exportRequest.Requested // Matching request.
                        || (importRequest.Requested == CivilianResource.Length && !importRequest.Declined.Contains( exportRequest.Requested ))) // Export has a resource import accepts.
                      && importRequest.Station.Planet.GetHopsTo( exportRequest.Station.Planet ) <= Math.Min( requestedMaxHops, otherRequestedMaxHops ) )
                    {
                        otherStation = exportRequest.Station;
                        otherRequest = exportRequest;
                        break;
                    }
                }
                if ( otherStation != null )
                {
                    CivilianStatus cargoShipStatus = foundCargoShip.GetCivilianStatusExt( ExternalDataRetrieval.CreateIfNotFound );
                    cargoShipStatus.Origin = otherStation.PrimaryKeyID;
                    cargoShipStatus.Destination = requestingEntity.PrimaryKeyID;
                    factionData.ChangeCargoShipStatus( foundCargoShip, Status.Pathing );

                    // Remove the completed entities from processing.
                    importRequest.Processed = true;
                    if ( otherRequest != null )
                        otherRequest.Processed = true;
                }
            }

            // If we've finished due to not having enough trade ships, request more cargo ships.
            if ( factionData.ImportRequests.Count > 0 && factionData.ExportRequests.Count > 0 && factionData.CargoShipsIdle.Count == 0 )
                factionData.FailedCounter = (factionData.ImportRequests.Count, factionData.ExportRequests.Count);
            else
                factionData.FailedCounter = (0, 0);
            #endregion

            Engine_Universal.NewTimingsBeingBuilt.FinishRememberingFrame( FramePartTimings.TimingType.MainSimThreadNormal, "DoTradeRequests" );
        }

        // The following function is called once every second. Consider this our 'main' function of sorts, all of our logic is based on this bad boy calling all our pieces every second.
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( faction.MustBeAwakenedByPlayer && !faction.HasBeenAwakenedByPlayer )
                return;

            // Update faction relations and game settings.
            UpdateAllegianceAndSettings( faction );

            // Update mark levels every now and than.
            if ( World_AIW2.Instance.GameSecond % SecondsBetweenMilitiaUpgrades == 0 )
            {
                UpgradeAndPurgeUnitsAsNeeded( faction, Context );
            }

            // If we don't yet have it, create our factionData.
            if ( factionData == null )
            {
                // Load our data.
                factionData = faction.GetCivilianFactionExt( ExternalDataRetrieval.CreateIfNotFound );
            }

            // Increment rebuild timers.
            if ( factionData.GrandStationRebuildTimerInSeconds > 0 )
                factionData.GrandStationRebuildTimerInSeconds--;
            factionData.TradeStationRebuildTimerInSecondsByPlanet.DoFor( pair =>
            {
                if ( pair.Value > 0 )
                    pair.Value--;

                return DelReturn.Continue;
            } );

            // Decloak if needed.
            for ( int y = 0; y < LastGameSecondForMessageAboutThisPlanet.GetPairCount(); y++ )
            {
                Planet planet = LastGameSecondForMessageAboutThisPlanet.GetPairByIndex( y ).Key;
                int lastSecond = LastGameSecondForMessageAboutThisPlanet[planet];

                if ( !LastGameSecondForLastTachyonBurstOnThisPlanet.GetHasKey( planet ) )
                    LastGameSecondForLastTachyonBurstOnThisPlanet.AddPair( planet, lastSecond );

                if ( World_AIW2.Instance.GameSecond - LastGameSecondForLastTachyonBurstOnThisPlanet[planet] >= 30 )
                {
                    var threat = factionData.GetThreat( planet );
                    if ( threat.CloakedHostile > threat.Total * 0.9 )
                        FactionUtilityMethods.TachyonBlastPlanet( planet, faction, Context, false );
                    LastGameSecondForLastTachyonBurstOnThisPlanet[planet] = World_AIW2.Instance.GameSecond;
                }
            }

            // For each faction we're friendly to, proccess.
            for ( int x = 0; x < World_AIW2.Instance.Factions.Count; x++ )
            {
                Faction alignedFaction = World_AIW2.Instance.Factions[x];
                if ( !faction.GetIsFriendlyTowards( alignedFaction ) )
                    continue;

                if ( faction.FactionIndex == alignedFaction.FactionIndex )
                    continue; // Skip self

                // Grand Station creation.
                if ( factionData.GrandStation == null && factionData.GrandStationRebuildTimerInSeconds == 0 )
                {
                    CreateGrandStation( faction, alignedFaction, Context );
                }

                // If we don't yet have a grand station built for this faction, stop. Without our grand station, we're nothing.
                if ( factionData.GrandStation == null )
                    continue;

                // Handle spawning of trade stations.
                CreateTradeStations( faction, alignedFaction, Context );

                // Add buildings for the player to build.
                if ( alignedFaction.Type == FactionType.Player )
                {
                    if ( World_AIW2.Instance.GameSecond % 15 == 0 )
                        AddMilitiaBuildings( faction, alignedFaction, Context );

                    // Scan for any new buildings that the player has placed related to the mod.
                    ScanForMilitiaBuildings( faction, alignedFaction, Context );
                }
            }

            // Update trade requests.
            DoTradeRequests( faction, Context );

            // Handle basic resource generation. (Resources with no requirements, ala Goods or Ore.)
            DoResources( faction, Context );

            // Handle the creation of ships.
            DoShipSpawns( faction, Context );

            // Check for ship arrival.
            DoShipArrival( faction, Context );

            // Handle resource transfering.
            DoResourceTransfer( faction, Context );

            // Handle assigning militia to our ThreatReports.
            DoMilitiaAssignment( faction, Context );

            // Handle militia deployment and unit building.
            DoMilitiaDeployment( faction, Context );

            // Handle AI response. Have some variation on wave timers.
            if ( faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Allegiance != "AI Team" )
            {
                // Delay raids for the first hour of the game.
                if ( World_AIW2.Instance.GameSecond < 3600 )
                    factionData.NextRaidInThisSeconds = 1800;

                if ( factionData.NextRaidInThisSeconds > 300 )
                    factionData.NextRaidInThisSeconds = Math.Max( 300, factionData.NextRaidInThisSeconds - Context.RandomToUse.Next( 1, 3 ) );
                else if ( factionData.NextRaidInThisSeconds > 0 )
                    factionData.NextRaidInThisSeconds -= Context.RandomToUse.Next( 1, 3 );

                // Prepare (and warn the player about) an upcoming raid.
                if ( factionData.NextRaidInThisSeconds == 300 )
                    PrepareAIRaid( faction, Context );

                // Raid!
                if ( factionData.NextRaidInThisSeconds <= 0 )
                    DoAIRaid( faction, Context );
            }

            // Save our faction data.
            faction.SetCivilianFactionExt( factionData );
        }

        // Calculate threat values every planet.
        public void CalculateThreat( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            // Empty our dictionary.
            factionData.ThreatReports = new List<ThreatReport>();

            // Get the grand station's planet, to easily figure out when we're processing the home planet.
            if ( factionData.GrandStation == null )
                return;
            Planet grandPlanet = factionData.GrandStation.Planet;
            if ( grandPlanet == null )
                return;

            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                // Prepare variables to hold our soon to be detected threat values.
                int friendlyMobileStrength = 0, friendlyGuardStrength = 0, cloakedHostileStrength = 0, nonCloakedHostileStrength = 0, militiaMobileStrength = 0, militiaGuardStrength = 0, waveStrength = 0;
                // Wave detection.
                for ( int j = 0; j < World_AIW2.Instance.AIFactions.Count; j++ )
                {
                    Faction aiFaction = World_AIW2.Instance.AIFactions[j];
                    List<PlannedWave> QueuedWaves = aiFaction.GetWaveList( ExternalDataRetrieval.CreateIfNotFound );
                    for ( int k = 0; k < QueuedWaves.Count; k++ )
                    {
                        PlannedWave wave = QueuedWaves[k];

                        if ( wave.targetPlanetIdx != planet.Index )
                            continue;

                        if ( wave.gameTimeInSecondsForLaunchWave - World_AIW2.Instance.GameSecond <= 90 )
                            nonCloakedHostileStrength += wave.CalculateStrengthOfWave( aiFaction ) * 3;

                        else if ( wave.playerBeingAlerted )
                            waveStrength += wave.CalculateStrengthOfWave( aiFaction );
                    }
                }

                // Get hostile strength.
                LongRangePlanningData_PlanetFaction linkedPlanetFactionData = planet.LongRangePlanningData.PlanetFactionDataByIndex[faction.FactionIndex];
                LongRangePlanning_StrengthData_PlanetFaction_Stance hostileStrengthData = linkedPlanetFactionData.DataByStance[FactionStance.Hostile];
                // If on friendly planet, triple the threat.
                if ( factionData.IsPlanetFriendly( faction, planet ) )
                    nonCloakedHostileStrength += hostileStrengthData.TotalStrength * 3;
                else // If on hostile planet, don't factor in stealth.
                {
                    nonCloakedHostileStrength += hostileStrengthData.TotalStrength - hostileStrengthData.CloakedStrength;
                    cloakedHostileStrength += hostileStrengthData.CloakedStrength;
                }

                // Adjacent planet threat matters as well, but not as much as direct threat.
                // We'll only add it if the planet has no friendly forces on it.
                if ( factionData.IsPlanetFriendly( faction, planet ) )
                    planet.DoForLinkedNeighbors( false, delegate ( Planet linkedPlanet )
                    {
                        if ( factionData.IsPlanetFriendly( faction, linkedPlanet ) )
                            return DelReturn.Continue;

                        linkedPlanetFactionData = linkedPlanet.LongRangePlanningData.PlanetFactionDataByIndex[faction.FactionIndex];
                        LongRangePlanning_StrengthData_PlanetFaction_Stance attackingStrengthData = linkedPlanetFactionData.DataByStance[FactionStance.Friendly];
                        int attackingStrength = attackingStrengthData.TotalStrength;
                        attackingStrengthData = linkedPlanetFactionData.DataByStance[FactionStance.Self];
                        attackingStrength += attackingStrengthData.TotalStrength;

                        if ( attackingStrength < 1000 )
                        {
                            hostileStrengthData = linkedPlanetFactionData.DataByStance[FactionStance.Hostile];
                            nonCloakedHostileStrength += hostileStrengthData.RelativeToHumanTeam_ThreatStrengthVisible;
                            nonCloakedHostileStrength += hostileStrengthData.TotalHunterStrength_AgainstHumansVisible;
                        }

                        return DelReturn.Continue;
                    } );

                // If on home plant, double the total threat.
                if ( planet.Index == grandPlanet.Index )
                    nonCloakedHostileStrength *= 2;

                // Get friendly strength on the planet.
                LongRangePlanningData_PlanetFaction planetFactionData = planet.LongRangePlanningData.PlanetFactionDataByIndex[faction.FactionIndex];
                LongRangePlanning_StrengthData_PlanetFaction_Stance friendlyStrengthData = planetFactionData.DataByStance[FactionStance.Friendly];
                friendlyMobileStrength += friendlyStrengthData.MobileStrength;
                friendlyGuardStrength += friendlyStrengthData.TotalStrength - friendlyMobileStrength;

                // Get militia strength on the planet.
                LongRangePlanning_StrengthData_PlanetFaction_Stance militiaStrengthData = planetFactionData.DataByStance[FactionStance.Self];
                militiaMobileStrength = militiaStrengthData.MobileStrength;
                militiaGuardStrength = militiaStrengthData.TotalStrength - militiaMobileStrength;

                // Save our threat value.
                factionData.ThreatReports.Add( new ThreatReport( planet, militiaGuardStrength, militiaMobileStrength, friendlyGuardStrength, friendlyMobileStrength, cloakedHostileStrength, nonCloakedHostileStrength, waveStrength ) );
                return DelReturn.Continue;
            } );
            // Sort our reports.
            factionData.ThreatReports.Sort();
        }

        private void ProcessTradingResponse( List<int> ships, ref ArcenSparseLookup<int, int> AnsweringImport, ref ArcenSparseLookup<int, int> AnsweringExport )
        {
            for ( int x = 0; x < ships.Count; x++ )
            {
                GameEntity_Squad ship = World_AIW2.Instance.GetEntityByID_Squad( ships[x] );
                if ( ship == null )
                    continue;
                int target = ship.GetCivilianStatusExt( ExternalDataRetrieval.CreateIfNotFound ).Origin;
                if ( !AnsweringExport.GetHasKey( target ) )
                    AnsweringExport.AddPair( target, 1 );
                else
                    AnsweringExport[target]++;
            }
            ProcessTradingResponse( ships, ref AnsweringImport );
        }
        private void ProcessTradingResponse( List<int> ships, ref ArcenSparseLookup<int, int> AnsweringImport )
        {
            for ( int x = 0; x < ships.Count; x++ )
            {
                GameEntity_Squad ship = World_AIW2.Instance.GetEntityByID_Squad( ships[x] );
                if ( ship == null )
                    continue;
                int target = ship.GetCivilianStatusExt( ExternalDataRetrieval.CreateIfNotFound ).Destination;
                if ( !AnsweringImport.GetHasKey( target ) )
                    AnsweringImport.AddPair( target, 1 );
                else
                    AnsweringImport[target]++;
            }
        }

        // Find an optimal planet to expand to, prefering the cloest valid one to our Grand Station.
        public void FindNextTradeStationExpansionPoint( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( factionData?.GrandStation == null )
                return;

            bool playerAllied = false;
            List<Faction> preferredFactions = new List<Faction>(), secondaryFactions = new List<Faction>();
            // For each faction we're friendly to, proccess.
            for ( int x = 0; x < World_AIW2.Instance.Factions.Count; x++ )
            {
                Faction alignedFaction = World_AIW2.Instance.Factions[x];
                if ( !faction.GetIsFriendlyTowards( alignedFaction ) )
                    continue;

                if ( faction.FactionIndex == alignedFaction.FactionIndex )
                    continue; // Skip self

                switch ( alignedFaction.Type )
                {
                    case FactionType.Player:
                        preferredFactions.Add( alignedFaction );
                        playerAllied = true;
                        break;
                    case FactionType.AI:
                        preferredFactions.Add( alignedFaction );
                        break;
                    case FactionType.SpecialFaction:
                        if ( !alignedFaction.MustBeAwakenedByPlayer || alignedFaction.HasBeenAwakenedByPlayer )
                            secondaryFactions.Add( alignedFaction );
                        break;
                    default:
                        break;
                }
            }

            if ( playerAllied && !MilitiaExpandWithAllAllies )
                secondaryFactions.Clear();

            GameEntity_Squad bestTarget = null;
            int bestHops = 999;

            // Look for command stations from our preferred factions.
            for ( int x = 0; x < preferredFactions.Count; x++ )
                preferredFactions[x].DoForEntities( EntityRollupType.CommandStation, command =>
                {
                    // Factor out planets that already have a trade station.
                    for ( int y = 0; y < factionData.TradeStations.Count; y++ )
                    {
                        Planet tradeStationPlanet = World_AIW2.Instance.GetEntityByID_Squad( factionData.TradeStations[y] )?.Planet;
                        if ( tradeStationPlanet == null )
                            continue;

                        if ( command.Planet == tradeStationPlanet )
                            return DelReturn.Continue;
                    }

                    // Skip if we can't safely reach it.
                    if ( factionData.GrandStation == null || Fireteam.GetDangerOfPath( faction, Context, factionData.GrandStation.Planet, command.Planet, true, out short _ ) > 10000 )
                        return DelReturn.Continue;

                    var stanceData = command.PlanetFaction.DataByStance;

                    if ( stanceData[FactionStance.Self].TotalStrength < stanceData[FactionStance.Hostile].TotalStrength / 10 )
                        return DelReturn.Continue;

                    int hops = factionData.GrandStation.Planet.GetHopsTo( command.Planet );

                    if ( bestTarget == null )
                    {
                        bestTarget = command;
                        bestHops = hops;
                    }

                    if ( hops < bestHops )
                    {
                        bestTarget = command;
                        bestHops = hops;
                    }

                    return DelReturn.Continue;
                } );

            // If we don't have any command station targets, instead look for any good stationary units we can expand to.
            if ( bestTarget == null )
                for ( int x = 0; x < secondaryFactions.Count; x++ )
                {
                    Faction workingFaction = secondaryFactions[x];
                    World_AIW2.Instance.DoForPlanets( false, planet =>
                    {
                        // Factor out planets that already have a trade station.
                        for ( int y = 0; y < factionData.TradeStations.Count; y++ )
                        {
                            Planet tradeStationPlanet = World_AIW2.Instance.GetEntityByID_Squad( factionData.TradeStations[y] )?.Planet;
                            if ( tradeStationPlanet == null )
                                continue;

                            if ( planet == tradeStationPlanet )
                                return DelReturn.Continue;
                        }

                        // Skip if we can't safely reach it.
                        if ( factionData.GrandStation == null || Fireteam.GetDangerOfPath( faction, Context, factionData.GrandStation.Planet, planet, true, out short _ ) > 10000 )
                            return DelReturn.Continue;

                        PlanetFaction workingPFaction = planet.GetPlanetFactionForFaction( workingFaction );
                        var stanceData = workingPFaction.DataByStance;

                        if ( stanceData[FactionStance.Self].TotalStrength < stanceData[FactionStance.Hostile].TotalStrength / 10 )
                            return DelReturn.Continue;

                        // Found a planet that they have majority control over. Spawn around a stationary friendly unit.
                        bool foundCenterpiece = false, foundStationary = false;
                        workingPFaction.Entities.DoForEntities( delegate ( GameEntity_Squad allignedSquad )
                        {
                            // Default to the first stationary.
                            if ( bestTarget == null && !allignedSquad.TypeData.IsMobile )
                                bestTarget = allignedSquad;

                            // If found is a centerpiece, pick it.
                            if ( allignedSquad.TypeData.SpecialType == SpecialEntityType.NPCFactionCenterpiece )
                            {
                                if ( !foundCenterpiece )
                                {
                                    bestTarget = allignedSquad;
                                    foundCenterpiece = true;
                                }
                                else if ( allignedSquad.GetStrengthOfSelfAndContents() > bestTarget.GetStrengthOfSelfAndContents() )
                                    bestTarget = allignedSquad;
                            }
                            else if ( !foundCenterpiece )
                            {
                                // No centerpiece, default to strongest, preferring stationary.
                                if ( !allignedSquad.TypeData.IsMobile )
                                {
                                    if ( !foundStationary )
                                    {
                                        bestTarget = allignedSquad;
                                        foundStationary = true;
                                    }
                                    else if ( allignedSquad.GetStrengthOfSelfAndContents() > bestTarget.GetStrengthOfSelfAndContents() )
                                        bestTarget = allignedSquad;
                                }
                            }

                            return DelReturn.Continue;
                        } );

                        return DelReturn.Continue;
                    } );
                }

            if ( bestTarget != factionData.NextTradeStationTarget )
            {
                GameCommand command = StaticMethods.CreateGameCommand( Commands.SetNextTargetForTradeStation.ToString(), GameCommandSource.AnythingElse, faction );
                command.RelatedEntityIDs.Add( bestTarget.PrimaryKeyID );
                Context.QueueCommandForSendingAtEndOfContext( command );
            }
        }

        // Handle movement of cargo ships to their orign and destination points.
        public void DoCargoShipMovement( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            // Ships going somewhere for dropoff.
            ProcessIncoming( factionData.CargoShipsBuilding, 5000, faction, Context );
            ProcessIncoming( factionData.CargoShipsEnroute, 2000, faction, Context );
            ProcessIncoming( factionData.CargoShipsUnloading, 5000, faction, Context );

            // Ships going somewhere for pickup.
            ProcessOutgoing( factionData.CargoShipsLoading, 5000, faction, Context );
            ProcessOutgoing( factionData.CargoShipsPathing, 2000, faction, Context );
        }
        private void ProcessIncoming( List<int> ships, int maxDistance, Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            for ( int x = 0; x < ships.Count; x++ )
            {
                // Load the ship and its status.
                GameEntity_Squad ship = World_AIW2.Instance.GetEntityByID_Squad( ships[x] );
                if ( ship == null )
                    continue;
                CivilianStatus shipStatus = ship.GetCivilianStatusExt( ExternalDataRetrieval.ReturnNullIfNotFound );
                if ( shipStatus == null )
                    continue;
                // Enroute movement.
                // ship currently moving towards destination station.
                GameEntity_Squad destinationStation = World_AIW2.Instance.GetEntityByID_Squad( shipStatus.Destination );
                if ( destinationStation == null )
                    continue;
                Planet destinationPlanet = destinationStation.Planet;

                // Check if already on planet.
                if ( ship.Planet.Index == destinationPlanet.Index )
                {
                    if ( destinationStation.GetDistanceTo_ExpensiveAccurate( ship.WorldLocation, true, true ) < maxDistance )
                        continue; // Stop if already close enough.

                    // On planet. Begin pathing towards the station.
                    ship.QueueMovementCommand( destinationStation.WorldLocation );
                }
                else
                {
                    if ( ship.LongRangePlanningData != null && ship.LongRangePlanningData.FinalDestinationPlanetIndex != -1 )
                        continue; // Stop if already enroute.

                    // Not on planet yet, prepare wormhole navigation.
                    ship.QueueWormholeCommand( faction, destinationPlanet, Context );
                }
            }
        }
        private void ProcessOutgoing( List<int> ships, int maxDistance, Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            for ( int x = 0; x < ships.Count; x++ )
            {
                // Load the ship and its status.
                GameEntity_Squad ship = World_AIW2.Instance.GetEntityByID_Squad( ships[x] );
                if ( ship == null )
                    continue;
                CivilianStatus shipStatus = ship.GetCivilianStatusExt( ExternalDataRetrieval.ReturnNullIfNotFound );
                if ( shipStatus == null )
                    continue;
                // Ship currently moving towards origin station.
                GameEntity_Squad originStation = World_AIW2.Instance.GetEntityByID_Squad( shipStatus.Origin );
                if ( originStation == null )
                    continue;
                Planet originPlanet = originStation.Planet;

                // Check if already on planet.
                if ( ship.Planet.Index == originPlanet.Index )
                {
                    if ( originStation.GetDistanceTo_ExpensiveAccurate( ship.WorldLocation, true, true ) < maxDistance )
                        continue; // Stop if already close enough.

                    // On planet. Begin pathing towards the station.
                    ship.QueueMovementCommand( originStation.WorldLocation );
                }
                else
                {
                    if ( ship.LongRangePlanningData != null && ship.LongRangePlanningData.FinalDestinationPlanetIndex != -1 )
                        continue; // Stop if already moving.

                    // Not on planet yet, queue a wormhole movement command.
                    ship.QueueWormholeCommand( faction, originPlanet, Context );
                }
            }
        }

        /// <summary>
        /// Handle movement of militia construction ships.

        /// </summary>
        public void DoMilitiaConstructionShipMovement( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            // Loop through each of our militia ships.
            for ( int x = 0; x < factionData.MilitiaLeaders.Count; x++ )
            {
                // Load the ship and its status.
                GameEntity_Squad ship = World_AIW2.Instance.GetEntityByID_Squad( factionData.MilitiaLeaders[x] );
                if ( ship == null || !ship.TypeData.IsMobile )
                    continue;
                CivilianMilitia shipStatus = ship.GetCivilianMilitiaExt( ExternalDataRetrieval.ReturnNullIfNotFound );
                if ( shipStatus == null )
                    continue;
                Planet planet = World_AIW2.Instance.GetPlanetByIndex( shipStatus.PlanetFocus );
                if ( planet == null )
                    continue;

                // Pathing movement.
                if ( shipStatus.Status == CivilianMilitiaStatus.PathingForMine || shipStatus.Status == CivilianMilitiaStatus.PathingForWormhole || shipStatus.Status == CivilianMilitiaStatus.PathingForShipyard )
                {
                    // Check if already on planet.
                    if ( ship.Planet.Index == shipStatus.PlanetFocus )
                    {
                        // On planet. Begin pathing towards the station.
                        GameEntity_Squad goalStation = null;

                        // Find the trade station.
                        planet.DoForEntities( delegate ( GameEntity_Squad entity )
                        {
                            // If we find its index in our records, thats our trade station.
                            if ( factionData.TradeStations.Contains( entity.PrimaryKeyID ) )
                            {
                                goalStation = entity;
                                return DelReturn.Break;
                            }

                            return DelReturn.Continue;
                        } );

                        if ( goalStation == null )
                            continue;

                        if ( ship.LongRangePlanningData != null && ship.LongRangePlanningData.DestinationPoint == goalStation.WorldLocation )
                            continue; // Stop if we're already enroute.

                        ship.QueueMovementCommand( goalStation.WorldLocation );
                    }
                    else
                    {
                        // Not on planet yet, prepare wormhole navigation.
                        if ( ship.LongRangePlanningData != null && ship.LongRangePlanningData.FinalDestinationPlanetIndex != -1 )
                            continue; // Stop if we're already enroute.

                        ship.QueueWormholeCommand( faction, planet, Context );
                    }
                }
                else if ( shipStatus.Status == CivilianMilitiaStatus.EnrouteWormhole )
                {
                    // Enroute movement.
                    // Ship has made it to the planet (and, if detected, the trade station on the planet).
                    // We'll now have it begin moving towards its assigned wormhole.
                    // Distance detection for it is handled in the persecond logic further up, all this handles are movement commands.
                    GameEntity_Other wormhole = shipStatus.getWormhole();
                    if ( wormhole == null )
                    {
                        ArcenDebugging.SingleLineQuickDebug( "Civilian Industries: Failed to find wormhole." );
                        continue;
                    }

                    // Generate our location to move to.
                    ArcenPoint point = ship.WorldLocation.GetPointAtAngleAndDistance( ship.WorldLocation.GetAngleToDegrees( wormhole.WorldLocation ), 5000 );
                    if ( point == ArcenPoint.ZeroZeroPoint )
                        continue;

                    if ( ship.LongRangePlanningData != null && ship.LongRangePlanningData.DestinationPoint.GetDistanceTo( point, true ) < 1000 )
                        continue; // Stop if we're already enroute.

                    ship.QueueMovementCommand( point );
                }
                else if ( shipStatus.Status == CivilianMilitiaStatus.EnrouteMine )
                {
                    // Enroute movement.
                    // Ship has made it to the planet (and, if detected, the trade station on the planet).
                    // We'll now have it begin moving towards its assigned mine.
                    // Distance detection for it is handled in the persecond logic further up, all this handles are movement commands.
                    GameEntity_Squad mine = shipStatus.getMine();
                    if ( mine == null )
                    {
                        ArcenDebugging.SingleLineQuickDebug( "Civilian Industries: Failed to find mine." );
                        continue;
                    }

                    if ( ship.LongRangePlanningData != null && ship.LongRangePlanningData.DestinationPoint == mine.WorldLocation )
                        continue; // Stop if we're enroute.

                    ship.QueueMovementCommand( mine.WorldLocation );
                }
            }
        }

        // Handle reactive moevement of patrolling ship fleets.
        public void DoMilitiaThreatReaction( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            // If we don't have any threat reports yet (usually due to game load) wait.
            if ( factionData.ThreatReports == null || factionData.ThreatReports.Count == 0 )
                return;

            Engine_Universal.NewTimingsBeingBuilt.StartRememberingFrame( FramePartTimings.TimingType.ShortTermBackgroundThreadEntry, "DoMilitiaThreatReaction" );
            GameCommand atEaseCommand = StaticMethods.CreateGameCommand( Commands.SetMilitiaAtEase.ToString(), GameCommandSource.AnythingElse, faction );
            GameCommand storeAtEaseUnitsCommand = StaticMethods.CreateGameCommand( Commands.AttemptedToStoreAtEaseUnit.ToString(), GameCommandSource.AnythingElse, faction );

            // Amount of strength ready to raid on each planet.
            // This means that it, and all friendly planets adjacent to it, are safe.
            ArcenSparseLookup<Planet, int> raidStrength = new ArcenSparseLookup<Planet, int>();

            // Planets that have been given an order. Used for AtEase logic at the bottom.
            List<Planet> isPatrolling = new List<Planet>();

            // Process all militia forces that are currently patrolling.
            #region Defensive Actions
            for ( int x = 0; x < factionData.MilitiaLeaders.Count; x++ )
            {
                GameEntity_Squad post = World_AIW2.Instance.GetEntityByID_Squad( factionData.MilitiaLeaders[x] );

                if ( post == null )
                    continue;

                CivilianMilitia militiaData = post.GetCivilianMilitiaExt( ExternalDataRetrieval.ReturnNullIfNotFound );

                if ( militiaData == null || militiaData.Status != CivilianMilitiaStatus.Patrolling )
                    continue;

                GameEntity_Squad centerpiece = World_AIW2.Instance.GetEntityByID_Squad( militiaData.Centerpiece );
                if ( centerpiece == null )
                    continue;

                // Where are we going to send all our units?
                Planet targetPlanet = null;

                // If our centerpiece is a battlestation, and the user has requested them to have defensive forces, act on that.
                if ( centerpiece.TypeData.IsBattlestation && DefensiveBattlestationForces )
                    targetPlanet = centerpiece.Planet;

                // If self or an adjacent friendly planet has hostile units on it that outnumber friendly defenses, including incoming waves, protect it.
                for ( int y = 0; y < factionData.ThreatReports.Count && targetPlanet == null; y++ )
                {
                    ThreatReport report = factionData.ThreatReports[y];

                    if ( report.Planet.GetHopsTo( centerpiece.Planet ) > 1 )
                        continue; // Skip if not adjacent

                    if ( report.TotalStrength * 2 < report.MilitiaGuardStrength + report.FriendlyGuardStrength )
                        continue; // Skip if defenses are strong enough.

                    if ( factionData.IsPlanetFriendly( faction, report.Planet ) )
                    {
                        targetPlanet = report.Planet;
                        break;
                    }
                }

                // If we have a target for defensive action, act on it.
                if ( targetPlanet != null )
                {
                    isPatrolling.Add( centerpiece.Planet );

                    for ( int y = 0; y < militiaData.Ships.GetPairCount(); y++ )
                    {
                        for ( int z = 0; z < militiaData.Ships[y].Count; z++ )
                        {
                            GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( militiaData.Ships[y][z] );
                            if ( entity == null || entity.LongRangePlanningData == null )
                                continue;

                            // Skip centerpiece.
                            if ( centerpiece.PrimaryKeyID == entity.PrimaryKeyID )
                                continue;

                            if ( entity.Planet.Index == targetPlanet.Index && entity.LongRangePlanningData.FinalDestinationPlanetIndex != -1 &&
                                entity.LongRangePlanningData.FinalDestinationPlanetIndex != targetPlanet.Index )
                            {
                                // We're on our target planet, but for some reason we're trying to leave it. Stop.
                                entity.Orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.DoNotClearDecollision, ClearSource.YesClearAnyOrders_IncludingFromHumans, "A Civilian Industry militia unit is attempting to leave a planet that it should be guarding." );
                            }

                            if ( entity.Planet.Index != targetPlanet.Index && entity.LongRangePlanningData.FinalDestinationPlanetIndex != targetPlanet.Index )
                            {
                                if ( entity.Planet.Index != centerpiece.Planet.Index )
                                {
                                    // Not yet on our target planet, and we're not yet on our centerpiece planet. Path to our centerpiece planet first.
                                    if ( entity.LongRangePlanningData.FinalDestinationPlanetIndex == centerpiece.Planet.Index )
                                        continue; // Stop if already moving towards it.

                                    entity.QueueWormholeCommand( faction, centerpiece.Planet, Context );
                                }
                                else
                                {
                                    // Not yet on our target planet, and we're on our centerpice planet. Path to our target planet.
                                    entity.QueueWormholeCommand( faction, targetPlanet, Context );
                                }
                            }
                        }
                    }
                }
                else
                {
                    int val = 0;
                    if ( raidStrength.GetHasKey( centerpiece.Planet ) )
                        val = raidStrength[centerpiece.Planet];
                    // If we have at least one planet adjacent to us that is hostile and threatening, add our patrol posts to the raiding pool.
                    centerpiece.Planet.DoForLinkedNeighbors( false, delegate ( Planet adjPlanet )
                    {
                        var threat = factionData.GetThreat( adjPlanet );
                        if ( !factionData.IsPlanetFriendly( faction, adjPlanet ) && threat.Total > MilitiaAttackMinimumStrength )
                        {
                            int strength = post.AdditionalStrengthFromFactions;
                            for ( int y = 0; y < militiaData.Ships.GetPairCount(); y++ )
                            {
                                for ( int z = 0; z < militiaData.Ships[y].Count; z++ )
                                {
                                    GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( militiaData.Ships[y][z] );
                                    if ( entity == null )
                                        continue;

                                    if ( entity.TypeData.IsMobileCombatant && (entity.TypeData.GetHasTag( "CivMobile" ) || entity.TypeData.GetHasTag( "CivProtector" )) )
                                        strength += entity.GetStrengthOfSelfAndContents();
                                }
                            }

                            if ( !raidStrength.GetHasKey( centerpiece.Planet ) )
                                raidStrength.AddPair( centerpiece.Planet, strength );
                            else
                                raidStrength[centerpiece.Planet] += strength;
                            return DelReturn.Break;
                        }
                        return DelReturn.Continue;
                    } );
                    if ( raidStrength.GetHasKey( centerpiece.Planet ) )
                        val = raidStrength[centerpiece.Planet];
                }
            }
            #endregion

            #region Offensive Actions
            // Figure out the potential strength we would have to attack each planet.
            List<AttackAssessment> attackAssessments = new List<AttackAssessment>();
            if ( raidStrength.GetPairCount() > 0 && raidStrength.GetPairCount() > 0 )
                raidStrength.DoFor( pair =>
                {
                    pair.Key.DoForLinkedNeighbors( false, delegate ( Planet adjPlanet )
                    {
                        // If friendly, skip.
                        if ( factionData.IsPlanetFriendly( faction, adjPlanet ) )
                            return DelReturn.Continue;

                        var threat = factionData.GetThreat( adjPlanet );

                        // See if they still have any active guard posts.
                        int reinforcePoints = 0;
                        adjPlanet.DoForEntities( EntityRollupType.ReinforcementLocations, delegate ( GameEntity_Squad reinforcementPoint )
                        {
                            if ( reinforcementPoint.TypeData.SpecialType == SpecialEntityType.GuardPost )
                                reinforcePoints++;
                            return DelReturn.Continue;
                        } );

                        // If we don't yet have an assessment for the planet, and it has enough threat, add it.
                        if ( reinforcePoints > 0 || threat.Total > MilitiaAttackMinimumStrength )
                        {
                            AttackAssessment adjAssessment = (from o in attackAssessments where o.Target.Index == adjPlanet.Index select o).FirstOrDefault();
                            if ( adjAssessment == null )
                            {
                                adjAssessment = new AttackAssessment( adjPlanet, (int)(threat.Total * MilitiaAttackOverkillPercentage), reinforcePoints > 0 ? true : false );
                                // If we already have units on the planet, mark it as such.
                                if ( threat.MilitiaMobile > 1000 )
                                    adjAssessment.MilitiaOnPlanet = true;

                                attackAssessments.Add( adjAssessment );
                            }
                            // Add our current fleet strength to the attack budget.
                            adjAssessment.Attackers.AddPair( pair.Key, pair.Value );
                        }
                        return DelReturn.Continue;
                    } );

                    return DelReturn.Continue;
                } );

            // Sort by strongest planets first. We want to attempt to take down the strongest planet.
            attackAssessments.Sort();

            // Keep poising to attack as long as the target we're aiming for is weak to us.
            while ( attackAssessments.Count > 0 )
            {
                AttackAssessment assessment = attackAssessments[0];

                // See if there are already any player units on the planet.
                // If there are, we should be heading there as soon as possible.
                bool alreadyAttacking = false;
                var threat = factionData.GetThreat( assessment.Target );
                if ( threat.FriendlyMobile + threat.FriendlyGuard > 1000 )
                {
                    // If they need our help, see if we can assist.
                    // Consider hostile strength less effective than regular for this purpose.
                    int effStr = threat.Total / 3;
                    if ( effStr > threat.FriendlyGuard + threat.FriendlyMobile + assessment.AttackPower )
                    {
                        attackAssessments.RemoveAt( 0 );
                        continue;
                    }
                }
                // If not strong enough, remove.
                else if ( assessment.AttackPower < assessment.StrengthRequired )
                {
                    attackAssessments.RemoveAt( 0 );
                    continue;
                }

                // If militia are already on the planet, pile in.
                if ( assessment.MilitiaOnPlanet )
                    alreadyAttacking = true;

                // Stop the attack if too many ships aren't ready, unless we're already attacking.
                int total = 0, ready = 0;

                for ( int x = 0; x < factionData.MilitiaLeaders.Count; x++ )
                {
                    GameEntity_Squad post = World_AIW2.Instance.GetEntityByID_Squad( factionData.MilitiaLeaders[x] );

                    if ( post == null )
                        continue;

                    CivilianMilitia militiaData = post.GetCivilianMilitiaExt( ExternalDataRetrieval.ReturnNullIfNotFound );

                    if ( militiaData == null || militiaData.Status != CivilianMilitiaStatus.Patrolling )
                        continue;

                    GameEntity_Squad centerpiece = World_AIW2.Instance.GetEntityByID_Squad( militiaData.Centerpiece );
                    if ( centerpiece == null )
                        continue;

                    // Skip if not an attacker.
                    bool isAttacker = false;
                    assessment.Attackers.DoFor( pair =>
                    {
                        if ( centerpiece.Planet.Index == pair.Key.Index )
                        {
                            isAttacker = true;
                            return DelReturn.Break;
                        }

                        return DelReturn.Continue;
                    } );

                    if ( !isAttacker )
                        continue;

                    isPatrolling.Add( centerpiece.Planet );

                    // Skip checks if we're already attacking or have already gotten enough strength.
                    if ( alreadyAttacking )
                        break;

                    // Prepare a movement command to gather our ships around a wormhole.
                    GameEntity_Other wormhole = centerpiece.Planet.GetWormholeTo( assessment.Target );

                    for ( int y = 0; y < militiaData.Ships.GetPairCount() && !alreadyAttacking; y++ )
                    {
                        if ( militiaData.ShipTypeData[y] != null )
                            total += militiaData.GetShipCount( militiaData.ShipTypeData[y] );

                        for ( int z = 0; z < militiaData.Ships[y].Count; z++ )
                        {
                            GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( militiaData.Ships[y][z] );
                            if ( entity == null || entity.LongRangePlanningData == null )
                                continue;

                            // Already attacking, stop checking and start raiding.
                            if ( entity.Planet.Index == assessment.Target.Index )
                            {
                                alreadyAttacking = true;
                                break;
                            }

                            // Skip centerpiece.
                            if ( centerpiece.PrimaryKeyID == entity.PrimaryKeyID )
                                continue;

                            // Get them moving if needed.
                            if ( entity.Planet.Index != centerpiece.Planet.Index )
                            {
                                if ( entity.LongRangePlanningData.FinalDestinationPlanetIndex != centerpiece.Planet.Index )
                                {
                                    entity.QueueWormholeCommand( faction, centerpiece.Planet, Context );
                                }
                            }
                            else if ( wormhole != null && wormhole.WorldLocation.GetExtremelyRoughDistanceTo( entity.WorldLocation ) > 5000 )
                            {
                                // Create and add all required parts of a move to point command.
                                if ( (entity.Orders.QueuedOrders.Count == 0 || entity.Orders.QueuedOrders[0].RelatedPoint != wormhole.WorldLocation) )
                                {
                                    entity.QueueMovementCommand( wormhole.WorldLocation );
                                }
                            }
                            else
                                ready += 1 + entity.ExtraStackedSquadsInThis;

                        }
                    }
                }

                // If 66% all of our ships are ready,  its raiding time.
                if ( ready > total * 0.8 || alreadyAttacking )
                {
                    for ( int x = 0; x < factionData.MilitiaLeaders.Count; x++ )
                    {
                        GameEntity_Squad post = World_AIW2.Instance.GetEntityByID_Squad( factionData.MilitiaLeaders[x] );
                        if ( post == null )
                            continue;

                        CivilianMilitia militiaData = post.GetCivilianMilitiaExt( ExternalDataRetrieval.ReturnNullIfNotFound );

                        if ( militiaData == null || militiaData.Status != CivilianMilitiaStatus.Patrolling )
                            continue;

                        GameEntity_Squad centerpiece = World_AIW2.Instance.GetEntityByID_Squad( militiaData.Centerpiece );
                        if ( centerpiece == null )
                            continue;

                        // Skip if not an attacker.
                        bool isAttacker = false;
                        assessment.Attackers.DoFor( pair =>
                        {
                            if ( centerpiece.Planet.Index == pair.Key.Index )
                            {
                                isAttacker = true;
                                return DelReturn.Break;
                            }

                            return DelReturn.Continue;
                        } );

                        if ( !isAttacker )
                            continue;

                        // We're here. The AI should release all of its forces to fight us.
                        FactionUtilityMethods.FlushUnitsFromReinforcementPoints( assessment.Target, faction, Context );
                        // Let the player know we're doing something, if our forces would matter.
                        if ( PlayerAligned && assessment.AttackPower > 5000 )
                        {
                            if ( !LastGameSecondForMessageAboutThisPlanet.GetHasKey( assessment.Target ) )
                                LastGameSecondForMessageAboutThisPlanet.AddPair( assessment.Target, 0 );
                            if ( World_AIW2.Instance.GameSecond - LastGameSecondForMessageAboutThisPlanet[assessment.Target] > 30 )
                            {
                                World_AIW2.Instance.QueueChatMessageOrCommand( $"Civilian Militia are attacking {assessment.Target.Name}.", ChatType.LogToCentralChat, Context );
                                LastGameSecondForMessageAboutThisPlanet[assessment.Target] = World_AIW2.Instance.GameSecond;
                            }
                        }

                        for ( int y = 0; y < militiaData.Ships.GetPairCount(); y++ )
                        {
                            for ( int z = 0; z < militiaData.Ships[y].Count; z++ )
                            {
                                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( militiaData.Ships[y][z] );
                                if ( entity == null || entity.LongRangePlanningData == null )
                                    continue;

                                // Skip centerpiece.
                                if ( centerpiece.PrimaryKeyID == entity.PrimaryKeyID )
                                    continue;

                                if ( entity.Planet.Index != assessment.Target.Index && entity.LongRangePlanningData.FinalDestinationPlanetIndex != assessment.Target.Index )
                                {
                                    if ( entity.Planet.Index != centerpiece.Planet.Index )
                                    {
                                        // Not yet on our target planet, and we're not yet on our centerpiece planet. Path to our centerpiece planet first.
                                        if ( entity.LongRangePlanningData.FinalDestinationPlanetIndex == centerpiece.Planet.Index )
                                            continue; // Stop if already moving towards it.

                                        entity.QueueWormholeCommand( faction, centerpiece.Planet, Context );
                                    }
                                    else
                                    {
                                        // Not yet on our target planet, and we're on our centerpice planet. Path to our target planet.
                                        entity.QueueWormholeCommand( faction, assessment.Target, Context );
                                    }
                                }
                            }
                        }
                    }
                }

                // If any of the planets involved in this attack are in other attacks, remove them from those other attacks.
                List<Planet> attackers = new List<Planet>();
                assessment.Attackers.DoFor( pair =>
                {
                    attackers.Add( pair.Key );

                    return DelReturn.Continue;
                } );
                for ( int i = 0; i < attackers.Count; i++ )
                    for ( int y = 1; y < attackAssessments.Count; y++ )
                    {
                        if ( attackAssessments[y].Attackers.GetHasKey( attackers[i] ) )
                            attackAssessments[y].Attackers.RemovePairByKey( attackers[i] );
                    }

                attackAssessments.RemoveAt( 0 );
                attackAssessments.Sort();
            }
            #endregion

            #region AtEase Actions
            // If we don't have an active defensive or offensive target, withdrawl back to the planet our leader is at.
            for ( int x = 0; x < factionData.MilitiaLeaders.Count; x++ )
            {
                GameEntity_Squad post = World_AIW2.Instance.GetEntityByID_Squad( factionData.MilitiaLeaders[x] );
                if ( post == null )
                    continue;

                CivilianMilitia militiaData = post.GetCivilianMilitiaExt( ExternalDataRetrieval.ReturnNullIfNotFound );

                if ( militiaData == null || militiaData.Status != CivilianMilitiaStatus.Patrolling )
                    continue;

                GameEntity_Squad centerpiece = World_AIW2.Instance.GetEntityByID_Squad( militiaData.Centerpiece );

                if ( centerpiece.PlanetFaction.Faction == faction )
                {
                    atEaseCommand.RelatedEntityIDs.Add( post.PrimaryKeyID );
                    if ( isPatrolling.Contains( post.Planet ) )
                        atEaseCommand.RelatedBools.Add( false );
                    else
                    {
                        atEaseCommand.RelatedBools.Add( true );

                        for ( int y = 0; y < militiaData.Ships.GetPairCount(); y++ )
                        {
                            for ( int z = 0; z < militiaData.Ships[y].Count; z++ )
                            {
                                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( militiaData.Ships[y][z] );
                                if ( entity == null )
                                    continue;

                                // Skip centerpiece.
                                if ( post.PrimaryKeyID == entity.PrimaryKeyID )
                                    continue;

                                // If we're not home, return.
                                if ( entity.Planet.Index != centerpiece.Planet.Index )
                                {
                                    // Wait a bit on planets, to look for any late threat.
                                    if ( entity.GetSecondsSinceEnteringThisPlanet() < 60 )
                                        continue;

                                    if ( entity.LongRangePlanningData.FinalDestinationPlanetIndex != centerpiece.Planet.Index )
                                        entity.QueueWormholeCommand( faction, post.Planet, Context );
                                }
                                else if ( entity.GetDistanceTo_VeryCheapButExtremelyRough( post, true ) > 2500 )
                                    entity.QueueMovementCommand( post.WorldLocation );
                                else
                                {
                                    storeAtEaseUnitsCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                                    storeAtEaseUnitsCommand.RelatedIntegers.Add( post.PrimaryKeyID );
                                    storeAtEaseUnitsCommand.RelatedIntegers2.Add( y );
                                }
                            }
                        }
                    }
                }
                else
                {
                    atEaseCommand.RelatedEntityIDs.Add( centerpiece.PrimaryKeyID );
                    if ( isPatrolling.Contains( centerpiece.Planet ) )
                        atEaseCommand.RelatedBools.Add( false );
                    else
                    {
                        atEaseCommand.RelatedBools.Add( true );

                        // We're part of a Battlestation group. Simply attempt to get to its planet.
                        for ( int y = 0; y < militiaData.Ships.GetPairCount(); y++ )
                        {
                            for ( int z = 0; z < militiaData.Ships[y].Count; z++ )
                            {
                                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( militiaData.Ships[y][z] );
                                if ( entity == null )
                                    continue;

                                // Skip centerpiece.
                                if ( centerpiece.PrimaryKeyID == entity.PrimaryKeyID )
                                    continue;

                                // If we're not home, return.
                                if ( entity.Planet.Index != centerpiece.Planet.Index )
                                {
                                    if ( entity.LongRangePlanningData.FinalDestinationPlanetIndex != centerpiece.Planet.Index )
                                        entity.QueueWormholeCommand( faction, centerpiece.Planet, Context );
                                }
                            }
                        }
                    }
                }
            }
            #endregion
            Context.QueueCommandForSendingAtEndOfContext( atEaseCommand );
            Context.QueueCommandForSendingAtEndOfContext( storeAtEaseUnitsCommand );
            Engine_Universal.NewTimingsBeingBuilt.FinishRememberingFrame( FramePartTimings.TimingType.ShortTermBackgroundThreadEntry, "DoMilitiaThreatReaction" );
        }

        // Update ship and turret caps for militia buildings.
        public void UpdateUnitCaps( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand updateCommand = StaticMethods.CreateGameCommand( Commands.SetMilitiaCaps.ToString(), GameCommandSource.AnythingElse, faction );

            // Count the number of militia barracks for each planet.
            ArcenSparseLookup<Planet, FInt> barrackBonusByPlanet = new ArcenSparseLookup<Planet, FInt>();
            ArcenSparseLookup<Planet, int> barrackBonusProtectorsByPlanet = new ArcenSparseLookup<Planet, int>();
            World_AIW2.Instance.DoForPlanets( false, planet =>
             {
                 planet.DoForEntities( "MilitiaBarracks", delegate ( GameEntity_Squad building )
                 {
                     if ( building.SelfBuildingMetalRemaining <= 0 && building.SecondsSpentAsRemains <= 0 )
                     {
                         if ( barrackBonusByPlanet.GetHasKey( planet ) )
                             barrackBonusByPlanet[planet] += 15 + (5 * building.CurrentMarkLevel);
                         else
                             barrackBonusByPlanet.AddPair( planet, FInt.FromParts( 15 + (5 * building.CurrentMarkLevel), 000 ) / 100 );
                         if ( building.CurrentMarkLevel >= 7 )
                             if ( barrackBonusProtectorsByPlanet.GetHasKey( planet ) )
                                 barrackBonusProtectorsByPlanet[planet]++;
                             else
                                 barrackBonusProtectorsByPlanet.AddPair( planet, 1 );
                     }
                     return DelReturn.Continue;
                 } );
                 return DelReturn.Continue;
             } );
            // Handle once for each militia leader.
            for ( int x = 0; x < factionData.MilitiaLeaders.Count; x++ )
            {
                // Load its ship and status.
                GameEntity_Squad militiaShip = World_AIW2.Instance.GetEntityByID_Squad( factionData.MilitiaLeaders[x] );
                if ( militiaShip == null )
                    continue;
                CivilianMilitia militiaStatus = militiaShip.GetCivilianMilitiaExt( ExternalDataRetrieval.ReturnNullIfNotFound );
                if ( militiaStatus == null )
                    continue;
                if ( militiaStatus.Status == CivilianMilitiaStatus.Defending )
                {
                    // For each type of unit, process.
                    for ( int y = 0; y < (int)CivilianResource.Length; y++ )
                    {
                        if ( militiaStatus.ShipTypeDataNames[y] == "none" )
                            continue; // Skip if not yet loaded.

                        GameEntityTypeData turretData = GameEntityTypeDataTable.Instance.GetRowByName( militiaStatus.ShipTypeDataNames[y] );

                        int capacity = (factionData.GetCap( faction ) / (FInt.Create( turretData.GetForMark( faction.GetGlobalMarkLevelForShipLine( turretData ) ).StrengthPerSquad_Original_DoesNotIncreaseWithMarkLevel, true ) / 10)).GetNearestIntPreferringHigher();
                        capacity = (int)(capacity * (militiaStatus.CapMultiplier / 100.0));

                        FInt bonus = FInt.One;
                        militiaShip.Planet.DoForLinkedNeighborsAndSelf( false, delegate ( Planet otherPlanet )
                        {
                            if ( barrackBonusByPlanet.GetHasKey( otherPlanet ) )
                                bonus += barrackBonusByPlanet[otherPlanet];
                            return DelReturn.Continue;
                        } );

                        if ( bonus > 1 )
                            capacity = Math.Max( capacity + 1, (capacity * bonus).GetNearestIntPreferringHigher() );

                        if ( capacity != militiaStatus.ShipCapacity[y] )
                        {
                            updateCommand.RelatedEntityIDs.Add( militiaShip.PrimaryKeyID );
                            updateCommand.RelatedIntegers.Add( y );
                            updateCommand.RelatedIntegers2.Add( capacity );
                        }
                    }
                }
                else if ( militiaStatus.Status == CivilianMilitiaStatus.Patrolling ) // If patrolling, do unit spawning.
                {
                    // For each type of unit, get ship count.
                    for ( int y = 0; y < (int)CivilianResource.Length; y++ )
                    {
                        if ( militiaStatus.ShipTypeDataNames[y] == "none" )
                            continue; // Skip if not yet loaded.

                        GameEntityTypeData shipData = GameEntityTypeDataTable.Instance.GetRowByName( militiaStatus.ShipTypeDataNames[y] );

                        // If advanced, simply set to 1.
                        if ( militiaShip.TypeData.GetHasTag( "BuildsProtectors" ) )
                        {
                            int protCap = 1;

                            militiaShip.Planet.DoForLinkedNeighborsAndSelf( false, otherPlanet =>
                            {
                                if ( barrackBonusProtectorsByPlanet.GetHasKey( otherPlanet ) )
                                    protCap += barrackBonusProtectorsByPlanet[otherPlanet];

                                return DelReturn.Continue;
                            } );

                            if ( militiaStatus.ShipCapacity[y] != protCap )
                            {
                                updateCommand.RelatedEntityIDs.Add( militiaShip.PrimaryKeyID );
                                updateCommand.RelatedIntegers.Add( y );
                                updateCommand.RelatedIntegers2.Add( protCap );
                            }
                            continue;
                        }
                        int capacity = (factionData.GetCap( faction ) / (FInt.Create( shipData.GetForMark( faction.GetGlobalMarkLevelForShipLine( shipData ) ).StrengthPerSquad_Original_DoesNotIncreaseWithMarkLevel, true ) / 10)).GetNearestIntPreferringHigher();
                        capacity = (int)(capacity * (militiaStatus.CapMultiplier / 100.0));

                        FInt bonus = FInt.One;
                        militiaShip.Planet.DoForLinkedNeighborsAndSelf( false, delegate ( Planet otherPlanet )
                        {
                            if ( barrackBonusByPlanet.GetHasKey( otherPlanet ) )
                                bonus += barrackBonusByPlanet[otherPlanet];
                            return DelReturn.Continue;
                        } );

                        if ( bonus > 1 )
                            capacity = Math.Max( capacity + 1, (capacity * bonus).GetNearestIntPreferringHigher() );

                        if ( capacity != militiaStatus.ShipCapacity[y] )
                        {
                            updateCommand.RelatedEntityIDs.Add( militiaShip.PrimaryKeyID );
                            updateCommand.RelatedIntegers.Add( y );
                            updateCommand.RelatedIntegers2.Add( capacity );
                        }
                    }
                }
            }

            if ( updateCommand.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( updateCommand );
        }

        // Do NOT directly change anything from this function. Doing so may cause desyncs in multiplayer.
        // What you can do from here is queue up game commands for units, and send them to be done via QueueCommandForSendingAtEndOfContext.
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( faction.MustBeAwakenedByPlayer && !faction.HasBeenAwakenedByPlayer )
                return;

            if ( factionData == null )
                return; // Wait until we have our faction data ready to go.

            CalculateThreat( faction, Context );
            FindNextTradeStationExpansionPoint( faction, Context );
            DoCargoShipMovement( faction, Context );
            DoMilitiaConstructionShipMovement( faction, Context );
            DoMilitiaThreatReaction( faction, Context );
            UpdateUnitCaps( faction, Context );

            // Execute all of our movement commands.
            faction.ExecuteMovementCommands( Context );
            faction.ExecuteWormholeCommands( Context );
        }

        // Check for our stuff dying.
        public override void DoOnAnyDeathLogic( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenSimContext Context )
        {
            // Skip if the ship was not defined by our mod.
            // Things like spawnt patrol ships and turrets don't need to be processed for death here.
            if ( !entity.TypeData.GetHasTag( "CivilianIndustryEntity" ) )
                return;

            if ( factionData == null && entity.PlanetFaction.Faction.Implementation is SpecialFaction_SKCivilianIndustry )
                factionData = entity.PlanetFaction.Faction.GetCivilianFactionExt( ExternalDataRetrieval.CreateIfNotFound );

            // Deal with its death.
            if ( factionData.GrandStation == entity )
            {
                factionData.GrandStationRebuildTimerInSeconds = 600;
                factionData.GrandStation = null;
            }

            // Everything else; simply remove it from its respective list(s).
            if ( factionData.TradeStations.Contains( entity.PrimaryKeyID ) )
            {
                factionData.TradeStations.Remove( entity.PrimaryKeyID );
                factionData.SetTradeStationRebuildTimer( entity.Planet, 300 );
            }

            factionData.RemoveCargoShip( entity.PrimaryKeyID );

            if ( factionData.MilitiaLeaders.Contains( entity.PrimaryKeyID ) )
                factionData.MilitiaLeaders.Remove( entity.PrimaryKeyID );

            // Save any changes.
            entity.PlanetFaction.Faction.SetCivilianFactionExt( factionData );
        }
    }
}
