using Arcen.AIW2.Core;
using Arcen.Universal;

namespace PreceptsOfThePrecursors
{
    // Class used to keep track of the Dyson Mothership's stats. Stored on the faction, since each faction will only ever have one Mothership.
    public class DysonMothershipData
    {
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
            this.DeserializedIntoSelf( Buffer, false );
        }

        public void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            buffer.AddByte( ReadStyleByte.Normal, Level );
            buffer.AddInt32( ReadStyle.NonNeg, Resources );
            buffer.AddInt16( ReadStyle.NonNeg, Mines );
            buffer.AddInt32( ReadStyle.Signed, SecondsUntilRespawn );
            buffer.AddInt32( ReadStyle.NonNeg, HullWhenEnteredPlanet );
            buffer.AddItem( IsNearMine );
            buffer.AddInt32( ReadStyle.PosExceptNeg1, LastGameSecondSpawnt );
            Trust.SerializeTo( buffer );
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
        public void DeserializedIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            if ( IsForPartialSyncDuringMultiplayer )
                DeserializedChangedValuesIntoSelf( buffer );
            else
            {
                if ( JournalEntries == null )
                    JournalEntries = new ArcenSparseLookup<string, string>();

                Level = buffer.ReadByte( ReadStyleByte.Normal );
                Resources = buffer.ReadInt32( ReadStyle.NonNeg );
                Mines = buffer.ReadInt16( ReadStyle.NonNeg );
                SecondsUntilRespawn = buffer.ReadInt32( ReadStyle.Signed );
                HullWhenEnteredPlanet = buffer.ReadInt32( ReadStyle.NonNeg );
                IsNearMine = buffer.ReadBool();
                LastGameSecondSpawnt = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
                Trust = new DysonTrust( buffer );
                int count = buffer.ReadInt32( ReadStyle.NonNeg );
                JournalEntries = new ArcenSparseLookup<string, string>();
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
        public void DeserializedChangedValuesIntoSelf( ArcenDeserializationBuffer buffer )
        {
            byte readByte = buffer.ReadByte( ReadStyleByte.Normal );
            Level = Level != readByte ? readByte : Level;

            int readInt = buffer.ReadInt32( ReadStyle.NonNeg );
            Resources = Resources != readInt ? readInt : Resources;

            short readShort = buffer.ReadInt16( ReadStyle.NonNeg );
            Mines = Mines != readShort ? readShort : Mines;

            readInt = buffer.ReadInt32( ReadStyle.Signed );
            SecondsUntilRespawn = SecondsUntilRespawn != readInt ? readInt : SecondsUntilRespawn;

            readInt = buffer.ReadInt32( ReadStyle.NonNeg );
            HullWhenEnteredPlanet = HullWhenEnteredPlanet != readInt ? readInt : HullWhenEnteredPlanet;

            bool readBool = buffer.ReadBool();
            IsNearMine = IsNearMine != readBool ? readBool : IsNearMine;

            readInt = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            LastGameSecondSpawnt = LastGameSecondSpawnt != readInt ? readInt : LastGameSecondSpawnt;

            if ( Trust == null )
                Trust = new DysonTrust( buffer );
            else
                Trust.PartialSync( buffer );

            if ( JournalEntries == null )
                JournalEntries = new ArcenSparseLookup<string, string>();

            int count = buffer.ReadInt32( ReadStyle.NonNeg );
            for(int x = 0; x < count; x++ )
            {
                string key = buffer.ReadString_Condensed();
                string value = buffer.ReadString_Condensed();
                if ( !JournalEntries.GetHasKey( key ) )
                    JournalEntries.AddPair( key, value );
                else if ( JournalEntries[key] != value )
                    JournalEntries[key] = value;
            }

            readBool = buffer.ReadBool();
            ReadyToMoveOn = ReadyToMoveOn != readBool ? readBool : ReadyToMoveOn;

            readInt = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            planetToBuildOn = planetToBuildOn != readInt ? readInt : planetToBuildOn;

            readInt = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            LastWaveGameSecond = LastWaveGameSecond != readInt ? readInt : LastWaveGameSecond;
        }
    }
    public class DysonMothershipExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private DysonMothershipData Data;

        public static int PatternIndex;

        public void ReceivePatternIndex( int Index )
        {
            PatternIndex = Index; //for internal use with the ExternalData code in the game engine itself
        }
        public int GetNumberOfItems()
        {
            return 1; //for internal use with the ExternalData code in the game engine itself
        }

