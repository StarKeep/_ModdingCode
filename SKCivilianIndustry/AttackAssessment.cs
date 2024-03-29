﻿using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCivilianIndustry
{

    /// <summary>
    /// Used to report on how strong an attack would be on a hostile planet.
    /// </summary>
    public class AttackAssessment : IComparable<AttackAssessment>
    {
        public Planet Target;
        public ArcenSparseLookup<Planet, int> Attackers;
        public int StrengthRequired;
        public bool MilitiaOnPlanet;
        public bool HasReinforcePoint;
        public int AttackPower { get { int value = 0; Attackers.DoFor( pair => { value += pair.Value; return DelReturn.Continue; } ); return value; } }

        public AttackAssessment(Planet target, int strengthRequired, bool hasReinforcePoints)
        {
            Target = target;
            Attackers = new ArcenSparseLookup<Planet, int>();
            StrengthRequired = strengthRequired;
            MilitiaOnPlanet = false;
            HasReinforcePoint = hasReinforcePoints;
        }
        public int CompareTo(AttackAssessment other)
        {
            // Planets that already have militia get higher priority. Reinforce ourselves.
            if (MilitiaOnPlanet && !other.MilitiaOnPlanet)
                return -1;
            else if (other.MilitiaOnPlanet && !MilitiaOnPlanet)
                return 1;
            else
                // We want higher threat to be first in a list, so reverse the normal sorting order.
                return other.StrengthRequired.CompareTo(this.StrengthRequired);
        }

        public override string ToString() => $"Target:{Target.Name} Attacker Count:{Attackers.GetPairCount()} Strength Required:{StrengthRequired} Attack Power:{AttackPower} Militia Already On Planet? {MilitiaOnPlanet}";
    }
}
