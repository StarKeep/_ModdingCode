﻿using System;
using System.Collections.Generic;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using PreceptsOfThePrecursors.Notifications;

namespace PreceptsOfThePrecursors
{
    // Main faction class.
    public class NeinzulWarChroniclers : BaseSpecialFaction
    {
        protected override string TracingName => "NeinzulWarChroniclers";
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
            value = FactionConfig.GetValueForCustomFieldOrDefaultValue( "Allegiance" );
            if ( value != null )
            {
                if ( hasAdded )
                    buffer.Add( "    " );
                else
                    hasAdded = true;
                buffer/*.Add( "Allegiance: " )*/.Add( value );
            }
        }

        public NeinzulWarChroniclersData factionData;
        public int Intensity;

        public ArcenSparseLookup<Planet, List<GameEntity_Squad>> assignedChroniclers;

        public static bool LoadedConstants;
        public static int SecondsPerMarkUpBase;
        public static int SecondsPerMarkUpDecreasePerIntensity;
        public static int BudgetPerSecondBase;
        public static int BudgetPerSecondIncreasePerIntensity;
        public static int SecondsOfConflictRequiredForArrival;
        public static int SecondsOfPeaceRequiredForDeparture;
        public static int SoftStrengthCapBase;
        public static int SoftStrengthCapIncreasePerAttack;
        public static int SoftStrengthCapIncreasePerAttackPerIntensity;
        public static int SoftStrengthCapIncreasePerHour;
        public static int SoftStrengthCapIncreasePerHourPerIntensity;

        public bool Initialized;
        public int BudgetPerSecond;
        public FInt PerSecondStrengthCapIncrease;
        public string Allegiance;

        public enum Commands
        {
            UpdateStudyBudgets
        }

        public byte ChroniclersMarkLevel( Faction faction )
        {
            int workingIntensity = Intensity > 0 ? Intensity : faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.ReturnNullIfNotFound )?.Intensity ?? 1;
            int secondsPerMarkUp = SecondsPerMarkUpBase - ((workingIntensity - 1) * SecondsPerMarkUpDecreasePerIntensity);

