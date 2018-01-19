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
		private class SimpleOrbitingResistForce : IForceCalculator
		{
			private readonly IPointProvider _point;
			private readonly double _threshold;

			/// <summary>
			/// Создание экземляра сопротивления круговому движению
			/// </summary>
			/// <param name="p">Точка, в которую надо прилететь</param>
			/// <param name="speedAccuracy">Точность определения скорости</param>
			/// <param name="forceAccuracy">Точность вычисления силы</param>
			public SimpleOrbitingResistForce(IPointProvider p, double speedAccuracy = ACCURACY_SPEED, double forceAccuracy = ACCURACY_FORCE)
			{
				if (p == null) throw new Exception($"Argument null {nameof(p)}");
				if (speedAccuracy < 0.0) throw new Exception($"Argument out of rang.{nameof(speedAccuracy)}:{speedAccuracy}, wait positive");
				if (forceAccuracy <= 0) throw new Exception($"Argument out of rang.{nameof(forceAccuracy)}:{forceAccuracy}, wait positive");
				_point = p;
				_threshold = 2.0 * speedAccuracy * forceAccuracy;
			}

			//true православный вариант, но есть баги
			public Vector3D Calculate(Environment env)
			{
				Log.Log($"Calculate({env})", nameof(SimpleOrbitingResistForce));
				var p = _point.Now(env);
				Log.Log($"Calculate.p:{Vector3.Round(p, 2)}", nameof(SimpleOrbitingResistForce));
				Vector3D v = env.VectorShipSpeed;
				Log.Log($"Calculate.{nameof(v)}:{Vector3.Round(v, 2)}", nameof(SimpleOrbitingResistForce));
				Vector3D f = MaxPowerForceCalculator.CalculateMaxPowerToPoint(env, p);
				Log.Log($"Calculate.{nameof(f)}:{Vector3.Round(f, 2)}", nameof(SimpleOrbitingResistForce));
				double a = Vector3D.Dot(v, f);
				Log.Log($"Calculate.{nameof(a)}:{a:0.00}", nameof(SimpleOrbitingResistForce));
				Vector3D answer = new Vector3D();
				if (a < _threshold)
					//answer = MaxPowerForceCalculator.CalculateMaxPowerToPoint(env, -v);
					answer = -env.Mass / env.TimeSinceLastRun * v;
				Log.Log($"Calculate.{nameof(answer)}:{Vector3.Round(answer, 2)}", nameof(SimpleOrbitingResistForce));
				Log.Log($"Calculate.End", nameof(SimpleOrbitingResistForce));
				return answer;
			}
		}
	}
}