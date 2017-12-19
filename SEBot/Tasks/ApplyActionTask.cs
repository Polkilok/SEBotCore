using Sandbox.ModAPI.Ingame;

namespace SEBot
{
	public sealed partial class Program
	{
		class ApplyActionTask : Task
		{
			private readonly IMyFunctionalBlock _block;
			private readonly string _actionId;

			public ApplyActionTask(string actionId, IMyFunctionalBlock block)
			{
				_actionId = actionId;
				_block = block;
			}
			public bool Execute()
			{
				_block.ApplyAction(_actionId);
				return true;
			}
		}
	}

}