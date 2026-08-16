using System;
using System.Text;

namespace Avalonia.Utilities;

/// <summary>Provide a cached reusable instance of stringbuilder per thread.</summary>
internal static class StringBuilderCache
{
	internal const int MaxBuilderSize = 360;

	private const int DefaultCapacity = 16;

	[ThreadStatic]
	private static StringBuilder t_cachedInstance;

	/// <summary>Get a StringBuilder for the specified capacity.</summary>
	/// <remarks>If a StringBuilder of an appropriate size is cached, it will be returned and the cache emptied.</remarks>
	public static StringBuilder Acquire(int capacity = 16)
	{
		if (capacity <= 360)
		{
			StringBuilder stringBuilder = t_cachedInstance;
			if (stringBuilder != null && capacity <= stringBuilder.Capacity)
			{
				t_cachedInstance = null;
				stringBuilder.Clear();
				return stringBuilder;
			}
		}
		return new StringBuilder(capacity);
	}

	/// <summary>Place the specified builder in the cache if it is not too big.</summary>
	public static void Release(StringBuilder sb)
	{
		if (sb.Capacity <= 360)
		{
			t_cachedInstance = sb;
		}
	}

	/// <summary>ToString() the stringbuilder, Release it to the cache, and return the resulting string.</summary>
	public static string GetStringAndRelease(StringBuilder sb)
	{
		string result = sb.ToString();
		Release(sb);
		return result;
	}
}
