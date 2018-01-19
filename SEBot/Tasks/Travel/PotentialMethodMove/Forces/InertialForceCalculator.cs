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
		private class InertialForceCalculator : IForceCalculator
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

			/// <summary>
			/// Вычисляет инерционную силу при подлете к точке с учетом мертвой зоны
			/// </summary>
			/// <param name="point">целевая точка в локальных коодинатах</param>
			/// <param name="deadZone">Радиус мертвой зоны. В ней сила == 0</param>
			/// <param name="accuracySpeed">Точность при сравнении скорости</param>
			/// <param name="accuracyPositioning">Точность при сравнении позиций</param>
			/// <returns></returns>
			public static Vector3D InertialForceCalculate(Environment env, Vector3D point, double deadZone, double accuracySpeed = ACCURACY_SPEED, double accuracyPositioning = ACCURACY_POSITIONING)
			{
				if (deadZone < 0.0) throw new Exception($"Argument out of range. {nameof(deadZone)}:{deadZone}. wait positive");
				if (accuracySpeed < 0.0) throw new Exception($"Argument out of range. {nameof(accuracySpeed)}:{accuracySpeed}. wait positive");
				if (accuracyPositioning < 0.0) throw new Exception($"Argument out of range. {nameof(accuracyPositioning)}:{accuracyPositioning}. wait positive");
				Log.Log($"InertialForceCalculate({point}, {deadZone})", nameof(InertialForceCalculator));
				double s = point.Length() - deadZone;
				Log.Log($"InertialForceCalculate.s:{s}", nameof(InertialForceCalculator));
				//if (s < accuracyPositioning)
				//{
				//	Log.Log($"InertialForceCalculate.return:{new Vector3D(0)}", nameof(InertialForceCalculator));
				//	Log.Log($"InertialForceCalculate.End", nameof(InertialForceCalculator));
				//	return new Vector3D(0);//если мы попали в мертвую зону, то ничего не делать
				//}
				Vector3D p = Vector3D.Normalize(point) * s;
				Log.Log($"InertialForceCalculate.p:{Vector3.Round(p, 2)}", nameof(InertialForceCalculator));
				double m = env.Mass;
				Log.Log($"InertialForceCalculate.m:{m}", nameof(InertialForceCalculator));
				double v = Vector3D.Dot(env.VectorShipSpeed, p) / s;
				Log.Log($"InertialForceCalculate.v:{v}", nameof(InertialForceCalculator));
				if (v < accuracySpeed)
				{
					Log.Log($"InertialForceCalculate.return:{new Vector3D(0)}", nameof(InertialForceCalculator));
					Log.Log($"InertialForceCalculate.End", nameof(InertialForceCalculator));
					return new Vector3D(0);
				}
				Vector3D vp = Vector3D.Normalize(p) * v;
				Log.Log($"InertialForceCalculate.vp:{vp}", nameof(InertialForceCalculator));
				double Enow = m * v * v / 2;
				Log.Log($"InertialForceCalculate.Enow:{Enow}", nameof(InertialForceCalculator));
				Vector3D maxResistForce = PotentialMethodMove.DistributeForce(env.Ship.MovementSystem.GetMaxPower(-vp, 0), vp);
				Log.Log($"InertialForceCalculate.{nameof(maxResistForce)}:{Vector3.Round(maxResistForce, 2)}", nameof(InertialForceCalculator));
				Vector3D maxAccelerationForce = PotentialMethodMove.DistributeForce(env.Ship.MovementSystem.GetMaxPower(vp, 0), vp);
				Log.Log($"InertialForceCalculate.{nameof(maxAccelerationForce)}:{Vector3.Round(maxAccelerationForce, 2)}", nameof(InertialForceCalculator));
				double t = (double)GlobalEventManeger.TickPeriod / 1000.0;
				Log.Log($"InertialForceCalculate.t:{t}", nameof(InertialForceCalculator));
				Vector3D ds = vp * t;
				Log.Log($"InertialForceCalculate.ds:{Vector3.Round(ds, 2)}", nameof(InertialForceCalculator));
				double Eprognosed = Enow + Vector3D.Dot(ds, maxAccelerationForce);
				Log.Log($"InertialForceCalculate.Eprognosed:{Eprognosed}", nameof(InertialForceCalculator));
				Vector3D s_prognosed = p - ds;
				Log.Log($"InertialForceCalculate.s_prognosed:{Vector3.Round(s_prognosed, 2)}", nameof(InertialForceCalculator));
				double MaxPossibleWork = (s_prognosed * maxResistForce).Sum;
				Log.Log($"InertialForceCalculate.MaxPossibleWork:{MaxPossibleWork}", nameof(InertialForceCalculator));
				double SafeE = 0.5 * m * accuracySpeed * accuracySpeed;//энергия, которую допустимо оставить
				Log.Log($"InertialForceCalculate.MaxPossibleWork:{MaxPossibleWork}", nameof(InertialForceCalculator));
				double dE = Eprognosed - Math.Max(MaxPossibleWork - SafeE, 0);
				Log.Log($"InertialForceCalculate.dE:{dE}", nameof(InertialForceCalculator));
				double force = Math.Max(dE / ds.Length(), 0);
				Log.Log($"InertialForceCalculate.force:{force}", nameof(InertialForceCalculator));
				var answer = force * Vector3D.Normalize(vp);
				Log.Log($"InertialForceCalculate.{nameof(answer)}:{Vector3.Round(answer, 2)}", nameof(InertialForceCalculator));
				Log.Log($"InertialForceCalculate.End", nameof(InertialForceCalculator));
				return answer;
			}

			public Vector3D Calculate(Environment env)
			{
				return InertialForceCalculate(env, _point.Now(env), 0.0, _accuracySpeed, _accuracyPositioning);
			}
		}
	}
}