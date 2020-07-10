using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using System.Collections.Generic;

namespace PreceptsOfThePrecursors
{
    public enum Unit
    {
        YounglingWorm,
        YounglingPuffin,
        YounglingSnake,
        YounglingEel,
        YounglingCheeta,
        YounglingBadger,
        YounglingLion,
        YounglingApe,
        YounglingBee,
        YounglingShrike,
        YounglingWolf,
        YounglingAnt,
        YounglingTurtle,
        ClanlingMammoth,
        ClanlingBear,
        ClanlingWyvern
    }

    // Base for all Enclave subfactions.
    public abstract class BaseRoamingEnclave : BaseSpecialFaction, IBulkPathfinding
    {
        protected override string TracingName => "RoamingEnclave";
        protected override bool EverNeedsToRunLongRangePlanning => true;

        public ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> WormholeCommands { get; set; }
        public ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> MovementCommands { get; set; }

        // Unit names and tags.
        public static string ENCLAVE_TAG = "RoamingEnclave";
        public static string HIVE_TAG = "EnclaveHive";
        public static string YOUNGLING_TAG = "EnclaveUnit";
        public static string HUMAN_HIVE_NAME = "HiveYounglingHuman";

        public List<GameEntity_Squad> Enclaves = new List<GameEntity_Squad>();
        public List<GameEntity_Squad> Hives = new List<GameEntity_Squad>();

        public List<Planet> EnclavePlanets = new List<Planet>();
        public List<Planet> HivePlanets = new List<Planet>();

        public static bool IsEnabled;
        public static int Intensity;

        public enum Commands
        {
            MarkUpUnits,
            PopulateEnclavesList,
            PopulateHivesList,
            PopulateEnclavePlanetList,
            PopulateHivePlanetsList,
            SetOrClearEnclaveOwnership,
            AddHivesToBuildList,
            ClaimHivesFromHumanAllies
        }
        public BaseRoamingEnclave()
        {
            IsEnabled = false;
            Intensity = 0;
        }

        public override bool GetShouldAttackNormallyExcludedTarget( Faction faction, GameEntity_Squad Target )
        {
            return !(this is RoamingEnclavePlayerTeam);
        }
        public override void UpdatePowerLevel( Faction faction )
        {
            faction.OverallPowerLevel = FInt.FromParts( 0, 150 ) * Hives.Count;
        }
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( !IsEnabled )
                return;

            HandleUnitSpawningForEnclaves( Context );

            HandleUnitSpawningForHives( Context );

            if ( Enclaves.Count == 0 && Hives.Count == 0 && (World_AIW2.Instance.GameSecond % 3600 == 0 || World_AIW2.Instance.GameSecond == 11) )
                BulkSpawn( faction, World_AIW2.Instance.CurrentGalaxy, Context );

            if ( World_AIW2.Instance.GameSecond % 900 == 0 )
                HandleEnclaveSpawning( faction, Context );

            if ( World_AIW2.Instance.GameSecond % 900 == 0 )
                HandleHiveExpansion( faction, Context );
        }

        private void HandleUnitSpawningForEnclaves( ArcenSimContext Context )
        {
            if ( Enclaves.Count > 0 && CanSpawnUnits( null, out GameEntityTypeData unitData ) )
                SpawnUnitsForEnclave( Enclaves, unitData, Context );
        }

        private void HandleUnitSpawningForHives( ArcenSimContext Context )
        {
            for ( int y = 0; y < Hives.Count; y++ )
                if ( CanSpawnUnits( Hives[y], out GameEntityTypeData unitData ) )
                    SpawnUnitsForHive( Hives[y], unitData, Context );
        }

        private bool CanSpawnUnits( GameEntity_Squad hiveOrNull, out GameEntityTypeData unitData )
        {
            unitData = GameEntityTypeDataTable.Instance.GetRowByName( hiveOrNull != null ? hiveOrNull.TypeData.InternalName.Substring( 4 ) : Unit.YounglingWorm.ToString() );
            if ( unitData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Failed to find unit data for " + hiveOrNull.TypeData.DisplayName, Verbosity.ShowAsError );
                return false;
            }
            return World_AIW2.Instance.GameSecond % (unitData.MetalCost / (20 + (Intensity * 3))) == 0;
        }

