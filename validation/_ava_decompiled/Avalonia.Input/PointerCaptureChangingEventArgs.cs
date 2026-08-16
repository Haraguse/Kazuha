using Avalonia.Interactivity;

namespace Avalonia.Input;

internal class PointerCaptureChangingEventArgs : RoutedEventArgs
{
	public IPointer Pointer { get; }

	public CaptureSource CaptureSource { get; }

	public IInputElement? NewValue { get; }

	internal PointerCaptureChangingEventArgs(object? source, IPointer pointer, IInputElement? newValue, CaptureSource captureSource)
		: base(InputElement.PointerCaptureChangingEvent)
	{
		Pointer = pointer;
		base.Source = source;
		NewValue = newValue;
		CaptureSource = captureSource;
	}
}
