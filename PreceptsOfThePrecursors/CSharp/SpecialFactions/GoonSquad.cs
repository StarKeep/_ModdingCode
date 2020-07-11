using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using System;

namespace PreceptsOfThePrecursors
{
    // Main faction class.
    public class GoonSquad : BaseSpecialFaction
    {
        protected override string TracingName => "GoonSquad";
        protected override bool EverNeedsToRunLongRangePlanning => true;
        protected override int MinimumSecondsBetweenLongRangePlannings => 5;

        public static GoonSquadData goonData;

        public enum Commands
        {
            PopulateGoonSquad
        }

        public enum Ship
        {
            DemocracyDownfall,
            NeinzulShardlingSwarm
        }

        public static ArcenSparseLookup<Ship, EntityCollection> Ships;

        public static string messageToSend;

        public GoonSquad() { goonData = null; messageToSend = null; }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( goonData == null )
            {
                goonData = World.Instance.GetGoonSquadData();
                World.Instance.SetGoonSquadData( goonData );
            }

            if ( Ships == null )
                Ships = new ArcenSparseLookup<Ship, EntityCollection>();

            for ( int x = 0; x < Ships.GetPairCount(); x++ )
            {
                Ship shipType = Ships.GetPairByIndex( x ).Key;
                Ships[shipType].DoForEntities( ( GameEntity_Squad entity ) =>
                {
                    switch ( shipType )
                    {
                        case Ship.DemocracyDownfall:
                            HandleDownfall( entity );
                            break;
                        case Ship.NeinzulShardlingSwarm:
                            HandleShardling( entity, Context );
                            break;
                        default:
                            break;
                    }

                    return DelReturn.Continue;
                } );
            }

            if ( messageToSend != null )
            {
                // Get our journal entry.
                JournalEntry journal = JournalEntryTable.Instance.GetRowByName( messageToSend );
                string header = DysonPrecursors.GetJournalHeading( "Admiral Alan Edwards", "Minor" );

                // Update our journal's text to include our new header.
                journal.FullText = header + journal.FullText;

                // Send our journal.
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( messageToSend, string.Empty, Context );

                // Add our new entry to be managed by our MothershipData on load.
                goonData.JournalEntries.AddPair( messageToSend, header );

                messageToSend = null;
            }
        }

        private void HandleDownfall( GameEntity_Squad entity )
        {
            if ( entity.PlanetFaction.Faction.Type == FactionType.Player && !goonData.JournalEntries.GetHasKey( Ship.DemocracyDownfall.ToString() ) )
                messageToSend = Ship.DemocracyDownfall.ToString();
        }

        private void HandleShardling( GameEntity_Squad entity, ArcenSimContext Context )
        {
            World_AIW2.Instance.DoForFactions( faction =>
            {
                if ( entity.PlanetFaction.Faction.GetIsHostileTowards( faction ) )
                    entity.Planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( ( GameEntity_Squad otherEntity ) =>
                      {
                          if ( otherEntity.TypeData.Mass_tX < 5 )
                              return DelReturn.Continue;

                          if ( entity.Systems[0].GetIsTargetInRange( otherEntity, RangeCheckType.ForActualFiring ) )
                          {
                              GameEntity_Squad shardling = GameEntity_Squad.CreateNew( entity.PlanetFaction, GameEntityTypeDataTable.Instance.GetRowByName( "NeinzulShardling" ), entity.CurrentMarkLevel, 
                                  entity.FleetMembership.Fleet, 1, otherEntity.WorldLocation, Context );
                              shardling.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, entity.PlanetFaction.Faction.FactionIndex );
                          }

                          return DelReturn.Continue;
                      } );
                return DelReturn.Continue;
            } );
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand populateCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.PopulateGoonSquad.ToString() ), GameCommandSource.AnythingElse, faction );
            populateCommand.RelatedBool = true;

            World_AIW2.Instance.DoForEntities( ( GameEntity_Squad workingEntity ) =>
            {
                if ( !Enum.TryParse( workingEntity.TypeData.InternalName, out Ship shipName ) )
                    return DelReturn.Continue;

                if ( !Ships.GetHasKey( shipName ) || !Ships[shipName].Contains( workingEntity ) )
                    populateCommand.RelatedEntityIDs.Add( workingEntity.PrimaryKeyID );

                return DelReturn.Continue;
            } );

            if ( populateCommand.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( populateCommand );
        }
    }
}