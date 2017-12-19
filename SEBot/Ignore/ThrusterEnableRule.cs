using System;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		//Класс, который предоставляет методы расчета условия включения двигателей
		//а так же мощность(в долях от максимума), с которой их нужно активировать
		class ThrusterEnableRule
		{
			protected readonly double AccuracyPositioning;
			protected readonly double AccuracySpeed;

			public ThrusterEnableRule(double accuracyPositioning, double accuracySpeed)
			{
				AccuracySpeed = accuracySpeed;
				AccuracyPositioning = accuracyPositioning;
			}

			//Проверяет условие достижения точки
			public bool PointIsReached(double leftDistance, Base6Directions.Direction direction)
			{
				leftDistance = Math.Abs(leftDistance);
				return leftDistance < AccuracyPositioning &&
					   Math.Abs(Ship.MovementSystem.GetSpeedInDirection(direction)) < AccuracySpeed;
			}

			//Подсказывает, нужно ли включать двигатели
			public virtual bool EnableCondition(double leftDistance, Base6Directions.Direction direction)
			{
				double v = Ship.MovementSystem.GetSpeedInDirection(direction);
				//Log.Log("ship speed " + Math.Round(v, 1).ToString());
				if (leftDistance > 15 || v < AccuracySpeed)//TODO magic number
					return true;
				else
					return false;
			}

			//Подсказывает, мощность двигателей (в долях от максимальной, т. е. в диапазоне от 0 до 1)
			//По умолчанию 1
			public virtual float ThrustPower(double leftDistance, Base6Directions.Direction direction)
			{
				return 1;
			}
		}
	}

}