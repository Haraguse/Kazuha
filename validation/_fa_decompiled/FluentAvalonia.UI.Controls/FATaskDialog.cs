using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls.Primitives;
using FluentAvalonia.UI.Windowing;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents and enhanced dialog with enhanced button, command, and progress support
/// </summary>
[PseudoClasses(new string[] { ":hosted", ":hidden", ":open" })]
[PseudoClasses(new string[] { ":header", ":subheader", ":icon", ":footer", ":footerAuto", ":expanded" })]
[PseudoClasses(new string[] { ":progress", ":progressError", ":progressSuspend" })]
[PseudoClasses(new string[] { ":headerForeground", ":iconForeground" })]
[TemplatePart("ButtonsHost", typeof(ItemsControl))]
[TemplatePart("CommandsHost", typeof(ItemsControl))]
[TemplatePart("MoreDetailsButton", typeof(Button))]
[TemplatePart("ProgressBar", typeof(ProgressBar))]
public class FATaskDialog : ContentControl
{
	private ItemsControl _buttonsHost;

	private ItemsControl _commandsHost;

	private ProgressBar _progressBar;

	private Button _moreDetailsButton;

	private FATaskDialogProgressState _currentProgressState;

	private Button _defaultButton;

	public Control _xamlOwner;

	private int _xamlOwnerChildIndex;

	private Control _host;

	private TaskCompletionSource<object> _tcs;

	internal bool _hasDeferralActive;

	private IInputElement _previousFocus;

	private bool _ignoreWindowClosingEvent;

