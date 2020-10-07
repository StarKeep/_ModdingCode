using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using System;
using System.Collections.Generic;

namespace PreceptsOfThePrecursors
{
	// Considers how much trust a faction holds for players on a per planet basis.
	public class Trust : ArcenExternalSubManagedData
	{
		protected ArcenSparseLookup<short, short> trustPerPlanet;
		// Limit our trust values.
		protected short maxTrust = 3000;
		public virtual short MaxTrust( Planet planet )
		{
			return maxTrust;
		}
		protected short minTrust = -3000;
		public virtual short MinTrust( Planet planet )
		{
			return minTrust;
		}
		public short GetTrust( Planet planet )
		{
			return GetTrust( planet.Index );
		}
		public virtual short GetTrust( short planetIndex )
		{
			if ( trustPerPlanet.GetHasKey( planetIndex ) )
				return trustPerPlanet[planetIndex];
			else
				return 0;
		}
		public void SetTrust( Planet planet, short trustToSet )
		{
			if ( trustPerPlanet.GetHasKey( planet.Index ) )
				trustPerPlanet[planet.Index] = trustToSet;
			else
				trustPerPlanet.AddPair( planet.Index, trustToSet );
			trustPerPlanet[planet.Index] = ClampTrust( planet, trustPerPlanet[planet.Index] );
		}
		public void AddOrSubtractTrust( Planet planet, short trustToAddOrSubtract )
		{
			if ( trustPerPlanet.GetHasKey( planet.Index ) )
				trustPerPlanet[planet.Index] += trustToAddOrSubtract;
			else
				trustPerPlanet.AddPair( planet.Index, trustToAddOrSubtract );
			trustPerPlanet[planet.Index] = ClampTrust( planet, trustPerPlanet[planet.Index] );
		}
		// Called automatically in the above functions, but made public in case another class requires it. Potentially private it later.
		public short ClampTrust( Planet planetToClampOn, short valueToClamp )
		{
			return Math.Max( MinTrust( planetToClampOn ), Math.Min( valueToClamp, MaxTrust( planetToClampOn ) ) );
		}

		public Trust()
		{
			trustPerPlanet = new ArcenSparseLookup<short, short>();
			maxTrust = 3000;
			minTrust = -3000;
		}
		public Trust( ArcenDeserializationBuffer Buffer ) : this()
		{
			this.DeserializeIntoSelf( Buffer, false );
		}
		public override void SerializeTo( ArcenSerializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
		{
			int count = trustPerPlanet.GetPairCount();
			buffer.AddInt32( ReadStyle.NonNeg, count );
			for ( int x = 0; x < count; x++ )
			{
				ArcenSparseLookupPair<short, short> pair = trustPerPlanet.GetPairByIndex( x );
				buffer.AddInt16( ReadStyle.PosExceptNeg1, pair.Key );
				buffer.AddInt16( ReadStyle.Signed, pair.Value );
			}

			buffer.AddInt16( ReadStyle.Signed, maxTrust );
			buffer.AddInt16( ReadStyle.Signed, minTrust );
		}
		public override void DeserializeIntoSelf( ArcenDeserializationBuffer buffer, bool IsForPartialSyncDuringMultiplayer )
		{
			if ( trustPerPlanet == null )
				trustPerPlanet = new ArcenSparseLookup<short, short>();
			else if ( IsForPartialSyncDuringMultiplayer )
				trustPerPlanet.Clear();

			int count = buffer.ReadInt32( ReadStyle.NonNeg );
			for ( int x = 0; x < count; x++ )
				trustPerPlanet.AddPair( buffer.ReadInt16(ReadStyle.PosExceptNeg1), buffer.ReadInt16(ReadStyle.Signed ) );

			maxTrust = buffer.ReadInt16( ReadStyle.Signed );
			minTrust = buffer.ReadInt16( ReadStyle.Signed );
		}
	}

	// Trust unique to the Dyson Precursors Faction.
	public class DysonTrust : Trust
	{
		public DysonTrust() : base() { }
		public DysonTrust( ArcenDeserializationBuffer Buffer ) : base( Buffer ) { }

		public override short MaxTrust( Planet planet )
		{
			short baseTrust = GetTrust( planet );
			DysonProtoSphereData.ProtoSphereType? sphereType = planet.GetProtoSphereData( ExternalDataRetrieval.ReturnNullIfNotFound )?.Type;
			if ( sphereType == null )
				return maxTrust;
			if ( (sphereType == DysonProtoSphereData.ProtoSphereType.Protecter || sphereType == DysonProtoSphereData.ProtoSphereType.Suppressor) && baseTrust < 1000 )
				return -2000;
			if ( DysonPrecursors.DysonNodes.GetHasKey( planet ) && baseTrust < 0 )
				return -1000;

			return maxTrust;
		}
		public override short MinTrust( Planet planet )
		{
			short baseTrust = GetTrust( planet );
			DysonProtoSphereData.ProtoSphereType? sphereType = planet.GetProtoSphereData( ExternalDataRetrieval.ReturnNullIfNotFound )?.Type;
			if ( sphereType == null )
				return minTrust;
			if ( (sphereType == DysonProtoSphereData.ProtoSphereType.Protecter || sphereType == DysonProtoSphereData.ProtoSphereType.Suppressor) && baseTrust > 1000 )
				return 2000;
			if ( DysonPrecursors.DysonNodes.GetHasKey( planet ) && baseTrust > 0 )
				return 1000;

			return minTrust;
		}
		// Find a nearby trusted planet.
		// If nothing is found, default to a nearby player planet that we don't distrust.
		public Planet GetNearbyTrustedPlanet( Planet origin, ArcenSimContext Context, int minTrust = 1000 )
		{
			// Return the planet closest to us that we trust.
			// If none are trusted, get closest Human or ProtoSphere planet.
			List<Planet> trustedPlanets = new List<Planet>();
			List<Planet> backupPlanets = new List<Planet>();
			int trustedHops = 99, humanHops = 99;
			World_AIW2.Instance.DoForPlanets( false, planet =>
			{
				if ( planet.Index == origin.Index )
					return DelReturn.Continue;

				int hops = planet.GetHopsTo( origin );
				int baseTrust = GetTrust( planet );
				int trust = Math.Abs( baseTrust );
				if ( baseTrust > minTrust )
					trust += 1; // Slight bias towards player-trusted planets.
				if ( trust > minTrust && hops <= trustedHops )
				{
					if ( hops < trustedHops )
					{
						// Found a planet that is fewer hops away. Reset our list and hop limiter.
						trustedHops = hops;
						trustedPlanets = new List<Planet>();
					}
					trustedPlanets.Add( planet );
				}
				if ( ((trust > -1000 && planet.GetIsControlledByFactionType( FactionType.Player )) || (planet.GetProtoSphereData( ExternalDataRetrieval.ReturnNullIfNotFound )?.Level ?? 0) > 0) && hops <= humanHops )
				{
					if ( hops < humanHops )
					{
						// Found a planet that is fewer hops away. Reset our list and hop limiter.
						humanHops = hops;
						backupPlanets = new List<Planet>();
					}
					backupPlanets.Add( planet );
				}

				return DelReturn.Continue;
			} );
			if ( trustedPlanets.Count > 0 )
				return trustedPlanets[World_AIW2.Instance.GameSecond % trustedPlanets.Count];
			if ( backupPlanets.Count > 0 )
				return backupPlanets[World_AIW2.Instance.GameSecond % backupPlanets.Count];
			return origin.GetRandomNeighbor( false, Context );
		}
	}
}