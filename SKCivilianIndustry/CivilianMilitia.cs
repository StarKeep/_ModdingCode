using System.Collections.Generic;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace SKCivilianIndustry
{
    /// <summary>
    /// Used on militia fleets. Tells us what their focus is.
    /// </summary>
    public class CivilianMilitia : ArcenExternalSubManagedData
    {
        public int Version;

        /// <summary>
        /// The leading unit for this Militia group. If -1, its whatever entity has this data assigned.
        /// </summary>
        public int Centerpiece;

        public CivilianMilitiaStatus Status;

        /// <summary>
        /// If patrolling, this determins if it should or shouldn't have its units docked within.
        /// Occurs if there is no threat that it can currently deal with nearby.
        /// </summary>
        public bool AtEase;

        public short PlanetFocus;

        public int EntityFocus;

        // GameEntityTypeData that this militia builds, a list of every ship of that type under their control, and their capacity.
        public ArcenSparseLookup<int, string> ShipTypeDataNames;
        public ArcenSparseLookup<int, GameEntityTypeData> ShipTypeData;
        public ArcenSparseLookup<int, List<int>> Ships;
        public ArcenSparseLookup<int, int> ShipCapacity;

        // Units currently stored inside of the base.
        public ArcenSparseLookup<int, int> StoredShips;

        // Multipliers for various things.
        public int CostMultiplier;
        public int CapMultiplier;

        /// <summary>
        /// Count the number of ships of a certain type that this militia controls.
        /// If called from a SimSafe thread, this will also handle purging of units as needed.
        /// </summary>
        /// <param name="typeData" >Type Data of the Entity to count.</param>
        /// <returns></returns>
        public int GetShipCount( GameEntityTypeData typeData, bool calledFromSimSafeThread = true )
        {
            int index = -1;
            ShipTypeData.DoFor( pair =>
            {
                if ( typeData == pair.Value )
                {
                    index = pair.Key;
                    return DelReturn.Break;
                }

                return DelReturn.Continue;
            } );
            if ( index == -1 )
                return 0;
            int shipCount = 0;
            for ( int x = 0; x < Ships[index].Count; x++ )
            {
                GameEntity_Squad squad = World_AIW2.Instance.GetEntityByID_Squad( Ships[index][x] );
                if ( squad == null )
                {
                    if ( calledFromSimSafeThread )
                    {
                        Ships[index].RemoveAt( x );
                        x--;
                    }
                    continue;
                }
                shipCount++;
                shipCount += squad.ExtraStackedSquadsInThis;
            }
            shipCount += StoredShips[index];
            return shipCount;
        }

        // Following three functions are used for initializing, saving, and loading data.
        // Initialization function.
        // Default values. Called on creation, NOT on load.
        public CivilianMilitia()
        {
            ShipTypeDataNames = new ArcenSparseLookup<int, string>();
            ShipTypeData = new ArcenSparseLookup<int, GameEntityTypeData>();
            Ships = new ArcenSparseLookup<int, List<int>>();
            ShipCapacity = new ArcenSparseLookup<int, int>();
            StoredShips = new ArcenSparseLookup<int, int>();

            this.Centerpiece = -1;
            this.Status = CivilianMilitiaStatus.Idle;
            this.AtEase = false;
            this.PlanetFocus = -1;
            this.EntityFocus = -1;
            for ( int x = 0; x < (int)CivilianResource.Length; x++ )
            {
                this.ShipTypeDataNames.AddPair( x, "none" );
                this.Ships.AddPair( x, new List<int>() );
                this.ShipCapacity.AddPair( x, 0 );
                this.StoredShips.AddPair( x, 0 );
            }
            this.CostMultiplier = 100; // 100%
            this.CapMultiplier = 100; // 100%
        }

        public CivilianMilitia( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializeIntoSelf( Buffer, false );
        }

        public override void SerializeTo( ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Buffer.AddInt32( ReadStyle.NonNeg, 4 );
            Buffer.AddInt32( ReadStyle.Signed, this.Centerpiece );
            Buffer.AddByte( ReadStyleByte.Normal, (byte)this.Status );
            Buffer.AddItem( this.AtEase );
            Buffer.AddInt16( ReadStyle.Signed, this.PlanetFocus );
            Buffer.AddInt32( ReadStyle.Signed, this.EntityFocus );
            int count = (int)CivilianResource.Length;
            Buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
            {
                Buffer.AddString_Condensed( this.ShipTypeDataNames[x] );
                int subCount = this.Ships[x].Count;
                Buffer.AddInt32( ReadStyle.NonNeg, subCount );
                for ( int y = 0; y < subCount; y++ )
                    Buffer.AddInt32( ReadStyle.PosExceptNeg1, this.Ships[x][y] );
                Buffer.AddInt32( ReadStyle.PosExceptNeg1, this.ShipCapacity[x] );
                Buffer.AddInt32( ReadStyle.PosExceptNeg1, this.StoredShips[x] );
            }
            Buffer.AddInt32( ReadStyle.NonNeg, this.CostMultiplier );
            Buffer.AddInt32( ReadStyle.NonNeg, this.CapMultiplier );
        }

        public override void DeserializeIntoSelf( ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            if ( ShipTypeDataNames == null )
                ShipTypeDataNames = new ArcenSparseLookup<int, string>();
            if ( ShipTypeData == null )
                ShipTypeData = new ArcenSparseLookup<int, GameEntityTypeData>();
            if ( Ships == null )
                Ships = new ArcenSparseLookup<int, List<int>>();
            if ( ShipCapacity == null )
                ShipCapacity = new ArcenSparseLookup<int, int>();
            if ( StoredShips == null )
                StoredShips = new ArcenSparseLookup<int, int>();

            if ( IsForPartialSyncDuringMultiplayer )
            {
                this.ShipTypeDataNames.Clear();
                this.ShipTypeData.Clear();
                this.Ships.Clear();
                this.ShipCapacity.Clear();
            }

            this.Version = Buffer.ReadInt32( ReadStyle.NonNeg );
            this.Centerpiece = Buffer.ReadInt32( ReadStyle.Signed );
            this.Status = (CivilianMilitiaStatus)Buffer.ReadByte( ReadStyleByte.Normal );
            if ( this.Version < 3 )
                this.AtEase = false;
            else
                this.AtEase = Buffer.ReadBool();
            if ( this.Version < 2 )
                this.PlanetFocus = (short)Buffer.ReadInt32( ReadStyle.Signed );
            else
                this.PlanetFocus = Buffer.ReadInt16( ReadStyle.Signed );
            this.EntityFocus = Buffer.ReadInt32( ReadStyle.Signed );
            int count = Buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
            {
                this.ShipTypeDataNames[x] = Buffer.ReadString_Condensed();
                this.Ships[x] = new List<int>();

                int subCount = Buffer.ReadInt32( ReadStyle.NonNeg );

                for ( int y = 0; y < subCount; y++ )
                    if ( this.Version < 4 )
                        this.Ships[x].Add( Buffer.ReadInt32( ReadStyle.NonNeg ) );
                    else
                        this.Ships[x].Add( Buffer.ReadInt32( ReadStyle.PosExceptNeg1 ) );

                if ( this.Version < 4 )
                    this.ShipCapacity[x] = Buffer.ReadInt32( ReadStyle.NonNeg );
                else
                    this.ShipCapacity[x] = Buffer.ReadInt32( ReadStyle.PosExceptNeg1 );

                if ( this.Version < 3 )
                    this.StoredShips[x] = 0;
                else if ( this.Version < 4 )
                    this.StoredShips[x] = Buffer.ReadInt32( ReadStyle.NonNeg );
                else
                    this.StoredShips[x] = Buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            }
            if ( this.ShipTypeDataNames.GetPairCount() < (int)CivilianResource.Length )
            {
                for ( int x = count; x < (int)CivilianResource.Length; x++ )
                {
                    this.ShipTypeDataNames.AddPair( x, "none" );
                    this.Ships.AddPair( x, new List<int>() );
                    this.ShipCapacity.AddPair( x, 0 );
                }
            }
            this.CostMultiplier = Buffer.ReadInt32( ReadStyle.NonNeg );
            this.CapMultiplier = Buffer.ReadInt32( ReadStyle.NonNeg );
        }

        public GameEntity_Squad getMine()
        {
            return World_AIW2.Instance.GetEntityByID_Squad( this.EntityFocus );
        }

        public GameEntity_Other getWormhole()
        {
            return World_AIW2.Instance.GetEntityByID_Other( this.EntityFocus );
        }
    }
}
