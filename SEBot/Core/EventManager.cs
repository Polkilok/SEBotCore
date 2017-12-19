using System;
using System.Collections.Generic;

namespace SEBot
{
	public sealed partial class Program
	{
		/*******************************************************************/
		/*******************************************************************/
		/*******************************************************************/
		/*******************************************************************/

		//Тот самый самый главный менеджер задач
		class EventManager
		{
			//Очередь задач
			private TaskSequence TaskQueue;

			private uint ticker;//счетчик, каждый раз, как Update получает управление, он увеличивается
			private uint UpdatePeriod;//определяет, во сколько раз реже, чем получение управления,
									  //будут выполняться текущие задачи
			private List<ConditionalTask> SmallTasksList;


			/// <summary>
			/// Показывает продолжительность тиков в мс
			/// </summary>
			//TODO оптимизировать и считать 1 раз, а не каждый тик
			public int TickPeriod { get; private set; }

			private DateTime valuePrew;

			public EventManager(uint updatePeriod)
			{
				ticker = updatePeriod;
				UpdatePeriod = updatePeriod;
				TaskQueue = new TaskSequence();
				TickPeriod = (int)(updatePeriod / 60.0 * 1000.0);//относително неплохая аппроксимация
				valuePrew = DateTime.UtcNow;
				SmallTasksList = new List<ConditionalTask>();
			}

			public void AddTask(Task task)
			{
				TaskQueue.AddTask(task);
			}

			public void Update()
			{
				//Log.Log($"tick:{ticker}", INIT_SYSTEM);
				ticker--;// = ++ticker % UpdatePeriod;
				if (ticker > 0)
					return;//не выйдет только если ticker == 0
				ticker = UpdatePeriod;

				DateTime valueNow = DateTime.UtcNow;

				TimeSpan span = valueNow - valuePrew;
				//TickPeriod = span.Milliseconds;
				TickPeriod = ((int)span.TotalMilliseconds + TickPeriod) / 2;

				//if (TickPeriod != 1)
				//	TickPeriod = (span.Milliseconds + TickPeriod) / 2;
				//else
				//	TickPeriod = span.Milliseconds;
				valuePrew = valueNow;
				ExecuteSmallTask();
				if (TaskQueue.Execute())
					Log.Log("Task compleated");
			}

			public void AddSmallTask(ConditionalTask task)
			{
				SmallTasksList.Add(task);
			}

			private void ExecuteSmallTask()
			{
				foreach (var task in SmallTasksList)
				{
					task.Execute();
				}
			}

			internal void Clear()
			{
				TaskQueue = new TaskSequence();
				SmallTasksList = new List<ConditionalTask>();
			}
		}
	}

}