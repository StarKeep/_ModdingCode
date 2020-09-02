using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using System;

namespace PreceptsOfThePrecursors.GameCommands
{
    public class SetPrimeTarget : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            Faction sleeperFaction = World_AIW2.Instance.GetFactionByIndex( command.RelatedFactionIndex );
            (sleeperFaction.Implementation as SleeperSubFaction).PrimeTarget = command.RelatedPoints[0];
            (sleeperFaction.Implementation as SleeperSubFaction).PrimeTargetPlanet = World_AIW2.Instance.GetPlanetByIndex( (short)command.RelatedIntegers[0] );
        }
    }
    public class SetSleeperTargets : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            Faction sleeperFaction = World_AIW2.Instance.GetFactionByIndex( command.RelatedFactionIndex );
            SleeperSubFaction sleeperImplementation = sleeperFaction.Implementation is SleeperSubFaction ? sleeperFaction.Implementation as SleeperSubFaction : null;

            if ( sleeperImplementation == null )
                return;

            sleeperImplementation.sleeperTargets = new ArcenSparseLookup<GameEntity_Squad, ArcenSparseLookupPair<Planet, ArcenPoint>>();

            for(int x = 0; x < command.RelatedEntityIDs.Count; x++ )
            {
                GameEntity_Squad sleeper = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs[x] );
                if ( sleeper == null )
                    continue;

                sleeperImplementation.sleeperTargets.AddPair( sleeper, new ArcenSparseLookupPair<Planet, ArcenPoint>() { Key = World_AIW2.Instance.GetPlanetByIndex( (short)command.RelatedIntegers[x] ), Value = command.RelatedPoints[x] });
            }
        }
    }
}
