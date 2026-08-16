using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
/// Represents a box shadow which can be attached to an element or control.
/// </summary>
public struct BoxShadow
{
	private struct ArrayReader(string[] arr)
	{
		private int _index = 0;

		private readonly string[] _arr = arr;

		public bool TryReadString([MaybeNullWhen(false)] out string s)
		{
			s = null;
			if (_index >= _arr.Length)
			{
				return false;
			}
			s = _arr[_index];
			_index++;
			return true;
		}

		public string ReadString()
		{
			if (!TryReadString(out string s))
			{
				throw new FormatException();
			}
			return s;
		}
	}

	private static readonly char[] s_Separator = new char[2] { ' ', '\t' };

	private const char OpeningParenthesis = '(';

	private const char ClosingParenthesis = ')';

	/// <summary>
	/// Gets or sets the horizontal offset (distance) of the shadow.
	/// </summary>
	/// <remarks>
	/// Positive values place the shadow to the right of the element while
	/// negative values place the shadow to the left.
	/// </remarks>
	public double OffsetX { get; set; }

	/// <summary>
	/// Gets or sets the vertical offset (distance) of the shadow.
	/// </summary>
	/// <remarks>
	/// Positive values place the shadow below the element while
	/// negative values place the shadow above.
	/// </remarks>
	public double OffsetY { get; set; }

	/// <summary>
	/// Gets or sets the blur radius.
	/// This is used to control the amount of blurring.
	/// </summary>
	/// <remarks>
	/// The larger this value, the bigger the blur effect, so the shadow becomes larger and more transparent.
	/// Negative values are not allowed. If not specified, the default (zero) is used and the shadow edge is sharp.
	/// </remarks>
	public double Blur { get; set; }

	/// <summary>
	/// Gets or sets the spread radius.
	/// This is used to control the overall size of the shadow.
	/// </summary>
	/// <remarks>
	/// Positive values will cause the shadow to expand and grow larger, negative values will cause the shadow to shrink.
	/// If not specified, the default (zero) is used and the shadow will be the same size as the element.
	/// </remarks>
	public double Spread { get; set; }

	/// <summary>
	/// Gets or sets the color of the shadow.
	/// </summary>
	public Color Color { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the shadow is inset and drawn within the element rather than outside of it.
	/// </summary>
	/// <remarks>
	/// Inset changes the shadow to inside the element (as if the content was depressed inside the box).
	/// If false (the default), the shadow is assumed to be a drop shadow (as if the box were raised above the content).
	/// <br /><br />
	/// Inset shadows are drawn inside the element, above the background (even when it's transparent), but below any content.
	/// </remarks>
	public bool IsInset { get; set; }

	/// <summary>
	/// Indicates whether the current object is equal to another object of the same type.
	/// </summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>
	/// <c>true</c> if the current object is equal to the other parameter; otherwise, <c>false</c>.
	/// </returns>
	public bool Equals(in BoxShadow other)
	{
		if (OffsetX == other.OffsetX && OffsetY == other.OffsetY && Blur == other.Blur && Spread == other.Spread && Color.Equals(other.Color))
		{
			return IsInset == other.IsInset;
		}
		return false;
	}

	/// <inheritdoc />
	public override bool Equals(object? obj)
	{
		if (obj is BoxShadow other)
		{
			return Equals(in other);
		}
		return false;
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return (((((((((OffsetX.GetHashCode() * 397) ^ OffsetY.GetHashCode()) * 397) ^ Blur.GetHashCode()) * 397) ^ Spread.GetHashCode()) * 397) ^ Color.GetHashCode()) * 397) ^ IsInset.GetHashCode();
	}

	/// <inheritdoc />
	public override string ToString()
	{
		StringBuilder stringBuilder = StringBuilderCache.Acquire();
		ToString(stringBuilder);
		return StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	internal void ToString(StringBuilder sb)
	{
		if (this == default(BoxShadow))
		{
			sb.Append("none");
			return;
		}
		if (IsInset)
		{
			sb.Append("inset ");
		}
		sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", OffsetX);
		sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", OffsetY);
		if (Blur != 0.0 || Spread != 0.0)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", Blur);
		}
		if (Spread != 0.0)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", Spread);
		}
		Color.ToString(sb);
	}

