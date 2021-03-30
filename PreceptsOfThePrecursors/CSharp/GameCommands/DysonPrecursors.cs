using System;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace PreceptsOfThePrecursors.GameCommands
{
    public class GameCommand_SetPlanetToBuildOn : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            Planet planet = World_AIW2.Instance.GetPlanetByName( false, command.RelatedString );
            if ( planet != null )
                DysonPrecursors.MothershipData.PlanetToBuildOn = planet;
        }
    }

    public class GameCommand_BuildPrecursorStructures : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            Faction faction = World_AIW2.Instance.GetFactionByIndex( command.RelatedFactionIndex );
            for(int x = 0; x < command.RelatedEntityIDs.Count; x++ )
            {
                GameEntity_Base origin;
                if ( command.RelatedBools[x] )
                    origin = World_AIW2.Instance.GetEntityByID_Other( command.RelatedEntityIDs[x] );
                else
                    origin = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs[x] );

                GameEntityTypeData structureData = GameEntityTypeDataTable.Instance.GetRowByName( ((DysonStructure)command.RelatedIntegers[x]).ToString() );

                PlanetFaction pFaction = origin.Planet.GetPlanetFactionForFaction( faction );
                ArcenPoint spawnPoint = origin.Planet.GetSafePlacementPoint( context, structureData, origin.WorldLocation, 0, 500 );

                GameEntity_Squad structure = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, structureData, (byte)command.RelatedIntegers2[x], pFaction.FleetUsedAtPlanet, 0,
                    spawnPoint, context);

                structure.MinorFactionStackingID = origin.PrimaryKeyID;

                origin.Planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).GameSecondLastStructureBuilt = World_AIW2.Instance.GameSecond;
            }
        }
    }
}
