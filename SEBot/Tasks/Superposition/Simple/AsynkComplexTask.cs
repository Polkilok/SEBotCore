namespace SEBot
{
	public sealed partial class Program
	{
		class AsynkComplexTask : ComplexTask
		{
			protected override bool ComplexExecute(Environment environment)
			{
				//иначе нас ждет оптимизация
				//List<bool> flags = new List<bool>(Tasks.Count);
				//for (int i = 0; i < Tasks.Count; ++i)
				//	flags[i] = Tasks[i].Execute();
				//return !flags.Contains(false);
				int flag = Tasks.Count;
				for (int i = 0; i < Tasks.Count; ++i)
					flag += Tasks[i].Execute(environment) ? -1 : 0;
				return flag == 0;
			}
		}
	}

}