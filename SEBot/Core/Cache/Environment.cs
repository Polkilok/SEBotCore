using System;
using System.Collections.Generic;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		public class Environment
		{
			private const string SHIP_POS = "ShipPos";
			private const string SHIP_SPEED = "ShipSpeed";
			public readonly ShipSystems Ship;
			private readonly Cache<string, Vector3> _vectorCache;
			private readonly Cache<string, double> _doubleCache;

			public readonly Cache<string, object> UserCache;

			public readonly VectorOperationCaches MathCache;

			public Environment(ShipSystems ship)
			{
				if (ship == null) throw new ArgumentNullException(nameof(ship));
				Ship = ship;

				MathCache = new VectorOperationCaches(ship);

				UserCache = new Cache<string, object>(s => null);

				_vectorCache = new Cache<string, Vector3>(
					s =>
					{
						switch (s)
						{
							case SHIP_SPEED:
								return Ship.TravelSystem.Speed;

							case SHIP_POS:
								return Ship.TravelSystem.GetPosition();

							default:
								return default(Vector3);
						}
					});

				_doubleCache = new Cache<string, double>(s =>
				{
					switch (s)
					{
						case SHIP_SPEED:
							return MathCache.Length(VectorShipSpeed);

						default:
							return default(double);
					}
				});
			}

			public Vector3D VectorShipSpeed => _vectorCache[SHIP_SPEED];

			public double ShipSpeed => _doubleCache[SHIP_SPEED];

			public Vector3D Position => _vectorCache[SHIP_POS];

			public double Mass => Ship.Mass;

			/// <summary>
			/// Time in Seconds
			/// </summary>
			public double TimeSinceLastRun => GlobalEventManeger.TimeSinceLastRun;
		}
	}
}