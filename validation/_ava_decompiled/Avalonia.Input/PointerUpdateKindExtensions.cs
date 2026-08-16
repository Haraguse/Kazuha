namespace Avalonia.Input;

public static class PointerUpdateKindExtensions
{
	public static MouseButton GetMouseButton(this PointerUpdateKind kind)
	{
		switch (kind)
		{
		case PointerUpdateKind.LeftButtonPressed:
		case PointerUpdateKind.LeftButtonReleased:
			return MouseButton.Left;
		case PointerUpdateKind.MiddleButtonPressed:
		case PointerUpdateKind.MiddleButtonReleased:
			return MouseButton.Middle;
		case PointerUpdateKind.RightButtonPressed:
		case PointerUpdateKind.RightButtonReleased:
			return MouseButton.Right;
		case PointerUpdateKind.XButton1Pressed:
		case PointerUpdateKind.XButton1Released:
			return MouseButton.XButton1;
		case PointerUpdateKind.XButton2Pressed:
		case PointerUpdateKind.XButton2Released:
			return MouseButton.XButton2;
		default:
			return MouseButton.None;
		}
	}
}
