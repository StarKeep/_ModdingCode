using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace PreceptsOfThePrecursors
{
    public class BaseScrapyardHack : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Hackable result = base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
            if ( Target == null )
                if ( result != Hackable.CanBeHacked )
                    return result;
            return Hackable.CanBeHacked;
        }

        public override void DoOneSecondOfHackingLogic_AsPartOfMainSim( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            Faction spawnFaction = World_AIW2.Instance.GetFactionByIndex( planet.InitialOwningAIFactionIndex );
            if ( spawnFaction == null )
                spawnFaction = BadgerFactionUtilityMethods.GetRandomAIFaction( Context );
            FInt mult = GetHackingLevelMultiplier( spawnFaction );

            FInt budget = FInt.Zero;
            if ( Hacker.ActiveHack_DurationThusFar % type.PrimaryHackResponseInterval == 0 )
                budget += type.PrimaryResponseStrengthPerInterval * mult * 1000;
            if ( Hacker.ActiveHack_DurationThusFar % type.SecondaryHackResponseInterval == 0 )
                budget += type.SecondaryResponseStrengthPerInterval * mult * 1000;
            if ( Hacker.ActiveHack_DurationThusFar % type.TertiaryHackResponseInterval == 0 )
                budget += type.TertiaryResponseStrengthPerInterval * mult * 1000;

            if (budget > FInt.One)
                ExoGalacticAttackManager.SendExoGalacticAttack( ExoOptions.CreateWithDefaults( Hacker, budget.GetNearestIntPreferringHigher(), spawnFaction, Hacker.PlanetFaction.Faction ), Context );
            if ( Hacker.ActiveHack_DurationThusFar >= type.GetEffectiveHackDuration() )
                DoSuccessfulCompletionLogic( Target, planet, Hacker, Context, type, Event );
        }

        public override bool DoSuccessfulCompletionLogic( GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            Faction spawnFaction = World_AIW2.Instance.GetFactionByIndex( planet.InitialOwningAIFactionIndex );
            if ( spawnFaction == null )
                spawnFaction = BadgerFactionUtilityMethods.GetRandomAIFaction( Context );
            FInt mult = GetHackingLevelMultiplier( spawnFaction );
            ExoGalacticAttackManager.SendExoGalacticAttack( ExoOptions.CreateWithDefaults( Hacker, (type.ResponseStrengthOnCompletion * mult * 1000).GetNearestIntPreferringHigher(), spawnFaction, Hacker.PlanetFaction.Faction ), Context );

            bool unused;
            Hacker.PlanetFaction.Faction.StoredHacking -= GetCostToHack( TargetOrNull, planet, type, out unused );
            Event.HackingPointsSpent = GetCostToHack( TargetOrNull, planet, type, out unused );
            Event.HackingPointsLeftAfterHack = Hacker.PlanetFaction.Faction.StoredHacking;

            TargetOrNull.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );

            return true;
        }
    }
    public class Hacking_ScrapyardHackAlpha : BaseScrapyardHack
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            ArcenDoubleCharacterBuffer buffer = new ArcenDoubleCharacterBuffer();
            Window_InGameHoverEntityInfo.GetTextForEntity( buffer, null, null, target.GetScrapyardData().Alpha, 1, hackerFaction, 1, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailsInfo.AIPCostOnGrant );
            return buffer.GetStringAndResetForNextUpdate();
        }

        public override bool DoSuccessfulCompletionLogic( GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            GameEntity_Squad centerpiece = planet.Mapgen_SeedEntity( Context, Hacker.PlanetFaction.Faction, TargetOrNull.GetScrapyardData().Alpha, PlanetSeedingZone.MostAnywhere );
            centerpiece.SetWorldLocation( TargetOrNull.WorldLocation );

            return base.DoSuccessfulCompletionLogic( TargetOrNull, planet, Hacker, Context, type, Event );
        }
    }
    public class Hacking_ScrapyardHackBeta : BaseScrapyardHack
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            ArcenDoubleCharacterBuffer buffer = new ArcenDoubleCharacterBuffer();
            Window_InGameHoverEntityInfo.GetTextForEntity( buffer, null, null, target.GetScrapyardData().Beta, 1, hackerFaction, 1, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailsInfo.AIPCostOnGrant );
            return buffer.GetStringAndResetForNextUpdate();
        }

        public override bool DoSuccessfulCompletionLogic( GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            GameEntity_Squad centerpiece = planet.Mapgen_SeedEntity( Context, Hacker.PlanetFaction.Faction, TargetOrNull.GetScrapyardData().Beta, PlanetSeedingZone.MostAnywhere );
            centerpiece.SetWorldLocation( TargetOrNull.WorldLocation );

            return base.DoSuccessfulCompletionLogic( TargetOrNull, planet, Hacker, Context, type, Event );
        }
    }
    public class Hacking_ScrapyardHackGamma : BaseScrapyardHack
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            ArcenDoubleCharacterBuffer buffer = new ArcenDoubleCharacterBuffer();
            Window_InGameHoverEntityInfo.GetTextForEntity( buffer, null, null, target.GetScrapyardData().Gamma, 1, hackerFaction, 1, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailsInfo.AIPCostOnGrant );
            return buffer.GetStringAndResetForNextUpdate();
        }

        public override bool DoSuccessfulCompletionLogic( GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            GameEntity_Squad centerpiece = planet.Mapgen_SeedEntity( Context, Hacker.PlanetFaction.Faction, TargetOrNull.GetScrapyardData().Gamma, PlanetSeedingZone.MostAnywhere );
            centerpiece.SetWorldLocation( TargetOrNull.WorldLocation );

            return base.DoSuccessfulCompletionLogic( TargetOrNull, planet, Hacker, Context, type, Event );
        }
    }
}
