namespace Avalonia.Input;

internal record XYFocusParams(InputElement Element, Rect Bounds)
{
	public double Score { get; set; }
}
