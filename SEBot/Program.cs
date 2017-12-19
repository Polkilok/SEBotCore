using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program : MyGridProgram
	{

		//Задает имя основного блока корабля
		const string MAIN_BLOCK_NAME = "Fighter Cockpit";
		//Определяет дефолтную точность наведения на цель
		//Если разница между нормализованными векторами меньше заданного значения - всё ок
		//P. S. не факт, что нормализованными
		const float GYRO_E = 0.02f;
		//Если кораблик сильно ошибается при подлете к точке или пролетает её на высокой скорости,
		//попробуйте уменьшить значения констант ниже
		//если корабль крутится вокруг одной точки, но не летит к другой
		//попробуйте увеличить данные значения, особенно это касается точности позиционирования
		const double ACCURACY_POSITIONING = 1;//в м
		const double ACCURACY_SPEED = 1;//в м/с
		const double ACCURACY_FORCE = 300;//в Н
		const double SAFETY_FLY_HEIGHT = 17;// в м
		const double ORBITING_RESIST_K = 0.9;//в долях от оставшегося расстояния
											 //используется для расчета инерции
		const double INERTIAL_COEFFICIENT = 0.3;
		//используется при фиксации вращения
		const double VERY_LARGE_DISTANCE = 1e5;//в м
											   //используется при стыковки
		const double DOCK_START_DISTANCE = 30;//в м
											  //определяет мертвую зону (расстояние от целевой точки), в которой вращения не происходят
											  //Значение по умолчанию для задачи TurnDirectionToPoint
		const double DEATH_ZONE_FOR_ROTATION = 3;//в м
												 //Определяет величину стороны решетки куба
		const double CUBE_GRID_SIDE = 5.0;
		//уменьшение частоты таймера
		const uint TICK_REDUCE = 10;
		//количество кадов в секунду, но лучше не использовать
		const double FPS = 60;
		//показывает, во сколко раз реальное ускорение больше расчетного
		const double ACCELERATION_K = 1.00937541809577;
		const int MAX_LOG_LINES = 2000;

		//дебажные константы
		const string INIT_SYSTEM = "Init";

		const int ENABLED_GROUP = CONDITION_LVL;
		const int UPDATE_SYSTEM = 0;
		const int GYRO_DEBUG_LVL = 1;
		const int THRUSTER_DEBUG_LVL = 1;
		const int MAIN_TASK_DEBUG_LVL = 1;
		const int COMPLEX_TASK_DEBUG_LVL = 2;
		const int GRAVITY_EXTENDS_TASKS = 3;
		const int EXTANDED_TASK_DEBUG_LVL = 4;
		const int TRAVEL_SYSTEM_DEBUG_LVL = 5;
		const int POTENTIAL_METHOD_DEBUG_LVL = 100;
		const int POTENTIAL_METHOD_POINT_PROVIDERS_LVL = 7;
		const int POTENTIAL_METHOD_RESULTS_DEBUG_LVL = 100;
		const int MINING_TASK_LVL = 9;
		const int CONDITION_LVL = 10;
		const int OLD_MOVEMENT_ENABLE_RULE_LVL = 11;
		const int GLOBAL_ALGORITHMIC_ACTION = 12;

		public static string FloorCoordinate(Vector3D pos)
		{
			double x = Math.Round(pos.GetDim(0), 3);
			double y = Math.Round(pos.GetDim(1), 3);
			double z = Math.Round(pos.GetDim(2), 3);
			return ("x:" + x.ToString() + "\ny:" + y.ToString() + "\nz:" + z.ToString());
		}

		public Program()
		{
			//начальная инициализация систем
			//Сначала логгер
			try
			{
				Log = new Logger(GridTerminalSystem, new List<string>
				{
					$"{ENABLED_GROUP}",
					INIT_SYSTEM
				}, MAX_LOG_LINES);
			}
			catch (Exception e)
			{
				Echo(e.ToString());
				return;
			}
			try
			{
				//потом корабль
				//а начнем с поиска главного блока
				var blocks = new List<IMyTerminalBlock>();
				IMyShipController myShip = (IMyShipController)GridTerminalSystem.GetBlockWithName(MAIN_BLOCK_NAME);
				if (myShip == null)
				{
					Log.Warning("Main block with name '" + MAIN_BLOCK_NAME + "' not found. Try another");
					GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(blocks);
					if (blocks.Count == 1)
						myShip = (IMyShipController)blocks[0];
					else
					{
						if (blocks.Count > 1)
						{
							myShip = (IMyShipController)blocks[0];
							Log.Warning("Find more then one RemoteControl not found. Try another");
						}
						else
							Log.Warning("Can't find RemoteControl. Try another");
						GridTerminalSystem.GetBlocksOfType<IMyCockpit>(blocks);
						if (blocks.Count >= 1)
						{
							myShip = (IMyShipController)blocks[0];
							foreach (IMyShipController cocpit in blocks)
								if (cocpit.IsUnderControl)
									myShip = cocpit;
						}
						else
							Log.Warning("Can't find Cockpit");
					}
				}
				//создадим прослойку для управления кораблем
				Ship = new ShipSystems(GridTerminalSystem, myShip);
				//и менеджер задач
				GlobalEventManeger = new EventManager(TICK_REDUCE);
				Log.Log(INIT_SYSTEM, "Created");

				//просто для удобства
				GlobalEventManeger.AddTask(new StopTask());

				Log.Flush();
			}
			catch (Exception e)
			{
				Echo(e.ToString());
				//Log.Error(e.ToString());
			}
		}

		void Start()
		{
			Log.Log(INIT_SYSTEM, "Started");

			//var b = Ship.DockSystem.Connector;
			//TaskCyclce testTask = new TaskCyclce();
			////testTask.AddTask(Ship.DockSystem.GetUnDockTask());
			//testTask.AddTask(AddHS(Ship.TravelSystem.DefaultTravelFactory.GetTask(
			//	new DistanceFromBlockPointProvider(b, new StaticPointProvider(new Vector3D(0, 0, 0))))));
			//testTask.AddTask(AddHS(Ship.TravelSystem.DefaultTravelFactory.GetTask(
			//	new DistanceFromBlockPointProvider(b, new StaticPointProvider(new Vector3D(10, 0, 0))))));
			//testTask.AddTask(AddHS(Ship.TravelSystem.DefaultTravelFactory.GetTask(
			//	new DistanceFromBlockPointProvider(b, new StaticPointProvider(new Vector3D(0, 10, 0))))));
			//testTask.AddTask(AddHS(Ship.TravelSystem.DefaultTravelFactory.GetTask(
			//	new DistanceFromBlockPointProvider(b, new StaticPointProvider(new Vector3D(0, 0, 10))))));
			////testTask.AddTask(Ship.DockSystem.GetDockTask());
			//GlobalEventManeger.AddTask(testTask);

			//check actions names
			//var actions = new List<ITerminalAction>();
			//Ship.DockSystem.Connector.GetActions(actions);
			//foreach (var action in actions)
			//	Log.Log(action.Id, ENABLED_GROUP);
			//Log.Flush();

			//var tp = new StaticPointProvider(new Vector3D(1034111.25, 174765.14, 1673531.88));
			//var mt = Ship.TravelSystem.DefaultTravelFactory.GetTask(tp);
			////mt.AddForce(new OrbitingResistForce(tp), 1.0);
			//GlobalEventManeger.AddTask(mt);

			//dock test
			//GlobalEventManeger.AddTask(Ship.DockSystem.GetDockTask());
			//GlobalEventManeger.AddTask(Ship.DockSystem.GetUnDockTask());
			//GlobalEventManeger.AddTask(Ship.TravelSystem.DefaultTravelFactory.GetTask(Ship.TravelSystem.GetPosition(), false));

			//var baseMove = Ship.TravelSystem.DefaultTravelFactory.GetTask(new Vector3D(51244.64, -30274.58, 12961.08));
			//GlobalEventManeger.AddTask(baseMove);

			//var targetPoint = new Vector3D(0);
			////var tpp = new StaticPointProvider(targetPoint);
			////var pt = new PotentialMethodMove(targetPoint, ACCURACY_POSITIONING, ACCURACY_SPEED, ACCURACY_FORCE);
			////pt.AddForce(new OrbitingResistForce(tpp), 1.0);
			//var pt = Ship.TravelSystem.DefaultTravelFactory.GetTask(targetPoint);
			//GlobalEventManeger.AddTask(pt);

			//test: simple patrol task cycle
			TaskCyclce testTask = new TaskCyclce();
			//testTask.AddTask(Ship.DockSystem.GetUnDockTask());
			testTask.AddTask(AddHS(Ship.TravelSystem.DefaultTravelFactory.GetTask(new Vector3D(0, 0, 0), false)));
			testTask.AddTask(AddHS(Ship.TravelSystem.DefaultTravelFactory.GetTask(new Vector3D(10, 0, 0), false)));
			testTask.AddTask(AddHS(Ship.TravelSystem.DefaultTravelFactory.GetTask(new Vector3D(0, 10, 0), false)));
			//testTask.AddTask(AddHS(Ship.TravelSystem.DefaultTravelFactory.GetTask(new Vector3D(0, 0, 10), false)));
			//testTask.AddTask(Ship.DockSystem.GetDockTask());
			GlobalEventManeger.AddTask(testTask);

			//test:mining
			//IFactoryPointBasedTask speedLimitTravelFactory = new SpeedLimitPotentialMethodFactory(3);
			//MiningHereFactoryTask miningFactory = new MiningHereFactoryTask(speedLimitTravelFactory, 50, 7, 70, 3);
			//GlobalEventManeger.AddTask(HSFactory.GetTask());
			//GlobalEventManeger.AddTask(miningFactory.GetTask());

			//ComplexTask testTask = new ComplexTask(ComplexTask.EndCondition.Repaired);
			//testTask.AddTaskAtEnd(new StopTask());
			//testTask.AddTaskAtEnd(new TurnDirectionToPoint(Base6Directions.Direction.Forward, new Vector3D(0, 0, 0), 1));
			//testTask.AddTaskAtEnd(new StopTask());
			//testTask.AddTaskAtEnd(Ship.TravelSystem.DefaultTravelFactory.GetTask(new Vector3D(0, 0, 0)));
			//GlobalEventManeger.AddTask(testTask);

			//test:simple observe task cycle
			//TaskCyclce testTask = new TaskCyclce();
			//testTask.AddTask(new TurnDirectionToPoint(Base6Directions.Direction.Forward, new Vector3D(0, 0, 0), 1));
			//testTask.AddTask(new TurnDirectionToPoint(Base6Directions.Direction.Forward, new Vector3D(10, 0, 0), 1));
			//testTask.AddTask(new TurnDirectionToPoint(Base6Directions.Direction.Forward, new Vector3D(0, 10, 0), 1));
			//testTask.AddTask(new TurnDirectionToPoint(Base6Directions.Direction.Forward, new Vector3D(0, 0, 10), 1));
			//GlobalEventManeger.AddTask(testTask);
		}

		private static readonly FactoryHorisontStability HSFactory = new FactoryHorisontStability();
		private static readonly HorisontallyStabilityCondition cond = new HorisontallyStabilityCondition();
		//TODO remove
		private static Task AddHS(Task travelTask)
		{
			InterruptedTask answer = new InterruptedTask(travelTask);

			answer.AddInterrupt(new Interrupt(cond, HSFactory));
			return answer;
		}


		static Logger Log;

		static EventManager GlobalEventManeger;

		class MyShipDrils
		{
			private List<IMyShipDrill> Tools;

			public MyShipDrils(IMyGridTerminalSystem gridTerminalSystem)
			{
				Tools = new List<IMyShipDrill>();
				gridTerminalSystem.GetBlocksOfType<IMyShipDrill>(Tools);
				Log.Log("Drils count " + Tools.Count.ToString());
			}

			public void Enable()
			{
				foreach (var tool in Tools)
				{
					tool.ApplyAction("OnOff_On");
				}
			}

			public void Disable()
			{
				foreach (var tool in Tools)
				{
					tool.ApplyAction("OnOff_Off");
				}
			}
		}

		static ShipSystems Ship;

		void Main(string argument)
		{
			try
			{
				if (argument.Length != 0)
					Log.Log($"Main({argument})", ENABLED_GROUP);
				switch (argument)
				{
					//case "Save Point":
					//	Log.Log("Point saved: " + FloorCoordinate(Ship.TravelSystem.GetPosition()));
					//	Ship.TravelSystem.Points.Push(Ship.TravelSystem.GetPosition());
					//	break;
					//case "Start Mining":
					//	GlobalEventManeger.AddTask(new GoMining());
					//	break;
					case "start":
						Start();
						break;
					case "flush":
						Log.Flush();
						break;
					default:
						GlobalEventManeger.Update();
						break;
				}
			}
			catch (Exception e)
			{
				Echo("e.ToString()");
				//Log.Error(e.ToString());
				GlobalEventManeger.Clear();
				GlobalEventManeger.AddTask(new StopTask());
			}
		}
	}

}