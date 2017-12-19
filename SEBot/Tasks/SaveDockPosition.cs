namespace SEBot
{
	public sealed partial class Program
	{
		class SaveDockPosition : Task
		{
			public bool Execute()
			{
				Log.Log("Dock Matrix saved");
				//TODO проверять, пристыкован ли кораблик
				Ship.DockSystem.SavePosition();
				return true;
			}
		}
	}

}