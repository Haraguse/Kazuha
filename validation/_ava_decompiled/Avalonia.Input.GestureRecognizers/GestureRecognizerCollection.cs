using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Reactive;

namespace Avalonia.Input.GestureRecognizers;

public class GestureRecognizerCollection : IReadOnlyCollection<GestureRecognizer>, IEnumerable<GestureRecognizer>, IEnumerable
{
	private readonly IInputElement _inputElement;

	private List<GestureRecognizer>? _recognizers;

	private static readonly List<GestureRecognizer> s_Empty = new List<GestureRecognizer>();

	public int Count => _recognizers?.Count ?? 0;

	public GestureRecognizerCollection(IInputElement inputElement)
	{
		_inputElement = inputElement;
	}

	public void Add(GestureRecognizer recognizer)
	{
		if (_recognizers == null)
		{
			_recognizers = new List<GestureRecognizer>();
		}
		_recognizers.Add(recognizer);
		recognizer.Target = _inputElement;
		if (!(_inputElement is ILogical parent))
		{
			return;
		}
		if (recognizer == null)
		{
			return;
		}
		((ISetLogicalParent)recognizer).SetParent(parent);
		StyledElement styleableRecognizer = recognizer;
		if (styleableRecognizer != null && _inputElement is StyledElement o)
		{
			o.GetObservable(StyledElement.TemplatedParentProperty).Subscribe(delegate(AvaloniaObject templatedParent)
			{
				styleableRecognizer.TemplatedParent = templatedParent;
			});
		}
	}

	public bool Remove(GestureRecognizer recognizer)
	{
		if (_recognizers == null)
		{
			return false;
		}
		bool num = _recognizers.Remove(recognizer);
		if (num)
		{
			recognizer.Target = null;
			((ISetLogicalParent)recognizer)?.SetParent((ILogical?)null);
		}
		return num;
	}

	public IEnumerator<GestureRecognizer> GetEnumerator()
	{
		return _recognizers?.GetEnumerator() ?? s_Empty.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	internal bool HandlePointerPressed(PointerPressedEventArgs e)
	{
		if (_recognizers == null)
		{
			return false;
		}
		foreach (GestureRecognizer recognizer in _recognizers)
		{
			recognizer.PointerPressedInternal(e);
		}
		return e.Handled;
	}

	internal void HandleCaptureLost(IPointer pointer)
	{
		if (_recognizers == null || !(pointer is Pointer pointer2))
		{
			return;
		}
		foreach (GestureRecognizer recognizer in _recognizers)
		{
			if (pointer2.CapturedGestureRecognizer != recognizer)
			{
				recognizer.PointerCaptureLostInternal(pointer);
			}
		}
	}

	internal bool HandlePointerReleased(PointerReleasedEventArgs e)
	{
		if (_recognizers == null)
		{
			return false;
		}
		Pointer pointer = e.Pointer as Pointer;
		foreach (GestureRecognizer recognizer in _recognizers)
		{
			if (pointer?.CapturedGestureRecognizer != null)
			{
				break;
			}
			recognizer.PointerReleasedInternal(e);
		}
		return e.Handled;
	}

	internal bool HandlePointerMoved(PointerEventArgs e)
	{
		if (_recognizers == null)
		{
			return false;
		}
		Pointer pointer = e.Pointer as Pointer;
		foreach (GestureRecognizer recognizer in _recognizers)
		{
			if (pointer?.CapturedGestureRecognizer != null)
			{
				break;
			}
			recognizer.PointerMovedInternal(e);
		}
		return e.Handled;
	}
}
