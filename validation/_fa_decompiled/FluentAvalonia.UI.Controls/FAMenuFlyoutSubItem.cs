using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a menu item that displays a sub-menu in a <see cref="T:FluentAvalonia.UI.Controls.FAMenuFlyout" /> control.
/// </summary>
[PseudoClasses(new string[] { ":submenuopen" })]
public class FAMenuFlyoutSubItem : FAMenuFlyoutItemBase
{
	private Popup _subMenu;

	private FAMenuFlyoutPresenter _presenter;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyoutSubItem.Text" /> property
	/// </summary>
	public static readonly StyledProperty<string> TextProperty = FAMenuFlyoutItem.TextProperty.AddOwner<FAMenuFlyoutSubItem>((StyledPropertyMetadata<string>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyoutSubItem.IconSource" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconSource> IconSourceProperty = FASettingsExpander.IconSourceProperty.AddOwner<FAMenuFlyoutSubItem>((StyledPropertyMetadata<FAIconSource>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyoutSubItem.Items" /> property
	/// </summary>
	public static readonly StyledProperty<IEnumerable> ItemsSourceProperty = ItemsControl.ItemsSourceProperty.AddOwner<FAMenuFlyoutSubItem>((StyledPropertyMetadata<IEnumerable>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyoutSubItem.ItemTemplate" /> property
	/// </summary>
	public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty = ItemsControl.ItemTemplateProperty.AddOwner<FAMenuFlyoutSubItem>((StyledPropertyMetadata<IDataTemplate>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyoutSubItem.ItemContainerTheme" /> property
	/// </summary>
	public static readonly StyledProperty<ControlTheme> ItemContainerThemeProperty = ItemsControl.ItemContainerThemeProperty.AddOwner<ControlTheme>((StyledPropertyMetadata<ControlTheme>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyoutSubItem.TemplateSettings" /> property
	/// </summary>
	public static readonly StyledProperty<FAMenuFlyoutItemTemplateSettings> TemplateSettingsProperty = FAMenuFlyoutItem.TemplateSettingsProperty.AddOwner<FAMenuFlyoutSubItem>((StyledPropertyMetadata<FAMenuFlyoutItemTemplateSettings>)null);

	private const string s_pcSubmenuOpen = ":submenuopen";

	/// <summary>
	/// Gets or sets the text content of a MenuFlyoutSubItem.
	/// </summary>
	public string Text
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(TextProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(TextProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the graphic content of the menu flyout subitem.
	/// </summary>
	public FAIconSource IconSource
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAIconSource>(IconSourceProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAIconSource>(IconSourceProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the items of the MenuFlyoutSubItem
	/// </summary>
	/// <remarks>
	/// NOTE: Unlike normal ItemsControls, when ItemsSource is set, this property will
	/// not act as a view over the ItemsSource
	/// </remarks>
	[Content]
	public IList Items { get; }

	/// <summary>
	/// Gets or sets the collection used to generate the content of the sub-menu.
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
	/// Gets the template settings for this MenuFlyoutItem
	/// </summary>
	public FAMenuFlyoutItemTemplateSettings TemplateSettings
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAMenuFlyoutItemTemplateSettings>(TemplateSettingsProperty);
		}
		private set
		{
			((AvaloniaObject)this).SetValue<FAMenuFlyoutItemTemplateSettings>(TemplateSettingsProperty, value, (BindingPriority)0);
		}
	}

	public bool IsPointerOverSubMenu
	{
		get
		{
			Popup subMenu = _subMenu;
			if (subMenu == null)
			{
				return false;
			}
			return subMenu.IsPointerOverPopup;
		}
	}

	public FAMenuFlyoutSubItem()
	{
		TemplateSettings = new FAMenuFlyoutItemTemplateSettings();
		AvaloniaList<object> val = new AvaloniaList<object>();
		val.CollectionChanged += ItemsCollectionChanged;
		Items = (IList)val;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((Control)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)IconSourceProperty)
		{
			TemplateSettings.Icon = FAIconHelpers.CreateFromUnknown(AvaloniaPropertyChangedExtensions.GetNewValue<FAIconSource>(change));
		}
		else if (change.Property == (AvaloniaProperty)(object)ItemsSourceProperty)
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

	protected override void OnPointerEntered(PointerEventArgs e)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		base.OnPointerEntered(e);
		RoutedEventArgs e2 = new RoutedEventArgs((RoutedEvent)(object)MenuItem.PointerEnteredItemEvent, (object)this);
		((Interactive)this).RaiseEvent(e2);
	}

	protected override void OnPointerExited(PointerEventArgs e)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		base.OnPointerExited(e);
		RoutedEventArgs e2 = new RoutedEventArgs((RoutedEvent)(object)MenuItem.PointerExitedItemEvent, (object)this);
		((Interactive)this).RaiseEvent(e2);
	}

	internal void Open(bool fromKeyboard = false)
	{
		InitPopup();
		_subMenu.IsOpen = true;
		_presenter.MenuOpened(fromKeyboard);
	}

	internal void Close(bool isFullClose = false)
	{
		if (_presenter == null)
		{
			return;
		}
		foreach (Control realizedContainer in ((ItemsControl)_presenter).GetRealizedContainers())
		{
			if (realizedContainer is FAMenuFlyoutSubItem fAMenuFlyoutSubItem)
			{
				fAMenuFlyoutSubItem.Close();
			}
		}
		if (_subMenu != null)
		{
			if (!_subMenu.IsOpen)
			{
				return;
			}
			_subMenu.IsOpen = false;
		}
		if (isFullClose)
		{
			VisualExtensions.FindAncestorOfType<FAMenuFlyoutPresenter>((Visual)(object)this, false).CloseMenu();
		}
	}

	private void InitPopup()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		if (_subMenu == null)
		{
			FAMenuFlyoutPresenter fAMenuFlyoutPresenter = new FAMenuFlyoutPresenter();
			((ItemsControl)fAMenuFlyoutPresenter).ItemsSource = ItemsSource ?? Items;
			IndexerDescriptor val = !(AvaloniaProperty)(object)ItemContainerThemeProperty;
			((AvaloniaObject)fAMenuFlyoutPresenter)[val] = ((AvaloniaObject)this)[!(AvaloniaProperty)(object)ItemContainerThemeProperty];
			fAMenuFlyoutPresenter.InternalParent = (AvaloniaObject)(object)this;
			_presenter = fAMenuFlyoutPresenter;
			_subMenu = new Popup
			{
				Child = (Control)(object)_presenter,
				HorizontalOffset = -4.0,
				WindowManagerAddShadowHint = false,
				Placement = (PlacementMode)6,
				PlacementAnchor = (PopupAnchor)9,
				PlacementGravity = (PopupGravity)10,
				PlacementTarget = (Control)(object)this
			};
			((ICollection<ILogical>)((StyledElement)this).LogicalChildren).Add((ILogical)(object)_subMenu);
			_subMenu.Opened += OnPopupOpen;
			_subMenu.Closed += OnPopupClose;
		}
	}

	private void OnPopupOpen(object sender, EventArgs e)
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":submenuopen", true);
	}

	private void OnPopupClose(object sender, EventArgs e)
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":submenuopen", false);
	}

	private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (ItemsSource != null)
		{
			throw new InvalidOperationException("Cannot edit Items when ItemsSource is set");
		}
	}
}
