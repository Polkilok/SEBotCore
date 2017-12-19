using System;
using System.Collections.Generic;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		class PotentialMethodMove : Task
		{
			internal class ForceK
			{
				public readonly IForceCalculator Force;
				public readonly double K;
				public ForceK(IForceCalculator force, double Coef)
				{
					Force = force;
					K = Coef;
				}
			}
			private readonly List<ForceK> forces;
			private readonly IPointProvider _point;
			private readonly double _accuracyPositioning_2;
			private readonly double _accuracySpeed_2;
			private readonly double _accuracyForce;
			private double _maxForce;

			//TODO использовать (ITargetPointProvider targetPoint, ...)
			public PotentialMethodMove(IPointProvider targetPoint, double accuracyPositioning, double accuracySpeed, double accuracyForce)
			{
				if (accuracyPositioning < 0.0) throw new Exception($"Out of range, need positive. {nameof(accuracyPositioning)}:{accuracyPositioning}");
				if (accuracySpeed < 0.0) throw new Exception($"Out of range, need positive. {nameof(accuracySpeed)}:{accuracySpeed}");
				if (accuracyForce < 0.0) throw new Exception($"Out of range, need positive. {nameof(accuracyForce)}:{accuracyForce}");
				_accuracySpeed_2 = accuracySpeed * accuracySpeed;
				_accuracyPositioning_2 = accuracyPositioning * accuracyPositioning;
				_point = targetPoint;
				_accuracyForce = accuracyForce;
				forces = new List<ForceK>();
				_maxForce = 0.0;
				foreach (var dir in Base6Directions.EnumDirections)
					_maxForce = Math.Max(Ship.MovementSystem.GetMaxPowerInDirection(dir), _maxForce);
			}

			public bool Execute()
			{
				Log.Log($"PotentialMethodMove.Execute.Execute()", POTENTIAL_METHOD_RESULTS_DEBUG_LVL);
				Log.Log($"PotentialMethodMove.Execute.forces.Count:{forces.Count}", POTENTIAL_METHOD_RESULTS_DEBUG_LVL);
				double speed = Ship.TravelSystem.Speed.LengthSquared();
				double leftDistance = _point.Now().LengthSquared();
				if (leftDistance < _accuracyPositioning_2 && speed < _accuracySpeed_2)
				{
					Ship.MovementSystem.Stop();
					Log.Log($"PotentialMethodMove.Execute.End", POTENTIAL_METHOD_RESULTS_DEBUG_LVL);
					return true;
				}
				Vector3D resultForce = new Vector3D(0.0);
				foreach (var force in forces)
				{
					Vector3 f = force.Force.Calculate() * force.K;
					Log.Log($"PotentialMethodMove.Execute.f({force.Force.GetType().Name}):{Vector3.Round(f, 2)}", POTENTIAL_METHOD_RESULTS_DEBUG_LVL);
					resultForce = Vector3D.Add(f, resultForce);
				}
				Log.Log($"PotentialMethodMove.Execute.resultForce:{Vector3.Round(resultForce, 2)}", POTENTIAL_METHOD_RESULTS_DEBUG_LVL);
				foreach (var dir in Base6Directions.EnumDirections)
				{
					double proj = (resultForce * Base6Directions.GetVector(dir)).Sum;
					var oppositiveDir = Base6Directions.GetOppositeDirection(dir);
					if (proj > _accuracyForce)
					{
						var maxPower = Ship.MovementSystem.GetMaxPowerInDirection(oppositiveDir);
						Ship.MovementSystem.OverrideDirection(
							oppositiveDir,
							(float)Math.Min(proj / maxPower, 1.0));
					}
					else
						Ship.MovementSystem.OverrideDirection(oppositiveDir, 0f);
				}
				Log.Log($"PotentialMethodMove.Execute.End", POTENTIAL_METHOD_RESULTS_DEBUG_LVL);
				return false;
			}

			public void AddForce(IForceCalculator force, double Coef)
			{
				if (force == null) throw new Exception($"{nameof(force)} is null");
				forces.Add(new ForceK(force, Coef));
			}
			/// <summary>
			/// Распределяет силу по двигателям с максимальной эффективностью.
			/// Как минимум 1 двигатель будет задействован на 100%, остальные - меньше 100%
			/// </summary>
			/// <param name="trustersPower">силы двигателей</param>
			/// <param name="force">вектор силы, которую нужно реализовать</param>
			/// <returns>Вектор, описывающий распределение силы по двигателям</returns>
			public static Vector3D DistributeForce(Vector3D trustersPower, Vector3D force)
			{
				//перейдем в положительную ось
				Vector3D p = trustersPower * Vector3.Sign(trustersPower);
				Vector3D f = force * Vector3.Sign(force);
				//и вычислим коэфициенты
				Vector3D kVec = p / f;
				//нам нужен минимальный
				double k = kVec.Min();
				//всё, теперь мы уложимся в допустимый диапазон
				return k * force;
			}
		}
	}

}