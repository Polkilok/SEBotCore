namespace SEBot
{
	public sealed partial class Program
	{
		//Класс, описывающий условную задачу - задачу, которая выполнится только при положительном условии
		//При любых усливиях возвращается true
		class ConditionalTask : Task
		{
			private readonly Task Handler;
			private readonly ICondition Condition;
			public ConditionalTask(ICondition condition, Task handler)
			{
				Condition = condition;
				Handler = handler;
			}

			public bool Execute()
			{
				if (Condition.Check())
					Handler.Execute();
				return true;
			}
		}
	}

}