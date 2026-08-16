using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// An InfoBar is an inline notification for essential app-wide messages. 
/// The InfoBar will take up space in a layout and will not cover up other content 
/// or float on top of it. It supports rich content (including titles, messages, icons, and buttons) 
/// and can be configured to be user-dismissable or persistent.
/// </summary>
[PseudoClasses(new string[] { ":hidden", ":closehidden" })]
[PseudoClasses(new string[] { ":success", ":warning", ":error", ":informational" })]
[PseudoClasses(new string[] { ":icon", ":standardIcon" })]
[PseudoClasses(new string[] { ":foregroundset", ":noBannerContent" })]
[TemplatePart("CloseButton", typeof(Button))]
[TemplatePart("StandardIcon", typeof(FAFontIcon))]
public class FAInfoBar : ContentControl
{
	private Button _closeButton;

	private FAFontIcon _standardIcon;

	private bool _appliedTemplate;

	private bool _notifyOpen;

	private bool _isVisible;

	private FAInfoBarCloseReason _lastCloseReason;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAInfoBar.IsOpen" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsOpenProperty = AvaloniaProperty.Register<FAInfoBar, bool>("IsOpen", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAInfoBar.Title" /> property
	/// </summary>
	public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<FAInfoBar, string>("Title", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAInfoBar.Message" /> property
	/// </summary>
	public static readonly StyledProperty<string> MessageProperty = AvaloniaProperty.Register<FAInfoBar, string>("Message", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAInfoBar.Severity" /> property
	/// </summary>
	public static readonly StyledProperty<FAInfoBarSeverity> SeverityProperty = AvaloniaProperty.Register<FAInfoBar, FAInfoBarSeverity>("Severity", FAInfoBarSeverity.Informational, false, (BindingMode)1, (Func<FAInfoBarSeverity, bool>)null, (Func<AvaloniaObject, FAInfoBarSeverity, FAInfoBarSeverity>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAInfoBar.IconSource" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconSource> IconSourceProperty = FASettingsExpander.IconSourceProperty.AddOwner<FAIconSource>((StyledPropertyMetadata<FAIconSource>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAInfoBar.IsIconVisible" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsIconVisibleProperty = AvaloniaProperty.Register<FAInfoBar, bool>("IsIconVisible", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAInfoBar.IsClosable" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsClosableProperty = AvaloniaProperty.Register<FAInfoBar, bool>("IsClosable", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAInfoBar.CloseButtonCommand" /> property
	/// </summary>
	public static readonly StyledProperty<ICommand> CloseButtonCommandProperty = AvaloniaProperty.Register<FAInfoBar, ICommand>("CloseButtonCommand", (ICommand)null, false, (BindingMode)1, (Func<ICommand, bool>)null, (Func<AvaloniaObject, ICommand, ICommand>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAInfoBar.CloseButtonCommandParameter" /> property
	/// </summary>
	public static readonly StyledProperty<object> CloseButtonCommandParameterProperty = AvaloniaProperty.Register<FAInfoBar, object>("CloseButtonCommandParameter", (object)null, false, (BindingMode)1, (Func<object, bool>)null, (Func<AvaloniaObject, object, object>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAInfoBar.ActionButton" /> property
	/// </summary>
	public static readonly StyledProperty<Control> ActionButtonProperty = AvaloniaProperty.Register<FAInfoBar, Control>("ActionButton", (Control)null, false, (BindingMode)1, (Func<Control, bool>)null, (Func<AvaloniaObject, Control, Control>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAInfoBar.CloseButtonTheme" /> property
	/// </summary>
	public static readonly StyledProperty<ControlTheme> CloseButtonThemeProperty = AvaloniaProperty.Register<FAInfoBadge, ControlTheme>("CloseButtonTheme", (ControlTheme)null, false, (BindingMode)1, (Func<ControlTheme, bool>)null, (Func<AvaloniaObject, ControlTheme, ControlTheme>)null, false);

	private const string SR_InfoBarCloseButtonTooltip = "InfoBarCloseButtonTooltip";

	private const string SR_InfoBarCloseButtonName = "InfoBarCloseButtonName";

	private const string SR_InfoBarOpenedNotification = "InfoBarOpenedNotification";

	private const string SR_InfoBarClosedNotification = "InfoBarClosedNotification";

	private const string SR_InfoBarSeverityInformationalName = "InfoBarSeverityInformationalName";

	private const string SR_InfoBarSeveritySuccessName = "InfoBarSeveritySuccessName";

	private const string SR_InfoBarSeverityWarningName = "InfoBarSeverityWarningName";

	private const string SR_InfoBarSeverityErrorName = "InfoBarSeverityErrorName";

	private const string SR_InfoBarIconSeverityInformationalName = "InfoBarIconSeverityInformationalName";

	private const string SR_InfoBarIconSeveritySuccessName = "InfoBarIconSeveritySuccessName";

	private const string SR_InfoBarIconSeverityWarningName = "InfoBarIconSeverityWarningName";

	private const string SR_InfoBarIconSeverityErrorName = "InfoBarIconSeverityErrorName";

	private const string s_tpCloseButton = "CloseButton";

	private const string s_tpStandardIcon = "StandardIcon";

	private const string s_pcSuccess = ":success";

	private const string s_pcWarning = ":warning";

	private const string s_pcError = ":error";

	private const string s_pcInformational = ":informational";

	private const string s_pcStandardIcon = ":standardIcon";

	private const string s_pcCloseHidden = ":closehidden";

	private const string s_pcForegroundSet = ":foregroundset";

	private const string s_pcNoBannerContent = ":noBannerContent";

	/// <summary>
	/// Gets or sets a value that indicates whether the InfoBar is open.
	/// </summary>
	public bool IsOpen
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsOpenProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsOpenProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the title of the InfoBar.
	/// </summary>
	public string Title
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(TitleProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(TitleProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the message of the InfoBar.
	/// </summary>
	public string Message
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(MessageProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(MessageProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the type of the InfoBar to apply consistent status color, icon, 
	/// and assistive technology settings dependent on the criticality of the notification.
	/// </summary>
	public FAInfoBarSeverity Severity
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAInfoBarSeverity>(SeverityProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAInfoBarSeverity>(SeverityProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the graphic content to appear alongside the title and message in the InfoBar.
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
	/// Gets or sets a value that indicates whether the icon is visible in the InfoBar.
	/// </summary>
	public bool IsIconVisible
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsIconVisibleProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsIconVisibleProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the user can close the InfoBar.
	/// </summary>
	public bool IsClosable
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsClosableProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsClosableProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the command to invoke when the close button is clicked in the InfoBar.
	/// </summary>
	public ICommand CloseButtonCommand
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<ICommand>(CloseButtonCommandProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<ICommand>(CloseButtonCommandProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the parameter to pass to the command for the close button in the InfoBar.
	/// </summary>
	public object CloseButtonCommandParameter
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<object>(CloseButtonCommandParameterProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<object>(CloseButtonCommandParameterProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the action button of the InfoBar.
	/// </summary>
	public Control ActionButton
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<Control>(ActionButtonProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<Control>(ActionButtonProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the <see cref="T:Avalonia.Styling.ControlTheme" /> for the InfoBar's CloseButton
	/// </summary>
	public ControlTheme CloseButtonTheme
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<ControlTheme>(CloseButtonThemeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<ControlTheme>(CloseButtonThemeProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Occurs after the close button is clicked in the InfoBar.
	/// </summary>
	public event TypedEventHandler<FAInfoBar, EventArgs> CloseButtonClick;

	/// <summary>
	/// Occurs just before the InfoBar begins to close.
	/// </summary>
	public event TypedEventHandler<FAInfoBar, FAInfoBarClosingEventArgs> Closing;

	/// <summary>
	/// Occurs after the InfoBar is closed.
	/// </summary>
	public event TypedEventHandler<FAInfoBar, FAInfoBarClosedEventArgs> Closed;

	/// <summary>
	/// Occurs after the InfoBar is opened
	/// </summary>
	public event TypedEventHandler<FAInfoBar, FAInfoBarOpenedEventArgs> Opened;

	/// <inheritdoc />
	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		_appliedTemplate = false;
		Button closeButton = _closeButton;
		if (closeButton != null)
		{
			closeButton.Click -= OnCloseButtonClick;
		}
		((TemplatedControl)this).OnApplyTemplate(e);
		_closeButton = NameScopeExtensions.Find<Button>(e.NameScope, "CloseButton");
		if (_closeButton != null)
		{
			_closeButton.Click += OnCloseButtonClick;
			ToolTip.SetTip((Control)(object)_closeButton, (object)FALocalizationHelper.Instance.GetLocalizedStringResource("InfoBarCloseButtonTooltip"));
			if (AutomationProperties.GetName((StyledElement)(object)_closeButton) == null)
			{
				string localizedStringResource = FALocalizationHelper.Instance.GetLocalizedStringResource("InfoBarCloseButtonName");
				AutomationProperties.SetName((StyledElement)(object)_closeButton, localizedStringResource);
			}
		}
		FAFontIcon fAFontIcon = NameScopeExtensions.Find<FAFontIcon>(e.NameScope, "StandardIcon");
		if (fAFontIcon != null)
		{
			_standardIcon = fAFontIcon;
			AutomationProperties.SetName((StyledElement)(object)fAFontIcon, FALocalizationHelper.Instance.GetLocalizedStringResource(GetIconSeverityLevelResourceName(Severity)));
		}
		_appliedTemplate = true;
		UpdateVisibility(_notifyOpen);
		_notifyOpen = false;
		UpdateSeverity();
		UpdateIcon();
		UpdateIconVisibility();
		UpdateCloseButton();
		UpdateForeground();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((ContentControl)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)IsOpenProperty)
		{
			if (AvaloniaPropertyChangedExtensions.GetNewValue<bool>(change))
			{
				_lastCloseReason = FAInfoBarCloseReason.Programmatic;
				UpdateVisibility();
				RaiseOpenedEvent();
			}
			else
			{
				RaiseClosingEvent();
			}
		}
		else if (change.Property == (AvaloniaProperty)(object)SeverityProperty)
		{
			UpdateSeverity();
		}
		else if (change.Property == (AvaloniaProperty)(object)IconSourceProperty)
		{
			UpdateIcon();
			UpdateIconVisibility();
		}
		else if (change.Property == (AvaloniaProperty)(object)IsIconVisibleProperty)
		{
			UpdateIconVisibility();
		}
		else if (change.Property == (AvaloniaProperty)(object)IsClosableProperty)
		{
			UpdateCloseButton();
		}
		else if (change.Property == (AvaloniaProperty)(object)TextElement.ForegroundProperty)
		{
			UpdateForeground();
		}
		else if (change.Property == (AvaloniaProperty)(object)TitleProperty)
		{
			UpdateContentPosition();
		}
		else if (change.Property == (AvaloniaProperty)(object)MessageProperty)
		{
			UpdateContentPosition();
		}
		else if (change.Property == (AvaloniaProperty)(object)ActionButtonProperty)
		{
			UpdateContentPosition();
		}
	}

	protected override bool RegisterContentPresenter(ContentPresenter presenter)
	{
		if (((StyledElement)presenter).Name == "ContentPresenter")
		{
			return true;
		}
		return ((ContentControl)this).RegisterContentPresenter(presenter);
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return (AutomationPeer)(object)new FAInfoBarAutomationPeer((Control)(object)this);
	}

	private void OnCloseButtonClick(object sender, RoutedEventArgs e)
	{
		CloseButtonClick?.Invoke(this, EventArgs.Empty);
		_lastCloseReason = FAInfoBarCloseReason.CloseButton;
		IsOpen = false;
	}

	private void RaiseClosingEvent()
	{
		FAInfoBarClosingEventArgs e = new FAInfoBarClosingEventArgs(_lastCloseReason);
		Closing?.Invoke(this, e);
		if (!e.Cancel)
		{
			UpdateVisibility();
			RaiseClosedEvent();
		}
		else
		{
			IsOpen = true;
		}
	}

	private void RaiseClosedEvent()
	{
		FAInfoBarClosedEventArgs args = new FAInfoBarClosedEventArgs(_lastCloseReason);
		Closed?.Invoke(this, args);
	}

	private void RaiseOpenedEvent()
	{
		Opened?.Invoke(this, new FAInfoBarOpenedEventArgs());
	}

	private void UpdateVisibility(bool notify = true, bool force = true)
	{
		if (!_appliedTemplate)
		{
			_notifyOpen = true;
			return;
		}
		AutomationPeer val = ControlAutomationPeer.FromElement((Control)(object)this);
		if (!force && IsOpen == _isVisible)
		{
			return;
		}
		if (IsOpen)
		{
			_isVisible = true;
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":hidden", false);
			if (notify && val is FAInfoBarAutomationPeer fAInfoBarAutomationPeer)
			{
				FALocalizationHelper instance = FALocalizationHelper.Instance;
				string displayString = $"{instance.GetLocalizedStringResource("InfoBarOpenedNotification")}{instance.GetLocalizedStringResource(GetIconSeverityLevelResourceName(Severity))}{Title} {Message}";
				fAInfoBarAutomationPeer.RaiseOpenedEvent(Severity, displayString);
			}
			AutomationProperties.SetAccessibilityView((StyledElement)(object)this, (AccessibilityView)2);
		}
		else
		{
			if (notify && val is FAInfoBarAutomationPeer fAInfoBarAutomationPeer2)
			{
				string localizedStringResource = FALocalizationHelper.Instance.GetLocalizedStringResource("InfoBarClosedNotification");
				fAInfoBarAutomationPeer2.RaiseClosedEvent(Severity, localizedStringResource);
			}
			_isVisible = false;
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":hidden", true);
			AutomationProperties.SetAccessibilityView((StyledElement)(object)this, (AccessibilityView)1);
		}
	}

	private void UpdateSeverity()
	{
		if (_appliedTemplate)
		{
			FAInfoBarSeverity severity = Severity;
			switch (severity)
			{
			case FAInfoBarSeverity.Success:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":success", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":warning", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":error", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":informational", false);
				break;
			case FAInfoBarSeverity.Warning:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":success", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":warning", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":error", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":informational", false);
				break;
			case FAInfoBarSeverity.Error:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":success", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":warning", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":error", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":informational", false);
				break;
			default:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":success", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":warning", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":error", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":informational", true);
				break;
			}
			if (_standardIcon != null)
			{
				AutomationProperties.SetName((StyledElement)(object)_standardIcon, FALocalizationHelper.Instance.GetLocalizedStringResource(GetIconSeverityLevelResourceName(severity)));
			}
		}
	}

	private void UpdateIcon()
	{
	}

	private void UpdateIconVisibility()
	{
		if (!IsIconVisible)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":icon", false);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":standardIcon", false);
		}
		else
		{
			bool flag = IconSource != null;
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":icon", flag);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":standardIcon", !flag);
		}
	}

	private void UpdateCloseButton()
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":closehidden", !IsClosable);
	}

	private void UpdateForeground()
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":foregroundset", ((AvaloniaObject)this).GetValue<IBrush>((StyledProperty<IBrush>)(object)TextElement.ForegroundProperty) != AvaloniaProperty.UnsetValue);
	}

	private void UpdateContentPosition()
	{
		string title = Title;
		string message = Message;
		Control actionButton = ActionButton;
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":noBannerContent", string.IsNullOrEmpty(title) && string.IsNullOrEmpty(message) && actionButton == null);
	}

	private static string GetSeverityLevelResourceName(FAInfoBarSeverity severity)
	{
		return severity switch
		{
			FAInfoBarSeverity.Success => "InfoBarSeveritySuccessName", 
			FAInfoBarSeverity.Warning => "InfoBarSeverityWarningName", 
			FAInfoBarSeverity.Error => "InfoBarSeverityErrorName", 
			_ => "InfoBarSeverityInformationalName", 
		};
	}

	private static string GetIconSeverityLevelResourceName(FAInfoBarSeverity severity)
	{
		return severity switch
		{
			FAInfoBarSeverity.Success => "InfoBarIconSeveritySuccessName", 
			FAInfoBarSeverity.Warning => "InfoBarIconSeverityWarningName", 
			FAInfoBarSeverity.Error => "InfoBarIconSeverityErrorName", 
			_ => "InfoBarIconSeverityInformationalName", 
		};
	}
}
