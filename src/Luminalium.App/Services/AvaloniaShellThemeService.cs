using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using Luminalium.App.ViewModels;

namespace Luminalium.App.Services;

public sealed class AvaloniaShellThemeService(Application application) : IShellThemeService
{
    public void Apply(ShellThemeMode mode)
    {
        var theme = FindTheme();
        if (mode == ShellThemeMode.System)
        {
            application.RequestedThemeVariant = ThemeVariant.Default;
            theme.PreferSystemTheme = true;
            return;
        }

        theme.PreferSystemTheme = false;
        application.RequestedThemeVariant = mode == ShellThemeMode.Dark
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }

    public void UseSystemAccent() => FindTheme().CustomAccentColor = null;

    public void ApplyAccent(string accentKey) => FindTheme().CustomAccentColor = accentKey switch
    {
        "blue" => Color.FromRgb(0, 120, 212),
        "teal" => Color.FromRgb(3, 131, 135),
        "green" => Color.FromRgb(16, 124, 16),
        "orange" => Color.FromRgb(202, 80, 16),
        "rose" => Color.FromRgb(231, 72, 86),
        _ => null,
    };

    private FluentAvaloniaTheme FindTheme() =>
        application.Styles.OfType<FluentAvaloniaTheme>().Single();
}
