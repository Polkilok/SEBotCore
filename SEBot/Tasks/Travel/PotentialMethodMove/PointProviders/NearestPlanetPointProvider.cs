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
		private class NearestPlanetPointProvider : IPointProvider
		{
			private static readonly string VALUE_CACHE_NAME = $"{nameof(NearestPlanetPointProvider)}.NearestPlanetPosition";

			public Vector3D Now(Environment env)
			{
				Log.Log($"Now({env})", nameof(NearestPlanetPointProvider));
				if (env.UserCache[VALUE_CACHE_NAME] != null)
					return ((Vector3D?)env.UserCache[VALUE_CACHE_NAME]).Value;
				Vector3D answer = new Vector3D();
				Vector3D planetCenter = new Vector3D();
				if (env.Ship.MainController.TryGetPlanetPosition(out planetCenter))//нууу, возможно при искуственной гравитации...
				{
					Log.Log($"Now.planetCenter(Global):{Vector3.Round(planetCenter, 0)}", nameof(NearestPlanetPointProvider));
					planetCenter = env.Ship.TravelSystem.ToLocalCoordinate(planetCenter);
					Log.Log($"Now.planetCenter(InLocal):{Vector3.Round(planetCenter, 0)}", nameof(NearestPlanetPointProvider));
					double height = 0;
					env.Ship.MainController.TryGetPlanetElevation(MyPlanetElevation.Surface, out height);//TODO ну здесь false уж точно не возможен?
					Log.Log($"Now.{nameof(height)}:{height}", nameof(NearestPlanetPointProvider));
					answer = Vector3D.Normalize(planetCenter) * height;
				}
				//TODO в противном случае неплохо бы кидать исключение...
				Log.Log($"Now.{nameof(answer)}:{Vector3.Round(answer, 2)}", nameof(NearestPlanetPointProvider));
				env.UserCache[VALUE_CACHE_NAME] = answer;
				Log.Log($"Now.End", nameof(NearestPlanetPointProvider));
				return answer;
			}

			public Vector3D Prognosed(Environment env, double seconds)
			{
				Log.Log($"NearestPlanetPoint.Prognosed({env},{seconds})", nameof(NearestPlanetPointProvider));
				var answer = Now(env);
				Log.Log($"NearestPlanetPoint.Prognosed.End", nameof(NearestPlanetPointProvider));
				return answer;
			}
		}
	}
}