using System.Xml;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Discordians
{
    public enum Stance
    {
        Aggressive,
        Defensive
    }
    public class DiscordianData : ArcenExternalSubManagedData
    {
        public int Version;

        public string ID;
        public string UserName;
        public ArcenSparseLookup<string, int> ShipTypes;
        public int Experience;
        public int Research;
        private short targetPlanet;
        public Planet TargetPlanet { get { return World_AIW2.Instance.GetPlanetByIndex( targetPlanet ); } set { if ( value == null ) targetPlanet = -1; else targetPlanet = value.Index; } }
        public int SecondsUntilRespawn;

        public DiscordianData()
        {
            ID = string.Empty;
            UserName = string.Empty;
            ShipTypes = new ArcenSparseLookup<string, int>();
            Experience = -1;
            Research = -1;
            targetPlanet = -1;
            SecondsUntilRespawn = -1;
        }
        public DiscordianData( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializeIntoSelf( Buffer, false );
        }
        public override void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            buffer.AddInt32( ReadStyle.NonNeg, 1 );

            buffer.AddString_Condensed( ID );
            buffer.AddString_Condensed( UserName );

            int count = ShipTypes.GetPairCount();
            buffer.AddInt32( ReadStyle.PosExceptNeg1, count );
            ShipTypes.DoFor( pair =>
            {
                buffer.AddString_Condensed( pair.Key );
                buffer.AddInt32( ReadStyle.PosExceptNeg1, pair.Value );

                return DelReturn.Continue;
            } );

            buffer.AddInt32( ReadStyle.PosExceptNeg1, Experience );
            buffer.AddInt32( ReadStyle.PosExceptNeg1, Research );
            buffer.AddInt16( ReadStyle.PosExceptNeg1, targetPlanet );
            buffer.AddInt32( ReadStyle.PosExceptNeg1, SecondsUntilRespawn );
        }
        public override void DeserializeIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Version = buffer.ReadInt32( ReadStyle.NonNeg );

            ID = buffer.ReadString_Condensed();
            UserName = buffer.ReadString_Condensed();

            if ( ShipTypes == null )
                ShipTypes = new ArcenSparseLookup<string, int>();
            else
                ShipTypes.Clear();
            int count = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            for ( int x = 0; x < count; x++ )
                ShipTypes.AddPair( buffer.ReadString_Condensed(), buffer.ReadInt32( ReadStyle.PosExceptNeg1 ) );

            Experience = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            Research = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            targetPlanet = buffer.ReadInt16( ReadStyle.PosExceptNeg1 );
            SecondsUntilRespawn = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
        }
        public void WriteShips(XmlWriter w )
        {
            w.WriteStartElement( "Ships" );
            ShipTypes.DoFor( pair =>
            {
                w.WriteStartElement( "Ship" );
                w.WriteAttributeString( "Name", pair.Key );
                w.WriteAttributeString( "Unlocks", pair.Value.ToString() );
                w.WriteEndElement();

                return DelReturn.Continue;
            } );
            w.WriteEndElement();
        }
    }
    public class DiscordianExternalData : ArcenExternalDataPatternImplementationBase_Squad
    {
        private DiscordianData Data;
        public static int PatternIndex;

        public override void ReceivePatternIndex( int Index )
        {
            PatternIndex = Index;
        }
        public override int GetNumberOfItems()
        {
            return 1;
        }

        protected override void InitializeData( GameEntity_Squad Parent, object[] Target )
        {
            this.Data = new DiscordianData();
            Target[0] = this.Data;
        }
        public override void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            DiscordianData data = (DiscordianData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        protected override void DeserializeExternalData( GameEntity_Squad Parent, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            this.DeserializeExternalDataAsArcenExternalSubManagedData<DiscordianData>( Target, Buffer, IsForPartialSyncDuringMultiplayer );
        }
    }
    public static class SleeperExternalDataExtensions
    {
        public static DiscordianData GetDiscordianData( this GameEntity_Squad ParentObject, ExternalDataRetrieval RetrievalRules )
        {
            ArcenExternalData extData = ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, DiscordianExternalData.PatternIndex, RetrievalRules );
            if ( extData == null )
                return null;
            return (DiscordianData)extData.Data[0];
        }
        public static void SetDiscordianData( this GameEntity_Squad ParentObject, DiscordianData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, (int)DiscordianExternalData.PatternIndex, ExternalDataRetrieval.CreateIfNotFound ).Data[0] = data;
        }
    }
}
