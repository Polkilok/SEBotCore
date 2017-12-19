namespace SEBot
{
	public sealed partial class Program
	{
		class WaitTask : Task
		{
			ICondition condition;
			public WaitTask(ICondition cond)
			{
				condition = cond;
			}
			public bool Execute()
			{
				return condition.Check();
			}
		}
	}

}