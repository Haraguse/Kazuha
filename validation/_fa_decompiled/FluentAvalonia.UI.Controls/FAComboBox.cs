using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a selection control that combines a non-editable text box and a drop-down list box that 
/// allows users to select an item from a list.
/// </summary>
[TemplatePart("Popup", typeof(Popup))]
[TemplatePart("EditableText", typeof(TextBox))]
[TemplatePart("DropDownOverlay", typeof(Border))]
[PseudoClasses(new string[] { ":selected", ":focus", ":pressed" })]
[PseudoClasses(new string[] { ":editable", ":dropdownopen", ":popupAbove", ":header" })]
public class FAComboBox : HeaderedSelectingItemsControl
{
	private Popup _popup;

	private TextBox _textBox;

	private Border _dropDownOverlay;

	private IDataTemplate _displayMemberTemplate;

	private int _dropDownSelectedIndex = -1;

	private int _ignoreTextPropertyChange;

	private bool _ignoreTextSelectionChange;

	private bool _hasUnsubmittedText;

	private int _currentTextSelectionStart;

	private readonly ITemplate<Control> _noFocusAdornerTemplate = (ITemplate<Control>)(object)new FuncTemplate<Control>((Func<Control>)delegate
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		return (Control)new Decorator();
	});

	private BindingEvaluator<object> _displayMemberBindingEvaluator;

	private FACompositeDisposable _subscriptionsOnOpen = new FACompositeDisposable();

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAComboBox.MaxDropDownHeight" /> property
	/// </summary>
	public static readonly StyledProperty<double> MaxDropDownHeightProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAComboBox.IsEditable" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsEditableProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAComboBox.IsDropDownOpen" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsDropDownOpenProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAComboBox.IsSelectionBoxHighlighted" /> property
	/// </summary>
	public static readonly DirectProperty<FAComboBox, bool> IsSelectionBoxHighlightedProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAComboBox.SelectionBoxItem" /> property
	/// </summary>
	public static readonly DirectProperty<FAComboBox, object> SelectionBoxItemProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAComboBox.SelectionBoxItemTemplate" /> property
	/// </summary>
	public static readonly DirectProperty<FAComboBox, IDataTemplate> SelectionBoxItemTemplateProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAComboBox.PlaceholderText" /> property
	/// </summary>
	public static readonly StyledProperty<string> PlaceholderTextProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAComboBox.SelectionChangedTrigger" /> property
	/// </summary>
	public static readonly StyledProperty<FAComboBoxSelectionChangedTrigger> SelectionChangedTriggerProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAComboBox.PlaceholderForeground" /> property
	/// </summary>
	public static readonly StyledProperty<IBrush> PlaceholderForegroundProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAComboBox.TextBoxTheme" /> property
	/// </summary>
	public static readonly StyledProperty<ControlTheme> TextBoxThemeProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAComboBox.Text" /> property
	/// </summary>
	public static readonly StyledProperty<string> TextProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAComboBox.HorizontalContentAlignment" /> property
	/// </summary>
	public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAComboBox.VerticalContentAlignment" /> property
	/// </summary>
	public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty;

	private bool _isSelectionBoxHighlighted;

	private object _selectionBoxItem;

	private IDataTemplate _selectionBoxItemTemplate;

	private const string s_tpPopup = "Popup";

	private const string s_tpEditableText = "EditableText";

	private const string s_tpDropDownOverlay = "DropDownOverlay";

	private const string s_pcEditable = ":editable";

	private const string s_pcFocus = ":focus";

	private const string s_pcSelected = ":selected";

	private const string s_pcDropDownOpen = ":dropdownopen";

	private const string s_pcPopupAbove = ":popupAbove";

	/// <summary>
	/// Gets or sets the <see cref="T:Avalonia.Layout.HorizontalAlignment" /> of the content in the ComboBox
	/// </summary>
	public HorizontalAlignment HorizontalContentAlignment
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<HorizontalAlignment>(HorizontalContentAlignmentProperty);
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<HorizontalAlignment>(HorizontalContentAlignmentProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the <see cref="T:Avalonia.Layout.VerticalAlignment" /> of the content in the ComboBox
	/// </summary>
	public VerticalAlignment VerticalContentAlignment
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<VerticalAlignment>(VerticalContentAlignmentProperty);
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<VerticalAlignment>(VerticalContentAlignmentProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the maximum allowed height of the dropdown
	/// </summary>
	public double MaxDropDownHeight
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(MaxDropDownHeightProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(MaxDropDownHeightProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets whether this ComboBox is editable
	/// </summary>
	public bool IsEditable
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsEditableProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsEditableProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets whether this ComboBox's dropdown is open
	/// </summary>
	public bool IsDropDownOpen
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsDropDownOpenProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsDropDownOpenProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets whether the SelectionBox is hightlighted
	/// </summary>
	public bool IsSelectionBoxHighlighted
	{
		get
		{
			return _isSelectionBoxHighlighted;
		}
		private set
		{
			((AvaloniaObject)this).SetAndRaise<bool>((DirectPropertyBase<bool>)(object)IsSelectionBoxHighlightedProperty, ref _isSelectionBoxHighlighted, value);
		}
	}

	/// <summary>
	/// Gets the item shown whne the ComboBox is closed
	/// </summary>
	public object SelectionBoxItem
	{
		get
		{
			return _selectionBoxItem;
		}
		private set
		{
			((AvaloniaObject)this).SetAndRaise<object>((DirectPropertyBase<object>)(object)SelectionBoxItemProperty, ref _selectionBoxItem, value);
		}
	}

	/// <summary>
	/// Gets the template applied to the selection box content.
	/// </summary>
	public IDataTemplate SelectionBoxItemTemplate
	{
		get
		{
			return _selectionBoxItemTemplate;
		}
		private set
		{
			((AvaloniaObject)this).SetAndRaise<IDataTemplate>((DirectPropertyBase<IDataTemplate>)(object)SelectionBoxItemTemplateProperty, ref _selectionBoxItemTemplate, value);
		}
	}

	/// <summary>
	/// Gets or sets the text that is displayed in the control until the value is changed by a user action 
	/// or some other operation.
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
	/// Gets or sets a value that indicates what action causes a SelectionChanged event to occur.
	/// </summary>
	public FAComboBoxSelectionChangedTrigger SelectionChangedTrigger
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAComboBoxSelectionChangedTrigger>(SelectionChangedTriggerProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAComboBoxSelectionChangedTrigger>(SelectionChangedTriggerProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a brush that describes the color of placeholder text.
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
	/// Gets or sets the <see cref="T:Avalonia.Styling.ControlTheme" /> used for the TextBox part of the ComboBox
	/// </summary>
	public ControlTheme TextBoxTheme
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<ControlTheme>(TextBoxThemeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<ControlTheme>(TextBoxThemeProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the text in the ComboBox.
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
	/// Occurs when the drop-down portion of the ComboBox opens.
	/// </summary>
	public event EventHandler<EventArgs> DropDownOpened;

	/// <summary>
	/// Occurs when the drop-down portion of the ComboBox closes.
	/// </summary>
	public event EventHandler<EventArgs> DropDownClosed;

	/// <summary>
	/// Occurs when the user submits some text that does not correspond to an item in the ComboBox dropdown list.
	/// </summary>
	public event TypedEventHandler<FAComboBox, FAComboBoxTextSubmittedEventArgs> TextSubmitted;

	static FAComboBox()
	{
		MaxDropDownHeightProperty = ComboBox.MaxDropDownHeightProperty.AddOwner<FAComboBox>((StyledPropertyMetadata<double>)null);
		IsEditableProperty = AvaloniaProperty.Register<FAComboBox, bool>("IsEditable", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
		IsDropDownOpenProperty = AvaloniaProperty.Register<FAComboBox, bool>("IsDropDownOpen", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
		IsSelectionBoxHighlightedProperty = AvaloniaProperty.RegisterDirect<FAComboBox, bool>("IsSelectionBoxHighlighted", (Func<FAComboBox, bool>)((FAComboBox x) => x.IsSelectionBoxHighlighted), (Action<FAComboBox, bool>)null, false, (BindingMode)1, false);
		SelectionBoxItemProperty = AvaloniaProperty.RegisterDirect<FAComboBox, object>("SelectionBoxItem", (Func<FAComboBox, object>)((FAComboBox x) => x.SelectionBoxItem), (Action<FAComboBox, object>)null, (object)null, (BindingMode)1, false);
		SelectionBoxItemTemplateProperty = AvaloniaProperty.RegisterDirect<FAComboBox, IDataTemplate>("SelectionBoxItemTemplate", (Func<FAComboBox, IDataTemplate>)((FAComboBox x) => x.SelectionBoxItemTemplate), (Action<FAComboBox, IDataTemplate>)null, (IDataTemplate)null, (BindingMode)1, false);
		PlaceholderTextProperty = ComboBox.PlaceholderTextProperty.AddOwner<FAComboBox>((StyledPropertyMetadata<string>)null);
		SelectionChangedTriggerProperty = AvaloniaProperty.Register<FAComboBox, FAComboBoxSelectionChangedTrigger>("SelectionChangedTrigger", FAComboBoxSelectionChangedTrigger.Committed, false, (BindingMode)1, (Func<FAComboBoxSelectionChangedTrigger, bool>)null, (Func<AvaloniaObject, FAComboBoxSelectionChangedTrigger, FAComboBoxSelectionChangedTrigger>)null, false);
		PlaceholderForegroundProperty = ComboBox.PlaceholderForegroundProperty.AddOwner<FAComboBox>((StyledPropertyMetadata<IBrush>)null);
		TextBoxThemeProperty = AvaloniaProperty.Register<FAComboBox, ControlTheme>("TextBoxTheme", (ControlTheme)null, false, (BindingMode)1, (Func<ControlTheme, bool>)null, (Func<AvaloniaObject, ControlTheme, ControlTheme>)null, false);
		TextProperty = AvaloniaProperty.Register<FAComboBox, string>("Text", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);
		HorizontalContentAlignmentProperty = ContentControl.HorizontalContentAlignmentProperty.AddOwner<FAComboBox>((StyledPropertyMetadata<HorizontalAlignment>)null);
		VerticalContentAlignmentProperty = ContentControl.VerticalContentAlignmentProperty.AddOwner<FAComboBox>((StyledPropertyMetadata<VerticalAlignment>)null);
		InputElement.FocusableProperty.OverrideDefaultValue<FAComboBox>(true);
		SelectingItemsControl.IsTextSearchEnabledProperty.OverrideDefaultValue<FAComboBox>(true);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		if (_popup != null)
		{
			_popup.Opened -= OnPopupOpened;
			_popup.Closed -= OnPopupClosed;
		}
		if (_textBox != null)
		{
			_textBox.TextChanged -= OnTextBoxTextChanged;
			((InputElement)_textBox).KeyDown -= OnTextBoxKeyDown;
		}
		if (_dropDownOverlay != null)
		{
			((InputElement)_dropDownOverlay).PointerPressed -= OnDropDownOverlayPointerPressed;
			((InputElement)_dropDownOverlay).PointerReleased -= OnDropDownOverlayPointerReleased;
			((InputElement)_dropDownOverlay).PointerCaptureLost -= OnDropDownOverlayPointerCaptureLost;
		}
		((SelectingItemsControl)this).OnApplyTemplate(e);
		_popup = NameScopeExtensions.Get<Popup>(e.NameScope, "Popup");
		_popup.Opened += OnPopupOpened;
		_popup.Closed += OnPopupClosed;
		_textBox = NameScopeExtensions.Find<TextBox>(e.NameScope, "EditableText");
		if (_textBox != null)
		{
			_textBox.TextChanged += OnTextBoxTextChanged;
			((InputElement)_textBox).KeyDown += OnTextBoxKeyDown;
		}
		_dropDownOverlay = NameScopeExtensions.Find<Border>(e.NameScope, "DropDownOverlay");
		if (_dropDownOverlay != null)
		{
			((InputElement)_dropDownOverlay).PointerPressed += OnDropDownOverlayPointerPressed;
			((InputElement)_dropDownOverlay).PointerReleased += OnDropDownOverlayPointerReleased;
			((InputElement)_dropDownOverlay).PointerCaptureLost += OnDropDownOverlayPointerCaptureLost;
		}
		string text = Text;
		if (IsEditable && !string.IsNullOrEmpty(text))
		{
			UpdateSelectionBoxItem(text);
		}
		else
		{
			UpdateSelectionBoxItem(((SelectingItemsControl)this).SelectedItem);
		}
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((SelectingItemsControl)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)SelectingItemsControl.SelectedItemProperty)
		{
			OnSelectedItemChanged(change);
		}
		else if (change.Property == (AvaloniaProperty)(object)ItemsControl.DisplayMemberBindingProperty)
		{
			BindingBase temp = AvaloniaPropertyChangedExtensions.GetNewValue<BindingBase>(change);
			if (temp != null)
			{
				_displayMemberTemplate = (IDataTemplate)(object)new FuncDataTemplate<object>((Func<object, INameScope, Control>)delegate
				{
					//IL_0000: Unknown result type (might be due to invalid IL or missing references)
					//IL_0010: Unknown result type (might be due to invalid IL or missing references)
					//IL_001e: Expected O, but got Unknown
					return (Control?)new TextBlock { [!(AvaloniaProperty)(object)TextBlock.TextProperty] = temp };
				}, false);
			}
			else
			{
				_displayMemberTemplate = null;
			}
		}
		else if (change.Property == (AvaloniaProperty)(object)ItemsControl.ItemTemplateProperty)
		{
			IDataTemplate newValue = AvaloniaPropertyChangedExtensions.GetNewValue<IDataTemplate>(change);
			SelectionBoxItemTemplate = newValue;
		}
		else if (change.Property == (AvaloniaProperty)(object)IsEditableProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":editable", AvaloniaPropertyChangedExtensions.GetNewValue<bool>(change));
			UpdateSelectionBoxItem(((SelectingItemsControl)this).SelectedItem ?? Text);
		}
		else if (change.Property == (AvaloniaProperty)(object)TextProperty)
		{
			if (_textBox != null && IsEditable)
			{
				OnTextChanged(AvaloniaPropertyChangedExtensions.GetNewValue<string>(change));
			}
		}
		else if (change.Property == (AvaloniaProperty)(object)HeaderedSelectingItemsControl.HeaderProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":header", change.NewValue != null);
		}
	}

	protected override Control CreateContainerForItemOverride(object item, int index, object recycleKey)
	{
		return (Control)(object)new FAComboBoxItem();
	}

	protected override bool NeedsContainerOverride(object item, int index, out object recycleKey)
	{
		bool flag = item is FAComboBoxItem;
		recycleKey = (flag ? null : "FAComboBoxItem");
		return !flag;
	}

	protected override void InvalidateMirrorTransform()
	{
		((Visual)this).InvalidateMirrorTransform();
		UpdateFlowDirection();
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Invalid comparison between Unknown and I4
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Invalid comparison between Unknown and I4
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Invalid comparison between Unknown and I4
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Invalid comparison between Unknown and I4
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Invalid comparison between Unknown and I4
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Invalid comparison between Unknown and I4
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Invalid comparison between Unknown and I4
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Invalid comparison between Unknown and I4
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Invalid comparison between Unknown and I4
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Invalid comparison between Unknown and I4
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Invalid comparison between Unknown and I4
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Invalid comparison between Unknown and I4
		((ItemsControl)this).OnKeyDown(e);
		if (((RoutedEventArgs)e).Handled)
		{
			return;
		}
		bool isDropDownOpen = IsDropDownOpen;
		bool isEditable = IsEditable;
		if (((int)e.Key == 93 && !((Enum)e.KeyModifiers).HasFlag((Enum)(object)(KeyModifiers)1)) || (((int)e.Key == 26 || (int)e.Key == 24) && ((Enum)e.KeyModifiers).HasFlag((Enum)(object)(KeyModifiers)1)))
		{
			IsDropDownOpen = !isDropDownOpen;
			((RoutedEventArgs)e).Handled = true;
		}
		else if ((int)e.Key == 13)
		{
			if (isDropDownOpen)
			{
				IsDropDownOpen = false;
			}
			UpdateSelectionBoxItem(((SelectingItemsControl)this).SelectedItem);
			if (isEditable && ((SelectingItemsControl)this).SelectedItem == null)
			{
				Text = null;
			}
			((RoutedEventArgs)e).Handled = true;
		}
		else if (!isDropDownOpen && !isEditable && ((int)e.Key == 6 || (int)e.Key == 18))
		{
			IsDropDownOpen = true;
			((RoutedEventArgs)e).Handled = true;
		}
		else if (isEditable && (int)e.Key == 6)
		{
			if (_textBox != null && ((InputElement)_textBox).IsFocused)
			{
				OnTextSubmittedCore();
			}
			else
			{
				SelectFocusedItem();
			}
			IsDropDownOpen = false;
			((RoutedEventArgs)e).Handled = true;
		}
		else if (isDropDownOpen && ((int)e.Key == 6 || (int)e.Key == 18))
		{
			SelectFocusedItem();
			IsDropDownOpen = false;
			((RoutedEventArgs)e).Handled = true;
		}
		else if (!isDropDownOpen)
		{
			if ((int)e.Key == 26)
			{
				if (!isEditable)
				{
					SelectNext();
				}
				else
				{
					IsDropDownOpen = true;
				}
				((RoutedEventArgs)e).Handled = true;
			}
			else if ((int)e.Key == 24)
			{
				if (!isEditable)
				{
					SelectPrevious();
				}
				else
				{
					IsDropDownOpen = true;
				}
				((RoutedEventArgs)e).Handled = true;
			}
		}
		else
		{
			if (!isDropDownOpen || ((SelectingItemsControl)this).SelectedIndex >= 0 || ((ItemsControl)this).ItemCount <= 0 || ((int)e.Key != 24 && (int)e.Key != 26) || !((InputElement)this).IsFocused)
			{
				return;
			}
			ItemsPresenter presenter = ((ItemsControl)this).Presenter;
			object obj;
			if (presenter == null)
			{
				obj = null;
			}
			else
			{
				Panel panel = presenter.Panel;
				obj = ((panel != null) ? ((IEnumerable<Control>)panel.Children).FirstOrDefault((Control c) => CanFocus(c)) : null);
			}
			Control val = (Control)obj;
			if (val != null)
			{
				((InputElement)val).Focus((NavigationMethod)2, (KeyModifiers)0);
				((RoutedEventArgs)e).Handled = true;
			}
		}
	}

	protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		((InputElement)this).OnPointerWheelChanged(e);
		if (((RoutedEventArgs)e).Handled)
		{
			return;
		}
		if (!IsDropDownOpen)
		{
			if (((InputElement)this).IsFocused)
			{
				Vector delta = e.Delta;
				if (((Vector)(ref delta)).Y < 0.0)
				{
					SelectNext();
				}
				else
				{
					SelectPrevious();
				}
				((RoutedEventArgs)e).Handled = true;
			}
		}
		else
		{
			((RoutedEventArgs)e).Handled = true;
		}
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		((InputElement)this).OnPointerPressed(e);
		if (!((RoutedEventArgs)e).Handled)
		{
			object source = ((RoutedEventArgs)e).Source;
			Visual val = (Visual)((source is Visual) ? source : null);
			if (val != null)
			{
				Popup popup = _popup;
				if (popup != null && popup.IsInsidePopup(val))
				{
					return;
				}
			}
		}
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":pressed", true);
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		if (!((RoutedEventArgs)e).Handled)
		{
			object source = ((RoutedEventArgs)e).Source;
			Visual val = (Visual)((source is Visual) ? source : null);
			if (val != null)
			{
				Popup popup = _popup;
				if (popup != null && popup.IsInsidePopup(val))
				{
					Control containerFromEventSource = ((SelectingItemsControl)this).GetContainerFromEventSource(((RoutedEventArgs)e).Source);
					if (containerFromEventSource != null && ((SelectingItemsControl)this).UpdateSelectionFromEvent(containerFromEventSource, (RoutedEventArgs)(object)e))
					{
						_popup.Close();
						((RoutedEventArgs)e).Handled = true;
					}
				}
				else
				{
					if (!IsEditable)
					{
						bool isDropDownOpen = IsDropDownOpen;
						IsDropDownOpen = !isDropDownOpen;
					}
					((RoutedEventArgs)e).Handled = true;
				}
			}
		}
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":pressed", false);
		((Control)this).OnPointerReleased(e);
	}

	protected override void OnGotFocus(FocusChangedEventArgs e)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		((ItemsControl)this).OnGotFocus(e);
		if (IsEditable && _textBox != null)
		{
			if (!IsDropDownOpen)
			{
				((InputElement)_textBox).Focus(e.NavigationMethod, (KeyModifiers)0);
				HighlightTextBoxText();
			}
			else if (!((InputElement)_textBox).IsFocused)
			{
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_textBox).Classes, ":focus", true);
			}
		}
		UpdateIsSelectionBoxHighlighted();
	}

	protected override void OnLostFocus(FocusChangedEventArgs e)
	{
		((Control)this).OnLostFocus(e);
		if ((!IsDropDownOpen || !(((RoutedEventArgs)e).Source is FAComboBoxItem)) && IsEditable && !HasImplicitFocus())
		{
			if (_hasUnsubmittedText)
			{
				OnTextSubmittedCore();
			}
			if (IsDropDownOpen && _popup.IsLightDismissEnabled)
			{
				IsDropDownOpen = false;
			}
			ClearTextBoxSelection();
		}
		bool HasImplicitFocus()
		{
			TopLevel topLevel = TopLevel.GetTopLevel((Visual)(object)this);
			object obj;
			if (topLevel == null)
			{
				obj = null;
			}
			else
			{
				IFocusManager focusManager = topLevel.FocusManager;
				obj = ((focusManager != null) ? focusManager.GetFocusedElement() : null);
			}
			Control val = (Control)((obj is Control) ? obj : null);
			if (val is FAComboBoxItem)
			{
				return true;
			}
			if (val != null)
			{
				return VisualExtensions.FindAncestorOfType<FAComboBox>((Visual)(object)val, true) == this;
			}
			return false;
		}
	}

	protected override void PrepareContainerForItemOverride(Control element, object item, int index)
	{
		((SelectingItemsControl)this).PrepareContainerForItemOverride(element, item, index);
		if (IsEditable)
		{
			element.FocusAdorner = _noFocusAdornerTemplate;
		}
	}

	protected override void ClearContainerForItemOverride(Control element)
	{
		((SelectingItemsControl)this).ClearContainerForItemOverride(element);
		if (IsEditable)
		{
			((AvaloniaObject)element).ClearValue<ITemplate<Control>>(Control.FocusAdornerProperty);
		}
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return (AutomationPeer)(object)new FAComboBoxAutomationPeer((SelectingItemsControl)(object)this);
	}

	protected override bool ShouldTriggerSelection(Visual selectable, PointerEventArgs eventArgs)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		bool flag = ((SelectingItemsControl)this).ShouldTriggerSelection(selectable, eventArgs);
		if (!flag)
		{
			PointerReleasedEventArgs e = (PointerReleasedEventArgs)(object)((eventArgs is PointerReleasedEventArgs) ? eventArgs : null);
			if (e != null)
			{
				PointerPoint currentPoint = ((PointerEventArgs)e).GetCurrentPoint((Visual)(object)this);
				PointerPointProperties properties = ((PointerPoint)(ref currentPoint)).Properties;
				if ((int)((PointerPointProperties)(ref properties)).PointerUpdateKind == 5)
				{
					flag = true;
				}
			}
		}
		return flag;
	}

	internal void ItemFocused(FAComboBoxItem item)
	{
		if (IsDropDownOpen && ((InputElement)item).IsFocused && ((Layoutable)item).IsArrangeValid)
		{
			ControlExtensions.BringIntoView((Control)(object)item);
			Control val = ((ItemsControl)this).ContainerFromIndex(_dropDownSelectedIndex);
			if (val != null)
			{
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)val).Classes, ":selected", false);
			}
			if (SelectionChangedTrigger == FAComboBoxSelectionChangedTrigger.Always)
			{
				((SelectingItemsControl)this).SelectedIndex = ((ItemsControl)this).IndexFromContainer((Control)(object)item);
				return;
			}
			_dropDownSelectedIndex = ((ItemsControl)this).IndexFromContainer((Control)(object)item);
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)item).Classes, ":selected", true);
			UpdateSelectionBoxItem(((ItemsControl)this).ItemsView[_dropDownSelectedIndex]);
		}
	}

	protected virtual void OnSelectedItemChanged(AvaloniaPropertyChangedEventArgs args)
	{
		UpdateSelectionBoxItem(args.NewValue ?? Text);
		TryFocusSelectedItem();
	}

	protected virtual void OnTextSubmitted(FAComboBoxTextSubmittedEventArgs args)
	{
	}

	private void OnPopupOpened(object sender, EventArgs e)
	{
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		TryFocusSelectedItem();
		if (SelectionChangedTrigger == FAComboBoxSelectionChangedTrigger.Committed)
		{
			_dropDownSelectedIndex = ((SelectingItemsControl)this).SelectedIndex;
		}
		_subscriptionsOnOpen.Clear();
		TopLevel toplevel = TopLevel.GetTopLevel((Visual)(object)this);
		if (toplevel != null)
		{
			_subscriptionsOnOpen.Add(InteractiveExtensions.AddDisposableHandler<PointerWheelEventArgs>((Interactive)(object)toplevel, InputElement.PointerWheelChangedEvent, (EventHandler<PointerWheelEventArgs>)delegate(object? s, PointerWheelEventArgs ev)
			{
				if (IsDropDownOpen)
				{
					object source = ((RoutedEventArgs)ev).Source;
					if (TopLevel.GetTopLevel((Visual)((source is Visual) ? source : null)) == toplevel)
					{
						((RoutedEventArgs)ev).Handled = true;
					}
				}
			}, (RoutingStrategies)2, false));
		}
		_subscriptionsOnOpen.Add(AvaloniaObjectExtensions.GetObservable<bool>((AvaloniaObject)(object)this, (AvaloniaProperty<bool>)(object)Visual.IsVisibleProperty).Subscribe(new SimpleObserver<bool>(IsVisibleChanged)));
		foreach (Control item in VisualExtensions.GetVisualAncestors((Visual)(object)this).OfType<Control>())
		{
			_subscriptionsOnOpen.Add(AvaloniaObjectExtensions.GetObservable<bool>((AvaloniaObject)(object)item, (AvaloniaProperty<bool>)(object)Visual.IsVisibleProperty).Subscribe(new SimpleObserver<bool>(IsVisibleChanged)));
		}
		UpdateFlowDirection();
		DropDownOpened?.Invoke(this, EventArgs.Empty);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":dropdownopen", true);
		if (!IsEditable)
		{
			int selectedIndex = ((SelectingItemsControl)this).SelectedIndex;
			double num = 0.0;
			Size desiredSize;
			if (selectedIndex == -1)
			{
				desiredSize = ((Layoutable)_popup.Child).DesiredSize;
				num = ((Size)(ref desiredSize)).Height / 2.0;
			}
			else
			{
				Control val = ((ItemsControl)this).ContainerFromIndex(selectedIndex);
				if (val == null)
				{
					return;
				}
				TopLevel topLevel = TopLevel.GetTopLevel((Visual)(object)val);
				Matrix? val2 = VisualExtensions.TransformToVisual((Visual)(object)val, (Visual)(object)topLevel);
				if (!val2.HasValue)
				{
					return;
				}
				Point val3 = new Point(0.0, 0.0);
				val3 = ((Point)(ref val3)).Transform(val2.Value);
				double y = ((Point)(ref val3)).Y;
				desiredSize = ((Layoutable)val).DesiredSize;
				num = y + ((Size)(ref desiredSize)).Height;
			}
			_popup.VerticalOffset = 0.0 - num;
			if (TopLevel.GetTopLevel((Visual)(object)_popup.Child) is PopupRoot)
			{
				_popup.HorizontalOffset = -1.0;
			}
		}
		else
		{
			UpdateCornerRadius();
		}
	}

	private void UpdateCornerRadius()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		Visual child = (Visual)(object)_popup.Child;
		TopLevel topLevel = TopLevel.GetTopLevel((Visual)(object)this);
		TopLevel topLevel2 = TopLevel.GetTopLevel(child);
		bool flag = false;
		if (topLevel2 == topLevel)
		{
			Point val = new Point(0.0, 0.0);
			Point val2 = ((Point)(ref val)).Transform(VisualExtensions.TransformToVisual((Visual)(object)this, (Visual)(object)topLevel).Value);
			val = new Point(0.0, 0.0);
			Point val3 = ((Point)(ref val)).Transform(VisualExtensions.TransformToVisual(child, (Visual)(object)topLevel).Value);
			flag = ((Point)(ref val3)).Y < ((Point)(ref val2)).Y;
		}
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":popupAbove", flag);
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
		_subscriptionsOnOpen.Clear();
		if (CanFocus((Control)(object)this))
		{
			if (IsEditable && _textBox != null)
			{
				((InputElement)_textBox).Focus((NavigationMethod)0, (KeyModifiers)0);
			}
			else
			{
				((InputElement)this).Focus((NavigationMethod)0, (KeyModifiers)0);
			}
		}
		DropDownClosed?.Invoke(this, EventArgs.Empty);
		UpdateSelectionBoxItem(((SelectingItemsControl)this).SelectedItem ?? Text);
		if (_dropDownSelectedIndex != ((SelectingItemsControl)this).SelectedIndex)
		{
			Control val = ((ItemsControl)this).ContainerFromIndex(_dropDownSelectedIndex);
			if (val != null)
			{
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)val).Classes, ":selected", false);
			}
		}
		_dropDownSelectedIndex = -1;
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":dropdownopen", false);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":popupAbove", false);
	}

	private void IsVisibleChanged(bool isVisible)
	{
		if (!isVisible && IsDropDownOpen)
		{
			IsDropDownOpen = false;
		}
	}

	private void TryFocusSelectedItem()
	{
		int selectedIndex = ((SelectingItemsControl)this).SelectedIndex;
		if (IsDropDownOpen && selectedIndex != -1)
		{
			Control val = ((ItemsControl)this).ContainerFromIndex(selectedIndex);
			if (val == null && selectedIndex != -1)
			{
				((ItemsControl)this).ScrollIntoView(((SelectingItemsControl)this).Selection.SelectedIndex);
				val = ((ItemsControl)this).ContainerFromIndex(selectedIndex);
			}
			if (val != null && CanFocus(val))
			{
				((InputElement)val).Focus((NavigationMethod)0, (KeyModifiers)0);
			}
		}
	}

	private bool CanFocus(Control control)
	{
		if (((InputElement)control).Focusable && ((InputElement)control).IsEffectivelyEnabled)
		{
			return ((Visual)control).IsVisible;
		}
		return false;
	}

	private void UpdateSelectionBoxItem(object item)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Expected O, but got Unknown
		if (!VisualExtensions.IsAttachedToVisualTree((Visual)(object)this))
		{
			return;
		}
		if (IsEditable && item != null)
		{
			UpdateTextValue(FormatValue(item), false);
		}
		ContentControl val = (ContentControl)((item is ContentControl) ? item : null);
		if (val != null)
		{
			item = val.Content;
		}
		Control val2 = (Control)((item is Control) ? item : null);
		if (val2 != null)
		{
			((Layoutable)val2).Measure(Size.Infinity);
			object selectionBoxItem = SelectionBoxItem;
			Rectangle val3 = (Rectangle)(((selectionBoxItem is Rectangle) ? selectionBoxItem : null) ?? ((object)new Rectangle()));
			Size desiredSize = ((Layoutable)val2).DesiredSize;
			((Layoutable)val3).Width = ((Size)(ref desiredSize)).Width;
			desiredSize = ((Layoutable)val2).DesiredSize;
			((Layoutable)val3).Height = ((Size)(ref desiredSize)).Height;
			IBrush fill = ((Shape)val3).Fill;
			VisualBrush val4 = (VisualBrush)(object)((fill is VisualBrush) ? fill : null);
			if (val4 != null)
			{
				val4.Visual = (Visual)(object)val2;
			}
			else
			{
				((Shape)val3).Fill = (IBrush)new VisualBrush
				{
					Visual = (Visual)(object)val2,
					Stretch = (Stretch)0,
					AlignmentX = (AlignmentX)0
				};
			}
			if (selectionBoxItem != val3)
			{
				SelectionBoxItem = val3;
			}
			UpdateFlowDirection();
		}
		else if (item is string)
		{
			SelectionBoxItemTemplate = null;
			SelectionBoxItem = item;
		}
		else
		{
			IDataTemplate val5 = _displayMemberTemplate ?? ((ItemsControl)this).ItemTemplate;
			if (val5 != null)
			{
				SelectionBoxItemTemplate = val5;
			}
			SelectionBoxItem = item;
		}
	}

	private void UpdateFlowDirection()
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		object selectionBoxItem = SelectionBoxItem;
		Rectangle val = (Rectangle)((selectionBoxItem is Rectangle) ? selectionBoxItem : null);
		if (val != null)
		{
			IBrush fill = ((Shape)val).Fill;
			IBrush obj = ((fill is VisualBrush) ? fill : null);
			Visual val2 = ((obj != null) ? ((VisualBrush)obj).Visual : null);
			if (val2 != null)
			{
				Visual visualParent = VisualExtensions.GetVisualParent(val2);
				Visual obj2 = ((visualParent is Control) ? visualParent : null);
				FlowDirection flowDirection = (FlowDirection)((obj2 != null) ? ((int)obj2.FlowDirection) : 0);
				((Visual)val).FlowDirection = flowDirection;
			}
		}
	}

	private void SelectNext()
	{
		MoveSelection(((SelectingItemsControl)this).SelectedIndex, 1, ((SelectingItemsControl)this).WrapSelection);
	}

	private void SelectPrevious()
	{
		MoveSelection(((SelectingItemsControl)this).SelectedIndex, -1, ((SelectingItemsControl)this).WrapSelection);
	}

	private void MoveSelection(int startIndex, int step, bool wrap)
	{
		int itemCount = ((ItemsControl)this).ItemCount;
		for (int i = startIndex + step; i != startIndex; i += step)
		{
			if (i < 0 || i >= itemCount)
			{
				if (!wrap)
				{
					break;
				}
				if (i < 0)
				{
					i += itemCount;
				}
				else if (i >= itemCount)
				{
					i %= itemCount;
				}
			}
			object o = ((ItemsControl)this).ItemsView[i];
			Control o2 = ((ItemsControl)this).ContainerFromIndex(i);
			if (IsSelectable(o) && IsSelectable(o2))
			{
				((SelectingItemsControl)this).SelectedIndex = i;
				break;
			}
		}
		static bool IsSelectable(object obj2)
		{
			object obj = ((obj2 is AvaloniaObject) ? obj2 : null);
			if (obj == null)
			{
				return true;
			}
			return ((AvaloniaObject)obj).GetValue<bool>(InputElement.IsEnabledProperty);
		}
	}

	private void SelectFocusedItem()
	{
		if (SelectionChangedTrigger == FAComboBoxSelectionChangedTrigger.Committed)
		{
			((SelectingItemsControl)this).SelectedIndex = _dropDownSelectedIndex;
			_dropDownSelectedIndex = -1;
			return;
		}
		foreach (Control realizedContainer in ((ItemsControl)this).GetRealizedContainers())
		{
			if (((InputElement)realizedContainer).IsFocused)
			{
				((SelectingItemsControl)this).SelectedIndex = ((ItemsControl)this).IndexFromContainer(realizedContainer);
				break;
			}
		}
	}

	private void UpdateIsSelectionBoxHighlighted()
	{
		IsSelectionBoxHighlighted = !IsDropDownOpen && ((InputElement)this).IsKeyboardFocusWithin;
	}

	private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		if (IsDropDownOpen && ((int)e.Key == 24 || (int)e.Key == 26))
		{
			Control val = ((ItemsControl)this).ContainerFromIndex(0);
			if (val != null)
			{
				((InputElement)val).Focus((NavigationMethod)2, (KeyModifiers)0);
				((RoutedEventArgs)e).Handled = true;
			}
		}
	}

	private void OnDropDownOverlayPointerPressed(object sender, PointerPressedEventArgs e)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		PointerPoint currentPoint = ((PointerEventArgs)e).GetCurrentPoint((Visual)(object)_dropDownOverlay);
		PointerPointProperties properties = ((PointerPoint)(ref currentPoint)).Properties;
		if (((PointerPointProperties)(ref properties)).IsLeftButtonPressed)
		{
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_dropDownOverlay).Classes, ":pressed", true);
			((RoutedEventArgs)e).Handled = true;
		}
	}

	private void OnDropDownOverlayPointerReleased(object sender, PointerReleasedEventArgs e)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Invalid comparison between Unknown and I4
		PointerPoint currentPoint = ((PointerEventArgs)e).GetCurrentPoint((Visual)(object)_dropDownOverlay);
		PointerPointProperties properties = ((PointerPoint)(ref currentPoint)).Properties;
		if ((int)((PointerPointProperties)(ref properties)).PointerUpdateKind == 5)
		{
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_dropDownOverlay).Classes, ":pressed", false);
			IsDropDownOpen = !IsDropDownOpen;
			UpdateSelectionBoxItem(((SelectingItemsControl)this).SelectedItem);
			((RoutedEventArgs)e).Handled = true;
		}
	}

	private void OnDropDownOverlayPointerCaptureLost(object sender, PointerCaptureLostEventArgs e)
	{
		PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)_dropDownOverlay).Classes, ":pressed", false);
	}

	private string FormatValue(object item)
	{
		ContentControl val = (ContentControl)((item is ContentControl) ? item : null);
		if (val != null)
		{
			return val.Content.ToString();
		}
		if (item is string result)
		{
			return result;
		}
		string? result2 = GetBindingEvaluator().Evaluate(item)?.ToString();
		_displayMemberBindingEvaluator.ClearDataContext();
		return result2;
	}

	private BindingEvaluator<object> GetBindingEvaluator()
	{
		if (_displayMemberBindingEvaluator == null)
		{
			_displayMemberBindingEvaluator = new BindingEvaluator<object>();
		}
		_displayMemberBindingEvaluator.UpdateBinding(((ItemsControl)this).DisplayMemberBinding);
		return _displayMemberBindingEvaluator;
	}

	private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		Dispatcher.UIThread.Post((Action)delegate
		{
			TextUpdated(_textBox.Text, user: true);
		}, default(DispatcherPriority));
	}

	private void OnTextChanged(string newValue)
	{
		TextUpdated(newValue, user: false);
	}

	private void UpdateTextValue(string value, bool? user)
	{
		if ((user ?? true) && Text != value)
		{
			_ignoreTextPropertyChange++;
			Text = value;
		}
		if ((!user.HasValue || user == false) && _textBox != null && _textBox.Text != value)
		{
			_ignoreTextPropertyChange++;
			_textBox.Text = value ?? string.Empty;
		}
	}

	private void TextUpdated(string newText, bool user)
	{
		if (_ignoreTextPropertyChange > 0)
		{
			_ignoreTextPropertyChange--;
			return;
		}
		if (newText == null)
		{
			newText = string.Empty;
		}
		int selectionStart = _textBox.SelectionStart;
		int selectionEnd = _textBox.SelectionEnd;
		if (!((SelectingItemsControl)this).IsTextSearchEnabled || _textBox == null || selectionEnd - selectionStart <= 0 || selectionStart == _textBox.Text.Length)
		{
			_hasUnsubmittedText = true;
			UpdateTextValue(newText, user);
			if (!string.IsNullOrEmpty(newText) && newText.Length > 0)
			{
				_ignoreTextSelectionChange = true;
				UpdateTextCompletion(user: true);
			}
		}
	}

	private void UpdateTextCompletion(bool user)
	{
		string text = Text;
		if (((ItemsControl)this).ItemCount > 0 && ((((SelectingItemsControl)this).IsTextSearchEnabled && _textBox != null) & user))
		{
			int length = _textBox.Text.Length;
			int selectionStart = _textBox.SelectionStart;
			if (selectionStart == text.Length && selectionStart > _currentTextSelectionStart)
			{
				object obj = TryGetMatch(text, out var index);
				if (obj != null)
				{
					string text2 = FormatValue(obj);
					int minLength = Math.Min(text2.Length, text.Length);
					if (Compare(text, text2, minLength))
					{
						UpdateTextValue(text2, null);
						_textBox.SelectionStart = length;
						_textBox.SelectionEnd = text2.Length;
						if (IsDropDownOpen)
						{
							_dropDownSelectedIndex = index;
						}
					}
				}
			}
		}
		if (_ignoreTextSelectionChange)
		{
			_ignoreTextSelectionChange = false;
			if (_textBox != null)
			{
				_currentTextSelectionStart = _textBox.SelectionStart;
			}
		}
		static bool Compare(string text3, string text4, int length2)
		{
			ReadOnlySpan<char> span = text3.AsSpan().Slice(0, length2);
			ReadOnlySpan<char> other = text4.AsSpan().Slice(0, length2);
			return span.Equals(other, StringComparison.OrdinalIgnoreCase);
		}
	}

	private object TryGetMatch(string currentText, out int index)
	{
		index = -1;
		ItemsSourceView itemsView = ((ItemsControl)this).ItemsView;
		int count = itemsView.Count;
		for (int i = 0; i < count; i++)
		{
			object obj = itemsView[i];
			string text = FormatValue(obj);
			if (!string.IsNullOrEmpty(text) && text.StartsWith(currentText, StringComparison.OrdinalIgnoreCase))
			{
				index = i;
				return obj;
			}
		}
		return null;
	}

	private void OnTextSubmittedCore()
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		FAComboBoxTextSubmittedEventArgs e = new FAComboBoxTextSubmittedEventArgs(Text);
		OnTextSubmitted(e);
		TextSubmitted?.Invoke(this, e);
		if (e.Handled)
		{
			_hasUnsubmittedText = false;
			ClearTextBoxSelection();
			return;
		}
		int num = FindItemFromTextValue(e.Text);
		int selectedIndex = ((SelectingItemsControl)this).SelectedIndex;
		((SelectingItemsControl)this).SelectedIndex = num;
		if (selectedIndex == -1 && num == -1)
		{
			((Interactive)this).RaiseEvent((RoutedEventArgs)new SelectionChangedEventArgs((RoutedEvent)(object)SelectingItemsControl.SelectionChangedEvent, (IList)null, (IList)null));
			UpdateSelectionBoxItem(Text);
		}
		_hasUnsubmittedText = false;
		ClearTextBoxSelection();
	}

	private void HighlightTextBoxText()
	{
		_textBox.SelectAll();
	}

	private void ClearTextBoxSelection()
	{
		if (_textBox != null)
		{
			int num = _textBox.Text?.Length ?? 0;
			TextBox textBox = _textBox;
			int selectionStart = (_textBox.SelectionEnd = num);
			textBox.SelectionStart = selectionStart;
		}
	}

	private int FindItemFromTextValue(string text)
	{
		ItemsSourceView itemsView = ((ItemsControl)this).ItemsView;
		int count = itemsView.Count;
		BindingBase displayMemberBinding = ((ItemsControl)this).DisplayMemberBinding;
		BindingEvaluator<object> bindingEvaluator = GetBindingEvaluator();
		for (int i = 0; i < count; i++)
		{
			object obj = itemsView[i];
			ContentControl val = (ContentControl)((obj is ContentControl) ? obj : null);
			if (val != null)
			{
				if (val.Content.ToString().Equals(text))
				{
					return i;
				}
				continue;
			}
			object obj2 = ((displayMemberBinding == null) ? obj.ToString() : bindingEvaluator.Evaluate(obj));
			if (obj2 != null && obj2.Equals(text))
			{
				bindingEvaluator.ClearDataContext();
				return i;
			}
		}
		bindingEvaluator.ClearDataContext();
		return -1;
	}
}
