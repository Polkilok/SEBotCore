﻿using System.Collections.Generic;

namespace SEBot
{
	public sealed partial class Program
	{
		//Предоставляет возможность добавлять прерывания для задачи
		//Единовременно обрабатывается только 1 прерывание
		//На время его обработки остальные прерывания заблокированы
		//Их срабатывание нигде не отмечается и не сохраняется
		class InterruptedTask : Task
		{
			//основная задача
			private readonly Task MainTask;
			//обрабатываемое прерывание
			private Task Interrupt;
			//Список прерываний
			private List<Interrupt> Interrupts;

			public InterruptedTask(Task mainTask)
			{
				MainTask = mainTask;
				Interrupt = null;
				Interrupts = new List<Interrupt>();
			}

			public void AddInterrupt(Interrupt interrupt)
			{
				Interrupts.Add(interrupt);
			}

			public bool Execute()
			{
				//нету прерывания? проверим возможные срабатывания
				if (Interrupt == null)
					Interrupt = CheckInterrupts();
				//всё еще нет? тогда выполним основную задачу
				if (Interrupt == null)
					return MainTask.Execute();
				//Есть прерывание
				//Выполним его, если оно выполнится - удалим
				if (Interrupt.Execute())
					Interrupt = null;
				return false;//Конечно, задача не может в таком случае считаться выполненной
			}

			//проверяет условия прерываний
			private Task CheckInterrupts()
			{
				foreach (var inter in Interrupts)
				{
					//Log.Log("check interrupt " + interrupt.Сond.);
					if (inter.Condition.Check())
						return inter.Handler.GetTask();
				}
				return null;
			}
		}
	}

}