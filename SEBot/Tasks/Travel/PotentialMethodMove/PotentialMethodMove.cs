using System;
using System.Collections.Generic;
using VRageMath;

// ReSharper disable once CheckNamespace
namespace SEBot
{
	public sealed partial class Program
	{
		private class PotentialMethodMove : ITask
		{
			private readonly double _accuracyForce;

			private readonly double _accuracyPositioning;

			private readonly double _accuracySpeed;

			private readonly List<ForceK> _forces;

			private readonly IPointProvider _targetPoint;

			public PotentialMethodMove(IPointProvider targetPoint, double accuracyPositioning, double accuracySpeed, double accuracyForce)
			{
				if (accuracyPositioning < 0.0) throw new Exception($"Out of range, need positive. {nameof(accuracyPositioning)}:{accuracyPositioning}");
				if (accuracySpeed < 0.0) throw new Exception($"Out of range, need positive. {nameof(accuracySpeed)}:{accuracySpeed}");
				if (accuracyForce < 0.0) throw new Exception($"Out of range, need positive. {nameof(accuracyForce)}:{accuracyForce}");
				_accuracySpeed = accuracySpeed;
				_accuracyPositioning = accuracyPositioning;
				_targetPoint = targetPoint;
				_accuracyForce = accuracyForce;
				_forces = new List<ForceK>();
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

			public void AddForce(IForceCalculator force, double coef)
			{
				if (force == null) throw new Exception($"{nameof(force)} is null");
				_forces.Add(new ForceK(force, coef));
			}

			public bool Execute(Environment env)
			{
				Log.Log($"PotentialMethodMove.Execute.Execute({env})", nameof(PotentialMethodMove));
				Log.Log($"PotentialMethodMove.Execute.forces.Count:{_forces.Count}", nameof(PotentialMethodMove));
				double speed = env.ShipSpeed;
				double leftDistance = env.MathCache.Length(_targetPoint.Now(env));
				if (leftDistance < _accuracyPositioning && speed < _accuracySpeed)
				{
					env.Ship.MovementSystem.Stop();
					Log.Log($"PotentialMethodMove.Execute.End", nameof(PotentialMethodMove));
					return true;
				}
				Vector3D resultForce = new Vector3D(0.0);

				foreach (var force in _forces)
				{
					Vector3 f = force.Force.Calculate(env) * force.K;
					Log.Log($"PotentialMethodMove.Execute.f({force.Force.GetType().Name}):{Vector3.Round(f, 2)}", nameof(PotentialMethodMove));
					resultForce = Vector3D.Add(f, resultForce);
				}
				ApplyForce(env, resultForce);
				Log.Log($"PotentialMethodMove.Execute.End", nameof(PotentialMethodMove));
				return false;
			}

			private void ApplyForce(Environment environment, Vector3 resultForce)
			{
				Log.Log($"PotentialMethodMove.ApplyForce({environment},{Vector3.Round(resultForce, 2)}", nameof(PotentialMethodMove));
				foreach (var dir in Base6Directions.EnumDirections)
				{
					double proj = Vector3D.Dot(resultForce, Base6Directions.GetVector(dir));
					var oppositiveDir = Base6Directions.GetOppositeDirection(dir);
					if (proj > _accuracyForce)
					{
						var maxPower = environment.Ship.MovementSystem.GetMaxPowerInDirection(oppositiveDir);
						environment.Ship.MovementSystem.OverrideDirection(
							oppositiveDir,
							(float)Math.Min(proj / maxPower, 1.0));
					}
					else
						environment.Ship.MovementSystem.OverrideDirection(oppositiveDir, 0f);
				}
				Log.Log($"PotentialMethodMove.ApplyForce.End", nameof(PotentialMethodMove));
			}

			private class ForceK
			{
				public readonly IForceCalculator Force;
				public readonly double K;

				public ForceK(IForceCalculator force, double coef)
				{
					Force = force;
					K = coef;
				}
			}
		}
	}
}