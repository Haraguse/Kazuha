using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents an item in a <see cref="T:FluentAvalonia.UI.Controls.FAMenuFlyout" /> that a user can change 
/// between two states, checked or unchecked.
/// </summary>
[PseudoClasses(new string[] { ":checked" })]
public class FAToggleMenuFlyoutItem : FAMenuFlyoutItem
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAToggleMenuFlyoutItem.IsChecked" /> Property
	/// </summary>
	public static readonly StyledProperty<bool> IsCheckedProperty = AvaloniaProperty.Register<FAToggleMenuFlyoutItem, bool>("IsChecked", false, false, (BindingMode)2, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Gets or sets whether the ToggleMenuFlyoutItem is checked.
	/// </summary>
	public bool IsChecked
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsCheckedProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsCheckedProperty, value, (BindingPriority)0);
		}
	}

	protected override Type StyleKeyOverride => typeof(FAToggleMenuFlyoutItem);

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)IsCheckedProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":checked", AvaloniaPropertyChangedExtensions.GetNewValue<bool>(change));
		}
	}

	protected override void OnClick()
	{
		base.OnClick();
		IsChecked = !IsChecked;
	}
}
