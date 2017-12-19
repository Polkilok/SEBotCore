namespace SEBot
{
	public sealed partial class Program
	{
		class LogTask : Task
		{
			string msg;
			public LogTask(string Message)
			{
				msg = Message;
			}
			public bool Execute()
			{
				Log.Log("Task \'LogTask\'\n\t" + msg);
				return true;
			}
		}
	}

}