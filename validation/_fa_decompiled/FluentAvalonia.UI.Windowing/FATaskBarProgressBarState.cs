namespace FluentAvalonia.UI.Windowing;

/// <summary>
/// Represents constants that define the TaskBarProgressBar's state
/// </summary>
public enum FATaskBarProgressBarState
{
	/// <summary>
	/// No TaskBarProgressBar is displayed
	/// </summary>
	None = 0,
	/// <summary>
	/// A green indicator is displayed in the taskbar button
	/// </summary>
	Normal = 2,
	/// <summary>
	/// A yellow progress indicator is displayed in the taskbar button.
	/// </summary>
	Paused = 8,
	/// <summary>
	/// A red progress indicator is displayed in the taskbar button.
	/// </summary>
	Error = 4,
	/// <summary>
	/// A pulsing green indicator is displayed in the taskbar button.
	/// </summary>
	Indeterminate = 1
}
