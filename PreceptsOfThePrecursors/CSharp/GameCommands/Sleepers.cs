using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace PreceptsOfThePrecursors.GameCommands
{
    public class SetPrimeTarget : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            Faction sleeperFaction = World_AIW2.Instance.GetFactionByIndex( command.RelatedFactionIndex );
            SleeperData sData = (sleeperFaction.Implementation as SleeperSubFaction).Prime?.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound );
            if ( sData == null )
                return;

            sData.TargetPlanet = World_AIW2.Instance.GetPlanetByIndex( (short)command.RelatedIntegers[0] );
            sData.TargetPoint = command.RelatedPoints[0];
        }
    }
    public class SetSleeperTargets : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            for ( int x = 0; x < command.RelatedEntityIDs.Count; x++ )
            {
                GameEntity_Squad sleeper = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs[x] );
                SleeperData sData = sleeper?.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound );
                if ( sData == null )
                    continue;

                sData.TargetPlanet = World_AIW2.Instance.GetPlanetByIndex( (short)command.RelatedIntegers[x] );
                sData.TargetPoint = command.RelatedPoints[x];
            }
        }
    }
}
