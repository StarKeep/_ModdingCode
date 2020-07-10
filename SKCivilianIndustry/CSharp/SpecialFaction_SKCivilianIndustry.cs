using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using SKCivilianIndustry.Storage;
using System;
using System.Collections.Generic;

namespace SKCivilianIndustry
{
    // The main faction class.
    public class SpecialFaction_SKCivilianIndustry : BaseSpecialFaction
    {
        // Information required for our faction.
        // General identifier for our faction.
        protected override string TracingName => "SKCivilianIndustry";

        // Let the game know we're going to want to use the DoLongRangePlanning_OnBackgroundNonSimThread_Subclass function.
        // This function is generally used for things that do not need to always run, such as navigation requests.
        protected override bool EverNeedsToRunLongRangePlanning => true;

        // The following can be set to limit the number of times the background thread can be ran.
        //protected override int MinimumSecondsBetweenLongRangePlannings => 5;

        // Various constants
        private static int OUTPOST_DISTANCE = 3000;

        // Unit internal names.
        public static readonly string GRANDSTATION = "GrandStation";
        public static readonly string TRADESTATION = "TradeStation";
        public static readonly string CONSTRUCTION_POST = "CivConstructionPost";
        public static readonly string MILITIA_OUTPOST = "MilitiaOutpost";
        public static readonly string PATROL_POST = "PatrolPost";

        private GameEntity_Squad GrandStation;
        private ArcenSparseLookup<Planet, GameEntity_Squad> TradeStations;
        private List<GameEntity_Squad> ConstructionPosts;

        private CivProtectionData protData;

        // Set up initial relationships.
        public override void SetStartingFactionRelationships( Faction faction )
        {
            base.SetStartingFactionRelationships( faction );

            // Start by becoming hostile to everybody.
            enemyThisFactionToAll( faction );

            // Than do our intial relationship step.
            UpdateAllegiance( faction );
        }

        // Update relationships and settings.
        private void UpdateAllegiance( Faction faction )
        {
            // Set relationships.
            switch ( faction.Ex_MinorFactionCommon_GetPrimitives().Allegiance )
            {
                case "AI Team":
                    allyThisFactionToAI( faction );
                    break;
                case "Minor Faction Team Red":
                case "Minor Faction Team Blue":
                case "Minor Faction Team Green":
                    allyThisFactionToMinorFactionTeam( faction, faction.Ex_MinorFactionCommon_GetPrimitives().Allegiance );
                    break;
                default:
                    allyThisFactionToHumans( faction );
                    break;
            }
        }

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( protData == null )
                protData = faction.GetProtectionData();

            GrandStation = null;
            TradeStations = new ArcenSparseLookup<Planet, GameEntity_Squad>();
            ConstructionPosts = new List<GameEntity_Squad>();

            faction.Entities.DoForEntities( ( GameEntity_Squad entity ) =>
            {
                if ( entity.GetConstructionData().SecondsLeft >= 0 )
                    ConstructionPosts.Add( entity );

                if ( entity.TypeData.InternalName == GRANDSTATION || entity.GetConstructionData().InternalName == GRANDSTATION )
                    GrandStation = entity;
                else if ( entity.TypeData.InternalName == TRADESTATION || entity.GetConstructionData().InternalName == TRADESTATION )
                    TradeStations.AddPair( entity.Planet, entity );

                return DelReturn.Continue;
            } );

            protData.EntitiesToProtect = new ArcenSparseLookup<GameEntity_Squad, GameEntity_Squad>();
            UpdateEntitiesToProtect();
        }

        private void UpdateEntitiesToProtect()
        {
            for ( int x = 0; x < protData.EntitiesToProtectRaw.GetPairCount(); x++ )
            {
                int entityToProtectRaw = protData.EntitiesToProtectRaw.GetPairByIndex( x ).Key;
                GameEntity_Squad entityToProtect = World_AIW2.Instance.GetEntityByID_Squad( entityToProtectRaw );
                if ( entityToProtect == null )
                {
                    protData.EntitiesToProtectRaw.RemovePairByKey( entityToProtectRaw );
                    x--;
                    continue;
                }
                GameEntity_Squad protectingEntity = World_AIW2.Instance.GetEntityByID_Squad( protData.EntitiesToProtectRaw[entityToProtectRaw] );
                protData.EntitiesToProtect.AddPair( entityToProtect, protectingEntity );
            }
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( faction.MustBeAwakenedByPlayer && !faction.HasBeenAwakenedByPlayer )
                return;

            CreateGrandStation( faction, Context );

            if ( GrandStation == null )
                return;

            protData.DecreaseBuildTimers();

            CreateTradeStations( faction, Context );

            CreateDefensiveBuildings( faction, Context );

            UpdateConstructionPosts( faction, Context );
        }

