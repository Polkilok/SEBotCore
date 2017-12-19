using System;

namespace SEBot
{
	public sealed partial class Program
	{
		class CriticalMassCondition : ICondition
		{
			private readonly double _criticalMass;
			//Создает условие, которое срабатывает, когда заданное направление двигателей использует тягу более заданной мощности
			//Здесь мощность - отношение реальной силы к максимальной, т. е. в диапазоне от 0 до 1
			public CriticalMassCondition(double criticalMass)
			{
				Log.Log($"CriticalMassCondition.CriticalMassCondition({criticalMass})", CONDITION_LVL);
				if (criticalMass < 0.0) throw new Exception($"argument out of range. {nameof(criticalMass)}:{criticalMass}, wait >0.0");
				_criticalMass = criticalMass;
			}
			public bool Check()
			{
				return Ship.Mass > _criticalMass;
			}
		}
	}

}