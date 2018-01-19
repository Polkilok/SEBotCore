using VRageMath;

// ReSharper disable once CheckNamespace
namespace SEBot
{
	public sealed partial class Program
	{
		//Задача поворота заданного направления к заданной точке
		//Можно указать мертвую зону, в которой вращения осуществляться не будут
		//TODO сделать "умное" вращение - обратная связь для лучшей точности
		private class TurnDirectionToPoint : ITask
		{
			private readonly IPointProvider _point;
			private readonly Base6Directions.Direction _direction;
			private readonly double _minRange;
			private readonly double _accuracy;

			private double _power;

			//создает задачу вращения заданного направления к точке
			//при этом можно указать расстояние до точки, начиная с которого данная задача больше выполняться не будет
			//TODO сохранять силу
			public TurnDirectionToPoint(Base6Directions.Direction direction, IPointProvider point, double minRotationRange = 0, double accuracy = GYRO_E)
			{
				_point = point;
				_direction = direction;
				_minRange = minRotationRange;
				_accuracy = accuracy;
				_power = 1;
				Log.Log("minRotationRange " + minRotationRange.ToString("0.0"), nameof(TurnDirectionToPoint));
			}

			public TurnDirectionToPoint(Base6Directions.Direction direction, Vector3D point, double minRotationRange = 0, double accuracy = GYRO_E)
			: this(direction, new StaticPointProvider(point), minRotationRange, accuracy)
			{
			}

			public bool Execute(Environment env)
			{
				Log.Log("_point " + FloorCoordinate(_point.Now(env)), nameof(TurnDirectionToPoint));
				double leftDistance = env.MathCache.Length(_point.Now(env));
				Log.Log("leftDistance " + leftDistance.ToString("0.00"), nameof(TurnDirectionToPoint));

				if (leftDistance < _minRange)
				{
					env.Ship.OrientationSystem.DisableOverride();
					return true;
				}
				else
				{
					env.Ship.OrientationSystem.TurnDirectionToPoint(_direction, _point.Now(env));

					Vector3D pointLC = env.MathCache.Normalize(_point.Now(env));
					Log.Log("pointLC " + FloorCoordinate(pointLC), nameof(TurnDirectionToPoint));
					Vector3D direction = Base6Directions.GetVector(_direction);

					var bias = pointLC - direction;
					Log.Log("pointLC - direction " + FloorCoordinate(bias), nameof(TurnDirectionToPoint));

					bias.SetDim(direction.AbsMaxComponent(), 0);

					Log.Log("bias " + FloorCoordinate(bias), nameof(TurnDirectionToPoint));

					if (bias.Length() < _accuracy)
					{
						env.Ship.OrientationSystem.DisableOverride();
						return true;
					}
				}
				return false;
			}
		}
	}
}