        private void SpawnUnitsForHive( GameEntity_Squad hive, GameEntityTypeData unitData, ArcenSimContext Context )
        {
            GameEntity_Squad unit = GameEntity_Squad.CreateNew( hive.PlanetFaction, unitData, hive.CurrentMarkLevel, hive.PlanetFaction.FleetUsedAtPlanet, 0, hive.WorldLocation, Context );
            unit.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, hive.PlanetFaction.Faction.FactionIndex );
            unit.MinorFactionStackingID = -1;
            if ( this is RoamingEnclavePlayerTeam )
                (this as RoamingEnclavePlayerTeam).SpawnUnitsForPlayerCenterpieces( hive, unitData, Context );
        }

        private void SpawnUnitsForEnclave( List<GameEntity_Squad> enclaves, GameEntityTypeData unitData, ArcenSimContext Context )
        {
            for ( int x = 0; x < enclaves.Count; x++ )
            {
                GameEntity_Squad enclave = enclaves[x];
                GameEntity_Squad unit = GameEntity_Squad.CreateNew( enclave.PlanetFaction, unitData, enclave.CurrentMarkLevel, enclave.PlanetFaction.FleetUsedAtPlanet, 0, enclave.WorldLocation, Context );
                unit.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, enclave.PlanetFaction.Faction.FactionIndex );
                unit.MinorFactionStackingID = enclave.PrimaryKeyID;
            }
        }

        protected abstract void BulkSpawn( Faction faction, Galaxy galaxy, ArcenSimContext Context );

        protected abstract void HandleEnclaveSpawning( Faction faction, ArcenSimContext Context );

        protected abstract void HandleHiveExpansion( Faction faction, ArcenSimContext Context );

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( !IsEnabled )
                return;

            if ( this.Teams == null )
                this.Teams = new ArcenLessLinkedList<Fireteam>();
            if ( this.TeamsAimedAtPlanet == null )
                this.TeamsAimedAtPlanet = new ArcenSparseLookup<Planet, FireteamRegiment>();

            Fireteam.DoFor( this.Teams, delegate ( Fireteam team )
            {
                team.Reset();
                return DelReturn.Continue;
            } );

            SetupEnclaves( faction, Context );
            SetupYounglings( faction, Context );

            Fireteam.DoFor( this.Teams, delegate ( Fireteam team )
            {
                if ( team.ships.Count == 0 )
                    team.Disband( Context );
                return DelReturn.Continue;
            } );
            FireteamUtility.CleanUpDisbandedFireteams( Teams );

            SetupHives( faction, Context );

            ArcenCharacterBuffer buffer = this.tracingBuffer_longTerm;
            FireteamUtility.UpdateFireteams( faction, Context, Teams, TeamsAimedAtPlanet, buffer, FInt.One );
            FireteamUtility.UpdateRegiments( faction, Context, Teams, TeamsAimedAtPlanet, buffer, 1, true );

            faction.ExecuteMovementCommands( Context );
            faction.ExecuteWormholeCommands( Context );
        }

        private void SetupEnclaves( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand markUpCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.MarkUpUnits.ToString() ), GameCommandSource.AnythingElse, faction );
            GameCommand enclavePopulateCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.PopulateEnclavesList.ToString() ), GameCommandSource.AnythingElse, faction );
            GameCommand enclavePlanetsPopulateCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.PopulateEnclavePlanetList.ToString() ), GameCommandSource.AnythingElse, faction );

            faction.DoForEntities( ENCLAVE_TAG, enclave =>
            {
                enclavePopulateCommand.RelatedEntityIDs.Add( enclave.PrimaryKeyID );
                if ( !enclavePlanetsPopulateCommand.RelatedIntegers.Contains( enclave.Planet.Index ) )
                {
                    enclavePlanetsPopulateCommand.RelatedIntegers.Add( enclave.Planet.Index );
                    BadgerFactionUtilityMethods.FlushUnitsFromReinforcementPoints( enclave.Planet, faction, Context );
                }
                if ( enclave.CurrentMarkLevel < 7 && enclave.GetSecondsSinceCreation() > enclave.CurrentMarkLevel * 1800 )
                    markUpCommand.RelatedEntityIDs.Add( enclave.PrimaryKeyID );
                if ( enclave.FireteamId < 0 )
                {
                    if ( enclave.Planet.GetHopsTo( GetNearestHivePlanet( faction, enclave.Planet, Context ) ) == 0 )
                    {
                        Fireteam team = new Fireteam();
                        team.MyStrengthMultiplierForStrengthCalculation = FInt.One;
                        team.EnemyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 050 );
                        team.id = FireteamUtility.GetNextFireteamId( this.Teams );
                        team.DefenseMode = team.id % 3 == 0;
                        team.StrengthToBringOnline = 0;
                        team.AddUnit( enclave );
                        this.Teams.AddIfNotAlreadyIn( team );
                    }
                    else
                    {
                        if ( enclave.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength < 100 ||
                            enclave.PlanetFaction.DataByStance[FactionStance.Self].TotalStrength + enclave.PlanetFaction.DataByStance[FactionStance.Friendly].TotalStrength < enclave.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength )
                            enclave.QueueWormholeCommand( GetNearestHivePlanet( faction, enclave.Planet, Context, true ) );
                    }
                }
                else
                {
                    Fireteam team = this.GetFireteamById( faction, enclave.FireteamId );
                    if ( team != null )
                    {
                        if ( team.status == FireteamStatus.Assembling )
                        {
                            team.Disband( Context );
                            enclave.FireteamId = -1;
                        }
                        else
                            team.AddUnit( enclave );
                    }
                    else
                        enclave.FireteamId = -1; //something happened to the fireteam, so lets find a new one next LRP stage
                }

                return DelReturn.Continue;
            } );

            if ( markUpCommand.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( markUpCommand );
            Context.QueueCommandForSendingAtEndOfContext( enclavePopulateCommand );
            Context.QueueCommandForSendingAtEndOfContext( enclavePlanetsPopulateCommand );
        }

        private Planet GetNearestHivePlanet( Faction faction, Planet planet, ArcenLongTermIntermittentPlanningContext Context, bool careAboutDanger = false )
        {
            Planet bestPlanet = null;
            int lowestDanger = 99999;
            for ( int x = 0; x < HivePlanetsForBackgroundThreadOnly.Count; x++ )
            {
                Planet hivePlanet = HivePlanetsForBackgroundThreadOnly[x];
                int hops = planet.GetHopsTo( hivePlanet );
                int danger = Fireteam.GetDangerOfPath( faction, Context, planet, hivePlanet, false, out short _ );

                if ( bestPlanet == null )
                {
                    bestPlanet = hivePlanet;
                    lowestDanger = danger;
                }
                else if ( careAboutDanger )
                {
                    if ( danger < lowestDanger )
                    {
                        bestPlanet = hivePlanet;
                        lowestDanger = danger;
                    }
                }
                else if ( hops < planet.GetHopsTo( bestPlanet ) )
                    bestPlanet = hivePlanet;
            }
            return bestPlanet != null ? bestPlanet : planet;
        }

        private void SetupHives( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand markUpCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.MarkUpUnits.ToString() ), GameCommandSource.AnythingElse, faction );
            GameCommand hivePopulateCommands = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.PopulateHivesList.ToString() ), GameCommandSource.AnythingElse, faction );
            GameCommand hivePlanetsPopulateCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.PopulateHivePlanetsList.ToString() ), GameCommandSource.AnythingElse, faction );

            HivePlanetsForBackgroundThreadOnly = new List<Planet>();

            faction.DoForEntities( HIVE_TAG, hive =>
            {
                hivePopulateCommands.RelatedEntityIDs.Add( hive.PrimaryKeyID );
                if ( !hivePlanetsPopulateCommand.RelatedIntegers.Contains( hive.Planet.Index ) )
                    hivePlanetsPopulateCommand.RelatedIntegers.Add( hive.Planet.Index );
                if ( hive.CurrentMarkLevel < 7 && hive.GetSecondsSinceCreation() > hive.CurrentMarkLevel * 1800 )
                    markUpCommand.RelatedEntityIDs.Add( hive.PrimaryKeyID );
                if ( !HivePlanetsForBackgroundThreadOnly.Contains( hive.Planet ) )
                    HivePlanetsForBackgroundThreadOnly.Add( hive.Planet );

                return DelReturn.Continue;
            } );

            if ( markUpCommand.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( markUpCommand );
            Context.QueueCommandForSendingAtEndOfContext( hivePopulateCommands );
            Context.QueueCommandForSendingAtEndOfContext( hivePlanetsPopulateCommand );
        }

        public void SetupYounglings( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand markUpCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.MarkUpUnits.ToString() ), GameCommandSource.AnythingElse, faction );
            GameCommand ownershipCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.SetOrClearEnclaveOwnership.ToString() ), GameCommandSource.AnythingElse, faction );

            List<GameEntity_Squad> enclaves = new List<GameEntity_Squad>();
            faction.DoForEntities( ENCLAVE_TAG, workingEnclave =>
            {
                enclaves.Add( workingEnclave );

                return DelReturn.Continue;
            } );

            faction.DoForEntities( YOUNGLING_TAG, youngling =>
            {
                if ( youngling.CurrentMarkLevel < 7 && youngling.GetSecondsSinceCreation() > youngling.CurrentMarkLevel * Context.RandomToUse.Next( 600, 1200 ) )
                    markUpCommand.RelatedEntityIDs.Add( youngling.PrimaryKeyID );

                GameEntity_Squad enclave = World_AIW2.Instance.GetEntityByID_Squad( youngling.MinorFactionStackingID );
                if ( enclave == null )
                {
                    if ( enclaves.Count > 0 )
                        enclave = enclaves[Context.RandomToUse.Next( enclaves.Count )];
                    else
                        return DelReturn.Continue;
                }
                ownershipCommand.RelatedIntegers.Add( youngling.PrimaryKeyID );
                ownershipCommand.RelatedIntegers2.Add( enclave.PrimaryKeyID );

                Fireteam team = this.GetFireteamById( faction, enclave.FireteamId );
                bool shouldAdd = false;
                if ( team != null )
                    switch ( team.status )
                    {
                        case FireteamStatus.ReadyToAttack:
                        case FireteamStatus.Attacking:
                            shouldAdd = true;
                            break;
                        default:
                            if ( youngling.Planet == enclave.Planet )
                                shouldAdd = true;
                            break;
                    }
                if ( shouldAdd )
                    team.AddUnit( youngling );
                else
                {
                    if ( youngling.Planet != enclave.Planet && youngling.LongRangePlanningData.FinalDestinationPlanetIndex == -1 )
                        youngling.QueueWormholeCommand( enclave.Planet, Context );
                    youngling.FireteamId = -1;
                }

                return DelReturn.Continue;
            } );

            if ( markUpCommand.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( markUpCommand );
            Context.QueueCommandForSendingAtEndOfContext( ownershipCommand );
        }

        #region Fireteams
        public ArcenLessLinkedList<Fireteam> Teams = null;
        public ArcenSparseLookup<Planet, FireteamRegiment> TeamsAimedAtPlanet = new ArcenSparseLookup<Planet, FireteamRegiment>();
        public List<Planet> HivePlanetsForBackgroundThreadOnly = new List<Planet>();

        int alliedAssaultFriendlyThreshold = 5000;
        int alliedAssaultHostileThresholdMult = 2;
        int maxStrengthToBreakThrough = 10000;
        int hivesInDangerThreshold = 2500;
        int hivesThreatenedThreshold = 5000;

        public override Fireteam GetFireteamById( Faction faction, int id )
        {
            return FireteamUtility.GetFireteamById( this.Teams, id );
        }
        public override FireteamBase GetFireteamBaseById( Faction faction, int id )
        {
            return FireteamUtility.GetFireteamById( this.Teams, id );
        }

        public override void GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass( Faction faction, bool DefenseMode, Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, ref List<FireteamTarget> PreferredTargets, ref List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            List<Planet> planetsInRange = new List<Planet>();
            PreferredTargets.Clear();
            FallbackTargets.Clear();

            List<Planet> hivesInDanger = new List<Planet>();
            List<Planet> alliedAssaults = new List<Planet>();
            List<Planet> hivesThreatened = new List<Planet>();
            List<GameEntity_Squad> importantTargets = new List<GameEntity_Squad>();

            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                int hops = planet.GetHopsTo( GetNearestHivePlanet( faction, planet, Context ) );

                int hostileStrength = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Hostile].TotalStrength;
                int friendlyStrength = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Friendly].TotalStrength;

                if ( this is RoamingEnclavePlayerTeam && faction.Ex_MinorFactionCommon_GetPrimitives().DebugMode && friendlyStrength > alliedAssaultFriendlyThreshold
                  && hostileStrength > friendlyStrength * alliedAssaultHostileThresholdMult )
                {
                    alliedAssaults.Add( planet );
                    if ( this is RoamingEnclavePlayerTeam && faction.Ex_MinorFactionCommon_GetPrimitives().DebugMode )
                        ArcenDebugging.SingleLineQuickDebug( planet.Name + " is considered an Allied Assault." );
                }
                else if ( hops == 0 )
                {
                    if ( hostileStrength > hivesInDangerThreshold )
                    {
                        hivesInDanger.Add( planet );
                        if ( this is RoamingEnclavePlayerTeam && faction.Ex_MinorFactionCommon_GetPrimitives().DebugMode )
                            ArcenDebugging.SingleLineQuickDebug( planet.Name + " is considered a hive in danger." );
                    }
                }
                else if ( hops == 1 )
                {
                    if ( hostileStrength > hivesThreatenedThreshold )
                    {
                        hivesThreatened.Add( planet );
                        if ( this is RoamingEnclavePlayerTeam && faction.Ex_MinorFactionCommon_GetPrimitives().DebugMode )
                            ArcenDebugging.SingleLineQuickDebug( planet.Name + " is considered a hive threatened." );
                    }
                }
                else if ( hops < 5 )
                    planetsInRange.Add( planet );

                return DelReturn.Continue;
            } );

            World_AIW2.Instance.DoForFactions( otherFaction =>
            {
                if ( !faction.GetIsHostileTowards( otherFaction ) )
                    return DelReturn.Continue;

                otherFaction.DoForEntities( EntityRollupType.GrantsMinorFactionPlanetControl, controller =>
                {
                    if ( planetsInRange.Contains( controller.Planet ) )
                        if ( Fireteam.GetDangerOfPath( faction, Context, GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( faction, controller.Planet, 0, CurrentPlanetForFireteam, Context ), controller.Planet, false, out short _ ) < maxStrengthToBreakThrough )
                        {
                            importantTargets.Add( controller );
                        }
                    return DelReturn.Continue;
                } );

                otherFaction.DoForEntities( SpecialEntityType.GuardPost, guardPost =>
                {
                    if ( planetsInRange.Contains( guardPost.Planet ) )
                        if ( Fireteam.GetDangerOfPath( faction, Context, GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( faction, guardPost.Planet, 0, CurrentPlanetForFireteam, Context ), guardPost.Planet, false, out short _ ) < maxStrengthToBreakThrough )
                        {
                            importantTargets.Add( guardPost );
                        }
                    return DelReturn.Continue;
                } );

                otherFaction.DoForEntities( EntityRollupType.KingUnitsOnly, king =>
                {
                    if ( planetsInRange.Contains( king.Planet ) )
                        if ( Fireteam.GetDangerOfPath( faction, Context, GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( faction, king.Planet, 0, CurrentPlanetForFireteam, Context ), king.Planet, false, out short _ ) < maxStrengthToBreakThrough )
                        {
                            importantTargets.Add( king );
                        }
                    return DelReturn.Continue;
                } );

                if ( !(this is RoamingEnclavePlayerTeam) )
                    otherFaction.DoForEntities( EntityRollupType.CommandStation, command =>
                    {
                        if ( planetsInRange.Contains( command.Planet ) )
                            if ( Fireteam.GetDangerOfPath( faction, Context, GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( faction, command.Planet, 0, CurrentPlanetForFireteam, Context ), command.Planet, false, out short _ ) < maxStrengthToBreakThrough )
                            {
                                importantTargets.Add( command );
                            }
                        return DelReturn.Continue;
                    } );

                return DelReturn.Continue;
            } );

            if ( hivesInDanger.Count > 0 )
                for ( int x = 0; x < hivesInDanger.Count; x++ )
                    PreferredTargets.Add( new FireteamTarget( hivesInDanger[x] ) );
            if ( hivesThreatened.Count > 0 )
                for ( int x = 0; x < hivesThreatened.Count; x++ )
                    PreferredTargets.Add( new FireteamTarget( hivesThreatened[x] ) );

            if ( DefenseMode )
                for ( int x = 0; x < HivePlanets.Count; x++ )
                    FallbackTargets.Add( new FireteamTarget( HivePlanets[x] ) );
            else
            {
                if ( alliedAssaults.Count > 0 )
                    for ( int x = 0; x < alliedAssaults.Count; x++ )
                        PreferredTargets.Add( new FireteamTarget( alliedAssaults[x] ) );

                if ( importantTargets.Count > 0 )
                    for ( int x = 0; x < importantTargets.Count; x++ )
                        FallbackTargets.Add( new FireteamTarget( importantTargets[x] ) );

                if ( this is RoamingEnclavePlayerTeam && faction.Ex_MinorFactionCommon_GetPrimitives().DebugMode )
                {
                    for ( int x = 0; x < PreferredTargets.Count; x++ )
                    {
                        Planet planet = PreferredTargets[x].targetSquad != null ? PreferredTargets[x].targetSquad.Planet : PreferredTargets[x].planet;
                        ArcenDebugging.SingleLineQuickDebug( planet.Name + " is a preferred target" );
                    }
                    for ( int x = 0; x < FallbackTargets.Count; x++ )
                    {
                        Planet planet = FallbackTargets[x].targetSquad != null ? FallbackTargets[x].targetSquad.Planet : FallbackTargets[x].planet;
                        ArcenDebugging.SingleLineQuickDebug( planet.Name + " is a fallback target" );
                    }
                }
            }
        }

        public override Planet GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( Faction faction, Planet TargetPlanet, int TeamStrength, Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context )
        {
            return GetNearestHivePlanet( faction, TargetPlanet, Context, true );
        }

        public override GameEntity_Squad GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( Faction faction, Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context )
        {
            return GetNearestHivePlanet( faction, CurrentPlanetForFireteam, Context, true ).GetPlanetFactionForFaction( faction ).Entities.GetFirstMatching( HIVE_TAG, false, true );
        }
        #endregion
    }
    // Acts as a base for all npc subfactions.
    public abstract class RoamingEnclaveNPC : BaseRoamingEnclave
    {
        protected override void HandleEnclaveSpawning( Faction faction, ArcenSimContext Context )
        {
            if ( HivePlanets.Count == 0 )
                return;

            int toSpawn = 1;
            if ( Intensity >= 3 )
                toSpawn += Intensity / 3;
            if ( Intensity >= 4 )
                toSpawn += Intensity / 4;
            if ( Intensity >= 7 )
                toSpawn++;
            if ( Intensity == 10 )
                toSpawn++;

            for ( int x = 0; x < HivePlanets.Count && toSpawn > 0; x++ )
            {
                if ( !EnclavePlanets.Contains( HivePlanets[x] ) )
                {
                    HivePlanets[x].Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
                    toSpawn--;
                }
            }

            for ( int x = 0; x < toSpawn; x++ )
            {
                Planet spawnPlanet = HivePlanets[Context.RandomToUse.Next( HivePlanets.Count )];

                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
            }
        }

        protected override void HandleHiveExpansion( Faction faction, ArcenSimContext Context )
        {
            if ( HivePlanets.Count == 0 )
                return;

            int duplicationChance = 100;
            if ( Intensity >= 5 )
                duplicationChance += 100;
            if ( Intensity >= 7 )
                duplicationChance += 100;
            if ( Intensity == 10 )
                duplicationChance += 100;

            for ( int x = 0; x < Hives.Count && duplicationChance > 10; x++ )
                if ( Context.RandomToUse.Next( duplicationChance -= Context.RandomToUse.Next( 50 ) ) > 10 )
                    Hives[x].Planet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );

            int toSpawn = 1;
            if ( Intensity >= 4 )
                toSpawn += Intensity / 4;
            if ( Intensity == 10 )
                toSpawn++;

            for ( int x = 0; x < HivePlanets.Count; x++ )
            {
                HivePlanets[x].DoForLinkedNeighbors( false, planet =>
                {
                    if ( !HivePlanets.Contains( planet ) )
                    {
                        planet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );
                        toSpawn--;
                    }

                    if ( toSpawn == 0 )
                        return DelReturn.Break;

                    return DelReturn.Continue;
                } );
            }

            for ( int x = 0; x < toSpawn; x++ )
            {
                Planet spawnPlanet = HivePlanets[Context.RandomToUse.Next( HivePlanets.Count )];

                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );
            }
        }
    }

    // Acts as the hostile to all faction, and as the overall enabler. All subfactions will be disabled if this one is not enabled from the lobby.
    public class RoamingEnclaveEnabler : RoamingEnclaveNPC
    {
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            IsEnabled = true;
            Intensity = faction.Ex_MinorFactionCommon_GetPrimitives().Intensity;

            base.DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim( faction, Context );
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            enemyThisFactionToAll( faction );

            if ( World_AIW2.Instance.GameSecond == 11 || World_AIW2.Instance.GameSecond % 900 == 0 )
                World_AIW2.Instance.QueueChatMessageOrCommand( "A new influx of Neinzul have arrived in our galaxy.", ChatType.LogToCentralChat, Context );

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }

        protected override void BulkSpawn( Faction faction, Galaxy galaxy, ArcenSimContext Context )
        {
            // Spawn in a bunch of hives and enclaves based on intensity.
            int toSpawn = 1;
            if ( Intensity > 1 )
                toSpawn += Intensity;
            if ( Intensity > 5 )
                toSpawn += Intensity - 5;
            if ( Intensity > 7 )
                toSpawn += Intensity - 7;
            if ( Intensity == 10 )
                toSpawn += 5;

            List<Planet> potentialPlanets = new List<Planet>();
            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                if ( planet.OriginalHopsToAnyHomeworld > 3 )
                    potentialPlanets.Add( planet );

                return DelReturn.Continue;
            } );

            if ( potentialPlanets.Count == 0 )
                return;

            Planet spawnPlanet = potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];

            if ( World_AIW2.Instance.GameSecond > 11 )
                World_AIW2.Instance.QueueChatMessageOrCommand( $"A group of hostile {faction.StartFactionColourForLog()}Neinzul Enclaves</color> have entered the galaxy on {spawnPlanet.Name}.", ChatType.LogToCentralChat, Context );

            for ( int x = 0; x < toSpawn; x++ )
            {
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
            }
        }
    }

    public class RoamingEnclaveRedTeam : RoamingEnclaveNPC
    {
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            faction.Ex_MinorFactionCommon_GetPrimitives().Allegiance = "Minor Faction Team Red";
            allyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Red" );

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }
        protected override void BulkSpawn( Faction faction, Galaxy galaxy, ArcenSimContext Context )
        {
            // Spawn in a bunch of hives and enclaves based on intensity.
            int toSpawn = 1;
            if ( Intensity >= 3 )
                toSpawn += Intensity / 3;
            if ( Intensity >= 4 )
                toSpawn += Intensity / 4;
            if ( Intensity >= 7 )
                toSpawn++;
            if ( Intensity == 10 )
                toSpawn++;

            List<Planet> potentialPlanets = new List<Planet>();
            World_AIW2.Instance.DoForFactions( otherFaction =>
            {
                if ( otherFaction.GetIsFriendlyTowards( faction ) )
                {
                    otherFaction.DoForEntities( ( GameEntity_Squad entity ) =>
                    {
                        if ( potentialPlanets.Count < 5 && !potentialPlanets.Contains( entity.Planet ) )
                            potentialPlanets.Add( entity.Planet );

                        return DelReturn.Continue;
                    } );
                }

                return DelReturn.Continue;
            } );

            if ( potentialPlanets.Count == 0 )
                return;

            Planet spawnPlanet = potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];

            for ( int x = 0; x < toSpawn; x++ )
            {
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
            }
        }
    }

    public class RoamingEnclaveBlueTeam : RoamingEnclaveNPC
    {
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            faction.Ex_MinorFactionCommon_GetPrimitives().Allegiance = "Minor Faction Team Blue";
            allyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Blue" );

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }
        protected override void BulkSpawn( Faction faction, Galaxy galaxy, ArcenSimContext Context )
        {
            // Spawn in a bunch of hives and enclaves based on intensity.
            int toSpawn = 1;
            if ( Intensity >= 3 )
                toSpawn += Intensity / 3;
            if ( Intensity >= 4 )
                toSpawn += Intensity / 4;
            if ( Intensity >= 7 )
                toSpawn++;
            if ( Intensity == 10 )
                toSpawn++;

            List<Planet> potentialPlanets = new List<Planet>();
            World_AIW2.Instance.DoForFactions( otherFaction =>
            {
                if ( otherFaction.GetIsFriendlyTowards( faction ) )
                {
                    otherFaction.DoForEntities( ( GameEntity_Squad entity ) =>
                    {
                        if ( potentialPlanets.Count < 5 && !potentialPlanets.Contains( entity.Planet ) )
                            potentialPlanets.Add( entity.Planet );

                        return DelReturn.Continue;
                    } );
                }

                return DelReturn.Continue;
            } );

            if ( potentialPlanets.Count == 0 )
                return;

            Planet spawnPlanet = potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];

            for ( int x = 0; x < toSpawn; x++ )
            {
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
            }
        }
    }

    public class RoamingEnclaveGreenTeam : RoamingEnclaveNPC
    {
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            faction.Ex_MinorFactionCommon_GetPrimitives().Allegiance = "Minor Faction Team Green";
            allyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Green" );

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }
        protected override void BulkSpawn( Faction faction, Galaxy galaxy, ArcenSimContext Context )
        {
            // Spawn in a bunch of hives and enclaves based on intensity.
            int toSpawn = 1;
            if ( Intensity >= 3 )
                toSpawn += Intensity / 3;
            if ( Intensity >= 4 )
                toSpawn += Intensity / 4;
            if ( Intensity >= 7 )
                toSpawn++;
            if ( Intensity == 10 )
                toSpawn++;

            List<Planet> potentialPlanets = new List<Planet>();
            World_AIW2.Instance.DoForFactions( otherFaction =>
            {
                if ( otherFaction.GetIsFriendlyTowards( faction ) )
                {
                    otherFaction.DoForEntities( ( GameEntity_Squad entity ) =>
                    {
                        if ( potentialPlanets.Count < 5 && !potentialPlanets.Contains( entity.Planet ) )
                            potentialPlanets.Add( entity.Planet );

                        return DelReturn.Continue;
                    } );
                }

                return DelReturn.Continue;
            } );

            if ( potentialPlanets.Count == 0 )
                return;

            Planet spawnPlanet = potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];

            for ( int x = 0; x < toSpawn; x++ )
            {
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
            }
        }
    }

    public class RoamingEnclaveDarkTeam : RoamingEnclaveNPC
    {
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            faction.Ex_MinorFactionCommon_GetPrimitives().Allegiance = "Dark Alliance";
            allyThisFactionToMinorFactionTeam( faction, "Dark Alliance" );

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }
        protected override void BulkSpawn( Faction faction, Galaxy galaxy, ArcenSimContext Context )
        {
            // Spawn in a bunch of hives and enclaves based on intensity.
            int toSpawn = 1;
            if ( Intensity >= 3 )
                toSpawn += Intensity / 3;
            if ( Intensity >= 4 )
                toSpawn += Intensity / 4;
            if ( Intensity >= 7 )
                toSpawn++;
            if ( Intensity == 10 )
                toSpawn++;

            List<Planet> potentialPlanets = new List<Planet>();
            Faction darkFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( SpecialFaction_DarkZenith ) );
            if ( darkFaction != null )
                darkFaction.DoForEntities( ( GameEntity_Squad entity ) =>
                {
                    if ( potentialPlanets.Count < 5 && !potentialPlanets.Contains( entity.Planet ) )
                        potentialPlanets.Add( entity.Planet );

                    return DelReturn.Continue;
                } );
            if ( potentialPlanets.Count == 0 )
                World_AIW2.Instance.DoForFactions( otherFaction =>
                {
                    if ( otherFaction.GetIsFriendlyTowards( faction ) )
                    {
                        otherFaction.DoForEntities( ( GameEntity_Squad entity ) =>
                        {
                            if ( potentialPlanets.Count < 5 && !potentialPlanets.Contains( entity.Planet ) )
                                potentialPlanets.Add( entity.Planet );

                            return DelReturn.Continue;
                        } );
                    }

                    return DelReturn.Continue;
                } );

            if ( potentialPlanets.Count == 0 )
                return;

            Planet spawnPlanet = potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];

            for ( int x = 0; x < toSpawn; x++ )
            {
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
            }
        }
    }

    public class RoamingEnclaveAITeam : RoamingEnclaveNPC
    {
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            allyThisFactionToAI( faction );

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }
        protected override void BulkSpawn( Faction faction, Galaxy galaxy, ArcenSimContext Context )
        {
            // Spawn in a bunch of hives and enclaves based on intensity.
            int toSpawn = 1;
            if ( Intensity >= 3 )
                toSpawn += Intensity / 3;
            if ( Intensity >= 4 )
                toSpawn += Intensity / 4;
            if ( Intensity >= 7 )
                toSpawn++;
            if ( Intensity == 10 )
                toSpawn++;

            // Spawn in a single hive to start with.
            Planet spawnPlanet = BadgerFactionUtilityMethods.findAIKing();

            for ( int x = 0; x < toSpawn; x++ )
            {
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
            }
        }
    }

    public class RoamingEnclavePlayerTeam : BaseRoamingEnclave
    {
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( !IsEnabled )
                return;

            allyThisFactionToHumans( faction );

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }

        public void SpawnUnitsForPlayerCenterpieces( GameEntity_Squad hive, GameEntityTypeData unitData, ArcenSimContext Context )
        {
            hive.Planet.DoForEntities( EntityRollupType.MobileFleetFlagships, centerpiece =>
            {
                if ( centerpiece.PlanetFaction.Faction.Type == FactionType.Player && centerpiece.TypeData.SpecialType != SpecialEntityType.MobileSupportFleetFlagship )
                {
                    Fleet.Membership mem = centerpiece.FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates( unitData, 1 );

                    int count = 0;
                    for ( int x = 0; x < mem.TransportContents.Count; x++ )
                        count += 1 + mem.TransportContents[x].ExtraShipsInStack;
                    mem.DoForEntities( entity =>
                    {
                        if ( count == 50 )
                            entity.Despawn( Context, true, InstancedRendererDeactivationReason.SelfDestructOnTooHighOfCap );
                        else
                            count += 1 + entity.ExtraStackedSquadsInThis;

                        return DelReturn.Continue;
                    } );

                    if ( count >= 50 )
                        return DelReturn.Continue;

                    GameEntity_Squad unit = GameEntity_Squad.CreateNew( centerpiece.PlanetFaction, unitData, centerpiece.PlanetFaction.Faction.GetGlobalMarkLevelForShipLine( unitData ),
                        centerpiece.PlanetFaction.FleetUsedAtPlanet, 0, hive.WorldLocation, Context );

                    mem.AddEntityToFleetMembership( unit );
                    unit.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, centerpiece.PlanetFaction.Faction.FactionIndex );
                }

                return DelReturn.Continue;
            } );
        }

        protected override void BulkSpawn( Faction faction, Galaxy galaxy, ArcenSimContext Context )
        {
            // Spawn in a bunch of enclaves based on intensity and a singular hive.
            int toSpawn = 1;
            if ( Intensity >= 3 )
                toSpawn += Intensity / 3;
            if ( Intensity >= 4 )
                toSpawn += Intensity / 4;
            if ( Intensity >= 7 )
                toSpawn++;
            if ( Intensity == 10 )
                toSpawn++;

            Planet spawnPlanet = BadgerFactionUtilityMethods.findHumanKing();
            for ( int x = 0; x < toSpawn; x++ )
            {
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
                if ( x < 3 )
                    spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );
            }
        }

        protected override void HandleEnclaveSpawning( Faction faction, ArcenSimContext Context )
        {
            int toSpawn = 1;
            if ( Intensity >= 3 )
                toSpawn += Intensity / 3;
            if ( Intensity >= 4 )
                toSpawn += Intensity / 4;
            if ( Intensity >= 7 )
                toSpawn++;
            if ( Intensity == 10 )
                toSpawn++;

            List<Planet> validPlanets = new List<Planet>();

            World_AIW2.Instance.DoForFactions( otherFaction =>
            {
                if ( otherFaction.Type == FactionType.Player && otherFaction.GetIsFriendlyTowards( faction ) )
                {
                    otherFaction.DoForControlledPlanets( planet =>
                    {
                        if ( planet.GetCommandStationOrNull() != null )
                        {
                            validPlanets.Add( planet );
                        }

                        return DelReturn.Continue;
                    } );
                }
                return DelReturn.Continue;
            } );

            for ( int x = 0; x < toSpawn; x++ )
            {
                Planet spawnPlanet = validPlanets[Context.RandomToUse.Next( validPlanets.Count )];

                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
            }
        }

        protected override void HandleHiveExpansion( Faction faction, ArcenSimContext Context )
        {
            // The player gets a free Hive on every planet if they don't already have one.
            // They otherwise receive only a single new hive every 15 minutes, unless manually built.

            List<Planet> forceSpawn = new List<Planet>();
            List<Planet> validPlanets = new List<Planet>();

            World_AIW2.Instance.DoForFactions( otherFaction =>
            {
                if ( otherFaction.Type == FactionType.Player && otherFaction.GetIsFriendlyTowards( faction ) )
                {
                    otherFaction.DoForControlledPlanets( planet =>
                    {
                        if ( planet.GetCommandStationOrNull() != null )
                        {
                            validPlanets.Add( planet );
                            if ( !HivePlanets.Contains( planet ) )
                                forceSpawn.Add( planet );
                        }

                        return DelReturn.Continue;
                    } );
                }
                return DelReturn.Continue;
            } );

            Planet spawnPlanet = validPlanets[Context.RandomToUse.Next( validPlanets.Count )];

            spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );

            for ( int x = 0; x < forceSpawn.Count; x++ )
            {
                forceSpawn[x].Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );
            }
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( !IsEnabled )
                return;

            AddAndClaimHivesFromPlayers( faction, Context );

            base.DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( faction, Context );
        }

        private void AddAndClaimHivesFromPlayers( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand addHiveToBuildListsCommand = GameCommand.Create( GameCommandTypeTable.Instance.GetRowByName( Commands.AddHivesToBuildList.ToString() ), GameCommandSource.AnythingElse );
            GameCommand takeFullyBuiltHivesCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.ClaimHivesFromHumanAllies.ToString() ), GameCommandSource.AnythingElse, faction );

            World_AIW2.Instance.DoForFactions( otherFaction =>
            {
                if ( otherFaction.Type == FactionType.Player && otherFaction.GetIsFriendlyTowards( faction ) )
                {
                    otherFaction.DoForEntities( ( GameEntity_Squad entity ) =>
                    {
                        if ( entity.TypeData.IsCommandStation )
                            addHiveToBuildListsCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );

                        if ( entity.TypeData.InternalName == HUMAN_HIVE_NAME
                         && entity.SecondsSpentAsRemains <= 0 && entity.SelfBuildingMetalRemaining <= 0 )
                            takeFullyBuiltHivesCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );

                        return DelReturn.Continue;
                    } );
                }
                return DelReturn.Continue;
            } );

            if ( addHiveToBuildListsCommand.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( addHiveToBuildListsCommand );
            if ( takeFullyBuiltHivesCommand.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( takeFullyBuiltHivesCommand );
        }

    }
}
