using Arcen.AIW2.Core;
using Arcen.Universal;

namespace PreceptsOfThePrecursors
{
    // Class used to keep track of the Dyson Mothership's stats. Stored on the faction, since each faction will only ever have one Mothership.
    public class AncestorsArksData
    {
        // What Journals have been sent, and what header did we assign it? Helps us figure out when a log is invalid to be sent.
        public ArcenSparseLookup<string, string> JournalEntries;

        public AncestorsArksData()
        {
            JournalEntries = new ArcenSparseLookup<string, string>();
        }
        public AncestorsArksData( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializedIntoSelf( Buffer, false );
        }

        public void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            int count = JournalEntries.GetPairCount();
            buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
            {
                ArcenSparseLookupPair<string, string> pair = JournalEntries.GetPairByIndex( x );
                buffer.AddString_Condensed( pair.Key );
                buffer.AddString_Condensed( pair.Value );
            }
        }
        public void DeserializedIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            if ( IsForPartialSyncDuringMultiplayer )
                DeserializedChangedValuesIntoSelf( buffer );
            else
            {
                int count = buffer.ReadInt32( ReadStyle.NonNeg );
                JournalEntries = new ArcenSparseLookup<string, string>();
                for ( int x = 0; x < count; x++ )
                    JournalEntries.AddPair( buffer.ReadString_Condensed(), buffer.ReadString_Condensed() );
            }
        }
        public void DeserializedChangedValuesIntoSelf( ArcenDeserializationBuffer buffer )
        {
            if ( JournalEntries == null )
                JournalEntries = new ArcenSparseLookup<string, string>();

            int count = buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
            {
                string key = buffer.ReadString_Condensed();
                string value = buffer.ReadString_Condensed();
                if ( JournalEntries.GetHasKey( key ) && JournalEntries[key] != value )
                    JournalEntries[key] = value;
                else
                    JournalEntries.AddPair( key, value );
            }
        }
    }
    public class AncestorsArksExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private AncestorsArksData Data;

        public static int PatternIndex;

        public void ReceivePatternIndex( int Index )
        {
            PatternIndex = Index;
        }
        public int GetNumberOfItems()
        {
            return 1;
        }

        public World ParentWorld;
        public void InitializeData( object ParentObject, object[] Target )
        {
            this.ParentWorld = ParentObject as World;
            if ( this.ParentWorld == null && ParentObject != null )
                ArcenDebugging.ArcenDebugLogSingleLine( "AncestorsArksExternalData: Tried to initialize Parent object as World, but type was " + ParentObject.GetType(), Verbosity.ShowAsError );

            this.Data = new AncestorsArksData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            AncestorsArksData data = (AncestorsArksData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            if ( IsForPartialSyncDuringMultiplayer )
            {
                //this is a partial sync, so use existing object and write into it
                (Target[0] as AncestorsArksData).DeserializedIntoSelf( Buffer, IsForPartialSyncDuringMultiplayer );
            }
            else
            {
                //this is a full sync, so create a new object
                Target[0] = new AncestorsArksData( Buffer );
            }
        }
    }
    public static class AncestorsArksExternalDataExtensions
    {
        // This loads the data assigned to whatever ParentObject you pass. So, say, you could assign the same class to different ships, and each would be able to get back the values assigned to it.
        // In our specific case here, we're going to be assigning a dictionary to every faction.
        public static AncestorsArksData GetAncestorsArksData( this World ParentObject )
        {
            return (AncestorsArksData)ParentObject.ExternalData.GetCollectionByPatternIndex( AncestorsArksExternalData.PatternIndex ).Data[0];
        }
        // This meanwhile saves the data, assigning it to whatever ParentObject you pass.
        public static void SetAncestorsArksData( this World ParentObject, AncestorsArksData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( AncestorsArksExternalData.PatternIndex ).Data[0] = data;
        }
    }

    public class ScrapyardData
    {
        private string[] ships;

        public GameEntityTypeData Alpha { get { return !string.IsNullOrEmpty( ships[0] ) ? GameEntityTypeDataTable.Instance.GetRowByName( ships[0] ) : null; } set { ships[0] = value.InternalName; } }
        public GameEntityTypeData Beta { get { return !string.IsNullOrEmpty( ships[1] ) ? GameEntityTypeDataTable.Instance.GetRowByName( ships[1] ) : null; } set { ships[1] = value.InternalName; } }
        public GameEntityTypeData Gamma { get { return !string.IsNullOrEmpty( ships[2] ) ? GameEntityTypeDataTable.Instance.GetRowByName( ships[2] ) : null; } set { ships[2] = value.InternalName; } }

        public ScrapyardData()
        {
            ships = new string[3];
        }
        public ScrapyardData( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializedIntoSelf( Buffer, false );
        }

        public void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            byte count = (byte)ships.Length;
            buffer.AddByte( ReadStyleByte.Normal, count );
            for ( byte x = 0; x < count; x++ )
                buffer.AddString_Condensed( ships[x] );
        }
        public void DeserializedIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            if ( IsForPartialSyncDuringMultiplayer )
                DeserializedChangedValuesIntoSelf( buffer );
            else
            {
                ships = new string[3];
                byte count = buffer.ReadByte( ReadStyleByte.Normal );
                for ( byte x = 0; x < count; x++ )
                    ships[x] = buffer.ReadString_Condensed();
            }
        }
        public void DeserializedChangedValuesIntoSelf( ArcenDeserializationBuffer buffer )
        {
            byte count = buffer.ReadByte( ReadStyleByte.Normal );
            if ( ships == null || ships.Length != count )
                ships = new string[count];

            for ( byte x = 0; x < count; x++ )
            {
                string value = buffer.ReadString_Condensed();
                if ( ships[x] != value )
                    ships[x] = value;
            }
        }
    }
    public class ScrapyardExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private ScrapyardData Data;

        public static int PatternIndex;
        public void ReceivePatternIndex( int Index )
        {
            PatternIndex = Index; //for internal use with the ExternalData code in the game engine itself
        }
        public int GetNumberOfItems()
        {
            return 1; //for internal use with the ExternalData code in the game engine itself
        }

        public GameEntity_Squad ParentSquad;
        public void InitializeData( object ParentObject, object[] Target )
        {
            this.ParentSquad = ParentObject as GameEntity_Squad;
            if ( this.ParentSquad == null && ParentObject != null )
                return;

            //this initialization is handled by the data structure itself
            this.Data = new ScrapyardData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            ScrapyardData data = (ScrapyardData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            if ( IsForPartialSyncDuringMultiplayer )
            {
                //this is a partial sync, so use existing object and write into it
                (Target[0] as ScrapyardData).DeserializedIntoSelf( Buffer, IsForPartialSyncDuringMultiplayer );
            }
            else
            {
                //this is a full sync, so create a new object
                Target[0] = new ScrapyardData( Buffer );
            }
        }
    }
    public static class ScrapyardExternalDataExtensions
    {
        // This loads the data assigned to whatever ParentObject you pass. So, say, you could assign the same class to different ships, and each would be able to get back the values assigned to it.
        // In our specific case here, we're going to be assigning a dictionary to every faction.
        public static ScrapyardData GetScrapyardData( this GameEntity_Squad ParentObject )
        {
            return (ScrapyardData)ParentObject.ExternalData.GetCollectionByPatternIndex( ScrapyardExternalData.PatternIndex ).Data[0];
        }
        // This meanwhile saves the data, assigning it to whatever ParentObject you pass.
        public static void SetScrapyardData( this GameEntity_Squad ParentObject, ScrapyardData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( ScrapyardExternalData.PatternIndex ).Data[0] = data;
        }
    }
}
