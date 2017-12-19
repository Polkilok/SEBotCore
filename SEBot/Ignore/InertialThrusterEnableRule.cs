using System;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		//Расширяет вариант MoveInDirection
		//вычисляет, может ли корабль остановиться вовремя на основе 
		//его кинетической энергии и силы двигателей
		//TODO учитывать гравитацию
		class InertialThrusterEnableRule : ThrusterEnableRule
		{
			//коэфициент передачи мощности дигателей, рекомендуется ставить меньше реального
			//это актуально для кораблей на ионниках и атмосферных двгателях в разреженной/плотной атмосферы
			//TODO работает странно
			//TODO Перспектива: сделать автобалансирование
			private double k;

			public InertialThrusterEnableRule(double accuracyPositioning, double accuracySpeed,
				double inertialCoefficient = INERTIAL_COEFFICIENT)
				: base(accuracyPositioning, accuracySpeed)
			{
				k = inertialCoefficient;
			}
			public override bool EnableCondition(double leftDistance, Base6Directions.Direction direction)
			{
				Log.Log($"InertialThrusterEnableRule.EnableСondition({leftDistance}, {direction})", OLD_MOVEMENT_ENABLE_RULE_LVL);
				int m = Ship.Mass;
				Log.Log($"InertialThrusterEnableRule.EnableСondition.{nameof(m)}:{m}", OLD_MOVEMENT_ENABLE_RULE_LVL);
				double F = Ship.MovementSystem.GetMaxPowerInDirection(Base6Directions.GetOppositeDirection(direction));
				Log.Log($"InertialThrusterEnableRule.EnableСondition.{nameof(F)}:{F}", OLD_MOVEMENT_ENABLE_RULE_LVL);
				double gravity = (Ship.Gravity * Base6Directions.GetVector(direction)).Sum;//добавим проекцию силы тяжести
				Log.Log($"InertialThrusterEnableRule.EnableСondition.{nameof(gravity)}:{gravity}", OLD_MOVEMENT_ENABLE_RULE_LVL);
				//TODO проверить правильность расчета тяги в заданном направлении и формулы ниже
				double s = leftDistance;
				double v = Ship.MovementSystem.GetSpeedInDirection(direction);
				Log.Log($"InertialThrusterEnableRule.EnableСondition.{nameof(v)}:{v}", OLD_MOVEMENT_ENABLE_RULE_LVL);
				Log.Log("Result of calc: " + ((2d * k * F * s) > (m * v * v)).ToString());
				Log.Log($"InertialThrusterEnableRule.EnableСondition.End", OLD_MOVEMENT_ENABLE_RULE_LVL);
				if (v < AccuracySpeed)//если скорость ниже порога точности - замем мучаться?
					return true;//если вдруг скорость направленна в противоположном направлении, её необходимо погасть
				else
					return (2d * k * F * s) > (m * v * v);
			}
			//TODO исправить
			public override float ThrustPower(double leftDistance, Base6Directions.Direction direction)
			{
				double s1 = leftDistance;
				double v1 = Ship.MovementSystem.GetSpeedInDirection(direction);
				double t = GlobalEventManeger.TickPeriod / 1000.0;
				double m = Ship.Mass;
				double Fmax = Ship.MovementSystem.GetMaxPowerInDirection(direction);

				double s2 = Math.Max(s1 - v1 * t, 0);
				double x = (0.5 * m * v1 * v1 - k * Fmax * s2) / Fmax / (s1 - s2);
				Log.Log($"direction:{direction}", TRAVEL_SYSTEM_DEBUG_LVL);
				Log.Log($"s1:{s1}, s2:{s2}, v1:{v1}", TRAVEL_SYSTEM_DEBUG_LVL);
				Log.Log($"ThrustPower. Resultation coef:{x}", TRAVEL_SYSTEM_DEBUG_LVL);
				if (x < 0)
					return 1f;
				return Math.Min(1f, Math.Max((float)x, 0.05f));
				//return 0.7f;
			}

		}
	}

}