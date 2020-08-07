using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using System;
using System.Collections.Generic;

//TODO - Setup Enclaves to only claim a singular Youngling type each.

namespace PreceptsOfThePrecursors
{
    public static class EnclaveSettings
    {
        public static int GetInt( Faction faction, GalaxyIntegers setting )
        {
            bool useCustom = AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( faction.SpecialFactionData.InternalName.Substring( 14 ) + GalaxyBooleans.EnclaveSettingsEnabled.ToString() );
            if ( useCustom )
                return AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( faction.SpecialFactionData.InternalName.Substring( 14 ) + setting.ToString() );
            else
                return AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( setting.ToString() );
        }
        public static bool GetIsEnabled( Faction faction )
        {
            return AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( faction.SpecialFactionData.InternalName.Substring( 14 ) + GalaxyBooleans.EnclaveEnabled );
        }
        public enum GalaxyBooleans
        {
            EnclaveEnabled,
            EnclaveSettingsEnabled
        }
        public enum GalaxyIntegers
        {
            EnclaveMaxHopsFromHiveToAttack,
            MakeEveryXFireteamDefensive,
            RetreatAtXHull,
            MaxDefensiveEnclave
        }
    }
    public enum Unit
    {
        YounglingWorm,
        YounglingPuffin,
        YounglingSnake,
        YounglingEel,
        YounglingCheetah,
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
        Length
    }

    // Base for all Enclave subfactions.
    public abstract class BaseRoamingEnclave : BaseSpecialFaction, IBulkPathfinding
    {
        protected override string TracingName => "RoamingEnclave";
        protected override bool EverNeedsToRunLongRangePlanning => true;

        protected virtual int EnclavesToSpawn => 1 + Intensity;
        protected virtual int HivesToSpawn => 1 + Intensity / 2;

        public ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> WormholeCommands { get; set; }
        public ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> MovementCommands { get; set; }

        // Unit names and tags.
        public static string ENCLAVE_TAG = "RoamingEnclave";
        public static string PLAYER_ENCLAVE_TAG = "PlayerRoamingEnclave";
        public static string CLANLING_HIVE_TAG = "ClanlingHive";
        public static string YOUNGLING_HIVE_TAG = "YounglingHive";
        public static string HIVE_TAG = "EnclaveHive";
        public static string YOUNGLING_TAG = "EnclaveUnit";
        public static string HUMAN_HIVE_NAME = "HiveYounglingHuman";

        public List<GameEntity_Squad> Enclaves = new List<GameEntity_Squad>();
        public List<GameEntity_Squad> Hives = new List<GameEntity_Squad>();

        public List<Planet> EnclavePlanets = new List<Planet>();
        public List<Planet> HivePlanets = new List<Planet>();

        public static bool EnclavesGloballyEnabled;
        public static int Intensity;

        public EnclaveFactionData FactionData;

        public static ArcenSparseLookup<GameEntityTypeData, int> SecondsPerUnitProduction;
        public static ArcenSparseLookup<GameEntityTypeData, GameEntityTypeData> YounglingTypeByHive;

        public static int SecondsPerInflux;
        public static int SecondsPerInfluxRandomizer;
        public static int SecondsForRespawn;
        public static int SecondsForRespawnRandomizer;

        public enum Commands
        {
            MarkUpUnits,
            PopulateEnclavesList,
            PopulateHivesList,
            PopulateEnclavePlanetList,
            PopulateHivePlanetsList,
            SetOrClearEnclaveOwnership,
            AddHivesToBuildList,
            ClaimHivesFromHumanAllies,
            LoadYounglingsIntoEnclaves,
            UnloadYounglingsFromEnclaves,
            StackYounglings
        }
        public BaseRoamingEnclave()
        {
            EnclavesGloballyEnabled = false;
            Intensity = 0;
        }

        public override bool GetShouldAttackNormallyExcludedTarget( Faction faction, GameEntity_Squad Target )
        {
            return !(this is RoamingEnclavePlayerTeam) && Target.TypeData.IsCommandStation;
        }
        public override void UpdatePowerLevel( Faction faction )
        {
            faction.OverallPowerLevel = FInt.Zero;
            if ( Hives.Count >= 10 )
                faction.OverallPowerLevel = FInt.FromParts( 1, 000 ) + ((FInt.FromParts( 0, 025 ) * (Hives.Count - 10)));
            else
                faction.OverallPowerLevel = FInt.FromParts( 0, 010 ) * Hives.Count;
        }
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( !EnclavesGloballyEnabled )
                return;

            if ( !EnclaveSettings.GetIsEnabled( faction ) )
                return;

            if ( FactionData == null )
                FactionData = faction.GetEnclaveFactionData();

            if ( faction.MustBeAwakenedByPlayer )
                faction.HasBeenAwakenedByPlayer = EnclaveSettings.GetIsEnabled( faction );

            HandleEnclaveRegeneration();

            HandleYounglingCombining( Context );

            HandleUnitSpawningForHives( Context );
        }

        private void HandleEnclaveRegeneration()
        {
            for ( int x = 0; x < Enclaves.Count; x++ )
            {
                if ( Enclaves[x].RepairDelaySeconds <= 0 )
                    Enclaves[x].TakeHullRepair( Enclaves[x].GetMaxHullPoints() / 100 );
            }
            if ( this is RoamingEnclavePlayerTeam )
                for ( int x = 0; x < (this as RoamingEnclavePlayerTeam).PlayerEnclaves.Count; x++ )
                    if ( (this as RoamingEnclavePlayerTeam).PlayerEnclaves[x].RepairDelaySeconds <= 0 )
                        (this as RoamingEnclavePlayerTeam).PlayerEnclaves[x].TakeHullRepair( Enclaves[x].GetMaxHullPoints() / 100 );
        }

        private void HandleYounglingCombining( ArcenSimContext Context )
        {
            for ( int x = 0; x < Enclaves.Count; x++ )
                Enclaves[x].CombineYounglingsIfAble( Context );
        }

        private void HandleUnitSpawningForHives( ArcenSimContext Context )
        {
            for ( int y = 0; y < Hives.Count; y++ )
                if ( CanSpawnUnits( Hives[y] ) )
                    SpawnUnitsForHive( Hives[y], Context );
        }

        private bool CanSpawnUnits( GameEntity_Squad hiveOrNull )
        {
            GameEntityTypeData younglingData = hiveOrNull != null && YounglingTypeByHive.GetHasKey( hiveOrNull.TypeData ) ? YounglingTypeByHive[hiveOrNull.TypeData] : GameEntityTypeDataTable.Instance.GetRowByName( "YounglingWorm" );

            return World_AIW2.Instance.GameSecond % SecondsPerUnitProduction[younglingData] == 0;
        }

