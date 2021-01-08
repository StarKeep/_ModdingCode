using System;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace PreceptsOfThePrecursors.GameCommands
{
    public class UpdateStudyBudgets : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            NeinzulWarChroniclers chroniclerFaction = (NeinzulWarChroniclers)(World_AIW2.Instance.GetFactionByIndex( command.RelatedFactionIndex ).Implementation);

            chroniclerFaction.currentBudgetStudyRates = new ArcenSparseLookup<GameEntity_Squad, int>();

            for(int x = 0; x < command.RelatedEntityIDs.Count; x++ )
            {
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs[x] );
                if ( entity != null )
                    chroniclerFaction.currentBudgetStudyRates.AddPair( entity, command.RelatedIntegers[x] );
            }
        }
    }
}
