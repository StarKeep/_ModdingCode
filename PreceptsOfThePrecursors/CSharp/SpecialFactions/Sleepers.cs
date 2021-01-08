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
            SleeperDerelict,
            SleeperTurretStructure,
            SleeperPrimeTurretStructure,
            SleeperSpecialStructure,
            SleeperPrimeSpecialStructure,
            SleeperHarbingerStructure,
            SleeperPrimeHarbingerStructure,
            SleeperHanger,
            SleeperMobile,
            SleeperPrimeMobile,
            SleeperMobileTier1,
            SleeperMobileTier2,
            SleeperMobileTier3,
            SleeperMobileTier4,
            SleeperMobileTier5,
            SleeperPrimeMobileTier1,
            SleeperPrimeMobileTier2,
            SleeperPrimeMobileTier3,
            SleeperPrimeMobileTier4,
            SleeperPrimeMobileTier5
        }

        public enum COMMANDS
        {
            SetPrimeTarget,
            SetSleeperTargets
        }

        public enum MODE
        {
            Inactive,
            Primeless,
            Claiming,
            Defense
        }

        public enum CONSTANT_INT
        {
            MaxTurretStructuresPerPlanet,
            MaxSpecialStructuresPerPlanet,
            MaxHarbingerStructuresPerPlanet,
            HangerCapacityAtMark1,
            HangerCapacityAtMark2,
            HangerCapacityAtMark3,
            HangerCapacityAtMark4,
            HangerCapacityAtMark5,
            IntensityForTier2Units,
            IntensityForTier3Units,
            IntensityForTier4Units,
            IntensityForTier5Units,
            CPAStrengthCapAtIntensity1,
            CPAStrengthCapAtIntensity10,
            CPABudgetPerSecondPerHangerAtIntensity1,
            CPABudgetPerSecondPerHangerAtIntensity10
        }

        public enum CONSTANT_FINT
        {
            MetalGenerationPercAtIntensity1,
            MetalGenerationPercAtIntensity10,
            MetalGenerationRatioPutIntoUpgrades
        }

        private static readonly string ConstantBase = "custom";
        private enum CONSTANT_DATATYPE
        {
            Int32,
            FInt
        }
        private static readonly string ConstantInt = "int";
        private static readonly string ConstantFInt = "FInt";
        private static readonly string ConstantNamespace = "Sleepers";
        private static string GetConstantDataType( CONSTANT_DATATYPE dataType )
        {
            switch ( dataType )
            {
                case CONSTANT_DATATYPE.Int32:
                    return ConstantInt;
                case CONSTANT_DATATYPE.FInt:
                    return ConstantFInt;
                default:
                    return null;
            }
        }
        private static string GetConstantNameSpace( CONSTANT_DATATYPE dataType )
        {
            return $"{ConstantBase}_{GetConstantDataType( dataType )}_{ConstantNamespace}";
        }
        public static int GetConstantValue( CONSTANT_INT constantToGet )
        {
            return ExternalConstants.Instance.GetCustomInt32_Slow( $"{GetConstantNameSpace( CONSTANT_DATATYPE.Int32 )}_{constantToGet.ToString()}" );
        }
        public static FInt GetConstantValue( CONSTANT_FINT constantToGet )
        {
            return ExternalConstants.Instance.GetCustomFInt_Slow( $"{GetConstantNameSpace( CONSTANT_DATATYPE.FInt )}_{constantToGet.ToString()}" );
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
        public Planet PrimeNextPlanetToMoveTo;

        public ArcenSparseLookup<Planet, List<GameEntity_Squad>> sleepersByPlanet;

        private int turretCapPerPlanet = -1;
        public int TurretCapPerPlanet
        {
            get
            {
                if ( turretCapPerPlanet == -1 )
                    turretCapPerPlanet = Sleepers.GetConstantValue( Sleepers.CONSTANT_INT.MaxTurretStructuresPerPlanet );

                return turretCapPerPlanet;
            }
        }

        private int specialCapPerPlanet = -1;
        public int SpecialCapPerPlanet
        {
            get
            {
                if ( specialCapPerPlanet == -1 )
                    specialCapPerPlanet = Sleepers.GetConstantValue( Sleepers.CONSTANT_INT.MaxSpecialStructuresPerPlanet );

                return specialCapPerPlanet;
            }
        }

        private int harbingerCapPerPlanet = -1;
        public int HarbingerCapPerPlanet
        {
            get
            {
                if ( harbingerCapPerPlanet == -1 )
                    harbingerCapPerPlanet = Sleepers.GetConstantValue( Sleepers.CONSTANT_INT.MaxHarbingerStructuresPerPlanet );

                return harbingerCapPerPlanet;
            }
        }

        private FInt resourceMult = FInt.Zero;
        public FInt GetResourceMult( ExternalDataRetrieval rule )
        {
            if ( resourceMult == FInt.Zero )
            {
                FInt min = Sleepers.GetConstantValue( Sleepers.CONSTANT_FINT.MetalGenerationPercAtIntensity1 );
                FInt max = Sleepers.GetConstantValue( Sleepers.CONSTANT_FINT.MetalGenerationPercAtIntensity10 );
                byte intensity = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( Sleepers ) ).Ex_MinorFactionCommon_GetPrimitives( rule )?.Intensity ?? 0;
                if ( intensity > 0 )
                    switch ( intensity )
                    {
                        case 1:
                            resourceMult = min;
                            break;
                        case 10:
                            resourceMult = max;
                            break;
                        default:
                            FInt step = (max - min) / 9;
                            resourceMult = min + (step * (intensity - 1));
                            break;
                    }
            }
            return resourceMult;
        }

        private FInt upgradeCollectionPerc = FInt.Zero;
        public FInt GetUpgradeCollectionPerc( ExternalDataRetrieval rule )
        {
            if ( upgradeCollectionPerc == FInt.Zero )
                upgradeCollectionPerc = Sleepers.GetConstantValue( Sleepers.CONSTANT_FINT.MetalGenerationRatioPutIntoUpgrades );
            return upgradeCollectionPerc;
        }

        public bool CanLaunchCPAStrike( Faction faction )
        {
            return faction.GetFirstMatching( Sleepers.UNIT_TAGS.SleeperPrimeHarbingerStructure.ToString(), true, true ) != null;
        }
        private int maxCPAStrength = -1;
        public int StrengthCapOfCPAStrike( ExternalDataRetrieval rule )
        {
            if ( maxCPAStrength < 0 )
            {
                int min = Sleepers.GetConstantValue( Sleepers.CONSTANT_INT.CPAStrengthCapAtIntensity1 );
                int max = Sleepers.GetConstantValue( Sleepers.CONSTANT_INT.CPAStrengthCapAtIntensity10 );
                byte intensity = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( Sleepers ) ).Ex_MinorFactionCommon_GetPrimitives( rule )?.Intensity ?? 0;
                if ( intensity > 0 )
                    switch ( intensity )
                    {
                        case 1:
                            maxCPAStrength = min;
                            break;
                        case 10:
                            maxCPAStrength = max;
                            break;
                        default:
                            int step = (max - min) / 9;
                            maxCPAStrength = min + (step * (intensity - 1));
                            break;
                    }
            }
            return maxCPAStrength;
        }

        private int perSecondCPAStrikeBudgetPerHanger = -1;
        public int GetPerSecondCPAStrikeBudget( Faction faction, ExternalDataRetrieval rule )
        {
            if ( Prime == null )
                return 0;
            if ( perSecondCPAStrikeBudgetPerHanger < 0 )
            {
                int min = Sleepers.GetConstantValue( Sleepers.CONSTANT_INT.CPABudgetPerSecondPerHangerAtIntensity1 );
                int max = Sleepers.GetConstantValue( Sleepers.CONSTANT_INT.CPABudgetPerSecondPerHangerAtIntensity10 );
                byte intensity = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( Sleepers ) ).Ex_MinorFactionCommon_GetPrimitives( rule )?.Intensity ?? 0;
                if ( intensity > 0 )
                    switch ( intensity )
                    {
                        case 1:
                            perSecondCPAStrikeBudgetPerHanger = min;
                            break;
                        case 10:
                            perSecondCPAStrikeBudgetPerHanger = max;
                            break;
                        default:
                            int step = (max - min) / 9;
                            perSecondCPAStrikeBudgetPerHanger = min + (step * (intensity - 1));
                            break;
                    }
            }
            int hangerCount = 0;
            faction.DoForEntities( Sleepers.UNIT_TAGS.SleeperHanger.ToString(), hanger => { hangerCount++; return DelReturn.Continue; } );
            return hangerCount * perSecondCPAStrikeBudgetPerHanger;
        }

        public Sleepers.MODE CurrentMode { get { return GetMode(); } }

        public ArcenSparseLookup<Planet, ArcenSparseLookup<Planet, List<GameEntity_Squad>>> WormholeCommands { get; set; }
        public ArcenSparseLookup<Planet, ArcenSparseLookup<ArcenPoint, List<GameEntity_Squad>>> MovementCommands { get; set; }

        public Sleepers.MODE GetMode()
        {
            if ( Prime == null )
            {
                if ( (sleepersByPlanet?.GetPairCount() ?? 0) > 0 )
                    return Sleepers.MODE.Primeless;
                else
                    return Sleepers.MODE.Inactive;
            }
            else if ( (Sleepers.derelictsByPlanet?.GetPairCount() ?? 0) > 0 )
                return Sleepers.MODE.Claiming;
            else
                return Sleepers.MODE.Defense;
        }

        public bool PrimeCanMoveOn { get { return CanMoveOn( Prime ); } }
        public bool CanMoveOn( GameEntity_Squad sleeper )
        {
            switch ( GetMode() )
            {
                case Sleepers.MODE.Claiming:
                    return Prime != null && (sleeper.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.SecondsSinceEnteringPlanet ?? 0) > 120;
                case Sleepers.MODE.Defense:
                    return true;
                default:
                    return false;
            }
        }

        public ArcenSparseLookup<Planet, EntityCollection> CachedTurretStructuresByPlanet;
        public ArcenSparseLookup<Planet, EntityCollection> CachedSpecialStructuresByPlanet;
        public ArcenSparseLookup<Planet, EntityCollection> CachedHarbingerStructuresByPlanet;

        public EntityCollection GetTurretStructuresForPlanet( Planet planet, Faction faction )
        {
            if ( !CachedTurretStructuresByPlanet.GetHasKey( planet ) )
            {
                CachedTurretStructuresByPlanet[planet] = new EntityCollection();
                planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( Sleepers.UNIT_TAGS.SleeperTurretStructure.ToString(), turret =>
                {
                    CachedTurretStructuresByPlanet[planet].AddEntity( turret );

                    return DelReturn.Continue;
                } );
                planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( Sleepers.UNIT_TAGS.SleeperPrimeTurretStructure.ToString(), turret =>
                {
                    CachedTurretStructuresByPlanet[planet].AddEntity( turret );

                    return DelReturn.Continue;
                } );
            }

            return CachedTurretStructuresByPlanet[planet];
        }

        public EntityCollection GetSpecialStructuresForPlanet( Planet planet, Faction faction )
        {
            if ( !CachedSpecialStructuresByPlanet.GetHasKey( planet ) )
            {
                CachedSpecialStructuresByPlanet[planet] = new EntityCollection();
                planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( Sleepers.UNIT_TAGS.SleeperSpecialStructure.ToString(), turret =>
                {
                    CachedSpecialStructuresByPlanet[planet].AddEntity( turret );

                    return DelReturn.Continue;
                } );
                planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( Sleepers.UNIT_TAGS.SleeperPrimeSpecialStructure.ToString(), turret =>
                {
                    CachedSpecialStructuresByPlanet[planet].AddEntity( turret );

                    return DelReturn.Continue;
                } );
            }

            return CachedSpecialStructuresByPlanet[planet];
        }

        public GameEntityTypeData GetBuildableSpecialStructureEntityTypeForPlanet( Planet planet, Faction faction, ArcenSimContext Context, bool forPrime )
        {
            Sleepers.UNIT_TAGS tag = forPrime ? Sleepers.UNIT_TAGS.SleeperPrimeSpecialStructure : Sleepers.UNIT_TAGS.SleeperSpecialStructure;
            List<GameEntityTypeData> rawList = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( tag.ToString() );
            List<GameEntityTypeData> validList = new List<GameEntityTypeData>();
            for ( int x = 0; x < rawList.Count; x++ )
            {
                GameEntityTypeData workingType = rawList[x];
                int cap = 0;
                for ( int y = 0; y < workingType.TagsList.Count; y++ )
                {
                    string workingTag = workingType.TagsList[y];
                    if ( workingTag.Substring( 0, 8 ) == "SUnitCap" )
                        try
                        {
                            cap = int.Parse( workingTag.Substring( 8 ) );
                        }
                        catch ( Exception )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( $"Error! Failed to process the SUnitCap tag on {workingType.InternalName}. Please double check the syntax, as it should be SCap followed by a number, such as SUnitCap5.", Verbosity.ShowAsError );
                            continue;
                        }
                }
                if ( (GetSpecialStructuresForPlanet( planet, faction ).GetListOfEntitiesByEntityTypeOrNull( workingType )?.Count ?? 0) < cap )
                    validList.Add( workingType );
            }

            if ( validList.Count == 0 )
                return null;
            else
                return validList[Context.RandomToUse.Next( validList.Count )];
        }

        public EntityCollection GetHarbingerStructuresForPlanet( Planet planet, Faction faction )
        {
            if ( !CachedHarbingerStructuresByPlanet.GetHasKey( planet ) )
            {
                CachedHarbingerStructuresByPlanet[planet] = new EntityCollection();
                planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( Sleepers.UNIT_TAGS.SleeperHarbingerStructure.ToString(), turret =>
                {
                    CachedHarbingerStructuresByPlanet[planet].AddEntity( turret );

                    return DelReturn.Continue;
                } );
                planet.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( Sleepers.UNIT_TAGS.SleeperPrimeHarbingerStructure.ToString(), turret =>
                {
                    CachedHarbingerStructuresByPlanet[planet].AddEntity( turret );

                    return DelReturn.Continue;
                } );
            }

            return CachedHarbingerStructuresByPlanet[planet];
        }

        public GameEntityTypeData GetBuildableHarbingerStructureEntityTypeForPlanet( Planet planet, Faction faction, ArcenSimContext Context, bool forPrime )
        {
            Sleepers.UNIT_TAGS tag = forPrime ? Sleepers.UNIT_TAGS.SleeperPrimeHarbingerStructure : Sleepers.UNIT_TAGS.SleeperHarbingerStructure;
            if ( GetHarbingerStructuresForPlanet( planet, faction ).SquadCount >= HarbingerCapPerPlanet )
                return null;
            else
                return GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, tag.ToString() );
        }

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            Prime = null;
            Prime = faction.GetFirstMatching( Sleepers.UNIT_TAGS.SleeperPrime.ToString(), true, true );

            if ( sleepersByPlanet == null )
                sleepersByPlanet = new ArcenSparseLookup<Planet, List<GameEntity_Squad>>();
            else
                sleepersByPlanet.Clear();

            faction.DoForEntities( Sleepers.UNIT_TAGS.Sleeper.ToString(), sleeper =>
            {
                sleeper.AddToPerPlanetLookup( ref sleepersByPlanet );

                return DelReturn.Continue;
            } );

            if ( CachedTurretStructuresByPlanet == null )
                CachedTurretStructuresByPlanet = new ArcenSparseLookup<Planet, EntityCollection>();
            else
                CachedTurretStructuresByPlanet.Clear();

            if ( CachedSpecialStructuresByPlanet == null )
                CachedSpecialStructuresByPlanet = new ArcenSparseLookup<Planet, EntityCollection>();
            else
                CachedSpecialStructuresByPlanet.Clear();

            if ( CachedHarbingerStructuresByPlanet == null )
                CachedHarbingerStructuresByPlanet = new ArcenSparseLookup<Planet, EntityCollection>();
            else
                CachedHarbingerStructuresByPlanet.Clear();
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( CurrentMode == Sleepers.MODE.Inactive )
                return;

            HandleSleeperPerSecondLogic( faction, Context );
            TransformSleepers( faction, Context );
            switch ( CurrentMode )
            {
                case Sleepers.MODE.Inactive:
                    break;
                case Sleepers.MODE.Primeless:
                    break;
                case Sleepers.MODE.Claiming:
                    AwakenDerelictsIfAble( faction, Context );
                    break;
                case Sleepers.MODE.Defense:
                    GenerateResourcesIfAble( faction, Context );
                    BuildDefensiveStructuresIfAble( faction, Context );
                    UpgradeStructuresIfAble( faction, Context );
                    break;
                default:
                    break;
            }
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
            if ( sData.OriginalID == -1 )
                sData.OriginalID = sleeper.PrimaryKeyID;
        }

        public void TransformSleepers( Faction faction, ArcenSimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            if ( Prime != null )
            {
                GameEntityTypeData primeMobile = GameEntityTypeDataTable.Instance.GetRowByName( Sleepers.UNIT_NAMES.SleeperPrimeMobile.ToString() );
                GameEntityTypeData primeStationary = GameEntityTypeDataTable.Instance.GetRowByName( Sleepers.UNIT_NAMES.SleeperPrime.ToString() );
                SleeperData spData = Prime.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound );
                Planet PrimeTargetPlanet = spData.TargetPlanet;
                ArcenPoint PrimeTarget = spData.TargetPoint;
                if ( !PrimeCanMoveOn || (Prime.Planet == PrimeTargetPlanet && Prime.WorldLocation.GetDistanceTo( PrimeTarget, true ) <= 5000) )
                {
                    if ( Prime.TypeData != primeStationary )
                    {
                        spData.SecondLastTransformed = World_AIW2.Instance.GameSecond;
                        Prime.SetWorldLocation( Prime.Planet.GetSafePlacementPoint( Context, primeStationary, PrimeTarget, 0, 2000 ) );
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
                        SleeperData sData = sleeper.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound );

                        Planet targetPlanet = sData.TargetPlanet;
                        ArcenPoint targetPoint = sData.TargetPoint;

                        if ( targetPlanet == null || targetPoint == ArcenPoint.ZeroZeroPoint )
                            continue;

                        if ( !CanMoveOn( sleeper ) || sleeper.Planet == targetPlanet && targetPoint != ArcenPoint.OutOfRange
                        && sleeper.WorldLocation.GetExtremelyRoughDistanceTo( targetPoint ) <= 5000 )
                        {
                            if ( sleeper.TypeData != sleeperStationary )
                            {
                                sData.SecondLastTransformed = World_AIW2.Instance.GameSecond;
                                sleeper.SetWorldLocation( sleeper.Planet.GetSafePlacementPoint( Context, sleeperStationary, targetPoint, 0, 2000 ) );
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
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            if ( Prime == null || Prime.Planet != Prime.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound ).Planet || Prime.TypeData.InternalName == Sleepers.UNIT_NAMES.SleeperPrimeMobile.ToString() )
                return;

            if ( Prime.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound ).SecondsSinceLastTransformation < 300 )
                return;

            GameEntity_Squad derelict = Prime.Planet.GetFirstMatching( Sleepers.UNIT_TAGS.SleeperDerelict.ToString(), World_AIW2.Instance.GetNeutralFaction(), true, true );
            if ( derelict == null )
                return;

            GameEntity_Squad.CreateNew_ReturnNullIfMPClient( Prime.PlanetFaction, GameEntityTypeDataTable.Instance.GetRowByName( Sleepers.UNIT_NAMES.Sleeper.ToString() ), 7, Prime.PlanetFaction.FleetUsedAtPlanet, 0, derelict.WorldLocation, Context );
            derelict.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );
        }

        public virtual void GenerateResourcesIfAble( Faction faction, ArcenSimContext Context )
        {
            if ( Prime?.Planet == Prime?.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound ).TargetPlanet )
                Prime.Planet.DoForEntities( EntityRollupType.MetalProducers, generator =>
                {
                    int metalGenerated = (generator.DataForMark.GetResourceProduction( ResourceType.Metal ) * GetResourceMult( ExternalDataRetrieval.CreateIfNotFound )).GetNearestIntPreferringHigher();
                    int metalForUpgrades = (metalGenerated * GetUpgradeCollectionPerc( ExternalDataRetrieval.CreateIfNotFound )).GetNearestIntPreferringHigher();
                    int metalForConstruction = metalGenerated - metalForUpgrades;
                    Prime.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound ).StoredMetalForUpgrading += metalForUpgrades;
                    Prime.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound ).StoredMetalForConstruction += metalForConstruction;

                    return DelReturn.Continue;
                } );

            sleepersByPlanet.DoFor( pair =>
            {
                if ( pair.Value.Count <= 0 )
                    return DelReturn.Continue;

                int generated = 0;
                int metalForUpgrades = 0;
                int metalForConstruction = 0;
                pair.Key.DoForEntities( EntityRollupType.MetalProducers, generator =>
                {
                    generated += (generator.DataForMark.GetResourceProduction( ResourceType.Metal ) * GetResourceMult( ExternalDataRetrieval.CreateIfNotFound )).GetNearestIntPreferringHigher();
                    metalForUpgrades = (generated * GetUpgradeCollectionPerc( ExternalDataRetrieval.CreateIfNotFound )).GetNearestIntPreferringHigher();
                    metalForConstruction = generated - metalForUpgrades;

                    return DelReturn.Continue;
                } );

                for ( int x = 0; x < pair.Value.Count; x++ )
                {
                    GameEntity_Squad workingSleeper = pair.Value[x];
                    SleeperData sData = workingSleeper?.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound );
                    if ( workingSleeper.Planet == sData.TargetPlanet && sData != null )
                    {
                        sData.StoredMetalForUpgrading += metalForUpgrades;
                        sData.StoredMetalForConstruction += metalForConstruction;
                    }
                }

                return DelReturn.Continue;
            } );
        }

        public virtual void BuildDefensiveStructuresIfAble( Faction faction, ArcenSimContext Context )
        {
            if ( Prime?.Planet == Prime.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound ).TargetPlanet )
                BuildPrimeStructuresIfAble( faction, Context );
            else
                Prime.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound ).NextEntityToBuild = null;

            faction.DoForEntities( Sleepers.UNIT_TAGS.Sleeper.ToString(), sleeper =>
            {
                if ( sleeper.Planet == sleeper.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound ).TargetPlanet )
                    BuildSleeperStructuresIfAble( sleeper, faction, Context );
                else
                    sleeper.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound ).NextEntityToBuild = null;

                return DelReturn.Continue;
            } );
        }

        public void UpgradeStructuresIfAble( Faction faction, ArcenSimContext Context )
        {
            if ( Prime != null )
            {
                SleeperData sData = Prime.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound );
                if ( Prime.Planet == sData.TargetPlanet )
                    for ( byte x = 2; x <= 4; x++ )
                    {
                        bool needsUpgrade = false;
                        Prime.PlanetFaction.Entities.DoForEntities( ( GameEntity_Squad entity ) =>
                        {
                            if ( entity.CurrentMarkLevel < x )
                            {
                                needsUpgrade = true;
                                if ( Prime.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound ).StoredMetalForUpgrading > entity.TypeData.MetalCost )
                                {
                                    entity.SetCurrentMarkLevel( x, Context );
                                    sData.StoredMetalForUpgrading -= entity.TypeData.MetalCost;
                                }
                            }
                            return DelReturn.Continue;
                        } );
                        if ( needsUpgrade )
                            break;
                    }
            }
            sleepersByPlanet.DoFor( pair =>
            {
                for ( byte x = 2; x <= 4; x++ )
                {
                    bool needsUpgrade = false;
                    pair.Key.GetPlanetFactionForFaction( faction ).Entities.DoForEntities( ( GameEntity_Squad entity ) =>
                    {
                        if ( entity.CurrentMarkLevel < x )
                        {
                            needsUpgrade = true;
                            for ( int y = 0; y < pair.Value.Count; y++ )
                            {
                                SleeperData sData = pair.Value[y].GetSleeperData( ExternalDataRetrieval.CreateIfNotFound );
                                if ( sData.StoredMetalForUpgrading >= entity.TypeData.MetalCost )
                                {
                                    entity.SetCurrentMarkLevel( x, Context );
                                    sData.StoredMetalForUpgrading -= entity.TypeData.MetalCost;
                                    break;
                                }
                            }
                        }

                        return DelReturn.Continue;
                    } );

                    if ( needsUpgrade )
                        break;
                }

                return DelReturn.Continue;
            } );
        }

        public abstract void BuildPrimeStructuresIfAble( Faction faction, ArcenSimContext Context );

        public abstract void BuildSleeperStructuresIfAble( GameEntity_Squad sleeper, Faction faction, ArcenSimContext Context );

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            switch ( CurrentMode )
            {
                case Sleepers.MODE.Claiming:
                    HandleSleepersBackgroundClaimingLogic( faction, Context );
                    break;
                case Sleepers.MODE.Defense:
                    HandleSleepersBackgroundDefensiveLogic( faction, Context );
                    break;
                default:
                    break;
            }

            ExecuteMovementForSleepers( faction, Context );

            ExecuteMovementForPrime( faction, Context );

            faction.ExecuteMovementCommands( Context );
            faction.ExecuteWormholeCommands( Context );
        }

        public void HandleSleepersBackgroundDefensiveLogic( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( Prime?.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound ) != null )
            {
                PlanDefensiveMovementForPrime( faction, Context );

                if ( sleepersByPlanet != null )
                    PlanDefensiveMovementForSleepers( faction, Context );
            }
        }


        public virtual void HandleSleepersBackgroundClaimingLogic( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( Prime != null )
            {
                SleeperData sData = Prime.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound );
                if ( sData == null )
                    return;
                PlanClaimingMovementForPrime( faction, Context );
                if ( PrimeNextPlanetToMoveTo == Prime.Planet )
                    PrimeNextPlanetToMoveTo = Prime.QueueWormholeCommand( sData.TargetPlanet, Context, true, PathingMode.Shortest );

                if ( sleepersByPlanet != null )
                    PlanClaimingMovementForSleepers( faction, Context );
            }
        }

        public void PlanClaimingMovementForPrime( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            Planet nearestPlanet = null;
            Sleepers.derelictsByPlanet.DoFor( pair =>
            {
                if ( nearestPlanet == null ||
                Prime.Planet.GetHopsTo( pair.Key ) < Prime.Planet.GetHopsTo( nearestPlanet ) ||
                (Prime.Planet.GetHopsTo( pair.Key ) == Prime.Planet.GetHopsTo( nearestPlanet ) && pair.Key.Index < nearestPlanet.Index) )
                    nearestPlanet = pair.Key;

                return DelReturn.Continue;
            } );

            ArcenPoint targetPoint = Sleepers.derelictsByPlanet[nearestPlanet][0].WorldLocation;

            GameCommand targetCommand = StaticMethods.CreateGameCommand( Sleepers.COMMANDS.SetPrimeTarget.ToString(), GameCommandSource.AnythingElse, faction );
            targetCommand.RelatedIntegers.Add( nearestPlanet.Index );
            targetCommand.RelatedPoints.Add( targetPoint );
            Context.QueueCommandForSendingAtEndOfContext( targetCommand );
        }

        public abstract void PlanDefensiveMovementForPrime( Faction faction, ArcenLongTermIntermittentPlanningContext Context );

        public void ExecuteMovementForPrime( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( Prime == null )
                return;

            SleeperData sData = Prime.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound );
            if ( sData == null )
                return;

            if ( sData.TargetPlanet == null || sData.TargetPoint == ArcenPoint.ZeroZeroPoint )
                return;


            if ( Prime.Planet != sData.TargetPlanet )
                PrimeNextPlanetToMoveTo = Prime.QueueWormholeCommand( sData.TargetPlanet, Context, !PrimeCanMoveOn, PathingMode.Shortest );
            else if ( Prime.WorldLocation.GetDistanceTo( sData.TargetPoint, true ) > 4000 )
                Prime.QueueMovementCommand( sData.TargetPoint );
        }

        public void PlanClaimingMovementForSleepers( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand command = StaticMethods.CreateGameCommand( Sleepers.COMMANDS.SetSleeperTargets.ToString(), GameCommandSource.AnythingElse, faction );

            sleepersByPlanet.DoFor( pair =>
            {
                pair.Value.Sort( ( pair1, pair2 ) =>
                {
                    return (pair1.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.OriginalID ?? 1).CompareTo(
                        pair2.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.OriginalID ?? 2 );
                } );

                for ( int x = 0; x < pair.Value.Count; x++ )
                {
                    command.RelatedEntityIDs.Add( pair.Value[x].PrimaryKeyID );
                    command.RelatedIntegers.Add( Prime.Planet.Index );
                    command.RelatedPoints.Add( Prime.WorldLocation.GetPointAtAngleAndDistance( AngleDegrees.Create( 45 * x ), 1000 ) );
                }

                return DelReturn.Continue;
            } );

            Context.QueueCommandForSendingAtEndOfContext( command );
        }

        public abstract void PlanDefensiveMovementForSleepers( Faction faction, ArcenLongTermIntermittentPlanningContext Context );

        public void ExecuteMovementForSleepers( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            faction.DoForEntities( Sleepers.UNIT_TAGS.Sleeper.ToString(), sleeper =>
            {
                SleeperData sData = sleeper.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound );
                if ( sData == null )
                    return DelReturn.Continue;

                if ( sData.TargetPlanet != null && sleeper.Planet != sData.TargetPlanet )
                    sleeper.QueueWormholeCommand( sData.TargetPlanet, Context, false, PathingMode.Shortest );
                else if ( sData.TargetPoint != ArcenPoint.ZeroZeroPoint )
                    sleeper.QueueMovementCommand( sData.TargetPoint );


                return DelReturn.Continue;
            } );
        }

        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking( Faction faction, ArcenSimContext Context, bool IsFirstCallToFactionOfThisTypeThisCycle )
        {
            if ( Prime?.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound )?.TargetPlanet == null )
                return;

            switch ( CurrentMode )
            {
                case Sleepers.MODE.Inactive:
                    break;
                case Sleepers.MODE.Primeless:
                    break;
                case Sleepers.MODE.Claiming:
                    HandleClaimingNotifications( faction, Context );
                    break;
                case Sleepers.MODE.Defense:
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
            notifier.finalPlanet = Prime.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound ).TargetPlanet;

            if ( Prime.Planet != Prime.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound ).TargetPlanet )
                notifier.CurrentMode = PrimeClaimingNotifier.Mode.Moving;
            else
                notifier.CurrentMode = PrimeClaimingNotifier.Mode.Awakening;

            NotificationNonSim notification = Engine_AIW2.NonSimNotificationList_Building.GetOrAddEntry();
            notification.Assign( notifier.ClickHandler, notifier.ContentGetter, notifier.MouseoverHandler, "", 0, "SleepersClaimPrimeMovement", SortedNotificationPriorityLevel.Informational );
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

        public override void BuildPrimeStructuresIfAble( Faction faction, ArcenSimContext Context )
        {
            SleeperData sData = Prime.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound );
            if ( sData.NextEntityToBuild == null )
            {
                if ( GetTurretStructuresForPlanet( Prime.Planet, faction ).SquadCount < TurretCapPerPlanet )
                    sData.NextEntityToBuild = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, Sleepers.UNIT_TAGS.SleeperPrimeTurretStructure.ToString() );
                else if ( GetSpecialStructuresForPlanet( Prime.Planet, faction ).SquadCount < SpecialCapPerPlanet )
                    sData.NextEntityToBuild = GetBuildableSpecialStructureEntityTypeForPlanet( Prime.Planet, faction, Context, true );
                else if ( GetHarbingerStructuresForPlanet( Prime.Planet, faction ).SquadCount < HarbingerCapPerPlanet )
                    sData.NextEntityToBuild = GetBuildableHarbingerStructureEntityTypeForPlanet( Prime.Planet, faction, Context, true );
                else
                {
                    sData.StoredMetalForUpgrading += sData.StoredMetalForConstruction;
                    sData.StoredMetalForConstruction = 0;
                }
            }
            else if ( sData.StoredMetalForConstruction >= sData.NextEntityToBuild.MetalCost )
            {
                sData.StoredMetalForConstruction -= sData.NextEntityToBuild.MetalCost;
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                {
                    // Build structures randomly in the middle of the planet directed towards wormholes.
                    ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
                    List<AngleDegrees> potentialAngles = new List<AngleDegrees>();
                    int distance = 0;
                    Prime.Planet.DoForLinkedNeighbors( false, adjPlanet =>
                    {
                        potentialAngles.Add( center.GetAngleToDegrees( Prime.Planet.GetWormholeTo( adjPlanet ).WorldLocation ) );

                        distance = center.GetDistanceTo( Prime.Planet.GetWormholeTo( adjPlanet ).WorkingDestination_OnlyWriteFromMovementPlanning, true );

                        return DelReturn.Continue;
                    } );

                    int nextIndex = GetTurretStructuresForPlanet( Prime.Planet, faction ).SquadCount % potentialAngles.Count;
                    if ( GetTurretStructuresForPlanet( Prime.Planet, faction ).SquadCount >= TurretCapPerPlanet )
                        nextIndex = GetSpecialStructuresForPlanet( Prime.Planet, faction ).SquadCount % potentialAngles.Count;
                    else if ( GetSpecialStructuresForPlanet( Prime.Planet, faction ).SquadCount >= SpecialCapPerPlanet )
                        nextIndex = GetHarbingerStructuresForPlanet( Prime.Planet, faction ).SquadCount % potentialAngles.Count;

                    AngleDegrees angle = potentialAngles[nextIndex];

                    FInt placementDistance = FInt.FromParts( 0, 500 );
                    if ( GetTurretStructuresForPlanet( Prime.Planet, faction ).SquadCount >= TurretCapPerPlanet )
                        placementDistance = FInt.FromParts( 0, 350 );
                    else if ( GetSpecialStructuresForPlanet( Prime.Planet, faction ).SquadCount >= SpecialCapPerPlanet )
                        placementDistance = FInt.FromParts( 0, 200 );
                    ArcenPoint rawPoint = center.GetPointAtAngleAndDistance( angle, (distance * placementDistance).IntValue );
                    ArcenPoint spawnPoint = Prime.Planet.GetSafePlacementPoint( Context, sData.NextEntityToBuild, rawPoint, 0, 2500 );

                    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( Prime.PlanetFaction, sData.NextEntityToBuild, 1, Prime.PlanetFaction.FleetUsedAtPlanet, 0, spawnPoint, Context );
                }
                sData.NextEntityToBuild = null;
            }
        }

        public override void BuildSleeperStructuresIfAble( GameEntity_Squad sleeper, Faction faction, ArcenSimContext Context )
        {
            SleeperData sData = sleeper.GetSleeperData( ExternalDataRetrieval.CreateIfNotFound );
            if ( sData.NextEntityToBuild == null )
            {
                if ( GetTurretStructuresForPlanet( sleeper.Planet, faction ).SquadCount < TurretCapPerPlanet )
                    sData.NextEntityToBuild = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, Sleepers.UNIT_TAGS.SleeperTurretStructure.ToString() );
                else if ( GetSpecialStructuresForPlanet( sleeper.Planet, faction ).SquadCount < SpecialCapPerPlanet )
                    sData.NextEntityToBuild = GetBuildableSpecialStructureEntityTypeForPlanet( sleeper.Planet, faction, Context, false );
                else if ( GetHarbingerStructuresForPlanet( sleeper.Planet, faction ).SquadCount < HarbingerCapPerPlanet )
                    sData.NextEntityToBuild = GetBuildableHarbingerStructureEntityTypeForPlanet( sleeper.Planet, faction, Context, false );
                else
                {
                    sData.StoredMetalForUpgrading += sData.StoredMetalForConstruction;
                    sData.StoredMetalForConstruction = 0;
                }
            }
            else if ( sData.StoredMetalForConstruction >= sData.NextEntityToBuild.MetalCost )
            {
                sData.StoredMetalForConstruction -= sData.NextEntityToBuild.MetalCost;
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                {
                    ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
                    List<AngleDegrees> potentialAngles = new List<AngleDegrees>();
                    int distance = 0;
                    sleeper.Planet.DoForLinkedNeighbors( false, adjPlanet =>
                    {
                        potentialAngles.Add( center.GetAngleToDegrees( sleeper.Planet.GetWormholeTo( adjPlanet ).WorldLocation ) );

                        distance = center.GetDistanceTo( sleeper.Planet.GetWormholeTo( adjPlanet ).WorkingDestination_OnlyWriteFromMovementPlanning, true );

                        return DelReturn.Continue;
                    } );

                    int nextIndex = GetTurretStructuresForPlanet( sleeper.Planet, faction ).SquadCount % potentialAngles.Count;
                    if ( GetTurretStructuresForPlanet( sleeper.Planet, faction ).SquadCount >= TurretCapPerPlanet )
                        nextIndex = GetSpecialStructuresForPlanet( sleeper.Planet, faction ).SquadCount % potentialAngles.Count;
                    else if ( GetSpecialStructuresForPlanet( sleeper.Planet, faction ).SquadCount >= SpecialCapPerPlanet )
                        nextIndex = GetHarbingerStructuresForPlanet( sleeper.Planet, faction ).SquadCount % potentialAngles.Count;

                    AngleDegrees angle = potentialAngles[nextIndex];

                    FInt placementDistance = FInt.FromParts( 0, 500 );
                    if ( GetTurretStructuresForPlanet( sleeper.Planet, faction ).SquadCount >= TurretCapPerPlanet )
                        placementDistance = FInt.FromParts( 0, 350 );
                    else if ( GetSpecialStructuresForPlanet( sleeper.Planet, faction ).SquadCount >= SpecialCapPerPlanet )
                        placementDistance = FInt.FromParts( 0, 200 );
                    ArcenPoint rawPoint = center.GetPointAtAngleAndDistance( angle, (distance * placementDistance).IntValue );
                    ArcenPoint spawnPoint = sleeper.Planet.GetSafePlacementPoint( Context, sData.NextEntityToBuild, rawPoint, 0, 2500 );

                    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( sleeper.PlanetFaction, sData.NextEntityToBuild, 1, sleeper.PlanetFaction.FleetUsedAtPlanet, 0, spawnPoint, Context );
                }
                sData.NextEntityToBuild = null;
            }
        }

        public override void PlanDefensiveMovementForPrime( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            SleeperData sData = Prime?.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound );
            if ( sData == null )
                return;

            ArcenSparseLookup<Planet, GameEntity_Squad> humanKings = new ArcenSparseLookup<Planet, GameEntity_Squad>();
            List<GameEntity_Squad> allHumanKings = FactionUtilityMethods.findAllHumanKings();
            if ( allHumanKings == null )
                return;
            for ( int x = 0; x < allHumanKings.Count; x++ )
                humanKings.AddPair( allHumanKings[x].Planet, allHumanKings[x] );

            ArcenSparseLookupPair<Planet, GameEntity_Squad> nearestHumanKing = humanKings.GetPairByIndex( 0 );
            if ( humanKings.Count > 0 )
                humanKings.DoFor( pair =>
                {
                    if ( Prime.Planet.GetHopsTo( pair.Key ) < Prime.Planet.GetHopsTo( nearestHumanKing.Key ) )
                        nearestHumanKing = pair;

                    return DelReturn.Continue;
                } );

            if ( sData.TargetPlanet == nearestHumanKing.Key && sData.TargetPoint.GetExtremelyRoughDistanceTo( nearestHumanKing.Value.WorldLocation ) < 5000 )
                return;

            GameCommand targetCommand = StaticMethods.CreateGameCommand( Sleepers.COMMANDS.SetPrimeTarget.ToString(), GameCommandSource.AnythingElse, faction );
            targetCommand.RelatedIntegers.Add( nearestHumanKing.Key.Index );
            targetCommand.RelatedPoints.Add( nearestHumanKing.Key.GetSafePlacementPoint( Context, Prime.TypeData, Engine_AIW2.Instance.CombatCenter, 0, 2500 ) );
            Context.QueueCommandForSendingAtEndOfContext( targetCommand );
        }

        public override void PlanDefensiveMovementForSleepers( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand command = StaticMethods.CreateGameCommand( Sleepers.COMMANDS.SetSleeperTargets.ToString(), GameCommandSource.AnythingElse, faction );

            ArcenSparseLookup<Planet, int> humanPlanets = new ArcenSparseLookup<Planet, int>();
            World_AIW2.Instance.DoForPlanets( false, planet =>
            {
                if ( planet.GetControllingOrInfluencingFaction().GetIsFriendlyTowards( faction ) )
                {
                    // Distance is the most important, making them gain a huge priority if they are within a few hops of the homeworld
                    int distanceValue = -9999;
                    for ( int x = 0; x < FactionUtilityMethods.findAllHumanKings().Count; x++ )
                        distanceValue = Math.Max( distanceValue, 5 - planet.GetHopsTo( FactionUtilityMethods.findAllHumanKings()[x].Planet ) );

                    // Special Structures is the next important; spread out and try to evenly build them, using the above distance modifier to hopefully build in layers
                    int structureValue = SpecialCapPerPlanet - GetSpecialStructuresForPlanet( planet, faction ).SquadCount;

                    // Next, Focus on evenly spreading out our Harbinger values.
                    int harbingerValue = 0 - GetHarbingerStructuresForPlanet( planet, faction ).SquadCount;

                    // Finally, add a slight bonus for every hostile adjacent planet.
                    int hostileNeighborsValue = 0;
                    planet.DoForLinkedNeighbors( false, adjPlanet =>
                    {
                        if ( adjPlanet.GetControllingOrInfluencingFaction()?.GetIsHostileTowards( faction ) ?? false )
                            hostileNeighborsValue++;

                        return DelReturn.Continue;
                    } );

                    // Calculate the final values.
                    int finalValue = distanceValue * 5;
                    if ( structureValue > 0 )
                        finalValue += structureValue * 3;
                    finalValue += harbingerValue * 2;
                    finalValue += hostileNeighborsValue;

                    humanPlanets.AddPair( planet, finalValue );
                }

                return DelReturn.Continue;
            } );

            humanPlanets.Sort( ( pair1, pair2 ) =>
            {
                return -pair1.Value.CompareTo( pair2.Value ) + pair1.Key.Name.CompareTo( pair2.Key.Name );
            } );

            List<GameEntity_Squad> sleepersAwaitingOrders = new List<GameEntity_Squad>();
            sleepersByPlanet.DoFor( pair =>
            {
                for ( int x = 0; x < pair.Value.Count; x++ )
                    sleepersAwaitingOrders.Add( pair.Value[x] );

                return DelReturn.Continue;
            } );

            sleepersAwaitingOrders.Sort( ( sleeper1, sleeper2 ) =>
            {
                return (sleeper1.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.OriginalID ?? 0).CompareTo( (sleeper2.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.OriginalID ?? 1) );

            } );

            while ( sleepersAwaitingOrders.Count > 0 )
            {
                humanPlanets.DoFor( targetPair =>
                {
                    if ( (Prime?.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.TargetPlanet) == targetPair.Key || sleepersAwaitingOrders.Count <= 0 )
                        return DelReturn.Continue;

                    GameEntity_Squad nearestSleeper = null;
                    for ( int x = 0; x < sleepersAwaitingOrders.Count; x++ )
                    {
                        GameEntity_Squad workingSleeper = sleepersAwaitingOrders[x];
                        if ( nearestSleeper == null )
                        {
                            nearestSleeper = workingSleeper;
                            continue;
                        }

                        if ( (workingSleeper.Planet.GetHopsTo( targetPair.Key ) < nearestSleeper.Planet.GetHopsTo( targetPair.Key )) ||
                        (workingSleeper.Planet.GetHopsTo( targetPair.Key ) == nearestSleeper.Planet.GetHopsTo( targetPair.Key ) &&
                        workingSleeper?.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.Planet == targetPair.Key) )
                            nearestSleeper = workingSleeper;
                    }

                    if ( nearestSleeper != null )
                    {
                        sleepersAwaitingOrders.Remove( nearestSleeper );
                        command.RelatedEntityIDs.Add( nearestSleeper.PrimaryKeyID );
                        command.RelatedIntegers.Add( targetPair.Key.Index );
                        command.RelatedPoints.Add( targetPair.Key.GetSafePlacementPoint( Context, Prime.TypeData, Engine_AIW2.Instance.CombatCenter, 0, 2500 ) );
                    }

                    return DelReturn.Continue;
                } );
            }

            if ( command.RelatedEntityIDs.Count > 0 )
                Context.QueueCommandForSendingAtEndOfContext( command );
        }
    }
}