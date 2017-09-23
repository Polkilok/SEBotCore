using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities.Cube;
using VRageMath;
using VRage.Game;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Gui;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using ITerminalAction = Sandbox.ModAPI.Interfaces.ITerminalAction;

namespace Scripts
{
	public sealed class Program : MyGridProgram
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
		const int ENABLED_GROUP = CONDITION_LVL;
		const int INIT_SYSTEM = 0;
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

		static public string FloorCoordinate(Vector3D pos)
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
			Log = new Logger(GridTerminalSystem, ENABLED_GROUP);

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
			Log.Log("Created", INIT_SYSTEM);

			//и подвесим автосохранение координат коннектора
			GlobalEventManeger.AddSmallTask
			(
				new ConditionalTask
				(
					new DockedCondition(Ship.DockSystem.Connector),
					new SaveDockMatrix()
				)
			);
			//Log.Flush();
			//просто для удобства
			GlobalEventManeger.AddTask(new StopTask());
		}

		void Start()
		{
			Log.Log("Started", INIT_SYSTEM);

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
			var tp = new StaticPointProvider(new Vector3D(1034111.25, 174765.14, 1673531.88));
			var mt = Ship.TravelSystem.DefaultTravelFactory.GetTask(tp);
			//mt.AddForce(new OrbitingResistForce(tp), 1.0);
			GlobalEventManeger.AddTask(mt);

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
			//TaskCyclce testTask = new TaskCyclce();
			////testTask.AddTask(Ship.DockSystem.GetUnDockTask());
			//testTask.AddTask(AddHS(Ship.TravelSystem.DefaultTravelFactory.GetTask(new Vector3D(0, 0, 0), false)));
			//testTask.AddTask(AddHS(Ship.TravelSystem.DefaultTravelFactory.GetTask(new Vector3D(10, 0, 0), false)));
			//testTask.AddTask(AddHS(Ship.TravelSystem.DefaultTravelFactory.GetTask(new Vector3D(0, 10, 0), false)));
			//testTask.AddTask(AddHS(Ship.TravelSystem.DefaultTravelFactory.GetTask(new Vector3D(0, 0, 10), false)));
			////testTask.AddTask(Ship.DockSystem.GetDockTask());
			//GlobalEventManeger.AddTask(testTask);

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

		class SimpleFactory : IFactoryTask
		{
			private Func<Task> _f;
			public SimpleFactory(Func<Task> taskCreator)
			{
				_f = taskCreator;
			}
			public Task GetTask()
			{
				return _f();
			}
		}

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

		class Logger
		{
			private int lineCount;
			private IMyTextPanel DebugLog;
			private IMyTextPanel WarningAndErrorsLog;
			private StringBuilder buffer;
			private readonly int LogGroup;
			//инициализация логгера
			public Logger(IMyGridTerminalSystem GridTerminalSystem, int useGroup)
			{
				buffer = new StringBuilder();
				DebugLog = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("DebugLog");
				WarningAndErrorsLog = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("ErrorLog");
				LogGroup = useGroup;
				if (DebugLog != null)
				{
					DebugLog.WritePublicText("");
					DebugLog.ShowPublicTextOnScreen();
					if (WarningAndErrorsLog == null)
						WarningAndErrorsLog = DebugLog;
					WarningAndErrorsLog.WritePublicText(DateTime.Now.ToString("HH:mm:ss"));
					WarningAndErrorsLog.ShowPublicTextOnScreen();
				}
				lineCount = 0;
			}
			public void Log(string msg, int logGroup = 1)
			{
				if (logGroup == LogGroup)
				{
					buffer.AppendLine(msg);
					lineCount++;
					if (lineCount > MAX_LOG_LINES)
						Flush();
				}
				if (logGroup == INIT_SYSTEM)
					if (DebugLog != null)
						DebugLog.WritePublicText(msg + '\n', true);
			}
			public void Warning(string msg)
			{
				if (WarningAndErrorsLog != null)
				{
					WarningAndErrorsLog.WritePublicText("\nWARNING: " + msg, true);
				}
			}
			public void Error(string msg)
			{
				if (WarningAndErrorsLog != null)
				{
					WarningAndErrorsLog.WritePublicText("\nERRROR: " + msg, true);
				}
			}
			public void Clear()
			{
				if (DebugLog != null)
					DebugLog.WritePublicText("");
				if (WarningAndErrorsLog != null)
					WarningAndErrorsLog.WritePublicText("");
			}
			public void Flush()
			{
				if (DebugLog != null)
				{
					var msg = buffer.ToString();//Split('\n').Where((s) => s.Length > 3).Aggregate((s1, s2) => s1.Append(s2));
					DebugLog.WritePublicText("cleared\n");
					DebugLog.WritePublicText(msg.ToString(), true);
					//var lines = DebugLog.GetPublicText().Split('\n');
					//if (lines.Length > MAX_LOG_LINES)
					//{
					//	DebugLog.WritePublicText($"cleared\n" + lines.Skip((int)(0.75*MAX_LOG_LINES)).Aggregate((a, b) => $"{a}\n{b}"));
					//}
				}
				buffer = new StringBuilder();
				lineCount = 0;
			}
		}
		static Logger Log;

		/// <summary>
		/// Интерфейс, описывающий объект, который может быть сохранен
		/// </summary>
		interface Serialized
		{
			string Serialize();
			void Serialize(StringBuilder buf);
			Serialized Deserialize(StringBuilder buf);
		}

		class DefaultSeialize : Serialized
		{
			public string Serialize()
			{
				string typename = GetType().ToString();
				return $"<{typename}>{ToString()}</{typename}>";
			}
			public void Serialize(StringBuilder buf)
			{
				buf.Append(Serialize());
			}
			public Serialized Deserialize(StringBuilder buf)
			{
				return null;
			}
		}

		interface Task
		{
			bool Execute();
		}

		//Класс, описывающий прерывания
		class Interrupt
		{
			public readonly IFactoryTask Handler;
			public readonly ICondition Condition;
			public Interrupt(ICondition condition, IFactoryTask handler)
			{
				Condition = condition;
				Handler = handler;
			}
		}

		/// <summary>
		/// Задача, которая дает указание выполнить остановку
		/// </summary>
		class StopTask : Task
		{
			public bool Execute()
			{
				Ship.MovementSystem.Stop();
				Ship.OrientationSystem.DisableOverride();
				return true;
			}
		}

		abstract class IComplexTask : Task
		{
			protected readonly List<Task> Tasks;

			public IComplexTask()
			{
				Tasks = new List<Task>();
			}
			public void AddTask(Task t)
			{
				Tasks.Add(t);
			}
			public bool Execute()
			{
				if (Tasks.Count == 0)
				{
					Log.Warning("ComplexTask: empty");
					return true;//чтобы пропускать пустые листы
				}
				return ComplexExecute();
			}
			//Реализуйте собственный вариант комплексной задачи здесь
			//Проверять отсутствие задач не нужно!
			protected abstract bool ComplexExecute();
		}

		class AsynkComplexTask : IComplexTask
		{
			protected override bool ComplexExecute()
			{
				//иначе нас ждет оптимизация
				//List<bool> flags = new List<bool>(Tasks.Count);
				//for (int i = 0; i < Tasks.Count; ++i)
				//	flags[i] = Tasks[i].Execute();
				//return !flags.Contains(false);
				int flag = Tasks.Count;
				for (int i = 0; i < Tasks.Count; ++i)
					flag += Tasks[i].Execute() ? -1 : 0;
				return flag == 0;
			}
		}

		//представляет задачу, требующую одновременного выполнения нескольких других
		class OldComplexTask : Task
		{
			private readonly List<Task> Tasks;
			private readonly EndCondition Condition;
			public enum EndCondition { All, Any, Last, Repeat };
			//Создает пустую задачу одновременного выполнения, используя заданное условие выполнения
			//All - все задачи должны вернуть true
			//Any - хотя бы одна возвращает true - Задача выполнена
			//Last - только последняя - задача считается выполненной, если последняя вернула true
			//Repeat - следующая задача выполняется только если ВСЕ предыдущие вернули true, проверка на каждом тике
			//При этом возможен "возврат" назад, если какая-то здача провалится
			//TODO разнести по разным классам
			public OldComplexTask(EndCondition endCondition = EndCondition.All)
			{
				Tasks = new List<Task>();
				Condition = endCondition;
			}
			public bool Execute()
			{
				//Log.Log("Task \'ComplexTask\'");
				if (Tasks.Count == 0)
				{
					Log.Warning("ComplexTask: empty");
					return true;//чтобы пропускать пустые листы
				}

				List<bool> flags = new List<bool>(Tasks.Count);//иначе нас ждет оптимизация
				if (Condition == EndCondition.Repeat)
				{
					int i = 0;
					bool flag = true;
					while (i < Tasks.Count && flag)
						flag = Tasks[i++].Execute();
					return flag;
				}
				for (int i = 0; i < Tasks.Count; ++i)
					flags.Add(Tasks[i].Execute());
				if (Condition == EndCondition.All)
					return !flags.Contains(false);
				else if (Condition == EndCondition.Any)
					return flags.Contains(true);
				else if (Condition == EndCondition.Last)
					return flags[Tasks.Count - 1];
				else
					throw new Exception("Bad value in \'ComplexTask\' for ICondition or unrealized functional");
			}
			public void AddTask(Task task)
			{
				Tasks.Add(task);
			}
		}

		//представляет задачу последовательного выполнения задач
		//TODO объединить с ComplexTask
		class TaskSequence : Task
		{
			//Очередь задач
			private Queue<Task> TaskQueue;

			private Task CurrentTask;

			public TaskSequence()
			{
				TaskQueue = new Queue<Task>();
				CurrentTask = null;
			}

			public void AddTask(Task task)
			{
				TaskQueue.Enqueue(task);
			}

			//TODO проверить
			public void Reverse()
			{
				TaskQueue = new Queue<Task>(TaskQueue.Reverse());
			}

			public bool Execute()
			{
				if (CurrentTask == null)
					if (TaskQueue.Count > 0)
						CurrentTask = TaskQueue.Dequeue();
					else
						return true;
				if (CurrentTask.Execute())
					if (TaskQueue.Count == 0)
						CurrentTask = null;
					else
						CurrentTask = TaskQueue.Dequeue();
				if (CurrentTask != null) Log.Log($"TaskSequence.CurrentTask:{CurrentTask}", COMPLEX_TASK_DEBUG_LVL);
				return false;
			}
		}

		//представляет задачу цикличного выполнения задач
		class TaskCyclce : Task
		{
			//Очередь задач
			private List<Task> TaskList;
			private int CurrentTaskIndex;
			public TaskCyclce()
			{
				TaskList = new List<Task>();
				CurrentTaskIndex = 0;
			}
			public void AddTask(Task task)
			{
				TaskList.Add(task);
			}
			public bool Execute()
			{
				if (TaskList.Count == 0)
					return true;
				else if (TaskList[CurrentTaskIndex].Execute())
					CurrentTaskIndex = (CurrentTaskIndex + 1) % TaskList.Count;
				return false;
			}
		}

		//Предоставляет возможность добавлять прерывания для задачи
		//Единовременно обрабатывается только 1 прерывание
		//На время его обработки остальные прерывания заблокированы
		//Их срабатывание нигде не отмечается и не сохраняется
		class InterruptedTask : Task
		{
			//основная задача
			private readonly Task MainTask;
			//обрабатываемое прерывание
			private Task Interrupt;
			//Список прерываний
			private List<Interrupt> Interrupts;

			public InterruptedTask(Task mainTask)
			{
				MainTask = mainTask;
				Interrupt = null;
				Interrupts = new List<Interrupt>();
			}

			public void AddInterrupt(Interrupt interrupt)
			{
				Interrupts.Add(interrupt);
			}

			public bool Execute()
			{
				//нету прерывания? проверим возможные срабатывания
				if (Interrupt == null)
					Interrupt = CheckInterrupts();
				//всё еще нет? тогда выполним основную задачу
				if (Interrupt == null)
					return MainTask.Execute();
				//Есть прерывание
				//Выполним его, если оно выполнится - удалим
				if (Interrupt.Execute())
					Interrupt = null;
				return false;//Конечно, задача не может в таком случае считаться выполненной
			}

			//проверяет условия прерываний
			private Task CheckInterrupts()
			{
				foreach (var inter in Interrupts)
				{
					//Log.Log("check interrupt " + interrupt.Сond.);
					if (inter.Condition.Check())
						return inter.Handler.GetTask();
				}
				return null;
			}
		}

		//Класс, описывающий условную задачу - задачу, которая выполнится только при положительном условии
		//При любых усливиях возвращается true
		class ConditionalTask : Task
		{
			private readonly Task Handler;
			private readonly ICondition Condition;
			public ConditionalTask(ICondition condition, Task handler)
			{
				Condition = condition;
				Handler = handler;
			}

			public bool Execute()
			{
				if (Condition.Check())
					Handler.Execute();
				return true;
			}
		}

		//Представляет отложенно создаваемую задачу
		//Полезно, если понимание, что делать, приходит не сразу, а только в точке конечного назначения
		class DelayCreateTask : Task
		{
			private readonly IFactoryTask Creator;

			private Task RealTask;

			public DelayCreateTask(IFactoryTask taskCreator)
			{
				Creator = taskCreator;
				RealTask = null;
			}

			public bool Execute()
			{
				if (RealTask == null)
				{
					RealTask = Creator.GetTask();
				}
				return RealTask.Execute();
			}
		}

		//Задача поворота заданного направления к заданной точке
		//Можно указать мертвую зону, в которой вращения осуществляться не будут
		class TurnDirectionToPoint : Task
		{
			private readonly VRageMath.Vector3D Point;
			private readonly Base6Directions.Direction Direction;
			private readonly double MinRange;
			private readonly double Accuracy;

			private double Power;
			//создает задачу вращения заданного направления к точке
			//при этом можно указать расстояние до точки, начиная с которого данная задача больше выполняться не будет
			//TODO сохранять силу
			public TurnDirectionToPoint(Base6Directions.Direction direction, Vector3D point, double minRotationRange = 0, double accuracy = GYRO_E)
			{
				Point = point;
				Direction = direction;
				MinRange = minRotationRange;
				Accuracy = accuracy;
				Power = 1;
				Log.Log("TurnDirectionToPoint");
				Log.Log("minRotationRange " + minRotationRange.ToString("0.0"));
			}
			public bool Execute()
			{
				Log.Log("Task \'TurnDirectionToPoint\' \n" + FloorCoordinate(Point));
				Vector3D myPosition = Ship.TravelSystem.GetPosition();
				double leftDistance = (Point - myPosition).Length();
				if (leftDistance < MinRange)
				{
					Ship.OrientationSystem.DisableOverride();
					return true;
				}
				else
				{
					Ship.OrientationSystem.TurnDirectionToPoint(Direction, Point);
					Vector3D pointLC = Vector3D.Normalize(Ship.TravelSystem.ToLocalCoordinate(Point));

					Vector3D direction = Base6Directions.GetVector(Direction);

					var bias = pointLC - direction;

					bias.SetDim(direction.AbsMaxComponent(), 0);

					Log.Log("bias " + FloorCoordinate(bias));

					if (bias.Length() < Accuracy)
					{
						Ship.OrientationSystem.DisableOverride();
						return true;
					}
				}
				return false;
			}
		}

		class ApplyActionTask : Task
		{
			private IMyFunctionalBlock _block;
			private string _actionId;

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

		//предоставляет задачу перемещения в заданном направлении к заданной точке в абсолютных координатах
		//P. S. перемещает вдоль заданной прямой так, чтобы до точки было наименьшее расстояние,
		//может выполняться во время вращений
		//перемещения в перпендикулярных направлениях не учитываются
		//при "промахе" включается задний ход
		//дефолтный вариант 
		//после окончания переопределение направлений вперед/назад сбрасывается
		class MoveInDirection : Task
		{

			private readonly Base6Directions.Direction Direction;
			private readonly Vector3D DestinationPoint;
			private readonly ThrusterEnableRule EnableRule;
			public MoveInDirection(ThrusterEnableRule enableRule, Base6Directions.Direction direction, Vector3D point)
			{
				DestinationPoint = point;
				Direction = direction;
				EnableRule = enableRule;
			}
			public bool Execute()
			{
				//Log.Log("Task \'MoveInDirection\' :" + Direction.ToString());
				double leftDistance = CalculateLeftDistance();
				//Log.Log("LeftDistance " + Math.Round(LeftDistance, 1).ToString());
				if (EnableRule.PointIsReached(leftDistance, Direction))
				{
					//точка достигнута
					//сбросим тягу
					Ship.MovementSystem.OverrideDirection(Direction, false);
					Ship.MovementSystem.OverrideDirection(Base6Directions.GetOppositeDirection(Direction), false);
					return true;
				}
				else
					EnableThrust(leftDistance);
				return false;

			}

			private double CalculateLeftDistance()
			{
				Vector3D DestinationPointLC = Ship.TravelSystem.ToLocalCoordinate(DestinationPoint);
				Vector3D mask = Base6Directions.GetVector(Direction);
				//Log.Log("Left Distance trunc vec " + FloorCoordinate(trunc));
				return (DestinationPointLC * mask).Sum;
			}

			private void EnableThrust(double leftDistance)
			{
				Base6Directions.Direction TargetDirection = leftDistance > 0 ? Direction :
					Base6Directions.GetOppositeDirection(Direction);
				leftDistance = Math.Abs(leftDistance);
				Log.Log("MoveInDirection\tMove in real direction " + TargetDirection.ToString());
				if (EnableRule.EnableCondition(leftDistance, TargetDirection))
					Ship.MovementSystem.MoveInDirection(TargetDirection,
						EnableRule.ThrustPower(leftDistance, TargetDirection));
				else
				{
					//считаем, что включена система гашения инерции
					//TODO включать принудительно?
					Ship.MovementSystem.MoveInDirection(TargetDirection, 0);
					//TODO строчка ниже не нужна?
				}
			}
		}

		//Класс, который предоставляет методы расчета условия включения двигателей
		//а так же мощность(в долях от максимума), с которой их нужно активировать
		class ThrusterEnableRule
		{
			protected readonly double AccuracyPositioning;
			protected readonly double AccuracySpeed;

			public ThrusterEnableRule(double accuracyPositioning, double accuracySpeed)
			{
				AccuracySpeed = accuracySpeed;
				AccuracyPositioning = accuracyPositioning;
			}

			//Проверяет условие достижения точки
			public bool PointIsReached(double leftDistance, Base6Directions.Direction direction)
			{
				leftDistance = Math.Abs(leftDistance);
				return leftDistance < AccuracyPositioning &&
					   Math.Abs(Ship.MovementSystem.GetSpeedInDirection(direction)) < AccuracySpeed;
			}

			//Подсказывает, нужно ли включать двигатели
			public virtual bool EnableCondition(double leftDistance, Base6Directions.Direction direction)
			{
				double v = Ship.MovementSystem.GetSpeedInDirection(direction);
				//Log.Log("ship speed " + Math.Round(v, 1).ToString());
				if (leftDistance > 15 || v < AccuracySpeed)//TODO magic number
					return true;
				else
					return false;
			}

			//Подсказывает, мощность двигателей (в долях от максимальной, т. е. в диапазоне от 0 до 1)
			//По умолчанию 1
			public virtual float ThrustPower(double leftDistance, Base6Directions.Direction direction)
			{
				return 1;
			}
		}

		//Расширяет вариант MoveInDirection
		//вычисляет, может ли корабль остановиться вовремя на основе 
		//его кинетической энергии и силы двигателей
		//TODO учитывать гравитацию
		class InertialThrusterEnableRule : ThrusterEnableRule
		{
			//коэфициент передачи мощности дигателей, рекомендуется ставить меньше реального
			//это актуально для кораблей на ионниках и атмосферных двгателях в разреженной/плотной атмосферы
			//TODO работает странно
			//TODO Перспектива: сделать автобалансирование
			private double k;

			public InertialThrusterEnableRule(double accuracyPositioning, double accuracySpeed,
				double inertialCoefficient = INERTIAL_COEFFICIENT)
				: base(accuracyPositioning, accuracySpeed)
			{
				k = inertialCoefficient;
			}
			public override bool EnableCondition(double leftDistance, Base6Directions.Direction direction)
			{
				Log.Log($"InertialThrusterEnableRule.EnableСondition({leftDistance}, {direction})", OLD_MOVEMENT_ENABLE_RULE_LVL);
				int m = Ship.Mass;
				Log.Log($"InertialThrusterEnableRule.EnableСondition.{nameof(m)}:{m}", OLD_MOVEMENT_ENABLE_RULE_LVL);
				double F = Ship.MovementSystem.GetMaxPowerInDirection(Base6Directions.GetOppositeDirection(direction));
				Log.Log($"InertialThrusterEnableRule.EnableСondition.{nameof(F)}:{F}", OLD_MOVEMENT_ENABLE_RULE_LVL);
				double gravity = (Ship.Gravity * Base6Directions.GetVector(direction)).Sum;//добавим проекцию силы тяжести
				Log.Log($"InertialThrusterEnableRule.EnableСondition.{nameof(gravity)}:{gravity}", OLD_MOVEMENT_ENABLE_RULE_LVL);
				//TODO проверить правильность расчета тяги в заданном направлении и формулы ниже
				double s = leftDistance;
				double v = Ship.MovementSystem.GetSpeedInDirection(direction);
				Log.Log($"InertialThrusterEnableRule.EnableСondition.{nameof(v)}:{v}", OLD_MOVEMENT_ENABLE_RULE_LVL);
				Log.Log("Result of calc: " + ((2d * k * F * s) > (m * v * v)).ToString());
				Log.Log($"InertialThrusterEnableRule.EnableСondition.End", OLD_MOVEMENT_ENABLE_RULE_LVL);
				if (v < AccuracySpeed)//если скорость ниже порога точности - замем мучаться?
					return true;//если вдруг скорость направленна в противоположном направлении, её необходимо погасть
				else
					return (2d * k * F * s) > (m * v * v);
			}
			//TODO исправить
			public override float ThrustPower(double leftDistance, Base6Directions.Direction direction)
			{
				double s1 = leftDistance;
				double v1 = Ship.MovementSystem.GetSpeedInDirection(direction);
				double t = GlobalEventManeger.TickPeriod / 1000.0;
				double m = Ship.Mass;
				double Fmax = Ship.MovementSystem.GetMaxPowerInDirection(direction);

				double s2 = Math.Max(s1 - v1 * t, 0);
				double x = (0.5 * m * v1 * v1 - k * Fmax * s2) / Fmax / (s1 - s2);
				Log.Log($"direction:{direction}", TRAVEL_SYSTEM_DEBUG_LVL);
				Log.Log($"s1:{s1}, s2:{s2}, v1:{v1}", TRAVEL_SYSTEM_DEBUG_LVL);
				Log.Log($"ThrustPower. Resultation coef:{x}", TRAVEL_SYSTEM_DEBUG_LVL);
				if (x < 0)
					return 1f;
				return Math.Min(1f, Math.Max((float)x, 0.05f));
				//return 0.7f;
			}

		}

		//класс, предоставляющий движение с ограничением скорости
		//Если скорость в заданном направлении превышает целевую - отключене двигателей
		//Иначе - правило additionRule
		class SpeedLimitThrusterEnableRule : ThrusterEnableRule
		{
			private double SpeedLimit;
			private ThrusterEnableRule BaseRule;
			//Создает правило, ограничивающее максимальную скорость для заданного дополнительного правила
			//по умолчанию используется реализация InertialRuleThrusterEnableRule
			public SpeedLimitThrusterEnableRule(double accuracyPositioning, double accuracySpeed,
				double speedLimit, ThrusterEnableRule additionRule = null)
				: base(accuracyPositioning, accuracySpeed)
			{
				SpeedLimit = speedLimit;
				BaseRule = additionRule;
				if (BaseRule == null)
					BaseRule = new InertialThrusterEnableRule(accuracyPositioning, accuracySpeed);
			}
			public override bool EnableCondition(double leftDistance, Base6Directions.Direction direction)
			{
				double v = Ship.MovementSystem.GetSpeedInDirection(direction);
				return v < SpeedLimit && base.EnableCondition(leftDistance, direction);
			}
		}

		//Интерфейс фабрики, которая будет создавать задачи, основным аргументом которых является точка и необязательное условие
		interface IFactoryPointBasedTask
		{
			Task GetTask(Vector3D targetPoint, bool condition = false);
		}

		/// <summary>
		/// Предоставляет возможность создавать задачи, на основе меняющейся точки
		/// </summary>
		interface IFactoryPointProviderBasedTask
		{
			Task GetTask(IPointProvider targetPoint);
		}

		//Derpricated
		//Интерфейс фабрики, которая будет создавать задачи, основными аргументами которых является точка + направление
		interface IFactoryPointDirectionBasedTask
		{
			Task GetTask(Vector3D targetPoint, Base6Directions.Direction direction);
		}

		//интерфейс фабрики, которая будет создавать определенные задачи
		//Важно - не храните такие задачи - фабрики сделаны специально, чтобы создавать задачи по мере необходимости
		interface IFactoryTask
		{
			Task GetTask();
		}

		class FactoryMoveInDirection : IFactoryPointDirectionBasedTask
		{
			private readonly ThrusterEnableRule ThrusterEnableSwitch;
			public FactoryMoveInDirection(ThrusterEnableRule rule)
			{
				ThrusterEnableSwitch = rule;
			}

			public Task GetTask(Vector3D targetPoint, Base6Directions.Direction direction)
			{
				return new MoveInDirection(ThrusterEnableSwitch, direction, targetPoint);
			}
		}

		//класс, предоставляющий фабрику задач движения к заданной точке
		//использование вращения задается дополнительным условием (true - Использовать)
		class FactoryMoveToPoint : IFactoryPointBasedTask
		{
			private readonly IFactoryPointDirectionBasedTask MoveInDirectionFactory;

			private readonly double NoRotationRange;
			//аргумент moveInDirectionFactory будет передан в конструктор TravelToPoint
			public FactoryMoveToPoint(IFactoryPointDirectionBasedTask moveInDirectionFactory, double noRotationRange = DEATH_ZONE_FOR_ROTATION)
			{
				MoveInDirectionFactory = moveInDirectionFactory;
				NoRotationRange = noRotationRange;
			}
			public Task GetTask(Vector3D targetPoint, bool rotatate = false)
			{
				if (rotatate == false)
				{
					//OldComplexTask task = new OldComplexTask();
					AsynkComplexTask task = new AsynkComplexTask();
					task.AddTask(MoveInDirectionFactory.GetTask(targetPoint, Base6Directions.Direction.Left));
					task.AddTask(MoveInDirectionFactory.GetTask(targetPoint, Base6Directions.Direction.Up));
					task.AddTask(MoveInDirectionFactory.GetTask(targetPoint, Base6Directions.Direction.Forward));
					return task;
				}
				Task rotateTask = new TurnDirectionToPoint(Base6Directions.Direction.Forward, targetPoint, NoRotationRange);
				OldComplexTask outerTask = new OldComplexTask(OldComplexTask.EndCondition.Last);
				outerTask.AddTask(rotateTask);
				outerTask.AddTask(GetTask(targetPoint, false));
				return outerTask;
			}
		}

		//Фабрика, которая создает задачи стабилизации относительно горизонта
		class FactoryHorisontStability : IFactoryTask
		{
			public Task GetTask()
			{
				Log.Log($"FactoryHorisontStability.GetTask()", GRAVITY_EXTENDS_TASKS);
				Vector3D gravity = Ship.MainController.GetTotalGravity();
				Log.Log($"FactoryHorisontStability.GetTask.gravity(global):{Vector3.Round(gravity, 2)}", GRAVITY_EXTENDS_TASKS);
				gravity = Ship.MainController.GetPosition() + gravity * VERY_LARGE_DISTANCE;//TODO сделать параметром, передаваемым в конструктор
				Log.Log($"FactoryHorisontStability.GetTask.gravity(far far avay point):{Vector3.Round(gravity, 2)}", GRAVITY_EXTENDS_TASKS);
				TaskSequence answer = new TaskSequence();
				answer.AddTask(new StopTask());
				answer.AddTask(new TurnDirectionToPoint(Base6Directions.Direction.Down, gravity));
				Log.Log($"FactoryHorisontStability.GetTask.answer:{answer}", GRAVITY_EXTENDS_TASKS);
				Log.Log($"FactoryHorisontStability.GetTask.End", GRAVITY_EXTENDS_TASKS);
				return answer;
			}
		}

		interface ICondition
		{
			//проверяет условие
			bool Check();
		}

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
		//Создает условие, которое срабатывает, когда заданное направление двигателей использует тягу более заданной мощности
		//Здесь мощность - отношение реальной силы к максимальной, т. е. в диапазоне от 0 до 1
		class OverloadCondition : ICondition
		{
			private Base6Directions.Direction ThrustersDirection;
			private float WarningLevel;
			public OverloadCondition(Base6Directions.Direction thrustersDirection, float warningLevel)
			{
				ThrustersDirection = thrustersDirection;
				WarningLevel = warningLevel;
			}
			public bool Check()
			{
				double Power = Ship.MovementSystem.GetPowerForDirection(ThrustersDirection);
				//Log.Log("Power Override: " + Power.ToString("0.00"));
				double MaxPower = Ship.MovementSystem.GetMaxPowerInDirection(ThrustersDirection);
				return Power / MaxPower > WarningLevel;
			}
		}

		class CriticalMassCondition : ICondition
		{
			private double _criticalMass;
			//Создает условие, которое срабатывает, когда заданное направление двигателей использует тягу более заданной мощности
			//Здесь мощность - отношение реальной силы к максимальной, т. е. в диапазоне от 0 до 1
			public CriticalMassCondition(double criticalMass)
			{
				Log.Log($"CriticalMassCondition.CriticalMassCondition({criticalMass})", CONDITION_LVL);
				if (criticalMass < 0.0) throw new Exception($"argument out of range. {nameof(criticalMass)}:{criticalMass}, wait >0.0");
				_criticalMass = criticalMass;
			}
			public bool Check()
			{
				return Ship.Mass > _criticalMass;
			}
		}

		class HorisontallyStabilityCondition : ICondition
		{
			public bool Check()
			{
				if (Ship.MainController.GetTotalGravity().Length() < GYRO_E)
					return false;
				Vector3D gravity = Ship.TravelSystem.ToLocalCoordinate(Ship.TravelSystem.GetPosition() + Ship.MainController.GetTotalGravity());
				Vector3D down = Base6Directions.GetVector(Base6Directions.Direction.Down);
				gravity = Vector3D.Normalize(gravity);
				Vector3D bias = down - gravity;
				bias.SetDim(bias.AbsMaxComponent(), 0);
				//Log.Log("Cond. bias " + FloorCoordinate(bias));
				if (bias.Length() > GYRO_E)
					return true;
				else
					return false;
			}
		}

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

		class WaitTask : Task
		{
			ICondition condition;
			public WaitTask(ICondition cond)
			{
				condition = cond;
			}
			public bool Execute()
			{
				return condition.Check();
			}
		}

		//класс, описывающтй задачу стыковки - банально переключает состояние коннектора
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

		//класс, описывающтй задачу отстыковки - банально переключает состояние коннектора
		class UnDock : Task
		{
			private readonly IMyShipConnector _connector;
			//Создает задачу стыковки указанным коннектором, передавая позицию основного блока 
			//при последней успешной стыковке в качестве аргумента DockingPosition
			public UnDock(IMyShipConnector connector)
			{
				_connector = connector;
			}
			public bool Execute()
			{
				Log.Log($"UnDock.Execute()", GLOBAL_ALGORITHMIC_ACTION);
				Log.Log($"UnDock.Execute._connector.Status:{_connector.Status}", GLOBAL_ALGORITHMIC_ACTION);
				if (_connector.Status == MyShipConnectorStatus.Connected)
				{
					Ship.DockSystem.SavePosition();
					TerminalBlockExtentions.ApplyAction(_connector, "SwitchLock");
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

		class ReverToBaseFactoryTask : IFactoryTask
		{
			public Task GetTask()
			{
				Log.Log($"ReverToBaseFactoryTask.GetTask()", GLOBAL_ALGORITHMIC_ACTION);
				Log.Log($"ReverToBaseFactoryTask.Time:{DateTime.Now.ToShortTimeString()}", GLOBAL_ALGORITHMIC_ACTION);
				var answer = new TaskSequence();
				answer.AddTask(new DisableDrils());//TODO в перспективе выключать все инструменты
				answer.AddTask(Ship.DockSystem.GetDockTask());
				answer.AddTask(new WaitTask(new WaitTimeCondition(TimeSpan.FromSeconds(10))));
				answer.AddTask(Ship.DockSystem.GetUnDockTask());
				//отключение, т. к. коннектор может быть выставленн на автосброс
				answer.AddTask(new ApplyActionTask("OnOff_Off", Ship.DockSystem.Connector));
				//TODO ориентация корабля тоже имеет значение, возможно, сделать движение с вращением?
				answer.AddTask(Ship.TravelSystem.DefaultTravelFactory.GetTask(Ship.TravelSystem.GetPosition()));
				Log.Log($"ReverToBaseFactoryTask.End", GLOBAL_ALGORITHMIC_ACTION);
				return answer;
			}
		}

		class DockSystem
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
			public Task GetDockTask()
			{
				return DockTaskGenerator.GetTask();
			}

			//предоставляет задачу отстыковки
			public Task GetUnDockTask()
			{
				return UndockTaskGenerator.GetTask();
			}

			public void SavePosition()
			{
				Log.Log($"DockSystem.SavePosition()", UPDATE_SYSTEM);
				OuterConnector = new DistanceFromBlockPointProvider(Connector,
					new StaticPointProvider(
						Ship.TravelSystem.ToGlobalCoordinate(
							DistanceFromBlockPointProvider.LocalBlocCoordinates(Connector)
							//+ Base6Directions.GetVector(Ship.MainController.Orientation.TransformDirectionInverse(Connector.Orientation.Forward)) * (float)ACCURACY_POSITIONING * 0.3f
							)));
				Log.Log($"DockSystem.SavePosition.OuterConnector.Now():{OuterConnector.Now()}", UPDATE_SYSTEM);
				//добавляем несколько метров, чтобы корабль при стыковке не оставался висеть в паре милиметров от коннектора
				StartDocking = new DistanceFromBlockPointProvider(Connector,
					new StaticPointProvider(
						Ship.TravelSystem.ToGlobalCoordinate(
							DistanceFromBlockPointProvider.LocalBlocCoordinates(Connector)
							- Base6Directions.GetVector(connectorDir) * (float)DOCK_START_DISTANCE
							)));
				//StartDocking = new StaticPointProvider(
				//	Vector3D.Transform(Base6Directions.GetVector(connectorDir) * (float)DOCK_START_DISTANCE, Connector.WorldMatrix));
				Log.Log($"DockSystem.SavePosition.StartDocking.Now():{StartDocking.Now()}", UPDATE_SYSTEM);
				Log.Log($"DockSystem.SavePosition.End", UPDATE_SYSTEM);
			}

			public void SavePosition(Vector3D startDockGC, Vector3D OuterDockGC)
			{
				Log.Log($"DockSystem.SavePosition()", UPDATE_SYSTEM);

				OuterConnector = new DistanceFromBlockPointProvider(Connector,
					new StaticPointProvider(
						OuterDockGC));
				Log.Log($"DockSystem.SavePosition.OuterConnector.Now():{OuterConnector.Now()}", UPDATE_SYSTEM);
				//добавляем несколько метров, чтобы корабль при стыковке не оставался висеть в паре милиметров от коннектора
				StartDocking = new StaticPointProvider(
					Vector3D.Transform(Base6Directions.GetVector(connectorDir) * (float)DOCK_START_DISTANCE, Connector.WorldMatrix));
				Log.Log($"DockSystem.SavePosition.StartDocking.Now():{StartDocking.Now()}", UPDATE_SYSTEM);
				Log.Log($"DockSystem.SavePosition.End", UPDATE_SYSTEM);
			}

			public class DockFactory : IFactoryTask
			{
				private readonly DockSystem _parent;
				public DockFactory(DockSystem dockSystem)
				{
					if (dockSystem == null) throw new Exception($"argument {nameof(dockSystem)} is null exception.");
					_parent = dockSystem;
				}
				public Task GetTask()
				{
					if (_parent.OuterConnector == null) throw new Exception($"Can't dock. {nameof(_parent.OuterConnector)} is null");
					if (_parent.StartDocking == null) throw new Exception($"Can't dock. {nameof(_parent.StartDocking)} is null");
					var answer = new TaskSequence();
					var dockTask = new OldComplexTask(OldComplexTask.EndCondition.Repeat);
					//точка, на которую надо навестись
					Vector3D orientationPoint = Ship.TravelSystem.ToGlobalCoordinate(2d * _parent.OuterConnector.Now() - _parent.StartDocking.Now());
					//отключим дрелли
					answer.AddTask(new DisableDrils());//TODO наверное, надо отключать все инструменты?
													   //Прилететь к начальной точке, это не надо повторять
					answer.AddTask(_parent._travelFactory.GetTask(_parent.StartDocking));
					//соориентировать коннектор по направлению
					dockTask.AddTask(new TurnDirectionToPoint(_parent.connectorDir, orientationPoint));

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
				public Task GetTask()
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

		class SaveDockMatrix : Task
		{
			public bool Execute()
			{
				Log.Log("Dock Matrix saved");
				//TODO проверять, пристыкован ли кораблик
				Ship.DockSystem.SavePosition();
				return true;
			}
		}

		/******************************************************************************/
		/******************************************************************************/
		/******************************************************************************/
		//Специфичные задачи и фабрики

		//Да, задача просто включает буры
		class EnableDrils : Task
		{
			public bool Execute()
			{
				Ship.Drils.Enable();
				return true;
			}
		}

		class DisableDrils : Task
		{
			public bool Execute()
			{
				Ship.Drils.Disable();
				return true;
			}
		}

		//Определяет задачу добычи ресов в точке первого вызова
		//майнинг происходит по направлению вниз
		//Создает задачу движения змейкой в момент вызова
		class MiningHereFactoryTask : IFactoryTask
		{
			private readonly double LenghtSide;
			private readonly int LineCount;
			private readonly double MiningDeep;
			private readonly double DeepStep;
			private readonly IFactoryPointBasedTask SpeedLimitFactory;
			//размеры даются в метрах
			public MiningHereFactoryTask(IFactoryPointBasedTask speedLimitFactory, double lenghtSide, double widthShip, double miningDeep, double deepStep)
			{
				LenghtSide = lenghtSide;
				MiningDeep = miningDeep;
				DeepStep = deepStep;
				LineCount = (int)Math.Ceiling(lenghtSide / widthShip);
				SpeedLimitFactory = speedLimitFactory;
			}

			public Task GetTask()
			{
				Log.Log($"MiningHereFactoryTask.GetTask()", MINING_TASK_LVL);
				//0 - нужно включить буры
				//для этого создадим востанавливающуюся задачу
				OldComplexTask answer = new OldComplexTask(OldComplexTask.EndCondition.Repeat);
				Log.Log($"MiningHereFactoryTask.answer:{answer}", MINING_TASK_LVL);
				//и запихнём туда задачу включения буров
				answer.AddTask(new EnableDrils());
				//1 - во время движения нужно заблокировать вращения
				//добавим в начало задачу - посмотреть на тридевятое королевство
				Vector3D farFarAwayPoint = Base6Directions.GetVector(Base6Directions.Direction.Forward);
				Log.Log($"MiningHereFactoryTask.farFarAwayPoint(local):{Vector3.Round(farFarAwayPoint, 0)}", MINING_TASK_LVL);
				farFarAwayPoint = Ship.TravelSystem.ToGlobalCoordinate(farFarAwayPoint * VERY_LARGE_DISTANCE);
				Log.Log($"MiningHereFactoryTask.farFarAwayPoint(global):{Vector3.Round(farFarAwayPoint, 0)}", MINING_TASK_LVL);
				answer.AddTask(new TurnDirectionToPoint(Base6Directions.Direction.Forward, farFarAwayPoint));
				//теперь любые полёты будут происходить только после выравнивания на цель
				//2 - создадим классическую очередь задач - движение змейкой
				TaskSequence snakeMove = CreateMiningTask();
				Log.Log($"MiningHereFactoryTask.GetTask.snakeMove:{snakeMove}", MINING_TASK_LVL);
				//3 - добавим задачу в нашу камплексную задачу
				answer.AddTask(snakeMove);
				//4 - Profit
				Log.Log($"MiningHereFactoryTask.GetTask.answer:{answer}", MINING_TASK_LVL);
				Log.Log($"MiningHereFactoryTask.GetTask.End", MINING_TASK_LVL);
				return AddHS(AddUnloadInterrupt(answer));
			}

			private Task AddUnloadInterrupt(Task mainTask)
			{
				var answer = new InterruptedTask(mainTask);
				answer.AddInterrupt(new Interrupt(new CriticalMassCondition(
					Ship.MovementSystem.GetMaxPowerInDirection(Base6Directions.Direction.Down) / (Ship.Gravity.Length()) * 0.9), //TODO magick number 0.8
					new ReverToBaseFactoryTask()));
				return answer;
			}

			//создает задачу выемки 1 пласта земли
			private TaskSequence CreateLayerMining(MatrixD worldMatrixAtCentralPoint)
			{
				MatrixD matrixLoalCopy = new MatrixD(worldMatrixAtCentralPoint);
				TaskSequence checkPoints = new TaskSequence();
				double scaleFactor = LenghtSide / 2.0;
				Vector3D vecRight = Base6Directions.GetVector(Base6Directions.Direction.Right);
				vecRight *= LenghtSide / LineCount;
				//посчитаем точку начала
				Vector3D startPoint = -(Vector3D.Forward + Vector3D.Right) * scaleFactor;
				Vector3D point = Vector3D.Transform(startPoint, matrixLoalCopy);
				//удалим нафиг смещение - это 4-ая (3-я, считая с нуля) строчка матрицы
				Vector4 foo = matrixLoalCopy.GetRow(3);
				foo.X = 0;
				foo.Y = 0;
				foo.Z = 0;
				matrixLoalCopy.SetRow(3, foo);
				vecRight = Vector3D.Transform(vecRight, matrixLoalCopy);
				Vector3D line = Base6Directions.GetVector(Base6Directions.Direction.Forward);
				line *= LenghtSide;
				line = Vector3D.Transform(line, matrixLoalCopy);
				for (int i = LineCount; i >= 0; --i)
				{
					Log.Log("Snake point: line index - " + i.ToString());
					Log.Log("Snake point: " + FloorCoordinate(point));
					checkPoints.AddTask(SpeedLimitFactory.GetTask(point));
					point = point + line;
					Log.Log("Snake point: " + FloorCoordinate(point));
					checkPoints.AddTask(SpeedLimitFactory.GetTask(point));
					point = point + vecRight;
					line = Vector3D.Negate(line);
				}
				return checkPoints;
			}

			//создаем задачу выемки всех ресурсов
			private TaskSequence CreateMiningTask()
			{
				TaskSequence snakeMove = new TaskSequence();
				//для этого нам потребуется матрица ориентации корабля
				MatrixD shipMatrix = Ship.TravelSystem.GetMatrixTransformToGlobalCoordinate();
				//и матрица, которая будет смещать матрицу корабля на 1 шаг вниз
				Vector3D down = Vector3D.Multiply(Vector3D.Normalize(shipMatrix.Down), DeepStep);
				MatrixD moveDown = new MatrixD(
					1, 0, 0, 0,
					0, 1, 0, 0,
					0, 0, 1, 0,
					down.GetDim(0), down.GetDim(1), down.GetDim(2), 1);
				int layNumber = 1;
				//теперь создадим уровни
				for (double deep = 0.0; deep < MiningDeep; deep += DeepStep)
				{
					//создаем уровень
					TaskSequence task = CreateLayerMining(shipMatrix);
					if (layNumber++ % 2 == 0)//при необходимости изменяем порядок
						task.Reverse();
					//добавляем выемку слоя в общую задачу
					snakeMove.AddTask(task);
					//и сдвигаем матрицу вниз
					shipMatrix = MatrixD.Multiply(shipMatrix, moveDown);
					Log.Log("shipMatrix after multiply Matrix\n" + shipMatrix.ToString());
				}
				return snakeMove;
			}
		}

		//TODO Remove?
		class MoveOnDistanceInDirection : Task
		{
			FactoryMoveToPoint MoveFactory;
			Task MoveTask;
			Base6Directions.Direction Direction;
			double Distance;
			public MoveOnDistanceInDirection(double distance, Base6Directions.Direction direction, FactoryMoveToPoint moveFactory)
			{
				MoveFactory = moveFactory;
				MoveTask = null;
				Direction = direction;
				Distance = distance;
			}
			public bool Execute()
			{
				if (MoveTask == null)
				{
					Vector3D TargetPoint = Base6Directions.GetVector(Direction);
					TargetPoint = Vector3D.Multiply(TargetPoint, Distance);
					TargetPoint = Ship.TravelSystem.ToGlobalCoordinate(TargetPoint);
					MoveTask = MoveFactory.GetTask(TargetPoint);
				}
				return MoveTask.Execute();
			}
		}

		/*******************************************************************/
		/*******************************************************************/
		/*******************************************************************/
		/*******************************************************************/

		//Тот самый самый главный менеджер задач
		class EventManager
		{
			//Очередь задач
			private TaskSequence TaskQueue;

			private uint ticker;//счетчик, каждый раз, как Update получает управление, он увеличивается
			private uint UpdatePeriod;//определяет, во сколько раз реже, чем получение управления,
									  //будут выполняться текущие задачи
			private List<ConditionalTask> SmallTasksList;


			/// <summary>
			/// Показывает продолжительность тиков в мс
			/// </summary>
			//TODO оптимизировать и считать 1 раз, а не каждый тик
			public int TickPeriod { get; private set; }

			private DateTime valuePrew;

			public EventManager(uint updatePeriod)
			{
				ticker = updatePeriod;
				UpdatePeriod = updatePeriod;
				TaskQueue = new TaskSequence();
				TickPeriod = (int)(updatePeriod / 60.0 * 1000.0);//относително неплохая аппроксимация
				valuePrew = DateTime.UtcNow;
				SmallTasksList = new List<ConditionalTask>();
			}

			public void AddTask(Task task)
			{
				TaskQueue.AddTask(task);
			}

			public void Update()
			{
				//Log.Log($"tick:{ticker}", INIT_SYSTEM);
				ticker--;// = ++ticker % UpdatePeriod;
				if (ticker > 0)
					return;//не выйдет только если ticker == 0
				ticker = UpdatePeriod;

				DateTime valueNow = DateTime.UtcNow;

				TimeSpan span = valueNow - valuePrew;
				//TickPeriod = span.Milliseconds;
				TickPeriod = ((int)span.TotalMilliseconds + TickPeriod) / 2;

				//if (TickPeriod != 1)
				//	TickPeriod = (span.Milliseconds + TickPeriod) / 2;
				//else
				//	TickPeriod = span.Milliseconds;
				valuePrew = valueNow;
				ExecuteSmallTask();
				if (TaskQueue.Execute())
					Log.Log("Task compleated");
			}

			public void AddSmallTask(ConditionalTask task)
			{
				SmallTasksList.Add(task);
			}

			private void ExecuteSmallTask()
			{
				foreach (var task in SmallTasksList)
				{
					task.Execute();
				}
			}

			internal void Clear()
			{
				TaskQueue = new TaskSequence();
				SmallTasksList = new List<ConditionalTask>();
			}
		}

		static EventManager GlobalEventManeger;

		//FIXID внешне-локальные внутренне-корабельные координаты
		class SmartGyro
		{
			private IMyGyro m_Gyro;
			private string MapRotateByZ = "Roll";
			private string MapRotateByY = "Yaw";
			private string MapRotateByX = "Pitch";
			//private float MaxRotationValue;//TODO реализовать
			private readonly Matrix TransformToLocalMatrix;
			public SmartGyro(IMyGyro Gyro, IMyCubeBlock MainBlock)
			{
				m_Gyro = Gyro;
				if (m_Gyro == null)
					throw new Exception("Can't create SmartGyro: IMyGyro is null");
				Log.Log("Init gyro " + m_Gyro.CustomName, INIT_SYSTEM);
				Log.Log("gyro orientation " + m_Gyro.Orientation.ToString(), INIT_SYSTEM);
				Matrix matrix = new Matrix();
				MainBlock.Orientation.GetMatrix(out matrix);
				Log.Log("MainBlock Matrix " + matrix.ToString(), INIT_SYSTEM);
				TransformToLocalMatrix = new Matrix();
				m_Gyro.Orientation.GetMatrix(out TransformToLocalMatrix);
				Log.Log("Gyro Matrix " + TransformToLocalMatrix.ToString(), INIT_SYSTEM);
				TransformToLocalMatrix = Matrix.Invert(TransformToLocalMatrix);

				TransformToLocalMatrix = Matrix.Multiply(matrix, TransformToLocalMatrix);
				Log.Log("Final transform Matrix " + TransformToLocalMatrix.ToString(), INIT_SYSTEM);
				//MaxRotationValue = m_Gyro.GetMaximum<float>(RotateByZ);
			}

			public void SetPower(float power)
			{
				m_Gyro.GyroPower = power;
			}

			//Жестко задает вращения по осям. !!!никиких преобразований нет
			public void SetOverride(VRageMath.Vector3D settings)
			{
				//Vector3D vec = Vector3D.Transform(settings, TransformToLocalMatrix);

				Vector3D vec = settings;
				//Log.Log("Gyro name " + m_Gyro.CustomName);
				//Log.Log("Rotate input " + FloorCoordinate(settings));
				//Log.Log("Rotate apply " + FloorCoordinate(vec));
				if (!m_Gyro.GyroOverride)
					TerminalBlockExtentions.ApplyAction(m_Gyro, "Override");

				//TODO вращать только не 0-ые значения
				m_Gyro.SetValue(MapRotateByZ, (float)vec.Z);
				m_Gyro.SetValue(MapRotateByY, (float)vec.Y);
				m_Gyro.SetValue(MapRotateByX, (float)vec.X);

				//float x = m_Gyro.GetValue<float>(RotateByX);
				//float y = m_Gyro.GetValue<float>(RotateByY);
				//float z = m_Gyro.GetValue<float>(RotateByZ);

				//Log.Log("Gyro name: " + m_Gyro.CustomName);
				//Log.Log("RotateByX " + " real " + x.ToString() + " wait " + settings.X.ToString());
				//Log.Log("RotateByY " + " real " + y.ToString() + " wait " + settings.Y.ToString());
				//Log.Log("RotateByZ " + " real " + z.ToString() + " wait " + settings.Z.ToString());
			}

			public void DisableOverride()
			{
				m_Gyro.SetValue("Override", false);
			}

			public void TurnDirectionToPoint(Base6Directions.Direction directionInShipCoordinates, Vector3D pointInMainBlockCoordinates)
			{
				//Log.Log("Gyroscope name: " + m_Gyro.CustomName);
				//Log.Log("Input direction: " + directionInShipCoordinates.ToString());
				Vector3D pointInBlocCoordinates = Vector3D.Transform(pointInMainBlockCoordinates, TransformToLocalMatrix);
				//Log.Log("Ship target: " + FloorCoordinate(pointInMainBlockCoordinates));
				//Log.Log("Bloc target: " + FloorCoordinate(pointInBlocCoordinates));
				pointInBlocCoordinates = Vector3D.Normalize(pointInBlocCoordinates);
				Base6Directions.Direction directionInBlocCoordinates = m_Gyro.Orientation.TransformDirectionInverse(directionInShipCoordinates);
				//TODO разобраться, как работает этот костыль
				if (directionInBlocCoordinates == Base6Directions.Direction.Down)
				{
					directionInBlocCoordinates = Base6Directions.Direction.Up;
					pointInBlocCoordinates = Vector3D.Negate(pointInBlocCoordinates);
				}
				else if (directionInBlocCoordinates == Base6Directions.Direction.Backward)
				{
					directionInBlocCoordinates = Base6Directions.Direction.Forward;
					pointInBlocCoordinates = Vector3D.Negate(pointInBlocCoordinates);
				}
				else if (directionInBlocCoordinates == Base6Directions.Direction.Right)
				{
					pointInBlocCoordinates.Y = -pointInBlocCoordinates.Y;
				}
				else if (directionInBlocCoordinates == Base6Directions.Direction.Left)
				{
					directionInBlocCoordinates = Base6Directions.Direction.Right;
					pointInBlocCoordinates = Vector3D.Negate(pointInBlocCoordinates);
					pointInBlocCoordinates.Y = -pointInBlocCoordinates.Y;
				}

				//Log.Log("Used direction: " + directionInBlocCoordinates.ToString());
				Vector3D dir = Base6Directions.GetVector(directionInBlocCoordinates);
				Vector3D bias = pointInBlocCoordinates - dir;
				int unusedDimention = dir.AbsMaxComponent();
				if (bias.Length() > 1)//для увеличения скорости
				{
					bias.SetDim(unusedDimention, 0);
					bias = Vector3D.Normalize(bias);
				}
				bias.SetDim(unusedDimention, 0);
				//Log.Log("bias before swap " + FloorCoordinate(bias));
				Swap(ref bias, unusedDimention);
				//bias = Vector3D.Normalize(bias);
				//Log.Log("bias after swap " + FloorCoordinate(bias));
				//bias.X = 0;
				//bias.Y = 0;
				//bias.Z = 0;
				//SetOverride(pointLC);
				SetOverride(bias);
			}

			private static void Swap(ref Vector3D vec, int noSwappingIndex)
			{
				//TODO перебор ифами выглядит ущербно
				double val = 0;
				if (noSwappingIndex == 0)
				{
					val = vec.GetDim(1);
					vec.SetDim(1, vec.GetDim(2));
					vec.SetDim(2, val);
				}
				else if (noSwappingIndex == 1)
				{
					val = vec.GetDim(0);
					vec.SetDim(0, vec.GetDim(2));
					vec.SetDim(2, val);
				}
				else if (noSwappingIndex == 2)
				{
					val = vec.GetDim(0);
					vec.SetDim(0, vec.GetDim(1));
					vec.SetDim(1, val);
				}

			}
		}

		//FIXID внешне-локальные внутренне-корабельные координаты
		class MyGyros
		{
			private List<SmartGyro> SmartGyroscopes;
			private ShipSystems Ship;

			public MyGyros(ShipSystems owner, IMyGridTerminalSystem myGrid)
			{
				if (owner == null)
					throw new Exception("ShipController is null in MyGyros Constructor");
				Ship = owner;
				List<IMyTerminalBlock> Gyros = new List<IMyTerminalBlock>();
				myGrid.GetBlocksOfType<IMyGyro>(Gyros);
				if (Gyros.Count == 0)
					throw new Exception("We need at least one gyroscope");
				SmartGyroscopes = new List<SmartGyro>();
				foreach (var gyro in Gyros)
					SmartGyroscopes.Add(new SmartGyro(gyro as IMyGyro, owner.MainController));
			}

			private void ApplyTurnDirectionToAllGyroscopes(Base6Directions.Direction direction, Vector3D targetPointInShipCoordinate)
			{
				foreach (var gyro in SmartGyroscopes)
					gyro.TurnDirectionToPoint(direction, targetPointInShipCoordinate);
			}

			public void SetPower(float power)
			{
				foreach (var gyro in SmartGyroscopes)
					gyro.SetPower(power);
			}

			public void DisableOverride()
			{
				for (var iter = SmartGyroscopes.GetEnumerator(); iter.MoveNext();)
					iter.Current.DisableOverride();
			}

			//разворачивает в заданном направлении
			public void TurnDirectionToPoint(Base6Directions.Direction direction, Vector3D pointInGlobalCoordinates)
			{

				//переходим в координаты корабля
				Base6Directions.Direction directionInMainBlockCoordinates = Ship.MainController.Orientation.TransformDirection(direction);

				Vector3D pointInMainBlockCoordinates = Ship.TravelSystem.ToLocalCoordinate(pointInGlobalCoordinates);
				//Log.Log("TurnDirectionToPoint");
				//Log.Log("Input Direction " + direction.ToString());
				//Log.Log("Use Direction " + directionInMainBlockCoordinates.ToString());
				ApplyTurnDirectionToAllGyroscopes(directionInMainBlockCoordinates, pointInMainBlockCoordinates);

				Vector3D dir = Base6Directions.GetVector(directionInMainBlockCoordinates);
				Vector3D bias = dir - Vector3D.Normalize(pointInMainBlockCoordinates);
				bias.SetDim(dir.AbsMaxComponent(), 0);
				//Log.Log("Bias = " + FloorCoordinate(bias));
				//Override(Vector3.Normalize(bias));
				//return bias.Length() < GYRO_E;
			}
		}

		//FIXID внешне-локальные внутренне-корабельные координаты
		class MyTrusters
		{
			private Dictionary<Base6Directions.Direction, List<IMyThrust>> _thrusters;
			private ShipSystems _ship;
			public MyTrusters(ShipSystems ship, IMyGridTerminalSystem MyGrid)
			{
				_ship = ship;
				if (ship == null)
					throw new Exception("ShipController is null in MyTrusters Constructor");

				List<IMyTerminalBlock> Thrusters = new List<IMyTerminalBlock>();
				MyGrid.GetBlocksOfType<IMyThrust>(Thrusters);
				if (Thrusters.Count == 0)
					throw new Exception("We need at least one Thruster");
				Log.Log("Trusters count =" + Thrusters.Count.ToString(), INIT_SYSTEM);
				_thrusters = new Dictionary<Base6Directions.Direction, List<IMyThrust>>(Base6Directions.EnumDirections.Length);
				//создаем списки движков
				foreach (var i in Base6Directions.EnumDirections)
					_thrusters.Add(i, new List<IMyThrust>());
				//заполняем списки
				for (List<IMyTerminalBlock>.Enumerator iter = Thrusters.GetEnumerator(); iter.MoveNext();)
				{
					_thrusters[iter.Current.Orientation.Forward].Add((iter.Current) as IMyThrust);
					TerminalBlockExtentions.ApplyAction(iter.Current, "OnOff_On");
				}
				//Логгирования количества трастеров:
				foreach (var dir in Base6Directions.EnumDirections)
				{
					Base6Directions.Direction localDir = ship.MainController.Orientation.TransformDirection(dir);
					Log.Log("Thrusters count in direction: " + dir.ToString() + " " + _thrusters[localDir].Count.ToString(), INIT_SYSTEM);
					Log.Log("Power: " + GetMaxPowerInDirection(dir), INIT_SYSTEM);
				}
			}

			//Выдает скорость в заданном направлении
			//FIXID внешне-локальные внутренне-корабельные координаты (в изменении не нуждается)
			public double GetSpeedInDirection(VRageMath.Base6Directions.Direction direction)
			{
				Log.Log("GetSpeedInDirection ");
				Log.Log("Input dir " + direction.ToString());
				MyShipVelocities speed = Ship.MainController.GetShipVelocities();
				Log.Log("shipCoordinateSpeed Global\n" + FloorCoordinate(speed.LinearVelocity).ToString());
				VRageMath.Vector3D shipCoordinateSpeed = Ship.TravelSystem.ToLocalCoordinate(Ship.TravelSystem.GetPosition() + speed.LinearVelocity);
				VRageMath.Vector3D mask = Base6Directions.GetVector(direction);
				Log.Log("shipCoordinateSpeed\n" + FloorCoordinate(shipCoordinateSpeed).ToString());
				//Log.Log("mask\n" + FloorCoordinate(mask).ToString());
				return (shipCoordinateSpeed * mask).Sum;
			}

			//FIXID внешне-локальные внутренне-корабельные координаты
			public double GetMaxPowerInDirection(VRageMath.Base6Directions.Direction direction)
			{
				Base6Directions.Direction dir = _ship.MainController.Orientation.TransformDirection(direction);
				double power = 0;
				var thrusters = _thrusters[dir];
				foreach (var thruster in thrusters)
					power += thruster.MaxEffectiveThrust;
				return power;
			}

			//FIXID внешне-локальные внутренне-корабельные координаты (в изменении не нуждается)
			public void OverrideDirection(VRageMath.Base6Directions.Direction direction, bool EnableThrust = true)
			{
				if (EnableThrust)
					OverrideDirection(direction, 1);//TODO это магическое число
				else
					OverrideDirection(direction, 0);//TODO это магическое число
			}

			/// <summary>
			/// Задать силу тяги в долях от максимальной для заданного направления
			/// </summary>
			/// <param name="direction">целевое направление тяги двигателей</param>
			/// <param name="value">значение [0;1] задающее мощность двигателей</param>
			public void OverrideDirection(Base6Directions.Direction direction, float value)
			{
				if (value > 1.0)
					throw new Exception($"Out of range, need in [0,1] for {nameof(direction)}:{direction}");
				Base6Directions.Direction dir = _ship.MainController.Orientation.TransformDirection(direction);
				Log.Log($"MyTrusters.OverrideDirection({direction}, {value}) ", THRUSTER_DEBUG_LVL);
				Log.Log($"MyTrusters.OverrideDirection.dir{dir}", THRUSTER_DEBUG_LVL);
				List<IMyThrust> directionThrusters = _thrusters[dir];
				foreach (var thruster in directionThrusters)
				{
					//float maxThrust = thruster.MaxThrust;
					//Log.Log("Max thrust " + maxThrust.ToString("0.000"));
					//Log.Log("value " + value.ToString("0.000"));
					//Log.Log("set power " + (maxThrust * value).ToString("0.000"));
					thruster.SetValue("Override", value * 100);//задается в % TODO магическое число
															   //Log.Log("Apply - current " + thruster.CurrentThrust.ToString("0.000"));
				}
			}
			//задает движение в заданном направлении
			//тяга двигателей противоположного направления отключается
			//FIXID внешне-локальные внутренне-корабельные координаты (в изменении не нуждается)
			public void MoveInDirection(VRageMath.Base6Directions.Direction direction)
			{
				//получаем противоположное направление
				VRageMath.Base6Directions.Direction oppositiveDirection = VRageMath.Base6Directions.GetOppositeDirection(direction);
				OverrideDirection(oppositiveDirection, true);
				OverrideDirection(direction, false);
			}

			//задает тягу в заданном направлении
			//тяга двигателей противоположного направления отключается
			//power в диапазоне [0, 1]
			//FIXID внешне-локальные внутренне-корабельные координаты (в изменении не нуждается)
			public void MoveInDirection(VRageMath.Base6Directions.Direction direction, float power)
			{
				//получаем противоположное направление
				VRageMath.Base6Directions.Direction oppositiveDirection = VRageMath.Base6Directions.GetOppositeDirection(direction);
				OverrideDirection(oppositiveDirection, power);
				OverrideDirection(direction, false);
			}

			//Возвращает силу, с которой на данный момент работают двигатели
			//Сила в ньютонах(если верить Кинам)
			//FIXID внешне-локальные внутренне-корабельные координаты
			public double GetPowerForDirection(Base6Directions.Direction direction)
			{
				Base6Directions.Direction dir = _ship.MainController.Orientation.TransformDirection(direction);
				List<IMyThrust> directionThrusters = _thrusters[dir];
				double Sum = 0.0;
				foreach (var truster in directionThrusters)
					Sum += truster.CurrentThrust;
				return Sum;
			}
			/// <summary>
			/// Возвращает максимально возможную силу для заданного направления
			/// Это как GetMaxPowerInDirection, но для каждого направления в отдельности
			/// </summary>
			/// <param name="direction">Вектор, задающий направление тяги</param>
			/// <param name="accuracy">Точность сравнения с 0-ым значением</param>
			/// <returns></returns>
			public Vector3D GetMaxPower(Vector3D direction, double accuracy = ACCURACY_FORCE)
			{
				if (accuracy < 0.0) throw new Exception($"Need positive {nameof(accuracy)}:{accuracy}");
				Vector3D answer = new Vector3D(0);
				Vector3 x = new Vector3(direction.X, 0, 0);
				Vector3 y = new Vector3(0, direction.Y, 0);
				Vector3 z = new Vector3(0, 0, direction.Z);
				Log.Log($"MaxPowerForceCalculator.Calculate({direction})", THRUSTER_DEBUG_LVL);
				Log.Log($"MaxPowerForceCalculator.x:{x}", THRUSTER_DEBUG_LVL);
				Log.Log($"MaxPowerForceCalculator.y:{y}", THRUSTER_DEBUG_LVL);
				Log.Log($"MaxPowerForceCalculator.z:{z}", THRUSTER_DEBUG_LVL);
				return new Vector3D(
					Math.Abs(x.X) < accuracy ? 0.0 : Ship.MovementSystem.GetMaxPowerInDirection(Base6Directions.GetClosestDirection(x)),
					Math.Abs(y.Y) < accuracy ? 0.0 : Ship.MovementSystem.GetMaxPowerInDirection(Base6Directions.GetClosestDirection(y)),
					Math.Abs(z.Z) < accuracy ? 0.0 : Ship.MovementSystem.GetMaxPowerInDirection(Base6Directions.GetClosestDirection(z))
					);//TODO оптимизировать?
			}
			//Останавливает движение в любом направлении
			//FIXID внешне-локальные внутренне-корабельные координаты (в изменении не нуждается)
			public void Stop()
			{
				for (var i = Base6Directions.EnumDirections.GetEnumerator(); i.MoveNext();)
					OverrideDirection((Base6Directions.Direction)i.Current, false);
			}
		}
		//TODO
		class MyCargo
		{

		}
		//TODO
		class MyEnergySystem
		{
		}

		/// <summary>
		/// Класс для шаблонной компоновки ориентированных блоков
		/// </summary>
		/// <typeparam name="TBlockType"></typeparam>
		class OrientedBlocks<TBlockType>
			where TBlockType : class, IMyFunctionalBlock
		{
			private Dictionary<Base6Directions.Direction, List<TBlockType>> _blocks;
			public OrientedBlocks(ShipSystems ship, IMyGridTerminalSystem MyGrid)
			{
				Log.Log($"OrientedBlocks.{nameof(OrientedBlocks<TBlockType>)}({ship}, {MyGrid})", INIT_SYSTEM);
				if (ship == null)
					throw new Exception($"ShipController is null in {nameof(OrientedBlocks<TBlockType>)} Constructor");
				List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
				MyGrid.GetBlocksOfType<TBlockType>(blocks);
				if (blocks.Count == 0)
					throw new Exception($"We need at least one {nameof(TBlockType)}");
				Log.Log($"OrientedBlocks.{nameof(OrientedBlocks<TBlockType>)}.blocks.Count:{blocks.Count}", INIT_SYSTEM);
				//отлично, запилим словарик и рассортируем по направлениям все имеющиеся блоки
				_blocks = new Dictionary<Base6Directions.Direction, List<TBlockType>>(Base6Directions.EnumDirections.Length);
				//создаем списки блоков
				foreach (var i in Base6Directions.EnumDirections)
					_blocks.Add(i, new List<TBlockType>());
				//заполняем списки
				foreach (var block in blocks)
				{
					_blocks[block.Orientation.Forward].Add(block as TBlockType);
					TerminalBlockExtentions.ApplyAction(block, "OnOff_On");//на всякиц случай включим всё
				}
				//Логгирование количества блоков
				foreach (var i in Base6Directions.EnumDirections)
					Log.Log($"OrientedBlocks.{nameof(OrientedBlocks<TBlockType>)}._blocks[{i}].Count:{_blocks[i].Count}", INIT_SYSTEM);
				Log.Log($"OrientedBlocks.{nameof(OrientedBlocks<TBlockType>)}.End", INIT_SYSTEM);
			}

		}

		/// <summary>
		/// Класс для шаблонной компоновки однотипных блоков
		/// </summary>
		/// <typeparam name="TBlockType">тип блоков</typeparam>
		class Blocks<TBlockType>
			where TBlockType : class, IMyFunctionalBlock
		{
			private List<TBlockType> _blocks;
			public Blocks(ShipSystems ship, IMyGridTerminalSystem MyGrid)
			{
				Log.Log($"Blocks.{nameof(Blocks<TBlockType>)}({ship}, {MyGrid})", INIT_SYSTEM);
				if (ship == null)
					throw new Exception($"ShipController is null in {nameof(OrientedBlocks<TBlockType>)} Constructor");
				List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
				MyGrid.GetBlocksOfType<TBlockType>(blocks);
				if (blocks.Count == 0)
					throw new Exception($"We need at least one {nameof(TBlockType)}");
				Log.Log($"Blocks.{nameof(Blocks<TBlockType>)}.blocks.Count:{blocks.Count}", INIT_SYSTEM);
				//отлично, запилим словарик и рассортируем по направлениям все имеющиеся блоки
				_blocks = blocks.Select(block => blocks as TBlockType).ToList();
				Log.Log($"Blocks.{nameof(Blocks<TBlockType>)}.End", INIT_SYSTEM);
			}

		}

		class SpaceNavigateSystem
		{
			OrientedBlocks<IMySensorBlock> sensors;
			OrientedBlocks<IMyCameraBlock> camera;
			//TODO конструктор для сенсоров и камер
			//События по обнаружению объекта
			//Сопровождение объектов

		}

		class MyTravelSystem
		{
			private readonly IMyShipController ShipController;

			public readonly PotentialMethodMoveFactory DefaultTravelFactory;

			public MyTravelSystem(IMyShipController shipController,
				PotentialMethodMoveFactory travelFactory)
			{
				ShipController = shipController;
				DefaultTravelFactory = travelFactory;
			}
			/// <summary>
			/// Позиция основного блока в мировых координатах
			/// </summary>
			/// <returns> позиция корабля</returns>
			public Vector3D GetPosition()
			{
				return ShipController.CubeGrid.GridIntegerToWorld(ShipController.Position);
			}
			/// <summary>
			/// Для извлечения текущей скорости корабля
			/// </summary>
			/// <returns>Вектор скорости корабля в локальных координатах</returns>
			public Vector3D Speed
			{
				get
				{
					Log.Log($"MyTravelSystem.Speed", TRAVEL_SYSTEM_DEBUG_LVL);
					MyShipVelocities speed = Ship.MainController.GetShipVelocities();
					Log.Log($"globalCoordinateSpeed:{FloorCoordinate(speed.LinearVelocity)}", TRAVEL_SYSTEM_DEBUG_LVL);
					Vector3D shipCoordinateSpeed = Ship.TravelSystem.ToLocalCoordinate(Ship.TravelSystem.GetPosition() + speed.LinearVelocity);
					Log.Log($"localCoordinateSpeed:{FloorCoordinate(speed.LinearVelocity)}", TRAVEL_SYSTEM_DEBUG_LVL);
					Log.Log($"MyTravelSystem.End", TRAVEL_SYSTEM_DEBUG_LVL);
					return shipCoordinateSpeed;
				}
			}
			//Преобразует в киновские локальные координаты
			public Vector3D ToLocalCoordinate(Vector3D point)
			{
				var matrix = GetMatrixTransformToLocalCoordinate();
				return Vector3D.Transform(point, matrix);
			}
			//возвращает значение GetMatrixTransformToGlobalCoordinate * point
			//при этом ось X направлена вправо, а Z - назад
			//В соответствии с координатами Кинов
			public Vector3D ToGlobalCoordinate(Vector3D point)
			{
				var matrix = GetMatrixTransformToGlobalCoordinate();
				return Vector3D.Transform(point, matrix);
			}
			/// <summary>
			/// Дает матрицу преобразования в локальные координаты (по Кинам)
			/// </summary>
			/// <returns>Матрица для преобразования в локальные координаты</returns>
			public MatrixD GetMatrixTransformToLocalCoordinate()
			{
				return MatrixD.Invert(ShipController.WorldMatrix);
			}
			/// <summary>
			/// Дает матрицу преобразования в глобальные координаты (по Кинам)
			/// Преобразование в соответствии с поворотом корабля
			/// Позиция в соответствии с основным блоком
			/// </summary>
			/// <returns>матрица преобразования в глобальные координаты</returns>
			public MatrixD GetMatrixTransformToGlobalCoordinate()
			{
				//Matrix Rotation = new Matrix();
				//ShipController.Orientation.GetMatrix(out Rotation);
				//return MatrixD.Multiply(ShipController.WorldMatrix, Rotation);
				return ShipController.WorldMatrix;
			}
		}

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

		//все составляющие корабля должны работать в координатной сетке корабля, а не како-го то блока
		//при этом снаружи эта сетка должна выглядеть координатной сеткой, привязанной к основному блоку
		class ShipSystems
		{
			public readonly MyGyros OrientationSystem;
			public readonly MyTrusters MovementSystem;
			public MyTravelSystem TravelSystem;
			public MyCargo CargoSystem;
			public MyEnergySystem EnergySystem;
			public MyShipDrils Drils;
			//TODO реально это не система для стыковки
			public DockSystem DockSystem;
			public int Mass { get { return MainController.CalculateShipMass().TotalMass; } }
			public Vector3D Gravity { get { return TravelSystem.ToLocalCoordinate(TravelSystem.GetPosition() + MainController.GetTotalGravity()); } }
			public IMyShipController MainController { get; private set; }

			public ShipSystems(IMyGridTerminalSystem gridTerminalSystem, IMyShipController mainShipController)
			{
				Log.Log($"ShipSystems.ShipSystems({gridTerminalSystem},{mainShipController})", INIT_SYSTEM);
				MainController = mainShipController;
				Log.Log($"ShipSystems.ShipSystems.MainController.CustomName:{MainController.CustomName})", INIT_SYSTEM);
				Log.Log($"ShipSystems.ShipSystems.MainController.Orientation:\n{MainController.Orientation.ToString()})", INIT_SYSTEM);
				//Matrix M = new Matrix();
				//MainController.Orientation.GetMatrix(out M);
				//Log.Log($"Matrix {M.ToString()}", INIT_SYSTEM);
				OrientationSystem = new MyGyros(this, gridTerminalSystem);
				MovementSystem = new MyTrusters(this, gridTerminalSystem);
				CargoSystem = new MyCargo();
				EnergySystem = new MyEnergySystem();
				Drils = new MyShipDrils(gridTerminalSystem);
				ThrusterEnableRule rule = new InertialThrusterEnableRule(ACCURACY_POSITIONING, ACCURACY_SPEED);
				IFactoryPointDirectionBasedTask FactorySpeedLimit =
					new FactoryMoveInDirection(rule);
				//FactoryMoveToPoint travelFactory = new FactoryMoveToPoint(FactorySpeedLimit);
				var travelFactory = new PotentialMethodMoveFactory(DEATH_ZONE_FOR_ROTATION, SAFETY_FLY_HEIGHT);
				TravelSystem = new MyTravelSystem(MainController, travelFactory);

				List<IMyTerminalBlock> Connectors = new List<IMyTerminalBlock>();
				gridTerminalSystem.GetBlocksOfType<IMyShipConnector>(Connectors);
				if (Connectors.Count == 0)
					throw new Exception("We need at least one Connector");
				DockSystem = new DockSystem(MainController, Connectors[0] as IMyShipConnector, travelFactory);
				Log.Log($"ShipSystems.ShipSystems.End", INIT_SYSTEM);

			}

		}

		static ShipSystems Ship;

		class PotentialMethodMove : Task
		{
			internal class ForceK
			{
				public readonly IForceCalculator Force;
				public readonly double K;
				public ForceK(IForceCalculator force, double Coef)
				{
					Force = force;
					K = Coef;
				}
			}
			private readonly List<ForceK> forces;
			private readonly IPointProvider _point;
			private readonly double _accuracyPositioning_2;
			private readonly double _accuracySpeed_2;
			private readonly double _accuracyForce;
			private double _maxForce;

			//TODO использовать (ITargetPointProvider targetPoint, ...)
			public PotentialMethodMove(IPointProvider targetPoint, double accuracyPositioning, double accuracySpeed, double accuracyForce)
			{
				if (accuracyPositioning < 0.0) throw new Exception($"Out of range, need positive. {nameof(accuracyPositioning)}:{accuracyPositioning}");
				if (accuracySpeed < 0.0) throw new Exception($"Out of range, need positive. {nameof(accuracySpeed)}:{accuracySpeed}");
				if (accuracyForce < 0.0) throw new Exception($"Out of range, need positive. {nameof(accuracyForce)}:{accuracyForce}");
				_accuracySpeed_2 = accuracySpeed * accuracySpeed;
				_accuracyPositioning_2 = accuracyPositioning * accuracyPositioning;
				_point = targetPoint;
				_accuracyForce = accuracyForce;
				forces = new List<ForceK>();
				_maxForce = 0.0;
				foreach (var dir in Base6Directions.EnumDirections)
					_maxForce = Math.Max(Ship.MovementSystem.GetMaxPowerInDirection(dir), _maxForce);
			}

			public bool Execute()
			{
				Log.Log($"PotentialMethodMove.Execute.Execute()", POTENTIAL_METHOD_RESULTS_DEBUG_LVL);
				Log.Log($"PotentialMethodMove.Execute.forces.Count:{forces.Count}", POTENTIAL_METHOD_RESULTS_DEBUG_LVL);
				double speed = Ship.TravelSystem.Speed.LengthSquared();
				double leftDistance = _point.Now().LengthSquared();
				if (leftDistance < _accuracyPositioning_2 && speed < _accuracySpeed_2)
				{
					Ship.MovementSystem.Stop();
					Log.Log($"PotentialMethodMove.Execute.End", POTENTIAL_METHOD_RESULTS_DEBUG_LVL);
					return true;
				}
				Vector3D resultForce = new Vector3D(0.0);
				foreach (var force in forces)
				{
					Vector3 f = force.Force.Calculate() * force.K;
					Log.Log($"PotentialMethodMove.Execute.f({force.Force.GetType().Name}):{Vector3.Round(f, 2)}", POTENTIAL_METHOD_RESULTS_DEBUG_LVL);
					resultForce = Vector3D.Add(f, resultForce);
				}
				Log.Log($"PotentialMethodMove.Execute.resultForce:{Vector3.Round(resultForce, 2)}", POTENTIAL_METHOD_RESULTS_DEBUG_LVL);
				foreach (var dir in Base6Directions.EnumDirections)
				{
					double proj = (resultForce * Base6Directions.GetVector(dir)).Sum;
					var oppositiveDir = Base6Directions.GetOppositeDirection(dir);
					if (proj > _accuracyForce)
					{
						var maxPower = Ship.MovementSystem.GetMaxPowerInDirection(oppositiveDir);
						Ship.MovementSystem.OverrideDirection(
							oppositiveDir,
							(float)Math.Min(proj / maxPower, 1.0));
					}
					else
						Ship.MovementSystem.OverrideDirection(oppositiveDir, 0f);
				}
				Log.Log($"PotentialMethodMove.Execute.End", POTENTIAL_METHOD_RESULTS_DEBUG_LVL);
				return false;
			}

			public void AddForce(IForceCalculator force, double Coef)
			{
				if (force == null) throw new Exception($"{nameof(force)} is null");
				forces.Add(new ForceK(force, Coef));
			}
			/// <summary>
			/// Распределяет силу по двигателям с максимальной эффективностью.
			/// Как минимум 1 двигатель будет задействован на 100%, остальные - меньше 100%
			/// </summary>
			/// <param name="trustersPower">силы двигателей</param>
			/// <param name="force">вектор силы, которую нужно реализовать</param>
			/// <returns>Вектор, описывающий распределение силы по двигателям</returns>
			public static Vector3D DistributeForce(Vector3D trustersPower, Vector3D force)
			{
				//перейдем в положительную ось
				Vector3D p = trustersPower * Vector3.Sign(trustersPower);
				Vector3D f = force * Vector3.Sign(force);
				//и вычислим коэфициенты
				Vector3D kVec = p / f;
				//нам нужен минимальный
				double k = kVec.Min();
				//всё, теперь мы уложимся в допустимый диапазон
				return k * force;
			}
		}

		class PotentialMethodMoveFactory : IFactoryPointBasedTask, IFactoryPointProviderBasedTask
		{
			private readonly double _noRotationZone;
			private readonly double _accuracyPositioning;
			private readonly double _accuracySpeed;
			private readonly double _accuracyForce;
			private readonly double _safetyFlyHeight;

			public PotentialMethodMoveFactory(double noRotationZone = DEATH_ZONE_FOR_ROTATION,
				double safetyFlyHeight = SAFETY_FLY_HEIGHT,
				double accuracyPositioning = ACCURACY_POSITIONING,
				double accuracySpeed = ACCURACY_SPEED,
				double accuracyForce = ACCURACY_FORCE)
			{
				if (noRotationZone < 0.0) throw new Exception($"Argument out of range. {nameof(noRotationZone)}:{noRotationZone}, need positive");
				if (accuracyPositioning < 0.0) throw new Exception($"Argument out of range. {nameof(accuracyPositioning)}:{accuracyPositioning}, need positive");
				if (accuracySpeed < 0.0) throw new Exception($"Argument out of range. {nameof(accuracySpeed)}:{accuracySpeed}, need positive");
				if (accuracyForce < 0.0) throw new Exception($"Argument out of range. {nameof(accuracyForce)}:{accuracyForce}, need positive");
				_noRotationZone = noRotationZone;
				_accuracyPositioning = accuracyPositioning;
				_accuracySpeed = accuracySpeed;
				_accuracyForce = accuracyForce;
				_safetyFlyHeight = safetyFlyHeight;
			}
			public Task GetTask(Vector3D targetPoint, bool rotatate = false)
			{
				Log.Log($"PotentialMethodMoveFactory.GetTask({Vector3.Round(targetPoint, 0)}, {rotatate}", TRAVEL_SYSTEM_DEBUG_LVL);
				if (rotatate == false)
				{
					var targetProvider = new StaticPointProvider(targetPoint);
					var answer = GetTask(targetProvider);
					Log.Log($"PotentialMethodMoveFactory.GetTask.answer:{answer}", TRAVEL_SYSTEM_DEBUG_LVL);
					Log.Log($"PotentialMethodMoveFactory.GetTask.End", TRAVEL_SYSTEM_DEBUG_LVL);
					return answer;
				}
				Task rotateTask = new TurnDirectionToPoint(Base6Directions.Direction.Forward, targetPoint, _noRotationZone);
				OldComplexTask outerTask = new OldComplexTask(OldComplexTask.EndCondition.Last);
				outerTask.AddTask(rotateTask);
				outerTask.AddTask(GetTask(targetPoint, false));
				Log.Log($"PotentialMethodMoveFactory.GetTask.outerTask:{outerTask}", TRAVEL_SYSTEM_DEBUG_LVL);
				Log.Log($"PotentialMethodMoveFactory.GetTask.End", TRAVEL_SYSTEM_DEBUG_LVL);
				return outerTask;
			}

			public Task GetTask(IPointProvider target)
			{
				Log.Log($"PotentialMethodMoveFactory.GetTask({target})", TRAVEL_SYSTEM_DEBUG_LVL);
				var moveTask = new PotentialMethodMove(target, _accuracyPositioning, _accuracySpeed, _accuracyForce);
				moveTask.AddForce(new OrbitingResistForce(target), 2.0);//TODO use no default parameters
				moveTask.AddForce(new InertialForceCalculator(target), -2.0);// * ACCELERATION_K);//TODO magic numbers
				moveTask.AddForce(new GravityResistForce(target, _accuracyPositioning), 1.0);//TODO magic numbers
				moveTask.AddForce(new DangerousZoneForce(target, new NearestPlanetPointProvider(), _safetyFlyHeight, _accuracyPositioning), 2.0);
				moveTask.AddForce(new MaxPowerForceCalculator(target), 1.0);
				Log.Log($"PotentialMethodMoveFactory.GetTask.moveTask:{moveTask}", TRAVEL_SYSTEM_DEBUG_LVL);
				Log.Log($"PotentialMethodMoveFactory.GetTask.End", TRAVEL_SYSTEM_DEBUG_LVL);
				return AddHS(moveTask);//TODO стабилизация здесь - плохая идея
			}
		}

		/// <summary>
		///Класс, предоставляющий движение с ограничением скорости
		/// </summary>
		class SpeedLimitPotentialMethodFactory : IFactoryPointBasedTask
		{
			private readonly double _noRotationZone;
			private readonly double _accuracyPositioning;
			private readonly double _accuracySpeed;
			private readonly double _accuracyForce;
			private readonly double _safetyFlyHeight;
			private readonly double _maxSpeed;

			public SpeedLimitPotentialMethodFactory(
				double maxSpeed,
				double noRotationZone = DEATH_ZONE_FOR_ROTATION,
				double safetyFlyHeight = SAFETY_FLY_HEIGHT,
				double accuracyPositioning = ACCURACY_POSITIONING,
				double accuracySpeed = ACCURACY_SPEED,
				double accuracyForce = ACCURACY_FORCE)
			{
				if (maxSpeed < 0.0) throw new Exception($"Argument out of range. {nameof(maxSpeed)}:{maxSpeed}, need positive");
				if (noRotationZone < 0.0) throw new Exception($"Argument out of range. {nameof(noRotationZone)}:{noRotationZone}, need positive");
				if (accuracyPositioning < 0.0) throw new Exception($"Argument out of range. {nameof(accuracyPositioning)}:{accuracyPositioning}, need positive");
				if (accuracySpeed < 0.0) throw new Exception($"Argument out of range. {nameof(accuracySpeed)}:{accuracySpeed}, need positive");
				if (accuracyForce < 0.0) throw new Exception($"Argument out of range. {nameof(accuracyForce)}:{accuracyForce}, need positive");
				_noRotationZone = noRotationZone;
				_accuracyPositioning = accuracyPositioning;
				_accuracySpeed = accuracySpeed;
				_accuracyForce = accuracyForce;
				_safetyFlyHeight = safetyFlyHeight;
				_maxSpeed = maxSpeed;
			}
			public Task GetTask(Vector3D targetPoint, bool rotatate = false)
			{
				Log.Log($"SimplePotentialMethodFactory.GetTask({Vector3.Round(targetPoint, 0)}, {rotatate}", TRAVEL_SYSTEM_DEBUG_LVL);
				if (rotatate == false)
				{
					var targetProvider = new StaticPointProvider(targetPoint);
					var moveTask = new PotentialMethodMove(targetProvider, _accuracyPositioning, _accuracySpeed, _accuracyForce);
					moveTask.AddForce(new SpeedResistForce(_maxSpeed, 5), 1.0);//TODO magic numbers
					moveTask.AddForce(new GravityResistForce(targetProvider, _accuracyPositioning), 1.0);//TODO magic numbers
																										 //moveTask.AddForce(new OrbitingResistForce(targetProvider), 1.0);//TODO use no default parameters
					moveTask.AddForce(new InertialForceCalculator(targetProvider), -2.0 * ACCELERATION_K);//TODO magic numbers
					moveTask.AddForce(new MaxPowerForceCalculator(targetProvider), 1.0);
					Log.Log($"SimplePotentialMethodFactory.GetTask.moveTask:{moveTask}", TRAVEL_SYSTEM_DEBUG_LVL);
					Log.Log($"SimplePotentialMethodFactory.GetTask.End", TRAVEL_SYSTEM_DEBUG_LVL);
					return AddHS(moveTask);
				}
				Task rotateTask = new TurnDirectionToPoint(Base6Directions.Direction.Forward, targetPoint, _noRotationZone);
				OldComplexTask outerTask = new OldComplexTask(OldComplexTask.EndCondition.Last);
				outerTask.AddTask(rotateTask);
				outerTask.AddTask(GetTask(targetPoint, false));
				Log.Log($"SimplePotentialMethodFactory.GetTask.outerTask:{outerTask}", TRAVEL_SYSTEM_DEBUG_LVL);
				Log.Log($"SimplePotentialMethodFactory.GetTask.End", TRAVEL_SYSTEM_DEBUG_LVL);
				return outerTask;
			}
		}

		/// <summary>
		/// Интерфейс, который расчитывает некоторую силу, генерируемую заданной точкой
		/// Используется для расчетов в методе потенциалов
		/// </summary>
		interface IForceCalculator
		{
			Vector3D Calculate();
		}

		interface IPointProvider
		{
			/// <summary>
			/// Текущее положение точки
			/// </summary>
			/// <returns>Положение точки в локальных координатах</returns>
			Vector3D Now();
			/// <summary>
			/// Прогноз положения точки на указанное время
			/// </summary>
			/// <param name="seconds">количество секунд, на которые выполняется прогноз</param>
			/// <returns>предполагаемое положение точки</returns>
			Vector3D Prognosed(double seconds);
		}

		class StaticPointProvider : IPointProvider
		{
			private Vector3D _point;
			public StaticPointProvider(Vector3D pointInGlobalCoordinates)
			{
				_point = pointInGlobalCoordinates;
			}

			public Vector3D Now()
			{
				return Ship.TravelSystem.ToLocalCoordinate(_point);
			}

			//TODO test
			public Vector3D Prognosed(double seconds)
			{
				return Ship.TravelSystem.ToLocalCoordinate(_point) - Ship.TravelSystem.Speed * seconds;
			}
		}

		/// <summary>
		/// Предоставляет ближайшую точку планеты
		/// Насколько она ближайшая? Ну... насколько правильно работает метод 
		/// TryGetPlanetElevation(MyPlanetElevation.Surface, out height) у Keen's
		/// </summary>
		class NearestPlanetPointProvider : IPointProvider
		{
			public Vector3D Now()
			{
				Vector3D answer = new Vector3D();
				Vector3D planetCenter = new Vector3D();
				if (Ship.MainController.TryGetPlanetPosition(out planetCenter))//нууу, возможно при искуственной гравитации...
				{
					Log.Log($"NearestPlanetPoint.Now.planetCenter(Global):{Vector3.Round(planetCenter, 0)}", POTENTIAL_METHOD_POINT_PROVIDERS_LVL);
					planetCenter = Ship.TravelSystem.ToLocalCoordinate(planetCenter);
					Log.Log($"NearestPlanetPoint.Now.planetCenter(InLocal):{Vector3.Round(planetCenter, 0)}", POTENTIAL_METHOD_POINT_PROVIDERS_LVL);
					double height = 0;
					Ship.MainController.TryGetPlanetElevation(MyPlanetElevation.Surface, out height);//TODO ну здесь false уж точно не возможен?
					Log.Log($"NearestPlanetPoint.Now.{nameof(height)}:{height}", POTENTIAL_METHOD_POINT_PROVIDERS_LVL);
					answer = Vector3D.Normalize(planetCenter) * height;
				}
				//TODO в противном случае неплохо бы кидать исключение...
				Log.Log($"NearestPlanetPoint.Now.{nameof(answer)}:{Vector3.Round(answer, 2)}", POTENTIAL_METHOD_POINT_PROVIDERS_LVL);
				return answer;
			}

			public Vector3D Prognosed(double seconds)
			{
				Log.Log($"NearestPlanetPoint.Prognosed({seconds})", POTENTIAL_METHOD_POINT_PROVIDERS_LVL);
				var answer = Now();
				Log.Log($"NearestPlanetPoint.Prognosed.End)", POTENTIAL_METHOD_POINT_PROVIDERS_LVL);
				return answer;
			}
		}

		/// <summary>
		/// Предоставляет смещение, такое, как если бы основным блоком являлся другой блок
		/// Но при этом не делает вращения (это может привести к неприятным последствиям)
		/// </summary>
		class DistanceFromBlockPointProvider : IPointProvider
		{
			private readonly IMyCubeBlock _block;
			private readonly IPointProvider _pointProvider;

			public DistanceFromBlockPointProvider(IMyCubeBlock block, IPointProvider pointProvider)
			{
				if (block == null) throw new Exception($"Argument null exception. Argname:{nameof(block)}");
				if (pointProvider == null) throw new Exception($"Argument null exception. Argname:{nameof(pointProvider)}");
				_pointProvider = pointProvider;
				_block = block;
			}

			public Vector3D Now()
			{
				return _pointProvider.Now() - LocalBlocCoordinates(_block);
			}

			public Vector3D Prognosed(double seconds)
			{
				return _pointProvider.Prognosed(seconds) - LocalBlocCoordinates(_block);
			}

			public static Vector3D LocalBlocCoordinates(IMyCubeBlock block)
			{
				return Ship.TravelSystem.ToLocalCoordinate(block.CubeGrid.GridIntegerToWorld(block.Position));
			}
		}

		/// <summary>
		/// Расчет силы инерции.
		/// В данном контексте - избыточная сила, которая не позволит остановиться в целевой точке
		/// </summary>
		class InertialForceCalculator : IForceCalculator
		{
			private readonly double _accuracySpeed;
			private readonly double _accuracyPositioning;
			private readonly IPointProvider _point;
			/// <summary>
			/// Создает обьект расчета силы для двигателей для заданного направления
			/// </summary>
			/// <param name="accuracySpeed">Точность вычисления скорости</param>
			public InertialForceCalculator(IPointProvider p, double accuracySpeed = ACCURACY_SPEED, double accuracyPositioning = ACCURACY_POSITIONING)
			{
				if (accuracySpeed < 0.0) throw new Exception($"Out of range, need positive. {nameof(accuracySpeed)}:{accuracySpeed}");
				if (accuracyPositioning < 0.0) throw new Exception($"Out of range, need positive. {nameof(accuracyPositioning)}:{accuracyPositioning}");
				if (p == null) throw new Exception($"Argument null{nameof(p)}");
				_point = p;
				_accuracySpeed = accuracySpeed;
				_accuracyPositioning = accuracyPositioning;
			}
			public Vector3D Calculate()
			{
				return InertialForceCalculate(_point.Now(), 0.0, _accuracySpeed, _accuracyPositioning);
			}

			/// <summary>
			/// Вычисляет инерционную силу при подлете к точке с учетом мертвой зоны
			/// </summary>
			/// <param name="point">целевая точка в локальных коодинатах</param>
			/// <param name="deadZone">Радиус мертвой зоны. В ней сила == 0</param>
			/// <param name="accuracySpeed">Точность при сравнении скорости</param>
			/// <param name="accuracyPositioning">Точность при сравнении позиций</param>
			/// <returns></returns>
			public static Vector3D InertialForceCalculate(Vector3D point, double deadZone, double accuracySpeed = ACCURACY_SPEED, double accuracyPositioning = ACCURACY_POSITIONING)
			{
				if (deadZone < 0.0) throw new Exception($"Argument out of range. {nameof(deadZone)}:{deadZone}. wait positive");
				if (accuracySpeed < 0.0) throw new Exception($"Argument out of range. {nameof(accuracySpeed)}:{accuracySpeed}. wait positive");
				if (accuracyPositioning < 0.0) throw new Exception($"Argument out of range. {nameof(accuracyPositioning)}:{accuracyPositioning}. wait positive");
				Log.Log($"InertialForceCalculator.InertialForceCalculate({point}, {deadZone})",
					POTENTIAL_METHOD_DEBUG_LVL);
				double s = point.Length() - deadZone;
				Log.Log($"InertialForceCalculator.InertialForceCalculate.s:{s}", POTENTIAL_METHOD_DEBUG_LVL);
				//if (s < accuracyPositioning)
				//{
				//	Log.Log($"InertialForceCalculator.InertialForceCalculate.return:{new Vector3D(0)}", POTENTIAL_METHOD_DEBUG_LVL);
				//	Log.Log($"InertialForceCalculator.InertialForceCalculate.End", POTENTIAL_METHOD_DEBUG_LVL);
				//	return new Vector3D(0);//если мы попали в мертвую зону, то ничего не делать
				//}
				Vector3D p = Vector3D.Normalize(point) * s;
				Log.Log($"InertialForceCalculator.InertialForceCalculate.p:{Vector3.Round(p, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				double m = Ship.Mass;
				Log.Log($"InertialForceCalculator.InertialForceCalculate.m:{m}", POTENTIAL_METHOD_DEBUG_LVL);
				double v = Vector3D.Dot(Ship.TravelSystem.Speed, p) / s;
				Log.Log($"InertialForceCalculator.InertialForceCalculate.v:{v}", POTENTIAL_METHOD_DEBUG_LVL);
				if (v < accuracySpeed)
				{
					Log.Log($"InertialForceCalculator.InertialForceCalculate.return:{new Vector3D(0)}", POTENTIAL_METHOD_DEBUG_LVL);
					Log.Log($"InertialForceCalculator.InertialForceCalculate.End", POTENTIAL_METHOD_DEBUG_LVL);
					return new Vector3D(0);
				}
				Vector3D vp = Vector3D.Normalize(p) * v;
				Log.Log($"InertialForceCalculator.InertialForceCalculate.vp:{vp}", POTENTIAL_METHOD_DEBUG_LVL);
				double Enow = m * v * v / 2;
				Log.Log($"InertialForceCalculator.InertialForceCalculate.Enow:{Enow}", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D maxResistForce = PotentialMethodMove.DistributeForce(Ship.MovementSystem.GetMaxPower(-vp, 0), vp);
				Log.Log($"InertialForceCalculator.InertialForceCalculate.{nameof(maxResistForce)}:{Vector3.Round(maxResistForce, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D maxAccelerationForce = PotentialMethodMove.DistributeForce(Ship.MovementSystem.GetMaxPower(vp, 0), vp);
				Log.Log($"InertialForceCalculator.InertialForceCalculate.{nameof(maxAccelerationForce)}:{Vector3.Round(maxAccelerationForce, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				double t = (double)GlobalEventManeger.TickPeriod / 1000.0;
				Log.Log($"InertialForceCalculator.InertialForceCalculate.t:{t}", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D ds = vp * t;
				Log.Log($"InertialForceCalculator.InertialForceCalculate.ds:{Vector3.Round(ds, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				double Eprognosed = Enow + Vector3D.Dot(ds, maxAccelerationForce);
				Log.Log($"InertialForceCalculator.InertialForceCalculate.Eprognosed:{Eprognosed}", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D s_prognosed = p - ds;
				Log.Log($"InertialForceCalculator.InertialForceCalculate.s_prognosed:{Vector3.Round(s_prognosed, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				double MaxPossibleWork = (s_prognosed * maxResistForce).Sum;
				Log.Log($"InertialForceCalculator.InertialForceCalculate.MaxPossibleWork:{MaxPossibleWork}", POTENTIAL_METHOD_DEBUG_LVL);
				double SafeE = 0.5 * m * accuracySpeed * accuracySpeed;//энергия, которую допустимо оставить
				Log.Log($"InertialForceCalculator.InertialForceCalculate.MaxPossibleWork:{MaxPossibleWork}", POTENTIAL_METHOD_DEBUG_LVL);
				double dE = Eprognosed - Math.Max(MaxPossibleWork - SafeE, 0);
				Log.Log($"InertialForceCalculator.InertialForceCalculate.dE:{dE}", POTENTIAL_METHOD_DEBUG_LVL);
				double force = Math.Max(dE / ds.Length(), 0);
				Log.Log($"InertialForceCalculator.InertialForceCalculate.force:{force}", POTENTIAL_METHOD_DEBUG_LVL);
				var answer = force * Vector3D.Normalize(vp);
				Log.Log($"InertialForceCalculator.InertialForceCalculate.{nameof(answer)}:{Vector3.Round(answer, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Log.Log($"InertialForceCalculator.InertialForceCalculate.End", POTENTIAL_METHOD_DEBUG_LVL);
				return answer;
			}
		}

		/// <summary>
		/// Предоставляет максимальную силу двигателей в направлении прогноза точки точки
		/// </summary>
		class MaxPowerForceCalculator : IForceCalculator
		{
			IPointProvider _point;
			public MaxPowerForceCalculator(IPointProvider p)
			{
				if (p == null) throw new Exception($"Argument null{nameof(p)}");
				_point = p;
			}
			public Vector3D Calculate()
			{
				Vector3D p = _point.Prognosed(GlobalEventManeger.TickPeriod / 1000.0);
				//Vector3D p = _point.Now();
				return CalculateMaxPowerToPoint(p);
			}

			/// <summary>
			/// Предоставляет расчет силы по направлению к целевой точке
			/// </summary>
			/// <param name="point">цель</param>
			/// <returns>максимально возможная сила, с учетом возможностей двигателей</returns>
			public static Vector3D CalculateMaxPowerToPoint(Vector3D point)
			{
				Log.Log($"MaxPowerForceCalculator.CalculatePower({point})", POTENTIAL_METHOD_DEBUG_LVL);
				//Vector3D p = Vector3D.Normalize(pointInLocalCoordinates);
				//вычислим базовую силу
				Vector3D basePower = Ship.MovementSystem.GetMaxPower(-point, 0.0);
				Log.Log($"MaxPowerForceCalculator.CalculatePower.basePower:{Vector3.Round(basePower, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				//а теперь распределим её в соответствии с направлением к точке
				Vector3D scaledPower = PotentialMethodMove.DistributeForce(basePower, point);
				Log.Log($"MaxPowerForceCalculator.CalculatePower.scaledPower:{Vector3.Round(scaledPower, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Log.Log($"MaxPowerForceCalculator.CalculatePower.End", POTENTIAL_METHOD_DEBUG_LVL);
				return scaledPower;
			}
		}

		//TODO test
		class GravityResistForce : IForceCalculator
		{
			private readonly double _accuracyPositioning;
			private readonly double _gravityK;
			private readonly IPointProvider _targetPoint;
			public GravityResistForce(IPointProvider targetPoint, double accuracyPositioning = ACCURACY_POSITIONING, double gravityK = 1.0)
			{
				if (targetPoint == null) throw new Exception($"argument {nameof(targetPoint)} null exception.");
				if (accuracyPositioning < 0) throw new Exception($"argument out of range. {nameof(accuracyPositioning)}:{accuracyPositioning}. need positive");
				Log.Log($"GravityResistForce.GravityResistForce({targetPoint}, {accuracyPositioning}, {gravityK})", POTENTIAL_METHOD_DEBUG_LVL);
				_targetPoint = targetPoint;
				_accuracyPositioning = accuracyPositioning;
				_gravityK = gravityK;
				Log.Log($"GravityResistForce.GravityResistForce.End", POTENTIAL_METHOD_DEBUG_LVL);
			}

			public Vector3D Calculate()
			{
				Log.Log($"GravityResistForce.Calculate()", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D g = Ship.Gravity;
				Log.Log($"GravityResistForce.Calculate.g(local):{Vector3.Round(g, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				if (!g.IsValid() || g.LengthSquared() < ACCURACY_SPEED)//TODO magick number. Вынести в параметры?
				{
					Log.Log($"GravityResistForce.Calculate.return:{new Vector3D(0)}", POTENTIAL_METHOD_DEBUG_LVL);
					Log.Log($"GravityResistForce.Calculate.End", POTENTIAL_METHOD_DEBUG_LVL);
					return new Vector3D(0);
				}
				double m = Ship.Mass;
				Log.Log($"GravityResistForce.Calculate.m:{m}", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D gravityResist = Vector3D.Negate(m * g) * _gravityK;
				Log.Log($"GravityResistForce.Calculate.{nameof(gravityResist)}:{Vector3.Round(gravityResist, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D answer = new Vector3D(gravityResist);
				Log.Log($"GravityResistForce.Calculate.{nameof(answer)}:{Vector3.Round(answer, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Log.Log($"GravityResistForce.Calculate.End", POTENTIAL_METHOD_DEBUG_LVL);
				return answer;
			}
		}

		/// <summary>
		/// Расчет силы сопротивления движению - для решения ситуации "выхода на орбиту"
		/// Внутри - расчет части силы поля притежения, которая обеспечивает центростремительную состовляющую
		/// Далее - вычисление точки цетра вращения
		/// Если при такой же центростремительной силе мы окажемся перед целевой точкой - всё ОК
		/// Если позади неё - гасим перпендикулярную часть скорости
		/// </summary>
		class OrbitingResistForce : IForceCalculator
		{
			private readonly IPointProvider _point;
			private readonly double _orbitingResistK;
			private readonly double _speedAccuracy;
			private readonly double _positioningAccuracy;

			/// <summary>
			/// TODO довести до ума сейчас работает так себе
			/// Создание экземляра сопротивления круговому движению
			/// </summary>
			/// <param name="p">Точка, в которую надо прилететь</param>
			/// <param name="orbitingResistK">Определяет запас расстояния по "промаху" при движении к точке. Значения - от 0.0, больше 1.0 не рекомендуется</param>
			/// <param name="speedAccuracy">Точность определения скорости. Если перпендикулярная части скороти меньше заданной точности, сила будет 0<param>
			public OrbitingResistForce(IPointProvider p, double orbitingResistK = ORBITING_RESIST_K, double speedAccuracy = ACCURACY_SPEED, double positioningAccuracy = ACCURACY_POSITIONING)
			{
				if (p == null) throw new Exception($"Argument null {nameof(p)}");
				if (orbitingResistK < 0.0) throw new Exception($"Argument out of range.{nameof(orbitingResistK)}:{orbitingResistK}, wait positive");
				if (speedAccuracy < 0.0) throw new Exception($"Argument out of rang.{nameof(speedAccuracy)}:{speedAccuracy}, wait positive");
				if (positioningAccuracy < 0.0) throw new Exception($"Argument out of rang.{nameof(positioningAccuracy)}:{positioningAccuracy}, wait positive");
				_point = p;
				_orbitingResistK = orbitingResistK;
				_speedAccuracy = speedAccuracy;
				_positioningAccuracy = positioningAccuracy;
			}

			public Vector3D Calculate1()
			{
				Log.Log($"OrbitingResistForce.Calculate()", POTENTIAL_METHOD_DEBUG_LVL);
				var answer = new Vector3D(0.0);
				var p = _point.Now();
				Log.Log($"OrbitingResistForce.Calculate.p:{Vector3.Round(p, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D v = Ship.TravelSystem.Speed;
				Log.Log($"OrbitingResistForce.Calculate.{nameof(v)}:{Vector3.Round(v, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				var pn = Vector3D.Normalize(p);
				Log.Log($"OrbitingResistForce.Calculate.{nameof(pn)}:{Vector3.Round(pn, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D perpendicularV = v - Vector3D.Dot(v, pn) * pn;
				Log.Log($"OrbitingResistForce.Calculate.{nameof(perpendicularV)}:{Vector3.Round(perpendicularV, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				if (v.Length() > _speedAccuracy && perpendicularV.Length() > _speedAccuracy && perpendicularV.Length() / v.Length() > 0.5)
					answer = MaxPowerForceCalculator.CalculateMaxPowerToPoint(-perpendicularV);
				Log.Log($"OrbitingResistForce.Calculate.{nameof(answer)}:{Vector3.Round(answer, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Log.Log($"OrbitingResistForce.Calculate.End", POTENTIAL_METHOD_DEBUG_LVL);
				return answer;
			}

			//true православный вариант, но есть баги
			public Vector3D Calculate()
			{
				Log.Log($"OrbitingResistForce.Calculate()", POTENTIAL_METHOD_DEBUG_LVL);
				var answer = new Vector3D(0.0);
				var p = _point.Now();
				Log.Log($"OrbitingResistForce.Calculate.p:{Vector3.Round(p, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D vVector = Ship.TravelSystem.Speed;
				Log.Log($"OrbitingResistForce.Calculate.{nameof(vVector)}:{Vector3.Round(vVector, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				double m = Ship.Mass;
				Log.Log($"OrbitingResistForce.Calculate.{nameof(m)}:{m.ToString("0.0")}", POTENTIAL_METHOD_DEBUG_LVL);
				double v = vVector.Length();
				Log.Log($"OrbitingResistForce.Calculate.v:{v.ToString("0.00")}", POTENTIAL_METHOD_DEBUG_LVL);
				var Ft = MaxPowerForceCalculator.CalculateMaxPowerToPoint(p);
				Log.Log($"OrbitingResistForce.Calculate.{nameof(Ft)}:{Vector3.Round(Ft, 2)}", POTENTIAL_METHOD_DEBUG_LVL);

				var vn = Vector3D.Normalize(vVector);
				Log.Log($"OrbitingResistForce.Calculate.{nameof(vn)}:{Vector3.Round(vn, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D Fc = Ft - Vector3D.Dot(Ft, vn) * vn;
				//var Fc = Vector3D.Reject(Ft, vVector);
				Log.Log($"OrbitingResistForce.Calculate.{nameof(Fc)}:{Vector3.Round(Fc, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				var r = m * v * v / Fc.Length();
				Log.Log($"OrbitingResistForce.Calculate.{nameof(r)}:{r.ToString("0.0")}", POTENTIAL_METHOD_DEBUG_LVL);
				if (r > _positioningAccuracy)
				{
					var O = Vector3D.Normalize(Fc) * r;
					Log.Log($"OrbitingResistForce.Calculate.{nameof(O)}:{Vector3.Round(O, 1)}", POTENTIAL_METHOD_DEBUG_LVL);
					var OT = _orbitingResistK * (p - O).Length();
					Log.Log($"OrbitingResistForce.Calculate.{nameof(OT)}:{OT.ToString("0.00")}", POTENTIAL_METHOD_DEBUG_LVL);
					var perpendicularV = Vector3.Reject(vVector, p);
					if (perpendicularV.LengthSquared() > _speedAccuracy)
						if (OT + _positioningAccuracy < r)
						{
							var t = GlobalEventManeger.TickPeriod / 1000d;
							Log.Log($"OrbitingResistForce.Calculate.{nameof(t)}:{t.ToString("0.000")}", POTENTIAL_METHOD_DEBUG_LVL);
							var k = m * perpendicularV.Length() / t * ACCELERATION_K;
							Log.Log($"OrbitingResistForce.Calculate.{nameof(k)}:{k.ToString("0.00")}", POTENTIAL_METHOD_DEBUG_LVL);
							answer = k * MaxPowerForceCalculator.CalculateMaxPowerToPoint(-perpendicularV);
						}
					//else if (OT - _positioningAccuracy < r)
					//	answer = MaxPowerForceCalculator.CalculateMaxPowerToPoint(O);
				}
				Log.Log($"OrbitingResistForce.Calculate.{nameof(answer)}:{Vector3.Round(answer, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Log.Log($"OrbitingResistForce.Calculate.End", POTENTIAL_METHOD_DEBUG_LVL);
				return answer;
			}
		}

		class TunnelPointProvider : IPointProvider
		{
			private readonly IPointProvider _end;
			private readonly IPointProvider _begin;
			public TunnelPointProvider(IPointProvider begin, IPointProvider end)
			{
				if (begin == null) throw new Exception($"Argument null exception: {nameof(begin)}");
				if (end == null) throw new Exception($"Argument null exception: {nameof(end)}");
				_begin = begin;
				_end = end;
			}

			public Vector3D Now()
			{
				var a = _begin.Now();
				var b = _end.Now();
				//Point2d a, b;      //наша прямая
				var x = new Vector3D(0);        //точка которая проецируется
				var v = b - a; //сдвинем точку "a" в начало координат
				var vn = Vector3D.Normalize(v);     //нормализуем вектор
				var vx = x;// x - Point2d(0, 0);   //превратим точку в вектор с началом в начале координат
				var f = Vector3D.Dot(vn, vx); //скалярное произведение, порядок не важен
				var xp = a + vn * f;            //спроецированная точка
				return xp;
			}

			//TODO реализовать
			public Vector3D Prognosed(double seconds)
			{
				return Now();
			}
		}

		/// <summary>
		/// Сопротивление движению. Для ограничения максимальной скорости или "лишних" движений
		/// </summary>
		class SpeedResistForce : IForceCalculator
		{
			private readonly double _maxSpeed;
			private readonly double _pow;

			public SpeedResistForce(double maxSpeed, int pow)
			{
				if (pow <= 0) throw new Exception($"Argument out of range.{nameof(pow)}:{pow}, need positive");
				if (maxSpeed < 0.0) throw new Exception($"Argument out of range.{nameof(maxSpeed)}:{maxSpeed}, need positive");
				_maxSpeed = maxSpeed;
				_pow = pow;
			}

			public Vector3D Calculate()
			{
				Log.Log($"SpeedResistForce.Calculate()", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D v = Ship.TravelSystem.Speed;
				Log.Log($"SpeedResistForce.Calculate.{nameof(v)}:{Vector3.Round(v, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D nv = v / _maxSpeed * Vector3D.Sign(v);
				Log.Log($"SpeedResistForce.Calculate.{nameof(nv)}:{Vector3.Round(nv, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D answer = new Vector3D(1.0);
				Log.Log($"SpeedResistForce.Calculate.{nameof(answer)}(sign OR (1)):{Vector3.Round(answer, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				for (int i = 0; i < _pow; ++i)
					answer *= nv;
				Log.Log($"SpeedResistForce.Calculate.{nameof(answer)}(after pow):{Vector3.Round(answer, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				answer *= MaxPowerForceCalculator.CalculateMaxPowerToPoint(-v);
				Log.Log($"SpeedResistForce.Calculate.{nameof(answer)}:{Vector3.Round(answer, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Log.Log($"SpeedResistForce.Calculate.End", POTENTIAL_METHOD_DEBUG_LVL);
				return answer;
			}
		}
		/// <summary>
		/// Класс, описывающий опасную зону
		/// По умолчанию стремится не попадать туда
		/// Если цель находится внутри зоны, то движение допустимо с точки, из которой до цели минимальное расстояние
		/// </summary>
		class DangerousZoneForce : IForceCalculator
		{
			private readonly double _minDistance;
			private readonly double _accuracyPositioning;
			private readonly IPointProvider _targetPoint;
			private readonly IPointProvider _zoneCenter;
			public DangerousZoneForce(IPointProvider targetPoint, IPointProvider zoneCenter, double minDistance, double accuracyPositioning = ACCURACY_POSITIONING * 4.0)
			{
				if (targetPoint == null) throw new Exception($"argument {nameof(targetPoint)} null exception.");
				if (zoneCenter == null) throw new Exception($"argument {nameof(zoneCenter)} null exception.");
				if (accuracyPositioning < 0) throw new Exception($"argument out of range. {nameof(accuracyPositioning)}:{accuracyPositioning}. need positive");
				Log.Log($"DangerousZoneForce.DangerousZoneForce({targetPoint}, {zoneCenter}, {minDistance}, {accuracyPositioning})", POTENTIAL_METHOD_DEBUG_LVL);
				_targetPoint = targetPoint;
				_zoneCenter = zoneCenter;
				_minDistance = minDistance;
				_accuracyPositioning = accuracyPositioning;
				Log.Log($"DangerousZoneForce.DangerousZoneForce.End", POTENTIAL_METHOD_DEBUG_LVL);
			}
			public Vector3D Calculate()
			{

				Log.Log($"DangerousZoneForce.Calculate()", POTENTIAL_METHOD_DEBUG_LVL);
				var answer = CalculateDangerousZoneForce(_targetPoint.Now(), _zoneCenter.Now(), _minDistance, _accuracyPositioning);
				Log.Log($"DangerousZoneForce.Calculate.{nameof(answer)}:{Vector3.Round(answer, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Log.Log($"DangerousZoneForce.Calculate.End", POTENTIAL_METHOD_DEBUG_LVL);
				return answer;
			}
			/// <summary>
			/// Вычисление влияния этого поля
			/// </summary>
			/// <param name="targetPoint">Точка, куда лежит путь(в локальных координатах)</param>
			/// <param name="zoneCenter">Центр опасной зоны(в локальных координатах</param>
			/// <param name="minDistance">Радиус опасной зоны</param>
			/// <param name="freeTunnelRadius">Точность позиционирования</param>
			/// <returns>Сила, создаваемая этим полем в текущей позиции корабля</returns>
			public static Vector3D CalculateDangerousZoneForce(Vector3D targetPoint, Vector3D zoneCenter, double minDistance, double freeTunnelRadius = ACCURACY_POSITIONING)
			{
				if (minDistance < 0.0)
					throw new Exception($"Argument out of range. {nameof(minDistance)}:{minDistance}. Need Positive");
				Log.Log($"DangerousZoneForce.CalculateDangerousZoneForce({Vector3.Round(targetPoint, 2)}, {Vector3.Round(zoneCenter, 2)}, {minDistance}, {freeTunnelRadius})", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D answer = new Vector3D(0);
				//добавим сопротивление, чтобы в зону случайно не залететь
				answer = Vector3D.Add(answer,
					-2.0 * ACCELERATION_K *
					InertialForceCalculator.InertialForceCalculate(
						zoneCenter, minDistance));
				Log.Log($"DangerousZoneForce.CalculateDangerousZoneForce.{nameof(answer)}(InertialResist):{Vector3.Round(answer, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				double l = targetPoint.Length();//сколько вообще надо пролететь
				Log.Log($"DangerousZoneForce.CalculateDangerousZoneForce.l:{l}", POTENTIAL_METHOD_DEBUG_LVL);
				double lp = (targetPoint * zoneCenter).Sum / zoneCenter.Length();//сколько надо пролететь в опасной зоне
				Log.Log($"DangerousZoneForce.CalculateDangerousZoneForce.lp:{lp}", POTENTIAL_METHOD_DEBUG_LVL);
				double toCenter = zoneCenter.Length();
				Log.Log($"DangerousZoneForce.CalculateDangerousZoneForce.{nameof(toCenter)}:{toCenter}", POTENTIAL_METHOD_DEBUG_LVL);
				if (toCenter < minDistance && (Math.Sqrt(l * l - lp * lp) > freeTunnelRadius))// && (zoneCenter - targetPoint).Length() < minDistance)//TODO из-за этого условия возможны "туннели" к цели вне поля
					answer = Vector3D.Add(answer,
						Vector3D.Multiply(MaxPowerForceCalculator.CalculateMaxPowerToPoint(zoneCenter), -2.0));
				Log.Log($"DangerousZoneForce.CalculateDangerousZoneForce.{nameof(answer)}:{Vector3.Round(answer, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Log.Log($"DangerousZoneForce.CalculateDangerousZoneForce.End", POTENTIAL_METHOD_DEBUG_LVL);
				return answer;
			}
		}

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
				Log.Error(e.ToString());
				GlobalEventManeger.Clear();
				GlobalEventManeger.AddTask(new StopTask());
			}
		}
	}

}