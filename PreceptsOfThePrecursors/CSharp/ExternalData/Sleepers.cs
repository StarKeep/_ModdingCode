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
        public void SerializeTo( ArcenSerializationBuffer buffer )
        {
            buffer.AddInt16( ReadStyle.PosExceptNeg1, planet );
            buffer.AddInt32( ReadStyle.NonNeg, SecondEnteredPlanet );
            buffer.AddInt32( ReadStyle.NonNeg, SecondLastTransformed );
        }
        public SleeperData( ArcenDeserializationBuffer buffer ) : this()
        {
            planet = buffer.ReadInt16( ReadStyle.PosExceptNeg1 );
            SecondEnteredPlanet = buffer.ReadInt32( ReadStyle.NonNeg );
            SecondLastTransformed = buffer.ReadInt32( ReadStyle.NonNeg );
        }
    }
    public class SleeperExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private SleeperData Data;

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
            this.Data = new SleeperData();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer )
        {
            //For saving to disk, translate this object into the buffer
            SleeperData data = (SleeperData)Source[0];
            data.SerializeTo( Buffer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            Target[0] = new SleeperData( Buffer );
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
