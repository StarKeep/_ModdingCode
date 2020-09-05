using System;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

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
        public EnclaveFactionData( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializedIntoSelf( Buffer, false );
        }
        public void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            FireteamUtility.SerializeFireteams( buffer, Teams );
            buffer.AddInt32( ReadStyle.PosExceptNeg1, SecondsUntilNextRespawn );
        }
        public void DeserializedIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            if ( IsForPartialSyncDuringMultiplayer )
                DeserializedChangedValuesIntoSelf( buffer );
            else
            {
                if ( Teams == null )
                    Teams = new ArcenLessLinkedList<Fireteam>();
                if ( TeamsAimedAtPlanet == null )
                    TeamsAimedAtPlanet = new ArcenSparseLookup<Planet, FireteamRegiment>();

                FireteamUtility.DeserializeFireteamsAndDiscardAnyExtraLeftovers( buffer, Teams, "Roaming Enclave" );
                SecondsUntilNextRespawn = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            }
        }
        public void DeserializedChangedValuesIntoSelf( ArcenDeserializationBuffer buffer )
        {
            if ( Teams == null )
                Teams = new ArcenLessLinkedList<Fireteam>();
            if ( TeamsAimedAtPlanet == null )
                TeamsAimedAtPlanet = new ArcenSparseLookup<Planet, FireteamRegiment>();

            FireteamUtility.DeserializeFireteamsAndDiscardAnyExtraLeftovers( buffer, Teams, "Roaming Enclave" );

            int readInt = buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            SecondsUntilNextRespawn = SecondsUntilNextRespawn != readInt ? readInt : SecondsUntilNextRespawn;
        }
    }
    public class EnclaveFactionExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private EnclaveFactionData Data;

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
            this.Data = new EnclaveFactionData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            EnclaveFactionData data = (EnclaveFactionData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            if ( IsForPartialSyncDuringMultiplayer )
            {
                //this is a partial sync, so use existing object and write into it
                (Target[0] as EnclaveFactionData).DeserializedIntoSelf( Buffer, IsForPartialSyncDuringMultiplayer );
            }
            else
            {
                //this is a full sync, so create a new object
                Target[0] = new EnclaveFactionData( Buffer );
            }
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
        public EnclaveWorldData( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializedIntoSelf( Buffer, false );
        }
        public void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            buffer.AddInt32( ReadStyle.NonNeg, SecondsUntilNextInflux );
        }
        public void DeserializedIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            if ( IsForPartialSyncDuringMultiplayer )
                DeserializedChangedValuesIntoSelf( buffer );
            else
                SecondsUntilNextInflux = buffer.ReadInt32( ReadStyle.NonNeg );
        }
        public void DeserializedChangedValuesIntoSelf( ArcenDeserializationBuffer buffer )
        {
            int readInt = buffer.ReadInt32( ReadStyle.NonNeg );
            SecondsUntilNextInflux = SecondsUntilNextInflux != readInt ? readInt : SecondsUntilNextInflux;
        }
    }
    public class EnclaveWorldExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private EnclaveWorldData Data;

        public static int PatternIndex;

        public void ReceivePatternIndex( int Index )
        {
            PatternIndex = Index;
        }
        public int GetNumberOfItems()
        {
            return 1;
        }

        public World ParentWorld;
        public void InitializeData( object ParentObject, object[] Target )
        {
            this.ParentWorld = ParentObject as World;
            if ( this.ParentWorld == null && ParentObject != null )
                return;

            this.Data = new EnclaveWorldData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            EnclaveWorldData data = (EnclaveWorldData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            if ( IsForPartialSyncDuringMultiplayer )
            {
                //this is a partial sync, so use existing object and write into it
                (Target[0] as EnclaveWorldData).DeserializedIntoSelf( Buffer, IsForPartialSyncDuringMultiplayer );
            }
            else
            {
                //this is a full sync, so create a new object
                Target[0] = new EnclaveWorldData( Buffer );
            }
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

            StoredYounglingsData storage = RelatedEntityOrNull.GetStoredYounglings();
            if ( storage.StoredYounglings.GetPairCount() > 0 )
                Buffer.Add( $"This Enclave contains the following Younglings of total strength {Math.Round( RelatedEntityOrNull.AdditionalStrengthFromFactions / 1000f, 3 )}.\n" );
            for ( int x = 0; x < storage.StoredYounglings.GetPairCount(); x++ )
            {
                int count = 0;
                int strength = 0;
                YounglingUnit unit = storage.StoredYounglings.GetPairByIndex( x ).Key;
                GameEntityTypeData unitData = GameEntityTypeDataTable.Instance.GetRowByName( unit.ToString() );
                for ( int y = 0; y < storage.StoredYounglings[unit].UnitsByMark.GetPairCount(); y++ )
                {
                    ArcenSparseLookupPair<byte, int> pair = storage.StoredYounglings[unit].UnitsByMark.GetPairByIndex( y );
                    count += pair.Value;
                    strength += unitData.GetForMark( pair.Key ).StrengthPerSquad_Original_DoesNotIncreaseWithMarkLevel * pair.Value;
                }
                if ( Window_InGameHoverEntityInfo.CalculateTooltipDetailLevel() >= TooltipDetail.Full )
                    Buffer.Add( $"{count} {unitData.DisplayName} ({(double)strength / 1000} strength)\n" );
            }
        }
    }

    public class YounglingCollection
    {
        public ArcenSparseLookup<byte, int> UnitsByMark;
        private int strength;
        public int Strength { get { return strength; } }

        public void AddStrength( int strength )
        {
            this.strength += strength;
        }

        public void SubtractStrength( int strength )
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

        public void DeployYounglings( YounglingUnit younglingType, GameEntity_Squad enclave, Faction spawnFaction, ArcenSimContext Context, bool setFree = false )
        {
            GameEntityTypeData unitData = GameEntityTypeDataTable.Instance.GetRowByName( younglingType.ToString() );
            for ( int i = 0; i < UnitsByMark.GetPairCount(); i++ )
            {
                byte markLevel = UnitsByMark.GetPairByIndex( i ).Key;
                int toSpawnInTotal = UnitsByMark[markLevel];
                int stackingCutoff = 10;
                int stackSize = Math.Max( 1, toSpawnInTotal / stackingCutoff );
                while ( toSpawnInTotal > 0 )
                {
                    short toSpawn = (short)Math.Min( toSpawnInTotal, stackSize );
                    GameEntity_Squad youngling = GameEntity_Squad.CreateNew( enclave.Planet.GetPlanetFactionForFaction( spawnFaction ), unitData, markLevel, enclave.Planet.GetPlanetFactionForFaction( spawnFaction ).FleetUsedAtPlanet, 1, enclave.WorldLocation, Context );
                    youngling.AddOrSetExtraStackedSquadsInThis( (short)(toSpawn - 1), true );
                    youngling.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, spawnFaction.FactionIndex );
                    if ( setFree )
                        youngling.MinorFactionStackingID = -1;
                    else
                        youngling.MinorFactionStackingID = enclave.PrimaryKeyID;
                    toSpawnInTotal -= toSpawn;
                }
            }
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
        public ArcenSparseLookup<YounglingUnit, YounglingCollection> StoredYounglings;
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
            if ( Enum.TryParse( youngling.TypeData.InternalName, out YounglingUnit unitName ) )
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
            Faction spawnFaction = enclave.PlanetFaction.Faction.Type == FactionType.SpecialFaction ? enclave.PlanetFaction.Faction : World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( RoamingEnclavePlayerTeam ) );
            for ( int x = 0; x < StoredYounglings.GetPairCount(); x++ )
            {
                StoredYounglings.GetPairByIndex( x ).Value.DeployYounglings( StoredYounglings.GetPairByIndex( x ).Key, enclave, spawnFaction, Context );
            }
            StoredYounglings = new ArcenSparseLookup<YounglingUnit, YounglingCollection>();
        }

        public void AttemptToCombineYounglings( GameEntity_Squad enclave, YounglingUnit unit )
        {
            if ( !StoredYounglings.GetHasKey( unit ) )
                return;

            YounglingCollection collection = StoredYounglings[unit];

            GameEntityTypeData unitData = GameEntityTypeDataTable.Instance.GetRowByName( unit.ToString() );

            for ( byte x = 1; x < 7; x++ )
            {
                if ( collection.UnitsByMark.GetHasKey( x ) )
                {
                    if ( collection.UnitsByMark[x] > 100 )
                    {
                        collection.SubtractStrength( unitData.GetForMark( x ).GetCalculatedStrengthPerSquadForFleetOrNull( null ) * 2 );
                        collection.AddStrength( unitData.GetForMark( (byte)(x + 1) ).GetCalculatedStrengthPerSquadForFleetOrNull( null ) );

                        collection.UnitsByMark[x] -= 2;
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
            StoredYounglings = new ArcenSparseLookup<YounglingUnit, YounglingCollection>();
        }
        public StoredYounglingsData( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializedIntoSelf( Buffer, false );
        }
        public void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            int count = StoredYounglings.GetPairCount();
            buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
            {
                ArcenSparseLookupPair<YounglingUnit, YounglingCollection> pair = StoredYounglings.GetPairByIndex( x );
                buffer.AddByte( ReadStyleByte.Normal, (byte)pair.Key );
                pair.Value.SerializeTo( buffer );
            }
        }
        // TODO - This one will require more work to take the sync into account. If it ends up being a chonker; we'll deal with it.
        public void DeserializedIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            if ( StoredYounglings == null )
                StoredYounglings = new ArcenSparseLookup<YounglingUnit, YounglingCollection>();

            int count = buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
                StoredYounglings.AddPair( (YounglingUnit)buffer.ReadByte( ReadStyleByte.Normal ), new YounglingCollection( buffer ) );
        }
    }

    public class StoredYounglingsExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private StoredYounglingsData Data;

        public static int PatternIndex;
        public void ReceivePatternIndex( int Index )
        {
            PatternIndex = Index; //for internal use with the ExternalData code in the game engine itself
        }
        public int GetNumberOfItems()
        {
            return 1; //for internal use with the ExternalData code in the game engine itself
        }

        public GameEntity_Squad ParentSquad;
        public void InitializeData( object ParentObject, object[] Target )
        {
            this.ParentSquad = ParentObject as GameEntity_Squad;
            if ( this.ParentSquad == null && ParentObject != null )
                return;

            //this initialization is handled by the data structure itself
            this.Data = new StoredYounglingsData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            StoredYounglingsData data = (StoredYounglingsData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            if ( IsForPartialSyncDuringMultiplayer )
            {
                //this is a partial sync, so use existing object and write into it
                (Target[0] as StoredYounglingsData).DeserializedIntoSelf( Buffer, IsForPartialSyncDuringMultiplayer );
            }
            else
            {
                //this is a full sync, so create a new object
                Target[0] = new StoredYounglingsData( Buffer );
            }
        }
    }
    public static class StoredYounglingsExternalDataExtensions
    {
        public static void StoreYoungling( this GameEntity_Squad enclave, GameEntity_Squad youngling, ArcenSimContext Context )
        {
            StoredYounglingsData storage = enclave.GetStoredYounglings();
            enclave.AdditionalStrengthFromFactions = 0;
            if ( Enum.TryParse( youngling.TypeData.InternalName, out YounglingUnit unitType ) )
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

        public static void YounglingStoragePerSecondLogic( this GameEntity_Squad enclave, ArcenSimContext Context )
        {
            enclave.AdditionalStrengthFromFactions = 0;

            StoredYounglingsData collection = enclave.GetStoredYounglings();

            if ( collection.StoredYounglings.GetPairCount() > 0 )
            {
                Faction spawnFaction = enclave.PlanetFaction.Faction.Type == FactionType.SpecialFaction ? enclave.PlanetFaction.Faction : World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( RoamingEnclavePlayerTeam ) );

                collection.StoredYounglings.Sort( ( pair1, pair2 ) => { return pair2.Value.Strength.CompareTo( pair1.Value.Strength ); } );
                for ( int x = 1; x < collection.StoredYounglings.GetPairCount(); x++ )
                {
                    YounglingUnit younglingType = collection.StoredYounglings.GetPairByIndex( x ).Key;
                    collection.StoredYounglings[younglingType].DeployYounglings( younglingType, enclave, spawnFaction, Context, true );
                    collection.StoredYounglings.RemovePairByKey( younglingType );
                    x--;
                }
            }

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
