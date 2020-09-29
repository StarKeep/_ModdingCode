using Arcen.AIW2.Core;
using Arcen.Universal;

namespace SKCivilianIndustry.Persistence
{
    public static class CivilianMilitiaExtensions
    {
        public static CivilianMilitia GetCivilianMilitiaExt( this GameEntity_Squad ParentObject, ExternalDataRetrieval RetrievalRules )
        {
            ArcenExternalData extData = ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, CivilianMilitiaExternalData.PatternIndex, RetrievalRules );
            if ( extData == null )
                return null;
            return (CivilianMilitia)extData.Data[0];
        }
        public static void SetCivilianMilitiaExt( this GameEntity_Squad ParentObject, CivilianMilitia data )
        {
            ParentObject.ExternalData.GetCollectionByPatternIndex( ParentObject, (int)CivilianMilitiaExternalData.PatternIndex, ExternalDataRetrieval.CreateIfNotFound ).Data[0] = data;
        }
    }
}
