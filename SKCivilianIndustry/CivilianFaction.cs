using System;
using System.Collections.Generic;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using SKCivilianIndustry.Persistence;

namespace SKCivilianIndustry
{

    /// <summary>
    /// Invidual storage class for each faction.
    /// </summary>
    public class CivilianFaction : ArcenExternalSubManagedData
    {
        // Version of this class.
        public int Version;

        // All values stored are the index value of ships. This is done as to simply the process of saving and loading.
        // We index all of our faction ships so that they can be easily looped through based on what we're doing.

        // Index of this faction's Grand Station.
        private int GrandStationID;
        public GameEntity_Squad GrandStation { get { return World_AIW2.Instance.GetEntityByID_Squad( GrandStationID ); } set { GrandStationID = value?.PrimaryKeyID ?? -1; } }

        // Rebuild timer for Grand Station.
        public int GrandStationRebuildTimerInSeconds;

        // Index of all trade stations that belong to this faction.
        public List<int> TradeStations;

        // Rebuild timer for Trade Stations by planet index.
        public ArcenSparseLookup<int, int> TradeStationRebuildTimerInSecondsByPlanet;

        // Functions for setting and getting rebuild timers.
        public int GetTradeStationRebuildTimer( Planet planet )
        {
            return GetTradeStationRebuildTimer( planet.Index );
        }
        public int GetTradeStationRebuildTimer( int planet )
        {
            if ( TradeStationRebuildTimerInSecondsByPlanet.GetHasKey( planet ) )
                return TradeStationRebuildTimerInSecondsByPlanet[planet];
            else
                return 0;
        }
        public void SetTradeStationRebuildTimer( Planet planet, int timer )
        {
            SetTradeStationRebuildTimer( planet.Index, timer );
        }
        public void SetTradeStationRebuildTimer( int planet, int timer )
        {
            if ( TradeStationRebuildTimerInSecondsByPlanet.GetHasKey( planet ) )
                TradeStationRebuildTimerInSecondsByPlanet[planet] = timer;
            else
                TradeStationRebuildTimerInSecondsByPlanet.AddPair( planet, timer );
        }

        // Index of all cargo ships that belong to this faction.
        public List<int> CargoShips;

        // List of all cargo ships by current status that belong to this faction.
        public List<int> CargoShipsIdle;
        public List<int> CargoShipsLoading;
        public List<int> CargoShipsUnloading;
        public List<int> CargoShipsBuilding;
        public List<int> CargoShipsPathing;
        public List<int> CargoShipsEnroute;

        // Index of all Militia Construction Ships and/or Militia Buildings
        public List<int> MilitiaLeaders;

        // Counter used to determine when another cargo ship should be built.
        public int BuildCounter;

        /// <summary>
        /// Last reported number of failed trade routes due to a lack of cargo ships.
        /// </summary>
        public (int Import, int Export) FailedCounter;

        // Counter used to determine when another militia ship should be built.
        public int MilitiaCounter;

        // How long until the next raid?
        public int NextRaidInThisSeconds;

        // Index of wormholes for the next raid.
        public List<int> NextRaidWormholes;

        // The next entity to build a Trade Station next to.
        private int NextTradeStationTargetID;
        public GameEntity_Squad NextTradeStationTarget { get { return World_AIW2.Instance.GetEntityByID_Squad( NextTradeStationTargetID ); } set { NextTradeStationTargetID = value?.PrimaryKeyID ?? -1; } }

        // Unlike every other value, the follow values are not stored and saved. They are simply regenerated whenever needed.
        // Contains the calculated threat value on every planet.
        // Calculated threat is all hostile strength - all friendly (excluding our own) strength.
        public List<ThreatReport> ThreatReports;
        public List<TradeRequest> ImportRequests;
        public List<TradeRequest> ExportRequests;

        // Get the threat value for a planet.
        public (int MilitiaGuard, int MilitiaMobile, int FriendlyGuard, int FriendlyMobile, int CloakedHostile, int NonCloakedHostile, int Wave, int Total) GetThreat( Planet planet )
        {
            try
            {
                // If reports aren't generated, return 0.
                if ( ThreatReports == null )
                    return (0, 0, 0, 0, 0, 0, 0, 0);
                else
                    for ( int x = 0; x < ThreatReports.Count; x++ )
                        if ( ThreatReports[x].Planet.Index == planet.Index )
                            return ThreatReports[x].GetThreat();
                // Planet not processed. Return 0.
                return (0, 0, 0, 0, 0, 0, 0, 0);
            }
            catch ( Exception e )
            {
                // Failed to return a report, return 0. Harmless, so we don't worry about informing the player.
                ArcenDebugging.SingleLineQuickDebug( e.Message );
                return (0, 0, 0, 0, 0, 0, 0, 0);
            }
        }

