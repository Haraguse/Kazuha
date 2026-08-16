using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting;

internal static class FormattingBufferHelper
{
	private const long MaxKeptBufferSizeInBytes = 1048576L;

	public static void ClearThenResetIfTooLarge<T>(ref ArrayBuilder<T> arrayBuilder)
	{
		arrayBuilder.Clear();
		if (IsBufferTooLarge<T>((uint)arrayBuilder.Capacity))
		{
			arrayBuilder = default(ArrayBuilder<T>);
		}
	}

	public static void ClearThenResetIfTooLarge<T>(List<T> list)
	{
		list.Clear();
		if (IsBufferTooLarge<T>((uint)list.Capacity))
		{
			list.TrimExcess();
		}
	}

	public static void ClearThenResetIfTooLarge<T>(Stack<T> stack)
	{
		uint capacity = RoundUpToPowerOf2((uint)stack.Count);
		stack.Clear();
		if (IsBufferTooLarge<T>(capacity))
		{
			stack.TrimExcess();
		}
	}

	public static void ClearThenResetIfTooLarge<TKey, TValue>(ref Dictionary<TKey, TValue> dictionary) where TKey : notnull
	{
		uint capacity = RoundUpToPowerOf2((uint)dictionary.Count);
		dictionary.Clear();
		if (IsBufferTooLarge<KeyValuePair<TKey, TValue>>(capacity))
		{
			dictionary.TrimExcess();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsBufferTooLarge<T>(uint capacity)
	{
		return (long)(uint)Unsafe.SizeOf<T>() * (long)capacity > 1048576;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint RoundUpToPowerOf2(uint value)
	{
		return BitOperations.RoundUpToPowerOf2(value);
	}
}
