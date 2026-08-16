using Avalonia.Interactivity;

namespace Avalonia.Input;

/// <summary>
/// Information pertaining to when the access key associated with an element is pressed
/// </summary>
internal class AccessKeyEventArgs : RoutedEventArgs
{
	/// <summary>
	/// The key that was pressed which invoked this access key
	/// </summary>
	/// <value></value>
	public string Key { get; }

	/// <summary>
	/// Were there other elements which are also invoked by this key
	/// </summary>
	/// <value></value>
	public bool IsMultiple { get; }

	/// <summary>
	/// Constructor
	/// </summary>
	internal AccessKeyEventArgs(string key, bool isMultiple)
	{
		base.RoutedEvent = AccessKeyHandler.AccessKeyEvent;
		Key = key;
		IsMultiple = isMultiple;
	}
}