        // Returns the base resource cost for ships.
        public int GetResourceCost( Faction faction )
        {
            // 51 - (Intensity ^ 1.5)
            return 51 - (int)Math.Pow( faction.Ex_MinorFactionCommon_GetPrimitives(ExternalDataRetrieval.CreateIfNotFound).Intensity, 1.5 );
        }

        /// <summary>
        /// Returns the ship/turret capacity. Increases based on intensity and trade station count.
        /// </summary>
        /// <returns></returns>
        public int GetCap( Faction faction )
        {
            int baseCap = 10;
            int intensity = faction.Ex_MinorFactionCommon_GetPrimitives(ExternalDataRetrieval.CreateIfNotFound).Intensity;
            int intensityBonus = intensity > 5 ? intensity * 2 : 0;
            FInt intensityMult = FInt.FromParts( 0, 750 ) + (FInt.FromParts( 0, 050 ) * intensity);
            int stationBonus = 2 * TradeStations.Count;
            int cap = ((baseCap + intensityBonus + stationBonus) * intensityMult).GetNearestIntPreferringHigher();
            return cap;
        }

        // Should never be used by itself, removes the cargo ship from all applicable statuses, but keeps it in the main cargo ship list.
        private void RemoveCargoShipStatus( int cargoShipID )
        {
            if ( this.CargoShipsIdle.Contains( cargoShipID ) )
                this.CargoShipsIdle.Remove( cargoShipID );
            if ( this.CargoShipsLoading.Contains( cargoShipID ) )
                this.CargoShipsLoading.Remove( cargoShipID );
            if ( this.CargoShipsUnloading.Contains( cargoShipID ) )
                this.CargoShipsUnloading.Remove( cargoShipID );
            if ( this.CargoShipsBuilding.Contains( cargoShipID ) )
                this.CargoShipsBuilding.Remove( cargoShipID );
            if ( this.CargoShipsPathing.Contains( cargoShipID ) )
                this.CargoShipsPathing.Remove( cargoShipID );
            if ( this.CargoShipsEnroute.Contains( cargoShipID ) )
                this.CargoShipsEnroute.Remove( cargoShipID );
        }

        /// <summary>
        /// Remove a cargo ship from amy list it is currently in, effectively deleting it from the faction, but NOT from the world.
        /// The entity itself must be killed or despawned before or after this.
        /// </summary>
        /// <param name="cargoShipID">The PrimaryKeyID of the ship to remove.</param>
        public void RemoveCargoShip( int cargoShipID )
        {
            if ( this.CargoShips.Contains( cargoShipID ) )
                this.CargoShips.Remove( cargoShipID );
            RemoveCargoShipStatus( cargoShipID );
        }

        /// <summary>
        /// Remove a cargo ship from whatever it is currently doing, and change its action to the requested action.
        /// </summary>
        /// <param name="cargoShipID">The PrimaryKeyID of the ship to modify.</param>
        /// <param name="status">The status to change to. Idle, Loading, Unloading, Building, Pathing, or Enroute</param>
        public void ChangeCargoShipStatus( GameEntity_Squad cargoShip, Status status )
        {
            int cargoShipID = cargoShip.PrimaryKeyID;
            if ( !this.CargoShips.Contains( cargoShipID ) )
                return;
            RemoveCargoShipStatus( cargoShipID );
            switch ( status )
            {
                case Status.Loading:
                    this.CargoShipsLoading.Add( cargoShipID );
                    break;
                case Status.Unloading:
                    this.CargoShipsUnloading.Add( cargoShipID );
                    break;
                case Status.Building:
                    this.CargoShipsBuilding.Add( cargoShipID );
                    break;
                case Status.Pathing:
                    this.CargoShipsPathing.Add( cargoShipID );
                    break;
                case Status.Enroute:
                    this.CargoShipsEnroute.Add( cargoShipID );
                    break;
                default:
                    this.CargoShipsIdle.Add( cargoShipID );
                    break;
            }
        }

