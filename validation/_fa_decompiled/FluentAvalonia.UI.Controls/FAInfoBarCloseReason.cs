namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Defines constants that indicate the cause of the InfoBar closure.
/// </summary>
public enum FAInfoBarCloseReason
{
	/// <summary>
	/// The InfoBar was closed by the user clicking the close button.
	/// </summary>
	CloseButton,
	/// <summary>
	/// The InfoBar was programmatically closed.
	/// </summary>
	Programmatic
}
