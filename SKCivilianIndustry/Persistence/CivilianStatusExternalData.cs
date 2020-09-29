using Arcen.AIW2.Core;
using Arcen.Universal;

namespace SKCivilianIndustry.Persistence
{
    public class CivilianStatusExternalData : ArcenExternalDataPatternImplementationBase_Squad
    {
        private CivilianStatus Data;
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
            this.Data = new CivilianStatus();
            Target[0] = this.Data;
        }
        public override void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            CivilianStatus data = (CivilianStatus)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        protected override void DeserializeExternalData( GameEntity_Squad Parent, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            this.DeserializeExternalDataAsArcenExternalSubManagedData<CivilianStatus>( Target, Buffer, IsForPartialSyncDuringMultiplayer );
        }
    }
}
