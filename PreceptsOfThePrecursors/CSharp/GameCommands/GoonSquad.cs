using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using System;

namespace PreceptsOfThePrecursors.GameCommands
{
    public class PopulateGoonSquad : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenSimContext context )
        {
            if ( GoonSquad.Ships == null )
                GoonSquad.Ships = new Arcen.Universal.ArcenSparseLookup<GoonSquad.Ship, EntityCollection>();

            command.DoForRelatedEntities( ( GameEntity_Squad entity ) =>
            {
                if ( !Enum.TryParse( entity.TypeData.InternalName, out GoonSquad.Ship shipType ) )
                    return Arcen.Universal.DelReturn.Continue;

                if ( !GoonSquad.Ships.GetHasKey( shipType ) )
                    GoonSquad.Ships.AddPair( shipType, new EntityCollection() );
                if ( command.RelatedBool || !GoonSquad.Ships[shipType].Contains( entity ) )
                    GoonSquad.Ships[shipType].AddEntity( entity );

                return Arcen.Universal.DelReturn.Continue;
            } );
        }
    }
}
