using Arcen.AIW2.Core;
using Arcen.Universal;

namespace PreceptsOfThePrecursors
{
    public class AncestorsArksData : ArcenExternalSubManagedData
    {
        // What Journals have been sent, and what header did we assign it? Helps us figure out when a log is invalid to be sent.
        public ArcenSparseLookup<string, string> JournalEntries;

        public AncestorsArksData()
        {
            JournalEntries = new ArcenSparseLookup<string, string>();
        }
        public AncestorsArksData( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializeIntoSelf( Buffer, false );
        }

        public override void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
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

        public override void DeserializeIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            int count = buffer.ReadInt32( ReadStyle.NonNeg );
            if ( JournalEntries == null )
                JournalEntries = new ArcenSparseLookup<string, string>();
            else if ( IsForPartialSyncDuringMultiplayer )
                JournalEntries.Clear();
            for ( int x = 0; x < count; x++ )
                JournalEntries.AddPair( buffer.ReadString_Condensed(), buffer.ReadString_Condensed() );
        }
    }
    public class AncestorsArksExternalData : ArcenExternalDataPatternImplementationBase_Faction
    {
        private AncestorsArksData Data;
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
            this.Data = new AncestorsArksData();
            Target[0] = this.Data;
        }
        public override void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            AncestorsArksData data = (AncestorsArksData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        protected override void DeserializeExternalData( Faction Parent, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            this.DeserializeExternalDataAsArcenExternalSubManagedData<AncestorsArksData>( Target, Buffer, IsForPartialSyncDuringMultiplayer );
        }
    }
    public static class AncestorsArksExternalDataExtensions
    {
        // This loads the data assigned to whatever ParentObject you pass. So, say, you could assign the same class to different ships, and each would be able to get back the values assigned to it.
        public static AncestorsArksData GetAncestorsArksData( this Faction ParentObject, ExternalDataRetrieval RetrievalRules )
        {
            ArcenExternalData extData = ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, AncestorsArksExternalData.PatternIndex, RetrievalRules );
            if ( extData == null )
                return null;
            return (AncestorsArksData)extData.Data[0];
        }
        // This meanwhile saves the data, assigning it to whatever ParentObject you pass.
        public static void SetAncestorsArksData( this Faction ParentObject, AncestorsArksData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, (int)AncestorsArksExternalData.PatternIndex, ExternalDataRetrieval.CreateIfNotFound ).Data[0] = data;
        }
    }

    public class ScrapyardData : ArcenExternalSubManagedData
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
            this.DeserializeIntoSelf( Buffer, false );
        }

        public override void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            byte count = (byte)ships.Length;
            buffer.AddByte( ReadStyleByte.Normal, count );
            for ( byte x = 0; x < count; x++ )
                buffer.AddString_Condensed( ships[x] );
        }
        public override void DeserializeIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            if ( ships == null || IsForPartialSyncDuringMultiplayer )
                ships = new string[3];
            byte count = buffer.ReadByte( ReadStyleByte.Normal );
            for ( byte x = 0; x < count; x++ )
                ships[x] = buffer.ReadString_Condensed();
        }
    }
    public class ScrapyardExternalData : ArcenExternalDataPatternImplementationBase_Squad
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private ScrapyardData Data;
        public static int PatternIndex;

        public override void ReceivePatternIndex( int Index )
        {
            PatternIndex = Index;
        }
        public override int GetNumberOfItems()
        {
            return 1;
        }

        protected override void InitializeData( GameEntity_Squad Parent, object[] Target )
        {
            this.Data = new ScrapyardData();
            Target[0] = this.Data;
        }
        public override void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            ScrapyardData data = (ScrapyardData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        protected override void DeserializeExternalData( GameEntity_Squad Parent, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            this.DeserializeExternalDataAsArcenExternalSubManagedData<ScrapyardData>( Target, Buffer, IsForPartialSyncDuringMultiplayer );
        }
    }
    public static class ScrapyardExternalDataExtensions
    {
        public static ScrapyardData GetScrapyardData( this GameEntity_Squad ParentObject, ExternalDataRetrieval RetrievalRules )
        {
            ArcenExternalData extData = ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, ScrapyardExternalData.PatternIndex, RetrievalRules );
            if ( extData == null )
                return null;
            return (ScrapyardData)extData.Data[0];
        }
        public static void SetScrapyardData( this GameEntity_Squad ParentObject, ScrapyardData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, (int)ScrapyardExternalData.PatternIndex, ExternalDataRetrieval.CreateIfNotFound ).Data[0] = data;
        }
    }
}
