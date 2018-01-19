using System.Collections.Generic;

namespace SEBot
{
	public sealed partial class Program
	{
		//представляет задачу цикличного выполнения задач
		class TaskCyclce : ITask
		{
			//Очередь задач
			private readonly List<ITask> _taskList;
			private int _currentTaskIndex;
			public TaskCyclce()
			{
				_taskList = new List<ITask>();
				_currentTaskIndex = 0;
			}
			public void AddTask(ITask task)
			{
				_taskList.Add(task);
			}

			public bool Execute(Environment env)
			{
				if (_taskList.Count == 0)
					return true;
				else if (_taskList[_currentTaskIndex].Execute(env))
					_currentTaskIndex = (_currentTaskIndex + 1) % _taskList.Count;
				return false;
			}
		}
	}

}