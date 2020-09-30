using System;
using System.Collections.Generic;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace MacrophageHistiocytes
{
    public class MacrophageHistiocytes : BaseSpecialFaction, IBulkPathfinding
    {
        protected override string TracingName => "MacrophageHistiocytes";
        protected override bool EverNeedsToRunLongRangePlanning => true;
        protected override int MinimumSecondsBetweenLongRangePlannings => 3;

        public ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> WormholeCommands { get; set; }
        public ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> MovementCommands { get; set; }

        public enum Tags
        {
            MetalGenerator,
            MetalGeneratorInfested,
            MacrophageHistiocyte
        }

        public enum Settings
        {
            TeliumRegen,
            MacrophageDarkSpireRivalry,
            AIMacrophageSpray,
            SporeModule,
            ChillOutPhage
        }

        public override void UpdatePowerLevel( Faction faction )
        {
            World_AIW2.Instance.DoForFactions( workingFaction =>
            {
                if ( !(workingFaction.Implementation is SpecialFaction_MacrophageInfestation) )
                    return DelReturn.Continue;

                SpecialFaction_MacrophageInfestation implementation = workingFaction.Implementation as SpecialFaction_MacrophageInfestation;

                if ( implementation.Telia == null || implementation.Telia.Count == 0 )
                {
                    workingFaction.OverallPowerLevel = FInt.Zero;
                    return DelReturn.Continue;
                }

                FInt fromTelia = FInt.Zero, fromInfested = FInt.Zero;

                fromTelia = implementation.Telia.GetPairCount() * FInt.FromParts( 0, 010 );

                workingFaction.DoForEntities( Tags.MetalGeneratorInfested.ToString(), infested =>
                {
                    fromInfested += FInt.FromParts( 0, 005 );

                    return DelReturn.Continue;
                } );

                workingFaction.OverallPowerLevel = fromTelia + fromInfested;

                if ( workingFaction.OverallPowerLevel > 2 )
                    workingFaction.OverallPowerLevel = FInt.FromParts( 2, 000 );

                return DelReturn.Continue;
            } );
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            // Revert any infested harvesters owned by non-macrophage factions, or by a player faction with no tamed telia, back to their natural state.
            RevertInfestedGeneratorsAsNeeded( Context );

            // Convert metal generators.
            ConvertMetalGeneratorsIfAble( Context );

            // Give any infested mines owned by the Tamed to the player if they control the planet.
            TransferGeneratorsWithOwnerIfAble( Context );

            // Level up Metal Generators and Telia based on time alive.
            LevelUpEntitiesAndTheirDronesIfAble( Context );

            if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( Settings.TeliumRegen.ToString() ) == true )
                HandleTeliumRegenStage3Logic( Context );

            if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( Settings.MacrophageDarkSpireRivalry.ToString() ) == true )
                HandleDarkSpireRivalryStage3Logic( Context );

            if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( Settings.AIMacrophageSpray.ToString() ) == true )
                HandleAIMacrophageSprayStage3Logic( Context );

            if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( Settings.SporeModule.ToString() ) == true )
            {
                HandleSporeSpawningFromInfectedMetalGenerators( Context );
                HandleSporeInfection( Context );
            }

            if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( Settings.ChillOutPhage.ToString() ) == true )
                InflateEnragedCapacityCounts( Context );
        }

        public void RevertInfestedGeneratorsAsNeeded( ArcenSimContext Context )
        {
            World_AIW2.Instance.DoForEntities( Tags.MetalGeneratorInfested.ToString(), generator =>
            {
                if ( generator.PlanetFaction.Faction.Implementation is Macrophage )
                    return DelReturn.Continue; // Macrophage owned are okay.

                if ( generator.PlanetFaction.Faction.Type == FactionType.Player )
                {
                    SpecialFaction_MacrophageInfestationTamed tamedImplementation = (World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( SpecialFaction_MacrophageInfestationTamed ) )?.Implementation as SpecialFaction_MacrophageInfestationTamed);

                    if ( tamedImplementation == null )
                        return DelReturn.Continue; // Player owned should wait to be judged until the game has finished its per second logic.

                    if ( tamedImplementation.Telia != null && tamedImplementation.Telia.GetPairCount() > 0 )
                        return DelReturn.Continue; // Player owned are okay so long as the Tamed has presence on the map.
                }

                GameEntity_Squad.CreateNew( generator.PlanetFaction, GameEntityTypeDataTable.Instance.GetRowByName( Tags.MetalGenerator.ToString() ), generator.CurrentMarkLevel, generator.PlanetFaction.FleetUsedAtPlanet, 0,
                    generator.WorldLocation, Context );

                generator.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );

                return DelReturn.RemoveAndContinue;
            } );
        }

        public void ConvertMetalGeneratorsIfAble( ArcenSimContext Context )
        {
            World_AIW2.Instance.DoForEntities( Macrophage.HarvesterTag, harvester =>
            {
                bool converted = false;
                harvester.Planet.GetPlanetFactionForFaction( World_AIW2.Instance.GetNeutralFaction() ).Entities.DoForEntities( Tags.MetalGenerator.ToString(), metalGenerator =>
                {
                    if ( metalGenerator.TypeData.GetHasTag( Tags.MetalGeneratorInfested.ToString() ) )
                        return DelReturn.Continue; // Skip already infested ones.

                    if ( metalGenerator.GetSecondsSinceCreation() < 120 )
                        return DelReturn.Continue; // Cooldown between conversion.

                    if ( harvester.GetDistanceTo_VeryCheapButExtremelyRough( metalGenerator, true ) > 1000 )
                        return DelReturn.Continue;

                    GameEntity_Squad.CreateNew( harvester.PlanetFaction, GameEntityTypeDataTable.Instance.GetRowByName( Tags.MetalGeneratorInfested.ToString() ), 1, harvester.PlanetFaction.FleetUsedAtPlanet, 0,
                        metalGenerator.WorldLocation, Context );

                    metalGenerator.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );

                    converted = true;

                    return DelReturn.RemoveAndContinue;
                } );

                if ( converted )
                    return DelReturn.Break;
                return DelReturn.Continue;
            } );
        }

        public void TransferGeneratorsWithOwnerIfAble( ArcenSimContext Context )
        {
            Faction tamedFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( SpecialFaction_MacrophageInfestationTamed ) );
            if ( tamedFaction == null )
                return;

            World_AIW2.Instance.DoForEntities( Tags.MetalGeneratorInfested.ToString(), infestedGenerator =>
            {
                if ( infestedGenerator.PlanetFaction.Faction != tamedFaction && !infestedGenerator.PlanetFaction.Faction.GetIsFriendlyTowards( tamedFaction ) )
                    return DelReturn.Continue;

                bool converted = false;
                switch ( infestedGenerator.PlanetFaction.Faction.Type )
                {
                    case FactionType.Player:
                    case FactionType.AI:
                        if ( infestedGenerator.PlanetFaction.Faction != infestedGenerator.Planet.GetControllingFaction() )
                        {
                            GameEntity_Squad.CreateNew( infestedGenerator.Planet.GetPlanetFactionForFaction( tamedFaction ), GameEntityTypeDataTable.Instance.GetRowByName( Tags.MetalGeneratorInfested.ToString() ),
                                infestedGenerator.Planet.GetControllingFaction().GetGlobalMarkLevelForShipLine( infestedGenerator.TypeData ),
                                infestedGenerator.Planet.GetPlanetFactionForFaction( tamedFaction ).FleetUsedAtPlanet, 0, infestedGenerator.WorldLocation, Context );

                            infestedGenerator.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );

                            converted = true;
                        }
                        break;
                    default:
                        if ( infestedGenerator.Planet.GetIsControlledByFactionType( FactionType.Player ) && infestedGenerator.PlanetFaction.Faction != infestedGenerator.Planet.GetControllingFaction() )
                        {
                            GameEntity_Squad.CreateNew( infestedGenerator.Planet.GetPlanetFactionForFaction( infestedGenerator.Planet.GetControllingFaction() ), GameEntityTypeDataTable.Instance.GetRowByName( Tags.MetalGeneratorInfested.ToString() ),
                                1, infestedGenerator.Planet.GetPlanetFactionForFaction( infestedGenerator.Planet.GetControllingFaction() ).FleetUsedAtPlanet, 0, infestedGenerator.WorldLocation, Context );

                            infestedGenerator.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );

                            converted = true;
                        }
                        break;
                }
                if ( converted )
                    return DelReturn.Break;
                return DelReturn.Continue;
            } );
        }

        public void LevelUpEntitiesAndTheirDronesIfAble( ArcenSimContext Context )
        {
            World_AIW2.Instance.DoForEntities( Macrophage.TeliumTag, telium =>
            {
                if ( telium.CurrentMarkLevel >= 7 )
                    return DelReturn.Continue;

                byte effectiveMark = (byte)(Math.Min( 7, 1 + telium.GetSecondsSinceCreation() / 1800 ));

                telium.SetCurrentMarkLevelIfHigherThanCurrent( effectiveMark, Context );

                telium.FleetMembership.Fleet.DoForMemberGroups( mem => { mem.EffectiveMark = effectiveMark; return DelReturn.Continue; } );

                return DelReturn.Continue;
            } );
            World_AIW2.Instance.DoForEntities( Tags.MetalGeneratorInfested.ToString(), generator =>
            {
                if ( generator.CurrentMarkLevel >= 7 )
                    return DelReturn.Continue;

                byte effectiveMark = (byte)(Math.Min( 7, 1 + generator.GetSecondsSinceCreation() / 900 ));

                generator.SetCurrentMarkLevelIfHigherThanCurrent( effectiveMark, Context );

                generator.FleetMembership.Fleet.DoForMemberGroups( mem => { mem.EffectiveMark = effectiveMark; return DelReturn.Continue; } );

                return DelReturn.Continue;
            } );
            World_AIW2.Instance.DoForEntities( Macrophage.HarvesterTag, harvester =>
            {
                harvester.FleetMembership.Fleet.DoForMemberGroups( mem => { mem.EffectiveMark = harvester.CurrentMarkLevel; return DelReturn.Continue; } );

                return DelReturn.Continue;
            } );
        }

        public void HandleTeliumRegenStage3Logic( ArcenSimContext Context )
        {
            FInt perSecondRegen = FInt.FromParts( 0, 001 );

            World_AIW2.Instance.DoForEntities( Macrophage.TeliumTag, telium =>
            {
                if ( telium.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength > 1000 )
                    return DelReturn.Continue;

                telium.TakeHullRepair( (telium.GetMaxHullPoints() * perSecondRegen).GetNearestIntPreferringHigher() );

                return DelReturn.Continue;
            } );
        }

        public void HandleDarkSpireRivalryStage3Logic( ArcenSimContext Context )
        {
            FInt perSecondGeneratorDamage = FInt.FromParts( 0, 001 );
            FInt perSecondTeliumDecay = FInt.FromParts( 0, 003 );
            List<Planet> vgPlanets = new List<Planet>();
            World_AIW2.Instance.DoForEntities( "VengeanceGenerator", generator =>
            {
                if ( !vgPlanets.Contains( generator.Planet ) )
                    vgPlanets.Add( generator.Planet );

                generator.Planet.DoForEntities( Tags.MacrophageHistiocyte.ToString(), histiocyte =>
                {
                    int distance = histiocyte.GetDistanceTo_VeryCheapButExtremelyRough( generator, true );
                    if ( distance < 3000 )
                    {
                        generator.TakeHullRepair( -(generator.GetMaxHullPoints() * perSecondGeneratorDamage).GetNearestIntPreferringHigher() );
                        if ( Context.RandomToUse.Next( generator.GetCurrentHullPoints(), generator.GetMaxHullPoints() ) < generator.GetMaxHullPoints() / 10 )
                            SpecialFaction_DarkSpire.PerformVengeanceStrike();
                    }

                    return DelReturn.Continue;
                } );

                return DelReturn.Continue;
            } );

            for ( int x = 0; x < vgPlanets.Count; x++ )
                vgPlanets[x].DoForLinkedNeighborsAndSelf( false, planet =>
                {
                    planet.DoForEntities( Macrophage.TeliumTag, telium =>
                    {
                        telium.TakeHullRepair( -(telium.GetMaxHullPoints() * perSecondTeliumDecay).GetNearestIntPreferringHigher() );

                        return DelReturn.Continue;
                    } );

                    return DelReturn.Continue;
                } );
        }

        public void HandleAIMacrophageSprayStage3Logic( ArcenSimContext Context )
        {
            FInt perSecondTeliumDecay = FInt.FromParts( 0, 003 );
            World_AIW2.Instance.DoForEntities( "ExtragalacticWar", warUnit =>
            {
                warUnit.Planet.DoForEntities( Macrophage.TeliumTag, telium =>
                {
                    telium.TakeHullRepair( -(telium.GetMaxHullPoints() * perSecondTeliumDecay).GetNearestIntPreferringHigher() );

                    return DelReturn.Continue;
                } );

                return DelReturn.Continue;
            } );
        }

        public void HandleSporeSpawningFromInfectedMetalGenerators( ArcenSimContext Context )
        {
            World_AIW2.Instance.DoForEntities( Tags.MetalGeneratorInfested.ToString(), infested =>
            {
                if ( infested.GetSecondsSinceCreation() < 5 || infested.GetSecondsSinceCreation() % 299 != 0 )
                    return DelReturn.Continue;

                GameEntity_Squad telia = null;
                infested.PlanetFaction.Faction.DoForEntities( Macrophage.TeliumTag, telium =>
                {
                    if ( telia == null || Context.RandomToUse.NextBool() )
                        telia = telium;

                    return DelReturn.Continue;
                } );

                if ( telia == null )
                    return DelReturn.Continue;

                SpawnSpore( infested.WorldLocation, Context, telia );

                return DelReturn.Continue;
            } );
        }

        private void SpawnSpore( ArcenPoint spawnLocation, ArcenSimContext Context, GameEntity_Squad telium )
        {
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, Macrophage.SporeTag );
            PlanetFaction pFaction = telium.PlanetFaction;
            if ( entityData == null )
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: no MacrophageSpore tag found", Verbosity.DoNotShow );
            GameEntity_Squad spore = GameEntity_Squad.CreateNew( pFaction, entityData, entityData.MarkFor( pFaction ),
                pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context );
            MacrophagePerSporeData sData = spore.GetMacrophagePerSporeDataExt( ExternalDataRetrieval.CreateIfNotFound );
            sData.TeliumID = telium.GetMacrophagePerTeliumDataExt( ExternalDataRetrieval.CreateIfNotFound ).UniqueID;
            sData.SpawnTime = World_AIW2.Instance.GameSecond;
            spore.SetMacrophagePerSporeDataExt( sData );

        }

        public void HandleSporeInfection( ArcenSimContext Context )
        {
            World_AIW2.Instance.DoForEntities( Macrophage.SporeTag, spore =>
            {
                bool converted = false;
                spore.Planet.GetPlanetFactionForFaction( World_AIW2.Instance.GetNeutralFaction() ).Entities.DoForEntities( Tags.MetalGenerator.ToString(), metalGenerator =>
                {
                    if ( metalGenerator.TypeData.GetHasTag( Tags.MetalGeneratorInfested.ToString() ) )
                        return DelReturn.Continue; // Skip already infested ones.

                    if ( metalGenerator.GetSecondsSinceCreation() < 120 )
                        return DelReturn.Continue; // Cooldown between conversion.

                    if ( spore.GetDistanceTo_VeryCheapButExtremelyRough( metalGenerator, true ) > 1000 )
                        return DelReturn.Continue;

                    GameEntity_Squad.CreateNew( spore.PlanetFaction, GameEntityTypeDataTable.Instance.GetRowByName( Tags.MetalGeneratorInfested.ToString() ), 1, spore.PlanetFaction.FleetUsedAtPlanet, 0,
                        metalGenerator.WorldLocation, Context );

                    metalGenerator.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );

                    converted = true;

                    return DelReturn.RemoveAndContinue;
                } );

                if ( converted )
                    return DelReturn.Break;
                return DelReturn.Continue;
            } );
        }

        public void InflateEnragedCapacityCounts( ArcenSimContext Context )
        {
            Macrophage.HarvesterLimitBeforeEnraging = 9999;
            Macrophage.SpireHarvesterLimitBeforeEnraging = 9999;
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            World_AIW2.Instance.DoForEntities( Tags.MacrophageHistiocyte.ToString(), entity =>
            {
                GameEntity_Squad host = entity.FleetMembership.Fleet.Centerpiece;
                if ( host == null )
                    return DelReturn.Continue;

                if ( entity.Planet != host.Planet )
                    faction.QueueWormholeCommand( entity, host.Planet );
                else if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "MacrophageDarkSpireRivalry" ) == true )
                {
                    GameEntity_Squad generator = entity.Planet.GetFirstMatching( FactionType.SpecialFaction, "VengeanceGenerator", true, true );
                    if ( generator == null )
                        return DelReturn.Continue;

                    if ( entity.GetDistanceTo_VeryCheapButExtremelyRough( generator, true ) > 1000 )
                        faction.QueueMovementCommand( entity, generator.WorldLocation );
                }

                return DelReturn.Continue;
            } );

            if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( Settings.SporeModule.ToString() ) == true )
                World_AIW2.Instance.DoForEntities( Macrophage.SporeTag, spore =>
                {
                    GameEntity_Squad nearest = null;
                    int nearestDistance = -1;
                    spore.Planet.GetPlanetFactionForFaction( World_AIW2.Instance.GetNeutralFaction() ).Entities.DoForEntities( Tags.MetalGenerator.ToString(), metalGenerator =>
                    {
                        if ( metalGenerator.TypeData.GetHasTag( Tags.MetalGeneratorInfested.ToString() ) )
                            return DelReturn.Continue; // Skip already infested ones.

                        int distance = spore.GetDistanceTo_VeryCheapButExtremelyRough( metalGenerator, true );

                        if ( nearest == null || distance < nearestDistance )
                        {
                            nearest = metalGenerator;
                            nearestDistance = distance;
                        }

                        return DelReturn.Continue;
                    } );

                    if ( nearest != null )
                        faction.QueueMovementCommand( spore, nearest.WorldLocation );

                    return DelReturn.Continue;
                } );

            faction.ExecuteWormholeCommands( Context );
            faction.ExecuteMovementCommands( Context );
        }

        public override void DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips( ref int debugStage, GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, Faction factionThatKilledEntity, Faction entityOwningFaction, int numExtraStacksKilled, ArcenSimContext Context )
        {
            if ( FiringSystemOrNull == null )
                return;

            GameEntity_Squad EntityThatKilledTarget = FiringSystemOrNull.ParentEntity;
            if ( EntityThatKilledTarget == null )
                return;

            if ( !EntityThatKilledTarget.TypeData.GetHasTag( Tags.MacrophageHistiocyte.ToString() ) )
                return;

            debugStage = 16100;
            GameEntity_Squad host = EntityThatKilledTarget?.FleetMembership?.Fleet?.Centerpiece;
            if ( host == null )
                return;

            if ( host.TypeData.GetHasTag( Macrophage.HarvesterTag ) )
            {
                MacrophagePerHarvesterData pData = host.GetMacrophagePerHarvesterDataExt( ExternalDataRetrieval.CreateIfNotFound );
                if ( pData != null )
                {
                    debugStage = 16110;
                    int metalGained = entity.GetStrengthPerSquad() + entity.GetStrengthPerSquad() * numExtraStacksKilled;
                    metalGained *= Macrophage.HarvesterMetalFromKillMultiplier;
                    pData.CurrentMetal += metalGained;
                    pData.TotalMetalEverCollected += metalGained;
                    host.SetMacrophagePerHarvesterDataExt( pData );
                }
            }
            else if ( host.TypeData.GetHasTag( Macrophage.TeliumTag ) )
            {
                debugStage = 16200;
                MacrophagePerTeliumData tData = host.GetMacrophagePerTeliumDataExt( ExternalDataRetrieval.CreateIfNotFound );
                if ( tData != null )
                {
                    debugStage = 16210;
                    int metalGained = entity.GetStrengthPerSquad() + entity.GetStrengthPerSquad() * numExtraStacksKilled;
                    metalGained *= Macrophage.HarvesterMetalFromKillMultiplier;
                    tData.CurrentMetal += metalGained;
                    tData.TotalMetalEverCollected += metalGained;
                    host.SetMacrophagePerTeliumDataExt( tData );
                }
            }
        }
    }
}
