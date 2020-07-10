using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using SKCivilianIndustry.DescriptionAppenders;

namespace SKCivilianIndustry.Storage
{
    public class CivIndustryData
    {
        public ArcenSparseLookup<string, int> StoredResources;
        public ArcenSparseLookup<string, string> UnitTypeBuilt;
        public ArcenSparseLookup<string, int> Cost;
        public ArcenSparseLookup<string, int> Capacity;

        public bool BuildForOwner;

        public bool Initialized;

        public void Initialize( GameEntityTypeData entityData )
        {
            if ( Initialized )
                return;

            for ( int x = 0; x < entityData.TagsList.Count; x++ )
            {
                string rawTag = entityData.TagsList[x];
                if ( rawTag.Substring( 0, SKTradeLogicFaction.BUILD_TAG.Length ) == SKTradeLogicFaction.BUILD_TAG )
                {
                    string[] splitTag = rawTag.Split( '/' );
                    if ( splitTag.Length < 5 )
                    {
                        ArcenDebugging.SingleLineQuickDebug( $"Failed to process {SKTradeLogicFaction.BUILD_TAG} tag on entity named {entityData.InternalName}. Failed to find all required parts." );
                    }

                    string unitName = splitTag[1], rawCapacity = splitTag[2], resourceName = splitTag[3], rawCost = splitTag[4];

                    // UnitName / UnitCapacity / ResourceName / ResourceCost
                    int capacity;
                    if ( !int.TryParse( rawCapacity, out capacity ) )
                    {
                        ArcenDebugging.SingleLineQuickDebug( $"Failed to process {SKTradeLogicFaction.BUILD_TAG}'s capacity tag on entity named {entityData.InternalName}." );
                        continue;
                    }
                    int cost;
                    if ( rawCost.Substring( 0, 4 ) == "Auto" )
                    {
                        if ( !int.TryParse( rawCost.Substring( 4 ), out cost ) )
                        {
                            ArcenDebugging.SingleLineQuickDebug( $"Failed to process {SKTradeLogicFaction.BUILD_TAG}'s cost tag on entity named {entityData.InternalName}." );
                            continue;
                        }
                        GameEntityTypeData dataToSpawn = GameEntityTypeDataTable.Instance.GetRowByName( unitName );
                        cost *= dataToSpawn.CostForAIToPurchase;
                    }
                    else if ( !int.TryParse( rawCost, out cost ) )
                    {
                        ArcenDebugging.SingleLineQuickDebug( $"Failed to process {SKTradeLogicFaction.BUILD_TAG}'s cost tag on entity named {entityData.InternalName}." );
                        continue;
                    }
                    StoredResources.AddPair( resourceName, 0 );
                    UnitTypeBuilt.AddPair( resourceName, unitName );
                    Cost.AddPair( resourceName, cost );
                    Capacity.AddPair( resourceName, capacity );
                }
                else if ( rawTag == SKTradeLogicFaction.BUILD_FOR_OWNER_TAG )
                {
                    BuildForOwner = true;
                }
            }
            Initialized = true;
        }

        public CivIndustryData()
        {
            StoredResources = new ArcenSparseLookup<string, int>();
            UnitTypeBuilt = new ArcenSparseLookup<string, string>();
            Cost = new ArcenSparseLookup<string, int>();
            Capacity = new ArcenSparseLookup<string, int>();
        }

        public CivIndustryData( ArcenDeserializationBuffer Buffer ) : this()
        {
            int count = Buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
            {
                string key = Buffer.ReadString_Condensed();
                StoredResources.AddPair( key, Buffer.ReadInt32( ReadStyle.NonNeg ) );
                UnitTypeBuilt.AddPair( key, Buffer.ReadString_Condensed() );
                Cost.AddPair( key, Buffer.ReadInt32( ReadStyle.NonNeg ) );
                Capacity.AddPair( key, Buffer.ReadInt32( ReadStyle.PosExceptNeg1 ) );
            }
            BuildForOwner = Buffer.ReadBool();
            Initialized = Buffer.ReadBool();
        }

