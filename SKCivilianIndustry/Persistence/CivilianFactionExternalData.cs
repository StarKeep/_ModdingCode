using Arcen.AIW2.Core;
using Arcen.Universal;

namespace SKCivilianIndustry.Persistence
{
    public class CivilianFactionExternalData : ArcenExternalDataPatternImplementationBase_Faction
    {
        private CivilianFaction Data;
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
            this.Data = new CivilianFaction();
            Target[0] = this.Data;
        }
        public override void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            CivilianFaction data = (CivilianFaction)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        protected override void DeserializeExternalData( Faction Parent, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            this.DeserializeExternalDataAsArcenExternalSubManagedData<CivilianFaction>( Target, Buffer, IsForPartialSyncDuringMultiplayer );
        }
    }
}
