using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using SKCivilianIndustry.Persistence;

namespace SKCivilianIndustry.GameCommands
{
    public class SetMilitiaCaps : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            for ( int x = 0; x < command.RelatedEntityIDs.Count; x++ )
            {
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs[x] );
                entity.GetCivilianMilitiaExt( ExternalDataRetrieval.CreateIfNotFound ).ShipCapacity[command.RelatedIntegers[x]] = command.RelatedIntegers2[x];
            }
        }
    }

    public class SetMilitiaAtEase : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            for ( int x = 0; x < command.RelatedEntityIDs.Count; x++ )
            {
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs[x] );
                if ( entity == null )
                    continue;
                CivilianMilitia militiaStatus = entity.GetCivilianMilitiaExt( ExternalDataRetrieval.CreateIfNotFound );
                if ( militiaStatus == null )
                    continue;
                if ( militiaStatus.AtEase != command.RelatedBools[x] )
                    militiaStatus.AtEase = command.RelatedBools[x];
            }
        }
    }

    public class AttemptedToStoreAtEaseUnit : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            for ( int x = 0; x < command.RelatedEntityIDs.Count; x++ )
            {
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs[x] );
                if ( entity == null )
                    continue;

                GameEntity_Squad owner = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedIntegers[x] );
                if ( owner == null )
                    continue;

                CivilianMilitia militiaStatus = owner.GetCivilianMilitiaExt( ExternalDataRetrieval.ReturnNullIfNotFound );
                if ( militiaStatus == null )
                    continue;

                militiaStatus.StoredShips[command.RelatedIntegers2[x]] += 1 + entity.ExtraStackedSquadsInThis;

                entity.Despawn( context, true, InstancedRendererDeactivationReason.GettingIntoTransport );
            }
        }
    }

    public class SetNextTargetForTradeStation : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            Faction faction = World_AIW2.Instance.GetFactionByIndex( command.RelatedFactionIndex );
            if ( faction == null || !(faction.Implementation is SpecialFaction_SKCivilianIndustry) )
                return;

            GameEntity_Squad target = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs[0] );
            if ( target == null )
                return;

            (faction.Implementation as SpecialFaction_SKCivilianIndustry).factionData.NextTradeStationTarget = target;
        }
    }
}
