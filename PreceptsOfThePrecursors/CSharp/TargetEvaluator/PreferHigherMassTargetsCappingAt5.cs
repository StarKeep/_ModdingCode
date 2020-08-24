using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace PreceptsOfThePrecursors.TargetEvaluator
{
    public class PreferHigherMassTargetsCappingAt5 : ITargetEvaluatorImplementation
    {
        public float CalculateNormalizedAdditionToCoreImportance( GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, GameEntity_Squad DefenderEntity, bool priorityAIBased )
        {
            if ( DefenderEntity.TypeData.Mass_tX > 5 )
                return 0;

            if ( AttackerSystem.TypeData.BaseKnockbackPerShot != 0 && AttackerSystem.GetKnockbackPowerAgainst( DefenderEntity ) == 0 )
                return 0; // If we're supposed to pull/push something, and we can't, we don't otherwise care.

            return (TargetListPlanning.FACTOR_IMPORTANCE_FOR_ADDED_MINOR_EFFECT / 5) * DefenderEntity.TypeData.Mass_tX.GetNearestIntPreferringHigher();
        }
    }
}