        public void CreateGrandStation( Faction faction, ArcenSimContext Context )
        {
            if ( GrandStation != null )
                return;

            // Find a planet that has a friendly strength advantage that we're friendly to.
            World_AIW2.Instance.DoForPlanets( false, workingPlanet =>
            {
                LongRangePlanningData_PlanetFaction workingData = workingPlanet.LongRangePlanningData.PlanetFactionDataByIndex[faction.FactionIndex];
                if ( workingData == null )
                    return DelReturn.Continue;

                if ( !protData.GetCanBuild( workingPlanet ) )
                    return DelReturn.Continue;

                int friendlyStrength = workingData.DataByStance[FactionStance.Self].TotalStrength + workingData.DataByStance[FactionStance.Friendly].TotalStrength;
                if ( friendlyStrength / 2 > workingData.DataByStance[FactionStance.Hostile].TotalStrength )
                {
                    // Found a planet that they have majority control over. Spawn around a strong stationary friendly unit.
                    GameEntity_Squad bestEntity = null;
                    bool foundCenterpiece = false, foundStationary = false;
                    workingPlanet.DoForEntities( delegate ( GameEntity_Squad allignedSquad )
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
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( CONSTRUCTION_POST );

                    // Get the total radius of both our grand station and the king unit.
                    // This will be used to find a safe spawning location.
                    int radius = entityData.ForMark[Balance_MarkLevelTable.MaxOrdinal].Radius + bestEntity.TypeData.ForMark[Balance_MarkLevelTable.MaxOrdinal].Radius;

                    // Get the spawning coordinates for our start station.
                    ArcenPoint spawnPoint = ArcenPoint.ZeroZeroPoint;
                    int outerMax = 1;
                    do
                    {
                        outerMax++;
                        spawnPoint = bestEntity.Planet.GetSafePlacementPoint( Context, GameEntityTypeDataTable.Instance.GetRowByName( GRANDSTATION ), bestEntity.WorldLocation, radius, radius * outerMax );
                    } while ( spawnPoint == ArcenPoint.ZeroZeroPoint );

                    // Get the planetary faction to spawn our station in as.
                    PlanetFaction pFaction = bestEntity.Planet.GetPlanetFactionForFaction( faction );

                    // Spawn in our constructor.
                    GameEntity_Squad constructor = GameEntity_Squad.CreateNew( pFaction, entityData, entityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0, spawnPoint, Context );

                    // Set up our constructor.
                    CivConstructionData constructorData = constructor.GetConstructionData();
                    constructorData.InternalName = GRANDSTATION;
                    constructorData.SecondsLeft = 60;
                    constructorData.BuiltFor = bestEntity.PrimaryKeyID;

                    // Set a cooldown on the planet.
                    protData.AddToBuildTimer( constructor.Planet, 90 );

                    return DelReturn.Break;
                }
                return DelReturn.Continue;
            } );

        }

        public void CreateTradeStations( Faction faction, ArcenSimContext Context )
        {
            World_AIW2.Instance.DoForPlanets( false, workingPlanet =>
            {
                // Skip if we already have a trade station on the planet.
                if ( TradeStations.GetHasKey( workingPlanet ) )
                    return DelReturn.Continue;

                if ( !protData.GetCanBuild( workingPlanet ) )
                    return DelReturn.Continue;

                LongRangePlanningData_PlanetFaction workingData = workingPlanet.LongRangePlanningData.PlanetFactionDataByIndex[faction.FactionIndex];
                if ( workingData == null )
                    return DelReturn.Break;
                int friendlyStrength = workingData.DataByStance[FactionStance.Self].TotalStrength + workingData.DataByStance[FactionStance.Friendly].TotalStrength;
                if ( friendlyStrength / 2 > workingData.DataByStance[FactionStance.Hostile].TotalStrength )
                {
                    // Found a planet that they have majority control over. Spawn around a stationary friendly unit.
                    GameEntity_Squad bestEntity = null;
                    bool foundCenterpiece = false, foundStationary = false;
                    workingPlanet.DoForEntities( delegate ( GameEntity_Squad allignedSquad )
                    {
                        // Default to the first stationary.
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

                    // No trade station found for this planet. Create one.
                    // Load in our trade station's data.
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( CONSTRUCTION_POST );

                    // Get the total radius of both our trade station, and the command station.
                    // This will be used to find a safe spawning location.
                    int radius = entityData.ForMark[Balance_MarkLevelTable.MaxOrdinal].Radius + bestEntity.TypeData.ForMark[Balance_MarkLevelTable.MaxOrdinal].Radius;

                    // Get the spawning coordinates for our trade station.
                    ArcenPoint spawnPoint = ArcenPoint.ZeroZeroPoint;
                    int outerMax = 1;
                    do
                    {
                        outerMax++;
                        spawnPoint = workingPlanet.GetSafePlacementPoint( Context, GameEntityTypeDataTable.Instance.GetRowByName( TRADESTATION ), bestEntity.WorldLocation, radius, radius * outerMax );
                    } while ( spawnPoint == ArcenPoint.ZeroZeroPoint );

                    // Get the planetary faction to spawn our trade station in as.
                    PlanetFaction pFaction = workingPlanet.GetPlanetFactionForFaction( faction );

                    // Spawn in our constructor.
                    GameEntity_Squad constructor = GameEntity_Squad.CreateNew( pFaction, entityData, entityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0, spawnPoint, Context );

                    // Set up our constructor.
                    CivConstructionData constructorData = constructor.GetConstructionData();
                    constructorData.InternalName = TRADESTATION;
                    constructorData.SecondsLeft = 120;
                    constructorData.BuiltFor = bestEntity.PrimaryKeyID;

                    // Set a cooldown on the planet.
                    protData.AddToBuildTimer( constructor.Planet, 30 );
                }
                return DelReturn.Continue;
            } );

        }

        public void CreateDefensiveBuildings( Faction faction, ArcenSimContext Context )
        {
            for ( int y = 0; y < protData.EntitiesToProtect.GetPairCount(); y++ )
            {
                GameEntity_Squad entity = protData.EntitiesToProtect.GetPairByIndex( y ).Key;
                if ( protData.GetIsProtected( entity ) )
                    continue;

                Planet planet = entity.Planet;
                if ( !protData.GetCanBuild( planet ) )
                    continue;

                GameEntity_Squad tradeStation = TradeStations.GetHasKey( planet ) ? TradeStations[planet] : null;
                if ( tradeStation == null )
                    continue;

                CreatePatrolPosts( planet, entity, faction, Context );
            }
        }

        public void CreateMilitiaOutposts( GameEntity_Squad tradeStation, Planet planet, GameEntity_Base wormhole, Faction faction, ArcenSimContext Context )
        {
            ArcenPoint defensivePoint = tradeStation.WorldLocation.GetPointAtAngleAndDistance( tradeStation.WorldLocation.GetAngleToDegrees( wormhole.WorldLocation ), OUTPOST_DISTANCE );

            // Load in our outpost's data.
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( CONSTRUCTION_POST );

            // Get the planetary faction to spawn our outpost in as.
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );

            // Spawn in our constructor.
            GameEntity_Squad constructor = GameEntity_Squad.CreateNew( pFaction, entityData, entityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0, defensivePoint, Context );

            // Set up our constructor.
            CivConstructionData constructorData = constructor.GetConstructionData();
            constructorData.InternalName = MILITIA_OUTPOST;
            constructorData.SecondsLeft = 120;
            constructorData.BuiltFor = wormhole.PrimaryKeyID;

            // Set a cooldown on the planet.
            protData.AddToBuildTimer( planet, 30 );

            // Let our faction know we're protecting it.
            protData.EntitiesToProtectRaw[wormhole.PrimaryKeyID] = constructor.PrimaryKeyID;
        }

        public void CreatePatrolPosts( Planet planet, GameEntity_Squad entityToProtect, Faction faction, ArcenSimContext Context )
        {
            // Load in our outpost's data.
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( CONSTRUCTION_POST );

            // Get the planetary faction to spawn our outpost in as.
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );

            // Spawn in our constructor.
            GameEntity_Squad constructor = GameEntity_Squad.CreateNew( pFaction, entityData, entityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0,
                planet.GetSafePlacementPoint( Context, entityData, entityToProtect.WorldLocation, 0, 1000 ), Context );

            // Set up our constructor.
            CivConstructionData constructorData = constructor.GetConstructionData();
            constructorData.InternalName = PATROL_POST;
            constructorData.SecondsLeft = 120;
            constructorData.BuiltFor = entityToProtect.PrimaryKeyID;

            // Set a cooldown on the planet.
            protData.AddToBuildTimer( planet, 30 );

            // Let our faction know we're protecting it.
            protData.EntitiesToProtectRaw[entityToProtect.PrimaryKeyID] = constructor.PrimaryKeyID;
        }

        private void UpdateConstructionPosts( Faction faction, ArcenSimContext Context )
        {
            for ( int x = 0; x < ConstructionPosts.Count; x++ )
            {
                GameEntity_Squad constructor = ConstructionPosts[x];
                CivConstructionData constructorData = constructor.GetConstructionData();

                GameEntity_Squad sourceEntity = World_AIW2.Instance.GetEntityByID_Squad( constructorData.BuiltFor );
                if ( sourceEntity == null )
                {
                    constructor.SetConstructionData( new CivConstructionData() );
                    continue;
                }

                GameEntityTypeData entityToBecome = GameEntityTypeDataTable.Instance.GetRowByName( constructorData.InternalName );
                if ( entityToBecome == null )
                {
                    constructor.SetConstructionData( new CivConstructionData() );
                    continue;
                }

                bool isProtector = entityToBecome.InternalName == MILITIA_OUTPOST || entityToBecome.InternalName == PATROL_POST;

                constructorData.SecondsLeft = Math.Max( 0, constructorData.SecondsLeft - 1 );
                if ( constructorData.SecondsLeft == 0 )
                {
                    GameEntity_Squad entity = constructor.TransformInto( Context, entityToBecome, 1 );
                    entity.SetConstructionData( new CivConstructionData() );

                    if ( isProtector )
                        protData.EntitiesToProtectRaw[sourceEntity.PrimaryKeyID] = entity.PrimaryKeyID;
                }
            }
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            GetEntitiesToProtect( faction );
        }

        private void GetEntitiesToProtect( Faction faction )
        {
            for ( int x = 0; x < TradeStations.GetPairCount(); x++ )
            {
                Planet planet = TradeStations.GetPairByIndex( x ).Key;

                if ( TradeStations[planet] == null )
                    continue;

                LongRangePlanningData_PlanetFaction workingData = planet.LongRangePlanningData.PlanetFactionDataByIndex[faction.FactionIndex];
                if ( workingData == null )
                    continue;

                int friendlyStrength = workingData.DataByStance[FactionStance.Self].TotalStrength + workingData.DataByStance[FactionStance.Friendly].TotalStrength;
                if ( friendlyStrength / 2 > workingData.DataByStance[FactionStance.Hostile].TotalStrength )
                {
                    planet.DoForEntities( ( GameEntity_Squad entity ) =>
                    {
                        if ( protData.EntitiesToProtectRaw.GetHasKey( entity.PrimaryKeyID ) )
                            return DelReturn.Continue;

                        if ( entity.TypeData.IsMobile || entity.PlanetFaction.Faction.GetIsHostileTowards( faction ) )
                            return DelReturn.Continue;

                        if ( entity.TypeData.GetHasTag( "MetalGenerator" ) )
                            protData.EntitiesToProtectRaw.AddPair( entity.PrimaryKeyID, -1 );

                        return DelReturn.Continue;
                    } );
                }
            }
        }
    }
}
