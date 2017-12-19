using System;

namespace SEBot
{
	public sealed partial class Program
	{
		class WaitTimeCondition : ICondition
		{
			private bool IsStarted;
			private DateTime EndTime;
			private readonly TimeSpan WaitTime;
			public WaitTimeCondition(TimeSpan waitTime)
			{
				//TODO валидация?
				Log.Log($"WaitTimeCondition.WaitTimeCondition({waitTime})", CONDITION_LVL);
				IsStarted = false;
				WaitTime = waitTime;
				Log.Log($"WaitTimeCondition.WaitTimeCondition.End", CONDITION_LVL);
			}

			public bool Check()
			{
				Log.Log($"WaitTimeCondition.Check()", CONDITION_LVL);
				if (!IsStarted)
				{
					IsStarted = true;
					EndTime = DateTime.UtcNow.Add(WaitTime);
					Log.Log($"WaitTimeCondition.Check.{nameof(EndTime)}:{EndTime}", CONDITION_LVL);
				}
				Log.Log($"WaitTimeCondition.Check.End", CONDITION_LVL);
				return DateTime.UtcNow > EndTime;
			}
		}
	}

}