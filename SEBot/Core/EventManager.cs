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
		private class EventManager
		{
			private readonly uint _updatePeriod;

			//будут выполняться текущие задачи
			private List<ConditionalTask> _smallTasksList;

			//Очередь задач
			private TaskSequence _taskQueue;

			private uint _ticker;//счетчик, каждый раз, как Update получает управление, он увеличивается
			private DateTime valuePrew;

			public EventManager(uint updatePeriod)
			{
				_ticker = updatePeriod;
				_updatePeriod = updatePeriod;
				_taskQueue = new TaskSequence();
				TickPeriod = (int)(updatePeriod / 60.0 * 1000.0);//относително неплохая аппроксимация
				valuePrew = DateTime.UtcNow;
				_smallTasksList = new List<ConditionalTask>();
			}

			//определяет, во сколько раз реже, чем получение управления,
			/// <summary>
			/// Показывает продолжительность тиков в мс
			/// </summary>
			//TODO оптимизировать и считать 1 раз, а не каждый тик
			public int TickPeriod { get; private set; }

			/// <summary>
			/// Return time in seconds
			/// </summary>
			public double TimeSinceLastRun => TickPeriod / 1000.0;

			public void AddSmallTask(ConditionalTask task)
			{
				_smallTasksList.Add(task);
			}

			public void AddTask(ITask task)
			{
				_taskQueue.AddTask(task);
			}

			public void Update()
			{
				//Log.Log($"tick:{ticker}", INIT_SYSTEM);
				_ticker--;// = ++ticker % UpdatePeriod;
				if (_ticker > 0)
					return;//не выйдет только если ticker == 0
				_ticker = _updatePeriod;

				DateTime valueNow = DateTime.UtcNow;

				TimeSpan span = valueNow - valuePrew;
				//TickPeriod = span.Milliseconds;
				TickPeriod = ((int)span.TotalMilliseconds + TickPeriod) / 2;

				//if (TickPeriod != 1)
				//	TickPeriod = (span.Milliseconds + TickPeriod) / 2;
				//else
				//	TickPeriod = span.Milliseconds;
				valuePrew = valueNow;
				var env = new Environment(Ship);
				ExecuteSmallTask(env);
				if (_taskQueue.Execute(env))
					Log.Log("Task compleated");
			}

			internal void Clear()
			{
				_taskQueue = new TaskSequence();
				_smallTasksList = new List<ConditionalTask>();
			}

			private void ExecuteSmallTask(Environment env)
			{
				foreach (var task in _smallTasksList)
				{
					task.Execute(env);
				}
			}
		}
	}
}