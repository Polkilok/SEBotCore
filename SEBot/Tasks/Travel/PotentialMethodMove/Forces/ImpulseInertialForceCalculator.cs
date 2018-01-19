using System;
using VRageMath;
using VRageRender;

namespace SEBot
{
	public sealed partial class Program
	{
		/// <summary>
		/// Расчет силы инерции.
		/// В данном контексте - избыточная сила, которая не позволит остановиться в целевой точке
		/// Работает НАМНОГО лучше InertialForceCalculator при ОЧЕНЬ мощных двигателях, при разумном соотношении - хуже
		/// </summary>
		private class ImpulseInertialForceCalculator : IForceCalculator
		{
			private readonly double _accuracySpeed;
			private readonly double _accuracyPositioning;
			private readonly IPointProvider _point;

			/// <summary>
			/// Создает обьект расчета силы для двигателей для заданного направления
			/// </summary>
			/// <param name="accuracySpeed">Точность вычисления скорости</param>
			public ImpulseInertialForceCalculator(IPointProvider p, double accuracySpeed = ACCURACY_SPEED, double accuracyPositioning = ACCURACY_POSITIONING)
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
			/// <param name="env"></param>
			/// <param name="point">целевая точка в локальных коодинатах</param>
			/// <param name="deadZone">Радиус мертвой зоны. В ней сила == 0</param>
			/// <param name="accuracySpeed">Точность при сравнении скорости</param>
			/// <param name="accuracyPositioning">Точность при сравнении позиций</param>
			/// <returns></returns>
			public static Vector3D InertialForceCalculate(Environment env, Vector3D point, double deadZone, double accuracySpeed = ACCURACY_SPEED, double accuracyPositioning = ACCURACY_POSITIONING)
			{
				//описание метода:
				//всё в локальных координатах
				//1:
				//	o1 = (0,0,0) - позиция корабля
				//	p1 - его импульс
				//2:
				//	o2 - следующая позиция
				//	p2 - следующий импульс
				//	pMax - максимально допустимый импульс для остановки в заданной точке
				//
				// если Проекция p2 на pMax > |pMax| тормозить
				// Полезное: dp = F * dt; (сохраняя векторную форму)
				if (deadZone < 0.0) throw new Exception($"Argument out of range. {nameof(deadZone)}:{deadZone}. wait positive");
				if (accuracySpeed < 0.0) throw new Exception($"Argument out of range. {nameof(accuracySpeed)}:{accuracySpeed}. wait positive");
				if (accuracyPositioning < 0.0) throw new Exception($"Argument out of range. {nameof(accuracyPositioning)}:{accuracyPositioning}. wait positive");
				Log.Log($"InertialForceCalculate({Vector3.Round(point, 3)}, {deadZone}, {accuracySpeed}, {accuracyPositioning})", nameof(ImpulseInertialForceCalculator));
				if (deadZone > accuracyPositioning)
					point = env.MathCache.Normalize(point) * Math.Max(env.MathCache.Length(point) - deadZone, 0);
				Log.Log($"InertialForceCalculate.point:{Vector3.Round(point, 3)}", nameof(ImpulseInertialForceCalculator));
				if (point.Length() < accuracyPositioning)
					return new Vector3D(0);
				double m = env.Mass;
				Log.Log($"InertialForceCalculate.m:{m}", nameof(ImpulseInertialForceCalculator));
				var v = env.VectorShipSpeed;
				Log.Log($"InertialForceCalculate.v:{Vector3.Round(v, 3)}", nameof(ImpulseInertialForceCalculator));
				var p1 = m * v;
				Log.Log($"InertialForceCalculate.p1:{Vector3.Round(p1, 3)}", nameof(ImpulseInertialForceCalculator));
				Vector3D answer;
				if (env.MathCache.Length(p1) > m * accuracySpeed)
				{
					var Ftarget = MaxPowerForceCalculator.CalculateMaxPowerToPoint(env, point);
					Log.Log($"InertialForceCalculate.Ftarget:{Vector3.Round(Ftarget, 3)}", nameof(ImpulseInertialForceCalculator));
					var dt = env.TimeSinceLastRun;
					Log.Log($"InertialForceCalculate.t:{dt:0.00}", nameof(ImpulseInertialForceCalculator));
					var p2 = p1 + Ftarget * dt;
					Log.Log($"InertialForceCalculate.p2:{Vector3.Round(p2, 3)}", nameof(ImpulseInertialForceCalculator));
					var o2 = v * dt + ((dt * dt) / (2.0 * m)) * Ftarget;
					Log.Log($"InertialForceCalculate.o1:{Vector3.Round(o2, 3)}", nameof(ImpulseInertialForceCalculator));
					var correction = CalculateCorrectImpulse(env, point - o2, p2, m * accuracySpeed);
					Log.Log($"InertialForceCalculate.correction:{Vector3.Round(correction, 3)}", nameof(ImpulseInertialForceCalculator));
					answer = correction / dt;
				}
				else
					answer = new Vector3D(0.0);
				Log.Log($"InertialForceCalculate.{nameof(answer)}:{Vector3.Round(answer, 2)}", nameof(ImpulseInertialForceCalculator));
				Log.Log($"InertialForceCalculate.End", nameof(ImpulseInertialForceCalculator));
				return answer;
			}

			private static Vector3D CalculateCorrectImpulse(Environment env, Vector3D point, Vector3D p, double maxPdeadZone)
			{
				Log.Log($"CalculateCorrectImpulse({env}, {point}, {p}, {maxPdeadZone})", nameof(ImpulseInertialForceCalculator));
				var vectorFstop = MaxPowerForceCalculator.CalculateMaxPowerToPoint(env, -point);
				Log.Log($"CalculateCorrectImpulse.vectorFstop:{Vector3.Round(vectorFstop, 3)}", nameof(ImpulseInertialForceCalculator));
				var t = point.Length() / env.ShipSpeed;
				Log.Log($"CalculateCorrectImpulse.t:{t:0.00}", nameof(ImpulseInertialForceCalculator));
				var vectorMaxP = -vectorFstop * t;
				Log.Log($"CalculateCorrectImpulse.vectorMaxP:{Vector3.Round(vectorMaxP, 3)}", nameof(ImpulseInertialForceCalculator));
				var maxP = vectorMaxP.Length();
				//maxP = maxP < maxPdeadZone ? 0 : maxP;
				Log.Log($"CalculateCorrectImpulse.maxP:{maxP:0.00}", nameof(ImpulseInertialForceCalculator));
				var correction =
					env.MathCache.Projection(p, vectorMaxP);
				Log.Log($"CalculateCorrectImpulse.correction:{correction:0.00}", nameof(ImpulseInertialForceCalculator));
				if (correction < maxP)
					return new Vector3D(0);
				return (correction - maxP) * env.MathCache.Normalize(vectorMaxP);
			}

			public Vector3D Calculate(Environment env)
			{
				return InertialForceCalculate(env, _point.Now(env), 0.0, _accuracySpeed, _accuracyPositioning);
			}
		}
	}
}