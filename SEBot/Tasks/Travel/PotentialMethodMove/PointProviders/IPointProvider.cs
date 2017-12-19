using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		interface IPointProvider
		{
			/// <summary>
			/// Текущее положение точки
			/// </summary>
			/// <returns>Положение точки в локальных координатах</returns>
			Vector3D Now();
			/// <summary>
			/// Прогноз положения точки на указанное время
			/// </summary>
			/// <param name="seconds">количество секунд, на которые выполняется прогноз</param>
			/// <returns>предполагаемое положение точки</returns>
			Vector3D Prognosed(double seconds);
		}
	}

}