using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Media.Immutable;
using Avalonia.SourceGenerator;

namespace Avalonia.Media;

internal static class KnownColors
{
	private static readonly IReadOnlyDictionary<string, KnownColor> _knownColorNames;

	private static readonly IReadOnlyDictionary<uint, string> _knownColors;

	private static readonly Dictionary<KnownColor, IImmutableSolidColorBrush> _knownBrushes;

	[GenerateEnumValueDictionary]
	private static Dictionary<string, KnownColor> GetKnownColors()
	{
		return new Dictionary<string, KnownColor>
		{
			{
				"None",
				KnownColor.None
			},
			{
				"AliceBlue",
				KnownColor.AliceBlue
			},
			{
				"AntiqueWhite",
				KnownColor.AntiqueWhite
			},
			{
				"Aqua",
				KnownColor.Aqua
			},
			{
				"Aquamarine",
				KnownColor.Aquamarine
			},
			{
				"Azure",
				KnownColor.Azure
			},
			{
				"Beige",
				KnownColor.Beige
			},
			{
				"Bisque",
				KnownColor.Bisque
			},
			{
				"Black",
				KnownColor.Black
			},
			{
				"BlanchedAlmond",
				KnownColor.BlanchedAlmond
			},
			{
				"Blue",
				KnownColor.Blue
			},
			{
				"BlueViolet",
				KnownColor.BlueViolet
			},
			{
				"Brown",
				KnownColor.Brown
			},
			{
				"BurlyWood",
				KnownColor.BurlyWood
			},
			{
				"CadetBlue",
				KnownColor.CadetBlue
			},
			{
				"Chartreuse",
				KnownColor.Chartreuse
			},
			{
				"Chocolate",
				KnownColor.Chocolate
			},
			{
				"Coral",
				KnownColor.Coral
			},
			{
				"CornflowerBlue",
				KnownColor.CornflowerBlue
			},
			{
				"Cornsilk",
				KnownColor.Cornsilk
			},
			{
				"Crimson",
				KnownColor.Crimson
			},
			{
				"Cyan",
				KnownColor.Aqua
			},
			{
				"DarkBlue",
				KnownColor.DarkBlue
			},
			{
				"DarkCyan",
				KnownColor.DarkCyan
			},
			{
				"DarkGoldenrod",
				KnownColor.DarkGoldenrod
			},
			{
				"DarkGray",
				KnownColor.DarkGray
			},
			{
				"DarkGreen",
				KnownColor.DarkGreen
			},
			{
				"DarkKhaki",
				KnownColor.DarkKhaki
			},
			{
				"DarkMagenta",
				KnownColor.DarkMagenta
			},
			{
				"DarkOliveGreen",
				KnownColor.DarkOliveGreen
			},
			{
				"DarkOrange",
				KnownColor.DarkOrange
			},
			{
				"DarkOrchid",
				KnownColor.DarkOrchid
			},
			{
				"DarkRed",
				KnownColor.DarkRed
			},
			{
				"DarkSalmon",
				KnownColor.DarkSalmon
			},
			{
				"DarkSeaGreen",
				KnownColor.DarkSeaGreen
			},
			{
				"DarkSlateBlue",
				KnownColor.DarkSlateBlue
			},
			{
				"DarkSlateGray",
				KnownColor.DarkSlateGray
			},
			{
				"DarkTurquoise",
				KnownColor.DarkTurquoise
			},
			{
				"DarkViolet",
				KnownColor.DarkViolet
			},
			{
				"DeepPink",
				KnownColor.DeepPink
			},
			{
				"DeepSkyBlue",
				KnownColor.DeepSkyBlue
			},
			{
				"DimGray",
				KnownColor.DimGray
			},
			{
				"DodgerBlue",
				KnownColor.DodgerBlue
			},
			{
				"Firebrick",
				KnownColor.Firebrick
			},
			{
				"FloralWhite",
				KnownColor.FloralWhite
			},
			{
				"ForestGreen",
				KnownColor.ForestGreen
			},
			{
				"Fuchsia",
				KnownColor.Fuchsia
			},
			{
				"Gainsboro",
				KnownColor.Gainsboro
			},
			{
				"GhostWhite",
				KnownColor.GhostWhite
			},
			{
				"Gold",
				KnownColor.Gold
			},
			{
				"Goldenrod",
				KnownColor.Goldenrod
			},
			{
				"Gray",
				KnownColor.Gray
			},
			{
				"Green",
				KnownColor.Green
			},
			{
				"GreenYellow",
				KnownColor.GreenYellow
			},
			{
				"Honeydew",
				KnownColor.Honeydew
			},
			{
				"HotPink",
				KnownColor.HotPink
			},
			{
				"IndianRed",
				KnownColor.IndianRed
			},
			{
				"Indigo",
				KnownColor.Indigo
			},
			{
				"Ivory",
				KnownColor.Ivory
			},
			{
				"Khaki",
				KnownColor.Khaki
			},
			{
				"Lavender",
				KnownColor.Lavender
			},
			{
				"LavenderBlush",
				KnownColor.LavenderBlush
			},
			{
				"LawnGreen",
				KnownColor.LawnGreen
			},
			{
				"LemonChiffon",
				KnownColor.LemonChiffon
			},
			{
				"LightBlue",
				KnownColor.LightBlue
			},
			{
				"LightCoral",
				KnownColor.LightCoral
			},
			{
				"LightCyan",
				KnownColor.LightCyan
			},
			{
				"LightGoldenrodYellow",
				KnownColor.LightGoldenrodYellow
			},
			{
				"LightGreen",
				KnownColor.LightGreen
			},
			{
				"LightGray",
				KnownColor.LightGray
			},
			{
				"LightPink",
				KnownColor.LightPink
			},
			{
				"LightSalmon",
				KnownColor.LightSalmon
			},
			{
				"LightSeaGreen",
				KnownColor.LightSeaGreen
			},
			{
				"LightSkyBlue",
				KnownColor.LightSkyBlue
			},
			{
				"LightSlateGray",
				KnownColor.LightSlateGray
			},
			{
				"LightSteelBlue",
				KnownColor.LightSteelBlue
			},
			{
				"LightYellow",
				KnownColor.LightYellow
			},
			{
				"Lime",
				KnownColor.Lime
			},
			{
				"LimeGreen",
				KnownColor.LimeGreen
			},
			{
				"Linen",
				KnownColor.Linen
			},
			{
				"Magenta",
				KnownColor.Fuchsia
			},
			{
				"Maroon",
				KnownColor.Maroon
			},
			{
				"MediumAquamarine",
				KnownColor.MediumAquamarine
			},
			{
				"MediumBlue",
				KnownColor.MediumBlue
			},
			{
				"MediumOrchid",
				KnownColor.MediumOrchid
			},
			{
				"MediumPurple",
				KnownColor.MediumPurple
			},
			{
				"MediumSeaGreen",
				KnownColor.MediumSeaGreen
			},
			{
				"MediumSlateBlue",
				KnownColor.MediumSlateBlue
			},
			{
				"MediumSpringGreen",
				KnownColor.MediumSpringGreen
			},
			{
				"MediumTurquoise",
				KnownColor.MediumTurquoise
			},
			{
				"MediumVioletRed",
				KnownColor.MediumVioletRed
			},
			{
				"MidnightBlue",
				KnownColor.MidnightBlue
			},
			{
				"MintCream",
				KnownColor.MintCream
			},
			{
				"MistyRose",
				KnownColor.MistyRose
			},
			{
				"Moccasin",
				KnownColor.Moccasin
			},
			{
				"NavajoWhite",
				KnownColor.NavajoWhite
			},
			{
				"Navy",
				KnownColor.Navy
			},
			{
				"OldLace",
				KnownColor.OldLace
			},
			{
				"Olive",
				KnownColor.Olive
			},
			{
				"OliveDrab",
				KnownColor.OliveDrab
			},
			{
				"Orange",
				KnownColor.Orange
			},
			{
				"OrangeRed",
				KnownColor.OrangeRed
			},
			{
				"Orchid",
				KnownColor.Orchid
			},
			{
				"PaleGoldenrod",
				KnownColor.PaleGoldenrod
			},
			{
				"PaleGreen",
				KnownColor.PaleGreen
			},
			{
				"PaleTurquoise",
				KnownColor.PaleTurquoise
			},
			{
				"PaleVioletRed",
				KnownColor.PaleVioletRed
			},
			{
				"PapayaWhip",
				KnownColor.PapayaWhip
			},
			{
				"PeachPuff",
				KnownColor.PeachPuff
			},
			{
				"Peru",
				KnownColor.Peru
			},
			{
				"Pink",
				KnownColor.Pink
			},
			{
				"Plum",
				KnownColor.Plum
			},
			{
				"PowderBlue",
				KnownColor.PowderBlue
			},
			{
				"Purple",
				KnownColor.Purple
			},
			{
				"Red",
				KnownColor.Red
			},
			{
				"RosyBrown",
				KnownColor.RosyBrown
			},
			{
				"RoyalBlue",
				KnownColor.RoyalBlue
			},
			{
				"SaddleBrown",
				KnownColor.SaddleBrown
			},
			{
				"Salmon",
				KnownColor.Salmon
			},
			{
				"SandyBrown",
				KnownColor.SandyBrown
			},
			{
				"SeaGreen",
				KnownColor.SeaGreen
			},
			{
				"SeaShell",
				KnownColor.SeaShell
			},
			{
				"Sienna",
				KnownColor.Sienna
			},
			{
				"Silver",
				KnownColor.Silver
			},
			{
				"SkyBlue",
				KnownColor.SkyBlue
			},
			{
				"SlateBlue",
				KnownColor.SlateBlue
			},
			{
				"SlateGray",
				KnownColor.SlateGray
			},
			{
				"Snow",
				KnownColor.Snow
			},
			{
				"SpringGreen",
				KnownColor.SpringGreen
			},
			{
				"SteelBlue",
				KnownColor.SteelBlue
			},
			{
				"Tan",
				KnownColor.Tan
			},
			{
				"Teal",
				KnownColor.Teal
			},
			{
				"Thistle",
				KnownColor.Thistle
			},
			{
				"Tomato",
				KnownColor.Tomato
			},
			{
				"Transparent",
				KnownColor.Transparent
			},
			{
				"Turquoise",
				KnownColor.Turquoise
			},
			{
				"Violet",
				KnownColor.Violet
			},
			{
				"Wheat",
				KnownColor.Wheat
			},
			{
				"White",
				KnownColor.White
			},
			{
				"WhiteSmoke",
				KnownColor.WhiteSmoke
			},
			{
				"Yellow",
				KnownColor.Yellow
			},
			{
				"YellowGreen",
				KnownColor.YellowGreen
			}
		};
	}

