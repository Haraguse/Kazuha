namespace Avalonia.Styling.Activators;

/// <summary>
/// Receives notifications from an <see cref="T:Avalonia.Styling.Activators.IStyleActivator" />.
/// </summary>
internal interface IStyleActivatorSink
{
	/// <summary>
	/// Called when the subscribed activator value changes.
	/// </summary>
	/// <param name="value">The new value.</param>
	void OnNext(bool value);
}
