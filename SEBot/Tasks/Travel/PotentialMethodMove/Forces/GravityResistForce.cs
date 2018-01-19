using System;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		private class GravityResistForce : IForceCalculator
		{
			//TODO inspect
			private readonly double _accuracyPositioning;

			private readonly double _gravityK;

			private readonly IPointProvider _targetPoint;

			public GravityResistForce(IPointProvider targetPoint, double accuracyPositioning = ACCURACY_POSITIONING, double gravityK = 1.0)
			{
				if (targetPoint == null) throw new Exception($"argument {nameof(targetPoint)} null exception.");
				if (accuracyPositioning < 0) throw new Exception($"argument out of range. {nameof(accuracyPositioning)}:{accuracyPositioning}. need positive");
				Log.Log($"GravityResistForce({targetPoint}, {accuracyPositioning}, {gravityK})", nameof(GravityResistForce));
				_targetPoint = targetPoint;
				_accuracyPositioning = accuracyPositioning;
				_gravityK = gravityK;
				Log.Log($"GravityResistForce.End", nameof(GravityResistForce));
			}

			public Vector3D Calculate(Environment env)
			{
				Log.Log($"Calculate()", nameof(GravityResistForce));
				Vector3D g = env.Ship.Gravity;
				Log.Log($"Calculate.g(local):{Vector3.Round(g, 2)}", nameof(GravityResistForce));
				if (!g.IsValid() || env.MathCache.Length(g) < ACCURACY_SPEED)//TODO magick number. Вынести в параметры?
				{
					Log.Log($"Calculate.return:{new Vector3D(0)}", nameof(GravityResistForce));
					Log.Log($"Calculate.End", nameof(GravityResistForce));
					return new Vector3D(0);
				}
				double m = env.Mass;
				Log.Log($"Calculate.m:{m}", nameof(GravityResistForce));
				Vector3D gravityResist = Vector3D.Negate(m * g) * _gravityK;
				Log.Log($"Calculate.{nameof(gravityResist)}:{Vector3.Round(gravityResist, 2)}", nameof(GravityResistForce));
				Vector3D answer = new Vector3D(gravityResist);
				Log.Log($"Calculate.{nameof(answer)}:{Vector3.Round(answer, 2)}", nameof(GravityResistForce));
				Log.Log($"Calculate.End", nameof(GravityResistForce));
				return answer;
			}
		}
	}
}