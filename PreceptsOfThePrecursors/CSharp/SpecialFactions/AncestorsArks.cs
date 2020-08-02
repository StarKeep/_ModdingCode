using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using System;

namespace PreceptsOfThePrecursors
{
    // Main faction class.
    public class AncestorsArks : BaseSpecialFaction
    {
        protected override string TracingName => "AncestorsArks";
        protected override bool EverNeedsToRunLongRangePlanning => true;
        protected override int MinimumSecondsBetweenLongRangePlannings => 5;

        public static AncestorsArksData goonData;

        public enum Commands
        {
            PopulateAncestorsArks
        }

        public enum Ship
        {
            DemocracyDownfall,
            NeinzulShardlingSwarm,
            TheClockwork,
            ReprocessorRegnat
        }

        public static ArcenSparseLookup<Ship, EntityCollection> Ships;

        public static string messageToSend;

        public AncestorsArks() { goonData = null; messageToSend = null; }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( goonData == null )
            {
                goonData = World.Instance.GetAncestorsArksData();
                World.Instance.SetAncestorsArksData( goonData );
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
                            AddDownfallJournal( entity );
                            break;
                        case Ship.NeinzulShardlingSwarm:
                            if ( entity.SelfBuildingMetalRemaining <= 0 && entity.SecondsSpentAsRemains <= 0 )
                                HandleShardling( entity, Context );
                            break;
                        case Ship.TheClockwork:
                            AddClockworkJournal( entity );
                            break;
                        case Ship.ReprocessorRegnat:
                            AddReprocessorRegnatJournal( entity );
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

        private void AddDownfallJournal( GameEntity_Squad entity )
        {
            if ( entity.PlanetFaction.Faction.Type == FactionType.Player && !goonData.JournalEntries.GetHasKey( Ship.DemocracyDownfall.ToString() ) )
                messageToSend = Ship.DemocracyDownfall.ToString();
        }

        private void AddClockworkJournal(GameEntity_Squad entity )
        {
            if ( entity.PlanetFaction.Faction.Type == FactionType.Player && !goonData.JournalEntries.GetHasKey( Ship.TheClockwork.ToString() ) )
                messageToSend = Ship.TheClockwork.ToString();
        }

        private void AddReprocessorRegnatJournal(GameEntity_Squad entity )
        {
            if ( entity.PlanetFaction.Faction.Type == FactionType.Player && !goonData.JournalEntries.GetHasKey( Ship.ReprocessorRegnat.ToString() ) )
                messageToSend = Ship.ReprocessorRegnat.ToString();
        }

        private void HandleShardling( GameEntity_Squad entity, ArcenSimContext Context )
        {
            World_AIW2.Instance.DoForFactions( faction =>
            {
                if ( entity.PlanetFaction.Faction.GetIsHostileTowards( faction ) )
                    entity.Planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( ( GameEntity_Squad otherEntity ) =>
                      {
                          if ( otherEntity.TypeData.Mass_tX < 5 || otherEntity.TypeData.TargetTypeForPlayer != PlayerTargetType.AutotargetAlways )
                              return DelReturn.Continue;

                          if ( entity.Systems[0].GetIsTargetInRange( otherEntity, RangeCheckType.ForActualFiring ) )
                          {
                              for (int x = 0; x < entity.CurrentMarkLevel * 5; x++)
                              {
                                  GameEntity_Squad shardling = GameEntity_Squad.CreateNew(entity.PlanetFaction, GameEntityTypeDataTable.Instance.GetRowByName("NeinzulShardling"), entity.CurrentMarkLevel,
                                      entity.FleetMembership.Fleet, 1, otherEntity.WorldLocation, Context);
                                  shardling.Orders.SetBehaviorDirectlyInSim(EntityBehaviorType.Attacker_Full, entity.PlanetFaction.Faction.FactionIndex);
                              }
                          }

                          return DelReturn.Continue;
                      } );
                return DelReturn.Continue;
            } );
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand populateCommand = StaticMethods.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.PopulateAncestorsArks.ToString() ), GameCommandSource.AnythingElse, faction );
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