using Arcen.AIW2.Core;
using Arcen.Universal;

namespace SKCivilianIndustry
{
    /// <summary>
    /// World storage class. Everything can be found from here.
    /// </summary>
    public class CivilianWorld : ArcenExternalSubManagedData
    {
        public int Version;

        public bool Unused = false;

        public ArcenSparseLookup<short, CivilianResource> Unused2;

        // Following two functions are used for saving, and loading data.
        public CivilianWorld()
        {
            Unused2 = new ArcenSparseLookup<short, CivilianResource>();
        }

        public CivilianWorld( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializeIntoSelf( Buffer, false );
        }

        public override void SerializeTo( ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Buffer.AddInt32( ReadStyle.NonNeg, 2 );
        }

        public override void DeserializeIntoSelf( ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Version = Buffer.ReadInt32( ReadStyle.NonNeg );

            if ( Version < 2 )
            {
                this.Unused = Buffer.ReadBool();

                int count = Buffer.ReadInt32( ReadStyle.NonNeg );
                for ( int x = 0; x < count; x++ )
                {
                    Buffer.ReadInt16( ReadStyle.PosExceptNeg1 );
                    Buffer.ReadByte( ReadStyleByte.Normal );
                }
            }
        }
    }
}
