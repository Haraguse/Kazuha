using System;
using Avalonia.Input.GestureRecognizers;

namespace Avalonia.Input;

public class PullGestureRecognizer : GestureRecognizer
{
	internal static int MinPullDetectionSize = 50;

	private Point _initialPosition;

	private int _gestureId;

	private IPointer? _tracking;

	private bool _pullInProgress;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.PullGestureRecognizer.PullDirection" /> property.
	/// </summary>
	public static readonly StyledProperty<PullDirection> PullDirectionProperty = AvaloniaProperty.Register<PullGestureRecognizer, PullDirection>("PullDirection", PullDirection.TopToBottom);

	public PullDirection PullDirection
	{
		get
		{
			return GetValue(PullDirectionProperty);
		}
		set
		{
			SetValue(PullDirectionProperty, value);
		}
	}

	public PullGestureRecognizer(PullDirection pullDirection)
	{
		PullDirection = pullDirection;
	}

	public PullGestureRecognizer()
	{
	}

	protected override void PointerCaptureLost(IPointer pointer)
	{
		if (_tracking == pointer)
		{
			EndPull();
		}
	}

	protected override void PointerMoved(PointerEventArgs e)
	{
		if (_tracking != e.Pointer || !(base.Target is Visual relativeTo))
		{
			return;
		}
		Point position = e.GetPosition(relativeTo);
		Capture(e.Pointer);
		Vector delta = default(Vector);
		switch (PullDirection)
		{
		case PullDirection.TopToBottom:
			if (position.Y > _initialPosition.Y)
			{
				delta = new Vector(0.0, position.Y - _initialPosition.Y);
			}
			break;
		case PullDirection.BottomToTop:
			if (position.Y < _initialPosition.Y)
			{
				delta = new Vector(0.0, _initialPosition.Y - position.Y);
			}
			break;
		case PullDirection.LeftToRight:
			if (position.X > _initialPosition.X)
			{
				delta = new Vector(position.X - _initialPosition.X, 0.0);
			}
			break;
		case PullDirection.RightToLeft:
			if (position.X < _initialPosition.X)
			{
				delta = new Vector(_initialPosition.X - position.X, 0.0);
			}
			break;
		}
		_pullInProgress = true;
		PullGestureEventArgs e2 = new PullGestureEventArgs(_gestureId, delta, PullDirection);
		base.Target?.RaiseEvent(e2);
		e.Handled = e2.Handled;
	}

	protected override void PointerPressed(PointerPressedEventArgs e)
	{
		if (base.Target != null && base.Target is Visual visual && (e.Pointer.Type == PointerType.Touch || e.Pointer.Type == PointerType.Pen))
		{
			Point position = e.GetPosition(visual);
			bool flag = false;
			Rect bounds = visual.Bounds;
			switch (PullDirection)
			{
			case PullDirection.TopToBottom:
				flag = position.Y < Math.Max(MinPullDetectionSize, bounds.Height * 0.1);
				break;
			case PullDirection.BottomToTop:
				flag = position.Y > Math.Min(bounds.Height - (double)MinPullDetectionSize, bounds.Height - bounds.Height * 0.1);
				break;
			case PullDirection.LeftToRight:
				flag = position.X < Math.Max(MinPullDetectionSize, bounds.Width * 0.1);
				break;
			case PullDirection.RightToLeft:
				flag = position.X > Math.Min(bounds.Width - (double)MinPullDetectionSize, bounds.Width - bounds.Width * 0.1);
				break;
			}
			if (flag)
			{
				_gestureId = PullGestureEventArgs.GetNextFreeId();
				_tracking = e.Pointer;
				_initialPosition = position;
			}
		}
	}

	protected override void PointerReleased(PointerReleasedEventArgs e)
	{
		if (_tracking == e.Pointer && _pullInProgress)
		{
			EndPull();
		}
	}

	private void EndPull()
	{
		_tracking = null;
		_initialPosition = default(Point);
		_pullInProgress = false;
		base.Target?.RaiseEvent(new PullGestureEndedEventArgs(_gestureId, PullDirection));
	}
}
