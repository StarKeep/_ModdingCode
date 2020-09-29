using Arcen.Universal;

namespace SKCivilianIndustry
{
    public enum Status
    {
        Loading,
        Unloading,
        Building,
        Pathing,
        Enroute,
        Idle
    }

    /// <summary>
    /// Used on mobile ships. Tells us what they're currently doing.
    /// </summary>
    public class CivilianStatus : ArcenExternalSubManagedData
    {
        public int Version { get; private set; }

        /// <summary>
        /// The index of requesting station.
        /// If - 1 the it is being sent from the grand station.
        /// </summary>
        public int Origin { get; set; } = -1;

        /// <summary>
        /// The index of the ship's destination station, if any.
        /// </summary>
        public int Destination { get; set; } = -1;

        /// <summary>
        /// The amount of time left before departing from a loading job.
        /// Usually 120 seconds. Value here is interpreted as seconds.
        /// </summary>
        public int LoadTimer { get; set; } = 0;

        public CivilianStatus()
        {

        }

        public CivilianStatus( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializeIntoSelf( Buffer, false );
        }

        public override void SerializeTo( ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Buffer.AddInt32( ReadStyle.NonNeg, 2 );
            Buffer.AddInt32( ReadStyle.Signed, this.Origin );
            Buffer.AddInt32( ReadStyle.Signed, this.Destination );
            Buffer.AddInt32( ReadStyle.Signed, this.LoadTimer );
        }

        public override void DeserializeIntoSelf( ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            this.Version = Buffer.ReadInt32( ReadStyle.NonNeg );
            if ( this.Version < 2 )
                Buffer.ReadInt32( ReadStyle.Signed );
            this.Origin = Buffer.ReadInt32( ReadStyle.Signed );
            this.Destination = Buffer.ReadInt32( ReadStyle.Signed );
            this.LoadTimer = Buffer.ReadInt32( ReadStyle.Signed );
        }
    }
}
