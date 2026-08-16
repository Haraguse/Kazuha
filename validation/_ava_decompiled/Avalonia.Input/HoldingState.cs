namespace Avalonia.Input;

public enum HoldingState
{
	/// <summary>
	/// A single contact has been detected and a time threshold is crossed without the contact being lifted, another contact detected, or another gesture started.
	/// </summary>
	Started,
	/// <summary>
	/// The single contact is lifted.
	/// </summary>
	Completed,
	/// <summary>
	/// An additional contact is detected or a subsequent gesture (such as a slide) is detected.
	/// </summary>
	Canceled
}
