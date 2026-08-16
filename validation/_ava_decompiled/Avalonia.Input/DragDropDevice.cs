using System.Linq;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Avalonia.Input;

[PrivateApi]
public class DragDropDevice : IDragDropDevice, IInputDevice
{
	public static readonly DragDropDevice Instance = new DragDropDevice();

	private Interactive? _lastTarget;

	private static Interactive? GetTarget(IInputRoot root, Point local)
	{
		Interactive interactive = (root.RootElement?.InputHitTest(local) as Visual)?.GetSelfAndVisualAncestors()?.OfType<Interactive>()?.FirstOrDefault();
		if (interactive != null && DragDrop.GetAllowDrop(interactive))
		{
			return interactive;
		}
		return null;
	}

	private static DragDropEffects RaiseDragEvent(Interactive? target, IInputRoot inputRoot, Point point, RoutedEvent<DragEventArgs> routedEvent, DragDropEffects operation, IDataTransfer dataTransfer, KeyModifiers modifiers)
	{
		if (target == null)
		{
			return DragDropEffects.None;
		}
		Point? point2 = inputRoot.RootElement.TranslatePoint(point, target);
		if (!point2.HasValue)
		{
			return DragDropEffects.None;
		}
		DragEventArgs e = new DragEventArgs(routedEvent, dataTransfer, target, point2.Value, modifiers)
		{
			RoutedEvent = routedEvent,
			DragEffects = operation
		};
		target.RaiseEvent(e);
		return e.DragEffects;
	}

	private DragDropEffects DragEnter(IInputRoot inputRoot, Point point, IDataTransfer data, DragDropEffects effects, KeyModifiers modifiers)
	{
		_lastTarget = GetTarget(inputRoot, point);
		return RaiseDragEvent(_lastTarget, inputRoot, point, DragDrop.DragEnterEvent, effects, data, modifiers);
	}

	private DragDropEffects DragOver(IInputRoot inputRoot, Point point, IDataTransfer data, DragDropEffects effects, KeyModifiers modifiers)
	{
		Interactive target = GetTarget(inputRoot, point);
		if (target == _lastTarget)
		{
			return RaiseDragEvent(target, inputRoot, point, DragDrop.DragOverEvent, effects, data, modifiers);
		}
		try
		{
			if (_lastTarget != null)
			{
				RaiseDragEvent(_lastTarget, inputRoot, point, DragDrop.DragLeaveEvent, effects, data, modifiers);
			}
			return RaiseDragEvent(target, inputRoot, point, DragDrop.DragEnterEvent, effects, data, modifiers);
		}
		finally
		{
			_lastTarget = target;
		}
	}

	private void DragLeave(IInputRoot inputRoot, Point point, IDataTransfer data, DragDropEffects effects, KeyModifiers modifiers)
	{
		if (_lastTarget == null)
		{
			return;
		}
		try
		{
			RaiseDragEvent(_lastTarget, inputRoot, point, DragDrop.DragLeaveEvent, effects, data, modifiers);
		}
		finally
		{
			_lastTarget = null;
		}
	}

	private DragDropEffects Drop(IInputRoot inputRoot, Point point, IDataTransfer data, DragDropEffects effects, KeyModifiers modifiers)
	{
		try
		{
			return RaiseDragEvent(_lastTarget, inputRoot, point, DragDrop.DropEvent, effects, data, modifiers);
		}
		finally
		{
			_lastTarget = null;
		}
	}

	public void ProcessRawEvent(RawInputEventArgs e)
	{
		if (!e.Handled && e is RawDragEvent e2)
		{
			ProcessRawEvent(e2);
		}
	}

	private void ProcessRawEvent(RawDragEvent e)
	{
		switch (e.Type)
		{
		case RawDragEventType.DragEnter:
			e.Effects = DragEnter(e.Root, e.Location, e.DataTransfer, e.Effects, e.KeyModifiers);
			break;
		case RawDragEventType.DragOver:
			e.Effects = DragOver(e.Root, e.Location, e.DataTransfer, e.Effects, e.KeyModifiers);
			break;
		case RawDragEventType.DragLeave:
			DragLeave(e.Root, e.Location, e.DataTransfer, e.Effects, e.KeyModifiers);
			break;
		case RawDragEventType.Drop:
			e.Effects = Drop(e.Root, e.Location, e.DataTransfer, e.Effects, e.KeyModifiers);
			break;
		}
	}
}
