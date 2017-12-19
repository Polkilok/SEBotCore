using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		//TODO Remove?
		class MoveOnDistanceInDirection : Task
		{
			FactoryMoveToPoint MoveFactory;
			Task MoveTask;
			Base6Directions.Direction Direction;
			double Distance;
			public MoveOnDistanceInDirection(double distance, Base6Directions.Direction direction, FactoryMoveToPoint moveFactory)
			{
				MoveFactory = moveFactory;
				MoveTask = null;
				Direction = direction;
				Distance = distance;
			}
			public bool Execute()
			{
				if (MoveTask == null)
				{
					Vector3D TargetPoint = Base6Directions.GetVector(Direction);
					TargetPoint = Vector3D.Multiply(TargetPoint, Distance);
					TargetPoint = Ship.TravelSystem.ToGlobalCoordinate(TargetPoint);
					MoveTask = MoveFactory.GetTask(TargetPoint);
				}
				return MoveTask.Execute();
			}
		}
	}

}