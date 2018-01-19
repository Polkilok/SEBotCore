namespace SEBot
{
	public sealed partial class Program
	{
		/// <summary>
		/// Задача, которая дает указание выполнить остановку
		/// </summary>
		class StopTask : ITask
		{
			public bool Execute(Environment env)
			{
				env.Ship.MovementSystem.Stop();
				env.Ship.OrientationSystem.DisableOverride();
				return true;
			}
		}
	}

}