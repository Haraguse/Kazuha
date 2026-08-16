using System;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Input;

/// <summary>
/// Implements input-related functionality for a control.
/// </summary>
[PseudoClasses(new string[] { ":disabled", ":focus", ":focus-visible", ":focus-within", ":pointerover" })]
public class InputElement : Interactive, IInputElement
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.InputElement.Focusable" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> FocusableProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.InputElement.IsEnabled" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> IsEnabledProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.InputElement.IsEffectivelyEnabled" /> property.
	/// </summary>
	public static readonly DirectProperty<InputElement, bool> IsEffectivelyEnabledProperty;

	/// <summary>
	/// Gets or sets associated mouse cursor.
	/// </summary>
	public static readonly StyledProperty<Cursor?> CursorProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.InputElement.IsKeyboardFocusWithin" /> property.
	/// </summary>
	public static readonly DirectProperty<InputElement, bool> IsKeyboardFocusWithinProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.InputElement.IsFocused" /> property.
	/// </summary>
	public static readonly DirectProperty<InputElement, bool> IsFocusedProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.InputElement.IsHitTestVisible" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> IsHitTestVisibleProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.InputElement.IsPointerOver" /> property.
	/// </summary>
	public static readonly DirectProperty<InputElement, bool> IsPointerOverProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.InputElement.IsTabStop" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> IsTabStopProperty;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.GotFocus" /> event.
	/// </summary>
	public static readonly RoutedEvent<FocusChangedEventArgs> GotFocusEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.GettingFocus" /> event.
	/// </summary>
	public static readonly RoutedEvent<FocusChangingEventArgs> GettingFocusEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.LostFocus" /> event.
	/// </summary>
	public static readonly RoutedEvent<FocusChangedEventArgs> LostFocusEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.LosingFocus" /> event.
	/// </summary>
	public static readonly RoutedEvent<FocusChangingEventArgs> LosingFocusEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.KeyDown" /> event.
	/// </summary>
	public static readonly RoutedEvent<KeyEventArgs> KeyDownEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.KeyUp" /> event.
	/// </summary>
	public static readonly RoutedEvent<KeyEventArgs> KeyUpEvent;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.InputElement.TabIndex" /> property.
	/// </summary>
	public static readonly StyledProperty<int> TabIndexProperty;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.TextInput" /> event.
	/// </summary>
	public static readonly RoutedEvent<TextInputEventArgs> TextInputEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.TextInputMethodClientRequested" /> event.
	/// </summary>
	public static readonly RoutedEvent<TextInputMethodClientRequestedEventArgs> TextInputMethodClientRequestedEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.PointerEntered" /> event.
	/// </summary>
	public static readonly RoutedEvent<PointerEventArgs> PointerEnteredEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.PointerExited" /> event.
	/// </summary>
	public static readonly RoutedEvent<PointerEventArgs> PointerExitedEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.PointerMoved" /> event.
	/// </summary>
	public static readonly RoutedEvent<PointerEventArgs> PointerMovedEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.PointerPressed" /> event.
	/// </summary>
	public static readonly RoutedEvent<PointerPressedEventArgs> PointerPressedEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.PointerReleased" /> event.
	/// </summary>
	public static readonly RoutedEvent<PointerReleasedEventArgs> PointerReleasedEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.PointerCaptureChanging" /> routed event.
	/// </summary>
	internal static readonly RoutedEvent<PointerCaptureChangingEventArgs> PointerCaptureChangingEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.PointerCaptureLost" /> routed event.
	/// </summary>
	public static readonly RoutedEvent<PointerCaptureLostEventArgs> PointerCaptureLostEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.PointerWheelChanged" /> event.
	/// </summary>
	public static readonly RoutedEvent<PointerWheelEventArgs> PointerWheelChangedEvent;

	/// <summary>
	/// Provides event data for the <see cref="E:Avalonia.Input.InputElement.ContextRequested" /> event.
	/// </summary>
	public static readonly RoutedEvent<ContextRequestedEventArgs> ContextRequestedEvent;

	/// <summary>
	/// Provides event data for the <see cref="E:Avalonia.Input.InputElement.ContextCanceled" /> event.
	/// </summary>
	public static readonly RoutedEvent<RoutedEventArgs> ContextCanceledEvent;

	private bool _isEffectivelyEnabled = true;

	private bool _isFocused;

	private bool _isKeyboardFocusWithin;

	private bool _isFocusVisible;

	private bool _isPointerOver;

	private GestureRecognizerCollection? _gestureRecognizers;

	private bool _isContextMenuOnHolding;

	/// <summary>
	/// Defines the IsHoldingEnabled attached property.
	/// </summary>
	public static readonly AttachedProperty<bool> IsHoldingEnabledProperty;

	/// <summary>
	/// Defines the IsHoldWithMouseEnabled attached property.
	/// </summary>
	public static readonly AttachedProperty<bool> IsHoldWithMouseEnabledProperty;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.Pinch" /> event.
	/// </summary>
	public static readonly RoutedEvent<PinchEventArgs> PinchEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.PinchEnded" /> event.
	/// </summary>
	public static readonly RoutedEvent<PinchEndedEventArgs> PinchEndedEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.PullGesture" /> event.
	/// </summary>
	public static readonly RoutedEvent<PullGestureEventArgs> PullGestureEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.PullGestureEnded" /> event.
	/// </summary>
	public static readonly RoutedEvent<PullGestureEndedEventArgs> PullGestureEndedEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.SwipeGesture" /> event.
	/// </summary>
	public static readonly RoutedEvent<SwipeGestureEventArgs> SwipeGestureEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.SwipeGestureEnded" /> event.
	/// </summary>
	public static readonly RoutedEvent<SwipeGestureEndedEventArgs> SwipeGestureEndedEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.ScrollGesture" /> event.
	/// </summary>
	public static readonly RoutedEvent<ScrollGestureEventArgs> ScrollGestureEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.ScrollGestureInertiaStarting" /> event.
	/// </summary>
	public static readonly RoutedEvent<ScrollGestureInertiaStartingEventArgs> ScrollGestureInertiaStartingEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.ScrollGestureEnded" /> event.
	/// </summary>
	public static readonly RoutedEvent<ScrollGestureEndedEventArgs> ScrollGestureEndedEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.PointerTouchPadGestureMagnify" /> event.
	/// </summary>
	public static readonly RoutedEvent<PointerDeltaEventArgs> PointerTouchPadGestureMagnifyEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.PointerTouchPadGestureRotate" /> event.
	/// </summary>
	public static readonly RoutedEvent<PointerDeltaEventArgs> PointerTouchPadGestureRotateEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.PointerTouchPadGestureSwipe" /> event.
	/// </summary>
	public static readonly RoutedEvent<PointerDeltaEventArgs> PointerTouchPadGestureSwipeEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.Tapped" /> event.
	/// </summary>
	public static readonly RoutedEvent<TappedEventArgs> TappedEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.RightTapped" /> event.
	/// </summary>
	public static readonly RoutedEvent<TappedEventArgs> RightTappedEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.Holding" /> event.
	/// </summary>
	public static readonly RoutedEvent<HoldingRoutedEventArgs> HoldingEvent;

	/// <summary>
	/// Defines the <see cref="E:Avalonia.Input.InputElement.DoubleTapped" /> event.
	/// </summary>
	public static readonly RoutedEvent<TappedEventArgs> DoubleTappedEvent;

	/// <summary>
	/// Gets or sets a value indicating whether the control can receive focus.
	/// </summary>
	public bool Focusable
	{
		get
		{
			return GetValue(FocusableProperty);
		}
		set
		{
			SetValue(FocusableProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether the control is enabled for user interaction.
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

	/// <summary>
	/// Gets or sets associated mouse cursor.
	/// </summary>
	public Cursor? Cursor
	{
		get
		{
			return GetValue(CursorProperty);
		}
		set
		{
			SetValue(CursorProperty, value);
		}
	}

	/// <summary>
	/// Gets a value indicating whether keyboard focus is anywhere within the element or its visual tree child elements.
	/// </summary>
	public bool IsKeyboardFocusWithin
	{
		get
		{
			return _isKeyboardFocusWithin;
		}
		internal set
		{
			SetAndRaise(IsKeyboardFocusWithinProperty, ref _isKeyboardFocusWithin, value);
		}
	}

	/// <summary>
	/// Gets a value indicating whether the control is focused.
	/// </summary>
	public bool IsFocused
	{
		get
		{
			return _isFocused;
		}
		private set
		{
			SetAndRaise(IsFocusedProperty, ref _isFocused, value);
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether the control is considered for hit testing.
	/// </summary>
	public bool IsHitTestVisible
	{
		get
		{
			return GetValue(IsHitTestVisibleProperty);
		}
		set
		{
			SetValue(IsHitTestVisibleProperty, value);
		}
	}

	/// <summary>
	/// Gets a value indicating whether the pointer is currently over the control.
	/// </summary>
	public bool IsPointerOver
	{
		get
		{
			return _isPointerOver;
		}
		internal set
		{
			SetAndRaise(IsPointerOverProperty, ref _isPointerOver, value);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the control is included in tab navigation.
	/// </summary>
	public bool IsTabStop
	{
		get
		{
			return GetValue(IsTabStopProperty);
		}
		set
		{
			SetValue(IsTabStopProperty, value);
		}
	}

	/// <inheritdoc />
	public bool IsEffectivelyEnabled
	{
		get
		{
			return _isEffectivelyEnabled;
		}
		private set
		{
			SetAndRaise(IsEffectivelyEnabledProperty, ref _isEffectivelyEnabled, value);
			base.PseudoClasses.Set(":disabled", !value);
			if (!IsEffectivelyEnabled)
			{
				FocusManager focusManager = FocusManager.GetFocusManager(this);
				if (focusManager != null && object.Equals(focusManager.GetFocusedElement(), this))
				{
					focusManager.Focus(null);
				}
			}
		}
	}

	/// <summary>
	/// Gets or sets a value that determines the order in which elements receive focus when the
	/// user navigates through controls by pressing the Tab key.
	/// </summary>
	public int TabIndex
	{
		get
		{
			return GetValue(TabIndexProperty);
		}
		set
		{
			SetValue(TabIndexProperty, value);
		}
	}

	public List<KeyBinding> KeyBindings { get; } = new List<KeyBinding>();

	/// <summary>
	/// Allows a derived class to override the enabled state of the control.
	/// </summary>
	/// <remarks>
	/// Derived controls may wish to disable the enabled state of the control without overwriting the
	/// user-supplied <see cref="P:Avalonia.Input.InputElement.IsEnabled" /> setting. This can be done by overriding this property
	/// to return the overridden enabled state. If the value returned from <see cref="P:Avalonia.Input.InputElement.IsEnabledCore" />
	/// should change, then the derived control should call <see cref="M:Avalonia.Input.InputElement.UpdateIsEffectivelyEnabled" />.
	/// </remarks>
	protected virtual bool IsEnabledCore => IsEnabled;

	public GestureRecognizerCollection GestureRecognizers => _gestureRecognizers ?? (_gestureRecognizers = new GestureRecognizerCollection(this));

	/// <summary>
	/// Occurs when the control receives focus.
	/// </summary>
	public event EventHandler<FocusChangedEventArgs>? GotFocus
	{
		add
		{
			AddHandler(GotFocusEvent, value);
		}
		remove
		{
			RemoveHandler(GotFocusEvent, value);
		}
	}

	/// <summary>
	/// Occurs before the control receives focus.
	/// </summary>
	public event EventHandler<FocusChangingEventArgs>? GettingFocus
	{
		add
		{
			AddHandler(GettingFocusEvent, value);
		}
		remove
		{
			RemoveHandler(GettingFocusEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the control loses focus.
	/// </summary>
	public event EventHandler<FocusChangedEventArgs>? LostFocus
	{
		add
		{
			AddHandler(LostFocusEvent, value);
		}
		remove
		{
			RemoveHandler(LostFocusEvent, value);
		}
	}

	/// <summary>
	/// Occurs before the control loses focus.
	/// </summary>
	public event EventHandler<FocusChangingEventArgs>? LosingFocus
	{
		add
		{
			AddHandler(LosingFocusEvent, value);
		}
		remove
		{
			RemoveHandler(LosingFocusEvent, value);
		}
	}

	/// <summary>
	/// Occurs when a key is pressed while the control has focus.
	/// </summary>
	public event EventHandler<KeyEventArgs>? KeyDown
	{
		add
		{
			AddHandler(KeyDownEvent, value);
		}
		remove
		{
			RemoveHandler(KeyDownEvent, value);
		}
	}

	/// <summary>
	/// Occurs when a key is released while the control has focus.
	/// </summary>
	public event EventHandler<KeyEventArgs>? KeyUp
	{
		add
		{
			AddHandler(KeyUpEvent, value);
		}
		remove
		{
			RemoveHandler(KeyUpEvent, value);
		}
	}

	/// <summary>
	/// Occurs when a user typed some text while the control has focus.
	/// </summary>
	public event EventHandler<TextInputEventArgs>? TextInput
	{
		add
		{
			AddHandler(TextInputEvent, value);
		}
		remove
		{
			RemoveHandler(TextInputEvent, value);
		}
	}

	/// <summary>
	/// Occurs when an input element gains input focus and input method is looking for the corresponding client
	/// </summary>
	public event EventHandler<TextInputMethodClientRequestedEventArgs>? TextInputMethodClientRequested
	{
		add
		{
			AddHandler(TextInputMethodClientRequestedEvent, value);
		}
		remove
		{
			RemoveHandler(TextInputMethodClientRequestedEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the pointer enters the control.
	/// </summary>
	public event EventHandler<PointerEventArgs>? PointerEntered
	{
		add
		{
			AddHandler(PointerEnteredEvent, value);
		}
		remove
		{
			RemoveHandler(PointerEnteredEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the pointer leaves the control.
	/// </summary>
	public event EventHandler<PointerEventArgs>? PointerExited
	{
		add
		{
			AddHandler(PointerExitedEvent, value);
		}
		remove
		{
			RemoveHandler(PointerExitedEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the pointer moves over the control.
	/// </summary>
	public event EventHandler<PointerEventArgs>? PointerMoved
	{
		add
		{
			AddHandler(PointerMovedEvent, value);
		}
		remove
		{
			RemoveHandler(PointerMovedEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the pointer is pressed over the control.
	/// </summary>
	public event EventHandler<PointerPressedEventArgs>? PointerPressed
	{
		add
		{
			AddHandler(PointerPressedEvent, value);
		}
		remove
		{
			RemoveHandler(PointerPressedEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the pointer is released over the control.
	/// </summary>
	public event EventHandler<PointerReleasedEventArgs>? PointerReleased
	{
		add
		{
			AddHandler(PointerReleasedEvent, value);
		}
		remove
		{
			RemoveHandler(PointerReleasedEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the control or its child control is about to lose capture,
	/// event will not be triggered for a parent control if capture was transferred to another child of that parent control.
	/// </summary>
	internal event EventHandler<PointerCaptureChangingEventArgs>? PointerCaptureChanging
	{
		add
		{
			AddHandler(PointerCaptureChangingEvent, value);
		}
		remove
		{
			RemoveHandler(PointerCaptureChangingEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the control or its child control loses the pointer capture for any reason,
	/// event will not be triggered for a parent control if capture was transferred to another child of that parent control.
	/// </summary>
	public event EventHandler<PointerCaptureLostEventArgs>? PointerCaptureLost
	{
		add
		{
			AddHandler(PointerCaptureLostEvent, value);
		}
		remove
		{
			RemoveHandler(PointerCaptureLostEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the mouse is scrolled over the control.
	/// </summary>
	public event EventHandler<PointerWheelEventArgs>? PointerWheelChanged
	{
		add
		{
			AddHandler(PointerWheelChangedEvent, value);
		}
		remove
		{
			RemoveHandler(PointerWheelChangedEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the user has completed a context input gesture, such as a right-click.
	/// </summary>
	public event EventHandler<ContextRequestedEventArgs>? ContextRequested
	{
		add
		{
			AddHandler(ContextRequestedEvent, value);
		}
		remove
		{
			RemoveHandler(ContextRequestedEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the context input gesture continues into another gesture, to notify the element that the context flyout should not be opened.
	/// </summary>
	public event EventHandler<RoutedEventArgs>? ContextCanceled
	{
		add
		{
			AddHandler(ContextCanceledEvent, value);
		}
		remove
		{
			RemoveHandler(ContextCanceledEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the user moves two contact points closer together.
	/// </summary>
	public event EventHandler<PinchEventArgs>? Pinch
	{
		add
		{
			AddHandler(PinchEvent, value);
		}
		remove
		{
			RemoveHandler(PinchEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the user releases both contact points used in a pinch gesture.
	/// </summary>
	public event EventHandler<PinchEndedEventArgs>? PinchEnded
	{
		add
		{
			AddHandler(PinchEndedEvent, value);
		}
		remove
		{
			RemoveHandler(PinchEndedEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the user drags from the edge of a control.
	/// </summary>
	public event EventHandler<PullGestureEventArgs>? PullGesture
	{
		add
		{
			AddHandler(PullGestureEvent, value);
		}
		remove
		{
			RemoveHandler(PullGestureEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the user releases the pointer after a pull gesture.
	/// </summary>
	public event EventHandler<PullGestureEndedEventArgs>? PullGestureEnded
	{
		add
		{
			AddHandler(PullGestureEndedEvent, value);
		}
		remove
		{
			RemoveHandler(PullGestureEndedEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the user continuously moves the pointer in the same direction within the control’s boundaries.
	/// </summary>
	public event EventHandler<ScrollGestureEventArgs>? ScrollGesture
	{
		add
		{
			AddHandler(ScrollGestureEvent, value);
		}
		remove
		{
			RemoveHandler(ScrollGestureEvent, value);
		}
	}

	/// <summary>
	/// Occurs within a scroll gesture, when the user releases the pointer, and scrolling continues by transitioning to momentum-based gliding movement.
	/// </summary>
	public event EventHandler<ScrollGestureInertiaStartingEventArgs>? ScrollGestureInertiaStarting
	{
		add
		{
			AddHandler(ScrollGestureInertiaStartingEvent, value);
		}
		remove
		{
			RemoveHandler(ScrollGestureInertiaStartingEvent, value);
		}
	}

	/// <summary>
	/// Occurs when a scroll gesture has fully stopped, taking into account any inertial movement that continues the scroll after the user has released the pointer.
	/// </summary>
	public event EventHandler<ScrollGestureEndedEventArgs>? ScrollGestureEnded
	{
		add
		{
			AddHandler(ScrollGestureEndedEvent, value);
		}
		remove
		{
			RemoveHandler(ScrollGestureEndedEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the user moves two contact points away from each other on a touchpad.
	/// </summary>
	public event EventHandler<PointerDeltaEventArgs>? PointerTouchPadGestureMagnify
	{
		add
		{
			AddHandler(PointerTouchPadGestureMagnifyEvent, value);
		}
		remove
		{
			RemoveHandler(PointerTouchPadGestureMagnifyEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the user places two contact points and moves them in a circular motion on a touchpad.
	/// </summary>
	public event EventHandler<PointerDeltaEventArgs>? PointerTouchPadGestureRotate
	{
		add
		{
			AddHandler(PointerTouchPadGestureRotateEvent, value);
		}
		remove
		{
			RemoveHandler(PointerTouchPadGestureRotateEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the user rapidly drags the pointer in a single direction across the control.
	/// </summary>
	public event EventHandler<SwipeGestureEventArgs>? SwipeGesture
	{
		add
		{
			AddHandler(SwipeGestureEvent, value);
		}
		remove
		{
			RemoveHandler(SwipeGestureEvent, value);
		}
	}

	/// <summary>
	/// Occurs when a swipe gesture ends on the control.
	/// </summary>
	public event EventHandler<SwipeGestureEndedEventArgs>? SwipeGestureEnded
	{
		add
		{
			AddHandler(SwipeGestureEndedEvent, value);
		}
		remove
		{
			RemoveHandler(SwipeGestureEndedEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the user performs a rapid dragging motion in a single direction on a touchpad.
	/// </summary>
	public event EventHandler<PointerDeltaEventArgs>? PointerTouchPadGestureSwipe
	{
		add
		{
			AddHandler(PointerTouchPadGestureSwipeEvent, value);
		}
		remove
		{
			RemoveHandler(PointerTouchPadGestureSwipeEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the user briefly contacts and releases a single point, without significant movement.
	/// </summary>
	public event EventHandler<TappedEventArgs>? Tapped
	{
		add
		{
			AddHandler(TappedEvent, value);
		}
		remove
		{
			RemoveHandler(TappedEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the user briefly contacts and releases a single point, without significant movement, using a mechanism on the input device recognized as a right button or equivalent.
	/// </summary>
	public event EventHandler<TappedEventArgs>? RightTapped
	{
		add
		{
			AddHandler(RightTappedEvent, value);
		}
		remove
		{
			RemoveHandler(RightTappedEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the user makes a single contact, then maintains contact beyond a given time threshold without releasing or making another contact.
	/// </summary>
	public event EventHandler<HoldingRoutedEventArgs>? Holding
	{
		add
		{
			AddHandler(HoldingEvent, value);
		}
		remove
		{
			RemoveHandler(HoldingEvent, value);
		}
	}

	/// <summary>
	/// Occurs when the user briefly contacts and releases twice on a single point, without significant movement.
	/// </summary>
	public event EventHandler<TappedEventArgs>? DoubleTapped
	{
		add
		{
			AddHandler(DoubleTappedEvent, value);
		}
		remove
		{
			RemoveHandler(DoubleTappedEvent, value);
		}
	}

	/// <summary>
	/// Initializes static members of the <see cref="T:Avalonia.Input.InputElement" /> class.
	/// </summary>
	static InputElement()
	{
		FocusableProperty = AvaloniaProperty.Register<InputElement, bool>("Focusable", defaultValue: false);
		IsEnabledProperty = AvaloniaProperty.Register<InputElement, bool>("IsEnabled", defaultValue: true);
		IsEffectivelyEnabledProperty = AvaloniaProperty.RegisterDirect("IsEffectivelyEnabled", (InputElement o) => o.IsEffectivelyEnabled, null, unsetValue: false);
		CursorProperty = AvaloniaProperty.Register<InputElement, Cursor>("Cursor", null, inherits: true);
		IsKeyboardFocusWithinProperty = AvaloniaProperty.RegisterDirect("IsKeyboardFocusWithin", (InputElement o) => o.IsKeyboardFocusWithin, null, unsetValue: false);
		IsFocusedProperty = AvaloniaProperty.RegisterDirect("IsFocused", (InputElement o) => o.IsFocused, null, unsetValue: false);
		IsHitTestVisibleProperty = AvaloniaProperty.Register<InputElement, bool>("IsHitTestVisible", defaultValue: true);
		IsPointerOverProperty = AvaloniaProperty.RegisterDirect("IsPointerOver", (InputElement o) => o.IsPointerOver, null, unsetValue: false);
		IsTabStopProperty = KeyboardNavigation.IsTabStopProperty.AddOwner<InputElement>();
		GotFocusEvent = RoutedEvent.Register<InputElement, FocusChangedEventArgs>("GotFocus", RoutingStrategies.Bubble);
		GettingFocusEvent = RoutedEvent.Register<InputElement, FocusChangingEventArgs>("GettingFocus", RoutingStrategies.Bubble);
		LostFocusEvent = RoutedEvent.Register<InputElement, FocusChangedEventArgs>("LostFocus", RoutingStrategies.Bubble);
		LosingFocusEvent = RoutedEvent.Register<InputElement, FocusChangingEventArgs>("LosingFocus", RoutingStrategies.Bubble);
		KeyDownEvent = RoutedEvent.Register<InputElement, KeyEventArgs>("KeyDown", RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
		KeyUpEvent = RoutedEvent.Register<InputElement, KeyEventArgs>("KeyUp", RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
		TabIndexProperty = KeyboardNavigation.TabIndexProperty.AddOwner<InputElement>();
		TextInputEvent = RoutedEvent.Register<InputElement, TextInputEventArgs>("TextInput", RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
		TextInputMethodClientRequestedEvent = RoutedEvent.Register<InputElement, TextInputMethodClientRequestedEventArgs>("TextInputMethodClientRequested", RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
		PointerEnteredEvent = RoutedEvent.Register<InputElement, PointerEventArgs>("PointerEntered", RoutingStrategies.Direct);
		PointerExitedEvent = RoutedEvent.Register<InputElement, PointerEventArgs>("PointerExited", RoutingStrategies.Direct);
		PointerMovedEvent = RoutedEvent.Register<InputElement, PointerEventArgs>("PointerMoved", RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
		PointerPressedEvent = RoutedEvent.Register<InputElement, PointerPressedEventArgs>("PointerPressed", RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
		PointerReleasedEvent = RoutedEvent.Register<InputElement, PointerReleasedEventArgs>("PointerReleased", RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
		PointerCaptureChangingEvent = RoutedEvent.Register<InputElement, PointerCaptureChangingEventArgs>("PointerCaptureChanging", RoutingStrategies.Direct);
		PointerCaptureLostEvent = RoutedEvent.Register<InputElement, PointerCaptureLostEventArgs>("PointerCaptureLost", RoutingStrategies.Direct);
		PointerWheelChangedEvent = RoutedEvent.Register<InputElement, PointerWheelEventArgs>("PointerWheelChanged", RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
		ContextRequestedEvent = RoutedEvent.Register<InputElement, ContextRequestedEventArgs>("ContextRequested", RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
		ContextCanceledEvent = RoutedEvent.Register<InputElement, RoutedEventArgs>("ContextCanceled", RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
		IsHoldingEnabledProperty = AvaloniaProperty.RegisterAttached<StyledElement, bool>("IsHoldingEnabled", typeof(InputElement), defaultValue: true);
		IsHoldWithMouseEnabledProperty = AvaloniaProperty.RegisterAttached<StyledElement, bool>("IsHoldWithMouseEnabled", typeof(InputElement), defaultValue: false);
		PinchEvent = RoutedEvent.Register<InputElement, PinchEventArgs>("Pinch", RoutingStrategies.Bubble);
		PinchEndedEvent = RoutedEvent.Register<InputElement, PinchEndedEventArgs>("PinchEnded", RoutingStrategies.Bubble);
		PullGestureEvent = RoutedEvent.Register<InputElement, PullGestureEventArgs>("PullGesture", RoutingStrategies.Bubble);
		PullGestureEndedEvent = RoutedEvent.Register<InputElement, PullGestureEndedEventArgs>("PullGestureEnded", RoutingStrategies.Bubble);
		SwipeGestureEvent = RoutedEvent.Register<InputElement, SwipeGestureEventArgs>("SwipeGesture", RoutingStrategies.Bubble);
		SwipeGestureEndedEvent = RoutedEvent.Register<InputElement, SwipeGestureEndedEventArgs>("SwipeGestureEnded", RoutingStrategies.Bubble);
		ScrollGestureEvent = RoutedEvent.Register<InputElement, ScrollGestureEventArgs>("ScrollGesture", RoutingStrategies.Bubble);
		ScrollGestureInertiaStartingEvent = RoutedEvent.Register<InputElement, ScrollGestureInertiaStartingEventArgs>("ScrollGestureInertiaStarting", RoutingStrategies.Bubble);
		ScrollGestureEndedEvent = RoutedEvent.Register<InputElement, ScrollGestureEndedEventArgs>("ScrollGestureEnded", RoutingStrategies.Bubble);
		PointerTouchPadGestureMagnifyEvent = RoutedEvent.Register<InputElement, PointerDeltaEventArgs>("PointerTouchPadGestureMagnify", RoutingStrategies.Bubble);
		PointerTouchPadGestureRotateEvent = RoutedEvent.Register<InputElement, PointerDeltaEventArgs>("PointerTouchPadGestureRotate", RoutingStrategies.Bubble);
		PointerTouchPadGestureSwipeEvent = RoutedEvent.Register<InputElement, PointerDeltaEventArgs>("PointerTouchPadGestureSwipe", RoutingStrategies.Bubble);
		TappedEvent = RoutedEvent.Register<InputElement, TappedEventArgs>("Tapped", RoutingStrategies.Bubble);
		RightTappedEvent = RoutedEvent.Register<InputElement, TappedEventArgs>("RightTapped", RoutingStrategies.Bubble);
		HoldingEvent = RoutedEvent.Register<InputElement, HoldingRoutedEventArgs>("Holding", RoutingStrategies.Bubble);
		DoubleTappedEvent = RoutedEvent.Register<InputElement, TappedEventArgs>("DoubleTapped", RoutingStrategies.Bubble);
		IsEnabledProperty.Changed.Subscribe(IsEnabledChanged);
		GotFocusEvent.AddClassHandler(delegate(InputElement x, FocusChangedEventArgs e)
		{
			x.OnGotFocusCore(e);
		});
		LostFocusEvent.AddClassHandler(delegate(InputElement x, FocusChangedEventArgs e)
		{
			x.OnLostFocusCore(e);
		});
		GettingFocusEvent.AddClassHandler(delegate(InputElement x, FocusChangingEventArgs e)
		{
			x.OnGettingFocus(e);
		});
		LosingFocusEvent.AddClassHandler(delegate(InputElement x, FocusChangingEventArgs e)
		{
			x.OnLosingFocus(e);
		});
		KeyDownEvent.AddClassHandler(delegate(InputElement x, KeyEventArgs e)
		{
			x.OnKeyDown(e);
		});
		KeyUpEvent.AddClassHandler(delegate(InputElement x, KeyEventArgs e)
		{
			x.OnKeyUp(e);
		});
		TextInputEvent.AddClassHandler(delegate(InputElement x, TextInputEventArgs e)
		{
			x.OnTextInput(e);
		});
		PointerEnteredEvent.AddClassHandler(delegate(InputElement x, PointerEventArgs e)
		{
			x.OnPointerEnteredCore(e);
		});
		PointerExitedEvent.AddClassHandler(delegate(InputElement x, PointerEventArgs e)
		{
			x.OnPointerExitedCore(e);
		});
		PointerMovedEvent.AddClassHandler(delegate(InputElement x, PointerEventArgs e)
		{
			x.OnPointerMoved(e);
		});
		PointerPressedEvent.AddClassHandler(delegate(InputElement x, PointerPressedEventArgs e)
		{
			x.OnPointerPressed(e);
		});
		PointerReleasedEvent.AddClassHandler(delegate(InputElement x, PointerReleasedEventArgs e)
		{
			x.OnPointerReleased(e);
		});
		PointerCaptureChangingEvent.AddClassHandler(delegate(InputElement x, PointerCaptureChangingEventArgs e)
		{
			x.OnPointerCaptureChanging(e);
		});
		PointerCaptureLostEvent.AddClassHandler(delegate(InputElement x, PointerCaptureLostEventArgs e)
		{
			x.OnPointerCaptureLost(e);
		});
		PointerWheelChangedEvent.AddClassHandler(delegate(InputElement x, PointerWheelEventArgs e)
		{
			x.OnPointerWheelChanged(e);
		});
		TappedEvent.AddClassHandler(delegate(InputElement x, TappedEventArgs e)
		{
			x.OnTapped(e);
		});
		RightTappedEvent.AddClassHandler(delegate(InputElement x, TappedEventArgs e)
		{
			x.OnRightTapped(e);
		});
		DoubleTappedEvent.AddClassHandler(delegate(InputElement x, TappedEventArgs e)
		{
			x.OnDoubleTapped(e);
		});
		HoldingEvent.AddClassHandler(delegate(InputElement x, HoldingRoutedEventArgs e)
		{
			x.OnHolding(e);
		});
		Gestures.Tapped += delegate(object? s, TappedEventArgs e)
		{
			(s as InputElement)?.RaiseEvent(e);
		};
		Gestures.RightTapped += delegate(object? s, TappedEventArgs e)
		{
			(s as InputElement)?.RaiseEvent(e);
		};
		Gestures.DoubleTapped += delegate(object? s, TappedEventArgs e)
		{
			(s as InputElement)?.RaiseEvent(e);
		};
		Gestures.Holding += OnPreviewHolding;
		PointerMovedEvent.AddClassHandler(delegate(InputElement x, PointerEventArgs e)
		{
			x.OnGesturePointerMoved(e);
		}, RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: true);
		PointerPressedEvent.AddClassHandler(delegate(InputElement x, PointerPressedEventArgs e)
		{
			x.OnGesturePointerPressed(e);
		}, RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: true);
		PointerReleasedEvent.AddClassHandler(delegate(InputElement x, PointerReleasedEventArgs e)
		{
			x.OnGesturePointerReleased(e);
		}, RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: true);
		PointerCaptureLostEvent.AddClassHandler(delegate(InputElement x, PointerCaptureLostEventArgs e)
		{
			x.OnGesturePointerCaptureLost(e);
		}, RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: true);
		AccessKeyHandler.AccessKeyEvent.AddClassHandler(delegate(InputElement x, AccessKeyEventArgs e)
		{
			x.OnAccessKey(e);
		});
	}

	public InputElement()
	{
		UpdatePseudoClasses(IsFocused, IsPointerOver);
	}

	/// <inheritdoc />
	public bool Focus(NavigationMethod method = NavigationMethod.Unspecified, KeyModifiers keyModifiers = KeyModifiers.None)
	{
		return FocusManager.GetFocusManager(this)?.Focus(this, method, keyModifiers) ?? false;
	}

	/// <inheritdoc />
	protected override void OnDetachedFromVisualTreeCore(VisualTreeAttachmentEventArgs e)
	{
		base.OnDetachedFromVisualTreeCore(e);
		if (IsFocused)
		{
			Visual oldParent = e.AttachmentPoint ?? e.RootVisual;
			((FocusManager)e.PresentationSource.InputRoot.FocusManager)?.ClearFocusOnElementRemoved(this, oldParent);
		}
		IsKeyboardFocusWithin = false;
	}

	/// <summary>
	/// This method is used to execute the action on an effective IInputElement when a corresponding access key has been invoked.
	/// By default, the Focus() method is invoked with the NavigationMethod.Tab to indicate a visual focus adorner.
	/// Overwrite this method if other methods or additional functionality is needed when an item should receive the focus.
	/// </summary>
	/// <param name="e">AccessKeyEventArgs are passed on to indicate if there are multiple matches or not.</param>
	protected virtual void OnAccessKey(RoutedEventArgs e)
	{
		Focus(NavigationMethod.Tab);
	}

	/// <inheritdoc />
	protected override void OnAttachedToVisualTreeCore(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTreeCore(e);
		UpdateIsEffectivelyEnabled();
	}

	private void OnGotFocusCore(FocusChangedEventArgs e)
	{
		bool flag = e.Source == this;
		_isFocusVisible = flag && (e.NavigationMethod == NavigationMethod.Directional || e.NavigationMethod == NavigationMethod.Tab);
		IsFocused = flag;
		OnGotFocus(e);
	}

	protected virtual void OnGettingFocus(FocusChangingEventArgs e)
	{
	}

	protected virtual void OnLosingFocus(FocusChangingEventArgs e)
	{
	}

	/// <summary>
	/// Invoked when an unhandled <see cref="F:Avalonia.Input.InputElement.GotFocusEvent" /> reaches an element in its 
	/// route that is derived from this class. Implement this method to add class handling 
	/// for this event.
	/// </summary>
	/// <param name="e">Data about the event.</param>
	protected virtual void OnGotFocus(FocusChangedEventArgs e)
	{
	}

	private void OnLostFocusCore(FocusChangedEventArgs e)
	{
		_isFocusVisible = false;
		IsFocused = false;
		OnLostFocus(e);
	}

	/// <summary>
	/// Invoked when an unhandled <see cref="F:Avalonia.Input.InputElement.LostFocusEvent" /> reaches an element in its 
	/// route that is derived from this class. Implement this method to add class handling 
	/// for this event.
	/// </summary>
	/// <param name="e">Data about the event.</param>
	protected virtual void OnLostFocus(FocusChangedEventArgs e)
	{
	}

	/// <summary>
	/// Invoked when an unhandled <see cref="F:Avalonia.Input.InputElement.KeyDownEvent" /> reaches an element in its 
	/// route that is derived from this class. Implement this method to add class handling 
	/// for this event.
	/// </summary>
	/// <param name="e">Data about the event.</param>
	protected virtual void OnKeyDown(KeyEventArgs e)
	{
	}

	/// <summary>
	/// Invoked when an unhandled <see cref="F:Avalonia.Input.InputElement.KeyUpEvent" /> reaches an element in its 
	/// route that is derived from this class. Implement this method to add class handling 
	/// for this event.
	/// </summary>
	/// <param name="e">Data about the event.</param>
	protected virtual void OnKeyUp(KeyEventArgs e)
	{
	}

	/// <summary>
	/// Invoked when an unhandled <see cref="F:Avalonia.Input.InputElement.TextInputEvent" /> reaches an element in its 
	/// route that is derived from this class. Implement this method to add class handling 
	/// for this event.
	/// </summary>
	/// <param name="e">Data about the event.</param>
	protected virtual void OnTextInput(TextInputEventArgs e)
	{
	}

	/// <summary>
	/// Invoked when an unhandled <see cref="F:Avalonia.Input.InputElement.PointerEnteredEvent" /> reaches an element in its 
	/// route that is derived from this class. Implement this method to add class handling 
	/// for this event.
	/// </summary>
	/// <param name="e">Data about the event.</param>
	protected virtual void OnPointerEntered(PointerEventArgs e)
	{
	}

	/// <summary>
	/// Invoked when an unhandled <see cref="F:Avalonia.Input.InputElement.PointerExitedEvent" /> reaches an element in its 
	/// route that is derived from this class. Implement this method to add class handling 
	/// for this event.
	/// </summary>
	/// <param name="e">Data about the event.</param>
	protected virtual void OnPointerExited(PointerEventArgs e)
	{
	}

	/// <summary>
	/// Invoked when an unhandled <see cref="F:Avalonia.Input.InputElement.PointerMovedEvent" /> reaches an element in its 
	/// route that is derived from this class. Implement this method to add class handling 
	/// for this event.
	/// </summary>
	/// <param name="e">Data about the event.</param>
	protected virtual void OnPointerMoved(PointerEventArgs e)
	{
	}

	/// <summary>
	/// Invoked when an unhandled <see cref="F:Avalonia.Input.InputElement.PointerPressedEvent" /> reaches an element in its 
	/// route that is derived from this class. Implement this method to add class handling 
	/// for this event.
	/// </summary>
	/// <param name="e">Data about the event.</param>
	protected virtual void OnPointerPressed(PointerPressedEventArgs e)
	{
	}

	/// <summary>
	/// Invoked when an unhandled <see cref="F:Avalonia.Input.InputElement.PointerReleasedEvent" /> reaches an element in its 
	/// route that is derived from this class. Implement this method to add class handling 
	/// for this event.
	/// </summary>
	/// <param name="e">Data about the event.</param>
	protected virtual void OnPointerReleased(PointerReleasedEventArgs e)
	{
	}

	private void OnGesturePointerReleased(PointerReleasedEventArgs e)
	{
		if (!e.IsGestureRecognitionSkipped)
		{
			GestureRecognizerCollection? gestureRecognizers = _gestureRecognizers;
			if (gestureRecognizers != null && gestureRecognizers.HandlePointerReleased(e))
			{
				e.Handled = true;
			}
		}
	}

	private void OnGesturePointerCaptureLost(PointerCaptureLostEventArgs e)
	{
		_gestureRecognizers?.HandleCaptureLost(e.Pointer);
	}

	private void OnGesturePointerPressed(PointerPressedEventArgs e)
	{
		if (!e.IsGestureRecognitionSkipped)
		{
			GestureRecognizerCollection? gestureRecognizers = _gestureRecognizers;
			if (gestureRecognizers != null && gestureRecognizers.HandlePointerPressed(e))
			{
				e.Handled = true;
			}
		}
	}

	private void OnGesturePointerMoved(PointerEventArgs e)
	{
		if (!e.IsGestureRecognitionSkipped)
		{
			GestureRecognizerCollection? gestureRecognizers = _gestureRecognizers;
			if (gestureRecognizers != null && gestureRecognizers.HandlePointerMoved(e))
			{
				e.Handled = true;
			}
		}
	}

	/// <summary>
	/// Called when FocusManager get the next TabStop to interact with the focused control.
	/// </summary>
	/// <returns>Next tab stop.</returns>
	protected internal virtual InputElement? GetNextTabStopOverride()
	{
		return null;
	}

	/// <summary>
	/// Called when FocusManager get the previous TabStop to interact with the focused control.
	/// </summary>
	/// <returns>Previous tab stop.</returns>
	protected internal virtual InputElement? GetPreviousTabStopOverride()
	{
		return null;
	}

	/// <summary>
	/// Called when FocusManager is looking for the first focusable element from the specified search scope.
	/// </summary>
	/// <returns>First focusable element if available.</returns>
	protected internal virtual InputElement? GetFirstFocusableElementOverride()
	{
		return null;
	}

	/// <summary>
	/// Called when FocusManager is looking for the last focusable element from the specified search scope.
	/// </summary>
	/// <returns>Last focusable element if available/&gt;.</returns>
	protected internal virtual InputElement? GetLastFocusableElementOverride()
	{
		return null;
	}

	/// <summary>
	/// Invoked when an unhandled <see cref="F:Avalonia.Input.InputElement.PointerCaptureChangingEvent" /> reaches an element in its 
	/// route that is derived from this class. Implement this method to add class handling 
	/// for this event.
	/// </summary>
	/// <param name="e">Data about the event.</param>
	internal virtual void OnPointerCaptureChanging(PointerCaptureChangingEventArgs e)
	{
	}

	/// <summary>
	/// Invoked when an unhandled <see cref="F:Avalonia.Input.InputElement.PointerCaptureLostEvent" /> reaches an element in its 
	/// route that is derived from this class. Implement this method to add class handling 
	/// for this event.
	/// </summary>
	/// <param name="e">Data about the event.</param>
	protected virtual void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
	{
	}

	internal static bool ProcessTabStop(IInputElement? contentRoot, IInputElement? focusedElement, IInputElement? candidateTabStopElement, bool isReverse, bool didCycleFocusAtRootVisual, out IInputElement? newTabStop)
	{
		newTabStop = null;
		bool flag = false;
		bool flag2 = false;
		InputElement inputElement = focusedElement as InputElement;
		InputElement inputElement2 = candidateTabStopElement as InputElement;
		InputElement inputElement3 = null;
		IInputElement newTabStop2 = null;
		IInputElement newTabStop3 = null;
		if (inputElement != null)
		{
			flag = inputElement.ProcessTabStopInternal(candidateTabStopElement, isReverse, didCycleFocusAtRootVisual, out newTabStop3);
		}
		if (!flag && inputElement2 != null)
		{
			flag = inputElement2.ProcessCandidateTabStopInternal(focusedElement, null, isReverse, out newTabStop3);
		}
		else if (flag && newTabStop != null && newTabStop3 is InputElement inputElement4)
		{
			flag2 = inputElement4.ProcessCandidateTabStopInternal(focusedElement, newTabStop3, isReverse, out newTabStop2);
		}
		if (flag2)
		{
			if (newTabStop2 != null)
			{
				newTabStop = newTabStop2;
			}
			flag = true;
		}
		else if (flag)
		{
			if (newTabStop != null)
			{
				newTabStop = newTabStop3;
			}
			flag = true;
		}
		return flag;
	}

	private bool ProcessTabStopInternal(IInputElement? candidateTabStopElement, bool isReverse, bool didCycleFocusAtRootVisual, out IInputElement? newTabStop)
	{
		InputElement inputElement = this;
		newTabStop = null;
		bool flag = false;
		while (inputElement != null && !flag)
		{
			flag = inputElement.ProcessTabStopOverride(this, candidateTabStopElement, isReverse, didCycleFocusAtRootVisual, ref newTabStop);
			inputElement = inputElement?.Parent as InputElement;
		}
		return flag;
	}

	private bool ProcessCandidateTabStopInternal(IInputElement? currentTabStop, IInputElement? overridenCandidateTabStopElement, bool isReverse, out IInputElement? newTabStop)
	{
		InputElement inputElement = this;
		newTabStop = null;
		bool flag = false;
		while (inputElement != null && !flag)
		{
			flag = inputElement.ProcessCandidateTabStopOverride(currentTabStop, this, overridenCandidateTabStopElement, isReverse, ref newTabStop);
			inputElement = inputElement?.Parent as InputElement;
		}
		return flag;
	}

	protected internal virtual bool ProcessTabStopOverride(IInputElement? focusedElement, IInputElement? candidateTabStopElement, bool isReverse, bool didCycleFocusAtRootVisual, ref IInputElement? newTabStop)
	{
		return false;
	}

	protected internal virtual bool ProcessCandidateTabStopOverride(IInputElement? focusedElement, IInputElement? candidateTabStopElement, IInputElement? overridenCandidateTabStopElement, bool isReverse, ref IInputElement? newTabStop)
	{
		return false;
	}

	/// <summary>
	/// Invoked when an unhandled <see cref="F:Avalonia.Input.InputElement.PointerWheelChangedEvent" /> reaches an element in its 
	/// route that is derived from this class. Implement this method to add class handling 
	/// for this event.
	/// </summary>
	/// <param name="e">Data about the event.</param>
	protected virtual void OnPointerWheelChanged(PointerWheelEventArgs e)
	{
	}

	/// <summary>
	/// Invoked when an unhandled <see cref="F:Avalonia.Input.InputElement.TappedEvent" /> reaches an element in its 
	/// route that is derived from this class. Implement this method to add class handling 
	/// for this event.
	/// </summary>
	/// <param name="e">Data about the event.</param>
	protected virtual void OnTapped(TappedEventArgs e)
	{
	}

	/// <summary>
	/// Invoked when an unhandled <see cref="F:Avalonia.Input.InputElement.RightTappedEvent" /> reaches an element in its 
	/// route that is derived from this class. Implement this method to add class handling 
	/// for this event.
	/// </summary>
	/// <param name="e">Data about the event.</param>
	protected virtual void OnRightTapped(TappedEventArgs e)
	{
	}

	/// <summary>
	/// Invoked when an unhandled <see cref="F:Avalonia.Input.InputElement.DoubleTappedEvent" /> reaches an element in its 
	/// route that is derived from this class. Implement this method to add class handling 
	/// for this event.
	/// </summary>
	/// <param name="e">Data about the event.</param>
	protected virtual void OnDoubleTapped(TappedEventArgs e)
	{
	}

	/// <summary>
	/// Invoked when an unhandled <see cref="F:Avalonia.Input.InputElement.HoldingEvent" /> reaches an element in its 
	/// route that is derived from this class. Implement this method to add class handling 
	/// for this event.
	/// </summary>
	/// <param name="e">Data about the event.</param>
	protected virtual void OnHolding(HoldingRoutedEventArgs e)
	{
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == IsFocusedProperty)
		{
			UpdatePseudoClasses(change.GetNewValue<bool>(), null);
		}
		else if (change.Property == IsPointerOverProperty)
		{
			UpdatePseudoClasses(null, change.GetNewValue<bool>());
		}
		else if (change.Property == IsKeyboardFocusWithinProperty)
		{
			base.PseudoClasses.Set(":focus-within", change.GetNewValue<bool>());
		}
		else
		{
			if (!(change.Property == Visual.IsVisibleProperty) || change.GetNewValue<bool>() || !IsKeyboardFocusWithin)
			{
				return;
			}
			FocusManager focusManager = FocusManager.GetFocusManager(this);
			if (focusManager != null)
			{
				IInputElement focusedElement = focusManager.GetFocusedElement();
				if (focusedElement != null && base.VisualParent != null)
				{
					focusManager.ClearFocusOnElementRemoved(focusedElement, base.VisualParent);
				}
				else
				{
					focusManager.Focus(null);
				}
			}
		}
	}

	/// <summary>
	/// Updates the <see cref="P:Avalonia.Input.InputElement.IsEffectivelyEnabled" /> property value according to the parent
	/// control's enabled state and <see cref="P:Avalonia.Input.InputElement.IsEnabledCore" />.
	/// </summary>
	protected void UpdateIsEffectivelyEnabled()
	{
		UpdateIsEffectivelyEnabled(this.GetVisualParent<InputElement>());
	}

	private static void IsEnabledChanged(AvaloniaPropertyChangedEventArgs e)
	{
		((InputElement)e.Sender).UpdateIsEffectivelyEnabled();
	}

	/// <summary>
	/// Called before the <see cref="E:Avalonia.Input.InputElement.PointerEntered" /> event occurs.
	/// </summary>
	/// <param name="e">The event args.</param>
	private void OnPointerEnteredCore(PointerEventArgs e)
	{
		IsPointerOver = true;
		OnPointerEntered(e);
	}

	/// <summary>
	/// Called before the <see cref="E:Avalonia.Input.InputElement.PointerExited" /> event occurs.
	/// </summary>
	/// <param name="e">The event args.</param>
	private void OnPointerExitedCore(PointerEventArgs e)
	{
		IsPointerOver = false;
		OnPointerExited(e);
	}

	/// <summary>
	/// Updates the <see cref="P:Avalonia.Input.InputElement.IsEffectivelyEnabled" /> property based on the parent's
	/// <see cref="P:Avalonia.Input.InputElement.IsEffectivelyEnabled" />.
	/// </summary>
	/// <param name="parent">The parent control.</param>
	private void UpdateIsEffectivelyEnabled(InputElement? parent)
	{
		IsEffectivelyEnabled = IsEnabledCore && (parent?.IsEffectivelyEnabled ?? true);
		IAvaloniaList<Visual> visualChildren = base.VisualChildren;
		for (int i = 0; i < visualChildren.Count; i++)
		{
			(visualChildren[i] as InputElement)?.UpdateIsEffectivelyEnabled(this);
		}
	}

	private void UpdatePseudoClasses(bool? isFocused, bool? isPointerOver)
	{
		if (isFocused.HasValue)
		{
			base.PseudoClasses.Set(":focus", isFocused.Value);
			base.PseudoClasses.Set(":focus-visible", _isFocusVisible);
		}
		if (isPointerOver.HasValue)
		{
			base.PseudoClasses.Set(":pointerover", isPointerOver.Value);
		}
	}

	public static bool GetIsHoldingEnabled(StyledElement element)
	{
		return element.GetValue(IsHoldingEnabledProperty);
	}

	public static void SetIsHoldingEnabled(StyledElement element, bool value)
	{
		element.SetValue(IsHoldingEnabledProperty, value);
	}

	public static bool GetIsHoldWithMouseEnabled(StyledElement element)
	{
		return element.GetValue(IsHoldWithMouseEnabledProperty);
	}

	public static void SetIsHoldWithMouseEnabled(StyledElement element, bool value)
	{
		element.SetValue(IsHoldWithMouseEnabledProperty, value);
	}

	private static void OnPreviewHolding(object? sender, HoldingRoutedEventArgs e)
	{
		if (!(sender is InputElement inputElement))
		{
			return;
		}
		inputElement.RaiseEvent(e);
		if (!e.Handled && e.HoldingState == HoldingState.Started)
		{
			ContextRequestedEventArgs e2 = new ContextRequestedEventArgs(e);
			inputElement.RaiseEvent(e2);
			e.Handled = e2.Handled;
			if (e2.Handled)
			{
				inputElement._isContextMenuOnHolding = true;
			}
		}
		else if (e.HoldingState == HoldingState.Canceled && inputElement._isContextMenuOnHolding)
		{
			inputElement.RaiseEvent(new RoutedEventArgs(ContextCanceledEvent)
			{
				Source = inputElement
			});
			inputElement._isContextMenuOnHolding = false;
		}
		else if (e.HoldingState == HoldingState.Completed)
		{
			inputElement._isContextMenuOnHolding = false;
		}
	}
}
