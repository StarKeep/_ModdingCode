using System;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace PreceptsOfThePrecursors.DescriptionAppender
{
    public class DysonMothershipDescriptionAppender : IGameEntityDescriptionAppender
    {
        public void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenDoubleCharacterBuffer Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;

            // Testchamber.
            if ( RelatedEntityOrNull.PlanetFaction.Faction.Type == FactionType.Player )
                return;

            // Gifts.
            if ( !RelatedEntityOrNull.TypeData.GetHasTag( "DysonMothership" ) )
                return;

            DysonMothershipData MothershipData = DysonPrecursors.MothershipData;
            if ( MothershipData == null )
                return;

            if ( MothershipData.Level >= 7 )
                Buffer.Add( $"\nThis Mothership has reached its final form. It has stockpiled {MothershipData.Resources} metal and {MothershipData.Mines} mines. " );
            else
                Buffer.Add( $"\nThis Mothership is currently level {MothershipData.Level} and has {MothershipData.Resources}/{PrecursorCosts.Resources( MothershipData.Level, RelatedEntityOrNull.PlanetFaction.Faction )} of the metal and {MothershipData.Mines}/{PrecursorCosts.Mines( MothershipData.Level, RelatedEntityOrNull.PlanetFaction.Faction )} of the consumed mines required to level up. " );
            if ( MothershipData.Level < 7 && MothershipData.Resources >= PrecursorCosts.Resources( MothershipData.Level, RelatedEntityOrNull.PlanetFaction.Faction ) && MothershipData.Mines >= PrecursorCosts.Mines( MothershipData.Level, RelatedEntityOrNull.PlanetFaction.Faction ) )
                Buffer.Add( $"\nIt is ready to upgrade, and will attempt to do so over time on a friendly Noded planet." );

            Buffer.Add( $"\nIt can build another Proto Sphere after stockpiling {ProtoSphereCosts.BuildCost( RelatedEntityOrNull.PlanetFaction.Faction, false )} resources. " );
            if ( MothershipData.PlanetToBuildOn != null )
                Buffer.Add( $"\nShe is planning to build a Proto Sphere on {MothershipData.PlanetToBuildOn.Name} next." );
            if ( MothershipData.MetalGainedOrLostLastSecond > 0 )
                Buffer.Add( $"Gaining {MothershipData.MetalGainedOrLostLastSecond} metal per second. " );
            else if ( MothershipData.MetalGainedOrLostLastSecond < 0 )
                Buffer.Add( $"Losing {MothershipData.MetalGainedOrLostLastSecond} metal per second." );
            string mood;
            int trust = MothershipData.Trust.GetTrust( RelatedEntityOrNull.Planet );
            switch ( trust )
            {
                case int _ when trust < -2000:
                    mood = "Controlling";
                    break;
                case int _ when trust < -1000:
                    mood = "Demanding";
                    break;
                case int _ when trust < -500:
                    mood = "Annoyed";
                    break;
                case int _ when trust < 0:
                    mood = "Cautious";
                    break;
                case int _ when trust < 500:
                    mood = "Neutral";
                    break;
                case int _ when trust < 1000:
                    mood = "Curious";
                    break;
                case int _ when trust < 2000:
                    mood = "Accepting";
                    break;
                default:
                    mood = "Trusting";
                    break;
            }

            Buffer.Add( $"\nIt is currently {mood} ({trust}) towards us on this planet. " );
            if ( MothershipData.IsGainingTrust )
                Buffer.Add( " Its trust towards this planet is currently increasing. " );
            else if ( MothershipData.IsLosingTrust )
                Buffer.Add( " Its trust towards this planet is currently decreasing. " );

            bool hasAdjacentProtectorNode = false;
            RelatedEntityOrNull.Planet.DoForLinkedNeighbors( false, adjPlanet =>
            {
                if ( DysonPrecursors.DysonNodes.GetHasKey( adjPlanet ) && MothershipData.Trust.GetTrust( adjPlanet ) > 500 )
                {
                    hasAdjacentProtectorNode = true;
                    return DelReturn.Break;
                }

                return DelReturn.Continue;
            } );
            if ( RelatedEntityOrNull.Planet.GetIsControlledByFactionType( FactionType.Player ) && trust > -1000 && trust <= 1000 )
                Buffer.Add( " It has recognized that we consider this planet our home and has ceased aggression... for now. " );
            else if ( hasAdjacentProtectorNode )
                Buffer.Add( " It is currently friendly to us on this planet due to an adjacent Protector Node." );

            if ( World_AIW2.Instance.GameSecond - RelatedEntityOrNull.GameSecondEnteredThisPlanet < 30 )
                Buffer.Add( "\nIt is currently converting excess energy from its recent wormhole traversal to rapidly recharge its shields. " );

            if ( MothershipData.IsNearMine )
                Buffer.Add( "\nIt is currently using resources from nearby mines to repair itself. " );
            Buffer.Add( "\n" );
        }
    }

    public class DysonProtoSphereDescriptionAppender : IGameEntityDescriptionAppender
    {
        public void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenDoubleCharacterBuffer Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;

            // Testchamber.
            if ( RelatedEntityOrNull.PlanetFaction.Faction.Type == FactionType.Player )
                return;

            if ( RelatedEntityOrNull.CountOfEntitiesProvidingExternalInvulnerability > 0 )
                Buffer.Add( "This Sphere Golem is currently " ).StartColor( UnityEngine.Color.red ).Add( $"invulnerable.</color> The remaining Dyson Nodes on the planet must be killed first." );

            DysonPerPlanetData protoSphereData = RelatedEntityOrNull.Planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.ReturnNullIfNotFound );
            if ( protoSphereData == null )
                return;
            Buffer.Add( $"\nThis Sphere is level {protoSphereData.Level}. " );
            if ( protoSphereData.Level < 7 )
                Buffer.Add( $"\nThis Sphere Golem has acquired {protoSphereData.Resources}/{ProtoSphereCosts.Resources( protoSphereData.Level, World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonPrecursors ) ), false )} of the resources required to level up. " );

            Buffer.Add( "\n" );
        }
    }
}
