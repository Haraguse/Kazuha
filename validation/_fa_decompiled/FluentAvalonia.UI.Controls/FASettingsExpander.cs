using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Control used to display or group settings options within an app, like in
/// the Windows 11 Settings app
/// </summary>
[PseudoClasses(new string[] { ":allowClick", ":empty" })]
[TemplatePart("Expander", typeof(Expander))]
[TemplatePart("ContentHost", typeof(FASettingsExpanderItem))]
public class FASettingsExpander : HeaderedItemsControl, ICommandSource
{
	private bool _commandCanExecute = true;

	private Expander _expander;

	private ToggleButton _expanderToggleButton;

	private FASettingsExpanderItem _contentHost;

	private int _iconCount;

	private bool _hasAppliedTemplate;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpander.Description" /> property
	/// </summary>
	public static readonly StyledProperty<string> DescriptionProperty = AvaloniaProperty.Register<FASettingsExpander, string>("Description", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpander.IconSource" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconSource> IconSourceProperty = AvaloniaProperty.Register<FASettingsExpander, FAIconSource>("IconSource", (FAIconSource)null, false, (BindingMode)1, (Func<FAIconSource, bool>)null, (Func<AvaloniaObject, FAIconSource, FAIconSource>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpander.Footer" /> property
	/// </summary>
	public static readonly StyledProperty<object> FooterProperty = AvaloniaProperty.Register<FASettingsExpander, object>("Footer", (object)null, false, (BindingMode)1, (Func<object, bool>)null, (Func<AvaloniaObject, object, object>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpander.FooterTemplate" /> property
	/// </summary>
	public static readonly StyledProperty<IDataTemplate> FooterTemplateProperty = AvaloniaProperty.Register<FASettingsExpander, IDataTemplate>("FooterTemplate", (IDataTemplate)null, false, (BindingMode)1, (Func<IDataTemplate, bool>)null, (Func<AvaloniaObject, IDataTemplate, IDataTemplate>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpander.IsExpanded" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsExpandedProperty = Expander.IsExpandedProperty.AddOwner<FASettingsExpander>((StyledPropertyMetadata<bool>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpander.ActionIconSource" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconSource> ActionIconSourceProperty = AvaloniaProperty.Register<FASettingsExpander, FAIconSource>("ActionIconSource", (FAIconSource)null, false, (BindingMode)1, (Func<FAIconSource, bool>)null, (Func<AvaloniaObject, FAIconSource, FAIconSource>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpander.IsClickEnabled" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsClickEnabledProperty = AvaloniaProperty.Register<FASettingsExpander, bool>("IsClickEnabled", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpander.Command" /> property
	/// </summary>
	public static readonly StyledProperty<ICommand> CommandProperty = Button.CommandProperty.AddOwner<FASettingsExpander>((StyledPropertyMetadata<ICommand>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpander.CommandParameter" /> property
	/// </summary>
	public static readonly StyledProperty<object> CommandParameterProperty = Button.CommandParameterProperty.AddOwner<FASettingsExpander>((StyledPropertyMetadata<object>)null);

	/// <summary>
	/// Defines the <see cref="E:FluentAvalonia.UI.Controls.FASettingsExpander.Click" /> event
	/// </summary>
	public static readonly RoutedEvent<RoutedEventArgs> ClickEvent = RoutedEvent.Register<FASettingsExpander, RoutedEventArgs>("Click", (RoutingStrategies)6);

	private const string s_tpExpander = "Expander";

	private const string s_tpContentHost = "ContentHost";

	private const string s_pcEmpty = ":empty";

	private const string s_pcIconPlaceholder = ":iconPlaceholder";

	/// <summary>
	/// Gets or sets the description text
	/// </summary>
	public string Description
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(DescriptionProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(DescriptionProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the IconSource for the SettingsExpander
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
	/// Gets or sets the Footer content for the SettingsExpander
	/// </summary>
	public object Footer
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<object>(FooterProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<object>(FooterProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the Footer template for the SettingsExpander
	/// </summary>
	public IDataTemplate FooterTemplate
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IDataTemplate>(FooterTemplateProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IDataTemplate>(FooterTemplateProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets whether the SettingsExpander is currently expanded
	/// </summary>
	public bool IsExpanded
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsExpandedProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsExpandedProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the Action IconSource when <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpander.IsClickEnabled" /> is true
	/// </summary>
	public FAIconSource ActionIconSource
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAIconSource>(ActionIconSourceProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAIconSource>(ActionIconSourceProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets whether the item is clickable which can be used for navigation within an app
	/// </summary>
	/// <remarks>
	/// This property can only be set if no items are added to the SettingsExpander. Attempting to mark
	/// a settings expander clickable and adding child items will throw an exception
	/// </remarks>
	public bool IsClickEnabled
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsClickEnabledProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsClickEnabledProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the Command that is invoked upon clicking the item
	/// </summary>
	public ICommand Command
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<ICommand>(CommandProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<ICommand>(CommandProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the command parameter
	/// </summary>
	public object CommandParameter
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<object>(CommandParameterProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<object>(CommandParameterProperty, value, (BindingPriority)0);
		}
	}

	protected override bool IsEnabledCore
	{
		get
		{
			if (((InputElement)this).IsEnabledCore)
			{
				return _commandCanExecute;
			}
			return false;
		}
	}

	/// <summary>
	/// Event raised when the SettingsExpander is clicked and IsClickEnabled = true
	/// </summary>
	public event EventHandler<RoutedEventArgs> Click
	{
		add
		{
			((Interactive)this).AddHandler<RoutedEventArgs>(ClickEvent, value, (RoutingStrategies)5, false);
		}
		remove
		{
			((Interactive)this).RemoveHandler<RoutedEventArgs>(ClickEvent, value);
		}
	}

	public FASettingsExpander()
	{
		((ItemsControl)this).ItemsView.CollectionChanged += ItemsCollectionChanged;
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		((ItemsControl)this).OnApplyTemplate(e);
		_expander = NameScopeExtensions.Get<Expander>(e.NameScope, "Expander");
		((Control)_expander).Loaded += ExpanderLoaded;
		_expander.Expanding += ExpanderExpanding;
		_contentHost = NameScopeExtensions.Get<FASettingsExpanderItem>(e.NameScope, "ContentHost");
		_hasAppliedTemplate = true;
		SetIcons();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		((ItemsControl)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)IsClickEnabledProperty)
		{
			bool newValue = AvaloniaPropertyChangedExtensions.GetNewValue<bool>(change);
			if ((((ItemsControl)this).ItemCount > 0) & newValue)
			{
				throw new InvalidOperationException("Cannot set Items and mark IsClickEnabled to true on a SettingsExpander");
			}
			if (_expanderToggleButton != null)
			{
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_expanderToggleButton).Classes, ":allowClick", newValue || ((ItemsControl)this).ItemCount > 0);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_expanderToggleButton).Classes, ":empty", newValue);
			}
		}
		else if (change.Property == (AvaloniaProperty)(object)IsExpandedProperty)
		{
			if (((ItemsControl)this).ItemCount == 0 && AvaloniaPropertyChangedExtensions.GetNewValue<bool>(change) && VisualExtensions.IsAttachedToVisualTree((Visual)(object)this))
			{
				Dispatcher.UIThread.Post((Action)delegate
				{
					IsExpanded = false;
				}, DispatcherPriority.Send);
			}
		}
		else if (change.Property == (AvaloniaProperty)(object)CommandProperty)
		{
			if (((ILogical)this).IsAttachedToLogicalTree)
			{
				var (command, command2) = AvaloniaPropertyChangedExtensions.GetOldAndNewValue<ICommand>(change);
				if (command != null)
				{
					command.CanExecuteChanged -= CanExecuteChanged;
				}
				if (command2 != null)
				{
					command2.CanExecuteChanged += CanExecuteChanged;
				}
			}
			CanExecuteChanged(this, EventArgs.Empty);
		}
		else if (change.Property == (AvaloniaProperty)(object)CommandParameterProperty)
		{
			CanExecuteChanged(this, EventArgs.Empty);
		}
		else if (change.Property == (AvaloniaProperty)(object)IconSourceProperty)
		{
			if (change.OldValue != null)
			{
				_iconCount--;
			}
			if (change.NewValue != null)
			{
				_iconCount++;
			}
			SetIcons();
		}
	}

	private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (IsClickEnabled && ((ItemsControl)this).ItemsView.Count > 0)
		{
			throw new InvalidOperationException("Cannot set Items and mark IsClickEnabled to true on a SettingsExpander");
		}
		if (_expanderToggleButton != null)
		{
			bool flag = ((ItemsControl)this).ItemCount > 0;
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_expanderToggleButton).Classes, ":allowClick", flag);
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_expanderToggleButton).Classes, ":empty", !flag);
		}
	}

	protected override bool NeedsContainerOverride(object item, int index, out object recycleKey)
	{
		bool flag = item is FASettingsExpanderItem;
		recycleKey = (flag ? null : "FASettingsExpanderItem");
		return !flag;
	}

	protected override Control CreateContainerForItemOverride(object item, int index, object recycleKey)
	{
		if (((ITemplate<object, Control>)(object)DataTemplateExtensions.FindDataTemplate((Control)(object)this, item, ((ItemsControl)this).ItemTemplate))?.Build(item) is FASettingsExpanderItem fASettingsExpanderItem)
		{
			((StyledElement)fASettingsExpanderItem).DataContext = item;
			fASettingsExpanderItem.IsContainerFromTemplate = true;
			return (Control)(object)fASettingsExpanderItem;
		}
		return (Control)(object)new FASettingsExpanderItem();
	}

	protected override void PrepareContainerForItemOverride(Control container, object item, int index)
	{
		FASettingsExpanderItem obj = container as FASettingsExpanderItem;
		if (!obj.IsContainerFromTemplate)
		{
			((ItemsControl)this).PrepareContainerForItemOverride(container, item, index);
		}
		if (obj.IconSource != null)
		{
			_iconCount++;
		}
	}

	protected override void ClearContainerForItemOverride(Control container)
	{
		((ItemsControl)this).ClearContainerForItemOverride(container);
		if (container is FASettingsExpanderItem { IconSource: not null })
		{
			_iconCount--;
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		Size result = ((Layoutable)this).MeasureOverride(availableSize);
		SetIcons();
		return result;
	}

	/// <summary>
	/// Invoked when the SettingsExpander is clicked when IsClickEnabled = true
	/// </summary>
	protected internal virtual void OnClick()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		RoutedEventArgs e = new RoutedEventArgs((RoutedEvent)(object)ClickEvent);
		((Interactive)this).RaiseEvent(e);
		object commandParameter = CommandParameter;
		ICommand command = Command;
		if (!e.Handled && command != null && command.CanExecute(commandParameter))
		{
			command.Execute(commandParameter);
		}
	}

	private void ExpanderLoaded(object sender, RoutedEventArgs e)
	{
		((Control)_expander).Loaded -= ExpanderLoaded;
		if (_expanderToggleButton != null)
		{
			((Button)_expanderToggleButton).Click -= ExpanderToggleButtonClick;
		}
		ToggleButton val = TemplateExtensions.GetTemplateChildren((TemplatedControl)(object)_expander).OfType<ToggleButton>().FirstOrDefault();
		if (val == null)
		{
			throw new InvalidOperationException("Invalid template for SettingsExpander. Unable to find ToggleButton inside Expander");
		}
		_expanderToggleButton = val;
		((Button)_expanderToggleButton).Click += ExpanderToggleButtonClick;
		_ = IsClickEnabled;
		PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_expanderToggleButton).Classes, ":allowClick", IsClickEnabled || ((ItemsControl)this).ItemCount > 0);
		PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_expanderToggleButton).Classes, ":empty", IsClickEnabled || ((ItemsControl)this).ItemCount == 0);
	}

	private void ExpanderExpanding(object sender, CancelRoutedEventArgs e)
	{
		if (((ItemsControl)this).ItemCount == 0 && IsClickEnabled)
		{
			e.Cancel = true;
			((RoutedEventArgs)e).Handled = true;
		}
	}

	private void ExpanderToggleButtonClick(object sender, RoutedEventArgs e)
	{
		if (e.Source == _expanderToggleButton)
		{
			e.Handled = true;
			OnClick();
		}
	}

	private void CanExecuteChanged(object sender, EventArgs e)
	{
		bool flag = Command?.CanExecute(CommandParameter) ?? true;
		if (flag != _commandCanExecute)
		{
			_commandCanExecute = flag;
			((InputElement)this).UpdateIsEffectivelyEnabled();
		}
	}

	void ICommandSource.CanExecuteChanged(object sender, EventArgs e)
	{
		CanExecuteChanged(sender, e);
	}

	private void SetIcons()
	{
		if (!_hasAppliedTemplate || ((ItemsControl)this).ItemCount == 0)
		{
			return;
		}
		bool flag = _iconCount > 0;
		PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_contentHost).Classes, ":iconPlaceholder", flag);
		((ItemsControl)this).GetRealizedContainers();
		foreach (Control realizedContainer in ((ItemsControl)this).GetRealizedContainers())
		{
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)realizedContainer).Classes, ":iconPlaceholder", flag);
		}
	}

	internal void InvalidateIcons(FASettingsExpanderItem item)
	{
		if (item != _contentHost)
		{
			SetIcons();
		}
	}
}
