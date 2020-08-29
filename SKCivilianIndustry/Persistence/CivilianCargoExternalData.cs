﻿using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCivilianIndustry.Persistence
{
    public class CivilianCargoExternalData : IArcenExternalDataPatternImplementation
    {
        // Make sure you use the same class name that you use for whatever data you want saved here.
        private CivilianCargo Data;

        public static int PatternIndex;

        // So this is essentially what type of thing we're going to 'attach' our class to.
        public static string RelatedParentTypeName = "GameEntity_Squad";

        public void ReceivePatternIndex(int Index)
        {
            PatternIndex = Index;
        }
        public int GetNumberOfItems()
        {
            return 1;
        }
        public bool GetShouldInitializeOn(string ParentTypeName)
        {
            // Figure out which object type has this sort of ExternalData (in this case, Faction)
            return ArcenStrings.Equals(ParentTypeName, RelatedParentTypeName);
        }

        public void InitializeData(object ParentObject, object[] Target)
        {
            this.Data = new CivilianCargo();
            Target[0] = this.Data;
        }
        public void SerializeExternalData(object[] Source, ArcenSerializationBuffer Buffer)
        {
            //For saving to disk, translate this object into the buffer
            CivilianCargo data = (CivilianCargo)Source[0];
            data.SerializeTo(Buffer);
        }
        public void DeserializeExternalData(object ParentObject, object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer)
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            Target[0] = new CivilianCargo(Buffer);
        }
    }
}
