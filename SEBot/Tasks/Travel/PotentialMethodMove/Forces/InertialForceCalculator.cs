using System;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
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
	}

}