namespace SEBot
{
	public sealed partial class Program
	{
		//Представляет отложенно создаваемую задачу
		//Полезно, если понимание, что делать, приходит не сразу, а только в точке конечного назначения
		class DelayCreateTask : Task
		{
			private readonly IFactoryTask Creator;

			private Task RealTask;

			public DelayCreateTask(IFactoryTask taskCreator)
			{
				Creator = taskCreator;
				RealTask = null;
			}

			public bool Execute()
			{
				if (RealTask == null)
				{
					RealTask = Creator.GetTask();
				}
				return RealTask.Execute();
			}
		}
	}

}