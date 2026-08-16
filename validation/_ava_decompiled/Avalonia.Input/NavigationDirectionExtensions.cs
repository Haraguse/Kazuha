namespace Avalonia.Input;

public static class NavigationDirectionExtensions
{
	/// <summary>
	/// Checks whether a <see cref="T:Avalonia.Input.NavigationDirection" /> represents a tab movement.
	/// </summary>
	/// <param name="direction">The direction.</param>
	/// <returns>
	/// True if the direction represents a tab movement (<see cref="F:Avalonia.Input.NavigationDirection.Next" />
	/// or <see cref="F:Avalonia.Input.NavigationDirection.Previous" />); otherwise false.
	/// </returns>
	public static bool IsTab(this NavigationDirection direction)
	{
		if (direction != NavigationDirection.Next)
		{
			return direction == NavigationDirection.Previous;
		}
		return true;
	}

	/// <summary>
	/// Checks whether a <see cref="T:Avalonia.Input.NavigationDirection" /> represents a directional movement.
	/// </summary>
	/// <param name="direction">The direction.</param>
	/// <returns>
	/// True if the direction represents a directional movement (any value except 
	/// <see cref="F:Avalonia.Input.NavigationDirection.Next" /> and <see cref="F:Avalonia.Input.NavigationDirection.Previous" />);
	/// otherwise false.
	/// </returns>
	public static bool IsDirectional(this NavigationDirection direction)
	{
		if (direction > NavigationDirection.Previous)
		{
			return direction <= NavigationDirection.PageDown;
		}
		return false;
	}

	/// <summary>
	/// Converts a keypress into a <see cref="T:Avalonia.Input.NavigationDirection" />.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="modifiers">The keyboard modifiers.</param>
	/// <returns>
	/// A <see cref="T:Avalonia.Input.NavigationDirection" /> if the keypress represents a navigation keypress.
	/// </returns>
	public static NavigationDirection? ToNavigationDirection(this Key key, KeyModifiers modifiers = KeyModifiers.None)
	{
		return key switch
		{
			Key.Tab => ((modifiers & KeyModifiers.Shift) != KeyModifiers.None) ? NavigationDirection.Previous : NavigationDirection.Next, 
			Key.Up => NavigationDirection.Up, 
			Key.Down => NavigationDirection.Down, 
			Key.Left => NavigationDirection.Left, 
			Key.Right => NavigationDirection.Right, 
			Key.Home => NavigationDirection.First, 
			Key.End => NavigationDirection.Last, 
			Key.PageUp => NavigationDirection.PageUp, 
			Key.PageDown => NavigationDirection.PageDown, 
			_ => null, 
		};
	}
}
