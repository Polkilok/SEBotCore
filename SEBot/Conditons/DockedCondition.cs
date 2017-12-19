using Sandbox.ModAPI.Ingame;

namespace SEBot
{
	public sealed partial class Program
	{
		class DockedCondition : ICondition
		{
			IMyShipConnector Connector;
			public DockedCondition(IMyShipConnector connector)
			{
				Connector = connector;
			}

			public bool Check()
			{
				Log.Log("DockedCondition.Check()", CONDITION_LVL);
				var status = Connector.Status;
				Log.Log($"DockedCondition.Check.{nameof(status)}:{status}", CONDITION_LVL);
				Log.Log("DockedCondition.Check.End", CONDITION_LVL);
				return status == MyShipConnectorStatus.Connected || status == MyShipConnectorStatus.Connectable;
			}
		}
	}

}