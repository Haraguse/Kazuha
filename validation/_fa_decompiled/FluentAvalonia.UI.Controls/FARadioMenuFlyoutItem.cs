using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a menu item that is mutually exclusive with other radio menu items in its group.
/// </summary>
[PseudoClasses(new string[] { ":checked" })]
public class FARadioMenuFlyoutItem : FAMenuFlyoutItem
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FARadioMenuFlyoutItem.GroupName" /> property
	/// </summary>
	public static readonly StyledProperty<string> GroupNameProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FARadioMenuFlyoutItem.IsChecked" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsCheckedProperty;

	private bool _isSafeUncheck;

	internal static readonly SortedDictionary<string, WeakReference<FARadioMenuFlyoutItem>> SelectionMap;

	/// <summary>
	/// Gets or sets the name that specifies which RadioMenuFlyoutItem controls are mutually exclusive.
	/// </summary>
	public string GroupName
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(GroupNameProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(GroupNameProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets whether this RadioMenuFlyoutItem is checked
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

	protected override Type StyleKeyOverride => typeof(FARadioMenuFlyoutItem);

	static FARadioMenuFlyoutItem()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		GroupNameProperty = RadioButton.GroupNameProperty.AddOwner<FARadioMenuFlyoutItem>(new StyledPropertyMetadata<string>(default(Optional<string>), (BindingMode)0, (Func<AvaloniaObject, string, string>)((AvaloniaObject _, string x) => x ?? string.Empty), false));
		IsCheckedProperty = AvaloniaProperty.Register<FARadioMenuFlyoutItem, bool>("IsChecked", false, false, (BindingMode)2, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
		if (SelectionMap == null)
		{
			SelectionMap = new SortedDictionary<string, WeakReference<FARadioMenuFlyoutItem>>();
		}
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)IsCheckedProperty)
		{
			bool newValue = AvaloniaPropertyChangedExtensions.GetNewValue<bool>(change);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":checked", newValue);
			if (!_isSafeUncheck)
			{
				UpdateCheckedItemInGroup();
			}
		}
		else if (change.Property == (AvaloniaProperty)(object)GroupNameProperty)
		{
			UpdateCheckedItemInGroup();
		}
	}

	private void UncheckFromGroupSelection()
	{
		_isSafeUncheck = true;
		IsChecked = false;
		_isSafeUncheck = false;
	}

	private void UpdateCheckedItemInGroup()
	{
		if (!IsChecked)
		{
			return;
		}
		string groupName = GroupName;
		if (string.IsNullOrEmpty(groupName))
		{
			return;
		}
		if (SelectionMap.TryGetValue(groupName, out var value))
		{
			if (value.TryGetTarget(out var target))
			{
				if (target == this)
				{
					return;
				}
				target.UncheckFromGroupSelection();
			}
			SelectionMap[groupName] = new WeakReference<FARadioMenuFlyoutItem>(this);
		}
		else
		{
			SelectionMap.Add(groupName, new WeakReference<FARadioMenuFlyoutItem>(this));
		}
	}

	protected override void OnClick()
	{
		base.OnClick();
		IsChecked = !IsChecked;
	}
}
