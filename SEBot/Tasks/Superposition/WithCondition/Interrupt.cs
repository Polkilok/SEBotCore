namespace SEBot
{
	public sealed partial class Program
	{
		//Класс, описывающий прерывания
		class Interrupt
		{
			public readonly IFactoryTask Handler;
			public readonly ICondition Condition;
			public Interrupt(ICondition condition, IFactoryTask handler)
			{
				Condition = condition;
				Handler = handler;
			}
		}
	}

}