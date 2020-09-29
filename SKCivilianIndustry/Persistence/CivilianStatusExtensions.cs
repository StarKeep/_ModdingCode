using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCivilianIndustry.Persistence
{
    public static class CivilianStatusExtensions
    {
        public static CivilianStatus GetCivilianStatusExt( this GameEntity_Squad ParentObject, ExternalDataRetrieval RetrievalRules )
        {
            ArcenExternalData extData = ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, CivilianStatusExternalData.PatternIndex, RetrievalRules );
            if ( extData == null )
                return null;
            return (CivilianStatus)extData.Data[0];
        }
        public static void SetCivilianStatusExt( this GameEntity_Squad ParentObject, CivilianStatus data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, (int)CivilianStatusExternalData.PatternIndex, ExternalDataRetrieval.CreateIfNotFound ).Data[0] = data;
        }
    }
}
