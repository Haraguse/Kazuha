using System;
using System.Globalization;
using System.Text;
using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
/// An ARGB color.
/// </summary>
public readonly struct Color : IEquatable<Color>
{
	private const double byteToDouble = 1.0 / 255.0;

	/// <summary>
	/// Gets the Alpha component of the color.
	/// </summary>
	public byte A { get; }

	/// <summary>
	/// Gets the Red component of the color.
	/// </summary>
	public byte R { get; }

	/// <summary>
	/// Gets the Green component of the color.
	/// </summary>
	public byte G { get; }

	/// <summary>
	/// Gets the Blue component of the color.
	/// </summary>
	public byte B { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Color" /> struct.
	/// </summary>
	/// <param name="a">The alpha component.</param>
	/// <param name="r">The red component.</param>
	/// <param name="g">The green component.</param>
	/// <param name="b">The blue component.</param>
	public Color(byte a, byte r, byte g, byte b)
	{
		A = a;
		R = r;
		G = g;
		B = b;
	}

	/// <summary>
	/// Creates a <see cref="T:Avalonia.Media.Color" /> from alpha, red, green and blue components.
	/// </summary>
	/// <param name="a">The alpha component.</param>
	/// <param name="r">The red component.</param>
	/// <param name="g">The green component.</param>
	/// <param name="b">The blue component.</param>
	/// <returns>The color.</returns>
	public static Color FromArgb(byte a, byte r, byte g, byte b)
	{
		return new Color(a, r, g, b);
	}

	/// <summary>
	/// Creates a <see cref="T:Avalonia.Media.Color" /> from red, green and blue components.
	/// </summary>
	/// <param name="r">The red component.</param>
	/// <param name="g">The green component.</param>
	/// <param name="b">The blue component.</param>
	/// <returns>The color.</returns>
	public static Color FromRgb(byte r, byte g, byte b)
	{
		return new Color(byte.MaxValue, r, g, b);
	}

	/// <summary>
	/// Creates a <see cref="T:Avalonia.Media.Color" /> from an integer.
	/// </summary>
	/// <param name="value">The integer value.</param>
	/// <returns>The color.</returns>
	public static Color FromUInt32(uint value)
	{
		return new Color((byte)((value >> 24) & 0xFF), (byte)((value >> 16) & 0xFF), (byte)((value >> 8) & 0xFF), (byte)(value & 0xFF));
	}

	/// <summary>
	/// Parses a color string.
	/// </summary>
	/// <param name="s">The color string.</param>
	/// <returns>The <see cref="T:Avalonia.Media.Color" />.</returns>
	public static Color Parse(string s)
	{
		if (s == null)
		{
			throw new ArgumentNullException("s");
		}
		if (TryParse(s, out var color))
		{
			return color;
		}
		throw new FormatException("Invalid color string: '" + s + "'.");
	}

	/// <summary>
	/// Parses a color string.
	/// </summary>
	/// <param name="s">The color string.</param>
	/// <returns>The <see cref="T:Avalonia.Media.Color" />.</returns>
	public static Color Parse(ReadOnlySpan<char> s)
	{
		if (TryParse(s, out var color))
		{
			return color;
		}
		throw new FormatException("Invalid color string: '" + s.ToString() + "'.");
	}

	/// <summary>
	/// Parses a color string.
	/// </summary>
	/// <param name="s">The color string.</param>
	/// <param name="color">The parsed color</param>
	/// <returns>The status of the operation.</returns>
	public static bool TryParse(string? s, out Color color)
	{
		color = default(Color);
		if (string.IsNullOrEmpty(s))
		{
			return false;
		}
		if (s[0] == '#' && TryParseHexFormat(s.AsSpan(), out color))
		{
			return true;
		}
		if (s.Length >= 10 && (s[0] == 'r' || s[0] == 'R') && (s[1] == 'g' || s[1] == 'G') && (s[2] == 'b' || s[2] == 'B') && TryParseCssFormat(s, out color))
		{
			return true;
		}
		if (s.Length >= 10 && (s[0] == 'h' || s[0] == 'H') && (s[1] == 's' || s[1] == 'S') && (s[2] == 'l' || s[2] == 'L') && HslColor.TryParse(s, out var hslColor))
		{
			color = hslColor.ToRgb();
			return true;
		}
		if (s.Length >= 10 && (s[0] == 'h' || s[0] == 'H') && (s[1] == 's' || s[1] == 'S') && (s[2] == 'v' || s[2] == 'V') && HsvColor.TryParse(s, out var hsvColor))
		{
			color = hsvColor.ToRgb();
			return true;
		}
		KnownColor knownColor = KnownColors.GetKnownColor(s);
		if (knownColor != KnownColor.None)
		{
			color = knownColor.ToColor();
			return true;
		}
		return false;
	}

	/// <summary>
	/// Parses a color string.
	/// </summary>
	/// <param name="s">The color string.</param>
	/// <param name="color">The parsed color</param>
	/// <returns>The status of the operation.</returns>
	public static bool TryParse(ReadOnlySpan<char> s, out Color color)
	{
		if (s.Length == 0)
		{
			color = default(Color);
			return false;
		}
		if (s[0] == '#' && TryParseHexFormat(s, out color))
		{
			return true;
		}
		string s2 = s.ToString();
		if (s.Length >= 10 && (s[0] == 'r' || s[0] == 'R') && (s[1] == 'g' || s[1] == 'G') && (s[2] == 'b' || s[2] == 'B') && TryParseCssFormat(s2, out color))
		{
			return true;
		}
		if (s.Length >= 10 && (s[0] == 'h' || s[0] == 'H') && (s[1] == 's' || s[1] == 'S') && (s[2] == 'l' || s[2] == 'L') && HslColor.TryParse(s2, out var hslColor))
		{
			color = hslColor.ToRgb();
			return true;
		}
		if (s.Length >= 10 && (s[0] == 'h' || s[0] == 'H') && (s[1] == 's' || s[1] == 'S') && (s[2] == 'v' || s[2] == 'V') && HsvColor.TryParse(s2, out var hsvColor))
		{
			color = hsvColor.ToRgb();
			return true;
		}
		KnownColor knownColor = KnownColors.GetKnownColor(s2);
		if (knownColor != KnownColor.None)
		{
			color = knownColor.ToColor();
			return true;
		}
		color = default(Color);
		return false;
	}

	/// <summary>
	/// Parses the given span of characters representing a hex color value into a new <see cref="T:Avalonia.Media.Color" />.
	/// </summary>
	private static bool TryParseHexFormat(ReadOnlySpan<char> s, out Color color)
	{
		color = default(Color);
		ReadOnlySpan<char> input = s.Slice(1);
		if (input.Length == 3 || input.Length == 4)
		{
			Span<char> span = stackalloc char[2 * input.Length];
			for (int i = 0; i < input.Length; i++)
			{
				span[2 * i] = input[i];
				span[2 * i + 1] = input[i];
			}
			return TryParseCore(span, ref color);
		}
		return TryParseCore(input, ref color);
		static bool TryParseCore(ReadOnlySpan<char> span2, ref Color reference)
		{
			uint num = 0u;
			if (span2.Length == 6)
			{
				num = 4278190080u;
			}
			else if (span2.Length != 8)
			{
				return false;
			}
			if (!span2.TryParseUInt(NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
			{
				return false;
			}
			reference = FromUInt32(value | num);
			return true;
		}
	}

	/// <summary>
	/// Parses the given string representing a CSS color value into a new <see cref="T:Avalonia.Media.Color" />.
	/// </summary>
	private static bool TryParseCssFormat(string? s, out Color color)
	{
		bool flag = false;
		color = default(Color);
		if (s == null)
		{
			return false;
		}
		string text = s.Trim();
		if (text.Length == 0 || text.IndexOf(",", StringComparison.Ordinal) < 0)
		{
			return false;
		}
		if (text.Length >= 11 && text.StartsWith("rgba(", StringComparison.OrdinalIgnoreCase) && text.EndsWith(')'))
		{
			text = text.Substring(5, text.Length - 6);
			flag = true;
		}
		if (!flag && text.Length >= 10 && text.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase) && text.EndsWith(')'))
		{
			text = text.Substring(4, text.Length - 5);
			flag = true;
		}
		if (!flag)
		{
			return false;
		}
		string[] array = text.Split(',');
		byte outByte4;
		byte outByte5;
		byte outByte6;
		double outDouble;
		if (array.Length == 3)
		{
			if (InternalTryParseByte(array[0].AsSpan(), out var outByte) && InternalTryParseByte(array[1].AsSpan(), out var outByte2) && InternalTryParseByte(array[2].AsSpan(), out var outByte3))
			{
				color = new Color(byte.MaxValue, outByte, outByte2, outByte3);
				return true;
			}
		}
		else if (array.Length == 4 && InternalTryParseByte(array[0].AsSpan(), out outByte4) && InternalTryParseByte(array[1].AsSpan(), out outByte5) && InternalTryParseByte(array[2].AsSpan(), out outByte6) && InternalTryParseDouble(array[3].AsSpan(), out outDouble))
		{
			color = new Color((byte)Math.Round(outDouble * 255.0), outByte4, outByte5, outByte6);
			return true;
		}
		return false;
		static bool InternalTryParseByte(ReadOnlySpan<char> inString, out byte reference)
		{
			int num = inString.IndexOf("%".AsSpan(), StringComparison.Ordinal);
			if (num >= 0)
			{
				bool result = inString.Slice(0, num).TryParseDouble(NumberStyles.Number, CultureInfo.InvariantCulture, out var value);
				reference = (byte)Math.Round(value / 100.0 * 255.0);
				return result;
			}
			return inString.TryParseByte(NumberStyles.Number, CultureInfo.InvariantCulture, out reference);
		}
		static bool InternalTryParseDouble(ReadOnlySpan<char> inString, out double reference)
		{
			int num = inString.IndexOf("%".AsSpan(), StringComparison.Ordinal);
			if (num >= 0)
			{
				bool result = inString.Slice(0, num).TryParseDouble(NumberStyles.Number, CultureInfo.InvariantCulture, out var value);
				reference = value / 100.0;
				return result;
			}
			return inString.TryParseDouble(NumberStyles.Number, CultureInfo.InvariantCulture, out reference);
		}
	}

	/// <summary>
	/// Returns the string representation of the color.
	/// </summary>
	/// <returns>
	/// The string representation of the color.
	/// </returns>
	public override string ToString()
	{
		uint rgb = ToUInt32();
		return KnownColors.GetKnownColorName(rgb) ?? ("#" + rgb.ToString("x8", CultureInfo.InvariantCulture));
	}

	internal void ToString(StringBuilder builder)
	{
		uint num = ToUInt32();
		if (KnownColors.TryGetKnownColorName(num, out string name))
		{
			builder.Append(name);
			return;
		}
		builder.Append('#');
		builder.AppendFormat(CultureInfo.InvariantCulture, "{0:x8}", num);
	}

	/// <summary>
	/// Returns the integer representation of the color.
	/// </summary>
	/// <returns>
	/// The integer representation of the color.
	/// </returns>
	public uint ToUInt32()
	{
		return (uint)((A << 24) | (R << 16) | (G << 8) | B);
	}

	/// <summary>
	/// Returns the HSL color model equivalent of this RGB color.
	/// </summary>
	/// <returns>The HSL equivalent color.</returns>
	public HslColor ToHsl()
	{
		return ToHsl(R, G, B, A);
	}

	/// <summary>
	/// Returns the HSV color model equivalent of this RGB color.
	/// </summary>
	/// <returns>The HSV equivalent color.</returns>
	public HsvColor ToHsv()
	{
		return ToHsv(R, G, B, A);
	}

	/// <inheritdoc />
	public bool Equals(Color other)
	{
		if (A == other.A && R == other.R && G == other.G)
		{
			return B == other.B;
		}
		return false;
	}

	/// <inheritdoc />
	public override bool Equals(object? obj)
	{
		if (obj is Color other)
		{
			return Equals(other);
		}
		return false;
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return (((((A.GetHashCode() * 397) ^ R.GetHashCode()) * 397) ^ G.GetHashCode()) * 397) ^ B.GetHashCode();
	}

	/// <summary>
	/// Converts the given RGBA color component values to their HSL color equivalent.
	/// </summary>
	/// <param name="red">The Red component in the RGB color model.</param>
	/// <param name="green">The Green component in the RGB color model.</param>
	/// <param name="blue">The Blue component in the RGB color model.</param>
	/// <param name="alpha">The Alpha component.</param>
	/// <returns>A new <see cref="T:Avalonia.Media.HslColor" /> equivalent to the given RGBA values.</returns>
	public static HslColor ToHsl(byte red, byte green, byte blue, byte alpha = byte.MaxValue)
	{
		return ToHsl(1.0 / 255.0 * (double)(int)red, 1.0 / 255.0 * (double)(int)green, 1.0 / 255.0 * (double)(int)blue, 1.0 / 255.0 * (double)(int)alpha);
	}

	/// <summary>
	/// Converts the given RGBA color component values to their HSL color equivalent.
	/// </summary>
	/// <remarks>
	/// Warning: No bounds checks or clamping is done on the input component values.
	/// This method is for internal-use only and the caller must ensure bounds.
	/// </remarks>
	/// <param name="r">The Red component in the RGB color model within the range 0..1.</param>
	/// <param name="g">The Green component in the RGB color model within the range 0..1.</param>
	/// <param name="b">The Blue component in the RGB color model within the range 0..1.</param>
	/// <param name="a">The Alpha component in the RGB color model within the range 0..1.</param>
	/// <returns>A new <see cref="T:Avalonia.Media.HslColor" /> equivalent to the given RGBA values.</returns>
	internal static HslColor ToHsl(double r, double g, double b, double a = 1.0)
	{
		double num = ((!(r >= g)) ? ((g >= b) ? g : b) : ((r >= b) ? r : b));
		double num2 = ((!(r <= g)) ? ((g <= b) ? g : b) : ((r <= b) ? r : b));
		double num3 = num - num2;
		double num4 = ((num3 == 0.0) ? 0.0 : ((num == r) ? (((g - b) / num3 + 6.0) % 6.0) : ((num != g) ? (4.0 + (r - g) / num3) : (2.0 + (b - r) / num3))));
		double num5 = 0.5 * (num + num2);
		double saturation = ((num3 == 0.0) ? 0.0 : (num3 / (1.0 - Math.Abs(2.0 * num5 - 1.0))));
		return new HslColor(a, 60.0 * num4, saturation, num5, clampValues: false);
	}

	/// <summary>
	/// Converts the given RGBA color component values to their HSV color equivalent.
	/// </summary>
	/// <param name="red">The Red component in the RGB color model.</param>
	/// <param name="green">The Green component in the RGB color model.</param>
	/// <param name="blue">The Blue component in the RGB color model.</param>
	/// <param name="alpha">The Alpha component.</param>
	/// <returns>A new <see cref="T:Avalonia.Media.HsvColor" /> equivalent to the given RGBA values.</returns>
	public static HsvColor ToHsv(byte red, byte green, byte blue, byte alpha = byte.MaxValue)
	{
		return ToHsv(1.0 / 255.0 * (double)(int)red, 1.0 / 255.0 * (double)(int)green, 1.0 / 255.0 * (double)(int)blue, 1.0 / 255.0 * (double)(int)alpha);
	}

	/// <summary>
	/// Converts the given RGBA color component values to their HSV color equivalent.
	/// </summary>
	/// <remarks>
	/// Warning: No bounds checks or clamping is done on the input component values.
	/// This method is for internal-use only and the caller must ensure bounds.
	/// </remarks>
	/// <param name="r">The Red component in the RGB color model within the range 0..1.</param>
	/// <param name="g">The Green component in the RGB color model within the range 0..1.</param>
	/// <param name="b">The Blue component in the RGB color model within the range 0..1.</param>
	/// <param name="a">The Alpha component in the RGB color model within the range 0..1.</param>
	/// <returns>A new <see cref="T:Avalonia.Media.HsvColor" /> equivalent to the given RGBA values.</returns>
	internal static HsvColor ToHsv(double r, double g, double b, double a = 1.0)
	{
		double num = ((!(r >= g)) ? ((g >= b) ? g : b) : ((r >= b) ? r : b));
		double num2 = ((!(r <= g)) ? ((g <= b) ? g : b) : ((r <= b) ? r : b));
		double num3 = num;
		double num4 = num - num2;
		double num5;
		double saturation;
		if (num4 == 0.0)
		{
			num5 = 0.0;
			saturation = 0.0;
		}
		else
		{
			num5 = ((r == num) ? (60.0 * (g - b) / num4) : ((g != num) ? (240.0 + 60.0 * (r - g) / num4) : (120.0 + 60.0 * (b - r) / num4)));
			if (num5 < 0.0)
			{
				num5 += 360.0;
			}
			saturation = num4 / num3;
		}
		return new HsvColor(a, num5, saturation, num3, clampValues: false);
	}

	/// <summary>
	/// Indicates whether the values of two specified <see cref="T:Avalonia.Media.Color" /> objects are equal.
	/// </summary>
	/// <param name="left">The first object to compare.</param>
	/// <param name="right">The second object to compare.</param>
	/// <returns>True if left and right are equal; otherwise, false.</returns>
	public static bool operator ==(Color left, Color right)
	{
		return left.Equals(right);
	}

	/// <summary>
	/// Indicates whether the values of two specified <see cref="T:Avalonia.Media.Color" /> objects are not equal.
	/// </summary>
	/// <param name="left">The first object to compare.</param>
	/// <param name="right">The second object to compare.</param>
	/// <returns>True if left and right are not equal; otherwise, false.</returns>
	public static bool operator !=(Color left, Color right)
	{
		return !left.Equals(right);
	}
}
