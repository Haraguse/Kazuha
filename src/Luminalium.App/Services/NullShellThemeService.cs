using Luminalium.App.ViewModels;
using Luminalium.Theming;

namespace Luminalium.App.Services;

public sealed class NullShellThemeService : IShellThemeService
{
    public static NullShellThemeService Instance { get; } = new();

    private NullShellThemeService()
    {
    }

    public void Apply(ShellThemeMode mode)
    {
    }

    public void ApplyFontFamily(string resolvedFontFamily)
    {
    }

    public void UseSystemAccent()
    {
    }

    public void ApplyAccent(string accentKey)
    {
    }

    public void ApplyMonetPalette(MonetPalette palette)
    {
    }
}
