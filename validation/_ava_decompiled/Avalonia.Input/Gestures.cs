using System;
using System.Threading;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Reactive;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Input;

internal static class Gestures
{
	private record struct GestureState(GestureStateType Type, IPointer Pointer);

	private enum GestureStateType
	{
		Pending,
		Holding,
		DoubleTapped
	}

	private static GestureState? s_gestureState;

	private static readonly WeakReference<object?> s_lastPress;

	private static Point s_lastPressPoint;

	private static CancellationTokenSource? s_holdCancellationToken;

	public static event EventHandler<HoldingRoutedEventArgs>? Holding;

	public static event EventHandler<TappedEventArgs>? Tapped;

	public static event EventHandler<TappedEventArgs>? RightTapped;

	public static event EventHandler<TappedEventArgs>? DoubleTapped;

	static Gestures()
	{
		s_gestureState = null;
		s_lastPress = new WeakReference<object>(null);
		InputElement.PointerPressedEvent.RouteFinished.Subscribe(PointerPressed);
		InputElement.PointerReleasedEvent.RouteFinished.Subscribe(PointerReleased);
		InputElement.PointerMovedEvent.RouteFinished.Subscribe(PointerMoved);
	}

	private static object? GetCaptured(RoutedEventArgs? args)
	{
		if (!(args is PointerEventArgs e))
		{
			return null;
		}
		return e.Pointer?.Captured ?? e.Source;
	}

	private static void PointerPressed(RoutedEventArgs ev)
	{
		object source = GetCaptured(ev);
		if (source == null || ev.Route != RoutingStrategies.Bubble)
		{
			return;
		}
		PointerPressedEventArgs e = (PointerPressedEventArgs)ev;
		Visual visual = (Visual)source;
		if (s_gestureState.HasValue)
		{
			if (s_gestureState.Value.Type == GestureStateType.Holding && source is Interactive sender)
			{
				Holding?.Invoke(sender, new HoldingRoutedEventArgs(HoldingState.Canceled, s_lastPressPoint, s_gestureState.Value.Pointer.Type, e));
			}
			s_holdCancellationToken?.Cancel();
			s_holdCancellationToken?.Dispose();
			s_holdCancellationToken = null;
			s_gestureState = null;
		}
		object target;
		if (e.ClickCount % 2 == 1)
		{
			s_gestureState = new GestureState(GestureStateType.Pending, e.Pointer);
			s_lastPress.SetTarget(source);
			s_lastPressPoint = e.GetPosition((Visual)source);
			s_holdCancellationToken = new CancellationTokenSource();
			CancellationToken token = s_holdCancellationToken.Token;
			IPlatformSettings platformSettings = visual.GetPlatformSettings();
			if (platformSettings == null)
			{
				return;
			}
			DispatcherTimer.RunOnce(delegate
			{
				if (s_gestureState.HasValue && !token.IsCancellationRequested && source is InputElement inputElement && InputElement.GetIsHoldingEnabled(inputElement) && (e.Pointer.Type != PointerType.Mouse || InputElement.GetIsHoldWithMouseEnabled(inputElement)))
				{
					s_gestureState = new GestureState(GestureStateType.Holding, s_gestureState.Value.Pointer);
					Holding?.Invoke(inputElement, new HoldingRoutedEventArgs(HoldingState.Started, s_lastPressPoint, s_gestureState.Value.Pointer.Type, e));
				}
			}, platformSettings.HoldWaitDuration);
		}
		else if (e.ClickCount % 2 == 0 && e.GetCurrentPoint(visual).Properties.IsLeftButtonPressed && s_lastPress.TryGetTarget(out target) && target == source && source is Interactive sender2)
		{
			s_gestureState = new GestureState(GestureStateType.DoubleTapped, e.Pointer);
			DoubleTapped?.Invoke(sender2, new TappedEventArgs(InputElement.DoubleTappedEvent, e));
		}
	}

	private static void PointerReleased(RoutedEventArgs ev)
	{
		if (ev.Route != RoutingStrategies.Bubble)
		{
			return;
		}
		PointerReleasedEventArgs e = (PointerReleasedEventArgs)ev;
		object captured = GetCaptured(ev);
		bool flag = s_lastPress.TryGetTarget(out object target) && target == captured;
		if (flag)
		{
			MouseButton initialPressMouseButton = e.InitialPressMouseButton;
			bool flag2 = (uint)(initialPressMouseButton - 1) <= 1u;
			flag = flag2;
		}
		if (flag && captured is Interactive interactive)
		{
			PointerPoint currentPoint = e.GetCurrentPoint((Visual)target);
			Size size = interactive.GetPlatformSettings()?.GetTapSize(currentPoint.Pointer.Type) ?? new Size(4.0, 4.0);
			if (new Rect(s_lastPressPoint, default(Size)).Inflate(new Thickness(size.Width, size.Height)).ContainsExclusive(currentPoint.Position))
			{
				if (s_gestureState.HasValue && s_gestureState.GetValueOrDefault().Type == GestureStateType.Holding)
				{
					Holding?.Invoke(interactive, new HoldingRoutedEventArgs(HoldingState.Completed, s_lastPressPoint, s_gestureState.Value.Pointer.Type, e));
					RightTapped?.Invoke(interactive, new TappedEventArgs(InputElement.RightTappedEvent, e));
				}
				else if (e.InitialPressMouseButton == MouseButton.Right)
				{
					RightTapped?.Invoke(interactive, new TappedEventArgs(InputElement.RightTappedEvent, e));
				}
				else if (!s_gestureState.HasValue || s_gestureState.GetValueOrDefault().Type != GestureStateType.DoubleTapped)
				{
					Tapped?.Invoke(interactive, new TappedEventArgs(InputElement.TappedEvent, e));
				}
			}
			s_gestureState = null;
		}
		s_holdCancellationToken?.Cancel();
		s_holdCancellationToken?.Dispose();
		s_holdCancellationToken = null;
	}

	private static void PointerMoved(RoutedEventArgs ev)
	{
		if (ev.Route != RoutingStrategies.Bubble)
		{
			return;
		}
		PointerEventArgs e = (PointerEventArgs)ev;
		object captured = GetCaptured(e);
		if (!s_lastPress.TryGetTarget(out object target) || e.Pointer != s_gestureState?.Pointer || !(captured is Interactive sender))
		{
			return;
		}
		PointerPoint currentPoint = e.GetCurrentPoint((Visual)target);
		Size size = new Size(4.0, 4.0);
		if (!new Rect(s_lastPressPoint, default(Size)).Inflate(new Thickness(size.Width, size.Height)).ContainsExclusive(currentPoint.Position))
		{
			if (s_gestureState.Value.Type == GestureStateType.Holding)
			{
				Holding?.Invoke(sender, new HoldingRoutedEventArgs(HoldingState.Canceled, s_lastPressPoint, s_gestureState.Value.Pointer.Type, e));
			}
			s_holdCancellationToken?.Cancel();
			s_holdCancellationToken?.Dispose();
			s_holdCancellationToken = null;
			s_gestureState = null;
		}
	}
}
