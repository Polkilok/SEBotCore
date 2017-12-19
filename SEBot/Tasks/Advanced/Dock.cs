using Sandbox.ModAPI.Ingame;

namespace SEBot
{
	public sealed partial class Program
	{
		/// <summary>
		/// класс, описывающтй задачу стыковки - банально переключает состояние коннектора
		/// </summary>
		class Dock : Task
		{
			private readonly IMyShipConnector _connector;
			//Создает задачу стыковки указанным коннектором, передавая позицию основного блока 
			//при последней успешной стыковке в качестве аргумента DockingPosition
			public Dock(IMyShipConnector connector)
			{
				_connector = connector;
			}
			public bool Execute()
			{
				Log.Log($"Dock.Execute()", GLOBAL_ALGORITHMIC_ACTION);
				Log.Log($"Dock.Execute._connector.Status:{_connector.Status}", GLOBAL_ALGORITHMIC_ACTION);
				if (_connector.Status == MyShipConnectorStatus.Connectable)
				{
					TerminalBlockExtentions.ApplyAction(_connector, "SwitchLock");
					Ship.DockSystem.SavePosition();
				}
				else if (_connector.Status == MyShipConnectorStatus.Connected)
				{
					Log.Warning("UnDock - in connacted state");
					Ship.DockSystem.SavePosition();
				}
				else
				{
					Log.Warning("UnDock - can't connect - not other connector");
					Log.Log($"Dock.Execute.End", GLOBAL_ALGORITHMIC_ACTION);
					return false;
				}
				Log.Log($"Dock.Execute.End", GLOBAL_ALGORITHMIC_ACTION);
				return true;
			}
		}
	}

}