        public Faction ParentFaction;
        public void InitializeData( object ParentObject, object[] Target )
        {
            this.ParentFaction = ParentObject as Faction;
            if ( this.ParentFaction == null && ParentObject != null )
                return;

            //this initialization is handled by the data structure itself
            this.Data = new DysonMothershipData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            DysonMothershipData data = (DysonMothershipData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            if ( IsForPartialSyncDuringMultiplayer )
            {
                //this is a partial sync, so use existing object and write into it
                (Target[0] as DysonMothershipData).DeserializedIntoSelf( Buffer, IsForPartialSyncDuringMultiplayer );
            }
            else
            {
                //this is a full sync, so create a new object
                Target[0] = new DysonMothershipData( Buffer );
            }
        }
    }
    public static class DysonMothershipExternalDataExtensions
    {
        // This loads the data assigned to whatever ParentObject you pass. So, say, you could assign the same class to different ships, and each would be able to get back the values assigned to it.
        // In our specific case here, we're going to be assigning a dictionary to every faction.
        public static DysonMothershipData GetMothershipData( this Faction ParentObject )
        {
            return (DysonMothershipData)ParentObject.ExternalData.GetCollectionByPatternIndex( DysonMothershipExternalData.PatternIndex ).Data[0];
        }
        // This meanwhile saves the data, assigning it to whatever ParentObject you pass.
        public static void SetMothershipData( this Faction ParentObject, DysonMothershipData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( DysonMothershipExternalData.PatternIndex ).Data[0] = data;
        }
    }

    // Class used to store information about Proto-Spheres. Stored on the Planet since each planet can only ever have a singular Proto-Sphere.
    public class DysonProtoSphereData
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

        // When did the BigUnit die? - Purged
        public int GameSecondBigUnitDied;

        // Have we been hacked?
        public bool HasBeenHacked;

        public DysonProtoSphereData()
        {
            Level = 0;
            Resources = 0;
            Type = ProtoSphereType.None;
            GameSecondBigUnitDied = 0;
            HasBeenHacked = false;
        }
        public DysonProtoSphereData( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializedIntoSelf( Buffer, false );
        }

        public void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            buffer.AddByte( ReadStyleByte.Normal, Level );
            buffer.AddInt32( ReadStyle.PosExceptNeg1, Resources );
            buffer.AddByte( ReadStyleByte.Normal, (byte)Type );
            buffer.AddInt32( ReadStyle.PosExceptNeg1, -1 );
            buffer.AddInt32( ReadStyle.PosExceptNeg1, GameSecondBigUnitDied );
            buffer.AddItem( HasBeenHacked );
        }
        public void DeserializedIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            if ( IsForPartialSyncDuringMultiplayer )
                DeserializedChangedValuesIntoSelf( buffer );
            {
                Level = buffer.ReadByte( ReadStyleByte.Normal );
                Resources = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
                Type = (ProtoSphereType)buffer.ReadByte( ReadStyleByte.Normal );
                buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
                GameSecondBigUnitDied = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
                HasBeenHacked = buffer.ReadBool();
            }
        }
        public void DeserializedChangedValuesIntoSelf( ArcenDeserializationBuffer buffer )
        {
            byte readByte = buffer.ReadByte( ReadStyleByte.Normal );
            Level = Level != readByte ? readByte : Level;

            int readInt = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            Resources = Resources != readInt ? readInt : Resources;

            readByte = buffer.ReadByte( ReadStyleByte.Normal );
            ProtoSphereType type = (ProtoSphereType)readByte;
            Type = Type != type ? type : Type;

            buffer.ReadInt32( ReadStyle.PosExceptNeg1 );

            readInt = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            GameSecondBigUnitDied = GameSecondBigUnitDied != readInt ? readInt : GameSecondBigUnitDied;

            bool readBool = buffer.ReadBool();
            HasBeenHacked = HasBeenHacked != readBool ? readBool : HasBeenHacked;
        }
    }

    public class DysonProtoSphereExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private DysonProtoSphereData Data;

        public static int PatternIndex;
        public void ReceivePatternIndex( int Index )
        {
            PatternIndex = Index; //for internal use with the ExternalData code in the game engine itself
        }
        public int GetNumberOfItems()
        {
            return 1; //for internal use with the ExternalData code in the game engine itself
        }

        public Planet ParentPlanet;
        public void InitializeData( object ParentObject, object[] Target )
        {
            this.ParentPlanet = ParentObject as Planet;
            if ( this.ParentPlanet == null && ParentObject != null )
                return;

            //this initialization is handled by the data structure itself
            this.Data = new DysonProtoSphereData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            DysonProtoSphereData data = (DysonProtoSphereData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            if ( IsForPartialSyncDuringMultiplayer )
            {
                //this is a partial sync, so use existing object and write into it
                (Target[0] as DysonProtoSphereData).DeserializedIntoSelf( Buffer, IsForPartialSyncDuringMultiplayer );
            }
            else
            {
                //this is a full sync, so create a new object
                Target[0] = new DysonProtoSphereData( Buffer );
            }
        }
    }
    public static class DysonProtoSphereExternalDataExtensions
    {
        // This loads the data assigned to whatever ParentObject you pass. So, say, you could assign the same class to different ships, and each would be able to get back the values assigned to it.
        // In our specific case here, we're going to be assigning a dictionary to every faction.
        public static DysonProtoSphereData GetProtoSphereData( this Planet ParentObject )
        {
            return (DysonProtoSphereData)ParentObject.ExternalData.GetCollectionByPatternIndex( DysonProtoSphereExternalData.PatternIndex ).Data[0];
        }
        // This meanwhile saves the data, assigning it to whatever ParentObject you pass.
        public static void SetProtoSphereData( this Planet ParentObject, DysonProtoSphereData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( DysonProtoSphereExternalData.PatternIndex ).Data[0] = data;
        }
    }
}
