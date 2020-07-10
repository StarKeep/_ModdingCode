using Arcen.AIW2.Core;
using Arcen.Universal;
using System.Collections.Generic;

namespace SKCivilianIndustry.Storage
{
    public class CivFleet
    {
        public ArcenSparseLookup<string, List<int>> UnitsByInternalNameRaw;
        public ArcenSparseLookup<string, List<GameEntity_Squad>> UnitsByInternalName;

        public int GetUnitCount( string unitName )
        {
            if ( UnitsByInternalName.GetHasKey( unitName ) )
            {
                int count = 0;
                for ( int x = 0; x < UnitsByInternalName[unitName].Count; x++ )
                    count += 1 + UnitsByInternalName[unitName][x].ExtraStackedSquadsInThis;
                return count;
            }
            else
                return 0;
        }

        public CivFleet()
        {
            UnitsByInternalNameRaw = new ArcenSparseLookup<string, List<int>>();
            UnitsByInternalName = new ArcenSparseLookup<string, List<GameEntity_Squad>>();
        }

        public void SerializeTo( ArcenSerializationBuffer Buffer )
        {
            int count = UnitsByInternalNameRaw.GetPairCount();
            Buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
            {
                Buffer.AddString_Condensed( UnitsByInternalNameRaw.GetPairByIndex( x ).Key );
                int subCount = UnitsByInternalNameRaw.GetPairByIndex( x ).Value.Count;
                for ( int y = 0; y < subCount; y++ )
                    Buffer.AddInt32( ReadStyle.NonNeg, UnitsByInternalNameRaw.GetPairByIndex( x ).Value[y] );
            }
        }

        public CivFleet( ArcenDeserializationBuffer Buffer ) : this()
        {
            int count = Buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
            {
                string key = Buffer.ReadString_Condensed();
                UnitsByInternalNameRaw.AddPair( key, new List<int>() );
                int subCount = Buffer.ReadInt32( ReadStyle.NonNeg );
                for ( int y = 0; y < subCount; y++ )
                    UnitsByInternalNameRaw[key].Add( Buffer.ReadInt32( ReadStyle.NonNeg ) );
            }
        }
    }

    public class CivWorldData
    {
        public int Version;

        public List<int> CargoShipsRaw;

        public List<GameEntity_Squad> CargoShips;

        public List<int> TradeEntitiesRaw;

        public List<GameEntity_Squad> TradeEntities;

        public List<int> IndustryEntitiesRaw;

        public List<GameEntity_Squad> IndustryEntities;

        public ArcenSparseLookup<int, int> CargoShipsToBuildRaw;

        public ArcenSparseLookup<GameEntity_Squad, int> CargoShipsToBuild;

        public ArcenSparseLookup<int, CivFleet> CivFleetsRaw;

        public ArcenSparseLookup<GameEntity_Squad, CivFleet> CivFleets;

        public bool IsInCivFleet( GameEntity_Squad entity )
        {
            for ( int x = 0; x < CivFleetsRaw.GetPairCount(); x++ )
            {
                ArcenSparseLookupPair<int, CivFleet> pair = CivFleetsRaw.GetPairByIndex( x );
                if ( pair.Key == entity.MinorFactionStackingID && pair.Value.UnitsByInternalNameRaw.GetHasKey( entity.TypeData.InternalName ) && pair.Value.UnitsByInternalNameRaw[entity.TypeData.InternalName].Contains( entity.PrimaryKeyID ) )
                    return true;
            }
            return false;
        }

        public void AddToCivFleet( int owner, string unitType, int unit )
        {
            if ( !CivFleetsRaw.GetHasKey( owner ) )
                CivFleetsRaw.AddPair( owner, new CivFleet() );
            if ( !CivFleetsRaw[owner].UnitsByInternalNameRaw.GetHasKey( unitType ) )
                CivFleetsRaw[owner].UnitsByInternalNameRaw.AddPair( unitType, new List<int>() );
            CivFleetsRaw[owner].UnitsByInternalNameRaw[unitType].Add( unit );
        }

        public bool GetCanBuildAnother( GameEntity_Squad owner, string unit, bool requestedFromSim )
        {
            CivIndustryData industry;
            if ( requestedFromSim )
                industry = owner.GetIndustrySimSafeNeverNull();
            else
                industry = owner.GetIndustryNotSimSafeMayReturnNull();
            if ( industry == null )
                return false;

            int capacity = industry != null ? industry.Capacity[unit] : 0;

            if ( CivFleets.GetHasKey( owner ) )
                return CivFleets[owner].GetUnitCount( unit ) < capacity;
            else if ( CivFleetsRaw.GetHasKey( owner.PrimaryKeyID ) )
                return CivFleetsRaw[owner.PrimaryKeyID].GetUnitCount( unit ) < capacity;
            else
                return true;
        }