        /// <summary>
        /// Returns true if we should consider the planet friendly.
        /// </summary>
        /// <param name="faction">The Civilian Industry faction to check.</param>
        /// <param name="planet">The Planet to check.</param>
        /// <returns></returns>
        public bool IsPlanetFriendly( Faction faction, Planet planet )
        {
            if ( planet.GetControllingOrInfluencingFaction().GetIsFriendlyTowards( faction ) )
                return true; // If planet is owned by a friendly faction, its friendly.

            for ( int x = 0; x < TradeStations.Count; x++ )
            {
                GameEntity_Squad tradeStation = World_AIW2.Instance.GetEntityByID_Squad( TradeStations[x] );
                if ( tradeStation == null )
                    continue;
                if ( tradeStation.Planet.Index == planet.Index )
                    return true; // Planet has a trade station on it, its friendly.
            }

            for ( int x = 0; x < MilitiaLeaders.Count; x++ )
            {
                GameEntity_Squad militia = World_AIW2.Instance.GetEntityByID_Squad( MilitiaLeaders[x] );
                if ( militia == null )
                    continue;
                if ( militia.Planet.Index == planet.Index )
                    return true; // Planet has a militia leader on it, its friendly.

                CivilianMilitia militiaData = militia.GetCivilianMilitiaExt(ExternalDataRetrieval.ReturnNullIfNotFound);
                if ( militiaData == null )
                    continue;
                if ( militiaData.Centerpiece == -1 )
                    continue;
                GameEntity_Squad centerpiece = World_AIW2.Instance.GetEntityByID_Squad( militiaData.Centerpiece );
                if ( centerpiece == null )
                    continue;
                if ( centerpiece.Planet.Index == planet.Index )
                    return true; // Planet has a militia leader's centerpiece on it, its friendly.
            }

            // Nothing passed. Its hostile.
            return false;
        }

        // Following three functions are used for initializing, saving, and loading data.
        // Initialization function.
        // Default values. Called on creation, NOT on load.
        public CivilianFaction()
        {
            this.GrandStationID = -1;
            this.GrandStationRebuildTimerInSeconds = 0;
            this.TradeStations = new List<int>();
            this.TradeStationRebuildTimerInSecondsByPlanet = new ArcenSparseLookup<int, int>();
            this.CargoShips = new List<int>();
            this.CargoShipsIdle = new List<int>();
            this.CargoShipsLoading = new List<int>();
            this.CargoShipsUnloading = new List<int>();
            this.CargoShipsBuilding = new List<int>();
            this.CargoShipsPathing = new List<int>();
            this.CargoShipsEnroute = new List<int>();
            this.MilitiaLeaders = new List<int>();
            this.BuildCounter = 0;
            this.MilitiaCounter = 0;
            this.NextRaidInThisSeconds = 1800;
            this.NextRaidWormholes = new List<int>();
            this.NextTradeStationTarget = null;

            this.ThreatReports = new List<ThreatReport>();
            this.ImportRequests = new List<TradeRequest>();
            this.ExportRequests = new List<TradeRequest>();
        }

        public CivilianFaction( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializeIntoSelf( Buffer, false );
        }

