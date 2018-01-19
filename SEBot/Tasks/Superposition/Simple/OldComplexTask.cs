using System;
using System.Collections.Generic;

namespace SEBot
{
	public sealed partial class Program
	{
		//представляет задачу, требующую одновременного выполнения нескольких других
		class OldComplexTask : ITask
		{
			private readonly List<ITask> _tasks;
			private readonly EndCondition _condition;
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
				_tasks = new List<ITask>();
				_condition = endCondition;
			}

			public void AddTask(ITask task)
			{
				_tasks.Add(task);
			}

			public bool Execute(Environment env)
			{
				if (_tasks.Count == 0)
				{
					Log.Warning("ComplexTask: empty");
					return true;//чтобы пропускать пустые листы
				}

				List<bool> flags = new List<bool>(_tasks.Count);//иначе нас ждет оптимизация
				if (_condition == EndCondition.Repeat)
				{
					int i = 0;
					bool flag = true;
					while (i < _tasks.Count && flag)
						flag = _tasks[i++].Execute(env);
					return flag;
				}
				for (int i = 0; i < _tasks.Count; ++i)
					flags.Add(_tasks[i].Execute(env));
				if (_condition == EndCondition.All)
					return !flags.Contains(false);
				else if (_condition == EndCondition.Any)
					return flags.Contains(true);
				else if (_condition == EndCondition.Last)
					return flags[_tasks.Count - 1];
				else
					throw new Exception("Bad value in \'ComplexTask\' for ICondition or unrealized functional");
			}
		}
	}

}