using System;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		//TODO inspect
		/// <summary>
		/// Класс, описывающий опасную зону
		/// По умолчанию стремится не попадать туда
		/// Если цель находится внутри зоны, то движение допустимо с точки, из которой до цели минимальное расстояние
		/// </summary>
		private class DangerousZoneForce : IForceCalculator
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
				Log.Log($"DangerousZoneForce({targetPoint}, {zoneCenter}, {minDistance}, {accuracyPositioning})", nameof(DangerousZoneForce));
				_targetPoint = targetPoint;
				_zoneCenter = zoneCenter;
				_minDistance = minDistance;
				_accuracyPositioning = accuracyPositioning;
				Log.Log($"DangerousZoneForce.End", nameof(DangerousZoneForce));
			}

			/// <summary>
			/// Вычисление влияния этого поля
			/// </summary>
			/// <param name="env"></param>
			/// <param name="targetPoint">Точка, куда лежит путь(в локальных координатах)</param>
			/// <param name="zoneCenter">Центр опасной зоны(в локальных координатах</param>
			/// <param name="minDistance">Радиус опасной зоны</param>
			/// <param name="freeTunnelRadius">Точность позиционирования</param>
			/// <returns>Сила, создаваемая этим полем в текущей позиции корабля</returns>
			public static Vector3D CalculateDangerousZoneForce(Environment env, Vector3D targetPoint, Vector3D zoneCenter, double minDistance, double freeTunnelRadius = ACCURACY_POSITIONING)
			{
				if (minDistance < 0.0)
					throw new Exception($"Argument out of range. {nameof(minDistance)}:{minDistance}. Need Positive");
				Log.Log($"CalculateDangerousZoneForce({Vector3.Round(targetPoint, 2)}, {Vector3.Round(zoneCenter, 2)}, {minDistance}, {freeTunnelRadius})", nameof(DangerousZoneForce));
				Vector3D answer = new Vector3D(0);
				//добавим сопротивление, чтобы в зону случайно не залететь
				answer = Vector3D.Add(answer,
					-2.0 * ACCELERATION_K *
					InertialForceCalculator.InertialForceCalculate(
						env, zoneCenter, minDistance));
				Log.Log($"CalculateDangerousZoneForce.{nameof(answer)}(InertialResist):{Vector3.Round(answer, 2)}", nameof(DangerousZoneForce));
				double l = env.MathCache.Length(targetPoint);//сколько вообще надо пролететь
				Log.Log($"CalculateDangerousZoneForce.l:{l}", nameof(DangerousZoneForce));
				double lp = (targetPoint * zoneCenter).Sum / env.MathCache.Length(zoneCenter);//сколько надо пролететь в опасной зоне
				Log.Log($"CalculateDangerousZoneForce.lp:{lp}", nameof(DangerousZoneForce));
				double toCenter = env.MathCache.Length(zoneCenter);
				Log.Log($"CalculateDangerousZoneForce.{nameof(toCenter)}:{toCenter}", nameof(DangerousZoneForce));
				if (toCenter < minDistance && (Math.Sqrt(l * l - lp * lp) > freeTunnelRadius))// && (zoneCenter - targetPoint).Length() < minDistance)//TODO из-за этого условия возможны "туннели" к цели вне поля
					answer = Vector3D.Add(answer,
						Vector3D.Multiply(MaxPowerForceCalculator.CalculateMaxPowerToPoint(env, zoneCenter), -2.0));//TODO magick number
				Log.Log($"CalculateDangerousZoneForce.{nameof(answer)}:{Vector3.Round(answer, 2)}", nameof(DangerousZoneForce));
				Log.Log($"CalculateDangerousZoneForce.End", nameof(DangerousZoneForce));
				return answer;
			}

			public Vector3D Calculate(Environment env)
			{
				Log.Log($"Calculate()", nameof(DangerousZoneForce));
				var answer = CalculateDangerousZoneForce(env, _targetPoint.Now(env), _zoneCenter.Now(env), _minDistance, _accuracyPositioning);
				Log.Log($"Calculate.{nameof(answer)}:{Vector3.Round(answer, 2)}", nameof(DangerousZoneForce));
				Log.Log($"Calculate.End", nameof(DangerousZoneForce));
				return answer;
			}
		}
	}
}