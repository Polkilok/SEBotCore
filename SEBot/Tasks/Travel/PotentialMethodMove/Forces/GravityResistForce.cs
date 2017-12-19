using System;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		class GravityResistForce : IForceCalculator
		{
			private readonly double _accuracyPositioning;
			private readonly double _gravityK;
			private readonly IPointProvider _targetPoint;
			public GravityResistForce(IPointProvider targetPoint, double accuracyPositioning = ACCURACY_POSITIONING, double gravityK = 1.0)
			{
				if (targetPoint == null) throw new Exception($"argument {nameof(targetPoint)} null exception.");
				if (accuracyPositioning < 0) throw new Exception($"argument out of range. {nameof(accuracyPositioning)}:{accuracyPositioning}. need positive");
				Log.Log($"GravityResistForce.GravityResistForce({targetPoint}, {accuracyPositioning}, {gravityK})", POTENTIAL_METHOD_DEBUG_LVL);
				_targetPoint = targetPoint;
				_accuracyPositioning = accuracyPositioning;
				_gravityK = gravityK;
				Log.Log($"GravityResistForce.GravityResistForce.End", POTENTIAL_METHOD_DEBUG_LVL);
			}

			public Vector3D Calculate()
			{
				Log.Log($"GravityResistForce.Calculate()", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D g = Ship.Gravity;
				Log.Log($"GravityResistForce.Calculate.g(local):{Vector3.Round(g, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				if (!g.IsValid() || g.LengthSquared() < ACCURACY_SPEED)//TODO magick number. Вынести в параметры?
				{
					Log.Log($"GravityResistForce.Calculate.return:{new Vector3D(0)}", POTENTIAL_METHOD_DEBUG_LVL);
					Log.Log($"GravityResistForce.Calculate.End", POTENTIAL_METHOD_DEBUG_LVL);
					return new Vector3D(0);
				}
				double m = Ship.Mass;
				Log.Log($"GravityResistForce.Calculate.m:{m}", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D gravityResist = Vector3D.Negate(m * g) * _gravityK;
				Log.Log($"GravityResistForce.Calculate.{nameof(gravityResist)}:{Vector3.Round(gravityResist, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D answer = new Vector3D(gravityResist);
				Log.Log($"GravityResistForce.Calculate.{nameof(answer)}:{Vector3.Round(answer, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Log.Log($"GravityResistForce.Calculate.End", POTENTIAL_METHOD_DEBUG_LVL);
				return answer;
			}
		}
	}

}