namespace SEBot
{
	public sealed partial class Program
	{
		/// <summary>
		/// Задача, которая дает указание выполнить остановку
		/// </summary>
		class StopTask : Task
		{
			public bool Execute()
			{
				Ship.MovementSystem.Stop();
				Ship.OrientationSystem.DisableOverride();
				return true;
			}
		}
	}

}