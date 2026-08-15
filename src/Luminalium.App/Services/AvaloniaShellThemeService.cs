using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using Luminalium.App.ViewModels;
using Luminalium.Theming;

namespace Luminalium.App.Services;

public sealed class AvaloniaShellThemeService : IShellThemeService
{
    private static readonly string[] MonetResourceKeys =
    [
        "LuminaliumMonetSeedColor",
        "LuminaliumMonetPrimaryColor",
        "LuminaliumMonetBackgroundBrush",
        "LuminaliumMonetSurfaceBrush",
        "LuminaliumMonetTextBrush",
        "SystemAccentColor",
        "LayerFillColorDefaultBrush",
        "CardBackgroundFillColorDefaultBrush",
        "SolidBackgroundFillColorBaseBrush",
        "SolidBackgroundFillColorSecondaryBrush",
        "TextFillColorPrimaryBrush",
        "TextFillColorSecondaryBrush",
    ];

    private readonly Application _application;
    private MonetPalette? _monetPalette;

    public AvaloniaShellThemeService(Application application)
    {
        ArgumentNullException.ThrowIfNull(application);

        _application = application;
        _application.ActualThemeVariantChanged += (_, _) => ApplyStoredMonetPalette();
    }

    public void Apply(ShellThemeMode mode)
    {
        var theme = FindTheme();
        if (mode == ShellThemeMode.System)
        {
            _application.RequestedThemeVariant = ThemeVariant.Default;
            theme.PreferSystemTheme = true;
            ApplyStoredMonetPalette();
            return;
        }

        theme.PreferSystemTheme = false;
        _application.RequestedThemeVariant = mode == ShellThemeMode.Dark
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
        ApplyStoredMonetPalette();
    }

    public void ApplyFontFamily(string resolvedFontFamily)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedFontFamily);
        _application.Resources["LuminaliumFontFamily"] = new FontFamily(resolvedFontFamily);
    }

    public void UseSystemAccent()
    {
        _monetPalette = null;
        ClearMonetResources();
        FindTheme().CustomAccentColor = null;
    }

    public void ApplyAccent(string accentKey)
    {
        _monetPalette = null;
        ClearMonetResources();
        FindTheme().CustomAccentColor = accentKey switch
        {
            "blue" => Color.FromRgb(0, 120, 212),
            "teal" => Color.FromRgb(3, 131, 135),
            "green" => Color.FromRgb(16, 124, 16),
            "orange" => Color.FromRgb(202, 80, 16),
            "rose" => Color.FromRgb(231, 72, 86),
            _ => null,
        };
    }

    public void ApplyMonetPalette(MonetPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);

        _monetPalette = palette;
        var variant = ResolveVariantPalette(palette);
        var accent = ToAvaloniaColor(variant.Primary);
        FindTheme().CustomAccentColor = accent;
        _application.Resources["LuminaliumMonetSeedColor"] = ToAvaloniaColor(palette.Seed);
        _application.Resources["LuminaliumMonetPrimaryColor"] = accent;
        _application.Resources["SystemAccentColor"] = accent;
        _application.Resources["LuminaliumMonetBackgroundBrush"] = Brush(variant.Background);
        _application.Resources["LuminaliumMonetSurfaceBrush"] = Brush(variant.Surface);
        _application.Resources["LuminaliumMonetTextBrush"] = Brush(variant.Text);
        _application.Resources["LayerFillColorDefaultBrush"] = Brush(variant.Background);
        _application.Resources["CardBackgroundFillColorDefaultBrush"] = Brush(variant.Surface);
        _application.Resources["SolidBackgroundFillColorBaseBrush"] = Brush(variant.Background);
        _application.Resources["SolidBackgroundFillColorSecondaryBrush"] = Brush(variant.Surface);
        _application.Resources["TextFillColorPrimaryBrush"] = Brush(variant.Text);
        _application.Resources["TextFillColorSecondaryBrush"] = Brush(variant.Text);
    }

    private FluentAvaloniaTheme FindTheme() =>
        _application.Styles.OfType<FluentAvaloniaTheme>().Single();

    private void ApplyStoredMonetPalette()
    {
        if (_monetPalette is not null)
        {
            ApplyMonetPalette(_monetPalette);
        }
    }

    private void ClearMonetResources()
    {
        foreach (var key in MonetResourceKeys)
        {
            _application.Resources.Remove(key);
        }
    }

    private ThemeVariantPalette ResolveVariantPalette(MonetPalette palette) =>
        _application.ActualThemeVariant == ThemeVariant.Dark || _application.RequestedThemeVariant == ThemeVariant.Dark
            ? palette.Dark
            : palette.Light;

    private static SolidColorBrush Brush(RgbColor color) =>
        new(ToAvaloniaColor(color));

    private static Color ToAvaloniaColor(RgbColor color) =>
        Color.FromRgb(color.R, color.G, color.B);
}
