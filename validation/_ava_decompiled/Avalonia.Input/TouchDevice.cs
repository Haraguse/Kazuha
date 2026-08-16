using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Input;

/// <summary>
/// Handles raw touch events
/// </summary>
/// <remarks>
/// This class is supposed to be used on per-toplevel basis, don't use a shared one
/// </remarks>
[PrivateApi]
public class TouchDevice : IPointerDevice, IInputDevice, IDisposable
{
	private readonly Dictionary<long, Pointer> _pointers = new Dictionary<long, Pointer>();

	private bool _disposed;

	private int _clickCount;

	private Rect _lastClickRect;

	private ulong _lastClickTime;

	private static RawInputModifiers GetModifiers(RawInputModifiers modifiers, bool isLeftButtonDown)
	{
		RawInputModifiers rawInputModifiers = (modifiers &= RawInputModifiers.KeyboardMask);
		if (isLeftButtonDown)
		{
			rawInputModifiers |= RawInputModifiers.LeftMouseButton;
		}
		return rawInputModifiers;
	}

	public void ProcessRawEvent(RawInputEventArgs ev)
	{
		if (ev.Handled || _disposed)
		{
			return;
		}
		RawPointerEventArgs e = (RawPointerEventArgs)ev;
		if (!_pointers.TryGetValue(e.RawPointerId, out Pointer value))
		{
			if (e.Type == RawPointerEventType.TouchEnd)
			{
				return;
			}
			IInputElement item = e.InputHitTestResult.firstEnabledAncestor;
			value = (_pointers[e.RawPointerId] = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, _pointers.Count == 0));
			value.Capture(item, CaptureSource.Implicit);
		}
		IInputElement inputElement = value.Captured ?? e.InputHitTestResult.firstEnabledAncestor ?? e.Root.RootElement;
		IInputElement inputElement2 = value.CapturedGestureRecognizer?.Target;
		PointerUpdateKind kind = e.Type.ToUpdateKind();
		KeyModifiers modifiers = e.InputModifiers.ToKeyModifiers();
		if (e.Type == RawPointerEventType.TouchBegin)
		{
			if (_pointers.Count > 1)
			{
				_clickCount = 1;
				_lastClickTime = 0uL;
				_lastClickRect = default(Rect);
			}
			else
			{
				IPlatformSettings platformSettings = (inputElement as Interactive)?.GetPlatformSettings();
				if (platformSettings != null)
				{
					double totalMilliseconds = platformSettings.GetDoubleTapTime(PointerType.Touch).TotalMilliseconds;
					Size doubleTapSize = platformSettings.GetDoubleTapSize(PointerType.Touch);
					if (!_lastClickRect.Contains(e.Position) || (double)(ev.Timestamp - _lastClickTime) > totalMilliseconds)
					{
						_clickCount = 0;
					}
					_clickCount++;
					_lastClickTime = ev.Timestamp;
					_lastClickRect = new Rect(e.Position, default(Size)).Inflate(new Thickness(doubleTapSize.Width / 2.0, doubleTapSize.Height / 2.0));
				}
			}
			inputElement.RaiseEvent(new PointerPressedEventArgs(inputElement, value, e.Root.RootElement, e.Position, ev.Timestamp, new PointerPointProperties(GetModifiers(e.InputModifiers, isLeftButtonDown: true), kind, e.Point), modifiers, _clickCount, e.PlatformInputEventCookie));
		}
		if (e.Type == RawPointerEventType.TouchEnd)
		{
			_pointers.Remove(e.RawPointerId);
			using (value)
			{
				inputElement = inputElement2 ?? inputElement;
				PointerReleasedEventArgs e2 = new PointerReleasedEventArgs(inputElement, value, e.Root.RootElement, e.Position, ev.Timestamp, new PointerPointProperties(GetModifiers(e.InputModifiers, isLeftButtonDown: false), kind, e.Point), modifiers, MouseButton.Left);
				if (inputElement2 != null)
				{
					value?.CapturedGestureRecognizer?.PointerReleasedInternal(e2);
				}
				else
				{
					inputElement.RaiseEvent(e2);
				}
				value?.Capture(null, CaptureSource.Implicit);
			}
		}
		if (e.Type == RawPointerEventType.TouchCancel)
		{
			_pointers.Remove(e.RawPointerId);
			using (value)
			{
				value?.Capture(null, CaptureSource.Platform);
				value?.CaptureGestureRecognizer(null);
				if (value != null)
				{
					value.IsGestureRecognitionSkipped = false;
				}
			}
		}
		if (e.Type == RawPointerEventType.TouchUpdate)
		{
			inputElement = inputElement2 ?? inputElement;
			PointerEventArgs e3 = new PointerEventArgs(InputElement.PointerMovedEvent, inputElement, value, e.Root.RootElement, e.Position, ev.Timestamp, new PointerPointProperties(GetModifiers(e.InputModifiers, isLeftButtonDown: true), kind, e.Point), modifiers, e.IntermediatePoints);
			if (inputElement2 != null)
			{
				value?.CapturedGestureRecognizer?.PointerMovedInternal(e3);
			}
			else
			{
				inputElement.RaiseEvent(e3);
			}
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			Pointer[] array = _pointers.Values.ToArray();
			_pointers.Clear();
			_disposed = true;
			Pointer[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].Dispose();
			}
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

	internal void PlatformCaptureLost()
	{
		foreach (Pointer value in _pointers.Values)
		{
			value.Capture(null);
		}
	}
}
