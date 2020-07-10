using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCivilianIndustry.Storage
{
    public class CivCargoShipStatus
    {
        private int origin;
        public GameEntity_Squad Origin { get { return World_AIW2.Instance.GetEntityByID_Squad( origin ); } set { origin = value.PrimaryKeyID; } }

        private int destination;
        public GameEntity_Squad Destination { get { return World_AIW2.Instance.GetEntityByID_Squad( destination ); } set { destination = value.PrimaryKeyID; } }

        public bool IsDocked;

        public bool HasOrigin { get { return Origin != null; } }
        public bool HasDestination { get { return Destination != null; } }

        public bool IsIdle { get { return !(HasOrigin || HasDestination); } }

        public void FinishLoading()
        {
            origin = -1;
            IsDocked = false;
        }

        public void MakeIdle()
        {
            origin = -1;
            destination = -1;
            IsDocked = false;
        }

        public CivCargoShipStatus()
        {
            origin = -1;
            destination = -1;
        }

        public void SerializeTo(ArcenSerializationBuffer Buffer )
        {
            Buffer.AddInt32( ReadStyle.PosExceptNeg1, origin );
            Buffer.AddInt32( ReadStyle.PosExceptNeg1, destination );
            Buffer.AddItem( IsDocked );
        }

        public CivCargoShipStatus(ArcenDeserializationBuffer Buffer ) : this()
        {
            origin = Buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            destination = Buffer.ReadInt32( ReadStyle.PosExceptNeg1 );
            IsDocked = Buffer.ReadBool();
        }
    }
    public class CivCargoShipStatusExternalData : IArcenExternalDataPatternImplementation
    {
        private CivCargoShipStatus Data;

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
            this.Data = new CivCargoShipStatus();
            Target[0] = this.Data;
        }
        public void SerializeExternalData( object[] Source, ArcenSerializationBuffer Buffer )
        {
            //For saving to disk, translate this object into the buffer
            CivCargoShipStatus data = (CivCargoShipStatus)Source[0];
            data.SerializeTo( Buffer );
        }
        public void DeserializeExternalData( object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            Target[0] = new CivCargoShipStatus( Buffer );
        }
    }
    public static class CivCargoShipStatusExtensions
    {
        public static CivCargoShipStatus GetCargoShipStatus( this GameEntity_Squad ParentObject )
        {
            CivCargoShipStatus status = (CivCargoShipStatus)ParentObject.ExternalData.GetCollectionByPatternIndex( CivCargoShipStatusExternalData.PatternIndex ).Data[0];
            return status;
        }

        public static void SetCargoShipStatus( this GameEntity_Squad ParentObject, CivCargoShipStatus data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( (int)CivCargoShipStatusExternalData.PatternIndex ).Data[0] = data;
        }
    }
}
