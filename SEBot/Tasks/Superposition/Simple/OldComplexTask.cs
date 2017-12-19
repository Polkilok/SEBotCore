using System;
using System.Collections.Generic;

namespace SEBot
{
	public sealed partial class Program
	{
		//представляет задачу, требующую одновременного выполнения нескольких других
		class OldComplexTask : Task
		{
			private readonly List<Task> Tasks;
			private readonly EndCondition Condition;
			public enum EndCondition { All, Any, Last, Repeat };
			//Создает пустую задачу одновременного выполнения, используя заданное условие выполнения
			//All - все задачи должны вернуть true
			//Any - хотя бы одна возвращает true - Задача выполнена
			//Last - только последняя - задача считается выполненной, если последняя вернула true
			//Repeat - следующая задача выполняется только если ВСЕ предыдущие вернули true, проверка на каждом тике
			//При этом возможен "возврат" назад, если какая-то здача провалится
			//TODO разнести по разным классам
			public OldComplexTask(EndCondition endCondition = EndCondition.All)
			{
				Tasks = new List<Task>();
				Condition = endCondition;
			}
			public bool Execute()
			{
				//Log.Log("Task \'ComplexTask\'");
				if (Tasks.Count == 0)
				{
					Log.Warning("ComplexTask: empty");
					return true;//чтобы пропускать пустые листы
				}

				List<bool> flags = new List<bool>(Tasks.Count);//иначе нас ждет оптимизация
				if (Condition == EndCondition.Repeat)
				{
					int i = 0;
					bool flag = true;
					while (i < Tasks.Count && flag)
						flag = Tasks[i++].Execute();
					return flag;
				}
				for (int i = 0; i < Tasks.Count; ++i)
					flags.Add(Tasks[i].Execute());
				if (Condition == EndCondition.All)
					return !flags.Contains(false);
				else if (Condition == EndCondition.Any)
					return flags.Contains(true);
				else if (Condition == EndCondition.Last)
					return flags[Tasks.Count - 1];
				else
					throw new Exception("Bad value in \'ComplexTask\' for ICondition or unrealized functional");
			}
			public void AddTask(Task task)
			{
				Tasks.Add(task);
			}
		}
	}

}