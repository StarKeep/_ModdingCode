using System.Collections.Generic;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace PreceptsOfThePrecursors
{
    public class EternalRest : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Hackable result = base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
            if ( Sleepers.primesByPlanet != null && Sleepers.primesByPlanet.GetHasKey( planet ) )
                return Hackable.NeverBeHacked_ButStillShow;
            if ( result != Hackable.CanBeHacked )
                return result;
            return Hackable.CanBeHacked;
        }

        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            if ( Sleepers.primesByPlanet != null && Sleepers.primesByPlanet.GetHasKey( planet ) )
            {
                ArcenDoubleCharacterBuffer error = new ArcenDoubleCharacterBuffer();
                error.StartColor( UnityEngine.Color.red );
                error.Add( "A Prime is blocking all attempts to interface with Sleepers on this planet." );
                error.EndColor();
                return error.GetStringAndResetForNextUpdate();
            }
            return base.GetDynamicDescription( target, hackerOrNull, planet, hackerFaction, hackingType );
        }
    }

    public class PopTheDreamBubble : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Hackable result = base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
            if ( Sleepers.primesByPlanet != null && Sleepers.primesByPlanet.GetHasKey( planet ) )
                return Hackable.NeverBeHacked_ButStillShow;
            if ( result != Hackable.CanBeHacked )
                return result;
            return Hackable.CanBeHacked;
        }
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            if ( Sleepers.primesByPlanet != null && Sleepers.primesByPlanet.GetHasKey( planet ) )
            {
                ArcenDoubleCharacterBuffer error = new ArcenDoubleCharacterBuffer();
                error.StartColor( UnityEngine.Color.red );
                error.Add( "A Prime is blocking all attempts to interface with Sleepers on this planet." );
                error.EndColor();
                return error.GetStringAndResetForNextUpdate();
            }
            return base.GetDynamicDescription( target, hackerOrNull, planet, hackerFaction, hackingType );
        }

        public override void DoOneSecondOfHackingLogic_AsPartOfMainSim( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            Faction spawnFaction = World_AIW2.Instance.GetFactionByIndex( planet.InitialOwningAIFactionIndex );
            if ( spawnFaction == null )
                spawnFaction = World_AIW2.GetRandomAIFaction( Context );
            FInt mult = GetHackingLevelMultiplier( spawnFaction );

            FInt budget = FInt.Zero;
            if ( Hacker.ActiveHack_DurationThusFar % type.PrimaryHackResponseInterval == 0 )
                budget += type.PrimaryResponseStrengthPerInterval * mult * 1000;
            if ( Hacker.ActiveHack_DurationThusFar % type.SecondaryHackResponseInterval == 0 )
                budget += type.SecondaryResponseStrengthPerInterval * mult * 1000;
            if ( Hacker.ActiveHack_DurationThusFar % type.TertiaryHackResponseInterval == 0 )
                budget += type.TertiaryResponseStrengthPerInterval * mult * 1000;

            if ( budget > FInt.One )
            {
                List<GameEntity_Squad> targets = new List<GameEntity_Squad>();
                Hacker.PlanetFaction.Entities.DoForEntities( EntityRollupType.Combatants, entity =>
                {
                    targets.Add( entity );

                    return DelReturn.Continue;
                } );
                if ( targets.Count > 0 )
                    ExoGalacticAttackManager.SendExoGalacticAttack( ExoOptions.CreateWithDefaults( targets, budget.GetNearestIntPreferringHigher(), spawnFaction, Hacker.PlanetFaction.Faction ), Context );
                else
                    ExoGalacticAttackManager.SendExoGalacticAttack( ExoOptions.CreateWithDefaults( Hacker, budget.GetNearestIntPreferringHigher(), spawnFaction, Hacker.PlanetFaction.Faction ), Context );
            }
            if ( Hacker.ActiveHack_DurationThusFar >= type.GetEffectiveHackDuration() )
                DoSuccessfulCompletionLogic( Target, planet, Hacker, Context, type, Event );
        }

        public override bool DoSuccessfulCompletionLogic( GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            Faction spawnFaction = World_AIW2.Instance.GetFactionByIndex( planet.InitialOwningAIFactionIndex );
            if ( spawnFaction == null )
                spawnFaction = World_AIW2.GetRandomAIFaction( Context );
            FInt mult = GetHackingLevelMultiplier( spawnFaction );
            ExoGalacticAttackManager.SendExoGalacticAttack( ExoOptions.CreateWithDefaults( Hacker, (type.ResponseStrengthOnCompletion * mult * 1000).GetNearestIntPreferringHigher(), spawnFaction, Hacker.PlanetFaction.Faction ), Context );

            GameEntityTypeData primeData = GameEntityTypeDataTable.Instance.GetRowByName( Sleepers.UNIT_NAMES.SleeperPrime.ToString() );
            planet.Mapgen_SeedEntity( Context, World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType(typeof(Dreamers)), primeData, PlanetSeedingZone.MostAnywhere ).SetWorldLocation(TargetOrNull.WorldLocation);

            bool unused;
            Hacker.PlanetFaction.Faction.StoredHacking -= GetCostToHack( TargetOrNull, planet, type, out unused );
            Event.HackingPointsSpent = GetCostToHack( TargetOrNull, planet, type, out unused );
            Event.HackingPointsLeftAfterHack = Hacker.PlanetFaction.Faction.StoredHacking;

            TargetOrNull.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );

            return true;
        }
    }
}
