using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using SKCivilianIndustry.Storage;
using System;
using System.Collections.Generic;

namespace SKCivilianIndustry
{
    public class CivTeamData
    {
        public GameEntity_Squad GrandStation; // Optional.
        public List<Faction> Factions;
        public List<GameEntity_Squad> TradeEntities;
        public List<GameEntity_Squad> IndustryEntities;
        public List<GameEntity_Squad> CargoShips;
        public List<CivTradeRequest> TradeRequests;
        public ArcenSparseLookup<int, ArcenSparseLookup<string, int>> UnitsByCreator;

        public CivTeamData()
        {
            Factions = new List<Faction>();
            TradeEntities = new List<GameEntity_Squad>();
            IndustryEntities = new List<GameEntity_Squad>();
            CargoShips = new List<GameEntity_Squad>();
            TradeRequests = new List<CivTradeRequest>();
            UnitsByCreator = new ArcenSparseLookup<int, ArcenSparseLookup<string, int>>();
        }
    }
    public class CivTradeRequest : IComparable<CivTradeRequest>
    {
        public GameEntity_Squad Unit;
        public ArcenSparseLookup<CivResource, int> ResourcesOffered;
        public ArcenSparseLookup<CivResource, int> ResourcesRequested;
        public int CargoShipsAlreadyEnroute;

        public CivTradeRequest()
        {
            ResourcesOffered = new ArcenSparseLookup<CivResource, int>();
            ResourcesRequested = new ArcenSparseLookup<CivResource, int>();
        }

        public int TotalOffered
        {
            get
            {
                int total = 0;
                for ( int x = 0; x < ResourcesOffered.GetPairCount(); x++ )
                    total += ResourcesOffered.GetPairByIndex( x ).Value;
                return total;
            }
        }

        public int TotalRequested
        {
            get
            {
                int total = 0;
                for ( int x = 0; x < ResourcesRequested.GetPairCount(); x++ )
                    total += ResourcesRequested.GetPairByIndex( x ).Value;
                return total;
            }
        }

        public bool IsMatch( CivTradeRequest demandRequest )
        {
            for ( int x = 0; x < this.ResourcesOffered.GetPairCount(); x++ )
                for ( int y = 0; y < demandRequest.ResourcesRequested.GetPairCount(); y++ )
                    if ( this.ResourcesOffered.GetPairByIndex( x ).Key.Name == demandRequest.ResourcesRequested.GetPairByIndex( y ).Key.Name )
                        return true;

            return false;
        }

        public int CompareTo( CivTradeRequest other )
        {
            return this.CargoShipsAlreadyEnroute.CompareTo( other.CargoShipsAlreadyEnroute );
        }

        public override string ToString()
        {
            string output = $"Trade Request for {Unit.TypeData.DisplayName}#{Unit.PrimaryKeyID}";
            output += $"\nCargo Ships enroute: {CargoShipsAlreadyEnroute}";
            output += "\nResources Offered:";
            for ( int x = 0; x < ResourcesOffered.GetPairCount(); x++ )
                output += $"\n{ResourcesOffered.GetPairByIndex( x ).Key.Name}, {ResourcesOffered.GetPairByIndex( x ).Value}";
            output += "\nResources Requested:";
            for ( int x = 0; x < ResourcesRequested.GetPairCount(); x++ )
                output += $"\n{ResourcesRequested.GetPairByIndex( x ).Key.Name}, {ResourcesRequested.GetPairByIndex( x ).Value}";
            return output;
        }
    }

    // The main faction class.
    public class SKTradeLogicFaction : BaseSpecialFaction
    {
        protected override string TracingName => "SKTradeLogicFaction";

        protected override bool EverNeedsToRunLongRangePlanning => true;

        private List<CivTeamData> CivilianTeamsForBackgroundThreadOnly;

        // Sim friendly unit lists.
        public static CivWorldData WorldData;

        // Keep track of loaded resources.
        public static CivResourceData Resources;

        public enum Commands
        {
            ExecuteTradeOrder,
            PopulateCargoShipList,
            PopulateTradeEntitiesList,
            PopulateIndustryEntitiesList,
            PopulateCargoShipsToBuildList,
            AddUnitToCivFleet,
            InitializeCargo,
            InitializeIndustry
        }

        public SKTradeLogicFaction()
        {
            CivilianTeamsForBackgroundThreadOnly = null;
        }

        // A BUNCH of tags. My goodness.
        public static readonly string GRAND_SHIPYARD_TAG = "CivGrandShipyard";
        public static readonly string GRAND_SHIPYARD_SPAWNING_TAG = "CivGrandShipyardSpawns";
        public static readonly string CARGO_SHIP_TAG = "CivCargoShip";

        public static readonly string TRADER_TAG = "CivTrade";
        public static readonly string RANDOM_TAG = "CivRandom";
        public static readonly string PRODUCTION_TAG = "CivPro";
        public static readonly string TRADE_STORAGE_TAG = "CivStorage";
        public static readonly string IMPORT_TAG = "CivImport";

