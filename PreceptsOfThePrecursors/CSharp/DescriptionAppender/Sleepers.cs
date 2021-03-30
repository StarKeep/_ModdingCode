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

    public class SleeperHangerDescriptionAppender : IGameEntityDescriptionAppender
    {
        public void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenDoubleCharacterBuffer Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;

            // Make sure we're in a Sleeper faction
            if ( !(RelatedEntityOrNull.PlanetFaction.Faction.Implementation is SleeperSubFaction) )
                return;

            SleeperFactionData factionData = RelatedEntityOrNull.PlanetFaction.Faction.GetSleeperFactionData( ExternalDataRetrieval.ReturnNullIfNotFound );
            if ( factionData == null )
                return;

            SleeperSubFaction sFaction = (SleeperSubFaction)RelatedEntityOrNull.PlanetFaction.Faction.Implementation;
            Planet planet = RelatedEntityOrNull.Planet;
            Faction faction = RelatedEntityOrNull.PlanetFaction.Faction;
            bool isPrime = RelatedEntityOrNull.TypeData.GetHasTag( Sleepers.UNIT_TAGS.SleeperPrimeHarbingerStructure.ToString() );

            int capacity = sFaction.GetMobileUnitCapacityForPlanet( planet );

            if (capacity > 0 )
            {
                int strength = sFaction.GetMobileUnitStrengthForPlanet( planet, faction );
                if ( strength >= capacity )
                    Buffer.Add( $"\nThis planet has hit its strength cap of {capacity}. " );
                else
                {
                    Buffer.Add( $"\nThis planet has {strength}/{capacity} strength built. " );
                    if (factionData.GetEntityToBuild(planet) != null )
                    {
                        int perc = (factionData.GetMetal( planet ) * 100) / factionData.GetEntityToBuild( planet ).MetalCost;
                        Buffer.Add( $"It is currently {perc}% of the way towards building a {factionData.GetEntityToBuild( planet ).DisplayName}." );
                    }
                }
            }
        }
    }
}
