using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a flyout that displays a menu of commands.
/// </summary>
public class FAMenuFlyout : PopupFlyoutBase
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyout.Items" /> property
	/// </summary>
	public static readonly StyledProperty<IEnumerable> ItemsSourceProperty = ItemsControl.ItemsSourceProperty.AddOwner<FAMenuFlyout>((StyledPropertyMetadata<IEnumerable>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyout.ItemTemplate" /> property
	/// </summary>
	public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty = ItemsControl.ItemTemplateProperty.AddOwner<FAMenuFlyout>((StyledPropertyMetadata<IDataTemplate>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyout.ItemContainerTheme" /> property
	/// </summary>
	public static readonly StyledProperty<ControlTheme> ItemContainerThemeProperty = ItemsControl.ItemContainerThemeProperty.AddOwner<ControlTheme>((StyledPropertyMetadata<ControlTheme>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyout.FlyoutPresenterTheme" /> property
	/// </summary>
	public static readonly StyledProperty<ControlTheme> FlyoutPresenterThemeProperty = AvaloniaProperty.Register<FAMenuFlyout, ControlTheme>("FlyoutPresenterTheme", (ControlTheme)null, false, (BindingMode)1, (Func<ControlTheme, bool>)null, (Func<AvaloniaObject, ControlTheme, ControlTheme>)null, false);

	private FAMenuFlyoutPresenter _presenter;

	private Classes _classes;

	/// <summary>
	/// Gets the items of the MenuFlyoutSubItem
	/// </summary>
	/// <remarks>
	/// NOTE: Unlike normal ItemsControls, when ItemsSource is set, this property will
	/// not act as a view over the ItemsSource
	/// </remarks>
	[Content]
	public IList Items { get; private set; }

	/// <summary>
	/// Gets or sets the items of the MenuFlyout
	/// </summary>
	public IEnumerable ItemsSource
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IEnumerable>(ItemsSourceProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IEnumerable>(ItemsSourceProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the template used for the items
	/// </summary>
	public IDataTemplate ItemTemplate
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IDataTemplate>(ItemTemplateProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IDataTemplate>(ItemTemplateProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the <see cref="T:Avalonia.Styling.ControlTheme" /> to apply for the items
	/// </summary>
	public ControlTheme ItemContainerTheme
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<ControlTheme>(ItemContainerThemeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<ControlTheme>(ItemContainerThemeProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Sets the Classes used for styling the MenuFlyoutPresenter. This property
	/// takes the place of WinUI's MenuFlyoutPresenterStyle
	/// </summary>
	public Classes FlyoutPresenterClasses
	{
		get
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Expected O, but got Unknown
			//IL_0017: Expected O, but got Unknown
			Classes obj = _classes;
			if (obj == null)
			{
				Classes val = new Classes();
				Classes val2 = val;
				_classes = val;
				obj = val2;
			}
			return obj;
		}
	}

	/// <summary>
	/// Gets or sets the ControlTheme for the flyout presenter
	/// </summary>
	public ControlTheme FlyoutPresenterTheme
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<ControlTheme>(FlyoutPresenterThemeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<ControlTheme>(FlyoutPresenterThemeProperty, value, (BindingPriority)0);
		}
	}

	public FAMenuFlyout()
	{
		AvaloniaList<object> val = new AvaloniaList<object>();
		val.CollectionChanged += ItemsCollectionChanged;
		Items = (IList)val;
	}

	internal void Close()
	{
		((FlyoutBase)this).Hide();
	}

	protected override Control CreatePresenter()
	{
		FAMenuFlyoutPresenter fAMenuFlyoutPresenter = new FAMenuFlyoutPresenter();
		((ItemsControl)fAMenuFlyoutPresenter).ItemsSource = ItemsSource ?? Items;
		IndexerDescriptor val = !(AvaloniaProperty)(object)ItemContainerThemeProperty;
		((AvaloniaObject)fAMenuFlyoutPresenter)[val] = ((AvaloniaObject)this)[!(AvaloniaProperty)(object)ItemContainerThemeProperty];
		IndexerDescriptor val2 = !(AvaloniaProperty)(object)ItemTemplateProperty;
		((AvaloniaObject)fAMenuFlyoutPresenter)[val2] = ((AvaloniaObject)this)[!(AvaloniaProperty)(object)ItemTemplateProperty];
		fAMenuFlyoutPresenter.InternalParent = (AvaloniaObject)(object)this;
		_presenter = fAMenuFlyoutPresenter;
		return (Control)(object)_presenter;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((AvaloniaObject)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)ItemsSourceProperty)
		{
			if (Items.Count > 0)
			{
				throw new InvalidOperationException("Items collection must be empty before using ItemsSource.");
			}
			IEnumerable newValue = AvaloniaPropertyChangedExtensions.GetNewValue<IEnumerable>(change);
			if (_presenter != null)
			{
				((ItemsControl)_presenter).ItemsSource = newValue ?? Items;
			}
		}
	}

	protected override void OnOpened()
	{
		if (_classes != null)
		{
			SetPresenterClasses((Control)(object)_presenter, FlyoutPresenterClasses);
		}
		ControlTheme flyoutPresenterTheme = FlyoutPresenterTheme;
		if (flyoutPresenterTheme != null)
		{
			((StyledElement)_presenter).Theme = flyoutPresenterTheme;
		}
		((FlyoutBase)this).OnOpened();
		_presenter.MenuOpened();
	}

	protected override void OnClosed()
	{
		((FlyoutBase)this).OnClosed();
		_presenter.MenuClosed();
	}

	private static void SetPresenterClasses(Control presenter, Classes classes)
	{
		for (int num = ((AvaloniaList<string>)(object)((StyledElement)presenter).Classes).Count - 1; num >= 0; num--)
		{
			if (!((AvaloniaList<string>)(object)classes).Contains(((AvaloniaList<string>)(object)((StyledElement)presenter).Classes)[num]) && !((AvaloniaList<string>)(object)((StyledElement)presenter).Classes)[num].Contains(':'))
			{
				((AvaloniaList<string>)(object)((StyledElement)presenter).Classes).RemoveAt(num);
			}
		}
		((AvaloniaList<string>)(object)((StyledElement)presenter).Classes).AddRange((IEnumerable<string>)classes);
	}

	private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
	{
		if (ItemsSource != null)
		{
			throw new InvalidOperationException("Cannot edit Items when ItemsSource is set.");
		}
	}
}
