using System;
using System.Collections.Generic;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using PreceptsOfThePrecursors.Notifications;

namespace PreceptsOfThePrecursors
{
    // Main faction class.
    public class Sleepers : BaseSpecialFaction
    {
        protected override string TracingName => "Sleepers";
        protected override bool EverNeedsToRunLongRangePlanning => true;
        protected override int MinimumSecondsBetweenLongRangePlannings => 5;

        public override void WriteTextToSecondLineOfLeftSidebarInLobby( ConfigurationForFaction FactionConfig, Faction FactionOrNull, ArcenDoubleCharacterBuffer buffer )
        {
            string value = FactionConfig.GetValueForCustomFieldOrDefaultValue( "Intensity" );
            bool hasAdded = false;
            if ( value != null )
            {
                hasAdded = true;
                buffer.Add( "Strength: " ).Add( value );
            }
        }

        public enum UNIT_NAMES
        {
            Sleeper,
            SleeperDerelict,
            SleeperMobile,
            SleeperPrime,
            SleeperPrimeMobile
        }

        public enum UNIT_TAGS
        {
            Sleeper,
            SleeperPrime,
            SleeperDerelict
        }

        public enum COMMANDS
        {
            SetPrimeTarget,
            SetSleeperTargets
        }

        public static ArcenSparseLookup<Planet, List<GameEntity_Squad>> primesByPlanet;
        public static ArcenSparseLookup<Planet, List<GameEntity_Squad>> derelictsByPlanet;

        public Sleepers()
        {
            primesByPlanet = new ArcenSparseLookup<Planet, List<GameEntity_Squad>>();
            derelictsByPlanet = new ArcenSparseLookup<Planet, List<GameEntity_Squad>>();
        }

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            primesByPlanet = new ArcenSparseLookup<Planet, List<GameEntity_Squad>>();
            World_AIW2.Instance.GetNeutralFaction().DoForEntities( UNIT_TAGS.SleeperPrime.ToString(), prime =>
            {
                prime.AddToPerPlanetLookup( ref primesByPlanet );

                return DelReturn.Continue;
            } );

            derelictsByPlanet = new ArcenSparseLookup<Planet, List<GameEntity_Squad>>();
            World_AIW2.Instance.GetNeutralFaction().DoForEntities( UNIT_TAGS.SleeperDerelict.ToString(), sleeper =>
            {
                sleeper.AddToPerPlanetLookup( ref derelictsByPlanet );

                return DelReturn.Continue;
            } );
        }

