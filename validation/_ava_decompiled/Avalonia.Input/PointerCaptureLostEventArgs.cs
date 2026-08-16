using Avalonia.Interactivity;

namespace Avalonia.Input;

public class PointerCaptureLostEventArgs : RoutedEventArgs
{
	public IPointer Pointer { get; }

	public PointerCaptureLostEventArgs(object? source, IPointer pointer)
		: base(InputElement.PointerCaptureLostEvent)
	{
		Pointer = pointer;
		base.Source = source;
	}
}
