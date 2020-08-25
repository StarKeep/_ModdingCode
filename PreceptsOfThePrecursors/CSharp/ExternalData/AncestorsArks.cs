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
        public void SerializeTo( ArcenSerializationBuffer buffer )
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
        public AncestorsArksData( ArcenDeserializationBuffer buffer )
        {
            int count = buffer.ReadInt32( ReadStyle.NonNeg );
            JournalEntries = new ArcenSparseLookup<string, string>();
            for ( int x = 0; x < count; x++ )
                JournalEntries.AddPair( buffer.ReadString_Condensed(), buffer.ReadString_Condensed() );
        }
    }
    public class AncestorsArksExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private AncestorsArksData Data;

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
            // Figure out which object type has this sort of ExternalData (in this case, Faction)
            return ArcenStrings.Equals( ParentTypeName, RelatedParentTypeName );
        }

        public void InitializeData( object ParentObject, object[] Target )
        {
            this.Data = new AncestorsArksData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer )
        {
            //For saving to disk, translate this object into the buffer
            AncestorsArksData data = (AncestorsArksData)Source[0];
            data.SerializeTo( Buffer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            Target[0] = new AncestorsArksData( Buffer );
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
        public void SerializeTo( ArcenSerializationBuffer buffer )
        {
            byte count = (byte)ships.Length;
            buffer.AddByte( ReadStyleByte.Normal, count );
            for ( byte x = 0; x < count; x++ )
                buffer.AddString_Condensed( ships[x] );
        }
        public ScrapyardData( ArcenDeserializationBuffer buffer ) : this()
        {
            byte count = buffer.ReadByte( ReadStyleByte.Normal );
            for ( byte x = 0; x < count; x++ )
                ships[x] = buffer.ReadString_Condensed();
        }
    }
    public class ScrapyardExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private ScrapyardData Data;

        public static int PatternIndex;

        // So this is essentially what type of thing we're going to 'attach' our class to.
        public static string RelatedParentTypeName = "GameEntity_Squad";

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
            // Figure out which object type has this sort of ExternalData (in this case, Faction)
            return ArcenStrings.Equals( ParentTypeName, RelatedParentTypeName );
        }

        public void InitializeData( object ParentObject, object[] Target )
        {
            this.Data = new ScrapyardData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer )
        {
            //For saving to disk, translate this object into the buffer
            ScrapyardData data = (ScrapyardData)Source[0];
            data.SerializeTo( Buffer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            Target[0] = new ScrapyardData( Buffer );
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
