using Arcen.AIW2.Core;
using Arcen.Universal;
using SKCivilianIndustry.Storage;

namespace SKCivilianIndustry.DescriptionAppenders
{
    public class IndustryDescriptionAppender : IGameEntityDescriptionAppender
    {
        private IGameEntityDescriptionAppender original;
        public IndustryDescriptionAppender() { }
        public IndustryDescriptionAppender( IGameEntityDescriptionAppender originalAppender = null )
        {
            if ( originalAppender != null )
                original = originalAppender;
        }
        public void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenDoubleCharacterBuffer Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;

            if ( original != null )
                original.AddToDescriptionBuffer( RelatedEntityOrNull, RelatedEntityTypeData, Buffer );

            CivIndustryData industry = RelatedEntityOrNull.GetIndustryNotSimSafeMayReturnNull();
            if ( industry == null )
                return;

            for ( int x = 0; x < industry.StoredResources.GetPairCount(); x++ )
            {
                string key = industry.StoredResources.GetPairByIndex( x ).Key;
                string unitName = GameEntityTypeDataTable.Instance.GetRowByName( industry.UnitTypeBuilt[key] ).DisplayName;
                if ( industry.StoredResources[key] > 0 || (RelatedEntityOrNull.GetCivFleet() != null && RelatedEntityOrNull.GetCivFleet().GetUnitCount( unitName ) > 0) )
                    Buffer.Add( $"\n{industry.StoredResources[key]}/{industry.Cost[key]} {key} to build {unitName}" );
            }

            // Add in an empty line to stop any other gunk (such as the fleet display) from messing up our given information.
            Buffer.Add( "\n" );
            return;
        }
    }
}
