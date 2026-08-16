using System;
using System.Collections.Generic;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Input;

/// <summary>
/// Represents a mouse device.
/// </summary>
[PrivateApi]
public class MouseDevice : IMouseDevice, IPointerDevice, IInputDevice, IDisposable
{
	private static MouseDevice? _primary;

	private int _clickCount;

	private Rect _lastClickRect;

	private ulong _lastClickTime;

	private readonly Pointer _pointer;

	private bool _disposed;

	private MouseButton _lastMouseDownButton;

	internal static MouseDevice Primary => _primary ?? (_primary = new MouseDevice());

	internal Pointer Pointer => _pointer;

	public MouseDevice(Pointer? pointer = null)
	{
		_pointer = pointer ?? new Pointer(Avalonia.Input.Pointer.GetNextFreeId(), PointerType.Mouse, isPrimary: true);
	}

	internal static TMouseDevice GetOrCreatePrimary<TMouseDevice>() where TMouseDevice : MouseDevice, new()
	{
		if (_primary is TMouseDevice result)
		{
			return result;
		}
		return (TMouseDevice)(_primary = new TMouseDevice());
	}

	public void ProcessRawEvent(RawInputEventArgs e)
	{
		if (!e.Handled && e is RawPointerEventArgs e2)
		{
			ProcessRawEvent(e2);
		}
	}

	private static int ButtonCount(PointerPointProperties props)
	{
		int num = 0;
		if (props.IsLeftButtonPressed)
		{
			num++;
		}
		if (props.IsMiddleButtonPressed)
		{
			num++;
		}
		if (props.IsRightButtonPressed)
		{
			num++;
		}
		if (props.IsXButton1Pressed)
		{
			num++;
		}
		if (props.IsXButton2Pressed)
		{
			num++;
		}
		return num;
	}

	private void ProcessRawEvent(RawPointerEventArgs e)
	{
		e = e ?? throw new ArgumentNullException("e");
		MouseDevice mouseDevice = (MouseDevice)e.Device;
		if (mouseDevice._disposed)
		{
			return;
		}
		PointerPointProperties pointerPointProperties = CreateProperties(e);
		KeyModifiers inputModifiers = e.InputModifiers.ToKeyModifiers();
		switch (e.Type)
		{
		case RawPointerEventType.LeaveWindow:
		case RawPointerEventType.NonClientLeftButtonDown:
			LeaveWindow();
			break;
		case RawPointerEventType.LeftButtonDown:
		case RawPointerEventType.RightButtonDown:
		case RawPointerEventType.MiddleButtonDown:
		case RawPointerEventType.XButton1Down:
		case RawPointerEventType.XButton2Down:
			if (ButtonCount(pointerPointProperties) > 1)
			{
				e.Handled = MouseMove(mouseDevice, e.Timestamp, e.Root, e.Position, pointerPointProperties, inputModifiers, e.IntermediatePoints, e.InputHitTestResult.firstEnabledAncestor);
			}
			else
			{
				e.Handled = MouseDown(mouseDevice, e.Timestamp, e.Root, e.Position, pointerPointProperties, inputModifiers, e.InputHitTestResult.firstEnabledAncestor, e.PlatformInputEventCookie);
			}
			break;
		case RawPointerEventType.LeftButtonUp:
		case RawPointerEventType.RightButtonUp:
		case RawPointerEventType.MiddleButtonUp:
		case RawPointerEventType.XButton1Up:
		case RawPointerEventType.XButton2Up:
			if (ButtonCount(pointerPointProperties) != 0)
			{
				e.Handled = MouseMove(mouseDevice, e.Timestamp, e.Root, e.Position, pointerPointProperties, inputModifiers, e.IntermediatePoints, e.InputHitTestResult.firstEnabledAncestor);
			}
			else
			{
				e.Handled = MouseUp(mouseDevice, e.Timestamp, e.Root, e.Position, pointerPointProperties, inputModifiers, e.InputHitTestResult.firstEnabledAncestor);
			}
			break;
		case RawPointerEventType.Move:
			e.Handled = MouseMove(mouseDevice, e.Timestamp, e.Root, e.Position, pointerPointProperties, inputModifiers, e.IntermediatePoints, e.InputHitTestResult.firstEnabledAncestor);
			break;
		case RawPointerEventType.Wheel:
			e.Handled = MouseWheel(mouseDevice, e.Timestamp, e.Root, e.Position, pointerPointProperties, ((RawMouseWheelEventArgs)e).Delta, inputModifiers, e.InputHitTestResult.firstEnabledAncestor);
			break;
		case RawPointerEventType.Magnify:
			e.Handled = GestureMagnify(mouseDevice, e.Timestamp, e.Root, e.Position, pointerPointProperties, ((RawPointerGestureEventArgs)e).Delta, inputModifiers, e.InputHitTestResult.firstEnabledAncestor);
			break;
		case RawPointerEventType.Rotate:
			e.Handled = GestureRotate(mouseDevice, e.Timestamp, e.Root, e.Position, pointerPointProperties, ((RawPointerGestureEventArgs)e).Delta, inputModifiers, e.InputHitTestResult.firstEnabledAncestor);
			break;
		case RawPointerEventType.Swipe:
			e.Handled = GestureSwipe(mouseDevice, e.Timestamp, e.Root, e.Position, pointerPointProperties, ((RawPointerGestureEventArgs)e).Delta, inputModifiers, e.InputHitTestResult.firstEnabledAncestor);
			break;
		case RawPointerEventType.CancelCapture:
			PlatformCaptureLost();
			break;
		case RawPointerEventType.TouchBegin:
		case RawPointerEventType.TouchUpdate:
		case RawPointerEventType.TouchEnd:
		case RawPointerEventType.TouchCancel:
			break;
		}
	}

