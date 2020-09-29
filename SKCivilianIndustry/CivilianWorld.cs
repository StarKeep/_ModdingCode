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

        public ArcenSparseLookup<short, CivilianResource> ResourceByPlanet;

        public CivilianResource GetResourceForPlanet(Planet planet )
        {
            if ( ResourceByPlanet == null )
                return CivilianResource.Length;
            else if ( !ResourceByPlanet.GetHasKey( planet.Index ) )
                return CivilianResource.Length;
            else
                return ResourceByPlanet[planet.Index];
        }

        public void SetResourceForPlanet(Planet planet, CivilianResource resource )
        {
            if ( ResourceByPlanet == null )
                ResourceByPlanet = new ArcenSparseLookup<short, CivilianResource>();
            if ( ResourceByPlanet.GetHasKey( planet.Index ) )
                ResourceByPlanet[planet.Index] = resource;
            else
                ResourceByPlanet.AddPair( planet.Index, resource );
        }

        // Following two functions are used for saving, and loading data.
        public CivilianWorld()
        {
            ResourceByPlanet = new ArcenSparseLookup<short, CivilianResource>();
        }

        public CivilianWorld( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializeIntoSelf( Buffer, false );
        }

        public override void SerializeTo( ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Buffer.AddInt32( ReadStyle.NonNeg, 1 );
            Buffer.AddItem( Unused );

            int count = ResourceByPlanet.GetPairCount();
            Buffer.AddInt32( ReadStyle.NonNeg, count );
            for(int x = 0; x < count; x++ )
            {
                ArcenSparseLookupPair<short, CivilianResource> pair = ResourceByPlanet.GetPairByIndex( x );
                Buffer.AddInt16( ReadStyle.PosExceptNeg1, pair.Key );
                Buffer.AddByte( ReadStyleByte.Normal, (byte)pair.Key );
            }
        }

        public override void DeserializeIntoSelf( ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Version = Buffer.ReadInt32( ReadStyle.NonNeg );
            this.Unused = Buffer.ReadBool();

            if ( this.ResourceByPlanet == null )
                this.ResourceByPlanet = new ArcenSparseLookup<short, CivilianResource>();
            else if ( IsForPartialSyncDuringMultiplayer )
                this.ResourceByPlanet.Clear();

            int count = Buffer.ReadInt32( ReadStyle.NonNeg );
            for(int x = 0; x < count; x++ )
            {
                this.ResourceByPlanet.AddPair( Buffer.ReadInt16( ReadStyle.PosExceptNeg1 ), (CivilianResource)Buffer.ReadByte( ReadStyleByte.Normal ) );
            }
        }
    }
}
