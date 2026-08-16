namespace Avalonia.Utilities;

/// <summary>
/// Defines a listener to a event subscribed vis the <see cref="T:Avalonia.Utilities.WeakEvent`2" />.
/// </summary>
/// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
public interface IWeakEventSubscriber<in TEventArgs>
{
	void OnEvent(object? sender, WeakEvent ev, TEventArgs e);
}
