using System;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// An item displayed within a <see cref="T:FluentAvalonia.UI.Controls.FASettingsExpander" />
/// </summary>
[PseudoClasses(new string[] { ":footerBottom", ":footer", ":content", ":description" })]
[PseudoClasses(new string[] { ":allowClick" })]
[PseudoClasses(new string[] { ":pressed" })]
[PseudoClasses(new string[] { ":icon", ":actionIcon" })]
public class FASettingsExpanderItem : ContentControl, ICommandSource
{
	private bool _commandCanExecute = true;

	private bool _allowInteraction;

	private bool _isPressed;

	private bool _hasFooter;

	private bool _isFooterAtBottom;

	private IDisposable _adaptiveWidthDisposable;

	private double _adaptiveWidthTrigger = 460.0;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpanderItem.Description" /> property
	/// </summary>
	public static readonly StyledProperty<string> DescriptionProperty = FASettingsExpander.DescriptionProperty.AddOwner<FASettingsExpanderItem>((StyledPropertyMetadata<string>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpanderItem.IconSource" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconSource> IconSourceProperty = FASettingsExpander.IconSourceProperty.AddOwner<FASettingsExpanderItem>((StyledPropertyMetadata<FAIconSource>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpanderItem.Footer" /> property
	/// </summary>
	public static readonly StyledProperty<object> FooterProperty = FASettingsExpander.FooterProperty.AddOwner<FASettingsExpanderItem>((StyledPropertyMetadata<object>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpanderItem.FooterTemplate" /> property
	/// </summary>
	public static readonly StyledProperty<IDataTemplate> FooterTemplateProperty = FASettingsExpander.FooterTemplateProperty.AddOwner<FASettingsExpanderItem>((StyledPropertyMetadata<IDataTemplate>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpanderItem.ActionIconSource" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconSource> ActionIconSourceProperty = FASettingsExpander.ActionIconSourceProperty.AddOwner<FASettingsExpanderItem>((StyledPropertyMetadata<FAIconSource>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpanderItem.IsClickEnabled" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsClickEnabledProperty = FASettingsExpander.IsClickEnabledProperty.AddOwner<FASettingsExpanderItem>((StyledPropertyMetadata<bool>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpanderItem.Command" /> property
	/// </summary>
	public static readonly StyledProperty<ICommand> CommandProperty = Button.CommandProperty.AddOwner<FASettingsExpanderItem>((StyledPropertyMetadata<ICommand>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpanderItem.CommandParameter" /> property
	/// </summary>
	public static readonly StyledProperty<object> CommandParameterProperty = Button.CommandParameterProperty.AddOwner<FASettingsExpanderItem>((StyledPropertyMetadata<object>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpanderItem.TemplateSettings" /> property
	/// </summary>
	public static readonly StyledProperty<FASettingsExpanderTemplateSettings> TemplateSettingsProperty = AvaloniaProperty.Register<FASettingsExpanderItem, FASettingsExpanderTemplateSettings>("TemplateSettings", (FASettingsExpanderTemplateSettings)null, false, (BindingMode)1, (Func<FASettingsExpanderTemplateSettings, bool>)null, (Func<AvaloniaObject, FASettingsExpanderTemplateSettings, FASettingsExpanderTemplateSettings>)null, false);

	/// <summary>
	/// Defines the <see cref="E:FluentAvalonia.UI.Controls.FASettingsExpanderItem.Click" /> event
	/// </summary>
	public static readonly RoutedEvent<RoutedEventArgs> ClickEvent = FASettingsExpander.ClickEvent;

	private const string s_pcDescription = ":description";

	private const string s_pcContent = ":content";

	private const string s_pcActionIcon = ":actionIcon";

	private const string s_pcFooterBottom = ":footerBottom";

	private const string s_resAdaptiveWidthTrigger = "SettingsExpanderItemAdaptiveWidthTrigger";

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
	/// Gets or sets the Action IconSource when <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpanderItem.IsClickEnabled" /> is true
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

	/// <summary>
	/// Provides calculated values that can be referenced as TemplatedParent sources when defining 
	/// templates for a SettingsExpander. Not intended for general use.
	/// </summary>
	public FASettingsExpanderTemplateSettings TemplateSettings
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FASettingsExpanderTemplateSettings>(TemplateSettingsProperty);
		}
		private set
		{
			((AvaloniaObject)this).SetValue<FASettingsExpanderTemplateSettings>(TemplateSettingsProperty, value, (BindingPriority)0);
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

	internal bool IsContainerFromTemplate { get; set; }

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

	public FASettingsExpanderItem()
	{
		TemplateSettings = new FASettingsExpanderTemplateSettings();
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		if (_hasFooter)
		{
			if (_isFooterAtBottom)
			{
				if (((Size)(ref availableSize)).Width > _adaptiveWidthTrigger)
				{
					_isFooterAtBottom = false;
					PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":footerBottom", false);
				}
			}
			else if (((Size)(ref availableSize)).Width < _adaptiveWidthTrigger)
			{
				_isFooterAtBottom = true;
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":footerBottom", true);
			}
		}
		return ((Layoutable)this).MeasureOverride(availableSize);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		((TemplatedControl)this).OnApplyTemplate(e);
		_adaptiveWidthDisposable = ResourceNodeExtensions.GetResourceObservable((IResourceHost)(object)this, (object)"SettingsExpanderItemAdaptiveWidthTrigger", (Func<object, object>)null).Subscribe(OnAdaptiveWidthValueChanged);
		_allowInteraction = IsClickEnabled && VisualExtensions.FindAncestorOfType<ToggleButton>((Visual)(object)this, false) == null;
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":allowClick", _allowInteraction);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((ContentControl)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)IconSourceProperty)
		{
			OnIconSourceChanged(change);
		}
		else if (change.Property == (AvaloniaProperty)(object)ActionIconSourceProperty)
		{
			OnActionIconSourceChanged(change);
		}
		else if (change.Property == (AvaloniaProperty)(object)IsClickEnabledProperty)
		{
			OnIsClickEnabledChanged(change);
		}
		else if (change.Property == (AvaloniaProperty)(object)FooterProperty)
		{
			OnFooterChanged(change);
		}
		else if (change.Property == (AvaloniaProperty)(object)ContentControl.ContentProperty)
		{
			OnContentChanged(change);
		}
		else if (change.Property == (AvaloniaProperty)(object)DescriptionProperty)
		{
			OnDescriptionChanged(change);
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
	}

	protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		((TemplatedControl)this).OnAttachedToLogicalTree(e);
		if (Command != null)
		{
			Command.CanExecuteChanged += CanExecuteChanged;
			CanExecuteChanged(this, EventArgs.Empty);
		}
	}

	protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		((TemplatedControl)this).OnDetachedFromLogicalTree(e);
		_adaptiveWidthDisposable?.Dispose();
		_adaptiveWidthDisposable = null;
		if (Command != null)
		{
			Command.CanExecuteChanged -= CanExecuteChanged;
		}
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		((InputElement)this).OnPointerPressed(e);
		if (_allowInteraction && !((RoutedEventArgs)e).Handled)
		{
			PointerPoint currentPoint = ((PointerEventArgs)e).GetCurrentPoint((Visual)(object)this);
			PointerPointProperties properties = ((PointerPoint)(ref currentPoint)).Properties;
			if (((PointerPointProperties)(ref properties)).IsLeftButtonPressed)
			{
				_isPressed = true;
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":pressed", true);
				((RoutedEventArgs)e).Handled = true;
			}
		}
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		((InputElement)this).OnPointerMoved(e);
		if (_allowInteraction && !((RoutedEventArgs)e).Handled && e.Pointer.Captured != null)
		{
			PointerPoint currentPoint = e.GetCurrentPoint((Visual)(object)this);
			Rect val = ((Visual)this).Bounds;
			val = new Rect(((Rect)(ref val)).Size);
			if (((Rect)(ref val)).Contains(((PointerPoint)(ref currentPoint)).Position))
			{
				_isPressed = true;
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":pressed", true);
			}
			else
			{
				_isPressed = false;
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":pressed", false);
			}
		}
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		((Control)this).OnPointerReleased(e);
		if (_isPressed && _allowInteraction)
		{
			_isPressed = false;
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":pressed", false);
			if (!((RoutedEventArgs)e).Handled)
			{
				((RoutedEventArgs)e).Handled = true;
				OnClick();
			}
		}
	}

	protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
	{
		((InputElement)this).OnPointerCaptureLost(e);
		_isPressed = false;
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":pressed", false);
	}

	protected override bool RegisterContentPresenter(ContentPresenter presenter)
	{
		if (((StyledElement)presenter).Name == "ContentPresenter" || ((StyledElement)presenter).Name == "FooterPresenter")
		{
			return true;
		}
		return ((ContentControl)this).RegisterContentPresenter(presenter);
	}

	/// <summary>
	/// Invoked when the SettingsExpanderItem is clicked when IsClickEnabled = true
	/// </summary>
	protected virtual void OnClick()
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

	private void OnIconSourceChanged(AvaloniaPropertyChangedEventArgs args)
	{
		FAIconSource newValue = AvaloniaPropertyChangedExtensions.GetNewValue<FAIconSource>(args);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":icon", newValue != null);
		TemplateSettings.Icon = FAIconHelpers.CreateFromUnknown(newValue);
		VisualExtensions.FindAncestorOfType<FASettingsExpander>((Visual)(object)this, false)?.InvalidateIcons(this);
	}

	private void OnActionIconSourceChanged(AvaloniaPropertyChangedEventArgs args)
	{
		if (IsClickEnabled)
		{
			FAIconSource newValue = AvaloniaPropertyChangedExtensions.GetNewValue<FAIconSource>(args);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":actionIcon", newValue != null);
			TemplateSettings.ActionIcon = FAIconHelpers.CreateFromUnknown(newValue);
		}
	}

