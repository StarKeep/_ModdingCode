using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCivilianIndustry.Storage
{
    public class CivConstructionData
    {
        // Internal name to build into.
        public string InternalName;

        // Seconds until built.
        public int SecondsLeft;

        // Entity we're building for. If this dies, we should stop.
        public int BuiltFor;

        public CivConstructionData()
        {
            InternalName = string.Empty;
            SecondsLeft = -1;
            BuiltFor = -1;
        }

        public CivConstructionData( ArcenDeserializationBuffer Buffer ) : this()
        {
            InternalName = Buffer.ReadString_Condensed();
            SecondsLeft = Buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            BuiltFor = Buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
        }

        public void SerializeTo( ArcenSerializationBuffer Buffer )
        {
            Buffer.AddString_Condensed( InternalName );
            Buffer.AddInt32( ReadStyle.PosExceptNeg1, SecondsLeft );
            Buffer.AddInt32( ReadStyle.PosExceptNeg1, BuiltFor );
        }
    }
    public class CivConstructionDataExternalData : IArcenExternalDataPatternImplementation
    {
        private CivConstructionData Data;

        public static int PatternIndex;

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
            this.Data = new CivConstructionData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer )
        {
            //For saving to disk, translate this object into the buffer
            CivConstructionData data = (CivConstructionData)Source[0];
            data.SerializeTo( Buffer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            Target[0] = new CivConstructionData( Buffer );
        }
    }
    public static class CivConstructionDataExtensions
    {
        public static CivConstructionData GetConstructionData( this GameEntity_Squad ParentObject )
        {
            CivConstructionData constructionData = (CivConstructionData)ParentObject.ExternalData.GetCollectionByPatternIndex( CivConstructionDataExternalData.PatternIndex ).Data[0];
            return constructionData;
        }
        public static void SetConstructionData( this GameEntity_Squad ParentObject, CivConstructionData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( (int)CivConstructionDataExternalData.PatternIndex ).Data[0] = data;
        }
    }
}
