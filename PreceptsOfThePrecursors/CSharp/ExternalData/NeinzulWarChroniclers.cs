using System;
using System.Collections.Generic;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace PreceptsOfThePrecursors
{
    public class NeinzulWarChroniclersData : ArcenExternalSubManagedData
    {
        public int Version;

        // Our personal budget.
        public int PersonalBudget;
        public int ReadiedChroniclers { get { return PersonalBudget / 1000000; } }

        // The budget we have stored up from past fights.
        public ArcenSparseLookup<string, int> BudgetGenerated;

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
            int strength = BudgetGenerated[typeName];
            return strength;
        }
        public int EstimatedStrengthOfAttack( Faction faction, int markLevel, bool isForDisplayOnly = true )
        {
            if ( World_AIW2.Instance.GameSecond - LastSecondCached < 10 && CachedEstimatedStrengthOfAttack > 0 )
                return CachedEstimatedStrengthOfAttack; // Regenerate strength every 10 seconds.

            int strengthCap = 0;
            strengthCap += NeinzulWarChroniclers.SoftStrengthCapBase;
            strengthCap += NeinzulWarChroniclers.SoftStrengthCapIncreasePerAttack * SentAttacks;
            strengthCap += NeinzulWarChroniclers.SoftStrengthCapIncreasePerAttackPerIntensity * SentAttacks * faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.ReturnNullIfNotFound ).Intensity;
            strengthCap += (World_AIW2.Instance.GameSecond * FInt.FromParts( (NeinzulWarChroniclers.SoftStrengthCapIncreasePerHour + (NeinzulWarChroniclers.SoftStrengthCapIncreasePerHourPerIntensity * faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.ReturnNullIfNotFound ).Intensity)), 000 ) / 3600).GetNearestIntPreferringHigher();

            int strength = GameEntityTypeDataTable.Instance.GetRowByName( NeinzulWarChroniclers.Tags.NeinzulWarChronicler.ToString() ).GetForMark( (faction.Implementation as NeinzulWarChroniclers).ChroniclersMarkLevel( faction ) ).StrengthPerSquad_CalculatedWithNullFleetMembership;

            BudgetGenerated.Sort( ( pair1, pair2 ) =>
            {
                return -(GameEntityTypeDataTable.Instance.GetRowByName( pair1.Key ).GetForMark( 1 ).StrengthPerSquad_CalculatedWithNullFleetMembership - GameEntityTypeDataTable.Instance.GetRowByName( pair2.Key ).GetForMark( 1 ).StrengthPerSquad_CalculatedWithNullFleetMembership);
            } );

            BudgetGenerated.DoFor( pair =>
            {
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( pair.Key );
                if ( entityData == null )
                    if ( isForDisplayOnly )
                        return DelReturn.Continue;
                    else
                        return DelReturn.RemoveAndContinue;


                    int perUnitStrength = entityData.GetForMark( markLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                    int cost = perUnitStrength * 10;
                    strength += perUnitStrength * (pair.Value / cost);

                if ( strength > strengthCap )
                    return DelReturn.Break;

                return DelReturn.Continue;
            } );

            CachedEstimatedStrengthOfAttack = strength;
            LastSecondCached = World_AIW2.Instance.GameSecond;

            return strength;
        }

        public void AddBudget( GameEntity_Squad entity, int budgetToAdd )
        {
            AddBudget( entity.TypeData, entity.CurrentMarkLevel, budgetToAdd );
        }

        public void AddBudget( GameEntityTypeData entityData, int markLevelToAdd, int budgetToAdd )
        {
            if ( !BudgetGenerated.GetHasKey( entityData.InternalName ) )
                BudgetGenerated.AddPair( entityData.InternalName, 0 );

            int capacity = entityData.GetForMark( markLevelToAdd ).StrengthPerSquad_CalculatedWithNullFleetMembership * 500;

            AddBudget( entityData.InternalName, budgetToAdd, capacity );   
        }

        public void AddBudget( string entityName, int budgetToAdd, int capacity )
        {
            BudgetGenerated[entityName] = Math.Min( capacity, BudgetGenerated[entityName] + budgetToAdd );
        }

        public NeinzulWarChroniclersData()
        {
            PersonalBudget = 0;
            BudgetGenerated = new ArcenSparseLookup<string, int>();
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
            buffer.AddInt32( ReadStyle.NonNeg, 2 );

            buffer.AddInt32( ReadStyle.NonNeg, PersonalBudget );
            int count = BudgetGenerated.GetPairCount();
            buffer.AddInt32( ReadStyle.NonNeg, count );
            BudgetGenerated.DoFor( pair =>
            {
                buffer.AddString_Condensed( pair.Key );
                buffer.AddInt32( ReadStyle.PosExceptNeg1, pair.Value );

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
            Version = buffer.ReadInt32( ReadStyle.NonNeg );

            if ( BudgetGenerated == null )
                BudgetGenerated = new ArcenSparseLookup<string, int>();
            else
                BudgetGenerated.Clear();

            PersonalBudget = buffer.ReadInt32( ReadStyle.NonNeg );
            int count = buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
            {
                string key = buffer.ReadString_Condensed();
                int value = 0;
                if ( Version >= 2 )
                    value = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
                else
                {
                    int subCount = buffer.ReadInt32( ReadStyle.NonNeg );
                    for ( int y = 0; y < subCount; y++ )
                    {
                        buffer.ReadByte( ReadStyleByte.Normal );
                        buffer.ReadInt32( ReadStyle.NonNeg );
                    }
                }
                BudgetGenerated.AddPair( key, value );
            }
            currentPlanetAimedAt = buffer.ReadInt16( ReadStyle.PosExceptNeg1 );
            GameSecondAimed = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            currentPlanetWeAreDepartingFrom = buffer.ReadInt16( ReadStyle.PosExceptNeg1 );
            GameSecondDepartingStarted = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            SentAttacks = buffer.ReadInt32( ReadStyle.NonNeg );
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
