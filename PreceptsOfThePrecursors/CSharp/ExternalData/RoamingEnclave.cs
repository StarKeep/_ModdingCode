using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using System;

namespace PreceptsOfThePrecursors
{
    public class EnclaveFactionData
    {
        public ArcenLessLinkedList<Fireteam> Teams;
        public ArcenSparseLookup<Planet, FireteamRegiment> TeamsAimedAtPlanet;
        public int SecondsUntilNextRespawn;

        public EnclaveFactionData()
        {
            Teams = new ArcenLessLinkedList<Fireteam>();
            TeamsAimedAtPlanet = new ArcenSparseLookup<Planet, FireteamRegiment>();
            SecondsUntilNextRespawn = 11;
        }
        public void SerializeTo( ArcenSerializationBuffer buffer )
        {
            FireteamUtility.SerializeFireteams( buffer, Teams );
            buffer.AddInt32( ReadStyle.PosExceptNeg1, SecondsUntilNextRespawn );
        }
        public EnclaveFactionData( ArcenDeserializationBuffer buffer ) : this()
        {
            FireteamUtility.DeserializeFireteams( buffer, Teams, "Roaming Enclave" );
            SecondsUntilNextRespawn = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
        }
    }
    public class EnclaveFactionExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private EnclaveFactionData Data;

        public static int PatternIndex;

