using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using UnityEngine;
using Utilities;

namespace Discordians
{
    enum CommandType
    {
        none,
        spawn,
        target,
        status,
        unlock,
        help
    }

    public class DiscordianDescriptionAppender : IGameEntityDescriptionAppender
    {
        public void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenDoubleCharacterBuffer Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;

            // Load the viewer's data. Stop if not found.
            DiscordianData ddata = RelatedEntityOrNull.GetDiscordianData( ExternalDataRetrieval.ReturnNullIfNotFound );
            if ( ddata == null )
                return;
            Buffer.Add( "This force is lead by " ).StartColor( Color.green ).Add( ddata.UserName ).EndColor().Add( " " );
            int strength = 0;
            RelatedEntityOrNull.PlanetFaction.Faction.DoForEntities( ( GameEntity_Squad entity ) =>
            {
                if ( entity.MinorFactionStackingID == RelatedEntityOrNull.PrimaryKeyID )
                    strength += entity.GetStrengthOfSelfAndContents();

                return DelReturn.Continue;
            } );
            Buffer.Add( "and currently has " ).StartColor( Color.red ).Add( (FInt.FromParts( strength, 000 ) / 1000).ReadableString ).EndColor().Add( " strength worth of forces. " );
            if ( ddata.TargetPlanet != null && ddata.TargetPlanet != RelatedEntityOrNull.Planet )
                Buffer.Add( $" Its current destination is {ddata.TargetPlanet}. " );

            Buffer.Add( $"They have {ddata.Experience}/{Discordians.GetExperienceForLevelUp( RelatedEntityOrNull.PlanetFaction.Faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.ReturnNullIfNotFound )?.Intensity ?? 0, RelatedEntityOrNull.CurrentMarkLevel )} experience " );
            Buffer.Add( $"and {ddata.Research} research points." );

