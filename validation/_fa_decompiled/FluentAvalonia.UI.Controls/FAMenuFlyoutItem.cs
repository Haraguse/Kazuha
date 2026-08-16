using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using FluentAvalonia.UI.Input;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a command in a <see cref="T:FluentAvalonia.UI.Controls.FAMenuFlyout" /> control.
/// </summary>
[PseudoClasses(new string[] { ":hotkey" })]
[PseudoClasses(new string[] { ":pressed" })]
public class FAMenuFlyoutItem : FAMenuFlyoutItemBase, ICommandSource
{
	private bool _canExecute = true;

	private KeyGesture _hotkey;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyoutItem.Text" /> property
	/// </summary>
	public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<FAMenuFlyoutItem, string>("Text", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyoutItem.IconSource" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconSource> IconSourceProperty = FASettingsExpander.IconSourceProperty.AddOwner<FAMenuFlyoutItem>((StyledPropertyMetadata<FAIconSource>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyoutItem.Command" /> property
	/// </summary>
	public static readonly StyledProperty<ICommand> CommandProperty = Button.CommandProperty.AddOwner<FAMenuFlyoutItem>((StyledPropertyMetadata<ICommand>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyoutItem.CommandParameter" /> property
	/// </summary>
	public static readonly StyledProperty<object> CommandParameterProperty = Button.CommandParameterProperty.AddOwner<FAMenuFlyoutItem>((StyledPropertyMetadata<object>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyoutItem.HotKey" /> property
	/// </summary>
	public static readonly StyledProperty<KeyGesture> HotKeyProperty = Button.HotKeyProperty.AddOwner<FAMenuFlyoutItem>((StyledPropertyMetadata<KeyGesture>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyoutItem.InputGesture" /> property
	/// </summary>
	public static readonly StyledProperty<KeyGesture> InputGestureProperty = AvaloniaProperty.Register<FAMenuFlyoutItem, KeyGesture>("InputGesture", (KeyGesture)null, false, (BindingMode)1, (Func<KeyGesture, bool>)null, (Func<AvaloniaObject, KeyGesture, KeyGesture>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyoutItem.TemplateSettings" /> property
	/// </summary>
	public static readonly StyledProperty<FAMenuFlyoutItemTemplateSettings> TemplateSettingsProperty = AvaloniaProperty.Register<FAMenuFlyoutItem, FAMenuFlyoutItemTemplateSettings>("TemplateSettings", (FAMenuFlyoutItemTemplateSettings)null, false, (BindingMode)1, (Func<FAMenuFlyoutItemTemplateSettings, bool>)null, (Func<AvaloniaObject, FAMenuFlyoutItemTemplateSettings, FAMenuFlyoutItemTemplateSettings>)null, false);

	/// <summary>
	/// Defines the <see cref="E:FluentAvalonia.UI.Controls.FAMenuFlyoutItem.Click" /> event
	/// </summary>
	public static readonly RoutedEvent<RoutedEventArgs> ClickEvent = MenuItem.ClickEvent;

	/// <summary>
	/// Gets or sets the text content of a MenuFlyoutItem.
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
	/// Gets or sets the graphic content of the menu flyout item.
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
	/// Gets or sets the KeyGesture that should invoke this MenuFlyoutItem
	/// </summary>
	public KeyGesture HotKey
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<KeyGesture>(HotKeyProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<KeyGesture>(HotKeyProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the input gesture displayed by the MenuFlyoutItem
	/// </summary>
	/// <remarks>
	/// This property is equivalent to WinUI's KeyboardAcceleratorTextOverride
	/// property. It allows you to specify a key gesture without mapping to 
	/// a hotkey. This property takes priority over <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyoutItem.HotKey" />
	/// </remarks>
	public KeyGesture InputGesture
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<KeyGesture>(InputGestureProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<KeyGesture>(InputGestureProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the command to invoke when the item is pressed.
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
	/// Gets or sets the parameter to pass to the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyoutItem.Command" /> property.
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

	protected override bool IsEnabledCore
	{
		get
		{
			if (((InputElement)this).IsEnabledCore)
			{
				return _canExecute;
			}
			return false;
		}
	}

	/// <summary>
	/// Raised when this MenuFlyoutItem is invoked
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

	/// <summary>
	/// Create instance of <see cref="T:FluentAvalonia.UI.Controls.FAMenuFlyoutItem" />.
	/// </summary>
	public FAMenuFlyoutItem()
	{
		TemplateSettings = new FAMenuFlyoutItemTemplateSettings();
	}

	/// <inheritdoc />
	protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		if (_hotkey != (KeyGesture)null)
		{
			HotKey = _hotkey;
		}
		((TemplatedControl)this).OnAttachedToLogicalTree(e);
		if (Command != null)
		{
			Command.CanExecuteChanged += CanExecuteChanged;
			CanExecuteChanged(this, null);
		}
	}

	/// <inheritdoc />
	protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		if (HotKey != (KeyGesture)null)
		{
			_hotkey = HotKey;
			HotKey = null;
		}
		((TemplatedControl)this).OnDetachedFromLogicalTree(e);
		if (Command != null)
		{
			Command.CanExecuteChanged -= CanExecuteChanged;
		}
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((Control)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)CommandProperty)
		{
			ICommand oldValue = AvaloniaPropertyChangedExtensions.GetOldValue<ICommand>(change);
			ICommand newValue = AvaloniaPropertyChangedExtensions.GetNewValue<ICommand>(change);
			if (oldValue is FAXamlUICommand fAXamlUICommand)
			{
				if (Text == fAXamlUICommand.Label)
				{
					Text = null;
				}
				if (InputGesture == fAXamlUICommand.HotKey)
				{
					HotKey = null;
				}
			}
			if (newValue is FAXamlUICommand fAXamlUICommand2)
			{
				if (string.IsNullOrEmpty(Text))
				{
					Text = fAXamlUICommand2.Label;
				}
				if (IconSource == null)
				{
					IconSource = fAXamlUICommand2.IconSource;
				}
				if (InputGesture == (KeyGesture)null)
				{
					HotKey = fAXamlUICommand2.HotKey;
				}
			}
			if (((ILogical)this).IsAttachedToLogicalTree)
			{
				if (oldValue != null)
				{
					oldValue.CanExecuteChanged -= CanExecuteChanged;
				}
				if (newValue != null)
				{
					newValue.CanExecuteChanged += CanExecuteChanged;
				}
			}
			CanExecuteChanged(this, null);
		}
		else if (change.Property == (AvaloniaProperty)(object)CommandParameterProperty)
		{
			CanExecuteChanged(this, null);
		}
		else if (change.Property == (AvaloniaProperty)(object)InputGestureProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":hotkey", change.NewValue != null);
		}
		else if (change.Property == (AvaloniaProperty)(object)HotKeyProperty)
		{
			KeyGesture newValue2 = AvaloniaPropertyChangedExtensions.GetNewValue<KeyGesture>(change);
			InputGesture = newValue2;
		}
		else if (change.Property == (AvaloniaProperty)(object)IconSourceProperty)
		{
			TemplateSettings.Icon = FAIconHelpers.CreateFromUnknown(AvaloniaPropertyChangedExtensions.GetNewValue<FAIconSource>(change));
		}
	}

	/// <inheritdoc />
	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((InputElement)this).OnPointerPressed(e);
		PointerPoint currentPoint = ((PointerEventArgs)e).GetCurrentPoint((Visual)(object)this);
		PointerPointProperties properties = ((PointerPoint)(ref currentPoint)).Properties;
		if (((PointerPointProperties)(ref properties)).IsLeftButtonPressed)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":pressed", true);
		}
	}

	/// <inheritdoc />
	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		((Control)this).OnPointerReleased(e);
		if ((int)e.InitialPressMouseButton == 1)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":pressed", false);
		}
	}

	/// <summary>
	/// Raise <see cref="F:Avalonia.Controls.MenuItem.ClickEvent" /> and invke <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyoutItem.Command" /> if ti is set.
	/// </summary>
	protected virtual void OnClick()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		((Interactive)this).RaiseEvent(new RoutedEventArgs((RoutedEvent)(object)MenuItem.ClickEvent, (object)this));
		ICommand command = Command;
		if (command != null && command.CanExecute(CommandParameter))
		{
			Command.Execute(CommandParameter);
		}
	}

	internal void RaiseClick()
	{
		OnClick();
	}

	private void CanExecuteChanged(object sender, EventArgs e)
	{
		bool flag = Command == null || Command.CanExecute(CommandParameter);
		if (flag != _canExecute)
		{
			_canExecute = flag;
			((InputElement)this).UpdateIsEffectivelyEnabled();
		}
	}

	void ICommandSource.CanExecuteChanged(object sender, EventArgs e)
	{
		CanExecuteChanged(sender, e);
	}
}
