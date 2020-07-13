using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using System;

namespace PreceptsOfThePrecursors.GameCommands
{
    public class PopulateAncestorsArks : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            if ( AncestorsArks.Ships == null )
                AncestorsArks.Ships = new Arcen.Universal.ArcenSparseLookup<AncestorsArks.Ship, EntityCollection>();

            command.DoForRelatedEntities( ( GameEntity_Squad entity ) =>
            {
                if ( !Enum.TryParse( entity.TypeData.InternalName, out AncestorsArks.Ship shipType ) )
                    return Arcen.Universal.DelReturn.Continue;

                if ( !AncestorsArks.Ships.GetHasKey( shipType ) )
                    AncestorsArks.Ships.AddPair( shipType, new EntityCollection() );
                if ( command.RelatedBool || !AncestorsArks.Ships[shipType].Contains( entity ) )
                    AncestorsArks.Ships[shipType].AddEntity( entity );

                return Arcen.Universal.DelReturn.Continue;
            } );
        }
    }
}
