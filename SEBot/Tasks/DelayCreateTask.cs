namespace SEBot
{
	public sealed partial class Program
	{
		//Представляет отложенно создаваемую задачу
		//Полезно, если понимание, что делать, приходит не сразу, а только в точке конечного назначения
		class DelayCreateTask : ITask
		{
			private readonly IFactoryTask _creator;

			private ITask _realTask;

			public DelayCreateTask(IFactoryTask taskCreator)
			{
				_creator = taskCreator;
				_realTask = null;
			}

			public bool Execute(Environment env)
			{
				if (_realTask == null)
					_realTask = _creator.GetTask();
				return _realTask.Execute(env);
			}
		}
	}

}