	private void OnIsClickEnabledChanged(AvaloniaPropertyChangedEventArgs args)
	{
		bool newValue = AvaloniaPropertyChangedExtensions.GetNewValue<bool>(args);
		_allowInteraction = newValue && VisualExtensions.FindAncestorOfType<ToggleButton>((Visual)(object)this, false) == null;
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":allowClick", _allowInteraction);
		FAIconSource actionIconSource = ActionIconSource;
		if (!newValue && actionIconSource != null)
		{
			TemplateSettings.ActionIcon = null;
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":actionIcon", false);
		}
		else if (actionIconSource != null)
		{
			TemplateSettings.ActionIcon = FAIconHelpers.CreateFromUnknown(actionIconSource);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":actionIcon", true);
		}
	}

	private void OnFooterChanged(AvaloniaPropertyChangedEventArgs args)
	{
		_hasFooter = args.NewValue != null;
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":footer", _hasFooter);
	}

	private void OnContentChanged(AvaloniaPropertyChangedEventArgs args)
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":content", args.NewValue != null);
	}

	private void OnDescriptionChanged(AvaloniaPropertyChangedEventArgs args)
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":description", args.NewValue != null);
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

	private void OnAdaptiveWidthValueChanged(object value)
	{
		if (value != AvaloniaProperty.UnsetValue)
		{
			_adaptiveWidthTrigger = Unsafe.Unbox<double>(value);
			((Layoutable)this).InvalidateMeasure();
		}
	}

	void ICommandSource.CanExecuteChanged(object sender, EventArgs e)
	{
		CanExecuteChanged(sender, e);
	}
}
