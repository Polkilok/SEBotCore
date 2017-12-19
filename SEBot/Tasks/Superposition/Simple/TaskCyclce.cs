using System.Collections.Generic;

namespace SEBot
{
	public sealed partial class Program
	{
		//представляет задачу цикличного выполнения задач
		class TaskCyclce : Task
		{
			//Очередь задач
			private List<Task> TaskList;
			private int CurrentTaskIndex;
			public TaskCyclce()
			{
				TaskList = new List<Task>();
				CurrentTaskIndex = 0;
			}
			public void AddTask(Task task)
			{
				TaskList.Add(task);
			}
			public bool Execute()
			{
				if (TaskList.Count == 0)
					return true;
				else if (TaskList[CurrentTaskIndex].Execute())
					CurrentTaskIndex = (CurrentTaskIndex + 1) % TaskList.Count;
				return false;
			}
		}
	}

}