using Arcen.AIW2.Core;
using Arcen.Universal;
using SKCivilianIndustry.Storage;
using System;

namespace SKCivilianIndustry.DescriptionAppenders
{
    public class ConstructionBuildingAppender : IGameEntityDescriptionAppender
    {
        public void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenDoubleCharacterBuffer Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;

            CivConstructionData construction = RelatedEntityOrNull.GetConstructionData();

            if ( construction.InternalName == null )
                return;
            try
            {
                GameEntityTypeData constructionData = GameEntityTypeDataTable.Instance.GetRowByName( construction.InternalName );

                Buffer.Add( $"This building will finish constructing a {constructionData.DisplayName} in {construction.SecondsLeft} seconds." );
            }
            catch ( Exception )
            {
                Buffer.Add( $"Unable to find building name. Game may be paused." );
            }
            // Add in an empty line to stop any other gunk (such as the fleet display) from messing up our given information.
            Buffer.Add( "\n" );
            return;
        }
    }
}
