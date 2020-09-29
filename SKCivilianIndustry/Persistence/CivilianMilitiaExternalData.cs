using Arcen.AIW2.Core;
using Arcen.Universal;

namespace SKCivilianIndustry.Persistence
{
    public class CivilianMilitiaExternalData : ArcenExternalDataPatternImplementationBase_Squad
    {
        private CivilianMilitia Data;
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
            this.Data = new CivilianMilitia();
            Target[0] = this.Data;
        }
        public override void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            CivilianMilitia data = (CivilianMilitia)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        protected override void DeserializeExternalData( GameEntity_Squad Parent, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            this.DeserializeExternalDataAsArcenExternalSubManagedData<CivilianMilitia>( Target, Buffer, IsForPartialSyncDuringMultiplayer );
        }
    }
}
