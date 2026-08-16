using System;
using System.Diagnostics;
using Avalonia.Platform;

namespace Avalonia.Input.GestureRecognizers;

/// <summary>
/// A gesture recognizer that detects swipe gestures for paging interactions.
/// </summary>
/// <remarks>
/// Unlike <see cref="T:Avalonia.Input.GestureRecognizers.ScrollGestureRecognizer" />, this recognizer is optimized for discrete
/// paging interactions (e.g., carousel navigation) rather than continuous scrolling.
/// It does not include inertia or friction physics.
/// </remarks>
public class SwipeGestureRecognizer : GestureRecognizer
{
	private bool _swiping;

	private Point _trackedRootPoint;

	private IPointer? _tracking;

	private int _id;

	private Vector _velocity;

	private long _lastTimestamp;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.GestureRecognizers.SwipeGestureRecognizer.CanHorizontallySwipe" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> CanHorizontallySwipeProperty = AvaloniaProperty.Register<SwipeGestureRecognizer, bool>("CanHorizontallySwipe", defaultValue: false);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.GestureRecognizers.SwipeGestureRecognizer.CanVerticallySwipe" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> CanVerticallySwipeProperty = AvaloniaProperty.Register<SwipeGestureRecognizer, bool>("CanVerticallySwipe", defaultValue: false);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.GestureRecognizers.SwipeGestureRecognizer.Threshold" /> property.
	/// </summary>
	/// <remarks>
	/// A value of 0 (the default) causes the distance to be read from
	/// <see cref="T:Avalonia.Platform.IPlatformSettings" /> at the time of the first gesture.
	/// </remarks>
	public static readonly StyledProperty<double> ThresholdProperty = AvaloniaProperty.Register<SwipeGestureRecognizer, double>("Threshold", 0.0);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.GestureRecognizers.SwipeGestureRecognizer.IsMouseEnabled" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> IsMouseEnabledProperty = AvaloniaProperty.Register<SwipeGestureRecognizer, bool>("IsMouseEnabled", defaultValue: false);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.GestureRecognizers.SwipeGestureRecognizer.IsEnabled" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> IsEnabledProperty = AvaloniaProperty.Register<SwipeGestureRecognizer, bool>("IsEnabled", defaultValue: true);

	private const double DefaultTapSize = 10.0;

	/// <summary>
	/// Gets or sets a value indicating whether horizontal swipes are tracked.
	/// </summary>
	public bool CanHorizontallySwipe
	{
		get
		{
			return GetValue(CanHorizontallySwipeProperty);
		}
		set
		{
			SetValue(CanHorizontallySwipeProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether vertical swipes are tracked.
	/// </summary>
	public bool CanVerticallySwipe
	{
		get
		{
			return GetValue(CanVerticallySwipeProperty);
		}
		set
		{
			SetValue(CanVerticallySwipeProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the minimum pointer movement in pixels before a swipe is recognized.
	/// A value of 0 reads the threshold from <see cref="T:Avalonia.Platform.IPlatformSettings" /> at gesture time.
	/// </summary>
	public double Threshold
	{
		get
		{
			return GetValue(ThresholdProperty);
		}
		set
		{
			SetValue(ThresholdProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether mouse pointer events trigger swipe gestures.
	/// Defaults to <see langword="false" />; touch and pen are always enabled.
	/// </summary>
	public bool IsMouseEnabled
	{
		get
		{
			return GetValue(IsMouseEnabledProperty);
		}
		set
		{
			SetValue(IsMouseEnabledProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether this recognizer responds to pointer events.
	/// Defaults to <see langword="true" />.
	/// </summary>
	public bool IsEnabled
	{
		get
		{
			return GetValue(IsEnabledProperty);
		}
		set
		{
			SetValue(IsEnabledProperty, value);
		}
	}

	/// <inheritdoc />
	protected override void PointerPressed(PointerPressedEventArgs e)
	{
		if (IsEnabled)
		{
			PointerPoint currentPoint = e.GetCurrentPoint(null);
			PointerType type = e.Pointer.Type;
			bool flag = (uint)(type - 1) <= 1u;
			if ((flag || (IsMouseEnabled && e.Pointer.Type == PointerType.Mouse)) && currentPoint.Properties.IsLeftButtonPressed)
			{
				EndGesture();
				_tracking = e.Pointer;
				_id = SwipeGestureEventArgs.GetNextFreeId();
				_trackedRootPoint = currentPoint.Position;
				_velocity = default(Vector);
				_lastTimestamp = 0L;
			}
		}
	}

	/// <inheritdoc />
	protected override void PointerMoved(PointerEventArgs e)
	{
		if (e.Pointer != _tracking)
		{
			return;
		}
		Point position = e.GetPosition(null);
		double effectiveThreshold = GetEffectiveThreshold();
		if (!_swiping)
		{
			bool flag = CanHorizontallySwipe && Math.Abs(_trackedRootPoint.X - position.X) > effectiveThreshold;
			bool flag2 = CanVerticallySwipe && Math.Abs(_trackedRootPoint.Y - position.Y) > effectiveThreshold;
			if (flag | flag2)
			{
				_swiping = true;
				_trackedRootPoint = new Point(flag ? (_trackedRootPoint.X - ((_trackedRootPoint.X >= position.X) ? effectiveThreshold : (0.0 - effectiveThreshold))) : position.X, flag2 ? (_trackedRootPoint.Y - ((_trackedRootPoint.Y >= position.Y) ? effectiveThreshold : (0.0 - effectiveThreshold))) : position.Y);
				Capture(e.Pointer);
			}
		}
		if (!_swiping)
		{
			return;
		}
		Point point = _trackedRootPoint - position;
		long timestamp = Stopwatch.GetTimestamp();
		if (_lastTimestamp > 0)
		{
			double num = (double)(timestamp - _lastTimestamp) / (double)Stopwatch.Frequency;
			if (num > 0.0)
			{
				Point point2 = point / num;
				_velocity = _velocity * 0.5 + point2 * 0.5;
			}
		}
		_lastTimestamp = timestamp;
		base.Target.RaiseEvent(new SwipeGestureEventArgs(_id, point, _velocity));
		_trackedRootPoint = position;
		e.Handled = true;
	}

	/// <inheritdoc />
	protected override void PointerCaptureLost(IPointer pointer)
	{
		if (pointer == _tracking)
		{
			EndGesture();
		}
	}

	/// <inheritdoc />
	protected override void PointerReleased(PointerReleasedEventArgs e)
	{
		if (e.Pointer == _tracking && _swiping)
		{
			e.Handled = true;
			EndGesture();
		}
	}

	private void EndGesture()
	{
		_tracking = null;
		if (_swiping)
		{
			_swiping = false;
			SwipeGestureEndedEventArgs e = new SwipeGestureEndedEventArgs(_id, _velocity);
			_velocity = default(Vector);
			_lastTimestamp = 0L;
			_id = 0;
			base.Target.RaiseEvent(e);
		}
	}

	private double GetEffectiveThreshold()
	{
		double threshold = Threshold;
		if (threshold > 0.0)
		{
			return threshold;
		}
		return (AvaloniaLocator.Current?.GetService<IPlatformSettings>()?.GetTapSize(PointerType.Touch).Height ?? 10.0) / 2.0;
	}
}
