using System;
using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Displays the overflow content of a CommandBar.
/// </summary>
/// <remarks>
/// This class generally should not be used on your own and is meant for
/// the template of a <see cref="T:FluentAvalonia.UI.Controls.FACommandBar" />
/// </remarks>
[PseudoClasses(new string[] { ":icons", ":toggle" })]
public class FACommandBarOverflowPresenter : ItemsControl
{
	private int _hasIcons;

	private int _hasToggle;

	private const string s_pcIcons = ":icons";

	private const string s_pcToggle = ":toggle";

	protected override Type StyleKeyOverride => typeof(FACommandBarOverflowPresenter);

	public FACommandBarOverflowPresenter()
	{
		((ItemsControl)this).ItemsView.CollectionChanged += ItemsCollectionChanged;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((ItemsControl)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)ItemsControl.ItemsSourceProperty)
		{
			ItemsChanged(change);
		}
	}

	protected override bool NeedsContainerOverride(object item, int index, out object recycleKey)
	{
		recycleKey = null;
		return !(item is IFACommandBarElement);
	}

	private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		switch (e.Action)
		{
		case NotifyCollectionChangedAction.Add:
			RegisterItems(e.NewItems);
			break;
		case NotifyCollectionChangedAction.Remove:
			UnregisterItems(e.OldItems);
			break;
		case NotifyCollectionChangedAction.Reset:
			if (e.OldItems != null)
			{
				UnregisterItems(e.OldItems);
				break;
			}
			_hasIcons = 0;
			_hasToggle = 0;
			break;
		case NotifyCollectionChangedAction.Replace:
			UnregisterItems(e.OldItems);
			RegisterItems(e.NewItems);
			break;
		}
		UpdateVisualState();
	}

	private void ItemsChanged(AvaloniaPropertyChangedEventArgs e)
	{
		_hasIcons = 0;
		_hasToggle = 0;
		if (e.NewValue is IList l)
		{
			RegisterItems(l);
		}
		UpdateVisualState();
	}

	private void RegisterItems(IList l)
	{
		for (int i = 0; i < l.Count; i++)
		{
			if (l[i] is FACommandBarButton fACommandBarButton)
			{
				if (fACommandBarButton.IconSource != null)
				{
					_hasIcons++;
				}
				fACommandBarButton.IsInOverflow = true;
			}
			else if (l[i] is FACommandBarToggleButton fACommandBarToggleButton)
			{
				_hasToggle++;
				if (fACommandBarToggleButton.IconSource != null)
				{
					_hasIcons++;
				}
				fACommandBarToggleButton.IsInOverflow = true;
			}
			else if (l[i] is FACommandBarElementContainer fACommandBarElementContainer)
			{
				fACommandBarElementContainer.IsInOverflow = true;
			}
			else if (l[i] is FACommandBarSeparator fACommandBarSeparator)
			{
				fACommandBarSeparator.IsInOverflow = true;
			}
		}
	}

	private void UnregisterItems(IList l)
	{
		for (int i = 0; i < l.Count; i++)
		{
			if (l[i] is FACommandBarButton fACommandBarButton)
			{
				if (fACommandBarButton.IconSource != null)
				{
					_hasIcons--;
				}
				fACommandBarButton.IsInOverflow = false;
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)fACommandBarButton).Classes, ":icons", false);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)fACommandBarButton).Classes, ":toggle", false);
			}
			else if (l[i] is FACommandBarToggleButton fACommandBarToggleButton)
			{
				_hasToggle--;
				if (fACommandBarToggleButton.IconSource != null)
				{
					_hasIcons--;
				}
				fACommandBarToggleButton.IsInOverflow = false;
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)fACommandBarToggleButton).Classes, ":icons", false);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)fACommandBarToggleButton).Classes, ":toggle", false);
			}
			else if (l[i] is FACommandBarElementContainer fACommandBarElementContainer)
			{
				fACommandBarElementContainer.IsInOverflow = false;
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)fACommandBarElementContainer).Classes, ":icons", false);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)fACommandBarElementContainer).Classes, ":toggle", false);
			}
			else if (l[i] is FACommandBarSeparator fACommandBarSeparator)
			{
				fACommandBarSeparator.IsInOverflow = false;
			}
		}
	}

	private void UpdateVisualState()
	{
		IList items = (IList)((ItemsControl)this).Items;
		bool flag = _hasIcons > 0;
		bool flag2 = _hasToggle > 0;
		for (int i = 0; i < items.Count; i++)
		{
			object? obj = items[i];
			Control val = (Control)((obj is Control) ? obj : null);
			if (val != null)
			{
				IPseudoClasses classes = (IPseudoClasses)(object)((StyledElement)val).Classes;
				if (classes != null)
				{
					PseudoClassesExtensions.Set(classes, ":icons", flag);
					PseudoClassesExtensions.Set(classes, ":toggle", flag2);
				}
			}
		}
	}
}
