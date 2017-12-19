using System;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
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
	}

}