using Arcen.AIW2.Core;
using Arcen.AIW2.External;

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
}
