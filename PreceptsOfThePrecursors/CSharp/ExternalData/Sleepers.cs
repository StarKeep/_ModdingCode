using Arcen.AIW2.Core;
using Arcen.Universal;

namespace PreceptsOfThePrecursors
{
    // Created to keep track of various things that don't work right when an entity transforms.
    public class SleeperData
    {
        private short planet;
        public Planet Planet { get { return World_AIW2.Instance.GetPlanetByIndex( planet ); } set { planet = value.Index; } }
        public int SecondEnteredPlanet;
        public int SecondsSinceEnteringPlanet { get { return World_AIW2.Instance.GameSecond - SecondEnteredPlanet; } }

        public int SecondLastTransformed;
        public int SecondsSinceLastTransformation { get { return World_AIW2.Instance.GameSecond - SecondLastTransformed; } }

        public SleeperData()
        {
            planet = -1;
            SecondEnteredPlanet = 0;
            SecondLastTransformed = 0;
        }
        public SleeperData( ArcenDeserializationBuffer Buffer ) : this()
        {
            this.DeserializedIntoSelf( Buffer, false );
        }
        public void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            buffer.AddInt16( ReadStyle.PosExceptNeg1, planet );
            buffer.AddInt32( ReadStyle.NonNeg, SecondEnteredPlanet );
            buffer.AddInt32( ReadStyle.NonNeg, SecondLastTransformed );
        }
        public void DeserializedIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            if ( IsForPartialSyncDuringMultiplayer )
                DeserializedChangedValuesIntoSelf( buffer );
            else
            {
                planet = buffer.ReadInt16( ReadStyle.PosExceptNeg1 );
                SecondEnteredPlanet = buffer.ReadInt32( ReadStyle.NonNeg );
                SecondLastTransformed = buffer.ReadInt32( ReadStyle.NonNeg );
            }
        }
        public void DeserializedChangedValuesIntoSelf( ArcenDeserializationBuffer buffer )
        {
            short readShort = buffer.ReadInt16( ReadStyle.PosExceptNeg1 );
            planet = planet != readShort ? readShort : planet;

            int readInt = buffer.ReadInt32( ReadStyle.NonNeg );
            SecondEnteredPlanet = SecondEnteredPlanet != readInt ? readInt : SecondEnteredPlanet;

            readInt = buffer.ReadInt32( ReadStyle.NonNeg );
            SecondLastTransformed = SecondLastTransformed != readInt ? readInt : SecondLastTransformed;
        }
    }
    public class SleeperExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private SleeperData Data;

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
            this.Data = new SleeperData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //For saving to disk, translate this object into the buffer
            SleeperData data = (SleeperData)Source[0];
            data.SerializeTo( Buffer, IsForPartialSyncDuringMultiplayer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsForPartialSyncDuringMultiplayer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            if ( IsForPartialSyncDuringMultiplayer )
            {
                //this is a partial sync, so use existing object and write into it
                (Target[0] as SleeperData).DeserializedIntoSelf( Buffer, IsForPartialSyncDuringMultiplayer );
            }
            else
            {
                //this is a full sync, so create a new object
                Target[0] = new SleeperData( Buffer );
            }
        }
    }
    public static class SleeperExternalDataExtensions
    {
        // This loads the data assigned to whatever ParentObject you pass. So, say, you could assign the same class to different ships, and each would be able to get back the values assigned to it.
        // In our specific case here, we're going to be assigning a dictionary to every faction.
        public static SleeperData GetSleeperData( this GameEntity_Squad ParentObject )
        {
            return (SleeperData)ParentObject.ExternalData.GetCollectionByPatternIndex( SleeperExternalData.PatternIndex ).Data[0];
        }
        // This meanwhile saves the data, assigning it to whatever ParentObject you pass.
        public static void SetSleeperData( this GameEntity_Squad ParentObject, SleeperData data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( SleeperExternalData.PatternIndex ).Data[0] = data;
        }
    }
}
