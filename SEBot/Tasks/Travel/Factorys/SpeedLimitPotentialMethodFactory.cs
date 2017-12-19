using System;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		/// <summary>
		///Класс, предоставляющий движение с ограничением скорости
		/// </summary>
		class SpeedLimitPotentialMethodFactory : IFactoryPointBasedTask
		{
			private readonly double _noRotationZone;
			private readonly double _accuracyPositioning;
			private readonly double _accuracySpeed;
			private readonly double _accuracyForce;
			private readonly double _safetyFlyHeight;
			private readonly double _maxSpeed;

			public SpeedLimitPotentialMethodFactory(
				double maxSpeed,
				double noRotationZone = DEATH_ZONE_FOR_ROTATION,
				double safetyFlyHeight = SAFETY_FLY_HEIGHT,
				double accuracyPositioning = ACCURACY_POSITIONING,
				double accuracySpeed = ACCURACY_SPEED,
				double accuracyForce = ACCURACY_FORCE)
			{
				if (maxSpeed < 0.0) throw new Exception($"Argument out of range. {nameof(maxSpeed)}:{maxSpeed}, need positive");
				if (noRotationZone < 0.0) throw new Exception($"Argument out of range. {nameof(noRotationZone)}:{noRotationZone}, need positive");
				if (accuracyPositioning < 0.0) throw new Exception($"Argument out of range. {nameof(accuracyPositioning)}:{accuracyPositioning}, need positive");
				if (accuracySpeed < 0.0) throw new Exception($"Argument out of range. {nameof(accuracySpeed)}:{accuracySpeed}, need positive");
				if (accuracyForce < 0.0) throw new Exception($"Argument out of range. {nameof(accuracyForce)}:{accuracyForce}, need positive");
				_noRotationZone = noRotationZone;
				_accuracyPositioning = accuracyPositioning;
				_accuracySpeed = accuracySpeed;
				_accuracyForce = accuracyForce;
				_safetyFlyHeight = safetyFlyHeight;
				_maxSpeed = maxSpeed;
			}
			public Task GetTask(Vector3D targetPoint, bool rotatate = false)
			{
				Log.Log($"SimplePotentialMethodFactory.GetTask({Vector3.Round(targetPoint, 0)}, {rotatate}", TRAVEL_SYSTEM_DEBUG_LVL);
				if (rotatate == false)
				{
					var targetProvider = new StaticPointProvider(targetPoint);
					var moveTask = new PotentialMethodMove(targetProvider, _accuracyPositioning, _accuracySpeed, _accuracyForce);
					moveTask.AddForce(new SpeedResistForce(_maxSpeed, 5), 1.0);//TODO magic numbers
					moveTask.AddForce(new GravityResistForce(targetProvider, _accuracyPositioning), 1.0);//TODO magic numbers
																										 //moveTask.AddForce(new OrbitingResistForce(targetProvider), 1.0);//TODO use no default parameters
					moveTask.AddForce(new InertialForceCalculator(targetProvider), -2.0 * ACCELERATION_K);//TODO magic numbers
					moveTask.AddForce(new MaxPowerForceCalculator(targetProvider), 1.0);
					Log.Log($"SimplePotentialMethodFactory.GetTask.moveTask:{moveTask}", TRAVEL_SYSTEM_DEBUG_LVL);
					Log.Log($"SimplePotentialMethodFactory.GetTask.End", TRAVEL_SYSTEM_DEBUG_LVL);
					return AddHS(moveTask);
				}
				Task rotateTask = new TurnDirectionToPoint(Base6Directions.Direction.Forward, targetPoint, _noRotationZone);
				OldComplexTask outerTask = new OldComplexTask(OldComplexTask.EndCondition.Last);
				outerTask.AddTask(rotateTask);
				outerTask.AddTask(GetTask(targetPoint, false));
				Log.Log($"SimplePotentialMethodFactory.GetTask.outerTask:{outerTask}", TRAVEL_SYSTEM_DEBUG_LVL);
				Log.Log($"SimplePotentialMethodFactory.GetTask.End", TRAVEL_SYSTEM_DEBUG_LVL);
				return outerTask;
			}
		}
	}

}