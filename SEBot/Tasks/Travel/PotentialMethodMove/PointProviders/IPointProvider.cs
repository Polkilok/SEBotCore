using VRageMath;

// ReSharper disable once CheckNamespace
namespace SEBot
{
	public sealed partial class Program
	{
		public interface IPointProvider
		{
			/// <summary>
			/// Текущее положение точки в локальных координатах
			/// </summary>
			/// <returns>Положение точки в локальных координатах</returns>
			Vector3D Now(Environment env);

			/// <summary>
			/// Прогноз положения точки на указанное время
			/// </summary>
			/// <param name="env"></param>
			/// <param name="seconds">количество секунд, на которые выполняется прогноз</param>
			/// <returns>предполагаемое положение точки</returns>
			Vector3D Prognosed(Environment env, double seconds);
		}
	}
}