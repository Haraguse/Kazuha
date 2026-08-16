using System;
using Avalonia.Metadata;

namespace Avalonia.Animation;

/// <summary>
/// Interface for Transition objects.
/// </summary>
[NotClientImplementable]
[PrivateApi]
public interface ITransition
{
	/// <summary>
	/// Gets the property to be animated.
	/// </summary>
	AvaloniaProperty Property { get; set; }

	/// <summary>
	/// Applies the transition to the specified <see cref="T:Avalonia.Animation.Animatable" />.
	/// </summary>
	internal IDisposable Apply(Animatable control, IClock clock, object? oldValue, object? newValue);
}
