using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		class HorisontallyStabilityCondition : ICondition
		{
			public bool Check()
			{
				if (Ship.MainController.GetTotalGravity().Length() < GYRO_E)
					return false;
				Vector3D gravity = Ship.TravelSystem.ToLocalCoordinate(Ship.TravelSystem.GetPosition() + Ship.MainController.GetTotalGravity());
				Vector3D down = Base6Directions.GetVector(Base6Directions.Direction.Down);
				gravity = Vector3D.Normalize(gravity);
				Vector3D bias = down - gravity;
				bias.SetDim(bias.AbsMaxComponent(), 0);
				//Log.Log("Cond. bias " + FloorCoordinate(bias));
				if (bias.Length() > GYRO_E)
					return true;
				else
					return false;
			}
		}
	}

}