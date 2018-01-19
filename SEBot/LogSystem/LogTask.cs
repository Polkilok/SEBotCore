namespace SEBot
{
	public sealed partial class Program
	{
		private class LogTask : ITask
		{
			private readonly string _msg;
			private readonly string _source;

			public LogTask(string message, string source = Logger.INFO_STR)
			{
				_msg = message;
				_source = source;
			}

			public bool Execute(Environment env)
			{
				Log.Log(_msg, _source);
				return true;
			}
		}
	}
}