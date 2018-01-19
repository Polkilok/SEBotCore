using System;
using VRageMath;

// ReSharper disable once CheckNamespace
namespace SEBot
{
	public sealed partial class Program
	{
		//TODO Test
		//TODO make segmentPointProvider
		private class TunnelPointProvider : IPointProvider
		{
			private readonly IPointProvider _end;
			private readonly IPointProvider _begin;

			public TunnelPointProvider(IPointProvider begin, IPointProvider end)
			{
				if (begin == null) throw new Exception($"Argument null exception: {nameof(begin)}");
				if (end == null) throw new Exception($"Argument null exception: {nameof(end)}");
				_begin = begin;
				_end = end;
			}

			private Vector3D FindNearestPoint(Environment env)
			{
				var a = _begin.Now(env);
				var b = _end.Now(env);
				//Point2d a, b;      //наша прямая
				var x = new Vector3D(0);        //точка которая проецируется
				var v = b - a; //сдвинем точку "a" в начало координат
				var vn = Vector3D.Normalize(v);     //нормализуем вектор
				var vx = x;// x - Point2d(0, 0);   //превратим точку в вектор с началом в начале координат
				var f = Vector3D.Dot(vn, vx); //скалярное произведение, порядок не важен
				var xp = a + vn * f;            //спроецированная точка
				return xp;
			}

			public Vector3D Now(Environment env)
			{
				return FindNearestPoint(env);
			}

			public Vector3D Prognosed(Environment env, double seconds)
			{
				return Now(env) - env.ShipSpeed * seconds;// '-' because LocalCoordinates
			}
		}
	}
}