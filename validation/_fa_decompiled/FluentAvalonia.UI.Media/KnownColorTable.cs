using System;
using System.Collections.Generic;

namespace FluentAvalonia.UI.Media;

internal static class KnownColorTable
{
	private static Dictionary<Color2, string> ColorTable;

	public static string GetColorName(Color2 c)
	{
		InitColorTable();
		if (ColorTable.TryGetValue(c, out var value))
		{
			return value;
		}
		return "";
	}

	public static Color2 FromColorName(string name)
	{
		foreach (KeyValuePair<Color2, string> item in ColorTable)
		{
			if (name.Equals(item.Value, StringComparison.OrdinalIgnoreCase))
			{
				return item.Key;
			}
		}
		return Color2.Empty;
	}

	private static void InitColorTable()
	{
		if (ColorTable != null)
		{
			return;
		}
		KnownColor[] values = Enum.GetValues<KnownColor>();
		string[] names = Enum.GetNames<KnownColor>();
		ColorTable = new Dictionary<Color2, string>(values.Length);
		for (int i = 0; i < values.Length; i++)
		{
			Color2 key = Color2.FromUInt((uint)values[i]);
			if (!ColorTable.ContainsKey(key))
			{
				ColorTable.Add(key, names[i]);
			}
		}
	}
}