        // Serialize a list.
        private void SerializeList( List<int> list, ArcenSerializationBuffer Buffer )
        {
            // Lists require a special touch to save.
            // Get the number of items in the list, and store that as well.
            // This is so you know how many items you'll have to load later.
            int count = list.Count;
            Buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
                Buffer.AddInt32( ReadStyle.Signed, list[x] );
        }
        private void SerializeSparseLookup( ArcenSparseLookup<int, int> lookup, ArcenSerializationBuffer Buffer )
        {
            Buffer.AddInt32( ReadStyle.NonNeg, lookup.GetPairCount() );
            lookup.DoFor( pair =>
            {
                Buffer.AddInt32( ReadStyle.NonNeg, pair.Key );
                Buffer.AddInt32( ReadStyle.Signed, pair.Value );

                return DelReturn.Continue;
            } );
        }
        // Saving our data.
        public override void SerializeTo( ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Buffer.AddInt32( ReadStyle.NonNeg, 3 );
            Buffer.AddInt32( ReadStyle.Signed, this.GrandStationID );
            Buffer.AddInt32( ReadStyle.Signed, this.GrandStationRebuildTimerInSeconds );
            SerializeList( TradeStations, Buffer );
            SerializeSparseLookup( TradeStationRebuildTimerInSecondsByPlanet, Buffer );
            SerializeList( CargoShips, Buffer );
            SerializeList( MilitiaLeaders, Buffer );
            SerializeList( CargoShipsIdle, Buffer );
            SerializeList( CargoShipsLoading, Buffer );
            SerializeList( CargoShipsUnloading, Buffer );
            SerializeList( CargoShipsBuilding, Buffer );
            SerializeList( CargoShipsPathing, Buffer );
            SerializeList( CargoShipsEnroute, Buffer );
            Buffer.AddInt32( ReadStyle.Signed, this.BuildCounter );
            Buffer.AddInt32( ReadStyle.NonNeg, this.FailedCounter.Import );
            Buffer.AddInt32( ReadStyle.NonNeg, this.FailedCounter.Export );
            Buffer.AddInt32( ReadStyle.Signed, this.MilitiaCounter );
            Buffer.AddInt32( ReadStyle.Signed, this.NextRaidInThisSeconds );
            SerializeList( this.NextRaidWormholes, Buffer );
            Buffer.AddInt32( ReadStyle.PosExceptNeg1, this.NextTradeStationTargetID );
        }
        // Deserialize a list.
        public void DeserializeList( ref List<int> list, ArcenDeserializationBuffer Buffer )
        {
            // Lists require a special touch to load.
            // We'll have saved the number of items stored up above to be used here to determine the number of items to load.
            // ADDITIONALLY we'll need to recreate a blank list beforehand, as loading does not call the Initialization function.
            // Can't add values to a list that doesn't exist, after all.
            if ( list != null )
                list.Clear();
            else
                list = new List<int>();
            int count = Buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
                list.Add( Buffer.ReadInt32( ReadStyle.Signed ) );
        }
        public void DeserializeLookup( ref ArcenSparseLookup<int, int> lookup, ArcenDeserializationBuffer Buffer )
        {
            if ( lookup != null )
                lookup.Clear();
            else
                lookup = new ArcenSparseLookup<int, int>();
            int count = Buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
            {
                int key = Buffer.ReadInt32( ReadStyle.NonNeg );
                int value = Buffer.ReadInt32( ReadStyle.Signed );
                if ( !lookup.GetHasKey( key ) )
                    lookup.AddPair( key, value );
                else
                    lookup[key] = value;
            }
        }
        // Loading our data. Make sure the loading order is the same as the saving order.
        public override void DeserializeIntoSelf( ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            this.Version = Buffer.ReadInt32( ReadStyle.NonNeg );
            this.GrandStationID = Buffer.ReadInt32( ReadStyle.Signed );
            this.GrandStationRebuildTimerInSeconds = Buffer.ReadInt32( ReadStyle.Signed );
            DeserializeList( ref this.TradeStations, Buffer );
            DeserializeLookup( ref this.TradeStationRebuildTimerInSecondsByPlanet, Buffer );
            DeserializeList( ref this.CargoShips, Buffer );
            DeserializeList( ref this.MilitiaLeaders, Buffer );
            DeserializeList( ref this.CargoShipsIdle, Buffer );
            DeserializeList( ref this.CargoShipsLoading, Buffer );
            DeserializeList( ref this.CargoShipsUnloading, Buffer );
            DeserializeList( ref this.CargoShipsBuilding, Buffer );
            DeserializeList( ref this.CargoShipsPathing, Buffer );
            DeserializeList( ref this.CargoShipsEnroute, Buffer );
            this.BuildCounter = Buffer.ReadInt32( ReadStyle.Signed );
            if ( this.Version >= 2 )
                this.FailedCounter = (Buffer.ReadInt32( ReadStyle.NonNeg ), Buffer.ReadInt32( ReadStyle.NonNeg ));
            else
                this.FailedCounter = (0, 0);
            this.MilitiaCounter = Buffer.ReadInt32( ReadStyle.Signed );
            this.NextRaidInThisSeconds = Buffer.ReadInt32( ReadStyle.Signed );
            DeserializeList( ref this.NextRaidWormholes, Buffer );

            if ( this.Version < 3 )
                this.NextTradeStationTargetID = -1;
            else
                this.NextTradeStationTargetID = Buffer.ReadInt32( ReadStyle.PosExceptNeg1 );

            if ( this.ThreatReports == null )
                this.ThreatReports = new List<ThreatReport>();
            if ( this.ImportRequests == null )
                this.ImportRequests = new List<TradeRequest>();
            if ( this.ExportRequests == null )
                this.ExportRequests = new List<TradeRequest>();

            // Recreate an empty list on load. Will be populated when needed.
            this.ThreatReports.Clear();
            this.ImportRequests.Clear();
            this.ExportRequests.Clear();
        }
    }
}