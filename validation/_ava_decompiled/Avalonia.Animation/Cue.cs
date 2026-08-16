using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Animation;

/// <summary>
/// Determines the time index for a <see cref="T:Avalonia.Animation.KeyFrame" />. 
/// </summary>
[TypeConverter(typeof(CueTypeConverter))]
public readonly record struct Cue : IEquatable<double>
{
	/// <summary>
	/// The normalized percent value, ranging from 0.0 to 1.0
	/// </summary>
	public double CueValue { get; }

	/// <summary>
	/// Sets a new <see cref="T:Avalonia.Animation.Cue" /> object.
	/// </summary>
	/// <param name="value"></param>
	public Cue(double value)
	{
		if (value <= 1.0 && value >= 0.0)
		{
			CueValue = value;
			return;
		}
		throw new ArgumentException("This cue object's value should be within or equal to 0.0 and 1.0");
	}

	/// <summary>
	/// Parses a string to a <see cref="T:Avalonia.Animation.Cue" /> object.
	/// </summary>
	public static Cue Parse(string value, CultureInfo? culture)
	{
		string text = value;
		if (value.EndsWith('%'))
		{
			text = text.TrimEnd('%');
		}
		if (double.TryParse(text, NumberStyles.Float, culture, out var result))
		{
			return new Cue(result / 100.0);
		}
		throw new FormatException("Invalid Cue string \"" + value + "\"");
	}

	/// <summary>
	/// Checks for equality between a <see cref="T:Avalonia.Animation.Cue" />
	/// and a <see cref="T:System.Double" /> value.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public bool Equals(double other)
	{
		return CueValue == other;
	}
}
