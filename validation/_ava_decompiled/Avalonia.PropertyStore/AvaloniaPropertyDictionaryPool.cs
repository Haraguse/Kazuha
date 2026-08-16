using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.PropertyStore;

internal static class AvaloniaPropertyDictionaryPool<TValue>
{
	private const int MaxPoolSize = 4;

	private static readonly Stack<AvaloniaPropertyDictionary<TValue>> _pool = new Stack<AvaloniaPropertyDictionary<TValue>>();

	public static AvaloniaPropertyDictionary<TValue> Get()
	{
		if (_pool.Count != 0)
		{
			return _pool.Pop();
		}
		return new AvaloniaPropertyDictionary<TValue>();
	}

	public static void Release(AvaloniaPropertyDictionary<TValue> dictionary)
	{
		if (_pool.Count < 4)
		{
			dictionary.Clear();
			_pool.Push(dictionary);
		}
	}
}
