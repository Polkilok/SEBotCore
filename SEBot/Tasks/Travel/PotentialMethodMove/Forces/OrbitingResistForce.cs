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
				Log.Log($"OrbitingResistForce.Calculate.{nameof(v)}:{v.ToString("0.00")}", POTENTIAL_METHOD_DEBUG_LVL);
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
	}

}