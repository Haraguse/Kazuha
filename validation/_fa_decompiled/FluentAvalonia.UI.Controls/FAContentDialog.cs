using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Presents a asynchronous dialog to the user.
/// </summary>
[PseudoClasses(new string[] { ":hidden", ":open" })]
[PseudoClasses(new string[] { ":primary", ":secondary", ":close" })]
[PseudoClasses(new string[] { ":fullsize" })]
[TemplatePart("PrimaryButton", typeof(Button))]
[TemplatePart("SecondaryButton", typeof(Button))]
[TemplatePart("CloseButton", typeof(Button))]
public class FAContentDialog : ContentControl, ICustomKeyboardNavigation
{
	private IInputElement _lastFocus;

	private Control _originalHost;

	private int _originalHostIndex;

	private FADialogHost _host;

	private FAContentDialogResult _result;

	private TaskCompletionSource<FAContentDialogResult> _tcs;

	private Button _primaryButton;

	private Button _secondaryButton;

	private Button _closeButton;

	private bool _hasDeferralActive;

	private Visual _hotkeyDownVisual;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAContentDialog.CloseButtonCommand" /> property
	/// </summary>
	public static readonly StyledProperty<ICommand> CloseButtonCommandProperty = AvaloniaProperty.Register<FAContentDialog, ICommand>("CloseButtonCommand", (ICommand)null, false, (BindingMode)1, (Func<ICommand, bool>)null, (Func<AvaloniaObject, ICommand, ICommand>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAContentDialog.CloseButtonCommandParameter" /> property
	/// </summary>
	public static readonly StyledProperty<object> CloseButtonCommandParameterProperty = AvaloniaProperty.Register<FAContentDialog, object>("CloseButtonCommandParameter", (object)null, false, (BindingMode)1, (Func<object, bool>)null, (Func<AvaloniaObject, object, object>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAContentDialog.CloseButtonText" /> property
	/// </summary>
	public static readonly StyledProperty<string> CloseButtonTextProperty = AvaloniaProperty.Register<FAContentDialog, string>("CloseButtonText", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAContentDialog.DefaultButton" /> property
	/// </summary>
	public static readonly StyledProperty<FAContentDialogButton> DefaultButtonProperty = AvaloniaProperty.Register<FAContentDialog, FAContentDialogButton>("DefaultButton", FAContentDialogButton.None, false, (BindingMode)1, (Func<FAContentDialogButton, bool>)null, (Func<AvaloniaObject, FAContentDialogButton, FAContentDialogButton>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAContentDialog.IsPrimaryButtonEnabled" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsPrimaryButtonEnabledProperty = AvaloniaProperty.Register<FAContentDialog, bool>("IsPrimaryButtonEnabled", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAContentDialog.IsSecondaryButtonEnabled" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsSecondaryButtonEnabledProperty = AvaloniaProperty.Register<FAContentDialog, bool>("IsSecondaryButtonEnabled", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAContentDialog.PrimaryButtonCommand" /> property
	/// </summary>
	public static readonly StyledProperty<ICommand> PrimaryButtonCommandProperty = AvaloniaProperty.Register<FAContentDialog, ICommand>("PrimaryButtonCommand", (ICommand)null, false, (BindingMode)1, (Func<ICommand, bool>)null, (Func<AvaloniaObject, ICommand, ICommand>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAContentDialog.PrimaryButtonCommandParameter" /> property
	/// </summary>
	public static readonly StyledProperty<object> PrimaryButtonCommandParameterProperty = AvaloniaProperty.Register<FAContentDialog, object>("PrimaryButtonCommandParameter", (object)null, false, (BindingMode)1, (Func<object, bool>)null, (Func<AvaloniaObject, object, object>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAContentDialog.PrimaryButtonText" /> property
	/// </summary>
	public static readonly StyledProperty<string> PrimaryButtonTextProperty = AvaloniaProperty.Register<FAContentDialog, string>("PrimaryButtonText", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAContentDialog.SecondaryButtonCommand" /> property
	/// </summary>
	public static readonly StyledProperty<ICommand> SecondaryButtonCommandProperty = AvaloniaProperty.Register<FAContentDialog, ICommand>("SecondaryButtonCommand", (ICommand)null, false, (BindingMode)1, (Func<ICommand, bool>)null, (Func<AvaloniaObject, ICommand, ICommand>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAContentDialog.SecondaryButtonCommandParameter" /> property
	/// </summary>
	public static readonly StyledProperty<object> SecondaryButtonCommandParameterProperty = AvaloniaProperty.Register<FAContentDialog, object>("SecondaryButtonCommandParameter", (object)null, false, (BindingMode)1, (Func<object, bool>)null, (Func<AvaloniaObject, object, object>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAContentDialog.SecondaryButtonText" /> property
	/// </summary>
	public static readonly StyledProperty<string> SecondaryButtonTextProperty = AvaloniaProperty.Register<FAContentDialog, string>("SecondaryButtonText", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAContentDialog.Title" /> property
	/// </summary>
	public static readonly StyledProperty<object> TitleProperty = AvaloniaProperty.Register<FAContentDialog, object>("Title", (object)"", false, (BindingMode)1, (Func<object, bool>)null, (Func<AvaloniaObject, object, object>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAContentDialog.TitleTemplate" /> property
	/// </summary>
	public static readonly StyledProperty<IDataTemplate> TitleTemplateProperty = AvaloniaProperty.Register<FAContentDialog, IDataTemplate>("TitleTemplate", (IDataTemplate)null, false, (BindingMode)1, (Func<IDataTemplate, bool>)null, (Func<AvaloniaObject, IDataTemplate, IDataTemplate>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAContentDialog.FullSizeDesired" /> property
	/// </summary>
	public static readonly StyledProperty<bool> FullSizeDesiredProperty = AvaloniaProperty.Register<FAContentDialog, bool>("FullSizeDesired", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	private const string s_tpPrimaryButton = "PrimaryButton";

	private const string s_tpSecondaryButton = "SecondaryButton";

	private const string s_tpCloseButton = "CloseButton";

	private const string s_pcPrimary = ":primary";

	private const string s_pcSecondary = ":secondary";

	private const string s_pcClose = ":close";

	private const string s_pcFullSize = ":fullsize";

	/// <summary>
	/// Gets or sets the command to invoke when the close button is tapped.
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
	/// Gets or sets the parameter to pass to the command for the close button.
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
	/// Gets or sets the text to display on the close button.
	/// </summary>
	public string CloseButtonText
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(CloseButtonTextProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(CloseButtonTextProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates which button on the dialog is the default action.
	/// </summary>
	public FAContentDialogButton DefaultButton
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAContentDialogButton>(DefaultButtonProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAContentDialogButton>(DefaultButtonProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets whether the dialog's primary button is enabled.
	/// </summary>
	public bool IsPrimaryButtonEnabled
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsPrimaryButtonEnabledProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsPrimaryButtonEnabledProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets whether the dialog's secondary button is enabled.
	/// </summary>
	public bool IsSecondaryButtonEnabled
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsSecondaryButtonEnabledProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsSecondaryButtonEnabledProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the command to invoke when the primary button is tapped.
	/// </summary>
	public ICommand PrimaryButtonCommand
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<ICommand>(PrimaryButtonCommandProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<ICommand>(PrimaryButtonCommandProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the parameter to pass to the command for the primary button.
	/// </summary>
	public object PrimaryButtonCommandParameter
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<object>(PrimaryButtonCommandParameterProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<object>(PrimaryButtonCommandParameterProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the text to display on the primary button.
	/// </summary>
	public string PrimaryButtonText
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(PrimaryButtonTextProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(PrimaryButtonTextProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the command to invoke when the secondary button is tapped.
	/// </summary>
	public ICommand SecondaryButtonCommand
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<ICommand>(SecondaryButtonCommandProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<ICommand>(SecondaryButtonCommandProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the parameter to pass to the command for the secondary button.
	/// </summary>
	public object SecondaryButtonCommandParameter
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<object>(SecondaryButtonCommandParameterProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<object>(SecondaryButtonCommandParameterProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the text to be displayed on the secondary button.
	/// </summary>
	public string SecondaryButtonText
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(SecondaryButtonTextProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(SecondaryButtonTextProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the title of the dialog.
	/// </summary>
	public object Title
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<object>(TitleProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<object>(TitleProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the title template.
	/// </summary>
	public IDataTemplate TitleTemplate
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IDataTemplate>(TitleTemplateProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IDataTemplate>(TitleTemplateProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets whether the Dialog should show full screen
	/// On WinUI3, at least desktop, this just show the dialog at 
	/// the maximum size of a contentdialog.
	/// </summary>
	public bool FullSizeDesired
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(FullSizeDesiredProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(FullSizeDesiredProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Occurs before the dialog is opened
	/// </summary>
	public event TypedEventHandler<FAContentDialog, EventArgs> Opening;

	/// <summary>
	/// Occurs after the dialog is opened.
	/// </summary>
	public event TypedEventHandler<FAContentDialog, EventArgs> Opened;

	/// <summary>
	/// Occurs after the dialog starts to close, but before it is closed and before the Closed event occurs.
	/// </summary>
	public event TypedEventHandler<FAContentDialog, FAContentDialogClosingEventArgs> Closing;

	/// <summary>
	/// Occurs after the dialog is closed.
	/// </summary>
	public event TypedEventHandler<FAContentDialog, FAContentDialogClosedEventArgs> Closed;

	/// <summary>
	/// Occurs after the primary button has been tapped.
	/// </summary>
	public event TypedEventHandler<FAContentDialog, FAContentDialogButtonClickEventArgs> PrimaryButtonClick;

	/// <summary>
	/// Occurs after the secondary button has been tapped.
	/// </summary>
	public event TypedEventHandler<FAContentDialog, FAContentDialogButtonClickEventArgs> SecondaryButtonClick;

	/// <summary>
	/// Occurs after the close button has been tapped.
	/// </summary>
	public event TypedEventHandler<FAContentDialog, FAContentDialogButtonClickEventArgs> CloseButtonClick;

	public FAContentDialog()
	{
		((StyledElement)this).PseudoClasses.Add(":hidden");
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		if (_primaryButton != null)
		{
			_primaryButton.Click -= OnButtonClick;
		}
		if (_secondaryButton != null)
		{
			_secondaryButton.Click -= OnButtonClick;
		}
		if (_closeButton != null)
		{
			_closeButton.Click -= OnButtonClick;
		}
		((TemplatedControl)this).OnApplyTemplate(e);
		_primaryButton = NameScopeExtensions.Get<Button>(e.NameScope, "PrimaryButton");
		_primaryButton.Click += OnButtonClick;
		_secondaryButton = NameScopeExtensions.Get<Button>(e.NameScope, "SecondaryButton");
		_secondaryButton.Click += OnButtonClick;
		_closeButton = NameScopeExtensions.Get<Button>(e.NameScope, "CloseButton");
		_closeButton.Click += OnButtonClick;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((ContentControl)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)FullSizeDesiredProperty)
		{
			OnFullSizedDesiredChanged(change);
		}
	}

	protected override bool RegisterContentPresenter(ContentPresenter presenter)
	{
		if (((StyledElement)presenter).Name == "Content")
		{
			return true;
		}
		return ((ContentControl)this).RegisterContentPresenter(presenter);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Invalid comparison between Unknown and I4
		((InputElement)this).OnKeyDown(e);
		if (!((RoutedEventArgs)e).Handled && ((int)e.Key == 6 || (int)e.Key == 13))
		{
			object source = ((RoutedEventArgs)e).Source;
			_hotkeyDownVisual = (Visual)((source is Visual) ? source : null);
		}
	}

	protected override void OnKeyUp(KeyEventArgs e)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Invalid comparison between Unknown and I4
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Invalid comparison between O and Unknown
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Invalid comparison between Unknown and I4
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Invalid comparison between Unknown and I4
		if (((RoutedEventArgs)e).Handled)
		{
			((Control)this).OnKeyUp(e);
			return;
		}
		if (((int)e.Key == 6 || (int)e.Key == 13) && (_hotkeyDownVisual == null || (object)_hotkeyDownVisual != (object)(Visual)((RoutedEventArgs)e).Source))
		{
			((Control)this).OnKeyUp(e);
			return;
		}
		Key key = e.Key;
		if ((int)key != 6)
		{
			if ((int)key == 13)
			{
				HideCore();
				((RoutedEventArgs)e).Handled = true;
			}
		}
		else
		{
			switch (DefaultButton)
			{
			case FAContentDialogButton.Primary:
				OnButtonClick(_primaryButton, null);
				goto default;
			case FAContentDialogButton.Secondary:
				OnButtonClick(_secondaryButton, null);
				goto default;
			case FAContentDialogButton.Close:
				OnButtonClick(_closeButton, null);
				goto default;
			default:
				((RoutedEventArgs)e).Handled = true;
				break;
			case FAContentDialogButton.None:
				break;
			}
		}
		((Control)this).OnKeyUp(e);
	}

	/// <summary>
	/// Begins an asynchronous operation to show the dialog.
	/// </summary>
	public Task<FAContentDialogResult> ShowAsync()
	{
		return ShowAsyncCoreForTopLevel(null);
	}

	/// <summary>
	/// Begins an asynchronous operation to show the dialog using the specified window
	/// </summary>
	public Task<FAContentDialogResult> ShowAsync(Window w)
	{
		return ShowAsyncCoreForTopLevel((TopLevel)(object)w);
	}

	/// <summary>
	/// Begins an asynchronous operation to show the dialog using the specified top level
	/// </summary>
	/// <remarks>
	/// Use this when an ApplicationLifetime is unavailable (such as in headless unit tests)
	/// </remarks>
	public Task<FAContentDialogResult> ShowAsync(TopLevel tl)
	{
		return ShowAsyncCoreForTopLevel(tl);
	}

	/// <summary>
	/// Shows the content dialog on the specified window asynchronously.
	/// </summary>
	/// <remarks>
	/// Note that the placement parameter is not implemented and only accepts <see cref="F:FluentAvalonia.UI.Controls.FAContentDialogPlacement.Popup" />
	/// </remarks>
	private Task<FAContentDialogResult> ShowAsyncCore(Window window, FAContentDialogPlacement placement = FAContentDialogPlacement.Popup)
	{
		return ShowAsyncCoreForTopLevel((TopLevel)(object)window);
	}

	private async Task<FAContentDialogResult> ShowAsyncCoreForTopLevel(TopLevel topLevel)
	{
		_tcs = new TaskCompletionSource<FAContentDialogResult>();
		OnOpening();
		if (((StyledElement)this).Parent != null)
		{
			_originalHost = (Control)((StyledElement)this).Parent;
			Control originalHost = _originalHost;
			Panel val = (Panel)(object)((originalHost is Panel) ? originalHost : null);
			if (val == null)
			{
				Decorator val2 = (Decorator)(object)((originalHost is Decorator) ? originalHost : null);
				if (val2 == null)
				{
					ContentControl val3 = (ContentControl)(object)((originalHost is ContentControl) ? originalHost : null);
					if (val3 == null)
					{
						ContentPresenter val4 = (ContentPresenter)(object)((originalHost is ContentPresenter) ? originalHost : null);
						if (val4 != null)
						{
							val4.Content = null;
						}
					}
					else
					{
						val3.Content = null;
					}
				}
				else
				{
					val2.Child = null;
				}
			}
			else
			{
				_originalHostIndex = ((AvaloniaList<Control>)(object)val.Children).IndexOf((Control)(object)this);
				((AvaloniaList<Control>)(object)val.Children).Remove((Control)(object)this);
			}
		}
		if (_host == null)
		{
			_host = new FADialogHost();
		}
		((ContentControl)_host).Content = this;
		OverlayLayer overlayLayer;
		if (topLevel != null)
		{
			overlayLayer = OverlayLayer.GetOverlayLayer((Visual)(object)topLevel);
		}
		else
		{
			IApplicationLifetime applicationLifetime = Application.Current.ApplicationLifetime;
			IClassicDesktopStyleApplicationLifetime val5 = (IClassicDesktopStyleApplicationLifetime)(object)((applicationLifetime is IClassicDesktopStyleApplicationLifetime) ? applicationLifetime : null);
			if (val5 != null)
			{
				IReadOnlyList<Window> windows = val5.Windows;
				for (int i = 0; i < windows.Count; i++)
				{
					if (((WindowBase)windows[i]).IsActive)
					{
						topLevel = (TopLevel)(object)windows[i];
						break;
					}
				}
				if (topLevel == null)
				{
					if (val5.MainWindow == null)
					{
						throw new NotSupportedException("No TopLevel root found to parent ContentDialog");
					}
					topLevel = (TopLevel)(object)val5.MainWindow;
				}
				overlayLayer = OverlayLayer.GetOverlayLayer((Visual)(object)topLevel);
			}
			else
			{
				IApplicationLifetime applicationLifetime2 = Application.Current.ApplicationLifetime;
				ISingleViewApplicationLifetime val6 = (ISingleViewApplicationLifetime)(object)((applicationLifetime2 is ISingleViewApplicationLifetime) ? applicationLifetime2 : null);
				if (val6 == null)
				{
					throw new InvalidOperationException("No TopLevel found for ContentDialog and no ApplicationLifetime is set. Please either supply a valid ApplicationLifetime or TopLevel to ShowAsync()");
				}
				topLevel = TopLevel.GetTopLevel((Visual)(object)val6.MainView);
				overlayLayer = OverlayLayer.GetOverlayLayer((Visual)(object)val6.MainView);
			}
		}
		if (overlayLayer == null)
		{
			throw new InvalidOperationException("Unable to find OverlayLayer from given TopLevel");
		}
		_lastFocus = topLevel.FocusManager.GetFocusedElement();
		((AvaloniaList<Control>)(object)((Panel)overlayLayer).Children).Add((Control)(object)_host);
		((Visual)this).IsVisible = true;
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":hidden", false);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":open", true);
		((Control)this).Loaded += DialogLoaded;
		return await _tcs.Task;
	}

	/// <summary>
	/// Closes the current <see cref="T:FluentAvalonia.UI.Controls.FAContentDialog" /> without a result (<see cref="T:FluentAvalonia.UI.Controls.FAContentDialogResult" />.<see cref="F:FluentAvalonia.UI.Controls.FAContentDialogResult.None" />)
	/// </summary>
	public void Hide()
	{
		Hide(FAContentDialogResult.None);
	}

	/// <summary>
	/// Closes the current <see cref="T:FluentAvalonia.UI.Controls.FAContentDialog" /> with the given <see cref="T:FluentAvalonia.UI.Controls.FAContentDialogResult" /> <para>ddd</para>
	/// </summary>
	/// <param name="dialogResult">The <see cref="T:FluentAvalonia.UI.Controls.FAContentDialogResult" /> to return</param>
	public void Hide(FAContentDialogResult dialogResult)
	{
		_result = dialogResult;
		HideCore();
	}

	/// <summary>
	/// Called when the primary button is invoked
	/// </summary>
	protected virtual void OnPrimaryButtonClick(FAContentDialogButtonClickEventArgs args)
	{
		PrimaryButtonClick?.Invoke(this, args);
	}

	/// <summary>
	/// Called when the secondary button is invoked
	/// </summary>
	protected virtual void OnSecondaryButtonClick(FAContentDialogButtonClickEventArgs args)
	{
		SecondaryButtonClick?.Invoke(this, args);
	}

	/// <summary>
	/// Called when the close button is invoked
	/// </summary>
	protected virtual void OnCloseButtonClick(FAContentDialogButtonClickEventArgs args)
	{
		CloseButtonClick?.Invoke(this, args);
	}

	/// <summary>
	/// Called when the ContentDialog is requested to be opened
	/// </summary>
	protected virtual void OnOpening()
	{
		Opening?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Called after the ContentDialog is initialized but just before its presented on screen
	/// </summary>
	protected virtual void OnOpened()
	{
		Opened?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Called when the ContentDialog has been requested to close, but before it actually closes
	/// </summary>
	/// <param name="args"></param>
	protected virtual void OnClosing(FAContentDialogClosingEventArgs args)
	{
		Closing?.Invoke(this, args);
	}

	/// <summary>
	/// Called when the ContentDialog has been closed and removed from the tree
	/// </summary>
	protected virtual void OnClosed(FAContentDialogClosedEventArgs args)
	{
		Closed?.Invoke(this, args);
	}

	private void HideCore()
	{
		if (_hasDeferralActive)
		{
			return;
		}
		FAContentDialogClosingEventArgs args = new FAContentDialogClosingEventArgs(_result);
		FADeferral deferral = new FADeferral(delegate
		{
			Dispatcher.UIThread.VerifyAccess();
			_hasDeferralActive = false;
			if (!args.Cancel)
			{
				FinalCloseDialog();
			}
		});
		args.SetDeferral(deferral);
		_hasDeferralActive = true;
		args.IncrementDeferralCount();
		OnClosing(args);
		args.DecrementDeferralCount();
	}

	internal void SetupDialog()
	{
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Expected O, but got Unknown
		if (_primaryButton == null)
		{
			throw new InvalidOperationException("Attempted to setup ContentDialog but the template has not been applied yet.");
		}
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":primary", !string.IsNullOrEmpty(PrimaryButtonText));
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":secondary", !string.IsNullOrEmpty(SecondaryButtonText));
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":close", !string.IsNullOrEmpty(CloseButtonText));
		_ = ((ContentControl)this).Presenter;
		switch (DefaultButton)
		{
		case FAContentDialogButton.Primary:
			if (((Visual)_primaryButton).IsVisible)
			{
				((AvaloniaList<string>)(object)((StyledElement)_primaryButton).Classes).Add("accent");
				((AvaloniaList<string>)(object)((StyledElement)_secondaryButton).Classes).Remove("accent");
				((AvaloniaList<string>)(object)((StyledElement)_closeButton).Classes).Remove("accent");
				((InputElement)_primaryButton).Focus((NavigationMethod)0, (KeyModifiers)0);
			}
			return;
		case FAContentDialogButton.Secondary:
			if (((Visual)_secondaryButton).IsVisible)
			{
				((AvaloniaList<string>)(object)((StyledElement)_secondaryButton).Classes).Add("accent");
				((AvaloniaList<string>)(object)((StyledElement)_primaryButton).Classes).Remove("accent");
				((AvaloniaList<string>)(object)((StyledElement)_closeButton).Classes).Remove("accent");
				((InputElement)_secondaryButton).Focus((NavigationMethod)0, (KeyModifiers)0);
			}
			return;
		case FAContentDialogButton.Close:
			if (((Visual)_closeButton).IsVisible)
			{
				((AvaloniaList<string>)(object)((StyledElement)_closeButton).Classes).Add("accent");
				((AvaloniaList<string>)(object)((StyledElement)_primaryButton).Classes).Remove("accent");
				((AvaloniaList<string>)(object)((StyledElement)_secondaryButton).Classes).Remove("accent");
				((InputElement)_closeButton).Focus((NavigationMethod)0, (KeyModifiers)0);
			}
			return;
		}
		((AvaloniaList<string>)(object)((StyledElement)_closeButton).Classes).Remove("accent");
		((AvaloniaList<string>)(object)((StyledElement)_primaryButton).Classes).Remove("accent");
		((AvaloniaList<string>)(object)((StyledElement)_secondaryButton).Classes).Remove("accent");
		IFocusManager focusManager = TopLevel.GetTopLevel((Visual)(object)this).FocusManager;
		FindNextElementOptions val = new FindNextElementOptions();
		val.set_SearchRoot((InputElement)(object)this);
		IInputElement obj = focusManager.FindNextElement((NavigationDirection)0, val);
		if (obj != null)
		{
			obj.Focus((NavigationMethod)0, (KeyModifiers)0);
		}
	}

	private async void FinalCloseDialog()
	{
		((InputElement)this).IsHitTestVisible = false;
		((InputElement)this).Focus((NavigationMethod)0, (KeyModifiers)0);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":hidden", true);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":open", false);
		await Task.Delay(200);
		OnClosed(new FAContentDialogClosedEventArgs(_result));
		if (_lastFocus != null)
		{
			_lastFocus.Focus((NavigationMethod)0, (KeyModifiers)0);
			_lastFocus = null;
		}
		OverlayLayer overlayLayer = OverlayLayer.GetOverlayLayer((Visual)(object)_host);
		if (overlayLayer == null)
		{
			return;
		}
		((AvaloniaList<Control>)(object)((Panel)overlayLayer).Children).Remove((Control)(object)_host);
		((ContentControl)_host).Content = null;
		if (_originalHost != null)
		{
			Control originalHost = _originalHost;
			Panel val = (Panel)(object)((originalHost is Panel) ? originalHost : null);
			if (val != null)
			{
				((AvaloniaList<Control>)(object)val.Children).Insert(_originalHostIndex, (Control)(object)this);
			}
			else
			{
				Control originalHost2 = _originalHost;
				Decorator val2 = (Decorator)(object)((originalHost2 is Decorator) ? originalHost2 : null);
				if (val2 != null)
				{
					val2.Child = (Control)(object)this;
				}
				else
				{
					Control originalHost3 = _originalHost;
					ContentControl val3 = (ContentControl)(object)((originalHost3 is ContentControl) ? originalHost3 : null);
					if (val3 != null)
					{
						val3.Content = this;
					}
					else
					{
						Control originalHost4 = _originalHost;
						ContentPresenter val4 = (ContentPresenter)(object)((originalHost4 is ContentPresenter) ? originalHost4 : null);
						if (val4 != null)
						{
							val4.Content = this;
						}
					}
				}
			}
		}
		_hotkeyDownVisual = null;
		_tcs.TrySetResult(_result);
	}

	private void OnButtonClick(object sender, RoutedEventArgs e)
	{
		if (_hasDeferralActive)
		{
			return;
		}
		FAContentDialogButtonClickEventArgs args = new FAContentDialogButtonClickEventArgs();
		FADeferral deferral = new FADeferral(delegate
		{
			Dispatcher.UIThread.VerifyAccess();
			_hasDeferralActive = false;
			if (!args.Cancel)
			{
				if (sender == _primaryButton)
				{
					if (PrimaryButtonCommand != null && PrimaryButtonCommand.CanExecute(PrimaryButtonCommandParameter))
					{
						PrimaryButtonCommand.Execute(PrimaryButtonCommandParameter);
					}
					_result = FAContentDialogResult.Primary;
				}
				else if (sender == _secondaryButton)
				{
					if (SecondaryButtonCommand != null && SecondaryButtonCommand.CanExecute(SecondaryButtonCommandParameter))
					{
						SecondaryButtonCommand.Execute(SecondaryButtonCommandParameter);
					}
					_result = FAContentDialogResult.Secondary;
				}
				else if (sender == _closeButton)
				{
					if (CloseButtonCommand != null && CloseButtonCommand.CanExecute(CloseButtonCommandParameter))
					{
						CloseButtonCommand.Execute(CloseButtonCommandParameter);
					}
					_result = FAContentDialogResult.None;
				}
				HideCore();
			}
		});
		args.SetDeferral(deferral);
		_hasDeferralActive = true;
		args.IncrementDeferralCount();
		if (sender == _primaryButton)
		{
			OnPrimaryButtonClick(args);
		}
		else if (sender == _secondaryButton)
		{
			OnSecondaryButtonClick(args);
		}
		else if (sender == _closeButton)
		{
			OnCloseButtonClick(args);
		}
		args.DecrementDeferralCount();
	}

	private void OnFullSizedDesiredChanged(AvaloniaPropertyChangedEventArgs e)
	{
		bool flag = (bool)e.NewValue;
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":fullsize", flag);
	}

	public (bool handled, IInputElement next) GetNext(IInputElement element, NavigationDirection direction)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Invalid comparison between Unknown and I4
		List<IInputElement> list = VisualExtensions.GetVisualDescendants((Visual)(object)this).OfType<IInputElement>().Where(delegate(IInputElement x)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Expected O, but got Unknown
			return KeyboardNavigation.GetIsTabStop((InputElement)x) && x.Focusable && x.IsEffectivelyVisible && ((InputElement)this).IsEffectivelyEnabled;
		})
			.ToList();
		if (list.Count == 0)
		{
			return (handled: false, next: null);
		}
		IInputElement focusedElement = TopLevel.GetTopLevel((Visual)(object)this).FocusManager.GetFocusedElement();
		if (focusedElement == null)
		{
			return (handled: false, next: null);
		}
		if ((int)direction == 0)
		{
			for (int num = 0; num < list.Count; num++)
			{
				if (list[num] == focusedElement)
				{
					if (num == list.Count - 1)
					{
						return (handled: true, next: list[0]);
					}
					return (handled: true, next: list[num + 1]);
				}
			}
		}
		else if ((int)direction == 1)
		{
			for (int num2 = list.Count - 1; num2 >= 0; num2--)
			{
				if (list[num2] == focusedElement)
				{
					if (num2 == 0)
					{
						return (handled: true, next: list[list.Count - 1]);
					}
					return (handled: true, next: list[num2 - 1]);
				}
			}
		}
		return (handled: false, next: null);
	}

	private void DialogLoaded(object sender, RoutedEventArgs args)
	{
		((Control)this).Loaded -= DialogLoaded;
		SetupDialog();
		((Layoutable)this).UpdateLayout();
		OnOpened();
	}
}
