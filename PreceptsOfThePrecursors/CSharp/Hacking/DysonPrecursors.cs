using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using System;
using System.Collections.Generic;

namespace PreceptsOfThePrecursors
{
    public class Hacking_OverrideMothershipPacification : BaseHackingImplementation
    {
        public static bool IsActive = false;
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Hackable result = base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
            if ( !Target.PlanetFaction.Faction.GetIsFriendlyTowards( HackerFaction ) || Hacking_MothershipPacification.IsActive )
                return Hackable.NeverCanBeHacked_Hide;
            if ( result != Hackable.CanBeHacked )
                return result;
            return Hackable.CanBeHacked;
        }
        public override void DoOneSecondOfHackingLogic_AsPartOfMainSim( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            Target.PlanetFaction.Faction.HackingPointsUsedAgainstThisFaction += type.HackingCostPerSecond;
            Hacker.PlanetFaction.Faction.StoredHacking -= type.HackingCostPerSecond;
            Event.HackingPointsSpent += type.HackingCostPerSecond;
            IsActive = true;
            if ( Target.Planet.Index != Hacker.Planet.Index )
                base.DoOnCancel( Target, planet, Hacker, type, Event );
        }
        public override bool CheckIfHackIsDone( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type )
        {
            if ( Hacker.PlanetFaction.Faction.StoredHacking <= 0 ) //Stops the hack if you hit 0 hacking
            {
                return true;
            }

            //you can just do the hack until the AI forces break you or hit zero hacking
            return false;
        }
        public override int GetTotalSecondsToHack( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type )
        {
            return -1;
        }
        public override void DoOnCancel( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type, HackingEvent Event )
        {
            return;
        }
        public override bool DoSuccessfulCompletionLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            return true;
        }
    }
    public class Hacking_MothershipPacification : BaseHackingImplementation
    {
        public static bool IsActive = false;
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Hackable result = base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
            if ( Target.PlanetFaction.Faction.GetIsFriendlyTowards( HackerFaction ) || Hacking_OverrideMothershipPacification.IsActive )
                return Hackable.NeverCanBeHacked_Hide;
            if ( result != Hackable.CanBeHacked )
                return result;
            return Hackable.CanBeHacked;
        }
        public override void DoOneSecondOfHackingLogic_AsPartOfMainSim( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            Target.PlanetFaction.Faction.HackingPointsUsedAgainstThisFaction += type.HackingCostPerSecond;
            Hacker.PlanetFaction.Faction.StoredHacking -= type.HackingCostPerSecond;
            Event.HackingPointsSpent += type.HackingCostPerSecond;
            IsActive = true;
            if ( Target.Planet.Index != Hacker.Planet.Index )
                base.DoOnCancel( Target, planet, Hacker, type, Event );
        }
        public override bool CheckIfHackIsDone( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type )
        {
            if ( Hacker.PlanetFaction.Faction.StoredHacking <= 0 ) //Stops the hack if you hit 0 hacking
            {
                return true;
            }

            //you can just do the hack until the AI forces break you or hit zero hacking
            return false;
        }
        public override int GetTotalSecondsToHack( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type )
        {
            return -1;
        }
        public override void DoOnCancel( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type, HackingEvent Event )
        {
            return;
        }
        public override bool DoSuccessfulCompletionLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            return true;
        }
    }
    public class Hacking_DestroyDysonNodeWeakest : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Hackable result = base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
            if ( result != Hackable.CanBeHacked )
                return result;
            if ( !Target.PlanetFaction.Faction.GetIsFriendlyTowards( HackerFaction ) )
                return Hackable.NeverCanBeHacked_Hide;
            if ( DysonPrecursors.DysonNodes == null || !DysonPrecursors.DysonNodes.GetHasKey( Target.Planet ) )
                return Hackable.NeverCanBeHacked_Hide;
            for ( int x = 0; x < Target.CurrentMarkLevel - 1; x++ )
                if ( DysonPrecursors.DysonNodes[Target.Planet][x] != null )
                    return Hackable.NeverCanBeHacked_Hide;
            return Hackable.CanBeHacked;
        }
        public override bool DoSuccessfulCompletionLogic( GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            TargetOrNull.Die( Context, true );
            return true;
        }
    }
    public class Hacking_DestroyDysonNodeStrongest : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Hackable result = base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
            if ( result != Hackable.CanBeHacked )
                return result;
            if ( !Target.PlanetFaction.Faction.GetIsFriendlyTowards( HackerFaction ) )
                return Hackable.NeverCanBeHacked_Hide;
            if ( DysonPrecursors.DysonNodes == null || !DysonPrecursors.DysonNodes.GetHasKey( Target.Planet ) )
                return Hackable.NeverCanBeHacked_Hide;
            for ( int x = 6; x > Target.CurrentMarkLevel - 1; x-- )
                if ( DysonPrecursors.DysonNodes[Target.Planet][x] != null )
                    return Hackable.NeverCanBeHacked_Hide;
            return Hackable.CanBeHacked;
        }
        public override bool DoSuccessfulCompletionLogic( GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            TargetOrNull.Die( Context, true );
            return true;
        }
    }
    public class Hacking_StealDysonUnits : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Hackable result = base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
            if ( result != Hackable.CanBeHacked )
                return result;
            if ( !Target.PlanetFaction.Faction.GetIsFriendlyTowards( HackerFaction ) || Target.Planet.GetProtoSphereData().Type != DysonProtoSphereData.ProtoSphereType.Protecter || Target.Planet.GetProtoSphereData().HasBeenHacked )
                return Hackable.NeverCanBeHacked_Hide;
            return Hackable.CanBeHacked;
        }
        public override void DoOneSecondOfHackingLogic_AsPartOfMainSim( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            Target.PlanetFaction.Faction.HackingPointsUsedAgainstThisFaction += type.HackingCostPerSecond;
            Hacker.PlanetFaction.Faction.StoredHacking -= type.HackingCostPerSecond;
            Event.HackingPointsSpent += type.HackingCostPerSecond;

            // Spawn drones. Small ones every second, big ones every 5 seconds. Use the Suppressor faction.
            // Not too strong, the big spawns come from popping Dyson Nodes.
            Faction spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonSuppressors ) );
            GameEntityTypeData sentinelData = GameEntityTypeDataTable.Instance.GetRowByName( "DysonDefender" );
            GameEntityTypeData defenderData = GameEntityTypeDataTable.Instance.GetRowByName( "DysonSentinel" );
            GameEntityTypeData bulwarkData = GameEntityTypeDataTable.Instance.GetRowByName( "DysonBulwark" );
            planet.Mapgen_SeedEntity( Context, spawnFaction, sentinelData, PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, spawnFaction.FactionIndex );
            planet.Mapgen_SeedEntity( Context, spawnFaction, defenderData, PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, spawnFaction.FactionIndex );
            if ( Hacker.ActiveHack_DurationThusFar % 5 == 0 )
                planet.Mapgen_SeedEntity( Context, spawnFaction, bulwarkData, PlanetSeedingZone.OuterSystem ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, spawnFaction.FactionIndex );

            if ( Hacker.ActiveHack_DurationThusFar < 60 )
                return;

            // Give them their first batch of units.
            if ( Hacker.ActiveHack_DurationThusFar == 60 )
            {
                Fleet.Membership defenderMem = Hacker.FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( sentinelData );
                defenderMem.ExplicitBaseSquadCap += 10;

                Fleet.Membership sentinelMem = Hacker.FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( defenderData );
                sentinelMem.ExplicitBaseSquadCap += 10;

                World_AIW2.Instance.QueueChatMessageOrCommand( "You have stolen the designs for 10 Dyson Defenders and 10 Dyson Sentinels.", ChatType.LogToCentralChat, Context );
            }
            else
            {
                if ( Hacker.ActiveHack_DurationThusFar % 30 == 0 )
                {
                    // Pop a node and give them some more units.
                    GameEntity_Squad nodeToPop = null;
                    for ( int x = 0; x < DysonPrecursors.DysonNodes[planet].Length && nodeToPop == null; x++ )
                        if ( DysonPrecursors.DysonNodes[planet][x] != null )
                            nodeToPop = DysonPrecursors.DysonNodes[planet][x];

                    nodeToPop.Die( Context, true );

                    Fleet.Membership defenderMem = Hacker.FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( sentinelData );
                    defenderMem.ExplicitBaseSquadCap += 5;

                    Fleet.Membership sentinelMem = Hacker.FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( defenderData );
                    sentinelMem.ExplicitBaseSquadCap += 5;

                    // If the hack has gone on for long enough, also reward them with a Bulwark.
                    if ( Hacker.ActiveHack_DurationThusFar > 210 )
                    {
                        Fleet.Membership bulwarkMem = Hacker.FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( bulwarkData );
                        bulwarkMem.ExplicitBaseSquadCap++;
                        World_AIW2.Instance.QueueChatMessageOrCommand( $"You have stolen the designs from a Dyson Node on {planet.Name}, gaining another 5 Dyson Defenders and 5 Dyson Sentinels. You have also managed to get a Dyson Bulwark!", ChatType.LogToCentralChat, Context );
                    }
                    else
                        World_AIW2.Instance.QueueChatMessageOrCommand( "You have stolen the designs from a Dyson Node on {planet.Name}, gaining another 5 Dyson Defenders and 5 Dyson Sentinels.", ChatType.LogToCentralChat, Context );
                }
            }

        }
        public override bool CheckIfHackIsDone( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type )
        {
            // Stop the hack if you run out of HaP, or there are no more valid Dyson Nodes.
            if ( Hacker.PlanetFaction.Faction.StoredHacking <= 0 )
            {
                return true;
            }

            if ( DysonPrecursors.DysonNodes[Target.Planet] == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( $"Your hack on {planet.Name} has ended. No more Dyson Nodes.", ChatType.LogToCentralChat, null );
                return true;
            }

            //you can just do the hack until the AI forces break you or hit zero hacking
            return false;
        }
        public override int GetTotalSecondsToHack( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type )
        {
            return -1;
        }
        public override void DoOnCancel( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type, HackingEvent Event )
        {
            return;
        }
        public override bool DoSuccessfulCompletionLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            return true;
        }
    }
    public class Hacking_AwakenDysonPrecursors : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Hackable result = base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
            if ( result != Hackable.CanBeHacked )
                return result;
            Faction precursorFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonPrecursors ) );
            if ( precursorFaction == null || (precursorFaction.MustBeAwakenedByPlayer && precursorFaction.HasBeenAwakenedByPlayer) )
                return Hackable.NeverCanBeHacked_Hide;
            return Hackable.CanBeHacked;
        }
        public override void DoOneSecondOfHackingLogic_AsPartOfMainSim( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            // Spawn drones. small ones every second, big one every 10 seconds. Use the Suppressor faction.
            Faction spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonSuppressors ) );
            GameEntityTypeData sentinelData = GameEntityTypeDataTable.Instance.GetRowByName( "DysonDefender" );
            GameEntityTypeData defenderData = GameEntityTypeDataTable.Instance.GetRowByName( "DysonSentinel" );
            GameEntityTypeData bulwarkData = GameEntityTypeDataTable.Instance.GetRowByName( "DysonBulwark" );
            List<GameEntity_Squad> spawntShips = new List<GameEntity_Squad>();
            spawntShips.Add( planet.Mapgen_SeedEntity( Context, spawnFaction, sentinelData, PlanetSeedingZone.OuterSystem ) );
            spawntShips.Add( planet.Mapgen_SeedEntity( Context, spawnFaction, defenderData, PlanetSeedingZone.OuterSystem ) );
            if ( Hacker.ActiveHack_DurationThusFar % 10 == 0 )
                spawntShips.Add( planet.Mapgen_SeedEntity( Context, spawnFaction, bulwarkData, PlanetSeedingZone.OuterSystem ) );

            // Mark up our ships if needed, and set them to attack.
            byte markLevel = (byte)Math.Min( 7, 1 + Hacker.ActiveHack_DurationThusFar / 30 );
            for ( int x = 0; x < spawntShips.Count; x++ )
            {
                spawntShips[x].SetCurrentMarkLevel( markLevel, Context );
                spawntShips[x].Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, spawnFaction.FactionIndex );
            }

            // Every 30 seconds, spawn in another node if there is room.
            if ( Hacker.ActiveHack_DurationThusFar % 30 == 0 )
            {
                planet.DoForLinkedNeighborsAndSelf( false, workingPlanet =>
                {
                    int highestMarkNode = 0;
                    workingPlanet.DoForEntities( delegate ( GameEntity_Squad workingEntity )
                    {
                        if ( workingEntity.TypeData.GetHasTag( DysonPrecursors.DYSON_NODE_NAME ) )
                            highestMarkNode = Math.Max( highestMarkNode, workingEntity.CurrentMarkLevel );

                        return DelReturn.Continue;
                    } );
                    if ( highestMarkNode < 7 )
                    {
                        (spawnFaction.Implementation as DysonSuppressors).CreateDysonNode( spawnFaction, workingPlanet, highestMarkNode + 1, Context, string.Empty );
                        World_AIW2.Instance.QueueChatMessageOrCommand( $"Another Dyson Node has been awakened on {workingPlanet.Name}.", ChatType.ShowToEveryone, Context );
                    }

                    return DelReturn.Continue;
                } );
            }

            base.DoOneSecondOfHackingLogic_AsPartOfMainSim( Target, planet, Hacker, Context, type, Event );
        }
        public override bool DoSuccessfulCompletionLogic( GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenSimContext Context, HackingType type, HackingEvent Event )
        {
            // Awaken the faction.
            Faction precursorFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonPrecursors ) );
            precursorFaction.HasBeenAwakenedByPlayer = true;

            // Give a notifiaction and science.
            World_AIW2.Instance.QueueChatMessageOrCommand( "The Dyson Precursors have been awakened, and you have gained 20000 science from studying the process.", ChatType.ShowToEveryone, Context );
            Hacker.PlanetFaction.Faction.StoredScience += 20000;

            // Spawn the mothership.
            (precursorFaction.Implementation as DysonPrecursors).SpawnMothership( TargetOrNull, precursorFaction, Context );

            // Kill trust on nearby planets.
            planet.DoForPlanetsWithinXHops( Context, 2, ( workingPlanet, distance ) =>
            {
                DysonPrecursors.MothershipData.Trust.SetTrust( workingPlanet, -3000 );

                return DelReturn.Continue;
            } );

            // Pop the node.
            TargetOrNull.Despawn( Context, true, InstancedRendererDeactivationReason.IFinishedMyJob );
            
            return base.DoSuccessfulCompletionLogic(TargetOrNull, planet, Hacker, Context, type, Event);
        }
    }
}
