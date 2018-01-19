// ReSharper disable once CheckNamespace
namespace SEBot
{
	public sealed partial class Program
	{
		class WaitTask : ITask
		{
			private readonly ICondition _condition;
			public WaitTask(ICondition cond)
			{
				_condition = cond;
			}

			public bool Execute(Environment env)
			{
				return _condition.Check();
			}
		}
	}

}