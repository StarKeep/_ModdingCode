﻿using System;
using System.Collections.Generic;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace PreceptsOfThePrecursors
{
    public static class PrecursorCosts
    {
        private static int MinesBase, MinesIncrease, ResourcesBase, ResourcesIncrease;

        public static void Initialize( Faction faction )
        {
            MinesBase = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DysonPrecursors_MinesToMarkUpBase" );
            MinesIncrease = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DysonPrecursors_MinesToMarkUpIncrease" );
            ResourcesBase = (ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DysonPrecursors_MothershipResourcesToMarkUpBase" ) / 10) * (5 + ((10 - faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity) / 2));
            ResourcesIncrease = (ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DysonPrecursors_MothershipResourcesToMarkUpIncrease" ) / 10) * (5 + ((10 - faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity) / 2));
        }

        public static int Mines( int currentMarkLevel, Faction faction )
        {
            int cost = MinesBase;
            if ( currentMarkLevel > 1 )
                cost += MinesIncrease * (currentMarkLevel - 1);

            if ( faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).ExtraStrongMode )
                return 0;

            return cost;
        }
        public static int Resources( int currentMarkLevel, Faction faction )
        {
            int cost = ResourcesBase;
            if ( currentMarkLevel > 1 )
                cost += ResourcesIncrease * (currentMarkLevel - 1);
            if ( faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).ExtraStrongMode )
                return 0;

            return cost;
        }
    }
    public static class ProtoSphereCosts
    {
        private static int Base, IncreasePerExisting, MarkUpBase, MarkUpIncrease;

        public static void Initialize( Faction faction )
        {
            Base = (ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DysonPrecursors_ProtoSphereBaseCost" ) / 10) * (5 + ((10 - faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity) / 2));
            IncreasePerExisting = (ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DysonPrecursors_ProtoSphereCostIncreasePerExistingSphere" ) / 10) * (5 + ((10 - faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity) / 2));
            MarkUpBase = (ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DysonPrecursors_ProtoSphereResourcesToMarkUpBase" ) / 10) * (5 + ((10 - faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity) / 2));
            MarkUpIncrease = (ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DysonPrecursors_ProtoSphereResourcesToMarkUpIncrease" ) / 10) * (5 + ((10 - faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity) / 2));
        }

        public static int BuildCost( Faction faction, bool calledFromSimSafeThread )
        {
            ExternalDataRetrieval rules = calledFromSimSafeThread ? ExternalDataRetrieval.CreateIfNotFound : ExternalDataRetrieval.ReturnNullIfNotFound;
            int cost = Base;

            World_AIW2.Instance.DoForPlanets( false, planet =>
             {
                 if ( planet.GetPrecursorPerPlanetData( rules ).Type != DysonPerPlanetData.ProtoSphereType.None )
                     cost += IncreasePerExisting;

                 return DelReturn.Continue;
             } );

            if ( faction.Ex_MinorFactionCommon_GetPrimitives( rules )?.ExtraStrongMode == true )
                cost /= 2;

            return cost;
        }
        public static int Resources( int currentMarkLevel, Faction faction, bool calledFromSimSafeThread )
        {
            ExternalDataRetrieval rules = calledFromSimSafeThread ? ExternalDataRetrieval.CreateIfNotFound : ExternalDataRetrieval.ReturnNullIfNotFound;
            int cost = MarkUpBase;

            if ( currentMarkLevel > 1 )
                cost += MarkUpIncrease * (currentMarkLevel - 1);

            if ( faction.Ex_MinorFactionCommon_GetPrimitives( rules )?.ExtraStrongMode == true )
                cost /= 2;

            return cost;
        }
    }
    public static class PacketTimers
    {
        private static int NodeBase, NodeIncrease, SphereBase, SphereIncrease;

        public static void Initialize( Faction faction )
        {
            NodeBase = (ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DysonPrecursors_PacketSpawnIntervalBase" ) / 10) * (5 + ((10 - faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity) / 2));
            NodeIncrease = (ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DysonPrecursors_PacketSpawnIntervalIncreasePerNodeLevel" ) / 10) * (5 + ((10 - faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity) / 2));
            SphereBase = (ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DysonPrecursors_PacketSpawnIntervalSphereBase" ) / 10) * (5 + ((10 - faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity) / 2));
            SphereIncrease = (ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DysonPrecursors_PacketSpawnIntervalSphereIncreasePerLevel" ) / 10) * (5 + ((10 - faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity) / 2));

            if ( faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).ExtraStrongMode )
            {
                NodeBase /= 2;
                NodeIncrease /= 2;
                SphereBase /= 2;
                SphereIncrease /= 2;
            }
        }

        public static bool[] GetShouldSpawnNodeArray( Faction faction )
        {
            bool[] shouldSpawn = new bool[7];
            for ( int x = 0; x < 7; x++ )
            {
                bool canSpawn = World_AIW2.Instance.GameSecond % (NodeBase + (NodeIncrease * x)) == 0;
                shouldSpawn[x] = canSpawn;
            }

            return shouldSpawn;
        }

        public static bool[] GetShouldSpawnSphereArray( Faction faction )
        {
            bool[] shouldSpawn = new bool[7];
            for ( int x = 0; x < 7; x++ )
                shouldSpawn[x] = World_AIW2.Instance.GameSecond % (SphereBase + (SphereIncrease * x)) == 0;

            return shouldSpawn;
        }
    }

    // Main faction class.
    public class DysonPrecursors : BaseSpecialFaction, IBulkPathfinding
    {
        protected override string TracingName => "DysonPrecursors";
        protected override bool EverNeedsToRunLongRangePlanning => true;

        public override void WriteTextToSecondLineOfLeftSidebarInLobby( ConfigurationForFaction FactionConfig, Faction FactionOrNull, ArcenDoubleCharacterBuffer buffer )
        {
            string value = FactionConfig.GetValueForCustomFieldOrDefaultValue( "Intensity" );
            bool hasAdded = false;
            if ( value != null )
            {
                hasAdded = true;
                buffer.Add( "Strength: " ).Add( value );
            }
            value = FactionConfig.GetValueForCustomFieldOrDefaultValue( "ExtraStrongMode" );
            if ( value != null )
            {
                if ( hasAdded )
                    buffer.Add( "    " );
                else
                    hasAdded = true;
                buffer.Add( "Fully Awoken: " ).Add( value );
            }
        }

        public override bool GetShouldAttackNormallyExcludedTarget( Faction faction, GameEntity_Squad Target )
        {
            if ( Target.TypeData.GetHasTag( "NormalPlanetNastyPick" ) )
                return true;
            if ( Target.Planet.UnderInfluenceOfFactionIndex.Contains( faction.FactionIndex ) &&
                (Target.PlanetFaction.Faction.Type == FactionType.SpecialFaction || Target.TypeData.IsCommandStation) )
                return true;
            return false;
        }

        public ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> WormholeCommands { get; set; }
        public ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> MovementCommands { get; set; }

        public static string MOTHERSHIP_NAME = "DysonMothership";
        public static string DYSON_NODE_NAME = "DysonNode";
        public static string DYSON_PACKET_TAG = "DysonPacket";
        public static string DYSON_ANCIENT_NODE_NAME = "DysonNodeAncient";

        // Various things we need to keep track of.
        // Some are static to allow the ease of use for descriptions.
        public static GameEntity_Squad Mothership;
        public static DysonMothershipData MothershipData;
        public AntiMinorFactionWaveData WaveData;
        public ExoData ExoData;
        public static ArcenSparseLookup<Planet, GameEntity_Squad[]> DysonNodes;
        public static bool Sleeping
        {
            get
            {
                {
                    Faction faction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonPrecursors ) );
                    if ( faction == null )
                        return true;
                    if ( !faction.MustBeAwakenedByPlayer )
                        return false;
                    if ( faction.HasBeenAwakenedByPlayer )
                        return false;
                    if ( Hacking_AwakenDysonPrecursors.IsActive )
                    {
                        Hacking_AwakenDysonPrecursors.IsActive = false;
                        return false;
                    }

                    return true;
                }
            }
        }

        // Special. This will only ever be false once. Never set it back to false unless the game restarts.
        public static bool HeadersAppliedToOldJournals = false;

        private bool Initialized = false;

        public enum Commands
        {
            SetPlanetToBuildOn,
            BuildPrecursorStructures
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

            HandleMovedToNewPlanetLogicIfNeeded( Context );

            DecayTrust( faction );

            HandleTrust( faction );

            HandleMineConsumption( faction, Context );

            GetReadyToMoveOnIfAble( faction, Context );

            HandleAIResponse( faction, Context );

            SpawnPackets( faction, Context );
        }

        private void Initialize( Faction faction, ArcenSimContext Context )
        {
            if ( MothershipData == null )
                MothershipData = faction.GetMothershipData( ExternalDataRetrieval.CreateIfNotFound );

            if ( !Initialized )
            {
                PrecursorCosts.Initialize( faction );
                ProtoSphereCosts.Initialize( faction );
                PacketTimers.Initialize( faction );
                Initialized = true;
            }

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
                ExoData = faction.GetExoDataExt( ExternalDataRetrieval.CreateIfNotFound );
                if ( ExoData.StrengthRequiredForNextExo == 0 )
                {
                    ExoData.FactionIndexOfOriginFaction = faction.FactionIndex;
                    ExoData.FactionIndexOfExoSpawnFaction = World_AIW2.GetRandomAIFaction( Context ).FactionIndex;
                    ExoData.CurrentExoStrength = FInt.Zero;
                    ExoData.NumExosSoFar = 0;
                    ExoData.PercentToStartWarning = 75;
                }
            }

            if ( WaveData == null )
            {
                WaveData = faction.GetAntiMinorFactionWaveDataExt( ExternalDataRetrieval.CreateIfNotFound );
            }

            if ( Mothership == null )
            {
                // Mothership is dead.
                // If timer has not yet been set, set it based on intensity.
                if ( MothershipData.SecondsUntilRespawn < 0 )
                    MothershipData.SecondsUntilRespawn = 630 - faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity * 30;
                // Decrease timer.
                MothershipData.SecondsUntilRespawn = Math.Max( 0, MothershipData.SecondsUntilRespawn - 1 );
                // If ready, respawn Mothership.
                if ( MothershipData.SecondsUntilRespawn == 0 || World_AIW2.Instance.GameSecond < 60 )
                {
                    // Find our Ancient Node, or create a new one if needed.
                    GameEntity_Squad ancientNode = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonSuppressors ) ).GetFirstMatching( DYSON_ANCIENT_NODE_NAME, true, true );
                    if ( ancientNode == null )
                        ancientNode = SpawnAncientNode( faction, Context );
                    if ( ancientNode != null )
                        SpawnMothership( ancientNode, faction, Context );
                }
                return;
            }
        }
        private void GiveJournalsAsNeeded( Faction faction, ArcenSimContext Context )
        {
            MothershipLoreJournals( faction, Context );
            //GameGuides(faction, Context);
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
            if ( FactionUtilityMethods.GetHopsToPlayerPlanet( Mothership.Planet, Context ) == 1 )
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
            bool hasAdjacentProtectorNode = false;
            Mothership.Planet.DoForLinkedNeighbors( false, adjPlanet =>
            {
                if ( DysonNodes.GetHasKey( adjPlanet ) && MothershipData.Trust.GetTrust( adjPlanet ) > 500 )
                {
                    hasAdjacentProtectorNode = true;
                    return DelReturn.Break;
                }

                return DelReturn.Continue;
            } );
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
            else if ( hasAdjacentProtectorNode )
                allyThisFactionToHumans( faction );
            else if ( (Mothership.GetSecondsSinceEnteringThisPlanet() > 10 || faction.GetIsHostileTowards( FactionUtilityMethods.findHumanKing().GetControllingFaction() )) &&
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
                faction.MakeFriendlyTo( otherFaction );
                otherFaction.MakeFriendlyTo( faction );
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
                DysonPerPlanetData sphereData = mainPlanet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound );

                CalculateNodeIncome( mainPlanet, incomePerPlanet );

                if ( sphereData.Type != DysonPerPlanetData.ProtoSphereType.Other && sphereData.Level < 7 )
                    sphereData.Resources += incomePerPlanet[mainPlanet];

                DysonPerPlanetData protoSphereData = mainPlanet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound );
                Faction sphereFaction = null;
                if ( protoSphereData.Type == DysonPerPlanetData.ProtoSphereType.Protecter )
                    sphereFaction = protectorSphereFaction;
                else if ( protoSphereData.Type == DysonPerPlanetData.ProtoSphereType.Suppressor )
                    sphereFaction = suppressorSphereFaction;

                HandleFriendlyNPCPlanet( sphereData, mainPlanet );

                if ( sphereFaction == null )
                    return DelReturn.Continue;

                // Deleveling logic. Make sure aren't a different level or type that we should be.
                (sphereFaction.Implementation as BaseDysonSubfaction).FixProtoSphereLevelIfNeeded( sphereFaction, mainPlanet );

                // Node Logic.
                (sphereFaction.Implementation as BaseDysonSubfaction).HandleDysonNodeLogic( sphereFaction, mainPlanet, Context );

                // Leveling Logic.
                if ( protoSphereData.Level < 7 && protoSphereData.Resources > ProtoSphereCosts.Resources( protoSphereData.Level, faction, true ) )
                    (sphereFaction.Implementation as BaseDysonSubfaction).UpgradeProtoSphere( sphereFaction, mainPlanet, Context );

                return DelReturn.Continue;
            } );

            ApplyCalculatedIncome( incomePerPlanet, faction );

            LevelUpMothershipIfAble( faction, Context );
        }
        private void CalculateNodeIncome( Planet planet, ArcenSparseLookup<Planet, int> incomePerPlanet )
        {
            if ( planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Level > 0 || (Mothership != null && Mothership.Planet.Index == planet.Index) )
            {
                incomePerPlanet.AddPair( planet, planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Level );
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
        private void HandleFriendlyNPCPlanet( DysonPerPlanetData protoSphereData, Planet planet )
        {
            List<GameEntity_Squad> dysonSpheres = SpecialFaction_DysonSphere.DysonSpheres_AllFactions;
            if ( dysonSpheres != null && dysonSpheres.Count > 0 )
                for ( int y = 0; y < dysonSpheres.Count; y++ )
                    if ( dysonSpheres[y].Planet.Index == planet.Index )
                    {
                        protoSphereData.Type = DysonPerPlanetData.ProtoSphereType.Other;
                        protoSphereData.Level = 7;
                        break;
                    }

        }
        private void ApplyCalculatedIncome( ArcenSparseLookup<Planet, int> incomePerPlanet, Faction faction )
        {
            for ( int x = 0; x < incomePerPlanet.GetPairCount(); x++ )
            {
                ArcenSparseLookupPair<Planet, int> pair = incomePerPlanet.GetPairByIndex( x );
                Planet planet = pair.Key;
                int income = pair.Value;
                DysonPerPlanetData sphereData = planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound );
                if ( sphereData.Level > 0 && sphereData.Level < 7 )
                    sphereData.Resources += income;

                if ( Mothership != null && Mothership.Planet == planet )
                {
                    // Increment Mothership resources based on mine and node count.
                    int mothershipIncomeFromMines = MothershipData.Mines * (1 + (faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity / 2));
                    MothershipData.Resources += 1 + mothershipIncomeFromMines + income;
                    MothershipData.MetalGainedOrLostLastSecond = 1 + mothershipIncomeFromMines + income;
                }
            }
        }
        private void LevelUpMothershipIfAble( Faction faction, ArcenSimContext Context )
        {
            if ( Mothership == null )
                return;
            if ( PrecursorCosts.Resources( Mothership.CurrentMarkLevel, faction ) > ProtoSphereCosts.BuildCost( faction, true ) )
                return;
            if ( MothershipData.Level < 7 && DysonNodes.GetHasKey( Mothership.Planet ) &&
                MothershipData.Resources >= PrecursorCosts.Resources( MothershipData.Level, faction ) &&
                MothershipData.Mines >= PrecursorCosts.Mines( MothershipData.Level, faction ) )
            {
                // Level on up.
                MothershipData.Resources -= PrecursorCosts.Resources( Mothership.CurrentMarkLevel, faction );
                GameEntityTypeData newMothershipData = GameEntityTypeDataTable.Instance.GetRowByName( MOTHERSHIP_NAME + (MothershipData.Level + 1) );
                Mothership = Mothership.TransformInto( Context, newMothershipData, 1 );
                MothershipData.Level++;
                if ( Mothership.Planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    World_AIW2.Instance.QueueChatMessageOrCommand( "The Dyson Mothership on " + Mothership.Planet.Name + " has leveled up to level " + MothershipData.Level + ".", ChatType.LogToCentralChat, Context );
            }
        }
        private void HandleMovedToNewPlanetLogicIfNeeded( ArcenSimContext Context )
        {
            if ( Mothership == null )
                return;

            if ( Mothership.GetSecondsSinceEnteringThisPlanet() <= 2 )
            {
                // Reset our hull check.
                MothershipData.HullWhenEnteredPlanet = Mothership.GetCurrentHullPoints();

                // Reset some movement boolean(s).
                MothershipData.ReadyToMoveOn = false;
            }
            // If we recently entered a new planet, regenerate our shields.
            if ( Mothership.GetSecondsSinceEnteringThisPlanet() < 30 )
                Mothership.TakeShieldRepair( 100000 );

            Faction darkSpire = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( SpecialFaction_DarkSpire ) );
            if ( darkSpire != null )
            {
                GameEntity_Squad darkSpireGenerator = Mothership.Planet.GetFirstMatching( FactionType.SpecialFaction, "VengeanceGeneratorNormalSpawn", false, false );
                if ( darkSpireGenerator != null )
                {
                    DarkSpireData darkSpireGlobalData = World.Instance.GetDarkSpireDataExt_AndCacheAfter( ExternalDataRetrieval.CreateIfNotFound );
                    FInt mult = (FInt.One * Mothership.CurrentMarkLevel) / (11 - darkSpire.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity);
                    if ( Mothership.GetSecondsSinceEnteringThisPlanet() == 2 )
                        World_AIW2.Instance.QueueChatMessageOrCommand( $"The Dyson Mothership on {Mothership.Planet.Name} has begune to consume a Vengence Generator, angering the Dark Spire. She will finish within a minute.", ChatType.LogToCentralChat, Context );
                    else if ( Mothership.GetSecondsSinceEnteringThisPlanet() >= 60 )
                    {
                        World_AIW2.Instance.QueueChatMessageOrCommand( $"The Dyson Mothership on {Mothership.Planet.Name} has finished consuming a Vengence Generator, and has built a Level 10 Packet Generator in its place.", ChatType.LogToCentralChat, Context );
                        Faction spawnFaction = null;
                        if ( MothershipData.Trust.GetTrust( Mothership.Planet ) > 500 )
                            spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonProtectors ) );
                        else
                            spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonSuppressors ) );
                        PlanetFaction pFaction = Mothership.Planet.GetPlanetFactionForFaction( spawnFaction );
                        GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, GameEntityTypeDataTable.Instance.GetRowByName( "Level10PacketGenerator" ), 7, pFaction.FleetUsedAtPlanet, 0, darkSpireGenerator.WorldLocation, Context );
                        darkSpireGenerator.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
                        for ( int x = 0; x < darkSpireGlobalData.PerPlanet.GetPairCount(); x++ )
                        {
                            darkSpireGlobalData.PerPlanet.GetPairByIndex( x ).Value.NetEnergy += 2500 * mult;
                            darkSpireGlobalData.PerPlanet.GetPairByIndex( x ).Value.TotalEnergy += 2500 * mult;
                        }
                    }
                    else
                        for ( int x = 0; x < darkSpireGlobalData.PerPlanet.GetPairCount(); x++ )
                        {
                            darkSpireGlobalData.PerPlanet.GetPairByIndex( x ).Value.NetEnergy += 100 * mult;
                            darkSpireGlobalData.PerPlanet.GetPairByIndex( x ).Value.TotalEnergy += 100 * mult;
                        }
                }
            }
        }
        private void DecayTrust( Faction faction )
        {
            if ( Mothership == null )
                return;

            // Decay trust that the Motherhsip is not on, down to an absolute of 1000.
            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                if ( Mothership.Planet.GetHopsTo( planet ) < 1 || (MothershipData.PlanetToBuildOn != null && MothershipData.PlanetToBuildOn == planet) )
                    return DelReturn.Continue;

                if ( MothershipData.Trust.GetTrust( planet ) > 1000 && MothershipData.Trust.GetTrust( planet ) > MothershipData.Trust.MinTrust( planet ) )
                    MothershipData.Trust.AddOrSubtractTrust( planet, -1 );
                else if ( MothershipData.Trust.GetTrust( planet ) < -1000 && MothershipData.Trust.GetTrust( planet ) < MothershipData.Trust.MaxTrust( planet ) )
                    MothershipData.Trust.AddOrSubtractTrust( planet, 1 );

                return DelReturn.Continue;
            } );
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
                    for ( int x = 0; x < 7; x++ )
                        if ( DysonNodes[planet][x] != null )
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
            // Factor in suppressrors.
            hostileStrength += Mothership.Planet.GetPlanetFactionForFaction( World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonSuppressors ) ) ).DataByStance[FactionStance.Self].TotalStrength;
            mothershipStrength = Mothership.PlanetFaction.DataByStance[FactionStance.Self].TotalStrength;

            if ( MothershipUndamaged( hullForTrustGain, shieldForTrustGain ) && HumanStrengthAdvantage( humanStrength, hostileStrength ) )
            {
                if ( trust < MothershipData.Trust.MaxTrust( Mothership.Planet ) )
                {
                    short potentialChange = (short)(baseChange + nodeBonus);
                    short change = Math.Max( baseChange, potentialChange );

                    MothershipData.Trust.AddOrSubtractTrust( Mothership.Planet, change );
                    MothershipData.IsGainingTrust = true;
                }

            }
            else if ( MothershipDamaged( hullForTrustLoss, shieldForTrustLoss ) || !HumanStrengthAdvantage( humanStrength, hostileStrength ) )
            {

                if ( trust > MothershipData.Trust.MinTrust( Mothership.Planet ) )
                {
                    short potentialChange = (short)((-baseChange) + nodeBonus);
                    short change = (short)Math.Min( -baseChange, potentialChange );

                    MothershipData.Trust.AddOrSubtractTrust( Mothership.Planet, change );
                    MothershipData.IsLosingTrust = true;
                }
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
                        GameEntity_Squad king = FactionUtilityMethods.findKing( Mothership.Planet.GetControllingOrInfluencingFaction() );
                        if ( king != null )
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
                        if ( Mothership.Planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
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
            if ( MothershipData.PlanetToBuildOn != null )
                timeToStay /= 2;
            if ( World_AIW2.Instance.GameSecond - Mothership.GameSecondEnteredThisPlanet > timeToStay )
            {
                if ( !MothershipData.ReadyToMoveOn )
                    MoveToNewPlanet( faction, Context );
            }
        }

        // Spawn an Ancient Node somewhere in the galaxy.
        private GameEntity_Squad SpawnAncientNode( Faction faction, ArcenSimContext Context )
        {
            // If we have an exising node somwhere in the galaxy, convert it into an Ancient Node instead.
            GameEntity_Squad nodeToConvert = null;
            if ( DysonNodes != null )
                DysonNodes.DoFor( pair =>
                {
                    if ( pair.Value[0] != null )
                        if ( nodeToConvert == null || Context.RandomToUse.NextBool() )
                            nodeToConvert = pair.Value[0];

                    return DelReturn.Continue;
                } );

            if ( nodeToConvert != null )
            {
                GameEntity_Squad ancientNode = nodeToConvert.Planet.Mapgen_SeedEntity( Context, nodeToConvert.PlanetFaction.Faction, GameEntityTypeDataTable.Instance.GetRowByName( DYSON_ANCIENT_NODE_NAME ), PlanetSeedingZone.OuterSystem );
                nodeToConvert.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );

                if ( ancientNode.Planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    World_AIW2.Instance.QueueChatMessageOrCommand( $"An Ancient Dyson Node has awoken on {ancientNode.Planet.Name}.", ChatType.LogToCentralChat, Context );
                return ancientNode;
            }

            string spawnOption = faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).SpawningOptions;

            try
            {
                List<Planet> potentialPlanets = new List<Planet>();
                if ( spawnOption == "Near Player" )
                {
                    World_AIW2.Instance.DoForPlanets( false, planet =>
                    {
                        if ( planet.OriginalHopsToHumanHomeworld == 4 )
                            potentialPlanets.Add( planet );

                        return DelReturn.Continue;
                    } );
                }
                if ( potentialPlanets.Count <= 0 )
                {
                    short workingHops = 5;
                    while ( potentialPlanets.Count == 0 )
                    {
                        World_AIW2.Instance.DoForPlanets( false, planet =>
                         {
                             bool isValid = true;
                             planet.DoForPlanetsWithinXHops( Context, workingHops, ( workingPlanet, hops ) =>
                             {
                                 if ( workingPlanet.GetControllingOrInfluencingFaction().Type == FactionType.Player )
                                     isValid = false;
                                 if ( workingPlanet.GetCommandStationOrNull() != null && workingPlanet.GetCommandStationOrNull().TypeData.IsKingUnit )
                                     isValid = false;

                                 return DelReturn.Continue;
                             } );

                             if ( isValid )
                             {
                                 potentialPlanets.Add( planet );
                             }
                             return DelReturn.Continue;
                         } );
                        workingHops--;
                    }
                }

                Planet spawnPlanet = potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];

                if ( spawnPlanet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    World_AIW2.Instance.QueueChatMessageOrCommand( $"An Ancient Dyson Node has spawnt on {spawnPlanet.Name}.", ChatType.LogToCentralChat, Context );

                // Spawn in our Ancient Node.
                GameEntityTypeData mothershipEntityData = GameEntityTypeDataTable.Instance.GetRowByName( DYSON_ANCIENT_NODE_NAME );
                Faction spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonSuppressors ) );
                return spawnPlanet.Mapgen_SeedEntity( Context, spawnFaction, mothershipEntityData, PlanetSeedingZone.OuterSystem );
            }
            catch ( Exception )
            {
                return null;
            }
        }

        // Spawn a Mothership on our Ancient Node.
        public void SpawnMothership( GameEntity_Squad ancientNode, Faction faction, ArcenSimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            // Spawn in our Mothership.
            GameEntityTypeData mothershipEntityData = GameEntityTypeDataTable.Instance.GetRowByName( MOTHERSHIP_NAME + "1" );
            PlanetFaction pFaction = ancientNode.Planet.GetPlanetFactionForFaction( faction );
            Mothership = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, mothershipEntityData, 1, pFaction.FleetUsedAtPlanet, 0, ancientNode.WorldLocation, Context );

            // Reset our Mothership data.
            MothershipData = new DysonMothershipData
            {
                LastGameSecondSpawnt = World_AIW2.Instance.GameSecond
            };
            faction.SetMothershipData( MothershipData );
            MothershipData = faction.GetMothershipData( ExternalDataRetrieval.CreateIfNotFound );

            if ( ancientNode.Planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                World_AIW2.Instance.QueueChatMessageOrCommand( $"A Dyson Mothership has spawnt on {ancientNode.Planet.Name}.", ChatType.LogToCentralChat, Context );
        }
        private void MoveToNewPlanet( Faction faction, ArcenSimContext Context )
        {
            if ( Mothership == null )
                return;
            // Node logic.
            // Upon leaving a planet, if the absolute trust of the planet is over 1k, drop down up to 2 Nodes on the planet if there are free Node slots left.
            for ( int i = 0; i < 2; i++ )
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
                            if ( spawnFaction == null && Mothership.Planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Type == DysonPerPlanetData.ProtoSphereType.Other )
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
                if ( Mothership.Planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    World_AIW2.Instance.QueueChatMessageOrCommand( $"A Dyson Mothership has left behind a fully functional mine on {Mothership.Planet.Name}.", ChatType.LogToCentralChat, Context );
            }

            // Proto-Sphere logic. Create a Proto-Sphere if:
            // Mothership Resources > Protosphere Cost
            // Trust > 2000 or < -2000
            // Its a planet we planned to build on.
            if ( MothershipData.Resources >= ProtoSphereCosts.BuildCost( faction, true ) &&
                MothershipData.PlanetToBuildOn != null && Mothership.Planet.Index == MothershipData.PlanetToBuildOn.Index )
            {
                DysonPerPlanetData protoSphereData = Mothership.Planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound );
                if ( protoSphereData.Level <= 0 )
                {
                    Faction spawnFaction = null;
                    if ( MothershipData.Trust.GetTrust( Mothership.Planet ) >= 2000 )
                        spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonProtectors ) );
                    else if ( MothershipData.Trust.GetTrust( Mothership.Planet ) <= -2000 )
                        spawnFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonSuppressors ) );
                    if ( spawnFaction != null )
                    {
                        MothershipData.Resources -= ProtoSphereCosts.BuildCost( faction, true );
                        (spawnFaction.Implementation as BaseDysonSubfaction).CreateProtoSphere( spawnFaction, Mothership.Planet, Context );
                        MothershipData.PlanetToBuildOn = null;
                    }
                }
            }

            MothershipData.ReadyToMoveOn = true;
        }

        private void HandleAIResponse( Faction faction, ArcenSimContext Context )
        {
            Faction suppressors = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonSuppressors ) );


            if ( World_AIW2.Instance.GameSecond < 10 || suppressors == null )
            {
                WaveData.timeForNextWave = 600;
                return;
            }

            if ( WaveData.timeForNextWave > 0 )
                WaveData.timeForNextWave--;
            else
            {
                List<Planet> capturedPlanets = new List<Planet>();
                World_AIW2.Instance.DoForPlanets( false, planet =>
                {
                    if ( planet.GetControllingOrInfluencingFaction().Implementation is BaseDysonSubfaction )
                        capturedPlanets.Add( planet );

                    return DelReturn.Continue;
                } );

                if ( capturedPlanets.Count == 0 )
                    WaveData.timeForNextWave = 600;
                else
                {
                    int budget = 0;
                    World_AIW2.Instance.DoForFactions( aiFaction =>
                    {
                        if ( !(aiFaction.Implementation is SpecialFaction_AI) )
                            return DelReturn.Continue;

                        SpecialFaction_AI aiImplementation = aiFaction.Implementation as SpecialFaction_AI;
                        FInt old = aiFaction.GetSentinelsExternal( ExternalDataRetrieval.CreateIfNotFound ).AIProgress_Total;
                        aiFaction.GetSentinelsExternal( ExternalDataRetrieval.CreateIfNotFound ).AIProgress_Total = FInt.FromParts( capturedPlanets.Count * 50, 000 );

                        budget = Math.Max( budget, (this.GetSpecificBudgetThreshold( faction, AIBudgetType.Wave )) );

                        aiFaction.GetSentinelsExternal( ExternalDataRetrieval.CreateIfNotFound ).AIProgress_Total = old;

                        return DelReturn.Continue;
                    } );
                    if ( budget > 0 )
                    {
                        AntiMinorFactionWaveData.QueueWave( suppressors, Context, budget );

                        WaveData.timeForNextWave = 600;
                    }
                }
            }

            // Boost the budget of extragalactic units.
            World_AIW2.Instance.DoForFactions( otherFaction =>
            {
                if ( otherFaction.Type != FactionType.AI )
                    return DelReturn.Continue;

                AISentinelsExternalData factionExternal = otherFaction.GetSentinelsExternal( ExternalDataRetrieval.CreateIfNotFound );
                List<ExtragalacticBudget> budgets = factionExternal.ExtragalacticBudgets;

                World_AIW2.Instance.DoForFactions( targetFaction =>
                {
                    if ( !(targetFaction.Implementation is DysonSuppressors) )
                        return DelReturn.Continue;

                    ExtragalacticBudget budget = ExtragalacticBudget.GetBudgetFromList( budgets, targetFaction );
                    if ( budget == null || budget.Budget < FInt.One * 100 )
                        return DelReturn.Continue;

                    budget.Budget += targetFaction.OverallPowerLevel * 100;
                    return DelReturn.Continue;
                } );

                return DelReturn.Continue;
            } );
        }

        private void SpawnPackets( Faction faction, ArcenSimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            if ( DysonNodes == null || DysonNodes.GetPairCount() < 1 )
                return;

            bool[] shouldSpawnForNodes = PacketTimers.GetShouldSpawnNodeArray( faction ), shouldSpawnForSpheres = PacketTimers.GetShouldSpawnSphereArray( faction );

            for ( int x = 0; x < DysonNodes.GetPairCount(); x++ )
                for ( int y = 0; y < 7; y++ )
                    if ( shouldSpawnForNodes[y] && DysonNodes.GetPairByIndex( x ).Value[y] != null )
                    {
                        GameEntity_Squad node = DysonNodes.GetPairByIndex( x ).Value[y];

                        GameEntity_Squad packet = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( node.PlanetFaction, GameEntityTypeDataTable.Instance.GetRowByName( "DysonPacket" + (y + 1) ), (byte)(y + 1), node.PlanetFaction.FleetUsedAtPlanet, 0, node.WorldLocation, Context );
                        packet.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, node.PlanetFaction.Faction.FactionIndex );
                    }

            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                if ( (planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Type == DysonPerPlanetData.ProtoSphereType.Protecter || planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Type == DysonPerPlanetData.ProtoSphereType.Suppressor)
                   && shouldSpawnForSpheres[planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Level - 1] )
                {
                    GameEntity_Squad sphere = planet.GetFirstMatching( FactionType.SpecialFaction, "ProtoSphere", false, false );
                    if ( sphere == null )
                        return DelReturn.Continue;

                    int levelToSpawn = 0;
                    if ( planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Level < 7 )
                        levelToSpawn = planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Level;
                    else
                        levelToSpawn = (planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Type == DysonPerPlanetData.ProtoSphereType.Protecter ? 8 : 9);

                    GameEntity_Squad packet = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( sphere.PlanetFaction, GameEntityTypeDataTable.Instance.GetRowByName( "DysonPacket" + levelToSpawn ), sphere.CurrentMarkLevel, sphere.PlanetFaction.FleetUsedAtPlanet, 0, sphere.WorldLocation, Context );
                    packet.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, sphere.PlanetFaction.Faction.FactionIndex );
                }

                return DelReturn.Continue;
            } );
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
            FactionUtilityMethods.FlushUnitsFromReinforcementPointsOnAllRelevantPlanets( faction, Context );

            HandleDroneMovement( faction, Context );

            HerdMiningGolems( faction, Context );

            faction.ExecuteWormholeCommands( Context );
            faction.ExecuteMovementCommands( Context );
        }
        private void HandleMothershipMovement( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            // If we've taken heavy damage, panic and run away.
            if ( Mothership.GetCurrentShieldPoints() < (Mothership.TypeData.GetForMark( 1 ).BaseShieldPoints / 4) * 3 && World_AIW2.Instance.GameSecond - Mothership.GameSecondEnteredThisPlanet > 30 )
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

            // If there is a Dark Spire generator on our current planet, wait until we finish chomping.
            if ( Mothership.Planet.GetFirstMatching( FactionType.SpecialFaction, "VengeanceGeneratorNormalSpawn", false, false ) != null )
                return;

            // If there is a high amount of strength near her territory, and shes mark 7, deal with it.
            if ( MothershipData.Level >= 7 )
            {
                List<Planet> threatenedPlanets = new List<Planet>();
                World_AIW2.Instance.DoForPlanets( false, planet =>
                {
                    if ( planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.ReturnNullIfNotFound )?.Type == DysonPerPlanetData.ProtoSphereType.Suppressor
                    || planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.ReturnNullIfNotFound )?.Type == DysonPerPlanetData.ProtoSphereType.Protecter
                    || DysonPrecursors.DysonNodes.GetHasKey( planet ) )
                        planet.DoForLinkedNeighborsAndSelf( false, adjPlanet =>
                        {
                            if ( adjPlanet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Hostile].TotalStrength > 3000000 )
                                threatenedPlanets.Add( adjPlanet );

                            return DelReturn.Continue;
                        } );

                    return DelReturn.Continue;
                } );

                if ( threatenedPlanets.Count > 0 )
                {
                    Planet targetPlanet = threatenedPlanets[0];
                    if ( threatenedPlanets.Count > 1 )
                        for ( int x = 0; x < threatenedPlanets.Count; x++ )
                            if ( Mothership.Planet.GetHopsTo( threatenedPlanets[x] ) < Mothership.Planet.GetHopsTo( targetPlanet ) )
                                targetPlanet = threatenedPlanets[x];

                    if ( Mothership.Planet != targetPlanet )
                        Mothership.QueueWormholeCommand( targetPlanet );

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
                    if ( MothershipData.Level >= 7 ||
                        ProtoSphereCosts.BuildCost( faction, false ) < PrecursorCosts.Resources( Mothership.CurrentMarkLevel, faction ) ||
                        MothershipData.Resources >= ProtoSphereCosts.BuildCost( faction, false ) )
                        HandleSphereBuildingMovement( faction, Context );
                    else
                        HandleCollectionAndNodeMovement( faction, Context );
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
        // Focus on collecting mines and building up our Node system.
        private void HandleCollectionAndNodeMovement( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
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
        private bool IsValidToBuildOn( Planet planet )
        {
            if ( planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.ReturnNullIfNotFound )?.Type != DysonPerPlanetData.ProtoSphereType.None )
                return false;
            return true;
        }
        // Handle movement for security and expansion.
        private void HandleSphereBuildingMovement( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            Planet endPlanet = null;
            // If we have enough resources to build a sphere, move right there.
            if ( MothershipData.Resources >= ProtoSphereCosts.BuildCost( faction, false ) )
            {
                Planet focusPlanet = MothershipData.PlanetToBuildOn;
                if ( focusPlanet != null && !IsValidToBuildOn( focusPlanet ) )
                    focusPlanet = null;
                if ( focusPlanet == null )
                {
                    // Find a planet with no sphere that we really hate or really like, and focus on it.
                    int bestValue = 1999;
                    bool firstTime = true;
                    World_AIW2.Instance.DoForPlanets( false, planet =>
                     {
                         int absTrust = Math.Abs( MothershipData.Trust.GetTrust( planet ) );
                         // Care about positive trust slightly more.
                         if ( MothershipData.Trust.GetTrust( planet ) >= 2000 )
                             absTrust += 100;
                         switch ( planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.ReturnNullIfNotFound )?.Type )
                         {
                             case DysonPerPlanetData.ProtoSphereType.None:
                                 if ( absTrust > bestValue )
                                 {
                                     focusPlanet = planet;
                                     bestValue = absTrust;
                                 }
                                 break;
                             case DysonPerPlanetData.ProtoSphereType.Protecter:
                             case DysonPerPlanetData.ProtoSphereType.Suppressor:
                                 firstTime = false;
                                 break;
                             default:
                                 break;
                         }

                         return DelReturn.Continue;
                     } );
                    // Failed to find a valid planet.
                    if ( focusPlanet == null && firstTime )
                    {
                        // If this is our first sphere, build it on our ancient node planet, if it exists.
                        if ( DysonNodes != null )
                            for ( int x = 0; x < DysonNodes.GetPairCount(); x++ )
                                for ( int y = 0; y < DysonNodes.GetPairByIndex( x ).Value.Length; y++ )
                                    if ( DysonNodes.GetPairByIndex( x ).Value[y] != null && DysonNodes.GetPairByIndex( x ).Value[y].TypeData.InternalName == DYSON_ANCIENT_NODE_NAME )
                                        focusPlanet = DysonNodes.GetPairByIndex( x ).Value[y].Planet;
                    }
                    if ( focusPlanet == null )
                    {
                        Planet basePlanet = MothershipData.Trust.GetNearbyTrustedPlanet( Mothership.Planet, Context, 2000 );
                        if ( ((basePlanet.GetPrecursorPerPlanetData( ExternalDataRetrieval.ReturnNullIfNotFound )?.Level) ?? 1) <= 0 )
                            focusPlanet = basePlanet;
                        else
                        {
                            int bestHop = 99;
                            World_AIW2.Instance.DoForPlanets( false, planet =>
                            {
                                if ( ((planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.ReturnNullIfNotFound )?.Level) ?? 0) > 0 )
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

                if ( planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.ReturnNullIfNotFound )?.Type != DysonPerPlanetData.ProtoSphereType.None || Mothership.Planet.Index == planet.Index )
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
                if ( Math.Abs( MothershipData.Trust.GetTrust( planet ) ) < 1000 && planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.ReturnNullIfNotFound )?.Type == DysonPerPlanetData.ProtoSphereType.None )
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

    public enum DysonStructure
    {
        DysonAntivirus,
        DysonFirewall,
        DysonRouter,
        DysonUplink,
        DysonDatabank
    }

    public class DysonPlanetaryStructures
    {
        public ArcenSparseLookup<GameEntity_Other, GameEntity_Squad> Antiviruses;
        public ArcenSparseLookup<GameEntity_Squad, GameEntity_Squad> FirewallsOnAntiviruses;
        public ArcenSparseLookup<GameEntity_Squad, GameEntity_Squad> FirewallsOnNodes;

        public ArcenSparseLookup<GameEntity_Squad, GameEntity_Squad> Routers;
        public ArcenSparseLookup<GameEntity_Squad, GameEntity_Squad> Uplinks;

        public ArcenSparseLookup<GameEntity_Other, GameEntity_Squad[]> Databanks;

        public DysonPlanetaryStructures()
        {
            Antiviruses = new ArcenSparseLookup<GameEntity_Other, GameEntity_Squad>();
            FirewallsOnAntiviruses = new ArcenSparseLookup<GameEntity_Squad, GameEntity_Squad>();
            FirewallsOnNodes = new ArcenSparseLookup<GameEntity_Squad, GameEntity_Squad>();
            Routers = new ArcenSparseLookup<GameEntity_Squad, GameEntity_Squad>();
            Uplinks = new ArcenSparseLookup<GameEntity_Squad, GameEntity_Squad>();
            Databanks = new ArcenSparseLookup<GameEntity_Other, GameEntity_Squad[]>();
        }
    }

    public abstract class BaseDysonSubfaction : BaseSpecialFaction, IBulkPathfinding
    {
        protected override bool EverNeedsToRunLongRangePlanning => false;

        public ArcenSparseLookup<Planet, DysonPlanetaryStructures> StructuresByPlanet;

        public abstract string SPHERE_TYPE_NAME { get; }

        // A list of Zenith factions that the Precursors, Suppressors, and Protectors should always be allied to.
        public static List<Faction> FactionsToAllyTo;
        public ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> WormholeCommands { get; set; }
        public ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> MovementCommands { get; set; }

        public BaseDysonSubfaction()
        {
            FactionsToAllyTo = null;
        }

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
                if ( planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    World_AIW2.Instance.QueueChatMessageOrCommand( $"{creator} on {planet.Name} has constructed a level {nodeMarkLevel} Dyson Node for {faction.StartFactionColourForLog()}{faction.GetDisplayName()}</color>.", ChatType.LogToCentralChat, Context );
            }
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
                planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Level = 0;
                planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Type = DysonPerPlanetData.ProtoSphereType.None;
                planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Resources = 0;
            }
        }

        public override void UpdatePlanetInfluence( Faction faction, ArcenSimContext Context )
        {
            faction.RemoveInfluenceFromAllPlanets();

            faction.DoForEntities( EntityRollupType.GrantsMinorFactionPlanetControl, controller =>
            {
                controller.Planet.AddFactionInfluencer( faction );

                return DelReturn.Continue;
            } );
        }

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( StructuresByPlanet == null )
                StructuresByPlanet = new ArcenSparseLookup<Planet, DysonPlanetaryStructures>();

            StructuresByPlanet.Clear();

            faction.DoForEntities( DysonStructure.DysonAntivirus.ToString(), antivirus =>
            {
                GameEntity_Other owner = World_AIW2.Instance.GetEntityByID_Other( antivirus.MinorFactionStackingID );
                if ( owner == null )
                {
                    antivirus.Die( Context, true );
                    return DelReturn.Continue;
                }

                if ( !StructuresByPlanet.GetHasKey( antivirus.Planet ) )
                    StructuresByPlanet.AddPair( antivirus.Planet, new DysonPlanetaryStructures() );

                if ( !StructuresByPlanet[antivirus.Planet].Antiviruses.GetHasKey( owner ) )
                    StructuresByPlanet[antivirus.Planet].Antiviruses.AddPair( owner, antivirus );

                return DelReturn.Continue;
            } );

            faction.DoForEntities( DysonStructure.DysonFirewall.ToString(), firewall =>
            {
                GameEntity_Squad owner = World_AIW2.Instance.GetEntityByID_Squad( firewall.MinorFactionStackingID );
                if ( owner == null )
                {
                    firewall.Die( Context, true );
                    return DelReturn.Continue;
                }

                if ( !StructuresByPlanet.GetHasKey( firewall.Planet ) )
                    StructuresByPlanet.AddPair( firewall.Planet, new DysonPlanetaryStructures() );

                if ( owner.TypeData.InternalName == DysonStructure.DysonAntivirus.ToString() )
                {
                    if ( !StructuresByPlanet[firewall.Planet].FirewallsOnAntiviruses.GetHasKey( owner ) )
                        StructuresByPlanet[firewall.Planet].FirewallsOnAntiviruses.AddPair( owner, firewall );
                }
                else
                {
                    if ( !StructuresByPlanet[firewall.Planet].FirewallsOnNodes.GetHasKey( owner ) )
                        StructuresByPlanet[firewall.Planet].FirewallsOnNodes.AddPair( owner, firewall );
                }



                return DelReturn.Continue;
            } );

            faction.DoForEntities( DysonStructure.DysonRouter.ToString(), router =>
            {
                GameEntity_Squad owner = World_AIW2.Instance.GetEntityByID_Squad( router.MinorFactionStackingID );
                if ( owner == null )
                {
                    router.Die( Context, true );
                    return DelReturn.Continue;
                }

                if ( !StructuresByPlanet.GetHasKey( router.Planet ) )
                    StructuresByPlanet.AddPair( router.Planet, new DysonPlanetaryStructures() );

                if ( !StructuresByPlanet[router.Planet].Routers.GetHasKey( owner ) )
                    StructuresByPlanet[router.Planet].Routers.AddPair( owner, router );

                return DelReturn.Continue;
            } );

            faction.DoForEntities( DysonStructure.DysonUplink.ToString(), uplink =>
            {
                GameEntity_Squad owner = World_AIW2.Instance.GetEntityByID_Squad( uplink.MinorFactionStackingID );
                if ( owner == null )
                {
                    uplink.Die( Context, true );
                    return DelReturn.Continue;
                }

                if ( !StructuresByPlanet.GetHasKey( uplink.Planet ) )
                    StructuresByPlanet.AddPair( uplink.Planet, new DysonPlanetaryStructures() );

                if ( !StructuresByPlanet[uplink.Planet].Uplinks.GetHasKey( owner ) )
                    StructuresByPlanet[uplink.Planet].Uplinks.AddPair( owner, uplink );

                return DelReturn.Continue;
            } );

            faction.DoForEntities( DysonStructure.DysonDatabank.ToString(), databank =>
            {
                GameEntity_Other owner = World_AIW2.Instance.GetEntityByID_Other( databank.MinorFactionStackingID );
                if ( owner == null )
                {
                    databank.Die( Context, true );
                    return DelReturn.Continue;
                }

                if ( !StructuresByPlanet.GetHasKey( databank.Planet ) )
                    StructuresByPlanet.AddPair( databank.Planet, new DysonPlanetaryStructures() );

                if ( !StructuresByPlanet[databank.Planet].Databanks.GetHasKey( owner ) )
                    StructuresByPlanet[databank.Planet].Databanks.AddPair( owner, new GameEntity_Squad[7] );

                StructuresByPlanet[databank.Planet].Databanks[owner][databank.CurrentMarkLevel - 1] = databank;

                return DelReturn.Continue;
            } );
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            Faction precFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonPrecursors ) );
            if ( faction.MustBeAwakenedByPlayer )
                faction.HasBeenAwakenedByPlayer = precFaction != null && (!precFaction.MustBeAwakenedByPlayer || precFaction.HasBeenAwakenedByPlayer);

            UpdateDysonAllegiance( faction, Context );
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            Faction precursorFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonPrecursors ) );
            if ( precursorFaction == null )
                return;

            HandlePacketMovement( faction, Context );

            SpawnNewStructuresOnEachNodedPlanet( faction, Context );

            faction.ExecuteMovementCommands( Context );
            faction.ExecuteWormholeCommands( Context );
        }

        public void HandlePacketMovement( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            List<GameEntity_Squad> packetsToMove = new List<GameEntity_Squad>();
            ArcenSparseLookup<Planet, int> packetsByPlanet = new ArcenSparseLookup<Planet, int>();

            faction.DoForEntities( delegate ( GameEntity_Squad entity )
            {
                if ( entity.TypeData.IsDrone )
                {
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
                }
                else if ( entity.TypeData.GetHasTag( DysonPrecursors.DYSON_PACKET_TAG ) )
                {
                    Planet effectivePlanet = entity.Planet;
                    int strength = entity.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength - entity.PlanetFaction.DataByStance[FactionStance.Hostile].CloakedStrength;
                    if ( entity.LongRangePlanningData.FinalDestinationPlanetIndex != -1 && entity.LongRangePlanningData.FinalDestinationPlanetIndex != entity.Planet.Index )
                        effectivePlanet = World_AIW2.Instance.GetPlanetByIndex( entity.LongRangePlanningData.FinalDestinationPlanetIndex );
                    else if ( strength < 500 )
                        packetsToMove.Add( entity );
                    if ( packetsByPlanet.GetHasKey( effectivePlanet ) )
                        packetsByPlanet[effectivePlanet] += entity.CurrentMarkLevel;
                    else
                        packetsByPlanet.AddPair( effectivePlanet, entity.CurrentMarkLevel );
                }

                return DelReturn.Continue;
            } );

            for ( int x = 0; x < packetsToMove.Count; x++ )
            {
                Planet bestPlanet = null;
                int bestPlanetPackets = 9999;
                bool bestPlanetHasHostiles = false;
                GameEntity_Squad packet = packetsToMove[x];
                if ( packet == null )
                    continue;
                packet.Planet.DoForLinkedNeighbors( false, planet =>
                {
                    if ( this is DysonSuppressors )
                    {
                        if ( planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.ReturnNullIfNotFound )?.Type == DysonPerPlanetData.ProtoSphereType.Protecter )
                            return DelReturn.Continue; // Do not path into Protector planets.

                        if ( planet.GetControllingFaction().Type == FactionType.Player && DysonPrecursors.MothershipData.Trust.GetTrust( planet ) > -500 )
                            return DelReturn.Continue; // Do not path into player planets unless they aggrevated our mothership.
                    }

                    bool workingPlanetHasHostiles = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Hostile].TotalStrength - planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Hostile].CloakedStrength > 2500;

                    if ( DysonPrecursors.Mothership != null && DysonPrecursors.Mothership.Planet == planet && workingPlanetHasHostiles )
                    {
                        // Our mothership is nearby and fighting, help her.
                        bestPlanet = planet;
                        return DelReturn.Break;
                    }

                    int workingPlanetPackets = packetsByPlanet.GetHasKey( planet ) ? packetsByPlanet[planet] : 0;

                    if ( bestPlanet == null )
                    {
                        bestPlanet = planet;
                        bestPlanetPackets = workingPlanetPackets;
                        if ( packetsByPlanet.GetHasKey( planet ) )
                            packetsByPlanet[planet]++;
                        else
                            packetsByPlanet.AddPair( planet, 1 );
                        bestPlanetHasHostiles = workingPlanetHasHostiles;
                    }
                    else
                    {
                        if ( workingPlanetHasHostiles && !bestPlanetHasHostiles ||
                        (!workingPlanetHasHostiles && !bestPlanetHasHostiles && workingPlanetPackets < bestPlanetPackets) ||
                        (workingPlanetHasHostiles && bestPlanetHasHostiles && workingPlanetPackets > bestPlanetPackets) )
                        {
                            bestPlanet = planet;
                            bestPlanetPackets = workingPlanetPackets;
                            if ( packetsByPlanet.GetHasKey( planet ) )
                                packetsByPlanet[planet]++;
                            else
                                packetsByPlanet.AddPair( planet, 1 );
                            bestPlanetHasHostiles = workingPlanetHasHostiles;
                        }
                    }

                    return DelReturn.Continue;
                } );

                packet.QueueWormholeCommand( bestPlanet );
            }
        }

        public void SpawnNewStructuresOnEachNodedPlanet( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( DysonPrecursors.DysonNodes == null || StructuresByPlanet == null )
                return;

            GameCommand build = Utilities.CreateGameCommand( DysonPrecursors.Commands.BuildPrecursorStructures.ToString(), GameCommandSource.AnythingElse, faction );

            int baseInterval = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DysonPrecursors_StructureSpawnInterval" );
            int interval = Utilities.GetScaledIntensityValue( World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonPrecursors ) ).Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity,
                baseInterval, baseInterval / 2 );

            DysonPrecursors.DysonNodes.DoFor( pair =>
            {
                if ( ((pair.Key.GetPrecursorPerPlanetData( ExternalDataRetrieval.ReturnNullIfNotFound )?.SecondsSinceLastStructureBuilt) ?? 0) < interval )
                    return DelReturn.Continue;

                for ( int x = 0; x < 7; x++ )
                {
                    GameEntity_Squad node = pair.Value[x];
                    if ( node == null )
                        continue;

                    if ( node.PlanetFaction.Faction != faction || node.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength > 10000 )
                        return DelReturn.Continue;
                }

                GameEntity_Squad sphere = pair.Key.GetPlanetFactionForFaction( faction ).Entities.GetFirstMatching( "ProtoSphere", false, false );

                ArcenSparseLookup<DysonStructure, List<GameEntity_Base>> options = new ArcenSparseLookup<DysonStructure, List<GameEntity_Base>>();
                ArcenSparseLookupPair<GameEntity_Other, int> nextDatabankWormhole = null;

                DysonPlanetaryStructures structures = StructuresByPlanet.GetHasKey( pair.Key ) ? StructuresByPlanet[pair.Key] : null;

                if ( sphere != null )
                {
                    if ( structures == null || !structures.FirewallsOnNodes.GetHasKey( sphere ) )
                        if ( !options.GetHasKey( DysonStructure.DysonFirewall ) )
                            options.AddPair( DysonStructure.DysonFirewall, new List<GameEntity_Base>() { sphere } );
                        else
                            options[DysonStructure.DysonFirewall].Add( sphere );

                    if ( structures == null || !structures.Routers.GetHasKey( sphere ) )
                        if ( !options.GetHasKey( DysonStructure.DysonRouter ) )
                            options.AddPair( DysonStructure.DysonRouter, new List<GameEntity_Base>() { sphere } );
                        else
                            options[DysonStructure.DysonRouter].Add( sphere );

                    if ( structures == null || !structures.Uplinks.GetHasKey( sphere ) )
                        if ( !options.GetHasKey( DysonStructure.DysonUplink ) )
                            options.AddPair( DysonStructure.DysonUplink, new List<GameEntity_Base>() { sphere } );
                        else
                            options[DysonStructure.DysonUplink].Add( sphere );
                }

                for ( int x = 0; x < 7; x++ )
                    pair.Key.DoForLinkedNeighbors( false, adjPlanet =>
                    {
                        GameEntity_Other wormhole = pair.Key.GetWormholeTo( adjPlanet );

                        if ( structures == null || !structures.Antiviruses.GetHasKey( wormhole ) )
                            if ( !options.GetHasKey( DysonStructure.DysonAntivirus ) )
                                options.AddPair( DysonStructure.DysonAntivirus, new List<GameEntity_Base>() { wormhole } );
                            else
                                options[DysonStructure.DysonAntivirus].Add( wormhole );

                        if ( pair.Value[x] != null && nextDatabankWormhole == null && (structures == null || !structures.Databanks.GetHasKey( wormhole ) || structures.Databanks[wormhole][x] == null) )
                            nextDatabankWormhole = new ArcenSparseLookupPair<GameEntity_Other, int>() { Key = wormhole, Value = x + 1 };

                        return DelReturn.Continue;
                    } );


                for ( int x = 0; x < 7; x++ )
                {
                    GameEntity_Squad node = pair.Value[x];
                    if ( node == null )
                        continue;

                    if ( structures == null || !structures.FirewallsOnNodes.GetHasKey( node ) )
                        if ( !options.GetHasKey( DysonStructure.DysonFirewall ) )
                            options.AddPair( DysonStructure.DysonFirewall, new List<GameEntity_Base>() { node } );
                        else
                            options[DysonStructure.DysonFirewall].Add( node );

                    if ( structures == null || !structures.Routers.GetHasKey( node ) )
                        if ( !options.GetHasKey( DysonStructure.DysonRouter ) )
                            options.AddPair( DysonStructure.DysonRouter, new List<GameEntity_Base>() { node } );
                        else
                            options[DysonStructure.DysonRouter].Add( node );

                    if ( structures == null || !structures.Uplinks.GetHasKey( node ) )
                        if ( !options.GetHasKey( DysonStructure.DysonUplink ) )
                            options.AddPair( DysonStructure.DysonUplink, new List<GameEntity_Base>() { node } );
                        else
                            options[DysonStructure.DysonUplink].Add( node );
                }


                pair.Key.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( DysonStructure.DysonAntivirus.ToString(), antivirus =>
                {
                    if ( structures == null || !structures.FirewallsOnAntiviruses.GetHasKey( antivirus ) )
                        if ( !options.GetHasKey( DysonStructure.DysonFirewall ) )
                            options.AddPair( DysonStructure.DysonFirewall, new List<GameEntity_Base>() { antivirus } );
                        else
                            options[DysonStructure.DysonFirewall].Add( antivirus );

                    return DelReturn.Continue;
                } );

                if ( options.GetPairCount() > 0 )
                {
                    int indexToUse = Math.Max( 0, Context.RandomToUse.Next( -1, options.GetPairCount() ) );
                    ArcenSparseLookupPair<DysonStructure, List<GameEntity_Base>> choice = options.GetPairByIndex( indexToUse );
                    indexToUse = Math.Max( 0, Context.RandomToUse.Next( -1, choice.Value.Count ) );
                    GameEntity_Base entity = choice.Value[indexToUse];

                    switch ( choice.Key )
                    {
                        case DysonStructure.DysonAntivirus:
                            build.RelatedBools.Add( true );
                            break;
                        default:
                            build.RelatedBools.Add( false );
                            break;
                    }

                    build.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    build.RelatedIntegers.Add( (byte)choice.Key );
                    build.RelatedIntegers2.Add( 1 );
                }
                else if ( nextDatabankWormhole != null )
                {
                    build.RelatedBools.Add( true );
                    build.RelatedEntityIDs.Add( nextDatabankWormhole.Key.PrimaryKeyID );
                    build.RelatedIntegers.Add( (byte)DysonStructure.DysonDatabank );
                    build.RelatedIntegers2.Add( nextDatabankWormhole.Value );
                }

                return DelReturn.Continue;
            } );

            if ( build.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( build );
        }

        public void UpdateDysonAllegiance( Faction faction, ArcenSimContext Context )
        {
            Faction precursorFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( DysonPrecursors ) );
            if ( precursorFaction == null )
                return;
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
            FactionsToAllyTo = new List<Faction>();
            // Ally ourselves (and our mothership) to other dyson/zenith factions.
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction.Implementation is SpecialFaction_Devourer || otherFaction.Implementation is SpecialFaction_ZenithTraitor || otherFaction.GetDisplayName().ToLower().Contains( "devourer" ) ||
                    ((otherFaction.GetDisplayName().ToLower().Contains( "dyson" ) || otherFaction.GetDisplayName().ToLower().Contains( "zenith" )) && !otherFaction.GetDisplayName().ToLower().Contains( "dark" )) )
                {
                    FactionsToAllyTo.Add( otherFaction );
                }
            }
        }
        public void SpawnDronesOnNodeOrPacketDeath( GameEntity_Squad nodeOrPacket, Faction faction, ArcenSimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

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
                         GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, spawnData, nodeOrPacket.CurrentMarkLevel, pFaction.FleetUsedAtPlanet, 0, nodeOrPacket.WorldLocation, Context ).Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
                 }

                 return DelReturn.Continue;
             } );
        }
        public void CreateProtoSphere( Faction faction, Planet planet, ArcenSimContext Context )
        {
            GameEntityTypeData protoSphereData = GameEntityTypeDataTable.Instance.GetRowByName( SPHERE_TYPE_NAME + "1" );
            planet.Mapgen_SeedEntity( Context, faction, protoSphereData, PlanetSeedingZone.OuterSystem );
            planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Level = 1;
            planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Type = DysonPerPlanetData.ProtoSphereType.Suppressor;
            if ( planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                World_AIW2.Instance.QueueChatMessageOrCommand( $"A Dyson Mothership on {planet.Name} has constructed a {protoSphereData.DisplayName}.", ChatType.LogToCentralChat, Context );
        }
        public void UpgradeProtoSphere( Faction faction, Planet planet, ArcenSimContext Context )
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
                GameEntityTypeData protoSphereData = GameEntityTypeDataTable.Instance.GetRowByName( SPHERE_TYPE_NAME + (planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Level + 1) );
                protoSphere = protoSphere.TransformInto( Context, protoSphereData, 1 );
                planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Level++;
                planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Resources = 0;
                if ( planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Level >= 7 )
                    World_AIW2.Instance.QueueChatMessageOrCommand( $"The {protoSphere.TypeData.DisplayName} on {planet.Name} has reached its maximum level.", ChatType.LogToCentralChat, Context );
                else
                    World_AIW2.Instance.QueueChatMessageOrCommand( $"The {protoSphere.TypeData.DisplayName} on {planet.Name} has leveled up to level {planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Level} .", ChatType.LogToCentralChat, Context );
            }
        }
    }
    public class DysonSuppressors : BaseDysonSubfaction
    {
        protected override string TracingName => "DysonSuppressors";
        protected override bool EverNeedsToRunLongRangePlanning => true;
        protected override int MinimumSecondsBetweenLongRangePlannings => 5;

        public override string SPHERE_TYPE_NAME => "DysonProtoSuppressorSphere";

        public override void UpdatePowerLevel( Faction faction )
        {
            faction.OverallPowerLevel = FInt.Zero;
            if ( DysonPrecursors.MothershipData == null )
                return;

            FInt fromMothership = FInt.Zero, fromFaction = FInt.Zero;

            if ( DysonPrecursors.MothershipData.Level < 6 )
            {
                for ( int x = 0; x < DysonPrecursors.MothershipData.Level; x++ )
                    fromMothership += FInt.FromParts( 0, 334 );
            }
            else
                fromMothership = FInt.FromParts( 2, 000 );

            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                if ( planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Type == DysonPerPlanetData.ProtoSphereType.Suppressor )
                    fromFaction += FInt.FromParts( 0, 250 ) * planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Level;

                if ( DysonPrecursors.MothershipData.Trust.GetTrust( planet ) < 0 && DysonPrecursors.DysonNodes.GetHasKey( planet ) )
                    for ( int x = 0; x < 7; x++ )
                        if ( DysonPrecursors.DysonNodes[planet][x] != null )
                            fromFaction += FInt.FromParts( 0, 005 ) * (x + 1);

                return DelReturn.Continue;
            } );

            if ( fromFaction > 0 )
                faction.OverallPowerLevel = fromMothership + fromFaction;

            if ( faction.OverallPowerLevel > 5 )
                faction.OverallPowerLevel = FInt.FromParts( 5, 000 );
        }

        public override bool GetShouldAttackNormallyExcludedTarget( Faction faction, GameEntity_Squad Target )
        {
            if ( Target.TypeData.GetHasTag( "NormalPlanetNastyPick" ) )
                return true;
            if ( Target.Planet.UnderInfluenceOfFactionIndex.Contains( faction.FactionIndex ) &&
                (Target.PlanetFaction.Faction.Type == FactionType.SpecialFaction || Target.TypeData.IsCommandStation) )
                return true;
            return false;
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

            if ( protoSphere != null && protoSphere.GetSecondsSinceCreation() % 300 == 0 )
            {
                for ( int x = 0; x < planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Level; x++ )
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
                if ( planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    World_AIW2.Instance.QueueChatMessageOrCommand( $"{creator} on {planet.Name} has constructed a level {nodeMarkLevel} Dyson Node.", ChatType.LogToCentralChat, Context );
        }
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            enemyThisFactionToAll( faction );
            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
            if ( DysonPrecursors.Sleeping )
                allyThisFactionToEveryoneButPlayers( faction );
        }

        public override void DoOnAnyDeathLogic( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenSimContext Context )
        {
            base.DoOnAnyDeathLogic( entity, Damage, FiringSystemOrNull, Context );
            if ( entity.TypeData.GetHasTag( "ProtoSphere" ) )
            {
                DysonPerPlanetData data = entity.Planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound );
                data.Level = 0;
                data.Resources = 0;
                data.Type = DysonPerPlanetData.ProtoSphereType.None;

                World_AIW2.Instance.QueueChatMessageOrCommand( $"A Proto Suppressor Sphere Golem on {entity.Planet.Name} has been destroyed.", ChatType.LogToCentralChat, Context );
            }
            else if ( entity.TypeData.GetHasTag( DysonPrecursors.DYSON_NODE_NAME ) )
            {
                World_AIW2.Instance.DoForFactions( faction =>
                    {
                        if ( faction.Type == FactionType.Player && entity.Planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Self].TotalStrength > 100 )
                        {
                            faction.StoredMetal += entity.TypeData.MetalCost;

                            if ( entity.TypeData.GetHasTag( DysonPrecursors.DYSON_ANCIENT_NODE_NAME ) )
                                faction.StoredHacking += 10;
                            else
                                faction.StoredHacking += entity.CurrentMarkLevel;
                        }

                        return DelReturn.Continue;
                    } );
                SpawnDronesOnNodeOrPacketDeath( entity, entity.PlanetFaction.Faction, Context );
            }
        }
    }

    public class DysonProtectors : BaseDysonSubfaction
    {
        protected override string TracingName => "DysonProtectors";
        protected override bool EverNeedsToRunLongRangePlanning => true;
        protected override int MinimumSecondsBetweenLongRangePlannings => 5;

        public override string SPHERE_TYPE_NAME => "DysonProtoProtectorSphere";
        public override void UpdatePowerLevel( Faction faction )
        {
            faction.OverallPowerLevel = FInt.Zero;
            if ( DysonPrecursors.MothershipData == null || DysonPrecursors.DysonNodes == null )
                return;

            FInt fromFaction = FInt.Zero;

            World_AIW2.Instance.DoForPlanets( false, planet =>
             {
                 if ( planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Type == DysonPerPlanetData.ProtoSphereType.Protecter )
                     fromFaction += FInt.FromParts( 0, 150 ) * planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Level;

                 if ( DysonPrecursors.MothershipData.Trust.GetTrust( planet ) > 0 && DysonPrecursors.DysonNodes.GetHasKey( planet ) )
                     for ( int x = 0; x < 7; x++ )
                         if ( DysonPrecursors.DysonNodes[planet][x] != null )
                             fromFaction += FInt.FromParts( 0, 005 ) * (x + 1);

                 return DelReturn.Continue;
             } );

            if ( fromFaction > 0 )
                faction.OverallPowerLevel = fromFaction;

            if ( faction.OverallPowerLevel > 3 )
                faction.OverallPowerLevel = FInt.FromParts( 3, 000 );
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
                for ( int x = 0; x < planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound ).Level; x++ )
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
                if ( planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    World_AIW2.Instance.QueueChatMessageOrCommand( $"{creator} on {planet.Name} has constructed a level {nodeMarkLevel} Dyson Node.", ChatType.LogToCentralChat, Context );
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            allyThisFactionToHumans( faction );

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );

        }
        public override void DoOnAnyDeathLogic( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenSimContext Context )
        {
            base.DoOnAnyDeathLogic( entity, Damage, FiringSystemOrNull, Context );
            if ( entity.TypeData.GetHasTag( "ProtoSphere" ) )
            {
                DysonPerPlanetData data = entity.Planet.GetPrecursorPerPlanetData( ExternalDataRetrieval.CreateIfNotFound );

                if ( entity.Planet.GetControllingOrInfluencingFaction().Type == FactionType.Player )
                {
                    GameEntity_Squad command = entity.Planet.GetCommandStationOrNull();
                    if ( command != null )
                    {
                        for ( int x = 1; x <= data.Level; x++ )
                            command.FleetMembership.Fleet.GetButDoNotAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( GameEntityTypeDataTable.Instance.GetRowByName( DysonPrecursors.DYSON_PACKET_TAG + x ) ).ExplicitBaseSquadCap = 0;
                    }
                }

                data.Level = 0;
                data.Resources = 0;
                data.Type = DysonPerPlanetData.ProtoSphereType.None;

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
        }
        public override void DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips( ref int debugStage, GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, Faction factionThatKilledEntity, Faction entityOwningFaction, int numExtraStacksKilled, ArcenSimContext Context )
        {
            if ( entity.TypeData.GetHasTag( DysonPrecursors.DYSON_PACKET_TAG ) )
                SpawnDronesOnNodeOrPacketDeath( entity, entity.PlanetFaction.Faction, Context );
        }
    }
}
