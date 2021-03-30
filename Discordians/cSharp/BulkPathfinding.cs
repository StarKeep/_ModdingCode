using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using System.Collections.Generic;

namespace Utilities
{
    public interface IBulkPathfinding
    {
        ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> WormholeCommands { get; set; }
        ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> MovementCommands { get; set; }
    }

    public static class BulkPathfinding
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="destination"></param>
        /// <param name="ContextForSmartPathfindingOrNullForDumb"></param>
        /// <returns>The next planet the entity will move to.</returns>
        public static Planet QueueWormholeCommand( this GameEntity_Squad entity, Planet destination, ArcenLongTermIntermittentPlanningContext ContextForSmartPathfindingOrNullForDumb = null, bool forResultOnlyDoNotActuallyQueue = false, PathingMode pathingMode = PathingMode.Default )
        {
            if ( !(entity.PlanetFaction.Faction.Implementation is IBulkPathfinding) )
                return null;

            if ( destination == null )
                return null;

            IBulkPathfinding BPFaction = entity.PlanetFaction.Faction.Implementation as IBulkPathfinding;

            if ( BPFaction.WormholeCommands == null )
                BPFaction.WormholeCommands = new ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>>();

            Planet origin = entity.Planet;
            Planet nextPlanet = null;
            if ( ContextForSmartPathfindingOrNullForDumb != null )
            {
                List<Planet> path = entity.PlanetFaction.Faction.FindPath( entity.Planet, destination, pathingMode, ContextForSmartPathfindingOrNullForDumb );
                for ( int x = 0; x < path.Count; x++ )
                    if ( path[x] != origin )
                    {
                        nextPlanet = path[x];
                        break;
                    }
            }
            if ( nextPlanet == null )
                origin.DoForLinkedNeighbors( false, planet =>
                {
                    if ( nextPlanet == null )
                        nextPlanet = planet;
                    else if ( planet.GetHopsTo( destination ) < nextPlanet.GetHopsTo( destination ) )
                        nextPlanet = planet;

                    return DelReturn.Continue;
                } );

            if ( !forResultOnlyDoNotActuallyQueue )
            {
                if ( !BPFaction.WormholeCommands.GetHasKey( origin ) )
                    BPFaction.WormholeCommands.AddPair( origin, new ArcenSparseLookup<Planet, List<GameEntity_Squad>>() );
                if ( !BPFaction.WormholeCommands[origin].GetHasKey( nextPlanet ) )
                    BPFaction.WormholeCommands[origin].AddPair( nextPlanet, new List<GameEntity_Squad>() );
                BPFaction.WormholeCommands[origin][nextPlanet].Add( entity );
            }
            return nextPlanet;
        }
        public static void ExecuteWormholeCommands( this Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( !(faction.Implementation is IBulkPathfinding) )
                return;

            IBulkPathfinding BPFaction = faction.Implementation as IBulkPathfinding;

            if ( BPFaction.WormholeCommands == null )
                return;

            for ( int x = 0; x < BPFaction.WormholeCommands.GetPairCount(); x++ )
            {
                ArcenSparseLookupPair<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> originPair = BPFaction.WormholeCommands.GetPairByIndex( x );
                if ( originPair == null )
                    continue;
                Planet origin = originPair.Key;
                ArcenSparseLookup<Planet, List<GameEntity_Squad>> destinations = originPair.Value;
                for ( int y = 0; y < destinations.GetPairCount(); y++ )
                {
                    ArcenSparseLookupPair<Planet, List<GameEntity_Squad>> destinationPair = destinations.GetPairByIndex( y );
                    if ( destinationPair == null )
                        continue;
                    Planet destination = destinationPair.Key;
                    List<GameEntity_Squad> entities = destinationPair.Value;
                    if ( entities == null )
                        continue;
                    List<Planet> path = faction.FindPath( origin, destination, PathingMode.Safest, Context );
                    GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCDirectedMob], GameCommandSource.AnythingElse );
                    for ( int p = 0; p < path.Count; p++ )
                        command.RelatedIntegers.Add( path[p].Index );
                    for ( int z = 0; z < entities.Count; z++ )
                    {
                        GameEntity_Squad entity = entities[z];
                        if ( entity != null && entity.LongRangePlanningData.FinalDestinationPlanetIndex != destination.Index )
                            command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    }
                    if ( command.RelatedEntityIDs.Count > 0 )
                        Context.QueueCommandForSendingAtEndOfContext( command );
                }
            }

            BPFaction.WormholeCommands = null;
        }
        public static void QueueMovementCommand( this GameEntity_Squad entity, ArcenPoint destination )
        {
            if ( !(entity.PlanetFaction.Faction.Implementation is IBulkPathfinding) )
                return;

            IBulkPathfinding BPFaction = entity.PlanetFaction.Faction.Implementation as IBulkPathfinding;

            if ( BPFaction.MovementCommands == null )
                BPFaction.MovementCommands = new ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>>();

            Planet planet = entity.Planet;
            if ( !BPFaction.MovementCommands.GetHasKey( planet ) )
                BPFaction.MovementCommands.AddPair( planet, new ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>() );
            if ( !BPFaction.MovementCommands[planet].GetHasKey( destination ) )
                BPFaction.MovementCommands[planet].AddPair( destination, new List<GameEntity_Squad>() );
            BPFaction.MovementCommands[planet][destination].Add( entity );
        }
        public static void ExecuteMovementCommands( this Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( !(faction.Implementation is IBulkPathfinding) )
                return;

            IBulkPathfinding BPFaction = faction.Implementation as IBulkPathfinding;

            if ( BPFaction.MovementCommands == null )
                return;

            for ( int x = 0; x < BPFaction.MovementCommands.GetPairCount(); x++ )
            {
                ArcenSparseLookupPair<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> planetPair = BPFaction.MovementCommands.GetPairByIndex( x );
                if ( planetPair == null )
                    continue;
                Planet planet = planetPair.Key;
                ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>> destinations = planetPair.Value;
                for ( int y = 0; y < destinations.GetPairCount(); y++ )
                {
                    ArcenSparseLookupPair<ArcenPoint, List<GameEntity_Squad>> destinationPair = destinations.GetPairByIndex( y );
                    if ( destinationPair == null )
                        continue;
                    ArcenPoint destination = destinationPair.Key;
                    List<GameEntity_Squad> entities = destinationPair.Value;
                    if ( entities == null )
                        continue;
                    GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint], GameCommandSource.AnythingElse );
                    command.RelatedPoints.Add( destination );
                    for ( int z = 0; z < entities.Count; z++ )
                    {
                        GameEntity_Squad entity = entities[z];
                        if ( entities != null && entity.LongRangePlanningData.DestinationPoint != destination )
                            command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    }
                    if ( command.RelatedEntityIDs.Count > 0 )
                        Context.QueueCommandForSendingAtEndOfContext( command );
                }
            }

            BPFaction.MovementCommands = null;
        }
    }
}