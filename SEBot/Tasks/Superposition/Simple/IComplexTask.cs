using System.Collections.Generic;

namespace SEBot
{
	public sealed partial class Program
	{
		abstract class IComplexTask : Task
		{
			protected readonly List<Task> Tasks;

			public IComplexTask()
			{
				Tasks = new List<Task>();
			}
			public void AddTask(Task t)
			{
				Tasks.Add(t);
			}
			public bool Execute()
			{
				if (Tasks.Count == 0)
				{
					Log.Warning("ComplexTask: empty");
					return true;//чтобы пропускать пустые листы
				}
				return ComplexExecute();
			}
			//Реализуйте собственный вариант комплексной задачи здесь
			//Проверять отсутствие задач не нужно!
			protected abstract bool ComplexExecute();
		}
	}

}