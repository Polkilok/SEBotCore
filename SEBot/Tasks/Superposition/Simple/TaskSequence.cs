using System.Collections.Generic;

namespace SEBot
{
	public sealed partial class Program
	{
		//представляет задачу последовательного выполнения задач
		//TODO объединить с ComplexTask
		class TaskSequence : Task
		{
			//Очередь задач
			private Queue<Task> TaskQueue;

			private Task CurrentTask;

			public TaskSequence()
			{
				TaskQueue = new Queue<Task>();
				CurrentTask = null;
			}

			public void AddTask(Task task)
			{
				TaskQueue.Enqueue(task);
			}

			//TODO проверить
			public void Reverse()
			{
				TaskQueue = new Queue<Task>(TaskQueue.Reverse());
			}

			public bool Execute()
			{
				if (CurrentTask == null)
					if (TaskQueue.Count > 0)
						CurrentTask = TaskQueue.Dequeue();
					else
						return true;
				if (CurrentTask.Execute())
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