            if ( World_AIW2.Instance.GameSecond > secondsPerMarkUp * 6 )
                return 7;
            else
                return (byte)(1 + World_AIW2.Instance.GameSecond / secondsPerMarkUp);
        }

        public enum Tags
        {
            NeinzulWarChronicler
        }

        private void UpdateAllegiance( Faction faction )
        {
            switch ( this.Allegiance )
            {
                case "Hostile To All":
                case "HostileToAll":
                    enemyThisFactionToAll( faction );
                    break;
                case "Minor Faction Team Red":
                case "Minor Faction Team Blue":
                case "Minor Faction Team Green":
                    allyThisFactionToMinorFactionTeam( faction, this.Allegiance );
                    break;
                case "HostileToAI":
                case "Friendly To Players":
                    allyThisFactionToHumans( faction );
                    break;
                default:
                    break;
            }
        }

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( !LoadedConstants )
            {
                SecondsPerMarkUpBase = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NeinzulWarChroniclers_SecondsPerMarkUpBase" );
                SecondsPerMarkUpDecreasePerIntensity = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NeinzulWarChroniclers_SecondsPerMarkUpDecreasePerIntensity" );
                BudgetPerSecondBase = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NeinzulWarChroniclers_BudgetPerSecondBase" );
                BudgetPerSecondIncreasePerIntensity = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NeinzulWarChroniclers_BudgetPerSecondIncreasePerIntensity" );
                SecondsOfConflictRequiredForArrival = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NeinzulWarChroniclers_SecondsOfConflictRequiredForArrival" );
                SecondsOfPeaceRequiredForDeparture = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NeinzulWarChroniclers_SecondsOfPeaceRequiredForDeparture" );
                SoftStrengthCapBase = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NeinzulWarChroniclers_SoftStrengthCapBase" );
                SoftStrengthCapIncreasePerAttack = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NeinzulWarChroniclers_SoftStrengthCapIncreasePerAttack" );
                SoftStrengthCapIncreasePerAttackPerIntensity = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NeinzulWarChroniclers_SoftStrengthCapIncreasePerAttackPerIntensity" );
                SoftStrengthCapIncreasePerHour = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NeinzulWarChroniclers_SoftStrengthCapIncreasePerHour" );
                SoftStrengthCapIncreasePerHourPerIntensity = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NeinzulWarChroniclers_SoftStrengthCapIncreasePerHourPerIntensity" );
                LoadedConstants = true;
            }
            if ( factionData == null )
            {
                factionData = faction.GetNeinzulWarChroniclersData( ExternalDataRetrieval.CreateIfNotFound );
            }
            if ( !Initialized )
            {
                Intensity = faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Intensity;
                Allegiance = faction.Ex_MinorFactionCommon_GetPrimitives( ExternalDataRetrieval.CreateIfNotFound ).Allegiance;
                BudgetPerSecond = BudgetPerSecondBase + (Intensity * BudgetPerSecondIncreasePerIntensity);
                PerSecondStrengthCapIncrease = FInt.FromParts( (SoftStrengthCapIncreasePerHour + (SoftStrengthCapIncreasePerHourPerIntensity * Intensity)), 000 ) / 3600;

                Initialized = true;
            }

            assignedChroniclers = new ArcenSparseLookup<Planet, List<GameEntity_Squad>>();
            faction.DoForEntities( Tags.NeinzulWarChronicler.ToString(), entity =>
            {
                entity.AddToPerPlanetLookup( ref assignedChroniclers );

                return DelReturn.Continue;
            } );
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            //if ( factionData != null )
            //    factionData.OutputToDebugLog();

            ClearCrippledUnits( faction, Context );

            UpdateAllegiance( faction );

            UpdatePersonalBudget( faction, Context );

            AggressiveLogic( faction, Context );

            DepartureLogic( faction, Context );

            StudyLogic( faction, Context );
        }

        public void ClearCrippledUnits( Faction faction, ArcenSimContext Context )
        {
            faction.DoForEntities( ( GameEntity_Squad entity ) =>
            {
                if ( entity.GetIsCrippled() || entity.SecondsSpentAsRemains > 5 )
                    entity.Despawn( Context, true, InstancedRendererDeactivationReason.IFinishedMyJob );

                return DelReturn.Continue;
            } );
        }

        public void UpdatePersonalBudget( Faction faction, ArcenSimContext Context )
        {
            int baseIncrease = 500;
            int increaseFromIntensity = Intensity * 50;
            int capacity = 1 + Intensity / 3;
            int budgetCapacity = 1000000 * capacity;

            // Infuse them with a free early game spawn. Free of charge.
            if ( World_AIW2.Instance.GameSecond == 30 )
                factionData.PersonalBudget += 1000000;

            factionData.PersonalBudget = Math.Min( factionData.PersonalBudget + baseIncrease + increaseFromIntensity, budgetCapacity );
        }

        public void AggressiveLogic( Faction faction, ArcenSimContext Context )
        {
            if ( factionData.PersonalBudget < 1000000 )
            {
                if ( factionData.CurrentPlanetAimedAt != null )
                {
                    factionData.CurrentPlanetAimedAt = null;
                    factionData.GameSecondAimed = -1;
                }
                return;
            }

            if ( factionData.CurrentPlanetAimedAt != null )
                CheckIfTargetStillValid( faction, Context );

            if ( factionData.CurrentPlanetAimedAt == null )
                AimAtNewPlanet( faction, Context );

            if ( factionData.CurrentPlanetAimedAt == null )
                return;

            if ( factionData.SecondsAimedAtPlanet >= SecondsOfConflictRequiredForArrival )
                SendAttack( faction, Context );
        }

        public void CheckIfTargetStillValid( Faction faction, ArcenSimContext Context )
        {
            if ( !PlanetHasConflict( factionData.CurrentPlanetAimedAt, faction ) )
            {
                factionData.CurrentPlanetAimedAt = null;
                factionData.GameSecondAimed = -1;
            }
        }

        public void AimAtNewPlanet( Faction faction, ArcenSimContext Context )
        {
            List<Planet> conflictPlanets = new List<Planet>();
            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                bool alreadyAimedAt = false;
                World_AIW2.Instance.DoForFactions( otherFaction =>
                {
                    if ( faction == otherFaction )
                        return DelReturn.Continue;

                    if ( otherFaction.Implementation is NeinzulWarChroniclers )
                    {
                        Planet factionAimedAt = (otherFaction.Implementation as NeinzulWarChroniclers).factionData.CurrentPlanetAimedAt;
                        if ( factionAimedAt != null && factionAimedAt == planet )
                        {
                            alreadyAimedAt = true;
                            return DelReturn.Break;
                        }
                    }

                    return DelReturn.Continue;
                } );

                if ( !alreadyAimedAt && PlanetHasConflict( planet, faction ) && Context.RandomToUse.Next( 100 ) > 10 )
                    conflictPlanets.Add( planet );

                return DelReturn.Continue;
            } );

            switch ( conflictPlanets.Count )
            {
                case 0:
                    return;
                case 1:
                    factionData.CurrentPlanetAimedAt = conflictPlanets[0];
                    break;
                default:
                    factionData.CurrentPlanetAimedAt = conflictPlanets[Context.RandomToUse.Next( conflictPlanets.Count )];
                    break;
            }
            factionData.GameSecondAimed = World_AIW2.Instance.GameSecond;
        }

        public void SendAttack( Faction faction, ArcenSimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            int strengthCap = 0;
            strengthCap += SoftStrengthCapBase;
            strengthCap += SoftStrengthCapIncreasePerAttack * factionData.SentAttacks;
            strengthCap += SoftStrengthCapIncreasePerAttackPerIntensity * factionData.SentAttacks * Intensity;
            strengthCap += (World_AIW2.Instance.GameSecond * FInt.FromParts( (SoftStrengthCapIncreasePerHour + (SoftStrengthCapIncreasePerHourPerIntensity * Intensity)), 000 ) / 3600).GetNearestIntPreferringHigher();

            GameEntity_Squad chronicler = factionData.CurrentPlanetAimedAt.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRowByName( Tags.NeinzulWarChronicler.ToString() ), PlanetSeedingZone.OuterSystem );
            chronicler.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
            chronicler.SetCurrentMarkLevel( ChroniclersMarkLevel( faction ), Context );
            factionData.PersonalBudget -= 1000000;

            int strengthSent = chronicler.GetStrengthPerSquad();

            factionData.BudgetGenerated.Sort( ( pair1, pair2 ) =>
            {
                return -(GameEntityTypeDataTable.Instance.GetRowByName( pair1.Key ).GetForMark( 1 ).StrengthPerSquad_CalculatedWithNullFleetMembership - GameEntityTypeDataTable.Instance.GetRowByName( pair2.Key ).GetForMark( 1 ).StrengthPerSquad_CalculatedWithNullFleetMembership);
            } );

            factionData.BudgetGenerated.DoFor( pair =>
            {
                if ( pair.Key == Tags.NeinzulWarChronicler.ToString() )
                    return DelReturn.RemoveAndContinue;

                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( pair.Key );
                if ( entityData == null )
                    return DelReturn.RemoveAndContinue;


                int cost = entityData.GetForMark( pair.Value ).StrengthPerSquad_CalculatedWithNullFleetMembership * 10;
                int toSend = pair.Value / cost;

                if ( toSend > 50 )
                {
                    toSend = 50;
                    pair.Value = cost * 50;
                }

                for ( int x = 0; x < toSend; x++ )
                {
                    GameEntity_Squad newSpawn = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( chronicler.PlanetFaction, entityData, ChroniclersMarkLevel(faction), chronicler.PlanetFaction.FleetUsedAtPlanet, 0, chronicler.WorldLocation, Context );
                    newSpawn.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );

                    strengthSent += newSpawn.GetStrengthPerSquad();

                    if ( strengthSent > strengthCap )
                        break;
                }

                if ( toSend > 0 )
                    pair.Value /= 2;

                return DelReturn.Continue;
            } );

            World_AIW2.Instance.QueueChatMessageOrCommand( "The " + faction.StartFactionColourForLog() + faction.GetDisplayName() + "</color> have arrived on " + factionData.CurrentPlanetAimedAt.Name, ChatType.LogToCentralChat, Context );

            factionData.CurrentPlanetAimedAt = null;
            factionData.GameSecondAimed = -1;
            factionData.SentAttacks++;
        }

        public void DepartureLogic( Faction faction, ArcenSimContext Context )
        {
            if ( factionData.CurrentPlanetWeAreDepartingFrom != null )
                if ( PlanetHasConflict( factionData.CurrentPlanetWeAreDepartingFrom, faction, false )
                  || factionData.CurrentPlanetWeAreDepartingFrom.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Self].TotalStrength < 1000 )
                {
                    factionData.CurrentPlanetWeAreDepartingFrom = null;
                    factionData.GameSecondDepartingStarted = -1;
                }

            if ( factionData.CurrentPlanetWeAreDepartingFrom == null )
                StartToDepartureIfNeeded( faction, Context );

            if ( factionData.CurrentPlanetWeAreDepartingFrom == null )
                return;

            if ( factionData.SecondsSinceDepartingStarted >= SecondsOfPeaceRequiredForDeparture )
                Depart( faction, Context );
        }

        public void StartToDepartureIfNeeded( Faction faction, ArcenSimContext Context )
        {
            faction.DoForEntities( Tags.NeinzulWarChronicler.ToString(), chronicler =>
            {
                if ( !PlanetHasConflict( chronicler.Planet, faction, false ) )
                {
                    factionData.CurrentPlanetWeAreDepartingFrom = chronicler.Planet;
                    factionData.GameSecondDepartingStarted = World_AIW2.Instance.GameSecond;
                    return DelReturn.Break;
                }

                return DelReturn.Continue;
            } );
        }

        public void Depart( Faction faction, ArcenSimContext Context )
        {
            PlanetFaction pFaction = factionData.CurrentPlanetWeAreDepartingFrom.GetPlanetFactionForFaction( faction );

            pFaction.Entities.DoForEntities( EntityRollupType.MobileCombatants, entity =>
            {
                if ( entity.TypeData.IsDrone )
                    return DelReturn.Continue;

                switch ( entity.TypeData.SpecialType )
                {
                    case SpecialEntityType.AIKingMobile:
                    case SpecialEntityType.FactoryForPlayerMobileFleets:
                    case SpecialEntityType.DroneGeneral:
                    case SpecialEntityType.NPCFactionCenterpiece:
                    case SpecialEntityType.MobileSupportFleetFlagship:
                    case SpecialEntityType.MobileStrikeCombatFleetFlagship:
                    case SpecialEntityType.DroneFrigate:
                    case SpecialEntityType.MobileCustomStrikeCombatFleetFlagship:
                    case SpecialEntityType.Minefield:
                    case SpecialEntityType.Buttress:
                    case SpecialEntityType.Length:
                        return DelReturn.Continue;
                    default:
                        if ( entity.TypeData.GetHasTag( Tags.NeinzulWarChronicler.ToString() ) )
                            factionData.PersonalBudget += 1000000; // Refund.
                        else
                            // We spend 50% of each unit's cost, which is 10x the unit's strength, to send.
                            // If the unit survives, refund the 50%.
                            factionData.AddBudget( entity, entity.TypeData.GetForMark( entity.CurrentMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership * 5 );
                        break;
                }

                entity.Despawn( Context, true, InstancedRendererDeactivationReason.IFinishedMyJob );
                return DelReturn.Continue;
            } );

            pFaction.Entities.DoForEntities( other =>
            {
                other.Despawn( Context, true, InstancedRendererDeactivationReason.IFinishedMyJob );

                return DelReturn.Continue;
            } );

            World_AIW2.Instance.QueueChatMessageOrCommand( "The " + faction.StartFactionColourForLog() + faction.GetDisplayName() + "</color> have departed from " + factionData.CurrentPlanetWeAreDepartingFrom.Name, ChatType.LogToCentralChat, Context );

            factionData.CurrentPlanetWeAreDepartingFrom = null;
            factionData.GameSecondDepartingStarted = -1;
        }

        public ArcenSparseLookup<GameEntity_Squad, int> currentBudgetStudyRates;

        public void StudyLogic( Faction faction, ArcenSimContext Context )
        {
            if ( currentBudgetStudyRates == null )
                return;

            currentBudgetStudyRates.DoFor( pair =>
            {
                factionData.AddBudget( pair.Key, pair.Value );

                return DelReturn.Continue;
            } );
        }

        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking( Faction faction, ArcenSimContext Context, bool IsFirstCallToFactionOfThisTypeThisCycle )
        {
            if ( factionData == null )
                return;

            bool shouldShowNotifications = faction.HasBeenSeenByPlayer || faction.RandomImpact == TypeDifficulty.Unset || GameSettings.Current.GetBoolBySetting( "ShowRandomAIType" );
            if ( !shouldShowNotifications )
                return;

            if ( factionData.CurrentPlanetAimedAt != null && factionData.CurrentPlanetAimedAt.IntelLevel > PlanetIntelLevel.Unexplored )
            {
                WarChroniclerIncomingNotifier notifier = new WarChroniclerIncomingNotifier();
                notifier.planet = factionData.CurrentPlanetAimedAt;
                notifier.faction = faction;
                notifier.secondsLeft = SecondsOfConflictRequiredForArrival - factionData.SecondsAimedAtPlanet;
                notifier.estimatedStrength = (factionData.EstimatedStrengthOfAttack( faction, ChroniclersMarkLevel( faction ) ) / 4) * 3;

                NotificationNonSim notification = Engine_AIW2.NonSimNotificationList_Building.GetOrAddEntry();
                notification.Assign( notifier.ClickHandler, notifier.ContentGetter, notifier.MouseoverHandler, "", 0, "WarChroniclerIncoming", SortedNotificationPriorityLevel.Major );
            }

            if ( factionData.CurrentPlanetWeAreDepartingFrom != null )
            {
                WarChroniclerDepartingNotifier notifier = new WarChroniclerDepartingNotifier();
                notifier.planet = factionData.CurrentPlanetWeAreDepartingFrom;
                notifier.faction = faction;
                notifier.secondsLeft = SecondsOfPeaceRequiredForDeparture - factionData.SecondsSinceDepartingStarted;

                NotificationNonSim notification = Engine_AIW2.NonSimNotificationList_Building.GetOrAddEntry();
                notification.Assign( notifier.ClickHandler, notifier.ContentGetter, notifier.MouseoverHandler, "", 0, "WarChroniclerDeparting", SortedNotificationPriorityLevel.Minor );
            }
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            CalculateBudget( faction, Context );
        }

        public void CalculateBudget( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand studyCommand = Utilities.CreateGameCommand( GameCommandTypeTable.Instance.GetRowByName( Commands.UpdateStudyBudgets.ToString() ), GameCommandSource.AnythingElse, faction );
            studyCommand.RelatedFactionIndex = faction.FactionIndex;

            faction.DoForEntities( Tags.NeinzulWarChronicler.ToString(), chronicler =>
            {
                World_AIW2.Instance.DoForFactions( otherFaction =>
                {
                    if ( otherFaction.GetIsFriendlyTowards( faction ) || otherFaction.Implementation is NeinzulWarChroniclers )
                        return DelReturn.Continue;

                    chronicler.Planet.GetPlanetFactionForFaction( otherFaction ).Entities.DoForEntities( EntityRollupType.MobileCombatants, entity =>
                    {
                        if ( entity.TypeData.IsDrone || entity.TypeData.SelfAttritionsXPercentPerSecondIfParentShipNotOnPlanet != 0 || entity.TypeData.GetHasTag( Tags.NeinzulWarChronicler.ToString() ) )
                            return DelReturn.Continue;

                        if ( factionData.BudgetGenerated.GetHasKey(entity.TypeData.InternalName) && factionData.BudgetGenerated[entity.TypeData.InternalName] > entity.GetStrengthPerSquad() * 100 )
                            return DelReturn.Continue;

                        switch ( entity.TypeData.SpecialType )
                        {
                            case SpecialEntityType.AIKingMobile:
                            case SpecialEntityType.FactoryForPlayerMobileFleets:
                            case SpecialEntityType.DroneGeneral:
                            case SpecialEntityType.NPCFactionCenterpiece:
                            case SpecialEntityType.MobileSupportFleetFlagship:
                            case SpecialEntityType.MobileStrikeCombatFleetFlagship:
                            case SpecialEntityType.DroneFrigate:
                            case SpecialEntityType.MobileCustomStrikeCombatFleetFlagship:
                            case SpecialEntityType.Minefield:
                            case SpecialEntityType.Buttress:
                            case SpecialEntityType.Length:
                                return DelReturn.Continue;
                            default:
                                break;
                        }

                        studyCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                        studyCommand.RelatedIntegers.Add( BudgetPerSecond );

                        return DelReturn.Continue;
                    } );

                    return DelReturn.Continue;
                } );

                return DelReturn.Continue;
            } );

            Context.QueueCommandForSendingAtEndOfContext( studyCommand );
        }

        public bool PlanetHasConflict( Planet planet, Faction faction, bool factorOutPlanetsWeAreAlreadyOn = true )
        {
            if ( planet == null )
                return false;

            if ( factorOutPlanetsWeAreAlreadyOn && assignedChroniclers.GetHasKey( planet ) )
                return false;

            List<Faction> foundFactionsWithStrength = new List<Faction>();
            bool conflictPlanet = false;
            World_AIW2.Instance.DoForFactions( otherFaction =>
            {
                if ( otherFaction.Implementation is NeinzulWarChroniclers )
                    return DelReturn.Continue;

                int strength = planet.GetPlanetFactionForFaction( otherFaction ).DataByStance[FactionStance.Self].TotalStrength;
                if ( strength < 5000 )
                    return DelReturn.Continue;

                foundFactionsWithStrength.Add( otherFaction );

                for ( int x = 0; x < foundFactionsWithStrength.Count; x++ )
                {
                    Faction workingFaction = foundFactionsWithStrength[x];
                    if ( otherFaction == workingFaction )
                        continue;

                    if ( otherFaction.GetIsHostileTowards( workingFaction ) )
                    {
                        conflictPlanet = true;
                        return DelReturn.Break;
                    }
                }

                return DelReturn.Continue;
            } );

            return conflictPlanet;
        }
    }
}