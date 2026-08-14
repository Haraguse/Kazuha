using Luminalium.Core.Identity;

namespace Luminalium.App.ViewModels;

public sealed class AboutDialogViewModel(string versionDisplay)
{
    public string ProductName { get; } = ProductIdentity.DisplayName;

    public string VersionDisplay { get; } = versionDisplay;

    public string AppUserModelId { get; } = ProductIdentity.WindowsAppUserModelId;
}
