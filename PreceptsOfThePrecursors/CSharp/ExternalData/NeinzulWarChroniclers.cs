using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace PreceptsOfThePrecursors
{
    public class NeinzulWarChroniclersData
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

        public int EstimatedStrengthOfAttack( Faction faction, bool isForDisplayOnly = true )
        {
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
            this.DeserializedIntoSelf( Buffer, false );
        }
        public void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
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
        public void DeserializedIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            if ( IsForPartialSyncDuringMultiplayer )
                DeserializedChangedValuesIntoSelf( buffer );
            else
            {
                if ( BudgetGenerated == null )
                    BudgetGenerated = new ArcenSparseLookup<string, ArcenSparseLookup<byte, int>>();

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
        }
        public void DeserializedChangedValuesIntoSelf( ArcenDeserializationBuffer buffer )
        {
            if ( BudgetGenerated == null )
                BudgetGenerated = new ArcenSparseLookup<string, ArcenSparseLookup<byte, int>>();

            int readInt = buffer.ReadInt32( ReadStyle.NonNeg );
            PersonalBudget = PersonalBudget != readInt ? readInt : PersonalBudget;

            int count = buffer.ReadInt32( ReadStyle.NonNeg );
            for(int x = 0; x < count; x++ )
            {
                string key = buffer.ReadString_Condensed();
                if ( !BudgetGenerated.GetHasKey( key ) )
                    BudgetGenerated.AddPair( key, new ArcenSparseLookup<byte, int>() );

                int subCount = buffer.ReadInt32( ReadStyle.NonNeg );
                for(int y = 0; y < subCount; y++ )
                {
                    byte subKey = buffer.ReadByte( ReadStyleByte.Normal );
                    int subValue = buffer.ReadInt32( ReadStyle.NonNeg );

                    if ( !BudgetGenerated[key].GetHasKey( subKey ) )
                        BudgetGenerated[key].AddPair( subKey, subValue );
                    else if ( BudgetGenerated[key][subKey] != subValue )
                        BudgetGenerated[key][subKey] = subValue;
                }
            }

            short readShort = buffer.ReadInt16( ReadStyle.PosExceptNeg1 );
            currentPlanetAimedAt = currentPlanetAimedAt != readShort ? readShort : currentPlanetAimedAt;

            readInt = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            GameSecondAimed = GameSecondAimed != readInt ? readInt : GameSecondAimed;

            readShort = buffer.ReadInt16( ReadStyle.PosExceptNeg1 );
            currentPlanetWeAreDepartingFrom = currentPlanetWeAreDepartingFrom != readShort ? readShort : currentPlanetWeAreDepartingFrom;

            readInt = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            GameSecondDepartingStarted = GameSecondDepartingStarted != readInt ? readInt : GameSecondDepartingStarted;

            readInt = buffer.ReadInt32( ReadStyle.NonNeg );
            SentAttacks = SentAttacks != readInt ? readInt : SentAttacks;
        }
    }
    public class NeinzulWarChroniclersExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private NeinzulWarChroniclersData Data;

        public static int PatternIndex;

        public void ReceivePatternIndex( int Index )
        {
            PatternIndex = Index; //for internal use with the ExternalData code in the game engine itself
        }
        public int GetNumberOfItems()
        {
            return 1; //for internal use with the ExternalData code in the game engine itself
        }

        public Faction ParentFaction;
        public void InitializeData( object ParentObject, object[] Target )
        {
            this.ParentFaction = ParentObject as Faction;
            if ( this.ParentFaction == null && ParentObject != null )
                return;

            //this initialization is handled by the data structure itself
            this.Data = new NeinzulWarChroniclersData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            NeinzulWarChroniclersData data = (NeinzulWarChroniclersData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            if ( IsForPartialSyncDuringMultiplayer )
            {
                //this is a partial sync, so use existing object and write into it
                (Target[0] as NeinzulWarChroniclersData).DeserializedIntoSelf( Buffer, IsForPartialSyncDuringMultiplayer );
            }
            else
            {
                //this is a full sync, so create a new object
                Target[0] = new NeinzulWarChroniclersData( Buffer );
            }
        }
    }
    public static class NeinzulWarChroniclersExternalDataExtensions
    {
        // This loads the data assigned to whatever ParentObject you pass. So, say, you could assign the same class to different ships, and each would be able to get back the values assigned to it.
        // In our specific case here, we're going to be assigning a dictionary to every faction.
        public static NeinzulWarChroniclersData GetNeinzulWarChroniclersData( this Faction ParentObject )
        {
            return (NeinzulWarChroniclersData)ParentObject.ExternalData.GetCollectionByPatternIndex( NeinzulWarChroniclersExternalData.PatternIndex ).Data[0];
        }
        // This meanwhile saves the data, assigning it to whatever ParentObject you pass.
        public static void SetNeinzulWarChroniclersData( this Faction ParentObject, NeinzulWarChroniclersData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( NeinzulWarChroniclersExternalData.PatternIndex ).Data[0] = data;
        }
    }
}
