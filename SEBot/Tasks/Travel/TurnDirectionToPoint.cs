using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		//Задача поворота заданного направления к заданной точке
		//Можно указать мертвую зону, в которой вращения осуществляться не будут
		class TurnDirectionToPoint : Task
		{
			private readonly VRageMath.Vector3D Point;
			private readonly Base6Directions.Direction Direction;
			private readonly double MinRange;
			private readonly double Accuracy;

			private double Power;
			//создает задачу вращения заданного направления к точке
			//при этом можно указать расстояние до точки, начиная с которого данная задача больше выполняться не будет
			//TODO сохранять силу
			public TurnDirectionToPoint(Base6Directions.Direction direction, Vector3D point, double minRotationRange = 0, double accuracy = GYRO_E)
			{
				Point = point;
				Direction = direction;
				MinRange = minRotationRange;
				Accuracy = accuracy;
				Power = 1;
				Log.Log("TurnDirectionToPoint");
				Log.Log("minRotationRange " + minRotationRange.ToString("0.0"));
			}
			public bool Execute()
			{
				Log.Log("Task \'TurnDirectionToPoint\' \n" + FloorCoordinate(Point));
				Vector3D myPosition = Ship.TravelSystem.GetPosition();
				double leftDistance = (Point - myPosition).Length();
				if (leftDistance < MinRange)
				{
					Ship.OrientationSystem.DisableOverride();
					return true;
				}
				else
				{
					Ship.OrientationSystem.TurnDirectionToPoint(Direction, Point);
					Vector3D pointLC = Vector3D.Normalize(Ship.TravelSystem.ToLocalCoordinate(Point));

					Vector3D direction = Base6Directions.GetVector(Direction);

					var bias = pointLC - direction;

					bias.SetDim(direction.AbsMaxComponent(), 0);

					Log.Log("bias " + FloorCoordinate(bias));

					if (bias.Length() < Accuracy)
					{
						Ship.OrientationSystem.DisableOverride();
						return true;
					}
				}
				return false;
			}
		}
	}

}