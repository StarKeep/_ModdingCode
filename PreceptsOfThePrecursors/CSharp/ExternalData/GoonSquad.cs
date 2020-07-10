using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using System;
using System.Collections.Generic;

namespace PreceptsOfThePrecursors
{
    // Class used to keep track of the Dyson Mothership's stats. Stored on the faction, since each faction will only ever have one Mothership.
    public class GoonSquadData
    {
        // What Journals have been sent, and what header did we assign it? Helps us figure out when a log is invalid to be sent.
        public ArcenSparseLookup<string, string> JournalEntries;

        public GoonSquadData()
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
        public GoonSquadData( ArcenDeserializationBuffer buffer )
        {
            int count = buffer.ReadInt32(ReadStyle.NonNeg);
            JournalEntries = new ArcenSparseLookup<string, string>();
            for ( int x = 0; x < count; x++ )
                JournalEntries.AddPair( buffer.ReadString_Condensed(), buffer.ReadString_Condensed() );
        }    
    }
    public class GoonSquadExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private GoonSquadData Data;

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
            this.Data = new GoonSquadData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer )
        {
            //For saving to disk, translate this object into the buffer
            GoonSquadData data = (GoonSquadData)Source[0];
            data.SerializeTo( Buffer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            Target[0] = new GoonSquadData( Buffer );
        }
    }
    public static class GoonSquadExternalDataExtensions
    {
        // This loads the data assigned to whatever ParentObject you pass. So, say, you could assign the same class to different ships, and each would be able to get back the values assigned to it.
        // In our specific case here, we're going to be assigning a dictionary to every faction.
        public static GoonSquadData GetGoonSquadData( this World ParentObject )
        {
            return (GoonSquadData)ParentObject.ExternalData.GetCollectionByPatternIndex( GoonSquadExternalData.PatternIndex ).Data[0];
        }
        // This meanwhile saves the data, assigning it to whatever ParentObject you pass.
        public static void SetGoonSquadData( this World ParentObject, GoonSquadData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( GoonSquadExternalData.PatternIndex ).Data[0] = data;
        }
    }
}
