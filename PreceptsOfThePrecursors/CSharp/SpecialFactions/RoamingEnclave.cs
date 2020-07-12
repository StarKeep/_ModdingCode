using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using System;
using System.Collections.Generic;

namespace PreceptsOfThePrecursors
{
    public static class EnclaveSettings
    {
        public static int GetInt( Faction faction, Integers setting )
        {
            bool useCustom = AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( faction.SpecialFactionData.InternalName.Substring( 14 ) + Booleans.EnclaveSettingsEnabled.ToString() );
            if ( useCustom )
                return AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( faction.SpecialFactionData.InternalName.Substring( 14 ) + setting.ToString() );
            else
                return AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( setting.ToString() );
        }
        public static bool GetIsEnabled( Faction faction )
        {
            return AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( faction.SpecialFactionData.InternalName.Substring( 14 ) + Booleans.EnclaveEnabled );
        }
        public enum Booleans
        {
            EnclaveEnabled,
            EnclaveSettingsEnabled
        }
        public enum Integers
        {
            EnclaveMaxHopsFromHiveToAttack,
            MakeEveryXFireteamDefensive,
            RetreatAtXHull
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
        public static string PLAYER_ENCLAVE_TAG = "PlayerRoamingEnclave";
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
            AddPlayerRoamingYounglingToBuildList,
            ActivatePlayerRoamingYoungling,
            PopulatePlayerEnclavesList
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
            if ( Hives.Count > 50 )
                faction.OverallPowerLevel = FInt.FromParts( 2, 000 );
            else if ( Hives.Count > 10 )
                faction.OverallPowerLevel = FInt.FromParts( 1, 000 ) + (FInt.FromParts( 0, 025 ) * (Hives.Count - 10));
            else
                faction.OverallPowerLevel = FInt.FromParts( 0, 010 ) * Hives.Count;
        }
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( !EnclavesGloballyEnabled )
                return;

            if ( FactionData == null )
                FactionData = faction.GetEnclaveFactionData();

            HandleEnclaveRegeneration();

            HandleYounglingCombining( Context );

            HandleUnitSpawningForEnclaves( Context );

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
                for(int x = 0; x < (this as RoamingEnclavePlayerTeam).PlayerEnclaves.Count; x++ )
                    if ( (this as RoamingEnclavePlayerTeam).PlayerEnclaves[x].RepairDelaySeconds <= 0 )
                        (this as RoamingEnclavePlayerTeam).PlayerEnclaves[x].TakeHullRepair( Enclaves[x].GetMaxHullPoints() / 100 );
        }

        private void HandleYounglingCombining( ArcenSimContext Context )
        {
            for ( int x = 0; x < Enclaves.Count; x++ )
                Enclaves[x].CombineYounglingsIfAble( Context );
        }

        private void HandleUnitSpawningForEnclaves( ArcenSimContext Context )
        {
            if ( Enclaves.Count > 0 && CanSpawnUnits( null, out GameEntityTypeData unitData, 150 ) )
                SpawnUnitsForEnclave( Enclaves, unitData, Context );
        }

        private void HandleUnitSpawningForHives( ArcenSimContext Context )
        {
            for ( int y = 0; y < Hives.Count; y++ )
                if ( CanSpawnUnits( Hives[y], out GameEntityTypeData unitData, 250 ) )
                    SpawnUnitsForHive( Hives[y], unitData, Context );
        }

        private bool CanSpawnUnits( GameEntity_Squad hiveOrNull, out GameEntityTypeData unitData, int fallbackCost )
        {
            unitData = GameEntityTypeDataTable.Instance.GetRowByName( hiveOrNull != null ? hiveOrNull.TypeData.InternalName.Substring( 4 ) : Unit.YounglingWorm.ToString() );
            if ( unitData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Failed to find unit data for " + hiveOrNull.TypeData.DisplayName, Verbosity.ShowAsError );
                return false;
            }
            int cost = unitData.MetalCost;
            if ( cost == 0 )
                cost = fallbackCost;
            return World_AIW2.Instance.GameSecond % (unitData.MetalCost / (20 + (Intensity * 3))) == 0;
        }

