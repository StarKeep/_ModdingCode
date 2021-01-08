using Arcen.AIW2.Core;
using Arcen.Universal;

namespace PreceptsOfThePrecursors
{
    public class SleeperData : ArcenExternalSubManagedData
    {
        public int Version;

        private short planet;
        public Planet Planet
        {
            get { return World_AIW2.Instance.GetPlanetByIndex( planet ); }
            set
            {
                if ( value.Index != planet )
                {
                    planet = value.Index;
                    nextEntityToBuild = string.Empty;
                    cachedNextEntityToBuild = null;
                }
            }
        }
        public int SecondEnteredPlanet;
        public int SecondsSinceEnteringPlanet { get { return World_AIW2.Instance.GameSecond - SecondEnteredPlanet; } }

        public int SecondLastTransformed;
        public int SecondsSinceLastTransformation { get { return World_AIW2.Instance.GameSecond - SecondLastTransformed; } }

        public int StoredMetalForConstruction;
        public int StoredMetalForUpgrading;

        private string nextEntityToBuild;
        private GameEntityTypeData cachedNextEntityToBuild;
        public GameEntityTypeData NextEntityToBuild
        {
            get
            {
                if ( cachedNextEntityToBuild == null )
                    cachedNextEntityToBuild = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( nextEntityToBuild );
                return cachedNextEntityToBuild;
            }
            set
            {
                cachedNextEntityToBuild = null;
                if ( value == null )
                    nextEntityToBuild = string.Empty;
                else
                    nextEntityToBuild = value.InternalName;
            }
        }

        public int OriginalID;

        private short targetPlanet;
        public Planet TargetPlanet { get { return World_AIW2.Instance.GetPlanetByIndex( targetPlanet ); } set { targetPlanet = value == null ? (short)-1 : value.Index; } }

        public ArcenPoint TargetPoint;

        public SleeperData()
        {
            planet = -1;
            SecondEnteredPlanet = 0;
            SecondLastTransformed = 0;
            StoredMetalForConstruction = 0;
            StoredMetalForUpgrading = 0;
            nextEntityToBuild = string.Empty;
            OriginalID = -1;
            targetPlanet = -1;
            TargetPoint = ArcenPoint.ZeroZeroPoint;
        }
        public SleeperData( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializeIntoSelf( Buffer, false );
        }
        public override void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            buffer.AddInt32( ReadStyle.NonNeg, 5 );

            buffer.AddInt16( ReadStyle.PosExceptNeg1, planet );
            buffer.AddInt32( ReadStyle.NonNeg, SecondEnteredPlanet );
            buffer.AddInt32( ReadStyle.NonNeg, SecondLastTransformed );

            buffer.AddInt32( ReadStyle.NonNeg, StoredMetalForConstruction );
            buffer.AddInt32( ReadStyle.NonNeg, StoredMetalForUpgrading );
            buffer.AddString_Condensed( nextEntityToBuild );

            buffer.AddInt32( ReadStyle.PosExceptNeg1, OriginalID );

            buffer.AddInt16( ReadStyle.PosExceptNeg1, targetPlanet );
            buffer.AddInt32( ReadStyle.Signed, TargetPoint.X );
            buffer.AddInt32( ReadStyle.Signed, TargetPoint.Y );
        }
        public override void DeserializeIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Version = buffer.ReadInt32( ReadStyle.NonNeg );

            planet = buffer.ReadInt16( ReadStyle.PosExceptNeg1 );
            SecondEnteredPlanet = buffer.ReadInt32( ReadStyle.NonNeg );
            SecondLastTransformed = buffer.ReadInt32( ReadStyle.NonNeg );

            if ( Version >= 2 )
            {
                StoredMetalForConstruction = buffer.ReadInt32( ReadStyle.NonNeg );
                if ( Version >= 5 )
                    StoredMetalForUpgrading = buffer.ReadInt32( ReadStyle.NonNeg );
                nextEntityToBuild = buffer.ReadString_Condensed();
                cachedNextEntityToBuild = null;
            }
            if ( Version >= 3 )
                OriginalID = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            if (Version >= 4 )
            {
                targetPlanet = buffer.ReadInt16( ReadStyle.PosExceptNeg1 );
                TargetPoint = ArcenPoint.Create( buffer.ReadInt32( ReadStyle.Signed ), buffer.ReadInt32( ReadStyle.Signed ) );
            }
        }
    }
    public class SleeperExternalData : ArcenExternalDataPatternImplementationBase_Squad
    {
        private SleeperData Data;
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
            this.Data = new SleeperData();
            Target[0] = this.Data;
        }
        public override void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            SleeperData data = (SleeperData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        protected override void DeserializeExternalData( GameEntity_Squad Parent, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            this.DeserializeExternalDataAsArcenExternalSubManagedData<SleeperData>( Target, Buffer, IsForPartialSyncDuringMultiplayer );
        }
    }
    public static class SleeperExternalDataExtensions
    {
        public static SleeperData GetSleeperData( this GameEntity_Squad ParentObject, ExternalDataRetrieval RetrievalRules )
        {
            ArcenExternalData extData = ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, SleeperExternalData.PatternIndex, RetrievalRules );
            if ( extData == null )
                return null;
            return (SleeperData)extData.Data[0];
        }
        public static void SetSleeperData( this GameEntity_Squad ParentObject, SleeperData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, (int)SleeperExternalData.PatternIndex, ExternalDataRetrieval.CreateIfNotFound ).Data[0] = data;
        }
    }

    public class SleeperFactionData : ArcenExternalSubManagedData
    {
        public int Version;

        public int StrengthOfNextSleeperCPA;

        public SleeperFactionData()
        {
            StrengthOfNextSleeperCPA = 0;
        }
        public SleeperFactionData( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializeIntoSelf( Buffer, false );
        }
        public override void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            buffer.AddInt32( ReadStyle.NonNeg, 1 );
        }
        public override void DeserializeIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            Version = buffer.ReadInt32( ReadStyle.NonNeg );
        }
    }
    public class SleeperFactionExternalData : ArcenExternalDataPatternImplementationBase_Faction
    {
        private SleeperFactionData Data;
        public static int PatternIndex;

        public override void ReceivePatternIndex( int Index )
        {
            PatternIndex = Index;
        }
        public override int GetNumberOfItems()
        {
            return 1;
        }

        protected override void InitializeData( Faction Parent, object[] Target )
        {
            this.Data = new SleeperFactionData();
            Target[0] = this.Data;
        }
        public override void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            SleeperFactionData data = (SleeperFactionData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        protected override void DeserializeExternalData( Faction Parent, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            this.DeserializeExternalDataAsArcenExternalSubManagedData<SleeperFactionData>( Target, Buffer, IsForPartialSyncDuringMultiplayer );
        }
    }
    public static class SleeperFactionExternalDataExtensions
    {
        public static SleeperFactionData GetSleeperFactionData( this Faction ParentObject, ExternalDataRetrieval RetrievalRules )
        {
            ArcenExternalData extData = ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, SleeperFactionExternalData.PatternIndex, RetrievalRules );
            if ( extData == null )
                return null;
            return (SleeperFactionData)extData.Data[0];
        }
        public static void SetSleeperFactionData( this Faction ParentObject, SleeperFactionData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, (int)SleeperFactionExternalData.PatternIndex, ExternalDataRetrieval.CreateIfNotFound ).Data[0] = data;
        }
    }
}