            return;
        }
    }

    public class Discordians : BaseSpecialFaction, IBulkPathfinding
    {
        protected override string TracingName => "Discordians";
        protected override bool EverNeedsToRunLongRangePlanning => true;
        protected override int MinimumSecondsBetweenLongRangePlannings => 2;

        public ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> WormholeCommands { get; set; }
        public ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> MovementCommands { get; set; }

        private static readonly string DATA_FOLDER = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), "HRFBot/Discordians" );

        private static readonly string BOT_OUTPUT_FLAG = Path.Combine( DATA_FOLDER, "BotOutputReady" );
        private static readonly string BOT_OUTPUT_FILE = Path.Combine( DATA_FOLDER, "BotOutput.xml" );

        private static readonly string GAME_OUTPUT_FLAG = Path.Combine( DATA_FOLDER, "GameOutputReady" );
        private static readonly string GAME_OUTPUT_FILE = Path.Combine( DATA_FOLDER, "GameOutput.xml" );

        private static readonly string GAME_RULES_FLAG = Path.Combine( DATA_FOLDER, "GameRulesReady" );
        private static readonly string GAME_RULES_FILE = Path.Combine( DATA_FOLDER, "GameRules.xml" );

        private static readonly string GAME_MAP_FLAG = Path.Combine( DATA_FOLDER, "GameMapReady" );
        private static readonly string GAME_MAP_FILE = Path.Combine( DATA_FOLDER, "GameMap.xml" );

        public static int GetCostMultiplier( int intensity )
        {
            int min = 20, max = 15;
            return Utilities.GetScaledIntensityValue( intensity, min, max );
        }

        public static int GetExperienceForLevelUp( int intensity, int currentMarkLevel )
        {
            int min = 250 * intensity * currentMarkLevel, max = 100 * intensity * currentMarkLevel;
            return Utilities.GetScaledIntensityValue( intensity, min, max );
        }

        public static int GetStrengthPerMark1Wing( int intensity )
        {
            return Utilities.GetScaledIntensityValue( intensity, 500, 1000 );
        }

        public override void UpdatePowerLevel( Faction faction )
        {
            faction.OverallPowerLevel = FInt.Zero;
        }

        public ArcenSparseLookup<GameEntityTypeData, byte> ShipCaps;

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( ShipCaps == null )
                ShipCaps = new ArcenSparseLookup<GameEntityTypeData, byte>();
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Allegiance = "Friendly To Players";
            allyThisFactionToHumans( faction );

            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            if ( ShipCaps == null )
                return;

            int intensity = faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.ReturnNullIfNotFound )?.Intensity ?? 0;

            faction.DoForEntities( "DiscordianActive", leader =>
            {
                DiscordianData dData = leader.GetDiscordianData( ExternalDataRetrieval.CreateIfNotFound );
                if ( leader.CurrentMarkLevel < 7 )
                    dData.Experience++;

                if ( leader.RepairDelaySeconds > 10 )
                    leader.RepairDelaySeconds = 10;

                if ( dData.Experience > GetExperienceForLevelUp( intensity, leader.CurrentMarkLevel ) )
                {
                    dData.Experience -= GetExperienceForLevelUp( intensity, leader.CurrentMarkLevel );
                    leader.SetCurrentMarkLevelIfHigherThanCurrent( (byte)(leader.CurrentMarkLevel + 1), Context );
                }

                dData.Research += leader.CurrentMarkLevel;

                dData.ShipTypes.DoFor( pair =>
                {
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( pair.Key );

                    int baseCost;

                    if ( entityData.IsStrikecraft )
                        baseCost = entityData.GetForMark( 1 ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                    else
                        baseCost = entityData.GetForMark( 1 ).StrengthPerSquad_CalculatedWithNullFleetMembership / 10;

                    int cost = Utilities.GetScaledIntensityValue( intensity, baseCost / 2, baseCost / 5 );
                    if ( World_AIW2.Instance.GameSecond % cost != 0 )
                        return DelReturn.Continue;

                    int capPer = GetStrengthPerMark1Wing( intensity ) / entityData.GetForMark( 1 ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                    int cap = pair.Value * capPer;

                    int count = 0;
                    faction.DoForEntities( entityData, entity =>
                    {
                        if ( entity.MinorFactionStackingID == leader.PrimaryKeyID )
                        {
                            count += (1 + entity.ExtraStackedSquadsInThis);
                            entity.SetCurrentMarkLevelIfHigherThanCurrent( leader.CurrentMarkLevel, Context );
                        }

                        return DelReturn.Continue;
                    } );

                    if ( count < cap )
                    {
                        GameEntity_Squad strikecraft = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( leader.PlanetFaction, entityData, leader.CurrentMarkLevel,
                            leader.PlanetFaction.FleetUsedAtPlanet, 0, leader.WorldLocation, Context );

                        strikecraft.MinorFactionStackingID = leader.PrimaryKeyID;
                        strikecraft.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, strikecraft.PlanetFaction.Faction.FactionIndex );
                        if ( pair.Value > 1 )
                            strikecraft.AddOrSetExtraStackedSquadsInThis( (short)(pair.Value - 1), false );
                    }

                    return DelReturn.Continue;
                } );

                return DelReturn.Continue;
            } );

            faction.DoForEntities( "DiscordianBroken", leader =>
            {
                if ( leader.Planet.GetCommandStationOrNull()?.TypeData.IsKingUnit ?? false )
                    if ( leader.WorldLocation.GetDistanceTo( leader.Planet.GetCommandStationOrNull().WorldLocation, false ) <= 5000 )
                    {
                        DiscordianData ddata = leader.GetDiscordianData( ExternalDataRetrieval.CreateIfNotFound );
                        GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( leader.PlanetFaction,
                            GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DiscordianActive" ),
                            leader.CurrentMarkLevel, leader.PlanetFaction.FleetUsedAtPlanet, 0, leader.WorldLocation, Context );

                        faction.DoForEntities( ( GameEntity_Squad strikecraft ) =>
                        {
                            if ( strikecraft.MinorFactionStackingID == leader.PrimaryKeyID )
                                strikecraft.MinorFactionStackingID = newEntity.PrimaryKeyID;

                            return DelReturn.Continue;
                        } );

                        newEntity.SetDiscordianData( ddata );
                        newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );

                        leader.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
                        return DelReturn.Break;
                    }

                return DelReturn.Continue;
            } );
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            if ( ShipCaps == null )
                return;

            if ( !Directory.Exists( DATA_FOLDER ) )
                Directory.CreateDirectory( DATA_FOLDER );

            try
            {
                if ( !File.Exists( GAME_MAP_FLAG ) )
                    OutputGalaxyMap( faction );
            }
            catch ( Exception ) { }


            ArcenSparseLookup<GameEntityTypeData, byte> workingShipCaps = new ArcenSparseLookup<GameEntityTypeData, byte>();

            World_AIW2.Instance.DoForFleets( FleetStatus.AnyStatus, fleet =>
            {
                if ( fleet.Faction.Type != FactionType.Player )
                    return DelReturn.Continue;

                fleet.DoForMemberGroups( mem =>
                {
                    if ( mem.TypeData.IsDrone )
                        return DelReturn.Continue;

                    if ( mem.TypeData.IsStrikecraft ) 
                        if ( workingShipCaps.GetHasKey( mem.TypeData ) )
                            workingShipCaps[mem.TypeData] += 5;
                        else
                            workingShipCaps.AddPair( mem.TypeData, 5 );
                    else if ( mem.TypeData.SpecialType == SpecialEntityType.Frigate && mem.TypeData.CanGoThroughWormholes )
                    {
                        if ( workingShipCaps.GetHasKey( mem.TypeData ) )
                            workingShipCaps[mem.TypeData] = 1;
                        else
                            workingShipCaps.AddPair( mem.TypeData, 1 );
                    }


                    return DelReturn.Continue;
                } );

                return DelReturn.Continue;
            } );

            workingShipCaps.DoFor( pair =>
            {
                if ( ShipCaps.GetHasKey( pair.Key ) && pair.Value < ShipCaps[pair.Key] )
                    return DelReturn.Continue;

                GameCommand command = Utilities.CreateGameCommand( "UpdateDiscordianShipCapsCommand", GameCommandSource.AnythingElse, faction );
                command.RelatedString = pair.Key.InternalName;
                command.RelatedIntegers.Add( pair.Value );
                Context.QueueCommandForSendingAtEndOfContext( command );

                return DelReturn.Continue;
            } );

            GameCommand speed = Utilities.CreateGameCommand( "CreateSpeedGroup_FireteamAttack", GameCommandSource.AnythingElse, faction );
            speed.RelatedBool = true;
            speed.RelatedIntegers.Add( 1200 );
            GameCommand endSpeed = Utilities.CreateGameCommand( "CreateSpeedGroup_Destroy", GameCommandSource.AnythingElse, faction );
            endSpeed.RelatedString = "DestroySpeedGroups";

            List<GameEntity_Squad> leaders = new List<GameEntity_Squad>();

            faction.DoForEntities( "DiscordianLeader", leader =>
            {
                leaders.Add( leader );

                DiscordianData dData = leader.GetDiscordianData( ExternalDataRetrieval.CreateIfNotFound );

                Planet targetPlanet = dData.TargetPlanet;

                faction.DoForEntities( ( GameEntity_Squad strikecraft ) =>
                {
                    if ( strikecraft.MinorFactionStackingID != leader.PrimaryKeyID )
                        return DelReturn.Continue;

                    if ( strikecraft.Planet != targetPlanet )
                    {
                        strikecraft.QueueWormholeCommand( targetPlanet, Context, false, PathingMode.Safest );
                        if ( strikecraft.GroupMoveSpeed == null )
                            speed.RelatedEntityIDs.Add( strikecraft.PrimaryKeyID );
                    }
                    else if ( strikecraft.GroupMoveSpeed != null )
                        endSpeed.RelatedEntityIDs.Add( strikecraft.PrimaryKeyID );

                    return DelReturn.Continue;
                } );

                return DelReturn.Continue;
            } );

            int intensity = faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.ReturnNullIfNotFound )?.Intensity ?? 0;

            faction.DoForEntities( "DiscordianActive", leader =>
            {
                DiscordianData dData = leader.GetDiscordianData( ExternalDataRetrieval.CreateIfNotFound );

                Planet targetPlanet = dData.TargetPlanet;

                if ( targetPlanet != null )
                {
                    if ( leader.Planet != targetPlanet )
                    {
                        leader.QueueWormholeCommand( targetPlanet, Context, false, PathingMode.Safest );
                        if ( leader.GroupMoveSpeed == null )
                            speed.RelatedEntityIDs.Add( leader.PrimaryKeyID );
                    }
                    else if ( leader.GroupMoveSpeed != null )
                        endSpeed.RelatedEntityIDs.Add( leader.PrimaryKeyID );
                }

                return DelReturn.Continue;
            } );

            faction.DoForEntities( "DiscordianBroken", leader =>
            {
                GameEntity_Squad nearestKing = null;

                List<GameEntity_Squad> kings = FactionUtilityMethods.findAllHumanKings();

                for ( int x = 0; x < kings.Count; x++ )
                    if ( nearestKing == null || kings[x].Planet.GetHopsTo( leader.Planet ) < nearestKing.Planet.GetHopsTo( leader.Planet ) )
                        nearestKing = kings[x];

                if ( leader.Planet != nearestKing.Planet )
                    leader.QueueWormholeCommand( nearestKing.Planet, Context, false, PathingMode.Shortest );
                else
                    leader.QueueMovementCommand( nearestKing.WorldLocation );

                if ( leader.GroupMoveSpeed != null )
                    endSpeed.RelatedEntityIDs.Add( leader.PrimaryKeyID );

                return DelReturn.Continue;
            } );

            if ( speed.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( speed );
            if ( endSpeed.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( endSpeed );

            try
            {
                if ( File.Exists( BOT_OUTPUT_FLAG ) )
                {
                    XmlDocument botXML = new XmlDocument();
                    Stream fileStream = File.Open( BOT_OUTPUT_FILE, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite );
                    botXML.Load( fileStream );
                    foreach ( XmlElement element in botXML.GetElementsByTagName( "PlayerCommand" ) )
                    {
                        if ( !Enum.TryParse( element.GetAttribute( "Type" ), true, out CommandType commandType ) )
                            continue;

                        string ID = element.GetAttribute( "ID" );
                        string Username = element.GetAttribute( "Username" );
                        string Primary = element.GetAttribute( "PrimaryArguement" );
                        string Secondary = element.GetAttribute( "SecondaryArguement" );

                        GameEntity_Squad discordian = leaders.Find( ( x ) => (x.GetDiscordianData( ExternalDataRetrieval.ReturnNullIfNotFound )?.ID ?? "-1") == ID );

                        switch ( commandType )
                        {
                            case CommandType.spawn:
                                if ( discordian == null && ShipCaps != null && ShipCaps.GetPairCount() > 0 )
                                {
                                    GameCommand command = Utilities.CreateGameCommand( "ExecuteDiscordianSpawnCommand", GameCommandSource.AnythingElse, faction );
                                    command.RelatedString = ID;
                                    command.RelatedString2 = Username;
                                    command.RelatedBool = true;
                                    Context.QueueCommandForSendingAtEndOfContext( command );
                                }
                                break;
                            case CommandType.target:
                                if ( discordian != null )
                                {
                                    GameCommand command = Utilities.CreateGameCommand( "ExecuteDiscordianTargetCommand", GameCommandSource.AnythingElse, faction );
                                    command.RelatedEntityIDs.Add( discordian.PrimaryKeyID );
                                    command.RelatedString = Primary;
                                    Context.QueueCommandForSendingAtEndOfContext( command );
                                }
                                break;
                            case CommandType.unlock:
                                if ( discordian != null )
                                {
                                    GameCommand command = Utilities.CreateGameCommand( "ExecuteDiscordianUnlockCommand", GameCommandSource.AnythingElse, faction );
                                    command.RelatedEntityIDs.Add( discordian.PrimaryKeyID );
                                    command.RelatedString = Primary;
                                    Context.QueueCommandForSendingAtEndOfContext( command );
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    File.Delete( BOT_OUTPUT_FLAG );
                }
            }
            catch ( Exception )
            {
                try
                {
                    File.Delete( BOT_OUTPUT_FILE );
                    File.Delete( BOT_OUTPUT_FLAG );
                }
                catch ( Exception )
                {

                }
            }
            try
            {
                if ( !File.Exists( GAME_OUTPUT_FLAG ) && leaders.Count > 0 )
                {
                    Stream fileStream = File.Open( GAME_OUTPUT_FILE, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite );
                    XmlWriter w = XmlWriter.Create( fileStream );
                    w.WriteStartDocument();
                    w.WriteStartElement( "root" );
                    for ( int x = 0; x < leaders.Count; x++ )
                    {
                        GameEntity_Squad leader = leaders[x];
                        DiscordianData dData = leader.GetDiscordianData( ExternalDataRetrieval.ReturnNullIfNotFound );
                        if ( dData == null )
                            continue;

                        w.WriteStartElement( "PlayerData" );
                        w.WriteAttributeString( "ID", dData.ID );

                        w.WriteAttributeString( "CapitalShipStrength", leader.GetStrengthPerSquad().ToString() );
                        int strength = 0;
                        faction.DoForEntities( ( GameEntity_Squad entity ) =>
                        {
                            if ( entity.MinorFactionStackingID == leader.PrimaryKeyID )
                                strength += entity.GetStrengthOfSelfAndContents();

                            return DelReturn.Continue;
                        } );
                        w.WriteAttributeString( "FleetStrength", strength.ToString() );
                        w.WriteAttributeString( "ShipHullMax", leader.GetMaxHullPoints().ToString() );
                        w.WriteAttributeString( "ShipHull", leader.GetCurrentHullPoints().ToString() );
                        w.WriteAttributeString( "Level", leader.CurrentMarkLevel.ToString() );
                        w.WriteAttributeString( "Experience", dData.Experience.ToString() );
                        w.WriteAttributeString( "ExperienceForNextLevel", GetExperienceForLevelUp( intensity, leader.CurrentMarkLevel ).ToString() );
                        w.WriteAttributeString( "Research", dData.Research.ToString() );
                        w.WriteAttributeString( "CurrentPlanet", leader.Planet.Name );
                        w.WriteAttributeString( "TargetPlanet", dData.TargetPlanet.Name );
                        string status = leader.TypeData.GetHasTag( "DiscordianActive" ) ? "Enabled" : "Disabled";
                        w.WriteAttributeString( "Status", status );
                        dData.WriteShips( w );
                        w.WriteEndElement();
                    }
                    w.WriteEndDocument();
                    w.Close();
                    File.Create( GAME_OUTPUT_FLAG );
                }
            }
            catch ( Exception e )
            {
                try
                {
                    ArcenDebugging.LogException( e );
                    File.Delete( GAME_OUTPUT_FILE );
                    File.Delete( GAME_OUTPUT_FLAG );
                }
                catch ( Exception )
                {

                }
            }

            try
            {
                if ( !File.Exists( GAME_RULES_FLAG ) )
                {
                    Stream fileStream = File.Open( GAME_RULES_FILE, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite );
                    XmlWriter w = XmlWriter.Create( fileStream );
                    w.WriteStartDocument();
                    w.WriteStartElement( "root" );
                    World_AIW2.Instance.DoForPlanets( false, planet =>
                    {
                        w.WriteStartElement( "GameRule" );
                        w.WriteAttributeString( "Type", "Planet" );
                        w.WriteAttributeString( "Name", planet.Name );
                        w.WriteEndElement();

                        return DelReturn.Continue;
                    } );
                    ShipCaps.DoFor( pair =>
                    {
                        w.WriteStartElement( "GameRule" );
                        w.WriteAttributeString( "Type", "Ship" );
                        w.WriteAttributeString( "Name", pair.Key.InternalName );
                        w.WriteAttributeString( "Capacity", pair.Value.ToString() );

                        int baseCost;

                        if ( pair.Key.IsStrikecraft )
                            baseCost = pair.Key.GetForMark( 1 ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                        else
                            baseCost = pair.Key.GetForMark( 1 ).StrengthPerSquad_CalculatedWithNullFleetMembership / 10;

                        int cost = baseCost * Discordians.GetCostMultiplier( intensity );
                        w.WriteAttributeString( "Cost", cost.ToString() );
                        w.WriteEndElement();

                        return DelReturn.Continue;
                    } );
                    w.WriteEndDocument();
                    w.Close();
                    File.Create( GAME_RULES_FLAG );
                }
            }
            catch ( Exception )
            {
                try
                {
                    File.Delete( GAME_RULES_FILE );
                    File.Delete( GAME_RULES_FLAG );
                }
                catch ( Exception )
                {

                }
            }
            faction.ExecuteWormholeCommands( Context );
            faction.ExecuteMovementCommands( Context );
        }

        // Help
        public void OutputGalaxyMap( Faction faction )
        {
            int minX = 0, minY = 0, maxX = 0, maxY = 0;
            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                if ( planet.GalaxyLocation.X < minX )
                    minX = planet.GalaxyLocation.X;
                if ( planet.GalaxyLocation.X > maxX )
                    maxX = planet.GalaxyLocation.X;
                if ( planet.GalaxyLocation.Y < minY )
                    minY = planet.GalaxyLocation.Y;
                if ( planet.GalaxyLocation.Y > maxY )
                    maxY = planet.GalaxyLocation.Y;

                return DelReturn.Continue;
            } );

            int xOffset = Math.Max( 0, -minX ) + 250;
            int yOffset = Math.Max( 0, -minY ) + 250;

            Stream fileStream = File.Open( GAME_MAP_FILE, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite );
            XmlWriter w = XmlWriter.Create( fileStream );
            w.WriteStartDocument();
            w.WriteStartElement( "root" );
            w.WriteStartElement( "Galaxy" );
            w.WriteAttributeString( "X", (maxX + xOffset + 250).ToString() );
            w.WriteAttributeString( "Y", (maxY + yOffset + 250).ToString() );
            w.WriteEndElement();

            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                int x = planet.GalaxyLocation.X + xOffset, y = planet.GalaxyLocation.Y + yOffset;

                Color32 color = planet.GetControllingOrInfluencingFaction().FactionCenterColor.TeamColor;

                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );

                int friendlyStrength = (pFaction.DataByStance[FactionStance.Self].TotalStrength + pFaction.DataByStance[FactionStance.Friendly].TotalStrength) / 1000;
                int hostileStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength / 1000;

                w.WriteStartElement( "Planet" );

                w.WriteAttributeString( "Name", planet.Name );
                w.WriteAttributeString( "Owner", planet.GetControllingOrInfluencingFaction().GetDisplayName() );
                w.WriteAttributeString( "FriendlyStrength", friendlyStrength.ToString() );
                w.WriteAttributeString( "HostileStrength", hostileStrength.ToString() );
                w.WriteAttributeString( "X", x.ToString() );
                w.WriteAttributeString( "Y", y.ToString() );
                w.WriteAttributeString( "A", color.a.ToString() );
                w.WriteAttributeString( "R", color.r.ToString() );
                w.WriteAttributeString( "G", color.g.ToString() );
                w.WriteAttributeString( "B", color.b.ToString() );

                planet.DoForLinkedNeighbors( false, adjPlanet =>
                {
                    x = adjPlanet.GalaxyLocation.X + xOffset;
                    y = adjPlanet.GalaxyLocation.Y + yOffset;

                    w.WriteStartElement( "Connection" );
                    w.WriteAttributeString( "X", x.ToString() );
                    w.WriteAttributeString( "Y", y.ToString() );
                    w.WriteEndElement();

                    return DelReturn.Continue;
                } );

                w.WriteEndElement();

                return DelReturn.Continue;
            } );

            w.WriteEndDocument();
            w.Close();
            File.Create( GAME_MAP_FLAG );
        }

        public override void DoOnAnyDeathLogic( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenSimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            if ( entity.TypeData.GetHasTag( "DiscordianActive" ) )
            {
                DiscordianData ddata = entity.GetDiscordianData( ExternalDataRetrieval.CreateIfNotFound );
                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( entity.PlanetFaction,
                    GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DiscordianBroken" ),
                    entity.CurrentMarkLevel, entity.PlanetFaction.FleetUsedAtPlanet, 0, entity.WorldLocation, Context );

                entity.PlanetFaction.Faction.DoForEntities( ( GameEntity_Squad strikecraft ) =>
                {
                    if ( strikecraft.MinorFactionStackingID == entity.PrimaryKeyID )
                        strikecraft.MinorFactionStackingID = newEntity.PrimaryKeyID;

                    return DelReturn.Continue;
                } );

                newEntity.SetDiscordianData( ddata );
                newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, newEntity.PlanetFaction.Faction.FactionIndex );
            }
        }
    }
}
