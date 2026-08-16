using System;
using Avalonia.Input.Raw;

namespace Avalonia.Input;

internal class PointerOverPreProcessor : IObserver<RawInputEventArgs>
{
	private IPointerDevice? _lastActivePointerDevice;

	private (IPointer pointer, PixelPoint position)? _currentPointer;

	private PixelPoint? _lastKnownPosition;

	private readonly IInputRoot _inputRoot;

	public PixelPoint? LastPosition => _lastKnownPosition;

	public PointerOverPreProcessor(IInputRoot inputRoot)
	{
		_inputRoot = inputRoot ?? throw new ArgumentNullException("inputRoot");
	}

	public void OnCompleted()
	{
		ClearPointerOver();
	}

	public void OnError(Exception error)
	{
	}

	public void OnNext(RawInputEventArgs value)
	{
		if (value is RawDragEvent rawDragEvent)
		{
			_lastKnownPosition = _inputRoot.RootElement.PointToScreen(rawDragEvent.Location);
		}
		else
		{
			if (!(value is RawPointerEventArgs e) || e.Root != _inputRoot || !(value.Device is IPointerDevice pointerDevice))
			{
				return;
			}
			if (pointerDevice != _lastActivePointerDevice)
			{
				ClearPointerOver();
				_lastActivePointerDevice = pointerDevice;
			}
			RawPointerEventType type = e.Type;
			if ((type == RawPointerEventType.LeaveWindow || type == RawPointerEventType.NonClientLeftButtonDown || (uint)(type - 16) <= 1u) ? true : false)
			{
				(IPointer, PixelPoint)? currentPointer = _currentPointer;
				if (currentPointer.HasValue)
				{
					(IPointer, PixelPoint) valueOrDefault = currentPointer.GetValueOrDefault();
					IPointer item = valueOrDefault.Item1;
					PixelPoint item2 = valueOrDefault.Item2;
					_currentPointer = null;
					ClearPointerOver(item, e.Root, 0uL, PointToClient(e.Root, item2), new PointerPointProperties(e.InputModifiers, e.Type.ToUpdateKind()), e.InputModifiers.ToKeyModifiers());
				}
				return;
			}
			type = e.Type;
			if ((uint)(type - 14) <= 1u)
			{
				InputElement rootElement = e.Root.RootElement;
				if (rootElement != null)
				{
					_lastKnownPosition = rootElement.PointToScreen(e.Position);
					return;
				}
			}
			type = e.Type;
			if ((uint)(type - 16) <= 1u)
			{
				IPointer pointer = pointerDevice.TryGetPointer(e);
				if (pointer != null)
				{
					_currentPointer = null;
					ClearPointerOver(pointer, e.Root, 0uL, e.Position, new PointerPointProperties(e.InputModifiers, e.Type.ToUpdateKind()), e.InputModifiers.ToKeyModifiers());
					return;
				}
			}
			IPointer pointer2 = pointerDevice.TryGetPointer(e);
			if (pointer2 != null && pointer2.Type != PointerType.Touch && e.Type != RawPointerEventType.CancelCapture)
			{
				IInputElement effectivePointerOverElement = GetEffectivePointerOverElement(e.InputHitTestResult.firstEnabledAncestor, pointer2.Captured);
				SetPointerOver(pointer2, e.Root, effectivePointerOverElement, e.Timestamp, e.Position, new PointerPointProperties(e.InputModifiers, e.Type.ToUpdateKind()), e.InputModifiers.ToKeyModifiers());
			}
		}
	}

	public void SceneInvalidated(Rect dirtyRect)
	{
		(IPointer, PixelPoint)? currentPointer = _currentPointer;
		if (currentPointer.HasValue)
		{
			(IPointer, PixelPoint) valueOrDefault = currentPointer.GetValueOrDefault();
			IPointer item = valueOrDefault.Item1;
			PixelPoint item2 = valueOrDefault.Item2;
			Point point = PointToClient(_inputRoot, item2);
			if (dirtyRect.Contains(point))
			{
				IInputElement effectivePointerOverElement = GetEffectivePointerOverElement(_inputRoot.RootElement.InputHitTest(point), item.Captured);
				SetPointerOver(item, _inputRoot, effectivePointerOverElement, 0uL, point, PointerPointProperties.None, KeyModifiers.None);
			}
			else if (!_inputRoot.RootElement.Bounds.Contains(point))
			{
				ClearPointerOver(item, _inputRoot, 0uL, point, PointerPointProperties.None, KeyModifiers.None);
			}
		}
	}

	private static IInputElement? GetEffectivePointerOverElement(IInputElement? hitTestElement, IInputElement? captured)
	{
		if (captured == null || hitTestElement == captured)
		{
			return hitTestElement;
		}
		return null;
	}