        public static readonly string INDUSTRY_TAG = "CivIndustry";
        public static readonly string BUILD_TAG = "CivBuild";
        public static readonly string BUILD_FOR_OWNER_TAG = "CivBuildForOwner";

        public override void DoPerSecondLogic_Stage1Clearing_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( Resources == null )
                InitializeResources();
        }

        private void InitializeResources()
        {
            Resources = new CivResourceData();
            for ( int x = 0; x < GameEntityTypeDataTable.Instance.RowsByTag.GetPairCount(); x++ )
            {
                ArcenSparseLookupPair<string, List<GameEntityTypeData>> pair = GameEntityTypeDataTable.Instance.RowsByTag.GetPairByIndex( x );

                if ( pair.Key == TRADER_TAG )
                {
                    for ( int y = 0; y < pair.Value.Count; y++ )
                    {
                        GameEntityTypeData workingData = pair.Value[y];
                        for ( int t = 0; t < workingData.TagsList.Count; t++ )
                        {
                            string name = string.Empty;
                            string tag = workingData.TagsList[t];
                            if ( tag.Contains( PRODUCTION_TAG ) )
                                if ( tag.Length > PRODUCTION_TAG.Length + 2 )
                                    name = tag.Substring( PRODUCTION_TAG.Length + 2 );
                                else
                                    ArcenDebugging.ArcenDebugLogSingleLine( $"Error. {PRODUCTION_TAG} tag length expected at least 9, got {PRODUCTION_TAG.Length}.", Verbosity.ShowAsError );
                            else
                                continue;

                            int amount;

                            if ( !int.TryParse( tag.Substring( PRODUCTION_TAG.Length, 2 ), out amount ) )
                            {
                                ArcenDebugging.ArcenDebugLogSingleLine( $"Error. Expected two digits after tag {PRODUCTION_TAG} on {workingData.InternalName}. {tag.Substring( PRODUCTION_TAG.Length, 2 )}", Verbosity.ShowAsError );
                                continue;
                            }

                            if ( name != string.Empty && Resources.GetResourceByName( name ) == null )
                            {
                                Resources.Resources.Add( new CivResource( name ) );
                                ArcenDebugging.SingleLineQuickDebug( $"Adding Civilian Resource named {name}." );
                            }
                        }
                    }
                }
            }
        }

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            UpdateWorldData();
        }

        private void UpdateWorldData()
        {
            if ( WorldData == null )
                WorldData = World.Instance.GetCivWorldData();
            UpdateCargoShips();
            UpdateTradeEntities();
            UpdateIndustryEntities();
            UpdateCargoShipsToBuild();
            UpdateCivFleets();
        }

        private void UpdateCargoShips()
        {
            WorldData.CargoShips = new List<GameEntity_Squad>();
            for ( int x = 0; x < WorldData.CargoShipsRaw.Count; x++ )
            {
                GameEntity_Squad cargoShip = World_AIW2.Instance.GetEntityByID_Squad( WorldData.CargoShipsRaw[x] );
                if ( cargoShip == null )
                {
                    WorldData.CargoShipsRaw.RemoveAt( x );
                    x--;
                    continue;
                }
                WorldData.CargoShips.Add( cargoShip );
            }
        }

        private void UpdateTradeEntities()
        {
            WorldData.TradeEntities = new List<GameEntity_Squad>();
            for ( int x = 0; x < WorldData.TradeEntitiesRaw.Count; x++ )
            {
                GameEntity_Squad cargoShip = World_AIW2.Instance.GetEntityByID_Squad( WorldData.TradeEntitiesRaw[x] );
                if ( cargoShip == null )
                {
                    WorldData.TradeEntitiesRaw.RemoveAt( x );
                    x--;
                    continue;
                }
                WorldData.TradeEntities.Add( cargoShip );
            }
        }

        private void UpdateIndustryEntities()
        {
            WorldData.IndustryEntities = new List<GameEntity_Squad>();
            for ( int x = 0; x < WorldData.IndustryEntitiesRaw.Count; x++ )
            {
                GameEntity_Squad cargoShip = World_AIW2.Instance.GetEntityByID_Squad( WorldData.IndustryEntitiesRaw[x] );
                if ( cargoShip == null )
                {
                    WorldData.IndustryEntitiesRaw.RemoveAt( x );
                    x--;
                    continue;
                }
                WorldData.IndustryEntities.Add( cargoShip );
            }
        }

        private void UpdateCargoShipsToBuild()
        {
            WorldData.CargoShipsToBuild = new ArcenSparseLookup<GameEntity_Squad, int>();
            for ( int x = 0; x < WorldData.CargoShipsToBuildRaw.GetPairCount(); x++ )
            {
                int key = WorldData.CargoShipsToBuildRaw.GetPairByIndex( x ).Key;
                GameEntity_Squad shipyard = World_AIW2.Instance.GetEntityByID_Squad( key );
                if ( shipyard == null )
                {
                    WorldData.CargoShipsToBuildRaw.RemovePairByKey( key );
                    x--;
                    continue;
                }
                WorldData.CargoShipsToBuild.AddPair( shipyard, WorldData.CargoShipsToBuildRaw[key] );
            }
        }

        private void UpdateCivFleets()
        {
            WorldData.CivFleets = new ArcenSparseLookup<GameEntity_Squad, CivFleet>();
            for ( int x = 0; x < WorldData.CivFleetsRaw.GetPairCount(); x++ )
            {
                int rawOwner = WorldData.CivFleetsRaw.GetPairByIndex( x ).Key;
                GameEntity_Squad owner = World_AIW2.Instance.GetEntityByID_Squad( rawOwner );
                CivFleet civFleet = WorldData.CivFleetsRaw[rawOwner];
                if ( owner == null )
                {
                    DecayOwnerlessUnits( civFleet );
                    WorldData.CivFleetsRaw.RemovePairByKey( rawOwner );
                    x--;
                    continue;
                }

                for(int y = 0; y < civFleet.UnitsByInternalNameRaw.GetPairCount(); y++ )
                {
                    string unitName = civFleet.UnitsByInternalNameRaw.GetPairByIndex( y ).Key;
                    List<GameEntity_Squad> unitsWithName = new List<GameEntity_Squad>();
                    for ( int z = 0; z < civFleet.UnitsByInternalNameRaw[unitName].Count; z++ )
                    {
                        GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( civFleet.UnitsByInternalNameRaw[unitName][z] );
                        if (entity == null )
                        {
                            civFleet.UnitsByInternalNameRaw[unitName].RemoveAt( z );
                            z--;
                            continue;
                        }
                        unitsWithName.Add( entity );
                    }
                    if ( unitsWithName.Count > 0 )
                        civFleet.UnitsByInternalName.AddPair( unitName, unitsWithName );
                }
                WorldData.CivFleets.AddPair( owner, civFleet );
            }
        }

        private void DecayOwnerlessUnits( CivFleet civFleet )
        {
            for ( int x = 0; x < civFleet.UnitsByInternalNameRaw.GetPairCount(); x++ )
            {
                ArcenSparseLookupPair<string, List<int>> pair = civFleet.UnitsByInternalNameRaw.GetPairByIndex( x );
                for ( int y = 0; y < pair.Value.Count; y++ )
                {
                    GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( pair.Value[y] );
                    if ( entity != null )
                        entity.TakeHullRepair( -(entity.GetMaxHullPoints() / 100) );
                    pair.Value.RemoveAt( y );
                    y--;
                }
                civFleet.UnitsByInternalNameRaw.RemovePairByKey( pair.Key );
                x--;
            }
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            HandlePerSecondResources();

            DockOrUndockCargoShips();

            HandleResourceTransferring( Context );

            CreateCargoShipsIfNeededAndAble( Context );
        }
        private void HandlePerSecondResources()
        {
            for ( int x = 0; x < WorldData.TradeEntities.Count; x++ )
            {
                GameEntity_Squad entity = WorldData.TradeEntities[x];
                CivCargoData cargo = entity.GetCargoSimSafeNeverNull();
                cargo.HandlePerSecondResources();
            }
        }
        private void DockOrUndockCargoShips()
        {
            for ( int x = 0; x < WorldData.CargoShips.Count; x++ )
            {
                GameEntity_Squad cargoShip = WorldData.CargoShips[x];
                CivCargoShipStatus status = cargoShip.GetCargoShipStatus();
                if ( status.IsIdle )
                    continue;
                GameEntity_Squad goal = status.HasOrigin ? status.Origin : status.HasDestination ? status.Destination : null;
                if ( cargoShip.Planet != goal.Planet )
                {
                    status.IsDocked = false;
                    continue;
                }
                if ( cargoShip.GetDistanceTo_VeryCheapButExtremelyRough( goal.WorldLocation, true ) < 1000 )
                    status.IsDocked = true;
                else
                    status.IsDocked = false;
            }
        }
        private void HandleResourceTransferring( ArcenSimContext Context )
        {
            for ( int x = 0; x < WorldData.CargoShips.Count; x++ )
            {
                GameEntity_Squad cargoShip = WorldData.CargoShips[x];
                CivCargoShipStatus status = cargoShip.GetCargoShipStatus();
                CivCargoData cargo = cargoShip.GetCargoSimSafeNeverNull();
                if ( status.IsDocked )
                    if ( status.HasOrigin )
                        HandleLoadingResources( cargoShip, cargo, status.Origin );
                    else if ( status.HasDestination )
                        if ( status.Destination.TypeData.GetHasTag( INDUSTRY_TAG ) )
                            HandleUnloadingResourcesToIndustryEntity( cargoShip, cargo, status.Destination, Context );
                        else
                            HandleUnloadingResourcesToTradeEntity( cargoShip, cargo, status.Destination );
            }
        }
        private void HandleLoadingResources( GameEntity_Squad cargoShip, CivCargoData cargo, GameEntity_Squad origin )
        {
            bool busy = false;
            CivCargoData originCargo = origin.GetCargoSimSafeNeverNull();
            for ( int x = 0; x < originCargo.Amount.GetPairCount(); x++ )
            {
                string resource = originCargo.Amount.GetPairByIndex( x ).Key;
                if ( originCargo.GetPerSecond( resource ) >= 0 && originCargo.GetAmount( resource ) > 0 && cargo.GetAmount( resource ) < cargo.GetCapacity( resource ) )
                {
                    cargo.AddAmount( resource, 1 );
                    originCargo.AddAmount( resource, -1 );
                    busy = true;
                }
            }
            if ( !busy )
                cargoShip.GetCargoShipStatus().FinishLoading();
        }
        private void HandleUnloadingResourcesToIndustryEntity( GameEntity_Squad cargoShip, CivCargoData cargo, GameEntity_Squad destination, ArcenSimContext Context )
        {
            bool busy = false;
            CivIndustryData industry = destination.GetIndustrySimSafeNeverNull();
            for ( int x = 0; x < industry.UnitTypeBuilt.GetPairCount(); x++ )
            {
                string resource = industry.UnitTypeBuilt.GetPairByIndex( x ).Key;
                if ( !WorldData.GetCanBuildAnother( destination, industry.UnitTypeBuilt[resource], true ) )
                    continue;

                if ( cargo.GetAmount( resource ) > 0 )
                {
                    cargo.AddAmount( resource, -1 );
                    industry.StoredResources[resource]++;

                    BuildUnitIfAble( resource, destination, industry, Context );

                    busy = true;
                }
            }
            if ( !busy )
                cargoShip.GetCargoShipStatus().MakeIdle();
        }
        private void BuildUnitIfAble( string resource, GameEntity_Squad industryUnit, CivIndustryData industry, ArcenSimContext Context )
        {
            if ( industry.StoredResources[resource] >= industry.Cost[resource] )
            {
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( industry.UnitTypeBuilt[resource] );

                Faction spawnFaction = industryUnit.PlanetFaction.Faction;

                if ( !industry.BuildForOwner )
                {
                    World_AIW2.Instance.DoForFactions( faction =>
                    {
                        if ( faction.Implementation is SpecialFaction_SKCivilianIndustry && faction.GetIsFriendlyTowards( spawnFaction ) )
                        {
                            spawnFaction = faction;
                            return DelReturn.Break;
                        }

                        return DelReturn.Continue;
                    } );
                }
                ArcenDebugging.SingleLineQuickDebug( $"Spawn faction: {spawnFaction.GetDisplayName()}, BuildForOwner: {industry.BuildForOwner}" );
                PlanetFaction pFaction = industryUnit.Planet.GetPlanetFactionForFaction( spawnFaction );
                ArcenPoint spawnPoint = entityData.IsMobile ? industryUnit.WorldLocation : industryUnit.Planet.GetSafePlacementPoint( Context, entityData, industryUnit.WorldLocation, 500, 10000 );
                GameEntity_Squad entity = GameEntity_Squad.CreateNew( pFaction, entityData, 1, pFaction.FleetUsedAtPlanet, 0, spawnPoint, Context );
                entity.MinorFactionStackingID = industryUnit.PrimaryKeyID;
                if ( entityData.IsMobile )
                    entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, spawnFaction.FactionIndex );

                industry.StoredResources[resource] -= industry.Cost[resource];
            }
        }
        private void HandleUnloadingResourcesToTradeEntity( GameEntity_Squad cargoShip, CivCargoData cargo, GameEntity_Squad destination )
        {
            bool busy = false;
            CivCargoData destinationCargo = destination.GetCargoSimSafeNeverNull();
            for ( int x = 0; x < cargo.Amount.GetPairCount(); x++ )
            {
                string resource = cargo.Amount.GetPairByIndex( x ).Key;
                if ( destinationCargo.GetPerSecond( resource ) <= 0 && cargo.GetAmount( resource ) > 0 && destinationCargo.GetAmount( resource ) < destinationCargo.GetCapacity( resource ) )
                {
                    destinationCargo.AddAmount( resource, 1 );
                    cargo.AddAmount( resource, -1 );
                    busy = true;
                }
            }
            if ( !busy )
                cargoShip.GetCargoShipStatus().MakeIdle();
        }
        private void CreateCargoShipsIfNeededAndAble( ArcenSimContext Context )
        {
            for ( int x = 0; x < WorldData.CargoShipsToBuild.GetPairCount(); x++ )
            {
                ArcenSparseLookupPair<GameEntity_Squad, int> pair = WorldData.CargoShipsToBuild.GetPairByIndex( x );

                GameEntityTypeData cargoShipData = GameEntityTypeDataTable.Instance.GetRowByName( CARGO_SHIP_TAG );

                GetCargoShipFromGrandStationIfExists( pair.Key, ref cargoShipData, Context );

                for ( int y = 0; y < pair.Value; y++ )
                    GameEntity_Squad.CreateNew( pair.Key.PlanetFaction, cargoShipData, 1, pair.Key.PlanetFaction.FleetUsedAtPlanet, 0, pair.Key.WorldLocation, Context );
            }
        }
        private void GetCargoShipFromGrandStationIfExists( GameEntity_Squad station, ref GameEntityTypeData cargoShipData, ArcenSimContext Context )
        {
            for ( int x = 0; x < station.TypeData.TagsList.Count; x++ )
            {
                string tag = station.TypeData.TagsList[x];
                if ( tag.Contains( GRAND_SHIPYARD_SPAWNING_TAG ) )
                {
                    string shipTag = tag.Substring( GRAND_SHIPYARD_SPAWNING_TAG.Length );
                    List<GameEntityTypeData> validCargoShips = new List<GameEntityTypeData>();
                    for ( int y = 0; y < GameEntityTypeDataTable.Instance.RowsByTag[shipTag].Count; y++ )
                        if ( GameEntityTypeDataTable.Instance.RowsByTag[shipTag][y].GetHasTag( CARGO_SHIP_TAG ) )
                            validCargoShips.Add( GameEntityTypeDataTable.Instance.RowsByTag[shipTag][y] );

                    if ( validCargoShips.Count > 0 )
                        cargoShipData = validCargoShips[Context.RandomToUse.Next( validCargoShips.Count )];
                    break;
                }
            }
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            SetUpTradeTeams();

            GameCommand command = GameCommand.Create( GameCommandTypeTable.Instance.GetRowByName( Commands.PopulateCargoShipsToBuildList.ToString() ), GameCommandSource.AnythingElse );

            ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> wormholeCommands = new ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>>();
            ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> movementCommands = new ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>>();

            for ( int x = 0; x < CivilianTeamsForBackgroundThreadOnly.Count; x++ )
            {
                CivTeamData teamData = CivilianTeamsForBackgroundThreadOnly[x];

                PopulateUnitLists( teamData, Context );

                if ( teamData.TradeEntities.Count + teamData.IndustryEntities.Count == 0 )
                    continue;

                GetTradeRequests( teamData, Context );

                teamData.TradeRequests.Sort();

                int neededCargoShips = AssignTradeRequests( teamData, Context );
                if ( teamData.GrandStation != null )
                {
                    command.RelatedEntityIDs.Add( teamData.GrandStation.PrimaryKeyID );
                    command.RelatedIntegers.Add( neededCargoShips );
                }

                MoveCargoShips( teamData, ref wormholeCommands, ref movementCommands );
            }

            if ( command.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( command );

            if ( wormholeCommands.GetPairCount() > 0 )
                Utilities.ExecuteWormholeCommands( faction, Context, wormholeCommands );

            if ( movementCommands.GetPairCount() > 0 )
                Utilities.ExecuteMovementCommands( faction, Context, movementCommands );
        }
        private void SetUpTradeTeams()
        {
            CivilianTeamsForBackgroundThreadOnly = new List<CivTeamData>();

            World_AIW2.Instance.DoForFactions( workingFaction =>
            {
                if ( workingFaction.Type == FactionType.NaturalObject || (workingFaction.MustBeAwakenedByPlayer && !workingFaction.HasBeenAwakenedByPlayer) )
                    return DelReturn.Continue;

                bool partOfTeam = false;
                for ( int x = 0; x < CivilianTeamsForBackgroundThreadOnly.Count; x++ )
                {
                    CivTeamData workingTeamData = CivilianTeamsForBackgroundThreadOnly[x];
                    for ( int y = 0; y < workingTeamData.Factions.Count; y++ )
                        if ( workingTeamData.Factions[y].GetIsFriendlyTowards( workingFaction ) )
                        {
                            workingTeamData.Factions.Add( workingFaction );
                            partOfTeam = true;
                            break;
                        }
                }
                if ( !partOfTeam )
                    CivilianTeamsForBackgroundThreadOnly.Add( new CivTeamData() { Factions = new List<Faction>() { workingFaction } } );
                return DelReturn.Continue;
            } );
        }
        private void PopulateUnitLists( CivTeamData teamData, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand populateCargoShipsList = GameCommand.Create( GameCommandTypeTable.Instance.GetRowByName( Commands.PopulateCargoShipList.ToString() ), GameCommandSource.AnythingElse );
            GameCommand populateTradeEntitiesList = GameCommand.Create( GameCommandTypeTable.Instance.GetRowByName( Commands.PopulateTradeEntitiesList.ToString() ), GameCommandSource.AnythingElse );
            GameCommand populateIndustryEntitiesList = GameCommand.Create( GameCommandTypeTable.Instance.GetRowByName( Commands.PopulateIndustryEntitiesList.ToString() ), GameCommandSource.AnythingElse );
            populateCargoShipsList.RelatedBool = true;
            populateTradeEntitiesList.RelatedBool = true;
            populateIndustryEntitiesList.RelatedBool = true;

            for ( int x = 0; x < teamData.Factions.Count; x++ )
            {
                Faction faction = teamData.Factions[x];
                List<Planet> influencedPlanets = new List<Planet>();
                faction.Entities.DoForEntities( EntityRollupType.Tagged, ( GameEntity_Squad entity ) =>
                {
                    entity.Planet.DoForLinkedNeighborsAndSelf( false, planet =>
                    {
                        if ( !influencedPlanets.Contains( planet ) )
                            influencedPlanets.Add( planet );
                        return DelReturn.Break;
                    } );

                    if ( entity.TypeData.GetHasTag( GRAND_SHIPYARD_TAG ) )
                    {
                        AddGrandStationToTeam( entity, teamData );
                    }

                    else if ( entity.TypeData.GetHasTag( TRADER_TAG ) )
                    {
                        teamData.TradeEntities.Add( entity );
                        if ( !WorldData.TradeEntitiesRaw.Contains( entity.PrimaryKeyID ) )
                            populateTradeEntitiesList.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    }

                    else if ( entity.TypeData.GetHasTag( INDUSTRY_TAG ) )
                    {
                        teamData.IndustryEntities.Add( entity );
                        if ( !WorldData.IndustryEntitiesRaw.Contains( entity.PrimaryKeyID ) )
                            populateIndustryEntitiesList.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    }

                    else if ( entity.TypeData.GetHasTag( CARGO_SHIP_TAG ) && entity.TypeData.IsMobile )
                    {
                        teamData.CargoShips.Add( entity );
                        if ( !WorldData.CargoShipsRaw.Contains( entity.PrimaryKeyID ) )
                            populateCargoShipsList.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    }

                    else if ( World_AIW2.Instance.GetEntityByID_Squad( entity.MinorFactionStackingID ) != null )
                    {
                        AddUnitToTeamData( entity, teamData );
                        if ( !WorldData.IsInCivFleet( entity ) )
                        {
                            GameCommand command = GameCommand.Create( GameCommandTypeTable.Instance.GetRowByName( Commands.AddUnitToCivFleet.ToString() ), GameCommandSource.AnythingElse );
                            command.RelatedEntityIDs.Add( entity.MinorFactionStackingID );
                            command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                            command.RelatedString = entity.TypeData.InternalName;
                            Context.QueueCommandForSendingAtEndOfContext( command );
                        }
                    }

                    return DelReturn.Continue;
                } );
                World_AIW2.Instance.GetNeutralFaction().Entities.DoForEntities( EntityRollupType.Tagged, entity =>
                {
                    if ( !influencedPlanets.Contains( entity.Planet ) )
                        return DelReturn.Continue;

                    if ( entity.TypeData.GetHasTag( TRADER_TAG ) )
                    {
                        teamData.TradeEntities.Add( entity );
                        if ( !WorldData.TradeEntitiesRaw.Contains( entity.PrimaryKeyID ) )
                            populateTradeEntitiesList.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    }
                    else if ( entity.TypeData.GetHasTag( CARGO_SHIP_TAG ) && entity.TypeData.IsMobile )
                    {
                        teamData.CargoShips.Add( entity );
                        if ( !WorldData.CargoShipsRaw.Contains( entity.PrimaryKeyID ) )
                            populateCargoShipsList.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    }

                    return DelReturn.Continue;
                } );
            }

            if ( populateCargoShipsList.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( populateCargoShipsList );
            if ( populateTradeEntitiesList.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( populateTradeEntitiesList );
            if ( populateIndustryEntitiesList.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( populateIndustryEntitiesList );
        }
        private void AddGrandStationToTeam( GameEntity_Squad station, CivTeamData teamData )
        {
            if ( teamData.GrandStation == null || World_AIW2.Instance.GameSecond % 2 == 0 )
                teamData.GrandStation = station;
        }
        private void AddUnitToTeamData( GameEntity_Squad unit, CivTeamData teamData )
        {
            if ( teamData.UnitsByCreator.GetHasKey( unit.MinorFactionStackingID ) )
                if ( teamData.UnitsByCreator[unit.MinorFactionStackingID].GetHasKey( unit.TypeData.InternalName ) )
                    teamData.UnitsByCreator[unit.MinorFactionStackingID][unit.TypeData.InternalName] += 1 + unit.ExtraStackedSquadsInThis;
                else
                    teamData.UnitsByCreator[unit.MinorFactionStackingID].AddPair( unit.TypeData.InternalName, 1 + unit.ExtraStackedSquadsInThis );
            else
            {
                ArcenSparseLookup<string, int> _ = new ArcenSparseLookup<string, int>();
                _.AddPair( unit.TypeData.InternalName, 1 + unit.ExtraStackedSquadsInThis );
                teamData.UnitsByCreator.AddPair( unit.MinorFactionStackingID, _ );
            }
        }
        private void GetTradeRequests( CivTeamData teamData, ArcenLongTermIntermittentPlanningContext Context )
        {
            teamData.TradeRequests = new List<CivTradeRequest>();
            for ( int x = 0; x < teamData.TradeEntities.Count; x++ )
                GetTradeRequest( teamData.TradeEntities[x], teamData, Context );
            for ( int x = 0; x < teamData.IndustryEntities.Count; x++ )
                GetIndustryRequest( teamData.IndustryEntities[x], teamData, Context );
        }
        private void GetTradeRequest( GameEntity_Squad tradeUnit, CivTeamData teamData, ArcenLongTermIntermittentPlanningContext Context )
        {
            CivCargoData cargo = tradeUnit.GetCargoNotSimSafeMayReturnNull( Context );
            if ( cargo == null )
                return;

            CivTradeRequest importRequest = new CivTradeRequest() { Unit = tradeUnit };
            CivTradeRequest exportRequest = new CivTradeRequest() { Unit = tradeUnit };
            for ( int x = 0; x < Resources.Resources.Count; x++ )
            {
                CivResource resource = Resources.Resources[x];
                string key = resource.Name;

                bool canImport = tradeUnit.TypeData.GetHasTag( IMPORT_TAG );
                if ( !canImport && cargo.GetPerSecond( key ) <= 0 )
                    continue;

                bool requestOnly = false, supplyOnly = false;
                if ( cargo.GetPerSecond( key ) < 0 )
                    requestOnly = true;
                else if ( cargo.GetPerSecond( key ) > 0 )
                    supplyOnly = true;

                int outgoing = !requestOnly ? cargo.GetAmount( key ) : 0;
                int incoming = !supplyOnly ? cargo.GetCapacity( key ) - cargo.GetAmount( key ) : 0;
                int importEnroute = 0;
                int exportEnroute = 0;

                for ( int y = 0; y < teamData.CargoShips.Count; y++ )
                {
                    CivCargoShipStatus status = teamData.CargoShips[y].GetCargoShipStatus();
                    CivCargoData shipCargo = teamData.CargoShips[y].GetCargoNotSimSafeMayReturnNull( Context );
                    if ( shipCargo == null )
                        continue;

                    if ( status.Origin == tradeUnit )
                    {
                        outgoing -= shipCargo.GetCapacity( key ) - shipCargo.GetAmount(key);
                        exportEnroute++;
                    }
                    if ( status.Destination == tradeUnit )
                    {
                        incoming -= shipCargo.GetCapacity( key ) - shipCargo.GetAmount(key);
                        importEnroute++;
                    }
                }

                if ( outgoing >= 100 )
                    exportRequest.ResourcesOffered.AddPair( resource, outgoing );
                if ( incoming >= 100 )
                    importRequest.ResourcesRequested.AddPair( resource, incoming );
                exportRequest.CargoShipsAlreadyEnroute = exportEnroute;
                importRequest.CargoShipsAlreadyEnroute = importEnroute;
            }
            if ( exportRequest.ResourcesOffered.GetPairCount() > 0 )
                teamData.TradeRequests.Add( exportRequest );
            if ( importRequest.ResourcesRequested.GetPairCount() > 0 )
                teamData.TradeRequests.Add( importRequest );
        }
        private void GetIndustryRequest( GameEntity_Squad industryUnit, CivTeamData teamData, ArcenLongTermIntermittentPlanningContext Context )
        {
            CivIndustryData industry = industryUnit.GetIndustryNotSimSafeMayReturnNull( Context );
            if ( industry == null )
                return;
            ArcenDebugging.SingleLineQuickDebug( "Starting Industry Request" );
            CivTradeRequest tradeRequest = new CivTradeRequest() { Unit = industryUnit };
            for ( int x = 0; x < industry.UnitTypeBuilt.GetPairCount(); x++ )
            {
                ArcenSparseLookupPair<string, string> pair = industry.UnitTypeBuilt.GetPairByIndex( x );
                CivResource resource = Resources.GetResourceByName( pair.Key );
                string unit = pair.Value;
                ArcenDebugging.SingleLineQuickDebug( "Can we build another " + unit );
                if ( !WorldData.GetCanBuildAnother( industryUnit, unit, false ) )
                    continue;
                ArcenDebugging.SingleLineQuickDebug( "Can Build Another " + unit );
                int amount = Math.Max( 500, industry.Cost[resource.Name] * 2 );
                int enroute = 0;
                for ( int y = 0; y < teamData.CargoShips.Count; y++ )
                {
                    CivCargoShipStatus status = teamData.CargoShips[y].GetCargoShipStatus();
                    CivCargoData shipCargo = teamData.CargoShips[y].GetCargoNotSimSafeMayReturnNull( Context );
                    if ( shipCargo != null && status.Destination == industryUnit )
                    {
                        amount -= shipCargo.GetCapacity( resource.Name ) - shipCargo.GetAmount(resource.Name);
                        enroute++;
                    }
                }
                ArcenDebugging.SingleLineQuickDebug( "Amount: " + amount + " Enroute: " + enroute );
                if ( amount > 0 )
                    tradeRequest.ResourcesRequested.AddPair( resource, amount );
                tradeRequest.CargoShipsAlreadyEnroute = enroute;
            }
            if ( tradeRequest.ResourcesRequested.GetPairCount() > 0 )
                teamData.TradeRequests.Add( tradeRequest );
        }
        private int AssignTradeRequests( CivTeamData teamData, ArcenLongTermIntermittentPlanningContext Context )
        {
            List<GameEntity_Squad> idleCargoShips = GetIdleCargoShips( teamData );

            int cargoShipsNeeded = 0;
            GameCommand command = GameCommand.Create( GameCommandTypeTable.Instance.GetRowByName( Commands.ExecuteTradeOrder.ToString() ), GameCommandSource.AnythingElse );
            for ( int x = 0; x < teamData.TradeRequests.Count; x++ )
            {
                CivTradeRequest supply = teamData.TradeRequests[x];
                if ( supply.TotalOffered <= 0 )
                    continue;

                for ( int y = 0; y < teamData.TradeRequests.Count; y++ )
                {
                    if ( y == x )
                        continue;
                    CivTradeRequest demand = teamData.TradeRequests[y];
                    if ( demand.TotalRequested <= 0 )
                        continue;

                    if ( !supply.IsMatch( demand ) )
                        continue;

                    int deductionFromHops = 250 * supply.Unit.Planet.GetHopsTo( demand.Unit.Planet );

                    if ( supply.TotalOffered - deductionFromHops < 100 )
                        continue;

                    if ( idleCargoShips.Count <= 0 )
                    {
                        cargoShipsNeeded++;
                        teamData.TradeRequests.Remove( supply );
                        teamData.TradeRequests.Remove( demand );
                        if ( y < x )
                            x--;
                        x--;
                        break;
                    }

                    GameEntity_Squad cargoShip = null;
                    int currentHops = 99;
                    for ( int z = 0; z < idleCargoShips.Count; z++ )
                    {
                        GameEntity_Squad workingCargoShip = idleCargoShips[z];
                        int hops = workingCargoShip.Planet.GetHopsTo( supply.Unit.Planet );
                        if ( hops < currentHops )
                        {
                            List<Planet> path = workingCargoShip.PlanetFaction.Faction.FindPath( workingCargoShip.Planet, supply.Unit.Planet, PathingMode.Safest, Context );
                            bool isSafe = true;
                            for ( int p = 0; p < path.Count; p++ )
                            {
                                if ( path[p].GetControllingOrInfluencingFaction().GetIsFriendlyTowards( workingCargoShip.PlanetFaction.Faction ) )
                                    continue;

                                int friendlyStrength = path[p].GetPlanetFactionForFaction( workingCargoShip.PlanetFaction.Faction ).DataByStance[FactionStance.Self].TotalStrength +
                                                    path[p].GetPlanetFactionForFaction( workingCargoShip.PlanetFaction.Faction ).DataByStance[FactionStance.Friendly].TotalStrength;
                                int hostileStrength = path[p].GetPlanetFactionForFaction( workingCargoShip.PlanetFaction.Faction ).DataByStance[FactionStance.Hostile].TotalStrength;
                                if ( friendlyStrength * 5 < hostileStrength )
                                    isSafe = false;
                            }
                            if ( !isSafe )
                                continue;
                            cargoShip = workingCargoShip;
                            currentHops = hops;
                        }
                    }

                    if ( cargoShip == null )
                        continue;

                    idleCargoShips.Remove( cargoShip );

                    command.RelatedEntityIDs.Add( cargoShip.PrimaryKeyID );
                    command.RelatedIntegers.Add( supply.Unit.PrimaryKeyID );
                    command.RelatedIntegers2.Add( demand.Unit.PrimaryKeyID );

                    teamData.TradeRequests.Remove( supply );
                    teamData.TradeRequests.Remove( demand );

                    if ( y < x )
                        x--;
                    x--;

                    break;
                }
            }
            if ( command.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( command );
            return cargoShipsNeeded;
        }
        private List<GameEntity_Squad> GetIdleCargoShips( CivTeamData teamData )
        {
            List<GameEntity_Squad> idleCargoShips = new List<GameEntity_Squad>();

            for ( int x = 0; x < teamData.CargoShips.Count; x++ )
                if ( teamData.CargoShips[x].GetCargoShipStatus().IsIdle )
                    idleCargoShips.Add( teamData.CargoShips[x] );

            return idleCargoShips;
        }
        private void MoveCargoShips( CivTeamData teamData, ref ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> wormholeCommands,
                                                           ref ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> movementCommands )
        {
            for ( int x = 0; x < teamData.CargoShips.Count; x++ )
            {
                GameEntity_Squad cargoShip = teamData.CargoShips[x];
                CivCargoShipStatus status = cargoShip.GetCargoShipStatus();
                if ( status.IsDocked || status.IsIdle )
                    continue;
                if ( status.HasOrigin )
                    if ( cargoShip.Planet == status.Origin.Planet )
                        Utilities.QueueMovementCommand( cargoShip, status.Origin.WorldLocation, ref movementCommands );
                    else
                        Utilities.QueueWormholeCommand( cargoShip, status.Origin.Planet, ref wormholeCommands );
                else if ( status.HasDestination )
                    if ( cargoShip.Planet == status.Destination.Planet )
                        Utilities.QueueMovementCommand( cargoShip, status.Destination.WorldLocation, ref movementCommands );
                    else
                        Utilities.QueueWormholeCommand( cargoShip, status.Destination.Planet, ref wormholeCommands );
            }
        }
    }
}