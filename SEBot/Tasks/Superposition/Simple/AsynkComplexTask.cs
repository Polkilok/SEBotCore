namespace SEBot
{
	public sealed partial class Program
	{
		class AsynkComplexTask : IComplexTask
		{
			protected override bool ComplexExecute()
			{
				//иначе нас ждет оптимизация
				//List<bool> flags = new List<bool>(Tasks.Count);
				//for (int i = 0; i < Tasks.Count; ++i)
				//	flags[i] = Tasks[i].Execute();
				//return !flags.Contains(false);
				int flag = Tasks.Count;
				for (int i = 0; i < Tasks.Count; ++i)
					flag += Tasks[i].Execute() ? -1 : 0;
				return flag == 0;
			}
		}
	}

}