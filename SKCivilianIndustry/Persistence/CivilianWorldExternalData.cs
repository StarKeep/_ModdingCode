using Arcen.Universal;

namespace SKCivilianIndustry.Persistence
{
    public class CivilianWorldExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private CivilianWorld Data;

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
                ArcenDebugging.ArcenDebugLogSingleLine( "CivilianCargoExternalData: Tried to initialize Parent object as World, but type was " + ParentObject.GetType(), Verbosity.ShowAsError );

            this.Data = new CivilianWorld();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            CivilianWorld data = (CivilianWorld)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            if ( IsForPartialSyncDuringMultiplayer )
            {
                //this is a partial sync, so use existing object and write into it
                (Target[0] as CivilianWorld).DeserializedIntoSelf( Buffer, IsForPartialSyncDuringMultiplayer );
            }
            else
            {
                //this is a full sync, so create a new object
                Target[0] = new CivilianWorld( Buffer );
            }
        }
    }

}
