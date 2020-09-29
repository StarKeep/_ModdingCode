using Arcen.Universal;

namespace SKCivilianIndustry.Persistence
{
    public class CivilianWorldExternalData : ArcenExternalDataPatternImplementationBase_World
    {
        private CivilianWorld Data;
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
            this.Data = new CivilianWorld();
            Target[0] = this.Data;
        }
        public override void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            CivilianWorld data = (CivilianWorld)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        protected override void DeserializeExternalData( World Parent, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            this.DeserializeExternalDataAsArcenExternalSubManagedData<CivilianWorld>( Target, Buffer, IsForPartialSyncDuringMultiplayer );
        }
    }
}