	static KnownColors()
	{
		Dictionary<string, KnownColor> dictionary = new Dictionary<string, KnownColor>(StringComparer.OrdinalIgnoreCase);
		Dictionary<uint, string> dictionary2 = new Dictionary<uint, string>();
		foreach (KeyValuePair<string, KnownColor> knownColor in GetKnownColors())
		{
			KnownColor value = knownColor.Value;
			if (value != KnownColor.None)
			{
				dictionary.Add(knownColor.Key, value);
				if (!dictionary2.ContainsKey((uint)value))
				{
					dictionary2.Add((uint)value, knownColor.Key);
				}
			}
		}
		_knownColorNames = dictionary;
		_knownColors = dictionary2;
		_knownBrushes = new Dictionary<KnownColor, IImmutableSolidColorBrush>();
	}

	/// <summary>
	/// Attempts to resolve a color name string to a solid color brush.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Returns a cached immutable brush if the name matches one of the predefined
	/// <see cref="T:Avalonia.Media.KnownColor" /> values. Repeated calls with the same color name will
	/// return the same brush instance.
	/// </para>
	/// <para>
	/// The lookup is case-sensitive and depends on the set of predefined known colors.
	/// </para>
	/// </remarks>
	/// <param name="s">
	/// The color name to look up (for example, <c>"Red"</c> or <c>"CornflowerBlue"</c>).
	/// </param>
	/// <returns>
	/// An <see cref="T:Avalonia.Media.ISolidColorBrush" /> corresponding to the specified name,
	/// or <c>null</c> if the color name is not recognized.
	/// </returns>
	public static ISolidColorBrush? GetKnownBrush(string s)
	{
		KnownColor knownColor = GetKnownColor(s);
		if (knownColor == KnownColor.None)
		{
			return null;
		}
		return knownColor.ToBrush();
	}

