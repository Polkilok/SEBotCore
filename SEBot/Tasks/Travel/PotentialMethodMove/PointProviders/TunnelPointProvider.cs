using System;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		//TODO Test
		class TunnelPointProvider : IPointProvider
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

			public Vector3D Now()
			{
				var a = _begin.Now();
				var b = _end.Now();
				//Point2d a, b;      //наша прямая
				var x = new Vector3D(0);        //точка которая проецируется
				var v = b - a; //сдвинем точку "a" в начало координат
				var vn = Vector3D.Normalize(v);     //нормализуем вектор
				var vx = x;// x - Point2d(0, 0);   //превратим точку в вектор с началом в начале координат
				var f = Vector3D.Dot(vn, vx); //скалярное произведение, порядок не важен
				var xp = a + vn * f;            //спроецированная точка
				return xp;
			}

			//TODO реализовать
			public Vector3D Prognosed(double seconds)
			{
				return Now();
			}
		}
	}

}