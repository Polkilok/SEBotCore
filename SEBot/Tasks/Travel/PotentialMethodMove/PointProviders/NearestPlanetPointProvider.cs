using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		/// <summary>
		/// Предоставляет ближайшую точку планеты
		/// Насколько она ближайшая? Ну... насколько правильно работает метод 
		/// TryGetPlanetElevation(MyPlanetElevation.Surface, out height) у Keen's
		/// </summary>
		class NearestPlanetPointProvider : IPointProvider
		{
			public Vector3D Now()
			{
				Vector3D answer = new Vector3D();
				Vector3D planetCenter = new Vector3D();
				if (Ship.MainController.TryGetPlanetPosition(out planetCenter))//нууу, возможно при искуственной гравитации...
				{
					Log.Log($"NearestPlanetPoint.Now.planetCenter(Global):{Vector3.Round(planetCenter, 0)}", POTENTIAL_METHOD_POINT_PROVIDERS_LVL);
					planetCenter = Ship.TravelSystem.ToLocalCoordinate(planetCenter);
					Log.Log($"NearestPlanetPoint.Now.planetCenter(InLocal):{Vector3.Round(planetCenter, 0)}", POTENTIAL_METHOD_POINT_PROVIDERS_LVL);
					double height = 0;
					Ship.MainController.TryGetPlanetElevation(MyPlanetElevation.Surface, out height);//TODO ну здесь false уж точно не возможен?
					Log.Log($"NearestPlanetPoint.Now.{nameof(height)}:{height}", POTENTIAL_METHOD_POINT_PROVIDERS_LVL);
					answer = Vector3D.Normalize(planetCenter) * height;
				}
				//TODO в противном случае неплохо бы кидать исключение...
				Log.Log($"NearestPlanetPoint.Now.{nameof(answer)}:{Vector3.Round(answer, 2)}", POTENTIAL_METHOD_POINT_PROVIDERS_LVL);
				return answer;
			}

			public Vector3D Prognosed(double seconds)
			{
				Log.Log($"NearestPlanetPoint.Prognosed({seconds})", POTENTIAL_METHOD_POINT_PROVIDERS_LVL);
				var answer = Now();
				Log.Log($"NearestPlanetPoint.Prognosed.End)", POTENTIAL_METHOD_POINT_PROVIDERS_LVL);
				return answer;
			}
		}
	}

}