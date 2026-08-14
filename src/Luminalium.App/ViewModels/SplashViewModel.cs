using Luminalium.Core.Identity;

namespace Luminalium.App.ViewModels;

public sealed class SplashViewModel(string versionDisplay)
{
    public string ProductName { get; } = ProductIdentity.DisplayName;

    public string VersionDisplay { get; } = versionDisplay;
}
