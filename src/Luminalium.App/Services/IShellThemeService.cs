using Luminalium.App.ViewModels;
using Luminalium.Theming;

namespace Luminalium.App.Services;

public interface IShellThemeService
{
    void Apply(ShellThemeMode mode);

    void UseSystemAccent();

    void ApplyAccent(string accentKey);

    void ApplyMonetPalette(MonetPalette palette);
}
