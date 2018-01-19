using System;

namespace SEBot
{
	public sealed partial class Program
	{
		class ReverToBaseFactoryTask : IFactoryTask
		{
			public ITask GetTask()
			{
				Log.Log($"ReverToBaseFactoryTask.GetTask()", GLOBAL_ALGORITHMIC_ACTION);
				Log.Log($"ReverToBaseFactoryTask.Time:{DateTime.Now.ToShortTimeString()}", GLOBAL_ALGORITHMIC_ACTION);
				var answer = new TaskSequence();
				answer.AddTask(new DisableDrils());//TODO в перспективе выключать все инструменты
				answer.AddTask(Ship.DockSystem.GetDockTask());
				answer.AddTask(new WaitTask(new WaitTimeCondition(TimeSpan.FromSeconds(10))));
				answer.AddTask(Ship.DockSystem.GetUnDockTask());
				//отключение, т. к. коннектор может быть выставленн на автосброс
				answer.AddTask(new ApplyActionTask("OnOff_Off", Ship.DockSystem.Connector));
				//TODO ориентация корабля тоже имеет значение, возможно, сделать движение с вращением?
				answer.AddTask(Ship.TravelSystem.DefaultTravelFactory.GetTask(Ship.TravelSystem.GetPosition()));
				Log.Log($"ReverToBaseFactoryTask.End", GLOBAL_ALGORITHMIC_ACTION);
				return answer;
			}
		}
	}

}