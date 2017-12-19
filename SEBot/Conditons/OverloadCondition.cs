using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		//Создает условие, которое срабатывает, когда заданное направление двигателей использует тягу более заданной мощности
		//Здесь мощность - отношение реальной силы к максимальной, т. е. в диапазоне от 0 до 1
		class OverloadCondition : ICondition
		{
			private Base6Directions.Direction ThrustersDirection;
			private float WarningLevel;
			public OverloadCondition(Base6Directions.Direction thrustersDirection, float warningLevel)
			{
				ThrustersDirection = thrustersDirection;
				WarningLevel = warningLevel;
			}
			public bool Check()
			{
				double Power = Ship.MovementSystem.GetPowerForDirection(ThrustersDirection);
				//Log.Log("Power Override: " + Power.ToString("0.00"));
				double MaxPower = Ship.MovementSystem.GetMaxPowerInDirection(ThrustersDirection);
				return Power / MaxPower > WarningLevel;
			}
		}
	}

}