using System;

namespace Avalonia.Media.TextFormatting.Unicode;

internal class Utf16Utils
{
	public static int CharacterOffsetToStringOffset(string s, int off, bool throwOnOutOfRange)
	{
		if (off == 0)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < s.Length; i++)
		{
			if (num == off)
			{
				return i;
			}
			if (!char.IsSurrogatePair(s, i))
			{
				num++;
			}
		}
		if (throwOnOutOfRange)
		{
			throw new IndexOutOfRangeException();
		}
		return s.Length;
	}
}
