using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Input;

/// <summary>
/// Represents a pen/stylus device.
/// </summary>
[PrivateApi]
public class PenDevice : IPenDevice, IPointerDevice, IInputDevice, IDisposable
{
	private readonly Dictionary<long, Pointer> _pointers = new Dictionary<long, Pointer>();

	private readonly bool _releasePointerOnPenUp;

	private int _clickCount;

	private Rect _lastClickRect;

	private ulong _lastClickTime;

	private MouseButton _lastMouseDownButton;

	private bool _disposed;

	public PenDevice(bool releasePointerOnPenUp = false)
	{
		_releasePointerOnPenUp = releasePointerOnPenUp;
	}

	public void ProcessRawEvent(RawInputEventArgs e)
	{
		if (!e.Handled && e is RawPointerEventArgs e2)
		{
			ProcessRawEvent(e2);
		}
	}

	private void ProcessRawEvent(RawPointerEventArgs e)
	{
		e = e ?? throw new ArgumentNullException("e");
		if (!_pointers.TryGetValue(e.RawPointerId, out Pointer value))
		{
			if (e.Type == RawPointerEventType.LeftButtonUp || e.Type == RawPointerEventType.TouchEnd)
			{
				return;
			}
			value = (_pointers[e.RawPointerId] = new Pointer(Pointer.GetNextFreeId(), PointerType.Pen, _pointers.Count == 0));
		}
		PointerPointProperties properties = new PointerPointProperties(e.InputModifiers, e.Type.ToUpdateKind(), e.Point.Twist, e.Point.Pressure, e.Point.XTilt, e.Point.YTilt, e.Point.ContactRect);
		KeyModifiers inputModifiers = e.InputModifiers.ToKeyModifiers();
		bool flag = false;
		try
		{
			switch (e.Type)
			{
			case RawPointerEventType.LeaveWindow:
				flag = true;
				break;
			case RawPointerEventType.LeftButtonDown:
			case RawPointerEventType.RightButtonDown:
			case RawPointerEventType.MiddleButtonDown:
			case RawPointerEventType.XButton1Down:
			case RawPointerEventType.XButton2Down:
				e.Handled = PenDown(value, e.Timestamp, e.Root, e.Position, properties, inputModifiers, e.InputHitTestResult.firstEnabledAncestor, e.PlatformInputEventCookie);
				break;
			case RawPointerEventType.LeftButtonUp:
			case RawPointerEventType.RightButtonUp:
			case RawPointerEventType.MiddleButtonUp:
			case RawPointerEventType.XButton1Up:
			case RawPointerEventType.XButton2Up:
				if (_releasePointerOnPenUp)
				{
					flag = true;
				}
				e.Handled = PenUp(value, e.Timestamp, e.Root, e.Position, properties, inputModifiers, e.InputHitTestResult.firstEnabledAncestor);
				break;
			case RawPointerEventType.Move:
				e.Handled = PenMove(value, e.Timestamp, e.Root, e.Position, properties, inputModifiers, e.InputHitTestResult.firstEnabledAncestor, e.IntermediatePoints);
				break;
			}
		}
		finally
		{
			if (flag)
			{
				value.Dispose();
				_pointers.Remove(e.RawPointerId);
			}
		}
	}

	private bool PenDown(Pointer pointer, ulong timestamp, IInputRoot root, Point p, PointerPointProperties properties, KeyModifiers inputModifiers, IInputElement? hitTest, object? platformInputEventCookie)
	{
		IInputElement inputElement = pointer.Captured ?? hitTest;
		if (inputElement != null)
		{
			pointer.Capture(inputElement, CaptureSource.Implicit);
			IPlatformSettings platformSettings = (inputElement as Interactive)?.GetPlatformSettings();
			if (platformSettings != null)
			{
				double totalMilliseconds = platformSettings.GetDoubleTapTime(PointerType.Pen).TotalMilliseconds;
				Size doubleTapSize = platformSettings.GetDoubleTapSize(PointerType.Pen);
				if (!_lastClickRect.Contains(p) || (double)(timestamp - _lastClickTime) > totalMilliseconds)
				{
					_clickCount = 0;
				}
				_clickCount++;
				_lastClickTime = timestamp;
				_lastClickRect = new Rect(p, default(Size)).Inflate(new Thickness(doubleTapSize.Width / 2.0, doubleTapSize.Height / 2.0));
			}
			_lastMouseDownButton = properties.PointerUpdateKind.GetMouseButton();
			PointerPressedEventArgs e = new PointerPressedEventArgs(inputElement, pointer, root.RootElement, p, timestamp, properties, inputModifiers, _clickCount, platformInputEventCookie);
			inputElement.RaiseEvent(e);
			return e.Handled;
		}
		return false;
	}

	private static bool PenMove(Pointer pointer, ulong timestamp, IInputRoot root, Point p, PointerPointProperties properties, KeyModifiers inputModifiers, IInputElement? hitTest, Lazy<IReadOnlyList<RawPointerPoint>?>? intermediatePoints)
	{
		IInputElement inputElement = pointer.CapturedGestureRecognizer?.Target ?? pointer.Captured ?? hitTest;
		if (inputElement != null)
		{
			PointerEventArgs e = new PointerEventArgs(InputElement.PointerMovedEvent, inputElement, pointer, root.RootElement, p, timestamp, properties, inputModifiers, intermediatePoints);
			GestureRecognizer capturedGestureRecognizer = pointer.CapturedGestureRecognizer;
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

	private bool PenUp(Pointer pointer, ulong timestamp, IInputRoot root, Point p, PointerPointProperties properties, KeyModifiers inputModifiers, IInputElement? hitTest)
	{
		IInputElement inputElement = pointer.CapturedGestureRecognizer?.Target ?? pointer.Captured ?? hitTest;
		if (inputElement != null)
		{
			PointerReleasedEventArgs e = new PointerReleasedEventArgs(inputElement, pointer, root.RootElement, p, timestamp, properties, inputModifiers, _lastMouseDownButton);
			try
			{
				GestureRecognizer capturedGestureRecognizer = pointer.CapturedGestureRecognizer;
				if (capturedGestureRecognizer != null)
				{
					capturedGestureRecognizer.PointerReleasedInternal(e);
				}
				else
				{
					inputElement.RaiseEvent(e);
				}
			}
			finally
			{
				pointer.Capture(null, CaptureSource.Implicit);
				pointer.CaptureGestureRecognizer(null);
				pointer.IsGestureRecognitionSkipped = false;
				_lastMouseDownButton = MouseButton.None;
			}
			return e.Handled;
		}
		return false;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		List<Pointer> list = _pointers.Values.ToList();
		_pointers.Clear();
		_disposed = true;
		foreach (Pointer item in list)
		{
			item.Dispose();
		}
	}

	public IPointer? TryGetPointer(RawPointerEventArgs ev)
	{
		if (!_pointers.TryGetValue(ev.RawPointerId, out Pointer value))
		{
			return null;
		}
		return value;
	}
}
