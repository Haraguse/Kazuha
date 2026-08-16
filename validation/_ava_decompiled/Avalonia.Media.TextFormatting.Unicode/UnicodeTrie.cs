using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Media.TextFormatting.Unicode;

internal ref struct UnicodeTrie(ReadOnlySpan<uint> data, int highStart, uint errorValue)
{
	public ReadOnlySpan<uint> Data { get; } = data;

	public int HighStart { get; } = highStart;

	public uint ErrorValue { get; } = errorValue;

	/// <summary>
	/// Get the value for a code point as stored in the trie.
	/// </summary>
	/// <param name="codePoint">The code point.</param>
	/// <returns>The <see cref="T:System.UInt32" /> value.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint Get(uint codePoint)
	{
		ref uint reference = ref MemoryMarshal.GetReference(Data);
		if (codePoint > 56319)
		{
			if (codePoint <= 65535)
			{
				goto IL_0026;
			}
		}
		else if (codePoint < 55296)
		{
			goto IL_0026;
		}
		bool flag = false;
		goto IL_002c;
		IL_002c:
		if (flag)
		{
			uint num = Data[(int)(codePoint >> 5)];
			num = (num << 2) + (codePoint & 0x1F);
			return Unsafe.Add(ref reference, (nint)num);
		}
		if (codePoint <= 65535)
		{
			uint num = Data[(int)(2048 + (codePoint - 55296 >> 5))];
			num = (num << 2) + (codePoint & 0x1F);
			return Unsafe.Add(ref reference, (nint)num);
		}
		if (codePoint < HighStart)
		{
			uint num = 2080 + (codePoint >> 11);
			num = Data[(int)num];
			num += (codePoint >> 5) & 0x3F;
			num = Data[(int)num];
			num = (num << 2) + (codePoint & 0x1F);
			return Unsafe.Add(ref reference, (nint)num);
		}
		if (codePoint <= 1114111)
		{
			return Data[Data.Length - 4];
		}
		return ErrorValue;
		IL_0026:
		flag = true;
		goto IL_002c;
	}
}
