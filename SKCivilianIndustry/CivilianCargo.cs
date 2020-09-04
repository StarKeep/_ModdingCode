using Arcen.Universal;

namespace SKCivilianIndustry
{
    /// <summary>
    /// Used on any entity which has resources.
    /// </summary>
    public class CivilianCargo
    {
        public int Version;

        // We have three arrays here.
        // One for current amount, one for capacity, and one for per second change.
        public int[] Amount { get; set; }
        public int[] Capacity { get; set; }

        public int[] PerSecond { get; set; }

        // Following three functions are used for initializing, saving, and loading data.
        // Initialization function.
        // Default values. Called on creation, NOT on load.
        public CivilianCargo()
        {
            Amount = new int[(int)CivilianResource.Length];
            Capacity = new int[(int)CivilianResource.Length];
            PerSecond = new int[(int)CivilianResource.Length];
            // Values are set to the default for ships. Stations will manually initialize theirs.
            for ( int x = 0; x < this.Amount.Length; x++ )
            {
                this.Amount[x] = 0;
                this.Capacity[x] = 100;
                this.PerSecond[x] = 0;
            }
        }

        public CivilianCargo( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializedIntoSelf( Buffer, false );
        }

        public void SerializeTo( ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Buffer.AddInt32( ReadStyle.NonNeg, 1 );
            // Arrays
            // Get the number of items in the list, and store that as well.
            // This is so you know how many items you'll have to load later.
            // As we have one entry for each resource, we'll only have to get the count once.
            int count = this.Amount.Length;
            Buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
            {
                Buffer.AddInt32( ReadStyle.NonNeg, this.Amount[x] );
                Buffer.AddInt32( ReadStyle.NonNeg, this.Capacity[x] );
                Buffer.AddInt32( ReadStyle.Signed, this.PerSecond[x] );
            }
        }

        public void DeserializedIntoSelf( ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            if ( IsForPartialSyncDuringMultiplayer )
            {
                Amount = new int[(int)CivilianResource.Length];
                Capacity = new int[(int)CivilianResource.Length];
                PerSecond = new int[(int)CivilianResource.Length];
            }

            this.Version = Buffer.ReadInt32( ReadStyle.NonNeg );
            // Lists require a special touch to load.
            // We'll have saved the number of items stored up above to be used here to determine the number of items to load.
            // ADDITIONALLY we'll need to recreate our arrays beforehand, as loading does not call the Initialization function.
            // Can't add values to an array that doesn't exist, after all.
            // Its more important to be accurate than it is to be update safe here, so we'll always use our stored value to figure out the number of resources.
            int savedCount = Buffer.ReadInt32( ReadStyle.NonNeg );
            int resourceTypeCount = (int)CivilianResource.Length;
            for ( int x = 0; x < resourceTypeCount; x++ )
            {
                if ( x >= savedCount )
                {
                    this.Amount[x] = 0;
                    this.Capacity[x] = 100;
                    this.PerSecond[x] = 0;
                }
                else
                {
                    this.Amount[x] = Buffer.ReadInt32( ReadStyle.NonNeg );
                    this.Capacity[x] = Buffer.ReadInt32( ReadStyle.NonNeg );
                    this.PerSecond[x] = Buffer.ReadInt32( ReadStyle.Signed );
                }
            }
        }
    }
}
