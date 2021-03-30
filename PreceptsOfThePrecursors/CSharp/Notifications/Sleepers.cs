using System;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace PreceptsOfThePrecursors.Notifications
{
    public class PrimeClaimingNotifier : BasicPoolable
    {
        public Planet planet;
        public Faction faction;
        public GameEntity_Squad entity;
        public Planet nextPlanet;
        public Planet finalPlanet;
        public enum Mode
        {
            Moving,
            Awakening
        }
        public Mode CurrentMode;

        //these are just data from asset bundles, that's definitely okay to be static
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
        }

        public MouseHandlingResult ClickHandler()
        {
            if ( planet != null && entity != null && entity.GetShouldBeVisibleBasedOnPlanetIntel() )
            {
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                    Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( planet, false );
                else
                    World_AIW2.Instance.SwitchViewToPlanet( planet );
                return MouseHandlingResult.None;
            }
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer();
        public bool MouseoverHandler()
        {
            //galaxy map hover
            Planet.SetCurrentlySecondaryHoveredOver( planet );

            tooltipBuffer.Clear();

            switch ( CurrentMode )
            {
                case Mode.Moving:
                    tooltipBuffer.Add( $"A Sleeper Prime on {planet.Name} is on the move! " );
                    if ( !(faction.Implementation as SleeperSubFaction).PrimeCanMoveOn )
                        tooltipBuffer.Add( $"\nIt is currently recharging its engines after entering a wormhole, and will be ready to move in {(120 - (entity.GetSleeperData(ExternalDataRetrieval.ReturnNullIfNotFound)?.SecondsSinceEnteringPlanet ?? 120)) / 60}:{((120 - (entity.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.SecondsSinceEnteringPlanet ?? 120)) % 60).ToString( "00" )}. " );
                    if ( nextPlanet != null )
                        tooltipBuffer.Add( $"\nIt will be moving to {nextPlanet.Name} next." );
                    if ( finalPlanet != null )
                        tooltipBuffer.Add( $"\nIts final destination is {(finalPlanet.IntelLevel > PlanetIntelLevel.Unexplored ? finalPlanet.Name : "unknown")}." );
                    break;
                case Mode.Awakening:
                    tooltipBuffer.Add( $"A Sleeper Prime on {planet.Name} is attempting to awaken a Derelict Sleeper! " );
                    if ( !(faction.Implementation as SleeperSubFaction).PrimeCanMoveOn )
                        tooltipBuffer.Add( $"\nIt is currently recharging its engines after entering a wormhole, and will be ready to move in {(120 - (entity.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.SecondsSinceEnteringPlanet ?? 120)) / 60}:{((120 - (entity.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.SecondsSinceEnteringPlanet ?? 120)) % 60).ToString( "00" )}. " );
                    else if ( entity.TypeData.InternalName == Sleepers.UNIT_NAMES.SleeperPrimeMobile.ToString() )
                        tooltipBuffer.Add( $"\nIt is currently moving into position. " );
                    else
                        tooltipBuffer.Add( $"\nIt will fully awaken a Derelict Sleeper in {(300 - (entity.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.SecondsSinceLastTransformation ?? 300)) / 60}:{((300 - (entity.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.SecondsSinceLastTransformation ?? 300)) % 60).ToString( "00" )}. " );
                    break;
                default:
                    break;
            }

            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
            return true;
        }

        public bool ContentGetter( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
        {
            int debugStage = -1;
            try
            {
                InitIfNeeded();

                debugStage = 0;
                string colorString = string.Empty;
                colorString = faction.FactionCenterColor.ColorHexBrighter;
                debugStage = 1;
                Image.UpdateWith( entity.TypeData.GUISprite_Icon );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
                buffer.Add( "Prime" );
                buffer.Add( "</color>" );
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                debugStage = 6;
                switch ( CurrentMode )
                {
                    case Mode.Moving:
                        if ( !(faction.Implementation as SleeperSubFaction).PrimeCanMoveOn )
                            buffer.Add( $"{ (120 - (entity.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.SecondsSinceEnteringPlanet ?? 120)) / 60}:{ ((120 - (entity.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.SecondsSinceEnteringPlanet ?? 120)) % 60).ToString( "00" )}" );
                        else
                            buffer.Add( "Moving" );
                        break;
                    case Mode.Awakening:
                        if ( !(faction.Implementation as SleeperSubFaction).PrimeCanMoveOn )
                            buffer.Add( $"{ (120 - (entity.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.SecondsSinceEnteringPlanet ?? 120)) / 60}:{ ((120 - (entity.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.SecondsSinceEnteringPlanet ?? 120)) % 60).ToString( "00" )}" );
                        else if ( entity.TypeData.InternalName == Sleepers.UNIT_NAMES.SleeperPrimeMobile.ToString() )
                            buffer.Add( "Moving" );
                        else
                            buffer.Add( $"{(300 - (entity.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.SecondsSinceLastTransformation ?? 300)) / 60}:{((300 - (entity.GetSleeperData( ExternalDataRetrieval.ReturnNullIfNotFound )?.SecondsSinceLastTransformation ?? 300)) % 60).ToString( "00" )}" );
                        break;
                    default:
                        break;
                }
                debugStage = 9;
                if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                {
                    buffer.Add( "\n" ).Add( planet.Name );
                }
                else
                    buffer.Add( "\n" ).Add( "Unknown" );
                debugStage = 12;
                buffer.Add( "\n" );
                debugStage = 13;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PrimeMovementNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }

        public BasicPoolable DuplicateSelf()
        {
            //just for BasicPoolable
            return new PrimeClaimingNotifier();
        }
        public void Clear()
        {
            //just for BasicPoolable
        }
    }

    public class SleeperCPANotifier : BasicPoolable
    {
        public Faction faction;
        public FInt CPAStrengthCharged;
        public int CPAStrengthMax;

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/exogalacticstrikeforce.png" );
        }

        public MouseHandlingResult ClickHandler()
        {
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer();
        public bool MouseoverHandler()
        {
            tooltipBuffer.Clear();

            tooltipBuffer.Add( $"The {faction.GetDisplayName()} faction is preparing a large scale CPA strike against its opposition!" );
            tooltipBuffer.Add( $"\n{CPAStrengthCharged}/{CPAStrengthMax} strength prepared for the assault." );
            tooltipBuffer.Add( $"\n\nThe only way for this assault to be prevented is to find and destroy all Harbinger Hangers that the {faction.GetDisplayName()} own before it finishes charging." );

            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
            return true;
        }

        public bool ContentGetter( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
        {
            int debugStage = -1;
            try
            {
                InitIfNeeded();

                debugStage = 0;
                string colorString = string.Empty;
                colorString = faction.FactionCenterColor.ColorHexBrighter;
                debugStage = 1;
                Image.UpdateWith( sprite );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
                buffer.Add( "CPA" );
                buffer.Add( "</color>" );
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                debugStage = 6;
                buffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
                buffer.Add( faction.GetDisplayName() );
                buffer.Add( "</color>" );
                int perc = (CPAStrengthCharged * 100).GetNearestIntPreferringHigher() / CPAStrengthMax;
                buffer.Add( $"\n{perc}%" );
                debugStage = 12;
                buffer.Add( "\n" );
                debugStage = 13;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in SleeperCPANotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }

        public BasicPoolable DuplicateSelf()
        {
            //just for BasicPoolable
            return new PrimeClaimingNotifier();
        }
        public void Clear()
        {
            //just for BasicPoolable
        }
    }

    public class SleeperNPCAwakeningNotifier : BasicPoolable
    {
        public Faction faction;
        public Planet planet;
        public int SecondsLeft;

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/exogalacticstrikeforce.png" );
        }

        public MouseHandlingResult ClickHandler()
        {
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer();
        public bool MouseoverHandler()
        {
            tooltipBuffer.Clear();

            tooltipBuffer.Add( $"The {faction.GetDisplayName()} faction is preparing to awaken a Sleeper Prime on {planet.Name}." );

            tooltipBuffer.Add( $"\nOnce it finishes, its will awaken a Sleeper Prime that will begin claiming Sleepers to defend its territory." );

            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
            return true;
        }

        public bool ContentGetter( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
        {
            int debugStage = -1;
            try
            {
                InitIfNeeded();

                debugStage = 0;
                string colorString = string.Empty;
                colorString = faction.FactionCenterColor.ColorHexBrighter;
                debugStage = 1;
                Image.UpdateWith( sprite );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
                buffer.Add( "Prime" );
                buffer.Add( "</color>" );
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                debugStage = 6;
                buffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
                buffer.Add( faction.GetDisplayName() );
                buffer.Add( "</color>" );
                int minutes = SecondsLeft / 60;
                int seconds = SecondsLeft % 60;
                buffer.Add( $"\n{minutes}:{seconds}" );
                debugStage = 12;
                buffer.Add( "\n" );
                debugStage = 13;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in SleeperNPCAwakeningNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }

        public BasicPoolable DuplicateSelf()
        {
            //just for BasicPoolable
            return new PrimeClaimingNotifier();
        }
        public void Clear()
        {
            //just for BasicPoolable
        }
    }
}
