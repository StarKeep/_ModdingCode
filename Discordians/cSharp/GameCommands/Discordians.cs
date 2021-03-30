using System;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace Discordians
{
    public class ExecuteDiscordianSpawnCommand : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext Context )
        {
            Faction discordianFaction = World_AIW2.Instance.GetFactionByIndex( command.RelatedFactionIndex );
            string id = command.RelatedString;
            string username = command.RelatedString2;

            Planet spawnPlanet = FactionUtilityMethods.findHumanKing();

            GameEntity_Squad discordian = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( spawnPlanet.GetPlanetFactionForFaction( discordianFaction ),
                GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DiscordianActive" ), 1,
                spawnPlanet.GetPlanetFactionForFaction( discordianFaction ).FleetUsedAtPlanet,
                0, Engine_AIW2.Instance.CombatCenter, Context );

            discordian.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, discordianFaction.FactionIndex );

            discordian.GetDiscordianData( ExternalDataRetrieval.CreateIfNotFound ).ID = id;
            discordian.GetDiscordianData( ExternalDataRetrieval.CreateIfNotFound ).UserName = username;
            discordian.GetDiscordianData( ExternalDataRetrieval.CreateIfNotFound ).TargetPlanet = spawnPlanet;
            discordian.GetDiscordianData( ExternalDataRetrieval.CreateIfNotFound ).Experience = Math.Max( 100, World_AIW2.Instance.GameSecond / 2 );
            discordian.GetDiscordianData( ExternalDataRetrieval.CreateIfNotFound ).Research = Math.Max( 100, World_AIW2.Instance.GameSecond / 2 );

            World_AIW2.Instance.QueueChatMessageOrCommand( $"{username} has joined the galaxy!", ChatType.LogToCentralChat, Context );

            if ( command.RelatedBool )
            {
                GameEntityTypeData starter = (discordianFaction.Implementation as Discordians).ShipCaps.GetPairByIndex( Math.Max( 0, Context.RandomToUse.Next( -1, (discordianFaction.Implementation as Discordians).ShipCaps.GetPairCount() ) ) ).Key;
                if ( !discordian.GetDiscordianData( ExternalDataRetrieval.CreateIfNotFound ).ShipTypes.GetHasKey( starter.InternalName ) )
                    discordian.GetDiscordianData( ExternalDataRetrieval.CreateIfNotFound ).ShipTypes.AddPair( starter.InternalName, 0 );
                discordian.GetDiscordianData( ExternalDataRetrieval.CreateIfNotFound ).ShipTypes[starter.InternalName]++;
                World_AIW2.Instance.QueueChatMessageOrCommand( $"{username} has unlocked a wing of {starter.DisplayName}!", ChatType.LogToCentralChat, Context );
            }
        }
    }
    public class ExecuteDiscordianTargetCommand : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext Context )
        {
            GameEntity_Squad discordian = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs[0] );
            Planet targetPlanet = World_AIW2.Instance.GetPlanetByName( false, command.RelatedString );
            if ( targetPlanet == null )
                World_AIW2.Instance.DoForPlanets( false, planet =>
                {
                    if ( planet.Name.ToLower() == command.RelatedString.ToLower() )
                    {
                        targetPlanet = planet;
                        return DelReturn.Break;
                    }
                    return DelReturn.Continue;
                } );
            if ( discordian != null && targetPlanet != null )
            {
                discordian.GetDiscordianData( ExternalDataRetrieval.CreateIfNotFound ).TargetPlanet = targetPlanet;
                World_AIW2.Instance.QueueChatMessageOrCommand( $"{discordian.GetDiscordianData( ExternalDataRetrieval.CreateIfNotFound ).UserName} has targeted {targetPlanet.Name}.", ChatType.LogToCentralChat, Context );
            }
        }
    }
    public class ExecuteDiscordianUnlockCommand : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext Context )
        {
            Faction discordianFaction = World_AIW2.Instance.GetFactionByIndex( command.RelatedFactionIndex );
            Discordians implementation = discordianFaction.Implementation as Discordians;
            GameEntity_Squad discordian = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs[0] );
            DiscordianData dData = discordian.GetDiscordianData( ExternalDataRetrieval.CreateIfNotFound );
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.Rows.Find( r => r.InternalName.ToLower() == command.RelatedString.ToLower() );
            if ( entityData == null )
                return;

            if ( !command.RelatedBool )
            {
                if ( !implementation.ShipCaps.GetHasKey( entityData ) )
                    return;
                if ( dData.ShipTypes.GetHasKey( entityData.InternalName ) && dData.ShipTypes[entityData.InternalName] >= implementation.ShipCaps[entityData] )
                    return;

                int baseCost;

                if ( entityData.IsStrikecraft )
                    baseCost = entityData.GetForMark( 1 ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                else
                    baseCost = entityData.GetForMark( 1 ).StrengthPerSquad_CalculatedWithNullFleetMembership / 10;

                int cost = baseCost * Discordians.GetCostMultiplier( discordianFaction.Ex_MinorFactionCommon_GetPrimitives(ExternalDataRetrieval.CreateIfNotFound).Intensity );

                if ( dData.Research < cost )
                    return;

                dData.Research -= cost;
            }

            if ( !dData.ShipTypes.GetHasKey( entityData.InternalName ) )
                dData.ShipTypes.AddPair( entityData.InternalName, 0 );
            dData.ShipTypes[entityData.InternalName]++;

            World_AIW2.Instance.QueueChatMessageOrCommand( $"{dData.UserName} has unlocked a wing of {entityData.DisplayName}!", ChatType.LogToCentralChat, Context );
        }
    }
    public class UpdateDiscordianShipCapsCommand : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext Context )
        {
            Faction discordianFaction = World_AIW2.Instance.GetFactionByIndex( command.RelatedFactionIndex );
            Discordians implementation = discordianFaction.Implementation as Discordians;

            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( command.RelatedString );
            byte count = (byte)command.RelatedIntegers[0];

            if ( implementation.ShipCaps.GetHasKey( entityData ) )
                implementation.ShipCaps[entityData] = count;
            else
                implementation.ShipCaps.AddPair( entityData, count );
        }
    }
    public class SpawnDiscordianStrikecraftCommand : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext Context )
        {
            GameEntity_Squad discordian = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs[0] );
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( command.RelatedString );

            GameEntity_Squad strikecraft = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( discordian.PlanetFaction, entityData, 1,
                discordian.PlanetFaction.FleetUsedAtPlanet, 0, discordian.WorldLocation, Context );

            strikecraft.MinorFactionStackingID = discordian.PrimaryKeyID;
            strikecraft.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, strikecraft.PlanetFaction.Faction.FactionIndex );
        }
    }
}
