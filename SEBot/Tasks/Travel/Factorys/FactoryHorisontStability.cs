using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		//Фабрика, которая создает задачи стабилизации относительно горизонта
		class FactoryHorisontStability : IFactoryTask
		{
			public ITask GetTask()
			{
				Log.Log($"FactoryHorisontStability.GetTask()", GRAVITY_EXTENDS_TASKS);
				Vector3D gravity = Ship.MainController.GetTotalGravity();
				Log.Log($"FactoryHorisontStability.GetTask.gravity(global):{Vector3.Round(gravity, 2)}", GRAVITY_EXTENDS_TASKS);
				gravity = Ship.MainController.GetPosition() + gravity * VERY_LARGE_DISTANCE;//TODO сделать параметром, передаваемым в конструктор
				Log.Log($"FactoryHorisontStability.GetTask.gravity(far far avay point):{Vector3.Round(gravity, 2)}", GRAVITY_EXTENDS_TASKS);
				TaskSequence answer = new TaskSequence();
				answer.AddTask(new StopTask());
				answer.AddTask(new TurnDirectionToPoint(Base6Directions.Direction.Down, gravity));
				Log.Log($"FactoryHorisontStability.GetTask.answer:{answer}", GRAVITY_EXTENDS_TASKS);
				Log.Log($"FactoryHorisontStability.GetTask.End", GRAVITY_EXTENDS_TASKS);
				return answer;
			}
		}
	}

}