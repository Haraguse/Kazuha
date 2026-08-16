using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents the base class for <see cref="T:FluentAvalonia.UI.Controls.FANavigationView" /> menu items
/// </summary>
public class FANavigationViewItemBase : ListBoxItem
{
	protected readonly int _itemIndentation = 31;

	private WeakReference<FANavigationView> _navView;

	private int _depth;

	private NavigationViewRepeaterPosition _position;

	internal NavigationViewRepeaterPosition Position
	{
		get
		{
			return _position;
		}
		set
		{
			_position = value;
			OnNavigationViewItemBasePositionChanged();
		}
	}

	internal int Depth
	{
		get
		{
			return _depth;
		}
		set
		{
			if (_depth != value)
			{
				_depth = value;
				OnNavigationViewItemBaseDepthChanged();
			}
		}
	}

	internal FANavigationView GetNavigationView
	{
		get
		{
			if (_navView != null && _navView.TryGetTarget(out var target))
			{
				return target;
			}
			return null;
		}
	}

	internal SplitView GetSplitView => GetNavigationView?.GetSplitView;

	internal bool IsTopLevelItem { get; set; }

	internal bool CreatedByNavigationViewItemsFactory { get; set; }

	internal bool IsInNavigationViewOwnedRepeater { get; set; }

	public FANavigationViewItemBase()
	{
		((Control)this).Loaded += OnNavItemBaseLoaded;
	}

	internal void SetNavigationViewParent(FANavigationView navView)
	{
		_navView = new WeakReference<FANavigationView>(navView);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((ContentControl)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)ListBoxItem.IsSelectedProperty)
		{
			OnNavigationViewItemBaseIsSelectedChanged();
		}
	}

	protected virtual void OnNavigationViewItemBasePositionChanged()
	{
	}

	protected virtual void OnNavigationViewItemBaseDepthChanged()
	{
	}

	protected virtual void OnNavigationViewItemBaseIsSelectedChanged()
	{
	}

	private void OnNavItemBaseLoaded(object sender, RoutedEventArgs e)
	{
		if (_navView == null)
		{
			SetNavigationViewParent(VisualExtensions.FindAncestorOfType<FANavigationView>((Visual)(object)this, false));
		}
	}
}
