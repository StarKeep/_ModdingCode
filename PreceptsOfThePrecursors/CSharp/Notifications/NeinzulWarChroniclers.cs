using System;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace PreceptsOfThePrecursors.Notifications
{
    public class WarChroniclerIncomingNotifier : BasicPoolable
    {
        public Planet planet;
        public Faction faction;
        public int secondsLeft;
        public int estimatedStrength;

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
            if ( planet != null && planet.IntelLevel > PlanetIntelLevel.Unexplored )
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

            tooltipBuffer.Add( faction.StartFactionColourForLog() );
            tooltipBuffer.Add( $"The {faction.GetDisplayName()}" );
            tooltipBuffer.EndColor();

            tooltipBuffer.Add( $" are currently interested in the conflict on {planet.Name} and will intervene shortly." );

            int minutes = secondsLeft / 60;
            int seconds = secondsLeft % 60;

            tooltipBuffer.Add( $"\nThey will arrive in {minutes.ToString( "##0" )}:{seconds.ToString( "00" )}." );

            tooltipBuffer.Add( $"\nWe believe them to be bringing at least {(estimatedStrength/1000.0).ToString("#.000")} strength worth of forces." );

            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate() );
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
                colorString = faction.TeamCenterColor.ColorHexBrighter;
                debugStage = 1;
                Image.UpdateWith( GameEntityTypeDataTable.Instance.GetRowByName(NeinzulWarChroniclers.Tags.NeinzulWarChronicler.ToString()).GUISprite_Icon );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
                buffer.Add( "Incoming" );
                buffer.Add( "</color>" );
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                debugStage = 6;

                buffer.Add( $"\n{planet.Name}" );

                int minutes = secondsLeft / 60;
                int seconds = secondsLeft % 60;

                buffer.Add($"\n{minutes.ToString("##0")}:{seconds.ToString("00")}");

                debugStage = 12;

                debugStage = 13;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in WarChroniclerIncomingNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }

        public BasicPoolable DuplicateSelf()
        {
            //just for BasicPoolable
            return new WarChroniclerIncomingNotifier();
        }
        public void Clear()
        {
            //just for BasicPoolable
        }
    }

    public class WarChroniclerDepartingNotifier : BasicPoolable
    {
        public Planet planet;
        public Faction faction;
        public int secondsLeft;

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
            if ( planet != null && planet.IntelLevel > PlanetIntelLevel.Unexplored )
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

            tooltipBuffer.Add( faction.StartFactionColourForLog() );
            tooltipBuffer.Add( $"The {faction.GetDisplayName()}" );
            tooltipBuffer.EndColor();

            tooltipBuffer.Add( $" are preparing to depart {planet.Name}." );

            int minutes = secondsLeft / 60;
            int seconds = secondsLeft % 60;

            tooltipBuffer.Add( $"\nThey will warp out in {minutes.ToString( "##0" )}:{seconds.ToString( "00" )}." );

            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate() );
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
                colorString = faction.TeamCenterColor.ColorHexBrighter;
                debugStage = 1;
                Image.UpdateWith( GameEntityTypeDataTable.Instance.GetRowByName( NeinzulWarChroniclers.Tags.NeinzulWarChronicler.ToString() ).GUISprite_Icon );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
                buffer.Add( "Departing" );
                buffer.Add( "</color>" );
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                debugStage = 6;

                buffer.Add( $"\n{planet.Name}" );

                int minutes = secondsLeft / 60;
                int seconds = secondsLeft % 60;

                buffer.Add( $"\n{minutes.ToString( "##0" )}:{seconds.ToString( "00" )}" );

                debugStage = 12;

                debugStage = 13;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in WarChroniclerDepartingNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }

        public BasicPoolable DuplicateSelf()
        {
            //just for BasicPoolable
            return new WarChroniclerDepartingNotifier();
        }
        public void Clear()
        {
            //just for BasicPoolable
        }
    }
}
