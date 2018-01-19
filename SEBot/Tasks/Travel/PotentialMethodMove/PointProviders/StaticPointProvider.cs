using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		private class StaticPointProvider : IPointProvider
		{
			private readonly Vector3D _point;

			public StaticPointProvider(Vector3D pointInGlobalCoordinates)
			{
				_point = pointInGlobalCoordinates;
			}

			public Vector3D Now(Environment env)
			{
				return env.MathCache.ToLocal(_point);
			}

			public Vector3D Prognosed(Environment env, double seconds)
			{
				return Now(env) - env.VectorShipSpeed * seconds;// '-' because LocalCoordinates
			}
		}
	}
}