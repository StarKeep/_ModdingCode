using System;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace PreceptsOfThePrecursors.DescriptionAppender
{
    public class SleeperDescriptionAppender : IGameEntityDescriptionAppender
    {
        public void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenDoubleCharacterBuffer Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;

            SleeperData sData = RelatedEntityOrNull.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound );
            if ( sData == null )
                return;

            if ( sData.StoredMetalForConstruction > 0 )
                Buffer.Add( $"\nThis unit has gathered {sData.StoredMetalForConstruction} metal for use in construction projects. " );

            if ( sData.NextEntityToBuild != null )
                Buffer.Add( $"It will attempt to build a {sData.NextEntityToBuild.DisplayName} once it reaches {sData.NextEntityToBuild.MetalCost}. " );

            if ( sData.StoredMetalForUpgrading > 0 )
                Buffer.Add( $"\nIt has additionally gathered {sData.StoredMetalForUpgrading} metal for use in upgrading existing buildings. " );
        }
    }
}
