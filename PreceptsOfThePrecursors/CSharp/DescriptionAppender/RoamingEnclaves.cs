using System;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace PreceptsOfThePrecursors.DescriptionAppender
{
    public class EnclaveYounglingsStorageDescriptionAppender : IGameEntityDescriptionAppender
    {
        public void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenDoubleCharacterBuffer Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;

            StoredYounglingsData storage = RelatedEntityOrNull.GetStoredYounglings( ExternalDataRetrieval.ReturnNullIfNotFound );
            if ( storage == null )
                return;
            if ( storage.StoredYounglings.GetPairCount() > 0 )
                Buffer.Add( $"This Enclave contains the following Younglings of total strength {Math.Round( RelatedEntityOrNull.AdditionalStrengthFromFactions / 1000f, 3 )}.\n" );
            for ( int x = 0; x < storage.StoredYounglings.GetPairCount(); x++ )
            {
                int count = 0;
                int strength = 0;
                YounglingUnit unit = storage.StoredYounglings.GetPairByIndex( x ).Key;
                GameEntityTypeData unitData = GameEntityTypeDataTable.Instance.GetRowByName( unit.ToString() );
                for ( int y = 0; y < storage.StoredYounglings[unit].UnitsByMark.GetPairCount(); y++ )
                {
                    ArcenSparseLookupPair<byte, int> pair = storage.StoredYounglings[unit].UnitsByMark.GetPairByIndex( y );
                    count += pair.Value;
                    strength += unitData.GetForMark( pair.Key ).StrengthPerSquad_Original_DoesNotIncreaseWithMarkLevel * pair.Value;
                }
                if ( Window_InGameHoverEntityInfo.CalculateTooltipDetailLevel() >= TooltipDetail.Full )
                    Buffer.Add( $"{count} {unitData.DisplayName} ({(double)strength / 1000} strength)\n" );
            }
        }
    }
}
