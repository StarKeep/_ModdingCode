using Arcen.AIW2.Core;
using Arcen.Universal;
using SKCivilianIndustry.Storage;

namespace SKCivilianIndustry.DescriptionAppenders
{
    public class CargoDescriptionAppender : IGameEntityDescriptionAppender
    {
        private IGameEntityDescriptionAppender original;
        public CargoDescriptionAppender() { }
        public CargoDescriptionAppender( IGameEntityDescriptionAppender originalAppender = null )
        {
            if ( originalAppender != null )
                original = originalAppender;
        }
        public void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenDoubleCharacterBuffer Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;

            if ( original != null )
                original.AddToDescriptionBuffer( RelatedEntityOrNull, RelatedEntityTypeData, Buffer );

            CivCargoData cargo = RelatedEntityOrNull.GetCargoNotSimSafeMayReturnNull();
            if ( cargo == null )
                return;

            for ( int x = 0; x < cargo.Amount.GetPairCount(); x++ )
            {
                string key = cargo.Amount.GetPairByIndex( x ).Key;
                if ( cargo.GetAmount( key ) > 0 || cargo.GetPerSecond( key ) != 0 )
                    Buffer.Add( $"\n{cargo.GetAmount( key )}/{cargo.GetCapacity( key )} {key} {cargo.GetPerSecond( key )} Per Second" );
            }

            if ( RelatedEntityOrNull.TypeData.IsMobile )
            {
                CivCargoShipStatus status = RelatedEntityOrNull.GetCargoShipStatus();
                if ( status.IsIdle )
                    Buffer.Add( "\nShip is Idle" );
                else if ( status.HasOrigin )
                {
                    if ( status.IsDocked )
                        Buffer.Add( $"\nShip is docked and loading resources at {status.Origin.TypeData.DisplayName}." );
                    else
                        Buffer.Add( $"\nShip is moving towards {status.Origin.TypeData.DisplayName} on {status.Origin.Planet.Name}." );
                }
                else if ( status.IsDocked )
                    Buffer.Add( $"\nShip is docked and unloading resources at {status.Destination.TypeData.DisplayName}." );
                else
                    Buffer.Add( $"\nShip is moving towards {status.Destination.TypeData.DisplayName} on {status.Destination.Planet.Name}." );
            }

            // Add in an empty line to stop any other gunk (such as the fleet display) from messing up our given information.
            Buffer.Add( "\n" );
            return;
        }
    }
}
