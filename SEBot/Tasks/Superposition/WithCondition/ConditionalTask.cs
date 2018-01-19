namespace SEBot
{
	public sealed partial class Program
	{
		//Класс, описывающий условную задачу - задачу, которая выполнится только при положительном условии
		//При любых усливиях возвращается true
		class ConditionalTask : ITask
		{
			private readonly ITask Handler;
			private readonly ICondition Condition;
			public ConditionalTask(ICondition condition, ITask handler)
			{
				Condition = condition;
				Handler = handler;
			}

			public bool Execute(Environment env)
			{
				if (Condition.Check())
					Handler.Execute(env);
				return true;
			}
		}
	}

}