        // So this is essentially what type of thing we're going to 'attach' our class to.
        public static string RelatedParentTypeName = "Faction";

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
            this.Data = new EnclaveFactionData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer )
        {
            //For saving to disk, translate this object into the buffer
            EnclaveFactionData data = (EnclaveFactionData)Source[0];
            data.SerializeTo( Buffer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            Target[0] = new EnclaveFactionData( Buffer );
        }
    }
    public static class EnclaveFactionExternalDataExtensions
    {
        // This loads the data assigned to whatever ParentObject you pass. So, say, you could assign the same class to different ships, and each would be able to get back the values assigned to it.
        // In our specific case here, we're going to be assigning a dictionary to every faction.
        public static EnclaveFactionData GetEnclaveFactionData( this Faction ParentObject )
        {
            return (EnclaveFactionData)ParentObject.ExternalData.GetCollectionByPatternIndex( EnclaveFactionExternalData.PatternIndex ).Data[0];
        }
        // This meanwhile saves the data, assigning it to whatever ParentObject you pass.
        public static void SetEnclaveFactionData( this Faction ParentObject, EnclaveFactionData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( EnclaveFactionExternalData.PatternIndex ).Data[0] = data;
        }
    }

    public class EnclaveWorldData
    {
        public int SecondsUntilNextInflux;

        public EnclaveWorldData()
        {
            SecondsUntilNextInflux = 900;
        }
        public void SerializeTo( ArcenSerializationBuffer buffer )
        {
            buffer.AddInt32( ReadStyle.NonNeg, SecondsUntilNextInflux );
        }
        public EnclaveWorldData( ArcenDeserializationBuffer buffer ) : this()
        {
            SecondsUntilNextInflux = buffer.ReadInt32( ReadStyle.NonNeg );
        }
    }
    public class EnclaveWorldExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private EnclaveWorldData Data;

        public static int PatternIndex;

        // So this is essentially what type of thing we're going to 'attach' our class to.
        public static string RelatedParentTypeName = "World";

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
            this.Data = new EnclaveWorldData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer )
        {
            //For saving to disk, translate this object into the buffer
            EnclaveWorldData data = (EnclaveWorldData)Source[0];
            data.SerializeTo( Buffer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            Target[0] = new EnclaveWorldData( Buffer );
        }
    }
    public static class EnclaveWorldExternalDataExtensions
    {
        // This loads the data assigned to whatever ParentObject you pass. So, say, you could assign the same class to different ships, and each would be able to get back the values assigned to it.
        // In our specific case here, we're going to be assigning a dictionary to every faction.
        public static EnclaveWorldData GetEnclaveWorldData( this World ParentObject )
        {
            return (EnclaveWorldData)ParentObject.ExternalData.GetCollectionByPatternIndex( EnclaveWorldExternalData.PatternIndex ).Data[0];
        }
        // This meanwhile saves the data, assigning it to whatever ParentObject you pass.
        public static void SetEnclaveWorldData( this World ParentObject, EnclaveWorldData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( EnclaveWorldExternalData.PatternIndex ).Data[0] = data;
        }
    }

    public class EnclaveYounglingsStorageDescriptionAppender : IGameEntityDescriptionAppender
    {
        public void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenDoubleCharacterBuffer Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;

            // Testchamber.
            if ( RelatedEntityOrNull.PlanetFaction.Faction.Type == FactionType.Player )
                return;

            StoredYounglingsData storage = RelatedEntityOrNull.GetStoredYounglings();
            if ( storage.StoredYounglings.GetPairCount() > 0 )
                Buffer.Add( $"This Enclave contains the following Younglings of total strength {RelatedEntityOrNull.AdditionalStrengthFromFactions}.\n" );
            for ( int x = 0; x < storage.StoredYounglings.GetPairCount(); x++ )
            {
                int count = 0;
                int strength = 0;
                Unit unit = storage.StoredYounglings.GetPairByIndex( x ).Key;
                GameEntityTypeData unitData = GameEntityTypeDataTable.Instance.GetRowByName( unit.ToString() );
                for ( int y = 0; y < storage.StoredYounglings[unit].UnitsByMark.GetPairCount(); y++ )
                {
                    ArcenSparseLookupPair<byte, int> pair = storage.StoredYounglings[unit].UnitsByMark.GetPairByIndex( y );
                    count += pair.Value;
                    strength += unitData.GetForMark( pair.Key ).StrengthPerSquad_Original_DoesNotIncreaseWithMarkLevel * pair.Value;
                }
                Buffer.Add( $"{count} {unitData.DisplayName} ({(double)strength / 1000} strength)\n" );
            }
        }
    }

    public class YounglingCollection
    {
        public ArcenSparseLookup<byte, int> UnitsByMark;
        private int strength;
        public int Strength { get { return strength; } }

        public void AddStrength(int strength )
        {
            this.strength += strength;
        }

        public void SubtractStrength(int strength )
        {
            this.strength -= strength;
        }

        public void AddYoungling( GameEntity_Squad youngling )
        {
            if ( !UnitsByMark.GetHasKey( youngling.CurrentMarkLevel ) )
                UnitsByMark.AddPair( youngling.CurrentMarkLevel, 1 + youngling.ExtraStackedSquadsInThis );
            else
                UnitsByMark[youngling.CurrentMarkLevel] += 1 + youngling.ExtraStackedSquadsInThis;
            strength += youngling.GetStrengthPerSquad() * (1 + youngling.ExtraStackedSquadsInThis);
        }

        public YounglingCollection()
        {
            UnitsByMark = new ArcenSparseLookup<byte, int>();
            strength = 0;
        }

        public void SerializeTo( ArcenSerializationBuffer buffer )
        {
            int count = UnitsByMark.GetPairCount();
            buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
            {
                ArcenSparseLookupPair<byte, int> pair = UnitsByMark.GetPairByIndex( x );
                buffer.AddByte( ReadStyleByte.Normal, pair.Key );
                buffer.AddInt32( ReadStyle.NonNeg, pair.Value );
            }
            buffer.AddInt32( ReadStyle.NonNeg, strength );
        }

        public YounglingCollection( ArcenDeserializationBuffer buffer ) : this()
        {
            int count = buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
                UnitsByMark.AddPair( buffer.ReadByte( ReadStyleByte.Normal ), buffer.ReadInt32( ReadStyle.NonNeg ) );
            strength = buffer.ReadInt32( ReadStyle.NonNeg );
        }
    }

    public class StoredYounglingsData
    {
        public ArcenSparseLookup<Unit, YounglingCollection> StoredYounglings;
        public int TotalStrength
        {
            get
            {
                int value = 0;
                for ( int x = 0; x < StoredYounglings.GetPairCount(); x++ )
                    value += StoredYounglings.GetPairByIndex( x ).Value.Strength;
                return value;
            }
        }

        public bool AddYoungling( GameEntity_Squad youngling )
        {
            if ( Enum.TryParse( youngling.TypeData.InternalName, out Unit unitName ) )
            {
                if ( !StoredYounglings.GetHasKey( unitName ) )
                    StoredYounglings.AddPair( unitName, new YounglingCollection() );
                StoredYounglings[unitName].AddYoungling( youngling );
                return true;
            }
            return false;
        }

        public void DeployYounglings( GameEntity_Squad enclave, ArcenSimContext Context )
        {
            for ( int x = 0; x < StoredYounglings.GetPairCount(); x++ )
            {
                Unit unitName = StoredYounglings.GetPairByIndex( x ).Key;
                GameEntityTypeData unitData = GameEntityTypeDataTable.Instance.GetRowByName( unitName.ToString() );
                YounglingCollection collection = StoredYounglings[unitName];
                for ( int i = 0; i < collection.UnitsByMark.GetPairCount(); i++ )
                {
                    byte markLevel = collection.UnitsByMark.GetPairByIndex( i ).Key;
                    int toSpawnInTotal = collection.UnitsByMark[markLevel];
                    int StackingCutoff = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "StackingCutoffNPCs" );
                    while( toSpawnInTotal > 0 )
                    {
                        short toSpawn = (short)Math.Min( toSpawnInTotal, StackingCutoff );
                        GameEntity_Squad youngling = GameEntity_Squad.CreateNew( enclave.PlanetFaction, unitData, markLevel, enclave.PlanetFaction.FleetUsedAtPlanet, 1, enclave.WorldLocation, Context );
                        youngling.AddOrSetExtraStackedSquadsInThis( (short)(toSpawn - 1), true );
                        youngling.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, enclave.PlanetFaction.Faction.FactionIndex );
                        youngling.MinorFactionStackingID = enclave.PrimaryKeyID;
                        toSpawnInTotal -= toSpawn;
                    }
                }
            }
            StoredYounglings = new ArcenSparseLookup<Unit, YounglingCollection>();
        }

        public void AttemptToCombineYounglings( GameEntity_Squad enclave, Unit unit )
        {
            if ( !StoredYounglings.GetHasKey( unit ) )
                return;

            YounglingCollection collection = StoredYounglings[unit];

            GameEntityTypeData unitData = GameEntityTypeDataTable.Instance.GetRowByName( unit.ToString() );

            for ( byte x = 1, y = 7; x < 7; x++, y-- )
            {
                if ( collection.UnitsByMark.GetHasKey( x ) )
                {
                    if ( collection.UnitsByMark[x] > y * 2 )
                    {
                        collection.SubtractStrength( unitData.GetForMark( x ).GetCalculatedStrengthPerSquadForFleetOrNull(null) * y);
                        collection.AddStrength( unitData.GetForMark( (byte)(x + 1) ).GetCalculatedStrengthPerSquadForFleetOrNull( null ) );

                        collection.UnitsByMark[x] -= y;
                        if ( collection.UnitsByMark.GetHasKey( (byte)(x + 1) ) )
                            collection.UnitsByMark[(byte)(x + 1)]++;
                        else
                            collection.UnitsByMark.AddPair( (byte)(x + 1), 1 );
                    }
                }
            }
        }

        public StoredYounglingsData()
        {
            StoredYounglings = new ArcenSparseLookup<Unit, YounglingCollection>();
        }
        public void SerializeTo( ArcenSerializationBuffer buffer )
        {
            int count = StoredYounglings.GetPairCount();
            buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
            {
                ArcenSparseLookupPair<Unit, YounglingCollection> pair = StoredYounglings.GetPairByIndex( x );
                buffer.AddByte( ReadStyleByte.Normal, (byte)pair.Key );
                pair.Value.SerializeTo( buffer );
            }
        }
        public StoredYounglingsData( ArcenDeserializationBuffer buffer ) : this()
        {
            int count = buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
                StoredYounglings.AddPair( (Unit)buffer.ReadByte( ReadStyleByte.Normal ), new YounglingCollection( buffer ) );
        }
    }

    public class StoredYounglingsExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private StoredYounglingsData Data;

        public static int PatternIndex;

        // So this is essentially what type of thing we're going to 'attach' our class to.
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
            this.Data = new StoredYounglingsData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer )
        {
            //For saving to disk, translate this object into the buffer
            StoredYounglingsData data = (StoredYounglingsData)Source[0];
            data.SerializeTo( Buffer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            Target[0] = new StoredYounglingsData( Buffer );
        }
    }
    public static class StoredYounglingsExternalDataExtensions
    {
        public static void StoreYoungling( this GameEntity_Squad enclave, GameEntity_Squad youngling, ArcenSimContext Context )
        {
            StoredYounglingsData storage = enclave.GetStoredYounglings();
            enclave.AdditionalStrengthFromFactions = 0;
            if ( Enum.TryParse( youngling.TypeData.InternalName, out Unit unitType ) )
            {
                if ( storage.AddYoungling( youngling ) )
                    youngling.Despawn( Context, true, InstancedRendererDeactivationReason.GettingIntoTransport );
            }
            enclave.AdditionalStrengthFromFactions = storage.TotalStrength;
        }

        public static void UnloadYounglings( this GameEntity_Squad enclave, ArcenSimContext Context )
        {
            StoredYounglingsData storage = enclave.GetStoredYounglings();
            storage.DeployYounglings( enclave, Context );
            enclave.AdditionalStrengthFromFactions = 0;
        }

        public static void CombineYounglingsIfAble(this GameEntity_Squad enclave, ArcenSimContext Context )
        {
            enclave.AdditionalStrengthFromFactions = 0;
            
            StoredYounglingsData collection = enclave.GetStoredYounglings();
            for ( int x = 0; x < collection.StoredYounglings.GetPairCount(); x++ )
                collection.AttemptToCombineYounglings( enclave, collection.StoredYounglings.GetPairByIndex( x ).Key );

            enclave.AdditionalStrengthFromFactions = collection.TotalStrength;
        }

        // This loads the data assigned to whatever ParentObject you pass. So, say, you could assign the same class to different ships, and each would be able to get back the values assigned to it.
        // In our specific case here, we're going to be assigning a dictionary to every faction.
        public static StoredYounglingsData GetStoredYounglings( this GameEntity_Squad ParentObject )
        {
            return (StoredYounglingsData)ParentObject.ExternalData.GetCollectionByPatternIndex( StoredYounglingsExternalData.PatternIndex ).Data[0];
        }
        // This meanwhile saves the data, assigning it to whatever ParentObject you pass.
        public static void SetStoredYounglings( this GameEntity_Squad ParentObject, StoredYounglingsData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( StoredYounglingsExternalData.PatternIndex ).Data[0] = data;
        }
    }
}
