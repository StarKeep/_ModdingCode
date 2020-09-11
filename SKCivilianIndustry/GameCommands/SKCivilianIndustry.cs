using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using SKCivilianIndustry.Persistence;
using System;

namespace SKCivilianIndustry.GameCommands
{
    public class SetMilitiaCaps : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            for(int x = 0; x < command.RelatedEntityIDs.Count; x++ )
            {
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs[x] );
                entity.GetCivilianMilitiaExt( ExternalDataRetrieval.CreateIfNotFound ).ShipCapacity[command.RelatedIntegers[x]] = command.RelatedIntegers2[x];
            }
        }
    }

    public class RemoveUnitFromMilitiaByIndex : BaseGameCommand
    {
        public override void Execute (GameCommand command, ArcenSimContext context )
        {
            for(int x = 0; x < command.RelatedEntityIDs.Count; x++ )
            {
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs[x] );
                entity.GetCivilianMilitiaExt( ExternalDataRetrieval.CreateIfNotFound ).Ships[command.RelatedIntegers[x]].Remove( command.RelatedIntegers2[x] );
            }
        }
    }

    public class UpdateCargoShips : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            Faction faction = World_AIW2.Instance.GetFactionByIndex( command.RelatedFactionIndex );
            if ( !(faction.Implementation is SpecialFaction_SKCivilianIndustry) )
                return;

            CivilianFaction factionData = (faction.Implementation as SpecialFaction_SKCivilianIndustry).factionData;

            for ( int x = 0; x < command.RelatedEntityIDs.Count; x++ )
            {
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs[x] );
                // Update our cargo ship with its new mission.
                CivilianStatus cargoShipStatus = entity.GetCivilianStatusExt( ExternalDataRetrieval.CreateIfNotFound );
                cargoShipStatus.Origin = command.RelatedIntegers[x];    // No origin station required.
                cargoShipStatus.Destination = command.RelatedIntegers2[x];
                factionData.ChangeCargoShipStatus( entity, (Status)command.RelatedIntegers3[x] );
            }
        }
    }
}
