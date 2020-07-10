using Arcen.AIW2.Core;
using Arcen.Universal;

namespace SKCivilianIndustry.Storage
{
    public class CivProtectionData
    {
        public ArcenSparseLookup<int, int> EntitiesToProtectRaw;
        public ArcenSparseLookup<GameEntity_Squad, GameEntity_Squad> EntitiesToProtect;

        private ArcenSparseLookup<short, int> planetBuildCooldowns;

        public bool GetCanBuild( Planet planet )
        {
            return !planetBuildCooldowns.GetHasKey( planet.Index );
        }

        public void AddToBuildTimer( Planet planet, int seconds )
        {
            if ( planetBuildCooldowns.GetHasKey( planet.Index ) )
                planetBuildCooldowns[planet.Index] += seconds;
            else
                planetBuildCooldowns.AddPair( planet.Index, seconds );
        }

        public void DecreaseBuildTimers()
        {
            for ( int x = 0; x < planetBuildCooldowns.GetPairCount(); x++ )
            {
                short key = planetBuildCooldowns.GetPairByIndex( x ).Key;
                planetBuildCooldowns[key]--;
                if ( planetBuildCooldowns[key] <= 0 )
                {
                    planetBuildCooldowns.RemovePairByKey( key );
                    x--;
                }
            }
        }

        public bool GetIsProtected( GameEntity_Squad entity )
        {
            return EntitiesToProtect.GetHasKey(entity) && EntitiesToProtect[entity] != null;
        }

        public CivProtectionData()
        {
            EntitiesToProtectRaw = new ArcenSparseLookup<int, int>();
            EntitiesToProtect = new ArcenSparseLookup<GameEntity_Squad, GameEntity_Squad>();
            planetBuildCooldowns = new ArcenSparseLookup<short, int>();
        }

        public void SerializeTo( ArcenSerializationBuffer Buffer )
        {
            int count = EntitiesToProtectRaw.GetPairCount();
            Buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
            {
                int key = EntitiesToProtectRaw.GetPairByIndex( x ).Key;
                Buffer.AddInt32( ReadStyle.NonNeg, key );
                Buffer.AddInt32( ReadStyle.PosExceptNeg1, EntitiesToProtectRaw[key] );
            }
            count = planetBuildCooldowns.GetPairCount();
            Buffer.AddInt32( ReadStyle.NonNeg, count );
            for ( int x = 0; x < count; x++ )
            {
                short key = planetBuildCooldowns.GetPairByIndex( x ).Key;
                Buffer.AddInt16( ReadStyle.NonNeg, key );
                Buffer.AddInt32( ReadStyle.PosExceptNeg1, planetBuildCooldowns[key] );
            }
        }

        public CivProtectionData( ArcenDeserializationBuffer Buffer ) : this()
        {
            int count = Buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
            {
                EntitiesToProtectRaw.AddPair( Buffer.ReadInt32( ReadStyle.NonNeg ), Buffer.ReadInt32( ReadStyle.PosExceptNeg1 ) );
            }
            count = Buffer.ReadInt32( ReadStyle.NonNeg );
            for ( int x = 0; x < count; x++ )
                planetBuildCooldowns.AddPair( Buffer.ReadInt16( ReadStyle.NonNeg ), Buffer.ReadInt32( ReadStyle.PosExceptNeg1 ) );
        }
    }
    public class CivProtectionDataExternalData : IArcenExternalDataPatternImplementation
    {
        private CivProtectionData Data;

        public static int PatternIndex;

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
            this.Data = new CivProtectionData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer )
        {
            //For saving to disk, translate this object into the buffer
            CivProtectionData data = (CivProtectionData)Source[0];
            data.SerializeTo( Buffer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            Target[0] = new CivProtectionData( Buffer );
        }
    }
    public static class CivProtectionDataExtensions
    {
        public static CivProtectionData GetProtectionData( this Faction ParentObject )
        {
            CivProtectionData data = (CivProtectionData)ParentObject.ExternalData.GetCollectionByPatternIndex( CivProtectionDataExternalData.PatternIndex ).Data[0];

            return data;
        }
        public static void SetProtectionData( this Faction ParentObject, CivProtectionData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( CivProtectionDataExternalData.PatternIndex ).Data[0] = data;
        }
    }
}