	private void ClearPointerOver()
	{
		(IPointer, PixelPoint)? currentPointer = _currentPointer;
		if (currentPointer.HasValue)
		{
			(IPointer, PixelPoint) valueOrDefault = currentPointer.GetValueOrDefault();
			IPointer item = valueOrDefault.Item1;
			PixelPoint item2 = valueOrDefault.Item2;
			Point value = PointToClient(_inputRoot, item2);
			ClearPointerOver(item, _inputRoot, 0uL, value, PointerPointProperties.None, KeyModifiers.None);
		}
		_currentPointer = null;
		_lastActivePointerDevice = null;
	}

	private void ClearPointerOver(IPointer pointer, IInputRoot root, ulong timestamp, Point? position, PointerPointProperties properties, KeyModifiers inputModifiers)
	{
		IInputElement inputElement = root.PointerOverElement;
		if (inputElement != null)
		{
			PointerEventArgs e = new PointerEventArgs(InputElement.PointerExitedEvent, inputElement, pointer, position.HasValue ? root.RootElement : null, position.HasValue ? position.Value : default(Point), timestamp, properties, inputModifiers);
			if (inputElement is Visual { IsAttachedToVisualTree: false } && root.RootElement.IsPointerOver)
			{
				ClearChildrenPointerOver(e, root.RootElement, clearRoot: true);
			}
			while (inputElement != null)
			{
				e.Source = inputElement;
				e.Handled = false;
				inputElement.RaiseEvent(e);
				inputElement = GetVisualParent(inputElement);
			}
			root.PointerOverElement = null;
			root.CursorElement = pointer.Captured;
			_lastActivePointerDevice = null;
			_currentPointer = null;
		}
	}

	private void ClearChildrenPointerOver(PointerEventArgs e, IInputElement element, bool clearRoot)
	{
		if (element is Visual visual)
		{
			foreach (Visual visualChild in visual.VisualChildren)
			{
				if (visualChild is IInputElement { IsPointerOver: not false } inputElement)
				{
					ClearChildrenPointerOver(e, inputElement, clearRoot: true);
					break;
				}
			}
		}
		if (clearRoot)
		{
			e.Source = element;
			e.Handled = false;
			element.RaiseEvent(e);
		}
	}

	private void SetPointerOver(IPointer pointer, IInputRoot root, IInputElement? element, ulong timestamp, Point position, PointerPointProperties properties, KeyModifiers inputModifiers)
	{
		IInputElement pointerOverElement = root.PointerOverElement;
		PixelPoint pixelPoint = root.RootElement.PointToScreen(position);
		_lastKnownPosition = pixelPoint;
		if (element != pointerOverElement)
		{
			if (element != null)
			{
				SetPointerOverToElement(pointer, root, element, timestamp, position, properties, inputModifiers);
			}
			else
			{
				ClearPointerOver(pointer, root, timestamp, position, properties, inputModifiers);
			}
		}
		_currentPointer = (pointer, pixelPoint);
	}

	private void SetPointerOverToElement(IPointer pointer, IInputRoot root, IInputElement element, ulong timestamp, Point position, PointerPointProperties properties, KeyModifiers inputModifiers)
	{
		IInputElement inputElement = null;
		IInputElement inputElement2;
		for (inputElement2 = element; inputElement2 != null; inputElement2 = GetVisualParent(inputElement2))
		{
			if (inputElement2.IsPointerOver)
			{
				inputElement = inputElement2;
				break;
			}
		}
		inputElement2 = root.PointerOverElement;
		PointerEventArgs e = new PointerEventArgs(InputElement.PointerExitedEvent, inputElement2, pointer, root.RootElement, position, timestamp, properties, inputModifiers);
		if (inputElement2 is Visual visual && inputElement != null && !visual.IsAttachedToVisualTree)
		{
			ClearChildrenPointerOver(e, inputElement, clearRoot: false);
		}
		while (inputElement2 != null && inputElement2 != inputElement)
		{
			e.Source = inputElement2;
			e.Handled = false;
			inputElement2.RaiseEvent(e);
			inputElement2 = GetVisualParent(inputElement2);
		}
		IInputElement inputElement3 = (root.PointerOverElement = element);
		inputElement2 = inputElement3;
		root.CursorElement = pointer.Captured ?? element;
		e.RoutedEvent = InputElement.PointerEnteredEvent;
		while (inputElement2 != null && inputElement2 != inputElement)
		{
			e.Source = inputElement2;
			e.Handled = false;
			inputElement2.RaiseEvent(e);
			inputElement2 = GetVisualParent(inputElement2);
		}
	}

	private static IInputElement? GetVisualParent(IInputElement e)
	{
		return (e as Visual)?.VisualParent as IInputElement;
	}

	private static Point PointToClient(IInputRoot root, PixelPoint p)
	{
		return root.RootElement.PointToClient(p);
	}
}
