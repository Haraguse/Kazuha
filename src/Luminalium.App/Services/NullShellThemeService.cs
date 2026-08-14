using Luminalium.App.ViewModels;

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

    public void UseSystemAccent()
    {
    }

    public void ApplyAccent(string accentKey)
    {
    }
}
