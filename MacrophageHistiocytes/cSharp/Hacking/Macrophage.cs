using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using MacrophageHistiocytes.SpecialFaction;

namespace MacrophageHistiocytes.Hacking
{
    public class Hacking_TameTelium : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            // Must not already be tamed, must have an existing tamed faction (no old saves allowed), and must not already be set to player allied in the lobby.
            if ( Target.TypeData.GetHasTag( Macrophage.TeliumTag ) && World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( MacrophageInfestationTamedHistiocytes ) ) != null &&
                Target.PlanetFaction.Faction.Implementation.GetType() != typeof( MacrophageInfestationTamedHistiocytes ) &&
                !((SpecialFaction_MacrophageInfestation)(Target.PlanetFaction.Faction.Implementation)).humanAllied )
            {
                RejectionReasonDescription = string.Empty;
                return Hackable.CanBeHacked;
            }
            RejectionReasonDescription = "The target is not a wild Telium.";
            return Hackable.NeverCanBeHacked_Hide;
        }

        public override void DoOneSecondOfHackingLogic_AsPartOfMainSim( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            GameEntityTypeData enragedHarvester;
            if ( Target.PlanetFaction.Faction.HasObtainedSpireDebris ) // If the Wild Macrophage has acquired Debris from Fallen Spire, chance to spawn an Enraged Spire Harvester.
                enragedHarvester = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, Macrophage.EnragedHarvesterTag );
            else
                enragedHarvester = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, Macrophage.RegularEnragedHarvesterTag );

            if ( Hacker.ActiveHack_DurationThusFar % type.PrimaryHackResponseInterval == 0 )
            {
                for ( int x = 0; x < type.PrimaryResponseStrengthPerInterval; x++ )
                {
                    Faction spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( SpecialFaction_MacrophageInfestationEnraged ) );
                    GameEntity_Squad guardianHarvester = GameEntity_Squad.CreateNew( planet.GetPlanetFactionForFaction( spawnFaction ),
                        enragedHarvester, (byte)Math.Min( 7, (Hacker.ActiveHack_DurationThusFar / type.PrimaryHackResponseInterval * type.PrimaryResponseStrengthIncreasePerEffect).GetNearestIntPreferringHigher() ), planet.GetPlanetFactionForFaction( spawnFaction ).FleetUsedAtPlanet,
                        0, Target.WorldLocation, Context );
                    guardianHarvester.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, spawnFaction.FactionIndex );
                }
            }

            if ( Hacker.ActiveHack_DurationThusFar % type.SecondaryHackResponseInterval == 0 )
            {
                for ( int x = 0; x < type.SecondaryResponseStrengthPerInterval; x++ )
                {
                    Faction spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( SpecialFaction_MacrophageInfestationEnraged ) );
                    GameEntity_Squad guardianHarvester = GameEntity_Squad.CreateNew( planet.GetPlanetFactionForFaction( spawnFaction ),
                        enragedHarvester, (byte)Math.Min( 7, (Hacker.ActiveHack_DurationThusFar / type.SecondaryHackResponseInterval * type.SecondaryResponseStrengthIncreasePerEffect).GetNearestIntPreferringHigher() ), planet.GetPlanetFactionForFaction( spawnFaction ).FleetUsedAtPlanet,
                        0, Target.WorldLocation, Context );
                    guardianHarvester.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, spawnFaction.FactionIndex );
                }
            }

            if ( Hacker.ActiveHack_DurationThusFar % type.TertiaryHackResponseInterval == 0 )
            {
                for ( int x = 0; x < type.TertiaryResponseStrengthPerInterval; x++ )
                {
                    Faction spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( SpecialFaction_MacrophageInfestationEnraged ) );
                    GameEntity_Squad guardianHarvester = GameEntity_Squad.CreateNew( planet.GetPlanetFactionForFaction( spawnFaction ),
                        enragedHarvester, (byte)Math.Min( 7, (Hacker.ActiveHack_DurationThusFar / type.TertiaryHackResponseInterval * type.TertiaryResponseStrengthIncreasePerEffect).GetNearestIntPreferringHigher() ), planet.GetPlanetFactionForFaction( spawnFaction ).FleetUsedAtPlanet,
                        0, Target.WorldLocation, Context );
                    guardianHarvester.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, spawnFaction.FactionIndex );
                }
            }

            if ( Hacker.ActiveHack_DurationThusFar >= type.GetEffectiveHackDuration() )
                DoSuccessfulCompletionLogic( Target, planet, Hacker, Context, type, Event );
        }

        public override bool DoSuccessfulCompletionLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, Macrophage.TeliumTag );
            ArcenPoint spawnLocation = Target.WorldLocation;
            Faction tamedFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( MacrophageInfestationTamedHistiocytes ) );
            PlanetFaction pFaction = Target.Planet.GetPlanetFactionForFaction( tamedFaction );
            if ( entityData == null )
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: no Telium tag found", Verbosity.DoNotShow );
            GameEntity_Squad tamedTelium = GameEntity_Squad.CreateNew( pFaction, entityData, entityData.MarkFor( pFaction ),
                                                   pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context );
            Target.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );

            bool unused;
            Hacker.PlanetFaction.Faction.StoredHacking -= GetCostToHack( Target, planet, type, out unused );
            Event.HackingPointsSpent = GetCostToHack( Target, planet, type, out unused );
            Event.HackingPointsLeftAfterHack = Hacker.PlanetFaction.Faction.StoredHacking;

            // Enable the tamed faction.
            tamedFaction.HasBeenAwakenedByPlayer = true;
            return true;
        }
    }
}
