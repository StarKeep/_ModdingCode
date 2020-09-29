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
        /// If no Context is passed, such as in the ui, it can potentially return an invalid value.
        /// </summary>
        /// <param name="planet"></param>
        /// <param name="ContextForNewResourceGenerationOrNull"></param>
        /// <returns></returns>
        public static CivilianResource GetCivResourceForPlanet(this Planet planet, ArcenSimContext ContextForNewResourceGenerationOrNull)
        {
            CivilianWorld worldData = World.Instance.GetCivilianWorldExt( ExternalDataRetrieval.ReturnNullIfNotFound );
            if ( worldData == null )
                return CivilianResource.Length;

            CivilianResource resource = worldData.GetResourceForPlanet( planet );
            if (resource == CivilianResource.Length && ContextForNewResourceGenerationOrNull != null)
            {
                resource = (CivilianResource)ContextForNewResourceGenerationOrNull.RandomToUse.Next( (int)CivilianResource.Length );
                worldData.SetResourceForPlanet( planet, resource );
            }

            return resource;
        }
    }
}
