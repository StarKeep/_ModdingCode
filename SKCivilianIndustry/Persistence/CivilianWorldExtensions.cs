using Arcen.AIW2.Core;
using Arcen.Universal;
using SKCivilianIndustry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCivilianIndustry.Persistence
{
    public static class CivilianWorldExtensions
    {
        public static CivilianWorld GetCivilianWorldExt( this World ParentObject, ExternalDataRetrieval RetrievalRules )
        {
            ArcenExternalData extData = ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, CivilianWorldExternalData.PatternIndex, RetrievalRules );
            if ( extData == null )
                return null;
            return (CivilianWorld)extData.Data[0];
        }
        public static void SetCivilianWorldExt( this World ParentObject, CivilianWorld data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, (int)CivilianWorldExternalData.PatternIndex, ExternalDataRetrieval.CreateIfNotFound ).Data[0] = data;
        }

        /// <summary>
        /// Returns the Civilian Resource that a planet generates.
        /// </summary>
        /// <param name="planet"></param>
        /// <returns></returns>
        public static CivilianResource GetCivResourceForPlanet(this Planet planet)
        {
            return (CivilianResource)(planet.Index % (short)CivilianResource.Length); ;
        }
    }
}
