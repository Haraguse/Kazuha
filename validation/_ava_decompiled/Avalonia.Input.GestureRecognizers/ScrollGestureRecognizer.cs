using System;
using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Input.GestureRecognizers;

public class ScrollGestureRecognizer : GestureRecognizer
{
	internal const double InertialScrollSpeedEnd = 5.0;

	public const double InertialResistance = 0.15;

	private bool _canHorizontallyScroll;

	private bool _canVerticallyScroll;

	private bool _isScrollInertiaEnabled;

	private Vector? _offset;

	private Size? _viewport;

	private Size? _extent;

	private static readonly int s_defaultScrollStartDistance = (int)((AvaloniaLocator.Current?.GetService<IPlatformSettings>()?.GetTapSize(PointerType.Touch).Height ?? 10.0) / 2.0);

	private int _scrollStartDistance = s_defaultScrollStartDistance;

	private bool _scrolling;

	private Point _trackedRootPoint;

	private IPointer? _tracking;

	private Stopwatch? _stopWatch;

	private int _gestureId;

	private Point _pointerPressedPoint;

	private VelocityTracker? _velocityTracker;

	private Vector? _inertia;

	private ulong? _lastMoveTimestamp;

	private TimeSpan _lastTime;

	private TimeSpan _inertiaStartTime;

	private int _currentInertiaGestureId;

	private Point _delta;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.GestureRecognizers.ScrollGestureRecognizer.CanHorizontallyScroll" /> property.
	/// </summary>
	public static readonly DirectProperty<ScrollGestureRecognizer, bool> CanHorizontallyScrollProperty = AvaloniaProperty.RegisterDirect("CanHorizontallyScroll", (ScrollGestureRecognizer o) => o.CanHorizontallyScroll, delegate(ScrollGestureRecognizer o, bool v)
	{
		o.CanHorizontallyScroll = v;
	}, unsetValue: false);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.GestureRecognizers.ScrollGestureRecognizer.CanVerticallyScroll" /> property.
	/// </summary>
	public static readonly DirectProperty<ScrollGestureRecognizer, bool> CanVerticallyScrollProperty = AvaloniaProperty.RegisterDirect("CanVerticallyScroll", (ScrollGestureRecognizer o) => o.CanVerticallyScroll, delegate(ScrollGestureRecognizer o, bool v)
	{
		o.CanVerticallyScroll = v;
	}, unsetValue: false);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.GestureRecognizers.ScrollGestureRecognizer.IsScrollInertiaEnabled" /> property.
	/// </summary>
	public static readonly DirectProperty<ScrollGestureRecognizer, bool> IsScrollInertiaEnabledProperty = AvaloniaProperty.RegisterDirect("IsScrollInertiaEnabled", (ScrollGestureRecognizer o) => o.IsScrollInertiaEnabled, delegate(ScrollGestureRecognizer o, bool v)
	{
		o.IsScrollInertiaEnabled = v;
	}, unsetValue: false);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.GestureRecognizers.ScrollGestureRecognizer.ScrollStartDistance" /> property.
	/// </summary>
	public static readonly DirectProperty<ScrollGestureRecognizer, int> ScrollStartDistanceProperty = AvaloniaProperty.RegisterDirect("ScrollStartDistance", (ScrollGestureRecognizer o) => o.ScrollStartDistance, delegate(ScrollGestureRecognizer o, int v)
	{
		o.ScrollStartDistance = v;
	}, s_defaultScrollStartDistance);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.GestureRecognizers.ScrollGestureRecognizer.Offset" /> property.
	/// </summary>
	public static readonly DirectProperty<ScrollGestureRecognizer, Vector?> OffsetProperty = AvaloniaProperty.RegisterDirect("Offset", (ScrollGestureRecognizer o) => o.Offset, delegate(ScrollGestureRecognizer o, Vector? v)
	{
		o.Offset = v;
	});

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.GestureRecognizers.ScrollGestureRecognizer.Extent" /> property.
	/// </summary>
	public static readonly DirectProperty<ScrollGestureRecognizer, Size?> ExtentProperty = AvaloniaProperty.RegisterDirect("Extent", (ScrollGestureRecognizer o) => o.Extent, delegate(ScrollGestureRecognizer o, Size? v)
	{
		o.Extent = v;
	});

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.GestureRecognizers.ScrollGestureRecognizer.Viewport" /> property.
	/// </summary>
	public static readonly DirectProperty<ScrollGestureRecognizer, Size?> ViewportProperty = AvaloniaProperty.RegisterDirect("Viewport", (ScrollGestureRecognizer o) => o.Viewport, delegate(ScrollGestureRecognizer o, Size? v)
	{
		o.Viewport = v;
	});

