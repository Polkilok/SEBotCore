using System;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		/// <summary>
		/// Расчет силы сопротивления движению - для решения ситуации "выхода на орбиту"
		/// Внутри - расчет части силы поля притежения, которая обеспечивает центростремительную состовляющую
		/// Далее - вычисление точки цетра вращения
		/// Если при такой же центростремительной силе мы окажемся перед целевой точкой - всё ОК
		/// Если позади неё - гасим перпендикулярную часть скорости
		/// </summary>
		private class OrbitingResistForce : IForceCalculator
		{
			private readonly IPointProvider _point;
			private readonly double _orbitingResistK;
			private readonly double _speedAccuracy;
			private readonly double _positioningAccuracy;

			/// <summary>
			/// Создание экземляра сопротивления круговому движению
			/// </summary>
			/// <param name="p">Точка, в которую надо прилететь</param>
			/// <param name="orbitingResistK">Определяет запас расстояния по "промаху" при движении к точке. Значения - от 0.0, больше 1.0 не рекомендуется</param>
			/// <param name="speedAccuracy">Точность определения скорости. Если перпендикулярная части скороти меньше заданной точности, сила будет 0</param>
			/// <param name="positioningAccuracy">Точность определения позиции</param>
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

			//true православный вариант, но есть баги
			public Vector3D Calculate(Environment env)
			{
				//TODO rewrite
				Log.Log($"Calculate()", nameof(OrbitingResistForce));
				var answer = new Vector3D(0.0);
				var p = _point.Now(env);
				Log.Log($"Calculate.p:{Vector3.Round(p, 2)}", nameof(OrbitingResistForce));
				Vector3D vVector = Ship.TravelSystem.Speed;
				Log.Log($"Calculate.{nameof(vVector)}:{Vector3.Round(vVector, 2)}", nameof(OrbitingResistForce));
				double m = Ship.Mass;
				Log.Log($"Calculate.{nameof(m)}:{m.ToString("0.0")}", nameof(OrbitingResistForce));
				double v = vVector.Length();
				Log.Log($"Calculate.{nameof(v)}:{v.ToString("0.00")}", nameof(OrbitingResistForce));
				var Ft = MaxPowerForceCalculator.CalculateMaxPowerToPoint(env, p);
				Log.Log($"Calculate.{nameof(Ft)}:{Vector3.Round(Ft, 2)}", nameof(OrbitingResistForce));

				var vn = Vector3D.Normalize(vVector);
				Log.Log($"Calculate.{nameof(vn)}:{Vector3.Round(vn, 2)}", nameof(OrbitingResistForce));
				Vector3D Fc = Ft - Vector3D.Dot(Ft, vn) * vn;
				//var Fc = Vector3D.Reject(Ft, vVector);
				Log.Log($"Calculate.{nameof(Fc)}:{Vector3.Round(Fc, 2)}", nameof(OrbitingResistForce));
				var r = m * v * v / Fc.Length();
				Log.Log($"Calculate.{nameof(r)}:{r.ToString("0.0")}", nameof(OrbitingResistForce));
				if (r > _positioningAccuracy)
				{
					var O = Vector3D.Normalize(Fc) * r;
					Log.Log($"Calculate.{nameof(O)}:{Vector3.Round(O, 1)}", nameof(OrbitingResistForce));
					var OT = _orbitingResistK * (p - O).Length();
					Log.Log($"Calculate.{nameof(OT)}:{OT.ToString("0.00")}", nameof(OrbitingResistForce));
					var perpendicularV = Vector3.Reject(vVector, p);
					if (perpendicularV.LengthSquared() > _speedAccuracy)
						if (OT + _positioningAccuracy < r)
						{
							var t = GlobalEventManeger.TickPeriod / 1000d;
							Log.Log($"Calculate.{nameof(t)}:{t.ToString("0.000")}", nameof(OrbitingResistForce));
							var k = m * perpendicularV.Length() / t * ACCELERATION_K;
							Log.Log($"Calculate.{nameof(k)}:{k.ToString("0.00")}", nameof(OrbitingResistForce));
							answer = k * MaxPowerForceCalculator.CalculateMaxPowerToPoint(env, -perpendicularV);
						}
					//else if (OT - _positioningAccuracy < r)
					//	answer = MaxPowerForceCalculator.CalculateMaxPowerToPoint(O);
				}
				Log.Log($"Calculate.{nameof(answer)}:{Vector3.Round(answer, 2)}", nameof(OrbitingResistForce));
				Log.Log($"Calculate.End", nameof(OrbitingResistForce));
				return answer;
			}
		}
	}
}