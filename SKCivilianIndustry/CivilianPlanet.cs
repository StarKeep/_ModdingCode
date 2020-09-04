using Arcen.Universal;

namespace SKCivilianIndustry
{
    public class CivilianPlanet
    {
        /// <summary>
        /// Version of the class.
        /// </summary>
        public int Version;

        /// <summary>
        /// What resource this planet has.
        /// </summary>
        public CivilianResource Resource = CivilianResource.Length;

        public CivilianPlanet() { }

        public CivilianPlanet( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializedIntoSelf( Buffer, false );
        }

        public void SerializeTo( ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Buffer.AddInt32( ReadStyle.NonNeg, this.Version );
            Buffer.AddByte( ReadStyleByte.Normal, (byte)this.Resource );
        }

        public void DeserializedIntoSelf( ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            this.Version = Buffer.ReadInt32( ReadStyle.NonNeg );
            this.Resource = (CivilianResource)Buffer.ReadByte( ReadStyleByte.Normal );
        }
    }
}
