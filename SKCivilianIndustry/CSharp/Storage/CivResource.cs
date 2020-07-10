using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SKCivilianIndustry.Storage
{
    public class CivResourceData
    {
        private int Version;
        public List<CivResource> Resources;

        public CivResource GetResourceByName(string name )
        {
            for ( int x = 0; x < Resources.Count; x++ )
                if ( Resources[x].Name == name )
                    return Resources[x];
            return null;
        }

        public CivResourceData()
        {
            Resources = new List<CivResource>();
        }
        public void SerializeTo( ArcenSerializationBuffer Buffer )
        {
            Buffer.AddInt32( ReadStyle.NonNeg, 1 );

            int count = Resources.Count;
            Buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
                Resources[x].SerializeTo( Buffer );
        }
        public CivResourceData( ArcenDeserializationBuffer Buffer ) : this()
        {
            Version = Buffer.ReadInt32( ReadStyle.NonNeg );

            int count = Buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
                Resources.Add( new CivResource( Buffer ) );
        }
    }
    public class CivResource
    {
        public string Name;

        public CivResource() { }
        public CivResource( string name )
        {
            Name = name;
        }
        public void SerializeTo( ArcenSerializationBuffer Buffer )
        {
            Buffer.AddString_Condensed( Name );
        }
        public CivResource( ArcenDeserializationBuffer Buffer )
        {
            Name = Buffer.ReadString_Condensed();
        }
    }
}