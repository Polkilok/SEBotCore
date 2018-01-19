using Sandbox.ModAPI.Ingame;

// ReSharper disable once CheckNamespace
namespace SEBot
{
	public sealed partial class Program
	{
		//класс, описывающтй задачу отстыковки - банально переключает состояние коннектора
		class UnDock : ITask
		{
			private readonly IMyShipConnector _connector;
			//Создает задачу стыковки указанным коннектором, передавая позицию основного блока 
			//при последней успешной стыковке в качестве аргумента DockingPosition
			public UnDock(IMyShipConnector connector)
			{
				_connector = connector;
			}

			public bool Execute(Environment env)
			{
				Log.Log($"UnDock.Execute()", GLOBAL_ALGORITHMIC_ACTION);
				Log.Log($"UnDock.Execute._connector.Status:{_connector.Status}", GLOBAL_ALGORITHMIC_ACTION);
				if (_connector.Status == MyShipConnectorStatus.Connected)
				{
					Ship.DockSystem.SavePosition();
					_connector.Disconnect();
				}
				else if (_connector.Status == MyShipConnectorStatus.Connectable)
				{
					Ship.DockSystem.SavePosition();
					Log.Warning("UnDock - not have been docked, but other connector in near");
				}
				else
				{
					Log.Warning("UnDock - not have been docked, not near other connector");
				}
				Log.Log($"UnDock.Execute.End", GLOBAL_ALGORITHMIC_ACTION);
				return true;
			}
		}
	}

}