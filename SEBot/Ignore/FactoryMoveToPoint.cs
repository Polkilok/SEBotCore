using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		//класс, предоставляющий фабрику задач движения к заданной точке
		//использование вращения задается дополнительным условием (true - Использовать)
		class FactoryMoveToPoint : IFactoryPointBasedTask
		{
			private readonly IFactoryPointDirectionBasedTask MoveInDirectionFactory;

			private readonly double NoRotationRange;
			//аргумент moveInDirectionFactory будет передан в конструктор TravelToPoint
			public FactoryMoveToPoint(IFactoryPointDirectionBasedTask moveInDirectionFactory, double noRotationRange = DEATH_ZONE_FOR_ROTATION)
			{
				MoveInDirectionFactory = moveInDirectionFactory;
				NoRotationRange = noRotationRange;
			}
			public ITask GetTask(Vector3D targetPoint, bool rotatate = false)
			{
				if (rotatate == false)
				{
					//OldComplexTask task = new OldComplexTask();
					AsynkComplexTask task = new AsynkComplexTask();
					task.AddTask(MoveInDirectionFactory.GetTask(targetPoint, Base6Directions.Direction.Left));
					task.AddTask(MoveInDirectionFactory.GetTask(targetPoint, Base6Directions.Direction.Up));
					task.AddTask(MoveInDirectionFactory.GetTask(targetPoint, Base6Directions.Direction.Forward));
					return task;
				}
				ITask rotateTask = new TurnDirectionToPoint(Base6Directions.Direction.Forward, targetPoint, NoRotationRange);
				OldComplexTask outerTask = new OldComplexTask(OldComplexTask.EndCondition.Last);
				outerTask.AddTask(rotateTask);
				outerTask.AddTask(GetTask(targetPoint, false));
				return outerTask;
			}
		}
	}

}