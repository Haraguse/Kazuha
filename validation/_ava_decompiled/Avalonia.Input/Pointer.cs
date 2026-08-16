using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input.GestureRecognizers;
using Avalonia.VisualTree;

namespace Avalonia.Input;

public class Pointer : IPointer, IDisposable
{
	private static int s_NextFreePointerId = 1000;

	public int Id { get; }

	public IInputElement? Captured { get; private set; }

	public PointerType Type { get; }

	public bool IsPrimary { get; }

	/// <summary>
	/// Gets the gesture recognizer that is currently capturing by the pointer, if any.
	/// </summary>
	internal GestureRecognizer? CapturedGestureRecognizer { get; private set; }

	public bool IsGestureRecognitionSkipped { get; set; }

	internal CaptureSource CaptureSource { get; private set; } = CaptureSource.Platform;

	public static int GetNextFreeId()
	{
		return s_NextFreePointerId++;
	}

	public Pointer(int id, PointerType type, bool isPrimary)
	{
		Id = id;
		Type = type;
		IsPrimary = isPrimary;
	}

	private static IInputElement? FindCommonParent(IInputElement? control1, IInputElement? control2)
	{
		if (!(control1 is Visual visual) || !(control2 is Visual visual2))
		{
			return null;
		}
		HashSet<IInputElement> hashSet = new HashSet<IInputElement>(visual.GetSelfAndVisualAncestors().OfType<IInputElement>());
		return visual2.GetSelfAndVisualAncestors().OfType<IInputElement>().FirstOrDefault(hashSet.Contains);
	}

	protected virtual void PlatformCapture(IInputElement? element)
	{
	}

	internal void PlatformCaptureLost()
	{
		if (Captured != null)
		{
			Capture(null, CaptureSource.Platform);
		}
	}

	public void Capture(IInputElement? control)
	{
		Capture(control, CaptureSource.Explicit);
	}

	internal void Capture(IInputElement? control, CaptureSource source)
	{
		IInputElement captured = Captured;
		CaptureSource captureSource = CaptureSource;
		if (captured == control && captureSource == source)
		{
			return;
		}
		Visual visual = captured as Visual;
		Visual visual2 = control as Visual;
		IInputElement inputElement = null;
		if (visual != null || visual2 != null)
		{
			inputElement = FindCommonParent(control, captured);
			foreach (IInputElement item in (visual ?? visual2).GetSelfAndVisualAncestors().OfType<IInputElement>())
			{
				PointerCaptureChangingEventArgs e = new PointerCaptureChangingEventArgs(item, this, control, source);
				item.RaiseEvent(e);
				if (e.Handled)
				{
					return;
				}
				if (item == inputElement)
				{
					break;
				}
			}
		}
		if (visual != null)
		{
			visual.DetachedFromVisualTree -= OnCaptureDetached;
		}
		Captured = control;
		CaptureSource = source;
		if (captured != control && source != CaptureSource.Platform)
		{
			PlatformCapture(control);
		}
		if (visual != null)
		{
			foreach (IInputElement item2 in visual.GetSelfAndVisualAncestors().OfType<IInputElement>())
			{
				if (item2 == inputElement)
				{
					break;
				}
				item2.RaiseEvent(new PointerCaptureLostEventArgs(item2, this));
			}
		}
		if (visual2 != null)
		{
			visual2.DetachedFromVisualTree += OnCaptureDetached;
		}
		if (Captured != null)
		{
			CaptureGestureRecognizer(null);
		}
		if (Captured == null && CapturedGestureRecognizer == null)
		{
			IsGestureRecognitionSkipped = false;
		}
		if (Type != PointerType.Touch)
		{
			IInputRoot obj = visual?.PresentationSource?.InputRoot;
			IInputRoot inputRoot = visual2?.PresentationSource?.InputRoot;
			obj?.PointerOverInvalidated();
			if (obj != inputRoot)
			{
				inputRoot?.PointerOverInvalidated();
			}
		}
	}

	private static IInputElement? GetNextCapture(Visual? parent)
	{
		return (parent as IInputElement) ?? parent.FindAncestorOfType<IInputElement>();
	}

	private void OnCaptureDetached(object? sender, VisualTreeAttachmentEventArgs e)
	{
		Capture(GetNextCapture(e.AttachmentPoint));
	}

	public void Dispose()
	{
	}

	/// <summary>
	/// Captures pointer input to the specified gesture recognizer.
	/// </summary>
	/// <param name="gestureRecognizer">The gesture recognizer.</param>
	internal void CaptureGestureRecognizer(GestureRecognizer? gestureRecognizer)
	{
		if (CapturedGestureRecognizer != gestureRecognizer)
		{
			CapturedGestureRecognizer?.PointerCaptureLostInternal(this);
		}
		CapturedGestureRecognizer = gestureRecognizer;
		if (gestureRecognizer != null)
		{
			Capture(null);
		}
	}
}
