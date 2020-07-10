using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using System.Collections.Generic;

namespace SKCivilianIndustry
{
    public static class Utilities
    {
        public static void QueueWormholeCommand( GameEntity_Squad entity, Planet destination, ref ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> wormholeCommands )
        {
            Planet origin = entity.Planet;
            if ( !wormholeCommands.GetHasKey( origin ) )
                wormholeCommands.AddPair( origin, new ArcenSparseLookup<Planet, List<GameEntity_Squad>>() );
            if ( !wormholeCommands[origin].GetHasKey( destination ) )
                wormholeCommands[origin].AddPair( destination, new List<GameEntity_Squad>() );
            wormholeCommands[origin][destination].Add( entity );
        }

        public static void ExecuteWormholeCommands( Faction faction, ArcenLongTermIntermittentPlanningContext Context, ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> wormholeCommands )
        {
            for ( int x = 0; x < wormholeCommands.GetPairCount(); x++ )
            {
                ArcenSparseLookupPair<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> originPair = wormholeCommands.GetPairByIndex( x );
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
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.Instance.GetRowByName( "SetWormholePath_CivilianIndustryBulk" ), GameCommandSource.AnythingElse );
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
        }

        public static void QueueMovementCommand( GameEntity_Squad entity, ArcenPoint destination, ref ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> movementCommands )
        {
            Planet planet = entity.Planet;
            if ( !movementCommands.GetHasKey( planet ) )
                movementCommands.AddPair( planet, new ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>() );
            if ( !movementCommands[planet].GetHasKey( destination ) )
                movementCommands[planet].AddPair( destination, new List<GameEntity_Squad>() );
            movementCommands[planet][destination].Add( entity );
        }

        public static void ExecuteMovementCommands( Faction faction, ArcenLongTermIntermittentPlanningContext Context, ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> movementCommands )
        {
            for ( int x = 0; x < movementCommands.GetPairCount(); x++ )
            {
                ArcenSparseLookupPair<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> planetPair = movementCommands.GetPairByIndex( x );
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
                    if ( destination == ArcenPoint.ZeroZeroPoint )
                        continue;
                    List<GameEntity_Squad> entities = destinationPair.Value;
                    if ( entities == null )
                        continue;
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.Instance.GetRowByName( "MoveManyToOnePoint_CivilianIndustryBulk" ), GameCommandSource.AnythingElse );
                    command.RelatedPoints.Add( destination );
                    for ( int z = 0; z < entities.Count; z++ )
                    {
                        GameEntity_Squad entity = entities[z];
                        if ( entities != null && entity.LongRangePlanningData.DestinationPoint != destination )
                            command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    }
                    if ( command.RelatedEntityIDs.Count > 0 && command.RelatedPoints.Count > 0 )
                        Context.QueueCommandForSendingAtEndOfContext( command );
                }
            }
        }
    }
}
