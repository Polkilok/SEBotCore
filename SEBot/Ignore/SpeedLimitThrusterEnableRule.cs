using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		//класс, предоставляющий движение с ограничением скорости
		//Если скорость в заданном направлении превышает целевую - отключене двигателей
		//Иначе - правило additionRule
		class SpeedLimitThrusterEnableRule : ThrusterEnableRule
		{
			private double SpeedLimit;
			private ThrusterEnableRule BaseRule;
			//Создает правило, ограничивающее максимальную скорость для заданного дополнительного правила
			//по умолчанию используется реализация InertialRuleThrusterEnableRule
			public SpeedLimitThrusterEnableRule(double accuracyPositioning, double accuracySpeed,
				double speedLimit, ThrusterEnableRule additionRule = null)
				: base(accuracyPositioning, accuracySpeed)
			{
				SpeedLimit = speedLimit;
				BaseRule = additionRule;
				if (BaseRule == null)
					BaseRule = new InertialThrusterEnableRule(accuracyPositioning, accuracySpeed);
			}
			public override bool EnableCondition(double leftDistance, Base6Directions.Direction direction)
			{
				double v = Ship.MovementSystem.GetSpeedInDirection(direction);
				return v < SpeedLimit && base.EnableCondition(leftDistance, direction);
			}
		}
	}

}