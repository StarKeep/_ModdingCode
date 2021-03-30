using System;
using System.Collections.Generic;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace ReactiveAIFireteams
{
    // Base for all Enclave subfactions.
    public class ReactiveAIFireteams : BaseSpecialFaction
    {
        protected override string TracingName => "ReactiveAIFireteams";
        protected override bool EverNeedsToRunLongRangePlanning => true;

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            World_AIW2.Instance.DoForFactions( workingFaction =>
            {
                if ( workingFaction.Implementation is SpecialFaction_HunterFleet )
                    UpdateFireteamLogic( workingFaction, (workingFaction.Implementation as SpecialFaction_HunterFleet).Teams, Context );

                if ( workingFaction.Implementation is SpecialFaction_AISpecialForces )
                    UpdateFireteamLogic( workingFaction, (workingFaction.Implementation as SpecialFaction_AISpecialForces).Teams, Context );


                return DelReturn.Continue;
            } );
        }

        public void UpdateFireteamLogic( Faction faction, ArcenLessLinkedList<Fireteam> fireteams, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( fireteams == null || fireteams.Count == 0 )
                return;
            try
            {
                ArcenSparseLookup<Planet, List<Fireteam>> fireteamsAttacking = new ArcenSparseLookup<Planet, List<Fireteam>>();

                Fireteam.DoFor( fireteams, delegate ( Fireteam team )
                {
                    if ( team.ships.Count == 0 )
                        team.Disband( Context );
                    else
                    {
                        switch ( team.status )
                        {
                            case FireteamStatus.Attacking:
                                if ( team.TargetPlanet != null )
                                {
                                    if ( fireteamsAttacking.GetHasKey( team.TargetPlanet ) )
                                        fireteamsAttacking[team.TargetPlanet].Add( team );
                                    else
                                        fireteamsAttacking.AddPair( team.TargetPlanet, new List<Fireteam>() { team } );
                                }
                                break;
                            case FireteamStatus.Assembling:
                            case FireteamStatus.Staging:
                            case FireteamStatus.ReadyToAttack:
                                bool discarded = false;
                                if ( team.LurkPlanet != null && team.CurrentPlanet == team.LurkPlanet )
                                {
                                    int idleSince = -1;
                                    if ( team.History.Count > 0 )
                                        for ( int x = 0; x < team.History.Count; x++ )
                                            idleSince = Math.Max( idleSince, team.History[x].GameSecond );
                                    if ( idleSince > 0 && World_AIW2.Instance.GameSecond - idleSince > 15 )
                                    {
                                        team.DiscardCurrentObjectives();
                                        discarded = true;
                                    }
                                }
                                if ( !discarded && team.TargetPlanet != null )
                                {
                                    if ( fireteamsAttacking.GetHasKey( team.TargetPlanet ) )
                                        fireteamsAttacking[team.TargetPlanet].Add( team );
                                    else
                                        fireteamsAttacking.AddPair( team.TargetPlanet, new List<Fireteam>() { team } );
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    return DelReturn.Continue;
                } );
                fireteamsAttacking.DoFor( pair =>
                {
                    var stanceData = pair.Key.GetPlanetFactionForFaction( faction ).DataByStance;

                    int hostileStrength = stanceData[FactionStance.Hostile].TotalStrength;
                    int friendlyStrength = stanceData[FactionStance.Friendly].TotalStrength;
                    int ourStrength = stanceData[FactionStance.Self].TotalStrength;

                    for ( int x = 0; x < pair.Value.Count; x++ )
                    {
                        Fireteam team = pair.Value[x];
                        team.BuildShipsLookup( true, null );
                        team.shipsByPlanet.DoFor( ships =>
                        {
                            if ( ships.Key == pair.Key )
                                return DelReturn.Continue;

                            for ( int y = 0; y < ships.Value.Count; y++ )
                                ourStrength += (ships.Value[y].GetStrengthPerSquad() * (1 + ships.Value[y].ExtraStackedSquadsInThis)) + ships.Value[y].AdditionalStrengthFromFactions;

                            return DelReturn.Continue;
                        } );
                    }

                    GameEntity_Squad retreatPoint = GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( faction, pair.Key, Context );
                    if ( retreatPoint != null )
                    {
                        // Run from a losing fight.
                        if ( hostileStrength > (friendlyStrength + ourStrength) * 2 )
                            for ( int x = 0; x < pair.Value.Count; x++ )
                                pair.Value[x].DisbandAndRetreat( faction, Context, retreatPoint );
                        else
                            // Bleed off Enclaves from winning fights.
                            while ( ourStrength + friendlyStrength > hostileStrength * 5 && pair.Value.Count > 0 )
                            {
                                ourStrength -= pair.Value[0].TeamStrength;
                                pair.Value[0].DisbandAndRetreat( faction, Context, retreatPoint );
                                pair.Value.RemoveAt( 0 );
                            }
                    }

                    return DelReturn.Continue;
                } );
            }

            catch ( Exception ) { }
        }
    }
}