        private void SpawnUnitsForHive( GameEntity_Squad hive, GameEntityTypeData unitData, ArcenSimContext Context )
        {
            GameEntity_Squad unit = GameEntity_Squad.CreateNew( hive.PlanetFaction, unitData, hive.CurrentMarkLevel, hive.PlanetFaction.FleetUsedAtPlanet, 0, hive.WorldLocation, Context );
            unit.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, hive.PlanetFaction.Faction.FactionIndex );
            unit.MinorFactionStackingID = -1;
            if ( this is RoamingEnclavePlayerTeam )
                (this as RoamingEnclavePlayerTeam).SpawnYounglingsForPlayerEnclaves( hive, unitData, Context );
        }

        private void SpawnUnitsForEnclave( List<GameEntity_Squad> enclaves, GameEntityTypeData unitData, ArcenSimContext Context )
        {
            for ( int x = 0; x < enclaves.Count; x++ )
            {
                GameEntity_Squad enclave = enclaves[x];
                GameEntity_Squad unit = GameEntity_Squad.CreateNew( enclave.PlanetFaction, unitData, enclave.CurrentMarkLevel, enclave.PlanetFaction.FleetUsedAtPlanet, 0, enclave.WorldLocation, Context );
                enclave.StoreYoungling( unit, Context );
            }
            if ( this is RoamingEnclavePlayerTeam )
                (this as RoamingEnclavePlayerTeam).SpawnYounglingsForPlayerEnclaves( null, unitData, Context );
        }

        public abstract Planet BulkSpawn( Faction faction, Galaxy galaxy, ArcenSimContext Context );

        public abstract void HandleEnclaveSpawning( Faction faction, ArcenSimContext Context );

        public abstract void HandleHiveExpansion( Faction faction, ArcenSimContext Context );

        private int AttackHops;
        private int FireteamsPerDefense;
        private int RetreatPercentage;

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( !EnclavesGloballyEnabled )
                return;

            AttackHops = EnclaveSettings.GetInt( faction, EnclaveSettings.Integers.EnclaveMaxHopsFromHiveToAttack );
            FireteamsPerDefense = EnclaveSettings.GetInt( faction, EnclaveSettings.Integers.MakeEveryXFireteamDefensive );
            RetreatPercentage = EnclaveSettings.GetInt( faction, EnclaveSettings.Integers.RetreatAtXHull );

            FactionData.TeamsAimedAtPlanet.Clear();

            Fireteam.DoFor( FactionData.Teams, delegate ( Fireteam team )
            {
                team.Reset();
                return DelReturn.Continue;
            } );

            SetupHives( faction, Context );
            SetupEnclaves( faction, Context );
            SetupYounglings( faction, Context );

            Fireteam.DoFor( FactionData.Teams, delegate ( Fireteam team )
            {
                if ( team.ships.Count == 0 )
                    team.Disband( Context );
                return DelReturn.Continue;
            } );
            FireteamUtility.CleanUpDisbandedFireteams( FactionData.Teams );

            ArcenCharacterBuffer buffer = this.tracingBuffer_longTerm;
            FireteamUtility.UpdateFireteams( faction, Context, FactionData.Teams, FactionData.TeamsAimedAtPlanet, buffer, FInt.One );
            FireteamUtility.UpdateRegiments( faction, Context, FactionData.Teams, FactionData.TeamsAimedAtPlanet, buffer, 1, true );

            Fireteam.DoFor( FactionData.Teams, delegate ( Fireteam team )
            {
                if ( team.DefenseMode && team.TargetPlanet != null && team.TargetPlanet.GetHopsTo( GetNearestHivePlanetBackgroundThreadOnly( faction, team.TargetPlanet, Context ) ) > 0 )
                {
                    team.DiscardCurrentObjectives();
                    return DelReturn.Continue;
                }

                switch ( team.status )
                {
                    case FireteamStatus.Disbanded:
                    case FireteamStatus.Assembling:
                    case FireteamStatus.Staging:
                    case FireteamStatus.ReadyToAttack:
                        if ( team.History.Count > 0 )
                        {
                            int latestSecond = 0;
                            for ( int x = 0; x < team.History.Count; x++ )
                                latestSecond = Math.Max( latestSecond, team.History[x].GameSecond );
                            if ( World_AIW2.Instance.GameSecond - latestSecond > 60 )
                                team.DiscardCurrentObjectives();
                        }

                        break;
                    default:
                        break;
                }

                return DelReturn.Continue;
            } );

            faction.ExecuteMovementCommands( Context );
            faction.ExecuteWormholeCommands( Context );
        }

        private void SetupEnclaves( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand markUpCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.MarkUpUnits.ToString() ), GameCommandSource.AnythingElse, faction );
            GameCommand enclavePopulateCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.PopulateEnclavesList.ToString() ), GameCommandSource.AnythingElse, faction );
            GameCommand enclavePlanetsPopulateCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.PopulateEnclavePlanetList.ToString() ), GameCommandSource.AnythingElse, faction );
            GameCommand enclaveUnloadCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.UnloadYounglingsFromEnclaves.ToString() ), GameCommandSource.AnythingElse, faction );

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
                    if ( enclave.Planet.GetHopsTo( GetNearestHivePlanetBackgroundThreadOnly( faction, enclave.Planet, Context ) ) > 0 )
                        enclave.QueueWormholeCommand( GetNearestHivePlanetBackgroundThreadOnly( faction, enclave.Planet, Context ) );
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
                                Fireteam team = new Fireteam();
                                team.MyStrengthMultiplierForStrengthCalculation = FInt.One;
                                team.EnemyStrengthMultiplierForStrengthCalculation = FInt.One;
                                team.id = FireteamUtility.GetNextFireteamId( FactionData.Teams );
                                if ( FireteamsPerDefense > 0 )
                                    team.DefenseMode = team.id % FireteamsPerDefense == 0;
                                else
                                    team.DefenseMode = false;
                                team.StrengthToBringOnline = 0;
                                team.AddUnit( enclave );
                                FactionData.Teams.AddIfNotAlreadyIn( team );
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
                        {
                            team.AddUnit( enclave );
                            switch ( team.status )
                            {
                                case FireteamStatus.Assembling:
                                case FireteamStatus.Staging:
                                case FireteamStatus.ReadyToAttack:
                                    if ( (hostileStrength < 500 || alliedStrength < hostileStrength * 2) && enclave.LongRangePlanningData.FinalDestinationPlanetIndex == -1
                                    && enclave.Planet.GetHopsTo( GetNearestHivePlanetBackgroundThreadOnly( faction, enclave.Planet, Context ) ) > 0 )
                                        enclave.QueueWormholeCommand( GetNearestHivePlanetBackgroundThreadOnly( faction, enclave.Planet, Context ) );
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                            enclave.FireteamId = -1; //something happened to the fireteam, so lets find a new one next LRP stage
                    }
                }

                if ( hostileStrength > 0 )
                    enclaveUnloadCommand.RelatedEntityIDs.Add( enclave.PrimaryKeyID );

                return DelReturn.Continue;
            } );

            if ( markUpCommand.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( markUpCommand );
            Context.QueueCommandForSendingAtEndOfContext( enclavePopulateCommand );
            Context.QueueCommandForSendingAtEndOfContext( enclavePlanetsPopulateCommand );
            if ( enclaveUnloadCommand.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( enclaveUnloadCommand );
        }

        private Planet GetNearestHivePlanetBackgroundThreadOnly( Faction faction, Planet planet, ArcenLongTermIntermittentPlanningContext Context )
        {
            Planet bestPlanet = null;
            for ( int x = 0; x < HivePlanetsForBackgroundThreadOnly.Count; x++ )
            {
                Planet hivePlanet = HivePlanetsForBackgroundThreadOnly[x];
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

        int alliedAssaultFriendlyThreshold = 5000;
        int alliedAssaultHostileThresholdMult = 2;
        int maxStrengthToBreakThrough = 10000;
        int hivesInDangerThreshold = 2500;
        int hivesThreatenedThreshold = 5000;

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
            PreferredTargets.Clear();
            FallbackTargets.Clear();

            List<Planet> hivesInDanger = new List<Planet>();
            List<Planet> alliedAssaults = new List<Planet>();
            List<Planet> hivesThreatened = new List<Planet>();
            List<GameEntity_Squad> bigTargets = new List<GameEntity_Squad>();
            List<Planet> fallbackAttackPlanets = new List<Planet>();
            ArcenSparseLookup<int, List<Planet>> planetsByEnclaveCount = new ArcenSparseLookup<int, List<Planet>>();

            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                int hops = planet.GetHopsTo( GetNearestHivePlanetBackgroundThreadOnly( faction, planet, Context ) );

                int hostileStrength = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Hostile].TotalStrength;
                int friendlyStrength = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Friendly].TotalStrength;

                if ( this is RoamingEnclavePlayerTeam && friendlyStrength > alliedAssaultFriendlyThreshold
                  && hostileStrength * alliedAssaultHostileThresholdMult > friendlyStrength )
                {
                    alliedAssaults.Add( planet );
                }
                else if ( hops == 0 )
                {
                    if ( hostileStrength > hivesInDangerThreshold )
                    {
                        hivesInDanger.Add( planet );
                    }

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
                else if ( hops == 1 )
                {
                    if ( hostileStrength > hivesThreatenedThreshold )
                    {
                        hivesThreatened.Add( planet );
                    }
                }
                else if ( hops <= AttackHops )
                {
                    if ( !(this is RoamingEnclaveAITeam) )
                    {
                        planet.DoForEntities( "ExtragalacticWar", entity =>
                        {
                            bigTargets.Add( entity );

                            return DelReturn.Continue;
                        } );
                        planet.DoForEntities( SpecialEntityType.AIDireGuardian, entity =>
                        {
                            bigTargets.Add( entity );

                            return DelReturn.Continue;
                        } );
                    }
                }

                if ( hostileStrength > 500 )
                {
                    if ( hops <= AttackHops )
                        fallbackAttackPlanets.Add( planet );
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
                else if ( planetsByEnclaveCount.GetPairCount() > 0 )
                {
                    for ( int x = 0; x < planetsByEnclaveCount.GetPairByIndex( 0 ).Value.Count; x++ )
                        PreferredTargets.Add( new FireteamTarget( planetsByEnclaveCount.GetPairByIndex( 0 ).Value[x] ) );
                }
                for ( int x = 0; x < HivePlanetsForBackgroundThreadOnly.Count; x++ )
                    FallbackTargets.Add( new FireteamTarget( HivePlanetsForBackgroundThreadOnly[x] ) );
            }
            else
            {
                if ( hivesInDanger.Count > 0 )
                    for ( int x = 0; x < hivesInDanger.Count; x++ )
                        PreferredTargets.Add( new FireteamTarget( hivesInDanger[x] ) );
                else
                {
                    if ( alliedAssaults.Count > 0 )
                        for ( int x = 0; x < alliedAssaults.Count; x++ )
                            PreferredTargets.Add( new FireteamTarget( alliedAssaults[x] ) );
                    if ( hivesThreatened.Count > 0 )
                        for ( int x = 0; x < hivesThreatened.Count; x++ )
                            PreferredTargets.Add( new FireteamTarget( hivesThreatened[x] ) );
                }
                if ( bigTargets.Count > 0 )
                    for ( int x = 0; x < bigTargets.Count; x++ )
                        FallbackTargets.Add( new FireteamTarget( bigTargets[x] ) );
                else if ( fallbackAttackPlanets.Count > 0 )
                    for ( int x = 0; x < fallbackAttackPlanets.Count; x++ )
                        FallbackTargets.Add( new FireteamTarget( fallbackAttackPlanets[x] ) );
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
                worldData.SecondsUntilNextInflux = 900;
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
                    if ( REFaction.Hives.Count == 0 && REFaction.Enclaves.Count == 0 )
                    {
                        if ( REFaction.FactionData.SecondsUntilNextRespawn == -1 )
                            REFaction.FactionData.SecondsUntilNextRespawn = 1800;
                        if ( REFaction.FactionData.SecondsUntilNextRespawn > 0 )
                            REFaction.FactionData.SecondsUntilNextRespawn--;
                        if ( REFaction.FactionData.SecondsUntilNextRespawn == 0 )
                        {
                            Planet spawnPlanet = REFaction.BulkSpawn( otherFaction, World_AIW2.Instance.CurrentGalaxy, Context );
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

        public override void HandleHiveExpansion( Faction faction, ArcenSimContext Context )
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

    public class RoamingEnclaveHostileTeam : RoamingEnclaveNPC
    {
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            enemyThisFactionToAll( faction );

            FInt pseudoAIP = CalculateFactionOwnership( faction );

            HandleAIResponse( pseudoAIP, faction, Context );

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }

        public override Planet BulkSpawn( Faction faction, Galaxy galaxy, ArcenSimContext Context )
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
                return null;

            Planet spawnPlanet = potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];

            for ( int x = 0; x < toSpawn; x++ )
            {
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
            }

            return spawnPlanet;
        }

        private FInt CalculateFactionOwnership( Faction faction )
        {
            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                if ( planet.UnderInfluenceOfFactionIndex == faction.FactionIndex )
                    planet.UnderInfluenceOfFactionIndex = -1;

                return DelReturn.Continue;
            } );

            FInt pseudoAIP = FInt.Zero + Hives.Count * 5;

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
                AntiMinorFactionWaveData.QueueWave( faction, Context, waveData.currentWaveBudget.GetNearestIntPreferringHigher(), true );
                waveData.timeForNextWave = 600;
                waveData.currentWaveBudget = FInt.Zero;
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
        public override Planet BulkSpawn( Faction faction, Galaxy galaxy, ArcenSimContext Context )
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
                return null;

            Planet spawnPlanet = potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];

            for ( int x = 0; x < toSpawn; x++ )
            {
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
            }

            return spawnPlanet;
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
        public override Planet BulkSpawn( Faction faction, Galaxy galaxy, ArcenSimContext Context )
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
                return null;

            Planet spawnPlanet = potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];

            for ( int x = 0; x < toSpawn; x++ )
            {
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
            }

            return spawnPlanet;
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
        public override Planet BulkSpawn( Faction faction, Galaxy galaxy, ArcenSimContext Context )
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
                return null;

            Planet spawnPlanet = potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];

            for ( int x = 0; x < toSpawn; x++ )
            {
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
            }

            return spawnPlanet;
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
        public override Planet BulkSpawn( Faction faction, Galaxy galaxy, ArcenSimContext Context )
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
                return null;

            Planet spawnPlanet = potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];

            for ( int x = 0; x < toSpawn; x++ )
            {
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
            }

            return spawnPlanet;
        }
    }

    public class RoamingEnclaveAITeam : RoamingEnclaveNPC
    {
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            allyThisFactionToAI( faction );

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }
        public override Planet BulkSpawn( Faction faction, Galaxy galaxy, ArcenSimContext Context )
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

            if ( spawnPlanet == null )
                return null;

            for ( int x = 0; x < toSpawn; x++ )
            {
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
            }

            return spawnPlanet;
        }
    }

    public class RoamingEnclavePlayerTeam : BaseRoamingEnclave
    {
        public List<GameEntity_Squad> PlayerEnclaves = new List<GameEntity_Squad>();

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( !EnclavesGloballyEnabled )
                return;

            if ( !EnclaveSettings.GetIsEnabled( faction ) )
                return;

            allyThisFactionToHumans( faction );

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }

        public void SpawnYounglingsForPlayerEnclaves( GameEntity_Squad hive, GameEntityTypeData unitData, ArcenSimContext Context )
        {
            Faction spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( RoamingEnclavePlayerTeam ) );
            for ( int x = 0; x < PlayerEnclaves.Count; x++ )
            {
                GameEntity_Squad enclave = PlayerEnclaves[x];
                PlanetFaction pFaction = hive != null ? hive.PlanetFaction : enclave.Planet.GetPlanetFactionForFaction( spawnFaction );
                GameEntity_Squad unit = GameEntity_Squad.CreateNew( pFaction, unitData, enclave.CurrentMarkLevel, pFaction.FleetUsedAtPlanet, 0, hive != null ? hive.WorldLocation : enclave.WorldLocation, Context );
                if ( hive == null )
                    enclave.StoreYoungling( unit, Context );
                else
                {
                    unit.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, spawnFaction.FactionIndex );
                    unit.MinorFactionStackingID = enclave.PrimaryKeyID;
                }
            }
        }

        public override Planet BulkSpawn( Faction faction, Galaxy galaxy, ArcenSimContext Context )
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
            if ( spawnPlanet == null )
                return null;
            for ( int x = 0; x < toSpawn; x++ )
            {
                spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ENCLAVE_TAG ), PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
                if ( x < 3 )
                    spawnPlanet.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, HIVE_TAG ), PlanetSeedingZone.OuterSystem );
            }
            return spawnPlanet;
        }

        public override void HandleEnclaveSpawning( Faction faction, ArcenSimContext Context )
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
            if ( !EnclavesGloballyEnabled )
                return;

            if ( !EnclaveSettings.GetIsEnabled( faction ) )
                return;

            AddAndClaimHivesFromPlayers( faction, Context );

            base.DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( faction, Context );

            HandlePlayerEnclaveSpawning( faction, Context );

            HandlePlayerYounglingLogic( faction, Context );
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

        private void HandlePlayerEnclaveSpawning( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand spawnEnclaves = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.AddPlayerRoamingYounglingToBuildList.ToString() ), GameCommandSource.AnythingElse, faction );
            GameCommand activateEnclaves = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.ActivatePlayerRoamingYoungling.ToString() ), GameCommandSource.AnythingElse, faction );
            GameCommand populatePlayerEnclaves = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.PopulatePlayerEnclavesList.ToString() ), GameCommandSource.AnythingElse, faction );

            World_AIW2.Instance.DoForFactions( otherFaction =>
            {
                if ( otherFaction.Type == FactionType.Player && faction.GetIsFriendlyTowards( otherFaction ) )
                {
                    int giftedEnclave = 0;
                    otherFaction.DoForEntities( PLAYER_ENCLAVE_TAG, ( GameEntity_Squad entity ) =>
                    {
                        giftedEnclave++;

                        populatePlayerEnclaves.RelatedEntityIDs.Add( entity.PrimaryKeyID );

                        return DelReturn.Continue;
                    } );
                    Fleet.Membership mem = otherFaction.GetFactionKing().FleetMembership.Fleet.GetButDoNotAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( GameEntityTypeDataTable.Instance.GetRowByName( "PlayerRoamingEnclaveConstructor" ) );
                    if ( mem != null )
                        giftedEnclave += mem.ExplicitBaseSquadCap;

                    int toGetAnother = 10;
                    for ( int x = 0; x < giftedEnclave; x++ )
                        toGetAnother *= 2;
                    if ( Hives.Count >= toGetAnother )
                        spawnEnclaves.RelatedEntityIDs.Add( otherFaction.GetFactionKing().PrimaryKeyID );
                    if ( mem != null )
                        mem.DoForEntities( playerEnclave =>
                        {
                            if ( playerEnclave.SelfBuildingMetalRemaining <= 0 && playerEnclave.SecondsSpentAsRemains <= 0 )
                                activateEnclaves.RelatedEntityIDs.Add( playerEnclave.PrimaryKeyID );

                            return DelReturn.Continue;
                        } );
                }

                return DelReturn.Continue;
            } );

            if ( spawnEnclaves.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( spawnEnclaves );
            if ( activateEnclaves.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( activateEnclaves );
            if ( populatePlayerEnclaves.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( populatePlayerEnclaves );
        }

        private void HandlePlayerYounglingLogic( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand loadYounglingsCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.LoadYounglingsIntoEnclaves.ToString() ), GameCommandSource.AnythingElse, faction );
            GameCommand enclaveUnloadCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.UnloadYounglingsFromEnclaves.ToString() ), GameCommandSource.AnythingElse, faction );

            World_AIW2.Instance.DoForFactions( otherFaction =>
            {
                if ( otherFaction.Type != FactionType.Player || otherFaction.GetIsHostileTowards( faction ) )
                    return DelReturn.Continue;
                otherFaction.DoForEntities( PLAYER_ENCLAVE_TAG, enclave =>
                {
                    if ( !enclave.FleetMembership.Fleet.IsFleetInTransportLoadMode )
                        enclaveUnloadCommand.RelatedEntityIDs.Add( enclave.PrimaryKeyID );

                    faction.DoForEntities( YOUNGLING_TAG, youngling =>
                    {
                        if ( youngling.MinorFactionStackingID != enclave.PrimaryKeyID )
                            return DelReturn.Continue;

                        if ( youngling.Planet == enclave.Planet )
                        {
                            if ( enclave.FleetMembership.Fleet.IsFleetInTransportLoadMode || youngling.GetCurrentHullPoints() < youngling.GetMaxHullPoints() * 0.33 )
                            {
                                loadYounglingsCommand.RelatedIntegers.Add( youngling.PrimaryKeyID );
                                loadYounglingsCommand.RelatedIntegers2.Add( enclave.PrimaryKeyID );
                            }
                            else if ( enclave.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength <= 50 && youngling.GetDistanceTo_VeryCheapButExtremelyRough( enclave.WorldLocation, false ) > 1000 )
                                youngling.QueueMovementCommand( enclave.WorldLocation );
                        }
                        else if ( youngling.LongRangePlanningData.FinalDestinationPlanetIndex == -1 )
                            youngling.QueueWormholeCommand( enclave.Planet, Context );

                        return DelReturn.Continue;
                    } );

                    return DelReturn.Continue;
                } );
                return DelReturn.Continue;
            } );

            if ( loadYounglingsCommand.RelatedIntegers.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( loadYounglingsCommand );
            if ( enclaveUnloadCommand.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( enclaveUnloadCommand );
        }
    }
}
