namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Defines constants to define which commands should be available
/// in a <see cref="T:FluentAvalonia.UI.Controls.FATextCommandBarFlyout" />
/// </summary>
internal enum TextControlButtons
{
	None = 0,
	Cut = 1,
	Copy = 2,
	Paste = 4,
	Bold = 8,
	Italic = 0x10,
	Underline = 0x20,
	Undo = 0x40,
	Redo = 0x80,
	SelectAll = 0x100
}
