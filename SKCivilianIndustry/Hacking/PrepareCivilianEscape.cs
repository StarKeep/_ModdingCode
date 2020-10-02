using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace SKCivilianIndustry.Hacking
{
    public class PrepareCivilianEscape : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Hackable result = base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
            if ( result != Hackable.CanBeHacked )
                return result;
            return Hackable.CanBeHacked;
        }

        public override FInt GetHackingLevelMultiplier( Faction targetFaction )
        {
            FInt modifiedHackingLevel = FInt.FromParts( 0, 100 );
            if ( modifiedHackingLevel * targetFaction.HackingPointsUsedAgainstThisFaction < FInt.One )
                return FInt.One;
            else
                return modifiedHackingLevel * targetFaction.HackingPointsUsedAgainstThisFaction;

        }

        public override bool DoSuccessfulCompletionLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            base.DoSuccessfulCompletionLogic( Target, planet, Hacker, Context, type, Event );

            if ( !(Target.PlanetFaction.Faction.Implementation is SpecialFaction_SKCivilianIndustry) )
                return false;

            Target.PlanetFaction.Faction.HasBeenAwakenedByPlayer = true;

            return true;
        }
    }
}
