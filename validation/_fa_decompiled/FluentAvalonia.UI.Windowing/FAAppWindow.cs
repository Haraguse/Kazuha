using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using FluentAvalonia.Interop;
using FluentAvalonia.UI.Controls.Primitives;

namespace FluentAvalonia.UI.Windowing;

/// <summary>
/// Custom Window that supports a modern Windows look and title bar customization,
/// with a graceful fallback for MacOS and Linux
/// </summary>
public class FAAppWindow : Window
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Windowing.FAAppWindow.TemplateSettings" /> property
	/// </summary>
	public static readonly StyledProperty<FAAppWindowTemplateSettings> TemplateSettingsProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Windowing.FAAppWindow.Icon" /> property
	/// </summary>
	public static readonly StyledProperty<IImage> IconProperty;

	private SplashScreenContext _splashContext;

	private Border _templateRoot;

	private Panel _defaultTitleBar;

	private FAAppWindowTitleBar _titleBar;

	private bool _hideSizeButtons;

	private static readonly string s_TitleBarBackground;

	private static readonly string s_TitleBarForeground;

	private static readonly string s_TitleBarInactiveBackground;

	private static readonly string s_TitleBarInactiveForeground;

	private static readonly string s_SysCaptionBackground;

	private static readonly string s_SysCaptionForeground;

	private static readonly string s_SysCaptionBackgroundHover;

	private static readonly string s_SysCaptionForegroundHover;

	private static readonly string s_SysCaptionBackgroundPressed;

	private static readonly string s_SysCaptionForegroundPressed;

	private static readonly string s_SysCaptionBackgroundInactive;

	private static readonly string s_SysCaptionForegroundInactive;

	private Win32WindowManager _win32Manager;

	/// <summary>
	/// Provides calculated data for items within the Template of AppWindow
	/// </summary>
	public FAAppWindowTemplateSettings TemplateSettings
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAAppWindowTemplateSettings>(TemplateSettingsProperty);
		}
		private set
		{
			((AvaloniaObject)this).SetValue<FAAppWindowTemplateSettings>(TemplateSettingsProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the icon for the window
	/// </summary>
	/// <remarks>
	/// Note that this type is <see cref="T:Avalonia.Media.IImage" /> and not <see cref="T:Avalonia.Controls.WindowIcon" />, like on Window
	/// This is done to allow using a window icon in managed titlebar. Provided the
	/// image is an <see cref="T:Avalonia.Media.Imaging.IBitmap" />, it should convert to a WindowIcon without 
	/// issue and you'll still get the icon in the taskbar, on other OS's, etc.
	/// </remarks>
	public IImage Icon
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IImage>(IconProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IImage>(IconProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value whether the AppWindow should hide its minimize/maximize buttons like 
	/// a dialog window. This property is only respected on Windows.
	/// </summary>
	public bool ShowAsDialog
	{
		get
		{
			return _hideSizeButtons;
		}
		set
		{
			_hideSizeButtons = value;
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":dialog", value);
			((Window)this).CanMinimize = !value;
			((Window)this).CanMaximize = !value;
		}
	}

	/// <summary>
	/// Gets or sets the splash screen that should show when the window first loads
	/// </summary>
	public IFAApplicationSplashScreen SplashScreen
	{
		get
		{
			return _splashContext?.SplashScreen;
		}
		set
		{
			if (value == null)
			{
				if (_splashContext != null)
				{
					_splashContext.Host.SplashScreen = null;
				}
				_splashContext = null;
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":splashScreen", false);
			}
			else
			{
				_splashContext = new SplashScreenContext(value);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":splashScreen", true);
			}
		}
	}

	/// <summary>
	/// Gets the Titlebar description information for the AppWindow
	/// </summary>
	/// <remarks>
	/// Use this property to customize the colors, height, and whether the window contents should
	/// display in the titlebar area
	/// </remarks>
	public FAAppWindowTitleBar TitleBar => _titleBar;

	/// <summary>
	/// Gets the interface for custom platform-specific features through the AppWindow class
	/// NOTE: Only implemented on Windows right now
	/// </summary>
	public IFAAppWindowPlatformFeatures PlatformFeatures { get; private set; }

	protected internal bool IsWindows11 { get; internal set; }

	protected internal bool IsWindows { get; internal set; }

	protected override Type StyleKeyOverride => typeof(FAAppWindow);

	public FAAppWindow()
	{
		TemplateSettings = new FAAppWindowTemplateSettings();
		_titleBar = new FAAppWindowTitleBar(this);
		((StyledElement)this).PseudoClasses.Add(":noFullScreen");
		if (OperatingSystem.IsWindows() && !Design.IsDesignMode)
		{
			InitializeAppWindow();
		}
	}

	static FAAppWindow()
	{
		TemplateSettingsProperty = AvaloniaProperty.Register<FAAppWindow, FAAppWindowTemplateSettings>("TemplateSettings", (FAAppWindowTemplateSettings)null, false, (BindingMode)1, (Func<FAAppWindowTemplateSettings, bool>)null, (Func<AvaloniaObject, FAAppWindowTemplateSettings, FAAppWindowTemplateSettings>)null, false);
		IconProperty = AvaloniaProperty.Register<FAAppWindow, IImage>("Icon", (IImage)null, false, (BindingMode)1, (Func<IImage, bool>)null, (Func<AvaloniaObject, IImage, IImage>)null, false);
		s_TitleBarBackground = "TitleBarBackground";
		s_TitleBarForeground = "TitleBarForeground";
		s_TitleBarInactiveBackground = "TitleBarBackgroundInactive";
		s_TitleBarInactiveForeground = "TitleBarForegroundInactive";
		s_SysCaptionBackground = "CaptionButtonBackground";
		s_SysCaptionForeground = "CaptionButtonForeground";
		s_SysCaptionBackgroundHover = "CaptionButtonBackgroundPointerOver";
		s_SysCaptionForegroundHover = "CaptionButtonForegroundPointerOver";
		s_SysCaptionBackgroundPressed = "CaptionButtonBackgroundPressed";
		s_SysCaptionForegroundPressed = "CaptionButtonForegroundPressed";
		s_SysCaptionBackgroundInactive = "CaptionButtonBackgroundInactive";
		s_SysCaptionForegroundInactive = "CaptionButtonForegroundInactive";
		if (OperatingSystem.IsWindows())
		{
			Window.ExtendClientAreaToDecorationsHintProperty.OverrideDefaultValue<FAAppWindow>(true);
		}
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		((Window)this).OnApplyTemplate(e);
		if (IsWindows && !Design.IsDesignMode)
		{
			_templateRoot = NameScopeExtensions.Find<Border>(e.NameScope, "RootBorder");
			_defaultTitleBar = NameScopeExtensions.Find<Panel>(e.NameScope, "DefaultTitleBar");
			OnTitleBarHeightChanged(_titleBar.Height);
			SetTitleBarColors();
		}
		if (SplashScreen != null)
		{
			FAAppSplashScreen fAAppSplashScreen = NameScopeExtensions.Find<FAAppSplashScreen>(e.NameScope, "SplashHost");
			if (fAAppSplashScreen != null)
			{
				_splashContext.Host = fAAppSplashScreen;
			}
		}
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		((Window)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)Window.ExtendClientAreaToDecorationsHintProperty)
		{
			if (IsWindows)
			{
				throw new InvalidOperationException("FAAppWindow cannot be customized with ExtendClientAreaToDecorationsHintProperty.Use the TitleBar property or a regular Avalonia window");
			}
		}
		else if (change.Property == (AvaloniaProperty)(object)IconProperty)
		{
			object newValue = change.NewValue;
			((Window)this).Icon = new WindowIcon((Bitmap)((newValue is Bitmap) ? newValue : null));
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":icon", change.NewValue != null);
		}
		else if (change.Property == (AvaloniaProperty)(object)TopLevel.ActualThemeVariantProperty)
		{
			SetTitleBarColors();
		}
	}

	protected override async void OnOpened(EventArgs e)
	{
		if (_splashContext != null && !_splashContext.HasShownSplashScreen && !Design.IsDesignMode)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":splashOpen", true);
			DateTime time = DateTime.Now;
			await _splashContext.RunJobs();
			TimeSpan timeSpan = DateTime.Now - time;
			if (timeSpan.TotalMilliseconds < (double)_splashContext.SplashScreen.MinimumShowTime)
			{
				await Task.Delay(Math.Max(1, _splashContext.SplashScreen.MinimumShowTime - (int)timeSpan.TotalMilliseconds));
			}
			LoadApp();
		}
		_003C_003En__0(e);
	}

	protected override void OnClosed(EventArgs e)
	{
		_splashContext?.TryCancel();
		((WindowBase)this).OnClosed(e);
	}

	internal void OnExtendsContentIntoTitleBarChanged(bool isExtended)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (isExtended)
		{
			TemplateSettings.IsTitleBarContentVisible = false;
			TemplateSettings.ContentMargin = default(Thickness);
		}
		else
		{
			TemplateSettings.IsTitleBarContentVisible = true;
			TemplateSettings.ContentMargin = new Thickness(0.0, _titleBar.Height, 0.0, 0.0);
		}
	}

	internal void OnTitleBarHeightChanged(double height)
	{
		TemplateSettings.TitleBarHeight = height;
		OnExtendsContentIntoTitleBarChanged(_titleBar.ExtendsContentIntoTitleBar);
	}

	internal void TitleBarColorsChanged()
	{
		SetTitleBarColors();
	}

	internal bool HitTestTitleBar(Point p)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (_defaultTitleBar == null)
		{
			return false;
		}
		if (((Point)(ref p)).Y < _titleBar.Height)
		{
			if (!ComplexHitTest(p))
			{
				return false;
			}
			return true;
		}
		return false;
	}

	internal bool ComplexHitTest(Point p)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		IInputElement obj = InputExtensions.InputHitTest((IInputElement)(object)this, p);
		InputElement val = (InputElement)(object)((obj is InputElement) ? obj : null);
		Visual val2 = (Visual)(object)val;
		if (val2 != null && ((StyledElement)val2).TemplatedParent is FATabViewListView)
		{
			return false;
		}
		if ((object)val == _defaultTitleBar)
		{
			return true;
		}
		while (val != null)
		{
			if (val.IsHitTestVisible && val.Focusable)
			{
				return false;
			}
			Visual visualParent = VisualExtensions.GetVisualParent((Visual)(object)val);
			val = (InputElement)(object)((visualParent is InputElement) ? visualParent : null);
		}
		return true;
	}

	private void SetTitleBarColors()
	{
		if (_titleBar != null)
		{
			SetResource(s_TitleBarBackground, _titleBar.BackgroundColor);
			SetResource(s_TitleBarForeground, _titleBar.ForegroundColor);
			SetResource(s_TitleBarInactiveBackground, _titleBar.InactiveBackgroundColor);
			SetResource(s_TitleBarInactiveForeground, _titleBar.InactiveForegroundColor);
			SetResource(s_SysCaptionBackground, _titleBar.ButtonBackgroundColor);
			SetResource(s_SysCaptionForeground, _titleBar.ButtonForegroundColor);
			SetResource(s_SysCaptionBackgroundHover, _titleBar.ButtonHoverBackgroundColor);
			SetResource(s_SysCaptionForegroundHover, _titleBar.ButtonHoverForegroundColor);
			SetResource(s_SysCaptionBackgroundPressed, _titleBar.ButtonPressedBackgroundColor);
			SetResource(s_SysCaptionForegroundPressed, _titleBar.ButtonPressedForegroundColor);
			SetResource(s_SysCaptionBackgroundInactive, _titleBar.ButtonInactiveBackgroundColor);
			SetResource(s_SysCaptionForegroundInactive, _titleBar.ButtonInactiveForegroundColor);
		}
		void SetResource(string name, Color? color)
		{
			if (color.HasValue)
			{
				((IDictionary<object, object>)((StyledElement)this).Resources)[(object)name] = color;
			}
			else
			{
				((IDictionary<object, object>)((StyledElement)this).Resources).Remove((object)name);
			}
		}
	}

	internal void OnShowFullScreenButtonChanged(bool value)
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":noFullScreen", value);
	}

	private async void LoadApp()
	{
		ContentPresenter presenter = ((ContentControl)this).Presenter;
		if (presenter != null)
		{
			((Visual)presenter).IsVisible = true;
			Animation val = new Animation
			{
				Duration = TimeSpan.FromMilliseconds(250L),
				FillMode = (FillMode)1
			};
			KeyFrames children = val.Children;
			KeyFrame val2 = new KeyFrame
			{
				Cue = new Cue(0.0)
			};
			val2.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)Visual.OpacityProperty, (object)1.0));
			((AvaloniaList<KeyFrame>)(object)children).Add(val2);
			KeyFrames children2 = val.Children;
			KeyFrame val3 = new KeyFrame
			{
				Cue = new Cue(1.0)
			};
			val3.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)Visual.OpacityProperty, (object)0.0));
			val3.KeySpline = new KeySpline(0.0, 0.0, 0.0, 1.0);
			((AvaloniaList<KeyFrame>)(object)children2).Add(val3);
			Animation val4 = val;
			Animation val5 = new Animation
			{
				Duration = TimeSpan.FromMilliseconds(167L)
			};
			KeyFrames children3 = val5.Children;
			KeyFrame val6 = new KeyFrame
			{
				Cue = new Cue(0.0)
			};
			val6.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)Visual.OpacityProperty, (object)0.0));
			((AvaloniaList<KeyFrame>)(object)children3).Add(val6);
			KeyFrames children4 = val5.Children;
			KeyFrame val7 = new KeyFrame
			{
				Cue = new Cue(1.0)
			};
			val7.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)Visual.OpacityProperty, (object)1.0));
			val7.KeySpline = new KeySpline(0.0, 0.0, 0.0, 1.0);
			((AvaloniaList<KeyFrame>)(object)children4).Add(val7);
			Animation val8 = val5;
			InlineArray2<Task> buffer = default(InlineArray2<Task>);
			buffer[0] = val4.RunAsync((Animatable)(object)_splashContext.Host, default(CancellationToken));
			buffer[1] = val8.RunAsync((Animatable)(object)((ContentControl)this).Presenter, default(CancellationToken));
			await Task.WhenAll(buffer);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":splashOpen", false);
			_splashContext.HasShownSplashScreen = true;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void InitializeAppWindow()
	{
		IsWindows = true;
		IsWindows11 = OSVersionHelper.IsWindows11();
		_win32Manager = new Win32WindowManager(this);
		Win32Interop.ApplyTheme((nint)_win32Manager.Hwnd, useDark: true);
		((StyledElement)this).PseudoClasses.Add(":windows");
		PlatformFeatures = new Win32AppWindowFeatures(this);
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private void _003C_003En__0(EventArgs e)
	{
		((WindowBase)this).OnOpened(e);
	}
}
