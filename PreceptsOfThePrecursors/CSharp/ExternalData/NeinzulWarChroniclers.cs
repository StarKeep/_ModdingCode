using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace PreceptsOfThePrecursors
{
    public class NeinzulWarChroniclersData : ArcenExternalSubManagedData
    {
        // Our personal budget.
        public int PersonalBudget;
        public int ReadiedChroniclers { get { return PersonalBudget / 1000000; } }

        // The budget we have stored up from past fights.
        public ArcenSparseLookup<string, ArcenSparseLookup<byte, int>> BudgetGenerated;

        private short currentPlanetAimedAt;
        public Planet CurrentPlanetAimedAt { get { return World_AIW2.Instance.GetPlanetByIndex( currentPlanetAimedAt ); } set { currentPlanetAimedAt = (short)(value != null ? value.Index : -1); } }

        public int GameSecondAimed;
        public int SecondsAimedAtPlanet { get { return World_AIW2.Instance.GameSecond - GameSecondAimed; } }

        private short currentPlanetWeAreDepartingFrom;
        public Planet CurrentPlanetWeAreDepartingFrom { get { return World_AIW2.Instance.GetPlanetByIndex( currentPlanetWeAreDepartingFrom ); } set { currentPlanetWeAreDepartingFrom = (short)(value != null ? value.Index : -1); } }

        public int GameSecondDepartingStarted;
        public int SecondsSinceDepartingStarted { get { return World_AIW2.Instance.GameSecond - GameSecondDepartingStarted; } }

        public int SentAttacks;

        public int CachedEstimatedStrengthOfAttack = -1;
        public int LastSecondCached = -1;
        public int StrengthStoredForType( string typeName )
        {
            if ( !BudgetGenerated.GetHasKey( typeName ) )
                return 0;
            GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRowByName( typeName );
            if ( typeName == null )
                return 0;
            int strength = 0;
            BudgetGenerated[typeName].DoFor( pair =>
            {
                int perUnitStrength = typeData.GetForMark( pair.Key ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                int cost = perUnitStrength * 10;
                strength += perUnitStrength * (pair.Value / cost);

                return DelReturn.Continue;
            } );
            return strength;
        }
        public int EstimatedStrengthOfAttack( Faction faction, bool isForDisplayOnly = true )
        {
            if ( World_AIW2.Instance.GameSecond - LastSecondCached < 10 && CachedEstimatedStrengthOfAttack > 0 )
                return CachedEstimatedStrengthOfAttack; // Regenerate strength every 10 seconds.

            int strength = GameEntityTypeDataTable.Instance.GetRowByName( NeinzulWarChroniclers.Tags.NeinzulWarChronicler.ToString() ).GetForMark( (faction.Implementation as NeinzulWarChroniclers).ChroniclersMarkLevel( faction ) ).StrengthPerSquad_CalculatedWithNullFleetMembership;
            BudgetGenerated.DoFor( pair =>
            {
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( pair.Key );
                if ( entityData == null )
                    if ( isForDisplayOnly )
                        return DelReturn.Continue;
                    else
                        return DelReturn.RemoveAndContinue;

                pair.Value.DoFor( subPair =>
                {
                    int perUnitStrength = entityData.GetForMark( subPair.Key ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                    int cost = perUnitStrength * 10;
                    strength += perUnitStrength * (subPair.Value / cost);

                    return DelReturn.Continue;
                } );

                return DelReturn.Continue;
            } );

            CachedEstimatedStrengthOfAttack = strength;
            LastSecondCached = World_AIW2.Instance.GameSecond;

            return strength;
        }

        public void AddBudget( GameEntity_Squad entity, int budgetToAdd )
        {
            if ( !BudgetGenerated.GetHasKey( entity.TypeData.InternalName ) )
                BudgetGenerated.AddPair( entity.TypeData.InternalName, new ArcenSparseLookup<byte, int>() );
            if ( !BudgetGenerated[entity.TypeData.InternalName].GetHasKey( entity.CurrentMarkLevel ) )
                BudgetGenerated[entity.TypeData.InternalName].AddPair( entity.CurrentMarkLevel, 0 );

            int capacity = entity.TypeData.GetForMark( entity.CurrentMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership * 500;

            BudgetGenerated[entity.TypeData.InternalName][entity.CurrentMarkLevel] = Math.Min( capacity, BudgetGenerated[entity.TypeData.InternalName][entity.CurrentMarkLevel] + budgetToAdd );
        }

        public NeinzulWarChroniclersData()
        {
            PersonalBudget = 0;
            BudgetGenerated = new ArcenSparseLookup<string, ArcenSparseLookup<byte, int>>();
            currentPlanetAimedAt = -1;
            GameSecondAimed = -1;
            currentPlanetWeAreDepartingFrom = -1;
            GameSecondDepartingStarted = -1;
            SentAttacks = 0;
        }
        public NeinzulWarChroniclersData( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializeIntoSelf( Buffer, false );
        }
        public override void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            buffer.AddInt32( ReadStyle.NonNeg, PersonalBudget );
            int count = BudgetGenerated.GetPairCount();
            buffer.AddInt32( ReadStyle.NonNeg, count );
            BudgetGenerated.DoFor( pair =>
            {
                buffer.AddString_Condensed( pair.Key );
                int subCount = pair.Value.GetPairCount();
                buffer.AddInt32( ReadStyle.NonNeg, subCount );
                pair.Value.DoFor( subPair =>
                {
                    buffer.AddByte( ReadStyleByte.Normal, subPair.Key );
                    buffer.AddInt32( ReadStyle.NonNeg, subPair.Value );

                    return DelReturn.Continue;
                } );

                return DelReturn.Continue;
            } );
            buffer.AddInt16( ReadStyle.PosExceptNeg1, currentPlanetAimedAt );
            buffer.AddInt32( ReadStyle.PosExceptNeg1, GameSecondAimed );
            buffer.AddInt16( ReadStyle.PosExceptNeg1, currentPlanetWeAreDepartingFrom );
            buffer.AddInt32( ReadStyle.PosExceptNeg1, GameSecondDepartingStarted );
            buffer.AddInt32( ReadStyle.NonNeg, SentAttacks );
        }
        public override void DeserializeIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            if ( BudgetGenerated == null )
                BudgetGenerated = new ArcenSparseLookup<string, ArcenSparseLookup<byte, int>>();
            else if ( IsForPartialSyncDuringMultiplayer )
                BudgetGenerated.Clear();

            PersonalBudget = buffer.ReadInt32( ReadStyle.NonNeg );
            int count = buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
            {
                string key = buffer.ReadString_Condensed();
                BudgetGenerated.AddPair( key, new ArcenSparseLookup<byte, int>() );
                int subCount = buffer.ReadInt32( ReadStyle.NonNeg );
                for ( int y = 0; y < subCount; y++ )
                    BudgetGenerated[key].AddPair( buffer.ReadByte( ReadStyleByte.Normal ), buffer.ReadInt32( ReadStyle.NonNeg ) );
            }
            currentPlanetAimedAt = buffer.ReadInt16( ReadStyle.PosExceptNeg1 );
            GameSecondAimed = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            currentPlanetWeAreDepartingFrom = buffer.ReadInt16( ReadStyle.PosExceptNeg1 );
            GameSecondDepartingStarted = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            SentAttacks = buffer.ReadInt32( ReadStyle.NonNeg );
        }

        public void OutputToDebugLog()
        {
            BudgetGenerated.DoFor( pair =>
            {
                if ( pair.Value == null || pair.Value.GetPairCount() < 1 )
                    return DelReturn.Continue;
                ArcenDebugging.SingleLineQuickDebug( $"Budget for {pair.Key}: " );
                int total = 0;

                pair.Value.DoFor( subPair =>
                {
                    ArcenDebugging.SingleLineQuickDebug( $"{subPair.Key} - {subPair.Value}; " );
                    total += subPair.Value;
                    return DelReturn.Continue;
                } );

                ArcenDebugging.SingleLineQuickDebug( $" Total: {total}" );

                return DelReturn.Continue;
            } );
        }
    }
    public class NeinzulWarChroniclersExternalData : ArcenExternalDataPatternImplementationBase_Faction
    {
        private NeinzulWarChroniclersData Data;
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
            this.Data = new NeinzulWarChroniclersData();
            Target[0] = this.Data;
        }
        public override void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            NeinzulWarChroniclersData data = (NeinzulWarChroniclersData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        protected override void DeserializeExternalData( Faction Parent, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            this.DeserializeExternalDataAsArcenExternalSubManagedData<NeinzulWarChroniclersData>( Target, Buffer, IsForPartialSyncDuringMultiplayer );
        }
    }

    public static class NeinzulWarChroniclersExternalDataExtensions
    {
        public static NeinzulWarChroniclersData GetNeinzulWarChroniclersData( this Faction ParentObject, ExternalDataRetrieval RetrievalRules )
        {
            ArcenExternalData extData = ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, NeinzulWarChroniclersExternalData.PatternIndex, RetrievalRules );
            if ( extData == null )
                return null;
            return (NeinzulWarChroniclersData)extData.Data[0];
        }

        public static void SetNeinzulWarChroniclersData( this Faction ParentObject, NeinzulWarChroniclersData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, (int)NeinzulWarChroniclersExternalData.PatternIndex, ExternalDataRetrieval.CreateIfNotFound ).Data[0] = data;
        }
    }
}