	private void LeaveWindow()
	{
	}

	private static PointerPointProperties CreateProperties(RawPointerEventArgs args)
	{
		return new PointerPointProperties(args.InputModifiers, args.Type.ToUpdateKind());
	}

	private bool MouseDown(IMouseDevice device, ulong timestamp, IInputRoot root, Point p, PointerPointProperties properties, KeyModifiers inputModifiers, IInputElement? hitTest, object? platformInputEventCookie)
	{
		device = device ?? throw new ArgumentNullException("device");
		root = root ?? throw new ArgumentNullException("root");
		IInputElement inputElement = _pointer.Captured ?? root.RootElement.InputHitTest(p);
		if (inputElement != null)
		{
			_pointer.Capture(inputElement, CaptureSource.Implicit);
			IPlatformSettings platformSettings = (inputElement as Interactive)?.GetPlatformSettings();
			if (platformSettings != null)
			{
				double totalMilliseconds = platformSettings.GetDoubleTapTime(PointerType.Mouse).TotalMilliseconds;
				Size doubleTapSize = platformSettings.GetDoubleTapSize(PointerType.Mouse);
				if (!_lastClickRect.Contains(p) || (double)(timestamp - _lastClickTime) > totalMilliseconds)
				{
					_clickCount = 0;
				}
				_clickCount++;
				_lastClickTime = timestamp;
				_lastClickRect = new Rect(p, default(Size)).Inflate(new Thickness(doubleTapSize.Width / 2.0, doubleTapSize.Height / 2.0));
			}
			_lastMouseDownButton = properties.PointerUpdateKind.GetMouseButton();
			PointerPressedEventArgs e = new PointerPressedEventArgs(inputElement, _pointer, root.RootElement, p, timestamp, properties, inputModifiers, _clickCount, platformInputEventCookie);
			inputElement.RaiseEvent(e);
			return e.Handled;
		}
		return false;
	}

	private bool MouseMove(IMouseDevice device, ulong timestamp, IInputRoot root, Point p, PointerPointProperties properties, KeyModifiers inputModifiers, Lazy<IReadOnlyList<RawPointerPoint>?>? intermediatePoints, IInputElement? hitTest)
	{
		device = device ?? throw new ArgumentNullException("device");
		root = root ?? throw new ArgumentNullException("root");
		IInputElement inputElement = _pointer.CapturedGestureRecognizer?.Target ?? _pointer.Captured ?? hitTest;
		if (inputElement != null)
		{
			PointerEventArgs e = new PointerEventArgs(InputElement.PointerMovedEvent, inputElement, _pointer, root.RootElement, p, timestamp, properties, inputModifiers, intermediatePoints);
			GestureRecognizer capturedGestureRecognizer = _pointer.CapturedGestureRecognizer;
			if (capturedGestureRecognizer != null)
			{
				capturedGestureRecognizer.PointerMovedInternal(e);
			}
			else
			{
				inputElement.RaiseEvent(e);
			}
			return e.Handled;
		}
		return false;
	}