	/// <summary>
	/// Attempts to resolve a color name string to a <see cref="T:Avalonia.Media.KnownColor" /> value.
	/// </summary>
	/// <remarks>
	/// The lookup is case-sensitive and depends on the set of predefined known colors.
	/// </remarks>
	/// <param name="s">
	/// The color name to look up (for example, <c>Red</c> or <c>CornflowerBlue</c>).
	/// </param>
	/// <returns>
	/// A <see cref="T:Avalonia.Media.KnownColor" /> value if the name is recognized; otherwise <see cref="F:Avalonia.Media.KnownColor.None" />.
	/// </returns>
	public static KnownColor GetKnownColor(string s)
	{
		if (_knownColorNames.TryGetValue(s, out var value))
		{
			return value;
		}
		return KnownColor.None;
	}

	public static string? GetKnownColorName(uint rgb)
	{
		if (!_knownColors.TryGetValue(rgb, out string value))
		{
			return null;
		}
		return value;
	}

	internal static bool TryGetKnownColorName(uint rgb, [NotNullWhen(true)] out string? name)
	{
		return _knownColors.TryGetValue(rgb, out name);
	}

	public static Color ToColor(this KnownColor color)
	{
		return Color.FromUInt32((uint)color);
	}

	/// <summary>
	/// Converts a <see cref="T:Avalonia.Media.KnownColor" /> value to an immutable solid color brush.
	/// </summary>
	/// <remarks>
	/// This method maintains an internal cache of brushes to avoid unnecessary allocations.
	/// If the same <paramref name="color" /> is requested multiple times, the same immutable brush
	/// instance is returned.
	/// </remarks>
	/// <param name="color">The <see cref="T:Avalonia.Media.KnownColor" /> to convert.</param>
	/// <returns>
	/// An <see cref="T:Avalonia.Media.IImmutableSolidColorBrush" /> instance representing the specified color.
	/// Brushes created from known colors are cached and reused for efficiency.
	/// </returns>
	public static IImmutableSolidColorBrush ToBrush(this KnownColor color)
	{
		lock (_knownBrushes)
		{
			if (!_knownBrushes.TryGetValue(color, out IImmutableSolidColorBrush value))
			{
				value = new ImmutableSolidColorBrush(color.ToColor());
				_knownBrushes.Add(color, value);
			}
			return value;
		}
	}
}