        public CivWorldData()
        {
            CargoShipsRaw = new List<int>();
            CargoShips = new List<GameEntity_Squad>();
            TradeEntitiesRaw = new List<int>();
            TradeEntities = new List<GameEntity_Squad>();
            IndustryEntitiesRaw = new List<int>();
            IndustryEntities = new List<GameEntity_Squad>();
            CargoShipsToBuildRaw = new ArcenSparseLookup<int, int>();
            CargoShipsToBuild = new ArcenSparseLookup<GameEntity_Squad, int>();
            CivFleetsRaw = new ArcenSparseLookup<int, CivFleet>();
            CivFleets = new ArcenSparseLookup<GameEntity_Squad, CivFleet>();
        }

        public void SerializeTo( ArcenSerializationBuffer Buffer )
        {
            Buffer.AddInt32( ReadStyle.NonNeg, 1 );

            int count = CargoShipsRaw.Count;
            Buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
                Buffer.AddInt32( ReadStyle.NonNeg, CargoShipsRaw[x] );

            count = TradeEntitiesRaw.Count;
            Buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
                Buffer.AddInt32( ReadStyle.NonNeg, TradeEntitiesRaw[x] );

            count = IndustryEntitiesRaw.Count;
            Buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
                Buffer.AddInt32( ReadStyle.NonNeg, IndustryEntitiesRaw[x] );

            count = CargoShipsToBuildRaw.GetPairCount();
            Buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
            {
                Buffer.AddInt32( ReadStyle.NonNeg, CargoShipsToBuildRaw.GetPairByIndex( x ).Key );
                Buffer.AddInt32( ReadStyle.NonNeg, CargoShipsToBuildRaw.GetPairByIndex( x ).Value );
            }

            count = CivFleetsRaw.GetPairCount();
            Buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
            {
                Buffer.AddInt32( ReadStyle.NonNeg, CivFleetsRaw.GetPairByIndex( x ).Key );
                CivFleetsRaw.GetPairByIndex( x ).Value.SerializeTo( Buffer );
            }
        }

        public CivWorldData( ArcenDeserializationBuffer Buffer ) : this()
        {
            Version = Buffer.ReadInt32( ReadStyle.NonNeg );

            int count = Buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
                CargoShipsRaw.Add( Buffer.ReadInt32( ReadStyle.NonNeg ) );

            count = Buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
                TradeEntitiesRaw.Add( Buffer.ReadInt32( ReadStyle.NonNeg ) );

            count = Buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
                IndustryEntitiesRaw.Add( Buffer.ReadInt32( ReadStyle.NonNeg ) );

            count = Buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
                CargoShipsToBuildRaw.AddPair( Buffer.ReadInt32( ReadStyle.NonNeg ), Buffer.ReadInt32( ReadStyle.NonNeg ) );

            count = Buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
                CivFleetsRaw.AddPair( Buffer.ReadInt32( ReadStyle.NonNeg ), new CivFleet( Buffer ) );
        }
    }

    public class CivWorldDataExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private CivWorldData Data;

        public static int PatternIndex;

        // So this is essentially what type of thing we're going to 'attach' our class to.
        public static string RelatedParentTypeName = "World";

        public void ReceivePatternIndex( int Index )
        {
            PatternIndex = Index;
        }
        public int GetNumberOfItems()
        {
            return 1;
        }
        public bool GetShouldInitializeOn( string ParentTypeName )
        {
            // Figure out which object type has this sort of ExternalData (in this case, World)
            return ArcenStrings.Equals( ParentTypeName, RelatedParentTypeName );
        }

        public void InitializeData( object ParentObject, object[] Target )
        {
            this.Data = new CivWorldData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer )
        {
            //For saving to disk, translate this object into the buffer
            CivWorldData data = (CivWorldData)Source[0];
            data.SerializeTo( Buffer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            Target[0] = new CivWorldData( Buffer );
        }
    }
    public static class CivWorldDataExtensions
    {
        // This loads the data assigned to whatever ParentObject you pass. So, say, you could assign the same class to different ships, and each would be able to get back the values assigned to it.
        // In our specific case here, we're going to be assigning a dictionary to every faction.
        public static CivWorldData GetCivWorldData( this World ParentObject )
        {
            CivWorldData data = (CivWorldData)ParentObject.ExternalData.GetCollectionByPatternIndex( CivWorldDataExternalData.PatternIndex ).Data[0];
            return data;
        }

        /// <summary>
        /// Returns the CivFleet belonging to an Entity. Returns null if no fleet is found.
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public static CivFleet GetCivFleet( this GameEntity_Squad owner )
        {
            return SKTradeLogicFaction.WorldData == null ? null : !SKTradeLogicFaction.WorldData.CivFleets.GetHasKey( owner ) ? null : SKTradeLogicFaction.WorldData.CivFleets[owner];
        }

        public static void SetCivWorldData( this World ParentObject, CivWorldData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( (int)CivWorldDataExternalData.PatternIndex ).Data[0] = data;
        }
    }
}