        public void SerializeTo( ArcenSerializationBuffer Buffer )
        {
            int count = StoredResources.GetPairCount();
            Buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
            {
                string key = StoredResources.GetPairByIndex( x ).Key;
                Buffer.AddString_Condensed( key );
                Buffer.AddInt32( ReadStyle.NonNeg, StoredResources[key] );
                Buffer.AddString_Condensed( UnitTypeBuilt[key] );
                Buffer.AddInt32( ReadStyle.NonNeg, Cost[key] );
                Buffer.AddInt32( ReadStyle.PosExceptNeg1, Capacity[key] );
            }
            Buffer.AddItem( BuildForOwner );
            Buffer.AddItem( Initialized );
        }
    }
    public class CivIndustryDataExternalData : IArcenExternalDataPatternImplementation
    {
        private CivIndustryData Data;

        public static int PatternIndex;

        public static string RelatedParentTypeName = "GameEntity_Squad";

        public void ReceivePatternIndex( int Index )
        {
            PatternIndex = Index;
        }
        public int GetNumberOfItems()
        {
            return 1;
        }
        public bool GetShouldInitializeOn( string ParentTypeName )
        {
            // Figure out which object type has this sort of ExternalData (in this case, Faction)
            return ArcenStrings.Equals( ParentTypeName, RelatedParentTypeName );
        }

        public void InitializeData( object ParentObject, object[] Target )
        {
            this.Data = new CivIndustryData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer )
        {
            //For saving to disk, translate this object into the buffer
            CivIndustryData data = (CivIndustryData)Source[0];
            data.SerializeTo( Buffer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            Target[0] = new CivIndustryData( Buffer );
        }
    }
    public static class CivIndustryDataExtensions
    {
        /// <summary>
        /// If called from non-sim locations, such as UI or Background threads, may cause desync.
        /// </summary>
        /// <param name="ParentObject"></param>
        /// <returns></returns>
        public static CivIndustryData GetIndustrySimSafeNeverNull( this GameEntity_Squad ParentObject )
        {
            CivIndustryData data = (CivIndustryData)ParentObject.ExternalData.GetCollectionByPatternIndex( CivIndustryDataExternalData.PatternIndex ).Data[0];
            if ( !data.Initialized )
                data.Initialize( ParentObject.TypeData );
            if ( !(ParentObject.TypeData.DescriptionAppender is IndustryDescriptionAppender) )
                ParentObject.TypeData.DescriptionAppender = new IndustryDescriptionAppender( ParentObject.TypeData.DescriptionAppender );
            return data;
        }

        /// <summary>
        /// Returns null if the industry is not yet initialized. If Context is passed, this will queue up a GameCommand for requested industry to be initialized.
        /// </summary>
        /// <param name="ParentObject"></param>
        /// <param name="Context"></param>
        /// <returns></returns>
        public static CivIndustryData GetIndustryNotSimSafeMayReturnNull( this GameEntity_Squad ParentObject, ArcenLongTermIntermittentPlanningContext Context = null )
        {
            CivIndustryData data = (CivIndustryData)ParentObject.ExternalData.GetCollectionByPatternIndex( CivIndustryDataExternalData.PatternIndex ).Data[0];
            if ( !data.Initialized )
            {
                if ( Context != null )
                {
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.Instance.GetRowByName( SKTradeLogicFaction.Commands.InitializeIndustry.ToString() ), GameCommandSource.AnythingElse );
                    command.RelatedEntityIDs.Add( ParentObject.PrimaryKeyID );
                    Context.QueueCommandForSendingAtEndOfContext( command );
                }
                return null;
            }
            if ( !(ParentObject.TypeData.DescriptionAppender is IndustryDescriptionAppender) )
                ParentObject.TypeData.DescriptionAppender = new IndustryDescriptionAppender( ParentObject.TypeData.DescriptionAppender );
            return data;
        }

        public static void SetIndustryData( this GameEntity_Squad ParentObject, CivIndustryData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( (int)CivIndustryDataExternalData.PatternIndex ).Data[0] = data;
        }
    }
}
