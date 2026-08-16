using System;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a control that can be used to display and edit numbers.
/// </summary>
[PseudoClasses(new string[] { ":spinvisible", ":spinpopup", ":spincollapsed" })]
[PseudoClasses(new string[] { ":updisabled", ":downdisabled" })]
[PseudoClasses(new string[] { ":header" })]
[TemplatePart("DownSpinButton", typeof(RepeatButton))]
[TemplatePart("PopupDownSpinButton", typeof(RepeatButton))]
[TemplatePart("UpSpinButton", typeof(RepeatButton))]
[TemplatePart("PopupUpSpinButton", typeof(RepeatButton))]
[TemplatePart("InputBox", typeof(TextBox))]
[TemplatePart("UpDownPopup", typeof(Popup))]
public class FANumberBox : TemplatedControl
{
	private RepeatButton _spinDown;

	private RepeatButton _spinUp;

	private TextBox _textBox;

	private Popup _popup;

	private RepeatButton _popupUpButton;

	private RepeatButton _popupDownButton;

	private bool _textUpdating;

	private bool _valueUpdating;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.AcceptsExpression" /> property
	/// </summary>
	public static readonly StyledProperty<bool> AcceptsExpressionProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.Description" /> property
	/// </summary>
	public static readonly StyledProperty<string> DescriptionProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.Header" /> property
	/// </summary>
	public static readonly StyledProperty<object> HeaderProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.HeaderTemplate" /> property
	/// </summary>
	public static readonly StyledProperty<IDataTemplate> HeaderTemplateProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.IsWrapEnabled" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsWrapEnabledProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.LargeChange" /> property
	/// </summary>
	public static readonly StyledProperty<double> LargeChangeProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.Minimum" /> property
	/// </summary>
	public static readonly StyledProperty<double> MinimumProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.Maximum" /> property
	/// </summary>
	public static readonly StyledProperty<double> MaximumProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.PlaceholderText" /> property
	/// </summary>
	public static readonly StyledProperty<string> PlaceholderTextProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.PlaceholderForeground" /> property
	/// </summary>
	public static readonly StyledProperty<IBrush> PlaceholderForegroundProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.SelectionHighlightColor" /> property
	/// </summary>
	public static readonly StyledProperty<IBrush> SelectionHighlightColorProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.SmallChange" /> property
	/// </summary>
	public static readonly StyledProperty<double> SmallChangeProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.SpinButtonPlacementMode" /> property
	/// </summary>
	public static readonly StyledProperty<FANumberBoxSpinButtonPlacementMode> SpinButtonPlacementModeProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.Text" /> property
	/// </summary>
	public static readonly StyledProperty<string> TextProperty;

	/// <summary>
	/// Defines the <see cref="T:FluentAvalonia.UI.Controls.FANumberBoxValidationMode" /> property
	/// </summary>
	public static readonly StyledProperty<FANumberBoxValidationMode> ValidationModeProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.Value" /> property
	/// </summary>
	public static readonly StyledProperty<double> ValueProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.TextAlignment" /> property
	/// </summary>
	public static readonly StyledProperty<TextAlignment> TextAlignmentProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.SimpleNumberFormat" /> property
	/// </summary>
	public static readonly StyledProperty<string> SimpleNumberFormatProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.InnerLeftContent" /> property
	/// </summary>
	public static readonly StyledProperty<object> InnerLeftContentProperty;

	private const string s_tpDownSpinButton = "DownSpinButton";

	private const string s_tpPopupDownSpinButton = "PopupDownSpinButton";

	private const string s_tpUpSpinButton = "UpSpinButton";

	private const string s_tpPopupUpSpinButton = "PopupUpSpinButton";

	private const string s_tpInputBox = "InputBox";

	private const string s_tpUpDownPopup = "UpDownPopup";

	private const string s_pcSpinVisible = ":spinvisible";

	private const string s_pcSpinPopup = ":spinpopup";

	private const string s_pcSpinCollapsed = ":spincollapsed";

	private const string s_pcUpDisabled = ":updisabled";

	private const string s_pcDownDisabled = ":downdisabled";

	private const string SR_NumberBoxDownSpinButtonName = "NumberBoxDownSpinButtonName";

	private const string SR_NumberBoxUpSpinButtonName = "NumberBoxUpSpinButtonName";

	private const string SR_NumberBoxMaximumValueStatus = "NumberBoxMaximumValueStatus";

