using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using System;
using System.Collections.Generic;

namespace PreceptsOfThePrecursors
{
    // Costs for various things.
    public enum MinesToMarkUp
    {
        One = 14,
        Two = 28,
        Three = 42,
        Four = 56,
        Five = 70,
        Six = 84
    }
    public enum ResourcesToMarkUp
    {
        One = 70000,
        Two = 140000,
        Three = 210000,
        Four = 280000,
        Five = 350000,
        Six = 420000
    }
    public static class PrecursorCosts
    {
        public static int Mines( int currentMarkLevel, Faction faction )
        {
            int unModified;
            switch ( currentMarkLevel )
            {
                case 1:
                    unModified = (int)MinesToMarkUp.One;
                    break;
                case 2:
                    unModified = (int)MinesToMarkUp.Two;
                    break;
                case 3:
                    unModified = (int)MinesToMarkUp.Three;
                    break;
                case 4:
                    unModified = (int)MinesToMarkUp.Four;
                    break;
                case 5:
                    unModified = (int)MinesToMarkUp.Five;
                    break;
                default:
                    unModified = (int)MinesToMarkUp.Six;
                    break;
            }
            if ( faction.Ex_MinorFactionCommon_GetPrimitives().DebugMode )
                unModified = 5;
            return unModified;
        }
        public static int Resources( int currentMarkLevel, Faction faction )
        {
            int unModified;
            switch ( currentMarkLevel )
            {
                case 1:
                    unModified = (int)ResourcesToMarkUp.One;
                    break;
                case 2:
                    unModified = (int)ResourcesToMarkUp.Two;
                    break;
                case 3:
                    unModified = (int)ResourcesToMarkUp.Three;
                    break;
                case 4:
                    unModified = (int)ResourcesToMarkUp.Four;
                    break;
                case 5:
                    unModified = (int)ResourcesToMarkUp.Five;
                    break;
                default:
                    unModified = (int)ResourcesToMarkUp.Six;
                    break;
            }
            if ( faction.Ex_MinorFactionCommon_GetPrimitives().DebugMode )
                unModified /= 10;
            return unModified / 5 * (8 - (faction.Ex_MinorFactionCommon_GetPrimitives().Intensity / 2));
        }
    }
    public static class ProtoSphereCosts
    {
        public static int BuildCost( Faction faction )
        {
            int baseCost = 1000000;

            return baseCost / 5 * (8 - (faction.Ex_MinorFactionCommon_GetPrimitives().Intensity / 2));
        }
        public static int Resources( int currentMarkLevel, Faction faction )
        {
            int unModified;
            switch ( currentMarkLevel )
            {
                case 1:
                    unModified = (int)ResourcesToMarkUp.One;
                    break;
                case 2:
                    unModified = (int)ResourcesToMarkUp.Two;
                    break;
                case 3:
                    unModified = (int)ResourcesToMarkUp.Three;
                    break;
                case 4:
                    unModified = (int)ResourcesToMarkUp.Four;
                    break;
                case 5:
                    unModified = (int)ResourcesToMarkUp.Five;
                    break;
                default:
                    unModified = (int)ResourcesToMarkUp.Six;
                    break;
            }
            if ( faction.Ex_MinorFactionCommon_GetPrimitives().DebugMode )
                unModified /= 10;
            return unModified / 5 * (8 - (faction.Ex_MinorFactionCommon_GetPrimitives().Intensity / 2));
        }
    }

    // Description appender for Mothership.
    public class DysonMothershipDescriptionAppender : IGameEntityDescriptionAppender
    {
        public void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenDoubleCharacterBuffer Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;

            // Testchamber.
            if ( RelatedEntityOrNull.PlanetFaction.Faction.Type == FactionType.Player )
                return;

            // Gifts.
            if ( !RelatedEntityOrNull.TypeData.GetHasTag( "DysonMothership" ) )
                return;

            DysonMothershipData MothershipData = DysonPrecursors.MothershipData;
            if ( MothershipData == null )
                return;

            if ( MothershipData.Level >= 7 )
            {
                Buffer.Add( $"\nThis Mothership has reached its final form. It has stockpiled {MothershipData.Resources} metal and {MothershipData.Mines} mines. It can build another Proto Sphere after stockpiling {ProtoSphereCosts.BuildCost( RelatedEntityOrNull.PlanetFaction.Faction )} resources. " );
                if ( MothershipData.PlanetToBuildOn != null )
                    Buffer.Add( $"\nShe is planning to build a Proto Sphere on {MothershipData.PlanetToBuildOn.Name} next." );
            }
            else
            {
                Buffer.Add( $"\nThis Mothership is currently level {MothershipData.Level} and has {Math.Min( MothershipData.Resources, PrecursorCosts.Resources( MothershipData.Level, RelatedEntityOrNull.PlanetFaction.Faction ) )}/{PrecursorCosts.Resources( MothershipData.Level, RelatedEntityOrNull.PlanetFaction.Faction )} of the metal and {MothershipData.Mines}/{PrecursorCosts.Mines( MothershipData.Level, RelatedEntityOrNull.PlanetFaction.Faction )} of the consumed mines required to level up. " );
            }
            if ( MothershipData.MetalGainedOrLostLastSecond > 0 )
                Buffer.Add( $"Gaining {MothershipData.MetalGainedOrLostLastSecond} metal per second. " );
            else if ( MothershipData.MetalGainedOrLostLastSecond < 0 )
                Buffer.Add( $"Losing {MothershipData.MetalGainedOrLostLastSecond} metal per second." );
            string mood;
            int trust = MothershipData.Trust.GetTrust( RelatedEntityOrNull.Planet );
            switch ( trust )
            {
                case int _ when trust < -2000:
                    mood = "Controlling";
                    break;
                case int _ when trust < -1000:
                    mood = "Demanding";
                    break;
                case int _ when trust < -500:
                    mood = "Annoyed";
                    break;
                case int _ when trust < 0:
                    mood = "Cautious";
                    break;
                case int _ when trust < 500:
                    mood = "Neutral";
                    break;
                case int _ when trust < 1000:
                    mood = "Curious";
                    break;
                case int _ when trust < 2000:
                    mood = "Accepting";
                    break;
                default:
                    mood = "Trusting";
                    break;
            }

            Buffer.Add( $"\nIt is currently {mood} ({trust}) towards us on this planet. " );
            if ( MothershipData.IsGainingTrust )
                Buffer.Add( " Its trust towards nearby planets is currently increasing. " );
            else if ( MothershipData.IsLosingTrust )
                Buffer.Add( " Its trust towards nearby planets is currently decreasing. " );

            if ( RelatedEntityOrNull.Planet.GetIsControlledByFactionType( FactionType.Player ) && trust > -1000 && trust <= 1000 )
                Buffer.Add( " It has recognized that we consider this planet our home and has ceased aggression... for now. " );

            if ( World_AIW2.Instance.GameSecond - RelatedEntityOrNull.GameSecondEnteredThisPlanet < 30 )
                Buffer.Add( "\nIt is currently converting excess energy from its recent wormhole traversal to rapidly recharge its shields. " );

