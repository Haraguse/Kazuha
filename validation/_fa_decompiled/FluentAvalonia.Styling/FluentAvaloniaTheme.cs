using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using CompiledAvaloniaXaml;
using FluentAvalonia.Interop;
using FluentAvalonia.Interop.WinRT;
using FluentAvalonia.UI.Media;

namespace FluentAvalonia.Styling;

/// <summary>
/// Theme manager for FluentAvalonia, managing various components of the Fluentv2 theme
/// like AccentColor, styles, and platform settings
/// </summary>
public class FluentAvaloniaTheme : Styles, IResourceProvider, IResourceNode
{
	[CompilerGenerated]
	private class XamlClosure_117
	{
		public static object Build_1(IServiceProvider P_0)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Expected O, but got Unknown
			XamlIlContext.Context<FluentAvaloniaTheme> context = CreateContext(P_0);
			return (object)new FontFamily(((IUriContext)context).BaseUri, "/Fonts/#Symbols");
		}

		public static XamlIlContext.Context<FluentAvaloniaTheme> CreateContext(IServiceProvider P_0)
		{
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			XamlIlContext.Context<FluentAvaloniaTheme> context = new XamlIlContext.Context<FluentAvaloniaTheme>(P_0, new object[1] { _0021AvaloniaResources.NamespaceInfo_003A_002FStyling_002FCore_002FFluentAvaloniaTheme_002Eaxaml.Singleton }, "avares://FluentAvalonia/Styling/Core/FluentAvaloniaTheme.axaml");
			if (P_0 != null)
			{
				object service = P_0.GetService(typeof(IRootObjectProvider));
				if (service != null)
				{
					service = ((IRootObjectProvider)service).RootObject;
					context.RootObject = (FluentAvaloniaTheme)service;
				}
			}
			return context;
		}
	}

	/// <summary>
	/// High Contrast Theme
	/// </summary>
	public static readonly ThemeVariant HighContrastTheme;

	private bool _hasLoaded;

	private Color? _customAccentColor;

	private bool _preferSystemTheme;

	private bool _preferUserAccentColor;

	private ResourceDictionary _accentColorsDictionary;

	private IPlatformSettings _platformSettings;

	public const string LightModeString = "Light";

	public const string DarkModeString = "Dark";

	public const string HighContrastModeString = "HighContrast";

	[CompilerGenerated]
	private static Action<object> _0021XamlIlPopulateOverride;

	/// <summary>
	/// Gets or sets whether the system font should be used on Windows. Value only applies at startup
	/// </summary>
	/// <remarks>
	/// On Windows 10, this is "Segoe UI", and Windows 11, this is "Segoe UI Variable Text".
	/// </remarks>
	public bool UseSystemFontOnWindows { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to use the current system theme (light or dark mode).
	/// </summary>
	/// <remarks>
	/// This property is respected on Windows, MacOS, and Linux. However, on linux,
	/// the detection is different depending on the user's desktop environment. On KDE,
	/// Cinnamon, LXDE and LXQt, it requires the user's theme (color scheme in the
	/// case of KDE) name to contain "dark". On GNOME or Xfce, it requires 'color-scheme'
	/// to be set to either 'prefer-light', 'prefer-dark', or 'gtk-theme' to contain 'dark'.
	/// Also note, that high contrast theme will only resolve here on Windows.
	/// </remarks>
	public bool PreferSystemTheme
	{
		get
		{
			return _preferSystemTheme;
		}
		set
		{
			if (_preferSystemTheme != value)
			{
				_preferSystemTheme = value;
				if (value)
				{
					ResolveThemeAndInitializeSystemResources();
				}
			}
		}
	}

	/// <summary>
	/// Gets or sets whether to use the current user's accent color as the resource SystemAccentColor
	/// </summary>
	/// <remarks>
	/// On Linux, accent color detection is only supported on KDE (from current scheme,
	/// from wallpaper and custom), LXQt (from selection color) and LXDE (from custom selection
	/// color).
	/// </remarks>
	public bool PreferUserAccentColor
	{
		get
		{
			return _preferUserAccentColor;
		}
		set
		{
			if (_preferUserAccentColor != value)
			{
				_preferUserAccentColor = value;
				LoadCustomAccentColor();
			}
		}
	}

	/// <summary>
	/// Gets or sets a <see cref="T:Avalonia.Media.Color" /> to use as the SystemAccentColor for the app. Note this takes precedence over the
	/// <see cref="P:FluentAvalonia.Styling.FluentAvaloniaTheme.PreferUserAccentColor" /> property and must be set to null to restore the system color, if desired
	/// </summary>
	/// <remarks>
	/// The 6 variants (3 light/3 dark) are pregenerated from the given color. FluentAvalonia makes no checks to ensure the legibility and
	/// accessibility of the chosen color and places that responsibility upon you. For more control over the accent color variants, directly
	/// override SystemAccentColor or the variants in the Application level resource dictionary.
	/// </remarks>
	public Color? CustomAccentColor
	{
		get
		{
			return _customAccentColor;
		}
		set
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			Color? customAccentColor = _customAccentColor;
			Color? val = value;
			if (customAccentColor.HasValue != val.HasValue || (customAccentColor.HasValue && customAccentColor.GetValueOrDefault() != val.GetValueOrDefault()))
			{
				_customAccentColor = value;
				if (_hasLoaded)
				{
					LoadCustomAccentColor();
				}
			}
		}
	}

	/// <summary>
	/// Gets or sets a value that determines if/when style overrides should be used to alleviate issues
	/// with text alignment in some controls caused when Segoe UI or Segoe UI Variable font
	/// families do not exist. The default value is <see cref="F:FluentAvalonia.Styling.TextVerticalAlignmentOverride.EnabledNonWindows" />
	/// </summary>
	/// <remarks>
	/// These overrides apply to controls like RadioButton, CheckBox, ComboBox where the first line of text
	/// is explicitly aligned with the control. Adding the overrides modify the styles to use VerticalAlignment=Center
	/// to get a consistent experience, at the (small) expense of breaking Fluent design principles. If your controls
	/// never use multi-line text, you'll never see the effect of this property.
	/// </remarks>
	public TextVerticalAlignmentOverride TextVerticalAlignmentOverrideBehavior { get; set; } = TextVerticalAlignmentOverride.EnabledNonWindows;

	public AvaloniaList<IResourceDictionary> MergedDictionaries { get; }

	bool IResourceNode.HasResources => true;

	/// <summary>
	/// Create new instance of <see cref="T:FluentAvalonia.Styling.FluentAvaloniaTheme" />.
	/// </summary>
	public FluentAvaloniaTheme()
	{
		MergedDictionaries = new AvaloniaList<IResourceDictionary>();
		MergedDictionaries.CollectionChanged += MergedDictionariesCollectionChanged;
		Init();
	}

	/// <inheritdoc />
	public bool TryGetResource(object key, ThemeVariant theme, out object value)
	{
		value = null;
		Application current = Application.Current;
		if (current != null && ((IResourceNode)current.Resources).TryGetResource(key, theme, ref value))
		{
			return true;
		}
		if (((Styles)this).TryGetResource(key, theme, ref value))
		{
			return true;
		}
		value = null;
		return false;
	}

	bool IResourceNode.TryGetResource(object key, ThemeVariant theme, out object value)
	{
		return TryGetResource(key, theme, out value);
	}

	private void Init()
	{
		_0021XamlIlPopulateTrampoline(this);
		ResolveThemeAndInitializeSystemResources();
		if (OperatingSystem.IsWindows())
		{
			TryLoadHighContrastThemeColors();
		}
		SetTextAlignmentOverrides();
		_hasLoaded = true;
	}

	private void ResolveThemeAndInitializeSystemResources()
	{
		ThemeVariant val = null;
		if (_platformSettings == null)
		{
			_platformSettings = Application.Current.PlatformSettings;
			_platformSettings.ColorValuesChanged += OnPlatformColorValuesChanged;
		}
		if (OperatingSystem.IsWindows())
		{
			val = ResolveWindowsSystemSettings(_platformSettings);
		}
		else if (OperatingSystem.IsLinux())
		{
			val = ResolveLinuxSystemSettings(_platformSettings);
		}
		else if (OperatingSystem.IsMacOS())
		{
			val = ResolveMacOSSystemSettings(_platformSettings);
		}
		else
		{
			if (PreferSystemTheme)
			{
				val = GetThemeFromIPlatformSettings(_platformSettings);
			}
			TryLoadMacOSAccentColor(_platformSettings);
			AddOrUpdateSystemResource("ContentControlThemeFontFamily", FontFamily.Default);
		}
		if (val != (ThemeVariant)null)
		{
			Application.Current.RequestedThemeVariant = val;
		}
	}

	private void OnPlatformColorValuesChanged(object sender, PlatformColorValues e)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Invalid comparison between Unknown and I4
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		if (OperatingSystem.IsWindows())
		{
			TryLoadHighContrastThemeColors();
		}
		if (PreferSystemTheme)
		{
			ThemeVariant requestedThemeVariant = (((int)e.ContrastPreference != 1) ? (((int)e.ThemeVariant == 0) ? ThemeVariant.Light : ThemeVariant.Dark) : HighContrastTheme);
			Application.Current.RequestedThemeVariant = requestedThemeVariant;
		}
		if (!CustomAccentColor.HasValue && PreferUserAccentColor)
		{
			if (OperatingSystem.IsWindows())
			{
				TryLoadWindowsAccentColor();
			}
			else if (OperatingSystem.IsMacOS())
			{
				TryLoadMacOSAccentColor(_platformSettings);
			}
			else if (OperatingSystem.IsLinux())
			{
				TryLoadLinuxAccentColor();
			}
		}
	}

	private ThemeVariant ResolveMacOSSystemSettings(IPlatformSettings platformSettings)
	{
		ThemeVariant result = null;
		if (PreferSystemTheme)
		{
			result = GetThemeFromIPlatformSettings(platformSettings);
		}
		if (CustomAccentColor.HasValue)
		{
			LoadCustomAccentColor();
		}
		else if (PreferUserAccentColor)
		{
			TryLoadMacOSAccentColor(platformSettings);
		}
		else
		{
			LoadDefaultAccentColor();
		}
		AddOrUpdateSystemResource("ContentControlThemeFontFamily", FontFamily.Default);
		return result;
	}

	private ThemeVariant ResolveLinuxSystemSettings(IPlatformSettings platformSettings)
	{
		ThemeVariant result = null;
		if (PreferSystemTheme)
		{
			ThemeVariant val = LinuxThemeResolver.TryLoadSystemTheme();
			result = ((!(val != (ThemeVariant)null)) ? GetThemeFromIPlatformSettings(platformSettings) : val);
		}
		if (CustomAccentColor.HasValue)
		{
			LoadCustomAccentColor();
		}
		else if (PreferUserAccentColor)
		{
			TryLoadLinuxAccentColor();
		}
		else
		{
			LoadDefaultAccentColor();
		}
		AddOrUpdateSystemResource("ContentControlThemeFontFamily", FontFamily.Default);
		return result;
	}

	private ThemeVariant GetThemeFromIPlatformSettings(IPlatformSettings platformSettings)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		PlatformColorValues colorValues = platformSettings.GetColorValues();
		if ((int)colorValues.ContrastPreference != 1)
		{
			if ((int)colorValues.ThemeVariant != 0)
			{
				return ThemeVariant.Dark;
			}
			return ThemeVariant.Light;
		}
		return HighContrastTheme;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetTextAlignmentOverrides()
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Expected O, but got Unknown
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Expected O, but got Unknown
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Expected O, but got Unknown
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Expected O, but got Unknown
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Expected O, but got Unknown
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Expected O, but got Unknown
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Expected O, but got Unknown
		if (TextVerticalAlignmentOverrideBehavior != TextVerticalAlignmentOverride.Disabled && (TextVerticalAlignmentOverrideBehavior != TextVerticalAlignmentOverride.EnabledNonWindows || !OperatingSystem.IsWindows()))
		{
			((IDictionary<object, object>)((Styles)this).Resources).Add((object)"CheckBoxPadding", (object)new Thickness(8.0, 5.0, 0.0, 5.0));
			((IDictionary<object, object>)((Styles)this).Resources).Add((object)"ComboBoxPadding", (object)new Thickness(12.0, 5.0, 0.0, 5.0));
			((IDictionary<object, object>)((Styles)this).Resources).Add((object)"ComboBoxItemThemePadding", (object)new Thickness(11.0, 5.0, 11.0, 5.0));
			((IDictionary<object, object>)((Styles)this).Resources).Add((object)"TextControlThemePadding", (object)new Thickness(10.0, 5.0, 6.0, 5.0));
			Style val = new Style((Func<Selector, Selector>)((Selector? x) => Selectors.OfType(x, typeof(CheckBox))));
			((StyleBase)val).Setters.Add((SetterBase)new Setter((AvaloniaProperty)(object)ContentControl.VerticalContentAlignmentProperty, (object)(VerticalAlignment)2));
			((Styles)this).Add((IStyle)(object)val);
			Style val2 = new Style((Func<Selector, Selector>)((Selector? x) => Selectors.OfType(x, typeof(RadioButton))));
			((StyleBase)val2).Setters.Add((SetterBase)new Setter((AvaloniaProperty)(object)ContentControl.VerticalContentAlignmentProperty, (object)(VerticalAlignment)2));
			((StyleBase)val2).Setters.Add((SetterBase)new Setter((AvaloniaProperty)(object)Decorator.PaddingProperty, (object)new Thickness(8.0, 6.0, 0.0, 6.0)));
			((Styles)this).Add((IStyle)(object)val2);
			Style val3 = new Style((Func<Selector, Selector>)((Selector? x) => Selectors.OfType<TextBlock>(Selectors.Child(Selectors.OfType<ContentControl>(Selectors.Template(Selectors.OfType<ComboBox>(x)))))));
			((StyleBase)val3).Setters.Add((SetterBase)new Setter((AvaloniaProperty)(object)Layoutable.VerticalAlignmentProperty, (object)(VerticalAlignment)2));
			((Styles)this).Add((IStyle)(object)val3);
		}
	}

	private void LoadCustomAccentColor()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		if (!_customAccentColor.HasValue)
		{
			if (PreferUserAccentColor)
			{
				if (OperatingSystem.IsWindows())
				{
					TryLoadWindowsAccentColor();
				}
				else if (OperatingSystem.IsLinux())
				{
					TryLoadLinuxAccentColor();
				}
				else
				{
					TryLoadMacOSAccentColor(_platformSettings);
				}
			}
			else
			{
				LoadDefaultAccentColor();
			}
		}
		else
		{
			Color2 color = _customAccentColor.Value;
			UpdateAccentColors(_customAccentColor.Value, color.LightenPercent(0.15f), color.LightenPercent(0.3f), color.LightenPercent(0.45f), color.LightenPercent(-0.15f), color.LightenPercent(-0.3f), color.LightenPercent(-0.45f));
		}
	}

	private void TryLoadMacOSAccentColor(IPlatformSettings platformSettings)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Color2 color = platformSettings.GetColorValues().AccentColor1;
			UpdateAccentColors(color, color.LightenPercent(0.15f), color.LightenPercent(0.3f), color.LightenPercent(0.45f), color.LightenPercent(-0.15f), color.LightenPercent(-0.3f), color.LightenPercent(-0.45f));
		}
		catch
		{
			LoadDefaultAccentColor();
		}
	}

	private void TryLoadLinuxAccentColor()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		Color2? color = LinuxThemeResolver.TryLoadAccentColor();
		if (color.HasValue)
		{
			Color2 value = color.Value;
			UpdateAccentColors(value, value.LightenPercent(0.15f), value.LightenPercent(0.3f), value.LightenPercent(0.45f), value.LightenPercent(-0.15f), value.LightenPercent(-0.3f), value.LightenPercent(-0.45f));
		}
		else
		{
			LoadDefaultAccentColor();
		}
	}

	private void LoadDefaultAccentColor()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		UpdateAccentColors(Colors.SlateBlue, Color.Parse("#7F69FF"), Color.Parse("#9B8AFF"), Color.Parse("#B9ADFF"), Color.Parse("#43339C"), Color.Parse("#33238C"), Color.Parse("#1D115C"));
	}

	private void AddOrUpdateSystemResource(object key, object value)
	{
		if (((IDictionary<object, object>)((Styles)this).Resources).ContainsKey(key))
		{
			((IDictionary<object, object>)((Styles)this).Resources)[key] = value;
		}
		else
		{
			((IDictionary<object, object>)((Styles)this).Resources).Add(key, value);
		}
	}

	private void UpdateAccentColors(Color accent, Color light1, Color light2, Color light3, Color dark1, Color dark2, Color dark3)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Expected O, but got Unknown
		if (_accentColorsDictionary != null)
		{
			((Styles)this).Resources.MergedDictionaries.Remove((IResourceProvider)(object)_accentColorsDictionary);
		}
		ResourceDictionary val = new ResourceDictionary();
		val.Add((object)"SystemAccentColor", (object)accent);
		val.Add((object)"SystemAccentColorLight1", (object)light1);
		val.Add((object)"SystemAccentColorLight2", (object)light2);
		val.Add((object)"SystemAccentColorLight3", (object)light3);
		val.Add((object)"SystemAccentColorDark1", (object)dark1);
		val.Add((object)"SystemAccentColorDark2", (object)dark2);
		val.Add((object)"SystemAccentColorDark3", (object)dark3);
		_accentColorsDictionary = val;
		((Styles)this).Resources.MergedDictionaries.Add((IResourceProvider)(object)_accentColorsDictionary);
	}

	private void MergedDictionariesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected O, but got Unknown
		if (e.OldItems != null)
		{
			foreach (IResourceDictionary oldItem in e.OldItems)
			{
				IResourceDictionary item = oldItem;
				((Styles)this).Resources.MergedDictionaries.Remove((IResourceProvider)(object)item);
			}
		}
		if (e.NewItems == null)
		{
			return;
		}
		foreach (IResourceDictionary newItem in e.NewItems)
		{
			IResourceDictionary item2 = newItem;
			((Styles)this).Resources.MergedDictionaries.Add((IResourceProvider)(object)item2);
		}
	}

	private ThemeVariant ResolveWindowsSystemSettings(IPlatformSettings platformSettings)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected O, but got Unknown
		ThemeVariant result = null;
		if (PreferSystemTheme)
		{
			result = GetThemeFromIPlatformSettings(platformSettings);
		}
		ParametrizedLogger valueOrDefault;
		if (CustomAccentColor.HasValue)
		{
			LoadCustomAccentColor();
		}
		else if (PreferUserAccentColor)
		{
			try
			{
				TryLoadWindowsAccentColor();
			}
			catch
			{
				ParametrizedLogger? val = Logger.TryGet((LogEventLevel)2, "FluentAvaloniaTheme");
				if (val.HasValue)
				{
					valueOrDefault = val.GetValueOrDefault();
					((ParametrizedLogger)(ref valueOrDefault)).Log((object)"FluentAvaloniaTheme", "Unable to create instance of ComObject IUISettings");
				}
				LoadDefaultAccentColor();
			}
		}
		else
		{
			LoadDefaultAccentColor();
		}
		if (UseSystemFontOnWindows)
		{
			try
			{
				if (OSVersionHelper.IsWindows11())
				{
					AddOrUpdateSystemResource("ContentControlThemeFontFamily", (object)new FontFamily("Segoe UI Variable"));
				}
				else
				{
					AddOrUpdateSystemResource("ContentControlThemeFontFamily", (object)new FontFamily("Segoe UI"));
				}
			}
			catch
			{
				ParametrizedLogger? val = Logger.TryGet((LogEventLevel)2, "FluentAvaloniaTheme");
				if (val.HasValue)
				{
					valueOrDefault = val.GetValueOrDefault();
					((ParametrizedLogger)(ref valueOrDefault)).Log((object)"FluentAvaloniaTheme", "Error in detecting Windows system font.");
				}
				AddOrUpdateSystemResource("ContentControlThemeFontFamily", FontFamily.Default);
			}
		}
		return result;
	}

	private void TryLoadHighContrastThemeColors()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		IUISettings settings;
		try
		{
			settings = WinRTInterop.CreateInstance<IUISettings>("Windows.UI.ViewManagement.UISettings");
		}
		catch
		{
			ParametrizedLogger? val = Logger.TryGet((LogEventLevel)2, "FluentAvaloniaTheme");
			if (val.HasValue)
			{
				ParametrizedLogger valueOrDefault = val.GetValueOrDefault();
				((ParametrizedLogger)(ref valueOrDefault)).Log((object)"FluentAvaloniaTheme", "Loading high contrast theme resources failed. Unable to create ComObject IUISettings");
			}
			return;
		}
		TryAddResource("SystemColorWindowTextColor", UIElementType.WindowText);
		TryAddResource("SystemColorGrayTextColor", UIElementType.GrayText);
		TryAddResource("SystemColorButtonFaceColor", UIElementType.ButtonFace);
		TryAddResource("SystemColorWindowColor", UIElementType.Window);
		TryAddResource("SystemColorButtonTextColor", UIElementType.ButtonText);
		TryAddResource("SystemColorHighlightColor", UIElementType.Highlight);
		TryAddResource("SystemColorHighlightTextColor", UIElementType.HighlightText);
		TryAddResource("SystemColorHotlightColor", UIElementType.Hotlight);
		void TryAddResource(string resKey, UIElementType element)
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				Color val2 = (Color)settings.UIElementColor(element);
				IResourceProvider obj2 = ((Styles)this).Resources.MergedDictionaries[0];
				IThemeVariantProvider obj3 = ((ResourceDictionary)((obj2 is ResourceDictionary) ? obj2 : null)).ThemeDictionaries[HighContrastTheme];
				((ResourceDictionary)((obj3 is ResourceDictionary) ? obj3 : null))[(object)resKey] = val2;
			}
			catch
			{
				ParametrizedLogger? val3 = Logger.TryGet((LogEventLevel)2, "FluentAvaloniaTheme");
				if (val3.HasValue)
				{
					ParametrizedLogger valueOrDefault2 = val3.GetValueOrDefault();
					((ParametrizedLogger)(ref valueOrDefault2)).Log((object)"FluentAvaloniaTheme", "Loading high contrast theme resources failed. Unable to load " + resKey + " resource");
				}
			}
		}
	}

	private void TryLoadWindowsAccentColor()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			IUISettings3 iUISettings = WinRTInterop.CreateInstance<IUISettings3>("Windows.UI.ViewManagement.UISettings");
			UpdateAccentColors((Color)iUISettings.GetColorValue(UIColorType.Accent), (Color)iUISettings.GetColorValue(UIColorType.AccentLight1), (Color)iUISettings.GetColorValue(UIColorType.AccentLight2), (Color)iUISettings.GetColorValue(UIColorType.AccentLight3), (Color)iUISettings.GetColorValue(UIColorType.AccentDark1), (Color)iUISettings.GetColorValue(UIColorType.AccentDark2), (Color)iUISettings.GetColorValue(UIColorType.AccentDark3));
		}
		catch
		{
			ParametrizedLogger? val = Logger.TryGet((LogEventLevel)2, "FluentAvaloniaTheme");
			if (val.HasValue)
			{
				ParametrizedLogger valueOrDefault = val.GetValueOrDefault();
				((ParametrizedLogger)(ref valueOrDefault)).Log((object)"FluentAvaloniaTheme", "Loading system accent color failed, using fallback (SlateBlue)");
			}
			LoadDefaultAccentColor();
		}
	}

	/// <summary>
	/// On Windows, forces a specific <see cref="T:Avalonia.Controls.Window" /> to the current theme
	/// </summary>
	/// <param name="window">The window to force</param>
	/// <param name="theme">The theme to use, or null to use the current RequestedTheme</param>
	/// <exception cref="T:System.ArgumentNullException">If window is null</exception>
	public void ForceWin32WindowToTheme(Window window, ThemeVariant theme = null)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		if (window == null)
		{
			throw new ArgumentNullException("window");
		}
		if (!OperatingSystem.IsWindows())
		{
			return;
		}
		try
		{
			Win32Interop.ApplyTheme(((TopLevel)window).TryGetPlatformHandle().Handle, theme == ThemeVariant.Dark);
		}
		catch
		{
			ParametrizedLogger? val = Logger.TryGet((LogEventLevel)2, "FluentAvaloniaTheme");
			if (val.HasValue)
			{
				ParametrizedLogger valueOrDefault = val.GetValueOrDefault();
				((ParametrizedLogger)(ref valueOrDefault)).Log((object)"FluentAvaloniaTheme", "Unable to set window to theme.");
			}
		}
	}

	static FluentAvaloniaTheme()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		HighContrastTheme = new ThemeVariant((object)"HighContrast", ThemeVariant.Light);
	}

	[CompilerGenerated]
	private unsafe static void _0021XamlIlPopulate(IServiceProvider P_0, FluentAvaloniaTheme P_1)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		//IL_0095: Expected O, but got Unknown
		XamlIlContext.Context<FluentAvaloniaTheme> context = new XamlIlContext.Context<FluentAvaloniaTheme>(P_0, new object[1] { _0021AvaloniaResources.NamespaceInfo_003A_002FStyling_002FCore_002FFluentAvaloniaTheme_002Eaxaml.Singleton }, "avares://FluentAvalonia/Styling/Core/FluentAvaloniaTheme.axaml")
		{
			RootObject = P_1,
			IntermediateRoot = P_1
		};
		FluentAvaloniaTheme fluentAvaloniaTheme2;
		FluentAvaloniaTheme fluentAvaloniaTheme = (fluentAvaloniaTheme2 = P_1);
		context.PushParent(fluentAvaloniaTheme2);
		ResourceDictionary val = new ResourceDictionary();
		ResourceDictionary val2 = val;
		context.PushParent(val2);
		val2.MergedDictionaries.Add((IResourceProvider)(object)_0021AvaloniaResources.Build_003A_002FStyling_002FStylesV2_002FFluentv2Colors_002Eaxaml(XamlIlRuntimeHelpers.CreateRootServiceProviderV3((IServiceProvider)context)));
		val2.MergedDictionaries.Add((IResourceProvider)(object)_0021AvaloniaResources.Build_003A_002FStyling_002FStylesV2_002FFluentv2_002Eaxaml(XamlIlRuntimeHelpers.CreateRootServiceProviderV3((IServiceProvider)context)));
		val2.AddDeferred((object)"SymbolThemeFontFamily", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((IntPtr)(nint)(delegate*<IServiceProvider, object>)(&XamlClosure_117.Build_1), (IServiceProvider)context));
		context.PopParent();
		((Styles)fluentAvaloniaTheme2).Resources = (IResourceDictionary)val;
		((Styles)fluentAvaloniaTheme2).Add((IStyle)(object)_0021AvaloniaResources.Build_003A_002FStyling_002FControlThemes_002FControls_002Eaxaml(XamlIlRuntimeHelpers.CreateRootServiceProviderV3((IServiceProvider)context)));
		context.PopParent();
		StyledElement val3;
		if ((val3 = (StyledElement)(object)((fluentAvaloniaTheme is StyledElement) ? fluentAvaloniaTheme : null)) != null)
		{
			NameScope.SetNameScope(val3, context.AvaloniaNameScope);
		}
		context.AvaloniaNameScope.Complete();
	}

	[CompilerGenerated]
	private static void _0021XamlIlPopulateTrampoline(FluentAvaloniaTheme P_0)
	{
		if (_0021XamlIlPopulateOverride != null)
		{
			_0021XamlIlPopulateOverride(P_0);
		}
		else
		{
			_0021XamlIlPopulate(XamlIlRuntimeHelpers.CreateRootServiceProviderV3((IServiceProvider)null), P_0);
		}
	}
}
