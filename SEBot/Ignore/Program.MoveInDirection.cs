using System;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		//предоставляет задачу перемещения в заданном направлении к заданной точке в абсолютных координатах
		//P. S. перемещает вдоль заданной прямой так, чтобы до точки было наименьшее расстояние,
		//может выполняться во время вращений
		//перемещения в перпендикулярных направлениях не учитываются
		//при "промахе" включается задний ход
		//дефолтный вариант 
		//после окончания переопределение направлений вперед/назад сбрасывается
		class MoveInDirection : Program.Task
		{

			private readonly Base6Directions.Direction Direction;
			private readonly Vector3D DestinationPoint;
			private readonly Program.ThrusterEnableRule EnableRule;
			public MoveInDirection(Program.ThrusterEnableRule enableRule, Base6Directions.Direction direction, Vector3D point)
			{
				DestinationPoint = point;
				Direction = direction;
				EnableRule = enableRule;
			}
			public bool Execute()
			{
				//Log.Log("Task \'MoveInDirection\' :" + Direction.ToString());
				double leftDistance = CalculateLeftDistance();
				//Log.Log("LeftDistance " + Math.Round(LeftDistance, 1).ToString());
				if (EnableRule.PointIsReached(leftDistance, Direction))
				{
					//точка достигнута
					//сбросим тягу
					Program.Ship.MovementSystem.OverrideDirection(Direction, false);
					Program.Ship.MovementSystem.OverrideDirection(Base6Directions.GetOppositeDirection(Direction), false);
					return true;
				}
				else
					EnableThrust(leftDistance);
				return false;

			}

			private double CalculateLeftDistance()
			{
				Vector3D DestinationPointLC = Program.Ship.TravelSystem.ToLocalCoordinate(DestinationPoint);
				Vector3D mask = Base6Directions.GetVector(Direction);
				//Log.Log("Left Distance trunc vec " + FloorCoordinate(trunc));
				return (DestinationPointLC * mask).Sum;
			}

			private void EnableThrust(double leftDistance)
			{
				Base6Directions.Direction TargetDirection = leftDistance > 0 ? Direction :
					Base6Directions.GetOppositeDirection(Direction);
				leftDistance = Math.Abs(leftDistance);
				Program.Log.Log("MoveInDirection\tMove in real direction " + TargetDirection.ToString());
				if (EnableRule.EnableCondition(leftDistance, TargetDirection))
					Program.Ship.MovementSystem.MoveInDirection(TargetDirection,
						EnableRule.ThrustPower(leftDistance, TargetDirection));
				else
				{
					//считаем, что включена система гашения инерции
					//TODO включать принудительно?
					Program.Ship.MovementSystem.MoveInDirection(TargetDirection, 0);
					//TODO строчка ниже не нужна?
				}
			}
		}
	}

}