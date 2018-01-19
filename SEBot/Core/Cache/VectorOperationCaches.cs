using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		public class VectorOperationCaches
		{
			//TODO rewrite - bad hash - cycle start
			public class Pair<T1, T2>
			{
				public T1 First { get; set; }
				public T2 Second { get; set; }

				public override bool Equals(object obj)
				{
					var pair = obj as Pair<T1, T2>;
					return pair != null &&
						   EqualityComparer<T1>.Default.Equals(First, pair.First) &&
						   EqualityComparer<T2>.Default.Equals(Second, pair.Second);
				}

				public override int GetHashCode()
				{
					var hashCode = 0x0;
					hashCode ^= EqualityComparer<T1>.Default.GetHashCode(First);
					hashCode ^= EqualityComparer<T2>.Default.GetHashCode(Second);
					return hashCode;
				}
			}

			public readonly Cache<Vector3D, double> LenCache;

			public readonly Cache<Vector3D, Vector3D> NormCache;

			public readonly Cache<Pair<Vector3D, Vector3D>, double> ProjectionCache;

			private readonly ShipSystems _ship;

			private readonly Cache<Vector3D, Vector3D> _toGlobalCache;

			private readonly Cache<Vector3D, Vector3D> _toLocalCache;

			public VectorOperationCaches(ShipSystems ship)
			{
				_ship = ship;
				_toGlobalCache = new Cache<Vector3D, Vector3D>(
					local =>
					{
						var global = _ship.TravelSystem.ToGlobalCoordinate(local);
						_toLocalCache[global] = local;
						return global;
					});
				_toLocalCache = new Cache<Vector3D, Vector3D>(
					global =>
					{
						var local = _ship.TravelSystem.ToLocalCoordinate(global);
						_toGlobalCache[local] = global;
						return local;
					});
				LenCache = new Cache<Vector3D, double>(
					v => v.IsValid() ? v.Length() : double.NaN);
				NormCache = new Cache<Vector3D, Vector3D>(Vector3D.Normalize);
				ProjectionCache = new Cache<Pair<Vector3D, Vector3D>, double>(
					tuple => (tuple.First * tuple.Second).Sum);//Only for Normalize(tuple.Item2)
			}

			public double Length(Vector3D vector)
			{
				Log.Log($"Length({vector})", nameof(VectorOperationCaches));
				return LenCache[vector];
			}

			public Vector3D Normalize(Vector3D vector)
			{
				Log.Log($"Normalize({vector})", nameof(VectorOperationCaches));
				return NormCache[vector];
			}

			public Vector3D ToGlobal(Vector3D local)
			{
				if (!local.IsValid()) throw new Exception($"Value should be valid. {nameof(local)}");
				Log.Log($"ToGlobal({local})", nameof(VectorOperationCaches));
				return _toGlobalCache[local];
			}

			public Vector3D ToLocal(Vector3D global)
			{
				if (!global.IsValid()) throw new Exception($"Value should be valid. {nameof(global)}");
				Log.Log($"ToLocal({global})", nameof(VectorOperationCaches));
				return _toLocalCache[global];
			}

			/// <summary>
			/// TODO: Now without cache
			/// </summary>
			/// <param name="vector"></param>
			/// <param name="direction"></param>
			/// <returns></returns>
			public double Projection(Vector3D vector, Vector3D direction)
			{
				var norm = Normalize(direction);
				return Vector3D.Dot(vector, norm);
				//return ProjectionCache[new Pair<Vector3D, Vector3D> { First = vector, Second = norm }];
			}
		}
	}
}