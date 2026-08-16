using Avalonia.Metadata;

namespace Avalonia.Input.Raw;

[PrivateApi]
public class RawMouseWheelEventArgs : RawPointerEventArgs
{
	public Vector Delta { get; private set; }

	public RawMouseWheelEventArgs(IInputDevice device, ulong timestamp, IInputRoot root, Point position, Vector delta, RawInputModifiers inputModifiers)
		: base(device, timestamp, root, RawPointerEventType.Wheel, position, inputModifiers)
	{
		Delta = delta;
	}
}
