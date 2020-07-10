using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace PreceptsOfThePrecursors
{
    // Main faction class.
    public class GoonSquad : BaseSpecialFaction
    {
        protected override string TracingName => "GoonSquad";
        protected override bool EverNeedsToRunLongRangePlanning => true;
        protected override int MinimumSecondsBetweenLongRangePlannings => 5;

        public static GoonSquadData goonData;

        private static string DOWNFALL = "DemocracyDownfall";

        public static string messageToSend;

        public GoonSquad() { goonData = null; messageToSend = null; }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( goonData == null )
            {
                goonData = World.Instance.GetGoonSquadData();
                World.Instance.SetGoonSquadData( goonData );
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

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            World_AIW2.Instance.DoForEntities( ( GameEntity_Squad workingEntity ) =>
            {
                if ( workingEntity.PlanetFaction.Faction.Type != FactionType.Player )
                    return DelReturn.Continue;

                if ( workingEntity.TypeData.InternalName == DOWNFALL && !goonData.JournalEntries.GetHasKey( DOWNFALL ) )
                    messageToSend = DOWNFALL;

                return DelReturn.Continue;
            } );
        }
    }
}