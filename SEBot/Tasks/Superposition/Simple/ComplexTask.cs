using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace SEBot
{
	public sealed partial class Program
	{
		abstract class ComplexTask : ITask
		{
			protected readonly List<ITask> Tasks;

			public ComplexTask()
			{
				Tasks = new List<ITask>();
			}

			public void AddTask(ITask t)
			{
				Tasks.Add(t);
			}

			//Реализуйте собственный вариант комплексной задачи здесь
			//Проверять отсутствие задач не нужно!
			protected abstract bool ComplexExecute(Environment environment);

			public bool Execute(Environment env)
			{
				if (Tasks.Count == 0)
				{
					Log.Warning("ComplexTask: empty");
					return true;//чтобы пропускать пустые листы
				}
				return ComplexExecute(env);
			}
		}
	}

}