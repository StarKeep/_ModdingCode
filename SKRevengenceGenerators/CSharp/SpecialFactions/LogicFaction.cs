using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using System;

namespace SKRevengenceGenerators
{
    // Main faction class.
    public class LogicFaction : BaseSpecialFaction
    {
        protected override string TracingName => "LogicFaction";
        protected override bool EverNeedsToRunLongRangePlanning => false;

        private ArcenSparseLookup<GameEntity_Squad, int> HealthLastSecond;

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim( Faction faction, ArcenSimContext Context )
        {
            if ( World_AIW2.Instance.GameSecond < 60 )
                return;

            Faction darkSpireFaction = World_AIW2.Instance.GetFirstFactionWithSpecialFactionImplementationType( typeof( SpecialFaction_DarkSpire ) );

            if ( HealthLastSecond != null )
            {
                darkSpireFaction.Entities.DoForEntities( ( GameEntity_Squad entity ) =>
                {
                    if ( entity.TypeData.InternalName == "DarkSpireVengeanceGenerator" )
                    {
                        if ( HealthLastSecond[entity] > entity.GetCurrentHullPoints() + entity.GetCurrentShieldPoints() )
                        {
                            int difference = HealthLastSecond[entity] - (entity.GetCurrentHullPoints() + entity.GetCurrentShieldPoints());
                            SpecialFaction_DarkSpire.GlobalData.PerPlanet[entity.Planet.Index].TotalEnergy += difference;
                            SpecialFaction_DarkSpire.GlobalData.PerPlanet[entity.Planet.Index].NetEnergy += difference ;

                            SpecialFaction_DarkSpire.GlobalData.TimeForNextVengeanceStrike--;
                        }
                    }

                    return DelReturn.Continue;
                } );
            }
            HealthLastSecond = new ArcenSparseLookup<GameEntity_Squad, int>();
            darkSpireFaction.Entities.DoForEntities( ( GameEntity_Squad entity ) =>
            {
                if ( entity.TypeData.InternalName == "DarkSpireVengeanceGenerator" )
                {
                    HealthLastSecond.AddPair( entity, entity.GetCurrentHullPoints() + entity.GetCurrentShieldPoints() );
                }

                return DelReturn.Continue;
            } );
        }
                
    }
}