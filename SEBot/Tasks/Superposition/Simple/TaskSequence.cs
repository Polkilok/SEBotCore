using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SEBot
{
	public sealed partial class Program
	{
		//представляет задачу последовательного выполнения задач
		//TODO объединить с ComplexTask
		class TaskSequence : ITask
		{
			//Очередь задач
			private Queue<ITask> TaskQueue;

			private ITask CurrentTask;

			public TaskSequence()
			{
				TaskQueue = new Queue<ITask>();
				CurrentTask = null;
			}

			public void AddTask(ITask task)
			{
				TaskQueue.Enqueue(task);
			}

			//TODO проверить
			public void Reverse()
			{
				TaskQueue = new Queue<ITask>(TaskQueue.Reverse());
			}

			public bool Execute(Environment env)
			{
				if (CurrentTask == null)
					if (TaskQueue.Count > 0)
						CurrentTask = TaskQueue.Dequeue();
					else
						return true;
				if (CurrentTask.Execute(env))
					if (TaskQueue.Count == 0)
						CurrentTask = null;
					else
						CurrentTask = TaskQueue.Dequeue();
				if (CurrentTask != null) Log.Log($"TaskSequence.CurrentTask:{CurrentTask}", COMPLEX_TASK_DEBUG_LVL);
				return false;
			}
		}
	}

}