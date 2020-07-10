using Arcen.AIW2.Core;
using Arcen.Universal;
using System.Collections.Generic;

namespace PreceptsOfThePrecursors
{
    public static class StaticMethods
    {
        public static GameCommand CreateGameCommand( GameCommandType type, GameCommandSource source, Faction faction )
        {
            GameCommand command = GameCommand.Create( type, source );
            command.RelatedFactionIndex = faction.FactionIndex;
            return command;
        }
    }
    public static class ExtentionMethods
    {

    }
}
