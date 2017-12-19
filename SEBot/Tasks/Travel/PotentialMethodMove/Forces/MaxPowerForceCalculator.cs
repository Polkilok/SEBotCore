using System;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		/// <summary>
		/// Предоставляет максимальную силу двигателей в направлении прогноза точки точки
		/// </summary>
		class MaxPowerForceCalculator : IForceCalculator
		{
			IPointProvider _point;
			public MaxPowerForceCalculator(IPointProvider p)
			{
				if (p == null) throw new Exception($"Argument null{nameof(p)}");
				_point = p;
			}
			public Vector3D Calculate()
			{
				Vector3D p = _point.Prognosed(GlobalEventManeger.TickPeriod / 1000.0);
				//Vector3D p = _point.Now();
				return CalculateMaxPowerToPoint(p);
			}

			/// <summary>
			/// Предоставляет расчет силы по направлению к целевой точке
			/// </summary>
			/// <param name="point">цель</param>
			/// <returns>максимально возможная сила, с учетом возможностей двигателей</returns>
			public static Vector3D CalculateMaxPowerToPoint(Vector3D point)
			{
				Log.Log($"MaxPowerForceCalculator.CalculatePower({point})", POTENTIAL_METHOD_DEBUG_LVL);
				//Vector3D p = Vector3D.Normalize(pointInLocalCoordinates);
				//вычислим базовую силу
				Vector3D basePower = Ship.MovementSystem.GetMaxPower(-point, 0.0);
				Log.Log($"MaxPowerForceCalculator.CalculatePower.basePower:{Vector3.Round(basePower, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				//а теперь распределим её в соответствии с направлением к точке
				Vector3D scaledPower = PotentialMethodMove.DistributeForce(basePower, point);
				Log.Log($"MaxPowerForceCalculator.CalculatePower.scaledPower:{Vector3.Round(scaledPower, 2)}", POTENTIAL_METHOD_DEBUG_LVL);
				Log.Log($"MaxPowerForceCalculator.CalculatePower.End", POTENTIAL_METHOD_DEBUG_LVL);
				return scaledPower;
			}
		}
	}

}