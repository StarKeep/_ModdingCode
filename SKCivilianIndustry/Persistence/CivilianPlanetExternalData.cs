using Arcen.AIW2.Core;
using Arcen.Universal;

namespace SKCivilianIndustry.Persistence
{
    public class CivilianPlanetExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private CivilianPlanet Data;

        public static int PatternIndex;
        public void ReceivePatternIndex( int Index )
        {
            PatternIndex = Index; //for internal use with the ExternalData code in the game engine itself
        }
        public int GetNumberOfItems()
        {
            return 1; //for internal use with the ExternalData code in the game engine itself
        }

        public Planet ParentPlanet;
        public void InitializeData( object ParentObject, object[] Target )
        {
            this.ParentPlanet = ParentObject as Planet;
            if ( this.ParentPlanet == null && ParentObject != null )
                ArcenDebugging.ArcenDebugLogSingleLine( "CivilianCargoExternalData: Tried to initialize Parent object as Planet, but type was " + ParentObject.GetType(), Verbosity.ShowAsError );

            //this initialization is handled by the data structure itself
            this.Data = new CivilianPlanet();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            CivilianPlanet data = (CivilianPlanet)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            if ( IsForPartialSyncDuringMultiplayer )
            {
                //this is a partial sync, so use existing object and write into it
                (Target[0] as CivilianPlanet).DeserializedIntoSelf( Buffer, IsForPartialSyncDuringMultiplayer );
            }
            else
            {
                //this is a full sync, so create a new object
                Target[0] = new CivilianPlanet( Buffer );
            }
        }
    }
}
