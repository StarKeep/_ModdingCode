using Arcen.AIW2.Core;
using Arcen.AIW2.External;

namespace SKCivilianIndustry.GameCommands
{
    public class GameCommand_PopulateCargoShipList : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            for ( int x = 0; x < command.RelatedEntityIDs.Count; x++ )
            {
                if ( command.RelatedBool || !SKTradeLogicFaction.WorldData.CargoShipsRaw.Contains( command.RelatedEntityIDs[x] ) )
                    SKTradeLogicFaction.WorldData.CargoShipsRaw.Add( command.RelatedEntityIDs[x] );
            }
        }
    }

    public class GameCommand_PopulateTradeEntitiesList : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            for ( int x = 0; x < command.RelatedEntityIDs.Count; x++ )
            {
                if ( command.RelatedBool || !SKTradeLogicFaction.WorldData.TradeEntitiesRaw.Contains( command.RelatedEntityIDs[x] ) )
                    SKTradeLogicFaction.WorldData.TradeEntitiesRaw.Add( command.RelatedEntityIDs[x] );
            }
        }
    }

    public class GameCommand_PopulateIndustryEntitiesList : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            for ( int x = 0; x < command.RelatedEntityIDs.Count; x++ )
            {
                if ( command.RelatedBool || !SKTradeLogicFaction.WorldData.IndustryEntitiesRaw.Contains( command.RelatedEntityIDs[x] ) )
                    SKTradeLogicFaction.WorldData.IndustryEntitiesRaw.Add( command.RelatedEntityIDs[x] );
            }
        }
    }

    public class GameCommand_PopulateCargoShipsToBuildList : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            for ( int x = 0; x < command.RelatedEntityIDs.Count; x++ )
            {
                if ( SKTradeLogicFaction.WorldData.CargoShipsToBuildRaw.GetHasKey( command.RelatedEntityIDs[x] ) )
                    SKTradeLogicFaction.WorldData.CargoShipsToBuildRaw[command.RelatedEntityIDs[x]] = command.RelatedIntegers[x];
                else
                    SKTradeLogicFaction.WorldData.CargoShipsToBuildRaw.AddPair( command.RelatedEntityIDs[x], command.RelatedIntegers[x] );
            }
        }
    }

    public class GameCommand_AddUnitToCivFleet : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            SKTradeLogicFaction.WorldData.AddToCivFleet( command.RelatedEntityIDs[0], command.RelatedString, command.RelatedEntityIDs[1] );
        }
    }
}
