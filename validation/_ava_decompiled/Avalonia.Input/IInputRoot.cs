using Avalonia.Input.TextInput;
using Avalonia.Metadata;

namespace Avalonia.Input;

/// <summary>
/// Defines the interface for top-level input elements.
/// </summary>
[PrivateApi]
public interface IInputRoot
{
	/// <summary>
	/// Gets focus manager of the root.
	/// </summary>
	/// <remarks>
	/// Focus manager can be null only if window wasn't initialized yet.
	/// </remarks>
	IFocusManager? FocusManager { get; }

	/// <summary>
	/// Gets or sets the input element that the pointer is currently over.
	/// </summary>
	internal IInputElement? PointerOverElement { get; set; }

	internal IInputElement? CursorElement { get; set; }

	internal ITextInputMethodImpl? InputMethod { get; }

	internal InputElement RootElement { get; }

	InputElement FocusRoot { get; }

	/// <summary>
	/// Performs a hit-test for chrome/decoration elements at the given position.
	/// </summary>
	/// <param name="point">The point in root-relative coordinates.</param>
	/// <returns>
	/// <c>null</c> if no chrome element was hit (no chrome involvement at this point).
	/// <see cref="F:Avalonia.Input.WindowDecorationsElementRole.DecorationsElement" /> or <see cref="F:Avalonia.Input.WindowDecorationsElementRole.User" />
	/// if an interactive chrome element was hit — the platform should redirect non-client input to regular client input.
	/// Any other non-<see cref="F:Avalonia.Input.WindowDecorationsElementRole.None" /> value indicates a specific non-client role (titlebar, resize grip, etc.).
	/// </returns>
	internal WindowDecorationsElementRole? HitTestChromeElement(Point point)
	{
		return null;
	}

	/// <summary>
	/// Ask for the pointer-over element to be refreshed (usually after a capture change).
	/// </summary>
	internal void PointerOverInvalidated();
}
