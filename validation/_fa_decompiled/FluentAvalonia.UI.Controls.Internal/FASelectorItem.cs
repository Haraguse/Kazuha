using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;

namespace FluentAvalonia.UI.Controls.Internal;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class FASelectorItem : ContentControl, ISelectable
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.Internal.FASelectorItem.IsSelected" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> IsSelectedProperty;

	private bool _isPressed;

	private int _trackedPointerId;

	/// <summary>
	/// Gets or sets whether the item is selected
	/// </summary>
	public bool IsSelected
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsSelectedProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsSelectedProperty, value, (BindingPriority)0);
		}
	}

	static FASelectorItem()
	{
		IsSelectedProperty = SelectingItemsControl.IsSelectedProperty.AddOwner<FASelectorItem>((StyledPropertyMetadata<bool>)null);
		SelectableMixin.Attach<FASelectorItem>((AvaloniaProperty<bool>)(object)IsSelectedProperty);
		InputElement.FocusableProperty.OverrideDefaultValue<FASelectorItem>(true);
		((StyledProperty<IsOffscreenBehavior>)(object)AutomationProperties.IsOffscreenBehaviorProperty).OverrideDefaultValue<FASelectorItem>((IsOffscreenBehavior)3);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		((InputElement)this).OnPointerPressed(e);
		if (!IgnorePointerId(((PointerEventArgs)e).Pointer.Id))
		{
			if ((int)((PointerEventArgs)e).Pointer.Type == 0)
			{
				PointerPoint currentPoint = ((PointerEventArgs)e).GetCurrentPoint((Visual)(object)this);
				PointerPointProperties properties = ((PointerPoint)(ref currentPoint)).Properties;
				_isPressed = ((PointerPointProperties)(ref properties)).IsLeftButtonPressed;
			}
			else
			{
				_isPressed = true;
			}
			if (_isPressed)
			{
				UpdateVisualState();
			}
		}
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		((InputElement)this).OnPointerMoved(e);
		ProcessPointerOver(e);
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		((Control)this).OnPointerReleased(e);
		if (!IgnorePointerId(((PointerEventArgs)e).Pointer.Id) && _isPressed)
		{
			_isPressed = false;
			UpdateVisualState();
		}
	}

	protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
	{
		((InputElement)this).OnPointerCaptureLost(e);
		ProcessPointerCanceled(e.Pointer);
	}

	private void ProcessPointerOver(PointerEventArgs args)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (!IgnorePointerId(args.Pointer.Id))
		{
			Rect bounds = ((Visual)this).Bounds;
			Rect val = default(Rect);
			((Rect)(ref val))._002Ector(((Rect)(ref bounds)).Size);
			Point position = args.GetPosition((Visual)(object)this);
			_isPressed = ((Rect)(ref val)).Contains(position);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":pointerover", _isPressed);
		}
	}

	private void ProcessPointerCanceled(IPointer args)
	{
		if (!IgnorePointerId(args.Id))
		{
			_isPressed = false;
			ResetTrackedPointerId();
			UpdateVisualState();
		}
	}

	private void ResetTrackedPointerId()
	{
		_trackedPointerId = 0;
	}

	private bool IgnorePointerId(int id)
	{
		if (_trackedPointerId == 0)
		{
			_trackedPointerId = id;
		}
		else if (_trackedPointerId != id)
		{
			return true;
		}
		return false;
	}

	private void UpdateVisualState()
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":pressed", _isPressed);
	}
}
