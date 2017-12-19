using System;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
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
	}

}