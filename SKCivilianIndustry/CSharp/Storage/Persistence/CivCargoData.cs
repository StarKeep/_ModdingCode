using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using SKCivilianIndustry.DescriptionAppenders;
using System;
using System.Collections.Generic;

namespace SKCivilianIndustry.Storage
{
    public class CivCargoData
    {
        // Recommended to use the extention methods at the bottom of this file for ease of use.
        public ArcenSparseLookup<string, int> Amount;
        public ArcenSparseLookup<string, int> Capacity;
        public ArcenSparseLookup<string, int> PerSecond;

        public int BaseCapacity;

        public bool Initialized;

        public int GetAmount( string resource )
        {
            return Amount.GetHasKey( resource ) ? Amount[resource] : 0;
        }
        public void SetAmount( string resource, int valueToSet )
        {
            if ( Amount.GetHasKey( resource ) )
                Amount[resource] = Math.Max( 0, Math.Min( valueToSet, GetCapacity( resource ) ) );
            else
                Amount.AddPair( resource, Math.Max( 0, Math.Min( valueToSet, GetCapacity( resource ) ) ) );
        }
        public void AddAmount( string resource, int amountToAddOrSubtract )
        {
            int requestedAmount = Amount[resource] + amountToAddOrSubtract;
            if ( Amount.GetHasKey( resource ) )
                Amount[resource] = Math.Max( 0, Math.Min( requestedAmount, GetCapacity( resource ) ) );
            else
                Amount.AddPair( resource, Math.Max( 0, Math.Min( requestedAmount, GetCapacity( resource ) ) ) );
        }

        public int GetCapacity( string resource )
        {
            return Capacity.GetHasKey( resource ) ? Capacity[resource] : BaseCapacity;
        }
        public void SetCapacity( string resource, int valueToSet )
        {
            if ( Capacity.GetHasKey( resource ) )
                Capacity[resource] = valueToSet;
            else
                Capacity.AddPair( resource, valueToSet );
        }

        public int GetPerSecond( string resource )
        {
            return PerSecond.GetHasKey( resource ) ? PerSecond[resource] : 0;
        }
        public void SetPerSecond( string resource, int valueToSet )
        {
            if ( PerSecond.GetHasKey( resource ) )
                PerSecond[resource] = valueToSet;
            else
                PerSecond.AddPair( resource, valueToSet );
        }

        private bool CostsCanBeMet
        {
            get
            {
                bool met = true;
                for ( int x = 0; x < Amount.GetPairCount(); x++ )
                {
                    string key = Amount.GetPairByIndex( x ).Key;
                    int required = GetPerSecond( key );
                    if ( required >= 0 )
                        continue;
                    required = Math.Abs( required );
                    if ( required > GetAmount( key ) )
                    {
                        met = false;
                        break;
                    }
                }
                return met;
            }
        }

        public void HandlePerSecondResources()
        {
            if ( !CostsCanBeMet )
                return;
            for ( int y = 0; y < PerSecond.GetPairCount(); y++ )
            {
                string key = PerSecond.GetPairByIndex( y ).Key;
                Amount[key] = Math.Min( GetAmount( key ) + GetPerSecond( key ), GetCapacity( key ) );
            }
        }

        public void Initialize( GameEntity_Base entity )
        {
            if ( Initialized )
                return;

            int resourcesToUse = 99;
            List<string> resourcesFound = new List<string>();
            for ( int x = 0; x < entity.TypeData.TagsList.Count; x++ )
            {
                string baseTag = entity.TypeData.TagsList[x];

                if ( baseTag.Contains( SKTradeLogicFaction.PRODUCTION_TAG ) )
                {
                    resourcesFound.Add( baseTag.Substring( SKTradeLogicFaction.PRODUCTION_TAG.Length ) );
                }
                else if ( baseTag.Contains( SKTradeLogicFaction.RANDOM_TAG ) )
                {
                    if ( !int.TryParse( baseTag.Substring( SKTradeLogicFaction.RANDOM_TAG.Length ), out int toUse ) )
                    {
                        ArcenDebugging.SingleLineQuickDebug( $"Failed to parse {SKTradeLogicFaction.RANDOM_TAG} tag on entity named {entity.TypeData.InternalName}." );
                        continue;
                    }
                    resourcesToUse = toUse;
                }
                else if ( baseTag.Contains( SKTradeLogicFaction.TRADE_STORAGE_TAG ) )
                {
                    if ( !int.TryParse( baseTag.Substring( SKTradeLogicFaction.TRADE_STORAGE_TAG.Length ), out BaseCapacity ) )
                    {
                        ArcenDebugging.SingleLineQuickDebug( $"Failed to parse {SKTradeLogicFaction.TRADE_STORAGE_TAG} tag on entity named {entity.TypeData.InternalName}." );
                        continue;
                    }
                    for ( int y = 0; y < Capacity.GetPairCount(); y++ )
                        Capacity.GetPairByIndex( y ).Value = BaseCapacity;
                }

            }
            while ( resourcesToUse > 0 && resourcesFound.Count > 0 )
            {
                int seed = entity.Planet.Index % resourcesFound.Count;
                string resourceChosen = resourcesFound[seed];
                resourcesFound.RemoveAt( seed );
                if ( !int.TryParse( resourceChosen.Substring( 0, 2 ), out int perSecond ) )
                {
                    ArcenDebugging.SingleLineQuickDebug( $"Failed to parse {SKTradeLogicFaction.PRODUCTION_TAG} on entity named {entity.TypeData.InternalName}." );
                    continue;
                }
                string resource = resourceChosen.Substring( 2 );
                if ( SKTradeLogicFaction.Resources.GetResourceByName( resource ) == null )
                {
                    ArcenDebugging.SingleLineQuickDebug( $"Failed to add a resource named {resource} to {entity.TypeData.InternalName}. Resource was not initialized properly." );
                    continue;
                }
                Amount.AddPair( resource, 0 );
                Capacity.AddPair( resource, BaseCapacity );
                PerSecond.AddPair( resource, perSecond );
                resourcesToUse--;
            }
            Initialized = true;
        }

