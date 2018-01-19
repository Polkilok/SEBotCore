namespace SEBot
{
	public sealed partial class Program
	{
		class DisableDrils : ITask
		{
			public bool Execute(Environment env)
			{
				env.Ship.Drils.Disable();
				return true;
			}
		}
	}

}