﻿using System.Collections.Generic;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace PreceptsOfThePrecursors
{
    public static class Utilities
    {
        public static GameCommand CreateGameCommand( string type, GameCommandSource source, Faction faction )
        {
            GameCommandType commandType = GameCommandTypeTable.Instance.GetRowByName( type );
            return CreateGameCommand( commandType, source, faction );
        }
        public static GameCommand CreateGameCommand( GameCommandType type, GameCommandSource source, Faction faction )
        {
            GameCommand command = GameCommand.Create( type, source );
            command.RelatedFactionIndex = faction.FactionIndex;
            return command;
        }
        public static int GetScaledIntensityValue(int intensity, int minValue, int maxValue )
        {
            int step = (maxValue - minValue) / 9;

            return minValue + (step * (intensity - 1));
        }
        public static FInt GetScaledIntensityValue( int intensity, FInt minValue, FInt maxValue )
        {
            FInt step = (maxValue - minValue) / 9;

            return minValue + (step * (intensity - 1));
        }
    }
    public static class ExtentionMethods
    {
        public static void AddToPerPlanetLookup( this GameEntity_Squad entity, ref ArcenSparseLookup<Planet, List<GameEntity_Squad>> lookup )
        {
            if ( !lookup.GetHasKey( entity.Planet ) )
                lookup.AddPair( entity.Planet, new List<GameEntity_Squad>() );
            lookup[entity.Planet].Add( entity );
        }
    }
}
