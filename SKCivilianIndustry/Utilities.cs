using Arcen.AIW2.Core;
using Arcen.Universal;
using System.Collections.Generic;

namespace SKCivilianIndustry
{
    public static class StaticMethods
    {
        public static GameCommand CreateGameCommand( string type, GameCommandSource source, Faction faction )
        {
            GameCommandType commandType = GameCommandTypeTable.Instance.GetRowByName( type );
            return CreateGameCommand( commandType, source, faction );
        }
        public static GameCommand CreateGameCommand( GameCommandType type, GameCommandSource source, Faction faction )
        {
            GameCommand command = GameCommand.Create( type, source );
            command.RelatedFactionIndex = faction.FactionIndex;
            return command;
        }
    }
    public static class ExtentionMethods
    {
        public static void AddToPerPlanetLookup(this GameEntity_Squad entity, ref ArcenSparseLookup<Planet, List<GameEntity_Squad>> lookup)
        {
            if ( !lookup.GetHasKey( entity.Planet ) )
                lookup.AddPair( entity.Planet, new List<GameEntity_Squad>() );
            lookup[entity.Planet].Add( entity );
        }
    }
}
