using System;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		/// <summary>
		/// Предоставляет смещение, такое, как если бы основным блоком являлся другой блок
		/// Но при этом не делает вращения (это может привести к неприятным последствиям)
		/// </summary>
		class DistanceFromBlockPointProvider : IPointProvider
		{
			private readonly IMyCubeBlock _block;
			private readonly IPointProvider _pointProvider;

			public DistanceFromBlockPointProvider(IMyCubeBlock block, IPointProvider pointProvider)
			{
				if (block == null) throw new Exception($"Argument null exception. Argname:{nameof(block)}");
				if (pointProvider == null) throw new Exception($"Argument null exception. Argname:{nameof(pointProvider)}");
				_pointProvider = pointProvider;
				_block = block;
			}

			public Vector3D Now()
			{
				return _pointProvider.Now() - LocalBlocCoordinates(_block);
			}

			public Vector3D Prognosed(double seconds)
			{
				return _pointProvider.Prognosed(seconds) - LocalBlocCoordinates(_block);
			}

			public static Vector3D LocalBlocCoordinates(IMyCubeBlock block)
			{
				return Ship.TravelSystem.ToLocalCoordinate(block.CubeGrid.GridIntegerToWorld(block.Position));
			}
		}
	}

}