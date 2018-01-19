using System;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		/// <summary>
		/// Предоставляет максимальную силу двигателей в направлении прогноза точки точки
		/// </summary>
		private class MaxPowerForceCalculator : IForceCalculator
		{
			private readonly IPointProvider _point;

			private readonly double _accuracyPositioning;
			private double _accuracySpeed;

			public MaxPowerForceCalculator(IPointProvider p, double accuracyPositioning = ACCURACY_POSITIONING, double accuracySpeed = ACCURACY_SPEED)
			{
				if (p == null) throw new Exception($"Argument null {nameof(p)}");
				if (accuracyPositioning <= 0) throw new Exception($"ArgumentOutOfRangeException {nameof(accuracyPositioning)}, wait positive");
				if (accuracySpeed <= 0) throw new Exception($"ArgumentOutOfRangeException {nameof(accuracySpeed)}, wait positive");
				_point = p;
				_accuracySpeed = accuracySpeed;
				_accuracyPositioning = accuracyPositioning;
			}

			/// <summary>
			/// Предоставляет расчет силы по направлению к целевой точке
			/// </summary>
			/// <param name="env"></param>
			/// <param name="point">цель</param>
			/// <returns>максимально возможная сила, с учетом возможностей двигателей</returns>
			public static Vector3D CalculateMaxPowerToPoint(Environment env, Vector3D point)
			{
				if (env == null) throw new Exception($"Null argument exception {nameof(env)}");
				if (!point.IsValid()) throw new Exception($"Value should be valid. {nameof(point)}");
				Log.Log($"CalculatePower({point})", nameof(MaxPowerForceCalculator));
				var ans = env.UserCache[$"{nameof(MaxPowerForceCalculator)}.{point}"];
				if (ans != null)
					return (Vector3D)ans;
				//вычислим базовую силу
				Vector3D basePower = env.Ship.MovementSystem.GetMaxPower(-point, 0.0);
				Log.Log($"CalculatePower.basePower:{Vector3.Round(basePower, 2)}", nameof(MaxPowerForceCalculator));
				//а теперь распределим её в соответствии с направлением к точке
				Vector3D scaledPower = PotentialMethodMove.DistributeForce(basePower, point);
				Log.Log($"CalculatePower.scaledPower:{Vector3.Round(scaledPower, 2)}", nameof(MaxPowerForceCalculator));
				Log.Log($"CalculatePower.End", nameof(MaxPowerForceCalculator));
				env.UserCache[$"{nameof(MaxPowerForceCalculator)}.{point}"] = scaledPower;
				return scaledPower;
			}

			public Vector3D Calculate(Environment env)
			{
				Vector3D point = _point.Prognosed(env, env.TimeSinceLastRun);
				if (env.MathCache.Length(point) < _accuracyPositioning || env.MathCache.Length(_point.Now(env)) < _accuracyPositioning)
				{
					var p = env.Mass * env.VectorShipSpeed;
					var pMax = env.Mass * _accuracySpeed;
					var dp = pMax - env.MathCache.Length(p);
					Log.Log($"Calculate.dp:{dp}", nameof(MaxPowerForceCalculator));
					return dp / env.TimeSinceLastRun * env.MathCache.Normalize(_point.Now(env));
				}
				return CalculateMaxPowerToPoint(env, point);
			}
		}
	}
}