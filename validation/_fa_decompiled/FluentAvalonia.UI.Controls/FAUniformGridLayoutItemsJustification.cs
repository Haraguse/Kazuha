namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Defines constants that specify how items are aligned on the non-scrolling or non-virtualizing axis.
/// </summary>
public enum FAUniformGridLayoutItemsJustification
{
	/// <summary>
	/// Items are aligned with the start of the row or column, with extra space at the end. Spacing between items does not change.
	/// </summary>
	Start,
	/// <summary>
	/// Items are aligned in the center of the row or column, with extra space at the start and end. Spacing between items does not change.
	/// </summary>
	Center,
	/// <summary>
	/// Items are aligned with the end of the row or column, with extra space at the start. Spacing between items does not change.
	/// </summary>
	End,
	/// <summary>
	/// Items are aligned so that extra space is added evenly before and after each item.
	/// </summary>
	SpaceAround,
	/// <summary>
	/// Items are aligned so that extra space is added evenly between adjacent items. No space is added at the start or end.
	/// </summary>
	SpaceBetween,
	/// <summary>
	/// Items are aligned so that extra space is added evenly before and after each item.
	/// </summary>
	SpaceEvenly
}
