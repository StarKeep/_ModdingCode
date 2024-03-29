﻿using Arcen.AIW2.Core;
using Arcen.Universal;

namespace PreceptsOfThePrecursors
{
    // Class used to keep track of the Dyson Mothership's stats. Stored on the faction, since each faction will only ever have one Mothership.
    public class DysonMothershipData : ArcenExternalSubManagedData
    {
        public int Version;

        public byte Level; // Current level. Maxes out at 7.
        public int Resources; // Resources claimed so far. Generated from kills, when near mines, and passively from consumed mines.
        public short Mines; // Mines eaten so far.
        public int SecondsUntilRespawn; // How long until it respawns, set after death.

        // To figure out how much trust we should gain or lose, keep track of our hull rating.
        // As we take damage on a player-controlled planet, lose trust in them.
        public int HullWhenEnteredPlanet;

        // Set to true when the Mothership is near a mine, for the purpose of the description appender
        public bool IsNearMine;

        // When was the latest Mothership spawnt? For AI response strength.
        public int LastGameSecondSpawnt;
        public int GameSecondsSinceLastSpawnt { get { return World_AIW2.Instance.GameSecond - LastGameSecondSpawnt; } }

        // Set to true when we've finished our 'leaving the planet' logic.
        public bool ReadyToMoveOn;

        // The planet we're currently gathering resources to build a proto-sphere on.
        private int planetToBuildOn;
        public Planet PlanetToBuildOn { get { return World_AIW2.Instance.GetPlanetByIndex( false, planetToBuildOn ); } set { if ( value == null ) planetToBuildOn = -1; else planetToBuildOn = value.Index; } }

        // The last GameSecond that a wave was sent.
        public int LastWaveGameSecond;
        public int GameSecondsSinceLastWave { get { return World_AIW2.Instance.GameSecond - LastWaveGameSecond; } }

        // Following are set to true as needed, and are purely used for the description. Will never require saving to disk.
        public bool IsGainingTrust;
        public bool IsLosingTrust;
        public int MetalGainedOrLostLastSecond;

        // How much the Mothership trusts each planet.
        public DysonTrust Trust;

        // What Journals have been sent, and what header did we assign it? Helps us figure out when a log is invalid to be sent.
        public ArcenSparseLookup<string, string> JournalEntries;

        public DysonMothershipData()
        {
            Level = 1;
            Resources = 0;
            Mines = 0;
            SecondsUntilRespawn = -1;
            HullWhenEnteredPlanet = 0;
            IsNearMine = false;
            LastGameSecondSpawnt = 0;
            Trust = new DysonTrust();
            JournalEntries = new ArcenSparseLookup<string, string>();
            ReadyToMoveOn = false;
            PlanetToBuildOn = null;
            LastWaveGameSecond = 0;

            IsGainingTrust = false;
            IsLosingTrust = false;
            MetalGainedOrLostLastSecond = 0;
        }
        public DysonMothershipData( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializeIntoSelf( Buffer, false );
        }

        public override void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            buffer.AddInt32( ReadStyle.NonNeg, 1 );
            buffer.AddByte( ReadStyleByte.Normal, Level );
            buffer.AddInt32( ReadStyle.NonNeg, Resources );
            buffer.AddInt16( ReadStyle.NonNeg, Mines );
            buffer.AddInt32( ReadStyle.Signed, SecondsUntilRespawn );
            buffer.AddInt32( ReadStyle.NonNeg, HullWhenEnteredPlanet );
            buffer.AddItem( IsNearMine );
            buffer.AddInt32( ReadStyle.PosExceptNeg1, LastGameSecondSpawnt );
            Trust.SerializeTo( buffer, IsForPartialSyncDuringMultiplayer );
            int count = JournalEntries.GetPairCount();
            buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
            {
                ArcenSparseLookupPair<string, string> pair = JournalEntries.GetPairByIndex( x );
                buffer.AddString_Condensed( pair.Key );
                buffer.AddString_Condensed( pair.Value );
            }

            buffer.AddItem( ReadyToMoveOn );
            buffer.AddInt32( ReadStyle.PosExceptNeg1, planetToBuildOn );
            buffer.AddInt32( ReadStyle.PosExceptNeg1, LastWaveGameSecond );
        }
        public override void DeserializeIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Version = buffer.ReadInt32( ReadStyle.NonNeg );

            if ( JournalEntries == null )
                JournalEntries = new ArcenSparseLookup<string, string>();
            else if ( IsForPartialSyncDuringMultiplayer )
                JournalEntries.Clear();

