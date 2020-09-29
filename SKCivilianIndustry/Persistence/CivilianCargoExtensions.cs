using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCivilianIndustry.Persistence
{
    public static class CivilianCargoExtensions
    {
        public static CivilianCargo GetCivilianCargoExt( this GameEntity_Squad ParentObject, ExternalDataRetrieval RetrievalRules )
        {
            ArcenExternalData extData = ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, CivilianCargoExternalData.PatternIndex, RetrievalRules );
            if ( extData == null )
                return null;
            return (CivilianCargo)extData.Data[0];
        }
        public static void SetCivilianCargoExt( this GameEntity_Squad ParentObject, CivilianCargo data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, (int)CivilianCargoExternalData.PatternIndex, ExternalDataRetrieval.CreateIfNotFound ).Data[0] = data;
        }
    }
}