        private void SpawnUnitsForHive( GameEntity_Squad hive, ArcenSimContext Context )
        {
            GameEntity_Squad unit = GameEntity_Squad.CreateNew( hive.PlanetFaction, YounglingTypeByHive[hive.TypeData], hive.CurrentMarkLevel, hive.PlanetFaction.FleetUsedAtPlanet, 0, hive.WorldLocation, Context );
            unit.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, hive.PlanetFaction.Faction.FactionIndex );
            unit.MinorFactionStackingID = -1;
        }

        public abstract Planet BulkSpawn( Faction faction, ArcenSimContext Context );

        public abstract Planet GetPlanetForBulkSpawn( Faction faction, ArcenSimContext Context );

        public abstract void HandleEnclaveSpawning( Faction faction, ArcenSimContext Context );

        public abstract void HandleHiveExpansion( Faction faction, ArcenSimContext Context );

        private int AttackHops;
        private int FireteamsPerDefense;
        private int RetreatPercentage;

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( !EnclavesGloballyEnabled )
                return;

            if ( !EnclaveSettings.GetIsEnabled( faction ) )
                return;

            AttackHops = EnclaveSettings.GetInt( faction, EnclaveSettings.GalaxyIntegers.EnclaveMaxHopsFromHiveToAttack );
            FireteamsPerDefense = EnclaveSettings.GetInt( faction, EnclaveSettings.GalaxyIntegers.MakeEveryXFireteamDefensive );
            RetreatPercentage = EnclaveSettings.GetInt( faction, EnclaveSettings.GalaxyIntegers.RetreatAtXHull );

            FactionData.TeamsAimedAtPlanet.Clear();

            Fireteam.DoFor( FactionData.Teams, delegate ( Fireteam team )
            {
                team.Reset();
                return DelReturn.Continue;
            } );

            SetupHives( faction, Context );
            SetupEnclaves( faction, Context );
            SetupYounglings( faction, Context );

            ArcenSparseLookup<Planet, List<Fireteam>> fireteamsAttacking = new ArcenSparseLookup<Planet, List<Fireteam>>();

            Fireteam.DoFor( FactionData.Teams, delegate ( Fireteam team )
            {
                if ( team.ships.Count == 0 )
                    team.Disband( Context );
                else
                {
                    team.NoDeathballing = true;

                    team.DefenseMode = FireteamsPerDefense > 0 ? (team.id % FireteamsPerDefense == 0) : false;

                    switch ( team.status )
                    {
                        case FireteamStatus.Attacking:
                            if ( team.TargetPlanet != null )
                            {
                                if ( fireteamsAttacking.GetHasKey( team.TargetPlanet ) )
                                    fireteamsAttacking[team.TargetPlanet].Add( team );
                                else
                                    fireteamsAttacking.AddPair( team.TargetPlanet, new List<Fireteam>() { team } );
                            }
                            break;
                        case FireteamStatus.Assembling:
                        case FireteamStatus.Staging:
                        case FireteamStatus.ReadyToAttack:
                            bool discarded = false;
                            if ( team.LurkPlanet != null && team.CurrentPlanet == team.LurkPlanet )
                            {
                                int idleSince = -1;
                                if ( team.History.Count > 0 )
                                    for ( int x = 0; x < team.History.Count; x++ )
                                        idleSince = Math.Max( idleSince, team.History[x].GameSecond );
                                if ( idleSince > 0 && World_AIW2.Instance.GameSecond - idleSince > 15 )
                                {
                                    team.DiscardCurrentObjectives();
                                    discarded = true;
                                }
                            }
                            if ( !discarded && team.TargetPlanet != null )
                            {
                                if ( fireteamsAttacking.GetHasKey( team.TargetPlanet ) )
                                    fireteamsAttacking[team.TargetPlanet].Add( team );
                                else
                                    fireteamsAttacking.AddPair( team.TargetPlanet, new List<Fireteam>() { team } );
                            }
                            break;
                        default:
                            break;
                    }
                }
                return DelReturn.Continue;
            } );
            fireteamsAttacking.DoFor( pair =>
            {
                var stanceData = pair.Key.GetPlanetFactionForFaction( faction ).DataByStance;

                int hostileStrength = stanceData[FactionStance.Hostile].TotalStrength;
                int friendlyStrength = stanceData[FactionStance.Friendly].TotalStrength;
                int ourStrength = stanceData[FactionStance.Self].TotalStrength;

                for ( int x = 0; x < pair.Value.Count; x++ )
                {
                    Fireteam team = pair.Value[x];
                    team.BuildShipsLookup( true, null );
                    team.shipsByPlanet.DoFor( ships =>
                    {
                        if ( ships.Key == pair.Key )
                            return DelReturn.Continue;

                        for ( int y = 0; y < ships.Value.Count; y++ )
                            ourStrength += (ships.Value[y].GetStrengthPerSquad() * (1 + ships.Value[y].ExtraStackedSquadsInThis)) + ships.Value[y].AdditionalStrengthFromFactions;

                        return DelReturn.Continue;
                    } );
                }

                // Run from a losing fight.
                if ( hostileStrength > (friendlyStrength + ourStrength) * 2 )
                    for ( int x = 0; x < pair.Value.Count; x++ )
                        pair.Value[x].DisbandAndRetreat( faction, Context, GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( faction, pair.Value[x].CurrentPlanet, Context ) );
                else
                    // Bleed off Enclaves from winning fights.
                    while ( ourStrength + friendlyStrength > hostileStrength * 5 && pair.Value.Count > 0 )
                    {
                        ourStrength -= pair.Value[0].TeamStrength;
                        pair.Value[0].DisbandAndRetreat( faction, Context, GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( faction, pair.Value[0].CurrentPlanet, Context ) );
                        pair.Value.RemoveAt( 0 );
                    }

                return DelReturn.Continue;
            } );
            FireteamUtility.CleanUpDisbandedFireteams( FactionData.Teams );
            ArcenCharacterBuffer buffer = this.tracingBuffer_longTerm;
            FireteamUtility.UpdateFireteams( faction, Context, FactionData.Teams, FactionData.TeamsAimedAtPlanet, buffer, FInt.One );
            FireteamUtility.UpdateRegiments( faction, Context, FactionData.Teams, FactionData.TeamsAimedAtPlanet, buffer, 1, true );

            faction.ExecuteMovementCommands( Context );
            faction.ExecuteWormholeCommands( Context );
        }

        private void SetupEnclaves( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand markUpCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.MarkUpUnits.ToString() ), GameCommandSource.AnythingElse, faction );
            GameCommand enclavePopulateCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.PopulateEnclavesList.ToString() ), GameCommandSource.AnythingElse, faction );
            GameCommand enclavePlanetsPopulateCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.PopulateEnclavePlanetList.ToString() ), GameCommandSource.AnythingElse, faction );
            GameCommand enclaveUnloadCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.UnloadYounglingsFromEnclaves.ToString() ), GameCommandSource.AnythingElse, faction );

            List<GameEntity_Squad> enclavesThatNeedFireteam = new List<GameEntity_Squad>();

            faction.DoForEntities( ENCLAVE_TAG, enclave =>
            {
                enclavePopulateCommand.RelatedEntityIDs.Add( enclave.PrimaryKeyID );
                if ( !enclavePlanetsPopulateCommand.RelatedIntegers.Contains( enclave.Planet.Index ) )
                {
                    enclavePlanetsPopulateCommand.RelatedIntegers.Add( enclave.Planet.Index );
                    if ( faction.GetIsHostileTowards( enclave.Planet.GetControllingOrInfluencingFaction() ) )
                        BadgerFactionUtilityMethods.FlushUnitsFromReinforcementPoints( enclave.Planet, faction, Context );
                }
                if ( enclave.CurrentMarkLevel < 7 && enclave.GetSecondsSinceCreation() > enclave.CurrentMarkLevel * 1800 )
                    markUpCommand.RelatedEntityIDs.Add( enclave.PrimaryKeyID );

                int alliedStrength = enclave.PlanetFaction.DataByStance[FactionStance.Self].TotalStrength + enclave.PlanetFaction.DataByStance[FactionStance.Friendly].TotalStrength;
                int hostileStrength = enclave.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength;

                if ( RetreatPercentage > 0 && enclave.GetCurrentHullPoints() < (enclave.GetMaxHullPoints() / 100) * RetreatPercentage )
                {
                    if ( enclave.Planet.GetHopsTo( GetNearestHivePlanetBackgroundThreadOnly( faction, enclave.Planet, Context, true ) ) > 0 )
                        enclave.QueueWormholeCommand( GetNearestHivePlanetBackgroundThreadOnly( faction, enclave.Planet, Context, true ) );
                    enclave.FireteamId = -1;
                }
                else
                {
                    if ( enclave.FireteamId < 0 )
                    {
                        if ( enclave.Planet.GetHopsTo( GetNearestHivePlanetBackgroundThreadOnly( faction, enclave.Planet, Context ) ) == 0 )
                        {
                            if ( enclave.GetCurrentHullPoints() >= (enclave.GetMaxHullPoints() / 100) * 90 )
                            {
                                enclavesThatNeedFireteam.Add( enclave );
                            }
                        }
                        else
                        {
                            if ( hostileStrength < 500 || alliedStrength < hostileStrength )
                                enclave.QueueWormholeCommand( GetNearestHivePlanetBackgroundThreadOnly( faction, enclave.Planet, Context ) );
                        }
                    }
                    else
                    {
                        Fireteam team = this.GetFireteamById( faction, enclave.FireteamId );
                        if ( team != null )
                            team.AddUnit( enclave );
                        else
                            enclave.FireteamId = -1; //something happened to the fireteam, so lets find a new one next LRP stage
                    }
                }

                if ( hostileStrength > 0 )
                    enclaveUnloadCommand.RelatedEntityIDs.Add( enclave.PrimaryKeyID );

                return DelReturn.Continue;
            } );

            if ( enclavesThatNeedFireteam.Count > 0 )
            {
                List<int> takenID = new List<int>();
                Fireteam.DoFor( FactionData.Teams, workingTeam =>
                 {
                     if ( workingTeam.ships.Count > 0 )
                         takenID.Add( workingTeam.id );

                     return DelReturn.Continue;
                 } );
                for ( int x = 0; x < enclavesThatNeedFireteam.Count; x++ )
                {
                    GameEntity_Squad enclave = enclavesThatNeedFireteam[x];
                    Fireteam team = new Fireteam();
                    team.MyStrengthMultiplierForStrengthCalculation = FInt.One;
                    team.EnemyStrengthMultiplierForStrengthCalculation = FInt.One;
                    team.id = 1;
                    while ( takenID.Contains( team.id ) )
                        team.id++;
                    takenID.Add( team.id );
                    team.StrengthToBringOnline = 0;
                    team.NoDeathballing = true;
                    team.AddUnit( enclave );
                    FactionData.Teams.AddIfNotAlreadyIn( team );
                    team.DefenseMode = FireteamsPerDefense > 0 ? (team.id % FireteamsPerDefense == 0) : false;
                }
            }

            if ( markUpCommand.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( markUpCommand );
            Context.QueueCommandForSendingAtEndOfContext( enclavePopulateCommand );
            Context.QueueCommandForSendingAtEndOfContext( enclavePlanetsPopulateCommand );
            if ( enclaveUnloadCommand.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( enclaveUnloadCommand );
        }

        private Planet GetNearestHivePlanetBackgroundThreadOnly( Faction faction, Planet planet, ArcenLongTermIntermittentPlanningContext Context, bool ignoreSelf = false )
        {
            Planet bestPlanet = null;
            for ( int x = 0; x < HivePlanetsForBackgroundThreadOnly.Count; x++ )
            {
                Planet hivePlanet = HivePlanetsForBackgroundThreadOnly[x];
                if ( planet == hivePlanet && ignoreSelf )
                    continue;
                int hops = planet.GetHopsTo( hivePlanet );

                if ( bestPlanet == null )
                    bestPlanet = hivePlanet;
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
            GameCommand loadYounglingsCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.LoadYounglingsIntoEnclaves.ToString() ), GameCommandSource.AnythingElse, faction );

            List<GameEntity_Squad> enclaves = new List<GameEntity_Squad>();
            faction.DoForEntities( ENCLAVE_TAG, workingEnclave =>
            {
                enclaves.Add( workingEnclave );

                return DelReturn.Continue;
            } );

            ArcenSparseLookup<Planet, ArcenSparseLookup<int, ArcenSparseLookup<byte, List<GameEntity_Squad>>>> younglingGroups = new ArcenSparseLookup<Planet, ArcenSparseLookup<int, ArcenSparseLookup<byte, List<GameEntity_Squad>>>>();
            int totalCount = 0;

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

                // If player controlled, leave the Youngling alone.
                if ( enclave.PlanetFaction.Faction.Type == FactionType.Player )
                    return DelReturn.Continue;

                ownershipCommand.RelatedIntegers.Add( youngling.PrimaryKeyID );
                ownershipCommand.RelatedIntegers2.Add( enclave.PrimaryKeyID );

                Fireteam team = this.GetFireteamById( faction, enclave.FireteamId );

                if ( youngling.Planet != enclave.Planet )
                {
                    if ( youngling.LongRangePlanningData.FinalDestinationPlanetIndex == -1 )
                        youngling.QueueWormholeCommand( enclave.Planet, Context );
                    youngling.FireteamId = -1;
                    if ( team != null )
                        team.TeamStrength += youngling.GetStrengthPerSquad() * (1 + youngling.ExtraStackedSquadsInThis);
                }
                else
                {
                    if ( youngling.GetCurrentHullPoints() < youngling.GetCurrentHullPoints() * 0.33 || youngling.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength <= 50 )
                    {
                        youngling.FireteamId = -1;
                        loadYounglingsCommand.RelatedIntegers.Add( youngling.PrimaryKeyID );
                        loadYounglingsCommand.RelatedIntegers2.Add( enclave.PrimaryKeyID );
                        if ( team != null )
                            team.TeamStrength += youngling.GetStrengthPerSquad() * (1 + youngling.ExtraStackedSquadsInThis);
                    }
                    else
                    {
                        if ( team != null )
                            team.AddUnit( youngling );
                        else
                            youngling.FireteamId = -1;
                    }
                }

                totalCount++;

                if ( !younglingGroups.GetHasKey( youngling.Planet ) )
                    younglingGroups.AddPair( youngling.Planet, new ArcenSparseLookup<int, ArcenSparseLookup<byte, List<GameEntity_Squad>>>() );
                if ( !younglingGroups[youngling.Planet].GetHasKey( youngling.TypeData.RowIndexNonSim ) )
                    younglingGroups[youngling.Planet].AddPair( youngling.TypeData.RowIndexNonSim, new ArcenSparseLookup<byte, List<GameEntity_Squad>>() );
                if ( !younglingGroups[youngling.Planet][youngling.TypeData.RowIndexNonSim].GetHasKey( youngling.CurrentMarkLevel ) )
                    younglingGroups[youngling.Planet][youngling.TypeData.RowIndexNonSim].AddPair( youngling.CurrentMarkLevel, new List<GameEntity_Squad>() );

                younglingGroups[youngling.Planet][youngling.TypeData.RowIndexNonSim][youngling.CurrentMarkLevel].Add( youngling );

                return DelReturn.Continue;
            } );

            younglingGroups.DoFor( mainPair =>
            {
                int onPlanet = 0;
                mainPair.Value.DoFor( subPair =>
                {
                    subPair.Value.DoFor( pair =>
                    {
                        onPlanet += pair.Value.Count;

                        return DelReturn.Continue;
                    } );
                    return DelReturn.Continue;
                } );

                if ( onPlanet > 100 )
                    mainPair.Value.DoFor( subPair =>
                    {
                        subPair.Value.DoFor( pair =>
                        {
                            if ( pair.Value.Count < 2 )
                                return DelReturn.Continue;
                            GameCommand stackYounglingsCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.StackYounglings.ToString() ), GameCommandSource.AnythingElse, faction );
                            for ( int x = 0; x < pair.Value.Count; x++ )
                                stackYounglingsCommand.RelatedEntityIDs.Add( pair.Value[x].PrimaryKeyID );

                            Context.QueueCommandForSendingAtEndOfContext( stackYounglingsCommand );
                            return DelReturn.Continue;
                        } );
                        return DelReturn.Continue;
                    } );
                return DelReturn.Continue;
            } );

            if ( markUpCommand.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( markUpCommand );
            Context.QueueCommandForSendingAtEndOfContext( ownershipCommand );
            if ( loadYounglingsCommand.RelatedIntegers.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( loadYounglingsCommand );
        }

        #region Fireteams
        private List<Planet> HivePlanetsForBackgroundThreadOnly = new List<Planet>();

        public override Fireteam GetFireteamById( Faction faction, int id )
        {
            return FireteamUtility.GetFireteamById( FactionData.Teams, id );
        }
        public override FireteamBase GetFireteamBaseById( Faction faction, int id )
        {
            return FireteamUtility.GetFireteamById( FactionData.Teams, id );
        }

        public override void GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass( Faction faction, bool DefenseMode, Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, ref List<FireteamTarget> PreferredTargets, ref List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            PreferredTargets = new List<FireteamTarget>();
            FallbackTargets = new List<FireteamTarget>();

            List<Planet> hivesInDanger = new List<Planet>();
            List<Planet> alliedDefense = new List<Planet>();
            List<Planet> alliedAssaults = new List<Planet>();
            List<Planet> hivesThreatened = new List<Planet>();
            List<Planet> planetsToAttack = new List<Planet>();
            ArcenSparseLookup<int, List<Planet>> planetsByEnclaveCount = new ArcenSparseLookup<int, List<Planet>>();

            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                int hops = planet.GetHopsTo( GetNearestHivePlanetBackgroundThreadOnly( faction, planet, Context ) );

                int friendlyStrength = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Friendly].TotalStrength;
                int hostileStrength = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Hostile].TotalStrength;

                if ( friendlyStrength > 2500 && hostileStrength > 2500 && !Fireteam.IsThisAWinningBattle( faction, Context, planet, 5, false ) )
                {
                    if ( hops <= 1 )
                        alliedDefense.Add( planet );
                    else if ( hops <= AttackHops )
                        alliedAssaults.Add( planet );
                }
                if ( hops == 0 )
                {
                    if ( hostileStrength > 2500 && !Fireteam.IsThisAWinningBattle( faction, Context, planet, 5, false ) )
                        hivesInDanger.Add( planet );

                    int enclaveOnPlanet = 0;

                    planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( ENCLAVE_TAG, entity =>
                    {
                        if ( entity.PlanetFaction.Faction == faction )
                            enclaveOnPlanet++;

                        return DelReturn.Continue;
                    } );

                    if ( planetsByEnclaveCount.GetHasKey( enclaveOnPlanet ) )
                        planetsByEnclaveCount[enclaveOnPlanet].Add( planet );
                    else
                        planetsByEnclaveCount.AddPair( enclaveOnPlanet, new List<Planet>() { planet } );
                }
                else if ( hops == 1 && hostileStrength > 2500 && !Fireteam.IsThisAWinningBattle( faction, Context, planet, 5, false ) )
                {
                    hivesThreatened.Add( planet );
                }
                else if ( hops <= AttackHops && hostileStrength > 2500 && Fireteam.GetDangerOfPath( faction, Context, CurrentPlanetForFireteam, planet, false, out short _ ) < 500 && !Fireteam.IsThisAWinningBattle( faction, Context, planet, 5, false ) )
                {
                    planetsToAttack.Add( planet );
                }

                return DelReturn.Continue;
            } );

            planetsByEnclaveCount.Sort( ( pair1, pair2 ) => pair1.Key.CompareTo( pair2.Key ) );

            if ( DefenseMode )
            {
                if ( hivesInDanger.Count > 0 )
                {
                    for ( int x = 0; x < hivesInDanger.Count; x++ )
                        PreferredTargets.Add( new FireteamTarget( hivesInDanger[x] ) );
                }
                if ( hivesThreatened.Count > 0 )
                {
                    if ( PreferredTargets.Count == 0 )
                        for ( int x = 0; x < hivesThreatened.Count; x++ )
                            PreferredTargets.Add( new FireteamTarget( hivesThreatened[x] ) );
                    else
                        for ( int x = 0; x < hivesThreatened.Count; x++ )
                            FallbackTargets.Add( new FireteamTarget( hivesThreatened[x] ) );
                }
                if ( planetsByEnclaveCount.GetPairCount() > 0 )
                {
                    if ( PreferredTargets.Count == 0 )
                        for ( int x = 0; x < planetsByEnclaveCount.GetPairByIndex( 0 ).Value.Count; x++ )
                            PreferredTargets.Add( new FireteamTarget( planetsByEnclaveCount.GetPairByIndex( 0 ).Value[x] ) );
                    else if ( FallbackTargets.Count == 0 )
                        for ( int x = 0; x < planetsByEnclaveCount.GetPairByIndex( 0 ).Value.Count; x++ )
                            FallbackTargets.Add( new FireteamTarget( planetsByEnclaveCount.GetPairByIndex( 0 ).Value[x] ) );
                }
            }
            else
            {
                if ( hivesInDanger.Count > 0 )
                {
                    for ( int x = 0; x < hivesInDanger.Count; x++ )
                        PreferredTargets.Add( new FireteamTarget( hivesInDanger[x] ) );
                }
                if ( hivesThreatened.Count > 0 )
                {
                    if ( PreferredTargets.Count == 0 )
                        for ( int x = 0; x < hivesThreatened.Count; x++ )
                            PreferredTargets.Add( new FireteamTarget( hivesThreatened[x] ) );
                    else
                        for ( int x = 0; x < hivesThreatened.Count; x++ )
                            FallbackTargets.Add( new FireteamTarget( hivesThreatened[x] ) );
                }
                if ( alliedAssaults.Count > 0 )
                {
                    if ( PreferredTargets.Count == 0 )
                        for ( int x = 0; x < alliedAssaults.Count; x++ )
                            PreferredTargets.Add( new FireteamTarget( alliedAssaults[x] ) );
                    else if ( FallbackTargets.Count == 0 )
                        for ( int x = 0; x < alliedAssaults.Count; x++ )
                            FallbackTargets.Add( new FireteamTarget( alliedAssaults[x] ) );
                }
                if ( planetsToAttack.Count > 0 )
                {
                    if ( PreferredTargets.Count == 0 )
                        for ( int x = 0; x < planetsToAttack.Count; x++ )
                            PreferredTargets.Add( new FireteamTarget( planetsToAttack[x] ) );
                    else if ( FallbackTargets.Count == 0 )
                        for ( int x = 0; x < planetsToAttack.Count; x++ )
                            FallbackTargets.Add( new FireteamTarget( planetsToAttack[x] ) );
                }
                if ( planetsByEnclaveCount.GetPairCount() > 0 )
                {
                    if ( PreferredTargets.Count == 0 )
                        for ( int x = 0; x < planetsByEnclaveCount.GetPairByIndex( 0 ).Value.Count; x++ )
                            PreferredTargets.Add( new FireteamTarget( planetsByEnclaveCount.GetPairByIndex( 0 ).Value[x] ) );
                    else if ( FallbackTargets.Count == 0 )
                        for ( int x = 0; x < planetsByEnclaveCount.GetPairByIndex( 0 ).Value.Count; x++ )
                            FallbackTargets.Add( new FireteamTarget( planetsByEnclaveCount.GetPairByIndex( 0 ).Value[x] ) );
                }
            }
        }

        public override Planet GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( Faction faction, Planet TargetPlanet, int TeamStrength, Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context )
        {
            return GetNearestHivePlanetBackgroundThreadOnly( faction, TargetPlanet, Context );
        }

        public override GameEntity_Squad GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( Faction faction, Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context )
        {
            return GetNearestHivePlanetBackgroundThreadOnly( faction, CurrentPlanetForFireteam, Context ).GetPlanetFactionForFaction( faction ).Entities.GetFirstMatching( HIVE_TAG, false, true );
        }
        #endregion
    }

    // Enabler subfaction.
    public class RoamingEnclaveEnabler : BaseSpecialFaction
    {
        protected override string TracingName => "RoamingEnclave";
        protected override bool EverNeedsToRunLongRangePlanning => false;

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( BaseRoamingEnclave.SecondsPerUnitProduction == null )
            {
                BaseRoamingEnclave.SecondsPerUnitProduction = new ArcenSparseLookup<GameEntityTypeData, int>();
                bool useExternal = ExternalConstants.Instance.GetCustomData_Slow( "RoamingEnclaves" ).GetBool_Slow( "MetalCostOverride" );

                for ( int x = 0; x < (int)Unit.Length; x++ )
                {
                    GameEntityTypeData entityType = GameEntityTypeDataTable.Instance.GetRowByName( ((Unit)x).ToString() );
                    int baseCost;
                    if ( useExternal || entityType.MetalCost == 0 )
                    {
                        if ( x == 0 )
                            baseCost = ExternalConstants.Instance.GetCustomData_Slow( "RoamingEnclaves" ).GetInt_Slow( "WormCost" );
                        else
                            baseCost = ExternalConstants.Instance.GetCustomData_Slow( "RoamingEnclaves" ).GetInt_Slow( "YounglingCost" );
                    }
                    else
                        baseCost = GameEntityTypeDataTable.Instance.GetRowByName( ((Unit)x).ToString() ).MetalCost;
                    ArcenDebugging.SingleLineQuickDebug( $"Setting up Youngling Cost for {entityType.DisplayName}" );
                    int secondsPer = baseCost / (20 + (faction.Ex_MinorFactionCommon_GetPrimitives().Intensity * 3));
                    if ( secondsPer < 1 )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( $"Error! Per Second value for {entityType.DisplayName} is {secondsPer}, it must be at least 1. Defaulting the value to 1.", Verbosity.ShowAsError );
                        secondsPer = 1;
                    }
                    ArcenDebugging.SingleLineQuickDebug( $"Base cost: {baseCost}, secondsPer: {secondsPer}" );
                    BaseRoamingEnclave.SecondsPerUnitProduction.AddPair( entityType, secondsPer );
                }

                BaseRoamingEnclave.YounglingTypeByHive = new ArcenSparseLookup<GameEntityTypeData, GameEntityTypeData>();
                List<GameEntityTypeData> hiveData = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( BaseRoamingEnclave.HIVE_TAG );
                for ( int x = 0; x < hiveData.Count; x++ )
                {
                    GameEntityTypeData younglingData = GameEntityTypeDataTable.Instance.GetRowByName( hiveData[x].InternalName.Substring( 4 ) );
                    if ( younglingData != null )
                        BaseRoamingEnclave.YounglingTypeByHive.AddPair( hiveData[x], younglingData );
                }

                BaseRoamingEnclave.SecondsPerInflux = ExternalConstants.Instance.GetCustomData_Slow( "RoamingEnclaves" ).GetInt_Slow( "SecondsBetweenInfluxPeriod" );
                BaseRoamingEnclave.SecondsPerInfluxRandomizer = ExternalConstants.Instance.GetCustomData_Slow( "RoamingEnclaves" ).GetInt_Slow( "SecondsBetweenInfluxPeriodRandomizer" );
                BaseRoamingEnclave.SecondsForRespawn = ExternalConstants.Instance.GetCustomData_Slow( "RoamingEnclaves" ).GetInt_Slow( "SecondsUntilSubfactionRespawn" );
                BaseRoamingEnclave.SecondsForRespawnRandomizer = ExternalConstants.Instance.GetCustomData_Slow( "RoamingEnclaves" ).GetInt_Slow( "SecondsUntilSubfactionRespawnRandomizer" );
            }

            BaseRoamingEnclave.EnclavesGloballyEnabled = true;
            BaseRoamingEnclave.Intensity = faction.Ex_MinorFactionCommon_GetPrimitives().Intensity;

            base.DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim( faction, Context );
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            EnclaveWorldData worldData = World.Instance.GetEnclaveWorldData();
            bool isInflux = false;
            if ( worldData.SecondsUntilNextInflux == 0 )
            {
                isInflux = true;
                World_AIW2.Instance.QueueChatMessageOrCommand( "A new influx of Neinzul have arrived in our galaxy.", ChatType.LogToCentralChat, Context );
                worldData.SecondsUntilNextInflux = BaseRoamingEnclave.SecondsPerInflux + Context.RandomToUse.Next( -BaseRoamingEnclave.SecondsPerInfluxRandomizer, BaseRoamingEnclave.SecondsPerInfluxRandomizer );
            }
            else
                worldData.SecondsUntilNextInflux--;

            World_AIW2.Instance.DoForFactions( otherFaction =>
            {
                if ( otherFaction.Implementation is BaseRoamingEnclave && EnclaveSettings.GetIsEnabled( otherFaction ) )
                {
                    BaseRoamingEnclave REFaction = otherFaction.Implementation as BaseRoamingEnclave;
                    if ( REFaction.FactionData == null )
                        REFaction.FactionData = otherFaction.GetEnclaveFactionData();
                    if ( REFaction.Hives.Count == 0 )
                    {
                        if ( REFaction.FactionData.SecondsUntilNextRespawn == -1 )
                        {
                            REFaction.FactionData.SecondsUntilNextRespawn = BaseRoamingEnclave.SecondsForRespawn + Context.RandomToUse.Next( -BaseRoamingEnclave.SecondsForRespawnRandomizer, BaseRoamingEnclave.SecondsForRespawnRandomizer );
                        }
                        if ( REFaction.FactionData.SecondsUntilNextRespawn > 0 )
                            REFaction.FactionData.SecondsUntilNextRespawn--;
                        if ( REFaction.FactionData.SecondsUntilNextRespawn == 0 )
                        {
                            Planet spawnPlanet = REFaction.BulkSpawn( otherFaction, Context );
                            REFaction.FactionData.SecondsUntilNextRespawn = -1;
                            if ( spawnPlanet != null && spawnPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                            {
                                World_AIW2.Instance.QueueChatMessageOrCommand( $"The {otherFaction.StartFactionColourForLog()}{otherFaction.GetDisplayName()}</color> has arrived on {spawnPlanet.Name}.", ChatType.LogToCentralChat, Context );
                            }
                        }
                        return DelReturn.Continue;
                    }
                    else
                        REFaction.FactionData.SecondsUntilNextRespawn = -1;

                    if ( isInflux )
                    {
                        REFaction.HandleEnclaveSpawning( otherFaction, Context );
                        REFaction.HandleHiveExpansion( otherFaction, Context );
                    }
                }
                return DelReturn.Continue;
            } );
        }


    }

    // Acts as a base for all npc subfactions.
    public abstract class RoamingEnclaveNPC : BaseRoamingEnclave
    {
        public override void HandleEnclaveSpawning( Faction faction, ArcenSimContext Context )
        {
            if ( HivePlanets.Count == 0 )
                return;

            int toSpawn = EnclavesToSpawn;

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

        public override void HandleHiveExpansion( Faction faction, ArcenSimContext Context )
        {
            if ( HivePlanets.Count == 0 )
                return;

            int toSpawn = HivesToSpawn;
            ArcenSparseLookup<Planet, int> validPlanets = new ArcenSparseLookup<Planet, int>();

            for ( int x = 0; x < HivePlanets.Count; x++ )
            {
                HivePlanets[x].DoForLinkedNeighbors( false, planet =>
                {
                    if ( planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Hostile].TotalStrength > 2500 )
                        return DelReturn.Continue;

                    if ( HivePlanets.Contains( planet ) )
                    {
                        int hivesOnPlanet = 0;
                        for ( int y = 0; y < Hives.Count; y++ )
                            if ( Hives[y].Planet == planet )
                                hivesOnPlanet++;
                        if ( hivesOnPlanet < Intensity )
                            if ( validPlanets.GetHasKey( planet ) )
                                validPlanets[planet]++;
                            else
                                validPlanets.AddPair( planet, 1 );
                    }
                    else
                    {
                        planet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, YOUNGLING_HIVE_TAG ), PlanetSeedingZone.OuterSystem );
                        toSpawn--;
                    }

                    if ( toSpawn == 1 )
                        return DelReturn.Break;

                    return DelReturn.Continue;
                } );
            }

            while ( toSpawn > 0 && validPlanets.GetPairCount() > 0 )
                for ( int x = 0; x < validPlanets.GetPairCount() && toSpawn > 0; x++ )
                {
                    Planet planet = validPlanets.GetPairByIndex( x ).Key;
                    planet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, YOUNGLING_HIVE_TAG ), PlanetSeedingZone.OuterSystem );
                    validPlanets[planet]++;
                    toSpawn--;
                    if ( validPlanets[planet] >= Intensity )
                    {
                        validPlanets.RemovePairByKey( planet );
                        x--;
                    }
                }

            if ( faction.HasObtainedSpireDebris )
                World_AIW2.Instance.DoForPlanets( false, planet =>
                {
                    int clanlingHives = 0, younglingHives = 0;
                    planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( HIVE_TAG, ( GameEntity_Squad entity ) =>
                    {
                        if ( entity.TypeData.GetHasTag( YOUNGLING_HIVE_TAG ) )
                            younglingHives++;
                        else
                            clanlingHives++;

                        return DelReturn.Continue;
                    } );

                    for ( int x = younglingHives; x >= 3; x -= 3 )
                        if ( clanlingHives > 0 )
                        {
                            clanlingHives--;
                            continue;
                        }
                        else
                        {
                            planet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, CLANLING_HIVE_TAG ), PlanetSeedingZone.OuterSystem );
                        }

                    return DelReturn.Continue;
                } );
        }

        public override Planet BulkSpawn( Faction faction, ArcenSimContext Context )
        {
            Planet spawnPlanet = GetPlanetForBulkSpawn( faction, Context );

            if ( spawnPlanet == null )
                return null;

            GameEntityTypeData hiveData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, YOUNGLING_HIVE_TAG );
            GameEntityTypeData enclaveData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG );

            for ( int x = 0; x < HivesToSpawn; x++ )
                spawnPlanet.Mapgen_SeedEntity( Context, faction, hiveData, PlanetSeedingZone.OuterSystem );

            for ( int x = 0; x < EnclavesToSpawn; x++ )
                spawnPlanet.Mapgen_SeedEntity( Context, faction, enclaveData, PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );

            return spawnPlanet;
        }
    }

    public class RoamingEnclaveHostileTeam : RoamingEnclaveNPC
    {
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( !EnclavesGloballyEnabled )
                return;

            if ( !EnclaveSettings.GetIsEnabled( faction ) )
                return;

            enemyThisFactionToAll( faction );

            FInt pseudoAIP = CalculateFactionOwnership( faction );

            HandleAIResponse( pseudoAIP, faction, Context );

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }

        public override Planet GetPlanetForBulkSpawn( Faction faction, ArcenSimContext Context )
        {
            List<Planet> potentialPlanets = new List<Planet>();
            byte bestMark = 7;
            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                if ( planet.MarkLevelForAIOnly.Ordinal > bestMark )
                    return DelReturn.Continue;

                bool isValid = true;
                World_AIW2.Instance.DoForEntities( EntityRollupType.KingUnitsOnly, ( GameEntity_Squad king ) =>
                {
                    if ( planet.GetHopsTo( king.Planet ) < 3 )
                        isValid = false;

                    return DelReturn.Continue;
                } );

                if ( isValid && BadgerFactionUtilityMethods.GetHopsToPlayerPlanet( planet, Context ) > 1 )
                {
                    if ( planet.MarkLevelForAIOnly.Ordinal < bestMark )
                    {
                        potentialPlanets = new List<Planet>();
                        bestMark = planet.MarkLevelForAIOnly.Ordinal;
                    }
                    potentialPlanets.Add( planet );
                }
                return DelReturn.Continue;
            } );

            if ( potentialPlanets.Count == 0 )
                return null;

            return potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];
        }

        private FInt CalculateFactionOwnership( Faction faction )
        {
            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                if ( planet.UnderInfluenceOfFactionIndex == faction.FactionIndex )
                    planet.UnderInfluenceOfFactionIndex = -1;

                return DelReturn.Continue;
            } );

            FInt pseudoAIP = FInt.Zero + (Hives.Count + Enclaves.Count) * 5;

            for ( int x = 0; x < HivePlanets.Count; x++ )
            {
                Planet planet = HivePlanets[x];
                if ( planet.UnderInfluenceOfFactionIndex == -1 )
                    planet.UnderInfluenceOfFactionIndex = faction.FactionIndex;

                pseudoAIP += 20;
            }

            return pseudoAIP;
        }

        private void HandleAIResponse( FInt pseudoAIP, Faction faction, ArcenSimContext Context )
        {
            Faction aiFaction = null;

            World_AIW2.Instance.DoForFactions( otherFaction =>
            {
                if ( otherFaction.Type != FactionType.AI )
                    return DelReturn.Continue;

                if ( aiFaction == null )
                    aiFaction = otherFaction;
                else if ( otherFaction.GetSentinelsExternal().AIDifficulty.Difficulty > aiFaction.GetSentinelsExternal().AIDifficulty.Difficulty )
                    aiFaction = otherFaction;

                return DelReturn.Continue;
            } );

            FInt pseudoIncreaseFromAI = pseudoAIP / 20;
            pseudoIncreaseFromAI *= ExternalConstants.Instance.Balance_AIPurchaseCostPerCap_AI_Income;
            pseudoIncreaseFromAI *= aiFaction.GetSentinelsExternal().AIDifficulty.AIPurchaseCostIncomeMultiplier;
            pseudoIncreaseFromAI /= ExternalConstants.Instance.Balance_PlanetsWorthOfAIPTimesSecondsRequiredToAccumulateOneCap;
            pseudoIncreaseFromAI *= aiFaction.GetSentinelsExternal().AIType.MultiplierForBudget_Wave;

            AntiMinorFactionWaveData waveData = faction.GetAntiMinorFactionWaveDataExt();
            waveData.currentWaveBudget += pseudoIncreaseFromAI;
            if ( World_AIW2.Instance.GameSecond < 10 )
                waveData.timeForNextWave = 600;
            if ( waveData.timeForNextWave > 0 )
                waveData.timeForNextWave--;
            else
            {
                waveData.currentWaveBudget -= AntiMinorFactionWaveData.QueueWave( faction, Context, waveData.currentWaveBudget.GetNearestIntPreferringHigher() );
                waveData.timeForNextWave = 600;
            }
        }
    }

    public class RoamingEnclaveRedTeam : RoamingEnclaveNPC
    {
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            faction.Ex_MinorFactionCommon_GetPrimitives().Allegiance = "Minor Faction Team Red";
            allyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Red" );

            if ( !EnclavesGloballyEnabled )
                return;

            if ( !EnclaveSettings.GetIsEnabled( faction ) )
                return;

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }
        public override Planet GetPlanetForBulkSpawn( Faction faction, ArcenSimContext Context )
        {
            // Spawn in a bunch of hives and enclaves based on intensity.
            int toSpawn = Intensity;

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
                return null;

            return potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];
        }
    }

    public class RoamingEnclaveBlueTeam : RoamingEnclaveNPC
    {
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            faction.Ex_MinorFactionCommon_GetPrimitives().Allegiance = "Minor Faction Team Blue";
            allyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Blue" );

            if ( !EnclavesGloballyEnabled )
                return;

            if ( !EnclaveSettings.GetIsEnabled( faction ) )
                return;

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }
        public override Planet GetPlanetForBulkSpawn( Faction faction, ArcenSimContext Context )
        {
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
                return null;

            return potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];
        }
    }

    public class RoamingEnclaveGreenTeam : RoamingEnclaveNPC
    {
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            faction.Ex_MinorFactionCommon_GetPrimitives().Allegiance = "Minor Faction Team Green";
            allyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Green" );

            if ( !EnclavesGloballyEnabled )
                return;

            if ( !EnclaveSettings.GetIsEnabled( faction ) )
                return;

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }
        public override Planet GetPlanetForBulkSpawn( Faction faction, ArcenSimContext Context )
        {
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
                return null;

            return potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];
        }
    }

    public class RoamingEnclaveDarkTeam : RoamingEnclaveNPC
    {
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            faction.Ex_MinorFactionCommon_GetPrimitives().Allegiance = "Dark Alliance";
            allyThisFactionToMinorFactionTeam( faction, "Dark Alliance" );

            if ( !EnclavesGloballyEnabled )
                return;

            if ( !EnclaveSettings.GetIsEnabled( faction ) )
                return;

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }
        public override Planet GetPlanetForBulkSpawn( Faction faction, ArcenSimContext Context )
        {
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
                return null;

            return potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];
        }
    }

    public class RoamingEnclaveAITeam : RoamingEnclaveNPC
    {
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            allyThisFactionToAI( faction );

            if ( !EnclavesGloballyEnabled )
                return;

            if ( !EnclaveSettings.GetIsEnabled( faction ) )
                return;

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }
        public override Planet GetPlanetForBulkSpawn( Faction faction, ArcenSimContext Context )
        {
            // Spawn in a single hive to start with.
            return BadgerFactionUtilityMethods.findAIKing();
        }
    }

    public class RoamingEnclavePlayerTeam : BaseRoamingEnclave
    {
        public List<GameEntity_Squad> PlayerEnclaves = new List<GameEntity_Squad>();

        private bool Initialized;

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            allyThisFactionToHumans( faction );

            if ( !EnclavesGloballyEnabled )
                return;

            if ( !EnclaveSettings.GetIsEnabled( faction ) )
                return;

            if ( !Initialized )
            {
                World_AIW2.Instance.DoForEntities( PLAYER_ENCLAVE_TAG, playerEnclave =>
                {
                    World_AIW2.Instance.DoForEntities( YOUNGLING_TAG, youngling =>
                    {
                        if ( youngling.MinorFactionStackingID == playerEnclave.PrimaryKeyID )
                            youngling.Despawn( Context, true, InstancedRendererDeactivationReason.IFinishedMyJob );

                        return DelReturn.Continue;
                    } );
                    playerEnclave.Despawn( Context, true, InstancedRendererDeactivationReason.IFinishedMyJob );
                    return DelReturn.Continue;
                } );
                Initialized = true;
            }

            if ( PlayerEnclaves != null && PlayerEnclaves.Count > 0 )
                for ( int x = 0; x < PlayerEnclaves.Count; x++ )
                    PlayerEnclaves[x].CombineYounglingsIfAble( Context );

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }

        public override Planet GetPlanetForBulkSpawn( Faction faction, ArcenSimContext Context )
        {
            return BadgerFactionUtilityMethods.findHumanKing();
        }

        public override Planet BulkSpawn( Faction faction, ArcenSimContext Context )
        {
            Planet spawnPlanet = GetPlanetForBulkSpawn( faction, Context );

            if ( spawnPlanet == null )
                return null;

            for ( int x = 0; x < EnclavesToSpawn; x++ )
            {
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
                if ( x < 3 )
                    spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, YOUNGLING_HIVE_TAG ), PlanetSeedingZone.OuterSystem );
            }

            return spawnPlanet;
        }

        public override void HandleEnclaveSpawning( Faction faction, ArcenSimContext Context )
        {
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

            for ( int x = 0; x < EnclavesToSpawn; x++ )
            {
                Planet spawnPlanet = validPlanets[Context.RandomToUse.Next( validPlanets.Count )];

                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
            }
        }

        public override void HandleHiveExpansion( Faction faction, ArcenSimContext Context )
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
                            {
                                int hivesOnPlanet = 0;
                                for ( int x = 0; x < Hives.Count; x++ )
                                    if ( Hives[x].Planet == planet )
                                        hivesOnPlanet++;
                                if ( hivesOnPlanet < Intensity )
                                    forceSpawn.Add( planet );
                            }
                        }

                        return DelReturn.Continue;
                    } );
                }
                return DelReturn.Continue;
            } );

            if ( validPlanets.Count > 0 )
            {
                Planet spawnPlanet = validPlanets[Context.RandomToUse.Next( validPlanets.Count )];
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, YOUNGLING_HIVE_TAG ), PlanetSeedingZone.OuterSystem );
            }

            for ( int x = 0; x < forceSpawn.Count; x++ )
            {
                forceSpawn[x].Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, YOUNGLING_HIVE_TAG ), PlanetSeedingZone.OuterSystem );
            }

            if ( faction.HasObtainedSpireDebris )
                World_AIW2.Instance.DoForPlanets( false, planet =>
                {
                    int clanlingHives = 0, younglingHives = 0;
                    planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( HIVE_TAG, ( GameEntity_Squad entity ) =>
                    {
                        if ( entity.TypeData.GetHasTag( YOUNGLING_HIVE_TAG ) )
                            younglingHives++;
                        else
                            clanlingHives++;

                        return DelReturn.Continue;
                    } );

                    for ( int x = younglingHives; x >= 3; x -= 3 )
                        if ( clanlingHives > 0 )
                        {
                            clanlingHives--;
                            continue;
                        }
                        else
                        {
                            planet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, CLANLING_HIVE_TAG ), PlanetSeedingZone.OuterSystem );
                        }

                    return DelReturn.Continue;
                } );
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( !EnclavesGloballyEnabled )
                return;

            if ( !EnclaveSettings.GetIsEnabled( faction ) )
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
                        {
                            Fleet.Membership mem = entity.FleetMembership.Fleet.GetButDoNotAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( GameEntityTypeDataTable.Instance.GetRowByName( HUMAN_HIVE_NAME ) );
                            if ( mem == null || mem.ExplicitBaseSquadCap < 1 )
                                addHiveToBuildListsCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                        }

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