            Level = buffer.ReadByte( ReadStyleByte.Normal );
            Resources = buffer.ReadInt32( ReadStyle.NonNeg );
            Mines = buffer.ReadInt16( ReadStyle.NonNeg );
            SecondsUntilRespawn = buffer.ReadInt32( ReadStyle.Signed );
            HullWhenEnteredPlanet = buffer.ReadInt32( ReadStyle.NonNeg );
            IsNearMine = buffer.ReadBool();
            LastGameSecondSpawnt = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );

            if ( Trust == null )
                Trust = new DysonTrust();

            Trust.DeserializeIntoSelf( buffer, IsForPartialSyncDuringMultiplayer );

            int count = buffer.ReadInt32( ReadStyle.NonNeg );

            for ( int x = 0; x < count; x++ )
                JournalEntries.AddPair( buffer.ReadString_Condensed(), buffer.ReadString_Condensed() );

            ReadyToMoveOn = buffer.ReadBool();
            planetToBuildOn = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            LastWaveGameSecond = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );

            IsGainingTrust = false;
            IsLosingTrust = false;
            MetalGainedOrLostLastSecond = 0;
        }
    }
    public class DysonMothershipExternalData : ArcenExternalDataPatternImplementationBase_Faction
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private DysonMothershipData Data;

        public static int PatternIndex;

        public override void ReceivePatternIndex( int Index )
        {
            PatternIndex = Index;
        }
        public override int GetNumberOfItems()
        {
            return 1;
        }

        protected override void InitializeData( Faction Parent, object[] Target )
        {
            this.Data = new DysonMothershipData();
            Target[0] = this.Data;
        }
        public override void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            DysonMothershipData data = (DysonMothershipData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        protected override void DeserializeExternalData( Faction Parent, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            this.DeserializeExternalDataAsArcenExternalSubManagedData<DysonMothershipData>( Target, Buffer, IsForPartialSyncDuringMultiplayer );
        }
    }
    public static class DysonMothershipExternalDataExtensions
    {
        public static DysonMothershipData GetMothershipData( this Faction ParentObject, ExternalDataRetrieval RetrievalRules )
        {
            ArcenExternalData extData = ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, DysonMothershipExternalData.PatternIndex, RetrievalRules );
            if ( extData == null )
                return null;
            return (DysonMothershipData)extData.Data[0];
        }
        // This meanwhile saves the data, assigning it to whatever ParentObject you pass.
        public static void SetMothershipData( this Faction ParentObject, DysonMothershipData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, (int)DysonMothershipExternalData.PatternIndex, ExternalDataRetrieval.CreateIfNotFound ).Data[0] = data;
        }
    }

    // World class used to keep track of all planetary proto sphere data.
    public class DysonPrecursorsWorldData : ArcenExternalSubManagedData
    {
        public int Version;

        private ArcenSparseLookup<short, DysonPerPlanetData> SphereDataByPlanet;

        public DysonPerPlanetData GetPrecursorPerPlanetData( Planet planet )
        {
            if ( !SphereDataByPlanet.GetHasKey( planet.Index ) )
                SphereDataByPlanet.AddPair( planet.Index, new DysonPerPlanetData() );
            return SphereDataByPlanet[planet.Index];
        }

        public void SetPrecursorPerPlanetData( Planet planet, DysonPerPlanetData data )
        {
            if ( SphereDataByPlanet.GetHasKey( planet.Index ) )
                SphereDataByPlanet[planet.Index] = data;
            else
                SphereDataByPlanet.AddPair( planet.Index, data );
        }

        public DysonPrecursorsWorldData()
        {
            SphereDataByPlanet = new ArcenSparseLookup<short, DysonPerPlanetData>();
        }

        public DysonPrecursorsWorldData( ArcenDeserializationBuffer Buffer ) : this()
        {
            DeserializeIntoSelf( Buffer, false );
        }

        public override void SerializeTo( ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Buffer.AddInt32(ReadStyle.NonNeg, 1 );

            int count = SphereDataByPlanet.GetPairCount();
            Buffer.AddInt32( ReadStyle.NonNeg, count );
            SphereDataByPlanet.DoFor( pair =>
            {
                Buffer.AddInt16( ReadStyle.NonNeg, pair.Key );
                pair.Value.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );

                return DelReturn.Continue;
            } );
        }

        public override void DeserializeIntoSelf( ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Version = Buffer.ReadInt32(ReadStyle.NonNeg );

            if ( SphereDataByPlanet == null )
                SphereDataByPlanet = new ArcenSparseLookup<short, DysonPerPlanetData>();
            else if ( IsForPartialSyncDuringMultiplayer )
                SphereDataByPlanet.Clear();

            int count = Buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
                SphereDataByPlanet.AddPair( Buffer.ReadInt16( ReadStyle.NonNeg ), new DysonPerPlanetData( Buffer ) );
        }
    }

    public class DysonPerPlanetData : ArcenExternalSubManagedData
    {
        public enum ProtoSphereType
        {
            None,       // Set at start, and if the ProtoSphere is killed.
            Protecter,  // Used to coexist with trusted planets.
            Suppressor, // Used to shut down untrusted planets.
            Other       // Set if the planet is controlled or influenced by a non-proto subfaction.
        }

        // Current level of the ProtoSphere. Maxes out at 7 with an extra effect based on type.
        // Protectors spawn a Seedling, which is the equivilent of a Mark 4 Mothership, and grant it to the Player.
        // Suppressors spawn an Enforcer, which is the equivilent of a Mark 4 Mothership, and use them to take the fight directly to the AI.
        public byte Level { get; set; }

        // Current resource for the ProtoSphere.
        // Increased by Time and Kills.
        // Decreased by Hacks and Kills.
        public int Resources;

        // Type of Protosphere.
        public ProtoSphereType Type;

        // When did this planet last build a structure?
        public int GameSecondLastStructureBuilt;
        public int SecondsSinceLastStructureBuilt => World_AIW2.Instance.GameSecond - GameSecondLastStructureBuilt;

        // Have we been hacked?
        public bool HasBeenHacked;

        public DysonPerPlanetData()
        {
            Level = 0;
            Resources = 0;
            Type = ProtoSphereType.None;
            GameSecondLastStructureBuilt = -1;
            HasBeenHacked = false;
        }
        public DysonPerPlanetData( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializeIntoSelf( Buffer, false );
        }

        public override void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            buffer.AddByte( ReadStyleByte.Normal, Level );
            buffer.AddInt32( ReadStyle.PosExceptNeg1, Resources );
            buffer.AddByte( ReadStyleByte.Normal, (byte)Type );
            buffer.AddInt32( ReadStyle.PosExceptNeg1, -1 );
            buffer.AddInt32( ReadStyle.PosExceptNeg1, GameSecondLastStructureBuilt );
            buffer.AddItem( HasBeenHacked );
        }
        public override void DeserializeIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Level = buffer.ReadByte( ReadStyleByte.Normal );
            Resources = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            Type = (ProtoSphereType)buffer.ReadByte( ReadStyleByte.Normal );
            buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            GameSecondLastStructureBuilt = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            HasBeenHacked = buffer.ReadBool();
        }
    }

    public class DysonPrecursorsWorldExternalData : ArcenExternalDataPatternImplementationBase_World
    {
        private DysonPrecursorsWorldData Data;
        public static int PatternIndex;

        public override void ReceivePatternIndex( int Index )
        {
            PatternIndex = Index;
        }
        public override int GetNumberOfItems()
        {
            return 1;
        }

        protected override void InitializeData( World Parent, object[] Target )
        {
            this.Data = new DysonPrecursorsWorldData();
            Target[0] = this.Data;
        }
        public override void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            DysonPrecursorsWorldData data = (DysonPrecursorsWorldData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        protected override void DeserializeExternalData( World Parent, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            this.DeserializeExternalDataAsArcenExternalSubManagedData<DysonPrecursorsWorldData>( Target, Buffer, IsForPartialSyncDuringMultiplayer );
        }
    }
    public static class DysonProtoSphereExternalDataExtensions
    {
        public static DysonPrecursorsWorldData GetProtoSphereWorldData( this World ParentObject, ExternalDataRetrieval RetrievalRules )
        {
            ArcenExternalData extData = ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, DysonPrecursorsWorldExternalData.PatternIndex, RetrievalRules );
            if ( extData == null )
                return null;
            return (DysonPrecursorsWorldData)extData.Data[0];
        }
        public static void SetProtoSphereWorldData( this World ParentObject, DysonPrecursorsWorldData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, (int)DysonPrecursorsWorldExternalData.PatternIndex, ExternalDataRetrieval.CreateIfNotFound ).Data[0] = data;
        }

        public static DysonPerPlanetData GetPrecursorPerPlanetData( this Planet planet, ExternalDataRetrieval RetrievalRules )
        {
            return World.Instance.GetProtoSphereWorldData( RetrievalRules )?.GetPrecursorPerPlanetData( planet );
        }
        /// <summary>
        /// ONLY call from a sim-safe thread.
        /// </summary>
        /// <param name="planet"></param>
        /// <param name="sphereData"></param>
        public static void SetPrecursorPerPlanetData( this Planet planet, DysonPerPlanetData sphereData )
        {
            World.Instance.GetProtoSphereWorldData( ExternalDataRetrieval.CreateIfNotFound ).SetPrecursorPerPlanetData( planet, sphereData );
        }
    }
}