            if ( MothershipData.IsNearMine )
                Buffer.Add( "\nIt is currently using resources from nearby mines to repair itself. " );
            Buffer.Add( "\n" );
        }
    }

    // Main faction class.
    public class DysonPrecursors : BaseSpecialFaction, IBulkPathfinding
    {
        protected override string TracingName => "DysonPrecursors";
        protected override bool EverNeedsToRunLongRangePlanning => true;

        public ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> WormholeCommands { get; set; }
        public ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> MovementCommands { get; set; }

        public static string MOTHERSHIP_NAME = "DysonMothership";
        public static string DYSON_NODE_NAME = "DysonNode";
        public static string DYSON_PACKET_NAME = "DysonPacket";
        public static string DYSON_ANCIENT_NODE_NAME = "DysonNodeAncient";

        // Various things we need to keep track of.
        // Some are static to allow the ease of use for descriptions.
        public static GameEntity_Squad Mothership;
        public static DysonMothershipData MothershipData;
        public AntiMinorFactionWaveData WaveData;
        public ExoData ExoData;
        public static ArcenSparseLookup<Planet, GameEntity_Squad[]> DysonNodes;

        // Special. This will only ever be false once. Never set it back to false unless the game restarts.
        public static bool HeadersAppliedToOldJournals = false;

        public enum Commands
        {
            SetPlanetToBuildOn
        }

        public DysonPrecursors()
        {
            Mothership = null;
            MothershipData = null;
            DysonNodes = new ArcenSparseLookup<Planet, GameEntity_Squad[]>();
        }

        public static string GetJournalHeading( string sender, string urgency )
        {
            DateTime loggedTime = DateTime.Now;
            string date = DateTime.Now.ToLongDateString().Insert( DateTime.Now.ToLongDateString().Length - 4, "2" );
            return "Message Recieved" +
                $"\nDate: {date}" +
                $"\nSource: {sender}" +
                $"\nUrgency: {urgency}\n\n";
        }
        public override void SeedStartingEntities_LaterEverythingElse( Faction faction, Galaxy galaxy, ArcenSimContext Context, MapTypeData mapType )
        {
            if ( faction.MustBeAwakenedByPlayer )
            {
                SpawnAncientNode( faction, Context );
            }
        }

        public override bool GetShouldAttackNormallyExcludedTarget( Faction faction, GameEntity_Squad Target )
        {
            if ( Target.Planet.GetProtoSphereData().Level > 0 && (Target.TypeData.IsCommandStation || Target.TypeData.GetHasTag( "WarpGate" )) )
                return true;
            if ( Target.TypeData.GetHasTag( "NormalPlanetNastyPick" ) )
                return true;
            return false;
        }
        public override void UpdatePowerLevel( Faction faction )
        {
            faction.OverallPowerLevel = FInt.Zero;
            if ( MothershipData == null )
                return;
            if ( MothershipData.Level < 6 )
            {
                for ( int x = 0; x < MothershipData.Level; x++ )
                    faction.OverallPowerLevel += FInt.FromParts( 0, 500 );
                return;
            }
            else
                faction.OverallPowerLevel = FInt.FromParts( 3, 000 );
        }
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( faction.MustBeAwakenedByPlayer && !faction.HasBeenAwakenedByPlayer )
                return;

            // Make sure we always have our Mothership.
            GameEntity_Squad foundMothership = faction.GetFirstMatching( MOTHERSHIP_NAME, false, false );

            if ( foundMothership != null )
                Mothership = foundMothership;
            else
                Mothership = null;

            // Find our nodes.
            DysonNodes = new ArcenSparseLookup<Planet, GameEntity_Squad[]>();
            World_AIW2.Instance.DoForEntities( DYSON_NODE_NAME, node =>
            {
                if ( !DysonNodes.GetHasKey( node.Planet ) )
                    DysonNodes.AddPair( node.Planet, new GameEntity_Squad[7] );
                DysonNodes[node.Planet][node.CurrentMarkLevel - 1] = node;


                return DelReturn.Continue;
            } );
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( faction.MustBeAwakenedByPlayer && !faction.HasBeenAwakenedByPlayer )
                return;

            Initialize( faction, Context );

            GiveJournalsAsNeeded( faction, Context );

            UpdateAllegiance( faction, Context );

            GenerateResource( faction, Context );

            HandleMovedToNewPlanetLogicIfNeeded();

            HandleTrust( faction );

            HandleMineConsumption( faction, Context );

            GetReadyToMoveOnIfAble( faction, Context );

            HandleAIResponse( faction, Context );
        }

        private void Initialize( Faction faction, ArcenSimContext Context )
        {
            if ( MothershipData == null )
                MothershipData = faction.GetMothershipData();

            if ( !HeadersAppliedToOldJournals )
            {
                // For each Journal entry we have stored, reapply their header.
                for ( int x = 0; x < MothershipData.JournalEntries.GetPairCount(); x++ )
                {
                    ArcenSparseLookupPair<string, string> pair = MothershipData.JournalEntries.GetPairByIndex( x );
                    string entry = pair.Key;
                    string header = pair.Value;

                    // Get our journal entry.
                    JournalEntry journal = JournalEntryTable.Instance.GetRowByName( entry );

                    // Update our journal's text to include our new header if needed.
                    if ( !journal.FullText.Contains( header ) )
                        journal.FullText = header + journal.FullText;
                }

                HeadersAppliedToOldJournals = true;
            }

            if ( ExoData == null )
            {
                ExoData = faction.GetExoDataExt();
                if ( ExoData.StrengthRequiredForNextExo == 0 )
                {
                    ExoData.FactionIndexOfOriginFaction = faction.FactionIndex;
                    ExoData.FactionIndexOfExoSpawnFaction = BadgerFactionUtilityMethods.GetRandomAIFaction( Context ).FactionIndex;
                    ExoData.CurrentExoStrength = FInt.Zero;
                    ExoData.NumExosSoFar = 0;
                    ExoData.PercentToStartWarning = 75;
                }
            }

            if ( WaveData == null )
            {
                WaveData = faction.GetAntiMinorFactionWaveDataExt();
            }

            if ( Mothership == null )
            {
                // Mothership is dead.
                // If timer has not yet been set, set it based on intensity.
                if ( MothershipData.SecondsUntilRespawn < 0 )
                    MothershipData.SecondsUntilRespawn = 630 - faction.Ex_MinorFactionCommon_GetPrimitives().Intensity * 30;
                // Decrease timer.
                MothershipData.SecondsUntilRespawn = Math.Max( 0, MothershipData.SecondsUntilRespawn - 1 );
                // If ready, respawn Mothership.
                if ( MothershipData.SecondsUntilRespawn == 0 || World_AIW2.Instance.GameSecond < 60 )
                {
                    // Find our Ancient Node, or create a new one if needed.
                    GameEntity_Squad ancientNode = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType(typeof(DysonSuppressors)).GetFirstMatching(DYSON_ANCIENT_NODE_NAME, true, true);
                    if ( ancientNode == null )
                        ancientNode = SpawnAncientNode( faction, Context );
                    SpawnMothership( ancientNode, faction, Context );
                }
                return;
            }
        }
        private void GiveJournalsAsNeeded( Faction faction, ArcenSimContext Context )
        {
            MothershipLoreJournals( faction, Context );
            GameGuides( faction, Context );
        }
        private void MothershipLoreJournals( Faction faction, ArcenSimContext Context )
        {
            if ( Mothership == null )
                return;
            HandleMothershipSeen( faction, Context );
            HandleMothershipNear( faction, Context );
            HandleMothershipOnPlayerPlanet( faction, Context );
        }
        private void HandleMothershipSeen( Faction faction, ArcenSimContext Context )
        {
            if ( MothershipData.JournalEntries.GetHasKey( "MothershipSeen" ) )
                return;
            if ( Mothership.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
            {
                // Get our journal entry.
                JournalEntry journal = JournalEntryTable.Instance.GetRowByName( "MothershipSeen" );
                string header = GetJournalHeading( "Admiral Alan Edwards", "Moderate" );

                // Update our journal's text to include our new header.
                journal.FullText = header + journal.FullText;

                // Send our journal.
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "MothershipSeen", string.Empty, faction, Mothership.TypeData, Mothership.Planet, Context );

                // Add our new entry to be managed by our MothershipData on load.
                MothershipData.JournalEntries.AddPair( "MothershipSeen", header );
            }
        }
        private void HandleMothershipNear( Faction faction, ArcenSimContext Context )
        {
            if ( MothershipData.JournalEntries.GetHasKey( "MothershipNear" ) )
                return;
            if ( BadgerFactionUtilityMethods.GetHopsToPlayerPlanet( Mothership.Planet, Context ) == 1 )
            {
                // Get our journal entry.
                JournalEntry journal = JournalEntryTable.Instance.GetRowByName( "MothershipNear" );
                string header = GetJournalHeading( "Admiral Alan Edwards", "High" );

                // Update our journal's text to include our new header.
                journal.FullText = header + journal.FullText;

                // Send our journal.
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "MothershipNear", string.Empty, faction, Mothership.TypeData, Mothership.Planet, Context );

                // Add our new entry to be managed by our MothershipData on load.
                MothershipData.JournalEntries.AddPair( "MothershipNear", header );
            }
        }
        private void GameGuides( Faction faction, ArcenSimContext Context )
        {
            if ( World_AIW2.Instance.GameSecond > Context.RandomToUse.Next( 600, 1800 ) && !MothershipData.JournalEntries.GetHasKey( "MothershipTrust" ) )
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "MothershipTrust", string.Empty, Context );

                MothershipData.JournalEntries.AddPair( "MothershipTrust", string.Empty );
            }

            if ( World_AIW2.Instance.GameSecond > Context.RandomToUse.Next( 1200, 2400 ) && !MothershipData.JournalEntries.GetHasKey( "MothershipSubfactions" ) )
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "MothershipSubfactions", string.Empty, Context );

                MothershipData.JournalEntries.AddPair( "MothershipSubfactions", string.Empty );
            }
        }
        private void HandleMothershipOnPlayerPlanet( Faction faction, ArcenSimContext Context )
        {
            if ( MothershipData.JournalEntries.GetHasKey( "MothershipOnPlayerPlanet" ) )
                return;
            if ( Mothership.Planet.GetIsControlledByFactionType( FactionType.Player ) && MothershipData.Trust.GetTrust( Mothership.Planet ) > -1000 )
            {
                // Get our journal entry.
                JournalEntry journal = JournalEntryTable.Instance.GetRowByName( "MothershipOnPlayerPlanet" );
                string header = GetJournalHeading( "Admiral Alan Edwards", "Extreme" );

                // Update our journal's text to include our new header.
                journal.FullText = header + journal.FullText;

                // Send our journal.
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "MothershipOnPlayerPlanet", string.Empty, faction, Mothership.TypeData, Mothership.Planet, Context );

                // Add our new entry to be managed by our MothershipData on load.
                MothershipData.JournalEntries.AddPair( "MothershipOnPlayerPlanet", header );
            }
        }
        private void UpdateAllegiance( Faction faction, ArcenSimContext Context )
        {
            if ( Mothership == null )
                return;
            if ( Hacking_MothershipPacification.IsActive )
            {
                allyThisFactionToHumans( faction );
                Hacking_MothershipPacification.IsActive = false;
            }
            else if ( Hacking_OverrideMothershipPacification.IsActive )
            {
                enemyThisFactionToAll( faction );
                Hacking_OverrideMothershipPacification.IsActive = false;
            }
            else if ( Mothership.GetSecondsSinceEnteringThisPlanet() > 10 &&
                ((Mothership.Planet.GetIsControlledByFactionType( FactionType.Player ) && MothershipData.Trust.GetTrust( Mothership.Planet ) < -2000) ||
                (!Mothership.Planet.GetIsControlledByFactionType( FactionType.Player ) && MothershipData.Trust.GetTrust( Mothership.Planet ) < 1000)) )
                enemyThisFactionToAll( faction );
            else
                allyThisFactionToHumans( faction );

            // Always be allied to other Zenith
            if ( BaseDysonSubfaction.FactionsToAllyTo == null )
                return;
            for ( int i = 0; i < BaseDysonSubfaction.FactionsToAllyTo.Count; i++ )
            {
                Faction otherFaction = BaseDysonSubfaction.FactionsToAllyTo[i];
                if ( faction == otherFaction )
                    continue;
                if ( otherFaction.GetDisplayName().ToLower().Contains( "dyson" ) )
                {
                    faction.MakeFriendlyTo( otherFaction );
                    otherFaction.MakeFriendlyTo( faction );
                }
            }
        }
        private void GenerateResource( Faction faction, ArcenSimContext Context )
        {
            // Get our subfactions.
            Faction protectorSphereFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonProtectors ) );
            Faction suppressorSphereFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonSuppressors ) );

            // Generate passive experience for each Proto Sphere based on nodes in proximity.
            ArcenSparseLookup<Planet, int> incomePerPlanet = new ArcenSparseLookup<Planet, int>();
            World_AIW2.Instance.DoForPlanets( false, mainPlanet =>
            {
                DysonProtoSphereData sphereData = mainPlanet.GetProtoSphereData();

                CalculateNodeIncome( mainPlanet, incomePerPlanet );

                if ( sphereData.Type != DysonProtoSphereData.ProtoSphereType.Other && sphereData.Level < 7 )
                    sphereData.Resources += incomePerPlanet[mainPlanet];

                DysonProtoSphereData protoSphereData = mainPlanet.GetProtoSphereData();
                Faction sphereFaction = null;
                if ( protoSphereData.Type == DysonProtoSphereData.ProtoSphereType.Protecter )
                    sphereFaction = protectorSphereFaction;
                else if ( protoSphereData.Type == DysonProtoSphereData.ProtoSphereType.Suppressor )
                    sphereFaction = suppressorSphereFaction;

                // Reset ownership if we own it.
                if ( mainPlanet.UnderInfluenceOfFactionIndex == faction.FactionIndex || mainPlanet.UnderInfluenceOfFactionIndex == protectorSphereFaction.FactionIndex ||
                mainPlanet.UnderInfluenceOfFactionIndex == suppressorSphereFaction.FactionIndex )
                    mainPlanet.UnderInfluenceOfFactionIndex = -1;

                HandleFriendlyNPCPlanet( sphereData, mainPlanet );

                if ( sphereFaction == null )
                    return DelReturn.Continue;

                // Deleveling logic. Make sure aren't a different level or type that we should be.
                (sphereFaction.Implementation as BaseDysonSubfaction).FixProtoSphereLevelIfNeeded( sphereFaction, mainPlanet );

                // Node Logic.
                (sphereFaction.Implementation as BaseDysonSubfaction).HandleDysonNodeLogic( sphereFaction, mainPlanet, Context );

                // Leveling Logic.
                if ( protoSphereData.Level < 7 && protoSphereData.Resources > ProtoSphereCosts.Resources( protoSphereData.Level, faction ) )
                    (sphereFaction.Implementation as BaseDysonSubfaction).UpgradeProtoSphere( sphereFaction, mainPlanet, Context );

                // Ownership logic.
                if ( protoSphereData.Level > 0 && !mainPlanet.GetControllingOrInfluencingFaction().GetIsFriendlyTowards( sphereFaction ) )
                    mainPlanet.UnderInfluenceOfFactionIndex = sphereFaction.FactionIndex;

                return DelReturn.Continue;
            } );

            ApplyCalculatedIncome( incomePerPlanet, faction );

            LevelUpMothershipIfAble( faction, Context );
        }
        private void CalculateNodeIncome( Planet planet, ArcenSparseLookup<Planet, int> incomePerPlanet )
        {
            if ( planet.GetProtoSphereData().Level > 0 || (Mothership != null && Mothership.Planet.Index == planet.Index) )
            {
                incomePerPlanet.AddPair( planet, planet.GetProtoSphereData().Level );
                planet.DoForLinkedNeighborsAndSelf( false, workingPlanet =>
                {
                    if ( DysonNodes.GetHasKey( workingPlanet ) )
                        for ( int x = 0; x < DysonNodes[workingPlanet].Length; x++ )
                            if ( DysonNodes[workingPlanet][x] != null )
                                incomePerPlanet[planet] += x + 1;
                    return DelReturn.Continue;
                } );
            }
        }
        private void HandleFriendlyNPCPlanet( DysonProtoSphereData protoSphereData, Planet planet )
        {
            if ( BaseDysonSubfaction.FactionsToAllyTo == null )
                return;
            bool hasFriendlyFaction = false;
            if ( BaseDysonSubfaction.FactionsToAllyTo.Contains( planet.GetControllingOrInfluencingFaction() ) )
                hasFriendlyFaction = true; // Zenith owned planets.
            if ( !hasFriendlyFaction )
            {
                List<GameEntity_Squad> dysonSpheres = SpecialFaction_DysonSphere.DysonSpheres_AllFactions;
                if ( dysonSpheres != null && dysonSpheres.Count > 0 )
                    for ( int y = 0; y < dysonSpheres.Count; y++ )
                        if ( dysonSpheres[y].Planet.Index == planet.Index )
                        {
                            hasFriendlyFaction = true; // Planet with a Dyson Sphere on it.
                            break;
                        }
            }
            if ( hasFriendlyFaction )
            {
                protoSphereData.Type = DysonProtoSphereData.ProtoSphereType.Other;
                protoSphereData.Level = 7;
            }
        }
        private void ApplyCalculatedIncome( ArcenSparseLookup<Planet, int> incomePerPlanet, Faction faction )
        {
            for ( int x = 0; x < incomePerPlanet.GetPairCount(); x++ )
            {
                ArcenSparseLookupPair<Planet, int> pair = incomePerPlanet.GetPairByIndex( x );
                Planet planet = pair.Key;
                int income = pair.Value;
                DysonProtoSphereData sphereData = planet.GetProtoSphereData();
                if ( sphereData.Level > 0 && sphereData.Level < 7 )
                    sphereData.Resources += income;

                if ( Mothership != null && Mothership.Planet == planet )
                {
                    // Increment Mothership resources based on mine and node count.
                    int mothershipIncomeFromMines = MothershipData.Mines * (faction.Ex_MinorFactionCommon_GetPrimitives().Intensity / 2);
                    MothershipData.Resources += 1 + mothershipIncomeFromMines + income;
                    MothershipData.MetalGainedOrLostLastSecond = 1 + mothershipIncomeFromMines + income;
                }
            }
        }
        private void LevelUpMothershipIfAble( Faction faction, ArcenSimContext Context )
        {
            if ( Mothership == null )
                return;
            if ( MothershipData.Level < 7 && DysonNodes.GetHasKey( Mothership.Planet ) &&
                MothershipData.Resources >= PrecursorCosts.Resources( MothershipData.Level, faction ) &&
                MothershipData.Mines >= PrecursorCosts.Mines( MothershipData.Level, faction ) )
            {
                // Level on up.
                GameEntityTypeData newMothershipData = GameEntityTypeDataTable.Instance.GetRowByName( MOTHERSHIP_NAME + (MothershipData.Level + 1) );
                Mothership = Mothership.TransformInto( Context, newMothershipData, 1 );
                MothershipData.Level++;
                MothershipData.Resources = 0;
                World_AIW2.Instance.QueueChatMessageOrCommand( "The Dyson Mothership on " + Mothership.Planet.Name + " has leveled up to level " + MothershipData.Level + ".", ChatType.LogToCentralChat, Context );
            }
        }
        private void HandleMovedToNewPlanetLogicIfNeeded()
        {
            if ( Mothership == null )
                return;

            if ( Mothership.GetSecondsSinceEnteringThisPlanet() < 2 )
            {
                // Reset our hull check.
                MothershipData.HullWhenEnteredPlanet = Mothership.GetCurrentHullPoints();

                // Reset some movement boolean(s).
                MothershipData.ReadyToMoveOn = false;
            }
            // If we recently entered a new planet, regenerate our shields.
            if ( Mothership.GetSecondsSinceEnteringThisPlanet() < 30 )
                Mothership.TakeShieldRepair( Mothership.CurrentMarkLevel * 250000 );
        }
        private void HandleTrust( Faction faction )
        {
            if ( Mothership == null )
                return;

            // Reset description flags.
            MothershipData.IsGainingTrust = false;
            MothershipData.IsLosingTrust = false;

            // Trust.
            // Increase so long as the following are true:
            // Hull Has Not Decreased by more than 10% since entering
            // AND 
            // Shields Are Above 90%
            // AND
            // Human forces outnumber Hostile
            // Decrease by 1 so long as the following are true:
            // Not Player Owned
            // OR
            // Hull Has Decreased (By 50% of current at time of entry) Since Entering OR Shields Are Below 60%
            // Absolute trust cannot go below 1k if Node on planet.
            // Trust increases faster based on adjacent nodes.
            short baseChange = 1;
            short nodeBonus = 0;
            Mothership.Planet.DoForLinkedNeighborsAndSelf( false, planet =>
            {
                if ( DysonNodes.GetHasKey( planet ) )
                {
                    if ( MothershipData.Trust.GetTrust( planet ) > 0 )
                        nodeBonus++;
                    else
                        nodeBonus--;
                }

                return DelReturn.Continue;
            } );

            bool alliedPlanet = Mothership.Planet.GetControllingOrInfluencingFaction().GetIsFriendlyTowards( faction );
            int trust = MothershipData.Trust.GetTrust( Mothership.Planet );
            int hullForTrustGain = MothershipData.HullWhenEnteredPlanet - Mothership.GetMaxHullPoints() / 10;
            int hullForTrustLoss = MothershipData.HullWhenEnteredPlanet / 2;
            int shieldForTrustLoss = (Mothership.TypeData.GetForMark( Mothership.CurrentMarkLevel ).BaseShieldPoints / 10) * 6;
            int shieldForTrustGain = (Mothership.TypeData.GetForMark( Mothership.CurrentMarkLevel ).BaseShieldPoints / 10) * 9;

            int humanStrength = 0, hostileStrength = 1, mothershipStrength = 0;

            World_AIW2.Instance.DoForFactions( playerFaction =>
            {
                if ( playerFaction.Type != FactionType.Player )
                    return DelReturn.Continue;

                humanStrength += Mothership.Planet.GetPlanetFactionForFaction( playerFaction ).DataByStance[FactionStance.Self].TotalStrength;
                if ( faction.GetIsHostileTowards( playerFaction ) ) // Factor out humans so they aren't considered hostile.
                    hostileStrength -= Mothership.Planet.GetPlanetFactionForFaction( playerFaction ).DataByStance[FactionStance.Self].TotalStrength;
                return DelReturn.Continue;
            } );
            hostileStrength += Mothership.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength;
            mothershipStrength = Mothership.PlanetFaction.DataByStance[FactionStance.Self].TotalStrength;

            if ( MothershipUndamaged( hullForTrustGain, shieldForTrustGain ) && HumanStrengthAdvantage( humanStrength, hostileStrength ) )
            {
                Mothership.Planet.DoForLinkedNeighborsAndSelf( false, planet =>
                {
                    if ( trust < MothershipData.Trust.MaxTrust( Mothership.Planet ) )
                    {
                        short potentialChange = (short)(baseChange + nodeBonus);
                        short change = Math.Max( baseChange, potentialChange );

                        MothershipData.Trust.AddOrSubtractTrust( planet, change );
                        MothershipData.IsGainingTrust = true;
                    }
                    return DelReturn.Continue;
                } );

            }
            else if ( MothershipDamaged( hullForTrustLoss, shieldForTrustLoss ) || !HumanStrengthAdvantage( humanStrength, hostileStrength ) )
            {
                Mothership.Planet.DoForLinkedNeighborsAndSelf( false, planet =>
                {
                    if ( trust > MothershipData.Trust.MinTrust( Mothership.Planet ) )
                    {
                        short potentialChange = (short)((-baseChange) + nodeBonus);
                        short change = Math.Min( baseChange, potentialChange );

                        MothershipData.Trust.AddOrSubtractTrust( planet, change );
                        MothershipData.IsLosingTrust = true;
                    }
                    return DelReturn.Continue;
                } );
            }
        }
        private bool MothershipDamaged( int hullForTrustLoss, int shieldForTrustLoss )
        {
            return Mothership.GetCurrentHullPoints() < hullForTrustLoss || Mothership.GetCurrentShieldPoints() < shieldForTrustLoss;
        }
        private bool HumanStrengthAdvantage( int humanStrength, int hostileStrength )
        {
            return humanStrength > hostileStrength;
        }
        private bool MothershipUndamaged( int hullForTrustGain, int shieldForTrustGain )
        {
            return Mothership.GetCurrentHullPoints() >= hullForTrustGain && Mothership.GetCurrentShieldPoints() >= shieldForTrustGain;
        }
        private void HandleMineConsumption( Faction faction, ArcenSimContext Context )
        {
            if ( Mothership == null )
                return;

            // Try and eat any nearby mines.
            MothershipData.IsNearMine = false;
            int mineCount = 0;
            Mothership.Planet.DoForEntities( EntityRollupType.MetalProducers, delegate ( GameEntity_Squad mine )
            {
                if ( mine.TypeData.InternalName != "MetalGenerator" )
                    return DelReturn.Continue;
                mineCount++;
                return DelReturn.Continue;
            } );

            // Mine consumption logic.
            Mothership.Planet.DoForEntities( EntityRollupType.MetalProducers, delegate ( GameEntity_Squad mine )
            {
                if ( mine.TypeData.InternalName != "MetalGenerator" )
                    return DelReturn.Continue;

                if ( MothershipIsNearMine( mine ) )
                {
                    // See if we should consume this mine.
                    // Chance is base 20%, scales with Trust.
                    int baseChance = 10;
                    int trustMod = -MothershipData.Trust.GetTrust( Mothership.Planet ) / 100;

                    // Don't let her eat all of the player's economy.
                    int allyMod = 0;
                    if ( Mothership.Planet.GetControllingOrInfluencingFaction().GetIsFriendlyTowards( faction ) && mineCount <= 5 )
                    {
                        GameEntity_Squad king = BadgerFactionUtilityMethods.findKing( Mothership.Planet.GetControllingOrInfluencingFaction() );
                        if ( king.Planet.Index == Mothership.Planet.Index )
                            allyMod = -20;
                        else
                            allyMod = -5;
                    }
                    int roll = Context.RandomToUse.Next( 0, 100 );
                    bool consume = roll < baseChance + trustMod + allyMod;
                    if ( consume )
                    {
                        mine.Despawn( Context, true, InstancedRendererDeactivationReason.GettingIntoTransport );
                        MothershipData.Mines++;
                        World_AIW2.Instance.QueueChatMessageOrCommand( "A Dyson Mothership on " + Mothership.Planet.Name + " has just consumed a Mine.", ChatType.LogToCentralChat, Context );
                        Mothership.TakeHullRepair( Mothership.TypeData.GetForMark( Mothership.CurrentMarkLevel ).BaseHullPoints / 20 );
                        return DelReturn.Break;
                    }
                    else
                    {
                        Mothership.TakeHullRepair( Mothership.TypeData.GetForMark( Mothership.CurrentMarkLevel ).BaseHullPoints / 1000 );
                        MothershipData.Resources += 1;
                        // Update the description.
                        MothershipData.IsNearMine = true;
                    }
                }

                return DelReturn.Continue;
            } );
        }
        private bool MothershipIsNearMine( GameEntity_Squad mine )
        {
            return Mothership.WorldLocation.GetDistanceTo( mine.WorldLocation, true ) <
                     Math.Max( 2500, Mothership.TypeData.GetForMark( Mothership.CurrentMarkLevel ).Radius + 500 );
        }
        private void GetReadyToMoveOnIfAble( Faction faction, ArcenSimContext Context )
        {
            if ( Mothership == null )
                return;
            // If we've been at this planet too long, move on.
            int timeToStay = Math.Max( 60, Math.Min( 300, 300 - Math.Abs( MothershipData.Trust.GetTrust( Mothership.Planet ) ) / 10 ) );
            if ( World_AIW2.Instance.GameSecond - Mothership.GameSecondEnteredThisPlanet > timeToStay )
            {
                if ( !MothershipData.ReadyToMoveOn )
                    MoveToNewPlanet( faction, Context );
            }
        }

        // Spawn an Ancient Node somewhere in the galaxy.
        private GameEntity_Squad SpawnAncientNode( Faction faction, ArcenSimContext Context )
        {
            List<Planet> potentialPlanets = new List<Planet>();
            byte bestMark = 7;
            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                if ( planet.MarkLevelForAIOnly.Ordinal > bestMark )
                    return DelReturn.Continue;

                bool isValid = true;
                World_AIW2.Instance.DoForEntities( EntityRollupType.KingUnitsOnly, ( GameEntity_Squad king ) =>
                {
                    if ( planet.GetHopsTo( king.Planet ) < 3 )
                        isValid = false;

                    return DelReturn.Continue;
                } );

                if ( isValid && BadgerFactionUtilityMethods.GetHopsToPlayerPlanet( planet, Context ) > 1 )
                {
                    if ( planet.MarkLevelForAIOnly.Ordinal < bestMark )
                    {
                        potentialPlanets = new List<Planet>();
                        bestMark = planet.MarkLevelForAIOnly.Ordinal;
                    }
                    potentialPlanets.Add( planet );
                }
                return DelReturn.Continue;
            } );

            Planet spawnPlanet = potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];

            // If not the start of the game, notify the player.
            //if ( World_AIW2.Instance.GameSecond > 60 )
            World_AIW2.Instance.QueueChatMessageOrCommand( $"An Ancient Dyson Node has spawnt on {spawnPlanet.Name}.", ChatType.LogToCentralChat, Context );

            // Spawn in our Ancient Node.
            GameEntityTypeData mothershipEntityData = GameEntityTypeDataTable.Instance.GetRowByName( DYSON_ANCIENT_NODE_NAME );
            Faction spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonSuppressors ) );
            return spawnPlanet.Mapgen_SeedEntity( Context, spawnFaction, mothershipEntityData, PlanetSeedingZone.OuterSystem );
        }

        // Spawn a Mothership on our Ancient Node.
        public void SpawnMothership( GameEntity_Squad ancientNode, Faction faction, ArcenSimContext Context )
        {
            // Spawn in our Mothership.
            GameEntityTypeData mothershipEntityData = GameEntityTypeDataTable.Instance.GetRowByName( MOTHERSHIP_NAME + "1" );
            PlanetFaction pFaction = ancientNode.Planet.GetPlanetFactionForFaction( faction );
            Mothership = GameEntity_Squad.CreateNew( pFaction, mothershipEntityData, 1, pFaction.FleetUsedAtPlanet, 0, ancientNode.WorldLocation, Context );

            // Reset our Mothership data.
            MothershipData = new DysonMothershipData
            {
                LastGameSecondSpawnt = World_AIW2.Instance.GameSecond
            };
            faction.SetMothershipData( MothershipData );
            MothershipData = faction.GetMothershipData();

            if ( ancientNode.Planet.IntelLevel != PlanetIntelLevel.Unexplored )
                World_AIW2.Instance.QueueChatMessageOrCommand( $"A Dyson Mothership has spawnt on {ancientNode.Planet.Name}.", ChatType.LogToCentralChat, Context );
        }
        private void MoveToNewPlanet( Faction faction, ArcenSimContext Context )
        {
            if ( Mothership == null )
                return;
            // Node logic.
            // Upon leaving a planet, if the absolute trust of the planet is over 1k, drop down a Node on the planet if there are free Node slots left.
            if ( Math.Abs( MothershipData.Trust.GetTrust( Mothership.Planet ) ) >= 1000 )
            {
                for ( int x = 0; x < MothershipData.Level; x++ )
                {
                    if ( DysonNodes[Mothership.Planet] == null || DysonNodes[Mothership.Planet][x] == null )
                    {
                        // Found a free slot. Spawn a new node.
                        Faction spawnFaction = null;
                        if ( MothershipData.Trust.GetTrust( Mothership.Planet ) >= 1000 )
                        {
                            spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonProtectors ) );
                        }
                        else if ( MothershipData.Trust.GetTrust( Mothership.Planet ) <= -1000 )
                        {
                            spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonSuppressors ) );
                        }
                        // Support for non-protector, non-suppresor dyson factions.
                        if ( spawnFaction == null && Mothership.Planet.GetProtoSphereData().Type == DysonProtoSphereData.ProtoSphereType.Other )
                        {
                            spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonSuppressors ) );
                        }
                        if ( spawnFaction != null )
                        {
                            (spawnFaction.Implementation as BaseDysonSubfaction).CreateDysonNode( spawnFaction, Mothership.Planet, x + 1, Context );
                            break;
                        }
                    }
                }
            }

            // Gift logic
            // If the following are true, roll to see if we should gift a mine to the planet.
            // Planet Owned by Friendly Faction
            // AND
            // Planet Trust > 1000
            // AND
            // More Mines Consumed than Required for leveling up
            // AND
            // Roll at least a 50 + (Trust/100) + (Mines - MinesNeeded)
            int baseChance = 50;
            int trustMod = MothershipData.Trust.GetTrust( Mothership.Planet ) / 100;
            int mineMod = MothershipData.Mines - PrecursorCosts.Mines( MothershipData.Level, faction );
            int roll = Context.RandomToUse.Next( 0, 100 );
            bool rollPassed = roll < baseChance + trustMod + mineMod;
            if ( Mothership.Planet.GetControllingFaction().GetIsFriendlyTowards( faction ) && MothershipData.Trust.GetTrust( Mothership.Planet ) > 1000 &&
                MothershipData.Mines > PrecursorCosts.Mines( MothershipData.Level, faction ) && rollPassed )
            {
                Mothership.Planet.Mapgen_SeedEntity( Context, World_AIW2.Instance.GetNeutralFaction(),
                    GameEntityTypeDataTable.Instance.GetRowByName( "MetalGenerator" ), PlanetSeedingZone.MostAnywhere );
                MothershipData.Mines--;
                World_AIW2.Instance.QueueChatMessageOrCommand( $"A Dyson Mothership has left behind a fully functional mine on {Mothership.Planet.Name}.", ChatType.LogToCentralChat, Context );
            }

            // Proto-Sphere logic. Create a Proto-Sphere if:
            // Mothership Level 7
            // Mothership Resources > Protosphere Cost
            // Trust > 2000 or < -2000
            // Its a planet we planned to build on.
            if ( MothershipData.Level >= 7 && MothershipData.Resources >= ProtoSphereCosts.BuildCost( faction ) &&
                MothershipData.PlanetToBuildOn != null && Mothership.Planet.Index == MothershipData.PlanetToBuildOn.Index )
            {
                DysonProtoSphereData protoSphereData = Mothership.Planet.GetProtoSphereData();
                if ( protoSphereData.Level <= 0 )
                {
                    Faction spawnFaction = null;
                    if ( MothershipData.Trust.GetTrust( Mothership.Planet ) >= 2000 )
                        spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonProtectors ) );
                    else if ( MothershipData.Trust.GetTrust( Mothership.Planet ) <= -2000 )
                        spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonSuppressors ) );
                    if ( spawnFaction != null )
                    {
                        (spawnFaction.Implementation as BaseDysonSubfaction).CreateProtoSphere( spawnFaction, Mothership.Planet, Context );
                        MothershipData.Resources = 0;
                        MothershipData.PlanetToBuildOn = null;
                    }
                }
            }

            MothershipData.ReadyToMoveOn = true;
        }

        private void HandleAIResponse( Faction faction, ArcenSimContext Context )
        {
            // Increase strength for each proto sphere and dyson node that exists.
            int strMod = MothershipData.Level, reqStrength = 0;
            bool hasSphere = false;
            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                if ( planet.GetProtoSphereData().Level > 0 && planet.GetControllingOrInfluencingFaction().Implementation is DysonSuppressors )
                    hasSphere = true;
                strMod += planet.GetProtoSphereData().Level;
                reqStrength += planet.GetProtoSphereData().Level * 2500;
                if ( DysonNodes[planet] != null )
                    for ( int x = 0; x < DysonNodes[planet].Length; x++ )
                        if ( DysonNodes[planet][x] != null )
                        {
                            strMod += x + 1;
                            reqStrength += (x + 1) * 1000;
                        }
                return DelReturn.Continue;
            } );

            WaveData.strengthForNextWave = FInt.Zero + reqStrength + (World_AIW2.Instance.GameSecond * 10);

            // Handle the sending of waves towards Proto Sphere planets.
            if ( hasSphere )
            {
                WaveData.currentWaveBudget += strMod;
                if ( WaveData.currentWaveBudget > WaveData.strengthForNextWave )
                {
                    Faction suppressorSphereFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonSuppressors ) );
                    AntiMinorFactionWaveData.QueueWave( suppressorSphereFaction, Context, WaveData.currentWaveBudget.GetNearestIntPreferringHigher(), false );
                    WaveData.currentWaveBudget = FInt.Zero;
                    MothershipData.LastWaveGameSecond = World_AIW2.Instance.GameSecond;
                }
            }
        }
        // Upgrade existing Noded planets, or expand our Node Network to new planets.
        private Planet GetNodeTargetPlanet( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            Planet endPlanet = null;
            // We have enough mines. Huzzah. Work on building up our Node network for now.
            // Prefer maxing out nodes up to our current limit, before expanding.
            int maxNodeSlot = MothershipData.Level;
            // Get two lists of planets.
            // A list of close planets that already have a Node, and have an open slot.
            List<Planet> planetsToUpgrade = new List<Planet>();
            // A list of close planets that are adjacent to existing Node planets, prefering ones with a higher ratio of adjacent planets being Noded.
            // Once we get a valid planet in our above list however, stop populating this list. Its simply here for backup, and created at the same time for performance's sake.
            List<Planet> planetsToExpandTo = new List<Planet>();

            int upgradeHops = 99, expandHops = 99;

            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                // Skip if no nodes.
                if ( DysonNodes[planet] == null )
                    return DelReturn.Continue;

                int hops = Mothership.Planet.GetHopsTo( planet );

                // Search through node slots that are valid to be upgraded, so long as this planet isn't farther away than our current list.
                if ( hops <= upgradeHops )
                    for ( int x = 0; x < maxNodeSlot; x++ )
                    {
                        // If we have a slot free, add it to our upgrade list.
                        if ( DysonNodes[planet][x] == null )
                        {
                            // If this is the closest planet so far, clear all older planets.
                            if ( hops < upgradeHops )
                            {
                                planetsToUpgrade = new List<Planet>();
                                upgradeHops = hops;
                            }
                            planetsToUpgrade.Add( planet );
                            break;
                        }
                    }

                // If we have some planets to upgrade, don't search for expansion planets.
                if ( planetsToUpgrade.Count > 0 )
                    return DelReturn.Continue;

                // Add any non-noded adjacent planets to our expand list.
                planet.DoForLinkedNeighbors( false, adjPlanet =>
                {
                    hops = Mothership.Planet.GetHopsTo( adjPlanet );

                    // Skip if further away than our current list.
                    if ( hops > expandHops )
                        return DelReturn.Continue;

                    if ( DysonNodes[adjPlanet] == null && !planetsToExpandTo.Contains( adjPlanet ) )
                    {
                        // If this is the closest planet so far, clear all older planets.
                        if ( hops < expandHops )
                        {
                            planetsToExpandTo = new List<Planet>();
                            expandHops = hops;
                        }
                        planetsToExpandTo.Add( adjPlanet );
                    }

                    return DelReturn.Continue;
                } );

                return DelReturn.Continue;
            } );

            if ( planetsToUpgrade.Count > 0 )
                endPlanet = planetsToUpgrade[Context.RandomToUse.Next( planetsToUpgrade.Count )];
            else if ( planetsToExpandTo.Count > 0 )
                endPlanet = planetsToExpandTo[Context.RandomToUse.Next( planetsToExpandTo.Count )];

            return endPlanet;
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( faction.MustBeAwakenedByPlayer && !faction.HasBeenAwakenedByPlayer )
                return;
            if ( Mothership == null )
                return;

            // If our Mothership is not currently moving to a new planet, see if it needs a new command.
            if ( Mothership.LongRangePlanningData.FinalDestinationPlanetIndex == -1
              || Mothership.LongRangePlanningData.FinalDestinationPlanetIndex == Mothership.Planet.Index )
            {
                HandleMothershipMovement( faction, Context );
            }

            // Make the AI come out to play on our Mothership's planet.
            BadgerFactionUtilityMethods.FlushUnitsFromReinforcementPointsOnAllRelevantPlanets( faction, Context );

            HandleDroneMovement( faction, Context );

            HerdMiningGolems( faction, Context );

            faction.ExecuteWormholeCommands( Context );
            faction.ExecuteMovementCommands( Context );
        }
        private void HandleMothershipMovement( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            // If we've taken heavy damage, panic and run away.
            if ( Mothership.GetCurrentShieldPoints() < 5000000 * (Mothership.CurrentMarkLevel * 1.5) && World_AIW2.Instance.GameSecond - Mothership.GameSecondEnteredThisPlanet > 30 )
            {
                // If we're nowhere near a trusted planet, go to the nearest wormhole.
                Planet nearestTrustedPlanet = MothershipData.Trust.GetNearbyTrustedPlanet( Mothership.Planet, Context );
                if ( Mothership.Planet.GetHopsTo( nearestTrustedPlanet ) > 3 )
                {
                    ArcenSparseLookupPair<Planet, int> closestPlanet = null;
                    Mothership.Planet.DoForLinkedNeighbors( false, delegate ( Planet workingPlanet )
                    {
                        int distance = Mothership.WorldLocation.GetDistanceTo( Mothership.Planet.GetWormholeTo( workingPlanet ).WorldLocation, true );
                        if ( closestPlanet == null || distance < closestPlanet.Value )
                            closestPlanet = new ArcenSparseLookupPair<Planet, int>
                            {
                                Key = workingPlanet,
                                Value = distance
                            };

                        return DelReturn.Continue;
                    } );
                    Mothership.QueueWormholeCommand( closestPlanet.Key, Context );
                    return;
                }
                // Otherwise, start pathing towards the nearest trusted planet.
                else
                {
                    Mothership.QueueWormholeCommand( nearestTrustedPlanet );
                    return;
                }
            }

            // Get a list of mines on the planet.
            List<ArcenPoint> validPoints = new List<ArcenPoint>();
            Mothership.Planet.DoForEntities( EntityRollupType.MetalProducers, mine =>
            {
                if ( mine.TypeData.GetHasTag( "MetalGenerator" ) )
                    if ( Mothership.GetDistanceTo_VeryCheapButExtremelyRough( mine.WorldLocation, true ) >
                    Mothership.TypeData.GetForMark( Mothership.CurrentMarkLevel ).Radius + 1000 )
                        validPoints.Add( mine.WorldLocation );

                return DelReturn.Continue;
            } );

            // If there are mines on the planet, wait until we have no orders to let the Mothership finish potentially consuming.
            if ( validPoints.Count > 0 && Mothership.Orders.QueuedOrders.Count > 0 && !Mothership.Orders.GetHasDecollisionMoveOrderAsFirst() )
                return;

            // After processing in Stage3, move on.
            if ( MothershipData.ReadyToMoveOn && Mothership.GetSecondsSinceEnteringThisPlanet() > 10 )
            {
                if ( Mothership.LongRangePlanningData.FinalDestinationPlanetIndex == -1 || Mothership.LongRangePlanningData.FinalDestinationPlanetIndex == Mothership.Planet.Index )
                    if ( Mothership.CurrentMarkLevel < 7 )
                        HandlePreSphereMovement( faction, Context );
                    else
                        HandlePostSphereMovement( faction, Context );
                return;
            }

            // Move to a random mine on the planet.
            if ( validPoints.Count > 0 )
            {
                ArcenPoint movePoint = validPoints[Context.RandomToUse.Next( validPoints.Count )];
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint], GameCommandSource.AnythingElse );
                command.RelatedEntityIDs.Add( Mothership.PrimaryKeyID );
                command.RelatedPoints.Add( movePoint );
                Context.QueueCommandForSendingAtEndOfContext( command );
                return;
            }

            // Move to a random point on the planet.
            if ( Mothership.Orders.QueuedOrders.Count == 0 )
            {
                ArcenPoint movePoint = Engine_AIW2.Instance.CombatCenter.GetRandomPointWithinDistance( Context.RandomToUse, 2500, 15000 );
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint], GameCommandSource.AnythingElse );
                command.RelatedEntityIDs.Add( Mothership.PrimaryKeyID );
                command.RelatedPoints.Add( movePoint );
                Context.QueueCommandForSendingAtEndOfContext( command );
                return;
            }
        }
        // Handle movement for level 6 and below. Focus on collecting mines and building up our Node system.
        private void HandlePreSphereMovement( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            Planet endPlanet = null;
            // We'll want to find an optimal planet to head to.
            // Value planets based on what we need at the time.
            bool needMoreMines = MothershipData.Level < 7 && MothershipData.Mines < PrecursorCosts.Mines( MothershipData.Level, faction ) * 1.5;
            if ( needMoreMines )
            {
                // Find a planet near us that has mines that we could eat.
                List<Planet> validPlanets = new List<Planet>();
                int minHops = 99, maxMines = 0;
                World_AIW2.Instance.DoForPlanets( false, planet =>
                {
                    if ( Mothership.Planet.Index == planet.Index )
                        return DelReturn.Continue;

                    int hops = planet.GetHopsTo( Mothership.Planet );
                    // Skip if its farther away than our current best hop, or we trust it too much.
                    if ( hops > minHops || MothershipData.Trust.GetTrust( planet ) > 1000 )
                        return DelReturn.Continue;

                    int mines = 0;
                    planet.DoForEntities( EntityRollupType.MetalProducers, mine =>
                    {
                        if ( mine.TypeData.GetHasTag( "MetalGenerator" ) )
                            mines++;
                        return DelReturn.Continue;
                    } );

                    // Skip if no mines.
                    if ( mines == 0 )
                        return DelReturn.Continue;

                    // If its closer than our current target(s), clear our list and update our hops and mines.
                    if ( hops < minHops )
                    {
                        validPlanets = new List<Planet>();
                        minHops = hops;
                        maxMines = mines;
                    }
                    // If its more mines than our current target(s), clear our list and update our mines.
                    else if ( mines > maxMines )
                    {
                        validPlanets = new List<Planet>();
                        maxMines = mines;
                    }

                    validPlanets.Add( planet );

                    return DelReturn.Continue;
                } );

                if ( validPlanets.Count > 0 )
                    endPlanet = validPlanets[Context.RandomToUse.Next( validPlanets.Count )];
                else
                    endPlanet = Mothership.Planet.GetRandomNeighbor( false, Context );
            }
            else
                endPlanet = GetNodeTargetPlanet( faction, Context );

            // If no planet, or already at our optimal planet, get a random neighbor planet instead.
            if ( endPlanet == null || endPlanet.Index == Mothership.Planet.Index )
                endPlanet = Mothership.Planet.GetRandomNeighbor( false, Context );

            Mothership.QueueWormholeCommand( endPlanet );
        }
        // Handle movement for level 7, security and expansion.
        private void HandlePostSphereMovement( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            Planet endPlanet = null;
            // If we have enough resources to build a sphere, move right there.
            if ( MothershipData.Resources >= ProtoSphereCosts.BuildCost( faction ) )
            {
                Planet focusPlanet = MothershipData.PlanetToBuildOn;
                if ( focusPlanet == null )
                {
                    // Find a planet with no sphere that we really hate or really like, and focus on it.
                    int bestValue = 1999;
                    World_AIW2.Instance.DoForPlanets( false, planet =>
                    {
                        int absTrust = Math.Abs( MothershipData.Trust.GetTrust( planet ) );
                        // Care about positive trust slightly more.
                        if ( MothershipData.Trust.GetTrust( planet ) >= 2000 )
                            absTrust += 100;
                        if ( absTrust > bestValue && planet.GetProtoSphereData().Level <= 0 )
                        {
                            focusPlanet = planet;
                            bestValue = absTrust;
                        }

                        return DelReturn.Continue;
                    } );
                    // Failed to find a valid planet. Expand out from a nearby trusted planet.
                    if ( focusPlanet == null )
                    {
                        Planet basePlanet = MothershipData.Trust.GetNearbyTrustedPlanet( Mothership.Planet, Context, 2000 );
                        if ( basePlanet.GetProtoSphereData().Level <= 0 )
                            focusPlanet = basePlanet;
                        else
                        {
                            int bestHop = 99;
                            World_AIW2.Instance.DoForPlanets( false, planet =>
                            {
                                if ( planet.GetProtoSphereData().Level > 0 )
                                    return DelReturn.Continue;

                                int hops = basePlanet.GetHopsTo( planet );
                                if ( focusPlanet == null )
                                {
                                    focusPlanet = planet;
                                    bestHop = hops;
                                }
                                else
                                {
                                    if ( hops < bestHop )
                                    {
                                        focusPlanet = planet;
                                        bestHop = hops;
                                    }
                                    else if ( hops == bestHop && Math.Abs( MothershipData.Trust.GetTrust( planet ) ) > Math.Abs( MothershipData.Trust.GetTrust( focusPlanet ) ) )
                                        focusPlanet = planet;
                                }

                                return DelReturn.Continue;
                            } );
                        }
                    }
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.Instance.GetRowByName( Commands.SetPlanetToBuildOn.ToString() ), GameCommandSource.AnythingElse );
                    command.RelatedString = focusPlanet.Name;
                    Context.QueueCommandForSendingAtEndOfContext( command );
                }
                endPlanet = focusPlanet;
            }
            // If we don't have enough resources, use our node logic.
            else
                endPlanet = GetNodeTargetPlanet( faction, Context );

            // If no planet, or already at our optimal planet, get a random neighbor planet instead.
            if ( endPlanet == null || endPlanet.Index == Mothership.Planet.Index )
                endPlanet = Mothership.Planet.GetRandomNeighbor( false, Context );

            Mothership.QueueWormholeCommand( endPlanet );
        }
        private void HandleDroneMovement( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            // Move any drones not on our Mothership's planet to its planet.
            // Figure out some info that we'll need before looping.
            int maxDistance = 0;
            GameEntity_Other wormhole = null;
            if ( Mothership.LongRangePlanningData.FinalDestinationPlanetIndex != -1 && Mothership.LongRangePlanningData.FinalDestinationPlanetIndex != Mothership.Planet.Index )
            {
                wormhole = Mothership.Planet.GetWormholeTo( Mothership.LongRangePlanningData.FinalDestinationPlanetIndex );
                if ( wormhole != null )
                    maxDistance = Math.Max( 5000, (Mothership.WorldLocation.GetDistanceTo( wormhole.WorldLocation, true ) / 3) * 2 );
            }
            bool beDefensive = false;

            // Get our strength, to factor out ourselves. We'll consider how well the player will do against other hostiles on the planet.
            int ourStrength = 0;
            Mothership.FleetMembership.Fleet.DoForMemberGroups( delegate ( Fleet.Membership mem )
            {
                if ( mem.TypeData.IsDrone )
                    ourStrength += mem.TypeData.GetForMark( Mothership.CurrentMarkLevel ).GetCalculatedStrengthPerSquadForFleetOrNull( mem ) * mem.EffectiveSquadCap;
                return DelReturn.Continue;
            } );

            World_AIW2.Instance.DoForFactions( playerFaction =>
            {
                if ( playerFaction.Type != FactionType.Player )
                    return DelReturn.Continue;

                if ( faction.GetIsHostileTowards( playerFaction ) )
                {
                    PlanetFaction humanPFaction = Mothership.Planet.GetPlanetFactionForFaction( playerFaction );
                    int humansAndAllies = humanPFaction.DataByStance[FactionStance.Self].TotalStrength + humanPFaction.DataByStance[FactionStance.Friendly].TotalStrength;
                    int humanHostiles = humanPFaction.DataByStance[FactionStance.Hostile].TotalStrength - Mothership.PlanetFaction.DataByStance[FactionStance.Self].TotalStrength;
                    if ( humansAndAllies > humanHostiles * 0.75 )
                        beDefensive = true;
                }

                return DelReturn.Continue;
            } );
            faction.DoForEntities( delegate ( GameEntity_Squad workingEntity )
            {
                if ( !workingEntity.TypeData.IsDrone )
                    return DelReturn.Continue;
                if ( workingEntity.Planet.Index != Mothership.Planet.Index )
                    workingEntity.QueueWormholeCommand( Mothership.Planet, Context );
                else if ( beDefensive )
                {
                    // Keep our drones near our mothership as requested.
                    if ( Mothership.GetCurrentShieldPoints() > Mothership.TypeData.GetForMark( Mothership.CurrentMarkLevel ).BaseShieldPoints * 0.75 && (workingEntity.GetDistanceTo_VeryCheapButExtremelyRough( Mothership.WorldLocation, true ) > 5000 ||
                    (workingEntity.LongRangePlanningData.DestinationPoint != ArcenPoint.ZeroZeroPoint && workingEntity.LongRangePlanningData.DestinationPoint.GetExtremelyRoughDistanceTo( Mothership.WorldLocation ) > 5000)) )
                        workingEntity.QueueMovementCommand( Mothership.WorldLocation );
                }
                return DelReturn.Continue;
            } );
        }
        private void HerdMiningGolems( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            Faction golemFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( SpecialFaction_ZenithMiners ) );
            if ( golemFaction == null )
                return;

            SpecialFaction_ZenithMiners golemImplementation = golemFaction.Implementation as SpecialFaction_ZenithMiners;

            if ( golemImplementation.GlobalData.Miners == null )
                return;

            for ( int x = 0; x < golemImplementation.GlobalData.Miners.Count; x++ )
            {
                GameEntity_Squad golem = golemImplementation.GlobalData.Miners[x];
                Planet planet = golem.Planet;

                if ( planet.GetProtoSphereData().Type != DysonProtoSphereData.ProtoSphereType.None || Mothership.Planet.Index == planet.Index )
                {
                    golem.QueueWormholeCommand( GetNearbyPlanetMothershipDoesNotLike( planet, Context ) );
                }
            }
        }
        private Planet GetNearbyPlanetMothershipDoesNotLike( Planet golemPlanet, ArcenLongTermIntermittentPlanningContext Context )
        {
            int minHops = 99;
            List<Planet> planets = new List<Planet>();
            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                int hops = golemPlanet.GetHopsTo( planet );
                if ( Mothership.Planet.Index == planet.Index )
                    return DelReturn.Continue;
                if ( Math.Abs( MothershipData.Trust.GetTrust( planet ) ) < 1000 && planet.GetProtoSphereData().Type == DysonProtoSphereData.ProtoSphereType.None )
                {
                    if ( hops < minHops )
                    {
                        minHops = hops;
                        planets = new List<Planet>();
                    }
                    if ( hops <= minHops )
                        planets.Add( planet );
                }

                return DelReturn.Continue;
            } );
            if ( planets.Count > 0 )
                return planets[Context.RandomToUse.Next( planets.Count )];
            else
                return golemPlanet.GetRandomNeighbor( false, Context );
        }
        public override void DoOnAnyDeathLogic( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenSimContext Context )
        {
            Faction faction = entity.PlanetFaction.Faction;
            if ( entity.TypeData.GetHasTag( MOTHERSHIP_NAME ) )
            {
                MothershipData.Level = 1;
                MothershipData.Resources = 0;
                for ( int x = 0; x < MothershipData.Mines; x++ )
                    entity.Planet.Mapgen_SeedEntity( Context, World_AIW2.Instance.GetNeutralFaction(),
                        GameEntityTypeDataTable.Instance.GetRowByName( "MetalGenerator" ), PlanetSeedingZone.OuterSystem );
                if ( MothershipData.Mines > 0 )
                    World_AIW2.Instance.QueueChatMessageOrCommand( "The Dyson Mothership on " + entity.Planet.Name + " has been destroyed. " + MothershipData.Mines + " mines have appeared from its wreckage.", ChatType.LogToCentralChat, Context );
                MothershipData.Mines = 0;
                MothershipData.Trust.SetTrust( entity.Planet, -3000 );

                // Despawn all units currently hunting it.
                World_AIW2.Instance.DoForEntities( delegate ( GameEntity_Squad squad )
                {
                    if ( squad.ExoGalacticAttackTarget.GetSquad() != null && squad.ExoGalacticAttackTarget.PrimaryKeyID == Mothership.PrimaryKeyID )
                        squad.Despawn( Context, true, InstancedRendererDeactivationReason.IFinishedMyJob );

                    return DelReturn.Continue;
                } );
            }
        }
    }

    // Description appender for Proto Spheres.
    public class DysonProtoSphereDescriptionAppender : IGameEntityDescriptionAppender
    {
        public void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenDoubleCharacterBuffer Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;

            // Testchamber.
            if ( RelatedEntityOrNull.PlanetFaction.Faction.Type == FactionType.Player )
                return;

            if ( RelatedEntityOrNull.CurrentMarkLevel >= 7 )
                Buffer.Add( "This Sphere Golem is currently " ).StartColor( UnityEngine.Color.red ).Add( "invulnerable.</color>" );
            else if ( RelatedEntityOrNull.CountOfEntitiesProvidingExternalInvulnerability > 0 )
                Buffer.Add( "This Sphere Golem is currently " ).StartColor( UnityEngine.Color.red ).Add( $"invulnerable.</color> The remaining Dyson Nodes on the planet must be killed first." );

            DysonProtoSphereData protoSphereData = RelatedEntityOrNull.Planet.GetProtoSphereData();
            Buffer.Add( $"\nThis Sphere is level {protoSphereData.Level}. " );
            if ( protoSphereData.Level < 7 )
                Buffer.Add( $"\nThis Sphere Golem has acquired {protoSphereData.Resources}/{ProtoSphereCosts.Resources( protoSphereData.Level, World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonPrecursors ) ) )} of the resources required to level up. " );
            Buffer.Add( $"\nThis Sphere Golem will attempt to construct a new Node in {600 - (RelatedEntityOrNull.GetSecondsSinceCreation() % 600)} seconds, if a free slot is available at the time." );

            Buffer.Add( "\n" );
        }
    }
    public abstract class BaseDysonSubfaction : BaseSpecialFaction
    {
        protected override bool EverNeedsToRunLongRangePlanning => false;
        // A list of Zenith factions that the Precursors, Suppressors, and Protectors should always be allied to.
        public static List<Faction> FactionsToAllyTo;
        public BaseDysonSubfaction()
        {
            FactionsToAllyTo = null;
        }
        public abstract void CreateProtoSphere( Faction faction, Planet planet, ArcenSimContext Context );
        public abstract void UpgradeProtoSphere( Faction faction, Planet planet, ArcenSimContext Context );
        public abstract void HandleDysonNodeLogic( Faction faction, Planet planet, ArcenSimContext Context );
        // This base function will only ever be called when we're spawning nodes for a non-suppressor, non-protector Dyson faction.
        public virtual void CreateDysonNode( Faction faction, Planet planet, int nodeMarkLevel, ArcenSimContext Context, string creator = "A Dyson Mothership" )
        {
            GameEntityTypeData dysonNodeData = GameEntityTypeDataTable.Instance.GetRowByName( DysonPrecursors.DYSON_NODE_NAME + nodeMarkLevel );
            GameEntity_Squad newNode = planet.Mapgen_SeedEntity( Context, faction, dysonNodeData, PlanetSeedingZone.OuterSystem );
            if ( DysonPrecursors.DysonNodes[planet] == null )
                DysonPrecursors.DysonNodes[planet] = new GameEntity_Squad[7];
            DysonPrecursors.DysonNodes[planet][nodeMarkLevel - 1] = newNode;

            if ( creator != string.Empty )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( $"{creator} on {planet.Name} has constructed a level {nodeMarkLevel} Dyson Node for {faction.StartFactionColourForLog()}{faction.GetDisplayName()}</color>.", ChatType.LogToCentralChat, Context );
            }
        }
        public override bool GetShouldAttackNormallyExcludedTarget( Faction faction, GameEntity_Squad Target )
        {
            if ( Target.Planet.GetProtoSphereData().Level > 0 && (Target.TypeData.IsCommandStation || Target.TypeData.GetHasTag( "WarpGate" )) )
                return true;
            if ( Target.TypeData.GetHasTag( "NormalPlanetNastyPick" ) )
                return true;
            return false;
        }
        public void FixProtoSphereLevelIfNeeded( Faction faction, Planet planet )
        {
            bool sphereExists = false;
            planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( delegate ( GameEntity_Squad sphere )
             {
                 if ( sphere.TypeData.GetHasTag( "ProtoSphere" ) )
                 {
                     sphereExists = true;
                 }

                 return DelReturn.Continue;
             } );

            if ( !sphereExists )
            {
                planet.GetProtoSphereData().Level = 0;
                planet.GetProtoSphereData().Type = DysonProtoSphereData.ProtoSphereType.None;
                planet.GetProtoSphereData().Resources = 0;
            }
        }
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            UpdateDysonAllegiance( faction, Context );
        }
        private void UpdateDysonAllegiance( Faction faction, ArcenSimContext Context )
        {
            Faction precursorFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonPrecursors ) );
            if ( precursorFaction == null )
                return;
            enemyThisFactionToAll( faction );
            GetFactionsToAllyTo( faction, Context );
            // Ally up.
            for ( int x = 0; x < FactionsToAllyTo.Count; x++ )
            {
                Faction otherFaction = FactionsToAllyTo[x];
                if ( faction == otherFaction )
                    continue;
                faction.MakeFriendlyTo( otherFaction );
                otherFaction.MakeFriendlyTo( faction );
                if ( otherFaction != precursorFaction )
                {
                    precursorFaction.MakeFriendlyTo( otherFaction );
                    otherFaction.MakeFriendlyTo( precursorFaction );
                }
            }
        }
        private void GetFactionsToAllyTo( Faction faction, ArcenSimContext Context )
        {
            // Set up our factions that we should ally to list.
            if ( FactionsToAllyTo == null )
            {
                FactionsToAllyTo = new List<Faction>();
                // Ally ourselves (and our mothership) to other dyson/zenith factions.
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( faction == otherFaction )
                        continue;
                    if ( (otherFaction.GetDisplayName().ToLower().Contains( "dyson" ) || otherFaction.GetDisplayName().ToLower().Contains( "zenith" )) && !otherFaction.GetDisplayName().ToLower().Contains( "dark" ) )
                    {
                        FactionsToAllyTo.Add( otherFaction );
                    }
                }
            }
        }
        public void SpawnDronesOnNodeOrPacketDeath( GameEntity_Squad nodeOrPacket, Faction faction, ArcenSimContext Context )
        {
            PlanetFaction pFaction = nodeOrPacket.Planet.GetPlanetFactionForFaction( faction );
            nodeOrPacket.FleetMembership.Fleet.DoForMemberGroups( mem =>
            {
                if ( mem.TypeData.IsDrone )
                {
                    GameEntityTypeData spawnData;
                    switch ( mem.TypeData.InternalName )
                    {
                        case "DysonMothershipBastionDrone":
                            spawnData = GameEntityTypeDataTable.Instance.GetRowByName( "DysonBastionDecaying" );
                            break;
                        case "DysonMothershipBulwarkDrone":
                            spawnData = GameEntityTypeDataTable.Instance.GetRowByName( "DysonBulwarkDecaying" );
                            break;
                        case "DysonMothershipDefenderDrone":
                            spawnData = GameEntityTypeDataTable.Instance.GetRowByName( "DysonDefenderDecaying" );
                            break;
                        default:
                            spawnData = GameEntityTypeDataTable.Instance.GetRowByName( "DysonSentinelDecaying" );
                            break;
                    }
                    for ( int x = 0; x < mem.EffectiveSquadCap * 3; x++ )
                        GameEntity_Squad.CreateNew( pFaction, mem.TypeData, nodeOrPacket.CurrentMarkLevel, pFaction.FleetUsedAtPlanet, 0, nodeOrPacket.WorldLocation, Context );
                }

                return DelReturn.Continue;
            } );
        }
    }
    public class DysonSuppressors : BaseDysonSubfaction
    {
        protected override string TracingName => "DysonSuppressors";
        protected override bool EverNeedsToRunLongRangePlanning => true;

        public static readonly string SUPPRESSOR_SPHERE_NAME = "DysonProtoSuppressorSphere";

        public override void CreateProtoSphere( Faction faction, Planet planet, ArcenSimContext Context )
        {
            GameEntityTypeData protoSphereData = GameEntityTypeDataTable.Instance.GetRowByName( SUPPRESSOR_SPHERE_NAME + "1" );
            planet.Mapgen_SeedEntity( Context, faction, protoSphereData, PlanetSeedingZone.OuterSystem );
            planet.GetProtoSphereData().Level = 1;
            planet.GetProtoSphereData().Type = DysonProtoSphereData.ProtoSphereType.Suppressor;
            World_AIW2.Instance.QueueChatMessageOrCommand( $"A Dyson Mothership on {planet.Name} has constructed a Proto Suppressor Sphere Golem.", ChatType.LogToCentralChat, Context );
        }
        public override void UpgradeProtoSphere( Faction faction, Planet planet, ArcenSimContext Context )
        {
            GameEntity_Squad protoSphere = null;
            planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( delegate ( GameEntity_Squad entity )
            {
                if ( entity.TypeData.GetHasTag( "ProtoSphere" ) )
                {
                    protoSphere = entity;
                    return DelReturn.Break;
                }
                return DelReturn.Continue;
            } );

            if ( protoSphere != null )
            {
                GameEntityTypeData protoSphereData = GameEntityTypeDataTable.Instance.GetRowByName( SUPPRESSOR_SPHERE_NAME + (planet.GetProtoSphereData().Level + 1) );
                protoSphere = protoSphere.TransformInto( Context, protoSphereData, 1 );
                planet.GetProtoSphereData().Level++;
                planet.GetProtoSphereData().Resources = 0;
                if ( planet.GetProtoSphereData().Level >= 7 )
                    World_AIW2.Instance.QueueChatMessageOrCommand( $"The {protoSphere.TypeData.DisplayName} on {planet.Name} has reached its maximum level, and has completed constructing a Dyson Sphere. It has become indestructible.", ChatType.LogToCentralChat, Context );
                else
                    World_AIW2.Instance.QueueChatMessageOrCommand( $"The {protoSphere.TypeData.DisplayName} on {planet.Name} has leveled up to level {planet.GetProtoSphereData().Level} .", ChatType.LogToCentralChat, Context );
            }
        }
        public override void HandleDysonNodeLogic( Faction faction, Planet planet, ArcenSimContext Context )
        {
            GameEntity_Squad protoSphere = null;
            planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( delegate ( GameEntity_Squad entity )
            {
                if ( entity.TypeData.GetHasTag( "ProtoSphere" ) )
                {
                    protoSphere = entity;
                    return DelReturn.Break;
                }
                return DelReturn.Continue;
            } );

            if ( protoSphere != null && protoSphere.GetSecondsSinceCreation() % 600 == 0 )
            {
                for ( int x = 0; x < planet.GetProtoSphereData().Level; x++ )
                {
                    if ( DysonPrecursors.DysonNodes[planet] == null || DysonPrecursors.DysonNodes[planet][x] == null )
                    {
                        CreateDysonNode( faction, planet, x + 1, Context, "A Proto Sphere" );
                        break;
                    }
                }
            }
        }
        public override void CreateDysonNode( Faction faction, Planet planet, int nodeMarkLevel, ArcenSimContext Context, string creator = "A Dyson Mothership" )
        {
            GameEntityTypeData protoSphereData = GameEntityTypeDataTable.Instance.GetRowByName( DysonPrecursors.DYSON_NODE_NAME + nodeMarkLevel );
            GameEntity_Squad newNode = planet.Mapgen_SeedEntity( Context, faction, protoSphereData, PlanetSeedingZone.OuterSystem );
            if ( DysonPrecursors.DysonNodes[planet] == null )
                DysonPrecursors.DysonNodes[planet] = new GameEntity_Squad[7];
            DysonPrecursors.DysonNodes[planet][nodeMarkLevel - 1] = newNode;
            if ( creator != string.Empty )
                World_AIW2.Instance.QueueChatMessageOrCommand( $"{creator} on {planet.Name} has constructed a level {nodeMarkLevel} Dyson Node.", ChatType.LogToCentralChat, Context );
        }
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }

        public override void DoOnAnyDeathLogic( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenSimContext Context )
        {
            if ( entity.TypeData.GetHasTag( "ProtoSphere" ) )
            {
                DysonProtoSphereData data = entity.Planet.GetProtoSphereData();
                data.Level = 0;
                data.Resources = 0;
                data.Type = DysonProtoSphereData.ProtoSphereType.None;

                World_AIW2.Instance.QueueChatMessageOrCommand( $"A Proto Suppressor Sphere Golem on {entity.Planet.Name} has been destroyed.", ChatType.LogToCentralChat, Context );
            }
            else if ( entity.TypeData.GetHasTag( DysonPrecursors.DYSON_NODE_NAME ) )
            {
                if ( FiringSystemOrNull == null || FiringSystemOrNull.ParentEntity.PlanetFaction.Faction.Type == FactionType.Player )
                    World_AIW2.Instance.DoForFactions( faction =>
                     {
                         if ( faction.Type == FactionType.Player )
                             faction.StoredMetal += entity.TypeData.MetalCost;

                         return DelReturn.Continue;
                     } );
                SpawnDronesOnNodeOrPacketDeath( entity, entity.PlanetFaction.Faction, Context );
            }
            else if ( entity.TypeData.GetHasTag( DysonPrecursors.DYSON_PACKET_NAME ) )
                SpawnDronesOnNodeOrPacketDeath( entity, entity.PlanetFaction.Faction, Context );
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            Faction precursorFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonPrecursors ) );
            if ( precursorFaction == null )
                return;

            faction.DoForEntities( delegate ( GameEntity_Squad entity )
            {
                if ( !entity.TypeData.IsDrone )
                    return DelReturn.Continue;

                // Keep our drones at or ahead of our carriers.
                GameEntity_Squad carrier = entity.FleetMembership.Fleet.Centerpiece;
                if ( carrier == null )
                    return DelReturn.Continue;
                if ( entity.Planet.Index != carrier.Planet.Index )
                    entity.QueueWormholeCommand( carrier.Planet, Context );
                else
                {
                    // If our Carrier is healthy and we're on a player planet, stay near our carrier.
                    if ( carrier.Planet.GetControllingOrInfluencingFaction().Type == FactionType.Player && carrier.GetCurrentShieldPoints() > carrier.TypeData.GetForMark( carrier.CurrentMarkLevel ).BaseShieldPoints * 0.75 &&
                    (entity.GetDistanceTo_VeryCheapButExtremelyRough( carrier.WorldLocation, true ) > 5000 || (entity.LongRangePlanningData.DestinationPoint != ArcenPoint.ZeroZeroPoint &&
                    entity.LongRangePlanningData.DestinationPoint.GetExtremelyRoughDistanceTo( carrier.WorldLocation ) > 5000)) )
                        entity.QueueMovementCommand( carrier.WorldLocation );
                }

                return DelReturn.Continue;
            } );



            faction.ExecuteMovementCommands( Context );
            faction.ExecuteWormholeCommands( Context );
        }
    }

    public class DysonProtectors : BaseDysonSubfaction
    {
        protected override string TracingName => "DysonProtectors";
        protected override bool EverNeedsToRunLongRangePlanning => false;

        public static readonly string PROTECTOR_SPHERE_NAME = "DysonProtoProtectorSphere";

        public override void CreateProtoSphere( Faction faction, Planet planet, ArcenSimContext Context )
        {
            GameEntityTypeData protoSphereData = GameEntityTypeDataTable.Instance.GetRowByName( PROTECTOR_SPHERE_NAME + "1" );
            planet.Mapgen_SeedEntity( Context, faction, protoSphereData, PlanetSeedingZone.OuterSystem );
            planet.GetProtoSphereData().Level = 1;
            planet.GetProtoSphereData().Type = DysonProtoSphereData.ProtoSphereType.Protecter;
            World_AIW2.Instance.QueueChatMessageOrCommand( $"A Dyson Mothership on {planet.Name} has constructed a Proto Protector Sphere Golem.", ChatType.LogToCentralChat, Context );
        }
        public override void UpgradeProtoSphere( Faction faction, Planet planet, ArcenSimContext Context )
        {
            GameEntity_Squad protoSphere = null;
            planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( delegate ( GameEntity_Squad entity )
               {
                   if ( entity.TypeData.GetHasTag( "ProtoSphere" ) )
                   {
                       protoSphere = entity;
                       return DelReturn.Break;
                   }
                   return DelReturn.Continue;
               } );

            if ( protoSphere != null )
            {
                GameEntityTypeData protoSphereData = GameEntityTypeDataTable.Instance.GetRowByName( PROTECTOR_SPHERE_NAME + (planet.GetProtoSphereData().Level + 1) );
                protoSphere = protoSphere.TransformInto( Context, protoSphereData, 1 );
                planet.GetProtoSphereData().Level++;
                planet.GetProtoSphereData().Resources = 0;
                if ( planet.GetProtoSphereData().Level >= 7 )
                    World_AIW2.Instance.QueueChatMessageOrCommand( $"The {protoSphere.TypeData.DisplayName} on {planet.Name} has reached its maximum level and has become indestructible.", ChatType.LogToCentralChat, Context );
                else
                    World_AIW2.Instance.QueueChatMessageOrCommand( $"The {protoSphere.TypeData.DisplayName} on {planet.Name} has leveled up to level {planet.GetProtoSphereData().Level} .", ChatType.LogToCentralChat, Context );
            }
        }
        public override void HandleDysonNodeLogic( Faction faction, Planet planet, ArcenSimContext Context )
        {
            GameEntity_Squad protoSphere = null;
            planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( delegate ( GameEntity_Squad entity )
            {
                if ( entity.TypeData.GetHasTag( "ProtoSphere" ) )
                {
                    protoSphere = entity;
                    return DelReturn.Break;
                }
                return DelReturn.Continue;
            } );

            if ( protoSphere != null && protoSphere.GetSecondsSinceCreation() % 600 == 0 )
            {
                for ( int x = 0; x < planet.GetProtoSphereData().Level; x++ )
                {
                    if ( DysonPrecursors.DysonNodes[planet] == null || DysonPrecursors.DysonNodes[planet][x] == null )
                    {
                        CreateDysonNode( faction, planet, x + 1, Context, "A Suppressor Sphere" );
                        break;
                    }
                }
            }
        }
        public override void CreateDysonNode( Faction faction, Planet planet, int nodeMarkLevel, ArcenSimContext Context, string creator = "A Dyson Mothership" )
        {
            GameEntityTypeData protoSphereData = GameEntityTypeDataTable.Instance.GetRowByName( DysonPrecursors.DYSON_NODE_NAME + nodeMarkLevel );
            GameEntity_Squad newNode = planet.Mapgen_SeedEntity( Context, faction, protoSphereData, PlanetSeedingZone.OuterSystem );
            if ( DysonPrecursors.DysonNodes[planet] == null )
                DysonPrecursors.DysonNodes[planet] = new GameEntity_Squad[7];
            DysonPrecursors.DysonNodes[planet][nodeMarkLevel - 1] = newNode;
            if ( creator != string.Empty )
                World_AIW2.Instance.QueueChatMessageOrCommand( $"{creator} on {planet.Name} has constructed a level {nodeMarkLevel} Dyson Node.", ChatType.LogToCentralChat, Context );
        }
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
            allyThisFactionToHumans( faction );
        }
        public override void DoOnAnyDeathLogic( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenSimContext Context )
        {
            if ( entity.TypeData.GetHasTag( "ProtoSphere" ) )
            {
                DysonProtoSphereData data = entity.Planet.GetProtoSphereData();
                data.Level = 0;
                data.Resources = 0;
                data.Type = DysonProtoSphereData.ProtoSphereType.None;

                World_AIW2.Instance.QueueChatMessageOrCommand( $"A Proto Protector Sphere Golem on {entity.Planet.Name} has been destroyed.", ChatType.LogToCentralChat, Context );
            }
            else if ( entity.TypeData.GetHasTag( DysonPrecursors.DYSON_NODE_NAME ) )
            {
                if ( FiringSystemOrNull == null || FiringSystemOrNull.ParentEntity.PlanetFaction.Faction.Type == FactionType.Player )
                    World_AIW2.Instance.DoForFactions( faction =>
                    {
                        if ( faction.Type == FactionType.Player )
                            faction.StoredMetal += entity.TypeData.MetalCost;

                        return DelReturn.Continue;
                    } );
                Faction suppressorFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonSuppressors ) );
                (suppressorFaction.Implementation as DysonSuppressors).SpawnDronesOnNodeOrPacketDeath( entity, suppressorFaction, Context );
            }
            else if ( entity.TypeData.GetHasTag( DysonPrecursors.DYSON_PACKET_NAME ) )
                SpawnDronesOnNodeOrPacketDeath( entity, entity.PlanetFaction.Faction, Context );
        }
    }
}