	/// <summary>
	/// Gets or sets a value indicating whether the content can be scrolled horizontally.
	/// </summary>
	public bool CanHorizontallyScroll
	{
		get
		{
			return _canHorizontallyScroll;
		}
		set
		{
			SetAndRaise(CanHorizontallyScrollProperty, ref _canHorizontallyScroll, value);
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether the content can be scrolled vertically.
	/// </summary>
	public bool CanVerticallyScroll
	{
		get
		{
			return _canVerticallyScroll;
		}
		set
		{
			SetAndRaise(CanVerticallyScrollProperty, ref _canVerticallyScroll, value);
		}
	}

	/// <summary>
	/// Gets or sets whether the gesture should include inertia in it's behavior.
	/// </summary>
	public bool IsScrollInertiaEnabled
	{
		get
		{
			return _isScrollInertiaEnabled;
		}
		set
		{
			SetAndRaise(IsScrollInertiaEnabledProperty, ref _isScrollInertiaEnabled, value);
		}
	}

	/// <summary>
	/// Gets or sets a value indicating the distance the pointer moves before scrolling is started
	/// </summary>
	public int ScrollStartDistance
	{
		get
		{
			return _scrollStartDistance;
		}
		set
		{
			SetAndRaise(ScrollStartDistanceProperty, ref _scrollStartDistance, value);
		}
	}

	/// <summary>
	/// Gets the extent of the scrollable content.
	/// </summary>
	public Size? Extent
	{
		get
		{
			return _extent;
		}
		private set
		{
			SetAndRaise(ExtentProperty, ref _extent, value);
		}
	}

	/// <summary>
	/// Gets or sets the current scroll offset.
	/// </summary>
	public Vector? Offset
	{
		get
		{
			return _offset;
		}
		private set
		{
			SetAndRaise(OffsetProperty, ref _offset, value);
		}
	}

	/// <summary>
	/// Gets the size of the viewport on the scrollable content.
	/// </summary>
	public Size? Viewport
	{
		get
		{
			return _viewport;
		}
		private set
		{
			SetAndRaise(ViewportProperty, ref _viewport, value);
		}
	}

	protected override void PointerPressed(PointerPressedEventArgs e)
	{
		PointerPoint currentPoint = e.GetCurrentPoint(null);
		PointerType type = e.Pointer.Type;
		bool flag = (uint)(type - 1) <= 1u;
		if (flag && currentPoint.Properties.IsLeftButtonPressed)
		{
			EndGesture();
			_tracking = e.Pointer;
			_inertia = null;
			_gestureId = ScrollGestureEventArgs.GetNextFreeId();
			_trackedRootPoint = (_pointerPressedPoint = currentPoint.Position);
			_velocityTracker = new VelocityTracker();
			_velocityTracker?.AddPosition(TimeSpan.FromMilliseconds(e.Timestamp), default(Vector));
		}
	}

	protected override void PointerMoved(PointerEventArgs e)
	{
		if (e.Pointer != _tracking)
		{
			return;
		}
		Point position = e.GetPosition(null);
		if (!_scrolling)
		{
			if (CanVerticallyScroll)
			{
				double num = _trackedRootPoint.Y - position.Y;
				Vector? offset = Offset;
				if ((offset.HasValue && offset.GetValueOrDefault().Y == 0.0 && num < 0.0) || (Offset?.Y + Viewport?.Height - Extent?.Height == 0.0 && num > 0.0))
				{
					return;
				}
				if (Math.Abs(num) > (double)ScrollStartDistance)
				{
					_scrolling = true;
				}
			}
			if (CanHorizontallyScroll)
			{
				double num2 = _trackedRootPoint.X - position.X;
				Vector? offset = Offset;
				if ((offset.HasValue && offset.GetValueOrDefault().X == 0.0 && num2 < 0.0) || (Offset?.X + Viewport?.Width - Extent?.Width == 0.0 && num2 > 0.0))
				{
					return;
				}
				if (Math.Abs(num2) > (double)ScrollStartDistance)
				{
					_scrolling = true;
				}
			}
			if (_scrolling)
			{
				_trackedRootPoint = new Point(_trackedRootPoint.X - (double)((_trackedRootPoint.X >= position.X) ? ScrollStartDistance : (-ScrollStartDistance)), _trackedRootPoint.Y - (double)((_trackedRootPoint.Y >= position.Y) ? ScrollStartDistance : (-ScrollStartDistance)));
			}
		}
		if (!_scrolling)
		{
			return;
		}
		Point point = _trackedRootPoint - position;
		Point delta = _delta;
		_delta = _pointerPressedPoint - position;
		if (!(delta == _delta))
		{
			_velocityTracker?.AddPosition(TimeSpan.FromMilliseconds(e.Timestamp), _delta);
			_lastMoveTimestamp = e.Timestamp;
			ScrollGestureEventArgs e2 = new ScrollGestureEventArgs(_gestureId, point);
			base.Target.RaiseEvent(e2);
			_trackedRootPoint = position;
			e.Handled = e2.Handled;
			if (e.Handled)
			{
				Capture(e.Pointer);
			}
		}
	}

	protected override void PointerCaptureLost(IPointer pointer)
	{
		if (pointer == _tracking)
		{
			EndGesture();
		}
	}

	private void EndGesture()
	{
		_tracking = null;
		if (_scrolling)
		{
			_stopWatch?.Stop();
			_stopWatch = null;
			_inertia = null;
			_delta = default(Point);
			_scrolling = false;
			_velocityTracker = null;
			base.Target.RaiseEvent(new ScrollGestureEndedEventArgs(_gestureId));
			_gestureId = 0;
			_lastMoveTimestamp = null;
		}
	}

	protected override void PointerReleased(PointerReleasedEventArgs e)
	{
		if (e.Pointer == _tracking && _scrolling)
		{
			_inertia = _velocityTracker?.GetFlingVelocity().PixelsPerSecond ?? Vector.Zero;
			e.Handled = true;
			if (!_inertia.HasValue || _inertia == Vector.Zero || e.Timestamp == 0L || _lastMoveTimestamp == 0 || e.Timestamp - _lastMoveTimestamp > 200 || !IsScrollInertiaEnabled)
			{
				EndGesture();
				return;
			}
			_tracking = null;
			_stopWatch = Stopwatch.StartNew();
			_lastTime = _stopWatch.Elapsed;
			_inertiaStartTime = _lastTime;
			_currentInertiaGestureId = _gestureId;
			base.Target.RaiseEvent(new ScrollGestureInertiaStartingEventArgs(_gestureId, _inertia.Value));
			MediaContext.Instance.RequestAnimationFrame(OnAnimationRequested);
		}
	}

	private void OnAnimationRequested(TimeSpan _)
	{
		Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(delegate
		{
			if (_gestureId == _currentInertiaGestureId && _stopWatch != null)
			{
				Vector? inertia = _inertia;
				if (inertia.HasValue)
				{
					Vector valueOrDefault = inertia.GetValueOrDefault();
					TimeSpan elapsed = _stopWatch.Elapsed;
					TimeSpan timeSpan = elapsed - _lastTime;
					_lastTime = elapsed;
					Vector vector = valueOrDefault * Math.Pow(0.15, (_lastTime - _inertiaStartTime).TotalSeconds);
					Vector delta = vector * timeSpan.TotalSeconds;
					ScrollGestureEventArgs e = new ScrollGestureEventArgs(_gestureId, delta);
					base.Target.RaiseEvent(e);
					if (!e.Handled || e.ShouldEndScrollGesture)
					{
						EndGesture();
					}
					else
					{
						if (!CanVerticallyScroll || !CanHorizontallyScroll || !(Math.Abs(vector.X) < 5.0) || !(Math.Abs(vector.Y) <= 5.0))
						{
							if (CanVerticallyScroll && Math.Abs(vector.Y) <= 5.0)
							{
								EndGesture();
								return;
							}
							if (CanHorizontallyScroll && Math.Abs(vector.X) < 5.0)
							{
								EndGesture();
								return;
							}
						}
						MediaContext.Instance.RequestAnimationFrame(OnAnimationRequested);
					}
				}
			}
		}, DispatcherPriority.Input);
	}
}
