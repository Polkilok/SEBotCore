﻿using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		/// <summary>
		/// Интерфейс, который расчитывает некоторую силу, генерируемую заданной точкой
		/// Используется для расчетов в методе потенциалов
		/// </summary>
		public interface IForceCalculator
		{
			Vector3D Calculate(Environment env);
		}
	}
}