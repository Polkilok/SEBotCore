namespace SEBot
{
	public sealed partial class Program
	{
		//Да, задача просто включает буры
		class EnableDrils : ITask
		{
			public bool Execute(Environment env)
			{
				env.Ship.Drils.Enable();
				return true;
			}
		}
	}

}