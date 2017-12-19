using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		class StaticPointProvider : IPointProvider
		{
			private Vector3D _point;
			public StaticPointProvider(Vector3D pointInGlobalCoordinates)
			{
				_point = pointInGlobalCoordinates;
			}

			public Vector3D Now()
			{
				return Ship.TravelSystem.ToLocalCoordinate(_point);
			}

			//TODO test
			public Vector3D Prognosed(double seconds)
			{
				return Ship.TravelSystem.ToLocalCoordinate(_point) - Ship.TravelSystem.Speed * seconds;
			}
		}
	}

}