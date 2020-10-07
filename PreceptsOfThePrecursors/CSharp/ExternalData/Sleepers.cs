using Arcen.AIW2.Core;
using Arcen.Universal;

namespace PreceptsOfThePrecursors
{
    // Created to keep track of various things that don't work right when an entity transforms.
    public class SleeperData : ArcenExternalSubManagedData
    {
        private short planet;
        public Planet Planet { get { return World_AIW2.Instance.GetPlanetByIndex( planet ); } set { planet = value.Index; } }
        public int SecondEnteredPlanet;
        public int SecondsSinceEnteringPlanet { get { return World_AIW2.Instance.GameSecond - SecondEnteredPlanet; } }

        public int SecondLastTransformed;
        public int SecondsSinceLastTransformation { get { return World_AIW2.Instance.GameSecond - SecondLastTransformed; } }

        public SleeperData()
        {
            planet = -1;
            SecondEnteredPlanet = 0;
            SecondLastTransformed = 0;
        }
        public SleeperData( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializeIntoSelf( Buffer, false );
        }
        public override void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            buffer.AddInt16( ReadStyle.PosExceptNeg1, planet );
            buffer.AddInt32( ReadStyle.NonNeg, SecondEnteredPlanet );
            buffer.AddInt32( ReadStyle.NonNeg, SecondLastTransformed );
        }
        public override void DeserializeIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
                planet = buffer.ReadInt16( ReadStyle.PosExceptNeg1 );
                SecondEnteredPlanet = buffer.ReadInt32( ReadStyle.NonNeg );
                SecondLastTransformed = buffer.ReadInt32( ReadStyle.NonNeg );
        }
    }
    public class SleeperExternalData : ArcenExternalDataPatternImplementationBase_Squad
    {
        private SleeperData Data;
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
            this.Data = new SleeperData();
            Target[0] = this.Data;
        }
        public override void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            SleeperData data = (SleeperData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        protected override void DeserializeExternalData( GameEntity_Squad Parent, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            this.DeserializeExternalDataAsArcenExternalSubManagedData<SleeperData>( Target, Buffer, IsForPartialSyncDuringMultiplayer );
        }
    }
    public static class SleeperExternalDataExtensions
    {
        public static SleeperData GetSleeperData( this GameEntity_Squad ParentObject, ExternalDataRetrieval RetrievalRules )
        {
            ArcenExternalData extData = ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, SleeperExternalData.PatternIndex, RetrievalRules );
            if ( extData == null )
                return null;
            return (SleeperData)extData.Data[0];
        }
        public static void SetSleeperData( this GameEntity_Squad ParentObject, SleeperData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, (int)SleeperExternalData.PatternIndex, ExternalDataRetrieval.CreateIfNotFound ).Data[0] = data;
        }
    }
}
