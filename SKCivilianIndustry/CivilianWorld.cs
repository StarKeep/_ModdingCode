using Arcen.Universal;

namespace SKCivilianIndustry
{
    /// <summary>
    /// World storage class. Everything can be found from here.
    /// </summary>
    public class CivilianWorld
    {
        /// <summary>
        /// Version of this class.
        /// </summary>
        public int Version;

        /// <summary>
        /// Indicates whether resources have been already generated.
        /// </summary>
        public bool GeneratedResources = false;

        // Following two functions are used for saving, and loading data.
        public CivilianWorld() { }

        public CivilianWorld( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializedIntoSelf( Buffer, false );
        }

        public void SerializeTo( ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Buffer.AddInt32( ReadStyle.NonNeg, 1 );
            Buffer.AddItem( GeneratedResources );
        }

        public void DeserializedIntoSelf( ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Version = Buffer.ReadInt32( ReadStyle.NonNeg );
            this.GeneratedResources = Buffer.ReadBool();
        }
    }
}
