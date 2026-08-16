namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Gets the orientation, if any, in which items are laid out based on their index in the source collection.
/// </summary>
public enum FAIndexBasedLayoutOrientation
{
	/// <summary>
	/// There is no correlation between the items' layout and their index number.
	/// </summary>
	None,
	/// <summary>
	/// Items are laid out vertically with increasing indices.
	/// </summary>
	TopToBottom,
	/// <summary>
	/// Items are laid out horizontally with increasing indices.
	/// </summary>
	LeftToRight
}
