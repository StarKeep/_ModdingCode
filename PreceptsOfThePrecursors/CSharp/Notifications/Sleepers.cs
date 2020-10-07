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
}