	private bool _isOpening;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialog.Title" /> property
	/// </summary>
	public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<FATaskDialog, string>("Title", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialog.Header" /> property
	/// </summary>
	public static readonly StyledProperty<string> HeaderProperty = AvaloniaProperty.Register<FATaskDialog, string>("Header", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialog.SubHeader" /> property
	/// </summary>
	public static readonly StyledProperty<string> SubHeaderProperty = AvaloniaProperty.Register<FATaskDialog, string>("SubHeader", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialog.IconSource" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconSource> IconSourceProperty = AvaloniaProperty.Register<FATaskDialog, FAIconSource>("IconSource", (FAIconSource)null, false, (BindingMode)1, (Func<FAIconSource, bool>)null, (Func<AvaloniaObject, FAIconSource, FAIconSource>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialog.Buttons" /> property
	/// </summary>
	public static readonly DirectProperty<FATaskDialog, IList<FATaskDialogButton>> ButtonsProperty = AvaloniaProperty.RegisterDirect<FATaskDialog, IList<FATaskDialogButton>>("Buttons", (Func<FATaskDialog, IList<FATaskDialogButton>>)((FATaskDialog x) => x.Buttons), (Action<FATaskDialog, IList<FATaskDialogButton>>)delegate(FATaskDialog x, IList<FATaskDialogButton> v)
	{
		x.Buttons = v;
	}, (IList<FATaskDialogButton>)null, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialog.Commands" /> property
	/// </summary>
	public static readonly DirectProperty<FATaskDialog, IList<FATaskDialogCommand>> CommandsProperty = AvaloniaProperty.RegisterDirect<FATaskDialog, IList<FATaskDialogCommand>>("Commands", (Func<FATaskDialog, IList<FATaskDialogCommand>>)((FATaskDialog x) => x.Commands), (Action<FATaskDialog, IList<FATaskDialogCommand>>)delegate(FATaskDialog x, IList<FATaskDialogCommand> v)
	{
		x.Commands = v;
	}, (IList<FATaskDialogCommand>)null, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialog.FooterVisibility" /> property
	/// </summary>
	public static readonly StyledProperty<FATaskDialogFooterVisibility> FooterVisibilityProperty = AvaloniaProperty.Register<FATaskDialog, FATaskDialogFooterVisibility>("FooterVisibility", FATaskDialogFooterVisibility.Never, false, (BindingMode)1, (Func<FATaskDialogFooterVisibility, bool>)null, (Func<AvaloniaObject, FATaskDialogFooterVisibility, FATaskDialogFooterVisibility>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialog.IsFooterExpanded" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsFooterExpandedProperty = AvaloniaProperty.Register<FATaskDialog, bool>("IsFooterExpanded", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialog.Footer" /> property
	/// </summary>
	public static readonly StyledProperty<object> FooterProperty = AvaloniaProperty.Register<FATaskDialog, object>("Footer", (object)null, false, (BindingMode)1, (Func<object, bool>)null, (Func<AvaloniaObject, object, object>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialog.FooterTemplate" /> property
	/// </summary>
	public static readonly StyledProperty<IDataTemplate> FooterTemplateProperty = AvaloniaProperty.Register<FATaskDialog, IDataTemplate>("FooterTemplate", (IDataTemplate)null, false, (BindingMode)1, (Func<IDataTemplate, bool>)null, (Func<AvaloniaObject, IDataTemplate, IDataTemplate>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialog.ShowProgressBar" /> property
	/// </summary>
	public static readonly StyledProperty<bool> ShowProgressBarProperty = AvaloniaProperty.Register<FATaskDialog, bool>("ShowProgressBar", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialog.HeaderBackground" /> property
	/// </summary>
	public static readonly StyledProperty<IBrush> HeaderBackgroundProperty = AvaloniaProperty.Register<FATaskDialog, IBrush>("HeaderBackground", (IBrush)null, false, (BindingMode)1, (Func<IBrush, bool>)null, (Func<AvaloniaObject, IBrush, IBrush>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialog.HeaderForeground" /> property
	/// </summary>
	public static readonly StyledProperty<IBrush> HeaderForegroundProperty = AvaloniaProperty.Register<FATaskDialog, IBrush>("HeaderForeground", (IBrush)null, false, (BindingMode)1, (Func<IBrush, bool>)null, (Func<AvaloniaObject, IBrush, IBrush>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialog.IconForeground" /> property
	/// </summary>
	public static readonly StyledProperty<IBrush> IconForegroundProperty = AvaloniaProperty.Register<FATaskDialog, IBrush>("IconForeground", (IBrush)null, false, (BindingMode)1, (Func<IBrush, bool>)null, (Func<AvaloniaObject, IBrush, IBrush>)null, false);

	private IList<FATaskDialogButton> _buttons;

	private IList<FATaskDialogCommand> _commands;

	private const string s_tpButtonsHost = "ButtonsHost";

	private const string s_tpCommandsHost = "CommandsHost";

	private const string s_tpProgressBar = "ProgressBar";

	private const string s_tpMoreDetailsButton = "MoreDetailsButton";

	private const string s_pcHidden = ":hidden";

	private const string s_pcHosted = ":hosted";

	private const string s_pcSubheader = ":subheader";

	private const string s_pcFooter = ":footer";

	private const string s_pcFooterAuto = ":footerAuto";

	private const string s_pcExpanded = ":expanded";

	private const string s_pcProgress = ":progress";

	private const string s_pcProgressError = ":progressError";

	private const string s_pcProgressSuspend = ":progressSuspend";

	private const string s_pcHeaderForeground = ":headerForeground";

	private const string s_pcIconForeground = ":iconForeground";

	private const string s_cFATDCom = "FA_TaskDialogCommand";

	/// <summary>
	/// Gets or sets the title of the dialog
	/// </summary>
	/// <remarks>
	/// This is the window caption of the dialog displayed in the title bar. For platforms 
	/// where windowing is not supported, this property has no effect.
	/// </remarks>
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
	/// Gets or sets the dialog header text
	/// </summary>
	public string Header
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(HeaderProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(HeaderProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the dialog sub header text
	/// </summary>
	public string SubHeader
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(SubHeaderProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(SubHeaderProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the dialog Icon
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
	/// Gets the list of buttons that display at the bottom of the TaskDialog
	/// </summary>
	public IList<FATaskDialogButton> Buttons
	{
		get
		{
			return _buttons;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<IList<FATaskDialogButton>>((DirectPropertyBase<IList<FATaskDialogButton>>)(object)ButtonsProperty, ref _buttons, value);
		}
	}

	/// <summary>
	/// Gets the list of Commands displayed in the TaskDialog
	/// </summary>
	public IList<FATaskDialogCommand> Commands
	{
		get
		{
			return _commands;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<IList<FATaskDialogCommand>>((DirectPropertyBase<IList<FATaskDialogCommand>>)(object)CommandsProperty, ref _commands, value);
		}
	}

	/// <summary>
	/// Gets or sets the visibility of the Footer area
	/// </summary>
	public FATaskDialogFooterVisibility FooterVisibility
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FATaskDialogFooterVisibility>(FooterVisibilityProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FATaskDialogFooterVisibility>(FooterVisibilityProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets whether the footer is visible
	/// </summary>
	public bool IsFooterExpanded
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsFooterExpandedProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsFooterExpandedProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the footer content
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
	/// Gets or sets the IDataTemplate for the footer content
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
	/// Gets or sets whether this TaskDialog shows a progress bar
	/// </summary>
	public bool ShowProgressBar
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(ShowProgressBarProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(ShowProgressBarProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the background of the header region of the task dialog
	/// </summary>
	public IBrush HeaderBackground
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IBrush>(HeaderBackgroundProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IBrush>(HeaderBackgroundProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the foreground of the header text for the TaskDialog
	/// </summary>
	public IBrush HeaderForeground
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IBrush>(HeaderForegroundProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IBrush>(HeaderForegroundProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the foreground of the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialog.IconSource" /> for the TaskDialog
	/// </summary>
	public IBrush IconForeground
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IBrush>(IconForegroundProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IBrush>(IconForegroundProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the root visual that should host this dialog
	/// </summary>
	/// <remarks>
	/// For TaskDialogs declared in Xaml, this is automatically set. If you declare a 
	/// TaskDialog in C#, you MUST set this property before showing the dialog to prevent
	/// and error. For desktop platforms, set it to the Window that should own the dialog.
	/// For others, set it to the root TopLevel.
	/// </remarks>
	public Visual XamlRoot { get; set; }

	/// <summary>
	/// Raised when the TaskDialog is beginning to open, but is not yet visible
	/// </summary>
	public event TypedEventHandler<FATaskDialog, EventArgs> Opening;

	/// <summary>
	/// Raised when the TaskDialog is opened and ready to be shown on screen
	/// </summary>
	public event TypedEventHandler<FATaskDialog, EventArgs> Opened;

	/// <summary>
	/// Raised when the TaskDialog is beginning to close
	/// </summary>
	public event TypedEventHandler<FATaskDialog, FATaskDialogClosingEventArgs> Closing;

	/// <summary>
	/// Raised when the TaskDialog is closed
	/// </summary>
	public event TypedEventHandler<FATaskDialog, EventArgs> Closed;

	public FATaskDialog()
	{
		((StyledElement)this).PseudoClasses.Add(":hidden");
		_buttons = new List<FATaskDialogButton>();
		_commands = new List<FATaskDialogCommand>();
		((Interactive)this).AddHandler<RoutedEventArgs>(Button.ClickEvent, (EventHandler<RoutedEventArgs>)OnButtonClick, (RoutingStrategies)4, true);
		((Interactive)this).AddHandler<KeyEventArgs>(InputElement.KeyDownEvent, (EventHandler<KeyEventArgs>)OnKeyDownPreview, (RoutingStrategies)2, true);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		if (_moreDetailsButton != null)
		{
			_moreDetailsButton.Click -= MoreDetailsButtonClick;
		}
		((TemplatedControl)this).OnApplyTemplate(e);
		_buttonsHost = NameScopeExtensions.Get<ItemsControl>(e.NameScope, "ButtonsHost");
		_commandsHost = NameScopeExtensions.Get<ItemsControl>(e.NameScope, "CommandsHost");
		_moreDetailsButton = NameScopeExtensions.Find<Button>(e.NameScope, "MoreDetailsButton");
		_progressBar = NameScopeExtensions.Find<ProgressBar>(e.NameScope, "ProgressBar");
		if (_moreDetailsButton != null)
		{
			_moreDetailsButton.Click += MoreDetailsButtonClick;
		}
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((ContentControl)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)FooterVisibilityProperty)
		{
			FATaskDialogFooterVisibility newValue = AvaloniaPropertyChangedExtensions.GetNewValue<FATaskDialogFooterVisibility>(change);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":footerAuto", newValue == FATaskDialogFooterVisibility.Auto);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":footer", newValue != FATaskDialogFooterVisibility.Never);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":expanded", newValue == FATaskDialogFooterVisibility.Always);
		}
		else if (change.Property == (AvaloniaProperty)(object)IsFooterExpandedProperty)
		{
			if (FooterVisibility != FATaskDialogFooterVisibility.Always)
			{
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":expanded", AvaloniaPropertyChangedExtensions.GetNewValue<bool>(change));
			}
		}
		else if (change.Property == (AvaloniaProperty)(object)ShowProgressBarProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":progress", AvaloniaPropertyChangedExtensions.GetNewValue<bool>(change));
		}
		else if (change.Property == (AvaloniaProperty)(object)IconSourceProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":icon", change.NewValue != null);
		}
		else if (change.Property == (AvaloniaProperty)(object)HeaderProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":header", change.NewValue != null);
		}
		else if (change.Property == (AvaloniaProperty)(object)SubHeaderProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":subheader", change.NewValue != null);
		}
		else if (change.Property == (AvaloniaProperty)(object)HeaderForegroundProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":headerForeground", change.NewValue != null);
		}
		else if (change.Property == (AvaloniaProperty)(object)IconForegroundProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":iconForeground", change.NewValue != null);
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

	protected override void OnLoaded(RoutedEventArgs e)
	{
		((Control)this).OnLoaded(e);
		if (_isOpening)
		{
			SetButtons();
			SetCommands();
			TrySetInitialFocus();
		}
	}

	private void OnKeyDownPreview(object sender, KeyEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		if ((int)e.Key == 13)
		{
			Hide();
			((RoutedEventArgs)e).Handled = true;
		}
		else if ((int)e.Key == 6 && _defaultButton != null && ((StyledElement)_defaultButton).DataContext is FATaskDialogButton fATaskDialogButton)
		{
			ICommand command = fATaskDialogButton.Command;
			if (command != null && command.CanExecute(fATaskDialogButton.CommandParameter))
			{
				fATaskDialogButton.Command.Execute(fATaskDialogButton.CommandParameter);
			}
			fATaskDialogButton.RaiseClick();
			if (!(fATaskDialogButton is FATaskDialogCommand { ClosesOnInvoked: false }))
			{
				CloseCore(fATaskDialogButton.DialogResult);
				((RoutedEventArgs)e).Handled = true;
			}
		}
	}

	/// <summary>
	/// Shows the TaskDialog
	/// </summary>
	/// <param name="showHosted">Optional parameter that specifies whether this dialog should show in the OverlayLayer even on windowing platforms. Defaults to false</param>
	/// <returns>The TaskDialog result corresponding to the command/button used to close the dialog</returns>
	/// <remarks>
	/// Before calling this method, you MUST set <see cref="P:FluentAvalonia.UI.Controls.FATaskDialog.XamlRoot" /> property to the TopLevel/Window that should
	/// own or host this Dialog. If you declare the dialog in Xaml, this is done automatically since 
	/// the dialog is already attached to the visual tree
	/// </remarks>
	public async Task<object> ShowAsync(bool showHosted = false)
	{
		bool flag = VisualExtensions.IsAttachedToVisualTree((Visual)(object)this);
		if (!flag && XamlRoot == null)
		{
			throw new InvalidOperationException("XamlRoot not set on TaskDialog. This should be set to the TopLevel that should own or host the dialog.");
		}
		_isOpening = true;
		OnOpening();
		Visual val = (Visual)(((object)XamlRoot) ?? ((object)TopLevel.GetTopLevel((Visual)(object)this)));
		_previousFocus = TopLevel.GetTopLevel(val).FocusManager.GetFocusedElement();
		object obj;
		if (showHosted || !(val is WindowBase))
		{
			_tcs = new TaskCompletionSource<object>();
			if (flag)
			{
				UnparentDialog();
			}
			FADialogHost fADialogHost = new FADialogHost();
			((ContentControl)fADialogHost).Content = this;
			FADialogHost fADialogHost2 = (FADialogHost)(object)(_host = (Control)(object)fADialogHost);
			((AvaloniaList<Control>)(object)((Panel)(OverlayLayer.GetOverlayLayer(val) ?? throw new InvalidOperationException("Unable to find OverlayLayer for hosting the TaskDialog"))).Children).Add((Control)(object)fADialogHost2);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":hosted", true);
			((Visual)this).IsVisible = true;
			OnOpened();
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":open", true);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":hidden", false);
			obj = await _tcs.Task;
		}
		else
		{
			if (flag)
			{
				UnparentDialog();
			}
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":hidden", false);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":hosted", false);
			FAAppWindow fAAppWindow = new FAAppWindow();
			((Window)fAAppWindow).CanResize = false;
			((Window)fAAppWindow).SizeToContent = (SizeToContent)3;
			((Window)fAAppWindow).WindowStartupLocation = (WindowStartupLocation)2;
			fAAppWindow.ShowAsDialog = true;
			((ContentControl)fAAppWindow).Content = this;
			((Layoutable)fAAppWindow).MinWidth = 100.0;
			((Layoutable)fAAppWindow).MinHeight = 100.0;
			FAAppWindow fAAppWindow2 = fAAppWindow;
			if (_host == null)
			{
				((AvaloniaObject)fAAppWindow2)[!(AvaloniaProperty)(object)Window.TitleProperty] = ((AvaloniaObject)this)[!(AvaloniaProperty)(object)TitleProperty];
				((TopLevel)fAAppWindow2).Opened += delegate
				{
					OnOpened();
					TrySetInitialFocus();
				};
				((Window)fAAppWindow2).Closing += delegate(object? s, WindowClosingEventArgs e)
				{
					if (!_ignoreWindowClosingEvent)
					{
						((CancelEventArgs)(object)e).Cancel = true;
						CloseCore(FATaskDialogStandardResult.None);
					}
				};
			}
			_host = (Control)(object)fAAppWindow2;
			((Visual)this).IsVisible = true;
			obj = await ((Window)fAAppWindow2).ShowDialog<object>((Window)(object)((val is Window) ? val : null));
		}
		_isOpening = false;
		OnClosed();
		_host = null;
		IInputElement previousFocus = _previousFocus;
		if (previousFocus != null)
		{
			previousFocus.Focus((NavigationMethod)0, (KeyModifiers)0);
		}
		return obj ?? ((object)FATaskDialogStandardResult.None);
		void UnparentDialog()
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Expected O, but got Unknown
			_xamlOwner = (Control)((StyledElement)this).Parent;
			Control xamlOwner = _xamlOwner;
			Panel val2 = (Panel)(object)((xamlOwner is Panel) ? xamlOwner : null);
			if (val2 != null)
			{
				_xamlOwnerChildIndex = ((AvaloniaList<Control>)(object)val2.Children).IndexOf((Control)(object)this);
				((AvaloniaList<Control>)(object)val2.Children).RemoveAt(_xamlOwnerChildIndex);
			}
			else
			{
				Control xamlOwner2 = _xamlOwner;
				ContentControl val3 = (ContentControl)(object)((xamlOwner2 is ContentControl) ? xamlOwner2 : null);
				if (val3 != null)
				{
					val3.Content = null;
				}
				else
				{
					Control xamlOwner3 = _xamlOwner;
					ContentPresenter val4 = (ContentPresenter)(object)((xamlOwner3 is ContentPresenter) ? xamlOwner3 : null);
					if (val4 != null)
					{
						val4.Content = null;
					}
					else
					{
						Control xamlOwner4 = _xamlOwner;
						Decorator val5 = (Decorator)(object)((xamlOwner4 is Decorator) ? xamlOwner4 : null);
						if (val5 != null)
						{
							val5.Child = null;
						}
					}
				}
			}
		}
	}

	/// <summary>
	/// Hides the TaskDialog with a <see cref="F:FluentAvalonia.UI.Controls.FATaskDialogStandardResult.None" /> result
	/// </summary>
	public void Hide()
	{
		CloseCore(FATaskDialogStandardResult.None);
	}

	/// <summary>
	/// Hides the dialog with the specified dialog result
	/// </summary>
	public void Hide(object result)
	{
		CloseCore(result);
	}

	protected virtual void OnOpening()
	{
		Opening?.Invoke(this, EventArgs.Empty);
	}

	protected virtual void OnOpened()
	{
		Opened?.Invoke(this, EventArgs.Empty);
	}

	protected virtual void OnClosing(FATaskDialogClosingEventArgs args)
	{
		Closing?.Invoke(this, args);
	}

	protected virtual void OnClosed()
	{
		Closed?.Invoke(this, EventArgs.Empty);
	}

	private void CloseCore(object result)
	{
		if (_hasDeferralActive)
		{
			return;
		}
		FATaskDialogClosingEventArgs args = new FATaskDialogClosingEventArgs(result);
		FADeferral deferral = new FADeferral(delegate
		{
			Dispatcher.UIThread.VerifyAccess();
			_hasDeferralActive = false;
			if (!args.Cancel)
			{
				FinalCloseDialog(result);
			}
		});
		args.SetDeferral(deferral);
		_hasDeferralActive = true;
		args.IncrementDeferralCount();
		OnClosing(args);
		args.DecrementDeferralCount();
	}

	private async void FinalCloseDialog(object result)
	{
		Control host = _host;
		Window val = (Window)(object)((host is Window) ? host : null);
		if (val != null)
		{
			_ignoreWindowClosingEvent = true;
			val.Close(result);
			((Visual)this).IsVisible = false;
			((ContentControl)val).Content = null;
			ReturnDialogToParent();
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":open", false);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":hidden", true);
			_ignoreWindowClosingEvent = false;
			return;
		}
		Control host2 = _host;
		if (host2 is FADialogHost dh)
		{
			((InputElement)this).IsHitTestVisible = false;
			((InputElement)this).Focus((NavigationMethod)0, (KeyModifiers)0);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":open", false);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":hidden", true);
			await Task.Delay(200);
			((InputElement)this).IsHitTestVisible = true;
			((Visual)this).IsVisible = false;
			((ContentControl)dh).Content = null;
			ReturnDialogToParent();
			OverlayLayer overlayLayer = OverlayLayer.GetOverlayLayer((Visual)(object)dh);
			if (overlayLayer != null)
			{
				((AvaloniaList<Control>)(object)((Panel)overlayLayer).Children).Remove((Control)(object)dh);
				_tcs.TrySetResult(result);
			}
		}
		void ReturnDialogToParent()
		{
			if (_xamlOwner != null)
			{
				Control xamlOwner = _xamlOwner;
				Panel val2 = (Panel)(object)((xamlOwner is Panel) ? xamlOwner : null);
				if (val2 != null)
				{
					((AvaloniaList<Control>)(object)val2.Children).Insert(_xamlOwnerChildIndex, (Control)(object)this);
				}
				else
				{
					Control xamlOwner2 = _xamlOwner;
					Decorator val3 = (Decorator)(object)((xamlOwner2 is Decorator) ? xamlOwner2 : null);
					if (val3 != null)
					{
						val3.Child = (Control)(object)this;
					}
					else
					{
						Control xamlOwner3 = _xamlOwner;
						ContentControl val4 = (ContentControl)(object)((xamlOwner3 is ContentControl) ? xamlOwner3 : null);
						if (val4 != null)
						{
							val4.Content = this;
						}
						else
						{
							Control xamlOwner4 = _xamlOwner;
							ContentPresenter val5 = (ContentPresenter)(object)((xamlOwner4 is ContentPresenter) ? xamlOwner4 : null);
							if (val5 != null)
							{
								val5.Content = this;
							}
						}
					}
				}
			}
		}
	}

	private void OnButtonClick(object sender, RoutedEventArgs e)
	{
		if (_hasDeferralActive)
		{
			return;
		}
		object source = e.Source;
		Visual val = (Visual)((source is Visual) ? source : null);
		if (val != null)
		{
			FATaskDialogButtonHost fATaskDialogButtonHost = VisualExtensions.FindAncestorOfType<FATaskDialogButtonHost>(val, true);
			if (fATaskDialogButtonHost != null && ((StyledElement)fATaskDialogButtonHost).DataContext is FATaskDialogControl fATaskDialogControl && !(fATaskDialogControl is FATaskDialogCommand { ClosesOnInvoked: false }))
			{
				Hide(fATaskDialogControl.DialogResult);
			}
		}
	}

	public void SetProgressBarState(double value, FATaskDialogProgressState state)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		Dispatcher.UIThread.Post((Action)delegate
		{
			if (_progressBar != null)
			{
				((RangeBase)_progressBar).Value = value;
				_progressBar.IsIndeterminate = (state & FATaskDialogProgressState.Indeterminate) == FATaskDialogProgressState.Indeterminate;
				if (_currentProgressState != state)
				{
					_currentProgressState = state;
					PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":progressError", (state & FATaskDialogProgressState.Error) == FATaskDialogProgressState.Error);
					PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":progressSuspend", (state & FATaskDialogProgressState.Suspended) == FATaskDialogProgressState.Suspended);
				}
			}
		}, default(DispatcherPriority));
	}

	private void MoreDetailsButtonClick(object sender, RoutedEventArgs e)
	{
		IsFooterExpanded = !IsFooterExpanded;
	}

	private void SetButtons()
	{
		if (_buttons == null)
		{
			return;
		}
		List<FATaskDialogButtonHost> list = new List<FATaskDialogButtonHost>();
		bool flag = false;
		for (int i = 0; i < _buttons.Count; i++)
		{
			FATaskDialogButton fATaskDialogButton = _buttons[i];
			FATaskDialogButtonHost fATaskDialogButtonHost = new FATaskDialogButtonHost();
			IndexerDescriptor val = !(AvaloniaProperty)(object)ContentControl.ContentProperty;
			((AvaloniaObject)fATaskDialogButtonHost)[val] = ((AvaloniaObject)_buttons[i])[!(AvaloniaProperty)(object)FATaskDialogControl.TextProperty];
			IndexerDescriptor val2 = !(AvaloniaProperty)(object)FATaskDialogButtonHost.IconSourceProperty;
			((AvaloniaObject)fATaskDialogButtonHost)[val2] = ((AvaloniaObject)fATaskDialogButton)[!(AvaloniaProperty)(object)FATaskDialogButton.IconSourceProperty];
			((StyledElement)fATaskDialogButtonHost).DataContext = fATaskDialogButton;
			IndexerDescriptor val3 = !(AvaloniaProperty)(object)InputElement.IsEnabledProperty;
			((AvaloniaObject)fATaskDialogButtonHost)[val3] = ((AvaloniaObject)fATaskDialogButton)[!(AvaloniaProperty)(object)FATaskDialogControl.IsEnabledProperty];
			IndexerDescriptor val4 = !(AvaloniaProperty)(object)Button.CommandParameterProperty;
			((AvaloniaObject)fATaskDialogButtonHost)[val4] = ((AvaloniaObject)fATaskDialogButton)[!(AvaloniaProperty)(object)FATaskDialogButton.CommandParameterProperty];
			IndexerDescriptor val5 = !(AvaloniaProperty)(object)Button.CommandProperty;
			((AvaloniaObject)fATaskDialogButtonHost)[val5] = ((AvaloniaObject)fATaskDialogButton)[!(AvaloniaProperty)(object)FATaskDialogButton.CommandProperty];
			FATaskDialogButtonHost fATaskDialogButtonHost2 = fATaskDialogButtonHost;
			if (fATaskDialogButton.IsDefault)
			{
				if (flag)
				{
					throw new InvalidOperationException("Cannot set 'IsDefault' property on more than one item in a TaskDialog");
				}
				flag = true;
				((AvaloniaList<string>)(object)((StyledElement)fATaskDialogButtonHost2).Classes).Add("accent");
				_defaultButton = (Button)(object)fATaskDialogButtonHost2;
			}
			list.Add(fATaskDialogButtonHost2);
		}
		_buttonsHost.ItemsSource = list;
	}

	private void SetCommands()
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Expected O, but got Unknown
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Expected O, but got Unknown
		if (_commands == null)
		{
			return;
		}
		List<Control> list = new List<Control>();
		bool flag = _defaultButton != null;
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < _commands.Count; i++)
		{
			if (_commands[i] is FATaskDialogCheckBox fATaskDialogCheckBox)
			{
				CheckBox val = new CheckBox
				{
					[!(AvaloniaProperty)(object)ContentControl.ContentProperty] = ((AvaloniaObject)fATaskDialogCheckBox)[!(AvaloniaProperty)(object)FATaskDialogControl.TextProperty],
					DataContext = fATaskDialogCheckBox,
					[!(AvaloniaProperty)(object)InputElement.IsEnabledProperty] = ((AvaloniaObject)fATaskDialogCheckBox)[!(AvaloniaProperty)(object)FATaskDialogControl.IsEnabledProperty],
					[!(AvaloniaProperty)(object)ToggleButton.IsCheckedProperty] = ((AvaloniaObject)fATaskDialogCheckBox)[!(AvaloniaProperty)(object)FATaskDialogRadioButton.IsCheckedProperty]
				};
				((AvaloniaList<string>)(object)((StyledElement)val).Classes).Add("FA_TaskDialogCommand");
				list.Add((Control)(object)val);
				continue;
			}
			if (_commands[i] is FATaskDialogRadioButton fATaskDialogRadioButton)
			{
				RadioButton val5 = new RadioButton
				{
					[!(AvaloniaProperty)(object)ContentControl.ContentProperty] = ((AvaloniaObject)fATaskDialogRadioButton)[!(AvaloniaProperty)(object)FATaskDialogControl.TextProperty],
					DataContext = fATaskDialogRadioButton,
					[!(AvaloniaProperty)(object)InputElement.IsEnabledProperty] = ((AvaloniaObject)fATaskDialogRadioButton)[!(AvaloniaProperty)(object)FATaskDialogControl.IsEnabledProperty],
					[!(AvaloniaProperty)(object)ToggleButton.IsCheckedProperty] = ((AvaloniaObject)fATaskDialogRadioButton)[!(AvaloniaProperty)(object)FATaskDialogRadioButton.IsCheckedProperty]
				};
				((AvaloniaList<string>)(object)((StyledElement)val5).Classes).Add("FA_TaskDialogCommand");
				list.Add((Control)(object)val5);
				continue;
			}
			FATaskDialogCommand fATaskDialogCommand = _commands[i];
			if (fATaskDialogCommand == null)
			{
				continue;
			}
			FATaskDialogCommandHost fATaskDialogCommandHost = new FATaskDialogCommandHost();
			IndexerDescriptor val2 = !(AvaloniaProperty)(object)ContentControl.ContentProperty;
			((AvaloniaObject)fATaskDialogCommandHost)[val2] = ((AvaloniaObject)fATaskDialogCommand)[!(AvaloniaProperty)(object)FATaskDialogControl.TextProperty];
			((StyledElement)fATaskDialogCommandHost).DataContext = fATaskDialogCommand;
			IndexerDescriptor val3 = !(AvaloniaProperty)(object)InputElement.IsEnabledProperty;
			((AvaloniaObject)fATaskDialogCommandHost)[val3] = ((AvaloniaObject)fATaskDialogCommand)[!(AvaloniaProperty)(object)FATaskDialogControl.IsEnabledProperty];
			IndexerDescriptor val4 = !(AvaloniaProperty)(object)Button.CommandParameterProperty;
			((AvaloniaObject)fATaskDialogCommandHost)[val4] = ((AvaloniaObject)fATaskDialogCommand)[!(AvaloniaProperty)(object)FATaskDialogButton.CommandParameterProperty];
			IndexerDescriptor val6 = !(AvaloniaProperty)(object)Button.CommandProperty;
			((AvaloniaObject)fATaskDialogCommandHost)[val6] = ((AvaloniaObject)fATaskDialogCommand)[!(AvaloniaProperty)(object)FATaskDialogButton.CommandProperty];
			IndexerDescriptor val7 = !(AvaloniaProperty)(object)FATaskDialogButtonHost.IconSourceProperty;
			((AvaloniaObject)fATaskDialogCommandHost)[val7] = ((AvaloniaObject)fATaskDialogCommand)[!(AvaloniaProperty)(object)FATaskDialogButton.IconSourceProperty];
			FATaskDialogCommandHost fATaskDialogCommandHost2 = fATaskDialogCommandHost;
			if (fATaskDialogCommand.IsDefault)
			{
				if (flag)
				{
					throw new InvalidOperationException("Cannot set 'IsDefault' property on more than one item in a TaskDialog");
				}
				flag = true;
				((AvaloniaList<string>)(object)((StyledElement)fATaskDialogCommandHost2).Classes).Add("accent");
				_defaultButton = (Button)(object)fATaskDialogCommandHost2;
			}
			list.Add((Control)(object)fATaskDialogCommandHost2);
			if (fATaskDialogCommand.IconSource != null)
			{
				num++;
			}
			num2++;
		}
		if (num != num2)
		{
			for (int j = 0; j < list.Count; j++)
			{
				PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)list[j]).Classes, ":icon", true);
			}
		}
		_commandsHost.ItemsSource = list;
	}

	private void TrySetInitialFocus()
	{
		IInputElement focusedElement = TopLevel.GetTopLevel((Visual)(object)this).FocusManager.GetFocusedElement();
		IInputElement obj = ((focusedElement is Control) ? focusedElement : null);
		bool flag = false;
		if (((obj != null) ? VisualExtensions.FindAncestorOfType<FATaskDialog>((Visual)(object)obj, false) : null) == null)
		{
			flag = true;
		}
		if (!flag)
		{
			return;
		}
		if (_defaultButton != null)
		{
			((InputElement)_defaultButton).Focus((NavigationMethod)0, (KeyModifiers)0);
			return;
		}
		_ = TopLevel.GetTopLevel((Visual)(object)this).FocusManager;
		IInputElement val = FocusManager.FindFirstFocusableElement((IInputElement)(object)this);
		if (val != null)
		{
			val.Focus((NavigationMethod)0, (KeyModifiers)0);
		}
		else
		{
			((InputElement)this).Focus((NavigationMethod)0, (KeyModifiers)0);
		}
	}
}
