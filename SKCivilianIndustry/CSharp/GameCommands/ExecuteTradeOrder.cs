using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using SKCivilianIndustry.Storage;
using System;
using System.Collections.Generic;

namespace SKCivilianIndustry.GameCommands
{
    public class GameCommand_ExecuteTradeOrder : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            for ( int x = 0; x < command.RelatedEntityIDs.Count; x++ )
            {
                GameEntity_Squad cargoShip = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs[x] );
                GameEntity_Squad origin = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedIntegers[x] );
                GameEntity_Squad destination = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedIntegers2[x] );
                if ( cargoShip == null || origin == null || destination == null )
                    continue;

                CivCargoShipStatus status = cargoShip.GetCargoShipStatus();
                status.Origin = origin;
                status.Destination = destination;
            }
        }
    }
}
