using System;
using System.Collections.Generic;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

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

        public static ArcenSparseLookup<Ship, Arcen.AIW2.Core.EntityCollection> Ships;

        public static string messageToSend;

        public AncestorsArks() { goonData = null; messageToSend = null; }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( World_AIW2.Instance.GameSecond == 5 )
                SetupScrapyards( Context );

            if ( goonData == null )
            {
                goonData = faction.GetAncestorsArksData( ExternalDataRetrieval.CreateIfNotFound );
                faction.SetAncestorsArksData( goonData );
            }

            if ( Ships == null )
                Ships = new ArcenSparseLookup<Ship, Arcen.AIW2.Core.EntityCollection>();

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

        private void SetupScrapyards( ArcenSimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            int cap = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "ScrapyardCap" ), 
                baseHops = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "ScrapyardHopsBase" ), 
                hopsIncrease = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "ScrapyardHopIncreasePer" );

            List<GameEntityTypeData> possibleTypes = new List<GameEntityTypeData>();
            for ( int x = 0; x < GameEntityTypeDataTable.Instance.Rows.Count; x++ )
            {
                GameEntityTypeData workingData = GameEntityTypeDataTable.Instance.Rows[x];
                if ( workingData.CapturableCanSeedAtAll && workingData.CapturableMaxPerGalaxy > 0 && (workingData.IsGolem || workingData.IsArk) )
                    possibleTypes.Add( workingData );
            }

            for ( int x = 0; x < possibleTypes.Count; x++ )
                if ( World_AIW2.Instance.GetNeutralFaction().GetFirstMatching( possibleTypes[x], true, true ) != null )
                {
                    possibleTypes.RemoveAt( x );
                    x--;
                }

            for ( int x = 0; x < cap && possibleTypes.Count >= 3; x++ )
            {
                ScrapyardData scrapyardData = new ScrapyardData();
                int indexToUse;
                while ( scrapyardData.Alpha == null && possibleTypes.Count > 0 )
                {
                    indexToUse = Context.RandomToUse.Next( possibleTypes.Count );
                    scrapyardData.Alpha = possibleTypes[indexToUse];
                    possibleTypes.RemoveAt( indexToUse );
                }

                if ( possibleTypes.Count < 2 )
                    break;

                while ( scrapyardData.Beta == null && possibleTypes.Count > 0 )
                {
                    indexToUse = Context.RandomToUse.Next( possibleTypes.Count );
                    scrapyardData.Beta = possibleTypes[indexToUse];
                    possibleTypes.RemoveAt( indexToUse );
                }

                if ( possibleTypes.Count < 1 )
                    break;

                while ( scrapyardData.Gamma == null && possibleTypes.Count > 0 )
                {
                    indexToUse = Context.RandomToUse.Next( possibleTypes.Count );
                    scrapyardData.Gamma = possibleTypes[indexToUse];
                    possibleTypes.RemoveAt( indexToUse );
                }

                if ( scrapyardData.Alpha == null || scrapyardData.Beta == null || scrapyardData.Gamma == null )
                    break;

                List<Planet> possiblePlanets = new List<Planet>();
                World_AIW2.Instance.DoForPlanets( false, planet =>
                {
                    if ( planet.OriginalHopsToHumanHomeworld == baseHops + hopsIncrease * x )
                        possiblePlanets.Add( planet );

                    return DelReturn.Continue;
                } );

                if ( possiblePlanets.Count < 1 )
                    break;

                Planet spawnPlanet = possiblePlanets[Context.RandomToUse.Next( possiblePlanets.Count )];

                spawnPlanet.Mapgen_SeedEntity( Context, World_AIW2.Instance.GetNeutralFaction(), GameEntityTypeDataTable.Instance.GetRowByName( "PotPScrapyard" ), PlanetSeedingZone.MostAnywhere ).SetScrapyardData( scrapyardData );

                World_AIW2.Instance.QueueChatMessageOrCommand( $"Scrapyard seeded on {spawnPlanet.Name}.", ChatType.LogToCentralChat, Context );
            }
        }

        private void AddDownfallJournal( GameEntity_Squad entity )
        {
            if ( entity.PlanetFaction.Faction.Type == FactionType.Player && !goonData.JournalEntries.GetHasKey( Ship.DemocracyDownfall.ToString() ) )
                messageToSend = Ship.DemocracyDownfall.ToString();
        }

        private void AddClockworkJournal( GameEntity_Squad entity )
        {
            if ( entity.PlanetFaction.Faction.Type == FactionType.Player && !goonData.JournalEntries.GetHasKey( Ship.TheClockwork.ToString() ) )
                messageToSend = Ship.TheClockwork.ToString();
        }

        private void AddReprocessorRegnatJournal( GameEntity_Squad entity )
        {
            if ( entity.PlanetFaction.Faction.Type == FactionType.Player && !goonData.JournalEntries.GetHasKey( Ship.ReprocessorRegnat.ToString() ) )
                messageToSend = Ship.ReprocessorRegnat.ToString();
        }

        private void HandleShardling( GameEntity_Squad entity, ArcenSimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            World_AIW2.Instance.DoForFactions( faction =>
            {
                if ( entity.PlanetFaction.Faction.GetIsHostileTowards( faction ) )
                    entity.Planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( ( GameEntity_Squad otherEntity ) =>
                        {
                            if ( otherEntity.TypeData.Mass_tX < 5 || otherEntity.TypeData.TargetTypeForPlayer != PlayerTargetType.AutotargetAlways )
                                return DelReturn.Continue;

                            if ( entity.Systems[0].GetIsTargetInRange( otherEntity, RangeCheckType.ForActualFiring ) )
                            {
                                for ( int x = 0; x < entity.CurrentMarkLevel * 5; x++ )
                                {
                                    GameEntity_Squad shardling = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( entity.PlanetFaction, GameEntityTypeDataTable.Instance.GetRowByName( "NeinzulShardling" ), entity.CurrentMarkLevel,
                                        entity.FleetMembership.Fleet, 1, otherEntity.WorldLocation, Context );
                                    shardling.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, entity.PlanetFaction.Faction.FactionIndex );
                                }
                            }

                            return DelReturn.Continue;
                        } );
                return DelReturn.Continue;
            } );
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( Ships == null )
                return;

            GameCommand populateCommand = Utilities.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.PopulateAncestorsArks.ToString() ), GameCommandSource.AnythingElse, faction );
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