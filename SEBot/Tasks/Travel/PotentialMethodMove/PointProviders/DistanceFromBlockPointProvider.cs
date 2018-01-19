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
		private class DistanceFromBlockPointProvider : IPointProvider
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

			public static Vector3D LocalBlocCoordinates(Environment env, IMyCubeBlock block)
			{
				return env.MathCache.ToLocal(block.CubeGrid.GridIntegerToWorld(block.Position));
			}

			public Vector3D Now(Environment env)
			{
				return _pointProvider.Now(env) - LocalBlocCoordinates(env, _block);
			}

			public Vector3D Prognosed(Environment env, double seconds)
			{
				return Now(env) - env.VectorShipSpeed * seconds;
			}
		}
	}
}