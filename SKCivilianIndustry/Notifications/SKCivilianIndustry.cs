using System;
using System.Collections.Generic;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace SKCivilianIndustry.Notifications
{
    public class AIRaidNotifier : BasicPoolable
    {
        public List<int> raidingWormholes = new List<int>();
        public List<GameEntity_Squad> RaidingWormholes { get { List<GameEntity_Squad> wormholes = new List<GameEntity_Squad>(); for ( int x = 0; x < raidingWormholes.Count; x++ ) wormholes.Add( World_AIW2.Instance.GetEntityByID_Squad( raidingWormholes[x] ) ); return wormholes; } }
        public Faction faction;
        public int SecondsLeft;

        public List<Planet> RaidedPlanets { get { List<Planet> raidedPlanets = new List<Planet>(); for ( int x = 0; x < raidingWormholes.Count; x++ ) if ( !raidedPlanets.Contains( RaidingWormholes[x].Planet ) ) raidedPlanets.Add( RaidingWormholes[x].Planet ); return raidedPlanets; } }

        public enum Mode
        {
            Moving,
            Awakening
        }
        public Mode CurrentMode;

        //these are just data from asset bundles, that's definitely okay to be static
        private static bool hasInitialized = false;
        public int LastPlanetIndexCentered = 0;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;

            hasInitialized = true;
        }

        public MouseHandlingResult ClickHandler()
        {
            if ( RaidedPlanets != null && RaidedPlanets.Count > 0 )
            {
                if ( LastPlanetIndexCentered >= RaidedPlanets.Count )
                    LastPlanetIndexCentered = 0;
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                {
                    LastPlanetIndexCentered++;
                    if ( LastPlanetIndexCentered >= RaidedPlanets.Count )
                        LastPlanetIndexCentered = 0;
                    Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( RaidedPlanets[LastPlanetIndexCentered], false );
                }
                else
                {
                    if ( RaidedPlanets.Contains( Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() ) )
                    {
                        if ( RaidedPlanets.Count <= 1 )
                            return MouseHandlingResult.None;
                        int index = RaidedPlanets.IndexOf( Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() );
                        index++;
                        if ( index >= RaidedPlanets.Count )
                            index = 0;
                        World_AIW2.Instance.SwitchViewToPlanet( RaidedPlanets[index] );
                    }
                    else
                        World_AIW2.Instance.SwitchViewToPlanet( RaidedPlanets[0] );
                }
                return MouseHandlingResult.None;
            }
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer();
        public bool MouseoverHandler()
        {
            tooltipBuffer.Clear();

            if ( LastPlanetIndexCentered >= RaidedPlanets.Count )
                LastPlanetIndexCentered = 0;
            Planet.SetCurrentlySecondaryHoveredOver( RaidedPlanets[LastPlanetIndexCentered] );

            string planetsList = string.Empty;
            for ( int x = 0; x < RaidedPlanets.Count; x++ )
            {
                if ( x > 0 )
                    planetsList += ", ";
                if ( x == RaidedPlanets.Count - 1 )
                    planetsList += "and ";
                planetsList += RaidedPlanets[x].Name;
            }

            tooltipBuffer.Add( $"The AI is preparing to raid your economy on {planetsList}. Expect a large number of cloaked ships." );

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

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
                buffer.Add( "Raid" );
                buffer.Add( "</color>" );
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                debugStage = 6;
                debugStage = 9;
                int perc = 100 - ((FInt.FromParts( SecondsLeft, 000 ) / 300) * 100).GetNearestIntPreferringHigher();
                buffer.Add( $"{perc}%" );
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
            return new AIRaidNotifier();
        }
        public void Clear()
        {
            //just for BasicPoolable
        }
    }
}
