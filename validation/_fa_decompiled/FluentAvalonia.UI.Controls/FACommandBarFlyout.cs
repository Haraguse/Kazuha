using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Metadata;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a specialized flyout that provides layout for CommandBarButton,
/// CommandBarToggleButton, and CommandBarSeparator controls.
/// </summary>
public class FACommandBarFlyout : PopupFlyoutBase
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarFlyout.AlwaysExpanded" /> property
	/// </summary>
	public static readonly StyledProperty<bool> AlwaysExpandedProperty = AvaloniaProperty.Register<FACommandBarFlyout, bool>("AlwaysExpanded", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	protected FACommandBarFlyoutCommandBar _commandBar;

	protected FlyoutPresenter _presenter;

	/// <summary>
	/// Gets the collection of primary command elements for the CommandBarFlyout.
	/// </summary>
	[Content]
	public IAvaloniaList<IFACommandBarElement> PrimaryCommands { get; }

	/// <summary>
	/// Gets the collection of secondary command elements for the CommandBarFlyout.
	/// </summary>
	public IAvaloniaList<IFACommandBarElement> SecondaryCommands { get; }

	/// <summary>
	/// Gets or sets a value that indicates whether or not the CommandBarFlyout should 
	/// always stay in its Expanded state and block the user from entering the Collapsed state. 
	/// Defaults to false.
	/// </summary>
	public bool AlwaysExpanded
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(AlwaysExpandedProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(AlwaysExpandedProperty, value, (BindingPriority)0);
		}
	}

	public FACommandBarFlyout()
	{
		_commandBar = new FACommandBarFlyoutCommandBar();
		_commandBar.SetOwningFlyout(this);
		PrimaryCommands = (IAvaloniaList<IFACommandBarElement>)(object)new AvaloniaList<IFACommandBarElement>();
		SecondaryCommands = (IAvaloniaList<IFACommandBarElement>)(object)new AvaloniaList<IFACommandBarElement>();
		((INotifyCollectionChanged)PrimaryCommands).CollectionChanged += delegate(object? s, NotifyCollectionChangedEventArgs e)
		{
			if (_commandBar != null)
			{
				switch (e.Action)
				{
				case NotifyCollectionChangedAction.Add:
					_commandBar.PrimaryCommands.InsertRange(e.NewStartingIndex, e.NewItems.Cast<IFACommandBarElement>());
					break;
				case NotifyCollectionChangedAction.Remove:
					_commandBar.PrimaryCommands.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
					break;
				case NotifyCollectionChangedAction.Replace:
				case NotifyCollectionChangedAction.Move:
					_commandBar.PrimaryCommands.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
					_commandBar.PrimaryCommands.InsertRange(e.NewStartingIndex, e.NewItems.Cast<IFACommandBarElement>());
					break;
				case NotifyCollectionChangedAction.Reset:
					((ICollection<IFACommandBarElement>)_commandBar.PrimaryCommands).Clear();
					break;
				}
			}
		};
		((INotifyCollectionChanged)SecondaryCommands).CollectionChanged += delegate(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (_commandBar != null)
			{
				switch (e.Action)
				{
				case NotifyCollectionChangedAction.Add:
				{
					_commandBar.SecondaryCommands.InsertRange(e.NewStartingIndex, e.NewItems.Cast<IFACommandBarElement>());
					for (int k = 0; k < e.NewItems.Count; k++)
					{
						if (e.NewItems[k] is FACommandBarButton fACommandBarButton3)
						{
							((Button)fACommandBarButton3).Click += OnCommandBarButtonInSecondaryCommandsClick;
						}
						else if (e.NewItems[k] is FACommandBarToggleButton fACommandBarToggleButton3)
						{
							((Button)fACommandBarToggleButton3).Click += OnCommandBarButtonInSecondaryCommandsClick;
						}
					}
					break;
				}
				case NotifyCollectionChangedAction.Remove:
				{
					_commandBar.SecondaryCommands.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
					for (int j = 0; j < e.OldItems.Count; j++)
					{
						if (e.OldItems[j] is FACommandBarButton fACommandBarButton2)
						{
							((Button)fACommandBarButton2).Click -= OnCommandBarButtonInSecondaryCommandsClick;
						}
						else if (e.OldItems[j] is FACommandBarToggleButton fACommandBarToggleButton2)
						{
							((Button)fACommandBarToggleButton2).Click -= OnCommandBarButtonInSecondaryCommandsClick;
						}
					}
					break;
				}
				case NotifyCollectionChangedAction.Replace:
				case NotifyCollectionChangedAction.Move:
					_commandBar.SecondaryCommands.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
					_commandBar.SecondaryCommands.InsertRange(e.NewStartingIndex, e.NewItems.Cast<IFACommandBarElement>());
					break;
				case NotifyCollectionChangedAction.Reset:
					((ICollection<IFACommandBarElement>)_commandBar.SecondaryCommands).Clear();
					if (e.OldItems != null)
					{
						for (int i = 0; i < e.OldItems.Count; i++)
						{
							if (e.OldItems[i] is FACommandBarButton fACommandBarButton)
							{
								((Button)fACommandBarButton).Click -= OnCommandBarButtonInSecondaryCommandsClick;
							}
							else if (e.OldItems[i] is FACommandBarToggleButton fACommandBarToggleButton)
							{
								((Button)fACommandBarToggleButton).Click -= OnCommandBarButtonInSecondaryCommandsClick;
							}
						}
					}
					break;
				}
			}
		};
	}

	protected override Control CreatePresenter()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		_presenter = new FlyoutPresenter
		{
			Background = null,
			Foreground = null,
			BorderBrush = null,
			MinWidth = 0.0,
			MaxWidth = double.PositiveInfinity,
			MinHeight = 0.0,
			MaxHeight = double.PositiveInfinity,
			BorderThickness = new Thickness(0.0),
			Padding = new Thickness(0.0),
			Content = _commandBar
		};
		return (Control)(object)_presenter;
	}

	protected override void OnOpening(CancelEventArgs args)
	{
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		((PopupFlyoutBase)this).OnOpening(args);
		if (PrimaryCommands.Count > 0 && _commandBar.PrimaryCommands.Count == 0)
		{
			_commandBar.PrimaryCommands.AddRange((IEnumerable<IFACommandBarElement>)PrimaryCommands);
		}
		if (SecondaryCommands.Count > 0 && _commandBar.SecondaryCommands.Count == 0)
		{
			_commandBar.SecondaryCommands.AddRange((IEnumerable<IFACommandBarElement>)SecondaryCommands);
			for (int i = 0; i < SecondaryCommands.Count; i++)
			{
				if (SecondaryCommands[i] is FACommandBarButton fACommandBarButton)
				{
					((Button)fACommandBarButton).Click += OnCommandBarButtonInSecondaryCommandsClick;
				}
				else if (SecondaryCommands[i] is FACommandBarToggleButton fACommandBarToggleButton)
				{
					((Button)fACommandBarToggleButton).Click += OnCommandBarButtonInSecondaryCommandsClick;
				}
			}
		}
		if (AlwaysExpanded)
		{
			_commandBar.OverflowButtonVisibility = FACommandBarOverflowButtonVisibility.Collapsed;
			((PopupFlyoutBase)this).ShowMode = (FlyoutShowMode)0;
		}
		else
		{
			_commandBar.OverflowButtonVisibility = FACommandBarOverflowButtonVisibility.Auto;
		}
		if (((int)((PopupFlyoutBase)this).ShowMode == 0 && SecondaryCommands.Count > 0) || PrimaryCommands.Count == 0)
		{
			_commandBar.IsOpen = true;
		}
	}

	protected override void OnClosed()
	{
		((FlyoutBase)this).OnClosed();
		_commandBar.IsOpen = false;
	}

	private void OnCommandBarButtonInSecondaryCommandsClick(object sender, RoutedEventArgs e)
	{
		((PopupFlyoutBase)this).HideCore(false);
	}
}
