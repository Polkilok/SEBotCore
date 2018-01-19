using System;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		public class DockSystem
		{
			public readonly IMyShipConnector Connector;
			private readonly Base6Directions.Direction connectorDir;
			private readonly IFactoryPointProviderBasedTask _travelFactory;
			public IPointProvider OuterConnector { get; set; }
			public IPointProvider StartDocking { get; set; }

			public readonly IFactoryTask DockTaskGenerator;
			public readonly IFactoryTask UndockTaskGenerator;

			public DockSystem(IMyCubeBlock mainController, IMyShipConnector connector, IFactoryPointProviderBasedTask travelFactory)
			{
				if (mainController == null) throw new Exception($"Argument null exception. {nameof(mainController)}");
				if (connector == null) throw new Exception($"Argument null exception. {nameof(connector)}");
				if (travelFactory == null) throw new Exception($"Argument null exception. {nameof(travelFactory)}");
				Connector = connector;
				_travelFactory = travelFactory;
				DockTaskGenerator = new DockFactory(this);
				UndockTaskGenerator = new UndockFactory(this);
				connectorDir = mainController.Orientation.TransformDirectionInverse(Connector.Orientation.Forward);
			}

			//Создает задачу стыковки
			public ITask GetDockTask()
			{
				return DockTaskGenerator.GetTask();
			}

			//предоставляет задачу отстыковки
			public ITask GetUnDockTask()
			{
				return UndockTaskGenerator.GetTask();
			}

			public void SavePosition()
			{
				//TODO FIX
				//Log.Log($"DockSystem.SavePosition()", UPDATE_SYSTEM);
				//OuterConnector = new DistanceFromBlockPointProvider(Connector,
				//	new StaticPointProvider(
				//		Ship.TravelSystem.ToGlobalCoordinate(
				//			DistanceFromBlockPointProvider.LocalBlocCoordinates(Connector)
				//			//+ Base6Directions.GetVector(Ship.MainController.Orientation.TransformDirectionInverse(Connector.Orientation.Forward)) * (float)ACCURACY_POSITIONING * 0.3f
				//			)));
				//Log.Log($"DockSystem.SavePosition.OuterConnector.Now():{OuterConnector.Now()}", UPDATE_SYSTEM);
				////добавляем несколько метров, чтобы корабль при стыковке не оставался висеть в паре милиметров от коннектора
				//StartDocking = new DistanceFromBlockPointProvider(Connector,
				//	new StaticPointProvider(
				//		Ship.TravelSystem.ToGlobalCoordinate(
				//			DistanceFromBlockPointProvider.LocalBlocCoordinates(Connector)
				//			- Base6Directions.GetVector(connectorDir) * (float)DOCK_START_DISTANCE
				//			)));
				////StartDocking = new StaticPointProvider(
				////	Vector3D.Transform(Base6Directions.GetVector(connectorDir) * (float)DOCK_START_DISTANCE, Connector.WorldMatrix));
				//Log.Log($"DockSystem.SavePosition.StartDocking.Now():{StartDocking.Now()}", UPDATE_SYSTEM);
				//Log.Log($"DockSystem.SavePosition.End", UPDATE_SYSTEM);
			}

			public void SavePosition(Vector3D startDockGC, Vector3D OuterDockGC)
			{
				//TODO FIX
				//Log.Log($"DockSystem.SavePosition()", UPDATE_SYSTEM);
				//OuterConnector = new DistanceFromBlockPointProvider(Connector,
				//	new StaticPointProvider(
				//		OuterDockGC));
				//Log.Log($"DockSystem.SavePosition.OuterConnector.Now():{OuterConnector.Now()}", UPDATE_SYSTEM);
				////добавляем несколько метров, чтобы корабль при стыковке не оставался висеть в паре милиметров от коннектора
				//StartDocking = new StaticPointProvider(
				//	Vector3D.Transform(Base6Directions.GetVector(connectorDir) * (float)DOCK_START_DISTANCE, Connector.WorldMatrix));
				//Log.Log($"DockSystem.SavePosition.StartDocking.Now():{StartDocking.Now()}", UPDATE_SYSTEM);
				//Log.Log($"DockSystem.SavePosition.End", UPDATE_SYSTEM);
			}

			public class DockFactory : IFactoryTask
			{
				private readonly DockSystem _parent;

				public DockFactory(DockSystem dockSystem)
				{
					if (dockSystem == null) throw new Exception($"argument {nameof(dockSystem)} is null exception.");
					_parent = dockSystem;
				}

				public ITask GetTask()
				{
					//TODO FIX
					if (_parent.OuterConnector == null) throw new Exception($"Can't dock. {nameof(_parent.OuterConnector)} is null");
					if (_parent.StartDocking == null) throw new Exception($"Can't dock. {nameof(_parent.StartDocking)} is null");
					var answer = new TaskSequence();
					var dockTask = new OldComplexTask(OldComplexTask.EndCondition.Repeat);
					//точка, на которую надо навестись
					Vector3D orientationPoint = Ship.TravelSystem.ToGlobalCoordinate(2d * _parent.OuterConnector.Now(new Environment(Ship)) - _parent.StartDocking.Now(new Environment(Ship)));
					//отключим дрелли
					answer.AddTask(new DisableDrils());//TODO наверное, надо отключать все инструменты?
													   //Прилететь к начальной точке, это не надо повторять
					answer.AddTask(_parent._travelFactory.GetTask(_parent.StartDocking));
					//соориентировать коннектор по направлению
					dockTask.AddTask(new TurnDirectionToPoint(_parent.connectorDir, new StaticPointProvider(orientationPoint)));

					//подлететь к доку без вращений
					//var potMove = _parent._travelFactory.GetTask(_parent.OuterConnector) as PotentialMethodMove;
					//if(potMove != null)
					//{
					//	var tube = new TunnelPointProvider(_parent.StartDocking, _parent.OuterConnector);
					//	potMove.AddForce(new MaxPowerForceCalculator(tube), 0.5);
					//	potMove.AddForce(new InertialForceCalculator(tube), 1);
					//	potMove.AddForce(new OrbitingResistForce(tube), 0.5);
					//}
					//else
					dockTask.AddTask(_parent._travelFactory.GetTask(_parent.OuterConnector));//TODO может, имеет смысл как-то явно задавать, что вращения недопустимы?
																							 //на всякий случай включить коннектор
					dockTask.AddTask(new ApplyActionTask("OnOff_On", Ship.DockSystem.Connector));
					//и пристыковаться
					dockTask.AddTask(new Dock(_parent.Connector));
					//добавим стыковку в последовательность задач
					answer.AddTask(dockTask);
					return answer;
				}
			}

			public class UndockFactory : IFactoryTask
			{
				private readonly DockSystem _parent;

				public UndockFactory(DockSystem dockSystem)
				{
					if (dockSystem == null) throw new Exception($"argument {nameof(dockSystem)} is null exception.");
					_parent = dockSystem;
				}

				public ITask GetTask()
				{
					OldComplexTask answer = new OldComplexTask(OldComplexTask.EndCondition.Repeat);
					answer.AddTask(new UnDock(_parent.Connector));
					//вытащим направление коннектора для отстыковки
					Base6Directions.Direction forwardDir = Ship.MainController.Orientation.TransformDirectionInverse(Base6Directions.GetOppositeDirection(_parent.Connector.Orientation.Forward));
					//Log.Log("GetUnDockTask forwardDir" + forwardDir.ToString());
					//Log.Log("GetUnDockTask connector forward" + Connector.Orientation.Forward.ToString());
					//Log.Log("GetUnDockTask target point" + Connector.Orientation.Forward.ToString());
					//Вытащим точку, к которой надо прилететь чтобы закончить отстыковку
					Vector3D unDockEndPoint = Vector3D.Multiply(Base6Directions.GetVector(forwardDir), DOCK_START_DISTANCE);
					//улететь от дока без вращений
					answer.AddTask(_parent._travelFactory.GetTask(_parent.StartDocking));
					return answer;
				}
			}
		}
	}
}