using System;

namespace Avalonia.Utilities;

internal static class ByteSizeHelper
{
	private const string formatTemplateSeparated = "{0}{1:0.#} {2}";

	private const string formatTemplate = "{0}{1:0.#}{2}";

	private static readonly string[] Prefixes = new string[9] { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

	public static string ToString(ulong bytes, bool separate)
	{
		if (bytes == 0L)
		{
			return string.Format(separate ? "{0}{1:0.#} {2}" : "{0}{1:0.#}{2}", null, 0, Prefixes[0]);
		}
		double num = Math.Abs((double)bytes);
		int num2 = (int)Math.Log(num, 1000.0);
		int num3 = ((num2 >= Prefixes.Length) ? (Prefixes.Length - 1) : num2);
		double num4 = num / Math.Pow(1000.0, num3);
		return string.Format("{0}{1:0.#}{2}", (bytes < 0) ? "-" : null, num4, Prefixes[num3]);
	}
}
