using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.SourceGenerator;

namespace Avalonia.Animation.Easings;

/// <summary>
/// Base class for all Easing classes.
/// </summary>
[TypeConverter(typeof(EasingTypeConverter))]
public abstract class Easing : IEasing
{
	private const string Namespace = "Avalonia.Animation.Easings";

	/// <inheritdoc />
	public abstract double Ease(double progress);

	[SubtypesFactory(typeof(Easing), "Avalonia.Animation.Easings")]
	private static bool TryCreateEasingInstance(string type, [NotNullWhen(true)] out Easing? instance)
	{
		bool result;
		(result, instance) = type switch
		{
			"BackEaseIn" => (true, new BackEaseIn()), 
			"BackEaseInOut" => (true, new BackEaseInOut()), 
			"BackEaseOut" => (true, new BackEaseOut()), 
			"BounceEaseIn" => (true, new BounceEaseIn()), 
			"BounceEaseInOut" => (true, new BounceEaseInOut()), 
			"BounceEaseOut" => (true, new BounceEaseOut()), 
			"CircularEaseIn" => (true, new CircularEaseIn()), 
			"CircularEaseInOut" => (true, new CircularEaseInOut()), 
			"CircularEaseOut" => (true, new CircularEaseOut()), 
			"CubicEaseIn" => (true, new CubicEaseIn()), 
			"CubicEaseInOut" => (true, new CubicEaseInOut()), 
			"CubicEaseOut" => (true, new CubicEaseOut()), 
			"ElasticEaseIn" => (true, new ElasticEaseIn()), 
			"ElasticEaseInOut" => (true, new ElasticEaseInOut()), 
			"ElasticEaseOut" => (true, new ElasticEaseOut()), 
			"ExponentialEaseIn" => (true, new ExponentialEaseIn()), 
			"ExponentialEaseInOut" => (true, new ExponentialEaseInOut()), 
			"ExponentialEaseOut" => (true, new ExponentialEaseOut()), 
			"LinearEasing" => (true, new LinearEasing()), 
			"QuadraticEaseIn" => (true, new QuadraticEaseIn()), 
			"QuadraticEaseInOut" => (true, new QuadraticEaseInOut()), 
			"QuadraticEaseOut" => (true, new QuadraticEaseOut()), 
			"QuarticEaseIn" => (true, new QuarticEaseIn()), 
			"QuarticEaseInOut" => (true, new QuarticEaseInOut()), 
			"QuarticEaseOut" => (true, new QuarticEaseOut()), 
			"QuinticEaseIn" => (true, new QuinticEaseIn()), 
			"QuinticEaseInOut" => (true, new QuinticEaseInOut()), 
			"QuinticEaseOut" => (true, new QuinticEaseOut()), 
			"SineEaseIn" => (true, new SineEaseIn()), 
			"SineEaseInOut" => (true, new SineEaseInOut()), 
			"SineEaseOut" => (true, new SineEaseOut()), 
			"SplineEasing" => (true, new SplineEasing()), 
			"SpringEasing" => (true, new SpringEasing()), 
			_ => (false, null), 
		};
		return result;
	}

	/// <summary>
	/// Parses a Easing type string.
	/// </summary>
	/// <param name="e">The Easing type string.</param>
	/// <returns>Returns the instance of the parsed type.</returns>
	public static Easing Parse(string e)
	{
		if (e.Contains(','))
		{
			return new SplineEasing(KeySpline.Parse(e, CultureInfo.InvariantCulture));
		}
		if (!TryCreateEasingInstance(e, out Easing instance))
		{
			throw new FormatException($"Easing \"{e}\" was not found in {"Avalonia.Animation.Easings"} namespace.");
		}
		return instance;
	}
}
