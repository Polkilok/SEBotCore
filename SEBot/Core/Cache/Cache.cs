using System;
using System.Collections.Generic;

namespace SEBot
{
	public sealed partial class Program
	{
		public class Cache<TKey, TValue>
		{
			private readonly Dictionary<TKey, TValue> _cache;

			public readonly Func<TKey, TValue> ConvertFunc;

			public Cache(Func<TKey, TValue> convertFunc)
			{
				if (convertFunc == null)
					throw new Exception($"ArgumentNull {nameof(convertFunc)}");
				ConvertFunc = convertFunc;
				_cache = new Dictionary<TKey, TValue>();
			}

			public TValue this[TKey key]
			{
				get
				{
					// ReSharper disable once InlineOutVariableDeclaration
					TValue val;
					if (_cache.TryGetValue(key, out val))
						return val;
					_cache.Add(key, ConvertFunc.Invoke(key));
					return this[key];//for the Great Recursion!
				}
				set
				{
					if (_cache.ContainsKey(key))
						_cache[key] = value;
					else
						_cache.Add(key, value);
				}
			}
		}
	}
}