using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using SKCivilianIndustry.Storage;

namespace SKCivilianIndustry.GameCommands
{
    public class GameCommand_InitializeCargo : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            for ( int x = 0; x < command.RelatedEntityIDs.Count; x++ )
            {
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs[x] );
                if ( entity != null )
                    entity.GetCargoSimSafeNeverNull();
            }
        }
    }

    public class GameCommand_InitializeIndustry : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            for ( int x = 0; x < command.RelatedEntityIDs.Count; x++ )
            {
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs[x] );
                if ( entity != null )
                    entity.GetIndustrySimSafeNeverNull();
            }
        }
    }
}
