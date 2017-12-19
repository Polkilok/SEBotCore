using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
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
	}

}