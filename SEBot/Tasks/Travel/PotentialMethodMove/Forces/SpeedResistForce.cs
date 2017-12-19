using System;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		/// <summary>
		/// Сопротивление движению. Для ограничения максимальной скорости или "лишних" движений
		/// </summary>
		class SpeedResistForce : IForceCalculator
		{
			private readonly double _maxSpeed;
			private readonly double _pow;

			public SpeedResistForce(double maxSpeed, int pow)
			{
				if (pow <= 0) throw new Exception($"Argument out of range.{nameof(pow)}:{pow}, need positive");
				if (maxSpeed < 0.0) throw new Exception($"Argument out of range.{nameof(maxSpeed)}:{maxSpeed}, need positive");
				_maxSpeed = maxSpeed;
				_pow = pow;
			}

			public Vector3D Calculate()
			{
				Log.Log($"SpeedResistForce.Calculate()", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D v = Ship.TravelSystem.Speed;
				Log.Log($"SpeedResistForce.Calculate.{nameof(v)}:{Vector3.Round(v, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D nv = v / _maxSpeed * Vector3D.Sign(v);
				Log.Log($"SpeedResistForce.Calculate.{nameof(nv)}:{Vector3.Round(nv, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Vector3D answer = new Vector3D(1.0);
				Log.Log($"SpeedResistForce.Calculate.{nameof(answer)}(sign OR (1)):{Vector3.Round(answer, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				for (int i = 0; i < _pow; ++i)
					answer *= nv;
				Log.Log($"SpeedResistForce.Calculate.{nameof(answer)}(after pow):{Vector3.Round(answer, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				answer *= MaxPowerForceCalculator.CalculateMaxPowerToPoint(-v);
				Log.Log($"SpeedResistForce.Calculate.{nameof(answer)}:{Vector3.Round(answer, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Log.Log($"SpeedResistForce.Calculate.End", POTENTIAL_METHOD_DEBUG_LVL);
				return answer;
			}
		}
	}

}