        public override void SeedStartingEntities_LaterEverythingElse( Faction faction, Galaxy galaxy, ArcenSimContext Context, MapTypeData mapType )
        {
            int toSeedMinimum = 2;
            FInt maxFromIntensity = faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity * FInt.FromParts( 0, 800 );
            int toSeed = Math.Max( toSeedMinimum, maxFromIntensity.GetNearestIntPreferringHigher() );

            Mapgen_Base.Mapgen_SeedSpecialEntities( Context, galaxy, SpecialEntityType.None, UNIT_TAGS.SleeperDerelict.ToString(), SeedingType.HardcodedCount, toSeed,
                MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 3, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal );
        }
    }

    // Base class for all subfactions.
    public abstract class SleeperSubFaction : BaseSpecialFaction, IBulkPathfinding
    {
        public GameEntity_Squad Prime;
        public Planet PrimeTargetPlanet;
        public Planet PrimeNextPlanetToMoveTo;
        public ArcenPoint PrimeTarget;

        public ArcenSparseLookup<Planet, List<GameEntity_Squad>> sleepersByPlanet;
        public ArcenSparseLookup<GameEntity_Squad, ArcenSparseLookupPair<Planet, ArcenPoint>> sleeperTargets;

        public Mode CurrentMode { get { return GetMode(); } }

        public ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> WormholeCommands { get; set; }
        public ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> MovementCommands { get; set; }

        public enum Mode
        {
            Inactive,
            Primeless,
            Claiming,
            Defense
        }

        public Mode GetMode()
        {
            if ( Prime == null )
            {
                if ( sleepersByPlanet.GetPairCount() > 0 )
                    return Mode.Primeless;
                else
                    return Mode.Inactive;
            }
            else if ( Sleepers.derelictsByPlanet.GetPairCount() > 0 )
                return Mode.Claiming;
            else
                return Mode.Defense;
        }

        public bool PrimeCanMoveOn { get { return Prime != null && (Prime.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.SecondsSinceEnteringPlanet ?? 0) > 120; } }

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            Prime = null;
            Prime = faction.GetFirstMatching( Sleepers.UNIT_TAGS.SleeperPrime.ToString(), true, true );

            sleepersByPlanet = new ArcenSparseLookup<Planet, List<GameEntity_Squad>>();
            faction.DoForEntities( Sleepers.UNIT_TAGS.Sleeper.ToString(), sleeper =>
            {
                sleeper.AddToPerPlanetLookup( ref sleepersByPlanet );

                return DelReturn.Continue;
            } );
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( CurrentMode == Mode.Inactive )
                return;

            HandleSleeperPerSecondLogic( faction, Context );
            TransformSleepers( faction, Context );
            if ( CurrentMode == Mode.Claiming )
                AwakenDerelictsIfAble( faction, Context );
        }

        public void HandleSleeperPerSecondLogic( Faction faction, ArcenSimContext Context )
        {
            if ( Prime != null )
                HandleSleeperPerSecondLogic_Helper( Prime, faction, Context );

            sleepersByPlanet.DoFor( pair =>
            {
                for ( int x = 0; x < pair.Value.Count; x++ )
                    HandleSleeperPerSecondLogic_Helper( pair.Value[x], faction, Context );

                return DelReturn.Continue;
            } );
        }

        public void HandleSleeperPerSecondLogic_Helper( GameEntity_Squad sleeper, Faction faction, ArcenSimContext Context )
        {
            SleeperData sData = sleeper.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound );
            if ( sleeper.Planet != sData.Planet )
            {
                sData.Planet = sleeper.Planet;
                sData.SecondEnteredPlanet = World_AIW2.Instance.GameSecond;
            }
        }

        public void TransformSleepers( Faction faction, ArcenSimContext Context )
        {
            if ( Prime != null && PrimeTarget != ArcenPoint.OutOfRange && PrimeTargetPlanet != null )
            {
                GameEntityTypeData primeMobile = GameEntityTypeDataTable.Instance.GetRowByName( Sleepers.UNIT_NAMES.SleeperPrimeMobile.ToString() );
                GameEntityTypeData primeStationary = GameEntityTypeDataTable.Instance.GetRowByName( Sleepers.UNIT_NAMES.SleeperPrime.ToString() );
                SleeperData spData = Prime.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound );
                if ( !PrimeCanMoveOn || (Prime.Planet == PrimeTargetPlanet && Prime.WorldLocation.GetDistanceTo( PrimeTarget, true ) <= 4000) )
                {
                    if ( Prime.TypeData != primeStationary )
                    {
                        spData.SecondLastTransformed = World_AIW2.Instance.GameSecond;
                        Prime.TransformInto( Context, primeStationary, 1 ).SetSleeperData( spData );
                    }
                }
                else if ( Prime.TypeData != primeMobile )
                {
                    spData.SecondLastTransformed = World_AIW2.Instance.GameSecond;
                    Prime.TransformInto( Context, primeMobile, 1 ).SetSleeperData( spData );
                }

                GameEntityTypeData sleeperMobile = GameEntityTypeDataTable.Instance.GetRowByName( Sleepers.UNIT_NAMES.SleeperMobile.ToString() );
                GameEntityTypeData sleeperStationary = GameEntityTypeDataTable.Instance.GetRowByName( Sleepers.UNIT_NAMES.Sleeper.ToString() );
                sleepersByPlanet.DoFor( pair =>
                {
                    for ( int x = 0; x < pair.Value.Count; x++ )
                    {
                        GameEntity_Squad sleeper = pair.Value[x];
                        ArcenSparseLookupPair<Planet, ArcenPoint> target = sleeperTargets.GetHasKey( sleeper ) ? sleeperTargets[sleeper] : null;
                        if ( target == null )
                            continue;

                        SleeperData sData = sleeper.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound );
                        if ( Prime.TypeData.InternalName == Sleepers.UNIT_NAMES.SleeperPrime.ToString() && sleeper.Planet == target.Key && target.Value != ArcenPoint.OutOfRange
                        && sleeper.WorldLocation == target.Value )
                        {
                            if ( sleeper.TypeData != sleeperStationary )
                            {
                                sData.SecondLastTransformed = World_AIW2.Instance.GameSecond;
                                sleeper.TransformInto( Context, sleeperStationary, 1 ).SetSleeperData( sData );
                            }
                        }
                        else if ( sleeper.TypeData != sleeperMobile )
                        {
                            sData.SecondLastTransformed = World_AIW2.Instance.GameSecond;
                            sleeper.TransformInto( Context, sleeperMobile, 1 ).SetSleeperData( sData );
                        }
                    }

                    return DelReturn.Continue;
                } );
            }
        }

        public void AwakenDerelictsIfAble( Faction faction, ArcenSimContext Context )
        {
            if ( Prime == null || Prime.Planet != PrimeTargetPlanet || Prime.TypeData.InternalName == Sleepers.UNIT_NAMES.SleeperPrimeMobile.ToString() )
                return;

            if ( Prime.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound ).SecondsSinceLastTransformation < 300 )
                return;

            GameEntity_Squad derelict = Prime.Planet.GetFirstMatching( Sleepers.UNIT_TAGS.SleeperDerelict.ToString(), World_AIW2.Instance.GetNeutralFaction(), true, true );
            if ( derelict == null )
                return;

            GameEntity_Squad.CreateNew( Prime.PlanetFaction, GameEntityTypeDataTable.Instance.GetRowByName( Sleepers.UNIT_NAMES.Sleeper.ToString() ), 7, Prime.PlanetFaction.FleetUsedAtPlanet, 0, derelict.WorldLocation, Context );
            derelict.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            switch ( CurrentMode )
            {
                case Mode.Claiming:
                    HandleSleepersBackgroundClaimingLogic( faction, Context );
                    break;
                case Mode.Defense:
                    HandleSleepersBackgroundDefensiveLogic( faction, Context );
                    break;
                default:
                    break;
            }

            faction.ExecuteMovementCommands( Context );
            faction.ExecuteWormholeCommands( Context );
        }

        public abstract void HandleSleepersBackgroundDefensiveLogic( Faction faction, ArcenLongTermIntermittentPlanningContext Context );

        public virtual void HandleSleepersBackgroundClaimingLogic( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( Prime != null )
            {
                PlanClaimingMovementForPrime( faction, Context );
                ExecuteClaimingMovementForPrime( faction, Context );
                if ( PrimeNextPlanetToMoveTo == Prime.Planet )
                    PrimeNextPlanetToMoveTo = Prime.QueueWormholeCommand( PrimeTargetPlanet, Context, true );

                if ( sleepersByPlanet != null )
                    PlanClaimingMovementForSleepers( faction, Context );

                if ( sleeperTargets != null )
                    ExecuteClaimingMovementForSleepers( faction, Context );
            }
        }

        public void PlanClaimingMovementForPrime( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            Planet nearestPlanet = null;
            Sleepers.derelictsByPlanet.DoFor( pair =>
            {
                if ( nearestPlanet == null )
                    nearestPlanet = pair.Key;
                else if ( Prime.Planet.GetHopsTo( pair.Key ) < Prime.Planet.GetHopsTo( nearestPlanet ) )
                    nearestPlanet = pair.Key;

                return DelReturn.Continue;
            } );

            ArcenPoint targetPoint = Sleepers.derelictsByPlanet[nearestPlanet][0].WorldLocation;

            GameCommand targetCommand = StaticMethods.CreateGameCommand( Sleepers.COMMANDS.SetPrimeTarget.ToString(), GameCommandSource.AnythingElse, faction );
            targetCommand.RelatedIntegers.Add( nearestPlanet.Index );
            targetCommand.RelatedPoints.Add( targetPoint );
            Context.QueueCommandForSendingAtEndOfContext( targetCommand );
        }

        public void ExecuteClaimingMovementForPrime( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( PrimeTarget == ArcenPoint.OutOfRange || PrimeTargetPlanet == null )
                return;

            if ( Prime.Planet != PrimeTargetPlanet )
                PrimeNextPlanetToMoveTo = Prime.QueueWormholeCommand( PrimeTargetPlanet, Context, !PrimeCanMoveOn );
            else if ( Prime.WorldLocation.GetDistanceTo( PrimeTarget, true ) > 4000 )
                Prime.QueueMovementCommand( PrimeTarget );
        }

        public void PlanClaimingMovementForSleepers( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand command = StaticMethods.CreateGameCommand( Sleepers.COMMANDS.SetSleeperTargets.ToString(), GameCommandSource.AnythingElse, faction );

            List<int> sleeperPrimaryKeys = new List<int>();
            sleepersByPlanet.DoFor( pair =>
            {
                for ( int x = 0; x < pair.Value.Count; x++ )
                {
                    command.RelatedEntityIDs.Add( pair.Value[x].PrimaryKeyID );
                    sleeperPrimaryKeys.Add( pair.Value[x].PrimaryKeyID );
                    command.RelatedIntegers.Add( Prime.Planet.Index );
                }

                return DelReturn.Continue;
            } );

            sleeperPrimaryKeys.Sort();

            for ( int x = 0; x < command.RelatedEntityIDs.Count; x++ )
                command.RelatedPoints.Add( Prime.WorldLocation.GetPointAtAngleAndDistance( AngleDegrees.Create( 45 * sleeperPrimaryKeys.IndexOf( command.RelatedEntityIDs[x] ) ), 1000 ) );

            Context.QueueCommandForSendingAtEndOfContext( command );
        }

        public void ExecuteClaimingMovementForSleepers( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            sleeperTargets.DoFor( pair =>
            {
                if ( pair.Key.Planet != pair.Value.Key )
                    pair.Key.QueueWormholeCommand( pair.Value.Key, Context );
                else if ( pair.Value.Value != ArcenPoint.OutOfRange )
                    pair.Key.QueueMovementCommand( pair.Value.Value );

                return DelReturn.Continue;
            } );
        }

        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking( Faction faction, ArcenSimContext Context, bool IsFirstCallToFactionOfThisTypeThisCycle )
        {
            if ( Prime == null || PrimeTargetPlanet == null )
                return;

            switch ( CurrentMode )
            {
                case Mode.Inactive:
                    break;
                case Mode.Primeless:
                    break;
                case Mode.Claiming:
                    HandleClaimingNotifications( faction, Context );
                    break;
                case Mode.Defense:
                    break;
                default:
                    break;
            }
        }

        public void HandleClaimingNotifications( Faction faction, ArcenSimContext Context )
        {
            PrimeClaimingNotifier notifier = new PrimeClaimingNotifier();
            notifier.entity = Prime;
            notifier.planet = Prime.Planet;
            notifier.faction = faction;
            notifier.nextPlanet = PrimeNextPlanetToMoveTo;

            if ( Prime.Planet != PrimeTargetPlanet )
                notifier.CurrentMode = PrimeClaimingNotifier.Mode.Moving;
            else
                notifier.CurrentMode = PrimeClaimingNotifier.Mode.Awakening;

            NotificationNonSim notification = Engine_AIW2.NonSimNotificationList_Building.GetOrAddEntry();
            notification.Assign( notifier.ClickHandler, notifier.ContentGetter, notifier.MouseoverHandler, "", 0, "SleepersClaimPrimeMovement" );
        }
    }

    public class Dreamers : SleeperSubFaction
    {
        protected override string TracingName => "Dreamers";
        protected override bool EverNeedsToRunLongRangePlanning => true;
        protected override int MinimumSecondsBetweenLongRangePlannings => 1;

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            allyThisFactionToHumans( faction );

            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( faction, Context );
        }

        public override void HandleSleepersBackgroundDefensiveLogic( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            return;
        }
    }
}