using System;
using System.Globalization;
using System.Text;
using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
/// Defines a color using the hue/saturation/lightness (HSL) model.
/// This uses a cylindrical-coordinate representation of a color.
/// </summary>
public readonly struct HslColor : IEquatable<HslColor>
{
	/// <inheritdoc cref="P:Avalonia.Media.HsvColor.A" />
	public double A { get; }

	/// <inheritdoc cref="P:Avalonia.Media.HsvColor.H" />
	public double H { get; }

	/// <inheritdoc cref="P:Avalonia.Media.HsvColor.S" />
	public double S { get; }

	/// <summary>
	/// Gets the Lightness component in the range from 0..1 (percentage).
	/// </summary>
	/// <remarks>
	/// <list type="bullet">
	///   <item>0 is fully black.</item>
	///   <item>1 is fully white.</item>
	/// </list>
	/// </remarks>
	public double L { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.HslColor" /> struct.
	/// </summary>
	/// <param name="alpha">The Alpha (transparency) component in the range from 0..1.</param>
	/// <param name="hue">The Hue component in the range from 0..360.
	/// Note that 360 is equivalent to 0 and will be adjusted automatically.</param>
	/// <param name="saturation">The Saturation component in the range from 0..1.</param>
	/// <param name="lightness">The Lightness component in the range from 0..1.</param>
	public HslColor(double alpha, double hue, double saturation, double lightness)
	{
		A = MathUtilities.Clamp(alpha, 0.0, 1.0);
		H = MathUtilities.Clamp(hue, 0.0, 360.0);
		S = MathUtilities.Clamp(saturation, 0.0, 1.0);
		L = MathUtilities.Clamp(lightness, 0.0, 1.0);
		H = ((H == 360.0) ? 0.0 : H);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.HslColor" /> struct.
	/// </summary>
	/// <remarks>
	/// This constructor exists only for internal use where performance is critical.
	/// Whether or not the component values are in the correct ranges must be known.
	/// </remarks>
	/// <param name="alpha">The Alpha (transparency) component in the range from 0..1.</param>
	/// <param name="hue">The Hue component in the range from 0..360.
	/// Note that 360 is equivalent to 0 and will be adjusted automatically.</param>
	/// <param name="saturation">The Saturation component in the range from 0..1.</param>
	/// <param name="lightness">The Lightness component in the range from 0..1.</param>
	/// <param name="clampValues">Whether to clamp component values to their required ranges.</param>
	internal HslColor(double alpha, double hue, double saturation, double lightness, bool clampValues)
	{
		if (clampValues)
		{
			A = MathUtilities.Clamp(alpha, 0.0, 1.0);
			H = MathUtilities.Clamp(hue, 0.0, 360.0);
			S = MathUtilities.Clamp(saturation, 0.0, 1.0);
			L = MathUtilities.Clamp(lightness, 0.0, 1.0);
			H = ((H == 360.0) ? 0.0 : H);
		}
		else
		{
			A = alpha;
			H = hue;
			S = saturation;
			L = lightness;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.HslColor" /> struct.
	/// </summary>
	/// <param name="color">The RGB color to convert to HSL.</param>
	public HslColor(Color color)
	{
		HslColor hslColor = color.ToHsl();
		A = hslColor.A;
		H = hslColor.H;
		S = hslColor.S;
		L = hslColor.L;
	}

	/// <inheritdoc />
	public bool Equals(HslColor other)
	{
		if (other.A == A && other.H == H && other.S == S)
		{
			return other.L == L;
		}
		return false;
	}

	/// <inheritdoc />
	public override bool Equals(object? obj)
	{
		if (obj is HslColor other)
		{
			return Equals(other);
		}
		return false;
	}

	/// <summary>
	/// Gets a hashcode for this object.
	/// Hashcode is not guaranteed to be unique.
	/// </summary>
	/// <returns>The hashcode for this object.</returns>
	public override int GetHashCode()
	{
		return (((((A.GetHashCode() * 397) ^ H.GetHashCode()) * 397) ^ S.GetHashCode()) * 397) ^ L.GetHashCode();
	}

	/// <summary>
	/// Returns the RGB color model equivalent of this HSL color.
	/// </summary>
	/// <returns>The RGB equivalent color.</returns>
	public Color ToRgb()
	{
		return ToRgb(H, S, L, A);
	}

	/// <summary>
	/// Returns the HSV color model equivalent of this HSL color.
	/// </summary>
	/// <returns>The HSV equivalent color.</returns>
	public HsvColor ToHsv()
	{
		return ToHsv(H, S, L, A);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		StringBuilder stringBuilder = StringBuilderCache.Acquire();
		stringBuilder.Append("hsva(");
		stringBuilder.Append(H.ToString(CultureInfo.InvariantCulture));
		stringBuilder.Append(", ");
		stringBuilder.Append(S.ToString(CultureInfo.InvariantCulture));
		stringBuilder.Append(", ");
		stringBuilder.Append(L.ToString(CultureInfo.InvariantCulture));
		stringBuilder.Append(", ");
		stringBuilder.Append(A.ToString(CultureInfo.InvariantCulture));
		stringBuilder.Append(')');
		return StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	/// <summary>
	/// Parses an HSL color string.
	/// </summary>
	/// <param name="s">The HSL color string to parse.</param>
	/// <returns>The parsed <see cref="T:Avalonia.Media.HslColor" />.</returns>
	public static HslColor Parse(string s)
	{
		if (s == null)
		{
			throw new ArgumentNullException("s");
		}
		if (TryParse(s, out var hslColor))
		{
			return hslColor;
		}
		throw new FormatException("Invalid HSL color string: '" + s + "'.");
	}

	/// <summary>
	/// Parses an HSL color string.
	/// </summary>
	/// <param name="s">The HSL color string to parse.</param>
	/// <param name="hslColor">The parsed <see cref="T:Avalonia.Media.HslColor" />.</param>
	/// <returns>True if parsing was successful; otherwise, false.</returns>
	public static bool TryParse(string? s, out HslColor hslColor)
	{
		bool flag = false;
		hslColor = default(HslColor);
		if (s == null)
		{
			return false;
		}
		string text = s.Trim();
		if (text.Length == 0 || text.IndexOf(",", StringComparison.Ordinal) < 0)
		{
			return false;
		}
		if (text.Length >= 11 && text.StartsWith("hsla(", StringComparison.OrdinalIgnoreCase) && text.EndsWith(')'))
		{
			text = text.Substring(5, text.Length - 6);
			flag = true;
		}
		if (!flag && text.Length >= 10 && text.StartsWith("hsl(", StringComparison.OrdinalIgnoreCase) && text.EndsWith(')'))
		{
			text = text.Substring(4, text.Length - 5);
			flag = true;
		}
		if (!flag)
		{
			return false;
		}
		string[] array = text.Split(',');
		double value2;
		double outDouble3;
		double outDouble4;
		double outDouble5;
		if (array.Length == 3)
		{
			if (array[0].AsSpan().TryParseDouble(NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && TryInternalParse(array[1].AsSpan(), out var outDouble) && TryInternalParse(array[2].AsSpan(), out var outDouble2))
			{
				hslColor = new HslColor(1.0, value, outDouble, outDouble2);
				return true;
			}
		}
		else if (array.Length == 4 && array[0].AsSpan().TryParseDouble(NumberStyles.Number, CultureInfo.InvariantCulture, out value2) && TryInternalParse(array[1].AsSpan(), out outDouble3) && TryInternalParse(array[2].AsSpan(), out outDouble4) && TryInternalParse(array[3].AsSpan(), out outDouble5))
		{
			hslColor = new HslColor(outDouble5, value2, outDouble3, outDouble4);
			return true;
		}
		return false;
		static bool TryInternalParse(ReadOnlySpan<char> inString, out double reference)
		{
			int num = inString.IndexOf("%".AsSpan(), StringComparison.Ordinal);
			if (num >= 0)
			{
				bool result = inString.Slice(0, num).TryParseDouble(NumberStyles.Number, CultureInfo.InvariantCulture, out var value3);
				reference = value3 / 100.0;
				return result;
			}
			return inString.TryParseDouble(NumberStyles.Number, CultureInfo.InvariantCulture, out reference);
		}
	}

	/// <summary>
	/// Creates a new <see cref="T:Avalonia.Media.HslColor" /> from individual color component values.
	/// </summary>
	/// <remarks>
	/// This exists for symmetry with the <see cref="T:Avalonia.Media.Color" /> struct; however, the
	/// appropriate constructor should commonly be used instead.
	/// </remarks>
	/// <param name="a">The Alpha (transparency) component in the range from 0..1.</param>
	/// <param name="h">The Hue component in the range from 0..360.</param>
	/// <param name="s">The Saturation component in the range from 0..1.</param>
	/// <param name="l">The Lightness component in the range from 0..1.</param>
	/// <returns>A new <see cref="T:Avalonia.Media.HslColor" /> built from the individual color component values.</returns>
	public static HslColor FromAhsl(double a, double h, double s, double l)
	{
		return new HslColor(a, h, s, l);
	}

	/// <summary>
	/// Creates a new <see cref="T:Avalonia.Media.HslColor" /> from individual color component values.
	/// </summary>
	/// <remarks>
	/// This exists for symmetry with the <see cref="T:Avalonia.Media.Color" /> struct; however, the
	/// appropriate constructor should commonly be used instead.
	/// </remarks>
	/// <param name="h">The Hue component in the range from 0..360.</param>
	/// <param name="s">The Saturation component in the range from 0..1.</param>
	/// <param name="l">The Lightness component in the range from 0..1.</param>
	/// <returns>A new <see cref="T:Avalonia.Media.HslColor" /> built from the individual color component values.</returns>
	public static HslColor FromHsl(double h, double s, double l)
	{
		return new HslColor(1.0, h, s, l);
	}

	/// <summary>
	/// Converts the given HSLA color component values to their RGB color equivalent.
	/// </summary>
	/// <param name="hue">The Hue component in the HSL color model in the range from 0..360.</param>
	/// <param name="saturation">The Saturation component in the HSL color model in the range from 0..1.</param>
	/// <param name="lightness">The Lightness component in the HSL color model in the range from 0..1.</param>
	/// <param name="alpha">The Alpha component in the range from 0..1.</param>
	/// <returns>A new RGB <see cref="T:Avalonia.Media.Color" /> equivalent to the given HSLA values.</returns>
	public static Color ToRgb(double hue, double saturation, double lightness, double alpha = 1.0)
	{
		while (hue >= 360.0)
		{
			hue -= 360.0;
		}
		while (hue < 0.0)
		{
			hue += 360.0;
		}
		saturation = ((saturation < 0.0) ? 0.0 : saturation);
		saturation = ((saturation > 1.0) ? 1.0 : saturation);
		lightness = ((lightness < 0.0) ? 0.0 : lightness);
		lightness = ((lightness > 1.0) ? 1.0 : lightness);
		alpha = ((alpha < 0.0) ? 0.0 : alpha);
		alpha = ((alpha > 1.0) ? 1.0 : alpha);
		double num = (1.0 - Math.Abs(2.0 * lightness - 1.0)) * saturation;
		double num2 = hue / 60.0;
		double num3 = num * (1.0 - Math.Abs(num2 % 2.0 - 1.0));
		double num4 = lightness - 0.5 * num;
		double num5;
		double num6;
		double num7;
		if (num2 < 1.0)
		{
			num5 = num;
			num6 = num3;
			num7 = 0.0;
		}
		else if (num2 < 2.0)
		{
			num5 = num3;
			num6 = num;
			num7 = 0.0;
		}
		else if (num2 < 3.0)
		{
			num5 = 0.0;
			num6 = num;
			num7 = num3;
		}
		else if (num2 < 4.0)
		{
			num5 = 0.0;
			num6 = num3;
			num7 = num;
		}
		else if (num2 < 5.0)
		{
			num5 = num3;
			num6 = 0.0;
			num7 = num;
		}
		else
		{
			num5 = num;
			num6 = 0.0;
			num7 = num3;
		}
		return new Color((byte)Math.Round(255.0 * alpha), (byte)Math.Round(255.0 * (num5 + num4)), (byte)Math.Round(255.0 * (num6 + num4)), (byte)Math.Round(255.0 * (num7 + num4)));
	}

	/// <summary>
	/// Converts the given HSLA color component values to their HSV color equivalent.
	/// </summary>
	/// <param name="hue">The Hue component in the HSL color model in the range from 0..360.</param>
	/// <param name="saturation">The Saturation component in the HSL color model in the range from 0..1.</param>
	/// <param name="lightness">The Lightness component in the HSL color model in the range from 0..1.</param>
	/// <param name="alpha">The Alpha component in the range from 0..1.</param>
	/// <returns>A new <see cref="T:Avalonia.Media.HsvColor" /> equivalent to the given HSLA values.</returns>
	public static HsvColor ToHsv(double hue, double saturation, double lightness, double alpha = 1.0)
	{
		while (hue >= 360.0)
		{
			hue -= 360.0;
		}
		while (hue < 0.0)
		{
			hue += 360.0;
		}
		saturation = ((saturation < 0.0) ? 0.0 : saturation);
		saturation = ((saturation > 1.0) ? 1.0 : saturation);
		lightness = ((lightness < 0.0) ? 0.0 : lightness);
		lightness = ((lightness > 1.0) ? 1.0 : lightness);
		alpha = ((alpha < 0.0) ? 0.0 : alpha);
		alpha = ((alpha > 1.0) ? 1.0 : alpha);
		double num = lightness + saturation * Math.Min(lightness, 1.0 - lightness);
		double saturation2 = ((!(num <= 0.0)) ? (2.0 * (1.0 - lightness / num)) : 0.0);
		return new HsvColor(alpha, hue, saturation2, num);
	}

	/// <summary>
	/// Indicates whether the values of two specified <see cref="T:Avalonia.Media.HslColor" /> objects are equal.
	/// </summary>
	/// <param name="left">The first object to compare.</param>
	/// <param name="right">The second object to compare.</param>
	/// <returns>True if left and right are equal; otherwise, false.</returns>
	public static bool operator ==(HslColor left, HslColor right)
	{
		return left.Equals(right);
	}

	/// <summary>
	/// Indicates whether the values of two specified <see cref="T:Avalonia.Media.HslColor" /> objects are not equal.
	/// </summary>
	/// <param name="left">The first object to compare.</param>
	/// <param name="right">The second object to compare.</param>
	/// <returns>True if left and right are not equal; otherwise, false.</returns>
	public static bool operator !=(HslColor left, HslColor right)
	{
		return !(left == right);
	}

	/// <summary>
	/// Explicit conversion from an <see cref="T:Avalonia.Media.HslColor" /> to a <see cref="T:Avalonia.Media.Color" />.
	/// </summary>
	/// <param name="hslColor">The <see cref="T:Avalonia.Media.HslColor" /> to convert.</param>
	public static explicit operator Color(HslColor hslColor)
	{
		return hslColor.ToRgb();
	}
}