        public CivCargoData()
        {
            Amount = new ArcenSparseLookup<string, int>();
            Capacity = new ArcenSparseLookup<string, int>();
            PerSecond = new ArcenSparseLookup<string, int>();
            BaseCapacity = 100;
        }

        public CivCargoData( ArcenDeserializationBuffer Buffer ) : this()
        {
            int count = Buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
            {
                string key = Buffer.ReadString_Condensed();
                Amount.AddPair( key, Buffer.ReadInt32( ReadStyle.NonNeg ) );
                Capacity.AddPair( key, Buffer.ReadInt32( ReadStyle.NonNeg ) );
                PerSecond.AddPair( key, Buffer.ReadInt32( ReadStyle.Signed ) );
            }
            BaseCapacity = Buffer.ReadInt32( ReadStyle.NonNeg );
            Initialized = Buffer.ReadBool();
        }

        public void SerializeTo( ArcenSerializationBuffer Buffer )
        {
            int count = Amount.GetPairCount();
            Buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
            {
                string key = Amount.GetPairByIndex( x ).Key;
                Buffer.AddString_Condensed( key );
                Buffer.AddInt32( ReadStyle.NonNeg, GetAmount( key ) );
                Buffer.AddInt32( ReadStyle.NonNeg, GetCapacity( key ) );
                Buffer.AddInt32( ReadStyle.Signed, GetPerSecond( key ) );
            }
            Buffer.AddInt32( ReadStyle.NonNeg, BaseCapacity );
            Buffer.AddItem( Initialized );
        }
    }
    public class CivCargoDataExternalData : IArcenExternalDataPatternImplementation
    {
        private CivCargoData Data;

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
            this.Data = new CivCargoData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer )
        {
            //For saving to disk, translate this object into the buffer
            CivCargoData data = (CivCargoData)Source[0];
            data.SerializeTo( Buffer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            Target[0] = new CivCargoData( Buffer );
        }
    }
    public static class CivCargoDataExtensions
    {
        /// <summary>
        /// If called from non-sim locations, such as UI or Background threads, may cause desync.
        /// </summary>
        /// <param name="ParentObject"></param>
        /// <returns></returns>
        public static CivCargoData GetCargoSimSafeNeverNull( this GameEntity_Squad ParentObject )
        {
            CivCargoData cargoData = (CivCargoData)ParentObject.ExternalData.GetCollectionByPatternIndex( CivCargoDataExternalData.PatternIndex ).Data[0];
            if ( !cargoData.Initialized )
                cargoData.Initialize( ParentObject );
            if ( !(ParentObject.TypeData.DescriptionAppender is CargoDescriptionAppender) )
                ParentObject.TypeData.DescriptionAppender = new CargoDescriptionAppender( ParentObject.TypeData.DescriptionAppender );
            return cargoData;
        }

        /// <summary>
        /// Returns null if the cargo is not yet initialized. If Context is passed, this will queue up a GameCommand for requested cargo to be initialized.
        /// </summary>
        /// <param name="ParentObject"></param>
        /// <param name="Context"></param>
        /// <returns></returns>
        public static CivCargoData GetCargoNotSimSafeMayReturnNull(this GameEntity_Squad ParentObject, ArcenLongTermIntermittentPlanningContext Context = null )
        {
            CivCargoData cargoData = (CivCargoData)ParentObject.ExternalData.GetCollectionByPatternIndex( CivCargoDataExternalData.PatternIndex ).Data[0];
            if ( !cargoData.Initialized )
            {
                if (Context != null )
                {
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.Instance.GetRowByName( SKTradeLogicFaction.Commands.InitializeCargo.ToString() ), GameCommandSource.AnythingElse );
                    command.RelatedEntityIDs.Add( ParentObject.PrimaryKeyID );
                    Context.QueueCommandForSendingAtEndOfContext( command );
                }
                return null;
            }
            if ( !(ParentObject.TypeData.DescriptionAppender is CargoDescriptionAppender) )
                ParentObject.TypeData.DescriptionAppender = new CargoDescriptionAppender( ParentObject.TypeData.DescriptionAppender );
            return cargoData;
        }

        public static void SetCargoData( this GameEntity_Squad ParentObject, CivCargoData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( (int)CivCargoDataExternalData.PatternIndex ).Data[0] = data;
        }
    }
}
