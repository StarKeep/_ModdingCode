using System;
using System.Collections.Generic;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace MacrophageHistiocytes.SpecialFaction
{
    public class MacrophageInfestationHistiocytes : SpecialFaction_MacrophageInfestation, IBulkPathfinding
    {
        public override void SeedStartingEntities_EarlyMajorFactionClaimsOnly( Faction faction, Galaxy galaxy, ArcenSimContext Context, MapTypeData mapType )
        {
            switch ( faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).SpawningOptions )
            {
                case "Hives":
                    break;
                default:
                    base.SeedStartingEntities_EarlyMajorFactionClaimsOnly( faction, galaxy, Context, mapType );
                    break;
            }
        }
        public override void SeedStartingEntities_LaterEverythingElse( Faction faction, Galaxy galaxy, ArcenSimContext Context, MapTypeData mapType )
        {
            switch ( faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).SpawningOptions )
            {
                case "Hives":
                    HandleHiveSpawning( faction, galaxy, Context, mapType );
                    break;
                default:
                    break;
            }
        }
        public void HandleHiveSpawning( Faction faction, Galaxy galaxy, ArcenSimContext Context, MapTypeData mapType )
        {
            byte Intensity = faction.Ex_MinorFactionCommon_GetPrimitives(ExternalDataRetrieval.CreateIfNotFound).Intensity;
            int planets = 0;
            galaxy.DoForPlanets( false, planet => { planets++; return DelReturn.Continue; } );
            int planetsToClaim = (planets / FInt.FromParts( 100, 000 ) * faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).NumberToSeed).GetNearestIntPreferringHigher();
            ArcenDebugging.SingleLineQuickDebug( $"{planets} {faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).NumberToSeed} {planetsToClaim}" );
            if ( planetsToClaim < 1 )
                planetsToClaim = 1;
            List<Planet> planetsSeededOn = Mapgen_Base.Mapgen_SeedSpecialEntities( Context, galaxy, faction, SpecialEntityType.None, TeliumTag, SeedingType.HardcodedCount, planetsToClaim,
                                                                          MapGenCountPerPlanet.One, MapGenSeedStyle.SmallBad, 3, 3, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal );
            for ( int x = 0; x < planetsSeededOn.Count; x++ )
            {
                Planet planet = planetsSeededOn[x];
                planet.DoForEntities( ( GameEntity_Squad entity ) =>
                {
                    if ( entity.PlanetFaction.Faction.Implementation is Macrophage )
                        return DelReturn.Continue;

                    entity.Despawn( Context, true, InstancedRendererDeactivationReason.IFinishedMyJob );

                    return DelReturn.Continue;
                } );

                for ( int j = 0; j < planet.Factions.Count; j++ )
                {
                    planet.Factions[j].AIPLeftFromControlling = FInt.Zero;
                    planet.Factions[j].AIPLeftFromCommandStation = FInt.Zero;
                    planet.Factions[j].AIPLeftFromWarpGate = FInt.Zero;
                }

                int extraHives = (2 + Intensity / 3);
                extraHives = Context.RandomToUse.Next( extraHives / 2, extraHives * 2 );
                int mines = (12 + Intensity / 2);
                mines = Context.RandomToUse.Next( mines / 2, mines );

                for ( int y = 0; y < extraHives; y++ )
                    planet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, TeliumTag ), PlanetSeedingZone.MostAnywhere );

                for ( int y = 0; y < mines; y++ )
                    planet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MetalGeneratorInfested" ), PlanetSeedingZone.MostAnywhere );
            }
        }
        public static new void CalculateGatheringPoints( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            // If our tamed subfaction doesn't exist, make sure its not loaded. Safety for old saves.
            if ( World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( MacrophageInfestationTamedHistiocytes ) ) == null )
                Tamed = null;

            bool ready = false;
            if ( Wilds != null )
                for ( int x = 0; x < Wilds.Count && ready == false; x++ )
                    if ( Wilds[x].Telia.GetPairCount() > 0 )
                        ready = true;

            if ( !ready && Tamed != null && Tamed.Telia.GetPairCount() > 0 )
                ready = true;

            if ( !ready )
                return; // Stop if none of our Subfactions are ready; due to either game load or full death.

            // Find any planet that is midway between our existing Telia.
            // Start by getting all of our planets with Telia into a list.
            List<Planet> teliumPlanets = new List<Planet>();
            if ( Wilds != null )
                for ( int x = 0; x < Wilds.Count; x++ )
                {
                    SpecialFaction_MacrophageInfestation workingWild = Wilds[x];
                    if ( workingWild == null || workingWild.Telia == null || workingWild.Telia.GetPairCount() == 0 )
                        continue;
                    for ( int y = 0; y < workingWild.Telia.GetPairCount(); y++ )
                    {
                        ArcenSparseLookupPair<GameEntity_Squad, MacrophagePerTeliumData> pair = workingWild.Telia.GetPairByIndex( y );
                        Planet workingPlanet = pair.Key.Planet;
                        if ( !teliumPlanets.Contains( workingPlanet ) )
                            teliumPlanets.Add( workingPlanet );
                    }
                }
            if ( Tamed != null )
                for ( int x = 0; x < Tamed.Telia.GetPairCount(); x++ )
                {
                    ArcenSparseLookupPair<GameEntity_Squad, MacrophagePerTeliumData> otherPair = Tamed.Telia.GetPairByIndex( x );
                    Planet workingPlanet = otherPair.Key.Planet;
                    if ( !teliumPlanets.Contains( workingPlanet ) )
                        teliumPlanets.Add( workingPlanet );
                }

            // Next, reset our gathering points list.
            SporeGatheringPoints = new List<Planet>();

            // Finally, add every single midway point between our planets together.
            for ( int y = 0; y < teliumPlanets.Count; y++ )
            {
                Planet workingPlanet = teliumPlanets[y];
                for ( int z = y + 1; z < teliumPlanets.Count; z++ )
                {
                    Planet otherPlanet = teliumPlanets[z];
                    List<Planet> path = faction.FindPath( workingPlanet, otherPlanet, PathingMode.Default, Context );
                    if ( path.Count > 0 )
                    {
                        Planet midPlanet = path[path.Count / 2];
                        if ( !SporeGatheringPoints.Contains( midPlanet ) )
                        {
                            SporeGatheringPoints.Add( midPlanet );
                        }
                    }
                }
            }
        }
        public static new void SpawnTeliumIfAble( ArcenSimContext Context )
        {
            if ( SporesPerPlanet == null || SporesPerPlanet.GetPairCount() <= 0 )
                return; // Stop if already processed. This is cleared whenever we call this function, and is rebuilt once every second in each Macrophage's Stage2.

            // If our tamed subfaction doesn't exist, make sure its not loaded. Safety for old saves.
            if ( World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( MacrophageInfestationTamedHistiocytes ) ) == null )
                Tamed = null;

            List<GameEntity_Squad> teliaSoFar = new List<GameEntity_Squad>();

            for ( int i = 0; i < SporesPerPlanet.GetPairCount(); i++ )
            {
                //Check for spores from multiple Telia on one planet, and if so then call SpawnTeliumOnPlanet
                //and destroy all the spores on that planet
                teliaSoFar.Clear();
                ArcenSparseLookupPair<Planet, List<GameEntity_Squad>> pair = SporesPerPlanet.GetPairByIndex( i );
                bool tamedSporeOnPlanet = false;

                for ( int j = 0; j < pair.Value.Count; j++ )
                {
                    GameEntity_Squad spore = pair.Value[j];
                    // If the spore no longer exists, remove it and continue.
                    if ( World_AIW2.Instance.GetEntityByID_Squad( spore.PrimaryKeyID ) == null )
                    {
                        if ( debug )
                            ArcenDebugging.SingleLineQuickDebug( "Removing invalid spore." );
                        pair.Value.RemoveAt( j );
                        j--;
                        continue;
                    }

                    MacrophagePerSporeData sData = null;
                    if ( Wilds != null )
                        for ( int x = 0; x < Wilds.Count && sData == null; x++ )
                            if ( Wilds[x].Spores.GetHasKey( spore ) )
                                sData = Wilds[x].Spores[spore];

                    if ( sData == null && Tamed != null && Tamed.Spores.GetHasKey( spore ) )
                    {
                        sData = Tamed.Spores[spore];
                        tamedSporeOnPlanet = true;
                    }
                    if ( sData == null )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "BUG: per spore data is null in check", Verbosity.DoNotShow );
                        continue;
                    }
                    GameEntity_Squad workingTelium = World_AIW2.Instance.GetEntityByID_Squad( sData.TeliumID );
                    if ( workingTelium != null )
                    {
                        if ( !teliaSoFar.Contains( workingTelium ) )
                        {
                            teliaSoFar.Add( workingTelium );
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( pair.Key.Name + " spore " + j + " teliumId " + sData.TeliumID + " is new, add its telium to the list", Verbosity.DoNotShow );
                        }
                        else if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( pair.Key.Name + " spore " + j + " teliumId " + sData.TeliumID + " is NOT new, don't its telium to the list", Verbosity.DoNotShow );
                    }
                    else if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( sData.TeliumID + " telium is not found", Verbosity.DoNotShow );
                }

                int teliaCount = 0;
                if ( Wilds != null )
                    for ( int x = 0; x < Wilds.Count; x++ )
                        if ( Wilds[x].TeliaPlanets.GetHasKey( pair.Key ) )
                            teliaCount += Wilds[x].TeliaPlanets[pair.Key];

                if ( Tamed != null && Tamed.TeliaPlanets.GetHasKey( pair.Key ) )
                    teliaCount += Tamed.TeliaPlanets[pair.Key];

                if ( teliaSoFar.Count >= NumDifferentTeliaRequired )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "We have spores from " + teliaSoFar.Count + " different telia on " + pair.Key.Name + ", so spawn a new Telia", Verbosity.DoNotShow );
                    bool spawnt = false;
                    if ( tamedSporeOnPlanet && teliaCount < TeliaPerPlanetHigh ) // Always treat the Tamed as intensity 10.
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Our new Telia is being built for the Tamed subfaction on " + pair.Key.Name, Verbosity.DoNotShow );
                        Tamed.SpawnTeliumOnPlanet( Context, pair.Key, null, true ); // At least one tamed spore, spawn a tamed telium.
                        spawnt = true;
                    }
                    else
                    {
                        // No Tamed, select a random wild Telia that has a Spore here and is below its planetary Telium cap here, and spawn a Telium for its faction.
                        while ( teliaSoFar.Count > 0 )
                        {
                            int workingIndex = Context.RandomToUse.Next( teliaSoFar.Count );
                            GameEntity_Squad workingTelium = teliaSoFar[workingIndex];
                            teliaSoFar.RemoveAt( workingIndex );
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Our new Telia is being built for a Wild subfaction. The Telia " + workingTelium.PrimaryKeyID + " was chosen as priority.", Verbosity.DoNotShow );

                            SpecialFaction_MacrophageInfestation infestation = ((SpecialFaction_MacrophageInfestation)workingTelium.PlanetFaction.Faction.Implementation);

                            if ( teliaCount < infestation.TeliaPerPlanet )
                            {
                                infestation.SpawnTeliumOnPlanet( Context, pair.Key, workingTelium.PlanetFaction.Faction );
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "We have successfully spawnt the new Telium on " + pair.Key.Name, Verbosity.DoNotShow );
                                spawnt = true;
                                break;
                            }
                            else if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "We have failed to spawn the new Telium. Removing the invalid Telium and trying again.", Verbosity.DoNotShow );
                        }
                    }
                    if ( spawnt )
                    {
                        for ( int k = 0; k < pair.Value.Count; k++ )
                            pair.Value[k].Despawn( Context, true, InstancedRendererDeactivationReason.ThereWereTooManyOfMe );
                        // Now that we're finished, clear our SporesPerPlanet list so that other Macrophage factions cannot rerun this function again this second.
                        // We rebuild SporesPerPlanet every second in Stage2, so we can acquire Spores from every Macrophage subfaction before processing.
                        SporesPerPlanet.Clear();
                        return;
                    }
                }
                else if ( debug )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( pair.Key.Name + " has " + pair.Value.Count + " spores with " + teliaSoFar.Count + " diffferent telia represented, and we need " + NumDifferentTeliaRequired, Verbosity.DoNotShow );
                }
            }
        }
        public new void SpawnTeliumOnPlanet( ArcenSimContext Context, Planet planet, Faction spawnFaction, bool forTamed = false )
        {
            if ( forTamed )
                spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( Tamed.GetType() );
            if ( spawnFaction == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: Unable to find valid faction to spawn a telium; was looking for tamed: " + forTamed.ToString(), Verbosity.DoNotShow );
                spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( MacrophageInfestationHistiocytes ) );
            }
            bool debug = false;
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, TeliumTag );
            if ( entityData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: no macrophagetelium defined for spawning", Verbosity.DoNotShow );
                return;
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "FLAGFLAG Spawning a new telium on " + planet.Name, Verbosity.DoNotShow );
            int minRadius = (ExternalConstants.Instance.DistanceScale_GravwellRadius * FInt.FromParts( 0, 150 )).IntValue;
            int maxRadius = (ExternalConstants.Instance.DistanceScale_GravwellRadius * FInt.FromParts( 0, 300 )).IntValue;
            ArcenPoint spawnLocation = planet.GetSafePlacementPoint( Context, entityData, Engine_AIW2.Instance.CombatCenter, minRadius, maxRadius );
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( spawnFaction );
            /*GameEntity entity = */
            GameEntity_Squad.CreateNew( pFaction, entityData, entityData.MarkFor( pFaction ),
                pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context );
            if ( ArcenNetworkAuthority.GetIsHostMode() )
            {
                if ( planet.IntelLevel > PlanetIntelLevel.Unexplored ) //no warning if you don't have vision
                    World_AIW2.Instance.QueueChatMessageOrCommand( spawnFaction.StartFactionColourForLog() + "Macrophage</color> Telium spawned on " + planet.Name + "!", ChatType.LogToCentralChat, "ArkChiefOfStaff_TeliumSpawn", Context );
            }
            //update our spore gathering points
            GatheringPointsUpdated = false;
        }
        public override void UpdatePowerLevel( Faction faction )
        {
            faction.OverallPowerLevel = FInt.Zero;
            int generators = 0, harvesters = 0;
            faction.DoForEntities( ( GameEntity_Squad entity ) =>
            {
                if ( entity.TypeData.GetHasTag( "MetalGeneratorInfested" ) )
                    generators++;

                if ( entity.TypeData.GetHasTag( HarvesterTag ) )
                    harvesters++;

                return DelReturn.Continue;
            } );
            int weightedTotal = generators + harvesters * 2;
            if ( weightedTotal > 60 )
                faction.OverallPowerLevel = FInt.FromParts( 2, 000 );
            else if ( weightedTotal > 20 )
                faction.OverallPowerLevel = FInt.FromParts( 1, (weightedTotal - 20) * 25 );
            else
                faction.OverallPowerLevel = FInt.FromParts( 0, weightedTotal * 50 );
        }
        public ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> WormholeCommands { get; set; }
        public ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> MovementCommands { get; set; }
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );

            if ( FinishedLoadingLogic )
                switch ( faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).SpawningOptions )
                {
                    case "Hives":
                        TeliaPerPlanet = TeliaPerPlanetHigh * 2;
                        break;
                    default:
                        break;
                }

            if ( World_AIW2.Instance.GameSecond < 10 )
                return;

            // Convert metal generators.
            ConvertMetalGeneratorsIfAble( faction, Context );

            // Level up Metal Generators and Telia based on time alive.
            LevelUpEntitiesAndTheirDronesIfAble( faction, Context );

            if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "MacrophageDarkSpireRivalry" ) == true )
                HandleDarkSpireRivalryStage3Logic( faction, Context );
        }
        public void ConvertMetalGeneratorsIfAble( Faction faction, ArcenSimContext Context )
        {
            faction.DoForEntities( HarvesterTag, harvester =>
            {
                bool converted = false;
                harvester.Planet.GetPlanetFactionForFaction( World_AIW2.Instance.GetNeutralFaction() ).Entities.DoForEntities( "MetalGenerator", metalGenerator =>
                {
                    if ( metalGenerator.TypeData.GetHasTag( "MetalGeneratorInfested" ) )
                        return DelReturn.Continue; // Skip already infested ones.

                    if ( metalGenerator.GetSecondsSinceCreation() < 120 )
                        return DelReturn.Continue; // Cooldown between conversion.

                    if ( harvester.GetDistanceTo_VeryCheapButExtremelyRough( metalGenerator, true ) > 1000 )
                        return DelReturn.Continue;

                    GameEntity_Squad.CreateNew( harvester.PlanetFaction, GameEntityTypeDataTable.Instance.GetRowByName( "MetalGeneratorInfested" ), 1, harvester.PlanetFaction.FleetUsedAtPlanet, 0,
                        metalGenerator.WorldLocation, Context );

                    metalGenerator.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );

                    converted = true;

                    return DelReturn.RemoveAndContinue;
                } );

                if ( converted )
                    return DelReturn.Break;
                return DelReturn.Continue;
            } );
        }
        public void LevelUpEntitiesAndTheirDronesIfAble( Faction faction, ArcenSimContext Context )
        {
            faction.DoForEntities( TeliumTag, telium =>
            {
                if ( telium.CurrentMarkLevel >= 7 )
                    return DelReturn.Continue;

                byte effectiveMark = (byte)(Math.Min( 7, 1 + telium.GetSecondsSinceCreation() / 1800 ));

                telium.SetCurrentMarkLevelIfHigherThanCurrent( effectiveMark, Context );

                telium.FleetMembership.Fleet.DoForMemberGroups( mem => { mem.EffectiveMark = effectiveMark; return DelReturn.Continue; } );

                return DelReturn.Continue;
            } );
            faction.DoForEntities( "MetalGeneratorInfested", generator =>
            {
                if ( generator.CurrentMarkLevel >= 7 )
                    return DelReturn.Continue;

                byte effectiveMark = (byte)(Math.Min( 7, 1 + generator.GetSecondsSinceCreation() / 900 ));

                generator.SetCurrentMarkLevelIfHigherThanCurrent( effectiveMark, Context );

                generator.FleetMembership.Fleet.DoForMemberGroups( mem => { mem.EffectiveMark = effectiveMark; return DelReturn.Continue; } );

                return DelReturn.Continue;
            } );
            faction.DoForEntities( HarvesterTag, harvester =>
            {
                harvester.FleetMembership.Fleet.DoForMemberGroups( mem => { mem.EffectiveMark = harvester.CurrentMarkLevel; return DelReturn.Continue; } );

                return DelReturn.Continue;
            } );
        }
        public void HandleDarkSpireRivalryStage3Logic( Faction faction, ArcenSimContext Context )
        {
            FInt perSecondGeneratorDamage = FInt.FromParts( 0, 001 );
            FInt perSecondTeliumDecay = FInt.FromParts( 0, 003 );
            List<Planet> vgPlanets = new List<Planet>();
            World_AIW2.Instance.DoForEntities( "VengeanceGenerator", generator =>
            {
                if ( !vgPlanets.Contains( generator.Planet ) )
                    vgPlanets.Add( generator.Planet );

                generator.Planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( "MacrophageHistiocyte", histiocyte =>
                {
                    int distance = histiocyte.GetDistanceTo_VeryCheapButExtremelyRough( generator, true );
                    if ( distance < 3000 )
                    {
                        generator.TakeHullRepair( -(generator.GetMaxHullPoints() * perSecondGeneratorDamage).GetNearestIntPreferringHigher() );
                        if ( Context.RandomToUse.Next( generator.GetCurrentHullPoints(), generator.GetMaxHullPoints() ) < generator.GetMaxHullPoints() / 10 )
                            SpecialFaction_DarkSpire.PerformVengeanceStrike();
                    }

                    return DelReturn.Continue;
                } );

                return DelReturn.Continue;
            } );

            for ( int x = 0; x < vgPlanets.Count; x++ )
                vgPlanets[x].DoForLinkedNeighborsAndSelf( false, planet =>
                {
                    planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( TeliumTag, telium =>
                    {
                        telium.TakeHullRepair( -(telium.GetMaxHullPoints() * perSecondTeliumDecay).GetNearestIntPreferringHigher() );

                        return DelReturn.Continue;
                    } );

                    return DelReturn.Continue;
                } );
        }
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            base.DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( faction, Context );

            faction.DoForEntities( "MacrophageHistiocyte", entity =>
            {
                GameEntity_Squad host = entity.FleetMembership.Fleet.Centerpiece;
                if ( host == null )
                    return DelReturn.Continue;

                if ( entity.Planet != host.Planet )
                    entity.QueueWormholeCommand( host.Planet );
                else if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "MacrophageDarkSpireRivalry" ) == true )
                {
                    GameEntity_Squad generator = entity.Planet.GetFirstMatching( FactionType.SpecialFaction, "VengeanceGenerator", true, true );
                    if ( generator == null )
                        return DelReturn.Continue;

                    if ( entity.GetDistanceTo_VeryCheapButExtremelyRough( generator, true ) > 1000 )
                        entity.QueueMovementCommand( generator.WorldLocation );
                }

                return DelReturn.Continue;
            } );

            faction.ExecuteWormholeCommands( Context );
            faction.ExecuteMovementCommands( Context );

            BadgerFactionUtilityMethods.FlushUnitsFromReinforcementPointsOnAllRelevantPlanets( faction, Context );
        }
    }

    public class MacrophageInfestationTamedHistiocytes : SpecialFaction_MacrophageInfestationTamed, IBulkPathfinding
    {
        public static new void CalculateGatheringPoints( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            // If our tamed subfaction doesn't exist, make sure its not loaded. Safety for old saves.
            if ( World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( MacrophageInfestationTamedHistiocytes ) ) == null )
                Tamed = null;

            bool ready = false;
            if ( Wilds != null )
                for ( int x = 0; x < Wilds.Count && ready == false; x++ )
                    if ( Wilds[x].Telia.GetPairCount() > 0 )
                        ready = true;

            if ( !ready && Tamed != null && Tamed.Telia.GetPairCount() > 0 )
                ready = true;

            if ( !ready )
                return; // Stop if none of our Subfactions are ready; due to either game load or full death.

            // Find any planet that is midway between our existing Telia.
            // Start by getting all of our planets with Telia into a list.
            List<Planet> teliumPlanets = new List<Planet>();
            if ( Wilds != null )
                for ( int x = 0; x < Wilds.Count; x++ )
                {
                    SpecialFaction_MacrophageInfestation workingWild = Wilds[x];
                    if ( workingWild == null || workingWild.Telia == null || workingWild.Telia.GetPairCount() == 0 )
                        continue;
                    for ( int y = 0; y < workingWild.Telia.GetPairCount(); y++ )
                    {
                        ArcenSparseLookupPair<GameEntity_Squad, MacrophagePerTeliumData> pair = workingWild.Telia.GetPairByIndex( y );
                        Planet workingPlanet = pair.Key.Planet;
                        if ( !teliumPlanets.Contains( workingPlanet ) )
                            teliumPlanets.Add( workingPlanet );
                    }
                }
            if ( Tamed != null )
                for ( int x = 0; x < Tamed.Telia.GetPairCount(); x++ )
                {
                    ArcenSparseLookupPair<GameEntity_Squad, MacrophagePerTeliumData> otherPair = Tamed.Telia.GetPairByIndex( x );
                    Planet workingPlanet = otherPair.Key.Planet;
                    if ( !teliumPlanets.Contains( workingPlanet ) )
                        teliumPlanets.Add( workingPlanet );
                }

            // Next, reset our gathering points list.
            SporeGatheringPoints = new List<Planet>();

            // Finally, add every single midway point between our planets together.
            for ( int y = 0; y < teliumPlanets.Count; y++ )
            {
                Planet workingPlanet = teliumPlanets[y];
                for ( int z = y + 1; z < teliumPlanets.Count; z++ )
                {
                    Planet otherPlanet = teliumPlanets[z];
                    List<Planet> path = faction.FindPath( workingPlanet, otherPlanet, PathingMode.Default, Context );
                    if ( path.Count > 0 )
                    {
                        Planet midPlanet = path[path.Count / 2];
                        if ( !SporeGatheringPoints.Contains( midPlanet ) )
                        {
                            SporeGatheringPoints.Add( midPlanet );
                        }
                    }
                }
            }
        }
        public static new void SpawnTeliumIfAble( ArcenSimContext Context )
        {
            if ( SporesPerPlanet == null || SporesPerPlanet.GetPairCount() <= 0 )
                return; // Stop if already processed. This is cleared whenever we call this function, and is rebuilt once every second in each Macrophage's Stage2.

            // If our tamed subfaction doesn't exist, make sure its not loaded. Safety for old saves.
            if ( World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( MacrophageInfestationTamedHistiocytes ) ) == null )
                Tamed = null;

            List<GameEntity_Squad> teliaSoFar = new List<GameEntity_Squad>();

            for ( int i = 0; i < SporesPerPlanet.GetPairCount(); i++ )
            {
                //Check for spores from multiple Telia on one planet, and if so then call SpawnTeliumOnPlanet
                //and destroy all the spores on that planet
                teliaSoFar.Clear();
                ArcenSparseLookupPair<Planet, List<GameEntity_Squad>> pair = SporesPerPlanet.GetPairByIndex( i );
                bool tamedSporeOnPlanet = false;

                for ( int j = 0; j < pair.Value.Count; j++ )
                {
                    GameEntity_Squad spore = pair.Value[j];
                    // If the spore no longer exists, remove it and continue.
                    if ( World_AIW2.Instance.GetEntityByID_Squad( spore.PrimaryKeyID ) == null )
                    {
                        if ( debug )
                            ArcenDebugging.SingleLineQuickDebug( "Removing invalid spore." );
                        pair.Value.RemoveAt( j );
                        j--;
                        continue;
                    }

                    MacrophagePerSporeData sData = null;
                    if ( Wilds != null )
                        for ( int x = 0; x < Wilds.Count && sData == null; x++ )
                            if ( Wilds[x].Spores.GetHasKey( spore ) )
                                sData = Wilds[x].Spores[spore];

                    if ( sData == null && Tamed != null && Tamed.Spores.GetHasKey( spore ) )
                    {
                        sData = Tamed.Spores[spore];
                        tamedSporeOnPlanet = true;
                    }
                    if ( sData == null )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "BUG: per spore data is null in check", Verbosity.DoNotShow );
                        continue;
                    }
                    GameEntity_Squad workingTelium = World_AIW2.Instance.GetEntityByID_Squad( sData.TeliumID );
                    if ( workingTelium != null )
                    {
                        if ( !teliaSoFar.Contains( workingTelium ) )
                        {
                            teliaSoFar.Add( workingTelium );
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( pair.Key.Name + " spore " + j + " teliumId " + sData.TeliumID + " is new, add its telium to the list", Verbosity.DoNotShow );
                        }
                        else if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( pair.Key.Name + " spore " + j + " teliumId " + sData.TeliumID + " is NOT new, don't its telium to the list", Verbosity.DoNotShow );
                    }
                    else if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( sData.TeliumID + " telium is not found", Verbosity.DoNotShow );
                }

                int teliaCount = 0;
                if ( Wilds != null )
                    for ( int x = 0; x < Wilds.Count; x++ )
                        if ( Wilds[x].TeliaPlanets.GetHasKey( pair.Key ) )
                            teliaCount += Wilds[x].TeliaPlanets[pair.Key];

                if ( Tamed != null && Tamed.TeliaPlanets.GetHasKey( pair.Key ) )
                    teliaCount += Tamed.TeliaPlanets[pair.Key];

                if ( teliaSoFar.Count >= NumDifferentTeliaRequired )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "We have spores from " + teliaSoFar.Count + " different telia on " + pair.Key.Name + ", so spawn a new Telia", Verbosity.DoNotShow );
                    bool spawnt = false;
                    if ( tamedSporeOnPlanet && teliaCount < TeliaPerPlanetHigh ) // Always treat the Tamed as intensity 10.
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Our new Telia is being built for the Tamed subfaction on " + pair.Key.Name, Verbosity.DoNotShow );
                        Tamed.SpawnTeliumOnPlanet( Context, pair.Key, null, true ); // At least one tamed spore, spawn a tamed telium.
                        spawnt = true;
                    }
                    else
                    {
                        // No Tamed, select a random wild Telia that has a Spore here and is below its planetary Telium cap here, and spawn a Telium for its faction.
                        while ( teliaSoFar.Count > 0 )
                        {
                            int workingIndex = Context.RandomToUse.Next( teliaSoFar.Count );
                            GameEntity_Squad workingTelium = teliaSoFar[workingIndex];
                            teliaSoFar.RemoveAt( workingIndex );
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Our new Telia is being built for a Wild subfaction. The Telia " + workingTelium.PrimaryKeyID + " was chosen as priority.", Verbosity.DoNotShow );

                            SpecialFaction_MacrophageInfestation infestation = ((SpecialFaction_MacrophageInfestation)workingTelium.PlanetFaction.Faction.Implementation);

                            if ( teliaCount < infestation.TeliaPerPlanet )
                            {
                                infestation.SpawnTeliumOnPlanet( Context, pair.Key, workingTelium.PlanetFaction.Faction );
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "We have successfully spawnt the new Telium on " + pair.Key.Name, Verbosity.DoNotShow );
                                spawnt = true;
                                break;
                            }
                            else if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "We have failed to spawn the new Telium. Removing the invalid Telium and trying again.", Verbosity.DoNotShow );
                        }
                    }
                    if ( spawnt )
                    {
                        for ( int k = 0; k < pair.Value.Count; k++ )
                            pair.Value[k].Despawn( Context, true, InstancedRendererDeactivationReason.ThereWereTooManyOfMe );
                        // Now that we're finished, clear our SporesPerPlanet list so that other Macrophage factions cannot rerun this function again this second.
                        // We rebuild SporesPerPlanet every second in Stage2, so we can acquire Spores from every Macrophage subfaction before processing.
                        SporesPerPlanet.Clear();
                        return;
                    }
                }
                else if ( debug )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( pair.Key.Name + " has " + pair.Value.Count + " spores with " + teliaSoFar.Count + " diffferent telia represented, and we need " + NumDifferentTeliaRequired, Verbosity.DoNotShow );
                }
            }
        }
        public new void SpawnTeliumOnPlanet( ArcenSimContext Context, Planet planet, Faction spawnFaction, bool forTamed = false )
        {
            if ( forTamed )
                spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( Tamed.GetType() );
            if ( spawnFaction == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: Unable to find valid faction to spawn a telium; was looking for tamed: " + forTamed.ToString(), Verbosity.DoNotShow );
                spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( MacrophageInfestationHistiocytes ) );
            }
            bool debug = false;
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, TeliumTag );
            if ( entityData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: no macrophagetelium defined for spawning", Verbosity.DoNotShow );
                return;
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "FLAGFLAG Spawning a new telium on " + planet.Name, Verbosity.DoNotShow );
            int minRadius = (ExternalConstants.Instance.DistanceScale_GravwellRadius * FInt.FromParts( 0, 150 )).IntValue;
            int maxRadius = (ExternalConstants.Instance.DistanceScale_GravwellRadius * FInt.FromParts( 0, 300 )).IntValue;
            ArcenPoint spawnLocation = planet.GetSafePlacementPoint( Context, entityData, Engine_AIW2.Instance.CombatCenter, minRadius, maxRadius );
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( spawnFaction );
            /*GameEntity entity = */
            GameEntity_Squad.CreateNew( pFaction, entityData, entityData.MarkFor( pFaction ),
                pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context );
            if ( ArcenNetworkAuthority.GetIsHostMode() )
            {
                if ( planet.IntelLevel > PlanetIntelLevel.Unexplored ) //no warning if you don't have vision
                    World_AIW2.Instance.QueueChatMessageOrCommand( spawnFaction.StartFactionColourForLog() + "Macrophage</color> Telium spawned on " + planet.Name + "!", ChatType.LogToCentralChat, "ArkChiefOfStaff_TeliumSpawn", Context );
            }
            //update our spore gathering points
            GatheringPointsUpdated = false;
        }

        public ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> WormholeCommands { get; set; }
        public ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> MovementCommands { get; set; }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );

            if ( World_AIW2.Instance.GameSecond == 2 )
            {
                // Fix a small bug in which factions can spawn owning infested generators.
                // We'll go through and manually remove them.
                World_AIW2.Instance.DoForEntities( "MetalGeneratorInfested", generator =>
                {
                    if ( generator.PlanetFaction.Faction.Implementation is Macrophage )
                        return DelReturn.Continue;

                    GameEntity_Squad.CreateNew( generator.PlanetFaction, GameEntityTypeDataTable.Instance.GetRowByName( "MetalGenerator" ), 1, generator.PlanetFaction.FleetUsedAtPlanet, 0,
                        generator.WorldLocation, Context );

                    generator.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );

                    return DelReturn.RemoveAndContinue;
                } );
            }

            // Convert metal generators if able.
            ConvertMetalGeneratorsIfAble( faction, Context );

            // Unconvert neutral generators.
            RevertNeutralGeneratorsIfAble( faction, Context );

            // Grant converted metal generators to our host if they own its planet. Otherwise, take them back.
            TransferGeneratorsWithOwnerIfAble( faction, Context );

            // Level up generators and Telia based on time alive.
            LevelUpEntitiesAndTheirDronesIfAble( faction, Context );

            if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "MacrophageDarkSpireRivalry" ) == true )
                HandleDarkSpireRivalryStage3Logic( faction, Context );
        }
        public void ConvertMetalGeneratorsIfAble( Faction faction, ArcenSimContext Context )
        {
            faction.DoForEntities( HarvesterTag, harvester =>
            {
                bool converted = false;
                harvester.Planet.DoForEntities( "MetalGenerator", metalGenerator =>
                {
                    if ( metalGenerator.TypeData.GetHasTag( "MetalGeneratorInfested" ) )
                        return DelReturn.Continue; // Skip already infested ones.

                    if ( metalGenerator.GetSecondsSinceCreation() < 120 )
                        return DelReturn.Continue; // Cooldown between conversion.

                    if ( harvester.GetDistanceTo_VeryCheapButExtremelyRough( metalGenerator, true ) > 1000 )
                        return DelReturn.Continue;

                    GameEntity_Squad.CreateNew( harvester.PlanetFaction, GameEntityTypeDataTable.Instance.GetRowByName( "MetalGeneratorInfested" ), 1, harvester.PlanetFaction.FleetUsedAtPlanet, 0,
                        metalGenerator.WorldLocation, Context );

                    metalGenerator.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );

                    converted = true;

                    return DelReturn.RemoveAndContinue;
                } );

                if ( converted )
                    return DelReturn.Break;
                return DelReturn.Continue;
            } );
        }
        public void RevertNeutralGeneratorsIfAble( Faction faction, ArcenSimContext Context )
        {
            World_AIW2.Instance.GetNeutralFaction().DoForEntities( "MetalGeneratorInfested", generator =>
            {
                GameEntity_Squad.CreateNew( generator.PlanetFaction, GameEntityTypeDataTable.Instance.GetRowByName( "MetalGenerator" ), 1, generator.PlanetFaction.FleetUsedAtPlanet, 0,
                        generator.WorldLocation, Context );

                generator.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );

                return DelReturn.RemoveAndContinue;
            } );
        }
        public void TransferGeneratorsWithOwnerIfAble( Faction faction, ArcenSimContext Context )
        {
            World_AIW2.Instance.DoForEntities( "MetalGeneratorInfested", infestedGenerator =>
            {
                if ( infestedGenerator.PlanetFaction.Faction != faction && !infestedGenerator.PlanetFaction.Faction.GetIsFriendlyTowards( faction ) )
                    return DelReturn.Continue;

                bool converted = false;
                switch ( infestedGenerator.PlanetFaction.Faction.Type )
                {
                    case FactionType.Player:
                    case FactionType.AI:
                        if ( infestedGenerator.PlanetFaction.Faction != infestedGenerator.Planet.GetControllingFaction() )
                        {
                            GameEntity_Squad.CreateNew( infestedGenerator.Planet.GetPlanetFactionForFaction( faction ), GameEntityTypeDataTable.Instance.GetRowByName( "MetalGeneratorInfested" ),
                                infestedGenerator.Planet.GetControllingFaction().GetGlobalMarkLevelForShipLine( infestedGenerator.TypeData ),
                                infestedGenerator.Planet.GetPlanetFactionForFaction( faction ).FleetUsedAtPlanet, 0, infestedGenerator.WorldLocation, Context );

                            infestedGenerator.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );

                            converted = true;
                        }
                        break;
                    default:
                        if ( infestedGenerator.Planet.GetIsControlledByFactionType( FactionType.Player ) && infestedGenerator.PlanetFaction.Faction != infestedGenerator.Planet.GetControllingFaction() )
                        {
                            GameEntity_Squad.CreateNew( infestedGenerator.Planet.GetPlanetFactionForFaction( infestedGenerator.Planet.GetControllingFaction() ), GameEntityTypeDataTable.Instance.GetRowByName( "MetalGeneratorInfested" ),
                                1, infestedGenerator.Planet.GetPlanetFactionForFaction( infestedGenerator.Planet.GetControllingFaction() ).FleetUsedAtPlanet, 0, infestedGenerator.WorldLocation, Context );

                            infestedGenerator.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );

                            converted = true;
                        }
                        break;
                }
                if ( converted )
                    return DelReturn.Break;
                return DelReturn.Continue;
            } );
        }
        public void LevelUpEntitiesAndTheirDronesIfAble( Faction faction, ArcenSimContext Context )
        {
            faction.DoForEntities( TeliumTag, telium =>
            {
                if ( telium.CurrentMarkLevel >= 7 )
                    return DelReturn.Continue;

                byte effectiveMark = (byte)(Math.Min( 7, 1 + telium.GetSecondsSinceCreation() / 1800 ));

                telium.SetCurrentMarkLevelIfHigherThanCurrent( effectiveMark, Context );

                telium.FleetMembership.Fleet.DoForMemberGroups( mem => { mem.EffectiveMark = effectiveMark; return DelReturn.Continue; } );

                return DelReturn.Continue;
            } );
            faction.DoForEntities( "MetalGeneratorInfested", generator =>
            {
                if ( generator.CurrentMarkLevel >= 7 )
                    return DelReturn.Continue;

                byte effectiveMark = (byte)(Math.Min( 7, 1 + generator.GetSecondsSinceCreation() / 900 ));

                generator.SetCurrentMarkLevelIfHigherThanCurrent( effectiveMark, Context );

                generator.FleetMembership.Fleet.DoForMemberGroups( mem => { mem.EffectiveMark = effectiveMark; return DelReturn.Continue; } );

                return DelReturn.Continue;
            } );
            faction.DoForEntities( HarvesterTag, harvester =>
            {
                harvester.FleetMembership.Fleet.DoForMemberGroups( mem => { mem.EffectiveMark = harvester.CurrentMarkLevel; return DelReturn.Continue; } );

                return DelReturn.Continue;
            } );
        }
        public void HandleDarkSpireRivalryStage3Logic( Faction faction, ArcenSimContext Context )
        {
            FInt perSecondGeneratorDamage = FInt.FromParts( 0, 001 );
            FInt perSecondTeliumDecay = FInt.FromParts( 0, 003 );
            List<Planet> vgPlanets = new List<Planet>();
            World_AIW2.Instance.DoForEntities( "VengeanceGenerator", generator =>
            {
                if ( !vgPlanets.Contains( generator.Planet ) )
                    vgPlanets.Add( generator.Planet );

                generator.Planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( "MacrophageHistiocyte", histiocyte =>
                {
                    int distance = histiocyte.GetDistanceTo_VeryCheapButExtremelyRough( generator, true );
                    if ( distance < 3000 )
                    {
                        generator.TakeHullRepair( -(generator.GetMaxHullPoints() * perSecondGeneratorDamage).GetNearestIntPreferringHigher() );
                        if ( Context.RandomToUse.Next( generator.GetCurrentHullPoints(), generator.GetMaxHullPoints() ) < generator.GetMaxHullPoints() / 10 )
                            SpecialFaction_DarkSpire.PerformVengeanceStrike();
                    }

                    return DelReturn.Continue;
                } );

                if ( generator.GetCurrentHullPoints() <= 1000 )
                {
                    generator.Despawn( Context, true, InstancedRendererDeactivationReason.IFinishedMyJob );
                    SpecialFaction_DarkSpire.PerformVengeanceStrike();
                }

                return DelReturn.Continue;
            } );

            for ( int x = 0; x < vgPlanets.Count; x++ )
                vgPlanets[x].DoForLinkedNeighborsAndSelf( false, planet =>
                {
                    planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( TeliumTag, telium =>
                    {
                        telium.TakeHullRepair( -(telium.GetMaxHullPoints() * perSecondTeliumDecay).GetNearestIntPreferringHigher() );

                        return DelReturn.Continue;
                    } );

                    return DelReturn.Continue;
                } );
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            base.DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( faction, Context );

            faction.DoForEntities( "MacrophageHistiocyte", entity =>
            {
                GameEntity_Squad host = entity.FleetMembership.Fleet.Centerpiece;
                if ( host == null )
                    return DelReturn.Continue;

                if ( entity.Planet != host.Planet )
                    entity.QueueWormholeCommand( host.Planet );
                else if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "MacrophageDarkSpireRivalry" ) == true )
                {
                    GameEntity_Squad generator = entity.Planet.GetFirstMatching( FactionType.SpecialFaction, "VengeanceGenerator", true, true );
                    if ( generator == null )
                        return DelReturn.Continue;

                    if ( entity.GetDistanceTo_VeryCheapButExtremelyRough( generator, true ) > 1000 )
                        entity.QueueMovementCommand( generator.WorldLocation );
                }

                return DelReturn.Continue;
            } );

            faction.ExecuteWormholeCommands( Context );
            faction.ExecuteMovementCommands( Context );
        }

        public override void DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips( ref int debugStage, GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, Faction factionThatKilledEntity, Faction entityOwningFaction, int numExtraStacksKilled, ArcenSimContext Context )
        {
            base.DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips( ref debugStage, entity, Damage, FiringSystemOrNull, factionThatKilledEntity, entityOwningFaction, numExtraStacksKilled, Context );

            if ( FiringSystemOrNull == null )
                return;

            GameEntity_Squad EntityThatKilledTarget = FiringSystemOrNull.ParentEntity;
            if ( EntityThatKilledTarget == null )
                return;

            if ( !EntityThatKilledTarget.TypeData.GetHasTag( "MacrophageHistiocyte" ) )
                return;

            debugStage = 16100;
            GameEntity_Squad host = EntityThatKilledTarget?.FleetMembership?.Fleet?.Centerpiece;
            if ( host == null )
                return;

            if ( host.TypeData.GetHasTag( HarvesterTag ) )
            {
                MacrophagePerHarvesterData pData = host.GetMacrophagePerHarvesterDataExt( ExternalDataRetrieval.CreateIfNotFound );
                if ( pData != null )
                {
                    debugStage = 16110;
                    int metalGained = entity.GetStrengthPerSquad() + entity.GetStrengthPerSquad() * numExtraStacksKilled;
                    metalGained *= HarvesterMetalFromKillMultiplier;
                    pData.CurrentMetal += metalGained;
                    pData.TotalMetalEverCollected += metalGained;
                    host.SetMacrophagePerHarvesterDataExt( pData );
                }
            }
            else if ( host.TypeData.GetHasTag( TeliumTag ) )
            {
                debugStage = 16200;
                MacrophagePerTeliumData tData = host.GetMacrophagePerTeliumDataExt( ExternalDataRetrieval.CreateIfNotFound );
                if ( tData != null )
                {
                    debugStage = 16210;
                    int metalGained = entity.GetStrengthPerSquad() + entity.GetStrengthPerSquad() * numExtraStacksKilled;
                    metalGained *= HarvesterMetalFromKillMultiplier;
                    tData.CurrentMetal += metalGained;
                    tData.TotalMetalEverCollected += metalGained;
                    host.SetMacrophagePerTeliumDataExt( tData );
                }
            }
        }
    }
}