	private bool MouseUp(IMouseDevice device, ulong timestamp, IInputRoot root, Point p, PointerPointProperties props, KeyModifiers inputModifiers, IInputElement? hitTest)
	{
		device = device ?? throw new ArgumentNullException("device");
		root = root ?? throw new ArgumentNullException("root");
		IInputElement inputElement = _pointer.CapturedGestureRecognizer?.Target ?? _pointer.Captured ?? hitTest;
		if (inputElement != null)
		{
			PointerReleasedEventArgs e = new PointerReleasedEventArgs(inputElement, _pointer, root.RootElement, p, timestamp, props, inputModifiers, _lastMouseDownButton);
			try
			{
				GestureRecognizer capturedGestureRecognizer = _pointer.CapturedGestureRecognizer;
				if (capturedGestureRecognizer != null)
				{
					capturedGestureRecognizer.PointerReleasedInternal(e);
				}
				else
				{
					inputElement?.RaiseEvent(e);
				}
			}
			finally
			{
				_pointer.Capture(null, CaptureSource.Implicit);
				_pointer.CaptureGestureRecognizer(null);
				_pointer.IsGestureRecognitionSkipped = false;
				_lastMouseDownButton = MouseButton.None;
			}
			return e.Handled;
		}
		return false;
	}

	private bool MouseWheel(IMouseDevice device, ulong timestamp, IInputRoot root, Point p, PointerPointProperties props, Vector delta, KeyModifiers inputModifiers, IInputElement? hitTest)
	{
		device = device ?? throw new ArgumentNullException("device");
		root = root ?? throw new ArgumentNullException("root");
		IInputElement inputElement = _pointer.Captured ?? hitTest;
		if (inputElement != null)
		{
			PointerWheelEventArgs e = new PointerWheelEventArgs(inputElement, _pointer, root.RootElement, p, timestamp, props, inputModifiers, delta);
			inputElement?.RaiseEvent(e);
			return e.Handled;
		}
		return false;
	}

	private bool GestureMagnify(IMouseDevice device, ulong timestamp, IInputRoot root, Point p, PointerPointProperties props, Vector delta, KeyModifiers inputModifiers, IInputElement? hitTest)
	{
		device = device ?? throw new ArgumentNullException("device");
		root = root ?? throw new ArgumentNullException("root");
		IInputElement inputElement = _pointer.Captured ?? hitTest;
		if (inputElement != null)
		{
			PointerDeltaEventArgs e = new PointerDeltaEventArgs(InputElement.PointerTouchPadGestureMagnifyEvent, inputElement, _pointer, root.RootElement, p, timestamp, props, inputModifiers, delta);
			inputElement?.RaiseEvent(e);
			return e.Handled;
		}
		return false;
	}

	private bool GestureRotate(IMouseDevice device, ulong timestamp, IInputRoot root, Point p, PointerPointProperties props, Vector delta, KeyModifiers inputModifiers, IInputElement? hitTest)
	{
		device = device ?? throw new ArgumentNullException("device");
		root = root ?? throw new ArgumentNullException("root");
		IInputElement inputElement = _pointer.Captured ?? hitTest;
		if (inputElement != null)
		{
			PointerDeltaEventArgs e = new PointerDeltaEventArgs(InputElement.PointerTouchPadGestureRotateEvent, inputElement, _pointer, root.RootElement, p, timestamp, props, inputModifiers, delta);
			inputElement?.RaiseEvent(e);
			return e.Handled;
		}
		return false;
	}

	private bool GestureSwipe(IMouseDevice device, ulong timestamp, IInputRoot root, Point p, PointerPointProperties props, Vector delta, KeyModifiers inputModifiers, IInputElement? hitTest)
	{
		device = device ?? throw new ArgumentNullException("device");
		root = root ?? throw new ArgumentNullException("root");
		IInputElement inputElement = _pointer.Captured ?? hitTest;
		if (inputElement != null)
		{
			PointerDeltaEventArgs e = new PointerDeltaEventArgs(InputElement.PointerTouchPadGestureSwipeEvent, inputElement, _pointer, root.RootElement, p, timestamp, props, inputModifiers, delta);
			inputElement?.RaiseEvent(e);
			return e.Handled;
		}
		return false;
	}

	public void Dispose()
	{
		_disposed = true;
		_pointer?.Dispose();
	}

	public IPointer? TryGetPointer(RawPointerEventArgs ev)
	{
		return _pointer;
	}

	internal void PlatformCaptureLost()
	{
		_pointer.PlatformCaptureLost();
	}
}
