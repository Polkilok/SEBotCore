namespace SEBot
{
	public sealed partial class Program
	{
		class DisableDrils : Task
		{
			public bool Execute()
			{
				Ship.Drils.Disable();
				return true;
			}
		}
	}

}