	private const string SR_NumberBoxMinimumValueStatus = "NumberBoxMinimumValueStatus";

	/// <summary>
	/// Toggles whether the control will accept and evaluate a basic formulaic expression entered as input.
	/// </summary>
	public bool AcceptsExpression
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(AcceptsExpressionProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(AcceptsExpressionProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets content that is shown below the control. The content should provide guidance 
	/// about the input expected by the control.
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
	/// Gets or sets the content for the control's header.
	/// </summary>
	public object Header
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<object>(HeaderProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<object>(HeaderProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the DataTemplate used to display the content of the control's header.
	/// </summary>
	public IDataTemplate HeaderTemplate
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IDataTemplate>(HeaderTemplateProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IDataTemplate>(HeaderTemplateProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Toggles whether line breaking occurs if a line of text extends beyond the available 
	/// width of the control.
	/// </summary>
	public bool IsWrapEnabled
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsWrapEnabledProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsWrapEnabledProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the value that is added to or subtracted from Value when a large change is made,
	/// such as with the PageUP and PageDown keys.
	/// </summary>
	public double LargeChange
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(LargeChangeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(LargeChangeProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the numerical minimum for Value.
	/// </summary>
	public double Minimum
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(MinimumProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(MinimumProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the numerical maximum for Value.
	/// </summary>
	public double Maximum
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(MaximumProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(MaximumProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// A function for customizing the format of the NumberBox Value Text.
	/// </summary>
	/// <remarks>
	/// .NET doesn't have all of the formatting stuff from WinUI/WinRT, thus doing fancy things
	/// requires a bit more manual work, and I'm not about to attempt to replicate the NumberFormatters :D
	/// NOTE: This cannot be used if <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.SimpleNumberFormat" /> is in use
	/// </remarks>
	public Func<double, string> NumberFormatter { get; set; }

	/// <summary>
	/// Use this for simple number formatting using normal .net formatting. Resulting string must still
	/// be numeric in value, no special characters, as they are not removed when attempting to convert
	/// text to value
	/// </summary>
	/// <remarks>
	/// This property cannot be used if <see cref="P:FluentAvalonia.UI.Controls.FANumberBox.NumberFormatter" /> is also in use
	/// </remarks>
	public string SimpleNumberFormat
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(SimpleNumberFormatProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(SimpleNumberFormatProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the text that is displayed in the control until the value is changed by a 
	/// user action or some other operation.
	/// </summary>
	public string PlaceholderText
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(PlaceholderTextProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(PlaceholderTextProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the foreground brush for the placeholder text
	/// </summary>
	public IBrush PlaceholderForeground
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IBrush>(PlaceholderForegroundProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IBrush>(PlaceholderForegroundProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the brush used to highlight the selected text.
	/// </summary>
	public IBrush SelectionHighlightColor
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IBrush>(SelectionHighlightColorProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IBrush>(SelectionHighlightColorProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the value that is added to or subtracted from Value when a small 
	/// change is made, such as with an arrow key or scrolling.
	/// </summary>
	public double SmallChange
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(SmallChangeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(SmallChangeProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates the placement of buttons used to increment 
	/// or decrement the Value property.
	/// </summary>
	public FANumberBoxSpinButtonPlacementMode SpinButtonPlacementMode
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FANumberBoxSpinButtonPlacementMode>(SpinButtonPlacementModeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FANumberBoxSpinButtonPlacementMode>(SpinButtonPlacementModeProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the string type representation of the Value property.
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
	/// Gets or sets the input validation behavior to invoke when invalid input is entered.
	/// </summary>
	public FANumberBoxValidationMode ValidationMode
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FANumberBoxValidationMode>(ValidationModeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FANumberBoxValidationMode>(ValidationModeProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the numeric value of a NumberBox.
	/// </summary>
	public double Value
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(ValueProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(ValueProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the TextAlignment of the text in the NumberBox
	/// </summary>
	public TextAlignment TextAlignment
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<TextAlignment>(TextAlignmentProperty);
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<TextAlignment>(TextAlignmentProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the inner left content of the TextBox within the NumberBox
	/// </summary>
	public object InnerLeftContent
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<object>(InnerLeftContentProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<object>(InnerLeftContentProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Occurs after the user triggers evaluation of new input by pressing the Enter key, 
	/// clicking a spin button, or by changing focus.
	/// </summary>
	public event TypedEventHandler<FANumberBox, FANumberBoxValueChangedEventArgs> ValueChanged;

	public FANumberBox()
	{
		((Interactive)this).AddHandler<PointerPressedEventArgs>(InputElement.PointerPressedEvent, (EventHandler<PointerPressedEventArgs>)OnPointerPressedPreview, (RoutingStrategies)2, false);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		UnhookEvents();
		((TemplatedControl)this).OnApplyTemplate(e);
		string localizedStringResource = FALocalizationHelper.Instance.GetLocalizedStringResource("NumberBoxDownSpinButtonName");
		string localizedStringResource2 = FALocalizationHelper.Instance.GetLocalizedStringResource("NumberBoxUpSpinButtonName");
		_spinDown = NameScopeExtensions.Find<RepeatButton>(e.NameScope, "DownSpinButton");
		_popupDownButton = NameScopeExtensions.Find<RepeatButton>(e.NameScope, "PopupDownSpinButton");
		if (_spinDown != null)
		{
			((Button)_spinDown).Click += OnSpinDownClick;
			if (AutomationProperties.GetName((StyledElement)(object)_spinDown) == null)
			{
				AutomationProperties.SetName((StyledElement)(object)_spinDown, localizedStringResource);
			}
		}
		RepeatButton popupDownButton = _popupDownButton;
		if (popupDownButton != null)
		{
			((Button)popupDownButton).Click += OnSpinDownClick;
		}
		_spinUp = NameScopeExtensions.Find<RepeatButton>(e.NameScope, "UpSpinButton");
		_popupUpButton = NameScopeExtensions.Find<RepeatButton>(e.NameScope, "PopupUpSpinButton");
		if (_spinUp != null)
		{
			((Button)_spinUp).Click += OnSpinUpClick;
			if (AutomationProperties.GetName((StyledElement)(object)_spinUp) == null)
			{
				AutomationProperties.SetName((StyledElement)(object)_spinUp, localizedStringResource2);
			}
		}
		RepeatButton popupUpButton = _popupUpButton;
		if (popupUpButton != null)
		{
			((Button)popupUpButton).Click += OnSpinUpClick;
		}
		_textBox = NameScopeExtensions.Find<TextBox>(e.NameScope, "InputBox");
		if (_textBox != null)
		{
			((Interactive)_textBox).AddHandler<KeyEventArgs>(InputElement.KeyDownEvent, (EventHandler<KeyEventArgs>)OnNumberBoxKeyDown, (RoutingStrategies)2, false);
			((InputElement)_textBox).KeyUp += OnNumberBoxKeyUp;
			((Control)_textBox).Loaded += OnTextBoxLoaded;
		}
		_popup = NameScopeExtensions.Find<Popup>(e.NameScope, "UpDownPopup");
		Popup popup = _popup;
		if (popup != null)
		{
			popup.OverlayInputPassThroughElement = (IInputElement)(object)this;
		}
		UpdateSpinButtonPlacement();
		UpdateSpinButtonEnabled();
		ReevaluateForwardedUIAProperties();
		if (double.IsNaN(Value) && !string.IsNullOrEmpty(Text))
		{
			UpdateValueToText();
		}
		else
		{
			UpdateTextToValue();
		}
	}

	protected override void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception error)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		((Control)this).UpdateDataValidation(property, state, error);
		if (property == (AvaloniaProperty)(object)ValueProperty)
		{
			DataValidationErrors.SetError((Control)(object)this, error);
		}
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((Control)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)ValueProperty)
		{
			OnValueChanged(AvaloniaPropertyChangedExtensions.GetOldValue<double>(change), AvaloniaPropertyChangedExtensions.GetNewValue<double>(change));
		}
		else if (change.Property == (AvaloniaProperty)(object)TextProperty)
		{
			if (!_textUpdating)
			{
				UpdateValueToText();
			}
		}
		else if (change.Property == (AvaloniaProperty)(object)IsWrapEnabledProperty)
		{
			UpdateSpinButtonEnabled();
		}
		else if (change.Property == (AvaloniaProperty)(object)SpinButtonPlacementModeProperty)
		{
			UpdateSpinButtonPlacement();
		}
		else if (change.Property == (AvaloniaProperty)(object)HeaderProperty || change.Property == (AvaloniaProperty)(object)HeaderTemplateProperty)
		{
			UpdateHeaderPresenterState();
		}
		else if (change.Property == (AvaloniaProperty)(object)LargeChangeProperty || change.Property == (AvaloniaProperty)(object)SmallChangeProperty)
		{
			UpdateSpinButtonEnabled();
		}
		else if (change.Property == (AvaloniaProperty)(object)ValidationModeProperty)
		{
			ValidateInput();
			UpdateSpinButtonEnabled();
		}
		else if (change.Property == (AvaloniaProperty)(object)SimpleNumberFormatProperty)
		{
			if (NumberFormatter != null)
			{
				throw new InvalidOperationException("NumberFormatter must be null");
			}
			UpdateTextToValue();
		}
		else if (change.Property == (AvaloniaProperty)(object)MinimumProperty || change.Property == (AvaloniaProperty)(object)MaximumProperty)
		{
			UpdateSpinButtonEnabled();
			ReevaluateForwardedUIAProperties();
		}
		else if (change.Property == (AvaloniaProperty)(object)AutomationProperties.NameProperty)
		{
			OnAutomationPropertiesNamePropertyChanged();
		}
		else if (change.Property == (AvaloniaProperty)(object)AutomationProperties.LabeledByProperty)
		{
			OnAutomationPropertiesLabeledByPropertyChanged();
		}
	}

	protected override void OnGotFocus(FocusChangedEventArgs e)
	{
		((Control)this).OnGotFocus(e);
		TextBox textBox = _textBox;
		if (textBox != null)
		{
			textBox.SelectAll();
		}
		if (SpinButtonPlacementMode == FANumberBoxSpinButtonPlacementMode.Compact)
		{
			Popup popup = _popup;
			if (popup != null)
			{
				popup.IsOpen = true;
			}
		}
	}

	protected override void OnLostFocus(FocusChangedEventArgs e)
	{
		((Control)this).OnLostFocus(e);
		ValidateInput();
		Popup popup = _popup;
		if (popup != null)
		{
			popup.IsOpen = false;
		}
	}

	protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		((InputElement)this).OnPointerWheelChanged(e);
		if (_textBox != null && ((InputElement)this).IsKeyboardFocusWithin)
		{
			Vector delta = e.Delta;
			if (((Vector)(ref delta)).Y > 0.0)
			{
				StepValue(SmallChange);
			}
			else
			{
				StepValue(0.0 - SmallChange);
			}
			((RoutedEventArgs)e).Handled = true;
		}
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return (AutomationPeer)(object)new FANumberBoxAutomationPeer((Control)(object)this);
	}

	private void OnTextBoxLoaded(object sender, RoutedEventArgs args)
	{
		UpdateSpinButtonPlacement();
	}

	private void OnPointerPressedPreview(object sender, PointerPressedEventArgs args)
	{
		if (SpinButtonPlacementMode == FANumberBoxSpinButtonPlacementMode.Compact && _popup != null && !_popup.IsOpen && ((InputElement)this).IsKeyboardFocusWithin)
		{
			_popup.IsOpen = true;
		}
	}

	private void OnValueChanged(double oldValue, double newValue)
	{
		if (_valueUpdating)
		{
			return;
		}
		try
		{
			_valueUpdating = true;
			if (newValue != oldValue && (!double.IsNaN(newValue) || !double.IsNaN(oldValue)))
			{
				FANumberBoxValueChangedEventArgs args = new FANumberBoxValueChangedEventArgs(oldValue, newValue);
				ValueChanged?.Invoke(this, args);
				(ControlAutomationPeer.FromElement((Control)(object)this) as FANumberBoxAutomationPeer)?.RaiseValueChangedEvent(oldValue, newValue);
			}
			UpdateTextToValue();
			UpdateSpinButtonEnabled();
		}
		finally
		{
			_valueUpdating = false;
		}
	}

	private void OnAutomationPropertiesNamePropertyChanged()
	{
		ReevaluateForwardedUIAProperties();
	}

	private void OnAutomationPropertiesLabeledByPropertyChanged()
	{
		ReevaluateForwardedUIAProperties();
	}

	private void ReevaluateForwardedUIAProperties()
	{
		if (_textBox != null)
		{
			string name = AutomationProperties.GetName((StyledElement)(object)this);
			string text = ((Minimum == double.MaxValue) ? string.Empty : $" {FALocalizationHelper.Instance.GetLocalizedStringResource("NumberBoxMinimumValueStatus")} {Minimum}");
			string text2 = ((Maximum == double.MaxValue) ? string.Empty : $" {FALocalizationHelper.Instance.GetLocalizedStringResource("NumberBoxMaximumValueStatus")} {Maximum}");
			if (!string.IsNullOrEmpty(name))
			{
				AutomationProperties.SetName((StyledElement)(object)_textBox, name + text + text2);
			}
			else if (Header is string text3)
			{
				AutomationProperties.SetName((StyledElement)(object)_textBox, text3 + text + text2);
			}
			Control labeledBy = AutomationProperties.GetLabeledBy((StyledElement)(object)this);
			if (labeledBy != null)
			{
				AutomationProperties.SetLabeledBy((StyledElement)(object)_textBox, labeledBy);
			}
		}
	}

	private void UpdateValueToText()
	{
		if (_textBox != null)
		{
			_textBox.Text = Text;
			ValidateInput();
		}
	}

	private void ValidateInput()
	{
		if (_textBox == null)
		{
			return;
		}
		string text = _textBox.Text?.Trim();
		if (string.IsNullOrEmpty(text))
		{
			Value = double.NaN;
			return;
		}
		double? num = (AcceptsExpression ? NumberBoxParser.Compute(text) : ParseDouble(text));
		if (!num.HasValue)
		{
			if (ValidationMode == FANumberBoxValidationMode.InvalidInputOverwritten)
			{
				UpdateTextToValue();
			}
		}
		else if (num.Value == Value)
		{
			UpdateTextToValue();
		}
		else
		{
			Value = num.Value;
		}
	}

	private double? ParseDouble(string txt)
	{
		if (double.TryParse(txt, NumberStyles.Any, CultureInfo.CurrentCulture, out var result))
		{
			return result;
		}
		return null;
	}

	private void OnSpinDownClick(object sender, RoutedEventArgs e)
	{
		StepValue(0.0 - SmallChange);
	}

	private void OnSpinUpClick(object sender, RoutedEventArgs e)
	{
		StepValue(SmallChange);
	}

	private void OnNumberBoxKeyDown(object sender, KeyEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		Key key = e.Key;
		if ((int)key <= 20)
		{
			if ((int)key != 19)
			{
				if ((int)key == 20)
				{
					StepValue(0.0 - LargeChange);
					((RoutedEventArgs)e).Handled = true;
				}
			}
			else
			{
				StepValue(LargeChange);
				((RoutedEventArgs)e).Handled = true;
			}
		}
		else if ((int)key != 24)
		{
			if ((int)key == 26)
			{
				StepValue(0.0 - SmallChange);
				((RoutedEventArgs)e).Handled = true;
			}
		}
		else
		{
			StepValue(SmallChange);
			((RoutedEventArgs)e).Handled = true;
		}
	}

	private void OnNumberBoxKeyUp(object sender, KeyEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		Key key = e.Key;
		if ((int)key != 6)
		{
			if ((int)key == 13)
			{
				UpdateTextToValue();
				((RoutedEventArgs)e).Handled = true;
			}
		}
		else
		{
			ValidateInput();
			((RoutedEventArgs)e).Handled = true;
		}
	}

	public void StepValue(double change)
	{
		ValidateInput();
		double value = Value;
		double maximum = Maximum;
		double minimum = Minimum;
		if (double.IsNaN(value))
		{
			return;
		}
		value += change;
		if (IsWrapEnabled)
		{
			if (value > maximum)
			{
				value = minimum;
			}
			else if (value < minimum)
			{
				value = maximum;
			}
		}
		Value = value;
		MoveCaretToTextEnd();
	}

	private void UpdateTextToValue()
	{
		if (_textBox == null)
		{
			return;
		}
		string text = "";
		double value = Value;
		if (!double.IsNaN(value))
		{
			double arg = Math.Round(value, 12);
			text = ((SimpleNumberFormat != null) ? arg.ToString(SimpleNumberFormat) : ((NumberFormatter == null) ? arg.ToString() : NumberFormatter(arg)));
		}
		_textBox.Text = text;
		try
		{
			_textUpdating = true;
			Text = text;
		}
		finally
		{
			_textUpdating = false;
			if (((InputElement)this).IsKeyboardFocusWithin)
			{
				MoveCaretToTextEnd();
			}
		}
	}

	private void UpdateSpinButtonPlacement()
	{
		switch (SpinButtonPlacementMode)
		{
		case FANumberBoxSpinButtonPlacementMode.Inline:
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":spinvisible", true);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":spinpopup", false);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":spincollapsed", false);
			if (_textBox != null)
			{
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_textBox).Classes, ":spinvisible", true);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_textBox).Classes, ":spinpopup", false);
			}
			break;
		case FANumberBoxSpinButtonPlacementMode.Compact:
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":spinvisible", false);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":spinpopup", true);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":spincollapsed", false);
			if (_textBox != null)
			{
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_textBox).Classes, ":spinvisible", false);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_textBox).Classes, ":spinpopup", true);
			}
			break;
		default:
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":spinvisible", false);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":spinpopup", false);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":spincollapsed", true);
			if (_textBox != null)
			{
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_textBox).Classes, ":spinvisible", false);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_textBox).Classes, ":spinpopup", false);
			}
			break;
		}
	}

	private void UpdateSpinButtonEnabled()
	{
		bool flag = false;
		bool flag2 = false;
		double value = Value;
		if (!double.IsNaN(value))
		{
			if (IsWrapEnabled || ValidationMode != FANumberBoxValidationMode.InvalidInputOverwritten)
			{
				flag = true;
				flag2 = true;
			}
			else
			{
				if (value < Maximum)
				{
					flag = true;
				}
				if (value > Minimum)
				{
					flag2 = true;
				}
			}
		}
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":updisabled", !flag);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":downdisabled", !flag2);
	}

	private void UpdateHeaderPresenterState()
	{
		bool flag = false;
		if (Header != null)
		{
			if (Header is string value)
			{
				if (!string.IsNullOrEmpty(value))
				{
					flag = true;
				}
			}
			else
			{
				flag = true;
			}
		}
		if (HeaderTemplate != null)
		{
			flag = true;
		}
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":header", flag);
	}

	private void MoveCaretToTextEnd()
	{
		TextBox textBox = _textBox;
		if (textBox != null)
		{
			TextBox textBox2 = _textBox;
			int num = (_textBox.CaretIndex = _textBox.Text.Length);
			int selectionStart = (textBox2.SelectionEnd = num);
			textBox.SelectionStart = selectionStart;
		}
	}

	private void CoerceValueIfNeeded(double min, double max)
	{
		double value = Value;
		if (!double.IsNaN(value))
		{
			if (value < min)
			{
				Value = min;
			}
			else if (value > max)
			{
				Value = max;
			}
		}
	}

	private double CoerceValueToRange(double val)
	{
		double maximum = Maximum;
		double minimum = Minimum;
		if (!double.IsNaN(val) && (val > maximum || val < minimum) && ValidationMode == FANumberBoxValidationMode.InvalidInputOverwritten)
		{
			if (val > maximum)
			{
				return maximum;
			}
			if (val < minimum)
			{
				return minimum;
			}
		}
		return val;
	}

	private void UnhookEvents()
	{
		RepeatButton spinDown = _spinDown;
		if (spinDown != null)
		{
			((Button)spinDown).Click -= OnSpinDownClick;
		}
		RepeatButton spinUp = _spinUp;
		if (spinUp != null)
		{
			((Button)spinUp).Click -= OnSpinUpClick;
		}
		RepeatButton popupDownButton = _popupDownButton;
		if (popupDownButton != null)
		{
			((Button)popupDownButton).Click -= OnSpinDownClick;
		}
		RepeatButton popupUpButton = _popupUpButton;
		if (popupUpButton != null)
		{
			((Button)popupUpButton).Click -= OnSpinUpClick;
		}
		TextBox textBox = _textBox;
		if (textBox != null)
		{
			((Interactive)textBox).RemoveHandler<KeyEventArgs>(InputElement.KeyDownEvent, (EventHandler<KeyEventArgs>)OnNumberBoxKeyDown);
		}
		TextBox textBox2 = _textBox;
		if (textBox2 != null)
		{
			((InputElement)textBox2).KeyUp -= OnNumberBoxKeyUp;
		}
	}

	static FANumberBox()
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		AcceptsExpressionProperty = AvaloniaProperty.Register<FANumberBox, bool>("AcceptsExpression", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
		DescriptionProperty = AvaloniaProperty.Register<FANumberBox, string>("Description", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);
		HeaderProperty = HeaderedContentControl.HeaderProperty.AddOwner<FANumberBox>((StyledPropertyMetadata<object>)null);
		HeaderTemplateProperty = HeaderedContentControl.HeaderTemplateProperty.AddOwner<FANumberBox>((StyledPropertyMetadata<IDataTemplate>)null);
		IsWrapEnabledProperty = AvaloniaProperty.Register<FANumberBox, bool>("IsWrapEnabled", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
		LargeChangeProperty = RangeBase.LargeChangeProperty.AddOwner<FANumberBox>((StyledPropertyMetadata<double>)null);
		MinimumProperty = RangeBase.MinimumProperty.AddOwner<FANumberBox>(new StyledPropertyMetadata<double>(Optional<double>.op_Implicit(double.MinValue), (BindingMode)0, (Func<AvaloniaObject, double, double>)delegate(AvaloniaObject ao, double d1)
		{
			FANumberBox obj = ao as FANumberBox;
			double maximum = obj.Maximum;
			if (d1 > maximum)
			{
				d1 = maximum;
			}
			obj.CoerceValueIfNeeded(d1, maximum);
			return d1;
		}, false));
		MaximumProperty = RangeBase.MaximumProperty.AddOwner<FANumberBox>(new StyledPropertyMetadata<double>(Optional<double>.op_Implicit(double.MaxValue), (BindingMode)0, (Func<AvaloniaObject, double, double>)delegate(AvaloniaObject ao, double d1)
		{
			FANumberBox obj = ao as FANumberBox;
			double minimum = obj.Minimum;
			if (d1 < minimum)
			{
				d1 = minimum;
			}
			obj.CoerceValueIfNeeded(minimum, d1);
			return d1;
		}, false));
		PlaceholderTextProperty = TextBox.PlaceholderTextProperty.AddOwner<FANumberBox>((StyledPropertyMetadata<string>)null);
		PlaceholderForegroundProperty = TextBox.PlaceholderForegroundProperty.AddOwner<FANumberBox>((StyledPropertyMetadata<IBrush>)null);
		SelectionHighlightColorProperty = TextBox.SelectionBrushProperty.AddOwner<FANumberBox>((StyledPropertyMetadata<IBrush>)null);
		SmallChangeProperty = RangeBase.SmallChangeProperty.AddOwner<FANumberBox>((StyledPropertyMetadata<double>)null);
		SpinButtonPlacementModeProperty = AvaloniaProperty.Register<FANumberBox, FANumberBoxSpinButtonPlacementMode>("SpinButtonPlacementMode", FANumberBoxSpinButtonPlacementMode.Hidden, false, (BindingMode)1, (Func<FANumberBoxSpinButtonPlacementMode, bool>)null, (Func<AvaloniaObject, FANumberBoxSpinButtonPlacementMode, FANumberBoxSpinButtonPlacementMode>)null, false);
		TextProperty = TextBlock.TextProperty.AddOwner<FANumberBox>(new StyledPropertyMetadata<string>(default(Optional<string>), (BindingMode)2, (Func<AvaloniaObject, string, string>)null, false));
		ValidationModeProperty = AvaloniaProperty.Register<FANumberBox, FANumberBoxValidationMode>("ValidationMode", FANumberBoxValidationMode.InvalidInputOverwritten, false, (BindingMode)1, (Func<FANumberBoxValidationMode, bool>)null, (Func<AvaloniaObject, FANumberBoxValidationMode, FANumberBoxValidationMode>)null, false);
		ValueProperty = RangeBase.ValueProperty.AddOwner<FANumberBox>(new StyledPropertyMetadata<double>(default(Optional<double>), (BindingMode)0, (Func<AvaloniaObject, double, double>)delegate(AvaloniaObject ao, double d1)
		{
			FANumberBox fANumberBox = ao as FANumberBox;
			double num = fANumberBox.CoerceValueToRange(d1);
			if (num == fANumberBox.Value)
			{
				fANumberBox.UpdateTextToValue();
			}
			return num;
		}, true));
		TextAlignmentProperty = (StyledProperty<TextAlignment>)(object)TextBlock.TextAlignmentProperty.AddOwner<FANumberBox>((StyledPropertyMetadata<TextAlignment>)null);
		SimpleNumberFormatProperty = AvaloniaProperty.Register<FANumberBox, string>("SimpleNumberFormat", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);
		InnerLeftContentProperty = TextBox.InnerLeftContentProperty.AddOwner<FANumberBox>((StyledPropertyMetadata<object>)null);
	}
}
