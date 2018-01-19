namespace SEBot
{
	public sealed partial class Program
	{
		class SaveDockPosition : ITask
		{
			public bool Execute(Environment env)
			{
				Log.Log("Dock Matrix saved");
				//TODO проверять, пристыкован ли кораблик
				env.Ship.DockSystem.SavePosition();
				return true;
			}
		}
	}

}