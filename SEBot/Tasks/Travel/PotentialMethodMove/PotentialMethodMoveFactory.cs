using System;
using VRageMath;

// ReSharper disable once CheckNamespace
namespace SEBot
{
	public sealed partial class Program
	{
		public class PotentialMethodMoveFactory : IFactoryPointBasedTask, IFactoryPointProviderBasedTask
		{
			private readonly double _noRotationZone;
			private readonly double _accuracyPositioning;
			private readonly double _accuracySpeed;
			private readonly double _accuracyForce;
			private readonly double _safetyFlyHeight;

			public PotentialMethodMoveFactory(double noRotationZone = DEATH_ZONE_FOR_ROTATION,
				double safetyFlyHeight = SAFETY_FLY_HEIGHT,
				double accuracyPositioning = ACCURACY_POSITIONING,
				double accuracySpeed = ACCURACY_SPEED,
				double accuracyForce = ACCURACY_FORCE)
			{
				if (noRotationZone < 0.0) throw new Exception($"Argument out of range. {nameof(noRotationZone)}:{noRotationZone}, need positive");
				if (accuracyPositioning < 0.0) throw new Exception($"Argument out of range. {nameof(accuracyPositioning)}:{accuracyPositioning}, need positive");
				if (accuracySpeed < 0.0) throw new Exception($"Argument out of range. {nameof(accuracySpeed)}:{accuracySpeed}, need positive");
				if (accuracyForce < 0.0) throw new Exception($"Argument out of range. {nameof(accuracyForce)}:{accuracyForce}, need positive");
				_noRotationZone = noRotationZone;
				_accuracyPositioning = accuracyPositioning;
				_accuracySpeed = accuracySpeed;
				_accuracyForce = accuracyForce;
				_safetyFlyHeight = safetyFlyHeight;
			}

			public ITask GetTask(Vector3D targetPoint, bool rotatate = false)
			{
				Log.Log($"PotentialMethodMoveFactory.GetTask({Vector3.Round(targetPoint, 0)}, {rotatate}", nameof(PotentialMethodMoveFactory));
				if (rotatate == false)
				{
					var targetProvider = new StaticPointProvider(targetPoint);
					var answer = GetTask(targetProvider);
					Log.Log($"PotentialMethodMoveFactory.GetTask.answer:{answer}", nameof(PotentialMethodMoveFactory));
					Log.Log($"PotentialMethodMoveFactory.GetTask.End", nameof(PotentialMethodMoveFactory));
					return answer;
				}
				ITask rotateTask = new TurnDirectionToPoint(Base6Directions.Direction.Forward, targetPoint, _noRotationZone);
				OldComplexTask outerTask = new OldComplexTask(OldComplexTask.EndCondition.Last);
				outerTask.AddTask(rotateTask);
				outerTask.AddTask(GetTask(targetPoint, false));
				Log.Log($"PotentialMethodMoveFactory.GetTask.outerTask:{outerTask}", nameof(PotentialMethodMoveFactory));
				Log.Log($"PotentialMethodMoveFactory.GetTask.End", nameof(PotentialMethodMoveFactory));
				return outerTask;
			}

			public ITask GetTask(IPointProvider target)
			{
				Log.Log($"PotentialMethodMoveFactory.GetTask({target})", nameof(PotentialMethodMoveFactory));
				var moveTask = new PotentialMethodMove(target, _accuracyPositioning, _accuracySpeed, _accuracyForce);
				moveTask.AddForce(new SimpleOrbitingResistForce(target, _accuracySpeed, _accuracyForce), 1.0);
				//moveTask.AddForce(new InertialForceCalculator(target), -2.0);// * ACCELERATION_K);//TODO magic numbers

				moveTask.AddForce(new ImpulseInertialForceCalculator(target, _accuracySpeed, _accuracyPositioning), -1.0);// * ACCELERATION_K);//TODO magic numbers

				//moveTask.AddForce(new GravityResistForce(target, _accuracyPositioning), 1.0);//TODO magic numbers
				//moveTask.AddForce(new DangerousZoneForce(target, new NearestPlanetPointProvider(), _safetyFlyHeight, _accuracyPositioning), 2.0);
				moveTask.AddForce(new MaxPowerForceCalculator(target), 1.0);
				Log.Log($"PotentialMethodMoveFactory.GetTask.moveTask:{moveTask}", nameof(PotentialMethodMoveFactory));
				Log.Log($"PotentialMethodMoveFactory.GetTask.End", nameof(PotentialMethodMoveFactory));
				return AddHS(moveTask);//TODO стабилизация здесь - плохая идея
			}
		}
	}
}