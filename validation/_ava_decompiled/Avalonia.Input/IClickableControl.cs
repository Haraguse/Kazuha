using System;
using Avalonia.Interactivity;

namespace Avalonia.Input;

/// <summary>
///
/// </summary>
internal interface IClickableControl
{
	/// <summary>
	/// Gets a value indicating whether this control and all its parents are enabled.
	/// </summary>
	bool IsEffectivelyEnabled { get; }

	event EventHandler<RoutedEventArgs> Click;

	void RaiseClick();
}