	/// <summary>
	/// Parses a <see cref="T:Avalonia.Media.BoxShadow" /> string.
	/// </summary>
	/// <remarks>
	/// A box shadow may be specified in multiple formats with separate components:
	///   <list type="bullet">
	///     <item>Two, three, or four length values.</item>
	///     <item>A color value.</item>
	///     <item>An optional inset keyword.</item>
	///   </list>
	/// If only two length values are given they will be interpreted as <see cref="P:Avalonia.Media.BoxShadow.OffsetX" /> and <see cref="P:Avalonia.Media.BoxShadow.OffsetY" />.
	/// If a third value is given, it is interpreted as a <see cref="P:Avalonia.Media.BoxShadow.Blur" />, and if a fourth value is given,
	/// it is interpreted as <see cref="P:Avalonia.Media.BoxShadow.Spread" />.
	/// </remarks>
	/// <param name="s">The input string to parse.</param>
	/// <returns>A new <see cref="T:Avalonia.Media.BoxShadow" /></returns>
	public static BoxShadow Parse(string s)
	{
		if (s == null)
		{
			throw new ArgumentNullException();
		}
		if (s.Length == 0)
		{
			throw new FormatException();
		}
		string[] array = StringSplitter.SplitRespectingBrackets(s, s_Separator, '(', ')', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 1 && array[0] == "none")
		{
			return default(BoxShadow);
		}
		if (array.Length < 3 || array.Length > 6)
		{
			throw new FormatException();
		}
		bool isInset = false;
		ArrayReader arrayReader = new ArrayReader(array);
		string text = arrayReader.ReadString();
		if (text == "inset")
		{
			isInset = true;
			text = arrayReader.ReadString();
		}
		double offsetX = double.Parse(text, CultureInfo.InvariantCulture);
		double offsetY = double.Parse(arrayReader.ReadString(), CultureInfo.InvariantCulture);
		double blur = 0.0;
		double spread = 0.0;
		arrayReader.TryReadString(out string s2);
		arrayReader.TryReadString(out string s3);
		arrayReader.TryReadString(out string s4);
		if (s3 != null)
		{
			blur = double.Parse(s2, CultureInfo.InvariantCulture);
		}
		if (s4 != null)
		{
			spread = double.Parse(s3, CultureInfo.InvariantCulture);
		}
		Color color = Color.Parse(s4 ?? s3 ?? s2);
		return new BoxShadow
		{
			IsInset = isInset,
			OffsetX = offsetX,
			OffsetY = offsetY,
			Blur = blur,
			Spread = spread,
			Color = color
		};
	}

	/// <summary>
	/// Transforms the specified bounding rectangle to account for the shadow's offset, spread, and blur.
	/// </summary>
	/// <param name="rect">The original bounding <see cref="T:Avalonia.Rect" /> to transform.</param>
	/// <returns>
	/// A new <see cref="T:Avalonia.Rect" /> that includes the shadow's offset, spread, and blur if the shadow is not inset;
	/// otherwise, the original rectangle.
	/// </returns>
	public Rect TransformBounds(in Rect rect)
	{
		if (!IsInset)
		{
			return rect.Translate(new Vector(OffsetX, OffsetY)).Inflate(Spread + Blur);
		}
		return rect;
	}

	/// <summary>
	/// Determines whether two <see cref="T:Avalonia.Media.BoxShadow" /> values are equal.
	/// </summary>
	/// <param name="left">The first <see cref="T:Avalonia.Media.BoxShadow" /> to compare.</param>
	/// <param name="right">The second <see cref="T:Avalonia.Media.BoxShadow" /> to compare.</param>
	/// <returns>
	/// <c>true</c> if the two <see cref="T:Avalonia.Media.BoxShadow" /> values are equal; otherwise, <c>false</c>.
	/// </returns>
	public static bool operator ==(BoxShadow left, BoxShadow right)
	{
		return left.Equals(in right);
	}

	/// <summary>
	/// Determines whether two <see cref="T:Avalonia.Media.BoxShadow" /> values are not equal.
	/// </summary>
	/// <param name="left">The first <see cref="T:Avalonia.Media.BoxShadow" /> to compare.</param>
	/// <param name="right">The second <see cref="T:Avalonia.Media.BoxShadow" /> to compare.</param>
	/// <returns>
	/// <c>true</c> if the two <see cref="T:Avalonia.Media.BoxShadow" /> values are not equal; otherwise, <c>false</c>.
	/// </returns>
	public static bool operator !=(BoxShadow left, BoxShadow right)
	{
		return !(left == right);
	}
}
