using Avalonia.Interactivity;

namespace Avalonia.Input;

/// <summary>
/// The inputs to an AccessKeyPressedEventHandler
/// </summary>
internal class AccessKeyPressedEventArgs : RoutedEventArgs
{
	/// <summary>
	/// Target element for the element that raised this event.
	/// </summary>
	/// <value></value>
	public IInputElement? Target { get; set; }

	/// <summary>
	/// Key that was pressed
	/// </summary>
	/// <value></value>
	public string? Key { get; }

	/// <summary>
	/// Constructor for AccessKeyPressed event args
	/// </summary>
	/// <param name="key"></param>
	public AccessKeyPressedEventArgs(string key)
	{
		base.RoutedEvent = AccessKeyHandler.AccessKeyPressedEvent;
		Key = key;
	}
}
