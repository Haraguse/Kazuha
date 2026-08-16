using System;
using System.Globalization;
using System.Text;
using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
/// Defines a color using the hue/saturation/value (HSV) model.
/// This uses a cylindrical-coordinate representation of a color.
/// </summary>
public readonly struct HsvColor : IEquatable<HsvColor>
{
	/// <summary>
	/// Gets the Alpha (transparency) component in the range from 0..1 (percentage).
	/// </summary>
	/// <remarks>
	/// <list type="bullet">
	///   <item>0 is fully transparent.</item>
	///   <item>1 is fully opaque.</item>
	/// </list>
	/// </remarks>
	public double A { get; }

	/// <summary>
	/// Gets the Hue component in the range from 0..360 (degrees).
	/// This is the color's location, in degrees, on a color wheel/circle from 0 to 360.
	/// Note that 360 is equivalent to 0 and will be adjusted automatically.
	/// </summary>
	/// <remarks>
	/// <list type="bullet">
	///   <item>0/360 degrees is Red.</item>
	///   <item>60 degrees is Yellow.</item>
	///   <item>120 degrees is Green.</item>
	///   <item>180 degrees is Cyan.</item>
	///   <item>240 degrees is Blue.</item>
	///   <item>300 degrees is Magenta.</item>
	/// </list>
	/// </remarks>
	public double H { get; }

	/// <summary>
	/// Gets the Saturation component in the range from 0..1 (percentage).
	/// </summary>
	/// <remarks>
	/// <list type="bullet">
	///   <item>0 is fully white (or a shade of gray) and shows no color.</item>
	///   <item>1 is the full color.</item>
	/// </list>
	/// </remarks>
	public double S { get; }

	/// <summary>
	/// Gets the Value (or Brightness/Intensity) component in the range from 0..1 (percentage).
	/// </summary>
	/// <remarks>
	/// <list type="bullet">
	///   <item>0 is fully black and shows no color.</item>
	///   <item>1 is the brightest and shows full color.</item>
	/// </list>
	/// </remarks>
	public double V { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.HsvColor" /> struct.
	/// </summary>
	/// <param name="alpha">The Alpha (transparency) component in the range from 0..1.</param>
	/// <param name="hue">The Hue component in the range from 0..360.
	/// Note that 360 is equivalent to 0 and will be adjusted automatically.</param>
	/// <param name="saturation">The Saturation component in the range from 0..1.</param>
	/// <param name="value">The Value component in the range from 0..1.</param>
	public HsvColor(double alpha, double hue, double saturation, double value)
	{
		A = MathUtilities.Clamp(alpha, 0.0, 1.0);
		H = MathUtilities.Clamp(hue, 0.0, 360.0);
		S = MathUtilities.Clamp(saturation, 0.0, 1.0);
		V = MathUtilities.Clamp(value, 0.0, 1.0);
		H = ((H == 360.0) ? 0.0 : H);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.HsvColor" /> struct.
	/// </summary>
	/// <remarks>
	/// This constructor exists only for internal use where performance is critical.
	/// Whether or not the component values are in the correct ranges must be known.
	/// </remarks>
	/// <param name="alpha">The Alpha (transparency) component in the range from 0..1.</param>
	/// <param name="hue">The Hue component in the range from 0..360.
	/// Note that 360 is equivalent to 0 and will be adjusted automatically.</param>
	/// <param name="saturation">The Saturation component in the range from 0..1.</param>
	/// <param name="value">The Value component in the range from 0..1.</param>
	/// <param name="clampValues">Whether to clamp component values to their required ranges.</param>
	internal HsvColor(double alpha, double hue, double saturation, double value, bool clampValues)
	{
		if (clampValues)
		{
			A = MathUtilities.Clamp(alpha, 0.0, 1.0);
			H = MathUtilities.Clamp(hue, 0.0, 360.0);
			S = MathUtilities.Clamp(saturation, 0.0, 1.0);
			V = MathUtilities.Clamp(value, 0.0, 1.0);
			H = ((H == 360.0) ? 0.0 : H);
		}
		else
		{
			A = alpha;
			H = hue;
			S = saturation;
			V = value;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.HsvColor" /> struct.
	/// </summary>
	/// <param name="color">The RGB color to convert to HSV.</param>
	public HsvColor(Color color)
	{
		HsvColor hsvColor = color.ToHsv();
		A = hsvColor.A;
		H = hsvColor.H;
		S = hsvColor.S;
		V = hsvColor.V;
	}

	/// <inheritdoc />
	public bool Equals(HsvColor other)
	{
		if (other.A == A && other.H == H && other.S == S)
		{
			return other.V == V;
		}
		return false;
	}

	/// <inheritdoc />
	public override bool Equals(object? obj)
	{
		if (obj is HsvColor other)
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
		return (((((A.GetHashCode() * 397) ^ H.GetHashCode()) * 397) ^ S.GetHashCode()) * 397) ^ V.GetHashCode();
	}

	/// <summary>
	/// Returns the RGB color model equivalent of this HSV color.
	/// </summary>
	/// <returns>The RGB equivalent color.</returns>
	public Color ToRgb()
	{
		return ToRgb(H, S, V, A);
	}

	/// <summary>
	/// Returns the HSL color model equivalent of this HSV color.
	/// </summary>
	/// <returns>The HSL equivalent color.</returns>
	public HslColor ToHsl()
	{
		return ToHsl(H, S, V, A);
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
		stringBuilder.Append(V.ToString(CultureInfo.InvariantCulture));
		stringBuilder.Append(", ");
		stringBuilder.Append(A.ToString(CultureInfo.InvariantCulture));
		stringBuilder.Append(')');
		return StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	/// <summary>
	/// Parses an HSV color string.
	/// </summary>
	/// <param name="s">The HSV color string to parse.</param>
	/// <returns>The parsed <see cref="T:Avalonia.Media.HsvColor" />.</returns>
	public static HsvColor Parse(string s)
	{
		if (s == null)
		{
			throw new ArgumentNullException("s");
		}
		if (TryParse(s, out var hsvColor))
		{
			return hsvColor;
		}
		throw new FormatException("Invalid HSV color string: '" + s + "'.");
	}

	/// <summary>
	/// Parses an HSV color string.
	/// </summary>
	/// <param name="s">The HSV color string to parse.</param>
	/// <param name="hsvColor">The parsed <see cref="T:Avalonia.Media.HsvColor" />.</param>
	/// <returns>True if parsing was successful; otherwise, false.</returns>
	public static bool TryParse(string? s, out HsvColor hsvColor)
	{
		bool flag = false;
		hsvColor = default(HsvColor);
		if (s == null)
		{
			return false;
		}
		string text = s.Trim();
		if (text.Length == 0 || text.IndexOf(",", StringComparison.Ordinal) < 0)
		{
			return false;
		}
		if (text.Length >= 11 && text.StartsWith("hsva(", StringComparison.OrdinalIgnoreCase) && text.EndsWith(")", StringComparison.Ordinal))
		{
			text = text.Substring(5, text.Length - 6);
			flag = true;
		}
		if (!flag && text.Length >= 10 && text.StartsWith("hsv(", StringComparison.OrdinalIgnoreCase) && text.EndsWith(")", StringComparison.Ordinal))
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
				hsvColor = new HsvColor(1.0, value, outDouble, outDouble2);
				return true;
			}
		}
		else if (array.Length == 4 && array[0].AsSpan().TryParseDouble(NumberStyles.Number, CultureInfo.InvariantCulture, out value2) && TryInternalParse(array[1].AsSpan(), out outDouble3) && TryInternalParse(array[2].AsSpan(), out outDouble4) && TryInternalParse(array[3].AsSpan(), out outDouble5))
		{
			hsvColor = new HsvColor(outDouble5, value2, outDouble3, outDouble4);
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
	/// Creates a new <see cref="T:Avalonia.Media.HsvColor" /> from individual color component values.
	/// </summary>
	/// <remarks>
	/// This exists for symmetry with the <see cref="T:Avalonia.Media.Color" /> struct; however, the
	/// appropriate constructor should commonly be used instead.
	/// </remarks>
	/// <param name="a">The Alpha (transparency) component in the range from 0..1.</param>
	/// <param name="h">The Hue component in the range from 0..360.</param>
	/// <param name="s">The Saturation component in the range from 0..1.</param>
	/// <param name="v">The Value component in the range from 0..1.</param>
	/// <returns>A new <see cref="T:Avalonia.Media.HsvColor" /> built from the individual color component values.</returns>
	public static HsvColor FromAhsv(double a, double h, double s, double v)
	{
		return new HsvColor(a, h, s, v);
	}

	/// <summary>
	/// Creates a new <see cref="T:Avalonia.Media.HsvColor" /> from individual color component values.
	/// </summary>
	/// <remarks>
	/// This exists for symmetry with the <see cref="T:Avalonia.Media.Color" /> struct; however, the
	/// appropriate constructor should commonly be used instead.
	/// </remarks>
	/// <param name="h">The Hue component in the range from 0..360.</param>
	/// <param name="s">The Saturation component in the range from 0..1.</param>
	/// <param name="v">The Value component in the range from 0..1.</param>
	/// <returns>A new <see cref="T:Avalonia.Media.HsvColor" /> built from the individual color component values.</returns>
	public static HsvColor FromHsv(double h, double s, double v)
	{
		return new HsvColor(1.0, h, s, v);
	}

	/// <summary>
	/// Converts the given HSVA color component values to their RGB color equivalent.
	/// </summary>
	/// <param name="hue">The Hue component in the HSV color model in the range from 0..360.</param>
	/// <param name="saturation">The Saturation component in the HSV color model in the range from 0..1.</param>
	/// <param name="value">The Value component in the HSV color model in the range from 0..1.</param>
	/// <param name="alpha">The Alpha component in the range from 0..1.</param>
	/// <returns>A new RGB <see cref="T:Avalonia.Media.Color" /> equivalent to the given HSVA values.</returns>
	public static Color ToRgb(double hue, double saturation, double value, double alpha = 1.0)
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
		value = ((value < 0.0) ? 0.0 : value);
		value = ((value > 1.0) ? 1.0 : value);
		alpha = ((alpha < 0.0) ? 0.0 : alpha);
		alpha = ((alpha > 1.0) ? 1.0 : alpha);
		double num = saturation * value;
		double num2 = value - num;
		if (num == 0.0)
		{
			return Color.FromArgb((byte)Math.Round(alpha * 255.0), (byte)Math.Round(num2 * 255.0), (byte)Math.Round(num2 * 255.0), (byte)Math.Round(num2 * 255.0));
		}
		int num3 = (int)(hue / 60.0);
		double num4 = hue / 60.0 - (double)num3;
		double num5 = num + num2;
		double num6 = 0.0;
		double num7 = 0.0;
		double num8 = 0.0;
		switch (num3)
		{
		case 0:
			num6 = num5;
			num7 = num2 + num * num4;
			num8 = num2;
			break;
		case 1:
			num6 = num2 + num * (1.0 - num4);
			num7 = num5;
			num8 = num2;
			break;
		case 2:
			num6 = num2;
			num7 = num5;
			num8 = num2 + num * num4;
			break;
		case 3:
			num6 = num2;
			num7 = num2 + num * (1.0 - num4);
			num8 = num5;
			break;
		case 4:
			num6 = num2 + num * num4;
			num7 = num2;
			num8 = num5;
			break;
		case 5:
			num6 = num5;
			num7 = num2;
			num8 = num2 + num * (1.0 - num4);
			break;
		}
		return new Color((byte)Math.Round(alpha * 255.0), (byte)Math.Round(num6 * 255.0), (byte)Math.Round(num7 * 255.0), (byte)Math.Round(num8 * 255.0));
	}

	/// <summary>
	/// Converts the given HSVA color component values to their HSL color equivalent.
	/// </summary>
	/// <param name="hue">The Hue component in the HSV color model in the range from 0..360.</param>
	/// <param name="saturation">The Saturation component in the HSV color model in the range from 0..1.</param>
	/// <param name="value">The Value component in the HSV color model in the range from 0..1.</param>
	/// <param name="alpha">The Alpha component in the range from 0..1.</param>
	/// <returns>A new <see cref="T:Avalonia.Media.HslColor" /> equivalent to the given HSVA values.</returns>
	public static HslColor ToHsl(double hue, double saturation, double value, double alpha = 1.0)
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
		value = ((value < 0.0) ? 0.0 : value);
		value = ((value > 1.0) ? 1.0 : value);
		alpha = ((alpha < 0.0) ? 0.0 : alpha);
		alpha = ((alpha > 1.0) ? 1.0 : alpha);
		double num = value * (1.0 - saturation / 2.0);
		double saturation2 = ((!(num <= 0.0) && !(num >= 1.0)) ? ((value - num) / Math.Min(num, 1.0 - num)) : 0.0);
		return new HslColor(alpha, hue, saturation2, num);
	}

	/// <summary>
	/// Indicates whether the values of two specified <see cref="T:Avalonia.Media.HsvColor" /> objects are equal.
	/// </summary>
	/// <param name="left">The first object to compare.</param>
	/// <param name="right">The second object to compare.</param>
	/// <returns>True if left and right are equal; otherwise, false.</returns>
	public static bool operator ==(HsvColor left, HsvColor right)
	{
		return left.Equals(right);
	}

	/// <summary>
	/// Indicates whether the values of two specified <see cref="T:Avalonia.Media.HsvColor" /> objects are not equal.
	/// </summary>
	/// <param name="left">The first object to compare.</param>
	/// <param name="right">The second object to compare.</param>
	/// <returns>True if left and right are not equal; otherwise, false.</returns>
	public static bool operator !=(HsvColor left, HsvColor right)
	{
		return !(left == right);
	}

	/// <summary>
	/// Explicit conversion from an <see cref="T:Avalonia.Media.HsvColor" /> to a <see cref="T:Avalonia.Media.Color" />.
	/// </summary>
	/// <param name="hsvColor">The <see cref="T:Avalonia.Media.HsvColor" /> to convert.</param>
	public static explicit operator Color(HsvColor hsvColor)
	{
		return hsvColor.ToRgb();
	}
}
