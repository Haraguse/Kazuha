using Luminalium.App.ViewModels;

namespace Luminalium.App.Services;

public interface IShellThemeService
{
    void Apply(ShellThemeMode mode);

    void UseSystemAccent();

    void ApplyAccent(string accentKey);
}
