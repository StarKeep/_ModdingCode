using System;
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

        public byte ChroniclersMarkLevel( Faction faction )
        {
            int workingIntensity = Intensity > 0 ? Intensity : faction.Ex_MinorFactionCommon_GetPrimitives().Intensity;
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

        public override void CheckIfPlayerHasSeenFaction( Faction faction, ArcenSimContext Context )
        {
            if ( faction.HasBeenSeenByPlayer )
                return;

            faction.DoForEntities( delegate ( GameEntity_Squad entity )
            {
                if ( entity.TypeData.GetHasTag( "Beacon" ) )  //don't flag beacons since those appear at game start
                    return DelReturn.Continue;
                if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                {
                    faction.HasBeenSeenByPlayer = true;
                    return DelReturn.Break;
                }
                return DelReturn.Continue;
            } );
        }

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( !LoadedConstants )
            {
                SecondsPerMarkUpBase = ExternalConstants.Instance.GetCustomData_Slow( "NeinzulWarChroniclers" ).GetInt_Slow( "SecondsPerMarkUpBase" );
                SecondsPerMarkUpDecreasePerIntensity = ExternalConstants.Instance.GetCustomData_Slow( "NeinzulWarChroniclers" ).GetInt_Slow( "SecondsPerMarkUpDecreasePerIntensity" );
                BudgetPerSecondBase = ExternalConstants.Instance.GetCustomData_Slow( "NeinzulWarChroniclers" ).GetInt_Slow( "BudgetPerSecondBase" );
                BudgetPerSecondIncreasePerIntensity = ExternalConstants.Instance.GetCustomData_Slow( "NeinzulWarChroniclers" ).GetInt_Slow( "BudgetPerSecondIncreasePerIntensity" );
                SecondsOfConflictRequiredForArrival = ExternalConstants.Instance.GetCustomData_Slow( "NeinzulWarChroniclers" ).GetInt_Slow( "SecondsOfConflictRequiredForArrival" );
                SecondsOfPeaceRequiredForDeparture = ExternalConstants.Instance.GetCustomData_Slow( "NeinzulWarChroniclers" ).GetInt_Slow( "SecondsOfPeaceRequiredForDeparture" );
                SoftStrengthCapBase = ExternalConstants.Instance.GetCustomData_Slow( "NeinzulWarChroniclers" ).GetInt_Slow( "SoftStrengthCapBase" );
                SoftStrengthCapIncreasePerAttack = ExternalConstants.Instance.GetCustomData_Slow( "NeinzulWarChroniclers" ).GetInt_Slow( "SoftStrengthCapIncreasePerAttack" );
                SoftStrengthCapIncreasePerAttackPerIntensity = ExternalConstants.Instance.GetCustomData_Slow( "NeinzulWarChroniclers" ).GetInt_Slow( "SoftStrengthCapIncreasePerAttackPerIntensity" );
                SoftStrengthCapIncreasePerHour = ExternalConstants.Instance.GetCustomData_Slow( "NeinzulWarChroniclers" ).GetInt_Slow( "SoftStrengthCapIncreasePerHour" );
                SoftStrengthCapIncreasePerHourPerIntensity = ExternalConstants.Instance.GetCustomData_Slow( "NeinzulWarChroniclers" ).GetInt_Slow( "SoftStrengthCapIncreasePerHourPerIntensity" );
                LoadedConstants = true;
            }
            if ( factionData == null )
            {
                factionData = faction.GetNeinzulWarChroniclersData();
            }
            if ( !Initialized )
            {
                Intensity = faction.Ex_MinorFactionCommon_GetPrimitives().Intensity;
                Allegiance = faction.Ex_MinorFactionCommon_GetPrimitives().Allegiance;
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
            UpdateAllegiance( faction );

            UpdatePersonalBudget( faction, Context );

            AggressiveLogic( faction, Context );

            DepartureLogic( faction, Context );

            StudyLogic( faction, Context );
        }

        public void UpdatePersonalBudget( Faction faction, ArcenSimContext Context )
        {
            int baseIncrease = 1500;
            int increaseFromIntensity = Intensity * 150;
            int capacity = 1 + Intensity / 2;
            int budgetCapacity = 1000000 * capacity;

            // Infuse them with a free early game spawn. Free of charge.
            if ( World_AIW2.Instance.GameSecond == 5 )
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
                if ( PlanetHasConflict( planet, faction ) && Context.RandomToUse.Next( 100 ) > 10 )
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
            GameEntity_Squad chronicler = factionData.CurrentPlanetAimedAt.Mapgen_SeedEntity( Context, faction, GameEntityTypeDataTable.Instance.GetRowByName( Tags.NeinzulWarChronicler.ToString() ), PlanetSeedingZone.OuterSystem );
            chronicler.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
            chronicler.SetCurrentMarkLevel( ChroniclersMarkLevel( faction ), Context );
            factionData.PersonalBudget -= 1000000;

            factionData.BudgetGenerated.DoFor( pair =>
            {
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( pair.Key );
                if ( entityData == null )
                    return DelReturn.RemoveAndContinue;

                pair.Value.DoFor( subPair =>
                {
                    int cost = entityData.GetForMark( subPair.Key ).StrengthPerSquad_CalculatedWithNullFleetMembership * 10;
                    int toSend = subPair.Value / cost;

                    for ( int x = 0; x < toSend; x++ )
                    {
                        GameEntity_Squad newSpawn = GameEntity_Squad.CreateNew( chronicler.PlanetFaction, entityData, subPair.Key, chronicler.PlanetFaction.FleetUsedAtPlanet, 0, chronicler.WorldLocation, Context );
                        newSpawn.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, faction.FactionIndex );
                    }

                    if ( toSend > 0 )
                    {
                        subPair.Value /= 5;
                        subPair.Value *= 4;
                    }

                    return DelReturn.Continue;
                } );

                return DelReturn.Continue;
            } );

            World_AIW2.Instance.QueueChatMessageOrCommand( "The " + faction.StartFactionColourForLog() + faction.GetDisplayName() + "</color> have arrived on " + factionData.CurrentPlanetAimedAt.Name, ChatType.ShowToEveryone, Context );

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

            GameEntity_Squad chronicler = pFaction.Entities.GetFirstMatching( Tags.NeinzulWarChronicler.ToString(), false, false );
            if ( chronicler != null )
            {
                chronicler.Despawn( Context, true, InstancedRendererDeactivationReason.IFinishedMyJob );
                factionData.PersonalBudget += (1000000 / 10) * Intensity; // Refund based on intensity.
            }

            pFaction.Entities.DoForEntities( other =>
            {
                other.Despawn( Context, true, InstancedRendererDeactivationReason.IFinishedMyJob );

                return DelReturn.Continue;
            } );

            World_AIW2.Instance.QueueChatMessageOrCommand( "The " + faction.StartFactionColourForLog() + faction.GetDisplayName() + "</color> have departed from " + factionData.CurrentPlanetWeAreDepartingFrom.Name, ChatType.ShowToEveryone, Context );

            factionData.CurrentPlanetWeAreDepartingFrom = null;
            factionData.GameSecondDepartingStarted = -1;
        }

        public void StudyLogic( Faction faction, ArcenSimContext Context )
        {
            int strengthCap = 0;
            strengthCap += SoftStrengthCapBase;
            strengthCap += SoftStrengthCapIncreasePerAttack * factionData.SentAttacks;
            strengthCap += SoftStrengthCapIncreasePerAttackPerIntensity * factionData.SentAttacks * Intensity;
            strengthCap += (World_AIW2.Instance.GameSecond * PerSecondStrengthCapIncrease).GetNearestIntPreferringHigher();

            if ( factionData.EstimatedStrengthOfAttack( faction ) / 1000 > strengthCap )
                return;

            faction.DoForEntities( Tags.NeinzulWarChronicler.ToString(), chronicler =>
            {
                World_AIW2.Instance.DoForFactions( otherFaction =>
                {
                    if ( otherFaction.GetIsFriendlyTowards( faction ) )
                        return DelReturn.Continue;

                    chronicler.Planet.GetPlanetFactionForFaction( otherFaction ).Entities.DoForEntities( EntityRollupType.MobileCombatants, entity =>
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
                                break;
                        }

                        factionData.AddBudget( entity, BudgetPerSecond );

                        return DelReturn.Continue;
                    } );

                    return DelReturn.Continue;
                } );

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
                notifier.estimatedStrength = (factionData.EstimatedStrengthOfAttack( faction ) / 4) * 3;

                NotificationNonSim notification = Engine_AIW2.NonSimNotificationList_Building.GetOrAddEntry();
                notification.Assign( notifier.ClickHandler, notifier.ContentGetter, notifier.MouseoverHandler, "", 0, "WarChroniclerIncoming" );
            }

            if ( factionData.CurrentPlanetWeAreDepartingFrom != null )
            {
                WarChroniclerDepartingNotifier notifier = new WarChroniclerDepartingNotifier();
                notifier.planet = factionData.CurrentPlanetWeAreDepartingFrom;
                notifier.faction = faction;
                notifier.secondsLeft = SecondsOfPeaceRequiredForDeparture - factionData.SecondsSinceDepartingStarted;

                NotificationNonSim notification = Engine_AIW2.NonSimNotificationList_Building.GetOrAddEntry();
                notification.Assign( notifier.ClickHandler, notifier.ContentGetter, notifier.MouseoverHandler, "", 0, "WarChroniclerDeparting" );
            }
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {

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